#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Monsters;

namespace SnowfallenMod.SnowfallenCode.Powers;

public sealed class ShengZhangSquadPower : SnowfallenPower
{
    private int _pendingIncomingDamage;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override bool IsVisibleInternal => false;

    public override bool ShouldPlayVfx => false;

    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        _pendingIncomingDamage = 0;
        Player? player = Owner.Player;
        if (player is null || target != Owner)
        {
            return Task.CompletedTask;
        }
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered))
        {
            return Task.CompletedTask;
        }
        if (ShengZhangMechanics.UnitCount(player) == 0)
        {
            return Task.CompletedTask;
        }

        _pendingIncomingDamage = Math.Max(0, (int)Math.Ceiling(amount));
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || _pendingIncomingDamage <= 0)
        {
            return;
        }

        int damage = _pendingIncomingDamage;
        _pendingIncomingDamage = 0;
        Player? player = Owner.Player;
        if (player is null)
        {
            return;
        }

        await ShengZhangMechanics.ApplyEqualDamageToUnits(
            choiceContext,
            player,
            damage,
            dealer);
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState state)
    {
        if (Owner.Player is not { } player)
        {
            return;
        }

        if (side == CombatSide.Enemy)
        {
            ShengZhangMechanics.RefreshAllUnitState(player);
            await ShengZhangMechanics.ExecuteUnits(player);
            return;
        }

        if (side == Owner.Side)
        {
            await ShengZhangMechanics.ApplyQueuedTeamPlating(player, Owner);
            ShengZhangMechanics.RefreshUnitIntents(player);
        }
    }
}

