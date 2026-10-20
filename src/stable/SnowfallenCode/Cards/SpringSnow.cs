using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class SpringSnow() : SnowfallenCard(
    2,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<RegenPower>(),
        HoverTipFactory.FromPower<WaterBreathPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RegenPower>(2m),
        new CardsVar(3),
        new DynamicVar("FreeThreshold", 5m),
        new PowerVar<WaterBreathPower>(5m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RegenPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["RegenPower"].BaseValue,
            Owner.Creature,
            this);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        WaterBreathPower waterBreath = Owner.Creature.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault();
        if ((waterBreath?.Amount ?? 0m) >= DynamicVars["FreeThreshold"].BaseValue)
        {
            await WaterBreathMechanics.Consume(
                choiceContext,
                Owner.Creature,
                DynamicVars["WaterBreathPower"].BaseValue,
                this);
        }
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!ReferenceEquals(card, this) ||
            card.Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return false;
        }

        decimal waterBreath = Owner.Creature.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        if (waterBreath < DynamicVars["FreeThreshold"].BaseValue)
        {
            return false;
        }

        modifiedCost = 0m;
        return modifiedCost != originalCost;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RegenPower"].UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
