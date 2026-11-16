using System.Collections.Generic;
using EFT;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings.Categories;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses;

public class EnemyAimTarget(EnemyData enemyData) : EnemyBase(enemyData, enemyData.Enemy.Bot)
{
    private static readonly EAimTargetPart[] _parts =
    [
        EAimTargetPart.Head,
        EAimTargetPart.Chest,
        EAimTargetPart.Stomach,
        EAimTargetPart.LeftArm,
        EAimTargetPart.RightArm,
        EAimTargetPart.LeftLeg,
        EAimTargetPart.RightLeg,
    ];

    private readonly float[] _weights = new float[_parts.Length];

    private EAimTargetPart? _chosen;
    private bool _headDeclined;
    private float _repickTime;
    private float _scatterRollTime = -999f;
    private Vector2 _scatterDisc;

    public EAimTargetPart? ChosenPart
    {
        get { return _chosen; }
    }

    private static AimTargetSettings Settings
    {
        get { return SAINPlugin.LoadedPreset.GlobalSettings.Aiming.AimTarget; }
    }
    
    public Vector3? GetPointToShoot(bool allowRepick = true)
    {
        if (!Settings.Enabled)
        {
            return null;
        }

        float distance = Enemy.RealDistance;
        if (distance < Settings.CloseRangeTorsoDistance)
        {
            _chosen = null;
            _headDeclined = false;
            Vector3? torso = TorsoPoint();
            if (torso == null && Settings.HeadOnlyScatterRadius > 0f && OnlyHeadCanBeShot())
            {
                // Only the head is shootable up close: it stays the aim point, with the head-only scatter (see HeadScatter).
                _headDeclined = true;
                return Enemy.Vision.EnemyParts.Parts[EBodyPart.Head].ShootPoint;
            }
            return torso;
        }

        var parts = Enemy.Vision.EnemyParts;
        // A head pick that the head rules declined stops at once when more than the head becomes shootable (the bot then aims normally).
        bool headDeclinedOver = _headDeclined && !OnlyHeadCanBeShot();
        if (_chosen == null || _repickTime < Time.time || !CanShoot(_chosen.Value) || headDeclinedOver)
        {
            if (!allowRepick)
            {
                return null;
            }

            _chosen = Pick(distance);
            _headDeclined = false;
            if (_chosen == null && Settings.HeadOnlyScatterRadius > 0f && OnlyHeadCanBeShot())
            {
                // Only the head peeks out and the head rules (aim for head, chance, weights) did not pick it: the head stays the aim point, with the head-only scatter.
                _chosen = EAimTargetPart.Head;
                _headDeclined = true;
            }
            _repickTime = Time.time + Random.Range(Settings.RepickTimeMin, Settings.RepickTimeMax);
        }

        if (_chosen == null)
        {
            return null;
        }

        var part = parts.Parts[_chosen.Value.ToBodyPart()];
        Vector3? point = part.ShootPoint;
        if (point == null)
        {
            return null;
        }

        float spread = Settings.AimPointSpread;
        if (spread >= 1f)
        {
            return point;
        }

        return Vector3.Lerp(part.Transform.position, point.Value, spread);
    }

    private EAimTargetPart? Pick(float distance)
    {
        AimTargetWeights weights = BotWeights();
        float limbScale = LimbScale(distance);
        bool canHead = Bot.Info.FileSettings.Aiming.AimForHead && EFTMath.RandomBool(Bot.Info.FileSettings.Aiming.AimForHeadChance);

        float total = 0f;
        for (int i = 0; i < _parts.Length; i++)
        {
            EAimTargetPart part = _parts[i];
            float weight = 0f;

            if (CanShoot(part) && (canHead || part != EAimTargetPart.Head))
            {
                weight = weights.For(part);
                if (IsSmallTarget(part))
                {
                    weight *= limbScale;
                }
            }

            _weights[i] = weight;
            total += weight;
        }

        if (total <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, total);
        EAimTargetPart? last = null;
        for (int i = 0; i < _parts.Length; i++)
        {
            if (_weights[i] <= 0f)
            {
                continue;
            }
            last = _parts[i];
            roll -= _weights[i];
            if (roll <= 0f)
            {
                return _parts[i];
            }
        }
        return last;
    }

    private AimTargetWeights BotWeights()
    {
        var botSettings = Bot.Info.FileSettings.Aiming;
        return botSettings.OverrideAimTargetWeights ? botSettings.AimTargetWeights : Settings.Weights;
    }

