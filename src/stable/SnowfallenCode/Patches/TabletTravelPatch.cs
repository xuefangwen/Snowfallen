using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using SnowfallenMod.SnowfallenCode.Relics;

namespace SnowfallenMod.SnowfallenCode.Patches;

internal static class TabletTravelState
{
    private static readonly FieldInfo CoordField =
        AccessTools.Field(typeof(MapPoint), "coord");

    private static readonly FieldInfo RowField =
        AccessTools.Field(typeof(MapCoord), "row");

    private static readonly FieldInfo MapPointDictionaryField =
        AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");

    private static readonly MethodInfo RecalculateTravelabilityMethod =
        AccessTools.Method(typeof(NMapScreen), "RecalculateTravelability");

    public static Tablet ActiveTablet { get; private set; }

    public static bool IsArmed(Tablet tablet)
    {
        return ActiveTablet == tablet;
    }

    public static bool TryArm(Tablet tablet)
    {
        IRunState runState = tablet.Owner?.RunState;
        if (!tablet.Available ||
            runState is null ||
            runState.Players.Count != 1 ||
            !runState.Map.GetAllMapPoints().Any(point => IsEligible(point, runState)))
        {
            return false;
        }

        ActiveTablet = tablet;
        return true;
    }

    public static void Reset()
    {
        ActiveTablet = null;
    }

    public static bool IsEligible(MapPoint point)
    {
        IRunState runState = ActiveTablet?.Owner?.RunState;
        return ActiveTablet is not null &&
               ActiveTablet.Available &&
               runState is not null &&
               IsEligible(point, runState);
    }

    public static void MarkSelected(MapPoint point)
    {
        if (!IsEligible(point))
        {
            Reset();
            return;
        }

        Tablet tablet = ActiveTablet;
        tablet.MarkTravelPending();
        Reset();
    }

    public static void RefreshEligibleNodes(NMapScreen screen)
    {
        if (ActiveTablet is null)
        {
            return;
        }

        var nodes = MapPointDictionaryField.GetValue(screen) as
            Dictionary<MapCoord, NMapPoint>;
        if (nodes is null)
        {
            return;
        }

        foreach (NMapPoint node in nodes.Values)
        {
            if (IsEligible(node.Point))
            {
                node.State = MapPointState.Travelable;
            }
        }
    }

    public static void RecalculateTravelability(NMapScreen screen)
    {
        RecalculateTravelabilityMethod.Invoke(screen, null);
    }

    private static bool IsEligible(MapPoint point, IRunState runState)
    {
        if (point.PointType != MapPointType.Elite)
        {
            return false;
        }

        MapCoord coord = GetCoord(point);
        int currentRow = runState.CurrentMapCoord.HasValue
            ? GetRow(runState.CurrentMapCoord.Value)
            : -1;
        return GetRow(coord) > currentRow;
    }

    private static MapCoord GetCoord(MapPoint point)
    {
        return (MapCoord)CoordField.GetValue(point);
    }

    private static int GetRow(MapCoord coord)
    {
        object boxed = coord;
        return (int)RowField.GetValue(boxed);
    }
}

[HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
public static class TabletRelicClickPatch
{
    [HarmonyPrefix]
    public static bool Prefix(RelicModel model)
    {
        if (model is not Tablet tablet ||
            !TabletTravelState.TryArm(tablet))
        {
            return true;
        }

        NMapScreen screen = NMapScreen.Instance;
        if (screen is null)
        {
            TabletTravelState.Reset();
            return true;
        }

        screen.Open(true);
        screen.SetTravelEnabled(true);
        TabletTravelState.RecalculateTravelability(screen);
        TabletTravelState.RefreshEligibleNodes(screen);
        return false;
    }
}

[HarmonyPatch(typeof(NMapScreen), "RecalculateTravelability")]
public static class TabletMapRefreshPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMapScreen __instance)
    {
        TabletTravelState.RefreshEligibleNodes(__instance);
    }
}

[HarmonyPatch(typeof(NNormalMapPoint), "OnPress")]
public static class TabletMapPointPressPatch
{
    [HarmonyPrefix]
    public static void Prefix(NNormalMapPoint __instance)
    {
        if (TabletTravelState.IsEligible(__instance.Point))
        {
            TabletTravelState.MarkSelected(__instance.Point);
        }
        else
        {
            TabletTravelState.Reset();
        }
    }
}

[HarmonyPatch(typeof(NMapScreen), "OnBackButtonPressed")]
public static class TabletMapCancelPatch
{
    [HarmonyPrefix]
    public static void Prefix(NButton _)
    {
        TabletTravelState.Reset();
    }
}
