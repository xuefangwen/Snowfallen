using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Enchantments;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.DongmingCode.Cards;

public class ChuE() : DongmingCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DoomPower>(), HoverTipFactory.FromPower<ChuEPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ChuEPower>(4m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var debuff in Owner.Creature.Powers
                     .Where(power => power.TypeForCurrentAmount == MegaCrit.Sts2.Core.Entities.Powers.PowerType.Debuff)
                     .ToList())
            await PowerCmd.Remove(debuff);
        await PowerCmd.Apply<ChuEPower>(choiceContext, Owner.Creature,
            DynamicVars["ChuEPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ChuEPower"].UpgradeValueBy(2m);
        AddKeyword(CardKeyword.Retain);
    }
}

public abstract class DongmingExclusiveCurse(
    int cost,
    TargetType target = TargetType.Self) :
    MoonPhaseCard(cost, CardType.Curse, CardRarity.Curse, target, MoonPhase.Dark)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override int MaxUpgradeLevel => 0;
    protected override bool ShouldGlowRedInternal => CurrentMoonPhase == MoonPhase.Dark;
    public override string Title => CurrentMoonPhase == MoonPhase.Bright
        ? new LocString("cards", $"{Id.Entry}.title.bright").GetFormattedText()
        : base.Title;
}

// Internal class names are intentionally retained for compatibility with existing saves.
public class ZhuiYi() : DongmingExclusiveCurse(1, TargetType.AllEnemies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move), new BlockVar(6m, ValueProp.Move), new StarsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            foreach (Creature ally in CombatState.Players.Select(player => player.Creature).Where(ally => ally.IsAlive))
                await CreatureCmd.GainBlock(ally, DynamicVars.Block, p);
        }
        else
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, p)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_bloody_impact", null, "blunt_attack.mp3")
                .Execute(c);
            await CreatureCmd.Damage(c, Owner.Creature, DynamicVars.Damage.BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                Owner.Creature, this, p);
        }
        await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
    }
}

public class LunHui() : DongmingExclusiveCurse(2)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Eternal];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StarsVar(4), new StarsVar("BrightStars", 2)];

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (CurrentMoonPhase == MoonPhase.Bright || card.Owner != Owner ||
            Pile?.Type != PileType.Hand || card is LunHui || autoPlayType != AutoPlayType.None)
            return true;
        return false;
    }

    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            List<CardModel> candidates = PileType.Hand.GetPile(Owner).Cards
                .Where(card => !ReferenceEquals(card, this)).ToList();
            if (candidates.Count > 0)
            {
                CardModel selected = (await CardSelectCmd.FromHand(c, Owner,
                    new CardSelectorPrefs(SelectionScreenPrompt, 1, 1), candidates.Contains, this)).First();
                selected.EnergyCost.SetThisTurnOrUntilPlayed(0);
            }

            await MoonlightMechanics.Gain(Owner, DynamicVars["BrightStars"].BaseValue);
            return;
        }

        await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
    }
}

