using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class DiagnosticLoggerTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        WriteFailureDoesNotEscape();
        DeletedLogIsRecreated();
    }

    private void WriteFailureDoesNotEscape()
    {
        var directory = CreateDirectory();
        try
        {
            var logger = new DiagnosticLogger(directory);
            logger.Log("TEST_STAGE");
            Passed++;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private void DeletedLogIsRecreated()
    {
        var directory = CreateDirectory();
        var path = Path.Combine(directory, "keyflip.log");
        try
        {
            var logger = new DiagnosticLogger(path);
            logger.Log("FIRST");
            File.Delete(path);
            logger.Log("SECOND");
            if (!File.Exists(path)) throw new InvalidOperationException("Logger did not recreate its file.");
            Passed++;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KeyFlip.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
