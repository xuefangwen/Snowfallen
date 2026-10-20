using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class RighteousDeed() : SnowfallenCard(
    2,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new DynamicVar("HealingDivisor", 4m)
    ];

    public override async Task BeforeCombatStart()
    {
        await TeamHealingMechanics.EnsureTracker(
            new BlockingPlayerChoiceContext(),
            Owner,
            this);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        var tracker = await TeamHealingMechanics.EnsureTracker(
            choiceContext,
            Owner,
            this);
        decimal damage = DynamicVars.Damage.BaseValue +
                         Math.Floor(
                             tracker.TotalHealing /
                             DynamicVars["HealingDivisor"].BaseValue);

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars["HealingDivisor"].UpgradeValueBy(-1m);
    }
}


