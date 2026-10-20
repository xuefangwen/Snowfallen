#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Monsters;

public sealed class ShengZhangUnit : MonsterModel
{
    private MoveState? _attackState;
    private MoveState? _defendState;

    [SavedProperty]
    public int ConfiguredMaxHp { get; private set; } = 50;

    [SavedProperty]
    public int BaseAttack { get; private set; } = 6;

    [SavedProperty]
    public int BaseBlock { get; private set; } = 6;

    [SavedProperty]
    public ShengZhangBlockTarget BlockTarget { get; private set; } = ShengZhangBlockTarget.Self;

    public override int MinInitialHp => ConfiguredMaxHp;

    public override int MaxInitialHp => ConfiguredMaxHp;

    public override bool ShouldDisappearFromDoom => false;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/chomper");

    public override LocString Title => new("monsters", "SHENG_ZHANG_UNIT.name");

    public void Configure(
        int maxHp,
        int attack,
        int block,
        ShengZhangBlockTarget blockTarget)
    {
        AssertMutable();
        ConfiguredMaxHp = maxHp;
        BaseAttack = attack;
        BaseBlock = block;
        BlockTarget = blockTarget;
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _attackState = new MoveState(
            "SHENG_ZHANG_ATTACK",
            AttackMove,
            new SingleAttackIntent(() => BaseAttack));
        _defendState = new MoveState(
            "SHENG_ZHANG_DEFEND",
            DefendMove,
            new DefendIntent());
        _attackState.FollowUpState = _defendState;
        _defendState.FollowUpState = _attackState;
        return new MonsterMoveStateMachine([_attackState, _defendState], _attackState);
    }

    public void SetInitialIntent()
    {
        if (_attackState is not null)
        {
            SetMoveImmediate(_attackState, forceTransition: true);
        }
    }

    public void AdvanceIntent()
    {
        if (_attackState is null || _defendState is null)
        {
            return;
        }

        MoveState next = NextMove.Id == _attackState.Id ? _defendState : _attackState;
        SetMoveImmediate(next, forceTransition: true);
    }

    public async Task ExecuteNextMove(IReadOnlyList<Creature> targets)
    {
        if (Creature.IsDead)
        {
            return;
        }

        MoveState move = NextMove;
        await move.PerformMove(targets);
        MoveStateMachine?.OnMovePerformed(move);
    }

    private async Task AttackMove(IReadOnlyList<Creature> _)
    {
        if (Creature.IsDead || Creature.CombatState is null)
        {
            return;
        }

        IReadOnlyList<Creature> enemies = Creature.CombatState.HittableEnemies;
        if (enemies.Count == 0)
        {
            return;
        }

        Creature? target = Creature.PetOwner!.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target is null)
        {
            return;
        }

        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.3f);
        await PowerCmd.Apply<VulnerablePower>(
            new BlockingPlayerChoiceContext(),
            target,
            1m,
            Creature,
            null);
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            target,
            BaseAttack,
            ValueProp.Move,
            null,
            null);
    }

    private async Task DefendMove(IReadOnlyList<Creature> _)
    {
        Creature? team = Creature.PetOwner?.Creature;
        if (team is null || team.IsDead)
        {
            return;
        }

        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.3f);
        await ShengZhangMechanics.GainBlockFromUnit(Creature.PetOwner!, this);
        ShengZhangMechanics.QueueTeamPlating(this);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("idle_loop", isLooping: true);
        AnimState attack = new("attack") { NextState = idle };
        AnimState cast = new("cast") { NextState = idle };
        AnimState hurt = new("hurt") { NextState = idle };
        AnimState death = new("die");
        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Dead", death);
        return animator;
    }
}

