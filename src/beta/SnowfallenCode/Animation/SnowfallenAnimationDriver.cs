using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace SnowfallenMod.SnowfallenCode.Animation;

/// <summary>
/// Runtime-installed animation driver. It is intentionally not referenced as a
/// C# script by the packed scene: BaseLib first converts the scene root to
/// NCreatureVisuals, then the Harmony patch creates this node from the already
/// loaded Snowfallen assembly.
/// </summary>
internal partial class SnowfallenAnimationDriver : Node
{
    private SnowfallenSpriteAnimator _animator;

    public void Initialize(AnimatedSprite2D sprite)
    {
        _animator?.Dispose();
        _animator = new SnowfallenSpriteAnimator(sprite);
        SetProcess(true);
        Log.Info("[Snowfallen.Animation] Runtime driver installed.");
    }

    public void SetTrigger(string trigger)
    {
        _animator?.SetTrigger(trigger);
    }

    public override void _Process(double delta)
    {
        _animator?.Update(delta);
    }

    public override void _ExitTree()
    {
        SetProcess(false);
        _animator?.Dispose();
        _animator = null;
        base._ExitTree();
    }
}

