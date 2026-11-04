# Monokel

Bildschirmlupe für Windows nach dem Vorbild der macOS-Zoomfunktion: Strg + Mausrad/Touchpad zoomt
stufenlos und weich um den Cursor herum.

Open Source (MIT, siehe `LICENSE`).

- C# / .NET 10, WinForms nur für das Tray-Symbol. Solution: `Monokel.slnx`, Projekt: `src/Monokel`.
- Bauen: `dotnet build`, Starten: `dotnet run --project src/Monokel`.
- Technik: Magnification API (`MagSetFullscreenTransform`) für die Vergrößerung, `WH_MOUSE_LL`-Hook für
  Strg + Rad, eigener Thread mit `DwmFlush()` für die weiche Animation im Takt der Bildwiederholrate.
- Zoomtaste (Strg oder Windows) im Tray-Menü wählbar, gespeichert in der Registry unter `HKCU\Software\Monokel`. Bei
  der Windows-Taste schiebt `WheelHook` eine unbelegte Taste (vkE8) ein, damit beim Loslassen kein Startmenü aufgeht.
- Autostart im Tray-Menü schaltbar (`Autostart.cs`): Der Zustand ist der Eintrag im `Run`-Schlüssel von HKCU selbst;
  der Haken ist nur gesetzt, wenn dort genau die laufende EXE steht.
- Alle `Mag*`-Aufrufe laufen auf dem Magnifier-Thread (`Magnifier.cs`); der Hook-Callback muss sofort zurückkehren.
- Programmsymbol: `src/Monokel/Monokel.ico` (EXE und Tray) wird mit `assets/make-ico.ps1` aus `assets/Monokel.png`
  erzeugt, ebenso `assets/Monokel-512.png` für die `README.md`.
- `uiAccess="true"` (Zoomen auch über Admin-Fenstern/UAC) erst, wenn signiert und unter `Program Files` installiert.
