using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using SnowfallenMod.DongmingCode.Cards;
using SnowfallenMod.DongmingCode.Mechanics;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.SnowfallenCode.Mechanics;

namespace SnowfallenMod.DongmingCode.Powers;

public sealed class ShiEStrengthDownPower :
    TemporaryStrengthPower,
    ICustomPower
{
    public override AbstractModel OriginModel => ModelDb.Card<ShiE>();
    protected override bool IsPositive => false;
    public string CustomPackedIconPath => "shi_e_strength_down_power.png".PowerImagePath();
    public string CustomBigIconPath => "shi_e_strength_down_power.png".BigPowerImagePath();
}

public sealed class NingBaiPower : DongmingPower
{
    public override string CustomPackedIconPath => "ning_bai_power.png".PowerImagePath();
    public override string CustomBigIconPath => "ning_bai_power.png".BigPowerImagePath();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> creatures)
    {
        if (side != Owner.Side)
        {
            return;
        }

        decimal frost = Math.Floor(Owner.Block * 0.5m) * Amount;
        if (frost > 0m)
        {
            await FrostMechanics.Gain(
                choiceContext,
                Owner,
                frost,
                Owner,
                null);
        }
        await PowerCmd.Remove(this);
    }
}

public sealed class NingHuaPower : DongmingPower
{
    private CardModel _qualifyingCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel cardSource,
        CardPlay cardPlay)
    {
        if (dealer != Owner ||
            !props.HasFlag(ValueProp.Move) ||
            cardSource?.Type != CardType.Attack ||
            !ConsumesResource(cardSource))
        {
            return 1m;
        }

        _qualifyingCard ??= cardSource;
        return ReferenceEquals(_qualifyingCard, cardSource)
            ? 1m + Amount * 0.1m
            : 1m;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (ReferenceEquals(_qualifyingCard, cardPlay.Card))
        {
            _qualifyingCard = null;
            await PowerCmd.Remove(this);
        }
    }

    private bool ConsumesResource(CardModel card)
    {
        if (card.LastStarsSpent > 0)
        {
            return true;
        }
        if (card is HanChe)
        {
            return DongmingAttackMechanics.FrostAmount(card.Owner) > 0;
        }
        if (card is SuiShuang)
        {
            return DongmingAttackMechanics.FrostAmount(card.Owner) >= 5;
        }
        return false;
    }
}

