using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class RemainingPagesArmorPowerBase : SnowfallenPower
{
    protected abstract decimal BlockPerStack { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath =>
        "remaining_pages_armor_power.png".PowerImagePath();
    public override string CustomBigIconPath =>
        "remaining_pages_armor_power.png".BigPowerImagePath();

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card is not IEmberCard ||
            cardPlay.Card.Owner.Creature != Owner)
        {
            return;
        }

        Flash();
        foreach (var ally in cardPlay.Card.CombatState.PlayerCreatures
                     .Where(creature => creature != Owner && !creature.IsDead))
        {
            await CreatureCmd.GainBlock(
                ally,
                BlockPerStack * Amount,
                ValueProp.Unpowered,
                cardPlay);
        }
    }
}

public class RemainingPagesArmorPower : RemainingPagesArmorPowerBase
{
    protected override decimal BlockPerStack => 2m;
}

public class UpgradedRemainingPagesArmorPower : RemainingPagesArmorPowerBase
{
    protected override decimal BlockPerStack => 3m;
}
