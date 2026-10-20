using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace SnowfallenMod.SnowfallenCode.Cards;

public class WhirlingDance() : SnowfallenCard(
    2,
    CardType.Attack,
    CardRarity.Common,
    TargetType.AllEnemies)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    private decimal _healingThisTurn;
    private int _lastKnownHp;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new ExtraDamageVar(6)
    ];

    public override Task BeforeCombatStart()
    {
        _lastKnownHp = Owner.Creature.CurrentHp;
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature)
        {
            int currentHp = creature.CurrentHp;
            _healingThisTurn += Math.Max(0, currentHp - _lastKnownHp);
            _lastKnownHp = currentHp;
        }

        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (creatures.Contains(Owner.Creature))
        {
            _healingThisTurn = 0m;
        }

        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await using AttackContext attackContext =
            await AttackCommand.CreateContextAsync(
                CombatState,
                choiceContext,
                cardPlay);
        await AddAttackHit(
            attackContext,
            choiceContext,
            DynamicVars.Damage.BaseValue);

        int repeats = Math.Min(2, (int)(_healingThisTurn / 2m));
        for (int i = 0; i < repeats; i++)
        {
            await AddAttackHit(
                attackContext,
                choiceContext,
                DynamicVars.ExtraDamage.BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }

    private async Task AddAttackHit(
        AttackContext attackContext,
        PlayerChoiceContext choiceContext,
        decimal damage)
    {
        await CreatureCmd.TriggerAnim(
            Owner.Creature,
            "Attack",
            Owner.Character.AttackAnimDelay);
        CombatSide targetSide = Owner.Creature.Side == CombatSide.Player
            ? CombatSide.Enemy
            : CombatSide.Player;
        VfxCmd.PlayOnSide(
            targetSide,
            "vfx/vfx_attack_slash",
            CombatState);
        NDebugAudioManager.Instance?.Play("slash_attack.mp3");
        foreach (Creature target in CombatState.HittableEnemies.ToList())
        {
            attackContext.AddHit(await CreatureCmd.Damage(
                choiceContext,
                target,
                damage,
                ValueProp.Move,
                this,
                null));
        }
    }

}

