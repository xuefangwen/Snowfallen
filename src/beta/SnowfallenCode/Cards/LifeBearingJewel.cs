using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class LifeBearingJewel() : SnowfallenCard(
    2,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BloodThresholdHoverTip()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(18m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        decimal missingPercent = Math.Clamp(
            100m * (1m -
                (decimal)Owner.Creature.CurrentHp / Owner.Creature.MaxHp),
            0m,
            100m);
        decimal bonusMultiplier = BloodThresholdMechanics.IsAllThresholdsActive(Owner.Creature)
            ? missingPercent * 0.10m
            : Math.Min(missingPercent, 30m) * 0.01m +
              Math.Min(Math.Max(missingPercent - 30m, 0m), 30m) * 0.03m +
              Math.Max(missingPercent - 60m, 0m) * 0.10m;
        decimal damage = Math.Floor(
            DynamicVars.Damage.BaseValue * (1m + bonusMultiplier));

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(7m);
    }
}


