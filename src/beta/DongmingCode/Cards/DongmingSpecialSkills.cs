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

public class RangZai() : DongmingCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<XuShiYinPower>(1m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await DongmingSkillMechanics.CreateCanShi(c, Owner, this);
        int marks = DynamicVars["XuShiYinPower"].IntValue;
        await PowerCmd.Apply<XuShiYinPower>(
            c, Owner.Creature, marks, Owner.Creature, this);
        // 立即触发的上限取当前总层数（原来只取本牌给的层数，存量多时会被卡在 2 张）
        await DongmingSkillMechanics.SelectAndToggleWithCurrentMarks(
            c, Owner, this, consumeMark: true);
    }
    protected override void OnUpgrade() =>
        DynamicVars["XuShiYinPower"].UpgradeValueBy(1m);
}

public class ShiE() : DongmingCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override int CanonicalStarCost => 2;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("StrengthLoss", 6m), new PowerVar<StrengthPower>(1m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        foreach (Creature enemy in CombatState.HittableEnemies.ToList())
            await PowerCmd.Apply<ShiEStrengthDownPower>(c, enemy, DynamicVars["StrengthLoss"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(c, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars["StrengthLoss"].UpgradeValueBy(3m); DynamicVars["StrengthPower"].UpgradeValueBy(1m); }
}

public class LiaoYue() : DongmingCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    /// <summary>打出时额外消耗 1 点月华。</summary>
    public override int CanonicalStarCost => 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1), new IntVar("GeneratedUpgrade", 0m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        List<CardModel> candidates = PileType.Hand.GetPile(Owner).Cards.Where(x => !ReferenceEquals(x, this)).ToList();
        if (candidates.Count == 0) return;
        CardModel selected = (await CardSelectCmd.FromHand(c, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 1, 1), candidates.Contains, this)).First();
        await CardCmd.Exhaust(c, selected);
        await DongmingSkillMechanics.CreateMoonlight(
            Owner,
            PileType.Hand,
            1,
            DynamicVars["GeneratedUpgrade"].IntValue > 0);
    }
    protected override void OnUpgrade() =>
        DynamicVars["GeneratedUpgrade"].UpgradeValueBy(1m);
}

public class QingHuiLiuXie() : DongmingCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override int CanonicalStarCost => 3;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2), new EnergyVar("NextEnergy", 1)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await PowerCmd.Apply<EnergyNextTurnPower>(c, Owner.Creature, DynamicVars["NextEnergy"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);
}

public class NingBai() : DongmingCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override int CanonicalStarCost => 1;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
        await PowerCmd.Apply<NingBaiPower>(c, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];
}

public class TaoGuang() : DongmingCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override int CanonicalStarCost => 2;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate, CardKeyword.Retain, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        List<CardModel> cards = PileType.Draw.GetPile(Owner).Cards
            .Where(IsSelectableCard).ToList();
        int count = Math.Min(DynamicVars.Cards.IntValue, cards.Count);
        if (count == 0) return;
        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            c, cards, Owner, new CardSelectorPrefs(SelectionScreenPrompt, 0, count));
        foreach (CardModel card in selected) CardCmd.ApplyKeyword(card, CardKeyword.Retain);
    }
    internal static bool IsSelectableCard(CardModel card) =>
        card.Type is not (CardType.Curse or CardType.Status);

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}

public class CengNing() : DongmingCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FrostPower>(5m), new IntVar("BonusX", 0m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await FrostMechanics.Gain(
            c,
            Owner.Creature,
            DynamicVars["FrostPower"].BaseValue *
                (ResolveEnergyXValue() + DynamicVars["BonusX"].IntValue),
            Owner.Creature,
            this);
        decimal totalFrost = Owner.Creature.Powers
            .OfType<FrostPower>()
            .Sum(power => power.Amount);
        int ningHua = (int)Math.Floor(totalFrost / 5m);
        if (ningHua > 0)
            await PowerCmd.Apply<NingHuaPower>(
                c, Owner.Creature, ningHua, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["BonusX"].UpgradeValueBy(1m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];
}

public class QianZai() : MoonPhaseCard(
    1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, MoonPhase.Bright)
{
    protected override string BrightDescriptionKey =>
        IsUpgraded
            ? $"{Id.Entry}.description.bright.upgraded"
            : base.BrightDescriptionKey;

    protected override string DarkDescriptionKey =>
        IsUpgraded
            ? $"{Id.Entry}.description.dark.upgraded"
            : base.DarkDescriptionKey;

    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    protected override bool HasExhaustionPursuit(MoonPhase phase) => true;
    public override int CanonicalStarCost => 1;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VulnerablePower>(2m),
        new PowerVar<WeakPower>("Weak", 2m),
        new IntVar("Curse", 5m)
    ];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        ArgumentNullException.ThrowIfNull(p.Target);
        if (CurrentMoonPhase == MoonPhase.Bright)
            await PowerCmd.Apply<VulnerablePower>(
                c, p.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
        else
            await PowerCmd.Apply<WeakPower>(
                c, p.Target, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<CursePower>(c, p.Target, DynamicVars["Curse"].BaseValue, Owner.Creature, this);
        if (DongmingAttackMechanics.IsExhausted(p.Target))
        {
            await PowerCmd.ModifyAmount(
                c,
                p.Target.Powers.OfType<ExhaustionPower>().First(),
                1m,
                Owner.Creature,
                this);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
        DynamicVars["Weak"].UpgradeValueBy(1m);

    }

    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
    [
        HoverTipFactory.FromPower<CursePower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];
}

