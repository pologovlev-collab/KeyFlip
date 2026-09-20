using System.Collections.Specialized;
using System.Drawing;
using System.Windows.Forms;

namespace KeyFlip;

internal enum ClipboardSnapshotStatus
{
    Empty,
    Complete,
    Partial,
    Unusable
}

internal sealed record ClipboardFormatCapture(
    string Format,
    string TypeName,
    bool Captured,
    string Reason,
    bool Transient = false);

internal sealed record ClipboardSnapshotCapture(
    ClipboardSnapshotStatus Status,
    ClipboardSnapshot? Snapshot,
    IReadOnlyList<ClipboardFormatCapture> Formats,
    int SourceFormatCount)
{
    internal bool CanUseForTransaction => Status is ClipboardSnapshotStatus.Empty or ClipboardSnapshotStatus.Complete ||
        Status == ClipboardSnapshotStatus.Partial && Snapshot?.HasMeaningfulData == true;
    internal bool HasTransientFailure => Formats.Any(format => !format.Captured && format.Transient);
}

internal sealed class ClipboardRestorePayload(DataObject dataObject, IReadOnlyList<IDisposable> values) : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _values = values;

    internal DataObject DataObject { get; } = dataObject;

    public void Dispose()
    {
        foreach (var value in _values) value.Dispose();
    }
}

internal sealed class ClipboardSnapshot : IDisposable
{
    private static readonly HashSet<string> MeaningfulFormats = new(StringComparer.Ordinal)
    {
        DataFormats.UnicodeText,
        DataFormats.Text,
        DataFormats.OemText,
        DataFormats.Rtf,
        DataFormats.Html,
        DataFormats.CommaSeparatedValue,
        DataFormats.Bitmap,
        DataFormats.Dib,
        DataFormats.EnhancedMetafile,
        DataFormats.FileDrop,
        DataFormats.WaveAudio,
        DataFormats.StringFormat
    };

    private readonly IReadOnlyList<ClipboardFormatValue> _formats;

    private ClipboardSnapshot(
        ClipboardSnapshotStatus status,
        IReadOnlyList<ClipboardFormatValue> formats)
    {
        Status = status;
        _formats = formats;
    }

    internal ClipboardSnapshotStatus Status { get; }
    internal bool IsEmpty => Status == ClipboardSnapshotStatus.Empty;
    internal int FormatCount => _formats.Count;
    internal bool HasMeaningfulData => _formats.Any(item => MeaningfulFormats.Contains(item.Format));

    internal static bool TryCreate(IDataObject? source, out ClipboardSnapshot snapshot)
    {
        var capture = Capture(source);
        if (capture.CanUseForTransaction && capture.Snapshot is not null)
        {
            snapshot = capture.Snapshot;
            return true;
        }

        capture.Snapshot?.Dispose();
        snapshot = null!;
        return false;
    }

