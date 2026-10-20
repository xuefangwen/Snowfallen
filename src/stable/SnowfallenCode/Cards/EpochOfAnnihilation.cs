using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class EpochOfAnnihilation() : SnowfallenCard(
    3,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(24m, ValueProp.Move),
        new ExtraDamageVar(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal consumed = await ConsumeWaterBreath(choiceContext, 10m);
        decimal gainedBonus = DynamicVars.ExtraDamage.BaseValue * consumed;

        decimal damage = DynamicVars.Damage.BaseValue +
                         gainedBonus;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    private Task<decimal> ConsumeWaterBreath(
        PlayerChoiceContext choiceContext,
        decimal maximum) =>
        WaterBreathMechanics.Consume(choiceContext, Owner.Creature, maximum, this);
}
