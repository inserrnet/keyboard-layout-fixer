using System.Diagnostics;

namespace KeyboardLayoutFixer;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly KeyboardHook _hook;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _startupRetryTimer;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        _hook = new KeyboardHook(_settings);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "Keyboard Layout Fixer",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _hook.Start();
        _startupRetryTimer = new System.Windows.Forms.Timer { Interval = 300 };
        _startupRetryTimer.Tick += (_, _) =>
        {
            if (!_hook.IsRunning)
            {
                _hook.Start();
            }

            _startupRetryTimer.Stop();
        };
        _startupRetryTimer.Start();

        _notifyIcon.ShowBalloonTip(2000, "Keyboard Layout Fixer", "Утилита запущена локально.", ToolTipIcon.Info);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var enabledItem = new ToolStripMenuItem("Включено")
        {
            Checked = _settings.Enabled,
            CheckOnClick = true
        };
        enabledItem.CheckedChanged += (_, _) =>
        {
            _settings.Enabled = enabledItem.Checked;
            _settings.Save();
            _notifyIcon.Text = _settings.Enabled ? "Keyboard Layout Fixer" : "Keyboard Layout Fixer выключен";
        };

        var autoCorrectItem = new ToolStripMenuItem("Автоисправление")
        {
            Checked = _settings.AutoCorrect,
            CheckOnClick = true
        };
        autoCorrectItem.CheckedChanged += (_, _) =>
        {
            _settings.AutoCorrect = autoCorrectItem.Checked;
            _settings.Save();
        };

        var settingsItem = new ToolStripMenuItem("Открыть настройки");
        settingsItem.Click += (_, _) =>
        {
            _settings.Save();
            Process.Start(new ProcessStartInfo(AppSettings.FilePath) { UseShellExecute = true });
        };

        var exitItem = new ToolStripMenuItem("Выход");
        exitItem.Click += (_, _) => ExitThread();

        menu.Items.Add(enabledItem);
        menu.Items.Add(autoCorrectItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(exitItem);
        return menu;
    }

    private static Icon LoadTrayIcon()
    {
        var stream = typeof(TrayApplicationContext).Assembly.GetManifestResourceStream("KeyboardLayoutFixer.app.ico");
        return stream is null ? SystemIcons.Application : new Icon(stream);
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _startupRetryTimer.Dispose();
        _notifyIcon.Dispose();
        _hook.Dispose();
        base.ExitThreadCore();
    }
}
