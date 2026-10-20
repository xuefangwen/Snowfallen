using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Patches;

/// <summary>
/// 逆血：低血（生命值不高于最大生命的 40%，即明兆低血线）时获得重放1。
///
/// BaseReplayCount 是"打出这张牌时额外执行几次逻辑"，在每次打出时取值，
/// 所以血线动态变化会自然体现为"有时有重放、有时没有"；低血时整张牌会
/// 再完整结算一次（失去生命与回复都会再走一遍）。
///
/// 图鉴/详情用的是 canonical（只读）实例，访问 Owner 会触发 AssertMutable，
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_BaseReplayCount")]
public static class ReverseBloodReplayPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref int __result)
    {
        if (__instance is not ReverseBlood card ||
            !card.IsMutable ||
            card.Owner?.Creature is not { } creature ||
            creature.MaxHp <= 0)
        {
            return;
        }

        if (BloodThresholdMechanics.IsLow(creature))
        {
            __result += 1;
        }
    }
}

