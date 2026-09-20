using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class FakeLanguageScorer(
    IReadOnlySet<string>? english,
    IReadOnlySet<string>? russian) : IWordLanguageScorer
{
    private static readonly HashSet<string> EnglishWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "plan", "b", "works", "vitamin", "d", "is", "important", "press", "c", "to",
        "continue", "r", "value", "z", "score", "hello", "world", "codwars", "email",
        "green", "banana", "source", "bp", "bpm", "e", "t", "kextt", "kexitt", "vjtve",
        "dsgecre", "depf", "dbpe"
    };
    private static readonly HashSet<string> RussianWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "я", "бы", "хотел", "попробовать", "пожить", "в", "другой", "стране", "надежде",
        "на", "надеюсь", "к", "моему", "выпуску", "из", "вуза", "всё", "мире", "станет",
        "сильно", "проще", "и", "визу", "получить", "тоже", "тут", "главное", "верить",
        "понимаю", "почему", "россии", "так", "сложно", "работу", "по", "какой", "причине",
        "мне", "не", "хотят", "давать", "они", "пытаются", "сделать", "мою", "дня", "день",
        "каждым", "днем", "хуже", "привет", "теперь", "у", "меня", "шагов"
    };

    internal static FakeLanguageScorer EnglishOnly() => new(EnglishWords, null);
    internal static FakeLanguageScorer RussianOnly() => new(null, RussianWords);
    internal static FakeLanguageScorer Both() => new(EnglishWords, RussianWords);
    internal static FakeLanguageScorer RecognizesEnglish(params string[] words) =>
        new(new HashSet<string>(words, StringComparer.OrdinalIgnoreCase), null);

    public bool? IsValid(string word, WordLanguage language)
    {
        var words = language == WordLanguage.English ? english : russian;
        return words?.Contains(word);
    }
}
