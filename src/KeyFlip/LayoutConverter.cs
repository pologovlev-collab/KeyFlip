using System.Text;

namespace KeyFlip;

public static class LayoutConverter
{
    private enum ConversionDirection
    {
        None,
        EnglishToRussian,
        RussianToEnglish
    }

    private readonly record struct WordToken(
        int Start,
        int End,
        ConversionDirection Direction);

    private const string StrongEnglishSymbols = "@#$^&";
    private const string StrongRussianSymbols = "№";
    private static readonly Lazy<MixedWordDecider> MixedDecider = new(static () => new MixedWordDecider());
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

    private static readonly IReadOnlyDictionary<char, char> RussianToEnglish = CreateReverseMap(
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

    public static string Convert(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var tokenCount = CountAlphabeticTokens(text);
        if (tokenCount == 0) return ConvertSymbolOnly(text);
        if (tokenCount == 1) return ConvertSingleToken(text);
        return ConvertSmart(text);
    }

    internal static ConversionResult ConvertCodeSafe(string text) =>
        ConvertWords(text, forceSingleToken: false, protectIdentifierFragments: false);

    internal static ConversionResult ConvertCodeAware(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var safeCandidate = ConvertCodeSafe(text);
        var syntaxCandidate = ApplyConfirmedWrongLayoutSyntax(text, safeCandidate.OutputText);
        return ConversionResult.FromCharacterDifferences(text, syntaxCandidate);
    }

    internal static ConversionResult ConvertTargeted(string text, bool forceSingleToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        var outputText = CountAlphabeticTokens(text) == 0
            ? ConvertSymbolOnly(text)
            : ConvertWithTokens(text, CreateWordTokens(
                text,
                preserveTechnicalTokens: true,
                forceSingleToken,
                conservative: true));
        return ConversionResult.FromCharacterDifferences(text, outputText);
    }

    internal static ConversionResult ConvertWords(
        string text,
        bool forceSingleToken,
        bool protectIdentifierFragments = true)
    {
        ArgumentNullException.ThrowIfNull(text);
        var words = CreateWordTokens(
            text,
            preserveTechnicalTokens: true,
            forceSingleToken,
            conservative: true,
            protectIdentifierFragments);
        if (words.Count == 0) return ConversionResult.Unchanged(text);

        var edits = words
            .Where(static word => word.Direction != ConversionDirection.None)
            .Select(word => new ConversionEdit(
                word.Start,
                word.End - word.Start,
                ConvertWithMap(text[word.Start..word.End], GetMap(word.Direction))))
            .ToArray();
        return ConversionResult.FromEdits(text, edits);
    }

    private static string ConvertSingleToken(string text)
    {
        foreach (var character in text)
        {
            var language = GetLanguage(character);
            if (language is null) continue;
            return ConvertWithMap(text, language == WordLanguage.English ? EnglishToRussian : RussianToEnglish);
        }

        return text;
    }

    private static string ConvertSmart(string text)
    {
        var words = CreateWordTokens(text, preserveTechnicalTokens: true, forceSingleToken: false, conservative: false);
        return ConvertWithTokens(text, words);
    }

    private static string ConvertWithTokens(string text, IReadOnlyList<WordToken> words)
    {
        var result = new StringBuilder(text.Length);

        AppendLeadingSeparator(result, text[..words[0].Start], words[0].Direction);
        for (var index = 0; index < words.Count; index++)
        {
            var word = words[index];
            AppendWithDirection(result, text[word.Start..word.End], word.Direction);

            if (index + 1 < words.Count)
            {
                var next = words[index + 1];
                AppendBetweenWords(result, text[word.End..next.Start], word.Direction, next.Direction);
            }
        }

        AppendTrailingSeparator(result, text[words[^1].End..], words[^1].Direction);
        return result.ToString();
    }

    private static List<WordToken> CreateWordTokens(
        string text,
        bool preserveTechnicalTokens,
        bool forceSingleToken,
        bool conservative,
        bool protectIdentifierFragments = true)
    {
        var words = new List<WordToken>();
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

            var original = text[start..index];
            var convertedLanguage = language == WordLanguage.English ? WordLanguage.Russian : WordLanguage.English;
            var direction = language == WordLanguage.English
                ? ConversionDirection.EnglishToRussian
                : ConversionDirection.RussianToEnglish;
            var converted = ConvertWithMap(original, GetMap(direction));
            var shouldConvert = conservative
                ? MixedDecider.Value.ShouldUseConvertedConservatively(original, language.Value, converted, convertedLanguage)
                : MixedDecider.Value.ShouldUseConverted(original, language.Value, converted, convertedLanguage);
            var protectCurrentIdentifierFragment = protectIdentifierFragments || language == WordLanguage.English;
            if (preserveTechnicalTokens &&
                TechnicalTokenDetector.ShouldKeep(text, start, index, protectCurrentIdentifierFragment) ||
                !shouldConvert)
            {
                direction = ConversionDirection.None;
            }

            words.Add(new WordToken(start, index, direction));
        }

        if (forceSingleToken && words.Count == 1)
        {
            var word = words[0];
            var language = GetLanguage(text[word.Start])!.Value;
            words[0] = word with
            {
                Direction = language == WordLanguage.English
                    ? ConversionDirection.EnglishToRussian
                    : ConversionDirection.RussianToEnglish
            };
        }

        return words;
    }

