extends Control

const LOOP_SECONDS: float = 6.0

const BLUE_POINT_TEXTURE: Texture2D = preload("res://Snowfallen/Images/CharacterSelect/Effects/blue_point.png")
const SPARKLE_TEXTURE: Texture2D = preload("res://Snowfallen/Images/CharacterSelect/Effects/sparkle.png")
const ICE_DUST_TEXTURE: Texture2D = preload("res://Snowfallen/Images/CharacterSelect/Effects/ice_dust.png")
const WATER_GLOW_TEXTURE: Texture2D = preload("res://Snowfallen/Images/CharacterSelect/Effects/water_glow.png")
const WATER_RING_TEXTURE: Texture2D = preload("res://Snowfallen/Images/CharacterSelect/Effects/water_ring.png")
const MOON_HALO_TEXTURE: Texture2D = preload("res://Snowfallen/Images/CharacterSelect/Effects/moon_halo.png")

const STATIONARY_LIGHTS: Array[Vector4] = [
	Vector4(0.12022, 0.09775, 1.865, 0.57499), Vector4(0.09244, 0.55678, 3.092, 0.65599),
	Vector4(0.11977, 0.90508, 1.618, 0.07113), Vector4(0.27860, 0.89980, 3.595, 0.47877),
	Vector4(0.33294, 0.83200, 2.411, 0.45741), Vector4(0.05319, 0.13004, 3.885, 0.75437),
	Vector4(0.17426, 0.57610, 1.851, 0.20526), Vector4(0.55650, 0.37748, 2.076, 0.68250),
	Vector4(0.41465, 0.32999, 1.717, 0.56708), Vector4(0.39120, 0.90724, 3.792, 0.20248),
	Vector4(0.43818, 0.90654, 3.997, 0.10238), Vector4(0.55495, 0.78558, 1.720, 0.43300),
	Vector4(0.47085, 0.35730, 2.280, 0.74996), Vector4(0.48629, 0.38382, 4.103, 0.17451),
	Vector4(0.40072, 0.64502, 1.983, 0.19440), Vector4(0.79284, 0.15533, 1.780, 0.91479),
	Vector4(0.74264, 0.17483, 3.620, 0.03328), Vector4(0.83544, 0.84590, 2.775, 0.08512),
	Vector4(0.73440, 0.43406, 3.892, 0.71912), Vector4(0.84125, 0.19578, 3.063, 0.62752),
	Vector4(0.83443, 0.28834, 3.481, 0.78681), Vector4(0.94806, 0.77548, 4.043, 0.21049),
	Vector4(0.75278, 0.76647, 3.502, 0.22921), Vector4(0.64387, 0.44378, 1.656, 0.02689),
]

const STATIONARY_CYCLES: Array[float] = [
	2, 2, 2, 2, 2, 3, 2, 3, 3, 2, 3, 3, 2, 2, 3, 3, 3, 2, 2, 2, 2, 2, 3, 2,
]

const STATIONARY_INTENSITIES: Array[float] = [
	0.792, 0.713, 0.707, 0.714, 0.782, 0.823, 0.741, 0.828,
	0.829, 0.915, 0.891, 0.889, 0.847, 0.853, 0.862, 0.989,
	0.979, 0.941, 1.026, 1.070, 1.079, 1.058, 0.963, 0.971,
]

const LOCAL_LIGHTS: Array[Vector4] = [
	Vector4(0.10, 0.30, 0.23033, 2), Vector4(0.18, 0.66, 0.69253, 2),
	Vector4(0.27, 0.20, 0.93669, 3), Vector4(0.39, 0.48, 0.10857, 2),
	Vector4(0.48, 0.72, 0.35526, 3), Vector4(0.58, 0.18, 0.22177, 2),
	Vector4(0.71, 0.43, 0.85978, 2), Vector4(0.76, 0.50, 0.96540, 2),
	Vector4(0.83, 0.39, 0.90394, 2), Vector4(0.86, 0.70, 0.07101, 2),
	Vector4(0.90, 0.57, 0.28963, 2),
]

var _time: float = 0.0
var _additive_material: CanvasItemMaterial
var _water_glow: Sprite2D
var _water_glow_base_scale: Vector2
var _moon_halo: Sprite2D
var _water_rings: Array[Sprite2D] = []
var _stationary_sprites: Array[Sprite2D] = []
var _stationary_base_scales: Array[float] = []
var _local_sprites: Array[Sprite2D] = []
var _ice_particles: CPUParticles2D


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_additive_material = CanvasItemMaterial.new()
	_additive_material.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD

	_water_glow = _create_sprite("WaterGlow", WATER_GLOW_TEXTURE, 0)
	_moon_halo = _create_sprite("MoonHalo", MOON_HALO_TEXTURE, 2)
	for index: int in range(2):
		_water_rings.append(_create_sprite("WaterRing%d" % index, WATER_RING_TEXTURE, 3))

	for index: int in range(STATIONARY_LIGHTS.size()):
		_stationary_sprites.append(_create_sprite("BluePoint%02d" % index, BLUE_POINT_TEXTURE, 5))
		_stationary_base_scales.append(1.0)
	for index: int in range(LOCAL_LIGHTS.size()):
		_local_sprites.append(_create_sprite("Sparkle%02d" % index, SPARKLE_TEXTURE, 6))

	_create_ice_particles()
	_apply_layout()
	_update_animation()


func _process(delta: float) -> void:
	_time = fposmod(_time + delta, LOOP_SECONDS)
	_update_animation()


func _notification(what: int) -> void:
	if what == NOTIFICATION_RESIZED and is_node_ready():
		_apply_layout()


