using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Models.Powers;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.CardPools;
using SnowfallenMod.DongmingCode.PotionPools;
using SnowfallenMod.DongmingCode.RelicPools;
using SnowfallenMod.DongmingCode.Relics;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.Character;

/// <summary>
/// Dongming's playable character shell. Until custom art is supplied, BaseLib
/// resolves the original Necrobinder assets through PlaceholderID.
/// </summary>
public class Dongming : PlaceholderCharacterModel
{
	public const string CharacterId = "dongming";
	public static readonly Color Color = new("EE82EE");

	public override string CustomCharacterSelectBg =>
		"res://Snowfallen/Scenes/CharacterSelect/dongming_character_select_bg.tscn";
	public override string PlaceholderID => "necrobinder";
	public override string CustomIconPath =>
		"res://Snowfallen/Scenes/UI/dongming_icon.tscn";
	public override string CustomIconTexturePath =>
		"character_icon_dongming.png".CharacterUiPath();
	public override string CustomIconOutlineTexturePath =>
		"character_icon_dongming_outline.png".CharacterUiPath();
	public override string CustomCharacterSelectIconPath =>
		"char_select_dongming.png".CharacterUiPath();
	public override string CustomCharacterSelectLockedIconPath =>
		"char_select_dongming_locked.png".CharacterUiPath();
	public override string CustomMapMarkerPath =>
		"character_icon_dongming.png".CharacterUiPath();
	public override string CustomArmPointingTexturePath =>
		"res://images/ui/hands/multiplayer_hand_dongming_point.png";
	public override string CustomArmRockTexturePath =>
		"res://images/ui/hands/multiplayer_hand_dongming_rock.png";
	public override string CustomArmPaperTexturePath =>
		"res://images/ui/hands/multiplayer_hand_dongming_paper.png";
	public override string CustomArmScissorsTexturePath =>
		"res://images/ui/hands/multiplayer_hand_dongming_scissors.png";

	protected override IEnumerable<string> ExtraAssetPaths =>
		base.ExtraAssetPaths.Concat(
		[
			"res://images/ui/hands/multiplayer_hand_dongming_point.png",
			"res://images/ui/hands/multiplayer_hand_dongming_rock.png",
			"res://images/ui/hands/multiplayer_hand_dongming_paper.png",
            "res://images/ui/hands/multiplayer_hand_dongming_scissors.png"
		]);

	// CharacterModel opts out of combat hooks by default. Dongming's
	// per-card moon-phase listeners are owned by the character model itself.
	public override bool ShouldReceiveCombatHooks => true;
	public override Color NameColor => Color;
	public override CharacterGender Gender => CharacterGender.Feminine;
	public override int StartingHp => 75;
	public override int StartingGold => 99;
	public override bool ShouldAlwaysShowStarCounter => true;
	public override float AttackAnimDelay => 0.15f;
	public override float CastAnimDelay => 0.25f;
	public override Color EnergyLabelOutlineColor => new("702D6FFF");
	public override CustomEnergyCounter? CustomEnergyCounter => new CustomEnergyCounter(
		layer => $"res://Snowfallen/Images/Charui/DongmingEnergy/combat_orb_layers/dongming_orb_layer_{layer}.png",
		new Color("135f91"),
		new Color("75d9ff"));
	public override Color DialogueColor => new("6B4658");
	// 天蓝：比原版故障机器人的 0D638C 更亮（亮度 174 vs 84），且色相明显不同
	public override Color MapDrawingColor => new("4FC3F7");
	public override Color RemoteTargetingLineColor => new("FD98C9FF");
	public override Color RemoteTargetingLineOutline => new("702D6FFF");
	public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

	public override IEnumerable<CardModel> StartingDeck =>
	[
		ModelDb.Card<DongmingStrike>(),
		ModelDb.Card<DongmingStrike>(),
		ModelDb.Card<DongmingStrike>(),
		ModelDb.Card<DongmingStrike>(),
		ModelDb.Card<DongmingDefend>(),
		ModelDb.Card<DongmingDefend>(),
		ModelDb.Card<DongmingDefend>(),
		ModelDb.Card<DongmingDefend>(),
		ModelDb.Card<ZhouJi>(),
		ModelDb.Card<RangZai>()
	];

