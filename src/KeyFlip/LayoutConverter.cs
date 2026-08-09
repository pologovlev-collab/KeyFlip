namespace KeyFlip;

public static class LayoutConverter
{
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

        if (latin == cyrillic) return text;
        var map = latin > cyrillic ? EnglishToRussian : RussianToEnglish;
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
