using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class LingeringEmber() : SnowfallenCard(
    0,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    public override HashSet<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WaterBreathPower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new ExtraDamageVar(4m),
        new PowerVar<WeakPower>(2m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal consumed = await ConsumeWaterBreath(choiceContext, 2m);
        decimal damage = DynamicVars.Damage.BaseValue +
                         DynamicVars.ExtraDamage.BaseValue * consumed;

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        if (consumed > 0m)
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                CombatState.HittableEnemies,
                DynamicVars.Weak.BaseValue * consumed,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    private Task<decimal> ConsumeWaterBreath(
        PlayerChoiceContext choiceContext,
        decimal maximum) =>
        WaterBreathMechanics.Consume(choiceContext, Owner.Creature, maximum, this);
}
