using KeyFlip;

namespace KeyFlip.Tests;

internal sealed record ConversionTokenDebug(
    string Original,
    string Mapped,
    string InitialDecision,
    bool? OriginalSpellResult,
    bool? MappedSpellResult,
    int OriginalScore,
    int MappedScore,
    string ClusterOriginal,
    string ClusterMapped,
    int LocalSupportingAnchors,
    int LocalOpposingAnchors,
    int ClauseConvertSupport,
    int ClauseKeepSupport,
    string FinalDecision);

internal sealed record ConversionDebugResult(
    string OutputText,
    IReadOnlyList<ConversionTokenDebug> Tokens);

internal static class ConversionDebugAnalyzer
{
    private const string EnglishClusterSymbols = "`[];',./?";
    private static readonly IReadOnlyDictionary<char, char> EnglishToRussian = CreateMap(
        ("`~", "ёЁ"),
        ("1!", "1!"), ("2@", "2\""), ("3#", "3№"), ("4$", "4;"), ("5%", "5%"),
        ("6^", "6:"), ("7&", "7?"), ("8*", "8*"), ("9(", "9("), ("0)", "0)"),
        ("-_", "-_"), ("=+", "=+"),
        ("qQ", "йЙ"), ("wW", "цЦ"), ("eE", "уУ"), ("rR", "кК"), ("tT", "еЕ"),
        ("yY", "нН"), ("uU", "гГ"), ("iI", "шШ"), ("oO", "щЩ"), ("pP", "зЗ"),
        ("[{", "хХ"), ("]}", "ъЪ"), ("\\|", "\\/"),
        ("aA", "фФ"), ("sS", "ыЫ"), ("dD", "вВ"), ("fF", "аА"), ("gG", "пП"),
        ("hH", "рР"), ("jJ", "оО"), ("kK", "лЛ"), ("lL", "дД"), (";:", "жЖ"),
        ("'\"", "эЭ"),
        ("zZ", "яЯ"), ("xX", "чЧ"), ("cC", "сС"), ("vV", "мМ"), ("bB", "иИ"),
        ("nN", "тТ"), ("mM", "ьЬ"), (",<", "бБ"), (".>", "юЮ"), ("/?", ".,"));
    private static readonly IReadOnlyDictionary<char, char> RussianToEnglish =
        EnglishToRussian.ToDictionary(pair => pair.Value, pair => pair.Key);

    internal static ConversionDebugResult Analyze(string text, IWordLanguageScorer? scorer)
    {
        var output = LayoutConverter.ConvertWithScorer(text, scorer);
        var decider = new MixedWordDecider(scorer);
        var tokens = Tokenize(text, decider, scorer);
        var debugTokens = new List<ConversionTokenDebug>(tokens.Count);

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            var (localSupport, localKeep) = CountAnchors(text, tokens, token.Direction, index, 4);
            var (clauseSupport, clauseKeep) = CountAnchors(text, tokens, token.Direction, index, int.MaxValue);
            var outputWord = output[token.Start..token.End];
            var final = outputWord == token.Mapped ? "Convert" : "Keep";
            debugTokens.Add(new ConversionTokenDebug(
                token.Original,
                token.Mapped,
                token.Decision.ToString(),
                token.Evidence.OriginalSpellResult,
                token.Evidence.ConvertedSpellResult,
                token.Evidence.OriginalScore,
                token.Evidence.ConvertedScore,
                token.ClusterOriginal,
                token.ClusterMapped,
                localSupport,
                localKeep,
                clauseSupport,
                clauseKeep,
                final));
        }

