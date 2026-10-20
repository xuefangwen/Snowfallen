using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class WaterDuckweedSplendor() : SnowfallenCard(
    2,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FrostPower>(),
        BloodThresholdHoverTip()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new HpLossVar(2m),
        new PowerVar<FrostPower>(3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool isHighHp = BloodThresholdMechanics.IsHigh(Owner.Creature);
        AttackCommand attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        int enemiesDamaged = attack.Results
            .SelectMany(results => results)
            .Count(result => result.TotalDamage > 0m);

        for (int i = 0; i < enemiesDamaged; i++)
        {
            if (isHighHp)
            {
                await SnowfallenMod.SnowfallenCode.Mechanics.SnowfallenHpLoss.Apply(
                    choiceContext,
                    Owner.Creature,
                    DynamicVars.HpLoss.BaseValue,
                    DamageProps.cardHpLoss,
                    this,
                    cardPlay);
                await FrostMechanics.Gain(
                    choiceContext,
                    Owner.Creature,
                    DynamicVars["FrostPower"].BaseValue,
                    Owner.Creature,
                    this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["FrostPower"].UpgradeValueBy(1m);
    }
}


