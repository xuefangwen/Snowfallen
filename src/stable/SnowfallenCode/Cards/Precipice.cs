using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.HoverTips;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class Precipice() : SnowfallenCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [BloodThresholdHoverTip()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new ExtraDamageVar(7m),
        new DynamicVar("MaxHp", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        bool isLowHp = BloodThresholdMechanics.IsLow(Owner.Creature);
        decimal damage = DynamicVars.Damage.BaseValue +
                         (isLowHp ? DynamicVars.ExtraDamage.BaseValue : 0m);

        AttackCommand attack = await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        if (isLowHp &&
            attack.Results
                .SelectMany(results => results)
                .Any((DamageResult result) => result.WasTargetKilled))
        {
            await CreatureCmd.GainMaxHp(
                Owner.Creature,
                DynamicVars["MaxHp"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.ExtraDamage.UpgradeValueBy(3m);
    }
}
