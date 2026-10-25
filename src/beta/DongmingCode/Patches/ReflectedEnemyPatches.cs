using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
public static class ReflectedMonsterActionPatch
{
    [HarmonyReversePatch]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task Original(MonsterModel instance) => throw new NotImplementedException();

    [HarmonyPrefix]
    public static bool Prefix(MonsterModel __instance, ref Task __result)
    {
        __result = Run(__instance);
        return false;
    }

    internal static async Task Run(MonsterModel monster)
    {
        var previous = ReflectedEnemyMechanics.ActingMonster.Value;
        ReflectedEnemyMechanics.ActingMonster.Value = monster;
        var combat = monster.Creature.CombatState;
        bool stun = monster.NextMove.Id == "STUNNED";
        try
        {
            await ReflectedEnemyMechanics.BeginAction(monster.Creature);
            await Original(monster);
            await ReflectedEnemyMechanics.EndAction(monster.Creature, stun);
        }
        catch (ReflectedEnemyMechanics.SmoggyAttackerDiedException) when (!monster.Creature.IsAlive)
        {
            // BeforeAttack canceled this dead summon's remaining move.
            // Vanilla Kill defers state removal while IsPerformingMove is true.
            AccessTools.Property(typeof(MonsterModel), nameof(MonsterModel.IsPerformingMove))
                .SetValue(monster, false);
            if (combat != null && combat.ContainsCreature(monster.Creature) &&
                Hook.ShouldCreatureBeRemovedFromCombatAfterDeath(combat, monster.Creature))
                combat.RemoveCreature(monster.Creature);
        }
        finally { ReflectedEnemyMechanics.ActingMonster.Value = previous; }
    }
}

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove))]
public static class ReflectedStunSchedulePatch
{
    [HarmonyPostfix] public static void Postfix(MonsterModel __instance) => ReflectedEnemyMechanics.ScheduleStun(__instance);
}

[HarmonyPatch(typeof(MonsterModel), "get_NextMove")]
public static class ReflectedMoveOwnerPatch
{
    [HarmonyPostfix] public static void Postfix(MonsterModel __instance, MoveState __result) => ReflectedEnemyMechanics.RegisterMove(__instance, __result);
}

[HarmonyPatch(typeof(MoveState), "get_Intents")]
public static class ReflectedIntentPatch
{
    [HarmonyPostfix] public static void Postfix(MoveState __instance, ref IReadOnlyList<AbstractIntent> __result) =>
        __result = ReflectedEnemyMechanics.DisplayIntents(__instance, __result);
}

[HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.Execute))]
public static class ReflectedAttackPatch
{
    [HarmonyPrefix]
    public static bool Prefix(AttackCommand __instance, PlayerChoiceContext choiceContext,
        ref Task<AttackCommand> __result)
    {
        if (__instance.Attacker is not { IsMonster: true, IsAlive: true } dealer) return true;
        if (!ReflectedEnemyMechanics.IsTangledActive(dealer)) return true;
        __result = ReflectedEnemyMechanics.ConvertAttack(__instance, choiceContext);
        return false;
    }

}

[HarmonyPatch(typeof(LivingFog), "BloatMove")]
public static class ReflectedSummonDiagnosticPatch
{
    [HarmonyPrefix]
    public static void Prefix(LivingFog __instance)
    {
        if (__instance.Creature.HasPower<ReflectedSmoggyPower>() &&
            string.IsNullOrEmpty(__instance.CombatState.Encounter?.GetNextSlot(__instance.CombatState)))
            ReflectedEnemyMechanics.Trace("Smoggy skipped: encounter has no free summon slot; no successful summon, no hidden debuff attached.");
    }
}
