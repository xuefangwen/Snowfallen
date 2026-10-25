using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SnowfallenMod.DongmingCode.Character;

namespace SnowfallenMod.SnowfallenCode.Animation;

/// <summary>
/// Visual-only attack, cast, hit, idle, and death replacement for Dongming. The base
/// Necrobinder scene is left intact for every other trigger and character.
/// </summary>
internal sealed partial class DongmingAttackAnimationDriver : Node2D
{
    internal const string FramesPath =
        "res://Snowfallen/Animations/dongming_attack_frames.tres";
    private const string CastFramePathFormat =
        "res://Snowfallen/Images/Character/Animation/dongming_cast_v1/dongming_cast_{0:000}.png";
    internal const string IdleFramesPath =
        "res://Snowfallen/Animations/dongming_idle_frames.tres";
    internal const string HitFramesPath =
        "res://Snowfallen/Animations/dongming_hit_frames.tres";
    internal const string DeathFramesPath =
        "res://Snowfallen/Animations/dongming_death_frames.tres";
    private const string SpriteNodeName = "DongmingAttackVisual";
    private const float IdlePlaybackSpeed = 0.25f;
    private const float DeathDurationSeconds = 49f / 30f;
    private static readonly StringName AttackAnimation = "Attack";
    private static readonly StringName CastAnimation = "Cast";
    private static readonly StringName IdleAnimation = "Idle";
    private static readonly StringName HitAnimation = "Hit";
    private static readonly StringName DeathAnimation = "Dead";
    private static SpriteFrames _cachedFrames;
    private static SpriteFrames _cachedCastFrames;
    private static SpriteFrames _cachedIdleFrames;
    private static SpriteFrames _cachedHitFrames;
    private static SpriteFrames _cachedDeathFrames;

    private NCreature _creature;
    private AnimatedSprite2D _sprite;
    private AnimatedSprite2D _castSprite;
    private AnimatedSprite2D _idleSprite;
    private AnimatedSprite2D _hitSprite;
    private AnimatedSprite2D _deathSprite;
    private CanvasItem _originalVisuals;
    private Node2D _originalBody;
    private bool _restoreOriginalVisibility;
    private bool _restoreOriginalBodyVisibility;
    private bool _isPlaying;
    private bool _isCastVisible;
    private bool _isIdleVisible;
    private bool _isHitVisible;
    private bool _isDeathVisible;
    private ulong _deathStartedAtMilliseconds;

    internal static bool TryPlay(NCreature creature)
    {
        if (creature?.Entity?.Player?.Character is not Dongming)
        {
            return false;
        }

        DongmingAttackAnimationDriver driver =
            creature.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName);
        if (driver is null)
        {
            driver = new DongmingAttackAnimationDriver
            {
                Name = SpriteNodeName
            };
            creature.AddChild(driver);
            if (!driver.Initialize(creature))
            {
                driver.QueueFree();
                return false;
            }
        }

