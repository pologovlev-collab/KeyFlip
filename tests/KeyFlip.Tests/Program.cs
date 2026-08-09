using KeyFlip;
using KeyFlip.Tests;

try
{
    var tests = new LayoutConverterTests();
    tests.Run();
    var nativeInteropTests = new NativeInteropTests();
    nativeInteropTests.Run();
    var inputSimulatorTests = new InputSimulatorTests();
    inputSimulatorTests.Run();
    var conversionGuardTests = new ConversionGuardTests();
    conversionGuardTests.Run();
    var clipboardSnapshotTests = new ClipboardSnapshotTests();
    clipboardSnapshotTests.Run();
    var settingsServiceTests = new SettingsServiceTests();
    settingsServiceTests.Run();
    var focusedContextClassifierTests = new FocusedContextClassifierTests();
    focusedContextClassifierTests.Run();
    var fileNameConverterTests = new FileNameConverterTests();
    fileNameConverterTests.Run();
    var codeSafeConversionTests = new CodeSafeConversionTests();
    codeSafeConversionTests.Run();
    var wordConversionTests = new WordConversionTests();
    wordConversionTests.Run();
    var hotkeyManagerTests = new HotkeyManagerTests();
    hotkeyManagerTests.Run();
    var buildInfoTests = new BuildInfoTests();
    buildInfoTests.Run();
    var diagnosticLoggerTests = new DiagnosticLoggerTests();
    diagnosticLoggerTests.Run();
    Console.WriteLine($"Passed: {tests.Passed + nativeInteropTests.Passed + inputSimulatorTests.Passed + conversionGuardTests.Passed + clipboardSnapshotTests.Passed + settingsServiceTests.Passed + focusedContextClassifierTests.Passed + fileNameConverterTests.Passed + codeSafeConversionTests.Passed + wordConversionTests.Passed + hotkeyManagerTests.Passed + buildInfoTests.Passed + diagnosticLoggerTests.Passed}");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    return 1;
}
