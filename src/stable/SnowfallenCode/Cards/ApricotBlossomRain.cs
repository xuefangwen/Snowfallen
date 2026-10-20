using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SnowfallenMod.SnowfallenCode.Powers;
namespace SnowfallenMod.SnowfallenCode.Cards;

public class ApricotBlossomRain() : SnowfallenCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VitalityPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(8m), new DynamicVar("VitalityTurns", 2m)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        // No immediate effect; the next actual owner-turn start is the first trigger.
        if (IsUpgraded)
            await PowerCmd.Apply<UpgradedApricotBlossomRainPower>(context, Owner.Creature, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<ApricotBlossomRainPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(4m);
}
