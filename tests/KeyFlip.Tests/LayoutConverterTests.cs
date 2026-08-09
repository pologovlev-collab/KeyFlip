using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class LayoutConverterTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        Converts("ghbdtn", "привет");
        Converts("руддщ", "hello");
        Converts("Ghbdtn", "Привет");
        Converts("ghbdtn 123🙂\nvbh", "привет 123🙂\nмир");
        Converts("q,<.>", "йбБюЮ");
        Converts("q@#$^&", "й\"№;:?");
        Converts("ёЁ", "`~");
        Unchanged("abc абв");
        Unchanged("123 🙂\n");
        Unchanged(string.Empty);
        CoversEveryPhysicalKeyAndRoundTrip();
    }

    private void Converts(string input, string expected) => Equal(expected, LayoutConverter.Convert(input));

    private void Unchanged(string input) => Equal(input, LayoutConverter.Convert(input));

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private void CoversEveryPhysicalKeyAndRoundTrip()
    {
        var keyPairs = new[]
        {
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
            ("nN", "тТ"), ("mM", "ьЬ"), (",<", "бБ"), (".>", "юЮ"), ("/?", ".,")
        };

        foreach (var (english, russian) in keyPairs)
        {
            for (var index = 0; index < english.Length; index++)
            {
                var en = english[index];
                var ru = russian[index];
                Equal($"й{ru}", LayoutConverter.Convert($"q{en}"));
                Equal($"q{en}", LayoutConverter.Convert($"й{ru}"));
            }
        }
    }
}
