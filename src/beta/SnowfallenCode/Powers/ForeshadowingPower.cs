using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class ForeshadowingPowerBase : SnowfallenPower
{
    protected abstract decimal FrostPerStack { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        int retained = PileType.Hand.GetPile(player).Cards.Count(card => card.ShouldRetainThisTurn);
        if (retained == 0)
        {
            return;
        }

        Flash();
        await FrostMechanics.Gain(
            choiceContext,
            Owner,
            FrostPerStack * Amount * retained,
            Owner,
            null);
    }
}

public class ForeshadowingPower : ForeshadowingPowerBase
{
    protected override decimal FrostPerStack => 2m;
}

public class UpgradedForeshadowingPower : ForeshadowingPowerBase
{
    protected override decimal FrostPerStack => 3m;
}

