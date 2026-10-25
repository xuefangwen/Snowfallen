using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Enchantments;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class ZhouFanPower : DongmingPower
{
    private sealed record PendingReflection(
        PowerModel Power,
        decimal Amount,
        Creature Applier);

    private sealed class Data
    {
        public Stack<PendingReflection> Pending { get; } = [];
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override object InitInternalData() => new Data();

    public override bool TryModifyPowerAmountReceived(
        PowerModel power,
        Creature target,
        decimal amount,
        Creature applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner || amount == 0m ||
            power.GetTypeForAmount(amount) != PowerType.Debuff ||
            power is ArtifactPower or ZhouFanPower)
        {
            return false;
        }

        // Artifact must consume the application before Zhou Fan can see it.
        if (power.IsVisible && Owner.Powers.OfType<ArtifactPower>().Any())
        {
            return false;
        }

        modifiedAmount = 0m;
        GetInternalData<Data>().Pending.Push(
            new PendingReflection(power, amount, applier));
        return true;
    }

    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        Stack<PendingReflection> pendingReflections =
            GetInternalData<Data>().Pending;
        if (!pendingReflections.TryPeek(out PendingReflection pending) ||
            !ReferenceEquals(pending.Power, power))
        {
            return;
        }

        pendingReflections.Pop();
        await PowerCmd.ModifyAmount(
            new BlockingPlayerChoiceContext(), this, -1m, Owner, null);

        if (pending.Applier is null ||
            pending.Applier.Side == Owner.Side ||
            !pending.Applier.IsAlive)
        {
            return;
        }

        // The hook receives either a newly-created mutable power or an
        // existing mutable power owned by the target. ToMutable() only accepts
        // canonical models and throws here. Cloning preserves the exact power
        // variant and dynamic data while PowerModel.AfterCloned clears Owner.
        PowerModel replacement = ReflectedEnemyMechanics.Replacement(pending.Power);
        PowerModel reflected = replacement?.ToMutable() ?? (PowerModel)pending.Power
            .ClonePreservingMutability();
        await PowerCmd.Apply(
            new BlockingPlayerChoiceContext(),
            reflected,
            pending.Applier,
            replacement is null ? pending.Amount : 1m,
            Owner,
            null,
            false);
    }
}

public sealed class YongHengYueYanPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterSideTurnEnd(PlayerChoiceContext c, CombatSide side, IEnumerable<Creature> creatures)
    { if (side == Owner.Side && creatures.Contains(Owner)) await MoonlightMechanics.Gain(Owner.Player, Owner.Player.PlayerCombatState.TurnNumber * Amount); }
}
public sealed class ENianPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterCardPlayed(PlayerChoiceContext c, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player || !MoonlightMechanics.IsCursedCard(play.Card))
        {
            return;
        }

        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
        {
            await PowerCmd.Apply<CursePower>(
                c,
                enemy,
                Amount,
                Owner,
                play.Card);
        }
    }
}

public sealed class CleverThoughtPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (card.Owner != Owner.Player ||
            !fromHandDraw ||
            card.Type != CardType.Curse)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
        await PowerCmd.Apply<DoomPower>(
            choiceContext, Owner, 3m, Owner, card);
    }
}

