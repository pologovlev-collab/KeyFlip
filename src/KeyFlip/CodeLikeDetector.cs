using System.Text.RegularExpressions;

namespace KeyFlip;

internal static partial class CodeLikeDetector
{
    private static readonly string[] StrongOperators = { "::", "=>", "//", "/*", "*/", "==", "!=", "<=", ">=", "&&", "||", "->" };

    internal static bool LooksLikeCode(string text, bool isCodeProcess = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (StrongOperators.Any(text.Contains)) return true;

        var evidence = 0;
        if (text.Contains('=') && !text.Contains(" = ", StringComparison.OrdinalIgnoreCase)) evidence++;
        else if (text.Contains('=')) evidence++;
        if (text.Contains(';')) evidence++;
        if (HasPair(text, '{', '}') && text.Contains(':')) evidence += 2;
        else if (HasPair(text, '{', '}')) evidence++;
        if (HasPair(text, '[', ']')) evidence++;
        if (CallExpression().IsMatch(text)) evidence += 2;
        if (Directive().IsMatch(text)) evidence += 2;

        var hasStructuredQuotes = HasPair(text, '"', '"') &&
            (text.Contains('=') || text.Contains(':') || text.Contains('(') || text.Contains('{'));
        if (hasStructuredQuotes) evidence++;

        return evidence >= 2 || (isCodeProcess && evidence >= 2);
    }

    private static bool HasPair(string text, char open, char close)
    {
        var first = text.IndexOf(open);
        return first >= 0 && text.IndexOf(close, first + 1) > first;
    }

    [GeneratedRegex(@"\b[A-Za-z_][A-Za-z0-9_]*\s*\([^\r\n]*\)")]
    private static partial Regex CallExpression();

    [GeneratedRegex(@"(?m)^\s*#\s*[A-Za-z_]+")]
    private static partial Regex Directive();
}
