using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class WordConversionTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        SingleWrongLayoutWordProducesOneEdit();
        AttachedEnglishPunctuationFollowsConvertedWord();
        AttachedRussianPunctuationFollowsConvertedWord();
        SymbolOnlySelectionProducesTargetedEdits();
        FormattedSelectionChangesOnlyWrongToken();
        ParagraphAndTableSeparatorsAreOutsideEdits();
    }

    private void AttachedEnglishPunctuationFollowsConvertedWord()
    {
        var result = WordEditPlanner.Create("ghbdtn@");

        Equal("привет\"", result.OutputText);
        True(result.Edits.Count > 0);
    }

    private void AttachedRussianPunctuationFollowsConvertedWord()
    {
        var result = WordEditPlanner.Create("руддщ№");

        Equal("hello#", result.OutputText);
        True(result.Edits.Count > 0);
    }

    private void SymbolOnlySelectionProducesTargetedEdits()
    {
        var result = WordEditPlanner.Create("@#$^&");

        Equal("\"№;:?", result.OutputText);
        True(result.Edits.Count > 0);
    }

    private void SingleWrongLayoutWordProducesOneEdit()
    {
        var result = WordEditPlanner.Create("руддщ");

        Equal("hello", result.OutputText);
        Edit(new ConversionEdit(0, 5, "hello"), result.Edits.Single());
    }

    private void FormattedSelectionChangesOnlyWrongToken()
    {
        var result = WordEditPlanner.Create("обычный текст\rЖИРНЫЙ ghbdtn\rкурсив");

        Equal("обычный текст\rЖИРНЫЙ привет\rкурсив", result.OutputText);
        Edit(new ConversionEdit(21, 6, "привет"), result.Edits.Single());
    }

    private void ParagraphAndTableSeparatorsAreOutsideEdits()
    {
        const string text = "абзац1\rghbdtn\r\nруддщ\aабзац2\r";
        var result = WordEditPlanner.Create(text);

        Equal(2, result.Edits.Count);
        Edit(new ConversionEdit(7, 6, "привет"), result.Edits[0]);
        Edit(new ConversionEdit(15, 5, "hello"), result.Edits[1]);
        foreach (var edit in result.Edits)
        {
            var source = text.Substring(edit.Start, edit.Length);
            False(source.Any(character => character is '\r' or '\n' or '\a'));
        }
    }

    private void Edit(ConversionEdit expected, ConversionEdit actual)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"Expected edit '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private void Equal(int expected, int actual)
    {
        if (expected != actual) throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        Passed++;
    }

    private void False(bool value)
    {
        if (value) throw new InvalidOperationException("Expected false, actual true.");
        Passed++;
    }

    private void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Expected true, actual false.");
        Passed++;
    }
}
