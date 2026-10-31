namespace Monokel;

/// <summary>Monokel läuft ohne Fenster, nur mit einem Symbol im Infobereich.</summary>
internal sealed class TrayContext : ApplicationContext
{
    private readonly Settings _settings = Settings.Load();
    private readonly Magnifier _magnifier = new();
    private readonly WheelHook _hook;
    private readonly NotifyIcon _tray;
    private readonly Icon _icon;
    private readonly ToolStripMenuItem _modifierMenu;

    public TrayContext()
    {
        _hook = new WheelHook(_settings.Modifier, _magnifier.ZoomBy);
        _icon = LoadTrayIcon();

        _modifierMenu = new ToolStripMenuItem("Zoomtaste");
        foreach (var modifier in Enum.GetValues<ZoomModifier>())
        {
            _modifierMenu.DropDownItems.Add(new ToolStripMenuItem(DisplayName(modifier), null, (_, _) => SetModifier(modifier))
            {
                Tag = modifier,
            });
        }

        var menu = new ContextMenuStrip();
        menu.Items.Add("Zoom zurücksetzen", null, (_, _) => _magnifier.Reset());
        menu.Items.Add(_modifierMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Beenden", null, (_, _) => ExitThread());

        _tray = new NotifyIcon
        {
            Icon = _icon,
            ContextMenuStrip = menu,
            Visible = true,
        };
        UpdateModifierDisplay();
    }

    private static string DisplayName(ZoomModifier modifier) => modifier switch
    {
        ZoomModifier.Windows => "Windows-Taste",
        _ => "Strg",
    };

    private void SetModifier(ZoomModifier modifier)
    {
        _settings.Modifier = modifier;
        _hook.Modifier = modifier;
        UpdateModifierDisplay();
        try
        {
            _settings.Save();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Einstellungen konnten nicht gespeichert werden:\n{e.Message}", "Monokel",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateModifierDisplay()
    {
        foreach (ToolStripMenuItem item in _modifierMenu.DropDownItems)
        {
            item.Checked = (ZoomModifier)item.Tag! == _settings.Modifier;
        }
        _tray.Text = $"Monokel – {DisplayName(_settings.Modifier)} + Mausrad zum Zoomen";
    }

    protected override void ExitThreadCore()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _hook.Dispose();
        _magnifier.Dispose();
        _icon.Dispose();
        base.ExitThreadCore();
    }

    /// <summary>Programmsymbol in der Größe, die der Infobereich bei der aktuellen DPI-Stufe braucht.</summary>
    private static Icon LoadTrayIcon()
    {
        using var stream = typeof(TrayContext).Assembly.GetManifestResourceStream("Monokel.ico")!;
        return new Icon(stream, SystemInformation.SmallIconSize);
    }
}
