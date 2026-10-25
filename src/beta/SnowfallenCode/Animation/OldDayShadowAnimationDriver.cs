using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using SnowfallenMod.DongmingCode.Monsters;

namespace SnowfallenMod.SnowfallenCode.Animation;

/// <summary>
/// Replaces only the Old Day Shadow's looping idle visual.  Its vanilla
/// Necrobinder scene remains available for one-shot triggers (hit/death/etc.).
/// </summary>
internal sealed partial class OldDayShadowAnimationDriver : Node2D
{
    internal const string FramesPath =
        "res://Snowfallen/Animations/old_day_shadow_idle_frames.tres";
    private const string NodeName = "OldDayShadowAnimation";
    private const string AnimationName = "Idle";

    private static SpriteFrames _cachedFrames;
    private NCreature _creature;
    private AnimatedSprite2D _sprite;
    private Node2D _originalBody;
    private bool _restoreBody;
    private bool _isVisible;

    internal static bool IsOldDayShadow(NCreature creature)
    {
        if (creature?.Entity?.Monster is OldDayShadow)
        {
            return true;
        }

        // Creature models can be reconstructed from the save/network layer;
        // in that path the concrete CLR type may not survive until _Ready.
        // Keep the visual replacement keyed to the stable model id as well.
        string id = creature?.Entity?.Monster?.Id?.ToString();
        return id?.Contains("OLD_DAY_SHADOW", StringComparison.OrdinalIgnoreCase) == true;
    }

    internal static void TryPlayIdle(NCreature creature)
    {
        if (!IsOldDayShadow(creature) || creature.Entity.IsDead)
        {
            return;
        }

        OldDayShadowAnimationDriver driver =
            creature.GetNodeOrNull<OldDayShadowAnimationDriver>(NodeName);
        if (driver is null)
        {
            driver = new OldDayShadowAnimationDriver { Name = NodeName };
            creature.AddChild(driver);
            if (!driver.Initialize(creature))
            {
                driver.QueueFree();
                return;
            }
        }

        driver.PlayIdle();
    }

    internal static bool TryPlayDeath(NCreature creature)
    {
        if (!IsOldDayShadow(creature))
        {
            return false;
        }

        OldDayShadowAnimationDriver driver =
            creature.GetNodeOrNull<OldDayShadowAnimationDriver>(NodeName);
        if (driver is null)
        {
            driver = new OldDayShadowAnimationDriver { Name = NodeName };
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

    internal static void StopForOtherTrigger(NCreature creature)
    {
        if (creature?.Entity?.Monster is OldDayShadow)
        {
            creature.GetNodeOrNull<OldDayShadowAnimationDriver>(NodeName)?.Stop();
        }
    }

    private bool Initialize(NCreature creature)
    {
        _creature = creature;
        _originalBody = creature.Visuals?.GetCurrentBody();
        SpriteFrames frames = _cachedFrames ??= ResourceLoader.Load<SpriteFrames>(FramesPath);
        if (frames is null || !frames.HasAnimation(AnimationName))
        {
            return false;
        }

        Vector2 visualScale = creature.Visuals?.Scale ?? Vector2.One;
        float scaleY = Mathf.Abs(visualScale.Y) > 0.001f ? Mathf.Abs(visualScale.Y) : 1f;
        // Intent icons are rendered by a separate combat overlay. Use a
        // global layer immediately behind that overlay so the custom sprite
        // stays in front of the arena but never obscures the intent.
        ZAsRelative = true;
        ZIndex = (_originalBody?.ZIndex ?? 0) - 1;
        _sprite = new AnimatedSprite2D
        {
            SpriteFrames = frames,
            Animation = AnimationName,
            Position = new Vector2(0f, -125f),
            Scale = new Vector2(0.32f * scaleY, 0.32f * scaleY),
            FlipH = visualScale.X < 0f,
            Centered = true,
            Visible = false,
            ProcessMode = ProcessModeEnum.Always
        };
        Node parent = _originalBody?.GetParent() ?? this;
        parent.AddChild(_sprite);
        SetProcess(false);
        return true;
    }

    private void PlayIdle()
    {
        if (_sprite is null || _creature is null || _creature.Entity.IsDead)
        {
            return;
        }

        if (_originalBody is not null)
        {
            if (!_isVisible)
            {
                _restoreBody = _originalBody.Visible;
            }

            _originalBody.Visible = false;
        }

        _isVisible = true;
        _sprite.Visible = true;
        _sprite.Stop();
        _sprite.Animation = AnimationName;
        _sprite.SetFrameAndProgress(0, 0f);
        _sprite.Play(AnimationName, customSpeed: 1f, fromEnd: false);
    }

    private void PlayDeath()
    {
        if (_sprite is null || !_sprite.SpriteFrames.HasAnimation("Dead"))
        {
            return;
        }

        if (_originalBody is not null)
        {
            if (!_isVisible)
            {
                _restoreBody = _originalBody.Visible;
            }

            _originalBody.Visible = false;
        }

        _isVisible = true;
        _sprite.Visible = true;
        _sprite.Stop();
        _sprite.Animation = "Dead";
        _sprite.SetFrameAndProgress(0, 0f);
        _sprite.Play("Dead", customSpeed: 1f, fromEnd: false);
    }

    private void Stop()
    {
        if (!_isVisible)
        {
            return;
        }

        _sprite?.Stop();
        if (_sprite is not null)
        {
            _sprite.Visible = false;
        }

        if (_originalBody is not null)
        {
            _originalBody.Visible = _restoreBody;
        }

        _isVisible = false;
    }

    public override void _ExitTree()
    {
        Stop();
        if (GodotObject.IsInstanceValid(_sprite))
        {
            _sprite.QueueFree();
        }

        base._ExitTree();
    }
}

