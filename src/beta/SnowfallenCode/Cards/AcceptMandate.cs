using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class AcceptMandate() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Rare,
    TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        IsUpgraded
            ? [HoverTipFactory.FromPower<UpgradedAcceptMandatePower>()]
            : [HoverTipFactory.FromPower<AcceptMandatePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(12m),
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<BufferPower>(0m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        if (cardPlay.Target.HasPower<AcceptMandateUsedPower>())
        {
            return;
        }

        await PowerCmd.Apply<AcceptMandateUsedPower>(
            choiceContext,
            cardPlay.Target,
            1m,
            Owner.Creature,
            this);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (IsUpgraded)
        {
            await PowerCmd.Apply<UpgradedAcceptMandatePower>(
                choiceContext,
                cardPlay.Target,
                1m,
                Owner.Creature,
                this);
        }
        else
        {
            await PowerCmd.Apply<AcceptMandatePower>(
                choiceContext,
                cardPlay.Target,
                1m,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.HpLoss.UpgradeValueBy(-4m);
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars["BufferPower"].UpgradeValueBy(1m);
    }
}

