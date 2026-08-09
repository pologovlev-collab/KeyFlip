namespace KeyFlip.Tests;

internal sealed class ConversionGuardTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        RejectsNullOrEmptySource();
        RejectsWhitespaceOnlySource();
        RejectsUnchangedConversion();
        RejectsSourceWithoutConvertibleCharacters();
        AllowsChangedConvertibleText();
    }

    private void RejectsNullOrEmptySource()
    {
        False(ConversionGuard.CanPaste(null, "привет"));
        False(ConversionGuard.CanPaste(string.Empty, "привет"));
    }

    private void RejectsWhitespaceOnlySource() => False(ConversionGuard.CanPaste(" \t\r\n", "привет"));

    private void RejectsUnchangedConversion() => False(ConversionGuard.CanPaste("hello", "hello"));

    private void RejectsSourceWithoutConvertibleCharacters() => False(ConversionGuard.CanPaste("🙂 123", "different"));

    private void AllowsChangedConvertibleText()
    {
        True(ConversionGuard.CanPaste("ghbdtn", "привет"));
        True(ConversionGuard.CanPaste("@#$^&", "\"№;:?"));
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
}
