extends RefCounted
## Stato di un giocatore. `pos` è il CENTRO dell'AABB, non i piedi.

var id: int = 0
var pos: Vector3 = Vector3.ZERO
var vel: Vector3 = Vector3.ZERO
var yaw: float = 0.0
var pitch: float = 0.0
var on_floor: bool = false
var stamina: float = 10.0
var crouching: bool = false
var jump_cd: float = 0.0


func copy_from(o) -> void:
	id = o.id
	pos = o.pos
	vel = o.vel
	yaw = o.yaw
	pitch = o.pitch
	on_floor = o.on_floor
	stamina = o.stamina
	crouching = o.crouching
	jump_cd = o.jump_cd
