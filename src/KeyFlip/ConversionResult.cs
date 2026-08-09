using System.Text;

namespace KeyFlip;

internal readonly record struct ConversionEdit(int Start, int Length, string Replacement);

internal sealed record ConversionResult(string OutputText, IReadOnlyList<ConversionEdit> Edits)
{
    internal bool Changed => Edits.Count > 0;

    internal static ConversionResult Unchanged(string text) => new(text, Array.Empty<ConversionEdit>());

    internal static ConversionResult FromEdits(string text, IReadOnlyList<ConversionEdit> edits)
    {
        if (edits.Count == 0) return Unchanged(text);

        var output = new StringBuilder(text.Length);
        var position = 0;
        foreach (var edit in edits)
        {
            output.Append(text.AsSpan(position, edit.Start - position));
            output.Append(edit.Replacement);
            position = edit.Start + edit.Length;
        }

        output.Append(text.AsSpan(position));
        return new ConversionResult(output.ToString(), edits);
    }
}
