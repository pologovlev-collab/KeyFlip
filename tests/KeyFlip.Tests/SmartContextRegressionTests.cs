using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class SmartContextRegressionTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        ConvertsTesterFailure("ntgthm e vtyz", "теперь у меня");
        ConvertsTesterFailure("rjulf z 'njn", "когда я этот");
        ConvertsTesterFailure("vjue kb z d nfrb[", "могу ли я в таких");
        ConvertsTesterFailure("d ltym bkb cgfnm d yb[", "в день или спать в них");
        ConvertsTesterFailure("gj'njve gecnm hf,jnftn", "поэтому пусть работает");
        ConvertsTesterFailure("ghj,ktve c jnj,hf;tybtv", "проблему с отображением");
        ConvertsTesterFailure("f yt 4", "а не 4");
        ConvertsTesterFailure(
            "ns pfxtv nj e,hfk ldjqye. fhbavtnbre bp ehjrf",
            "ты зачем то убрал двойную арифметику из урока");
        Converts("ldjbxye.", "двоичную");
        ConvertsTesterFailure("lj,fdm t` j,hfnyj b dc` jr", "добавь её обратно и всё ок");
        ConvertsTesterFailure("rfr vyt e;t yfljtkj", "как мне уже надоело");
        ConvertsTesterFailure("dctulf dc` yjhvfkmyj yt hf,jnftn", "всегда всё нормально не работает");
        ConvertsTesterFailure("z yt gjybvf.", "я не понимаю");
        ConvertsTesterFailure("rfr ;t vyt", "как же мне");

        Unchanged("как дела");
        Unchanged("hello world");
        Unchanged("SQL HTTP API");
        Converts("SQL и ghbdtn", "SQL и привет");
        Unchanged("я use API");
        Unchanged("он исправил проблему с отображением уровня и названия задач с codwars");
        Unchanged("API работает");
        Unchanged("Use SQL and HTTP in this project");
        Unchanged("test@example.com");
        Unchanged("https://example.com");
        Converts("Моя почта test@example.com и ghbdtn", "Моя почта test@example.com и привет");
    }

    private void ConvertsTesterFailure(string input, string expected) => Converts(input, expected);

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
}
