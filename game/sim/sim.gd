extends RefCounted
## LA SIMULAZIONE. Funzioni pure, passo fisso esplicito.
## Nessun riferimento a Godot oltre a Vector3/AABB: si può portare in C# così com'è.

const C = preload("res://sim/constants.gd")


static func spawn(state, id: int, feet_pos: Vector3) -> void:
	var p = state.ensure_player(id)
	p.pos = feet_pos + Vector3(0.0, C.HEIGHT * 0.5, 0.0)
	p.vel = Vector3.ZERO
	p.yaw = 0.0
	p.pitch = 0.0
	p.on_floor = false
	p.stamina = C.STAMINA_MAX
	p.crouching = false
	p.jump_cd = 0.0


static func step(state, inputs: Dictionary, world, dt: float = C.DT) -> void:
	state.tick += 1
	for id in inputs:
		apply_input(state, id, inputs[id], world, dt)


## Avanza UN giocatore. Non tocca il tick: serve anche al replay della riconciliazione.
static func apply_input(state, id: int, inp, world, dt: float = C.DT) -> void:
	var p = state.players.get(id, null)
	if p == null:
		return
	_step_player(p, inp, world, dt)


static func _step_player(p, inp, world, dt: float) -> void:
	p.yaw = inp.yaw
	p.pitch = clampf(inp.pitch, -1.55, 1.55)
	p.jump_cd = maxf(0.0, p.jump_cd - dt)

	var half_stand := Vector3(C.RADIUS, C.HEIGHT * 0.5, C.RADIUS)
	var half_crouch := Vector3(C.RADIUS, C.CROUCH_HEIGHT * 0.5, C.RADIUS)

	# --- accovacciato: si abbassa subito, si rialza solo se c'è spazio
	var want_crouch: bool = inp.crouch and p.on_floor
	if p.crouching and not want_crouch:
		var raised: Vector3 = p.pos + Vector3(0.0, (C.HEIGHT - C.CROUCH_HEIGHT) * 0.5, 0.0)
		if not _overlaps(raised, half_stand, world):
			p.pos = raised
			p.crouching = false
		# altrimenti resta accovacciato
	else:
		if want_crouch and not p.crouching:
			p.pos.y -= (C.HEIGHT - C.CROUCH_HEIGHT) * 0.5
		p.crouching = want_crouch

	var half = half_crouch if p.crouching else half_stand

	# --- velocità massima
	var moving: bool = inp.move.length() > 0.01
	var max_speed: float = C.WALK_SPEED
	var sprinting := false
	if p.crouching:
		max_speed = C.CROUCH_SPEED
	elif inp.ads:
		max_speed = C.WALK_SPEED * C.ADS_MULT
	elif inp.sprint and moving and p.stamina > 0.0:
		max_speed = C.SPRINT_SPEED
		sprinting = true

	# --- stamina
	if sprinting and p.on_floor:
		p.stamina = maxf(0.0, p.stamina - dt)
	else:
		p.stamina = minf(C.STAMINA_MAX, p.stamina + (C.STAMINA_MAX / C.STAMINA_RECOVERY) * dt)

	# --- direzione desiderata in spazio mondo (-Z è avanti)
	var wish := Vector3(inp.move.x, 0.0, -inp.move.y)
	if wish.length() > 0.0:
		wish = wish.normalized().rotated(Vector3.UP, p.yaw)
	var wish_speed: float = max_speed * clampf(inp.move.length(), 0.0, 1.0)

	# --- attrito e accelerazione
	if p.on_floor:
		p.vel = _friction(p.vel, dt)
		p.vel = _accelerate(p.vel, wish, wish_speed, C.GROUND_ACCEL, dt)
	else:
		# controllo aereo limitato: niente bhop, niente air-strafe
		p.vel = _accelerate(p.vel, wish, minf(wish_speed, C.MAX_AIR_WISHSPEED), C.AIR_ACCEL, dt)

	# --- salto
	if inp.jump and p.on_floor and p.jump_cd <= 0.0:
		p.vel.y = C.JUMP_VELOCITY
		p.on_floor = false
		p.jump_cd = C.JUMP_COOLDOWN

	# --- gravità / aggancio al suolo
	if p.on_floor:
		p.vel.y = -2.0   # piccola velocità verso il basso: serve a RESTARE a terra
	else:
		p.vel.y -= C.GRAVITY * dt

	# --- movimento e collisioni (assi separati)
	var res := _move(p.pos, half, p.vel * dt, world)
	p.pos = res["pos"]
	p.on_floor = res["floor"]
	if res["floor"] and p.vel.y < 0.0:
		p.vel.y = 0.0
	if res["ceil"] and p.vel.y > 0.0:
		p.vel.y = 0.0
	if res["wall_x"]:
		p.vel.x = 0.0
	if res["wall_z"]:
		p.vel.z = 0.0

	# --- caduto fuori dal mondo
	if p.pos.y < C.WORLD_FLOOR_Y:
		p.pos = Vector3(0.0, C.HEIGHT * 0.5 + 0.1, 0.0)
		p.vel = Vector3.ZERO


