# Fork notice

Personal fork of **SAIN** (Solarint's AI Modifications), 4.2.0+ line maintained by ArchangelWTF.

- Upstream: https://github.com/ArchangelWTF/SAIN (remote `upstream`), based on tag `v4.5.1`
- Original project: https://github.com/Solarint/SAIN
- License: MIT (see `LICENSE`, kept unchanged)
- Branches: `master` mirrors upstream 1:1; work lives on `door-fix`, `reload-fix` and `shot-reaction` (each branched from the previous).

## What changed

- `SAIN/SAIN.csproj`: `DeployToGameDir` target copies `SAIN.dll` and `SAIN.Preset.Shared.dll` to `BepInEx\plugins\SAIN\`, keeping `.orig` (first ever) and `.bak` (previous). Game folder comes from `SAIN/SAIN.csproj.user` (not committed). Opt out with `-p:DeployToGame=false`.

- `DoorOpener.cs`: door cooldown timestamps are kept in `_allDoors` (they were lost on every refresh); bots only close open doors if the new `Bots Close Doors` setting is on (default off).
- `DoorOpener.cs` / `BotPathData.cs`: when a door is pulled open, the bot walks to a point outside the door's swing circle instead of the previous path corner; one path recalculation after each door interaction.
- `BotPathData.cs`: door-interaction movement passed a world position to `SetTargetMoveDirection` where a direction is expected; now passes `target - bot position`.

- `DoorOpener.cs` / `BotPathData.cs`: a bot that has made no progress for 1 s and is within 2.5 m of an open door closes that door (stuck-only last resort, independent of `Bots Close Doors`).

- `ForkSettings.cs` (F6: Global Settings > Fork Tweaks, saved in the preset): all fork-only settings. Reload: random suppress cutoff per magazine (`SAINBotSuppressClass.cs`), bots relocate to different cover when they start a reload in close quarters (`SAINCoverClass.cs`, `SelfActionDecisionClass.cs`). Doors: `Bots Close Doors` moved here from `DoorSettings`, plus switches and values for the stuck-close, swing retreat and interaction time. The SAIN server mod must run the matching `SAIN.Preset.Shared.dll`, otherwise it drops these keys when it saves the preset.

- Range behavior (branch `shot-reaction`, settings in Fork Tweaks, each with an off switch):
  - `WeaponInfo.cs` / `BotWeaponInfoClass.cs`: a bot's effective reach is the weapon-class reach times a factor for its sights (iron < red dot < optic, rising with the optic's zoom read from the item's `SightComponent`). Before, optics did not matter: an iron-sight AK scav counted as a 150 m weapon.
  - `SAINShootData.cs` (`AimAndShootAtEnemy`) and `EnemyDecisionClass.cs` (`shallShootDistantEnemy`): a bot does not aim or fire at a visible enemy farther than reach times `Shoot Reach Factor`. The cover and search layers shoot through `ShootAnyVisibleEnemies`, which had no distance limit.
  - `SAINBotSuppressClass.cs` (`TrySuppressEnemy`): no suppressing bursts at a last known position farther than reach times `Suppress Reach Fraction`. Before, there was no distance limit (rogues full-auto suppressing at 300 m).
  - `ForkBrainFilter.cs` / `BotProfile.cs`: per-brain toggles (PMCs, Rogues, Raiders, Scavs, ...). Range and Shot Reaction each have their own set (`RangeBrain*`, `ShotBrain*`, `BotProfile.RangeEnabled` / `ShotReactionEnabled`). A bot type that is off keeps the original behavior for that feature. Default on: PMCs and Rogues.

- Shot reaction (branch `shot-reaction`, Fork Tweaks "Shot Reaction", master switch plus the same brain filter):
  - `ShotReactionClass.cs` (new, on `BotComponent.ShotReaction`): a bot hit by, or passed closely by a bullet of, an enemy it cannot see gets a vague area (`DegradePosition`: random point around the shooter, error grows with distance) and one weighted roll: Cover, Fight (hides for a reaction delay, then normal combat), Suppress, Hunt (hides, assesses, then normal search/advance). Weights per zone (short/mid/long, as fractions of the bot's sight-adjusted reach), pulled by weapon (sight factor, class, reach), personality, shooter concealment (suppressor, prone), hit vs bullet pass, bot state (running, badly hurt), with a minimum Cover share and a maximum Fight share. Event driven, nothing ticks.
  - `HearingSensor.cs`, `BotHitByEnemyClass.cs`: the hooks (position degraded for close bullets and heard gunfire; roll on hit or close bullet).
  - `EnemyDecisionClass.cs`: while the reaction says hide, the decision is `SeekCover`; otherwise the normal decisions run.
  - `EnemyKnownChecker.cs`: a shooter the bot never saw is forgotten when its reaction is over (unless new information arrived), so the bot goes back to what it was doing.
  - `SAINBotSuppressClass.cs`: suppressing bursts only for the Suppress outcome while a reaction is active.
  - `EnemyGainSightClass.cs`, `EnemyVisionDistanceClass.cs`: the bonus for spotting a shooter that fires at you (spot time x0.5, vision distance x1.5) is configurable and off by default for bots with shot reaction on.
  - Relocate (`ShotReactionClass.cs`, new `RelocateAction.cs`, new `ECombatDecision.Relocate`, `CombatSoloLayer.cs`): after a Cover or Suppress hide, the bot (chance configurable) walks slowly to a navmesh point 40-80 m away on the far side from the vague shot area, then resumes its task from there, instead of going back to where it was shot at. Settings `Relocate ...` in Fork Tweaks; no point found or the bot sees the shooter = original flow.
  - Over-reach fire (`BotWeaponInfoClass.IsInOverReachWindow`, used by `IsWithinShootReach` and the `outOfRange` check in `EnemyDecisionClass.shallStandAndShoot`): a bot that sees an enemy beyond its reach times Shoot Reach Factor still gets a chance (default 25%, falling linearly to 0 at 2.5 x reach) rolled once per 6 s that opens a ~3 s window in which it may aim and fire, instead of a hard cut-off. Direct fire only; suppressing keeps its 0.8 x reach cap.

- `References\` is no longer tracked in this fork (git-ignored). It holds game and third-party assemblies that upstream bundles; this fork does not redistribute them. To build, copy `References\` from the upstream repo at `v4.5.1` (`git show upstream/master:References/...` or a checkout of the tag) into the repo root; the files are used for compilation only.

- Fork Tweaks menu (F6): settings carry `[Category("Section/Subsection")]` (Reload, Doors, Range, Shot Reaction, each with subsections including its master switches and brain filter). `AttributesGUI.cs` (`DisplayOptionsByCategory`, `DisplaySectionMenu`) draws such categories as two-level collapsible menus, collapsed by default and forced open while searching; categories without a slash draw as before. Menu labels of the reload relocate and shot relocate settings were prefixed to tell them apart.

## AI use

Changes in this fork are written with AI assistance (Claude Code). The fork is personal and not meant to be sent upstream as is.
