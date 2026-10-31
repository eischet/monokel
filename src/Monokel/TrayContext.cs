using System.Drawing.Drawing2D;
using static Monokel.NativeMethods;

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
        _icon = CreateMonocleIcon();

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

    /// <summary>Vorläufiges Symbol, bis es ein richtiges .ico gibt: Glas mit Kettchen.</summary>
    private static Icon CreateMonocleIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var glass = new SolidBrush(Color.FromArgb(90, 170, 210, 255));
            using var rim = new Pen(Color.FromArgb(40, 40, 40), 3f);
            using var chain = new Pen(Color.Goldenrod, 2f) { DashStyle = DashStyle.Dot };
            g.FillEllipse(glass, 3, 3, 20, 20);
            g.DrawEllipse(rim, 3, 3, 20, 20);
            g.DrawBezier(chain, 20, 21, 24, 26, 28, 24, 30, 31);
        }
        var handle = bmp.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }
}
