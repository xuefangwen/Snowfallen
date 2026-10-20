using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.SnowfallenCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class WordsFlying() : SnowfallenCard(
    2,
    CardType.Attack,
    CardRarity.Rare,
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmberTrackerPower>();
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

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new CardsVar(1),
        new RepeatVar(2)
    ];

    public override async Task BeforeCombatStart()
    {
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
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        List<IEmberCard> candidates = PileType.Exhaust.GetPile(Owner).Cards
            .OfType<IEmberCard>()
            .Where(candidate => !ReferenceEquals(candidate.Card, this))
            .ToList();
        int maximum = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        List<IEmberCard> materials = maximum == 0
            ? []
            : (await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    candidates.Select(candidate => candidate.Card).ToList(),
                    Owner,
                    new CardSelectorPrefs(
                        SelectionScreenPrompt,
                        0,
                        maximum)))
                .OfType<IEmberCard>()
                .ToList();

        // Fusion is resolved before the first hit, so every transferred
        // effect participates in this play of Words Flying.
        foreach (IEmberCard material in materials)
        {
            Absorb(material);
            await CardPileCmd.RemoveFromCombat(material.Card, true);
        }

        decimal lakeLightDamage = await ConsumeLakeLightDamage();
        int resolutions = 1 + _replayCount;
        for (int resolution = 0; resolution < resolutions; resolution++)
        {
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

    public override async Task AfterCardGeneratedForCombat(
        CardModel card,
        Player creator)
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
        tracker.RegisterNewActiveEmber(this);

        IReadOnlyList<GlimmerPowerBase> glimmerPowers = Owner.Creature.Powers
            .OfType<GlimmerPowerBase>()
            .ToList();
        AddReplayBonus(glimmerPowers.Sum(power => (int)power.Amount));
        AddPermanentBlockBonus(glimmerPowers.Sum(
            power => power.ExtraBlockPerStack * power.Amount));
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

    private async Task<decimal> ConsumeLakeLightDamage()
    {
        LakeLightEmberDamagePower pending = Owner.Creature.Powers
            .OfType<LakeLightEmberDamagePower>()
            .FirstOrDefault();
        decimal bonus = pending?.Amount ?? 0m;
        if (pending is not null)
        {
            await PowerCmd.Remove(pending);
            return bonus;
        }

        LakeLightMaxHpPower legacy = Owner.Creature.Powers
            .OfType<LakeLightMaxHpPower>()
            .FirstOrDefault(power => !power.IsUsageTracker);
        if (legacy is not null)
        {
            bonus = legacy.Amount;
            await PowerCmd.Remove(legacy);
        }

        return bonus;
    }

    private async Task ResolvePlayEffect(
        PlayerChoiceContext choiceContext,
        Creature selectedTarget,
        decimal oneShotDamageBonus)
    {
        IReadOnlyList<Creature> effectTargets = IsAoe
            ? CombatState.HittableEnemies.ToList()
            : [selectedTarget];
        if (CombatState.HittableEnemies.Count > 0)
        {
            Creature target = !selectedTarget.IsDead &&
                              CombatState.HittableEnemies.Contains(selectedTarget)
                ? selectedTarget
                : null;
            if (IsAoe || target is not null)
            {
                AttackCommand attack = DamageCmd.Attack(
                        DynamicVars.Damage.BaseValue +
                        Math.Max(0m, oneShotDamageBonus))
                    .FromCard(this)
                    .WithHitCount(DynamicVars.Repeat.IntValue)
                    .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3");
                if (IsAoe)
                {
                    attack.TargetingAllOpponents(CombatState);
                }
                else
                {
                    attack.Targeting(target);
                }

                await attack.Execute(choiceContext);
            }
        }

        decimal gatherVulnerable = Owner.Creature.Powers
            .OfType<GatherEmbersPower>()
            .FirstOrDefault()?.Amount ?? 0m;
        decimal weak = _weakOnPlay;
        foreach (Creature affected in effectTargets.Where(enemy => !enemy.IsDead))
        {
            if (weak > 0m)
            {
                await PowerCmd.Apply<WeakPower>(
                    choiceContext,
                    affected,
                    weak,
                    Owner.Creature,
                    this);
            }
            if (_appliesTwist)
            {
                await PowerCmd.Apply<TwistPower>(
                    choiceContext,
                    affected,
                    1m,
                    Owner.Creature,
                    this);
            }
            if (_vulnerableOnPlay + gatherVulnerable > 0)
            {
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext,
                    affected,
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

    public void Absorb(IEmberCard absorbed)
    {
        if (ReferenceEquals(this, absorbed.Card) || Owner != absorbed.Card.Owner)
        {
            return;
        }

        EmberMechanics.ApplyMaterialBonuses(this, absorbed);

        if (absorbed.FusionDamage > DynamicVars.Damage.BaseValue)
        {
            // 言纷飞作为主体时，素材伤害更高也只是把伤害折算成重放1，
            // 素材携带的附加效果（易伤/虚弱/扭转/格挡/抽牌/对所有敌人/回手/重放）
            // 仍然照常继承——否则弃牌堆融合里素材几乎总是更高伤害
            // （余烬每次进入弃牌堆都有成长加成），言纷飞就永远吃不到附件。
            _replayCount++;
            TransferEffects(absorbed);
            return;
        }

        DynamicVars.Damage.BaseValue += absorbed.FusionDamage;
        _temporaryDamageBonus += absorbed.TemporaryDamageBonus;
        TransferEffects(absorbed);
    }

    private void TransferEffects(IEmberCard absorbed)
    {
        _playBlock += absorbed.PlayBlock;
        _extraDrawOnPlay += absorbed.ExtraDrawOnPlay;
        _isAoe |= absorbed.IsAoeEffect;
        _isAoeThisTurn |= absorbed.IsAoeThisTurnEffect;
        _weakOnPlay += absorbed.WeakOnPlay;
        _vulnerableOnPlay += absorbed.VulnerableOnPlay;
        _appliesTwist |= absorbed.AppliesTwist;
        _returnNextTurn |= absorbed.ReturnNextTurn;
        _replayCount += absorbed.ReplayCount;
    }

    public CardModel Card => this;
    public decimal FusionDamage => DynamicVars.Damage.BaseValue;
    public decimal TemporaryDamageBonus => _temporaryDamageBonus;
    public decimal PlayBlock => _playBlock;
    public int ExtraDrawOnPlay => _extraDrawOnPlay;
    public bool IsAoeEffect => _isAoe;
    public bool IsAoeThisTurnEffect => _isAoeThisTurn;
    public int WeakOnPlay => _weakOnPlay;
    public int VulnerableOnPlay => _vulnerableOnPlay;
    public bool AppliesTwist => _appliesTwist;
    public bool ReturnNextTurn => _returnNextTurn;
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public int ReplayCount { get => _replayCount; set => _replayCount = value; }
    public decimal FusionGrowth => 0m;
    public long MergeOrder => _mergeOrder;
    public bool IsFusionSubject => _isFusionSubject;
    private bool IsAoe => _isAoe || _isAoeThisTurn;

    public void AddPermanentDamageBonus(decimal amount) =>
        DynamicVars.Damage.BaseValue += Math.Max(0m, amount);
    public void AddPermanentBlockBonus(decimal amount) =>
        _playBlock += Math.Max(0m, amount);
    public void AddReplayBonus(int amount) =>
        _replayCount += Math.Max(0, amount);
    public void AddTemporaryDamageBonus(decimal amount)
    {
        decimal increase = Math.Max(0m, amount);
        _temporaryDamageBonus += increase;
        DynamicVars.Damage.BaseValue += increase;
    }
    public void AddDrawOnPlay(int amount) =>
        _extraDrawOnPlay += Math.Max(0, amount);
    public void AddWeakOnExhaust(int amount = 1) =>
        _weakOnPlay += Math.Max(0, amount);
    public void AddVulnerableOnPlay(int amount = 1) =>
        _vulnerableOnPlay += Math.Max(0, amount);
    public void AddTwistOnExhaust() => _appliesTwist = true;
    public void MakeAoe() => _isAoe = true;
    public void MakeAoEThisTurn() => _isAoeThisTurn = true;
    public void SetFusionSubject(bool isFusionSubject) =>
        _isFusionSubject = isFusionSubject;
    public void AssignMergeOrder(long order) => _mergeOrder = order;
    public void ClearReturnNextTurn() => _returnNextTurn = false;

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Cards.UpgradeValueBy(1m);
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
