using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class ClipboardServiceTests
{
    public int Passed { get; private set; }

    public async Task RunAsync()
    {
        await AcceptsSameTextAfterClipboardSequenceChanges();
        await RejectsStaleTextWhenClipboardSequenceDoesNotChange();
        await RestorationMaterializesOnceAcrossRetries();
        await TransientFormatFailureIsRetried();
        LogsSnapshotMetadataWithoutClipboardContents();
    }

    private async Task TransientFormatFailureIsRetried()
    {
        var source = new TransientDataObject();
        var directory = Path.Combine(Path.GetTempPath(), "KeyFlip.Tests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(directory, "keyflip.log");
        try
        {
            var snapshot = await CaptureWithRetriesAsync(
                () => source,
                new DiagnosticLogger(logPath),
                _ => Task.CompletedTask);

            True(snapshot is not null);
            using (snapshot)
            {
                Equal(2, source.GetDataCalls);
                var restored = snapshot!.CreateDataObject();
                Equal("ORIGINAL", (string)restored.GetData(DataFormats.UnicodeText, autoConvert: false)!);
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<ClipboardSnapshot?> CaptureWithRetriesAsync(
        Func<IDataObject?> getDataObject,
        DiagnosticLogger logger,
        Func<CancellationToken, Task> delay)
    {
        return await ClipboardService.CaptureSnapshotWithRetriesAsync(
            getDataObject,
            logger,
            delay,
            CancellationToken.None);
    }

    private async Task RestorationMaterializesOnceAcrossRetries()
    {
        using var sourceStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var source = new DataObject();
        source.SetData("KeyFlip.Test.Stream", autoConvert: false, sourceStream);
        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        using (snapshot)
        {
            var attempts = new List<DataObject>();
            var restored = await RestoreWithRetriesAsync(
                snapshot,
                () => { },
                dataObject =>
                {
                    attempts.Add(dataObject);
                    if (attempts.Count < 5) throw new ExternalException("Clipboard busy.");
                });

            True(restored);
            Equal(5, attempts.Count);
            True(attempts.All(item => ReferenceEquals(attempts[0], item)));
            var restoredStream = (Stream)attempts[0].GetData("KeyFlip.Test.Stream", autoConvert: false)!;
            ThrowsObjectDisposed(() => restoredStream.Position = 0);
        }
    }

    private static async Task<bool> RestoreWithRetriesAsync(
        ClipboardSnapshot snapshot,
        Action clear,
        Action<DataObject> setDataObject)
    {
        return await ClipboardService.RestoreSnapshotAsync(
            snapshot,
            clear,
            setDataObject,
            CancellationToken.None);
    }

    private void LogsSnapshotMetadataWithoutClipboardContents()
    {
        const string ClipboardContents = "SECRET_CLIPBOARD_CONTENTS";
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, autoConvert: false, ClipboardContents);
        data.SetData("KeyFlip.Test.Unsupported", autoConvert: false, new object());
        var capture = ClipboardSnapshot.Capture(data);
        var directory = Path.Combine(Path.GetTempPath(), "KeyFlip.Tests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(directory, "keyflip.log");

        try
        {
            ClipboardService.LogSnapshotCapture(capture, new DiagnosticLogger(logPath));
            var log = File.ReadAllText(logPath);
            Contains("stage=CLIPBOARD_FORMAT_CAPTURED", log);
            Contains($"format={DataFormats.UnicodeText}", log);
            Contains("type=System.String", log);
            Contains("stage=CLIPBOARD_FORMAT_SKIPPED", log);
            Contains("stage=CLIPBOARD_SNAPSHOT_PARTIAL", log);
            DoesNotContain(ClipboardContents, log);
        }
        finally
        {
            capture.Snapshot?.Dispose();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private async Task AcceptsSameTextAfterClipboardSequenceChanges()
    {
        var sequences = new Queue<uint>([41, 42]);
        var result = await ObserveAsync(
            sequenceBefore: 41,
            () => sequences.Dequeue(),
            () => "ghbdtn",
            maxAttempts: 2);

        True(result.SequenceChanged);
        Equal("ghbdtn", result.Text!);
        Equal((uint)41, result.SequenceBefore);
        Equal((uint)42, result.SequenceAfter);
    }

    private async Task RejectsStaleTextWhenClipboardSequenceDoesNotChange()
    {
        var result = await ObserveAsync(
            sequenceBefore: 17,
            () => 17,
            () => "STALE_CLIPBOARD_VALUE",
            maxAttempts: 2);

        False(result.SequenceChanged);
        True(result.Text is null);
    }

    private static async Task<ClipboardCopyResult> ObserveAsync(
        uint sequenceBefore,
        Func<uint> readSequence,
        Func<string?> readText,
        int maxAttempts)
    {
        return await ClipboardService.WaitForClipboardChangeAsync(
            sequenceBefore,
            readSequence,
            readText,
            _ => Task.CompletedTask,
            maxAttempts,
            CancellationToken.None);
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

    private void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private void Contains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected log to contain '{expected}'.");
        }

        Passed++;
    }

    private void DoesNotContain(string unexpected, string actual)
    {
        if (actual.Contains(unexpected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected log not to contain '{unexpected}'.");
        }

        Passed++;
    }

    private void ThrowsObjectDisposed(Action action)
    {
        try
        {
            action();
        }
        catch (ObjectDisposedException)
        {
            Passed++;
            return;
        }

        throw new InvalidOperationException("Expected ObjectDisposedException.");
    }

    private sealed class TransientDataObject : IDataObject
    {
        internal int GetDataCalls { get; private set; }

        public object? GetData(string format, bool autoConvert)
        {
            GetDataCalls++;
            if (GetDataCalls == 1) throw new ExternalException("Clipboard busy.");
            return string.Equals(format, DataFormats.UnicodeText, StringComparison.Ordinal)
                ? "ORIGINAL"
                : null;
        }

        public object? GetData(string format) => GetData(format, autoConvert: true);
        public object? GetData(Type format) => GetData(format.FullName ?? format.Name, autoConvert: true);
        public bool GetDataPresent(string format, bool autoConvert) =>
            string.Equals(format, DataFormats.UnicodeText, StringComparison.Ordinal);
        public bool GetDataPresent(string format) => GetDataPresent(format, autoConvert: true);
        public bool GetDataPresent(Type format) => GetDataPresent(format.FullName ?? format.Name, autoConvert: true);
        public string[] GetFormats(bool autoConvert) => [DataFormats.UnicodeText];
        public string[] GetFormats() => GetFormats(autoConvert: true);
        public void SetData(string format, bool autoConvert, object? data) => throw new NotSupportedException();
        public void SetData(string format, object? data) => throw new NotSupportedException();
        public void SetData(Type format, object? data) => throw new NotSupportedException();
        public void SetData(object? data) => throw new NotSupportedException();
    }
}
