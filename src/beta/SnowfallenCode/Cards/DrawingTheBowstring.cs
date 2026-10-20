using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class DrawingTheBowstring() : SnowfallenCard(
    1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        BloodThresholdHoverTip()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(3m),
        new DamageVar(9m, ValueProp.Move),
        new HealVar(2m),
        new PowerVar<VulnerablePower>("HighVulnerable", 2m),
        new PowerVar<VulnerablePower>("LowVulnerable", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        if (BloodThresholdMechanics.IsHigh(Owner.Creature))
        {
            await SnowfallenMod.SnowfallenCode.Mechanics.SnowfallenHpLoss.Apply(
                choiceContext,
                Owner.Creature,
                DynamicVars.HpLoss.BaseValue,
                DamageProps.cardHpLoss,
                this,
                cardPlay);
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["HighVulnerable"].BaseValue,
                Owner.Creature,
                this);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
                .Execute(choiceContext);
        }
        if (BloodThresholdMechanics.IsLow(Owner.Creature))
        {
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["LowVulnerable"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Heal.UpgradeValueBy(1m);
        DynamicVars["HighVulnerable"].UpgradeValueBy(1m);
        DynamicVars["LowVulnerable"].UpgradeValueBy(1m);
    }
}


