namespace KeyFlip;

internal static class ConversionGuard
{
    private const string ConvertibleSymbols = "`~!@#$%^&*()_+-=[]{}\\|;:'\",.<>/?№";

    internal static bool CanPaste(string? source, string? converted)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(converted)) return false;
        if (string.Equals(source, converted, StringComparison.Ordinal)) return false;
        return source.Any(IsPotentiallyConvertible);
    }

    private static bool IsPotentiallyConvertible(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or
            >= 'А' and <= 'Я' or >= 'а' and <= 'я' or 'Ё' or 'ё' ||
        ConvertibleSymbols.Contains(character, StringComparison.Ordinal);
}
