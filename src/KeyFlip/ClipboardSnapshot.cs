using System.Collections.Specialized;
using System.Drawing;
using System.Windows.Forms;

namespace KeyFlip;

internal sealed class ClipboardSnapshot : IDisposable
{
    private readonly IReadOnlyList<ClipboardFormatValue> _formats;

    private ClipboardSnapshot(bool isEmpty, IReadOnlyList<ClipboardFormatValue> formats)
    {
        IsEmpty = isEmpty;
        _formats = formats;
    }

    internal bool IsEmpty { get; }
    internal int FormatCount => _formats.Count;

    internal static bool TryCreate(IDataObject? source, out ClipboardSnapshot snapshot)
    {
        if (source is null)
        {
            snapshot = new ClipboardSnapshot(isEmpty: true, Array.Empty<ClipboardFormatValue>());
            return true;
        }

        var captured = new List<ClipboardFormatValue>();
        try
        {
            var formats = source.GetFormats(autoConvert: false);
            if (formats.Length == 0)
            {
                snapshot = new ClipboardSnapshot(isEmpty: true, captured);
                return true;
            }

            foreach (var format in formats.Distinct(StringComparer.Ordinal))
            {
                var value = source.GetData(format, autoConvert: false);
                if (value is null || !TryCloneValue(value, out var clone))
                {
                    DisposeValues(captured);
                    snapshot = null!;
                    return false;
                }

                captured.Add(new ClipboardFormatValue(format, clone));
            }

            snapshot = new ClipboardSnapshot(isEmpty: false, captured);
            return true;
        }
        catch (Exception)
        {
            DisposeValues(captured);
            snapshot = null!;
            return false;
        }
    }

    internal DataObject CreateDataObject()
    {
        var dataObject = new DataObject();
        foreach (var item in _formats)
        {
            dataObject.SetData(item.Format, autoConvert: false, item.Value);
        }

        return dataObject;
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
                clone = collection.Cast<string>().ToArray();
                return true;
            case Image image:
                clone = new Bitmap(image);
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
