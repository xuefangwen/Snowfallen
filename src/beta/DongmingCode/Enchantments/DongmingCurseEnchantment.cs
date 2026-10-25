using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.Enchantments;

public class DongmingCurseEnchantment : CustomEnchantmentModel
{
	protected override string CustomIconPath =>
		"curse_power.png".PowerImagePath();
	public override bool HasExtraCardText => true;
	public override bool ShouldGlowRed => true;

	public static bool CanEnchantTarget(CardModel card) =>
		card.Type != CardType.Curse &&
		card.Enchantment is null &&
		!card.Keywords.Contains(CardKeyword.Unplayable);

	public override bool CanEnchant(CardModel card)
	{
		// Lock Sui intentionally enchants Curse cards, which the base game
		// rejects before custom enchantment restrictions are evaluated.
		if (card.Type == CardType.Curse)
		{
			return card.Enchantment is null &&
				   card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard;
		}

		return CanEnchantTarget(card) && base.CanEnchant(card);
	}

	public override Task OnPlay(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay) =>
		CurseEnchantmentMechanics.Trigger(choiceContext, cardPlay.Card);

	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (!ReferenceEquals(card, Card) ||
			CurseEnchantmentMechanics.ConsumeExhaustedFromPlay(card))
		{
			return;
		}

		int triggerCount = 1 + YuanShaMechanics.GetExtraTriggerCount(card);
		for (int index = 0; index < triggerCount; index++)
		{
			await CurseEnchantmentMechanics.Trigger(choiceContext, card);
		}
	}

}

