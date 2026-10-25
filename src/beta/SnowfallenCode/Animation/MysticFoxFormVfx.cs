using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Forms;
using MegaCrit.Sts2.Core.TestSupport;
using SnowfallenMod.SnowfallenCode.Powers;

namespace SnowfallenMod.SnowfallenCode.Animation;

/// <summary>
/// Persistent, mod-owned form layer. Uses Demon Form's actual glow/fire-flow
/// materials with an instance-local water palette, replacing embers with water.
/// Kept separate from vanilla's single-slot FormVfx so another form cannot erase it.
/// </summary>
internal partial class MysticFoxFormVfx : NFormVfx
{
    internal const string NodeName = "SnowfallenMysticFoxFormVfx";
    internal const string SpiritPath =
        "res://Snowfallen/Images/Vfx/MysticFox/mystic_fox_water_spirit_v2.png";
    internal const float SpiritOpacity = 0.4f;
    private const float EchoPeriod = 2.3f;
    private const float WaterPulseDuration = 1.15f;
    internal const string TidalTexturePath =
        "res://Snowfallen/Images/Vfx/MysticFox/mystic_fox_tidal_ring_v1.png";
    internal const string TidalShaderPath =
        "res://Snowfallen/Images/Vfx/MysticFox/mystic_fox_tidal_ring.gdshader";
    private static Texture2D _spiritTexture;
    private static Texture2D _tidalTexture;
    private static Shader _tidalShader;
    private Creature _creature;
    private NCreatureVisuals _visuals;
    private GpuParticles2D _waterBurst;
    private Node2D _frontWater;
    private Sprite2D _spiritEcho;
    private float _echoTime;
    private readonly List<WaterPulse> _waterPulses = [];

    private sealed class WaterPulse
    {
        public Node2D Root;
        public Node2D FrontRoot;
        public Sprite2D Ring;
        public Sprite2D FrontRing;
        public ShaderMaterial BackMaterial;
        public ShaderMaterial FrontMaterial;
        public float Age;
    }

    internal static MysticFoxFormVfx Ensure(Creature owner) =>
        TestMode.IsOn ? null : Ensure(NCombatRoom.Instance?.GetCreatureNode(owner));

    internal static MysticFoxFormVfx Ensure(NCreature creatureNode)
    {
        if (creatureNode?.Entity is not { IsAlive: true } entity ||
            !HasForm(entity) || creatureNode.Visuals is not { } visuals)
            return null;
        var existing = visuals.GetNodeOrNull<MysticFoxFormVfx>(NodeName);
        if (existing is not null && !existing.IsQueuedForDeletion()) return existing;
        MysticFoxFormVfx effect = null;
        try
        {
            effect = new MysticFoxFormVfx { Name = NodeName, _creature = entity, _visuals = visuals };
            visuals.AddChild(effect);
            // Same layer as the body, drawn before it. Never alter the body's material.
            visuals.MoveChild(effect, 0);
            // Reparent after AddChild/Ready completes. The front crest is drawn
            // after the body without raising its global Z above other creatures.
            effect._frontWater.Reparent(visuals, false);
            return effect;
        }
        catch (Exception error)
        {
            effect?.SetActive(false);
            Log.Warn($"[Snowfallen.MysticFox] Could not create visual: {error.Message}");
            return null;
        }
    }

    private static bool HasForm(Creature creature) =>
        creature.Powers.OfType<MysticFoxFormPower>().Any(power => power.Amount > 0);

    internal static void Remove(Creature owner) =>
        Remove(NCombatRoom.Instance?.GetCreatureNode(owner));

    internal static void Remove(NCreature creatureNode)
    {
        var effect = creatureNode?.Visuals?.GetNodeOrNull<MysticFoxFormVfx>(NodeName);
        if (effect is not null && !effect.IsQueuedForDeletion()) effect.SetActive(false);
    }

    public override void _Ready()
    {
        BuildVisuals();
        SetActive(true);
    }

