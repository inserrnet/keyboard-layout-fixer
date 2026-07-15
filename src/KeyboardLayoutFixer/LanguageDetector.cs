namespace KeyboardLayoutFixer;

internal enum LayoutLanguage
{
    Other,
    English,
    Russian
}

internal sealed record CorrectionDecision(bool ShouldSwitch, bool ShouldReplace, LayoutLanguage TargetLanguage, string Replacement);

internal sealed class LanguageDetector
{
    public CorrectionDecision Analyze(string word, LayoutLanguage currentLanguage, bool autoCorrect, int minimumLength)
    {
        _ = minimumLength;

        if (!autoCorrect || word.Length == 0 || word.Any(char.IsDigit) || word.Contains('@') || word.Contains('/'))
        {
            return new CorrectionDecision(false, false, LayoutLanguage.Other, word);
        }

        return currentLanguage switch
        {
            LayoutLanguage.English => AnalyzeEnglishInput(word, autoCorrect),
            LayoutLanguage.Russian => AnalyzeRussianInput(word, autoCorrect),
            _ => new CorrectionDecision(false, false, LayoutLanguage.Other, word)
        };
    }

    private static CorrectionDecision AnalyzeEnglishInput(string word, bool autoCorrect)
    {
        if (!word.All(IsEnglishKeyboardChar))
        {
            return new CorrectionDecision(false, false, LayoutLanguage.Other, word);
        }

        var converted = LayoutMaps.ConvertToRussian(word);
        if (LooksRussian(converted) && IsRussianWord(converted) && !IsEnglishWord(word))
        {
            return new CorrectionDecision(true, autoCorrect, LayoutLanguage.Russian, converted);
        }

        return new CorrectionDecision(false, false, LayoutLanguage.Other, word);
    }

    private static CorrectionDecision AnalyzeRussianInput(string word, bool autoCorrect)
    {
        if (!word.All(IsRussianKeyboardChar))
        {
            return new CorrectionDecision(false, false, LayoutLanguage.Other, word);
        }

        var converted = LayoutMaps.ConvertToEnglish(word);
        if (LooksEnglish(converted) && IsEnglishWord(converted) && !IsRussianWord(word))
        {
            return new CorrectionDecision(true, autoCorrect, LayoutLanguage.English, converted);
        }

        return new CorrectionDecision(false, false, LayoutLanguage.Other, word);
    }

    private static bool LooksEnglish(string word) => word.All(ch => char.IsLetter(ch) && ch <= 127);

    private static bool LooksRussian(string word) => word.All(ch => ch is >= 'а' and <= 'я' or 'ё' or >= 'А' and <= 'Я' or 'Ё');

    private static bool IsEnglishWord(string word) =>
        WordDictionaries.English.Contains(word) ||
        (SystemSpellChecker.HasEnglishChecker && SystemSpellChecker.IsEnglishWord(word));

    private static bool IsRussianWord(string word) =>
        WordDictionaries.Russian.Contains(word) ||
        (SystemSpellChecker.HasRussianChecker && SystemSpellChecker.IsRussianWord(word));

    private static bool IsEnglishKeyboardChar(char ch) => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
        or '[' or ']' or ';' or '\'' or ',' or '.' or '`';

    private static bool IsRussianKeyboardChar(char ch) => ch is >= 'а' and <= 'я' or 'ё' or >= 'А' and <= 'Я' or 'Ё'
        or '[' or ']' or ';' or '\'' or ',' or '.' or '`';
}
