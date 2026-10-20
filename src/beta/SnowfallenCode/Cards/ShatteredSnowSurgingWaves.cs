using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class ShatteredSnowSurgingWaves() : SnowfallenCard(
    0,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AllEnemies)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
        , BloodThresholdHoverTip()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(2m),
        new DamageVar(10m, ValueProp.Move),
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await SnowfallenMod.SnowfallenCode.Mechanics.SnowfallenHpLoss.Apply(
            choiceContext,
            Owner.Creature,
            DynamicVars.HpLoss.BaseValue,
            DamageProps.cardHpLoss,
            this,
            cardPlay);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        if (!BloodThresholdMechanics.IsNormal(Owner.Creature) &&
            !BloodThresholdMechanics.IsLow(Owner.Creature))
        {
            return;
        }

        List<MegaCrit.Sts2.Core.Entities.Creatures.Creature> enemies =
            CombatState.HittableEnemies.ToList();
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                enemy,
                -DynamicVars.Strength.BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<DexterityPower>(
                choiceContext,
                enemy,
                -DynamicVars.Dexterity.BaseValue,
                Owner.Creature,
                this);
        }

        if (enemies.Count > 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Strength.BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<DexterityPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars.Dexterity.BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}