    private void BuildVisuals()
    {
        // Load raw bytes: the release pack deliberately includes the original RGBA
        // PNG, so this does not rely on a developer's .godot texture import cache.
        if (_spiritTexture is null)
        {
            using var image = Image.LoadFromFile(SpiritPath);
            if (image is null || image.IsEmpty())
                throw new InvalidOperationException($"Missing Mystic Fox texture: {SpiritPath}");
            _spiritTexture = ImageTexture.CreateFromImage(image);
        }
        if (_tidalTexture is null)
        {
            using var image = Image.LoadFromFile(TidalTexturePath);
            if (image is null || image.IsEmpty())
                throw new InvalidOperationException($"Missing tidal water texture: {TidalTexturePath}");
            _tidalTexture = ImageTexture.CreateFromImage(image);
        }
        if (_tidalShader is null)
        {
            string code = Godot.FileAccess.GetFileAsString(TidalShaderPath);
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException($"Missing tidal water shader: {TidalShaderPath}");
            _tidalShader = new Shader { Code = code };
        }
        _frontWater = new Node2D { Name = "MysticFoxFrontWater" };
        AddChild(_frontWater);

        // Independent low-opacity echo: the actual fox never fades out.
        _spiritEcho = new Sprite2D
        {
            Name = "FoxSpiritEcho", Texture = _spiritTexture,
            Position = new Vector2(-65, -165),
            Scale = Vector2.One * (350f / _spiritTexture.GetWidth()),
            SelfModulate = new Color(1, 1, 1, 0)
        };
        AddChild(_spiritEcho);
        AddChild(new Sprite2D
        {
            Name = "FoxSpirit", Texture = _spiritTexture,
            Position = new Vector2(-65, -165),
            Scale = Vector2.One * (350f / _spiritTexture.GetWidth()),
            SelfModulate = new Color(1, 1, 1, SpiritOpacity)
        });

        // These are the very same two material resources used by Demon Form.
        // Duplicate before changing uniforms; vanilla and other players stay intact.
        var glow = (ShaderMaterial)GD.Load<ShaderMaterial>(
            "res://materials/vfx/common/vfx_glow_noise_scale.tres").Duplicate(true);
        var glowSprite = new Sprite2D
        {
            Name = "DemonFormWaterGlow", Material = glow,
            Texture = GD.Load<Texture2D>("res://images/vfx/common/common_glow.png"),
            Position = new Vector2(0, -125), Scale = Vector2.One * 2.8f,
            SelfModulate = new Color(0.08f, 0.42f, 0.7f, 0.055f)
        };
        glowSprite.SetInstanceShaderParameter("use_r_as_alpha", 1f);
        AddChild(glowSprite);

        var flow = (ShaderMaterial)GD.Load<ShaderMaterial>(
            "res://materials/vfx/forms/demon/vfx_demon_form_idle_panning_fire.tres").Duplicate(true);
        flow.SetShaderParameter("lut", new GradientTexture1D
        {
            Gradient = new Gradient
            {
                Colors = [new Color(0.01f, 0.18f, 0.42f), new Color(0.08f, 0.55f, 0.85f)]
            }
        });
        // Slow the original two panning samples into a low-key stream.
        flow.SetShaderParameter("main_st_a", new Vector4(0.75f, 0.65f, 0.24f, 0));
        flow.SetShaderParameter("main_st_b", new Vector4(1.5f, 1.5f, 0.4f, 0));
        AddChild(new Sprite2D
        {
            Name = "DemonFormWaterFlow", Material = flow,
            Texture = GD.Load<Texture2D>("res://images/vfx/noise/vfx_noise_4.png"),
            TextureRepeat = TextureRepeatEnum.Enabled,
            Rotation = Mathf.Pi / 2, Scale = new Vector2(1.2f, 0.8f),
            SelfModulate = new Color(1, 1, 1, 0.085f)
        });

        // Procedural water droplets/streaks, not recolored fire/embers or smoke.
        var droplet = new GradientTexture2D
        {
            Width = 12, Height = 36,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(0.5f, 0),
            Gradient = new Gradient
            {
                Offsets = [0, 0.35f, 1],
                Colors = [Colors.White, new Color(0.6f, 0.9f, 1, 0.7f), new Color(0.3f, 0.7f, 1, 0)]
            }
        };
        var idle = MakeWaterParticles("AmbientWater", droplet, false);
        idle.Amount = 5;
        idle.Lifetime = 2.2;
        idle.Modulate = new Color(1, 1, 1, 0.12f);
        AddChild(idle);
        _waterBurst = MakeWaterParticles("ThresholdWaterBurst", droplet, true);
        AddChild(_waterBurst);
    }

    private static GpuParticles2D MakeWaterParticles(string name, Texture2D texture, bool burst)
    {
        var fade = new GradientTexture1D
        {
            Gradient = new Gradient
            {
                Offsets = [0, 0.15f, 0.7f, 1],
                Colors = [new Color(0.15f, 0.65f, 1, 0), new Color(0.25f, 0.8f, 1, 0.7f),
                    new Color(0.08f, 0.5f, 0.9f, 0.35f), new Color(0.08f, 0.4f, 0.8f, 0)]
            }
        };
        var process = new ParticleProcessMaterial
        {
            ParticleFlagDisableZ = true,
            EmissionShape = burst ? ParticleProcessMaterial.EmissionShapeEnum.Ring : ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(burst ? 50 : 75, 28, 0),
            EmissionRingAxis = new Vector3(0, 0, 1), EmissionRingRadius = 90,
            EmissionRingInnerRadius = 72, EmissionRingHeight = 1,
            Direction = new Vector3(0, -1, 0), Spread = burst ? 55 : 15,
            InitialVelocityMin = burst ? 75 : 12, InitialVelocityMax = burst ? 130 : 25,
            Gravity = new Vector3(0, burst ? 85 : -4, 0),
            ScaleMin = burst ? 0.4f : 0.18f, ScaleMax = burst ? 0.75f : 0.3f,
            ColorRamp = fade
        };
        return new GpuParticles2D
        {
            Name = name, Texture = texture, ProcessMaterial = process,
            Position = new Vector2(0, -115), Amount = burst ? 14 : 5,
            Lifetime = burst ? 0.7 : 2.2, OneShot = burst, Emitting = !burst,
            Explosiveness = burst ? 0.85f : 0, LocalCoords = true,
            VisibilityRect = new Rect2(-220, -350, 440, 450),
            Modulate = new Color(1, 1, 1, burst ? 0.48f : 0.12f)
        };
    }

