namespace KeyboardLayoutFixer;

internal sealed class GlobalHotkeyWindow : NativeWindow, IDisposable
{
    private const int AddWordHotkeyId = 1;
    private readonly Action _onAddWord;
    private bool _registered;

    public GlobalHotkeyWindow(Action onAddWord)
    {
        _onAddWord = onAddWord;
        CreateHandle(new CreateParams());
        _registered = NativeMethods.RegisterHotKey(
            Handle,
            AddWordHotkeyId,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT,
            NativeMethods.VK_D);
    }

    public bool IsRegistered => _registered;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == AddWordHotkeyId)
        {
            _onAddWord();
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(Handle, AddWordHotkeyId);
            _registered = false;
        }

        DestroyHandle();
    }
}
