using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class WovenHeartPowerBase : SnowfallenPower
{
    private int _embersTowardTrigger;
    private int _triggersThisTurn;

    protected abstract int TriggerLimit { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card is not IEmberCard || cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }

        _embersTowardTrigger++;
        if (_embersTowardTrigger < 2)
        {
            return;
        }

        _embersTowardTrigger = 0;
        if (_triggersThisTurn >= TriggerLimit)
        {
            return;
        }

        _triggersThisTurn++;
        Flash();
        if (CombatState.HittableEnemies.Count > 0)
        {
            Creature enemy = Owner.Player.RunState.Rng.CombatTargets
                .NextItem(CombatState.HittableEnemies);
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                enemy,
                -Amount,
                Owner,
                cardPlay.Card);
        }

    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner))
        {
            _triggersThisTurn = 0;
        }

        return Task.CompletedTask;
    }
}

public class WovenHeartPower : WovenHeartPowerBase
{
    protected override int TriggerLimit => 2;
}

public class UpgradedWovenHeartPower : WovenHeartPowerBase
{
    protected override int TriggerLimit => 3;
}