	public override IReadOnlyList<RelicModel> StartingRelics =>
	[
		ModelDb.Relic<DarkMoonNecklace>()
	];

	public override CardPoolModel CardPool => ModelDb.CardPool<DongmingCardPool>();
	public override RelicPoolModel RelicPool => ModelDb.RelicPool<DongmingRelicPool>();
	public override PotionPoolModel PotionPool => ModelDb.PotionPool<DongmingPotionPool>();

	public override IEnumerable<CardModel> ModifyMerchantCardPool(
		Player player,
		IEnumerable<CardModel> cards)
	{
		HashSet<ModelId> excludedStarterIds =
		[
			ModelDb.Card<ZhouJi>().Id,
			ModelDb.Card<RangZai>().Id,
			ModelDb.Card<CanShi>().Id
		];
		return cards
			.Where(card => !excludedStarterIds.Contains(card.Id))
			.Where(card => card is not PurifiedVanillaCard);
	}

	public override List<string> GetArchitectAttackVfx() =>
	[
		"vfx/vfx_thrash",
		"vfx/vfx_heavy_blunt",
		"vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact"
	];

	public override async Task AfterCardPlayed(
		PlayerChoiceContext choiceContext,
		CardPlay cardPlay)
	{
		Player owner = cardPlay.Card.Owner;
		if (owner?.Character != this)
		{
			return;
		}

		if (cardPlay.Card is PurifiedNormality normality)
		{
			await PurifiedVanillaEffects.SyncTianZiPower(
				choiceContext,
				normality.Owner);
		}

	}

	public override async Task AfterCardDiscarded(
		PlayerChoiceContext choiceContext,
		CardModel card)
	{
		// 利息（PurifiedDebt）的计数已改到 DebtDiscardPilePatch：
		// 原版只在 CardCmd.Discard() 里触发 AfterCardDiscarded，
		// 回合结束的自动弃牌不会走这里，会漏计数；
		// 若这里保留，用效果弃牌时又会与牌堆钩子重复 +5。

		if (card.Owner?.Character == this && card is PurifiedNormality)
		{
			await PurifiedVanillaEffects.SyncTianZiPower(choiceContext, card.Owner);
		}
	}

	public override async Task BeforeSideTurnStart(
		PlayerChoiceContext choiceContext,
		CombatSide side,
		IReadOnlyList<Creature> creatures,
		ICombatState combatState)
	{
		foreach (Player owner in creatures
			.Select(creature => creature.Player)
			.Where(player => player?.Character == this)
			.Cast<Player>())
		{
			foreach (PurifiedNormality normality in owner.PlayerCombatState.AllCards
				.OfType<PurifiedNormality>())
			{
				PurifiedVanillaEffects.ResetNormalityCounter(normality);
			}
		}
	}

	public override async Task AfterCardChangedPiles(
		CardModel card,
		PileType oldPileType,
		AbstractModel clonedBy)
	{
		if (card.Owner?.Character != this)
		{
			return;
		}

		if (card is PurifiedNormality)
		{
			await PurifiedVanillaEffects.SyncTianZiPower(
				new BlockingPlayerChoiceContext(),
				card.Owner);
		}
	}

	public override async Task AfterCardExhausted(
		PlayerChoiceContext choiceContext,
		CardModel card,
		bool causedByEthereal)
	{
		if (card.Owner?.Character == this && card is PurifiedNormality)
		{
			await PurifiedVanillaEffects.SyncTianZiPower(choiceContext, card.Owner);
		}

		if (card.Owner?.Character == this &&
			card is PurifiedVanillaCard purified &&
			!PurificationMechanics.ConsumeTransforming(card))
		{
			bool exhaustedFromHand =
				PurificationMechanics.ConsumeExhaustedFromHand(card);
			int triggerCount = 1 +
				YuanShaMechanics.GetExtraTriggerCount(purified);
			for (int index = 0; index < triggerCount; index++)
			{
				await PurifiedVanillaEffects.AfterExhausted(
					choiceContext,
					purified,
					exhaustedFromHand);
			}
		}
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		return Task.CompletedTask;
	}
}