        driver.PlayAttack();
        return true;
    }

    internal static bool TryPlayCast(NCreature creature)
    {
        if (creature?.Entity?.Player?.Character is not Dongming)
        {
            return false;
        }

        DongmingAttackAnimationDriver driver =
            creature.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName);
        if (driver is null)
        {
            driver = new DongmingAttackAnimationDriver { Name = SpriteNodeName };
            creature.AddChild(driver);
            if (!driver.Initialize(creature))
            {
                driver.QueueFree();
                return false;
            }
        }

        return driver.PlayCast();
    }

    internal static bool TryPlayIdle(NCreature creature)
    {
        if (creature?.Entity?.Player?.Character is not Dongming ||
            creature.Entity.IsDead)
        {
            return false;
        }

        DongmingAttackAnimationDriver driver =
            creature.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName);
        if (driver is null)
        {
            driver = new DongmingAttackAnimationDriver
            {
                Name = SpriteNodeName
            };
            creature.AddChild(driver);
            if (!driver.Initialize(creature))
            {
                driver.QueueFree();
                return false;
            }
        }

        driver.PlayIdle();
        return true;
    }

    internal static bool TryPlayDeath(NCreature creature)
    {
        if (creature?.Entity?.Player?.Character is not Dongming)
        {
            return false;
        }

        DongmingAttackAnimationDriver driver =
            creature.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName);
        if (driver is null)
        {
            driver = new DongmingAttackAnimationDriver
            {
                Name = SpriteNodeName
            };
            creature.AddChild(driver);
            if (!driver.Initialize(creature))
            {
                driver.QueueFree();
                return false;
            }
        }

        driver.PlayDeath();
        return true;
    }

    internal static bool TryPlayHit(NCreature creature)
    {
        if (creature?.Entity?.Player?.Character is not Dongming ||
            creature.Entity.IsDead)
        {
            return false;
        }

        DongmingAttackAnimationDriver driver =
            creature.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName);
        if (driver is null)
        {
            driver = new DongmingAttackAnimationDriver
            {
                Name = SpriteNodeName
            };
            creature.AddChild(driver);
            if (!driver.Initialize(creature))
            {
                driver.QueueFree();
                return false;
            }
        }

        driver.PlayHit();
        return true;
    }

    internal static bool TryGetDeathDuration(
        NCreature creature,
        out float duration)
    {
        DongmingAttackAnimationDriver driver =
            creature?.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName);
        if (driver?._isDeathVisible != true)
        {
            duration = 0f;
            return false;
        }

        duration = DeathDurationSeconds;
        return true;
    }

    internal static bool TryGetDeathTimeRemaining(
        NCreature creature,
        out float remaining)
    {
        DongmingAttackAnimationDriver driver =
            creature?.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName);
        if (driver?._isDeathVisible != true)
        {
            remaining = 0f;
            return false;
        }

        float elapsed = (Time.GetTicksMsec() - driver._deathStartedAtMilliseconds) / 1000f;
        remaining = Mathf.Max(0f, DeathDurationSeconds - elapsed);
        return true;
    }

    internal static void StopForOtherTrigger(NCreature creature)
    {
        if (creature?.Entity?.Player?.Character is not Dongming)
        {
            return;
        }

        creature.GetNodeOrNull<DongmingAttackAnimationDriver>(SpriteNodeName)?
            .StopAttack(forceDeathStop: creature.Entity?.IsAlive == true);
    }

    private bool Initialize(NCreature creature)
    {
        _creature = creature;
        _originalVisuals = creature.Visuals;
        _originalBody = creature.Visuals.GetCurrentBody();
        // Match the placeholder visual's layer exactly. The driver is a
        // sibling visual node; adding +1 here promoted Dongming above the
        // room's intended creature/effect ordering.
        ZIndex = creature.Visuals.ZIndex;
        ZAsRelative = creature.Visuals.ZAsRelative;
        SpriteFrames frames = GetFrames();
        SpriteFrames castFrames = GetCastFrames();
        SpriteFrames idleFrames = GetIdleFrames();
        SpriteFrames hitFrames = GetHitFrames();
        SpriteFrames deathFrames = GetDeathFrames();
        if (frames is null || !frames.HasAnimation(AttackAnimation))
        {
            Log.Error($"[Dongming.Animation] Missing attack SpriteFrames at {FramesPath}");
            return false;
        }

        bool hasCastFrames = castFrames is not null && castFrames.HasAnimation(CastAnimation);
        if (!hasCastFrames)
        {
            // The cast .tres is kept as an editor source, but the game's
            // BaseLib export path does not accept that raw text resource.
            // Runtime construction keeps the cast animation independent of
            // the exported visual scene and avoids touching vanilla state.
            Log.Warn("[Dongming.Animation] Cast SpriteFrames could not be constructed; using vanilla Cast.");
        }

        if (idleFrames is null || !idleFrames.HasAnimation(IdleAnimation))
        {
            Log.Error($"[Dongming.Animation] Missing idle SpriteFrames at {IdleFramesPath}");
            return false;
        }

        if (hitFrames is null || !hitFrames.HasAnimation(HitAnimation))
        {
            Log.Error($"[Dongming.Animation] Missing hit SpriteFrames at {HitFramesPath}");
            return false;
        }

        if (deathFrames is null || !deathFrames.HasAnimation(DeathAnimation))
        {
            Log.Error($"[Dongming.Animation] Missing death SpriteFrames at {DeathFramesPath}");
            return false;
        }

        _sprite = new AnimatedSprite2D
        {
            SpriteFrames = frames,
            Animation = AttackAnimation,
            Position = new Vector2(0f, -125f),
            Scale = new Vector2(0.32f, 0.32f),
            FlipH = false,
            Centered = true,
            Visible = false,
            ZIndex = 0,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(_sprite);
        _sprite.AnimationFinished += OnAttackAnimationFinished;
        if (hasCastFrames)
        {
            _castSprite = new AnimatedSprite2D
            {
                SpriteFrames = castFrames,
                Animation = CastAnimation,
                Position = _sprite.Position,
                Scale = _sprite.Scale,
                FlipH = _sprite.FlipH,
                Centered = true,
                Visible = false,
                ZIndex = 0,
                ProcessMode = ProcessModeEnum.Always
            };
            AddChild(_castSprite);
            _castSprite.AnimationFinished += OnCastAnimationFinished;
        }
        _idleSprite = new AnimatedSprite2D
        {
            SpriteFrames = idleFrames,
            Animation = IdleAnimation,
            Position = _sprite.Position,
            Scale = _sprite.Scale,
            FlipH = _sprite.FlipH,
            Centered = true,
            Visible = false,
            ZIndex = 0,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(_idleSprite);
        _hitSprite = new AnimatedSprite2D
        {
            SpriteFrames = hitFrames,
            Animation = HitAnimation,
            Position = _sprite.Position,
            Scale = _sprite.Scale,
            FlipH = _sprite.FlipH,
            Centered = true,
            Visible = false,
            ZIndex = 0,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(_hitSprite);
        _hitSprite.AnimationFinished += OnHitAnimationFinished;
        _deathSprite = new AnimatedSprite2D
        {
            SpriteFrames = deathFrames,
            Animation = DeathAnimation,
            Position = _sprite.Position,
            Scale = _sprite.Scale,
            FlipH = _sprite.FlipH,
            Centered = true,
            Visible = false,
            ZIndex = 0,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(_deathSprite);
        // Stay a sibling of the hidden vanilla visual, but inherit its live
        // transform on our shared container so every animation and foot anchor
        // shrinks together. Surrounded flips Body, not Visuals.
        ProcessMode = ProcessModeEnum.Always;
        SyncVisualTransform();
        SetProcess(true);
        return true;
    }

    internal static Transform2D ComposeVisualTransform(Transform2D visuals, float bodyScaleX) =>
        new(visuals.X * (bodyScaleX < 0f ? -1f : 1f), visuals.Y, visuals.Origin);

    private void SyncVisualTransform()
    {
        if (!GodotObject.IsInstanceValid(_creature) ||
            !GodotObject.IsInstanceValid(_creature.Visuals))
        {
            return;
        }

        float bodyScaleX = GodotObject.IsInstanceValid(_originalBody)
            ? _originalBody.Scale.X
            : 1f;
        Transform = ComposeVisualTransform(_creature.Visuals.Transform, bodyScaleX);
    }

    public override void _Process(double delta) => SyncVisualTransform();

    private void PlayAttack()
    {
        if (_sprite is null || _creature is null || _isDeathVisible)
        {
            return;
        }

        if (!_isPlaying && !_isIdleVisible && _originalVisuals is not null)
        {
            _restoreOriginalVisibility = _originalVisuals.Visible;
            _restoreOriginalBodyVisibility = _originalBody?.Visible ?? false;
        }

        SetVanillaVisualsVisible(false);

        _isPlaying = true;
        _isCastVisible = false;
        _isIdleVisible = false;
        _isHitVisible = false;
        _idleSprite?.Stop();
        _hitSprite?.Stop();
        _castSprite?.Stop();
        if (_idleSprite is not null)
        {
            _idleSprite.Visible = false;
        }
        if (_hitSprite is not null)
        {
            _hitSprite.Visible = false;
        }
        if (_castSprite is not null)
        {
            _castSprite.Visible = false;
        }
        _sprite.Visible = true;
        _sprite.Stop();
        _sprite.Animation = AttackAnimation;
        _sprite.SetFrameAndProgress(0, 0f);
        _sprite.Play(AttackAnimation, customSpeed: 1f, fromEnd: false);
        Log.Info("[Dongming.Animation] Playing Dongming attack animation.");
    }

    private bool PlayCast()
    {
        if (_castSprite is null || _creature is null || _isDeathVisible)
        {
            return false;
        }

        if (!_isPlaying && !_isIdleVisible && !_isHitVisible && !_isCastVisible && _originalVisuals is not null)
        {
            _restoreOriginalVisibility = _originalVisuals.Visible;
            _restoreOriginalBodyVisibility = _originalBody?.Visible ?? false;
        }

        SetVanillaVisualsVisible(false);

        _isPlaying = false;
        _isCastVisible = true;
        _isIdleVisible = false;
        _isHitVisible = false;
        _sprite?.Stop();
        _idleSprite?.Stop();
        _hitSprite?.Stop();
        _sprite!.Visible = false;
        _idleSprite!.Visible = false;
        _hitSprite!.Visible = false;
        _castSprite.Visible = true;
        _castSprite.Stop();
        _castSprite.Animation = CastAnimation;
        _castSprite.SetFrameAndProgress(0, 0f);
        _castSprite.Play(CastAnimation, customSpeed: 1f, fromEnd: false);
        Log.Info("[Dongming.Animation] Playing Dongming cast animation.");
        return true;
    }

    private void PlayIdle()
    {
        if (_idleSprite is null || _creature is null || _isDeathVisible)
        {
            return;
        }

        if (!_isPlaying && !_isIdleVisible && !_isHitVisible && !_isCastVisible && _originalVisuals is not null)
        {
            _restoreOriginalVisibility = _originalVisuals.Visible;
            _restoreOriginalBodyVisibility = _originalBody?.Visible ?? false;
        }

        SetVanillaVisualsVisible(false);

        _isPlaying = false;
        _isCastVisible = false;
        _isIdleVisible = true;
        _isHitVisible = false;
        _sprite?.Stop();
        _castSprite?.Stop();
        _hitSprite?.Stop();
        if (_sprite is not null)
        {
            _sprite.Visible = false;
        }
        if (_castSprite is not null)
        {
            _castSprite.Visible = false;
        }
        if (_hitSprite is not null)
        {
            _hitSprite.Visible = false;
        }

        _idleSprite.Visible = true;
        _idleSprite.Stop();
        _idleSprite.Animation = IdleAnimation;
        _idleSprite.SetFrameAndProgress(0, 0f);
        _idleSprite.Play(IdleAnimation, customSpeed: IdlePlaybackSpeed, fromEnd: false);
        Log.Info("[Dongming.Animation] Playing Dongming idle animation.");
    }

    private void PlayHit()
    {
        if (_hitSprite is null || _creature is null || _isDeathVisible)
        {
            return;
        }

        if (!_isPlaying && !_isIdleVisible && !_isHitVisible && _originalVisuals is not null)
        {
            _restoreOriginalVisibility = _originalVisuals.Visible;
            _restoreOriginalBodyVisibility = _originalBody?.Visible ?? false;
        }

        SetVanillaVisualsVisible(false);

        _isPlaying = false;
        _isCastVisible = false;
        _isIdleVisible = false;
        _isHitVisible = true;
        _sprite?.Stop();
        _castSprite?.Stop();
        _idleSprite?.Stop();
        if (_sprite is not null)
        {
            _sprite.Visible = false;
        }
        if (_castSprite is not null)
        {
            _castSprite.Visible = false;
        }
        if (_idleSprite is not null)
        {
            _idleSprite.Visible = false;
        }

        _hitSprite.Visible = true;
        _hitSprite.Stop();
        _hitSprite.Animation = HitAnimation;
        _hitSprite.SetFrameAndProgress(0, 0f);
        _hitSprite.Play(HitAnimation, customSpeed: 1f, fromEnd: false);
        Log.Info("[Dongming.Animation] Playing Dongming hit animation (59 frames at 60 FPS).");
    }

    private void PlayDeath()
    {
        if (_deathSprite is null || _creature is null)
        {
            return;
        }

        if (!_isPlaying && !_isIdleVisible && !_isHitVisible && !_isCastVisible && !_isDeathVisible &&
            _originalVisuals is not null)
        {
            _restoreOriginalVisibility = _originalVisuals.Visible;
        }

        if (_originalVisuals is not null)
        {
            SetVanillaVisualsVisible(false);
        }

        _isPlaying = false;
        _isCastVisible = false;
        _isIdleVisible = false;
        _isHitVisible = false;
        _isDeathVisible = true;
        _sprite?.Stop();
        _castSprite?.Stop();
        _idleSprite?.Stop();
        _hitSprite?.Stop();
        if (_sprite is not null)
        {
            _sprite.Visible = false;
        }
        if (_castSprite is not null)
        {
            _castSprite.Visible = false;
        }
        if (_idleSprite is not null)
        {
            _idleSprite.Visible = false;
        }
        if (_hitSprite is not null)
        {
            _hitSprite.Visible = false;
        }

        _deathSprite.Visible = true;
        _deathSprite.Stop();
        _deathSprite.Animation = DeathAnimation;
        _deathSprite.SetFrameAndProgress(0, 0f);
        _deathStartedAtMilliseconds = Time.GetTicksMsec();
        _deathSprite.Play(DeathAnimation, customSpeed: 1f, fromEnd: false);
        Log.Info("[Dongming.Animation] Playing Dongming death animation (49 frames at 30 FPS).");
    }

    private void OnAttackAnimationFinished()
    {
        if (!_isPlaying)
        {
            return;
        }

        FinishOneShotAnimation();
    }

    private void OnCastAnimationFinished()
    {
        if (!_isCastVisible)
        {
            return;
        }

        FinishOneShotAnimation();
    }

    private void OnHitAnimationFinished()
    {
        if (!_isHitVisible)
        {
            return;
        }

        FinishOneShotAnimation();
    }

    private void FinishOneShotAnimation()
    {
        if (_creature is null)
        {
            return;
        }

        // Stay inside the Mod-owned visual driver so there is no one-frame
        // flash of the placeholder Necrobinder between Attack and Idle.
        if (GodotObject.IsInstanceValid(_creature) && _creature.Entity?.IsAlive == true)
        {
            PlayIdle();
            return;
        }

        StopAttack();
    }

    private void StopAttack(bool forceDeathStop = false)
    {
        if (_isDeathVisible && !forceDeathStop)
        {
            return;
        }

        if (!_isPlaying && !_isIdleVisible && !_isHitVisible && !_isCastVisible && !_isDeathVisible)
        {
            return;
        }

        _isPlaying = false;
        _isCastVisible = false;
        _isIdleVisible = false;
        _isHitVisible = false;
        _isDeathVisible = false;
        _sprite?.Stop();
        _castSprite?.Stop();
        if (_sprite is not null)
        {
            _sprite.Visible = false;
        }
        if (_castSprite is not null)
        {
            _castSprite.Visible = false;
        }

        _idleSprite?.Stop();
        if (_idleSprite is not null)
        {
            _idleSprite.Visible = false;
        }

        _hitSprite?.Stop();
        if (_hitSprite is not null)
        {
            _hitSprite.Visible = false;
        }

        _deathSprite?.Stop();
        if (_deathSprite is not null)
        {
            _deathSprite.Visible = false;
        }

        RestoreVanillaVisualsVisibility();
    }

    private void SetVanillaVisualsVisible(bool visible)
    {
        if (_originalVisuals is not null)
        {
            _originalVisuals.Visible = visible;
        }

        if (_originalBody is not null)
        {
            _originalBody.Visible = visible;
        }
    }

    private void RestoreVanillaVisualsVisibility()
    {
        if (_originalVisuals is not null)
        {
            _originalVisuals.Visible = _restoreOriginalVisibility;
        }

        if (_originalBody is not null)
        {
            _originalBody.Visible = _restoreOriginalBodyVisibility;
        }
    }

    private static SpriteFrames GetFrames()
    {
        return _cachedFrames ??= ResourceLoader.Load<SpriteFrames>(FramesPath);
    }

    private static SpriteFrames GetCastFrames()
    {
        if (_cachedCastFrames is not null)
        {
            return _cachedCastFrames;
        }

        SpriteFrames frames = new();
        frames.AddAnimation(CastAnimation);
        frames.SetAnimationLoop(CastAnimation, false);
        frames.SetAnimationSpeed(CastAnimation, 30d);
        for (int index = 1; index <= 33; index++)
        {
            string path = string.Format(CastFramePathFormat, index);
            Texture2D texture = ResourceLoader.Load<Texture2D>(path);
            if (texture is null)
            {
                Log.Error($"[Dongming.Animation] Missing cast texture at {path}");
                return null;
            }

            frames.AddFrame(CastAnimation, texture, 1f);
        }

        _cachedCastFrames = frames;
        Log.Info("[Dongming.Animation] Constructed Dongming cast animation from 33 runtime textures.");
        return _cachedCastFrames;
    }

    private static SpriteFrames GetIdleFrames()
    {
        return _cachedIdleFrames ??= ResourceLoader.Load<SpriteFrames>(IdleFramesPath);
    }

    private static SpriteFrames GetHitFrames()
    {
        return _cachedHitFrames ??= ResourceLoader.Load<SpriteFrames>(HitFramesPath);
    }

    private static SpriteFrames GetDeathFrames()
    {
        return _cachedDeathFrames ??= ResourceLoader.Load<SpriteFrames>(DeathFramesPath);
    }

    public override void _ExitTree()
    {
        StopAttack(forceDeathStop: true);

        if (_sprite is not null)
        {
            _sprite.AnimationFinished -= OnAttackAnimationFinished;
        }

        if (_castSprite is not null)
        {
            _castSprite.AnimationFinished -= OnCastAnimationFinished;
        }

        if (_hitSprite is not null)
        {
            _hitSprite.AnimationFinished -= OnHitAnimationFinished;
        }

        base._ExitTree();
    }
}
