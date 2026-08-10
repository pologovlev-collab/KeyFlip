using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class CodeSafeConversionTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        CodeLike("std::string text = \"руддщ\";");
        CodeLike("print(\"руддщ\")");
        CodeLike("{\"text\": \"руддщ\"}");
        NotCodeLike("Это ghbdtn@ текст");
        NotCodeLike("Он сказал \"привет\"");
        CodeProcessConverts("\"руддщ\"", "\"hello\"");
        CodeProcessConverts("'руддщ'", "'hello'");
        CodeProcessConverts("руддщ;", "hello;");
        CodeProcessConverts("int руддщ;", "int hello;");
        NotCodeLikeInCodeProcess("Это ghbdtn@ текст");

        Converts("std::string text = \"руддщ\";", "std::string text = \"hello\";");
        Converts("std::string text = \"ghbdtn\";", "std::string text = \"привет\";");
        Converts("const char* x = \"руддщ\";", "const char* x = \"hello\";");
        Converts("print(\"руддщ\")", "print(\"hello\")");
        Converts("{\"text\": \"руддщ\"}", "{\"text\": \"hello\"}");
        Unchanged("const API_URL = \"https://example.com\";");
        Unchanged("std::string");
        Unchanged("SQL HTTP HTTPS JSON API_URL");

        GeneralUnchanged("Use SQL HTML CSS HTTP API JSON in this project");
        GeneralUnchanged("Я использую SQL HTML CSS HTTP API JSON в этом проекте");
    }

    private void CodeLike(string text) => True(CodeLikeDetector.LooksLikeCode(text));

    private void NotCodeLike(string text) => False(CodeLikeDetector.LooksLikeCode(text));

    private void NotCodeLikeInCodeProcess(string text) => False(CodeLikeDetector.LooksLikeCode(text, isCodeProcess: true));

    private void CodeProcessConverts(string input, string expected)
    {
        True(CodeLikeDetector.LooksLikeCode(input, isCodeProcess: true));
        Equal(expected, LayoutConverter.ConvertCodeSafe(input).OutputText);
    }

    private void Converts(string input, string expected) =>
        Equal(expected, LayoutConverter.ConvertCodeSafe(input).OutputText);

    private void Unchanged(string input) => Converts(input, input);

    private void GeneralUnchanged(string input) => Equal(input, LayoutConverter.Convert(input));

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Expected true, actual false.");
        Passed++;
    }

    private void False(bool value)
    {
        if (value) throw new InvalidOperationException("Expected false, actual true.");
        Passed++;
    }
}
