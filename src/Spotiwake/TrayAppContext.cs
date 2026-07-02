namespace Spotiwake;

/// <summary>
/// Aplicativo de bandeja: sem janela principal, apenas um NotifyIcon com menu
/// de contexto e um timer que verifica o Spotify periodicamente.
/// </summary>
internal sealed class TrayAppContext : ApplicationContext
{
    private const int PollIntervalMs = 5000;

    private readonly AppSettings _settings;
    private readonly SpotifyDetector _detector = new();
    private readonly PowerGuard _power = new();
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer;

    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _displayItem;
    private readonly ToolStripMenuItem _startupItem;

    public TrayAppContext()
    {
        _settings = AppSettings.Load();

        _statusItem = new ToolStripMenuItem("Verificando...") { Enabled = false };

        _enabledItem = new ToolStripMenuItem("Ativado")
        {
            Checked = _settings.Enabled,
            CheckOnClick = true,
        };
        _enabledItem.CheckedChanged += (_, _) =>
        {
            _settings.Enabled = _enabledItem.Checked;
            _settings.Save();
            Refresh();
        };

        _displayItem = new ToolStripMenuItem("Manter a tela ligada também")
        {
            Checked = _settings.KeepDisplayOn,
            CheckOnClick = true,
        };
        _displayItem.CheckedChanged += (_, _) =>
        {
            _settings.KeepDisplayOn = _displayItem.Checked;
            _settings.Save();
            Refresh();
        };

        _startupItem = new ToolStripMenuItem("Iniciar com o Windows")
        {
            Checked = StartupManager.IsEnabled(),
            CheckOnClick = true,
        };
        _startupItem.CheckedChanged += (_, _) => StartupManager.SetEnabled(_startupItem.Checked);

        var exitItem = new ToolStripMenuItem("Sair");
        exitItem.Click += (_, _) => ExitApplication();

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_enabledItem);
        menu.Items.Add(_displayItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _tray = new NotifyIcon
        {
            Icon = TrayIcons.Idle,
            Text = "Spotiwake",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _tray.DoubleClick += (_, _) => _enabledItem.Checked = !_enabledItem.Checked;

        _timer = new System.Windows.Forms.Timer { Interval = PollIntervalMs };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();

        Refresh();
    }

    private void Refresh()
    {
        var status = _detector.GetStatus();
        bool keepAwake = _settings.Enabled && status == SpotifyStatus.Playing;

        // KeepAwake é idempotente; reafirmar a cada ciclo é seguro e garante
        // que o estado sobreviva a eventos externos.
        if (keepAwake)
        {
            _power.KeepAwake(_settings.KeepDisplayOn);
        }
        else
        {
            _power.Release();
        }

        UpdateUi(status, keepAwake);
    }

    private void UpdateUi(SpotifyStatus status, bool keepAwake)
    {
        string statusText;
        Icon icon;

        if (!_settings.Enabled)
        {
            statusText = "Proteção desativada";
            icon = TrayIcons.Disabled;
        }
        else if (keepAwake)
        {
            statusText = "Spotify tocando — suspensão bloqueada";
            icon = TrayIcons.Blocking;
        }
        else if (status == SpotifyStatus.Paused)
        {
            statusText = "Spotify pausado — suspensão permitida";
            icon = TrayIcons.Idle;
        }
        else
        {
            statusText = "Spotify não está em execução";
            icon = TrayIcons.Idle;
        }

        _statusItem.Text = statusText;

        if (!ReferenceEquals(_tray.Icon, icon))
        {
            _tray.Icon = icon;
        }

        _tray.Text = Truncate($"Spotiwake — {statusText}", 63);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private void ExitApplication()
    {
        _timer.Stop();
        _power.Release();
        _tray.Visible = false;
        _tray.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _power.Dispose();
            _tray.Dispose();
        }

        base.Dispose(disposing);
    }
}
