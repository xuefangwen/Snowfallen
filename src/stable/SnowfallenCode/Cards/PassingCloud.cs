using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class PassingCloud() : SnowfallenCard(
    2,
    CardType.Skill,
    CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<OverCloudPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<OverCloudPower>(3m),
        new DynamicVar("StrengthLoss", 5m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<OverCloudPower>(
                choiceContext,
                enemy,
                DynamicVars["OverCloudPower"].BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<PassingCloudStrengthDownPower>(
                choiceContext,
                enemy,
                DynamicVars["StrengthLoss"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["OverCloudPower"].UpgradeValueBy(2m);
        DynamicVars["StrengthLoss"].UpgradeValueBy(4m);
    }
}
