# Keyboard Layout Fixer

Local Windows tray utility for Russian/English keyboard layout mistakes.

The app watches only the current word locally, detects obvious cases like `ghbdtn` -> `привет` or `руддщ` -> `hello`, switches the keyboard layout, and can optionally replace the mistyped word.

## Features

- Runs locally on Windows.
- Lives in the system tray.
- Detects Russian and English keyboard layout mix-ups.
- Auto-correction can be turned off from the tray menu.
- Stores settings in `%APPDATA%\KeyboardLayoutFixer\settings.json`.
- Uses local dictionaries in `%APPDATA%\KeyboardLayoutFixer\dictionaries\`.
- Does not send typed text anywhere and does not keep typing logs.

## Build

The repository includes a GitHub Actions workflow that builds a self-contained Windows x64 executable.

To build manually on a machine with the .NET SDK:

```powershell
dotnet publish src/KeyboardLayoutFixer/KeyboardLayoutFixer.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o artifacts/KeyboardLayoutFixer
```

## Notes

The app is intentionally conservative: it waits until a word is completed and only acts when the converted word is present in the local dictionary. Apps such as terminals and code editors are excluded by default in the settings file.
