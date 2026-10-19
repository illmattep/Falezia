extends RefCounted
## Protocollo binario. Canale di gioco = inaffidabile, non ordinato.
## L'input lo decide il client, lo stato lo decide il server. Sempre.

const SimInput = preload("res://sim/sim_input.gd")
const SimPlayer = preload("res://sim/sim_player.gd")
const SimState = preload("res://sim/sim_state.gd")

const MSG_INPUT := 1
const MSG_SNAPSHOT := 2

const FLAG_ON_FLOOR := 1
const FLAG_CROUCH := 2


static func encode_input(inp) -> PackedByteArray:
	var b := StreamPeerBuffer.new()
	b.put_u8(MSG_INPUT)
	b.put_u32(inp.tick)
	b.put_float(inp.move.x)
	b.put_float(inp.move.y)
	b.put_float(inp.yaw)
	b.put_float(inp.pitch)
	var flags: int = 0
	if inp.jump: flags |= 1
	if inp.sprint: flags |= 2
	if inp.crouch: flags |= 4
	if inp.ads: flags |= 8
	b.put_u8(flags)
	return b.data_array


static func decode_input(data: PackedByteArray):
	if data.size() < 22:
		return null
	var b := StreamPeerBuffer.new()
	b.data_array = data
	if b.get_u8() != MSG_INPUT:
		return null
	var i = SimInput.new()
	i.tick = b.get_u32()
	i.move = Vector2(b.get_float(), b.get_float())
	i.yaw = b.get_float()
	i.pitch = b.get_float()
	var f: int = b.get_u8()
	i.jump = (f & 1) != 0
	i.sprint = (f & 2) != 0
	i.crouch = (f & 4) != 0
	i.ads = (f & 8) != 0
	return i


## Snapshot per UN client: contiene l'ack dei SUOI input, così la riconciliazione è esatta.
static func encode_snapshot(tick: int, ack: int, your_id: int, players: Dictionary) -> PackedByteArray:
	var b := StreamPeerBuffer.new()
	b.put_u8(MSG_SNAPSHOT)
	b.put_u32(tick)
	b.put_u32(ack)
	b.put_u8(your_id)
	b.put_u8(players.size())
	for id in players:
		var p = players[id]
		b.put_u8(id)
		b.put_float(p.pos.x)
		b.put_float(p.pos.y)
		b.put_float(p.pos.z)
		b.put_float(p.vel.x)
		b.put_float(p.vel.y)
		b.put_float(p.vel.z)
		b.put_float(p.yaw)
		b.put_float(p.pitch)
		b.put_float(p.stamina)
		b.put_float(p.jump_cd)
		var flags: int = 0
		if p.on_floor: flags |= FLAG_ON_FLOOR
		if p.crouching: flags |= FLAG_CROUCH
		b.put_u8(flags)
	return b.data_array


static func decode_snapshot(data: PackedByteArray):
	if data.size() < 11:
		return null
	var b := StreamPeerBuffer.new()
	b.data_array = data
	if b.get_u8() != MSG_SNAPSHOT:
		return null
	var s = SimState.new()
	s.tick = b.get_u32()
	var ack: int = b.get_u32()
	var your_id: int = b.get_u8()
	var n: int = b.get_u8()
	for k in range(n):
		var id: int = b.get_u8()
		var p = SimPlayer.new()
		p.id = id
		p.pos = Vector3(b.get_float(), b.get_float(), b.get_float())
		p.vel = Vector3(b.get_float(), b.get_float(), b.get_float())
		p.yaw = b.get_float()
		p.pitch = b.get_float()
		p.stamina = b.get_float()
		p.jump_cd = b.get_float()
		var flags: int = b.get_u8()
		p.on_floor = (flags & FLAG_ON_FLOOR) != 0
		p.crouching = (flags & FLAG_CROUCH) != 0
		s.players[id] = p
	return {"state": s, "ack": ack, "your_id": your_id}
