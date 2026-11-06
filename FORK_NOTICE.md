# Fork notice

Personal fork of **SAIN** (Solarint's AI Modifications), 4.2.0+ line maintained by ArchangelWTF.

- Upstream: https://github.com/ArchangelWTF/SAIN (remote `upstream`), based on tag `v4.5.1`
- Original project: https://github.com/Solarint/SAIN
- License: MIT (see `LICENSE`, kept unchanged)
- Branches: `master` mirrors upstream 1:1; work lives on `door-fix`.

## What changed

- `SAIN/SAIN.csproj`: `DeployToGameDir` target copies `SAIN.dll` and `SAIN.Preset.Shared.dll` to `BepInEx\plugins\SAIN\`, keeping `.orig` (first ever) and `.bak` (previous). Game folder comes from `SAIN/SAIN.csproj.user` (not committed). Opt out with `-p:DeployToGame=false`.

## AI use

Changes in this fork are written with AI assistance (Claude Code). The fork is personal and not meant to be sent upstream as is.
