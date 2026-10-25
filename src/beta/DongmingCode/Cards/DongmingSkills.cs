using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Cards;

public class YueYou() : DongmingCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override int CanonicalStarCost => 1;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(9m, ValueProp.Move)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}

public class YueZhuan() : MoonPhaseCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override int CanonicalStarCost => 2;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6m, ValueProp.Move), new CardsVar(3), new EnergyVar("DarkEnergy", 1)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
        if (CurrentMoonPhase == MoonPhase.Bright)
            await CardPileCmd.Draw(c, DynamicVars.Cards.BaseValue, Owner);
        else
            await PlayerCmd.GainEnergy(DynamicVars["DarkEnergy"].BaseValue, Owner);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["DarkEnergy"].UpgradeValueBy(1m);
    }
}

public class YueXian() : DongmingCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override int CanonicalStarCost => 5;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IntangiblePower>(2m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => PowerCmd.Apply<IntangiblePower>(c, Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); AddKeyword(CardKeyword.Retain); }
}

public class CuiXu() : DongmingCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<XuShiYinPower>()];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<XuShiYinPower>(1m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        PurifiedVoid card = ((MegaCrit.Sts2.Core.Combat.CombatState)Owner.Creature.CombatState)
            .CreateCard<PurifiedVoid>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            card, PileType.Draw, Owner, CardPilePosition.Top);
        await PowerCmd.Apply<XuShiYinPower>(c, Owner.Creature, DynamicVars["XuShiYinPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["XuShiYinPower"].UpgradeValueBy(1m);
}

public class XianHen() : DongmingCard(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 1;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [
            HoverTipFactory.FromPower<CursePower>(),
            HoverTipFactory.FromPower<DebilitatePower>(),
            ExhaustionPursuit.CreateTip(this)
        ];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<CursePower>(4m)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        ArgumentNullException.ThrowIfNull(p.Target);
        decimal amount = DynamicVars["CursePower"].BaseValue;
        if (DongmingAttackMechanics.IsExhausted(p.Target))
        {
            await PowerCmd.Apply<DebilitatePower>(
                c, p.Target, 2m, Owner.Creature, this);
        }
        else
        {
            await PowerCmd.Apply<CursePower>(
                c, p.Target, amount, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => DynamicVars["CursePower"].UpgradeValueBy(1m);
}

public class YunKai() : MoonPhaseCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override int CanonicalStarCost => 2;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VulnerablePower>(3m), new PowerVar<DebilitatePower>(3m)];
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<DebilitatePower>()];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        ArgumentNullException.ThrowIfNull(p.Target);
        if (p.Target.Block > 0)
            await CreatureCmd.LoseBlock(
                c,
                p.Target,
                (decimal)p.Target.Block,
                Owner.Creature);
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            foreach (PowerModel power in p.Target.Powers.Where(x => x is IntangiblePower or PlatingPower or ArtifactPower).ToList())
                await PowerCmd.Remove(power);
        }
        else
        {
            await PowerCmd.Apply<VulnerablePower>(c, p.Target,
                DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<DebilitatePower>(c, p.Target,
                DynamicVars["DebilitatePower"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public class XingYi() : DongmingCard(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 1;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VigorPower>(), ExhaustionPursuit.CreateTip(this)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        ArgumentNullException.ThrowIfNull(p.Target);
        decimal amount = DongmingAttackMechanics.IsExhausted(p.Target)
            ? 12m
            : Math.Floor((p.Target.Powers.OfType<CursePower>().FirstOrDefault()?.Amount ?? 0m) / 2m);
        if (amount > 0m)
            await PowerCmd.Apply<VigorPower>(
                c, Owner.Creature, amount, Owner.Creature, this);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public class ShuangYin() : MoonPhaseCard(
    1, CardType.Skill, CardRarity.Common, TargetType.Self, MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override int CanonicalStarCost => 1;
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new PowerVar<SnowfallenMod.SnowfallenCode.Powers.FrostPower>(3m),
        new IntVar("DarkFrost", 6m)
    ];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
            await FrostMechanics.Gain(c, Owner.Creature,
                DynamicVars["FrostPower"].BaseValue, Owner.Creature, this);
        }
        else
        {
            await FrostMechanics.Gain(c, Owner.Creature,
                DynamicVars["DarkFrost"].BaseValue, Owner.Creature, this);
        }
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["FrostPower"].UpgradeValueBy(2m);
        DynamicVars["DarkFrost"].UpgradeValueBy(2m);
    }

    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<SnowfallenMod.SnowfallenCode.Powers.FrostPower>()];
}

public class SuGuo() : DongmingCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SnowfallenMod.SnowfallenCode.Powers.FrostPower>(7m)];
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => FrostMechanics.Gain(c, Owner.Creature, DynamicVars["FrostPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["FrostPower"].UpgradeValueBy(3m);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<SnowfallenMod.SnowfallenCode.Powers.FrostPower>()];
}

public class ShouShi() : DongmingCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move), new EnergyVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, p);
        await PowerCmd.Apply<EnergyNextTurnPower>(c, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<XuShiYinPower>(c, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

