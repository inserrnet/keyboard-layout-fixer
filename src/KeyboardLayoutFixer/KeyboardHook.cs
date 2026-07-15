using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyboardLayoutFixer;

internal sealed class KeyboardHook : IDisposable
{
    private readonly AppSettings _settings;
    private readonly LanguageDetector _detector = new();
    private readonly SynchronizationContext _context;
    private readonly NativeMethods.LowLevelKeyboardProc _callback;
    private readonly StringBuilder _currentWord = new();
    private nint _hook;
    private bool _disposed;

    public bool IsRunning => _hook != 0;

    public KeyboardHook(AppSettings settings)
    {
        _settings = settings;
        _context = SynchronizationContext.Current ?? new SynchronizationContext();
        _callback = HookCallback;
    }

    public void Start()
    {
        if (_hook != 0)
        {
            return;
        }

        _hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _callback, 0, 0);
    }

    public void Stop()
    {
        if (_hook == 0)
        {
            return;
        }

        NativeMethods.UnhookWindowsHookEx(_hook);
        _hook = 0;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0 || !_settings.Enabled || (wParam != NativeMethods.WM_KEYDOWN && wParam != NativeMethods.WM_SYSKEYDOWN))
        {
            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        var info = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
        if ((info.Flags & NativeMethods.LLKHF_INJECTED) != 0 || IsExcludedForegroundProcess())
        {
            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        var activeWindow = NativeMethods.GetForegroundWindow();
        var threadId = NativeMethods.GetWindowThreadProcessId(activeWindow, out _);
        var keyboardLayout = NativeMethods.GetKeyboardLayout(threadId);
        var language = GetLanguage(keyboardLayout);
        var typed = TranslateKey(info, keyboardLayout);

        if (typed is null)
        {
            if (info.VkCode is NativeMethods.VK_BACK)
            {
                TrimLastCharacter();
            }

            return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        if (IsWordCharacter(typed.Value))
        {
            if (_currentWord.Length < 48)
            {
                _currentWord.Append(typed.Value);
            }
        }
        else if (CompleteWord(typed.Value, language, activeWindow))
        {
            return 1;
        }

        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool CompleteWord(char delimiter, LayoutLanguage language, nint activeWindow)
    {
        if (_currentWord.Length == 0)
        {
            return false;
        }

        var word = _currentWord.ToString();
        _currentWord.Clear();

        var decision = _detector.Analyze(word, language, _settings.AutoCorrect, _settings.MinimumWordLength);
        if (!decision.ShouldSwitch)
        {
            return false;
        }

        var shouldReplace = decision.ShouldReplace && delimiter is not '\r' and not '\n' and not '\t';
        _context.Post(_ =>
        {
            if (shouldReplace)
            {
                ReplaceLastWord(word.Length, delimiter, decision.Replacement);
            }

            SwitchLayout(activeWindow, decision.TargetLanguage);
        }, null);

        return shouldReplace;
    }

    private static void SwitchLayout(nint activeWindow, LayoutLanguage targetLanguage)
    {
        var layoutId = targetLanguage == LayoutLanguage.Russian ? "00000419" : "00000409";
        var layout = NativeMethods.LoadKeyboardLayout(layoutId, NativeMethods.KLF_ACTIVATE);
        NativeMethods.PostMessage(activeWindow, NativeMethods.WM_INPUTLANGCHANGEREQUEST, 0, layout);
    }

    private static void ReplaceLastWord(int originalLength, char delimiter, string replacement)
    {
        var inputs = new List<NativeMethods.Input>();

        for (var i = 0; i < originalLength; i++)
        {
            AddVirtualKey(inputs, NativeMethods.VK_BACK);
        }

        foreach (var ch in replacement)
        {
            AddUnicode(inputs, ch);
        }

        AddUnicode(inputs, delimiter);

        NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<NativeMethods.Input>());
    }

    private static void AddVirtualKey(List<NativeMethods.Input> inputs, ushort key)
    {
        inputs.Add(new NativeMethods.Input { Type = 1, U = new NativeMethods.InputUnion { Ki = new NativeMethods.KeybdInput { WVk = key } } });
        inputs.Add(new NativeMethods.Input { Type = 1, U = new NativeMethods.InputUnion { Ki = new NativeMethods.KeybdInput { WVk = key, DwFlags = NativeMethods.KEYEVENTF_KEYUP } } });
    }

    private static void AddUnicode(List<NativeMethods.Input> inputs, char ch)
    {
        inputs.Add(new NativeMethods.Input { Type = 1, U = new NativeMethods.InputUnion { Ki = new NativeMethods.KeybdInput { WScan = (ushort)ch, DwFlags = NativeMethods.KEYEVENTF_UNICODE } } });
        inputs.Add(new NativeMethods.Input { Type = 1, U = new NativeMethods.InputUnion { Ki = new NativeMethods.KeybdInput { WScan = (ushort)ch, DwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP } } });
    }

    private static char? TranslateKey(NativeMethods.KbdLlHookStruct info, nint keyboardLayout)
    {
        var keyboardState = new byte[256];
        if (!NativeMethods.GetKeyboardState(keyboardState))
        {
            return null;
        }

        var buffer = new StringBuilder(8);
        var result = NativeMethods.ToUnicodeEx(info.VkCode, info.ScanCode, keyboardState, buffer, buffer.Capacity, 0, keyboardLayout);
        return result == 1 ? buffer[0] : null;
    }

    private static LayoutLanguage GetLanguage(nint keyboardLayout)
    {
        var languageId = (ushort)((long)keyboardLayout & 0xffff);
        return languageId switch
        {
            0x0409 => LayoutLanguage.English,
            0x0419 => LayoutLanguage.Russian,
            _ => LayoutLanguage.Other
        };
    }

    private bool IsExcludedForegroundProcess()
    {
        try
        {
            var window = NativeMethods.GetForegroundWindow();
            NativeMethods.GetWindowThreadProcessId(window, out var processId);
            var processName = Process.GetProcessById((int)processId).ProcessName;
            return _settings.ExcludedProcesses.Any(name => string.Equals(name, processName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private void TrimLastCharacter()
    {
        if (_currentWord.Length > 0)
        {
            _currentWord.Length--;
        }
    }

    private static bool IsWordCharacter(char ch) => char.IsLetter(ch) || ch is '\'' or '-';

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}
