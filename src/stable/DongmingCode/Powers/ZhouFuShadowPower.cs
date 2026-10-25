using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class ZhouFuShadowPower : DongmingPower
{
    private bool _dying;
    private bool _relayingDamage;
    private bool _hasPendingIncomingDamage;
    private int _hpBeforeIncomingDamage;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool OwnerIsSecondaryEnemy => true;
    [SavedProperty] public uint BoundTargetId { get; private set; }
    [SavedProperty] public int InitialMaxHp { get; private set; }
    public void Configure(uint targetId, int initialMaxHp) { AssertMutable(); BoundTargetId = targetId; InitialMaxHp = initialMaxHp; }
    public async Task LoseLifeForAction()
    {
        if (!Owner.IsAlive) return;
        decimal loss = Math.Max(1m, Math.Floor(Owner.CurrentHp * 0.10m));
        await CreatureCmd.Damage(new BlockingPlayerChoiceContext(), Owner, loss, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, Owner, null);
    }
    public override Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        MegaCrit.Sts2.Core.Models.CardModel cardSource)
    {
        if (target == Owner && !_relayingDamage)
        {
            _hpBeforeIncomingDamage = Owner.CurrentHp;
            _hasPendingIncomingDamage = true;
        }

        return Task.CompletedTask;
    }
    public override async Task AfterCurrentHpChanged(
        Creature creature,
        decimal delta)
    {
        if (_relayingDamage ||
            creature != Owner ||
            delta >= 0m ||
            CombatState is null)
        {
            return;
        }

        _hasPendingIncomingDamage = false;

        Creature target = CombatState.Enemies.FirstOrDefault(
            x => x.CombatId == BoundTargetId && x.IsAlive);
        if (target is null)
        {
            return;
        }

        _relayingDamage = true;
        try
        {
            await CreatureCmd.Damage(
                new BlockingPlayerChoiceContext(),
                target,
                -delta,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                Owner,
                null);
        }
        finally
        {
            _relayingDamage = false;
        }
    }
    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, bool prevented, float anim)
    {
        if (_dying || prevented || creature != Owner || CombatState is null) return;
        Creature target = CombatState.Enemies.FirstOrDefault(x => x.CombatId == BoundTargetId && x.IsAlive);
        if (target is null) return;
        _dying = true;
        try
        {
            if (_hasPendingIncomingDamage)
            {
                int lethalLifeLoss = Math.Max(
                    0,
                    _hpBeforeIncomingDamage - Owner.CurrentHp);
                _hasPendingIncomingDamage = false;
                if (lethalLifeLoss > 0)
                {
                    await CreatureCmd.Damage(
                        context,
                        target,
                        lethalLifeLoss,
                        ValueProp.Unblockable | ValueProp.Unpowered |
                        ValueProp.Move,
                        null,
                        null);
                }
            }

            if (!target.IsAlive)
            {
                return;
            }

            await CreatureCmd.Damage(
                context,
                target,
                InitialMaxHp,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                null,
                null);

            if (!target.IsAlive)
            {
                return;
            }

        }
        finally
        {
            _dying = false;
        }
    }
}
