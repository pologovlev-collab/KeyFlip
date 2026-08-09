using KeyFlip;
using KeyFlip.Tests;

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
Console.WriteLine($"Passed: {tests.Passed + nativeInteropTests.Passed + inputSimulatorTests.Passed + conversionGuardTests.Passed + clipboardSnapshotTests.Passed + settingsServiceTests.Passed + focusedContextClassifierTests.Passed}");
