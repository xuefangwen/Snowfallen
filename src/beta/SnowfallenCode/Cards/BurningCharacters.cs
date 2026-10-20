using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class BurningCharacters() : SnowfallenCard(
    2,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar("DamageCap", 16m)
    ];

    public override async Task BeforeCombatStart()
    {
        await TeamHealingMechanics.EnsureTracker(
            new BlockingPlayerChoiceContext(),
            Owner,
            this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TeamHealingTrackerPower tracker = await TeamHealingMechanics.EnsureTracker(
            choiceContext,
            Owner,
            this);
        decimal damage = Math.Min(
            DynamicVars["DamageCap"].BaseValue,
            DynamicVars.Damage.BaseValue + Math.Floor(tracker.OwnerHealing / 3m));

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        await EmberMechanics.CreateInHand(
            Owner,
            CombatState,
            1,
            permanentDamageBonus: Math.Max(0m, damage - 4m));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DamageCap"].UpgradeValueBy(9m);
    }
}


