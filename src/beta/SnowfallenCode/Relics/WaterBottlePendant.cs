using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Relics;

public class WaterBottlePendant : SnowfallenRelics
{
    private int _lastKnownHp;
    private bool _combatStartEffectsPending;

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeCombatStart()
    {
        _lastKnownHp = Owner.Creature.CurrentHp;
        _combatStartEffectsPending = true;
        await CombatHpLossMechanics.Ensure(Owner);
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner || !_combatStartEffectsPending)
        {
            return;
        }

        _combatStartEffectsPending = false;

        if (!Owner.Creature.Powers.OfType<OmenPower>().Any())
        {
            await PowerCmd.Apply<OmenPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                null);
        }

        if (HasCompletedCombatBeforeCurrent())
        {
            await ResolveVictoryMaxHpGain();
        }

        await ApplySynchronizedCombatStartEffect(choiceContext);
        DogBiscuits.EnableHealingTrackingAfterStarterRelic(Owner);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner.Creature)
        {
            return;
        }

        int currentHp = creature.CurrentHp;
        decimal actualHealing = Math.Max(0, currentHp - _lastKnownHp);
        _lastKnownHp = currentHp;

        if (actualHealing <= 0m ||
            !CombatManager.Instance.IsInProgress ||
            WaterBreathGainSuppression.IsSuppressed(creature))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<WaterBreathPower>(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            actualHealing,
            Owner.Creature,
            null);
    }

    protected virtual Task ApplySynchronizedCombatStartEffect(
        PlayerChoiceContext choiceContext)
    {
        return WaterBreathMechanics.Gain(
            choiceContext,
            Owner.Creature,
            2m,
            Owner.Creature,
            null);
    }

    private bool HasCompletedCombatBeforeCurrent()
    {
        var currentEntry = Owner.RunState.CurrentMapPointHistoryEntry;
        return Owner.RunState.MapPointHistory
            .SelectMany(actHistory => actHistory)
            .Any(entry =>
                !ReferenceEquals(entry, currentEntry) &&
                (entry.HasRoomOfType(RoomType.Monster) ||
                 entry.HasRoomOfType(RoomType.Elite) ||
                 entry.HasRoomOfType(RoomType.Boss)));
    }

    private async Task ResolveVictoryMaxHpGain()
    {
        Creature creature = Owner.Creature;
        if (creature.IsDead || creature.MaxHp <= 0)
        {
            return;
        }

        // 常态或高血（非低血）：与明兆 OmenPower 的低血线（最大生命 40%）口径一致。
        decimal hpRatio = (decimal)creature.CurrentHp / creature.MaxHp;
        if (hpRatio > 0.4m)
        {
            Flash();
            await WaterBreathGainSuppression.Run(
                creature,
                () => CreatureCmd.GainMaxHp(creature, 1m));
        }
    }

}

