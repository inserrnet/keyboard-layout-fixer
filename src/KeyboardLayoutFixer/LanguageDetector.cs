using System.Collections.Frozen;

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
    private static readonly FrozenSet<string> CommonEnglish = new[]
    {
        "hello", "test", "yes", "no", "thanks", "thank", "you", "the", "and", "for", "with", "from", "this", "that", "what",
        "where", "when", "please", "ok", "okay", "good", "bad", "file", "text", "code", "login", "password", "email", "work",
        "browser", "program", "layout", "keyboard", "windows", "github", "openai", "chat", "message", "example", "word",
        "english", "russian", "language", "input", "space", "auto", "correct", "fix", "fixer", "download", "install"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> CommonRussian = new[]
    {
        "привет", "тест", "да", "нет", "спасибо", "пожалуйста", "что", "где", "когда", "это", "как", "для", "или", "если",
        "текст", "код", "файл", "почта", "пароль", "работа", "можно", "нужно", "хорошо", "плохо",
        "теперь", "русские", "слова", "предыдущее", "предложение", "пример", "пишу", "печатаю", "исправляет",
        "раскладка", "раскладку", "автоисправление", "отключено", "включено", "сейчас"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public CorrectionDecision Analyze(string word, LayoutLanguage currentLanguage, bool autoCorrect, int minimumLength)
    {
        if (!autoCorrect || word.Length < minimumLength || word.Any(char.IsDigit) || word.Contains('@') || word.Contains('/'))
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
        if (LooksRussian(converted) && ScoreRussianCandidate(converted, word) >= ScoreEnglish(word) + 1)
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
        if (LooksEnglish(converted) && ScoreEnglishCandidate(converted, word) >= ScoreRussian(word) + 1)
        {
            return new CorrectionDecision(true, autoCorrect, LayoutLanguage.English, converted);
        }

        return new CorrectionDecision(false, false, LayoutLanguage.Other, word);
    }

    private static int ScoreEnglish(string word)
    {
        var lower = word.ToLowerInvariant();
        var score = 0;
        if (CommonEnglish.Contains(lower)) score += 6;
        if (lower.Any(ch => "aeiouy".Contains(ch))) score += 2;
        if (lower.Count(ch => "aeiouy".Contains(ch)) >= 2) score += 1;
        if (ContainsAny(lower, "th", "he", "er", "re", "on", "in", "an", "or", "ow", "br", "ro", "se", "la", "ou", "en")) score += 2;
        if (ContainsAny(lower, "ing", "ion", "ent", "ter", "ver", "ser", "lay", "out", "gram", "word")) score += 2;
        if (lower.Any(ch => "qw[];'`".Contains(ch))) score -= 2;
        if (lower is "ghbdtn" or "ntcn" or "lf" or "ytn") score -= 5;
        if (lower.Length >= 4 && lower.Distinct().Count() >= 3) score += 1;
        return score;
    }

    private static int ScoreEnglishCandidate(string converted, string original)
    {
        var score = ScoreEnglish(converted);
        if (original.Any(ch => ch is >= 'а' and <= 'я' or 'ё' or >= 'А' and <= 'Я' or 'Ё'))
        {
            score += 2;
        }

        if (original.Any(ch => "ъхжэ".Contains(char.ToLowerInvariant(ch))))
        {
            score += 1;
        }

        return score;
    }

    private static int ScoreRussianCandidate(string converted, string original)
    {
        var score = ScoreRussian(converted);
        if (original.Any(ch => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z'))
        {
            score += 2;
        }

        if (original.Any(ch => "qwertyuiop[]asdfghjkl;'zxcvbnm,.`".Contains(char.ToLowerInvariant(ch))))
        {
            score += 1;
        }

        return score;
    }

    private static int ScoreRussian(string word)
    {
        var lower = word.ToLowerInvariant();
        var score = 0;
        if (CommonRussian.Contains(lower)) score += 6;
        if (lower.Any(ch => "аеёиоуыэюя".Contains(ch))) score += 2;
        if (lower.Any(ch => "ъхжэ".Contains(ch))) score -= 1;
        if (lower is "руддщ" or "еуые" or "нуы" or "тщ") score -= 5;
        if (lower.Length >= 4 && lower.Distinct().Count() >= 3) score += 1;
        return score;
    }

    private static bool LooksEnglish(string word) => word.All(ch => char.IsLetter(ch) && ch <= 127);

    private static bool LooksRussian(string word) => word.All(ch => ch is >= 'а' and <= 'я' or 'ё' or >= 'А' and <= 'Я' or 'Ё');

    private static bool ContainsAny(string text, params string[] fragments) => fragments.Any(text.Contains);

    private static bool IsEnglishKeyboardChar(char ch) => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z'
        or '[' or ']' or ';' or '\'' or ',' or '.' or '`';

    private static bool IsRussianKeyboardChar(char ch) => ch is >= 'а' and <= 'я' or 'ё' or >= 'А' and <= 'Я' or 'Ё'
        or '[' or ']' or ';' or '\'' or ',' or '.' or '`';
}
