namespace KeyboardLayoutFixer;

internal static class WordDictionaries
{
    private static readonly object SyncRoot = new();

    private static readonly string[] BuiltInEnglish =
    [
        "auto", "bad", "browser", "chat", "code", "correct", "download", "email", "english", "example",
        "file", "fix", "fixer", "for", "from", "github", "good", "hello", "input", "install",
        "keyboard", "language", "layout", "login", "message", "no", "okay", "openai", "password", "please",
        "program", "russian", "space", "test", "text", "thanks", "thank", "that", "the", "this",
        "what", "when", "where", "windows", "with", "word", "work", "yes", "you"
    ];

    private static readonly string[] BuiltInRussian =
    [
        "автоисправление", "включено", "где", "да", "для", "если", "или", "исправляет", "как", "когда",
        "код", "можно", "нет", "нужно", "отключено", "пароль", "печатаю", "пишу", "плохо", "пожалуйста",
        "почта", "предложение", "предыдущее", "пример", "привет", "работа", "раскладка", "раскладку",
        "русские", "сейчас", "слова", "спасибо", "теперь", "тест", "текст", "файл", "хорошо", "что",
        "это"
    ];

    public static HashSet<string> English { get; } = Load("en.txt", BuiltInEnglish);

    public static HashSet<string> Russian { get; } = Load("ru.txt", BuiltInRussian);

    public static void EnsureUserDictionaryFiles()
    {
        Directory.CreateDirectory(DictionaryDirectory);
        EnsureFile("en.txt", BuiltInEnglish);
        EnsureFile("ru.txt", BuiltInRussian);
    }

    public static bool AddWord(LayoutLanguage language, string word)
    {
        var normalized = Normalize(word);
        if (normalized.Length == 0)
        {
            return false;
        }

        var fileName = language switch
        {
            LayoutLanguage.English => "en.txt",
            LayoutLanguage.Russian => "ru.txt",
            _ => null
        };

        var dictionary = language switch
        {
            LayoutLanguage.English => English,
            LayoutLanguage.Russian => Russian,
            _ => null
        };

        if (fileName is null || dictionary is null)
        {
            return false;
        }

        lock (SyncRoot)
        {
            if (!dictionary.Add(normalized))
            {
                return false;
            }

            Directory.CreateDirectory(DictionaryDirectory);
            File.AppendAllLines(Path.Combine(DictionaryDirectory, fileName), [normalized]);
            return true;
        }
    }

    private static string DictionaryDirectory => Path.Combine(AppSettings.DirectoryPath, "dictionaries");

    private static string BundledDictionaryDirectory => Path.Combine(AppContext.BaseDirectory, "data", "dictionaries");

    private static HashSet<string> Load(string fileName, IEnumerable<string> builtInWords)
    {
        var words = new HashSet<string>(builtInWords.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        LoadFile(Path.Combine(BundledDictionaryDirectory, fileName), words);
        LoadFile(Path.Combine(DictionaryDirectory, fileName), words);
        return words;
    }

    private static void LoadFile(string path, HashSet<string> words)
    {
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var word = Normalize(line);
            if (word.Length > 0 && !word.StartsWith('#'))
            {
                words.Add(word);
            }
        }
    }

    private static void EnsureFile(string fileName, IEnumerable<string> builtInWords)
    {
        var path = Path.Combine(DictionaryDirectory, fileName);
        if (File.Exists(path))
        {
            return;
        }

        var bundledPath = Path.Combine(BundledDictionaryDirectory, fileName);
        if (File.Exists(bundledPath))
        {
            File.Copy(bundledPath, path);
            return;
        }

        File.WriteAllLines(path, builtInWords.Order(StringComparer.OrdinalIgnoreCase));
    }

    private static string Normalize(string word) => word.Trim().ToLowerInvariant();
}
