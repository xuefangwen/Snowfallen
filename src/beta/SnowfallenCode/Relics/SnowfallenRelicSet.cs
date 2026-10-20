using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Patches;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Relics;

public class GameMaster : SnowfallenSharedRelic
{
    private bool _drawPending;
    private int _turnsUntilReady;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override Task BeforeCombatStart()
    {
        _drawPending = false;
        _turnsUntilReady = 0;
        return Task.CompletedTask;
    }

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (player == Owner &&
            amount > 0m &&
            _turnsUntilReady <= 0 &&
            CombatManager.Instance.IsInProgress)
        {
            _drawPending = true;
        }

        return amount;
    }

    public override async Task AfterModifyingEnergyGain()
    {
        if (!_drawPending)
        {
            return;
        }

        _drawPending = false;
        _turnsUntilReady = 3;
        Flash();
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1m, Owner, false);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner && _turnsUntilReady > 0)
        {
            _turnsUntilReady--;
        }

        return Task.CompletedTask;
    }
}

public class UnsentSound : SnowfallenSharedRelic
{
    public int Turns { get; set; }

    public override RelicRarity Rarity => RelicRarity.Common;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Turns;

    public override Task BeforeCombatStart()
    {
        Turns = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        Turns++;
        InvokeDisplayAmountChanged();
        if (Turns < 7)
        {
            return;
        }

        Turns = 0;
        InvokeDisplayAmountChanged();
        Flash();
        foreach (Creature enemy in Owner.Creature.CombatState.Enemies.Where(enemy => !enemy.IsDead))
        {
            await PowerCmd.Apply<UnsentSoundStrengthDownPower>(
                choiceContext,
                enemy,
                10m,
                Owner.Creature,
                null);
        }
    }
}

public class MagicBean : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side)
        {
            return;
        }

        decimal waterBreath = Owner.Creature.Powers
            .OfType<WaterBreathPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        if (waterBreath < 10m)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<RegenPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            null);
    }
}

public class DeterrentStaff : SnowfallenRelics
{
    public int SkillsPlayed { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => true;
    public override int DisplayAmount => SkillsPlayed;

    public override Task BeforeCombatStart()
    {
        SkillsPlayed = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            SkillsPlayed = 0;
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Skill)
        {
            return;
        }

        SkillsPlayed++;
        InvokeDisplayAmountChanged();
        if (SkillsPlayed % 2 != 0)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in Owner.Creature.CombatState.Enemies.Where(enemy => !enemy.IsDead))
        {
            await PowerCmd.Apply<DeterrentStaffStrengthDownPower>(
                choiceContext,
                enemy,
                1m,
                Owner.Creature,
                null);
        }
    }
}

public class MistCloud : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeCombatStart()
    {
        if (Owner.RunState.CurrentRoom is not CombatRoom room)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in room.Enemies.Where(enemy => !enemy.IsDead))
        {
            await PowerCmd.Apply<OverCloudPower>(
                new BlockingPlayerChoiceContext(),
                enemy,
                1m,
                Owner.Creature,
                null);
        }
    }
}

public class SheepWolfDoll : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public bool PendingNextCombatReward { get; set; }

    public override Task AfterObtained()
    {
        PendingNextCombatReward = true;
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (!PendingNextCombatReward || Owner is null)
            return Task.CompletedTask;
        PendingNextCombatReward = false;
        List<CardModel> attacks = Owner.Character.CardPool.AllCards
            .Where(card => card.Type == CardType.Attack &&
                           card.Rarity is not CardRarity.Token)
            .ToList();
        List<CardModel> rareAttacks = attacks
            .Where(card => card.Rarity == CardRarity.Rare)
            .ToList();
        if (rareAttacks.Count == 0)
        {
            return Task.CompletedTask;
        }

        List<CardModel> selectedModels = CardFactory.GetDistinctForCombat(
                Owner,
                rareAttacks,
                1,
                Owner.RunState.Rng.Niche)
            .ToList();
        selectedModels.AddRange(CardFactory.GetDistinctForCombat(
            Owner,
            attacks.Where(card =>
                selectedModels.All(existing => existing.Id != card.Id)),
            Math.Min(2, attacks.Count - selectedModels.Count),
            Owner.RunState.Rng.Niche));

        List<CardModel> offered = selectedModels
            .Select(Owner.RunState.CloneCard)
            .ToList();
        foreach (CardModel card in offered.Where(card => card.IsUpgradable))
        {
            CardCmd.Upgrade(card);
        }

        CardReward reward = new(
            offered,
            CardCreationSource.Other,
            Owner,
            null,
            null)
        {
            CanSkip = true
        };
        room.AddExtraReward(Owner, reward);
        return Task.CompletedTask;
    }
}

public class FennelHerb : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<RegenPower>(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            3m,
            Owner.Creature,
            null);
    }
}

public class StoneRelic : SnowfallenSharedRelic
{
    private readonly Dictionary<Creature, int> _pendingRetaliations = [];

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override Task BeforeCombatStart()
    {
        _pendingRetaliations.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature dealer,
        CardModel cardSource)
    {
        if (target == Owner.Creature &&
            dealer is not null &&
            dealer.Side == CombatSide.Enemy &&
            result.UnblockedDamage > 0)
        {
            _pendingRetaliations.TryGetValue(dealer, out int count);
            _pendingRetaliations[dealer] = count + 1;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || _pendingRetaliations.Count == 0)
        {
            return;
        }

        List<KeyValuePair<Creature, int>> pending = _pendingRetaliations.ToList();
        _pendingRetaliations.Clear();
        Flash();
        foreach ((Creature dealer, int count) in pending)
        {
            if (dealer.IsDead)
            {
                continue;
            }

            for (int i = 0; i < count && !dealer.IsDead; i++)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    dealer,
                    6m,
                    ValueProp.Unpowered,
                    null,
                    null);
            }
        }
    }
}

