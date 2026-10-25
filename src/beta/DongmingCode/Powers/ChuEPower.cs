using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace SnowfallenMod.DongmingCode.Powers;

public class ChuEPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

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

        DoomPower doom = Owner.Powers.OfType<DoomPower>().FirstOrDefault();
        if (doom is null)
        {
            return;
        }

        Flash();
        await PowerCmd.ModifyAmount(
            choiceContext,
            doom,
            -Math.Min(doom.Amount, Amount),
            Owner,
            null);
    }
}

