extends RefCounted
## Tutti i numeri vengono dai documenti di design. Doc 05 = movimento.
## Questo file non dipende da Godot: solo costanti.

const TICK_RATE := 64
const DT := 1.0 / 64.0
const INPUT_BUFFER := 256

# --- movimento (doc 05)
const WALK_SPEED := 4.2
const SPRINT_SPEED := 6.5
const CROUCH_SPEED := 2.2
const ADS_MULT := 0.55

const STAMINA_MAX := 10.0
const STAMINA_RECOVERY := 8.0

const GRAVITY := 18.0
const JUMP_VELOCITY := 6.0
const JUMP_COOLDOWN := 0.15

const GROUND_ACCEL := 8.0
const AIR_ACCEL := 2.0
const FRICTION := 8.0
const MAX_AIR_WISHSPEED := 0.3

const RADIUS := 0.30
const HEIGHT := 1.80
const CROUCH_HEIGHT := 1.20

const CORRECTION_THRESHOLD := 0.01
const WORLD_FLOOR_Y := -20.0

# --- rete
const SERVER_PORT := 27015
const SERVER_HOST := "127.0.0.1"
const SNAPSHOT_EVERY := 2   # 32 Hz su un tick di 64 Hz
