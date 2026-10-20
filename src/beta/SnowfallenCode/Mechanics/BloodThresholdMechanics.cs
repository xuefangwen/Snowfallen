using MegaCrit.Sts2.Core.Entities.Creatures;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Mechanics;

public static class BloodThresholdMechanics
{
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

