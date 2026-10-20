using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Relics;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
public static class TouchOfOrobasSnowfallenPatch
{
    public static void Postfix(RelicModel starterRelic, ref RelicModel __result)
    {
        if (starterRelic is WaterBottlePendant)
        {
            __result = ModelDb.Relic<MysticImmortalMark>().ToMutable();
        }
    }
}

[HarmonyPatch(typeof(ArchaicTooth))]
public static class ArchaicToothSnowfallenPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(ArchaicTooth.SetupForPlayer))]
    public static bool SetupForPlayerPrefix(
        ArchaicTooth __instance,
        Player player,
        ref bool __result)
    {
        CardModel starter = player.Deck.Cards
            .FirstOrDefault(card => card is SongOfFloatingClouds);
        if (starter is null)
        {
            return true;
        }

        CardModel ancient = CreateAncientVersion(starter);
        __instance.SetupForTests(
            starter.ToSerializable(),
            ancient.ToSerializable());
        __result = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(ArchaicTooth.AfterObtained))]
    public static bool AfterObtainedPrefix(
        ArchaicTooth __instance,
        ref Task __result)
    {
        CardModel starter = __instance.Owner.Deck.Cards
            .FirstOrDefault(card => card is SongOfFloatingClouds);
        if (starter is null)
        {
            return true;
        }

        __result = CardCmd.Transform(starter, CreateAncientVersion(starter));
        return false;
    }

    private static CardModel CreateAncientVersion(CardModel starter)
    {
        CardModel ancient = starter.Owner.RunState.CreateCard<FrostForestDrunk>(
            starter.Owner);
        if (starter.IsUpgraded)
        {
            CardCmd.Upgrade(ancient);
        }
        if (starter.Enchantment is not null)
        {
            EnchantmentModel enchantment =
                (EnchantmentModel)starter.Enchantment.MutableClone();
            CardCmd.Enchant(enchantment, ancient, enchantment.Amount);
        }
        return ancient;
    }
}

[HarmonyPatch(typeof(DustyTome))]
public static class DustyTomeSnowfallenPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(DustyTome.SetupForPlayer))]
    public static bool SetupForPlayerPrefix(DustyTome __instance, Player player)
    {
        if (player.Character is not SnowfallenCharacter)
        {
            return true;
        }

        __instance.AncientCard = ModelDb.Card<RavenCold>().Id;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(nameof(DustyTome.AfterObtained))]
    public static bool AfterObtainedPrefix(
        DustyTome __instance,
        ref Task __result)
    {
        if (__instance.Owner.Character is not SnowfallenCharacter)
        {
            return true;
        }

        __result = GrantRavenCold(__instance);
        return false;
    }

    private static async Task GrantRavenCold(DustyTome tome)
    {
        CardModel ancient = tome.Owner.RunState.CreateCard<RavenCold>(tome.Owner);
        CardCmd.Upgrade(ancient, CardPreviewStyle.HorizontalLayout);
        CardPileAddResult result = await CardPileCmd.Add(
            ancient,
            PileType.Deck,
            CardPilePosition.Bottom,
            null,
            false);
        CardCmd.PreviewCardPileAdd(
            result,
            2f,
            CardPreviewStyle.HorizontalLayout);
    }
}