    private static int CountAlphabeticTokens(string text)
    {
        var count = 0;
        for (var index = 0; index < text.Length;)
        {
            var language = GetLanguage(text[index]);
            if (language is null)
            {
                index++;
                continue;
            }

            count++;
            while (index < text.Length && GetLanguage(text[index]) == language) index++;
        }

        return count;
    }

    private static void AppendLeadingSeparator(StringBuilder result, string separator, ConversionDirection nextDirection)
    {
        var lastWhitespace = separator.FindLastIndex(char.IsWhiteSpace);
        result.Append(separator.AsSpan(0, lastWhitespace + 1));
        AppendWithDirection(result, separator[(lastWhitespace + 1)..], nextDirection);
    }

    private static void AppendBetweenWords(
        StringBuilder result,
        string separator,
        ConversionDirection previousDirection,
        ConversionDirection nextDirection)
    {
        var firstWhitespace = separator.FindIndex(char.IsWhiteSpace);
        if (firstWhitespace < 0)
        {
            AppendWithDirection(result, separator, previousDirection);
            return;
        }

        var lastWhitespace = separator.FindLastIndex(char.IsWhiteSpace);
        AppendWithDirection(result, separator[..firstWhitespace], previousDirection);
        result.Append(separator.AsSpan(firstWhitespace, lastWhitespace - firstWhitespace + 1));
        AppendWithDirection(result, separator[(lastWhitespace + 1)..], nextDirection);
    }

    private static void AppendTrailingSeparator(StringBuilder result, string separator, ConversionDirection previousDirection)
    {
        var firstWhitespace = separator.FindIndex(char.IsWhiteSpace);
        if (firstWhitespace < 0)
        {
            AppendWithDirection(result, separator, previousDirection);
            return;
        }

        AppendWithDirection(result, separator[..firstWhitespace], previousDirection);
        result.Append(separator.AsSpan(firstWhitespace));
    }

    private static void AppendWithDirection(StringBuilder result, string text, ConversionDirection direction)
    {
        result.Append(direction == ConversionDirection.None ? text : ConvertWithMap(text, GetMap(direction)));
    }

    private static IReadOnlyDictionary<char, char> GetMap(ConversionDirection direction) => direction switch
    {
        ConversionDirection.EnglishToRussian => EnglishToRussian,
        ConversionDirection.RussianToEnglish => RussianToEnglish,
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private static int FindIndex(this string text, Func<char, bool> predicate)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (predicate(text[index])) return index;
        }

