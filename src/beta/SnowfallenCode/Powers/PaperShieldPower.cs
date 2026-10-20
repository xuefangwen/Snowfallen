using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class PaperShieldPower : SnowfallenPower
{
    public int TurnsRemaining { get; set; } = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card is IEmberCard && cardPlay.Card.Owner.Creature == Owner)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, cardPlay);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Side)
        {
            TurnsRemaining--;
            InvokeDisplayAmountChanged();
            if (TurnsRemaining <= 0)
            {
                await PowerCmd.Remove(this);
            }
        }
    }

    public void RefreshDuration()
    {
        TurnsRemaining = 2;
        InvokeDisplayAmountChanged();
    }
}

