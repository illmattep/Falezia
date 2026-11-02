extends Node3D
# Tavola di revisione di un'arma: viste del doc 12 + silhouette + pezzi mossi.
# Uso: godot --path . -- <file.glb> <prefisso_uscita> <lunghezza_m>
var model; var cam; var env; var lights = []
var views = []; var idx = 0; var frame = 0
var out = ""; var L = 0.2
var slide; var mag; var pump; var slide_rest; var mag_rest; var pump_rest

func _ready():
	var args = OS.get_cmdline_user_args()
	out = args[1]; L = float(args[2])
	var doc = GLTFDocument.new(); var st = GLTFState.new()
	var err = doc.append_from_file(args[0], st)
	if err != OK:
		print("ERRORE caricamento ", err); get_tree().quit(1); return
	model = doc.generate_scene(st); add_child(model)
	slide = model.find_child("slide", true, false); mag = model.find_child("mag", true, false); pump = model.find_child("pump", true, false)
	if slide: slide_rest = slide.position
	if mag: mag_rest = mag.position
	if pump: pump_rest = pump.position
	var ap = model.find_child("AnimationPlayer", true, false)
	if ap: ap.stop()

	env = Environment.new(); env.background_mode = Environment.BG_COLOR; env.background_color = Color("#4A4A4A")
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR; env.ambient_light_color = Color(0.55, 0.55, 0.55); env.ambient_light_energy = 0.35
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC
	var we = WorldEnvironment.new(); we.environment = env; add_child(we)
	# luci del doc 12: principale 45° alto-sinistra 5600K, riempimento -1,5 stop, bordo da dietro 6500K
	var key = DirectionalLight3D.new(); key.light_color = Color(1.0, 0.93, 0.86); key.light_energy = 1.6; add_child(key)
	var fill = DirectionalLight3D.new(); fill.light_color = Color(1.0, 0.93, 0.86); fill.light_energy = 1.6 * 0.35; add_child(fill)
	var rim = DirectionalLight3D.new(); rim.light_color = Color(1, 1, 1); rim.light_energy = 1.6 * 1.4 * 0.6; add_child(rim)
	lights = [key, fill, rim]
	cam = Camera3D.new(); cam.near = 0.005; cam.far = 20; add_child(cam); cam.current = true
	var aabb = total_aabb(model)
	var c = aabb.get_center()
	print("ORTHO ", max(aabb.size.z * 1000.0 / 1600.0, aabb.size.y) / 0.85)
	var d = L * 2.6
	# [nome, posizione, bersaglio, ortografica?, dimensione, posa]
	views = [
		["1_profilo_sx", c + Vector3(-d, 0, 0), c, true, max(aabb.size.z * 1000.0 / 1600.0, aabb.size.y) / 0.85, "rest"],
		["2_tre_quarti_ant_sx", c + Vector3(-d * 0.55, d * 0.22, -d * 0.55), c, false, 35.0, "rest"],
		["3_tre_quarti_post_dx", c + Vector3(d * 0.5, d * 0.25, d * 0.6), c, false, 35.0, "rest"],
		["4_culatta", Vector3(0, aabb.end.y * 0.6, 0) + Vector3(0.09, 0.05, 0.16) * (L / 0.2 if L < 0.3 else 1.4), Vector3(0, aabb.end.y * 0.6, -0.02), false, 30.0, "rest"],
		["5_pezzi_mossi", c + Vector3(d * 0.5, d * 0.1, d * 0.45), c, false, 35.0, "moved"],
		["6_prima_persona", Vector3(-0.15, 0.135, 0.33) + Vector3(0, 0.02, 0.0), Vector3(-0.15, 0.10, -0.6), false, 70.0, "rest"],
		["silhouette", c + Vector3(-d, 0, 0), c, true, max(aabb.size.z * 1000.0 / 1600.0, aabb.size.y) / 0.85, "sil"],
	]

func total_aabb(n):
	var box = AABB(); var first = true
	for mi in n.find_children("*", "MeshInstance3D", true, false):
		var b = mi.global_transform * mi.get_aabb()
		if first: box = b; first = false
		else: box = box.merge(b)
	return box

func set_pose(p):
	if slide: slide.position = slide_rest + (Vector3(0, 0, 0.026) if p == "moved" else Vector3.ZERO)
	if mag: mag.position = mag_rest + (Vector3(0, -0.06, 0.01) if p == "moved" else Vector3.ZERO)
	if pump: pump.position = pump_rest + (Vector3(0, 0, 0.09) if p == "moved" else Vector3.ZERO)
	var black = StandardMaterial3D.new(); black.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED; black.albedo_color = Color.BLACK
	for mi in model.find_children("*", "MeshInstance3D", true, false):
		mi.material_override = black if p == "sil" else null
	env.background_color = Color.WHITE if p == "sil" else Color("#4A4A4A")

func _process(_d):
	frame += 1
	if idx >= views.size():
		get_tree().quit(); return
	var v = views[idx]
	if frame % 8 == 1:
		set_pose(v[5])
		cam.position = v[1]; cam.look_at(v[2], Vector3.UP)
		if v[3]:
			cam.projection = Camera3D.PROJECTION_ORTHOGONAL; cam.size = v[4]
		else:
			cam.projection = Camera3D.PROJECTION_PERSPECTIVE; cam.fov = v[4]
		# luci relative alla camera: principale alto-sinistra, riempimento a destra, bordo da dietro
		var b = cam.global_transform.basis
		lights[0].look_at_from_position(Vector3.ZERO, (-b.z + b.y * -1.0 + b.x * 1.0).normalized() * 1.0 + Vector3.ZERO, Vector3.UP)
		lights[1].look_at_from_position(Vector3.ZERO, (-b.z - b.x * 0.6).normalized(), Vector3.UP)
		lights[2].look_at_from_position(Vector3.ZERO, (b.z - b.y * 0.4).normalized(), Vector3.UP)
	if frame % 8 == 0:
		get_viewport().get_texture().get_image().save_png("%s_%s.png" % [out, v[0]])
		idx += 1
