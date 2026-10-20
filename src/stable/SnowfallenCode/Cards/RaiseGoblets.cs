using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class RaiseGoblets() : SnowfallenCard(
    1,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WaterBreathPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(4m),
        new DynamicVar("AllyHeal", 2m),
        new DynamicVar("WaterDivisor", 6m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);

        decimal water = Owner.Creature.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        decimal allyHealing = DynamicVars["AllyHeal"].BaseValue +
                              Math.Ceiling(water / DynamicVars["WaterDivisor"].BaseValue);
        foreach (var ally in CombatState.PlayerCreatures
                     .Where(creature => creature != Owner.Creature && !creature.IsDead))
        {
            await CreatureCmd.Heal(ally, allyHealing);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(2m);
        DynamicVars["AllyHeal"].UpgradeValueBy(1m);
        DynamicVars["WaterDivisor"].UpgradeValueBy(-1m);
    }
}
