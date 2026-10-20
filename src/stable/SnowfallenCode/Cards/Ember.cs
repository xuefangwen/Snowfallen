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
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class Ember() : SnowfallenCard(
    0,
    CardType.Attack,
    CardRarity.Token,
    TargetType.AnyEnemy), IEmberCard
{
    private decimal _temporaryDamageBonus;
    private decimal _playBlock;
    private int _extraDrawOnPlay;
    private bool _isAoe;
    private bool _isAoeThisTurn;
    private int _weakOnPlay;
    private int _vulnerableOnPlay;
    private bool _appliesTwist;
    private bool _returnNextTurn;
    private int _replayCount;
    private long _mergeOrder;
    private bool _isFusionSubject;

    public override HashSet<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WaterBreathPower>();
            yield return HoverTipFactory.FromPower<EmberTrackerPower>();
            yield return FusionHoverTip();
            if (_isFusionSubject)
            {
                yield return HoverTipFactory.FromPower<EmberFusionSubjectPower>();
            }
            if (_vulnerableOnPlay > 0)
            {
                yield return HoverTipFactory.FromPower<VulnerablePower>();
            }
            if (_weakOnPlay > 0)
            {
                yield return HoverTipFactory.FromPower<WeakPower>();
            }
            if (_appliesTwist)
            {
                yield return HoverTipFactory.FromPower<TwistPower>();
            }
        }
    }

    private static IHoverTip FusionHoverTip()
    {
        return new HoverTip(
            new LocString("powers", "SNOWFALLENMOD-EMBER_FUSION.title"),
            new LocString("powers", "SNOWFALLENMOD-EMBER_FUSION.description"),
            ModelDb.Power<EmberTrackerPower>().Icon);
    }

    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new PowerVar<WaterBreathPower>(1m),
        new DynamicVar("Growth", 6m)
    ];

    public override async Task BeforeCombatStart()
    {
        // Fusion belongs to Ember itself. Initialize the hidden bookkeeping
        // before any card is played so end-of-turn discard fusion is
        // available from the first turn.
        await EmberMechanics.EnsureTracker(
            new BlockingPlayerChoiceContext(),
            Owner,
            CombatState,
            this);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (!IsAoe) ArgumentNullException.ThrowIfNull(cardPlay.Target);

        LakeLightEmberDamagePower lakeLight = Owner.Creature.Powers
            .OfType<LakeLightEmberDamagePower>()
            .FirstOrDefault();
        decimal lakeLightDamage = lakeLight?.Amount ?? 0m;
        if (lakeLight is not null)
        {
            await PowerCmd.Remove(lakeLight);
        }
        else
        {
            // Compatibility with combat saves made before the usage tracker
            // and the one-shot damage buff became separate powers.
            LakeLightMaxHpPower legacyLakeLight = Owner.Creature.Powers
                .OfType<LakeLightMaxHpPower>()
                .FirstOrDefault(power => !power.IsUsageTracker);
            if (legacyLakeLight is not null)
            {
                lakeLightDamage = legacyLakeLight.Amount;
                await PowerCmd.Remove(legacyLakeLight);
            }
        }

        int resolutions = 1 + _replayCount;
        for (int i = 0; i < resolutions; i++)
        {
            await WaterBreathMechanics.Gain(
                choiceContext,
                Owner.Creature,
                DynamicVars["WaterBreathPower"].BaseValue,
                Owner.Creature,
                this);
            await ResolvePlayEffect(
                choiceContext,
                cardPlay.Target,
                lakeLightDamage);
        }
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card, this))
        {
            return;
        }

        EmberTrackerPower tracker = await EmberMechanics.EnsureTracker(
            choiceContext,
            Owner,
            CombatState,
            this);
        tracker.RecordPlayed();
        if (_extraDrawOnPlay > 0)
        {
            await CardPileCmd.Draw(choiceContext, _extraDrawOnPlay, Owner);
        }
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel clonedBy)
    {
        if (!ReferenceEquals(card, this) ||
            Pile?.Type != PileType.Discard ||
            oldPileType == PileType.Discard)
        {
            return;
        }

        // Do not make the passive depend on an earlier Ember play. Generated
        // or externally moved Embers can be the first Ember seen in combat,
        // so the card itself guarantees the hidden tracker exists here.
        EmberTrackerPower tracker = await EmberMechanics.EnsureTracker(
            new BlockingPlayerChoiceContext(),
            Owner,
            CombatState,
            this);

        // Kingly Punch keeps its combat-long growth directly on the card
        // instance. Ember follows the same model: every real transition into
        // Discard immediately and permanently increases this card's damage.
        AddPermanentDamageBonus(
            DynamicVars["Growth"].BaseValue +
            tracker.GlobalPlayGrowthBonus);

        // Fusion is intentionally not started from this pile-change callback.
        // At the end of the owner's turn the tracker lets the player select a
        // subject and materials from the now-stable discard pile.
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (!ReferenceEquals(card, this))
        {
            return;
        }

        EmberTrackerPower tracker = await EmberMechanics.EnsureTracker(
            choiceContext,
            Owner,
            CombatState,
            this);
        tracker.RecordExhausted();

        RemainingWarmthPowerBase remainingWarmth = Owner.Creature.Powers
            .OfType<RemainingWarmthPowerBase>()
            .FirstOrDefault();
        if (remainingWarmth is not null &&
            await remainingWarmth.TryClaimNextEmber(choiceContext))
        {
            AddPermanentDamageBonus(6m);
            _returnNextTurn = true;
        }

        foreach (FinalStrokePower finalStroke in
                 Owner.Creature.Powers.OfType<FinalStrokePower>())
        {
            finalStroke.RecordExhaustedEmber(this);
        }
    }

    public override async Task AfterCardGeneratedForCombat(
        CardModel card,
        MegaCrit.Sts2.Core.Entities.Players.Player creator)
    {
        if (!ReferenceEquals(card, this) || creator != Owner)
        {
            return;
        }

        EmberTrackerPower tracker = await EmberMechanics.EnsureTracker(
            new BlockingPlayerChoiceContext(),
            Owner,
            CombatState,
            this);
        tracker.RecordGenerated();

        IReadOnlyList<GlimmerPowerBase> glimmerPowers = Owner.Creature.Powers
            .OfType<GlimmerPowerBase>()
            .ToList();
        AddReplayBonus(glimmerPowers.Sum(power => (int)power.Amount));
        AddPermanentBlockBonus(glimmerPowers.Sum(
            power => power.ExtraBlockPerStack * power.Amount));
    }

    private async Task ResolvePlayEffect(
        PlayerChoiceContext choiceContext,
        Creature selectedTarget,
        decimal oneShotDamageBonus)
    {
        IReadOnlyList<Creature> targets = IsAoe
            ? CombatState.HittableEnemies.ToList()
            : [selectedTarget];
        if (targets.Count == 0)
        {
            return;
        }

        AttackCommand attack = DamageCmd.Attack(
                DynamicVars.Damage.BaseValue + Math.Max(0m, oneShotDamageBonus))
            .FromCard(this)
            .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3");
        if (IsAoe)
        {
            attack.TargetingAllOpponents(CombatState);
        }
        else
        {
            attack.Targeting(selectedTarget);
        }

        await attack.Execute(choiceContext);

        decimal gatherVulnerable = Owner.Creature.Powers
            .OfType<GatherEmbersPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        decimal weak = _weakOnPlay;
        foreach (Creature target in targets.Where(target => !target.IsDead))
        {
            if (weak > 0m)
            {
                await PowerCmd.Apply<WeakPower>(
                    choiceContext,
                    target,
                    weak,
                    Owner.Creature,
                    this);
            }

            if (_appliesTwist)
            {
                await PowerCmd.Apply<TwistPower>(
                    choiceContext,
                    target,
                    1m,
                    Owner.Creature,
                    this);
            }

            if (_vulnerableOnPlay + gatherVulnerable > 0)
            {
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext,
                    target,
                    _vulnerableOnPlay + gatherVulnerable,
                    Owner.Creature,
                    this);
            }
        }

        if (_playBlock > 0m)
        {
            await CreatureCmd.GainBlock(
                Owner.Creature,
                _playBlock,
                ValueProp.Move,
                null);
        }
    }

    public void AddPermanentDamageBonus(decimal amount)
    {
        DynamicVars.Damage.BaseValue += Math.Max(0m, amount);
    }

    public void AddPermanentBlockBonus(decimal amount)
    {
        _playBlock += Math.Max(0m, amount);
    }

    public void AddReplayBonus(int amount)
    {
        _replayCount += Math.Max(0, amount);
    }

    public void AddTemporaryDamageBonus(decimal amount)
    {
        decimal increase = Math.Max(0m, amount);
        _temporaryDamageBonus += increase;
        DynamicVars.Damage.BaseValue += increase;
    }

    public void AddDrawOnPlay(int amount)
    {
        _extraDrawOnPlay += Math.Max(0, amount);
    }

    public void AddWeakOnExhaust(int amount = 1)
    {
        _weakOnPlay += Math.Max(0, amount);
    }

    public void AddVulnerableOnPlay(int amount = 1)
    {
        _vulnerableOnPlay += Math.Max(0, amount);
    }

    public void AddTwistOnExhaust()
    {
        _appliesTwist = true;
    }

    public void MakeAoe()
    {
        _isAoe = true;
    }

    public void MakeAoEThisTurn()
    {
        _isAoeThisTurn = true;
    }

    public void Absorb(IEmberCard absorbed)
    {
        if (ReferenceEquals(this, absorbed.Card) || Owner != absorbed.Card.Owner)
        {
            return;
        }

        EmberMechanics.ApplyMaterialBonuses(this, absorbed);

        if (absorbed.FusionDamage > DynamicVars.Damage.BaseValue)
        {
            _replayCount++;
            return;
        }

        DynamicVars.Damage.BaseValue += absorbed.FusionDamage;
        _temporaryDamageBonus += absorbed.TemporaryDamageBonus;
        _playBlock += absorbed.PlayBlock;
        _extraDrawOnPlay += absorbed.ExtraDrawOnPlay;
        _isAoe |= absorbed.IsAoeEffect;
        _isAoeThisTurn |= absorbed.IsAoeThisTurnEffect;
        _weakOnPlay += absorbed.WeakOnPlay;
        _vulnerableOnPlay += absorbed.VulnerableOnPlay;
        _appliesTwist |= absorbed.AppliesTwist;
        _returnNextTurn |= absorbed.ReturnNextTurn;
        _replayCount += absorbed.ReplayCount;
        DynamicVars["Growth"].BaseValue = Math.Max(
            DynamicVars["Growth"].BaseValue,
            absorbed.FusionGrowth);
    }

    public CardModel Card => this;
    public decimal FusionDamage => DynamicVars.Damage.BaseValue;
    public decimal TemporaryDamageBonus => _temporaryDamageBonus;
    public decimal PlayBlock => _playBlock;
    public int ExtraDrawOnPlay => _extraDrawOnPlay;
    public decimal ExhaustBlock => _playBlock;
    public bool IsAoeEffect => _isAoe;
    public bool IsAoeThisTurnEffect => _isAoeThisTurn;
    public int WeakOnPlay => _weakOnPlay;
    public bool AppliesWeak => _weakOnPlay > 0;
    public int VulnerableOnPlay => _vulnerableOnPlay;
    public bool AppliesTwist => _appliesTwist;
    public virtual bool IsAoe => _isAoe || _isAoeThisTurn;
    public bool ReturnNextTurn => _returnNextTurn;
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public int ReplayCount { get => _replayCount; set => _replayCount = value; }
    public decimal FusionGrowth => DynamicVars["Growth"].BaseValue;
    public long MergeOrder => _mergeOrder;
    public bool IsFusionSubject => _isFusionSubject;

    public void SetFusionSubject(bool isFusionSubject)
    {
        _isFusionSubject = isFusionSubject;
    }

    public void AssignMergeOrder(long order)
    {
        _mergeOrder = order;
    }

    public void ClearReturnNextTurn()
    {
        _returnNextTurn = false;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Growth"].UpgradeValueBy(4m);
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Combat.CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side == Owner.Creature.Side)
        {
            if (_temporaryDamageBonus > 0m)
            {
                DynamicVars.Damage.BaseValue -= _temporaryDamageBonus;
                _temporaryDamageBonus = 0m;
            }

            _isAoeThisTurn = false;
        }

        return Task.CompletedTask;
    }
}