public class HuangZaiBianPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Single;
    protected virtual decimal DoomPercent => 30m;
    public override string CustomPackedIconPath => "huang_zai_bian_power.png".PowerImagePath();
    public override string CustomBigIconPath => "huang_zai_bian_power.png".BigPowerImagePath();
    protected override bool IsVisibleInternal => ActiveForms > 0;
    [SavedProperty] public List<int> PendingTimers { get; set; } = [];
    [SavedProperty] public int ActiveForms { get; set; }
    private bool _reflectingDoom;

    public void QueueInstance(bool upgraded) => PendingTimers.Add(upgraded ? -1 : 1);

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext c,
        Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        // Process activations that have waited two turn starts, then apply the
        // second Doom stack for copies played on the preceding turn.
        int secondDoomCount = 0;
        int activateNow = 0;
        for (int index = PendingTimers.Count - 1; index >= 0; index--)
        {
            bool upgraded = PendingTimers[index] < 0;
            int remaining = Math.Abs(PendingTimers[index]) - 1;
            PendingTimers[index] = upgraded ? -remaining : remaining;
            if (remaining == 1)
                secondDoomCount++;
            else if (remaining <= 0)
            {
                activateNow++;
                PendingTimers.RemoveAt(index);
            }
        }

        if (secondDoomCount > 0)
        {
            await PowerCmd.Apply<DoomPower>(
                c, Owner,
                Math.Floor(Owner.MaxHp * DoomPercent / 100m) * secondDoomCount,
                Owner, null);
        }

        if (activateNow > 0)
        {
            ActiveForms += activateNow;
            decimal cleared = Owner.Powers
                .Where(power => power is DoomPower or CalamityPower)
                .Sum(power => (decimal)power.Amount);
            if (cleared > 0m)
            {
                foreach (PowerModel power in Owner.Powers
                             .Where(power => power is DoomPower or CalamityPower)
                             .ToList())
                    await PowerCmd.ModifyAmount(c, power, -power.Amount, Owner, null);
                await PowerCmd.Apply<EJieCounterPower>(c, Owner, cleared, Owner, null);
                int activeForms = Owner.Powers.OfType<HuangZaiBianPower>().Sum(power => power.ActiveForms);
                foreach (Creature enemy in CombatState.HittableEnemies.ToList())
                    await PowerCmd.Apply<DoomPower>(c, enemy, cleared * 3m * activeForms, Owner, null);
            }
        }
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext c, PowerModel power, decimal amount, Creature applier, CardModel source)
    {
        HuangZaiBianPower primaryActive = Owner.Powers.OfType<HuangZaiBianPower>().FirstOrDefault(item => item.ActiveForms > 0);
        if (_reflectingDoom || ActiveForms <= 0 || power is not (DoomPower or CalamityPower) || power.Owner != Owner || amount <= 0 || !ReferenceEquals(primaryActive, this)) return;
        _reflectingDoom = true;
        try
        {
        await PowerCmd.ModifyAmount(c, power, -amount, Owner, source);
        await PowerCmd.Apply<EJieCounterPower>(c, Owner, amount, Owner, source);
        int activeForms = Owner.Powers.OfType<HuangZaiBianPower>().Sum(item => item.ActiveForms);
        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
            await PowerCmd.Apply<DoomPower>(c, enemy, amount * 3m * activeForms, Owner, source);
        }
        finally { _reflectingDoom = false; }
    }
}

public sealed class HuangZaiBianUpgradedPower : HuangZaiBianPower
{
    protected override decimal DoomPercent => 50m;
}

public abstract class EJieDamageReductionPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool IsUncapped => Owner.Powers.OfType<HuangZaiBianPower>()
        .Any(power => power.ActiveForms > 0);
    private decimal EffectiveEJie => IsUncapped ? TotalEJie : Math.Min(TotalEJie, Owner.MaxHp);

    private decimal TotalEJie => Owner.Powers
        .Where(power => power is EJiePower or EJieCounterPower)
        .Sum(power => (decimal)power.Amount);

    private bool IsPrimaryEJiePower()
    {
        PowerModel primary = Owner.Powers
            .OfType<EJieCounterPower>()
            .Cast<PowerModel>()
            .FirstOrDefault() ?? Owner.Powers
            .OfType<EJiePower>()
            .Cast<PowerModel>()
            .FirstOrDefault();
        return ReferenceEquals(primary, this);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource,
        CardPlay cardPlay)
    {
        if (target != Owner || amount <= 0m || !IsPrimaryEJiePower())
        {
            return 1m;
        }

        // LoseMaxHp is implemented as source-less unblockable damage; it is
        // explicitly excluded from 厄劫 reduction.
        if (dealer is null && cardSource is null &&
            props.HasFlag(ValueProp.Unblockable))
        {
            return 1m;
        }

        decimal cappedStacks = EffectiveEJie;
        decimal reduction = Math.Floor(cappedStacks / 3m) * 0.01m;
        return Math.Max(0m, 1m - reduction);
    }

    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target != Owner || amount <= 0m || !IsPrimaryEJiePower())
            return amount;

        // Ordinary damage is already reduced in ModifyDamageMultiplicative.
        // Only unblockable life loss needs this second hook. Max-HP loss uses
        // an unpowered, source-less command and is intentionally excluded.
        if (!props.HasFlag(ValueProp.Unblockable) ||
            dealer is null && cardSource is null)
            return amount;

        decimal cappedStacks = EffectiveEJie;
        decimal reduction = Math.Floor(cappedStacks / 3m) * 0.01m;
        return Math.Floor(amount * Math.Max(0m, 1m - reduction));
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature applier,
        CardModel cardSource)
    {
        if (!ReferenceEquals(power, this) || amount <= 0m || IsUncapped)
        {
            return;
        }

        decimal excess = TotalEJie - Owner.MaxHp;
        if (excess <= 0m)
        {
            return;
        }

        decimal reduction = Math.Min(Amount, excess);
        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            -reduction,
            applier,
            cardSource);
    }
}

