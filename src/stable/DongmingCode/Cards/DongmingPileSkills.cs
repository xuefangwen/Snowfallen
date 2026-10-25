using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.DongmingCode.Cards;

public class YueShuangTongXin() : DongmingCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;
    public override int CanonicalStarCost => 1;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FrostPower>(8m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        foreach (Creature ally in CombatState.Players.Select(x => x.Creature).Where(x => x.IsAlive))
            await FrostMechanics.Gain(c, ally, DynamicVars["FrostPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["FrostPower"].UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];
}

public class JiZhou() : DongmingCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal static int CountCombatCurses(MegaCrit.Sts2.Core.Entities.Players.Player owner) =>
        new[] { PileType.Hand, PileType.Draw, PileType.Discard }
            .Sum(pile => pile.GetPile(owner).Cards.Count(card => card.Type == CardType.Curse));

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        ArgumentNullException.ThrowIfNull(p.Target);
        bool exhausted = DongmingAttackMechanics.IsExhausted(p.Target);
        int stacks = CountCombatCurses(Owner);
        if (stacks > 0) await PowerCmd.Apply<CursePower>(c, p.Target, stacks, Owner.Creature, this);
        if (exhausted || DongmingAttackMechanics.IsExhausted(p.Target))
            await PlayerCmd.GainEnergy(3m, Owner);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<CursePower>(), ExhaustionPursuit.CreateTip(this)];
}

public class ChengNian() : MoonPhaseCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        PileType sourcePile = CurrentMoonPhase == MoonPhase.Bright
            ? PileType.Discard
            : PileType.Draw;
        List<CardModel> cards = sourcePile.GetPile(Owner).Cards.ToList();
        int count = Math.Min(DynamicVars.Cards.IntValue, cards.Count);
        if (count > 0)
        {
            List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
                c, cards, Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 0, count)
                {
                    Cancelable = true,
                    RequireManualConfirmation = true
                })).Distinct().ToList();
            foreach (CardModel card in selected) await CardCmd.Exhaust(c, card);
            if (selected.Count > 0)
                await CardPileCmd.Draw(c, selected.Count * 3, Owner);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

public class CangFeng() : DongmingCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7m, ValueProp.Move), new PowerVar<NingHuaPower>(3m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
        await PowerCmd.Apply<NingHuaPower>(c, Owner.Creature,
            DynamicVars["NingHuaPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["NingHuaPower"].UpgradeValueBy(2m);
    }
}

public class YuanJia() : MoonPhaseCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, MoonPhase.Bright)
{
    public override int CanonicalStarCost => 1;
    public override bool GainsBlock => true;
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<PlatingPower>(), HoverTipFactory.FromPower<RegenPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar("BrightBlock", 3m, ValueProp.Move),
        new BlockVar("DarkBlock", 2m, ValueProp.Move),
        new PowerVar<PlatingPower>(1m),
        new PowerVar<RegenPower>(1m),
        new RepeatVar(3)
    ];
    public int CurseReplayCount => Math.Min(DynamicVars.Repeat.IntValue, PileType.Exhaust.GetPile(Owner).Cards.Count(MoonlightMechanics.IsCursedCard));
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            await CreatureCmd.GainBlock(Owner.Creature, (BlockVar)DynamicVars["BrightBlock"], p);
            await PowerCmd.Apply<PlatingPower>(c, Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, (BlockVar)DynamicVars["DarkBlock"], p);
            await PowerCmd.Apply<RegenPower>(c, Owner.Creature, DynamicVars["RegenPower"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(2m);
}

public class JinChangJie() : DongmingCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2), new IntVar("GeneratedUpgrade", 0m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        bool upgraded = DynamicVars["GeneratedUpgrade"].IntValue > 0;
        await DongmingSkillMechanics.CreateMoonlight(Owner, PileType.Draw, DynamicVars.Cards.IntValue, upgraded, CardPilePosition.Random);
        await DongmingSkillMechanics.CreateMoonlight(Owner, PileType.Hand, DynamicVars.Cards.IntValue, upgraded);
        await DongmingSkillMechanics.CreateMoonlight(Owner, PileType.Discard, DynamicVars.Cards.IntValue, upgraded);
    }
    protected override void OnUpgrade() =>
        DynamicVars["GeneratedUpgrade"].UpgradeValueBy(1m);
}

public class YuBu() : DongmingCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        List<CardModel> cards = PileType.Draw.GetPile(Owner).Cards.ToList();
        int count = Math.Min(DynamicVars.Cards.IntValue, cards.Count);
        if (count == 0) return;
        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(c, cards, Owner, new CardSelectorPrefs(SelectionScreenPrompt, count, count));
        foreach (CardModel card in selected) await CardPileCmd.Add(card, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public class BaiMang() : DongmingCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override int CanonicalStarCost => 3;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FrostPower>(18m), new IntVar("FrostPerNingHua", 6m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await FrostMechanics.Gain(c, Owner.Creature, DynamicVars["FrostPower"].BaseValue, Owner.Creature, this);
        int stacks = (int)Math.Floor(
            (Owner.Creature.Powers.OfType<FrostPower>().FirstOrDefault()?.Amount ?? 0m) /
            DynamicVars["FrostPerNingHua"].IntValue);
        if (stacks > 0) await PowerCmd.Apply<NingHuaPower>(c, Owner.Creature, stacks, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["FrostPower"].UpgradeValueBy(6m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FrostPower>(),
        HoverTipFactory.FromPower<NingHuaPower>()
    ];
}

public class HuaYue() : DongmingCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [
            new BlockVar(7m, ValueProp.Move),
            new IntVar("GeneratedUpgrade", 0m)
        ];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);

        bool generatedUpgraded =
            DynamicVars["GeneratedUpgrade"].IntValue > 0;
        foreach (CardModel status in PileType.Hand.GetPile(Owner).Cards
                     .Where(x => x.Type == CardType.Status)
                     .ToList())
        {
            YueHua moonlight = ((MegaCrit.Sts2.Core.Combat.CombatState)CombatState)
                .CreateCard<YueHua>(Owner);
            if (generatedUpgraded) CardCmd.Upgrade(moonlight);
            await CardCmd.Transform(status, moonlight);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["GeneratedUpgrade"].UpgradeValueBy(1m);
    }
}
