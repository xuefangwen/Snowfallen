using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class VitalityPower : SnowfallenPower
{
    private static readonly HashSet<Creature> RestoringMaxHp = [];
    internal static bool IsRestoringMaxHp(Creature creature) => RestoringMaxHp.Contains(creature);
    [SavedProperty] public int AppliedMaxHpBonus { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override string CustomPackedIconPath => "vitality_power.png".PowerImagePath();
    public override string CustomBigIconPath => "vitality_power.png".BigPowerImagePath();

    public override async Task AfterApplied(Creature applier, CardModel cardSource)
    {
        if (AppliedMaxHpBonus != 0) return;
        int oldMaxHp = Owner.MaxHp;
        await CreatureCmd.SetMaxHp(Owner, oldMaxHp + Math.Floor(oldMaxHp * 0.3m));
        AppliedMaxHpBonus = Owner.MaxHp - oldMaxHp;
    }

    private async Task RestoreMaxHp(Creature creature)
    {
        int bonus = AppliedMaxHpBonus;
        if (bonus <= 0) return;
        AppliedMaxHpBonus = 0;
        RestoringMaxHp.Add(creature);
        try
        {
            await CreatureCmd.SetMaxHp(creature, Math.Max(1, creature.MaxHp - bonus));
        }
        finally { RestoringMaxHp.Remove(creature); }
        foreach (var power in creature.Powers.OfType<EndlessLifePowerBase>())
            power.SynchronizeHpWithoutLoss();
    }

    public override Task AfterRemoved(Creature oldOwner) => RestoreMaxHp(oldOwner);
    public override Task AfterCombatEnd(CombatRoom room) => RestoreMaxHp(Owner);

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext,
        CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (creatures.Contains(Owner))
            await PowerCmd.ModifyAmount(choiceContext, this, -1m, Owner, null);
    }
}
