using System.Text.Json;

namespace KeyboardLayoutFixer;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public bool AutoCorrect { get; set; } = true;
    public int MinimumWordLength { get; set; } = 3;
    public List<string> ExcludedProcesses { get; set; } =
    [
        "WindowsTerminal",
        "cmd",
        "powershell",
        "pwsh",
        "Code",
        "devenv",
        "rider64"
    ];

    public static string DirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeyboardLayoutFixer");

    public static string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
