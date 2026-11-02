using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Monokel.NativeMethods;

namespace Monokel;

/// <summary>
/// Steuert die Vollbild-Vergrößerung auf einem eigenen Thread, der im Takt der Bildwiederholrate
/// (DwmFlush) den Zoom weich an den Zielwert annähert und den Ausschnitt dem Cursor folgen lässt.
/// </summary>
internal sealed class Magnifier : IDisposable
{
    public const double MinZoom = 1.0;
    public const double MaxZoom = 20.0;

    /// <summary>Rad-Delta, das den Zoom verdoppelt (600 = fünf Rasten).</summary>
    private const double DeltaPerDoubling = 600.0;

    /// <summary>Zeitkonstante der Annäherung an den Zielzoom; kleiner = direkter, größer = weicher.</summary>
    private const double SmoothingSeconds = 0.06;

    private readonly Thread _thread;
    private readonly AutoResetEvent _wake = new(false);
    private readonly object _lock = new();
    private double _targetZoom = MinZoom;
    private volatile bool _stopping;

    public Magnifier()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "Monokel.Magnifier" };
        _thread.Start();
    }

    public void ZoomBy(int wheelDelta)
    {
        lock (_lock)
        {
            _targetZoom = Math.Clamp(_targetZoom * Math.Pow(2, wheelDelta / DeltaPerDoubling), MinZoom, MaxZoom);
        }
        _wake.Set();
    }

    public void Reset()
    {
        lock (_lock)
        {
            _targetZoom = MinZoom;
        }
        _wake.Set();
    }

    private double TargetZoom
    {
        get { lock (_lock) { return _targetZoom; } }
    }

    private void Run()
    {
        // Alle Mag*-Aufrufe bleiben auf diesem Thread.
        if (!MagInitialize())
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "MagInitialize fehlgeschlagen");
        }
        try
        {
            var zoom = MinZoom;
            var clock = Stopwatch.StartNew();
            var last = clock.Elapsed;
            (float zoom, int x, int y) applied = (1f, 0, 0);

            while (!_stopping)
            {
                var target = TargetZoom;
                if (zoom == MinZoom && target == MinZoom)
                {
                    // Nichts vergrößert: schlafen, bis wieder gezoomt wird.
                    _wake.WaitOne();
                    last = clock.Elapsed;
                    continue;
                }

                DwmFlush();
                var now = clock.Elapsed;
                var dt = (now - last).TotalSeconds;
                last = now;

                // Im logarithmischen Raum annähern, damit sich jede Zoomstufe gleich schnell anfühlt.
                var alpha = 1 - Math.Exp(-dt / SmoothingSeconds);
                zoom = Math.Exp(Math.Log(zoom) + (Math.Log(target) - Math.Log(zoom)) * alpha);
                if (Math.Abs(zoom - target) < 0.001)
                {
                    zoom = target;
                }

                var next = ComputeTransform(zoom);
                if (next != applied)
                {
                    MagSetFullscreenTransform(next.zoom, next.x, next.y);
                    applied = next;
                }
            }
        }
        finally
        {
            MagSetFullscreenTransform(1f, 0, 0);
            MagUninitialize();
        }
    }

    /// <summary>
    /// Proportionale Verfolgung: Der Ausschnitt wird so gelegt, dass der Cursor auf dem Bildschirm genau dort
    /// erscheint, wo er unvergrößert wäre. Dadurch bleibt der Punkt unter dem Cursor beim Zoomen stehen, und jeder
    /// Rand des Desktops ist erreichbar, indem man die Maus dorthin bewegt.
    /// Die Vergrößerung wirkt auf den gesamten virtuellen Desktop, und Cursor wie Offset beziehen sich beide auf die
    /// linke obere Ecke des primären Monitors. Die Formel gilt deshalb auf jedem Monitor unverändert, auch bei
    /// negativen Koordinaten links oder oberhalb des primären.
    /// </summary>
    private static (float zoom, int x, int y) ComputeTransform(double zoom)
    {
        GetCursorPos(out var cursor);
        var factor = 1 - 1 / zoom;
        var x = (int)Math.Round(cursor.X * factor);
        var y = (int)Math.Round(cursor.Y * factor);
        return ((float)zoom, x, y);
    }

    public void Dispose()
    {
        _stopping = true;
        _wake.Set();
        _thread.Join(TimeSpan.FromSeconds(2));
        _wake.Dispose();
    }
}
