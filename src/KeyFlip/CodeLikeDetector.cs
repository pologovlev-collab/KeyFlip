using System.Text.RegularExpressions;

namespace KeyFlip;

internal static partial class CodeLikeDetector
{
    private static readonly string[] StrongOperators = { "::", "=>", "//", "/*", "*/", "==", "!=", "<=", ">=", "&&", "||", "->" };
    private const string AsciiSyntaxCharacters = ":.,;\"'()[]{}";

    internal static bool LooksLikeCode(string text, bool isCodeProcess = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (StrongOperators.Any(text.Contains)) return true;
        if (isCodeProcess && CompactSyntaxToken().IsMatch(text) && text.Any(AsciiSyntaxCharacters.Contains))
        {
            return true;
        }

        if (WrongLayoutQuotedSpan().IsMatch(text) &&
            (WrongLayoutFunctionCall().IsMatch(text) || text.Contains('=')))
        {
            return true;
        }

        var evidence = 0;
        if (text.Contains('=')) evidence++;
        if (text.TrimEnd().EndsWith(';')) evidence++;
        if (HasPair(text, '{', '}') && text.Contains(':')) evidence += 2;
        else if (HasPair(text, '{', '}')) evidence++;
        if (HasPair(text, '[', ']')) evidence++;
        if (CallExpression().IsMatch(text)) evidence += 2;
        if (Directive().IsMatch(text)) evidence += 2;

        var hasStructuredQuotes = HasPair(text, '"', '"') &&
            (text.Contains('=') || text.Contains(':') || text.Contains('(') || text.Contains('{'));
        if (hasStructuredQuotes) evidence++;
        if (QuotedToken().IsMatch(text)) evidence++;

        return evidence >= 2 || (isCodeProcess && evidence >= 1);
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

    [GeneratedRegex("""^\s*(["'])[A-Za-zА-Яа-яЁё]+\1\s*$""")]
    private static partial Regex QuotedToken();

    [GeneratedRegex("""^\s*[([{]*["']?[A-Za-zА-Яа-яЁё][A-Za-zА-Яа-яЁё0-9_]*["']?[)\]}]*[:.,;]?\s*$""")]
    private static partial Regex CompactSyntaxToken();

    [GeneratedRegex("Э[^Э\r\n]+Э")]
    private static partial Regex WrongLayoutQuotedSpan();

    [GeneratedRegex(@"\b[А-Яа-яЁё]+\s*\(\s*Э[^Э\r\n]+Э\s*\)")]
    private static partial Regex WrongLayoutFunctionCall();

}
