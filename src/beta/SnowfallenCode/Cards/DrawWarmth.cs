using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class DrawWarmth() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Rare,
    TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WaterBreathPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(3m),
        new PowerVar<WaterBreathPower>(1m),
        new DynamicVar("CleanseThreshold", 11m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal waterAtPlay = Owner.Creature.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);

        if (IsUpgraded)
        {
            await WaterBreathMechanics.Gain(
                choiceContext,
                Owner.Creature,
                DynamicVars["WaterBreathPower"].BaseValue,
                Owner.Creature,
                this);
        }

        if (waterAtPlay > DynamicVars["CleanseThreshold"].BaseValue)
        {
            PowerModel debuff = Owner.Creature.Powers
                .Where(power => power.Type == PowerType.Debuff)
                .OrderByDescending(power => power.Amount)
                .FirstOrDefault();
            if (debuff is not null)
            {
                await PowerCmd.Remove(debuff);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(1m);
        DynamicVars["CleanseThreshold"].UpgradeValueBy(-3m);
    }
}