    internal static ClipboardSnapshotCapture Capture(IDataObject? source)
    {
        if (source is null)
        {
            var empty = new ClipboardSnapshot(ClipboardSnapshotStatus.Empty, Array.Empty<ClipboardFormatValue>());
            return new ClipboardSnapshotCapture(
                ClipboardSnapshotStatus.Empty,
                empty,
                Array.Empty<ClipboardFormatCapture>(),
                SourceFormatCount: 0);
        }

        var captured = new List<ClipboardFormatValue>();
        var formatCaptures = new List<ClipboardFormatCapture>();
        try
        {
            var formats = source.GetFormats(autoConvert: false)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (formats.Length == 0)
            {
                var empty = new ClipboardSnapshot(ClipboardSnapshotStatus.Empty, captured);
                return new ClipboardSnapshotCapture(
                    ClipboardSnapshotStatus.Empty,
                    empty,
                    formatCaptures,
                    SourceFormatCount: 0);
            }

            foreach (var format in formats)
            {
                try
                {
                    var value = source.GetData(format, autoConvert: false);
                    if (value is null)
                    {
                        formatCaptures.Add(new ClipboardFormatCapture(format, "null", Captured: false, "null_value"));
                        continue;
                    }

                    var typeName = value.GetType().FullName ?? value.GetType().Name;
                    if (!TryCloneValue(value, out var clone))
                    {
                        formatCaptures.Add(new ClipboardFormatCapture(format, typeName, Captured: false, "unsupported_type"));
                        continue;
                    }

                    captured.Add(new ClipboardFormatValue(format, clone));
                    formatCaptures.Add(new ClipboardFormatCapture(format, typeName, Captured: true, "captured"));
                }
                catch (Exception exception)
                {
                    formatCaptures.Add(new ClipboardFormatCapture(
                        format,
                        "unknown",
                        Captured: false,
                        $"get_failed_{exception.GetType().Name}",
                        Transient: exception is System.Runtime.InteropServices.ExternalException));
                }
            }

            if (captured.Count == 0)
            {
                return new ClipboardSnapshotCapture(
                    ClipboardSnapshotStatus.Unusable,
                    Snapshot: null,
                    formatCaptures,
                    formats.Length);
            }

            var status = captured.Count == formats.Length
                ? ClipboardSnapshotStatus.Complete
                : ClipboardSnapshotStatus.Partial;
            var snapshot = new ClipboardSnapshot(status, captured);
            return new ClipboardSnapshotCapture(status, snapshot, formatCaptures, formats.Length);
        }
        catch (Exception exception)
        {
            DisposeValues(captured);
            return new ClipboardSnapshotCapture(
                ClipboardSnapshotStatus.Unusable,
                Snapshot: null,
                [new ClipboardFormatCapture(
                    "<formats>",
                    "unknown",
                    Captured: false,
                    $"enumeration_failed_{exception.GetType().Name}",
                    Transient: exception is System.Runtime.InteropServices.ExternalException)],
                SourceFormatCount: 0);
        }
    }

    internal DataObject CreateDataObject()
    {
        var dataObject = new DataObject();
        foreach (var item in _formats)
        {
            if (!TryCloneValue(item.Value, out var restoredValue))
            {
                throw new InvalidOperationException($"Captured clipboard format '{item.Format}' is no longer cloneable.");
            }

            dataObject.SetData(item.Format, autoConvert: false, restoredValue);
        }

        return dataObject;
    }

    internal ClipboardRestorePayload CreateRestorePayload()
    {
        var dataObject = new DataObject();
        var ownedValues = new List<IDisposable>();
        try
        {
            foreach (var item in _formats)
            {
                if (!TryCloneValue(item.Value, out var restoredValue))
                {
                    throw new InvalidOperationException($"Captured clipboard format '{item.Format}' is no longer cloneable.");
                }

                if (restoredValue is IDisposable disposable) ownedValues.Add(disposable);
                dataObject.SetData(item.Format, autoConvert: false, restoredValue);
            }

            return new ClipboardRestorePayload(dataObject, ownedValues);
        }
        catch
        {
            foreach (var value in ownedValues) value.Dispose();
            throw;
        }
    }

    public void Dispose() => DisposeValues(_formats);

    private static bool TryCloneValue(object value, out object clone)
    {
        switch (value)
        {
            case string text:
                clone = text;
                return true;
            case string[] strings:
                clone = strings.ToArray();
                return true;
            case byte[] bytes:
                clone = bytes.ToArray();
                return true;
            case StringCollection collection:
                var collectionClone = new StringCollection();
                collectionClone.AddRange(collection.Cast<string>().ToArray());
                clone = collectionClone;
                return true;
            case Image image:
                clone = (Image)image.Clone();
                return true;
            case Stream stream:
                clone = CloneStream(stream);
                return true;
            case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal or char:
                clone = value;
                return true;
            default:
                clone = null!;
                return false;
        }
    }

    private static MemoryStream CloneStream(Stream source)
    {
        var originalPosition = source.CanSeek ? source.Position : 0;
        if (source.CanSeek) source.Position = 0;
        try
        {
            var clone = new MemoryStream();
            source.CopyTo(clone);
            clone.Position = 0;
            return clone;
        }
        finally
        {
            if (source.CanSeek) source.Position = originalPosition;
        }
    }

    private static void DisposeValues(IEnumerable<ClipboardFormatValue> values)
    {
        foreach (var value in values)
        {
            if (value.Value is IDisposable disposable) disposable.Dispose();
        }
    }

    private sealed record ClipboardFormatValue(string Format, object Value);
}
