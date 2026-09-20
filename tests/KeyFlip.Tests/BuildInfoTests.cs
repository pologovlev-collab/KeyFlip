using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class BuildInfoTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        Equal("1.0.1-rc3", BuildInfo.Version);
        True(Path.IsPathFullyQualified(BuildInfo.ProcessPath));
        True(BuildInfo.StartupMetadata.Contains("version=1.0.1-rc3", StringComparison.Ordinal));
        True(BuildInfo.StartupMetadata.Contains($"path={BuildInfo.ProcessPath}", StringComparison.Ordinal));
    }

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
}
