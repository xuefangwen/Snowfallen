using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using SnowfallenMod.DongmingCode.Relics;

namespace SnowfallenMod.DongmingCode.Patches;

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold))]
public static class DongmingGoldSpendingPatch
{
    public static void Prefix(Player player, out int __state)
    {
        __state = player.Gold;
    }

    public static void Postfix(
        Player player,
        GoldLossType goldLossType,
        int __state)
    {
        if (goldLossType != GoldLossType.Spent ||
            player.GetRelic<HuLaoBanCheck>() is not HuLaoBanCheck relic)
        {
            return;
        }

        relic.RecordSpentGold(Math.Max(0, __state - player.Gold));
    }
}

