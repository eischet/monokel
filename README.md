<p align="center">
  <img src="assets/Monokel-512.png" alt="Monokel: ein Adler mit Fliegerhaube, der durch ein Monokel schaut" width="256">
</p>

# Monokel

Bildschirmlupe für Windows nach dem Vorbild der Zoomfunktion von macOS: Strg + Mausrad (oder Touchpad)
zoomt stufenlos und weich um den Mauszeiger herum.

## Bedienung

- **Strg + Mausrad** vergrößert bzw. verkleinert, die Lupe folgt dem Mauszeiger.
- Monokel läuft ohne Fenster, nur mit einem Symbol im Infobereich. Dessen Menü bietet:
  - **Zoom zurücksetzen**
  - **Zoomtaste:** Strg oder Windows-Taste
  - **Beenden**
- Die Einstellungen liegen in `%APPDATA%\Monokel\settings.json`.

## Bauen

Voraussetzung ist das .NET 10 SDK.

```powershell
dotnet build
dotnet run --project src/Monokel
```

Das Programmsymbol `src/Monokel/Monokel.ico` und das Bild oben (`assets/Monokel-512.png`) werden aus
`assets/Monokel.png` erzeugt:

```powershell
pwsh assets/make-ico.ps1
```

## Lizenz

MIT, siehe [LICENSE](LICENSE).