        return new ConversionDebugResult(output, debugTokens);
    }

    private static List<TokenEvidence> Tokenize(
        string text,
        MixedWordDecider decider,
        IWordLanguageScorer? scorer)
    {
        var tokens = new List<TokenEvidence>();
        for (var index = 0; index < text.Length;)
        {
            var language = GetLanguage(text[index]);
            if (language is null)
            {
                index++;
                continue;
            }

            var start = index;
            while (index < text.Length && GetLanguage(text[index]) == language) index++;
            var end = index;
            var original = text[start..end];
            var convertedLanguage = language == WordLanguage.English ? WordLanguage.Russian : WordLanguage.English;
            var direction = language == WordLanguage.English ? 1 : -1;
            var mapped = Convert(original, direction);
            var evidence = CreateEvidence(
                decider,
                scorer,
                original,
                language.Value,
                mapped,
                convertedLanguage);
            var allowNumericSuffix = IsContextualNumericSuffix(text, start, end, language.Value);
            var decision = !allowNumericSuffix && TechnicalTokenDetector.ShouldKeep(
                    text,
                    start,
                    end,
                    protectIdentifierFragments: true,
                    protectSingleCharacter: false)
                ? WordConversionDecision.HardKeep
                : evidence.Decision;
            var clusterOriginal = string.Empty;
            var clusterMapped = string.Empty;

            if (decision != WordConversionDecision.HardKeep && language == WordLanguage.English)
            {
                var clusterStart = start;
                while (clusterStart > 0 && IsEnglishClusterCharacter(text[clusterStart - 1])) clusterStart--;
                var clusterEnd = end;
                while (clusterEnd < text.Length && IsEnglishClusterCharacter(text[clusterEnd])) clusterEnd++;
                if (clusterStart != start || clusterEnd != end)
                {
                    clusterOriginal = text[clusterStart..clusterEnd];
                    clusterMapped = Convert(clusterOriginal, direction);
                    var originalLetters = new string(clusterOriginal.Where(IsEnglishLetter).ToArray());
                    var convertedLetters = new string(clusterMapped.Where(IsRussianLetter).ToArray());
                    var bonus = clusterStart < start && clusterOriginal[..(start - clusterStart)]
                        .Any(character => EnglishToRussian.TryGetValue(character, out var mappedCharacter) &&
                            mappedCharacter is ',' or '.' or '!' or '?')
                        ? 10
                        : 4;
                    evidence = CreateEvidence(
                        decider,
                        scorer,
                        originalLetters,
                        language.Value,
                        convertedLetters,
                        convertedLanguage,
                        bonus,
                        isPhysicalCluster: true);
                    if (evidence.Decision == WordConversionDecision.ConfidentConvert ||
                        decision != WordConversionDecision.ConfidentConvert)
                    {
                        decision = evidence.Decision;
                    }
                }
            }

            tokens.Add(new TokenEvidence(
                start,
                end,
                original,
                mapped,
                direction,
                decision,
                evidence,
                clusterOriginal,
                clusterMapped));
        }

        return tokens;
    }

    private static DecisionEvidence CreateEvidence(
        MixedWordDecider decider,
        IWordLanguageScorer? scorer,
        string original,
        WordLanguage originalLanguage,
        string converted,
        WordLanguage convertedLanguage,
        int conversionEvidenceBonus = 0,
        bool isPhysicalCluster = false)
    {
        var deterministic = new DeterministicWordLanguageScorer();
        return new DecisionEvidence(
            decider.Decide(
                original,
                originalLanguage,
                converted,
                convertedLanguage,
                conversionEvidenceBonus,
                isPhysicalCluster),
            scorer?.IsValid(original, originalLanguage),
            scorer?.IsValid(converted, convertedLanguage),
            deterministic.Score(original, originalLanguage),
            deterministic.Score(converted, convertedLanguage));
    }

    private static (int Support, int Keep) CountAnchors(
        string text,
        IReadOnlyList<TokenEvidence> tokens,
        int direction,
        int index,
        int window)
    {
        var support = 0;
        var keep = 0;
        if (tokens[index].Decision == WordConversionDecision.HardKeep) return (support, keep);
        var start = index;
        var end = index + 1;
        while (start > 0 && index - start < window &&
               tokens[start - 1].Decision != WordConversionDecision.HardKeep &&
               !HasContextBoundary(text, tokens[start - 1], tokens[start], direction))
        {
            start--;
        }
        while (end < tokens.Count && end - index <= window &&
               tokens[end].Decision != WordConversionDecision.HardKeep &&
               !HasContextBoundary(text, tokens[end - 1], tokens[end], direction))
        {
            end++;
        }
        for (var candidateIndex = start; candidateIndex < end; candidateIndex++)
        {
            if (candidateIndex == index) continue;
            var candidate = tokens[candidateIndex];
            if (candidate.Decision == WordConversionDecision.ConfidentConvert)
            {
                if (candidate.Direction == direction) support++;
                else keep++;
            }
            else if (candidate.Decision == WordConversionDecision.ConfidentKeep)
            {
                if (candidate.Direction != direction) support++;
                else keep++;
            }
        }

        return (support, keep);
    }

    private static bool HasContextBoundary(
        string text,
        TokenEvidence left,
        TokenEvidence right,
        int proposedDirection)
    {
        var separator = text[left.End..right.Start];
        if (separator.AsSpan().ContainsAny('\r', '\n')) return true;

        var firstWhitespace = separator.IndexOfAny([' ', '\t', '\r', '\n']);
        var lastWhitespace = separator.LastIndexOfAny([' ', '\t', '\r', '\n']);
        for (var index = 0; index < separator.Length; index++)
        {
            var raw = separator[index];
            if (char.IsWhiteSpace(raw)) continue;

            var contextDirection = firstWhitespace < 0 || index < firstWhitespace
                ? GetContextDirection(left, proposedDirection)
                : index > lastWhitespace
                    ? GetContextDirection(right, proposedDirection)
                    : 0;
            var semantic = raw;
            if (contextDirection != 0)
            {
                var map = contextDirection > 0 ? EnglishToRussian : RussianToEnglish;
                if (map.TryGetValue(raw, out var mapped)) semantic = mapped;
            }
            if (semantic is '.' or '!' or '?' &&
                (index + 1 >= separator.Length || char.IsWhiteSpace(separator[index + 1])))
            {
                return true;
            }
        }

        return false;
    }

    private static int GetContextDirection(TokenEvidence token, int proposedDirection)
    {
        if (token.Decision == WordConversionDecision.ConfidentConvert) return token.Direction;
        return token.Decision == WordConversionDecision.Ambiguous && token.Direction == proposedDirection
            ? proposedDirection
            : 0;
    }

    private static string Convert(string text, int direction)
    {
        var map = direction > 0 ? EnglishToRussian : RussianToEnglish;
        return new string(text.Select(character => map.TryGetValue(character, out var mapped) ? mapped : character).ToArray());
    }

    private static WordLanguage? GetLanguage(char character)
    {
        if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z') return WordLanguage.English;
        if (IsRussianLetter(character)) return WordLanguage.Russian;
        return null;
    }

    private static bool IsContextualNumericSuffix(
        string text,
        int start,
        int end,
        WordLanguage language)
    {
        if (language != WordLanguage.English || end - start != 1 || !char.IsLower(text[start]) ||
            start == 0 || !char.IsDigit(text[start - 1]) ||
            end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
        {
            return false;
        }

        var numberStart = start - 1;
        while (numberStart > 0 && char.IsDigit(text[numberStart - 1])) numberStart--;
        return numberStart == 0 ||
            !char.IsLetterOrDigit(text[numberStart - 1]) && text[numberStart - 1] != '_';
    }

    private static bool IsRussianLetter(char character) =>
        character is >= 'А' and <= 'Я' or >= 'а' and <= 'я' or 'Ё' or 'ё';

    private static bool IsEnglishLetter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static bool IsEnglishClusterCharacter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' ||
        EnglishClusterSymbols.Contains(character, StringComparison.Ordinal);

    private static IReadOnlyDictionary<char, char> CreateMap(params (string Source, string Target)[] keys) =>
        keys.SelectMany(pair => pair.Source.Zip(pair.Target))
            .ToDictionary(pair => pair.First, pair => pair.Second);

    private sealed record TokenEvidence(
        int Start,
        int End,
        string Original,
        string Mapped,
        int Direction,
        WordConversionDecision Decision,
        DecisionEvidence Evidence,
        string ClusterOriginal,
        string ClusterMapped);

    private readonly record struct DecisionEvidence(
        WordConversionDecision Decision,
        bool? OriginalSpellResult,
        bool? ConvertedSpellResult,
        int OriginalScore,
        int ConvertedScore);
}
