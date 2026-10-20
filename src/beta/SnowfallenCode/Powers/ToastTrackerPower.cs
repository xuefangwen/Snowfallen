using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace SnowfallenMod.SnowfallenCode.Powers;

public class ToastTrackerPower : SnowfallenPower
{
    public bool EnergyClaimed { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;

    public static async Task<ToastTrackerPower> Ensure(
        PlayerChoiceContext context,
        Creature owner,
        CardModel source)
    {
        ToastTrackerPower tracker = owner.Powers
            .OfType<ToastTrackerPower>()
            .FirstOrDefault();
        if (tracker is not null)
        {
            return tracker;
        }

        await PowerCmd.Apply<ToastTrackerPower>(
            context,
            owner,
            1m,
            owner,
            source);
        return owner.Powers.OfType<ToastTrackerPower>().First();
    }

    public bool TryClaimEnergy()
    {
        if (EnergyClaimed)
        {
            return false;
        }

        EnergyClaimed = true;
        InvokeDisplayAmountChanged();
        return true;
    }

    public override Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player == Owner.Player)
        {
            EnergyClaimed = false;
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }
}

