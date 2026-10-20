using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Relics;

public class MysticImmortalMark : WaterBottlePendant
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WaterBreathPower>(),
        HoverTipFactory.FromPower<VitalityPower>()
    ];

    protected override async Task ApplySynchronizedCombatStartEffect(
        PlayerChoiceContext choiceContext)
    {
        Flash();
        await WaterBreathMechanics.Gain(
            choiceContext,
            Owner.Creature,
            6m,
            Owner.Creature,
            null);
        await PowerCmd.Apply<VitalityPower>(
            choiceContext, Owner.Creature, 5m, Owner.Creature, null);
    }

}
