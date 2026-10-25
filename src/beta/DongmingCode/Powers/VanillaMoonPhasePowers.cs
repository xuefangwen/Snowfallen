using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.SnowfallenCode.Extensions;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class DazedBrightStrengthDownPower :
    TemporaryStrengthPower,
    ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Dazed>();
    protected override bool IsPositive => false;

    // TemporaryStrengthPower supplies the standard strength text; this only
    // supplies the BaseLib fallback art for the temporary status instance.
    public string CustomPackedIconPath => "dazed_bright_strength_down_power.png".PowerImagePath();
    public string CustomBigIconPath => "dazed_bright_strength_down_power.png".BigPowerImagePath();
}

public sealed class DongmingSoarDurationPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
        {
            return;
        }

        SoarPower soar = Owner.Powers.OfType<SoarPower>().FirstOrDefault();
        if (soar is null)
        {
            await PowerCmd.Remove(this);
        }
        else if (soar.Amount <= 1m)
        {
            await PowerCmd.Remove(soar);
            await PowerCmd.Remove(this);
        }
        else
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                soar,
                -1m,
                Owner,
                null);
        }
    }
}

public sealed class TianZiPower : DongmingPower
{
    public override string CustomPackedIconPath => "tian_zi_power.png".PowerImagePath();
    public override string CustomBigIconPath => "tian_zi_power.png".BigPowerImagePath();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldPlayVfx => false;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }

        int rewards = 0;
        foreach (PurifiedNormality normality in PileType.Hand.GetPile(Owner.Player).Cards
                     .OfType<PurifiedNormality>())
        {
            if (PurifiedVanillaEffects.RecordNormalityCardPlayed(normality) % 3 == 0)
            {
                rewards += 1 + YuanShaMechanics.GetExtraTriggerCount(normality);
            }
        }

        if (rewards > 0)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, rewards, Owner.Player, false);
            await MoonlightMechanics.Gain(Owner.Player, rewards);
        }
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            foreach (PurifiedNormality normality in Owner.Player.PlayerCombatState.AllCards
                         .OfType<PurifiedNormality>())
            {
                PurifiedVanillaEffects.ResetNormalityCounter(normality);
            }
        }
        return Task.CompletedTask;
    }
}

public sealed class LiXiPower : DongmingPower
{
    public override string CustomPackedIconPath => "li_xi_power.png".PowerImagePath();
    public override string CustomBigIconPath => "li_xi_power.png".BigPowerImagePath();
    public override PowerType Type => PowerType.Buff;
    // Counter：buff 上会显示数字；IsInstanced：每次施加都新建一条独立实例，
    // 于是每张"利息"牌各自占一个 buff 栏位、独立计数（与原版"神气致胜"同类做法）。
    public override PowerStackType StackType => PowerStackType.Counter;
    // TODO: IsInstanced 需要挂到 BaseLib 的 Power 基类层（当前层级无法 override，见下）
    public override bool ShouldPlayVfx => false;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Amount > 0m)
        {
            room.AddExtraReward(
                Owner.Player,
                new GoldReward((int)Amount, Owner.Player,
                    wasGoldStolenBack: false));
        }
        return Task.CompletedTask;
    }
}

public sealed class AnMianPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldPlayVfx => false;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner))
        {
            return;
        }

        foreach (PurifiedPoorSleep poorSleep in Owner.Player.PlayerCombatState.AllCards
                     .OfType<PurifiedPoorSleep>())
        {
            int due = PurifiedVanillaEffects.AdvancePoorSleep(poorSleep);
            for (int i = 0; i < due; i++)
            {
                decimal amount = Math.Floor(Owner.MaxHp * 0.10m);
                await CreatureCmd.Heal(Owner, amount);
                await MoonlightMechanics.Gain(Owner.Player, 4m);
                DoomPower doom = Owner.Powers.OfType<DoomPower>().FirstOrDefault();
                if (doom is null)
                {
                    continue;
                }

                await PowerCmd.ModifyAmount(
                    choiceContext,
                    doom,
                    -Math.Min(doom.Amount, amount),
                    Owner,
                    poorSleep);
            }
        }

        await PurifiedVanillaEffects.SyncAnMianPower(choiceContext, Owner.Player);
    }
}

public sealed class WritheNextTurnMoonlightPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> creatures,
        ICombatState combatState)
    {
        if (side != Owner.Side)
        {
            return;
        }

        await MoonlightMechanics.Gain(Owner.Player, Amount * 2m);
        foreach (PurifiedWrithe writhe in Owner.Player.PlayerCombatState.AllCards
                     .OfType<PurifiedWrithe>())
        {
            writhe.PendingNextTurnTriggers = 0;
        }
        await PowerCmd.Remove(this);
    }
}


public sealed class EnthralledNextTurnMoonlightPower : DongmingPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext,
        CombatSide side, IReadOnlyList<Creature> creatures, ICombatState combatState)
    {
        if (side != Owner.Side || !creatures.Contains(Owner)) return;
        await MoonlightMechanics.Gain(Owner.Player, Amount);
        await PowerCmd.Remove(this);
    }
}
