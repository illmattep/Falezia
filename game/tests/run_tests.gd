extends SceneTree
## Test headless della simulazione. Non serve una finestra, non serve un server.
##   godot --headless --path game --script res://tests/run_tests.gd

const C = preload("res://sim/constants.gd")
const Sim = preload("res://sim/sim.gd")
const SimWorld = preload("res://sim/sim_world.gd")
const SimState = preload("res://sim/sim_state.gd")
const SimInput = preload("res://sim/sim_input.gd")
const Protocol = preload("res://net/protocol.gd")

var passed: int = 0
var failed: int = 0


func _initialize() -> void:
	print("")
	print("=========== TEST M0 ===========")
	test_gravity()
	test_walk_speed()
	test_sprint_speed()
	test_crouch_speed()
	test_jump_height()
	test_no_air_strafe()
	test_wall_collision()
	test_stand_on_crate()
	test_determinism()
	test_protocol_input()
	test_protocol_snapshot()
	print("-------------------------------")
	print("  passati: %d    falliti: %d" % [passed, failed])
	print("")
	quit(0 if failed == 0 else 1)


func check(name: String, cond: bool, detail: String = "") -> void:
	if cond:
		passed += 1
		print("  OK    %s" % name)
	else:
		failed += 1
		print("  FAIL  %s   %s" % [name, detail])


func _hspeed(p) -> float:
	return sqrt(p.vel.x * p.vel.x + p.vel.z * p.vel.z)


func _run(state, world, inp, ticks: int) -> void:
	for i in range(ticks):
		Sim.step(state, {1: inp}, world)


# --------------------------------------------------------------- test

func test_gravity() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(0, 5, 0))
	var inp = SimInput.new()
	_run(s, w, inp, 180)
	var p = s.players[1]
	check("gravita: atterra", p.on_floor, "on_floor=%s y=%.3f" % [p.on_floor, p.pos.y])
	check("gravita: centro a meta altezza", absf(p.pos.y - C.HEIGHT * 0.5) < 0.05,
		"y=%.3f atteso %.3f" % [p.pos.y, C.HEIGHT * 0.5])


func test_walk_speed() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(0, 0.2, 0))
	var inp = SimInput.new()
	inp.move = Vector2(0, 1)
	_run(s, w, inp, 128)
	var v = _hspeed(s.players[1])
	check("passo: ~4.2 m/s", absf(v - C.WALK_SPEED) < 0.25, "%.3f" % v)


func test_sprint_speed() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(0, 0.2, 0))
	var inp = SimInput.new()
	inp.move = Vector2(0, 1)
	inp.sprint = true
	_run(s, w, inp, 128)
	var v = _hspeed(s.players[1])
	check("sprint: ~6.5 m/s", absf(v - C.SPRINT_SPEED) < 0.35, "%.3f" % v)


func test_crouch_speed() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(0, 0.2, 0))
	var inp = SimInput.new()
	inp.move = Vector2(0, 1)
	inp.crouch = true
	_run(s, w, inp, 128)
	var p = s.players[1]
	var v = _hspeed(p)
	check("accovacciato: posizione abbassata", p.crouching)
	check("accovacciato: ~2.2 m/s", absf(v - C.CROUCH_SPEED) < 0.25, "%.3f" % v)


func test_jump_height() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(0, 0.2, 0))
	var idle = SimInput.new()
	_run(s, w, idle, 30)          # assestati a terra
	var base_y: float = s.players[1].pos.y
	var jump = SimInput.new()
	jump.jump = true
	Sim.step(s, {1: jump}, w)
	var idle2 = SimInput.new()
	var peak: float = s.players[1].pos.y
	for i in range(60):
		Sim.step(s, {1: idle2}, w)
		peak = maxf(peak, s.players[1].pos.y)
	var h: float = peak - base_y
	check("salto: altezza 0.8-1.2 m", h > 0.8 and h < 1.2, "%.3f m" % h)


func test_no_air_strafe() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(0, 0.2, 0))
	var fwd = SimInput.new()
	fwd.move = Vector2(0, 1)
	_run(s, w, fwd, 64)
	# salta mantenendo l'inerzia, poi strafe laterale in aria
	var j = SimInput.new()
	j.jump = true
	Sim.step(s, {1: j}, w)
	var before: float = _hspeed(s.players[1])
	var air = SimInput.new()
	air.move = Vector2(1, 0)
	_run(s, w, air, 22)
	var after: float = _hspeed(s.players[1])
	check("nessun air-strafe: guadagno < 0.8 m/s", after - before < 0.8,
		"prima %.3f dopo %.3f" % [before, after])


