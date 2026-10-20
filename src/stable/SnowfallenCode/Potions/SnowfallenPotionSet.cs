using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Potions;

public class DpsQueryer : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        decimal damage = DamageForTarget(Owner, target);
        await CreatureCmd.Damage(
            choiceContext,
            target,
            damage,
            ValueProp.Unpowered,
            Owner.Creature,
            null);
    }
    internal static int PredictedAttackDamage(Player owner, Creature enemy)
    {
        var intents = enemy.Monster?.NextMove?.Intents;
        if (intents is null || !enemy.IsAlive) return 0;
        int total = 0;
        foreach (var attack in intents.OfType<AttackIntent>())
        {
            // Do not use AttackIntent.GetSingleDamage: it uses LocalContext,
            // which would calculate a different target on each client.
            if (attack.DamageCalc is null) continue;
            decimal perHit = Hook.ModifyDamage(owner.RunState, owner.Creature.CombatState,
                owner.Creature, enemy, attack.DamageCalc(), ValueProp.Move, null, 
                ModifyDamageHookType.All, CardPreviewMode.None, out _);
            total += Math.Max(0, (int)perHit) * Math.Max(0, attack.Repeats);
        }
        return total;
    }

    internal static decimal DamageForTarget(Player owner, Creature target)
    {
        var enemies = owner.Creature.CombatState?.HittableEnemies;
        if (enemies is null) return 15m;
        var predicted = enemies.ToDictionary(enemy => enemy,
            enemy => PredictedAttackDamage(owner, enemy));
        int highest = predicted.Values.DefaultIfEmpty(0).Max();
        return highest > 0 && predicted.TryGetValue(target, out int damage) &&
            damage == highest ? 30m : 15m;
    }

}

public class PlatedRainPotion : SnowfallenPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<OverCloudPower>(
            choiceContext,
            target,
            5m,
            Owner.Creature,
            null);
    }
}

public class RiverBeaverPotion : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await PlayerCmd.LoseEnergy(3m, target.Player);
        await PowerCmd.Apply<RiverBeaverPower>(choiceContext, target, 3m, Owner.Creature, null);
    }
}

public class InvestorPotion : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        await InvestorPower.RegisterUse(
            choiceContext,
            Owner.Creature,
            Owner.Creature);
    }
}

public class LurkingPotion : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 1m, Owner.Creature, null);
        await PowerCmd.Apply<FrailPower>(choiceContext, target, 1m, Owner.Creature, null);
        await PowerCmd.Apply<LurkingAftereffectPower>(
            choiceContext,
            target,
            1m,
            Owner.Creature,
            null);
    }
}

public class WinterPotion : SnowfallenPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await FrostMechanics.Gain(choiceContext, target, 15m, Owner.Creature, null);
    }
}

public class PotatoPotion : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await CreatureCmd.Heal(target, 5m);
        await PowerCmd.Apply<PotatoProtectionPower>(
            choiceContext,
            target,
            1m,
            Owner.Creature,
            null);
    }
}

public class BewitchingPotion : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await CreatureCmd.Stun(target, null);
    }
}

public class AncientWine : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        await PowerCmd.Apply<AncientWinePower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            null);
    }
}

public class ThunderBowlPotion : SnowfallenSharedPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        await ThunderBowlPower.RegisterUse(
            choiceContext,
            Owner.Creature,
            Owner.Creature);
    }
}

public class IronDefensePotion : SnowfallenPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        AssertValidForTargetedPotion(target);
        await CreatureCmd.GainBlock(target, 10m, ValueProp.Unpowered, null);
        await PowerCmd.Apply<IronDefenseWatchPower>(
            choiceContext,
            target,
            1m,
            Owner.Creature,
            null);
    }
}
