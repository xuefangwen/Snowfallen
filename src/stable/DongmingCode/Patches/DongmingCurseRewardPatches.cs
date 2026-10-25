using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Runs;
using SnowfallenMod.DongmingCode.Character;
using SnowfallenMod.DongmingCode.CardPools;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Cards;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(CardCreationOptions), nameof(CardCreationOptions.GetPossibleCards))]
public static class DongmingCurseCardPoolPatch
{
    public static void Postfix(
        CardCreationOptions __instance,
        Player player,
        ref IEnumerable<CardModel> __result)
    {
        if (player.Character is not Dongming)
        {
            return;
        }

        HashSet<ModelId> excludedStarterIds =
        [
            ModelDb.Card<ZhouJi>().Id,
            ModelDb.Card<RangZai>().Id,
            ModelDb.Card<CanShi>().Id
        ];
        __result = __result
            .Where(card => !excludedStarterIds.Contains(card.Id))
            .Where(card => card is not Day0Curse)
            .Where(card => card is not PurifiedVanillaCard)
            .ToArray();

        if (
            __instance.Source != CardCreationSource.Encounter ||
            !__instance.CardPools.Contains(ModelDb.CardPool<DongmingCardPool>()))
        {
            return;
        }

        __result = __result
            .Concat(ModelDb.AllCards.Where(card => card.Type == CardType.Curse))
            .Where(card => card is not Day0Curse)
            .Where(card => !typeof(PurifiedVanillaCard).IsAssignableFrom(card.GetType()))
            .Where(card => !excludedStarterIds.Contains(card.Id))
            .DistinctBy(card => card.Id)
            .ToArray();
    }
}

[HarmonyPatch(typeof(CardFactory), "RollForRarity")]
public static class DongmingCurseRarityPatch
{
    private static readonly System.Reflection.MethodInfo GetBaseOddsMethod =
        AccessTools.Method(typeof(CardRarityOdds), "GetBaseOdds");

    public static void Postfix(
        object[] __args,
        ref CardRarity __result)
    {
        Player player = (Player)__args[0];
        CardRarityOddsType rarityOdds = (CardRarityOddsType)__args[1];
        CardCreationSource source = (CardCreationSource)__args[2];
        HashSet<CardRarity> allowedRarities =
            (HashSet<CardRarity>)__args[3];

        if (player.Character is not Dongming ||
            source != CardCreationSource.Encounter ||
            !allowedRarities.Contains(CardRarity.Curse) ||
            !allowedRarities.Contains(CardRarity.Uncommon))
        {
            return;
        }

        float commonWeight = GetBaseOdds(
            player.PlayerOdds.CardRarity, rarityOdds, CardRarity.Common);
        float uncommonWeight = GetBaseOdds(
            player.PlayerOdds.CardRarity, rarityOdds, CardRarity.Uncommon);
        float rareWeight = GetBaseOdds(
            player.PlayerOdds.CardRarity, rarityOdds, CardRarity.Rare);
        float expandedWeight = commonWeight + uncommonWeight * 2f + rareWeight;
        if (uncommonWeight > 0f &&
            player.PlayerRng.Rewards.NextFloat(expandedWeight) < uncommonWeight)
        {
            __result = CardRarity.Curse;
        }
    }

    private static float GetBaseOdds(
        CardRarityOdds odds,
        CardRarityOddsType oddsType,
        CardRarity rarity)
    {
        return (float)GetBaseOddsMethod.Invoke(odds, [oddsType, rarity]);
    }
}
