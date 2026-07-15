using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace KeyboardLayoutFixer;

internal static partial class SelectedWordDictionaryAdder
{
    public static async void AddSelectedWord(NotifyIcon notifyIcon)
    {
        IDataObject? previousClipboard = null;
        var hadClipboard = false;

        try
        {
            previousClipboard = Clipboard.GetDataObject();
            hadClipboard = previousClipboard is not null;

            await WaitForHotkeyRelease();
            SendCopyShortcut();
            await Task.Delay(180);

            var selectedText = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
            var word = CleanSelectedWord(selectedText);
            if (word.Length == 0)
            {
                Show(notifyIcon, "Не вижу выделенного слова.");
                return;
            }

            var language = DetectLanguage(word);
            if (language == LayoutLanguage.Other)
            {
                Show(notifyIcon, "Выдели одно русское или английское слово.");
                return;
            }

            var added = WordDictionaries.AddWord(language, word);
            Show(notifyIcon, added ? $"Добавлено в словарь: {word}" : $"Уже есть в словаре: {word}");
        }
        catch (ExternalException)
        {
            Show(notifyIcon, "Не получилось прочитать буфер обмена.");
        }
        catch
        {
            Show(notifyIcon, "Не получилось добавить слово.");
        }
        finally
        {
            RestoreClipboard(previousClipboard, hadClipboard);
        }
    }

    private static void SendCopyShortcut()
    {
        var inputs = new List<NativeMethods.Input>();
        AddVirtualKey(inputs, NativeMethods.VK_CONTROL, keyUp: false);
        AddVirtualKey(inputs, NativeMethods.VK_C, keyUp: false);
        AddVirtualKey(inputs, NativeMethods.VK_C, keyUp: true);
        AddVirtualKey(inputs, NativeMethods.VK_CONTROL, keyUp: true);
        NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeMethods.Input>());
    }

    private static async Task WaitForHotkeyRelease()
    {
        for (var i = 0; i < 20; i++)
        {
            if (!IsKeyDown(NativeMethods.VK_CONTROL) && !IsKeyDown(NativeMethods.VK_SHIFT) && !IsKeyDown(NativeMethods.VK_D))
            {
                return;
            }

            await Task.Delay(25);
        }
    }

    private static bool IsKeyDown(ushort key) => (NativeMethods.GetAsyncKeyState(key) & 0x8000) != 0;

    private static void AddVirtualKey(List<NativeMethods.Input> inputs, ushort key, bool keyUp)
    {
        inputs.Add(new NativeMethods.Input
        {
            Type = 1,
            U = new NativeMethods.InputUnion
            {
                Ki = new NativeMethods.KeybdInput
                {
                    WVk = key,
                    DwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0
                }
            }
        });
    }

    private static string CleanSelectedWord(string text)
    {
        var word = text.Trim();
        word = TrimPunctuation(word);

        return SingleWordRegex().IsMatch(word) ? word : string.Empty;
    }

    private static string TrimPunctuation(string word) =>
        word.Trim(' ', '\t', '\r', '\n', '.', ',', '!', '?', ':', ';', '"', '\'', '`', '«', '»', '“', '”', '(', ')', '[', ']', '{', '}');

    private static LayoutLanguage DetectLanguage(string word)
    {
        if (word.All(ch => ch is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-'))
        {
            return LayoutLanguage.English;
        }

        if (word.All(ch => ch is >= 'а' and <= 'я' or 'ё' or >= 'А' and <= 'Я' or 'Ё' or '-'))
        {
            return LayoutLanguage.Russian;
        }

        return LayoutLanguage.Other;
    }

    private static void RestoreClipboard(IDataObject? previousClipboard, bool hadClipboard)
    {
        try
        {
            if (hadClipboard && previousClipboard is not null)
            {
                Clipboard.SetDataObject(previousClipboard, true);
            }
            else
            {
                Clipboard.Clear();
            }
        }
        catch
        {
            // Some apps keep the clipboard locked for a moment; the copied word is harmless if restore fails.
        }
    }

    private static void Show(NotifyIcon notifyIcon, string text) =>
        notifyIcon.ShowBalloonTip(1400, "Keyboard Layout Fixer", text, ToolTipIcon.Info);

    [GeneratedRegex(@"^[\p{L}-]+$")]
    private static partial Regex SingleWordRegex();
}
