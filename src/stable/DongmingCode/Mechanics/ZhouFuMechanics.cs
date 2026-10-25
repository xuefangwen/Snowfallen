using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SnowfallenMod.DongmingCode.Monsters;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Mechanics;

public static class ZhouFuMechanics
{
    public static async Task Apply(PlayerChoiceContext context, Player owner, Creature target, decimal ratio, CardModel source)
    {
        if (!target.IsAlive || target.Monster is OldDayShadow || target.CombatId is null) return;
        CombatState state = owner.Creature.CombatState as CombatState ?? throw new InvalidOperationException("Curse Bind requires active combat.");
        int hp = Math.Max(1, (int)Math.Floor(target.CurrentHp * ratio));
        Creature shadow = await CreatureCmd.Add((OldDayShadow)ModelDb.Monster<OldDayShadow>().ToMutable(), state, CombatSide.Enemy, null);
        shadow.SetMaxHpInternal(hp);
        shadow.SetCurrentHpInternal(hp);
        await PowerCmd.Apply<MinionPower>(context, shadow, 1m, owner.Creature, source);
        ZhouFuShadowPower binding = (ZhouFuShadowPower)ModelDb.Power<ZhouFuShadowPower>().ToMutable();
        binding.Configure(target.CombatId.Value, hp);
        await PowerCmd.Apply(context, binding, shadow, 1m, owner.Creature, source, false);
        Position(shadow, target);
    }

    private static void Position(Creature shadow, Creature target)
    {
        NCombatRoom room = NCombatRoom.Instance; if (room is null) return;
        var a = room.GetCreatureNode(shadow); var b = room.GetCreatureNode(target); if (a is null || b is null) return;
        a.GlobalPosition = b.GlobalPosition + new Vector2(0f, -120f);
    }
}
