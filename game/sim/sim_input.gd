extends RefCounted
## Input di un tick. È l'unica cosa che il client ha il diritto di inviare.

var tick: int = 0
var move: Vector2 = Vector2.ZERO   # x = strafe, y = avanti
var yaw: float = 0.0
var pitch: float = 0.0
var jump: bool = false
var sprint: bool = false
var crouch: bool = false
var ads: bool = false


func clone_into(dst) -> void:
	dst.tick = tick
	dst.move = move
	dst.yaw = yaw
	dst.pitch = pitch
	dst.jump = jump
	dst.sprint = sprint
	dst.crouch = crouch
	dst.ads = ads


func duplicate_input():
	var i = get_script().new()
	clone_into(i)
	return i
