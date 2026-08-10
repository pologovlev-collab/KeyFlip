using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class FileNameConverterTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        Converts("ghbdtn.zip", "привет.zip");
        Converts("руддщ.txt", "hello.txt");
        Converts("ghbdtn.exe", "привет.exe");
        Converts("ghbdtn", "привет");
        Unchanged(".gitignore");
        Converts("ghbdtn.JPG", "привет.JPG");
        Converts("ghbdtn.vbh.zip", "привет.мир.zip");
        Converts("ghbdtn vbh.zip", "привет мир.zip");
        Converts("руддщ.ntcn.txt", "hello.тест.txt");
        Unchanged("photo.JPG");
        Converts("ghbdtn.vbh.ZIP", "привет.мир.ZIP");
    }

    private void Converts(string input, string expected) => Equal(expected, FileNameConverter.ConvertForRename(input));

    private void Unchanged(string input) => Equal(input, FileNameConverter.ConvertForRename(input));

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }
}