    public override void OnEffectTriggered()
    {
        if (!_isActive) return;
        _waterBurst?.Restart();
        StartOutwardWaterPulse();
    }

    private void StartOutwardWaterPulse()
    {
        if (_waterPulses.Count == 3)
        {
            FreeWaterPulse(_waterPulses[0]);
            _waterPulses.RemoveAt(0);
        }
        var root = new Node2D { Name = "OutwardWaterPulse", Position = new Vector2(0, -15) };
        var frontRoot = new Node2D { Name = "FrontWaterPulse", Position = root.Position };
        var backMaterial = new ShaderMaterial { Shader = _tidalShader };
        var frontMaterial = new ShaderMaterial { Shader = _tidalShader };
        backMaterial.SetShaderParameter("half_select", -1);
        frontMaterial.SetShaderParameter("half_select", 1);
        var ring = new Sprite2D { Name = "TidalWaterBack", Texture = _tidalTexture, Material = backMaterial };
        var frontRing = new Sprite2D { Name = "TidalWaterFront", Texture = _tidalTexture, Material = frontMaterial };
        root.AddChild(ring);
        frontRoot.AddChild(frontRing);
        AddChild(root);
        _frontWater.AddChild(frontRoot);
        var pulse = new WaterPulse
        {
            Root = root, FrontRoot = frontRoot, Ring = ring, FrontRing = frontRing,
            BackMaterial = backMaterial, FrontMaterial = frontMaterial
        };
        _waterPulses.Add(pulse);
        UpdateWaterPulse(pulse);
    }

    private static void UpdateWaterPulse(WaterPulse pulse)
    {
        float progress = Mathf.Clamp(pulse.Age / WaterPulseDuration, 0, 1);
        float expansion = 1 - Mathf.Pow(1 - progress, 1.6f);
        float width = Mathf.Lerp(150, 530, expansion);
        pulse.Ring.Scale = pulse.FrontRing.Scale = Vector2.One * (width / _tidalTexture.GetWidth());
        float opacity = Mathf.SmoothStep(0, 0.12f, progress) * (1 - Mathf.SmoothStep(0.3f, 1, progress)) * 0.8f;
        pulse.BackMaterial.SetShaderParameter("phase", pulse.Age);
        pulse.FrontMaterial.SetShaderParameter("phase", pulse.Age);
        pulse.BackMaterial.SetShaderParameter("opacity", opacity);
        pulse.FrontMaterial.SetShaderParameter("opacity", opacity);
    }

    private static void FreeWaterPulse(WaterPulse pulse)
    {
        pulse.Root.Hide();
        pulse.FrontRoot.Hide();
        pulse.Root.QueueFree();
        pulse.FrontRoot.QueueFree();
    }

    private void AdvanceSpectralMotion(float delta)
    {
        if (!_isActive) return;
        _echoTime = (_echoTime + delta) % EchoPeriod;
        float progress = _echoTime / EchoPeriod;
        if (_spiritEcho is not null)
        {
            _spiritEcho.Scale = Vector2.One * (350f / _spiritTexture.GetWidth()) * Mathf.Lerp(1, 1.25f, progress);
            float fadeIn = Mathf.Min(progress / 0.07f, 1);
            _spiritEcho.SelfModulate = new Color(1, 1, 1, 0.18f * fadeIn * Mathf.Pow(1 - progress, 1.6f));
        }
        for (int i = _waterPulses.Count - 1; i >= 0; i--)
        {
            var pulse = _waterPulses[i];
            pulse.Age += delta;
            if (pulse.Age >= WaterPulseDuration)
            {
                FreeWaterPulse(pulse);
                _waterPulses.RemoveAt(i);
            }
            else UpdateWaterPulse(pulse);
        }
    }

    public override void SetActive(bool isActive)
    {
        base.SetActive(isActive);
        if (isActive) return;
        Hide();
        if (GodotObject.IsInstanceValid(_frontWater))
        {
            _frontWater.Hide();
            _frontWater.QueueFree();
        }
        SetProcess(false);
        QueueFree();
    }

    public override void _ExitTree()
    {
        // Also release the sibling layer if its owner is removed externally.
        if (GodotObject.IsInstanceValid(_frontWater) && !_frontWater.IsQueuedForDeletion())
        {
            _frontWater.Hide();
            _frontWater.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        AdvanceSpectralMotion((float)delta);
        if (_creature is null) return; // Also supports an isolated visual preview.
        if (!_creature.IsAlive || !HasForm(_creature))
        {
            SetActive(false);
            return;
        }
        // Match surrounded/body flips without following its internal sprite scale.
        var body = _visuals.GetCurrentBody();
        Scale = new Vector2(body.Scale.X < 0 ? -1 : 1, 1);
        if (GodotObject.IsInstanceValid(_frontWater) && _frontWater.GetParent() != this)
            _frontWater.Scale = Scale;
    }
}
