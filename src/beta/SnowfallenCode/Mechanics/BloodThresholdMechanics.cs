using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

public static class BloodThresholdMechanics
{
    // Only cards whose play/cost/replay rules actually use the threshold override.
    // SoulSeparationArt reacts to real HP crossings; Mystic Fox does not override it.
    internal static bool UsesMysticFoxForm(CardModel card) => card is
        DrawingTheBowstring or FadingCandle or FollowingTheEastWind or InkDye or
        LifeBearingJewel or MysticImmortalGuard or Precipice or Rejuvenation or
        ReverseBlood or ShatteredSnowSurgingWaves or Tempering or Toast or
        TransformingToReachFar or VastWaters or WaterDuckweedSplendor;

    public static bool IsAllThresholdsActive(Creature creature) =>
        creature.Powers.OfType<MysticFoxFormPower>().Any(power => power.Amount > 0m);

    public static bool IsHigh(Creature creature) =>
        IsAllThresholdsActive(creature) ||
        (creature.MaxHp > 0 &&
         (decimal)creature.CurrentHp / creature.MaxHp >= 0.7m);

    public static bool IsLow(Creature creature) =>
        IsAllThresholdsActive(creature) ||
        (creature.MaxHp > 0 &&
         (decimal)creature.CurrentHp / creature.MaxHp <= 0.4m);

    public static bool IsNormal(Creature creature) =>
        IsAllThresholdsActive(creature) ||
        (creature.MaxHp > 0 &&
         (decimal)creature.CurrentHp / creature.MaxHp > 0.4m &&
         (decimal)creature.CurrentHp / creature.MaxHp < 0.7m);
}

