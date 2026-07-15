# Keyboard Layout Fixer

Local Windows tray utility for Russian/English keyboard layout mistakes.

The app watches only the current word locally, converts it between Russian and English keyboard layouts, and corrects it only when the converted word is present in the local dictionary.

## Requirements

- Windows x64.
- .NET Desktop Runtime 8 installed.

## Features

- Runs locally on Windows.
- Lives in the system tray.
- Uses local dictionaries instead of broad guessing heuristics.
- Auto-correction can be turned off from the tray menu.
- Stores settings in `%APPDATA%\KeyboardLayoutFixer\settings.json`.
- Stores dictionaries in `%APPDATA%\KeyboardLayoutFixer\dictionaries\`.
- Does not send typed text anywhere and does not keep typing logs.

## Dictionaries

Use the tray menu item `Открыть словари` to open the dictionary folder.

- `ru.txt` contains Russian words.
- `en.txt` contains English words.
- Add one word per line.
- Restart the app after editing dictionaries.

## Build

The repository includes a GitHub Actions workflow that builds a small runtime-dependent Windows x64 executable.

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

The app is intentionally conservative: it waits until a word is completed and only acts when the converted word is present in the local dictionary. Apps such as terminals and code editors are excluded by default in the settings file.
