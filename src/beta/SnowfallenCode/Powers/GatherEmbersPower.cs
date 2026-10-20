using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class GatherEmbersPower : SnowfallenPower
{
    private int _turnsRemaining;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task BeforeApplied(Creature target, decimal amount, Creature applier, CardModel cardSource)
    {
        _turnsRemaining = 2;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side)
        {
            return;
        }

        _turnsRemaining--;
        if (_turnsRemaining <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}

