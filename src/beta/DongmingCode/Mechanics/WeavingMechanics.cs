using System;
using System.Linq;
using System.Threading.Tasks;
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

public static class WeavingMechanics
{
    public static async Task ApplyMark(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature target)
    {
        if (!target.IsAlive || target.Monster is OldDayShadow)
        {
            return;
        }

        CombatState combatState = owner.Creature.CombatState as CombatState
            ?? throw new InvalidOperationException("Weaving requires an active combat.");

        foreach (Creature markedCreature in combatState.Enemies.ToList())
        {
            WeavingMarkPower mark = markedCreature.GetPower<WeavingMarkPower>();
            if (mark is not null)
            {
                await PowerCmd.Remove(mark);
            }
        }

        foreach (Creature shadow in combatState.Enemies
                     .Where(c => c.Monster is OldDayShadow && c.HasPower<WeavingShadowPower>())
                     .ToList())
        {
            shadow.GetPower<WeavingShadowPower>()?.SuppressRelay();
            await CreatureCmd.Kill(shadow, force: true);
        }

        await PowerCmd.Apply<WeavingMarkPower>(
            choiceContext,
            target,
            1m,
            owner.Creature,
            null);

        if (!target.HasPower<WeavingMarkPower>())
        {
            return;
        }

        int shadowHp = Math.Max(1, target.CurrentHp);
        OldDayShadow shadowModel =
            (OldDayShadow)ModelDb.Monster<OldDayShadow>().ToMutable();
        Creature shadowCreature = await CreatureCmd.Add(
            shadowModel,
            combatState,
            CombatSide.Enemy,
            null);
        shadowCreature.SetMaxHpInternal(shadowHp);
        shadowCreature.SetCurrentHpInternal(shadowHp);

        await PowerCmd.Apply<MinionPower>(
            choiceContext,
            shadowCreature,
            1m,
            owner.Creature,
            null);
        await PowerCmd.Apply<WeavingShadowPower>(
            choiceContext,
            shadowCreature,
            1m,
            owner.Creature,
            null);

        PositionShadow(shadowCreature, target);
    }

    private static void PositionShadow(Creature shadow, Creature target)
    {
        NCombatRoom room = NCombatRoom.Instance;
        if (room is null)
        {
            return;
        }

        var shadowNode = room.GetCreatureNode(shadow);
        var targetNode = room.GetCreatureNode(target);
        if (shadowNode is null || targetNode is null)
        {
            return;
        }

        shadowNode.GlobalPosition = targetNode.GlobalPosition + new Vector2(0f, -120f);
    }
}

