using System.Runtime.InteropServices;

namespace KeyboardLayoutFixer;

internal static class SystemSpellChecker
{
    private static readonly Lazy<ISpellChecker?> English = new(() => Create("en-US", "en"));
    private static readonly Lazy<ISpellChecker?> Russian = new(() => Create("ru-RU", "ru"));

    public static bool IsEnglishWord(string word) => IsCorrect(English.Value, word);

    public static bool IsRussianWord(string word) => IsCorrect(Russian.Value, word);

    public static bool HasEnglishChecker => English.Value is not null;

    public static bool HasRussianChecker => Russian.Value is not null;

    private static bool IsCorrect(ISpellChecker? checker, string word)
    {
        if (checker is null)
        {
            return false;
        }

        try
        {
            checker.Check(word, out var errors);
            errors.Next(out var error);
            return error is null;
        }
        catch
        {
            return false;
        }
    }

    private static ISpellChecker? Create(params string[] languageTags)
    {
        try
        {
            var factory = (ISpellCheckerFactory)new SpellCheckerFactory();
            foreach (var languageTag in languageTags)
            {
                factory.IsSupported(languageTag, out var supported);
                if (!supported)
                {
                    continue;
                }

                factory.CreateSpellChecker(languageTag, out var checker);
                if (checker is not null)
                {
                    return checker;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    [ComImport]
    [Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
    private sealed class SpellCheckerFactory;

    [ComImport]
    [Guid("8E018A9D-2415-4677-BF08-794EA61F94BB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellCheckerFactory
    {
        void get_SupportedLanguages([MarshalAs(UnmanagedType.Interface)] out object languages);

        void IsSupported(
            [MarshalAs(UnmanagedType.LPWStr)] string languageTag,
            [MarshalAs(UnmanagedType.Bool)] out bool value);

        void CreateSpellChecker(
            [MarshalAs(UnmanagedType.LPWStr)] string languageTag,
            [MarshalAs(UnmanagedType.Interface)] out ISpellChecker spellChecker);
    }

    [ComImport]
    [Guid("B6FD0B71-98C5-403A-BF8B-80EE0C7DFCAA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellChecker
    {
        void get_LanguageTag([MarshalAs(UnmanagedType.LPWStr)] out string value);

        void Check(
            [MarshalAs(UnmanagedType.LPWStr)] string text,
            [MarshalAs(UnmanagedType.Interface)] out IEnumSpellingError value);
    }

    [ComImport]
    [Guid("803E3BD4-2828-4410-8290-418D1D73C762")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumSpellingError
    {
        void Next([MarshalAs(UnmanagedType.Interface)] out ISpellingError value);
    }

    [ComImport]
    [Guid("B7C82D61-FBE8-4B47-9B27-6C0D2E0DE0A3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISpellingError
    {
        void get_StartIndex(out uint value);

        void get_Length(out uint value);

        void get_CorrectiveAction(out int value);

        void get_Replacement([MarshalAs(UnmanagedType.LPWStr)] out string value);
    }
}