func test_wall_collision() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(-3.0, 0.2, 0))
	var inp = SimInput.new()
	inp.move = Vector2(-1, 0)   # verso il muro a x = -4
	_run(s, w, inp, 180)
	var p = s.players[1]
	check("muro: non penetra", p.pos.x >= -3.75, "x=%.3f" % p.pos.x)
	check("muro: si ferma al contatto", absf(p.pos.x - (-3.7)) < 0.1, "x=%.3f atteso -3.700" % p.pos.x)


func test_stand_on_crate() -> void:
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	# la cassa e' AABB(2,0,5) size (2.5,1,2.5): centro x=3.25 z=6.25, alto 1 m
	Sim.spawn(s, 1, Vector3(3.25, 4.0, 6.25))
	var inp = SimInput.new()
	_run(s, w, inp, 180)
	var p = s.players[1]
	var expected: float = 1.0 + C.HEIGHT * 0.5
	check("cassa: atterra sopra", absf(p.pos.y - expected) < 0.06,
		"y=%.3f atteso %.3f" % [p.pos.y, expected])


func test_determinism() -> void:
	var a = _scripted_run(12345)
	var b = _scripted_run(12345)
	var same: bool = a.players[1].pos.distance_to(b.players[1].pos) < 0.000001
	check("determinismo: due run identiche", same,
		"a=%s b=%s" % [a.players[1].pos, b.players[1].pos])
	var c = _scripted_run(999)
	var differs: bool = a.players[1].pos.distance_to(c.players[1].pos) > 0.0001
	check("determinismo: seed diverso -> risultato diverso", differs)


func _scripted_run(seed_value: int):
	var w = SimWorld.make_test_world()
	var s = SimState.new()
	Sim.spawn(s, 1, Vector3(0, 0.2, 0))
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	for i in range(300):
		var inp = SimInput.new()
		inp.move = Vector2(rng.randf_range(-1, 1), rng.randf_range(-1, 1))
		inp.yaw = rng.randf_range(-PI, PI)
		inp.pitch = rng.randf_range(-1, 1)
		inp.jump = rng.randf() < 0.08
		inp.sprint = rng.randf() < 0.5
		inp.crouch = rng.randf() < 0.1
		inp.ads = rng.randf() < 0.1
		Sim.step(s, {1: inp}, w)
	return s


func test_protocol_input() -> void:
	var i = SimInput.new()
	i.tick = 4242
	i.move = Vector2(-0.5, 0.75)
	i.yaw = 1.234
	i.pitch = -0.567
	i.jump = true
	i.sprint = true
	i.crouch = false
	i.ads = true
	var d = Protocol.decode_input(Protocol.encode_input(i))
	check("protocollo input: non nullo", d != null)
	if d == null:
		return
	check("protocollo input: tick", d.tick == 4242)
	check("protocollo input: move", d.move.distance_to(Vector2(-0.5, 0.75)) < 0.0001)
	check("protocollo input: yaw/pitch", absf(d.yaw - 1.234) < 0.0001 and absf(d.pitch + 0.567) < 0.0001)
	check("protocollo input: flag", d.jump and d.sprint and d.ads and not d.crouch)


func test_protocol_snapshot() -> void:
	var s = SimState.new()
	var p = s.ensure_player(3)
	p.pos = Vector3(1.5, 2.5, -3.5)
	p.vel = Vector3(0.1, 0.2, 0.3)
	p.yaw = 0.9
	p.pitch = -0.2
	p.stamina = 7.5
	p.jump_cd = 0.05
	p.on_floor = true
	p.crouching = true
	var data := Protocol.encode_snapshot(77, 33, 3, s.players)
	var d = Protocol.decode_snapshot(data)
	check("protocollo snapshot: non nullo", d != null)
	if d == null:
		return
	check("protocollo snapshot: tick/ack", d["state"].tick == 77 and d["ack"] == 33)
	check("protocollo snapshot: your_id", d["your_id"] == 3)
	var rp = d["state"].players[3]
	check("protocollo snapshot: posizione", rp.pos.distance_to(Vector3(1.5, 2.5, -3.5)) < 0.0001)
	check("protocollo snapshot: velocita", rp.vel.distance_to(Vector3(0.1, 0.2, 0.3)) < 0.0001)
	check("protocollo snapshot: flag", rp.on_floor and rp.crouching)
	check("protocollo snapshot: stamina", absf(rp.stamina - 7.5) < 0.0001)