        return -1;
    }

    private static int FindLastIndex(this string text, Func<char, bool> predicate)
    {
        for (var index = text.Length - 1; index >= 0; index--)
        {
            if (predicate(text[index])) return index;
        }

        return -1;
    }

    private static string ConvertSymbolOnly(string text)
    {
        var englishEvidence = text.Count(character => StrongEnglishSymbols.Contains(character, StringComparison.Ordinal));
        var russianEvidence = text.Count(character => StrongRussianSymbols.Contains(character, StringComparison.Ordinal));
        if (englishEvidence == russianEvidence) return text;
        return ConvertWithMap(text, englishEvidence > russianEvidence ? EnglishToRussian : RussianToEnglish);
    }

    private static string ConvertWithMap(string text, IReadOnlyDictionary<char, char> map)
    {
        return string.Create(text.Length, (text, map), static (destination, state) =>
        {
            for (var index = 0; index < state.text.Length; index++)
            {
                destination[index] = state.map.TryGetValue(state.text[index], out var converted)
                    ? converted
                    : state.text[index];
            }
        });
    }

    private static string ApplyConfirmedWrongLayoutSyntax(string source, string safeCandidate)
    {
        var result = safeCandidate.ToCharArray();
        for (var opening = source.IndexOf('Э'); opening >= 0; opening = source.IndexOf('Э', opening + 1))
        {
            var closing = source.IndexOf('Э', opening + 1);
            if (closing < 0 || !IsStringLiteralPosition(source, opening, closing)) continue;

            var content = source[(opening + 1)..closing];
            var convertedContent = ConvertWords(
                content,
                forceSingleToken: false,
                protectIdentifierFragments: false).OutputText;
            result[opening] = '"';
            convertedContent.CopyTo(0, result, opening + 1, convertedContent.Length);
            result[closing] = '"';
            if (closing + 1 < source.Length && source[closing + 1] == 'ж')
            {
                result[closing + 1] = ';';
            }

            opening = closing;
        }

        for (var index = 1; index < source.Length; index++)
        {
            if (source[index] == 'ж' && ")]}".Contains(source[index - 1], StringComparison.Ordinal) &&
                (index + 1 == source.Length || char.IsWhiteSpace(source[index + 1])))
            {
                result[index] = ';';
            }
        }

        return new string(result);
    }

    private static bool IsStringLiteralPosition(string text, int opening, int closing)
    {
        if (closing == opening + 1) return false;
        var before = FindNearestNonWhitespace(text, opening - 1, step: -1);
        var after = FindNearestNonWhitespace(text, closing + 1, step: 1);
        return (before == '\0' || "([{=,:".Contains(before, StringComparison.Ordinal)) &&
            (after == '\0' || ")]}.,;ж".Contains(after, StringComparison.Ordinal));
    }

    private static char FindNearestNonWhitespace(string text, int index, int step)
    {
        while (index >= 0 && index < text.Length)
        {
            if (!char.IsWhiteSpace(text[index])) return text[index];
            index += step;
        }

        return '\0';
    }

    private static WordLanguage? GetLanguage(char character)
    {
        if (IsLatinLetter(character)) return WordLanguage.English;
        if (IsRussianLetter(character)) return WordLanguage.Russian;
        return null;
    }

    private static bool IsLatinLetter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    private static bool IsRussianLetter(char character) =>
        character is >= 'А' and <= 'Я' or >= 'а' and <= 'я' or 'Ё' or 'ё';

    private static IReadOnlyDictionary<char, char> CreateMap(params (string Source, string Target)[] keys) =>
        keys.SelectMany(pair => pair.Source.Zip(pair.Target))
            .ToDictionary(pair => pair.First, pair => pair.Second);

    private static IReadOnlyDictionary<char, char> CreateReverseMap(params (string Source, string Target)[] keys) =>
        keys.SelectMany(pair => pair.Source.Zip(pair.Target))
            .ToDictionary(pair => pair.Second, pair => pair.First);
}
