using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.DongmingCode.Cards;

public class CaiYue() : DongmingCard(
    0,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    public override bool HasStarCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int moonlightSpent = ResolveStarXValue();
        if (moonlightSpent <= 0)
        {
            return;
        }

        decimal damage = DynamicVars.Damage.BaseValue;
        AttackCommand attack = DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .WithHitCount(moonlightSpent)
            .WithHitFx("vfx/vfx_starry_impact", null, "slash_attack.mp3");
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        attack.Targeting(cardPlay.Target);

        await attack.Execute(choiceContext);
        if (!cardPlay.Target.IsAlive)
        {
            await MoonlightMechanics.Gain(
                Owner,
                Math.Floor(moonlightSpent / 2m));
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

public class HanChe() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new IntVar("FrostPerHit", 5m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        int frostAmount = DongmingAttackMechanics.FrostAmount(Owner);
        int hitCount = 1 + frostAmount / DynamicVars["FrostPerHit"].IntValue;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        int block = (int)Math.Floor(frostAmount * 0.5m);
        if (block > 0)
            await CreatureCmd.GainBlock(
                Owner.Creature,
                new BlockVar(block, ValueProp.Move),
                cardPlay);
        await DongmingAttackMechanics.ConsumeFrost(
            choiceContext,
            Owner,
            frostAmount,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["FrostPerHit"].UpgradeValueBy(-2m);
    }
}

public class EJie() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>(),
        HoverTipFactory.FromPower<CalamityPower>(),
        HoverTipFactory.FromPower<ExhaustionPower>(),
        ExhaustionPursuit.CreateTip(this)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new IntVar("CurseDamage", 5m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int curseOrStatusCount = Owner.Deck.Cards.Count(
            card => card.Type is CardType.Curse or CardType.Status);
        decimal damage = DynamicVars.Damage.BaseValue +
                         curseOrStatusCount *
                         DynamicVars["CurseDamage"].BaseValue;
        damage = DongmingAttackMechanics.DoubleAgainstExhaustion(
            cardPlay.Target,
            damage);

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["CurseDamage"].UpgradeValueBy(3m);
    }
}

public class SuiShuang() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new ExtraDamageVar(10m),
        new IntVar("FrostCost", 5m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool empowered = DongmingAttackMechanics.FrostAmount(Owner) >
                         DynamicVars["FrostCost"].IntValue;
        decimal damage = DynamicVars.Damage.BaseValue +
                         (empowered ? DynamicVars.ExtraDamage.BaseValue : 0m);
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        if (empowered)
        {
            await DongmingAttackMechanics.ConsumeFrost(
                choiceContext,
                Owner,
                DynamicVars["FrostCost"].IntValue,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.ExtraDamage.UpgradeValueBy(5m);
    }
}

public class GuiYue() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        List<CardModel> candidates = PileType.Discard.GetPile(Owner).Cards.ToList();
        int count = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        if (count <= 0)
        {
            return;
        }

        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, count)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            });
        await DongmingAttackMechanics.MoveSelectedCardsToDraw(selected);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

public class ZhuiXingHe() : DongmingCard(
    0,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 3;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<XuShiYinPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<XuShiYinPower>(2m),
        new CardsVar(2)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        int marks = DynamicVars["XuShiYinPower"].IntValue;
        await PowerCmd.Apply<XuShiYinPower>(
            choiceContext, Owner.Creature, marks, Owner.Creature, this);
        List<CardModel> candidates = PileType.Draw.GetPile(Owner).Cards
            .Where(MoonlightMechanics.IsCursedCard)
            .ToList();
        if (candidates.Count > 0)
        {
            int maximum = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
            IEnumerable<CardModel> selected =
                await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    candidates,
                    Owner,
                    new CardSelectorPrefs(
                        new LocString(
                            "cards",
                            $"{Id.Entry}.drawSelectionScreenPrompt"),
                        0,
                        maximum)
                    {
                        Cancelable = true,
                        RequireManualConfirmation = true
                    });
            await DongmingAttackMechanics.MoveSelectedCardsToHand(
                selected.Distinct().ToList());
        }
        // 立即触发的上限取当前总层数（原来只取本牌给的层数）
        await DongmingSkillMechanics.SelectAndToggleWithCurrentMarks(
            choiceContext, Owner, this, consumeMark: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["XuShiYinPower"].UpgradeValueBy(1m);
    }
}


