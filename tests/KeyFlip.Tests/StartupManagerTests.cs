using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class StartupManagerTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        UsesTheCurrentExecutablePathVerbatim();
        RejectsAnUnavailableProcessPath();
    }

    private void UsesTheCurrentExecutablePathVerbatim()
    {
        Equal("\"D:\\Portable Apps\\KeyFlip.exe\"", StartupManager.BuildStartupCommand("D:\\Portable Apps\\KeyFlip.exe"));
    }

    private void RejectsAnUnavailableProcessPath()
    {
        try
        {
            StartupManager.BuildStartupCommand(null);
        }
        catch (InvalidOperationException)
        {
            Passed++;
            return;
        }

        throw new InvalidOperationException("Expected InvalidOperationException for a missing process path.");
    }

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }
}
