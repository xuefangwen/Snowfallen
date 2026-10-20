using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class AcceptMandatePowerBase : SnowfallenPower
{
    private bool _triggered;

    protected abstract decimal HpLoss { get; }
    protected abstract decimal Buffer { get; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override bool ShouldDieLate(Creature creature)
    {
        return creature != Owner;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();

        await WaterBreathGainSuppression.Run(
            creature,
            () => CreatureCmd.SetCurrentHp(creature, 1m));

        if (!_triggered && Applier is not null && !Applier.IsDead)
        {
            _triggered = true;
            var context = new BlockingPlayerChoiceContext();
            await SnowfallenMod.SnowfallenCode.Mechanics.SnowfallenHpLoss.Apply(
                context,
                Applier,
                HpLoss,
                DamageProps.cardHpLoss,
                null,
                null);

            if (Buffer > 0m && !Applier.IsDead)
            {
                await PowerCmd.Apply<BufferPower>(
                    context,
                    Applier,
                    Buffer,
                    Applier,
                    null);
            }
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public class AcceptMandatePower : AcceptMandatePowerBase
{
    protected override decimal HpLoss => 12m;
    protected override decimal Buffer => 0m;
}

public class UpgradedAcceptMandatePower : AcceptMandatePowerBase
{
    protected override decimal HpLoss => 8m;
    protected override decimal Buffer => 1m;
}

public class AcceptMandateUsedPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
}
