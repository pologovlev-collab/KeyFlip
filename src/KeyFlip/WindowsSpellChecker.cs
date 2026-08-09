using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace KeyFlip;

internal sealed class WindowsSpellChecker : IWordLanguageScorer
{
    private readonly IReadOnlyDictionary<WordLanguage, ISpellChecker> _checkers;

    private WindowsSpellChecker(IReadOnlyDictionary<WordLanguage, ISpellChecker> checkers) => _checkers = checkers;

    internal static WindowsSpellChecker? TryCreate()
    {
        ISpellCheckerFactory? factory = null;
        try
        {
            var factoryType = Type.GetTypeFromCLSID(new Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC"), throwOnError: true)!;
            factory = (ISpellCheckerFactory)Activator.CreateInstance(factoryType)!;
            var checkers = new Dictionary<WordLanguage, ISpellChecker>();
            AddIfSupported(factory, checkers, WordLanguage.English, "en-US");
            AddIfSupported(factory, checkers, WordLanguage.Russian, "ru-RU");
            return checkers.Count == 0 ? null : new WindowsSpellChecker(checkers);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            ReleaseComObject(factory);
        }
    }

    public bool? IsValid(string word, WordLanguage language)
    {
        if (!_checkers.TryGetValue(language, out var checker)) return null;

        IEnumSpellingError? errors = null;
        ISpellingError? error = null;
        try
        {
            errors = checker.Check(word);
            error = errors.Next();
            return error is null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            ReleaseComObject(error);
            ReleaseComObject(errors);
        }
    }

    private static void AddIfSupported(
        ISpellCheckerFactory factory,
        IDictionary<WordLanguage, ISpellChecker> checkers,
        WordLanguage language,
        string languageTag)
    {
        if (factory.IsSupported(languageTag)) checkers[language] = factory.CreateSpellChecker(languageTag);
    }

    private static void ReleaseComObject(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance)) Marshal.ReleaseComObject(instance);
    }

    [ComImport]
    [Guid("8E018A9D-2415-4677-BF08-794EA61F94BB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellCheckerFactory
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumString GetSupportedLanguages();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsSupported([MarshalAs(UnmanagedType.LPWStr)] string languageTag);

        [return: MarshalAs(UnmanagedType.Interface)]
        ISpellChecker CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string languageTag);
    }

    [ComImport]
    [Guid("B6FD0B71-E2BC-4653-8D05-F197E412770B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellChecker
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetLanguageTag();

        [return: MarshalAs(UnmanagedType.Interface)]
        IEnumSpellingError Check([MarshalAs(UnmanagedType.LPWStr)] string text);
    }

    [ComImport]
    [Guid("803E3BD4-2828-4410-8290-418D1D73C762")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumSpellingError
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        ISpellingError? Next();
    }

    [ComImport]
    [Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellingError
    {
    }
}
