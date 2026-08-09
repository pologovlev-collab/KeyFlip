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
        var words = CreateWordTokens(text);
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

    private static List<WordToken> CreateWordTokens(string text)
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
            if (!MixedDecider.Value.ShouldUseConverted(original, language.Value, converted, convertedLanguage))
            {
                direction = ConversionDirection.None;
            }

            words.Add(new WordToken(start, index, direction));
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
