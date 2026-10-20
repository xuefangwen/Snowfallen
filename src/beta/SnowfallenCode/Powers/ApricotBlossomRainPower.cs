using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Mechanics;
namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class ApricotBlossomRainPowerBase : SnowfallenPower
{
    protected abstract decimal Healing { get; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || Owner.IsDead || Amount <= 0) return;
        // Use the synchronized, saved combat turn number, never a local counter.
        int turn = player.PlayerCombatState.TurnNumber;
        if (turn <= 0) return;
        Flash();
        await ApricotBlossomRainEffect.Resolve(context, player, turn, Healing, Amount);
    }
}
public class ApricotBlossomRainPower : ApricotBlossomRainPowerBase
{
    protected override decimal Healing => 8m;
}
public class UpgradedApricotBlossomRainPower : ApricotBlossomRainPowerBase
{
    protected override decimal Healing => 12m;
}
