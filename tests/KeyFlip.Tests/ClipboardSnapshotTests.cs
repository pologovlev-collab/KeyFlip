using System.Windows.Forms;

namespace KeyFlip.Tests;

internal sealed class ClipboardSnapshotTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        CapturesEmptyClipboardState();
        DeepCopiesMaterializedFormats();
        RejectsUnsupportedFormatsRatherThanLosingThem();
    }

    private void CapturesEmptyClipboardState()
    {
        True(ClipboardSnapshot.TryCreate(null, out var snapshot));
        using (snapshot)
        {
            True(snapshot.IsEmpty);
            Equal(0, snapshot.FormatCount);
        }
    }

    private void DeepCopiesMaterializedFormats()
    {
        var files = new[] { @"C:\source\file.txt" };
        var bytes = new byte[] { 1, 2, 3 };
        using var stream = new MemoryStream(new byte[] { 4, 5, 6 });
        var source = new DataObject();
        source.SetData(DataFormats.UnicodeText, autoConvert: false, "ORIGINAL");
        source.SetData(DataFormats.FileDrop, autoConvert: false, files);
        source.SetData("KeyFlip.Test.Bytes", autoConvert: false, bytes);
        source.SetData("KeyFlip.Test.Stream", autoConvert: false, stream);

        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        using (snapshot)
        {
            files[0] = @"C:\changed.txt";
            bytes[0] = 9;
            stream.Position = 0;
            stream.WriteByte(9);

            var restored = snapshot.CreateDataObject();
            Equal("ORIGINAL", (string)restored.GetData(DataFormats.UnicodeText, autoConvert: false)!);
            Equal(@"C:\source\file.txt", ((string[])restored.GetData(DataFormats.FileDrop, autoConvert: false)!)[0]);
            Equal((byte)1, ((byte[])restored.GetData("KeyFlip.Test.Bytes", autoConvert: false)!)[0]);
            using var restoredStream = (MemoryStream)restored.GetData("KeyFlip.Test.Stream", autoConvert: false)!;
            Equal((byte)4, restoredStream.ToArray()[0]);
        }
    }

    private void RejectsUnsupportedFormatsRatherThanLosingThem()
    {
        var source = new DataObject();
        source.SetData("KeyFlip.Test.Unsupported", autoConvert: false, new object());
        False(ClipboardSnapshot.TryCreate(source, out _));
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
}
