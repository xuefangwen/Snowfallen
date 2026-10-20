using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using SnowfallenMod.SnowfallenCode.Extensions;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenMod.SnowfallenCode.Relics;
using SnowfallenMod.SnowfallenCode.RelicPools;
using SnowfallenMod.SnowfallenCode.PotionPools;

namespace SnowfallenMod.SnowfallenCode.Character;

public class Snowfallen : PlaceholderCharacterModel
{
    public const string CharacterId = "Snowfallen";

    public override string CustomCharacterSelectBg => "res://Snowfallen/Scenes/CharacterSelect/snowfallen_character_select_bg.tscn";

    public override string PlaceholderID => "necrobinder";

    public static readonly Color Color = new Color("75d9ff");

    public override CustomEnergyCounter? CustomEnergyCounter => new CustomEnergyCounter(
        layer => $"res://Snowfallen/Images/Charui/SnowfallenEnergy/combat_orb_layers/snowfallen_orb_layer_{layer}.png",
        new Color("135f91"),
        Color);

    public override RelicIconData CustomYummyCookie => new(
        "yummy_cookie_snowfallen.png".BigRelicImagePath(),
        "yummy_cookie_snowfallen.png".RelicImagePath(),
        "yummy_cookie_snowfallen_outline.png".RelicImagePath());

    public override Color NameColor => Color;
    public override Color MapDrawingColor => new("ff3030");
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 46;
    public override float AttackAnimDelay => 0.23f;
    public override float CastAnimDelay => 0.23f;

    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<SnowfallenAttack>(),
        ModelDb.Card<SnowfallenAttack>(),
        ModelDb.Card<SnowfallenAttack>(),
        ModelDb.Card<SnowfallenAttack>(),
        ModelDb.Card<SnowfallenBlock>(),
        ModelDb.Card<SnowfallenBlock>(),
        ModelDb.Card<SnowfallenBlock>(),
        ModelDb.Card<SnowfallenBlock>(),
        ModelDb.Card<SnowCover>(),
        ModelDb.Card<SongOfFloatingClouds>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<WaterBottlePendant>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<SnowfallenCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<SnowfallenRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<SnowfallenPotionPool>();

    /*  PlaceholderCharacterModel will utilize placeholder basegame assets for most of your character assets until you
		override all the other methods that define those assets.
		These are just some of the simplest assets, given some placeholders to differentiate your character with.
		You don't have to, but you're suggested to rename these images. */
    public override string CustomVisualPath => "res://Snowfallen/Scenes/snowfallen_visual.tscn";
    public override string CustomIconPath => "res://Snowfallen/Scenes/UI/snowfallen_icon.tscn";
    public override string CustomIconTexturePath => "character_icon_snowfallen.png".CharacterUiPath();
    public override string CustomIconOutlineTexturePath => "character_icon_snowfallen_outline.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_snowfallen.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_snowfallen_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "character_icon_snowfallen.png".CharacterUiPath();
    public override string CustomArmPointingTexturePath =>
        "res://images/ui/hands/multiplayer_hand_snowfallen_point.png";
    public override string CustomArmRockTexturePath =>
        "res://images/ui/hands/multiplayer_hand_snowfallen_rock.png";
    public override string CustomArmPaperTexturePath =>
        "res://images/ui/hands/multiplayer_hand_snowfallen_paper.png";
    public override string CustomArmScissorsTexturePath =>
        "res://images/ui/hands/multiplayer_hand_snowfallen_scissors.png";

    protected override IEnumerable<string> ExtraAssetPaths =>
        base.ExtraAssetPaths.Concat(
        [
            "res://images/ui/hands/multiplayer_hand_snowfallen_point.png",
            "res://images/ui/hands/multiplayer_hand_snowfallen_rock.png",
            "res://images/ui/hands/multiplayer_hand_snowfallen_paper.png",
            "res://images/ui/hands/multiplayer_hand_snowfallen_scissors.png"
        ]);
}
