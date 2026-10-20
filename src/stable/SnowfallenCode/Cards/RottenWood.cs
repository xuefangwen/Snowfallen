using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using SnowfallenMod.SnowfallenCode.Powers;
namespace SnowfallenMod.SnowfallenCode.Cards;

public class RottenWood() : SnowfallenCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    [SavedProperty] public int PendingDecay { get; set; }
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];
    public override bool HasTurnEndInHandEffect => true;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DecayPower>()];
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;
    protected override Task OnTurnEndInHand(PlayerChoiceContext context)
    {
        // This card remains in the combat exhaust pile; its saved pending value
        // is consumed once at its owner's next turn, including after reconnect.
        if (!Owner.Creature.IsDead) PendingDecay++;
        return Task.CompletedTask;
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || PendingDecay <= 0) return;
        int amount = PendingDecay;
        PendingDecay = 0;
        if (Owner.Creature.IsDead) return;
        await PowerCmd.Apply<DecayPower>(context, Owner.Creature, amount, Owner.Creature, this);
    }
}
