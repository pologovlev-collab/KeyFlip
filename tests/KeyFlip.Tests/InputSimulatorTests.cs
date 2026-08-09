namespace KeyFlip.Tests;

internal sealed class InputSimulatorTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        SendInputFailureContainsOnlyTechnicalMetadata();
        ModifierReleaseRequiresThreeConsecutiveUpPolls();
        ModifierReleaseResetsAfterPressedPoll();
        PartialKeySendStillReleasesSyntheticControl();
    }

    private void SendInputFailureContainsOnlyTechnicalMetadata()
    {
        var exception = new SendInputException(SendInputOperation.Copy, expected: 4, sent: 0, win32Error: 87, inputSize: 40);
        Equal("SendInput Copy failed. Sent=0/4, Win32Error=87, InputSize=40", exception.Message);
    }

    private void ModifierReleaseRequiresThreeConsecutiveUpPolls()
    {
        var tracker = new ModifierReleaseTracker(requiredUpPolls: 3);
        False(tracker.Observe(anyModifierPressed: false));
        False(tracker.Observe(anyModifierPressed: false));
        True(tracker.Observe(anyModifierPressed: false));
    }

    private void ModifierReleaseResetsAfterPressedPoll()
    {
        var tracker = new ModifierReleaseTracker(requiredUpPolls: 3);
        False(tracker.Observe(anyModifierPressed: false));
        False(tracker.Observe(anyModifierPressed: true));
        False(tracker.Observe(anyModifierPressed: false));
        False(tracker.Observe(anyModifierPressed: false));
        True(tracker.Observe(anyModifierPressed: false));
    }

    private void PartialKeySendStillReleasesSyntheticControl()
    {
        var batches = new List<NativeMethods.Input[]>();
        var sendCall = 0;
        var simulator = new InputSimulator(
            inputs =>
            {
                batches.Add(inputs.ToArray());
                sendCall++;
                return sendCall == 2 ? 1u : (uint)inputs.Length;
            },
            _ => 0);

        try
        {
            simulator.SendCtrlKey(System.Windows.Forms.Keys.C, SendInputOperation.Copy);
            throw new InvalidOperationException("Expected SendInputException.");
        }
        catch (SendInputException)
        {
            Equal(3, batches.Count);
            Equal(NativeMethods.VirtualKeyControl, batches[^1][0].Union.Keyboard.VirtualKey);
            True((batches[^1][0].Union.Keyboard.Flags & NativeMethods.KeyEventKeyUp) != 0);
        }
    }

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

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
