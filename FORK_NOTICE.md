# Fork notice

Personal fork of **SAIN** (Solarint's AI Modifications), 4.2.0+ line maintained by ArchangelWTF.

- Upstream: https://github.com/ArchangelWTF/SAIN (remote `upstream`), based on tag `v4.5.1`
- Original project: https://github.com/Solarint/SAIN
- License: MIT (see `LICENSE`, kept unchanged)
- Branches: `master` mirrors upstream 1:1; work lives on `door-fix`.

## What changed

- `SAIN/SAIN.csproj`: `DeployToGameDir` target copies `SAIN.dll` and `SAIN.Preset.Shared.dll` to `BepInEx\plugins\SAIN\`, keeping `.orig` (first ever) and `.bak` (previous). Game folder comes from `SAIN/SAIN.csproj.user` (not committed). Opt out with `-p:DeployToGame=false`.

- `DoorOpener.cs`: door cooldown timestamps are kept in `_allDoors` (they were lost on every refresh); bots only close open doors if the new `Bots Close Doors` setting is on (default off).
- `DoorOpener.cs` / `BotPathData.cs`: when a door is pulled open, the bot walks to a point outside the door's swing circle instead of the previous path corner; one path recalculation after each door interaction.
- `BotPathData.cs`: door-interaction movement passed a world position to `SetTargetMoveDirection` where a direction is expected; now passes `target - bot position`.

## AI use

Changes in this fork are written with AI assistance (Claude Code). The fork is personal and not meant to be sent upstream as is.