public class TropicalIcedTea : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<FrostPower>(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            5m,
            Owner.Creature,
            null);
    }
}

public class WolfberryWater : SnowfallenSharedRelic
{
    private bool _dexterityApplied;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        _dexterityApplied = false;
        await UpdateDexterity();
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature && CombatManager.Instance.IsInProgress)
        {
            await UpdateDexterity();
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _dexterityApplied = false;
        return Task.CompletedTask;
    }

    private async Task UpdateDexterity()
    {
        bool shouldApply = Owner.Creature.CurrentHp <=
                           (int)Math.Floor(Owner.Creature.MaxHp * 0.4m);
        if (shouldApply == _dexterityApplied)
        {
            return;
        }

        _dexterityApplied = shouldApply;
        Flash();
        await PowerCmd.Apply<DexterityPower>(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            shouldApply ? 3m : -3m,
            Owner.Creature,
            null);
    }
}

public class DogBiscuits : SnowfallenRelics
{
    private static readonly ConditionalWeakTable<Player, DogBiscuits> ActiveByOwner = new();

    public int Charges { get; set; }
    public int LastKnownHp { get; set; }
    private bool _grantingBonus;
    private bool _trackingEnabled;

    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShowCounter => true;
    public override int DisplayAmount => Charges;

    public override Task BeforeCombatStart()
    {
        ActiveByOwner.Remove(Owner);
        ActiveByOwner.Add(Owner, this);
        Charges = 2;
        LastKnownHp = Owner.Creature.CurrentHp;
        _grantingBonus = false;
        _trackingEnabled = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    internal static void EnableHealingTrackingAfterStarterRelic(Player player)
    {
        if (!ActiveByOwner.TryGetValue(player, out DogBiscuits relic))
        {
            return;
        }

        relic.LastKnownHp = player.Creature.CurrentHp;
        relic._trackingEnabled = true;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner.Creature)
        {
            return;
        }

        int currentHp = creature.CurrentHp;
        int actualHealing = Math.Max(0, currentHp - LastKnownHp);
        LastKnownHp = currentHp;
        if (!_trackingEnabled ||
            _grantingBonus ||
            actualHealing <= 0 ||
            Charges <= 0 ||
            !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        Charges--;
        InvokeDisplayAmountChanged();
        _grantingBonus = true;
        Flash();
        await CreatureCmd.Heal(creature, 5m);
        LastKnownHp = creature.CurrentHp;
        _grantingBonus = false;
    }
}

public class WaterDragonSpirit : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task BeforeCombatStart()
    {
        if (Owner.RunState.CurrentRoom is not CombatRoom room)
        {
            return;
        }

        Flash();
        await EmberMechanics.CreateInHand(Owner, room.CombatState, 3);
    }
}

public class Camera : SnowfallenSharedRelic
{
    public bool Available { get; set; } = true;

    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool IsUsedUp => !Available;

    public override async Task BeforeCombatStart()
    {
        if (!Available ||
            Owner.RunState.CurrentRoom is not CombatRoom room ||
            room.RoomType != RoomType.Elite)
        {
            return;
        }

        Available = false;
        Flash();
        foreach (Creature enemy in room.Enemies.Where(enemy => !enemy.IsDead))
        {
            await CreatureCmd.Stun(enemy, null);
        }
    }
}

public class OrangeCatStatue : SnowfallenSharedRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        Flash();
        await PlayerCmd.GainGold(15m, Owner, false);
    }
}

public class Orange : SnowfallenSharedRelic
{
    public int CombatsRemaining { get; set; } = 4;

    public override RelicRarity Rarity => RelicRarity.Shop;
    public override bool ShowCounter => true;
    public override int DisplayAmount => CombatsRemaining;
    public override bool IsUsedUp => CombatsRemaining <= 0;

    public override async Task BeforeCombatStart()
    {
        if (CombatsRemaining <= 0)
        {
            return;
        }

        CombatsRemaining--;
        InvokeDisplayAmountChanged();
        Flash();
        await CreatureCmd.GainMaxHp(Owner.Creature, 3m);
    }
}

public class GameGod : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override async Task BeforeCombatStart()
    {
        if (Owner.RunState.CurrentRoom is not CombatRoom room)
        {
            return;
        }

        List<CardModel> rareCards = Owner.Character.CardPool.AllCards
            .Where(card => card.Rarity == CardRarity.Rare)
            .ToList();
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
                Owner,
                rareCards,
                Math.Min(3, rareCards.Count),
                Owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (choices.Count == 0)
        {
            return;
        }

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            new BlockingPlayerChoiceContext(),
            choices,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1));
        CardModel card = selected.FirstOrDefault();
        if (card is null)
        {
            return;
        }

        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(
            card,
            PileType.Hand,
            Owner,
            CardPilePosition.Bottom);
    }
}

public class SnowLeopardFur : SnowfallenRelics
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<WaterBreathPower>(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            5m,
            Owner.Creature,
            null);
        await CreatureCmd.Heal(Owner.Creature, 3m);
    }
}

public class Tablet : SnowfallenSharedRelic
{
    public bool Available { get; set; } = true;
    private bool _travelPending;

    public override RelicRarity Rarity => RelicRarity.Shop;
    public override bool IsUsedUp => !Available;

    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    public override bool ShouldAllowFreeTravel()
    {
        return Available &&
               (TabletTravelState.IsArmed(this) || _travelPending);
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (!_travelPending)
        {
            return Task.CompletedTask;
        }

        _travelPending = false;
        Available = false;
        Flash();
        return Task.CompletedTask;
    }

    internal void MarkTravelPending()
    {
        _travelPending = true;
    }
}

