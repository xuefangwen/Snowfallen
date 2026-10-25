using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.DongmingCode.RelicPools;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Relics;

[Pool(typeof(DongmingRelicPool))]
public abstract class DongmingRelics : CustomRelicModel
{
    public override string PackedIconPath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".RelicImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : "relic.png".RelicImagePath();
        }
    }

    protected override string PackedIconOutlinePath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}_outline.png".RelicImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : PackedIconPath;
        }
    }

    protected override string BigIconPath
    {
        get
        {
            string path = $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigRelicImagePath(fallback: false);
            return ResourceLoader.Exists(path) ? path : PackedIconPath;
        }
    }
}

public class DarkMoonNecklace : DongmingRelics
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected virtual int BaseMoonlight => 2;
    protected virtual int MoonlightPerCurse => 1;
    protected virtual int XuShiYinAmount => 1;

    public override async Task BeforeCombatStart()
    {
        await PowerCmd.Remove<DongmingBrightMoonPhasePower>(Owner.Creature);
        await PowerCmd.Remove<DongmingDarkMoonPhasePower>(Owner.Creature);
        int amount = BaseMoonlight +
                     MoonlightMechanics.CountCursedCards(Owner) * MoonlightPerCurse;
        Flash();
        await MoonlightMechanics.Gain(Owner, amount);
        await PowerCmd.Apply<XuShiYinPower>(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            XuShiYinAmount,
            Owner.Creature,
            null);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature applier,
        CardModel cardSource)
    {
        if (power.Owner == Owner.Creature &&
            power is DoomPower or CalamityPower &&
            amount < 0m)
        {
            Flash();
            await PowerCmd.Apply<EJieCounterPower>(
                choiceContext,
                Owner.Creature,
                -amount,
                Owner.Creature,
                cardSource);
        }
    }

}

public class ShadowMoonNecklace : DarkMoonNecklace
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override int MoonlightPerCurse => 2;
    protected override int XuShiYinAmount => 2;
}

public abstract class DongmingFragmentRelic : DongmingRelics
{
    protected static bool HasCompleteSet(Player owner) =>
        owner.GetRelic<BrokenShaft>() is not null &&
        owner.GetRelic<BrokenSpearhead>() is not null;

    protected async Task TryCompleteSet(Player player)
    {
        if (player != Owner || !HasCompleteSet(player) ||
            player.GetRelic<MingShuang>() is not null)
        {
            return;
        }

        Flash();
        await RelicCmd.Obtain<MingShuang>(player);
        await RelicCmd.Remove(player.GetRelic<BrokenShaft>()!);
        await RelicCmd.Remove(player.GetRelic<BrokenSpearhead>()!);
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked) =>
        TryCompleteSet(player);
}

public sealed class BrokenShaft : DongmingFragmentRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    private bool _triggered;

    public override Task BeforeCombatStart()
    {
        _triggered = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext c, Player player)
    {
        if (player != Owner || _triggered || player.PlayerCombatState.TurnNumber != 2)
            return;
        _triggered = true;
        Flash();
        await CardPileCmd.Draw(c, 3m, Owner, false);
    }
}

public sealed class BrokenSpearhead : DongmingFragmentRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    private bool _triggered;

    public override Task BeforeCombatStart()
    {
        _triggered = false;
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext c,
        Creature dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel source)
    {
        if (_triggered || dealer != Owner.Creature || result.UnblockedDamage <= 0m)
            return;
        _triggered = true;
        Flash();
        await FrostMechanics.Gain(c, Owner.Creature, 5m, Owner.Creature, source);
    }
}

public sealed class MingShuang : DongmingRelics
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext c, Player player)
    {
        if (player != Owner)
            return;
        Flash();
        await CardPileCmd.Draw(c, 1m, Owner, false);
        await FrostMechanics.Gain(c, Owner.Creature, 5m, Owner.Creature, null);
        await PlayerCmd.GainEnergy(1m, Owner);
    }
}

