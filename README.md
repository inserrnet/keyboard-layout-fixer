# Keyboard Layout Fixer

Local Windows tray utility for Russian/English keyboard layout mistakes.

The app watches only the current word locally, detects obvious cases like `ghbdtn` -> `привет` or `руддщ` -> `hello`, switches the keyboard layout, and can optionally replace the mistyped word.

## Features

- Runs locally on Windows.
- Lives in the system tray.
- Detects Russian and English keyboard layout mix-ups.
- Auto-correction can be turned off from the tray menu.
- Stores settings in `%APPDATA%\KeyboardLayoutFixer\settings.json`.
- Uses Windows Spell Checking when it is available for the language.
- Uses local dictionaries in `%APPDATA%\KeyboardLayoutFixer\dictionaries\` as a fallback.
- Adds a selected word to the user dictionary with `Ctrl+Shift+D`.
- Does not send typed text anywhere and does not keep typing logs.

## Build

The repository includes a GitHub Actions workflow that builds a Windows x64 executable for .NET Desktop Runtime 8.

To build manually on a machine with the .NET SDK:

```powershell
dotnet publish src/KeyboardLayoutFixer/KeyboardLayoutFixer.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -o artifacts/KeyboardLayoutFixer
```

## Notes

The app is intentionally conservative: it waits until a word is completed and only acts when the converted word is recognized by Windows Spell Checking or by the local dictionary. To add a word manually, select it in any app and press `Ctrl+Shift+D`. Apps such as terminals and code editors are excluded by default in the settings file.