    private static float LimbScale(float distance)
    {
        float start = Settings.LimbFalloffStart;
        float end = Settings.LimbFalloffEnd;
        if (distance <= start)
        {
            return 1f;
        }
        if (distance >= end || end <= start)
        {
            return 0f;
        }
        return 1f - ((distance - start) / (end - start));
    }

    private static bool IsSmallTarget(EAimTargetPart part)
    {
        return part != EAimTargetPart.Chest && part != EAimTargetPart.Stomach;
    }

    private bool CanShoot(EAimTargetPart part)
    {
        return Enemy.Vision.EnemyParts.Parts.TryGetValue(part.ToBodyPart(), out var data) && data.CanShoot;
    }

    private static readonly Dictionary<int, float> _aimLogTimes = [];

    /// <summary>Diagnostic: while this bot shoots at the player, one line (at most every 1.5 s) saying where it aims and why (see the AimAtPlayerLog setting).</summary>
    public void LogAim(SAIN.Components.BotComponent bot, Vector3 endPoint, Vector3 realPoint, Vector3 scatter)
    {
        if (!SAINPlugin.LoadedPreset.GlobalSettings.Fork.AimAtPlayerLog || Enemy.EnemyPlayer?.IsYourPlayer != true || bot.BotOwner.ShootData?.Shooting != true)
        {
            return;
        }
        int id = bot.BotOwner.Id;
        if (_aimLogTimes.TryGetValue(id, out float last) && Time.time - last < 1.5f)
        {
            return;
        }
        _aimLogTimes[id] = Time.time;
        float feet = Enemy.EnemyPlayer.Position.y;
        var weapon = bot.Info.WeaponInfo;
        Logger.LogInfoAlways(
            $"[AimLog] t={Time.time:0.0} {bot.name} [{bot.Info.Profile.WildSpawnType}] shoots at you: dist {Enemy.RealDistance:0} m, aim height {endPoint.y - feet:0.00} m above your feet "
                + $"(real target {realPoint.y - feet:0.00}), chosen part {(_chosen.HasValue ? _chosen.Value.ToString() : "none")}, head only {OnlyHeadCanBeShot()}, head declined {_headDeclined}, "
                + $"scatter {scatter.magnitude:0.00} m, optic {bot.PlayerComponent.Equipment.CurrentWeaponInfo?.HasOptic}, fire mode {weapon.SelectedFireMode}, your pose {Enemy.EnemyPlayer.Pose}"
        );
    }

    /// <summary>True while only the head can be shot and the head-only scatter is on: the vanilla "aim for head" roll must not give a clean head aim then.</summary>
    public bool HeadOnlyTarget
    {
        get { return Settings.HeadOnlyScatterRadius > 0f && OnlyHeadCanBeShot(); }
    }

    private bool OnlyHeadCanBeShot()
    {
        var parts = Enemy.Vision.EnemyParts.Parts;
        if (!parts.TryGetValue(EBodyPart.Head, out var head) || !head.CanShoot)
        {
            return false;
        }
        foreach (var pair in parts)
        {
            if (pair.Key != EBodyPart.Head && pair.Value.CanShoot)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Extra aim error while only the head is shootable and the head rules declined it: a random offset in a disc (meters) perpendicular to the line of sight,
    /// re-rolled every 0.3 s so each burst or tap is independent. Zero when the debuff does not apply. Mostly misses the head (it lands on the cover or past it).
    /// </summary>
    public Vector3 HeadScatter(Vector3 aimPoint, Vector3 origin)
    {
        float radius = Settings.HeadOnlyScatterRadius;
        if (!_headDeclined || radius <= 0f || !OnlyHeadCanBeShot())
        {
            return Vector3.zero;
        }
        if (Time.time - _scatterRollTime > 0.3f)
        {
            _scatterRollTime = Time.time;
            _scatterDisc = Random.insideUnitCircle * radius;
        }
        Vector3 direction = aimPoint - origin;
        if (direction.sqrMagnitude < 0.01f)
        {
            return Vector3.zero;
        }
        direction.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, direction);
        if (right.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }
        right.Normalize();
        Vector3 up = Vector3.Cross(direction, right);
        return right * _scatterDisc.x + up * _scatterDisc.y;
    }

    private Vector3? TorsoPoint()
    {
        var parts = Enemy.Vision.EnemyParts.Parts;
        return parts[EBodyPart.Chest].ShootPoint ?? parts[EBodyPart.Stomach].ShootPoint;
    }
}