func _create_sprite(node_name: String, texture: Texture2D, layer: int) -> Sprite2D:
	var sprite := Sprite2D.new()
	sprite.name = node_name
	sprite.texture = texture
	sprite.centered = true
	sprite.z_index = layer
	sprite.material = _additive_material
	add_child(sprite)
	return sprite


func _create_ice_particles() -> void:
	_ice_particles = CPUParticles2D.new()
	_ice_particles.name = "IceDust"
	_ice_particles.z_index = 4
	_ice_particles.material = _additive_material
	_ice_particles.texture = ICE_DUST_TEXTURE
	_ice_particles.amount = 28
	_ice_particles.lifetime = LOOP_SECONDS
	_ice_particles.preprocess = LOOP_SECONDS
	_ice_particles.randomness = 0.88
	_ice_particles.emission_shape = CPUParticles2D.EMISSION_SHAPE_RECTANGLE
	_ice_particles.direction = Vector2(0.10, 1.0)
	_ice_particles.spread = 7.0
	_ice_particles.gravity = Vector2(3.0, 5.0)
	_ice_particles.initial_velocity_min = 10.0
	_ice_particles.initial_velocity_max = 23.0
	_ice_particles.scale_amount_min = 0.24
	_ice_particles.scale_amount_max = 0.62
	_ice_particles.color = Color(0.72, 0.92, 1.0, 0.34)
	_ice_particles.local_coords = true
	_ice_particles.emitting = true
	add_child(_ice_particles)


func _apply_layout() -> void:
	if size.x <= 1.0 or size.y <= 1.0:
		return
	var pixel_scale: float = minf(size.x / 1280.0, size.y / 720.0)
	var water_center := Vector2(size.x * 0.79, size.y * 0.58)
	_water_glow.position = water_center
	_water_glow_base_scale = Vector2(size.x * 0.34 / WATER_GLOW_TEXTURE.get_width(), size.y * 0.50 / WATER_GLOW_TEXTURE.get_height())
	_water_glow.scale = _water_glow_base_scale
	_moon_halo.position = Vector2(size.x * 0.817, size.y * 0.121)
	var moon_scale: float = size.x * 0.115 / MOON_HALO_TEXTURE.get_width()
	_moon_halo.scale = Vector2.ONE * moon_scale

	for ring: Sprite2D in _water_rings:
		ring.position = Vector2(size.x * 0.785, size.y * 0.585)

	for index: int in range(_stationary_sprites.size()):
		var light: Vector4 = STATIONARY_LIGHTS[index]
		_stationary_sprites[index].position = Vector2(light.x * size.x, light.y * size.y)
		_stationary_base_scales[index] = pixel_scale * (0.28 + light.z * 0.085)

	for index: int in range(_local_sprites.size()):
		var light: Vector4 = LOCAL_LIGHTS[index]
		_local_sprites[index].position = Vector2(light.x * size.x, light.y * size.y)

	_ice_particles.position = size * 0.5
	_ice_particles.emission_rect_extents = Vector2(size.x * 0.48, size.y * 0.52)


func _update_animation() -> void:
	var pulse: float = 0.5 + 0.5 * sin(TAU * _time / 3.0 - 0.5)
	_water_glow.modulate = Color(1.0, 1.0, 1.0, 0.55 + 0.30 * pulse)
	_water_glow.scale = _water_glow_base_scale * (1.0 + 0.008 * sin(TAU * _time / LOOP_SECONDS))
	_moon_halo.modulate = Color(1.0, 1.0, 1.0, 0.58 + 0.22 * pulse)

	for index: int in range(_water_rings.size()):
		var p: float = fposmod(_time / LOOP_SECONDS + float(index) * 0.5, 1.0)
		var local: float = fposmod(p * 2.0, 1.0)
		var ring: Sprite2D = _water_rings[index]
		if local > 0.72:
			ring.visible = false
			continue
		ring.visible = true
		var fade: float = pow(1.0 - local / 0.72, 1.7)
		var target_size := Vector2(size.x * (0.035 + 0.145 * local), size.y * (0.025 + 0.135 * local))
		ring.scale = Vector2(target_size.x / WATER_RING_TEXTURE.get_width(), target_size.y / WATER_RING_TEXTURE.get_height())
		ring.modulate = Color(1.0, 1.0, 1.0, 0.62 * fade)

	for index: int in range(_stationary_sprites.size()):
		var light: Vector4 = STATIONARY_LIGHTS[index]
		var wave: float = maxf(0.0, sin(TAU * (STATIONARY_CYCLES[index] * _time / LOOP_SECONDS + light.w)))
		var twinkle: float = 0.10 + 0.90 * pow(wave, 5.0)
		var sprite: Sprite2D = _stationary_sprites[index]
		var scale_value: float = _stationary_base_scales[index] * (0.94 + 0.12 * wave)
		sprite.scale = Vector2.ONE * scale_value
		sprite.modulate = Color(1.0, 1.0, 1.0, minf(1.0, twinkle * STATIONARY_INTENSITIES[index]))

	var pixel_scale: float = minf(size.x / 1280.0, size.y / 720.0)
	for index: int in range(_local_sprites.size()):
		var light: Vector4 = LOCAL_LIGHTS[index]
		var wave: float = pow(maxf(0.0, sin(TAU * (light.w * _time / LOOP_SECONDS + light.z))), 5.0)
		var sparkle: Sprite2D = _local_sprites[index]
		sparkle.visible = wave >= 0.02
		if sparkle.visible:
			var scale_value: float = pixel_scale * (0.13 + 0.11 * wave)
			sparkle.scale = Vector2.ONE * scale_value
			sparkle.modulate = Color(1.0, 1.0, 1.0, 0.82 * wave)
