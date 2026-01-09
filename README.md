# BossNotifier for SPT 4.0.11

A client mod that notifies you which bosses are present in your raid.

## Features

- 🔔 Notification at raid start showing which bosses spawned
- 📍 Shows boss spawn locations (if Intel Center unlocked)
- ✓ Real-time detection when you get near a boss
- 🌙 Cultist notifications only appear at night (7 PM - 7 AM)
- ⌨️ Press 'O' to show boss notifications again (can change by F12 menu)
  - Press F12 in-game to access BepInEx configuration:
    - Keyboard shortcut (default: O)
    - Show notifications on raid start
    - Intel Center level requirements 

## Installation


1. Download the latest release from [Releases](https://github.com/Mattexe21/BossNotifier/releases/tag/Latest)
2. Extract the zip into your SPT root folder (where `SPT.Server.exe` is)
3. Start the game


## Build Requirements

If you want to compile from source, you need these DLLs:
- `BepInEx.dll` (from SPT/BepInEx/core/)
- `0Harmony.dll` (from SPT/BepInEx/core/)
- `Assembly-CSharp.dll` (from SPT/EscapeFromTarkov_Data/Managed/)
- `UnityEngine.dll` (from SPT/EscapeFromTarkov_Data/Managed/)
- `spt-reflection.dll` (from SPT/BepInEx/plugins/spt/)
- `spt-common.dll` (from SPT/BepInEx/plugins/spt/)

## Credits

- Original mod by [Mattdokn](https://github.com/m-barneto/BossNotifier)
- Updated for SPT 4.0.11

## License

MIT License
