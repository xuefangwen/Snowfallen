#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Monsters;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

public enum ShengZhangBlockTarget
{
    Self,
    Team
}

public static class ShengZhangMechanics
{
    public const int MaxUnits = 3;
    private static readonly HashSet<ShengZhangUnit> PendingTeamPlating =
        new(ReferenceEqualityComparer.Instance);

    public static int UnitCount(Player owner) => GetUnits(owner).Count;

    public static List<ShengZhangUnit> GetUnits(Player owner)
    {
        if (owner.PlayerCombatState is null)
        {
            return [];
        }

        return owner.PlayerCombatState.Pets
            .Where(creature => creature.IsAlive && creature.Monster is ShengZhangUnit)
            .Select(creature => (ShengZhangUnit)creature.Monster!)
            .OrderBy(unit => unit.Creature.CombatId ?? uint.MaxValue)
            .ToList();
    }

    public static async Task Summon(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel source)
    {
        if (UnitCount(owner) >= MaxUnits || owner.Creature.CombatState is null)
        {
            return;
        }

        await EnsureManager(choiceContext, owner, source);
        if (UnitCount(owner) >= MaxUnits)
        {
            return;
        }

        int hp = source.IsUpgraded ? 75 : 50;
        int attack = source.IsUpgraded ? 9 : 6;
        int block = source.IsUpgraded ? 9 : 6;

        ShengZhangUnit model = (ShengZhangUnit)ModelDb.Monster<ShengZhangUnit>().ToMutable();
        model.Configure(hp, attack, block, ShengZhangBlockTarget.Team);
        Creature unit = owner.Creature.CombatState.CreateCreature(
            model,
            owner.Creature.Side,
            null);
        await PlayerCmd.AddPet(unit, owner);
        model.SetInitialIntent();
        RefreshAllUnitHealthBars(owner);
        RefreshNodeIntent(model, owner.Creature.CombatState.HittableEnemies);
    }

    public static void RefreshUnitIntents(Player owner)
    {
        if (owner.Creature.CombatState is null)
        {
            return;
        }

        foreach (ShengZhangUnit unit in GetUnits(owner))
        {
            unit.AdvanceIntent();
            EnsureUnitHealthBarVisible(unit);
            RefreshNodeIntent(unit, owner.Creature.CombatState.HittableEnemies);
        }
    }

    public static async Task ExecuteUnits(Player owner)
    {
        if (owner.Creature.CombatState is null)
        {
            return;
        }

        foreach (ShengZhangUnit unit in GetUnits(owner))
        {
            if (unit.Creature.IsDead)
            {
                continue;
            }

            NCreature? node = NCombatRoom.Instance?.GetCreatureNode(unit.Creature);
            if (node is not null)
            {
                await node.PerformIntent();
            }

            await unit.ExecuteNextMove(owner.Creature.CombatState.Enemies);
        }
    }

    public static void RefreshAllUnitState(Player owner)
    {
        RefreshAllUnitHealthBarsNow(owner);
    }

    public static async Task GainBlockFromUnit(Player owner, ShengZhangUnit unit)
    {
        int amount = unit.BaseBlock;
        if (unit.BlockTarget == ShengZhangBlockTarget.Self)
        {
            await CreatureCmd.GainBlock(unit.Creature, amount, ValueProp.Unpowered, null);
            return;
        }

        await CreatureCmd.GainBlock(owner.Creature, amount, ValueProp.Unpowered, null);
        foreach (ShengZhangUnit teamUnit in GetUnits(owner))
        {
            await CreatureCmd.GainBlock(teamUnit.Creature, amount, ValueProp.Unpowered, null);
        }
    }

    public static void QueueTeamPlating(ShengZhangUnit unit)
    {
        PendingTeamPlating.Add(unit);
    }

