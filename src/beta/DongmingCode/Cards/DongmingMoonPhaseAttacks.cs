using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Powers;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.DongmingCode.Cards;

public class ZhouJi() : MoonPhaseCard(
    1,
    CardType.Attack,
    CardRarity.Basic,
    TargetType.AnyEnemy,
    MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    protected override bool ShowExhaustionPursuitInPhaseDescription => false;
    protected override bool HasExhaustionPursuit(MoonPhase phase) =>
        true;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;

    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
    [
        HoverTipFactory.FromPower<CursePower>(),
        HoverTipFactory.FromPower<DoomPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("BrightDamage", 9m, ValueProp.Move),
        new DamageVar("DarkDamage", 9m, ValueProp.Move),
        new IntVar("BrightCurse", 4m),
        new IntVar("DoomReduction", 6m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool bright = CurrentMoonPhase == MoonPhase.Bright;
        decimal damage = DynamicVars[bright ? "BrightDamage" : "DarkDamage"].BaseValue;
        damage = DongmingAttackMechanics.CurseScalingDamage(cardPlay.Target, damage);
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        if (bright)
        {
            await DongmingAttackMechanics.ApplyCurse(
                choiceContext,
                Owner,
                cardPlay.Target,
                DynamicVars["BrightCurse"].BaseValue,
                this);
        }
        else
        {
            await DongmingAttackMechanics.ReduceDoom(
                choiceContext,
                Owner,
                DynamicVars["DoomReduction"].BaseValue,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BrightDamage"].UpgradeValueBy(3m);
        DynamicVars["DarkDamage"].UpgradeValueBy(3m);
        DynamicVars["BrightCurse"].UpgradeValueBy(1m);
        DynamicVars["DoomReduction"].UpgradeValueBy(4m);
    }
}

public class EZhou() : MoonPhaseCard(
    1,
    CardType.Attack,
    CardRarity.Ancient,
    TargetType.AllEnemies,
    MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    protected override bool ShowExhaustionPursuitInPhaseDescription => false;
    protected override bool HasExhaustionPursuit(MoonPhase phase) =>
        true;
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
    [
        HoverTipFactory.FromPower<CursePower>(),
        HoverTipFactory.FromPower<DebilitatePower>(),
        HoverTipFactory.FromPower<DoomPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("BrightDamage", 15m, ValueProp.Move),
        new DamageVar("DarkDamage", 15m, ValueProp.Move),
        new IntVar("BrightCurse", 4m),
        new IntVar("BrightDebilitate", 3m),
        new IntVar("BrightWeak", 3m),
        new IntVar("DoomReduction", 10m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        bool bright = CurrentMoonPhase == MoonPhase.Bright;
        IReadOnlyList<Creature> targets = CombatState.HittableEnemies.ToList();
        decimal damage = DynamicVars[bright ? "BrightDamage" : "DarkDamage"].BaseValue;
        foreach (Creature target in targets.Where(target => target.IsAlive))
        {
            await DamageCmd.Attack(
                    DongmingAttackMechanics.CurseScalingDamage(target, damage))
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
                .Execute(choiceContext);
        }

        if (bright)
        {
            foreach (Creature target in targets.Where(target => target.IsAlive))
            {
                await DongmingAttackMechanics.ApplyCurse(
                    choiceContext, Owner, target,
                    DynamicVars["BrightCurse"].BaseValue, this);
                if (!DongmingAttackMechanics.IsExhausted(target))
                    continue;
                await PowerCmd.Apply<DebilitatePower>(
                    choiceContext, target,
                    DynamicVars["BrightDebilitate"].BaseValue,
                    Owner.Creature, this);
                await PowerCmd.Apply<WeakPower>(
                    choiceContext, target,
                    DynamicVars["BrightWeak"].BaseValue,
                    Owner.Creature, this);
            }
        }
        else
        {
            foreach (Creature target in targets.Where(target => target.IsAlive &&
                         DongmingAttackMechanics.IsExhausted(target)))
            {
                await PowerCmd.Apply<DebilitatePower>(choiceContext, target,
                    DynamicVars["BrightDebilitate"].BaseValue, Owner.Creature, this);
                await PowerCmd.Apply<WeakPower>(choiceContext, target,
                    DynamicVars["BrightWeak"].BaseValue, Owner.Creature, this);
            }
            await DongmingAttackMechanics.ReduceDoom(
                choiceContext,
                Owner,
                DynamicVars["DoomReduction"].BaseValue,
                this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BrightDamage"].UpgradeValueBy(6m);
        DynamicVars["DarkDamage"].UpgradeValueBy(6m);
        DynamicVars["BrightCurse"].UpgradeValueBy(1m);
        DynamicVars["DoomReduction"].UpgradeValueBy(6m);
    }
}

public class CuiYue() : MoonPhaseCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy,
    MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override int CanonicalStarCost => 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("BrightDamage", 18m, ValueProp.Move),
        new DamageVar("DarkDamage", 6m, ValueProp.Move),
        new RepeatVar(3)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool bright = CurrentMoonPhase == MoonPhase.Bright;
        await DamageCmd.Attack(DynamicVars[bright ? "BrightDamage" : "DarkDamage"].BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(bright ? 1 : DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        Debris debris = ((MegaCrit.Sts2.Core.Combat.CombatState)CombatState)
            .CreateCard<Debris>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            debris, PileType.Hand, Owner, CardPilePosition.Bottom);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BrightDamage"].UpgradeValueBy(6m);
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}

public class ShiYan() : DongmingCard(
    2,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(13m, ValueProp.Move)];

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

        MegaCrit.Sts2.Core.Combat.CombatState combat =
            (MegaCrit.Sts2.Core.Combat.CombatState)CombatState;
        foreach (PileType pile in new[] { PileType.Draw, PileType.Hand, PileType.Discard })
        {
            PurifiedWound wound = combat.CreateCard<PurifiedWound>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(
                wound,
                pile,
                Owner,
                CardPilePosition.Bottom);
        }
    }

    protected override void OnUpgrade() =>
        DynamicVars.Damage.UpgradeValueBy(4m);
}

public class YinXue() : MoonPhaseCard(
    1,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AnyEnemy,
    MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<FrostPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("BrightDamage", 6m, ValueProp.Move),
        new DamageVar("DarkDamage", 4m, ValueProp.Move),
        new PowerVar<FrostPower>(5m),
        new PowerVar<FrostPower>("DarkFrost", 3m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(DynamicVars["BrightDamage"].BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
                .Execute(choiceContext);
            await FrostMechanics.Gain(
                choiceContext,
                Owner.Creature,
                DynamicVars["FrostPower"].BaseValue,
                Owner.Creature,
                this);
            return;
        }

        int enemyCount = CombatState.HittableEnemies.Count;
        await DamageCmd.Attack(DynamicVars["DarkDamage"].BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        await FrostMechanics.Gain(
            choiceContext,
            Owner.Creature,
            DynamicVars["DarkFrost"].BaseValue * enemyCount,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BrightDamage"].UpgradeValueBy(2m);
        DynamicVars["DarkDamage"].UpgradeValueBy(1m);
        DynamicVars["FrostPower"].UpgradeValueBy(2m);
        DynamicVars["DarkFrost"].UpgradeValueBy(1m);
    }
}

public class PingXin() : MoonPhaseCard(
    1,
    CardType.Attack,
    CardRarity.Uncommon,
    TargetType.AnyEnemy,
    MoonPhase.Bright)
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    public override int CanonicalStarCost => 1;

    protected override IEnumerable<IHoverTip> AdditionalMoonPhaseHoverTips =>
        [HoverTipFactory.FromPower<DoomPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("BrightDamage", 9m, ValueProp.Move),
        new DamageVar("DarkDamage", 12m, ValueProp.Move),
        new IntVar("BrightDoomReduction", 4m),
        new IntVar("DarkDoomReduction", 6m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (CurrentMoonPhase == MoonPhase.Bright)
        {
            int enemyCount = CombatState.HittableEnemies.Count;
            await DamageCmd.Attack(DynamicVars["BrightDamage"].BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
                .Execute(choiceContext);
            await DongmingAttackMechanics.ReduceDoomOnly(
                choiceContext,
                Owner,
                DynamicVars["BrightDoomReduction"].BaseValue * enemyCount,
                this);
            return;
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars["DarkDamage"].BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);
        await DongmingAttackMechanics.ReduceDoomOnly(
            choiceContext,
            Owner,
            DynamicVars["DarkDoomReduction"].BaseValue,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BrightDoomReduction"].UpgradeValueBy(2m);
        DynamicVars["DarkDoomReduction"].UpgradeValueBy(4m);
    }
}

public class NingYuan() : MoonPhaseCard(
    1,
    CardType.Attack,
    CardRarity.Rare,
    TargetType.AnyEnemy,
    MoonPhase.Bright), ICombatPhaseFlipListener
{
    protected override bool ShowOtherPhaseInHoverTipOnly => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new IntVar("BrightFlipCount", 0m),
        new IntVar("DarkFlipCount", 0m),
        new IntVar("BrightDamageStacks", 0m),
        new IntVar("BrightHitStacks", 0m),
        new IntVar("BrightStrengthStacks", 0m),
        new IntVar("DarkVulnerableStacks", 0m),
        new IntVar("DarkWeakStacks", 0m),
        new IntVar("DarkDexterityStacks", 0m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool bright = CurrentMoonPhase == MoonPhase.Bright;
        decimal damage = DynamicVars.Damage.BaseValue +
                         (bright
                             ? DynamicVars["BrightDamageStacks"].BaseValue * 5m
                             : 0m);
        int hitCount = 1 + (bright
            ? DynamicVars["BrightHitStacks"].IntValue
            : 0);
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
            .Execute(choiceContext);

        if (bright &&
            DynamicVars["BrightStrengthStacks"].IntValue > 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["BrightStrengthStacks"].BaseValue,
                Owner.Creature,
                this);
        }
        if (!bright &&
            DynamicVars["DarkDexterityStacks"].IntValue > 0)
        {
            await PowerCmd.Apply<DexterityPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["DarkDexterityStacks"].BaseValue,
                Owner.Creature,
                this);
        }
        if (!bright &&
            DynamicVars["DarkVulnerableStacks"].IntValue > 0)
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["DarkVulnerableStacks"].BaseValue,
                Owner.Creature,
                this);
        if (!bright &&
            DynamicVars["DarkWeakStacks"].IntValue > 0)
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                cardPlay.Target,
                DynamicVars["DarkWeakStacks"].BaseValue,
                Owner.Creature,
                this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }

    public async Task OnCombatPhaseFlipped(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext
            choiceContext)
    {
        int brightRoll = Owner.RunState.Rng.CombatCardGeneration.NextInt(3);
        int darkRoll = Owner.RunState.Rng.CombatCardGeneration.NextInt(3);
        ApplyFlipRolls(brightRoll, darkRoll);
        MoonPhaseMechanics.RefreshCardVisuals(this);
        await Task.CompletedTask;
    }

    internal void ApplyFlipRolls(int brightRoll, int darkRoll)
    {
        Increment("BrightFlipCount");
        switch (brightRoll)
        {
            case 0:
                Increment("BrightDamageStacks");
                break;
            case 1:
                Increment("BrightHitStacks");
                break;
            default:
                Increment("BrightStrengthStacks");
                break;
        }

        Increment("DarkFlipCount");
        switch (darkRoll)
        {
            case 0:
                Increment("DarkVulnerableStacks");
                break;
            case 1:
                Increment("DarkWeakStacks");
                break;
            default:
                Increment("DarkDexterityStacks");
                break;
        }

    }

    private void Increment(string key) =>
        DynamicVars[key].BaseValue += 1m;
}


