using System.Text;

namespace KeyFlip;

public static class LayoutConverter
{
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

        var latin = 0;
        var cyrillic = 0;
        foreach (var character in text)
        {
            if (IsLatinLetter(character)) latin++;
            else if (IsRussianLetter(character)) cyrillic++;
        }

        if (latin > 0 && cyrillic == 0) return ConvertWithMap(text, EnglishToRussian);
        if (cyrillic > 0 && latin == 0) return ConvertWithMap(text, RussianToEnglish);
        if (latin > 0 && cyrillic > 0) return ConvertMixed(text);
        return ConvertSymbolOnly(text);
    }

    private static string ConvertMixed(string text)
    {
        var result = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length;)
        {
            var language = GetLanguage(text[index]);
            if (language is null)
            {
                result.Append(text[index++]);
                continue;
            }

            var start = index;
            while (index < text.Length && GetLanguage(text[index]) == language) index++;
            var original = text[start..index];
            var convertedLanguage = language == WordLanguage.English ? WordLanguage.Russian : WordLanguage.English;
            var converted = ConvertWithMap(
                original,
                language == WordLanguage.English ? EnglishToRussian : RussianToEnglish);
            result.Append(MixedDecider.Value.ShouldUseConverted(original, language.Value, converted, convertedLanguage)
                ? converted
                : original);
        }

        return result.ToString();
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
