using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using SnowfallenMod.SnowfallenCode.Animation;
using SnowfallenMod.SnowfallenCode.Cards;
using SnowfallenCharacter = SnowfallenMod.SnowfallenCode.Character.Snowfallen;

namespace SnowfallenMod.SnowfallenCode.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
public static class SnowfallenAnimationPatch
{
    private const string CombatVisualScenePath =
        "res://Snowfallen/Scenes/snowfallen_visual.tscn";
    private static readonly StringName DeadAnimation = "Dead";
    private static SpriteFrames _cachedCombatFrames;

    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, string trigger)
    {
        SnowfallenAnimationDriver driver = TryInstallDriver(__instance);
        driver?.SetTrigger(trigger);
    }

    internal static SnowfallenAnimationDriver TryInstallDriver(NCreature creature)
    {
        Node visualRoot = creature.FindChild(
            "SnowfallenVisual",
            recursive: true,
            owned: false);
        if (visualRoot is null)
        {
            return null;
        }

        SnowfallenAnimationDriver existing = visualRoot.GetNodeOrNull<SnowfallenAnimationDriver>(
            "SnowfallenAnimationDriver");
        if (existing is not null)
        {
            return existing;
        }

        AnimatedSprite2D sprite = visualRoot.GetNodeOrNull<AnimatedSprite2D>("Visuals");
        if (sprite is null)
        {
            return null;
        }

        SnowfallenAnimationDriver driver = new()
        {
            Name = "SnowfallenAnimationDriver"
        };
        visualRoot.AddChild(driver);
        driver.Initialize(sprite);
        return driver;
    }

    internal static void PlayVisibleSnowfallenDeathAnimations()
    {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        Node root = tree?.Root;
        if (root is null)
        {
            return;
        }

        int creatureCount = 0;
        int spriteSwapCount = 0;
        foreach (Node node in EnumerateTree(root))
        {
            if (node is NCreature creature)
            {
                creatureCount++;
                SnowfallenAnimationDriver driver = TryInstallDriver(creature);
                if (driver is not null)
                {
                    driver.ProcessMode = Node.ProcessModeEnum.Always;
                    if (creature.FindChild(
                            "Visuals",
                            recursive: true,
                            owned: false) is AnimatedSprite2D combatSprite)
                    {
                        combatSprite.ProcessMode = Node.ProcessModeEnum.Always;
                    }

                    driver.SetTrigger("Dead");
                }

                // SnowfallenVisual 驱动；结束画面这里要显式触发它的死亡序列，
                // 否则"胜利通关（角色并没有真的死）"这类情况下没人触发它。


                continue;
            }

            if (node is not AnimatedSprite2D sprite ||
                (sprite.Name != "SnowfallenShopVisual" &&
                 sprite.Name != "SnowfallenCampfireVisual" &&
                 sprite.Name != "SnowfallenEventVisual"))
            {
                continue;
            }
            SpriteFrames frames = GetCombatFrames();
            if (frames is null || !frames.HasAnimation(DeadAnimation))
            {
                continue;
            }

            if (sprite.SpriteFrames == frames &&
                sprite.Animation == DeadAnimation &&
                sprite.IsPlaying())
            {
                continue;
            }

            sprite.ProcessMode = Node.ProcessModeEnum.Always;
            sprite.SpriteFrames = frames;
            sprite.Stop();
            sprite.Animation = DeadAnimation;
            sprite.SetFrameAndProgress(0, 0f);
            sprite.Play(DeadAnimation);
            spriteSwapCount++;
        }

        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[Snowfallen.DeathDiag] PlayVisibleDeath: creatures={creatureCount} " +
            $"spriteSwaps={spriteSwapCount}");
    }

    private static IEnumerable<Node> EnumerateTree(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren())
        {
            foreach (Node descendant in EnumerateTree(child))
            {
                yield return descendant;
            }
        }
    }

    private static SpriteFrames GetCombatFrames()
    {
        if (_cachedCombatFrames is not null)
        {
            return _cachedCombatFrames;
        }

        PackedScene scene = ResourceLoader.Load<PackedScene>(CombatVisualScenePath);
        Node instance = scene?.Instantiate();
        try
        {
            AnimatedSprite2D source = instance?.FindChild(
                "Visuals",
                recursive: true,
                owned: false) as AnimatedSprite2D;
            _cachedCombatFrames = source?.SpriteFrames;
        }
        finally
        {
            instance?.Free();
        }

        return _cachedCombatFrames;
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class SnowfallenAnimationReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        SnowfallenAnimationPatch.TryInstallDriver(__instance);
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class SnowfallenDeathAnimationPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {


        SnowfallenAnimationPatch.TryInstallDriver(__instance)?.SetTrigger("Dead");
    }
}

