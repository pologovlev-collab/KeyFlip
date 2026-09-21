using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class BuildInfoTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        Equal("1.1.0", BuildInfo.Version);
        Equal("KeyFlip 1.1.0", BuildInfo.DisplayVersion);
        True(Path.IsPathFullyQualified(BuildInfo.ProcessPath));
        True(BuildInfo.StartupMetadata.Contains("version=1.1.0", StringComparison.Ordinal));
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
