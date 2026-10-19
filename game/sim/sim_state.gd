extends RefCounted
## Stato completo della simulazione. Nessun nodo Godot: è un oggetto puro.

const SimPlayer = preload("res://sim/sim_player.gd")

var tick: int = 0
var players: Dictionary = {}


func get_player(id: int):
	return players.get(id, null)


func ensure_player(id: int):
	if not players.has(id):
		var p = SimPlayer.new()
		p.id = id
		players[id] = p
	return players[id]


func clone():
	var s = get_script().new()
	s.tick = tick
	for k in players:
		var p = SimPlayer.new()
		p.copy_from(players[k])
		s.players[k] = p
	return s
