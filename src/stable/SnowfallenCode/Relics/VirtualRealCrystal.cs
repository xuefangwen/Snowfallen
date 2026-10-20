using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Relics;

public class VirtualRealCrystal : SnowfallenRelics
{
    public bool Available { get; set; } = true;

    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool IsUsedUp => !Available;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FrostPower>()
    ];

    public override bool ShouldDieLate(Creature creature)
    {
        return creature != Owner.Creature || !Available;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner.Creature || !Available)
        {
            return;
        }

        Available = false;
        Flash();

        await WaterBreathGainSuppression.Run(
            creature,
            () => CreatureCmd.SetCurrentHp(creature, 1m));

        var choiceContext = new BlockingPlayerChoiceContext();
        decimal maxHp = Owner.Creature.MaxHp;
        await FrostMechanics.Gain(
            choiceContext,
            Owner.Creature,
            decimal.Floor(maxHp * 0.5m),
            Owner.Creature,
            null);

        foreach (Creature enemy in Owner.Creature.CombatState.Enemies
                     .Where(enemy => !enemy.IsDead)
                     .ToList())
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                maxHp,
                ValueProp.Unpowered,
                Owner.Creature,
                null);
        }
    }
}
