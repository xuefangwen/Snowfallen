using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Logging;

namespace SnowfallenMod.SnowfallenCode.Animation;

/// <summary>
/// PNG-sequence counterpart of the base game's Ironclad CreatureAnimator graph.
/// Ironclad uses Spine tracks; Snowfallen keeps the same state/trigger structure
/// while playing matching AnimatedSprite2D animations.
/// </summary>
internal sealed class SnowfallenSpriteAnimator : IDisposable
{
    private const double MinimumHitReplayIntervalSeconds = 4.0 / 30.0;

    private static readonly StringName IdleAnimation = "Idle";
    private static readonly StringName AttackAnimation = "Attack";
    private static readonly StringName CastAnimation = "Cast";
    private static readonly StringName HitAnimation = "Hit";
    private static readonly StringName DeadAnimation = "Dead";

    private readonly AnimatedSprite2D _sprite;
    private readonly AnimState _anyState = new("anyState");
    private readonly AnimState _hurtState;
    private AnimState _currentState;
    private double _remainingStateTime;
    private double _hitReplayCooldown;
    private int _pendingHitReplays;
    private int _stateVersion;
    private bool _disposed;

    public SnowfallenSpriteAnimator(AnimatedSprite2D sprite)
    {
        _sprite = sprite;

        // This graph mirrors Ironclad.GenerateAnimator from the base game:
        // cast/attack/hurt return to idle; death is terminal.
        AnimState idle = new("idle_loop", isLooping: true);
        AnimState cast = new("cast") { NextState = idle };
        AnimState attack = new("attack") { NextState = idle };
        _hurtState = new AnimState("hurt") { NextState = idle };
        AnimState dead = new("die");

        _anyState.AddBranch("Idle", idle);
        _anyState.AddBranch("Dead", dead);
        _anyState.AddBranch("Hit", _hurtState);
        _anyState.AddBranch("Attack", attack);
        _anyState.AddBranch("Cast", cast);
        _anyState.AddBranch("PowerUp", cast);

        // Ironclad-specific triggers are mapped to Snowfallen's closest states so
        // base-game commands cannot strand the sprite on an unsupported state.
        _anyState.AddBranch("heavyAttack", attack);
        _anyState.AddBranch("Relaxed", idle);

        _currentState = idle;
        _sprite.AnimationFinished += OnAnimationCompleted;
        SetNextState(idle);
    }

    public void SetTrigger(string trigger)
    {
        if (_disposed)
        {
            return;
        }

        trigger = NormalizeTrigger(trigger);
        if (string.Equals(trigger, "Hit", StringComparison.Ordinal))
        {
            TriggerHitReplay();
            return;
        }

        // A non-hit state change supersedes any delayed reactions that have not
        // yet become visible (most importantly Dead).
        _pendingHitReplays = 0;
        _hitReplayCooldown = 0.0;

        AnimState nextState = _anyState.CallTrigger(trigger) ?? _currentState.CallTrigger(trigger);
        if (nextState is not null)
        {
            SetNextState(nextState);
        }
    }

    private static string NormalizeTrigger(string trigger)
    {
        return trigger?.Trim().ToLowerInvariant() switch
        {
            "dead" or "death" or "die" => "Dead",
            "hit" or "hurt" or "damage" => "Hit",
            "attack" or "heavyattack" => "Attack",
            "cast" or "powerup" => "Cast",
            "idle" or "relaxed" => "Idle",
            _ => trigger
        };
    }

