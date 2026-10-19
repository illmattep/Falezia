extends RefCounted
## Mondo statico: solo AABB. Niente mesh, niente fisica del motore.
## Il client e il server costruiscono lo STESSO mondo dalla stessa funzione.

var boxes: Array = []


func add_box(b: AABB) -> void:
	boxes.append(b)


static func make_test_world():
	var w = load("res://sim/sim_world.gd").new()

	# Pavimento
	w.add_box(AABB(Vector3(-60, -1, -60), Vector3(120, 1, 120)))
	# Muro lungo sull'asse -X (per il test di collisione)
	w.add_box(AABB(Vector3(-10, 0, -6), Vector3(6, 3, 12)))
	# Cassa bassa 1 m (ci si sale sopra)
	w.add_box(AABB(Vector3(2, 0, 5), Vector3(2.5, 1, 2.5)))
	# Torre 5 m
	w.add_box(AABB(Vector3(8, 0, -4), Vector3(2, 5, 2)))
	# Gradino 40 cm
	w.add_box(AABB(Vector3(-4, 0, 8), Vector3(6, 0.4, 2)))
	# Muretto di riparo 1,1 m
	w.add_box(AABB(Vector3(0, 0, -10), Vector3(8, 1.1, 0.4)))

	return w
