# Monokel

Bildschirmlupe für Windows nach dem Vorbild der macOS-Zoomfunktion: Strg + Mausrad/Touchpad zoomt
stufenlos und weich um den Cursor herum. Eigenständiges Projekt, ohne Bezug zu den anderen Repos unter `C:\projects`.

Open Source (MIT, siehe `LICENSE`): keine Verweise auf nicht öffentliche Projekte einbringen.

- C# / .NET 10, WinForms nur für das Tray-Symbol. Solution: `Monokel.slnx`, Projekt: `src/Monokel`.
- Bauen: `dotnet build`, Starten: `dotnet run --project src/Monokel`.
- Technik: Magnification API (`MagSetFullscreenTransform`) für die Vergrößerung, `WH_MOUSE_LL`-Hook für
  Strg + Rad, eigener Thread mit `DwmFlush()` für die weiche Animation im Takt der Bildwiederholrate.
- Zoomtaste (Strg oder Windows) im Tray-Menü wählbar, gespeichert in `%APPDATA%\Monokel\settings.json`. Bei der
  Windows-Taste schiebt `WheelHook` eine unbelegte Taste (vkE8) ein, damit beim Loslassen kein Startmenü aufgeht.
- Alle `Mag*`-Aufrufe laufen auf dem Magnifier-Thread (`Magnifier.cs`); der Hook-Callback muss sofort zurückkehren.
- `uiAccess="true"` (Zoomen auch über Admin-Fenstern/UAC) erst, wenn signiert und unter `Program Files` installiert.
