using System.Collections.Specialized;
using System.Drawing;
using System.Windows.Forms;

namespace KeyFlip.Tests;

internal sealed class ClipboardSnapshotTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        CapturesEmptyClipboardState();
        DeepCopiesMaterializedFormats();
        CapturesBitmapAndFileDropFormats();
        DeepCopiesStringCollectionWithoutChangingItsType();
        KeepsRestoredDisposableValuesAliveAfterSnapshotDisposal();
        PreservesSupportedContentWhenAnOptionalFormatIsUnsupported();
        RejectsPartialSnapshotWithoutMeaningfulRestorableData();
        CapturesCompleteCustomFormatWithoutLoss();
        SkipsThrowingOptionalFormatWhenTextWasCaptured();
        RejectsUnsupportedFormatsRatherThanLosingThem();
    }

    private void DeepCopiesStringCollectionWithoutChangingItsType()
    {
        var collection = new StringCollection { "one", "two" };
        var source = new DataObject();
        source.SetData("KeyFlip.Test.StringCollection", autoConvert: false, collection);

        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        using (snapshot)
        {
            collection[0] = "changed";
            var restored = snapshot.CreateDataObject();
            var restoredCollection = restored.GetData(
                "KeyFlip.Test.StringCollection",
                autoConvert: false) as StringCollection;
            True(restoredCollection is not null);
            Equal("one", restoredCollection![0]!);
        }
    }

    private void CapturesBitmapAndFileDropFormats()
    {
        using var image = new Bitmap(2, 2);
        image.SetPixel(0, 0, Color.Red);
        var files = new StringCollection { @"C:\source\one.txt", @"C:\source\two.txt" };
        var source = new DataObject();
        source.SetImage(image);
        source.SetFileDropList(files);

        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        using (snapshot)
        {
            image.SetPixel(0, 0, Color.Blue);
            files[0] = @"C:\changed.txt";

            var restored = snapshot.CreateDataObject();
            using var restoredImage = (Image)restored.GetData(DataFormats.Bitmap, autoConvert: false)!;
            Equal(Color.Red.ToArgb(), ((Bitmap)restoredImage).GetPixel(0, 0).ToArgb());
            Equal(@"C:\source\one.txt", ((string[])restored.GetData(DataFormats.FileDrop, autoConvert: false)!)[0]);
        }
    }

    private void KeepsRestoredDisposableValuesAliveAfterSnapshotDisposal()
    {
        using var stream = new MemoryStream(new byte[] { 7, 8, 9 });
        using var image = new Bitmap(1, 1);
        image.SetPixel(0, 0, Color.Green);
        var source = new DataObject();
        source.SetData("KeyFlip.Test.Stream", autoConvert: false, stream);
        source.SetData(DataFormats.Bitmap, autoConvert: false, image);

        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        var restored = snapshot.CreateDataObject();
        snapshot.Dispose();

        using var restoredStream = (Stream)restored.GetData("KeyFlip.Test.Stream", autoConvert: false)!;
        restoredStream.Position = 0;
        Equal(7, restoredStream.ReadByte());
        using var restoredImage = (Image)restored.GetData(DataFormats.Bitmap, autoConvert: false)!;
        Equal(Color.Green.ToArgb(), ((Bitmap)restoredImage).GetPixel(0, 0).ToArgb());
    }

    private void PreservesSupportedContentWhenAnOptionalFormatIsUnsupported()
    {
        var source = new DataObject();
        source.SetData(DataFormats.UnicodeText, autoConvert: false, "ORIGINAL");
        source.SetData("KeyFlip.Test.Unsupported", autoConvert: false, new object());

        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        using (snapshot)
        {
            var restored = snapshot.CreateDataObject();
            Equal("ORIGINAL", (string)restored.GetData(DataFormats.UnicodeText, autoConvert: false)!);
            False(restored.GetDataPresent("KeyFlip.Test.Unsupported", autoConvert: false));
        }
    }

    private void RejectsPartialSnapshotWithoutMeaningfulRestorableData()
    {
        var source = new DataObject();
        source.SetData("KeyFlip.Test.Bytes", autoConvert: false, new byte[] { 1, 2, 3 });
        source.SetData("KeyFlip.Test.Unsupported", autoConvert: false, new object());

        False(ClipboardSnapshot.TryCreate(source, out _));

        var capture = ClipboardSnapshot.Capture(source);
        Equal(ClipboardSnapshotStatus.Partial, capture.Status);
        False(capture.CanUseForTransaction);
        True(capture.Snapshot is not null);
        using (capture.Snapshot)
        {
            var restored = capture.Snapshot!.CreateDataObject();
            Equal((byte)1, ((byte[])restored.GetData("KeyFlip.Test.Bytes", autoConvert: false)!)[0]);
        }
    }

    private void CapturesCompleteCustomFormatWithoutLoss()
    {
        var source = new DataObject();
        source.SetData("KeyFlip.Test.Bytes", autoConvert: false, new byte[] { 1, 2, 3 });

        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        using (snapshot)
        {
            var restored = snapshot.CreateDataObject();
            Equal((byte)1, ((byte[])restored.GetData("KeyFlip.Test.Bytes", autoConvert: false)!)[0]);
        }
    }

    private void SkipsThrowingOptionalFormatWhenTextWasCaptured()
    {
        var source = new ThrowingOptionalFormatDataObject();

        True(ClipboardSnapshot.TryCreate(source, out var snapshot));
        using (snapshot)
        {
            var restored = snapshot.CreateDataObject();
            Equal("ORIGINAL", (string)restored.GetData(DataFormats.UnicodeText, autoConvert: false)!);
            False(restored.GetDataPresent(ThrowingOptionalFormatDataObject.ThrowingFormat, autoConvert: false));
        }
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

        var capture = ClipboardSnapshot.Capture(source);
        Equal(ClipboardSnapshotStatus.Unusable, capture.Status);
        False(capture.CanUseForTransaction);
        True(capture.Snapshot is null);
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

    private sealed class ThrowingOptionalFormatDataObject : IDataObject
    {
        internal const string ThrowingFormat = "KeyFlip.Test.Throwing";

        public object? GetData(string format, bool autoConvert)
        {
            if (string.Equals(format, DataFormats.UnicodeText, StringComparison.Ordinal)) return "ORIGINAL";
            if (string.Equals(format, ThrowingFormat, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Delayed format failed.");
            }

            return null;
        }

        public object? GetData(string format) => GetData(format, autoConvert: true);
        public object? GetData(Type format) => GetData(format.FullName ?? format.Name, autoConvert: true);
        public bool GetDataPresent(string format, bool autoConvert) =>
            string.Equals(format, DataFormats.UnicodeText, StringComparison.Ordinal) ||
            string.Equals(format, ThrowingFormat, StringComparison.Ordinal);
        public bool GetDataPresent(string format) => GetDataPresent(format, autoConvert: true);
        public bool GetDataPresent(Type format) => GetDataPresent(format.FullName ?? format.Name, autoConvert: true);
        public string[] GetFormats(bool autoConvert) => [DataFormats.UnicodeText, ThrowingFormat];
        public string[] GetFormats() => GetFormats(autoConvert: true);
        public void SetData(string format, bool autoConvert, object? data) => throw new NotSupportedException();
        public void SetData(string format, object? data) => throw new NotSupportedException();
        public void SetData(Type format, object? data) => throw new NotSupportedException();
        public void SetData(object? data) => throw new NotSupportedException();
    }
}
