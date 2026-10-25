using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Monsters;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class WeavingMarkPower : DongmingPower
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || target != Owner || CombatState is null)
        {
            return;
        }

        foreach (Creature shadow in CombatState.Enemies
                     .Where(c => c.Monster is OldDayShadow && c.IsAlive && c.HasPower<WeavingShadowPower>())
                     .ToList())
        {
            await CreatureCmd.Kill(shadow, force: true);
        }
    }
}

public sealed class WeavingShadowPower : DongmingPower
{
    private bool _relaying;
    private bool _suppressRelay;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool OwnerIsSecondaryEnemy => true;

    public void SuppressRelay()
    {
        _suppressRelay = true;
    }

    public override async Task AfterCurrentHpChanged(
        Creature creature,
        decimal delta)
    {
        if (_suppressRelay || _relaying || creature != Owner || delta >= 0m || CombatState is null)
        {
            return;
        }

        Creature markedTarget = CombatState.Enemies
            .FirstOrDefault(c => c.HasPower<WeavingMarkPower>() && c != Owner && c.IsAlive);
        if (markedTarget is null)
        {
            return;
        }

        _relaying = true;
        try
        {
            await CreatureCmd.Damage(
                new BlockingPlayerChoiceContext(),
                markedTarget,
                -delta,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                Owner);
        }
        finally
        {
            _relaying = false;
        }
    }
}

