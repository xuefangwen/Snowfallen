using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.SnowfallenCode.Extensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Logging;
using SnowfallenMod.SnowfallenCode.Animation;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class MysticFoxTransformationPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        await PowerCmd.Apply<MysticFoxFormPower>(
            choiceContext,
            Owner,
            Amount,
            Owner,
            null);
        await PowerCmd.Remove(this);
    }
}

// Kept loadable for old combat saves which may still reference this model id.
public class UpgradedMysticFoxTransformationPower :
    MysticFoxTransformationPower
{
}

public class MysticFoxFormPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath =>
        "mystic_fox_transformation_power.png".PowerImagePath();
    public override string CustomBigIconPath =>
        "mystic_fox_transformation_power.png".BigPowerImagePath();

    public override Task AfterApplied(Creature applier, CardModel cardSource)
    {
        MysticFoxFormVfx.Ensure(Owner);
        return Task.CompletedTask;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Amount > 0 && cardPlay.Card.Owner == Owner.Player &&
            BloodThresholdMechanics.UsesMysticFoxForm(cardPlay.Card))
        {
            var effect = MysticFoxFormVfx.Ensure(Owner);
            if (effect is not null)
            {
                effect.OnEffectTriggered();
                Log.Info($"[Snowfallen.MysticFox] Outward water triggered: {cardPlay.Card.GetType().Name}, playIndex={cardPlay.PlayIndex}");
            }
            else
            {
                Log.Warn($"[Snowfallen.MysticFox] Threshold card played but visual unavailable: {cardPlay.Card.GetType().Name}");
            }
        }
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        MysticFoxFormVfx.Remove(oldOwner);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        MysticFoxFormVfx.Remove(Owner);
        return Task.CompletedTask;
    }
}

public class DecayPower : SnowfallenPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

