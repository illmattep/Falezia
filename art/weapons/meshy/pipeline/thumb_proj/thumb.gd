extends Node3D
# Miniature dello shop: profilo destro (volata a destra), sfondo trasparente, 512×256,
# inquadratura come art/weapons/arsenal2/thumbs.py. Uso: godot --path . -- <glb> <png>
var frame = 0
var out = ""
func _ready():
	get_viewport().transparent_bg = true
	var a = OS.get_cmdline_user_args(); out = a[1]
	var doc = GLTFDocument.new(); var st = GLTFState.new()
	doc.append_from_file(a[0], st)
	var m = doc.generate_scene(st); add_child(m)
	var ap = m.find_child("AnimationPlayer", true, false)
	if ap: ap.stop()
	var box = AABB(); var first = true
	for mi in m.find_children("*", "MeshInstance3D", true, false):
		var bb = mi.global_transform * mi.get_aabb()
		if first: box = bb; first = false
		else: box = box.merge(bb)
	var c = box.get_center()
	var env = Environment.new(); env.background_mode = Environment.BG_CLEAR_COLOR
	env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR; env.ambient_light_color = Color(0.55, 0.60, 0.66); env.ambient_light_energy = 0.55
	env.tonemap_mode = Environment.TONE_MAPPER_FILMIC; env.tonemap_exposure = 1.15
	var we = WorldEnvironment.new(); we.environment = env; add_child(we)
	for l in [[Vector3(-0.5, -0.8, -0.4), 1.7, Color(1, 0.97, 0.92)], [Vector3(0.3, -0.5, 0.8), 0.7, Color(0.80, 0.88, 1.0)], [Vector3(-0.8, 0.3, 0.5), 0.9, Color(1.0, 0.93, 0.85)]]:
		var d = DirectionalLight3D.new(); d.light_color = l[2]; d.light_energy = l[1]; add_child(d)
		d.look_at_from_position(Vector3.ZERO, l[0], Vector3.UP if abs(l[0].normalized().y) < 0.99 else Vector3.FORWARD)
	var cam = Camera3D.new(); cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = max(box.size.z, box.size.y * 2.0) * 1.12 * 0.5
	cam.near = 0.01; cam.far = 10
	add_child(cam); cam.current = true
	cam.position = c + Vector3(2, 0, 0); cam.look_at(c, Vector3.UP)
func _process(_d):
	frame += 1
	if frame == 6:
		get_viewport().get_texture().get_image().save_png(out)
		get_tree().quit()
