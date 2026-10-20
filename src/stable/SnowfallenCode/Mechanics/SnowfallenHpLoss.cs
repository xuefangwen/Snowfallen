using System.Threading;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

// Transient execution context only: no saved or network-consumed state.
internal static class SnowfallenHpLoss
{
    private sealed record Payment(Creature Target, ValueProp Props, Creature Dealer, CardModel Source);
    private static readonly AsyncLocal<Payment> Current = new();

    internal static bool BypassesBuffer(
        Creature target, ValueProp props, Creature dealer, CardModel source)
    {
        Payment payment = Current.Value;
        return payment is not null &&
            ReferenceEquals(payment.Target, target) &&
            payment.Props == props &&
            ReferenceEquals(payment.Source, source) &&
            ReferenceEquals(dealer, payment.Dealer);
    }

    public static Task Apply(
        PlayerChoiceContext context, Creature target, decimal amount,
        ValueProp props, CardModel source) =>
        Apply(context, target, amount, props, source.Owner.Creature, source);

    public static async Task Apply(
        PlayerChoiceContext context, Creature target, decimal amount,
        ValueProp props, Creature dealer, CardModel source = null)
    {
        Payment previous = Current.Value;
        Current.Value = new Payment(target, props, dealer, source);
        try
        {
            // Preserve the normal HP-loss pipeline and all other hooks.
            await CreatureCmd.Damage(context, target, amount, props, dealer, source);
        }
        finally
        {
            Current.Value = previous;
        }
    }
}
