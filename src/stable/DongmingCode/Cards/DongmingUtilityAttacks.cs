using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Cards;

public class WangYue() : MoonPhaseCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy,
    MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("BrightDamage", 9m, ValueProp.Move),
        new DamageVar("DarkDamage", 13m, ValueProp.Move),
        new PowerVar<XuShiYinPower>(1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool bright = CurrentMoonPhase == MoonPhase.Bright;
        await DamageCmd.Attack(
                DynamicVars[bright ? "BrightDamage" : "DarkDamage"].BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<XuShiYinPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["XuShiYinPower"].BaseValue,
            Owner.Creature,
            this);
        if (!bright)
        {
            Dazed dazed = ((MegaCrit.Sts2.Core.Combat.CombatState)CombatState)
                .CreateCard<Dazed>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                dazed,
                PileType.Draw,
                Owner,
                CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BrightDamage"].UpgradeValueBy(3m);
        DynamicVars["DarkDamage"].UpgradeValueBy(4m);
    }
}

public class CanGuang() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["VulnerablePower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
    }
}

public class YingYue() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AllEnemies)
{
    public override int CanonicalStarCost => 3;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(3),
        new PowerVar<StrengthPower>(1m),
        new IntVar("EnemyStrengthLoss", 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        IReadOnlyList<Creature> targets = CombatState.HittableEnemies.ToList();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature,
            this);
        foreach (Creature target in targets.Where(target => target.IsAlive))
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                target,
                -DynamicVars["EnemyStrengthLoss"].BaseValue,
                Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["StrengthPower"].UpgradeValueBy(1m);
    }
}

public class XinYue() : DongmingCard(
    3,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 3;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PlatingPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(24m, ValueProp.Move)];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        if (!cardPlay.Target.IsAlive)
            return;
        if (cardPlay.Target.Block > 0)
        {
            await CreatureCmd.LoseBlock(cardPlay.Target, cardPlay.Target.Block);
        }
        foreach (PlatingPower plating in cardPlay.Target.Powers
                     .OfType<PlatingPower>()
                     .ToList())
        {
            await PowerCmd.Remove(plating);
        }
        await CreatureCmd.Stun(cardPlay.Target, null);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

public class GuGuang() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CursePower>(),
        HoverTipFactory.FromPower<ExhaustionPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        ExhaustionPursuit.CreateTip(this)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new IntVar("Curse", 2m),
        new IntVar("Weak", 2m),
        new IntVar("Vulnerable", 2m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        IReadOnlyList<Creature> targets = CombatState.HittableEnemies.ToList();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        foreach (Creature target in targets.Where(target => target.IsAlive))
        {
            if (DongmingAttackMechanics.IsExhausted(target))
            {
                await PowerCmd.Apply<WeakPower>(
                    choiceContext,
                    target,
                    DynamicVars["Weak"].BaseValue,
                    Owner.Creature,
                    this);
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext,
                    target,
                    DynamicVars["Vulnerable"].BaseValue,
                    Owner.Creature,
                    this);
            }
            else
            {
                await DongmingAttackMechanics.ApplyCurse(
                    choiceContext,
                    Owner,
                    target,
                    DynamicVars["Curse"].BaseValue,
                    this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["Curse"].UpgradeValueBy(1m);
        DynamicVars["Weak"].UpgradeValueBy(1m);
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
    }
}

public class YuJian() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<XuShiYinPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new CardsVar(2)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PowerCmd.Apply<XuShiYinPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

public class ZhaoYing() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new PowerVar<WeakPower>(2m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["WeakPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }
}

public class DuanYue() : DongmingCard(
    1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 2;
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9m, ValueProp.Move)];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        if (cardPlay.Target.IsAlive &&
            cardPlay.Target.Monster is MonsterModel monster &&
            !monster.IntendsToAttack)
        {
            await CreatureCmd.Stun(cardPlay.Target, null);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

public class DuoYing() : DongmingCard(
    0,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy)
{
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CursePower>(),
        HoverTipFactory.FromPower<ExhaustionPower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        ExhaustionPursuit.CreateTip(this)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new IntVar("Curse", 4m),
        new IntVar("Weak", 3m),
        new IntVar("Vulnerable", 3m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        if (DongmingAttackMechanics.IsExhausted(cardPlay.Target))
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["Weak"].BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["Vulnerable"].BaseValue,
                Owner.Creature,
                this);
        }
        else
        {
            await DongmingAttackMechanics.ApplyCurse(
                choiceContext,
                Owner,
                cardPlay.Target,
                DynamicVars["Curse"].BaseValue,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Curse"].UpgradeValueBy(1m);
        DynamicVars["Weak"].UpgradeValueBy(1m);
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
    }
}
