using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.SnowfallenCode.Powers;

public abstract class SilentPalacePowerBase : SnowfallenPower
{
    private int _turnsRemaining = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (!creatures.Contains(Owner))
        {
            return;
        }

        Flash();
        await FrostMechanics.Gain(
            choiceContext,
            Owner,
            Amount,
            Owner,
            null);
        _turnsRemaining--;
        if (_turnsRemaining <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}

public class SilentPalacePower : SilentPalacePowerBase;

public class UpgradedSilentPalacePower : SilentPalacePowerBase;