public class ZhongDian() : DongmingExclusiveCurse(3)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;
    private const int BaseDoomPercent = 20;
    private const int DoomPercentIncrease = 5;
    private int _doomPercent = BaseDoomPercent;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<DoomPower>(), HoverTipFactory.FromPower<FrostPower>()];

    [SavedProperty]
    public int DoomPercent
    {
        get => _doomPercent;
        set
        {
            AssertMutable();
            _doomPercent = value;
            DynamicVars["DoomPercent"].BaseValue = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("DoomPercent", DoomPercent), new IntVar("DoomIncrease", DoomPercentIncrease),
        new HealVar(15m), new PowerVar<FrostPower>(15m), new StarsVar(3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            foreach (Creature ally in CombatState.Players.Select(player => player.Creature).Where(ally => ally.IsAlive))
                await CreatureCmd.Heal(ally, DynamicVars.Heal.BaseValue);
            await FrostMechanics.Gain(c, Owner.Creature, DynamicVars["FrostPower"].BaseValue,
                Owner.Creature, this);
            await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
            return;
        }

        decimal doom = Math.Max(1m, Math.Floor(Owner.Creature.MaxHp * DoomPercent / 100m));
        await PowerCmd.Apply<DoomPower>(c, Owner.Creature, doom, Owner.Creature, this);
        List<CardModel> candidates = Owner.PlayerCombatState.AllCards
            .Where(card => card.DeckVersion is not null)
            .Where(card => DongmingCurseEnchantment.CanEnchantTarget(card))
            .Where(card => DongmingCurseEnchantment.CanEnchantTarget(card.DeckVersion))
            .DistinctBy(card => card.DeckVersion).ToList();
        if (candidates.Count > 0)
        {
            CardModel selected = (await CardSelectCmd.FromSimpleGrid(c, candidates, Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
            if (selected?.DeckVersion is CardModel deckCard)
            {
                CardCmd.Enchant<DongmingCurseEnchantment>(deckCard, 1m);
                CardCmd.Enchant<DongmingCurseEnchantment>(selected, 1m);
            }
        }
        IncreaseDoomPercent(DoomPercentIncrease);
        (DeckVersion as ZhongDian)?.IncreaseDoomPercent(DoomPercentIncrease);
    }

    private void IncreaseDoomPercent(int amount) => DoomPercent += Math.Max(0, amount);
}

public class EZhao() : DongmingExclusiveCurse(1)
{
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<DoomPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StarsVar(2), new IntVar("Doom", 4m), new CardsVar(3)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
        if (CurrentMoonPhase == MoonPhase.Bright)
            await CardPileCmd.Draw(c, DynamicVars.Cards.BaseValue, Owner);
        else
            await PowerCmd.Apply<DoomPower>(c, Owner.Creature, DynamicVars["Doom"].BaseValue, Owner.Creature, this);
    }
}

public class XuZai() : DongmingExclusiveCurse(3)
{
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<YueZhiZuZhouPower>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<YueZhiZuZhouPower>(2m), new StarsVar(4), new EnergyVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            await PowerCmd.Apply<XuZaiHopePower>(c, Owner.Creature, 1m, Owner.Creature, this);
        }
        else
            await PowerCmd.Apply<YueZhiZuZhouPower>(c, Owner.Creature,
                DynamicVars["YueZhiZuZhouPower"].BaseValue, Owner.Creature, this);
    }
}

public class YuLe() : DongmingExclusiveCurse(1, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<YuLeStrengthDownPower>(), HoverTipFactory.FromPower<StrengthPower>()];
    public override TargetType TargetType => CurrentMoonPhase == MoonPhase.Bright
        ? TargetType.Self
        : TargetType.AnyEnemy;

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (CurrentMoonPhase == MoonPhase.Bright || card.Owner != Owner ||
            Pile?.Type != PileType.Hand || card is YuLe || autoPlayType != AutoPlayType.None)
            return true;
        return false;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<YuLeStrengthDownPower>(9m), new PowerVar<StrengthPower>(1m), new StarsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            await PowerCmd.Apply<StrengthPower>(c, Owner.Creature,
                DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
            return;
        }
        ArgumentNullException.ThrowIfNull(p.Target);
        await PowerCmd.Apply<YuLeStrengthDownPower>(c, Owner.Creature,
            DynamicVars["YuLeStrengthDownPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<YuLeStrengthDownPower>(c, p.Target,
            DynamicVars["YuLeStrengthDownPower"].BaseValue, Owner.Creature, this);
    }
}

public class XiangCan() : DongmingExclusiveCurse(1)
{
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<YueZhiZuZhouPower>()];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(5), new CardsVar("BrightCards", 4), new StarsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        bool bright = CurrentMoonPhase == MoonPhase.Bright;
        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards
            .Where(card => bright || MoonlightMechanics.IsCursedCard(card))
            .ToList();
        int maximum = Math.Min(bright ? DynamicVars["BrightCards"].IntValue : DynamicVars.Cards.IntValue,
            candidates.Count);
        if (maximum == 0) return;
        CardSelectorPrefs prefs = new(SelectionScreenPrompt, 0, maximum)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };
        List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(c, candidates, Owner, prefs)).ToList();
        foreach (CardModel card in selected)
            await CardCmd.Exhaust(c, card);
        if (bright)
            await MoonlightMechanics.Gain(Owner, selected.Count * DynamicVars.Stars.BaseValue);
        else
        {
            int stacks = Math.Max(0, selected.Count - 2);
            if (stacks > 0)
                await PowerCmd.Apply<YueZhiZuZhouPower>(c, Owner.Creature, stacks, Owner.Creature, this);
        }
    }
}