// Retains the old model ID so older combat saves load with the current effect.
public sealed class EJiePower : EJieDamageReductionPower
{
}

public sealed class EJieCounterPower : EJieDamageReductionPower
{
}

public sealed class HuiShuangPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task BeforeSideTurnStart(PlayerChoiceContext c, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    { if (side == Owner.Side && creatures.Contains(Owner)) await FrostMechanics.Gain(c, Owner, Amount, Owner, null); }
}
public sealed class NingYuePower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterStarsGained(int amount, Player player) { if (player == Owner.Player && amount > 0) await FrostMechanics.Gain(new BlockingPlayerChoiceContext(), Owner, amount * Amount, Owner, null); }
}

public sealed class NextMoonlightFreePower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private static bool IsMoonlightCard(CardModel card) =>
        card.Owner is not null &&
        card is DongmingCard dongmingCard &&
        (dongmingCard.CurrentStarCost > 0 || dongmingCard.HasStarCostX) &&
        card is not YueHua;

    public override bool TryModifyEnergyCostInCombatLate(
        CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner.Player || !IsMoonlightCard(card))
            return false;
        modifiedCost = 0m;
        return true;
    }

    public override bool TryModifyStarCost(
        CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner != Owner.Player || !IsMoonlightCard(card))
            return false;
        modifiedCost = 0m;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner.Player && IsMoonlightCard(cardPlay.Card))
            await PowerCmd.Remove(this);
    }
}
public sealed class CanXingPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterStarsSpent(int amount, Player player)
    {
        if (player != Owner.Player || amount <= 0)
        {
            return;
        }

        if (CanXingMechanics.TryDeferStarsSpent(this, amount, player))
        {
            return;
        }

        await PowerCmd.Apply<VigorPower>(
            new BlockingPlayerChoiceContext(),
            Owner,
            amount * Amount,
            Owner,
            null);
    }
}
public sealed class YuanShaPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override int ModifyCardPlayCount(CardModel card, Creature target, int count) =>
        card.Owner == Owner.Player
            ? count + YuanShaMechanics.GetExtraTriggerCount(card)
            : count;
}
public sealed class YuHenPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task BeforeSideTurnStart(PlayerChoiceContext c, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    { if (side == Owner.Side && creatures.Contains(Owner)) foreach (Creature e in CombatState.HittableEnemies.ToList()) await PowerCmd.Apply<CursePower>(c, e, Amount, Owner, null); }
}
public sealed class HuoShaoYunPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext c, PowerModel power, decimal amount, Creature applier, CardModel source)
    { if (power is CursePower && power.Owner.Side == CombatSide.Enemy && amount > 0 && applier == Owner) await PowerCmd.Apply<OverCloudPower>(c, power.Owner, amount * Amount, Owner, source); }
}
public sealed class ShiYuePower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterEnergyReset(Player player)
    {
        if (player != Owner.Player || player.PlayerCombatState.Stars <= 0)
            return;

        int before = player.PlayerCombatState.Stars;
        await MoonlightMechanics.Spend(player, Math.Min(Amount, before));
        int lost = before - player.PlayerCombatState.Stars;
        if (lost > 0)
            await PlayerCmd.GainEnergy(lost, player);
    }
}
public sealed class SuoSuiPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Single;
    public static void EnchantCurrentCombatPiles(Player player)
    {
        foreach (CardModel card in new[] { PileType.Hand, PileType.Draw, PileType.Discard }
                     .SelectMany(type => type.GetPile(player).Cards)
                     .Where(MoonlightMechanics.IsCursedCard)
                     .Where(card => card.Enchantment is null)
                     .ToList())
            CardCmd.Enchant<DongmingCurseEnchantment>(card, 1m);
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel source)
    {
        if (card.Owner == Owner.Player &&
            card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard &&
            MoonlightMechanics.IsCursedCard(card) &&
            card.Enchantment is null)
            CardCmd.Enchant<DongmingCurseEnchantment>(card, 1m);
        return Task.CompletedTask;
    }
}
public sealed class ShuangFengPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task BeforeSideTurnStart(PlayerChoiceContext c, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    { if (side == Owner.Side && creatures.Contains(Owner)) await PowerCmd.Apply<NingHuaPower>(c, Owner, Amount, Owner, null); }
}
public sealed class ShuangZhouPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    { if (side == Owner.Side && creatures.Contains(Owner)) await PowerCmd.Apply<ShuangZhouArmorPower>(new BlockingPlayerChoiceContext(), Owner, Amount, Owner, null); }
}
public sealed class ShuangZhouArmorPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    private decimal _pendingBlock;

    public override Task BeforeSideTurnStart(PlayerChoiceContext c, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    {
        _pendingBlock = side == Owner.Side && creatures.Contains(Owner)
            ? Math.Floor((Owner.Powers.OfType<FrostPower>().FirstOrDefault()?.Amount ?? 0m) * 0.25m) * Amount
            : 0m;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    {
        decimal block = _pendingBlock;
        _pendingBlock = 0m;
        if (side == Owner.Side && creatures.Contains(Owner) && block > 0m)
            await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}
public sealed class HuiQiPower : HuiQiPowerBase { protected override decimal Energy => 2; protected override decimal Draw => 2; protected override decimal Heal => 5; }
public sealed class HuiQiUpgradedPower : HuiQiPowerBase { protected override decimal Energy => 3; protected override decimal Draw => 3; protected override decimal Heal => 8; }
public abstract class HuiQiPowerBase : DongmingPower
{
    protected abstract decimal Energy { get; } protected abstract decimal Draw { get; } protected abstract decimal Heal { get; }
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterDeath(PlayerChoiceContext c, Creature creature, bool prevented, float anim)
    { if (prevented || creature.Side != CombatSide.Enemy || !creature.IsDead) return; foreach (Player p in CombatState.Players.Where(p => p.Creature.IsAlive)) { await PlayerCmd.GainEnergy(Energy * Amount, p); await CardPileCmd.Draw(c, (int)(Draw * Amount), p); await CreatureCmd.Heal(p.Creature, Heal * Amount); } }
}
public sealed class XuRuiPower : DongmingPower
{
    private bool _attackedThisTurn;
    public override PowerType Type => PowerType.Buff; public override PowerStackType StackType => PowerStackType.Counter;
    public override Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner.Player &&
            cardPlay.Card.Type == CardType.Attack)
        {
            _attackedThisTurn = true;
        }
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext c, CombatSide side, IReadOnlyList<Creature> creatures, ICombatState state)
    {
        if (side == Owner.Side && creatures.Contains(Owner))
        {
            if (!_attackedThisTurn)
            {
                await PowerCmd.Apply<VigorPower>(c, Owner, Amount, Owner, null);
                await FrostMechanics.Gain(c, Owner, Amount, Owner, null);
            }
            _attackedThisTurn = false;
        }
    }
}

