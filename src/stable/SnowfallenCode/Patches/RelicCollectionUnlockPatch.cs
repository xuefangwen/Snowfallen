using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace SnowfallenMod.SnowfallenCode.Patches;

/// <summary>
/// 1) 百科全书的"解锁"只认原版存档的 UnlockState.Relics，模组遗物不在其中，
///    所以之前被画成"锁定"。这里仅把本模组遗物的 Locked 改成 NotSeen。
/// </summary>
[HarmonyPatch(typeof(NRelicCollectionEntry), nameof(NRelicCollectionEntry.Create))]
internal static class RelicCollectionUnlockPatch
{
    [HarmonyPrefix]
    private static void Prefix(RelicModel relic, ref ModelVisibility visibility)
    {
        if (relic is null || visibility != ModelVisibility.Locked)
        {
            return;
        }

        if (ModRelicUtil.IsModRelic(relic))
        {
            // 模组遗物不参与原版的解锁/见过记录，直接按"已解锁且已知"显示
            visibility = ModelVisibility.Visible;
        }
    }
}

/// <summary>
/// 2) 非锁定条目会调用 relic.ToMutable()，而 ToMutable() 内部先 AssertCanonical()。
///    模组遗物在百科里拿到的是可变实例，于是抛 MutableModelException、条目变空框。
///    这里只对"本模组的可变遗物"直接返回自身，等同完成转换；原版遗物走原逻辑。
/// </summary>
[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.ToMutable))]
internal static class ModRelicMutableCompatPatch
{
    [HarmonyPrefix]
    private static bool Prefix(RelicModel __instance, ref AbstractModel __result)
    {
        if (__instance is null || !__instance.IsMutable || !ModRelicUtil.IsModRelic(__instance))
        {
            return true;
        }

        __result = __instance;
        return false;
    }
}

internal static class ModRelicUtil
{
    internal static bool IsModRelic(RelicModel relic) =>
        relic.GetType().Assembly == typeof(ModRelicUtil).Assembly;
}

/// <summary>
/// 3) 遗物详情页 NInspectRelicScreen.UpdateRelicDisplay() 会用
///    _allUnlockedRelics.Contains(relic.CanonicalInstance) 决定是否显示真实
///    名称/描述；该集合来自原版 unlockState.Relics，模组遗物不在其中，
///    于是详情页退化成占位样式（显示成默认遗物）。
///   这里在 Open() 之后把本模组遗物的 CanonicalInstance 补进去，原版遗物不受影响。
/// </summary>
[HarmonyPatch(typeof(NInspectRelicScreen), nameof(NInspectRelicScreen.Open))]
internal static class RelicInspectUnlockPatch
{
    private static readonly FieldInfo UnlockedSetField =
        AccessTools.Field(typeof(NInspectRelicScreen), "_allUnlockedRelics");
    private static readonly FieldInfo IndexField =
        AccessTools.Field(typeof(NInspectRelicScreen), "_index");
    private static readonly MethodInfo SetRelicMethod =
        AccessTools.Method(typeof(NInspectRelicScreen), "SetRelic");

    [HarmonyPostfix]
    private static void Postfix(NInspectRelicScreen __instance,
                                IReadOnlyList<RelicModel> relics,
                                RelicModel relic)
    {
        // 1) 把本模组遗物补进"已解锁"集合
        if (UnlockedSetField?.GetValue(__instance) is HashSet<RelicModel> unlockedSet)
        {
            foreach (RelicModel candidate in ModelDb.AllRelics)
            {
                if (ModRelicUtil.IsModRelic(candidate))
                {
                    unlockedSet.Add(candidate.CanonicalInstance);
                }
            }
        }

        // 2) 修正索引：条目持有的模型与传入列表可能不是同一个实例，
        //    原版按引用查不到就会停在 0 号（于是显示成燃烧之血）。
        if (relic is null || relics is null || IndexField is null || SetRelicMethod is null)
        {
            return;
        }

        for (int i = 0; i < relics.Count; i++)
        {
            RelicModel candidate = relics[i];
            if (ReferenceEquals(candidate, relic) ||
                (candidate is not null && Equals(candidate.Id, relic.Id)))
            {
                IndexField.SetValue(__instance, i);
                SetRelicMethod.Invoke(__instance, new object[] { i });
                break;
            }
        }
    }
}
