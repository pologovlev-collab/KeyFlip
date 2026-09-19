namespace KeyFlip;

internal enum WordLanguage
{
    English,
    Russian
}

internal enum WordConversionDecision
{
    ConfidentConvert,
    HardKeep,
    Ambiguous
}

internal interface IWordLanguageScorer
{
    bool? IsValid(string word, WordLanguage language);
}

internal sealed class MixedWordDecider
{
    private readonly IWordLanguageScorer? _windowsSpellChecker = WindowsSpellChecker.TryCreate();
    private readonly DeterministicWordLanguageScorer _fallback = new();

    internal WordConversionDecision Decide(
        string original,
        WordLanguage originalLanguage,
        string converted,
        WordLanguage convertedLanguage,
        int conversionEvidenceBonus = 0)
    {
        if (original.Length == 1)
        {
            if (_fallback.IsCommonWord(original, originalLanguage)) return WordConversionDecision.HardKeep;
            if (_fallback.IsCommonWord(converted, convertedLanguage)) return WordConversionDecision.ConfidentConvert;
        }

        var originalValidity = _windowsSpellChecker?.IsValid(original, originalLanguage);
        var convertedValidity = _windowsSpellChecker?.IsValid(converted, convertedLanguage);

        if (originalValidity is true) return WordConversionDecision.HardKeep;
        if (originalValidity is false && convertedValidity is true) return WordConversionDecision.ConfidentConvert;
        if (originalValidity is null && convertedValidity is true) return WordConversionDecision.ConfidentConvert;

        var originalScore = _fallback.Score(original, originalLanguage);
        var convertedScore = _fallback.Score(converted, convertedLanguage);
        if (convertedScore + conversionEvidenceBonus >= originalScore + 3) return WordConversionDecision.ConfidentConvert;
        if (originalScore >= convertedScore + 3) return WordConversionDecision.HardKeep;
        return WordConversionDecision.Ambiguous;
    }

    internal bool ShouldUseConvertedConservatively(
        string original,
        WordLanguage originalLanguage,
        string converted,
        WordLanguage convertedLanguage)
    {
        var originalScore = _fallback.Score(original, originalLanguage);
        var convertedScore = _fallback.Score(converted, convertedLanguage);
        return convertedScore >= originalScore + 6;
    }
}

internal static class TechnicalTokenDetector
{
    private static readonly HashSet<string> ProtectedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "std", "string", "int", "char", "bool", "void", "const", "auto", "return", "class",
        "namespace", "public", "private", "protected", "include",
        "api", "url", "uri", "http", "https", "html", "css", "sql", "json", "xml", "gpt",
        "cpu", "gpu", "ram", "ssd", "hdd", "ide", "cli", "sdk", "ui", "ux", "utf", "ascii",
        "tcp", "udp", "ip", "dns", "ssh", "ssl", "tls", "rest", "rpc", "jwt", "uuid", "guid",
        "os", "db"
    };

    internal static bool ShouldKeep(
        string text,
        int start,
        int end,
        bool protectIdentifierFragments = true,
        bool protectSingleCharacter = true)
    {
        var word = text[start..end];
        if (protectSingleCharacter && word.Length == 1) return true;
        if (IsKnownProtectedWord(word)) return true;
        if (word.Any(char.IsLower) && word.Skip(1).Any(char.IsUpper)) return true;
        if (BelongsToEmailOrUrl(text, start, end)) return true;

        var touchesIdentifierCharacter = protectIdentifierFragments &&
            (start > 0 && (text[start - 1] == '_' || char.IsDigit(text[start - 1])) ||
             end < text.Length && (text[end] == '_' || char.IsDigit(text[end])));
        return touchesIdentifierCharacter;
    }

    internal static bool IsKnownProtectedWord(string word) => ProtectedWords.Contains(word);

    private static bool BelongsToEmailOrUrl(string text, int start, int end)
    {
        var clusterStart = start;
        while (clusterStart > 0 && !char.IsWhiteSpace(text[clusterStart - 1])) clusterStart--;
        var clusterEnd = end;
        while (clusterEnd < text.Length && !char.IsWhiteSpace(text[clusterEnd])) clusterEnd++;
        var cluster = text[clusterStart..clusterEnd];
        var at = cluster.IndexOf('@');
        var looksLikeEmail = at > 0 && at + 1 < cluster.Length &&
            char.IsLetterOrDigit(cluster[at - 1]) && char.IsLetterOrDigit(cluster[at + 1]);
        return looksLikeEmail || cluster.Contains("://", StringComparison.Ordinal);
    }
}

internal sealed class DeterministicWordLanguageScorer
{
    private static readonly HashSet<string> CommonEnglish = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "be", "do", "hello", "how", "i", "is", "it", "me", "the", "this", "to", "what", "world", "you"
    };

    private static readonly HashSet<string> CommonRussian = new(StringComparer.OrdinalIgnoreCase)
    {
        "а", "в", "вы", "всегда", "дела", "делать", "и", "к", "как", "мне", "могу", "мы", "на", "не", "о", "он", "она", "привет", "с", "текст", "ты", "у", "что", "это", "я"
    };

    private const string EnglishBigrams = "th he in er an re on at en nd ti es or te of ed is it al ar st to nt ng se ha as ou io le ve co me de hi ri ro ic ne ea ra ce li ch ll be ma si om ur wo rl ld yo";
    private const string RussianBigrams = "ст но то на ен ов ни ра во ко пр ро по ре го ос ка ер от ол ал та ит ет те ор ан ли ве ел не ри ва вл тр ми де ем ие ый ьн ть чт мн ла ру";

    internal int Score(string word, WordLanguage language)
    {
        var normalized = word.ToLowerInvariant();
        if (!normalized.All(character => IsLanguageLetter(character, language))) return -100;

        var commonWords = language == WordLanguage.English ? CommonEnglish : CommonRussian;
        var score = commonWords.Contains(normalized) ? 20 : 0;
        var vowels = normalized.Count(character => IsVowel(character, language));
        if (vowels == 0 && normalized.Length > 2) score -= 3;
        else if (vowels > 0 && vowels * 4 <= normalized.Length * 3) score += 1;

        var bigrams = language == WordLanguage.English ? EnglishBigrams : RussianBigrams;
        for (var index = 0; index + 1 < normalized.Length; index++)
        {
            var pair = normalized.Substring(index, 2);
            score += bigrams.Contains(pair, StringComparison.Ordinal) ? 2 : -1;
            if (normalized[index] == normalized[index + 1]) score--;
        }

        return score;
    }

    internal bool IsCommonWord(string word, WordLanguage language) =>
        (language == WordLanguage.English ? CommonEnglish : CommonRussian).Contains(word);

    private static bool IsLanguageLetter(char character, WordLanguage language) => language == WordLanguage.English
        ? character is >= 'a' and <= 'z'
        : character is >= 'а' and <= 'я' or 'ё';

    private static bool IsVowel(char character, WordLanguage language) => language == WordLanguage.English
        ? "aeiou".Contains(character, StringComparison.Ordinal)
        : "аеёиоуыэюя".Contains(character, StringComparison.Ordinal);
}