    public void Update(double delta)
    {
        if (_disposed)
        {
            return;
        }

        _hitReplayCooldown = Math.Max(0.0, _hitReplayCooldown - delta);
        if (_pendingHitReplays > 0 && _hitReplayCooldown <= 0.0)
        {
            _pendingHitReplays--;
            RestartHitAnimation();
            Log.Info($"[Snowfallen.Animation] Replayed queued Hit; pending={_pendingHitReplays}");
            return;
        }

        if (_currentState.IsLooping)
        {
            EnsureLoopingStateIsPlaying();
            return;
        }

        if (_currentState.NextState is null)
        {
            return;
        }

        _remainingStateTime -= delta;
        if (_remainingStateTime <= 0.0)
        {
            AnimState completedState = _currentState;
            Log.Info($"[Snowfallen.Animation] Timed transition {completedState.Id} -> {completedState.NextState.Id}");
            SetNextState(completedState.NextState);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stateVersion++;
        _sprite.AnimationFinished -= OnAnimationCompleted;
    }

    private void SetNextState(AnimState state)
    {
        if (!TryGetSpriteAnimation(state, out StringName animation) ||
            !_sprite.SpriteFrames.HasAnimation(animation))
        {
            Log.Warn($"[Snowfallen] Missing sprite animation for state '{state.Id}'.");
            return;
        }

        _stateVersion++;
        _currentState = state;
        _remainingStateTime = GetAnimationDuration(animation);

        _sprite.Stop();
        _sprite.Animation = animation;
        _sprite.SetFrameAndProgress(0, 0f);
        _sprite.SpeedScale = 1f;
        _sprite.Play(animation, customSpeed: 1f, fromEnd: false);
        Log.Info($"[Snowfallen.Animation] Enter {state.Id} as {animation}; duration={_remainingStateTime:0.000}s");
    }

    private void TriggerHitReplay()
    {
        // CreatureCmd can emit every hit of a multi-attack in the same Godot
        // render frame. Restarting immediately for each call would make all but
        // the final restart invisible. Preserve those additional triggers and
        // replay them at a short, visible interval instead.
        if (ReferenceEquals(_currentState, _hurtState) && _hitReplayCooldown > 0.0)
        {
            _pendingHitReplays++;
            Log.Info($"[Snowfallen.Animation] Queued overlapping Hit; pending={_pendingHitReplays}");
            return;
        }

        RestartHitAnimation();
    }

    private void RestartHitAnimation()
    {
        SetNextState(_hurtState);
        _hitReplayCooldown = MinimumHitReplayIntervalSeconds;
    }

    private void OnAnimationCompleted()
    {
        AnimState completedState = _currentState;
        AnimState nextState = completedState.NextState;
        if (_disposed || nextState is null)
        {
            return;
        }

        // AnimatedSprite2D can still report IsPlaying() while sitting on the
        // final frame. Use the state generation, not IsPlaying(), to validate
        // the deferred transition. This is the PNG equivalent of Spine queuing
        // Ironclad's NextState before the one-shot animation begins.
        int completedVersion = _stateVersion;
        Callable.From(() => TryAdvanceToNextState(completedState, nextState, completedVersion)).CallDeferred();
    }

    private void EnsureLoopingStateIsPlaying()
    {
        if (!TryGetSpriteAnimation(_currentState, out StringName animation))
        {
            return;
        }

        bool needsRestart = _sprite.Animation != animation ||
                            Math.Abs(_sprite.GetPlayingSpeed()) <= 0.001;
        if (!needsRestart)
        {
            return;
        }

        _sprite.SpeedScale = 1f;
        _sprite.Play(animation, customSpeed: 1f, fromEnd: false);
        Log.Warn($"[Snowfallen.Animation] Restarted stalled looping state '{_currentState.Id}'.");
    }

    private void TryAdvanceToNextState(AnimState completedState, AnimState nextState, int completedVersion)
    {
        if (_disposed ||
            completedVersion != _stateVersion ||
            !ReferenceEquals(_currentState, completedState))
        {
            return;
        }

        SetNextState(nextState);
    }

    private double GetAnimationDuration(StringName animation)
    {
        SpriteFrames frames = _sprite.SpriteFrames;
        double framesPerSecond = frames.GetAnimationSpeed(animation);
        if (framesPerSecond <= 0.0)
        {
            return 0.0;
        }

        double duration = 0.0;
        int frameCount = frames.GetFrameCount(animation);
        for (int frame = 0; frame < frameCount; frame++)
        {
            duration += frames.GetFrameDuration(animation, frame) / framesPerSecond;
        }

        return duration;
    }

    private static bool TryGetSpriteAnimation(AnimState state, out StringName animation)
    {
        animation = state.Id switch
        {
            "idle_loop" => IdleAnimation,
            "attack" => AttackAnimation,
            "cast" => CastAnimation,
            "hurt" => HitAnimation,
            "die" => DeadAnimation,
            _ => default
        };

        return animation != default;
    }
}

