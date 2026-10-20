using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class TransformingToReachFar() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromPower<FrostPower>(),
            BloodThresholdHoverTip()
        ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(20m),
        new PowerVar<FrostPower>(20m),
        new HealVar(13m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (BloodThresholdMechanics.IsHigh(Owner.Creature))
        {
            await SnowfallenMod.SnowfallenCode.Mechanics.SnowfallenHpLoss.Apply(
                choiceContext,
                Owner.Creature,
                DynamicVars.HpLoss.BaseValue,
                DamageProps.cardHpLoss,
                Owner.Creature,
                this);
            await FrostMechanics.Gain(
                choiceContext,
                Owner.Creature,
                DynamicVars["FrostPower"].BaseValue,
                Owner.Creature,
                this);
        }
        if (BloodThresholdMechanics.IsLow(Owner.Creature))
        {
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(5m);
    }
}