# ---------------------------------------------------------------- movimento

static func _friction(vel: Vector3, dt: float) -> Vector3:
	var speed: float = sqrt(vel.x * vel.x + vel.z * vel.z)
	if speed < 0.01:
		return Vector3(0.0, vel.y, 0.0)
	var drop: float = speed * C.FRICTION * dt
	var new_speed: float = maxf(speed - drop, 0.0)
	var s: float = new_speed / speed
	return Vector3(vel.x * s, vel.y, vel.z * s)


static func _accelerate(vel: Vector3, wish_dir: Vector3, wish_speed: float, accel: float, dt: float) -> Vector3:
	if wish_dir.length() < 0.001 or wish_speed <= 0.0:
		return vel
	var current: float = vel.x * wish_dir.x + vel.z * wish_dir.z
	var add: float = wish_speed - current
	if add <= 0.0:
		return vel
	var accelspeed: float = minf(accel * wish_speed * dt, add)
	return Vector3(vel.x + wish_dir.x * accelspeed, vel.y, vel.z + wish_dir.z * accelspeed)


static func _move(pos: Vector3, half: Vector3, delta: Vector3, world) -> Dictionary:
	var r := {"pos": pos, "floor": false, "ceil": false, "wall_x": false, "wall_z": false}
	var p := pos
	p.x += delta.x
	p = _resolve(p, half, world, 0, r)
	p.z += delta.z
	p = _resolve(p, half, world, 2, r)
	p.y += delta.y
	p = _resolve(p, half, world, 1, r)
	r["pos"] = p
	return r


static func _resolve(pos: Vector3, half: Vector3, world, axis: int, r: Dictionary) -> Vector3:
	for b in world.boxes:
		if not _overlaps(pos, half, b):
			continue
		var bc: Vector3 = b.get_center()
		if axis == 0:
			if pos.x < bc.x:
				pos.x = b.position.x - half.x
			else:
				pos.x = b.position.x + b.size.x + half.x
			r["wall_x"] = true
		elif axis == 2:
			if pos.z < bc.z:
				pos.z = b.position.z - half.z
			else:
				pos.z = b.position.z + b.size.z + half.z
			r["wall_z"] = true
		else:
			if pos.y < bc.y:
				pos.y = b.position.y - half.y
				r["floor"] = true
			else:
				pos.y = b.position.y + b.size.y + half.y
				r["ceil"] = true
	return pos


static func _overlaps(pos: Vector3, half: Vector3, b: AABB) -> bool:
	var pmin: Vector3 = pos - half
	var pmax: Vector3 = pos + half
	return not (pmax.x <= b.position.x or pmin.x >= b.position.x + b.size.x
		or pmax.y <= b.position.y or pmin.y >= b.position.y + b.size.y
		or pmax.z <= b.position.z or pmin.z >= b.position.z + b.size.z)