    public static async Task ApplyQueuedTeamPlating(Player owner, Creature source)
    {
        List<ShengZhangUnit> units = GetUnits(owner);
        int layers = PendingTeamPlating.Count(units.Contains);
        PendingTeamPlating.Clear();
        if (layers <= 0)
        {
            return;
        }

        for (int i = 0; i < layers; i++)
        {
            await PowerCmd.Apply<PlatingPower>(
                new BlockingPlayerChoiceContext(),
                owner.Creature,
                1m,
                source,
                null);
            foreach (ShengZhangUnit unit in units)
            {
                await PowerCmd.Apply<PlatingPower>(
                    new BlockingPlayerChoiceContext(),
                    unit.Creature,
                    1m,
                    source,
                    null);
            }
        }

        RefreshAllUnitHealthBarsNow(owner);
    }

    public static async Task ApplyEqualDamageToUnits(
        PlayerChoiceContext choiceContext,
        Player owner,
        int damage,
        Creature? dealer)
    {
        List<ShengZhangUnit> units = GetUnits(owner);
        if (units.Count == 0 || damage <= 0)
        {
            return;
        }

        foreach (ShengZhangUnit unit in units)
        {
            Creature creature = unit.Creature;
            int blocked = Math.Min(creature.Block, damage);
            if (blocked > 0)
            {
                creature.LoseBlockInternal(blocked);
            }

            int hpLoss = damage - blocked;
            if (hpLoss > 0)
            {
                await CreatureCmd.TriggerAnim(creature, "Hit", 0f);
                creature.LoseHpInternal(
                    hpLoss,
                    ValueProp.Unblockable | ValueProp.Unpowered);
            }

            if (creature.IsDead)
            {
                _ = TaskHelper.RunSafely(KillDeadUnitLater(creature));
            }
        }
    }

    private static async Task KillDeadUnitLater(Creature creature)
    {
        await Cmd.Wait(0.01f);
        if (creature.IsDead && creature.CombatState?.ContainsCreature(creature) == true)
        {
            await CreatureCmd.Kill(creature, force: true);
        }
    }

    private static async Task EnsureManager(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel source)
    {
        if (owner.Creature.HasPower<ShengZhangSquadPower>())
        {
            return;
        }

        await PowerCmd.Apply<ShengZhangSquadPower>(
            choiceContext,
            owner.Creature,
            1m,
            owner.Creature,
            source);
    }

    private static void RefreshNodeIntent(
        ShengZhangUnit unit,
        IReadOnlyList<Creature> targets)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(unit.Creature);
        if (node is null)
        {
            return;
        }

        node.UpdateIntent(targets);
        node.IntentContainer.Modulate = Colors.White;
    }

    private static void EnsureUnitHealthBarVisible(ShengZhangUnit unit)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(unit.Creature);
        NCreatureStateDisplay? stateDisplay =
            node?.GetNodeOrNull<NCreatureStateDisplay>("%HealthBar");
        stateDisplay?.AnimateIn(HealthBarAnimMode.FromHidden);
        stateDisplay?
            .GetNodeOrNull<NHealthBar>("%HealthBar")
            ?.RefreshValues();
        EnsureUnitPowerBarVisible(node, stateDisplay);
    }

    private static void EnsureUnitPowerBarVisible(
        NCreature? node,
        NCreatureStateDisplay? stateDisplay)
    {
        NPowerContainer? powerContainer =
            stateDisplay?.GetNodeOrNull<NPowerContainer>("%PowerContainer");
        if (powerContainer is null)
        {
            return;
        }

        powerContainer.Visible = true;
        if (node?.Visuals.Bounds is Control bounds)
        {
            powerContainer.SetCreatureBounds(bounds);
        }
    }

    private static void RefreshAllUnitHealthBars(Player owner)
    {
        RefreshAllUnitHealthBarsNow(owner);
        Callable.From(() => RefreshAllUnitHealthBarsNow(owner)).CallDeferred();
    }

    private static void RefreshAllUnitHealthBarsNow(Player owner)
    {
        foreach (ShengZhangUnit unit in GetUnits(owner))
        {
            EnsureUnitHealthBarVisible(unit);
        }
    }
}

