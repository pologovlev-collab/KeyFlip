using System.Text;

namespace KeyFlip;

internal sealed class DiagnosticLogger
{
    private const long MaximumLogSize = 128 * 1024;
    private static readonly object Sync = new();
    private readonly string _path;

    internal DiagnosticLogger() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeyFlip",
        "keyflip.log")) { }

    internal DiagnosticLogger(string path) => _path = path;

    public void Log(string stage, string? metadata = null)
    {
        try
        {
            var sanitizedMetadata = metadata?.Replace('\r', ' ').Replace('\n', ' ');
            var line = $"{DateTimeOffset.Now:O} stage={stage}" +
                (string.IsNullOrWhiteSpace(sanitizedMetadata) ? string.Empty : $" {sanitizedMetadata}") + Environment.NewLine;

            lock (Sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                if (File.Exists(_path) && new FileInfo(_path).Length >= MaximumLogSize)
                {
                    File.WriteAllText(_path, string.Empty, Encoding.UTF8);
                }

                File.AppendAllText(_path, line, Encoding.UTF8);
            }
        }
        catch (Exception)
        {
            // Diagnostics must never prevent text conversion.
        }
    }
}
