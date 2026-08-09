namespace KeyFlip.Tests;

internal sealed class InputSimulatorTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        SendInputFailureContainsOnlyTechnicalMetadata();
    }

    private void SendInputFailureContainsOnlyTechnicalMetadata()
    {
        var exception = new SendInputException(SendInputOperation.Copy, expected: 4, sent: 0, win32Error: 87, inputSize: 40);
        Equal("SendInput Copy failed. Sent=0/4, Win32Error=87, InputSize=40", exception.Message);
    }

    private void Equal(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }
}
