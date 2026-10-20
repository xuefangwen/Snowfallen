using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class Toast() : SnowfallenCard(
    0,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BloodThresholdHoverTip()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(1m),
        new HpLossVar("HighHpLoss", 4m),
        new EnergyVar(1),
        new CardsVar(1)
    ];

    public override async Task BeforeCombatStart()
    {
        await ToastTrackerPower.Ensure(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool isHighHp = BloodThresholdMechanics.IsHigh(Owner.Creature);
        decimal hpLoss = isHighHp
            ? DynamicVars["HighHpLoss"].BaseValue
            : DynamicVars.HpLoss.BaseValue;

        await SnowfallenMod.SnowfallenCode.Mechanics.SnowfallenHpLoss.Apply(
            choiceContext,
            Owner.Creature,
            hpLoss,
            DamageProps.cardHpLoss,
            Owner.Creature,
            this);
        ToastTrackerPower tracker = await ToastTrackerPower.Ensure(
            choiceContext,
            Owner.Creature,
            this);
        if (tracker.TryClaimEnergy())
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
