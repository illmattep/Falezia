"""
Porta in Python di game/sim/Sim.cs (solo il movimento), per provare la mappa
camminandoci dentro con le stesse regole. NON è la fonte di verità: lo è Sim.cs.
Se le due divergono, ha ragione Sim.cs e questo file va aggiornato.
"""
import math

DT = 1 / 64
WALK, SPRINT, CROUCH = 4.2, 6.5, 2.2
G, JUMP = 18.0, 6.0
GROUND_ACCEL, FRICTION = 8.0, 8.0
RADIUS, HEIGHT = 0.30, 1.80
STEP = 0.35


class P:
    def __init__(self, feet):
        self.pos = [feet[0], feet[1] + HEIGHT / 2, feet[2]]
        self.vel = [0.0, 0.0, 0.0]
        self.floor = False


EPS = 1e-4   # come Simulation.ContactSkin


def overlaps(pos, half, b):
    return not (pos[0] + half[0] <= b.x0 + EPS or pos[0] - half[0] >= b.x1 - EPS or
                pos[1] + half[1] <= b.y0 + EPS or pos[1] - half[1] >= b.y1 - EPS or
                pos[2] + half[2] <= b.z0 + EPS or pos[2] - half[2] >= b.z1 - EPS)


_GRIDS = {}


def _near(boxes, x0, x1, z0, z1):
    """Come WorldState.Query: le scatole vicine, nell'ordine della lista."""
    key = id(boxes)
    g = _GRIDS.get(key)
    if g is None or g[0] is not boxes:
        cells = {}
        for i, b in enumerate(boxes):
            for cx in range(math.floor(b.x0 / 4), math.floor(b.x1 / 4) + 1):
                for cz in range(math.floor(b.z0 / 4), math.floor(b.z1 / 4) + 1):
                    cells.setdefault((cx, cz), []).append(i)
        g = (boxes, cells)
        _GRIDS[key] = g
    out = set()
    for cx in range(math.floor(x0 / 4), math.floor(x1 / 4) + 1):
        for cz in range(math.floor(z0 / 4), math.floor(z1 / 4) + 1):
            out.update(g[1].get((cx, cz), ()))
    return [boxes[i] for i in sorted(out)]


def move(pos, half, d, boxes):
    r = {"pos": list(pos), "floor": False, "ceil": False, "wx": False, "wz": False}
    for axis in (0, 2, 1):
        if abs(d[axis]) < 1e-9:
            continue
        r["pos"][axis] += d[axis]
        sx = abs(d[0]) if axis == 0 else 0.0
        sz = abs(d[2]) if axis == 2 else 0.0
        p = r["pos"]
        for b in _near(boxes, p[0] - half[0] - sx - 0.05, p[0] + half[0] + sx + 0.05,
                       p[2] - half[2] - sz - 0.05, p[2] + half[2] + sz + 0.05):
            if not overlaps(r["pos"], half, b):
                continue
            if axis == 0:
                r["pos"][0] = b.x0 - half[0] if d[0] > 0 else b.x1 + half[0]
                r["wx"] = True
            elif axis == 2:
                r["pos"][2] = b.z0 - half[2] if d[2] > 0 else b.z1 + half[2]
                r["wz"] = True
            else:
                if d[1] < 0:
                    r["pos"][1] = b.y1 + half[1]
                    r["floor"] = True
                else:
                    r["pos"][1] = b.y0 - half[1]
                    r["ceil"] = True
    return r


def step(p, wish_x, wish_z, boxes, sprint=False):
    """wish_x/wish_z: direzione desiderata in spazio mondo (normalizzata o zero)."""
    half = (RADIUS, HEIGHT / 2, RADIUS)
    speed = SPRINT if sprint else WALK
    if p.floor:
        sp = math.hypot(p.vel[0], p.vel[2])
        if sp < 0.01:
            p.vel[0] = p.vel[2] = 0.0
        else:
            s = max(sp - sp * FRICTION * DT, 0) / sp
            p.vel[0] *= s
            p.vel[2] *= s
        if wish_x or wish_z:
            cur = p.vel[0] * wish_x + p.vel[2] * wish_z
            add = speed - cur
            if add > 0:
                acc = min(GROUND_ACCEL * speed * DT, add)
                p.vel[0] += wish_x * acc
                p.vel[2] += wish_z * acc
        p.vel[1] = -2.0
    else:
        p.vel[1] -= G * DT

    was_floor = p.floor
    jumped = p.vel[1] > 0
    d = [v * DT for v in p.vel]
    r = move(p.pos, half, d, boxes)
    if was_floor and not jumped and (r["wx"] or r["wz"]):
        up = move(p.pos, half, [0, STEP, 0], boxes)
        raised = up["pos"][1] - p.pos[1]
        if raised >= 0.01:
            across = move(up["pos"], half, [d[0], 0, d[2]], boxes)
            down = move(across["pos"], half, [0, -(raised + 0.01), 0], boxes)
            if down["floor"]:
                h2 = lambda a: (a[0] - p.pos[0]) ** 2 + (a[2] - p.pos[2]) ** 2
                if h2(down["pos"]) > h2(r["pos"]) + 1e-8:
                    down["wx"], down["wz"] = across["wx"], across["wz"]
                    r = down
    if was_floor and not jumped and not r["floor"]:
        probe = move(r["pos"], half, [0, -STEP, 0], boxes)
        if probe["floor"]:
            r["pos"] = probe["pos"]
            r["floor"] = True
    p.pos = r["pos"]
    p.floor = r["floor"]
    if r["floor"] and p.vel[1] < 0:
        p.vel[1] = 0
    if r["wx"]:
        p.vel[0] = 0
    if r["wz"]:
        p.vel[2] = 0


def walk(boxes, feet, waypoints, max_s=120.0, sprint=False):
    """Cammina da `feet` attraverso i waypoint (x, z). Restituisce (arrivato, tempo, p, traccia)."""
    near = boxes
    p = P(feet)
    t = 0.0
    trace = []
    for _ in range(64):            # atterra
        step(p, 0, 0, near)
    for wx, wz in waypoints:
        while True:
            dx, dz = wx - p.pos[0], wz - p.pos[2]
            dist = math.hypot(dx, dz)
            if dist < 0.4:
                break
            step(p, dx / dist, dz / dist, near, sprint)
            t += DT
            trace.append((t, tuple(p.pos), p.floor))
            if t > max_s:
                return False, t, p, trace
    return True, t, p, trace