[HarmonyPatch(typeof(NCreature), "AnimDie")]
public static class SnowfallenDirectDeathAnimationPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        SnowfallenAnimationDriver driver =
            SnowfallenAnimationPatch.TryInstallDriver(__instance);
        if (driver is null)
        {
            return;
        }

        driver.ProcessMode = Node.ProcessModeEnum.Always;
        driver.SetTrigger("Dead");
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.Abandon))]
public static class SnowfallenAbandonAnimationPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        SnowfallenAnimationPatch.PlayVisibleSnowfallenDeathAnimations();
    }
}

[HarmonyPatch(typeof(NRun), nameof(NRun.ShowGameOverScreen))]
public static class SnowfallenGameOverAnimationPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        SnowfallenAnimationPatch.PlayVisibleSnowfallenDeathAnimations();
    }
}

// The vanilla game-over screen rebuilds the character visuals for non-combat
// rooms (rest site, event, etc.) and immediately plays the placeholder
// Necrobinder Spine "die" animation.  Those freshly-created visuals do not
// finished instead.
[HarmonyPatch(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi")]
public static class SnowfallenNonCombatGameOverAnimationPatch
{
    private const string SnowfallenDeathNodeName = "SnowfallenGameOverDeathVisual";
    private static readonly System.Reflection.FieldInfo RunStateField =
        AccessTools.Field(typeof(NGameOverScreen), "_runState");
    private static readonly System.Reflection.FieldInfo CreatureContainerField =
        AccessTools.Field(typeof(NGameOverScreen), "_creatureContainer");

    [HarmonyPostfix]
    public static void Postfix(NGameOverScreen __instance)
    {
        SpriteFrames snowfallenDeathFrames = GetCombatFrames();

        // Merchant mode reparents the existing merchant-character nodes.  The
        // custom looping sprite is retained, but must be switched to death.
        int spriteSwaps = 0;
        foreach (Node node in EnumerateTree(__instance))
        {
            if (node is not AnimatedSprite2D sprite ||
                (                 sprite.Name != "SnowfallenShopVisual" &&
                 sprite.Name != "SnowfallenCampfireVisual" &&
                 sprite.Name != "SnowfallenEventVisual"))
            {
                continue;
            }
            AnimateDeath(sprite, snowfallenDeathFrames);
            spriteSwaps++;
        }

        if (RunStateField?.GetValue(__instance) is not MegaCrit.Sts2.Core.Runs.RunState runState ||
            CreatureContainerField?.GetValue(__instance) is not Control container)
        {
            MegaCrit.Sts2.Core.Logging.Log.Info(
                $"[Snowfallen.DeathDiag] GameOverScreen: reflection failed " +
                $"(spritesSwapped={spriteSwaps})");
            return;
        }

        if (NMerchantRoom.Instance is not null)
        {
            MegaCrit.Sts2.Core.Logging.Log.Info(
                $"[Snowfallen.DeathDiag] GameOverScreen branch=merchant " +
                $"spritesSwapped={spriteSwaps}");
            return;
        }

        // Rest-site branches keep the custom sprite under NRestSiteCharacter,
        // while the vanilla game-over method creates a replacement visual.
        // Reparent the mod-owned sprite into the game-over container and hide
        // the replacement body before starting the correct death animation.
        if (NRestSiteRoom.Instance is not null)
        {
            List<NCreatureVisuals> restVisuals = container.GetChildren()
                .OfType<NCreatureVisuals>()
                .ToList();
            IReadOnlyList<NRestSiteCharacter> characters = NRestSiteRoom.Instance.Characters;
            int restCount = Math.Min(runState.Players.Count, restVisuals.Count);
            for (int index = 0; index < restCount; index++)
            {
                Player player = runState.Players[index];
                if (player.Character is not SnowfallenCharacter ||
                    index >= characters.Count)
                {
                    continue;
                }

                NCreatureVisuals vanilla = FindVisualForCharacter(
                    restVisuals,
                    index) ?? restVisuals[index];
                vanilla.GetCurrentBody().Visible = false;
                string nodeName = "SnowfallenCampfireVisual";
                if (characters[index].FindChild(nodeName, recursive: true, owned: false)
                    is not AnimatedSprite2D custom)
                {
                    continue;
                }

                custom.Reparent(container, keepGlobalTransform: true);
                custom.GlobalPosition = vanilla.GlobalPosition;
                AnimateDeath(custom, snowfallenDeathFrames);
            }
            MegaCrit.Sts2.Core.Logging.Log.Info(
                $"[Snowfallen.DeathDiag] GameOverScreen branch=restSite " +
                $"players={runState.Players.Count} visuals={restVisuals.Count} " +
                $"tags=[{string.Join(",", restVisuals.Select(DescribeVisualTag))}] " +
                $"spritesSwapped={spriteSwaps}");
            return;
        }

        // Event/other non-combat branches create one NCreatureVisuals per player in
        // run-state order.  The container can also hold event NPC visuals (the
        // Architect on a victory, for example), so pick by the character tag set in
        // SnowfallenCharacterVisualsTagPatch instead of trusting the child index.
        List<NCreatureVisuals> visuals = container.GetChildren()
            .OfType<NCreatureVisuals>()
            .ToList();
        int count = Math.Min(runState.Players.Count, visuals.Count);
        int installed = 0;
        for (int index = 0; index < count; index++)
        {
            Player player = runState.Players[index];
            if (player.Character is not SnowfallenCharacter)
            {
                continue;
            }

            NCreatureVisuals visual = FindVisualForCharacter(visuals, index);
            if (visual is null)
            {
                continue;
            }

            // 通关时原版走的是战斗分支：它把战斗里的视觉节点搬到结算容器（遮罩之上），
            // 设成了 Visible=false，于是"可见的那份是空的、有动画的那份看不见"。
            // 这里把视觉本体重新显示，并只隐藏原版身体，让挂在它下面的死亡精灵可见。
            visual.Visible = true;
            visual.GetCurrentBody().Visible = false;
            string deathNodeName = SnowfallenDeathNodeName;
            AnimatedSprite2D death = visual.GetNodeOrNull<AnimatedSprite2D>(
                deathNodeName);
            if (death is null)
            {
                death = new AnimatedSprite2D
                {
                    Name = deathNodeName,
                    Position = new Vector2(0f, -125f),
                    Centered = true,
                    ZIndex = 0,
                    ProcessMode = Node.ProcessModeEnum.Always
                };
                visual.AddChild(death);
            }

            float scaleY = Mathf.Abs(visual.Scale.Y) > 0.001f
                ? Mathf.Abs(visual.Scale.Y)
                : 1f;
            death.Scale = new Vector2(0.32f * scaleY, 0.32f * scaleY);
            death.FlipH = visual.Scale.X < 0f;
            AnimateDeath(death, snowfallenDeathFrames);
            installed++;
            MegaCrit.Sts2.Core.Logging.Log.Info(
                $"[Snowfallen.DeathDiag] installed on visual={DescribeVisualTag(visual)} " +
                $"visualPos={visual.Position} visualGlobalPos={visual.GlobalPosition} " +
                $"visualScale={visual.Scale} visualVisible={visual.Visible} " +
                $"bodyVisible={visual.GetCurrentBody().Visible} " +
                $"deathNode={death.Name} deathPos={death.Position} " +
                $"deathGlobalPos={death.GlobalPosition} deathScale={death.Scale} " +
                $"deathVisible={death.Visible} deathAnim={death.Animation} " +
                $"playing={death.IsPlaying()} z={death.ZIndex} modal={death.Modulate}");
        }

        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[Snowfallen.DeathDiag] GameOverScreen branch=nonMerchantNonRest " +
            $"(roomCombat={NCombatRoom.Instance is not null} " +
            $"roomMerchant={NMerchantRoom.Instance is not null} " +
            $"roomRest={NRestSiteRoom.Instance is not null}) " +
            $"players={runState.Players.Count} visuals={visuals.Count} " +
            $"tags=[{string.Join(",", visuals.Select(DescribeVisualTag))}] " +
            $"installed={installed} spritesSwapped={spriteSwaps}");
    }

    private static string DescribeVisualTag(NCreatureVisuals visual)
    {
        if (!visual.HasMeta(SnowfallenVisualTag.Key))
        {
            return $"{visual.Name}:<none>";
        }

        return $"{visual.Name}:{visual.GetMeta(SnowfallenVisualTag.Key).AsString()}";
    }

    private static NCreatureVisuals FindVisualForCharacter(
        List<NCreatureVisuals> visuals,
        int index)
    {
        string wanted = SnowfallenVisualTag.Snowfallen;
        NCreatureVisuals tagged = visuals.FirstOrDefault(visual =>
            visual.HasMeta(SnowfallenVisualTag.Key) &&
            visual.GetMeta(SnowfallenVisualTag.Key).AsString() == wanted);
        if (tagged is not null)
        {
            return tagged;
        }

        return index < visuals.Count ? visuals[index] : null;
    }

    private static void AnimateDeath(AnimatedSprite2D sprite, SpriteFrames frames)
    {
        if (sprite is null)
        {
            MegaCrit.Sts2.Core.Logging.Log.Info(
                "[Snowfallen.DeathDiag] AnimateDeath skipped: sprite is null");
            return;
        }

        if (frames is null)
        {
            MegaCrit.Sts2.Core.Logging.Log.Info(
                $"[Snowfallen.DeathDiag] AnimateDeath({sprite.Name}) skipped: frames is null");
            return;
        }

        if (!frames.HasAnimation("Dead"))
        {
            MegaCrit.Sts2.Core.Logging.Log.Info(
                $"[Snowfallen.DeathDiag] AnimateDeath({sprite.Name}) skipped: no 'Dead' " +
                $"animation, anims=[{string.Join(",", frames.GetAnimationNames())}]");
            return;
        }

        sprite.ProcessMode = Node.ProcessModeEnum.Always;
        sprite.SpriteFrames = frames;
        sprite.Visible = true;
        sprite.Stop();
        sprite.Animation = "Dead";
        sprite.SetFrameAndProgress(0, 0f);
        sprite.Play("Dead");
    }

    private static SpriteFrames GetCombatFrames()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(
            "res://Snowfallen/Scenes/snowfallen_visual.tscn");
        Node instance = scene?.Instantiate();
        try
        {
            return instance?.FindChild("Visuals", recursive: true, owned: false)
                is AnimatedSprite2D source ? source.SpriteFrames : null;
        }
        finally
        {
            instance?.Free();
        }
    }

    private static IEnumerable<Node> EnumerateTree(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren())
        {
            foreach (Node descendant in EnumerateTree(child))
            {
                yield return descendant;
            }
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
public static class SnowfallenSkillAnimationPatch
{
    [HarmonyPrefix]
    public static void Prefix(CardPlay cardPlay)
    {
        if (cardPlay.Card is not SnowfallenCard ||
            cardPlay.Card.Type is not (CardType.Skill or CardType.Power) ||
            !cardPlay.IsFirstInSeries)
        {
            return;
        }

        NCombatRoom combatRoom = NCombatRoom.Instance;
        NCreature creatureNode = combatRoom?.GetCreatureNode(cardPlay.Card.Owner?.Creature);
        creatureNode?.SetAnimationTrigger("Cast");
    }
}

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
public static class SnowfallenRestSiteAnimationPatch
{
    private const string CampfireFramesPath =
        "res://Snowfallen/Animations/snowfallen_campfire_frames.tres";
    private static readonly StringName CampfireAnimation = "Campfire";
    private static readonly System.Reflection.MethodInfo GetChildSpineNodesMethod =
        AccessTools.Method(typeof(NRestSiteCharacter), "GetChildSpineNodes");

    [HarmonyPostfix]
    public static void Postfix(NRestSiteCharacter __instance)
    {
        bool snowfallen = __instance.Player?.Character is SnowfallenCharacter;
        if (!snowfallen ||
            __instance.FindChild(
                "SnowfallenCampfireVisual",
                recursive: true,
                owned: false) is not null)
        {
            return;
        }

        Node2D[] originalSpineNodes = GetOriginalSpineNodes(__instance);
        Node2D anchor = originalSpineNodes.FirstOrDefault();
        Node parent = anchor?.GetParent() ?? __instance;
        Vector2 anchorPosition = anchor?.Position ?? Vector2.Zero;
        int anchorZIndex = anchor?.ZIndex ?? 0;

        string framesPath = CampfireFramesPath;
        SpriteFrames frames = ResourceLoader.Load<SpriteFrames>(framesPath);
        if (frames is null || !frames.HasAnimation(CampfireAnimation))
        {
            MegaCrit.Sts2.Core.Logging.Log.Error(
                $"[Snowfallen.Animation] Missing campfire SpriteFrames at {framesPath}");
            return;
        }

        foreach (Node2D original in originalSpineNodes)
        {
            original.Visible = false;
        }

        // Necrobinder is Snowfallen's placeholder rest-site scene. Hide its
        // character-specific pet and glow layers while retaining the original
        // hitbox, thought-bubble anchors, multiplayer flip logic, and the room's
        // own campfire/background.
        HideOriginalVisual(__instance, "Osty");
        HideOriginalVisual(__instance, "NecroFire");
        HideOriginalVisual(__instance, "OstyFire");

        AnimatedSprite2D sprite = new()
        {
            Name = "SnowfallenCampfireVisual",
            SpriteFrames = frames,
            Animation = CampfireAnimation,
            Position = anchorPosition + new Vector2(0f, -125f),
            Scale = new Vector2(0.32f, 0.32f),
            ZIndex = anchorZIndex,
            Centered = true
        };
        parent.AddChild(sprite);
        sprite.Play(CampfireAnimation);
        MegaCrit.Sts2.Core.Logging.Log.Info(
            "[Snowfallen.Animation] Installed looping Campfire animation in rest-site scene.");
    }

    private static void HideOriginalVisual(Node root, string nodeName)
    {
        if (root.FindChild(nodeName, recursive: true, owned: false) is CanvasItem item)
        {
            item.Visible = false;
        }
    }

    private static Node2D[] GetOriginalSpineNodes(NRestSiteCharacter instance)
    {
        return GetChildSpineNodesMethod?.Invoke(instance, null) is IEnumerable<Node2D> nodes
            ? nodes.ToArray()
            : [];
    }
}

[HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
public static class SnowfallenMerchantAnimationPatch
{
    private const string ShopFramesPath =
        "res://Snowfallen/Animations/snowfallen_shop_frames.tres";
    private static readonly StringName ShopAnimation = "Shop";
    private static readonly System.Reflection.FieldInfo PlayersField =
        AccessTools.Field(typeof(NMerchantRoom), "_players");

    [HarmonyPostfix]
    public static void Postfix(NMerchantRoom __instance)
    {
        if (PlayersField?.GetValue(__instance) is not IReadOnlyList<Player> players)
        {
            return;
        }

        IReadOnlyList<NMerchantCharacter> visuals = __instance.PlayerVisuals;
        int count = Math.Min(players.Count, visuals.Count);
        for (int index = 0; index < count; index++)
        {
            if (players[index].Character is SnowfallenCharacter)
            {
                InstallShopVisual(visuals[index]);
            }
            
        }
    }

    private static void InstallShopVisual(NMerchantCharacter character)
    {
        string visualName = "SnowfallenShopVisual";
        string framesPath = ShopFramesPath;
        if (character.GetNodeOrNull<AnimatedSprite2D>(visualName) is not null)
        {
            return;
        }

        SpriteFrames frames = ResourceLoader.Load<SpriteFrames>(framesPath);
        if (frames is null || !frames.HasAnimation(ShopAnimation))
        {
            MegaCrit.Sts2.Core.Logging.Log.Error(
                $"[Snowfallen.Animation] Missing shop SpriteFrames at {framesPath}");
            return;
        }

        foreach (Node child in character.GetChildren())
        {
            HideCanvasBranch(child);
        }

        AnimatedSprite2D sprite = new()
        {
            Name = visualName,
            SpriteFrames = frames,
            Animation = ShopAnimation,
            // Keep the source frame's foot anchor on the merchant-room floor
            // while scaling the character up to the room's full-height staging.
            Position = new Vector2(0f, -242f),
            Scale = new Vector2(0.62f, 0.62f),
            Centered = true
        };
        character.AddChild(sprite);
        sprite.Play(ShopAnimation);
        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[Snowfallen.Animation] Installed looping Snowfallen Shop animation in merchant scene.");
    }

    private static void HideCanvasBranch(Node node)
    {
        if (node is CanvasItem item)
        {
            item.Visible = false;
            return;
        }

        foreach (Node child in node.GetChildren())
        {
            HideCanvasBranch(child);
        }
    }
}

/// <summary>
/// 模组角色视觉标记。假商人事件（NFakeMerchant）用 Character.CreateVisuals()
/// 直接实例化 NCreatureVisuals，既不经过 NCreature（战斗补丁），也不经过
/// NMerchantRoom（商店补丁），玩家的自定义形象在那里会退回占位 necrobinder。
/// 这里给"由模组角色创建出来的视觉"打上标记，供事件补丁识别归属（不依赖子节点顺序）。
/// </summary>
internal static class SnowfallenVisualTag
{
    internal static readonly StringName Key = "snowfallen_visual_character";
    internal const string Snowfallen = "snowfallen";
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
public static class SnowfallenCharacterVisualsTagPatch
{
    [HarmonyPostfix]
    public static void Postfix(CharacterModel __instance, NCreatureVisuals __result)
    {
        if (__result is null)
        {
            return;
        }

        if (__instance is SnowfallenCharacter)
        {
            __result.SetMeta(SnowfallenVisualTag.Key, SnowfallenVisualTag.Snowfallen);
        }
    }
}

/// <summary>
/// 假商人事件的玩家角色补丁：原版在这里播放占位角色的 relaxed_loop，
/// 模组改用自有的商店帧（与商店房间同一套），并按战斗/火堆同一口径定位缩放。
/// </summary>
[HarmonyPatch(typeof(NFakeMerchant), "AfterRoomIsLoaded")]
public static class SnowfallenFakeMerchantAnimationPatch
{
    private const string SnowfallenFramesPath =
        "res://Snowfallen/Animations/snowfallen_shop_frames.tres";
    private const string SnowfallenVisualName = "SnowfallenEventVisual";
    private static readonly StringName ShopAnimation = "Shop";
    // 与火堆/战斗补丁同一口径：精灵挂在角色视觉所在容器下，偏移 (0,-125)、基准缩放 0.32。
    private static readonly Vector2 VisualOffset = new(0f, -125f);
    private const float VisualScaleBase = 0.32f;
    private static SpriteFrames _cachedSnowfallenFrames;

    [HarmonyPostfix]
    public static void Postfix(NFakeMerchant __instance)
    {
        Control container = __instance.GetNodeOrNull<Control>("%CharacterContainer");
        if (container is null)
        {
            MegaCrit.Sts2.Core.Logging.Log.Warn(
                "[Snowfallen.Animation] Fake merchant event has no CharacterContainer.");
            return;
        }

        foreach (Node child in container.GetChildren())
        {
            if (child is not NCreatureVisuals visuals ||
                !visuals.HasMeta(SnowfallenVisualTag.Key) ||
                visuals.GetMeta(SnowfallenVisualTag.Key).AsString() != SnowfallenVisualTag.Snowfallen)
            {
                continue;
            }
            InstallEventVisual(container, visuals);
        }
    }

    private static void InstallEventVisual(
        Control container,
        NCreatureVisuals visuals)
    {
        string visualName = SnowfallenVisualName;
        if (container.GetNodeOrNull<AnimatedSprite2D>(visualName) is not null)
        {
            return;
        }

        SpriteFrames frames = GetFrames();
        if (frames is null || !frames.HasAnimation(ShopAnimation))
        {
            MegaCrit.Sts2.Core.Logging.Log.Error(
                "[Snowfallen.Animation] Missing event SpriteFrames for " +
                "Snowfallen");
            return;
        }

        // 只隐藏视觉节点本身，保留它在容器里的占位与事件逻辑（对话、点击区域等）。
        visuals.Visible = false;

        float scaleY = Mathf.Abs(visuals.Scale.Y) > 0.001f
            ? Mathf.Abs(visuals.Scale.Y)
            : 1f;
        AnimatedSprite2D sprite = new()
        {
            Name = visualName,
            SpriteFrames = frames,
            Animation = ShopAnimation,
            Position = visuals.Position + VisualOffset,
            Scale = new Vector2(VisualScaleBase * scaleY, VisualScaleBase * scaleY),
            FlipH = visuals.Scale.X < 0f,
            Centered = true,
            Modulate = visuals.Modulate,
            ZIndex = visuals.ZIndex,
            ZAsRelative = visuals.ZAsRelative,
            ProcessMode = Node.ProcessModeEnum.Always
        };
        container.AddChild(sprite);
        sprite.Play(ShopAnimation);
        MegaCrit.Sts2.Core.Logging.Log.Info(
            $"[Snowfallen.Animation] Installed event visual for " +
            $"Snowfallen in fake-merchant event.");
    }

    private static SpriteFrames GetFrames()
    {
return _cachedSnowfallenFrames ??=
            ResourceLoader.Load<SpriteFrames>(SnowfallenFramesPath);
    }
}
