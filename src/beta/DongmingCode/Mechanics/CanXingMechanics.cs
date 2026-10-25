using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Mechanics;

/// <summary>
/// Defers CanXing's Vigor trigger until the complete card play has resolved.
/// The bookkeeping is process-local only; no original card or persistent state
/// is modified, so multiplayer state remains command-driven.
/// </summary>
public static class CanXingMechanics
{
	private static readonly Dictionary<Player, CardModel> ActiveResourceSpends =
		new(ReferenceEqualityComparer.Instance);
	private static readonly Dictionary<CardModel, Dictionary<Creature, decimal>> PendingContributions =
		new(ReferenceEqualityComparer.Instance);

	public static void BeginResourceSpend(CardModel card)
	{
		if (card.Owner is not null)
		{
			ActiveResourceSpends[card.Owner] = card;
		}
	}

	public static bool TryDeferStarsSpent(
		CanXingPower power,
		int amount,
		Player player)
	{
		if (amount <= 0 ||
			!ActiveResourceSpends.TryGetValue(player, out CardModel card) ||
			power.Owner is null)
		{
			return false;
		}

		if (!PendingContributions.TryGetValue(
				card,
				out Dictionary<Creature, decimal> contributions))
		{
			contributions = new(ReferenceEqualityComparer.Instance);
			PendingContributions[card] = contributions;
		}

		contributions.TryGetValue(power.Owner, out decimal pending);
		contributions[power.Owner] = pending + amount * power.Amount;
		return true;
	}

	public static async Task<(int Energy, int Stars)> CompleteResourceSpend(
		Task<(int Energy, int Stars)> resourceTask,
		CardModel card)
	{
		try
		{
			return await resourceTask;
		}
		finally
		{
			if (card.Owner is not null &&
				ActiveResourceSpends.TryGetValue(card.Owner, out CardModel active) &&
				ReferenceEquals(active, card))
			{
				ActiveResourceSpends.Remove(card.Owner);
			}
		}
	}

	public static async Task CompleteCardPlay(
		Task cardPlayTask,
		CardModel card,
		PlayerChoiceContext choiceContext)
	{
		try
		{
			await cardPlayTask;
		}
		catch
		{
			PendingContributions.Remove(card);
			throw;
		}

		if (!PendingContributions.Remove(
				card,
				out Dictionary<Creature, decimal> contributions))
		{
			return;
		}

		foreach ((Creature target, decimal amount) in contributions)
		{
			if (target.IsAlive)
			{
				await PowerCmd.Apply<VigorPower>(
					choiceContext,
					target,
					amount,
					target,
					null);
			}
		}
	}
}

