using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class FollowingTheEastWind() : SnowfallenCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BloodThresholdHoverTip(), HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new DynamicVar("HighBonus", 4m),
        new PowerVar<StrengthPower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool isHighHp = BloodThresholdMechanics.IsHigh(Owner.Creature);
        decimal damage = isHighHp
            ? DynamicVars.Damage.BaseValue + DynamicVars["HighBonus"].BaseValue
            : DynamicVars.Damage.BaseValue;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        if (isHighHp)
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature,
                DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars["HighBonus"].UpgradeValueBy(2m);
    }
}
