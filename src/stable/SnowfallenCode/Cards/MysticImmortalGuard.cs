using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

[BaseLib.Utils.Pool(typeof(SnowfallenMod.SnowfallenCode.Character.RetiredSnowfallenCardPool))]
public class MysticImmortalGuard() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Common,
    TargetType.Self, false)
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FrostPower>()
        , BloodThresholdHoverTip()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new BlockVar("LowBlock", 8m, ValueProp.Move),
        new PowerVar<FrostPower>(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool isHighHp = BloodThresholdMechanics.IsHigh(Owner.Creature);
        bool isLowHp = BloodThresholdMechanics.IsLow(Owner.Creature);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (isHighHp)
        {
            await FrostMechanics.Gain(
                choiceContext,
                Owner.Creature,
                DynamicVars["FrostPower"].BaseValue,
                Owner.Creature,
                this);
        }
        if (isLowHp)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                DynamicVars["LowBlock"].BaseValue,
                ValueProp.Move,
                cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["LowBlock"].UpgradeValueBy(2m);
        DynamicVars["FrostPower"].UpgradeValueBy(1m);
    }
}
