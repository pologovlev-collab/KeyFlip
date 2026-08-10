using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class LayoutConverterTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        Converts("ghbdtn", "привет");
        Converts("руддщ", "hello");
        KnownTechnicalAcronymsRemainUnchangedAsSingleTokens();
        Converts("Ghbdtn", "Привет");
        Converts("GHBDTN", "ПРИВЕТ");
        ConvertsOrKeepsAllCapsPhrase("GHBDTN RFR LTKF", "ПРИВЕТ КАК ДЕЛА");
        Converts("ghbdtn 123🙂\nvbh", "привет 123🙂\nмир");
        Converts("q,<.>", "йбБюЮ");
        Converts("q@#$^&", "й\"№;:?");
        Converts("ёЁ", "`~");
        Unchanged("abc абв");
        Unchanged("123 🙂\n");
        Unchanged(string.Empty);
        Converts("Это ghbdtn текст", "Это привет текст");
        Converts("hello руддщ world", "hello hello world");
        Converts(
            "ghbdtn rfr ltkf Xnj vyt ltkfnm! руддщ рщц фку нщг как дела",
            "привет как дела Что мне делать! hello how are you как дела");
        Converts("Это  ghbdtn,\nhello\tруддщ!", "Это  приветб\nhello\thello!");
        Converts("@#$^&", "\"№;:?");
        Converts("\"№;:?", "@#$^&");
        Unchanged("!!!");
        Converts("как руддщ дела", "как hello дела");
        Converts("руддщ рщц фку нщг", "hello how are you");
        Converts("привет руддщ мир", "привет hello мир");
        Converts("hello ghbdtn world", "hello привет world");
        Unchanged("как дела");
        Unchanged("hello world");
        Unchanged("SQL HTML CSS HTTP HTTPS API JSON XML GPT URL URI CPU GPU RAM SSD HDD IDE CLI SDK UI UX UTF ASCII TCP UDP IP DNS SSH SSL TLS REST RPC JWT UUID GUID OS DB");
        Converts("Моя почта test@example.com и ghbdtn", "Моя почта test@example.com и привет");
        Unchanged("test@example.com");
        Unchanged("https://example.com");
        Converts("Это ghbdtn@ текст", "Это привет\" текст");
        Converts("как руддщ№ дела", "как hello# дела");
        Converts("ghbdtn@", "привет\"");
        Converts("руддщ№", "hello#");
        Converts("ghbdtn\r\n", "привет\r\n");
        Converts("ghbdtn\nnext", "привет\nnext");
        Converts("ghbdtn\r", "привет\r");
        CoversEveryPhysicalKeyAndRoundTrip();
    }

    private void Converts(string input, string expected) => Equal(expected, LayoutConverter.Convert(input));

    private void Unchanged(string input) => Equal(input, LayoutConverter.Convert(input));

    private void ConvertsOrKeepsAllCapsPhrase(string input, string converted)
    {
        var actual = LayoutConverter.Convert(input);
        if (!string.Equals(actual, input, StringComparison.Ordinal) &&
            !string.Equals(actual, converted, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected unchanged '{input}' or converted '{converted}', actual '{actual}'.");
        }

        Passed++;
    }

    private void KnownTechnicalAcronymsRemainUnchangedAsSingleTokens()
    {
        var acronyms = new[]
        {
            "SQL", "API", "HTTP", "HTTPS", "HTML", "CSS", "JSON", "XML", "URL", "URI",
            "CPU", "GPU", "RAM", "SSD", "HDD", "IDE", "CLI", "SDK", "SSH", "SSL", "TLS",
            "TCP", "UDP", "IP", "DNS", "UUID", "GUID", "REST", "RPC", "JWT", "UTF", "ASCII",
            "UI", "UX", "GPT"
        };

        foreach (var acronym in acronyms) Unchanged(acronym);
        Unchanged("sql");
        Unchanged("Api");
        Unchanged("https");
    }

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
