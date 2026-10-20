using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class ByTheMilkyWayPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player ||
            cardPlay.Card is ByTheMilkyWay)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(1m, Owner.Player);
        await PowerCmd.Decrement(this);
    }
}