public class FuHua() : DongmingExclusiveCurse(0)
{
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>(), HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<DecayPower>(), HoverTipFactory.FromPower<MingXinDexterityPower>()
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StarsVar(2), new IntVar("Doom", 4m), new PowerVar<WeakPower>(2m),
            new PowerVar<DecayPower>(2m), new PowerVar<MingXinDexterityPower>(3m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            foreach (Creature ally in CombatState.Players.Select(player => player.Creature).Where(ally => ally.IsAlive))
                await PowerCmd.Apply<MingXinDexterityPower>(c, ally,
                    DynamicVars["MingXinDexterityPower"].BaseValue, Owner.Creature, this);
            return;
        }
        await PowerCmd.Apply<DoomPower>(c, Owner.Creature, DynamicVars["Doom"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(c, Owner.Creature, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<DecayPower>(c, Owner.Creature, DynamicVars["DecayPower"].BaseValue, Owner.Creature, this);
    }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation location)
    {
        return ReferenceEquals(card, this) && CurrentMoonPhase == MoonPhase.Dark
            ? new CardLocation(Owner, PileType.Draw, CardPilePosition.Top)
            : location;
    }
}

public class TongMing() : DongmingExclusiveCurse(1)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<TongMingStrengthPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WeakPower>(2m), new PowerVar<TongMingStrengthPower>(5m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        bool bright = CurrentMoonPhase == MoonPhase.Bright;
        if (bright)
        {
            foreach (Creature ally in CombatState.Players.Select(player => player.Creature).Where(ally => ally.IsAlive))
                await PowerCmd.Apply<TongMingStrengthPower>(c, ally,
                    DynamicVars["TongMingStrengthPower"].BaseValue, Owner.Creature, this);
        }
        else
        {
            await PowerCmd.Apply<WeakPower>(c, Owner.Creature,
                DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
            foreach (Creature enemy in CombatState.HittableEnemies)
                await PowerCmd.Apply<WeakPower>(c, enemy,
                    DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        }

        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards
            .Where(card => card.Type is CardType.Attack or CardType.Skill or CardType.Power)
            .Where(card => card.Enchantment is null)
            .Where(card => !card.Keywords.Contains(CardKeyword.Unplayable)).ToList();
        if (candidates.Count == 0) return;
        CardModel selected = (await CardSelectCmd.FromSimpleGrid(c, candidates, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).First();
        if (bright)
            selected.EnergyCost.SetUntilPlayed(0);
        else
            CardCmd.Enchant<DongmingCurseEnchantment>(selected, 1m);
    }
}

public class MingJie() : DongmingExclusiveCurse(1)
{
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<DoomPower>(), HoverTipFactory.FromPower<DrawCardsNextTurnPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Doom", 8m), new StarsVar(3), new StarsVar("BrightStars", 2), new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            await MoonlightMechanics.Gain(Owner, DynamicVars["BrightStars"].BaseValue);
            await CardPileCmd.Draw(c, DynamicVars.Cards.BaseValue, Owner);
            await PowerCmd.Apply<DrawCardsNextTurnPower>(c, Owner.Creature,
                DynamicVars.Cards.BaseValue, Owner.Creature, this);
            return;
        }

        await MoonlightMechanics.Gain(Owner, DynamicVars.Stars.BaseValue);
        await PowerCmd.Apply<DoomPower>(c, Owner.Creature,
            DynamicVars["Doom"].BaseValue, Owner.Creature, this);
    }
}


