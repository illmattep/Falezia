using System.Collections.Generic;
using Falezia.Sim;
using UnityEngine;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// Gli effetti dello sparo: traccia, foro sul muro, lampo.
/// Il punto d'impatto NON si inventa qui: lo decide <see cref="Combat.Shoot"/> nella
/// simulazione, la stessa funzione che userà il server. Qui si disegna soltanto.
/// </summary>
public class CombatFx : MonoBehaviour
{
    private const int BeamCount = 24;
    private const float BeamLife = 0.055f;

    private sealed class Beam
    {
        public LineRenderer Line;
        public float Left;
    }

    /// <summary>`-faleziaShots` attiva anche il log dei fori: serve a vedere dove finiscono.</summary>
    private static bool DebugShots => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-faleziaShots") >= 0;

    private readonly List<Beam> _beams = new();
    private BulletMarks _marks;
    private Light _flash;

    public void Init()
    {
        var tracer = Shader.Find("Falezia/Tracer");
        Material mat;
        if (tracer != null)
        {
            mat = new Material(tracer) { name = "Scia" };
            mat.SetTexture("_NoiseTex", Fx.Noise);
            mat.SetColor("_CoreColor", new Color(1f, 0.92f, 0.70f, 1f));
            mat.SetColor("_SmokeColor", new Color(0.86f, 0.88f, 0.92f, 1f));
            mat.SetFloat("_Opacity", 0.6f);
            mat.SetFloat("_Speed", 3.2f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat = Fx.Transparent(new Color(1f, 0.86f, 0.55f, 0.85f));
        }
        for (int i = 0; i < BeamCount; i++)
        {
            var go = new GameObject("Scie");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.positionCount = 2;
            lr.startWidth = 0.0075f;
            lr.endWidth = 0.0025f;          // la scia si assottiglia verso l'impatto
            lr.numCapVertices = 0;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
            _beams.Add(new Beam { Line = lr });
        }

        _marks = gameObject.AddComponent<BulletMarks>();

        _flash = new GameObject("Lampo").AddComponent<Light>();
        _flash.transform.SetParent(transform, false);
        _flash.type = LightType.Point;
        _flash.range = 3.2f;
        _flash.intensity = 0f;
        _flash.color = new Color(1f, 0.86f, 0.62f);
        _flash.shadows = LightShadows.None;
    }

    /// <summary>Un colpo appena partito: stessa direzione per ogni palla, stesso seme.</summary>
    public void Shoot(PlayerState p, WorldState world, Vector3 eye, float yaw, float pitch)
    {
        var def = Arsenal.Get(p.Weapon.Slot);
        if (def.Melee) return;

        var origin = new Vec3(eye.x, eye.y, eye.z);

        for (int i = 0; i < def.Pellets; i++)
        {
            var dir = Arsenal.PelletDirection(yaw, pitch, p.Weapon.ShotSpread, p.Weapon.ShotSeed, i, def.Pellets);
            var shot = Combat.Shoot(world, null, origin, dir, def);
            var end = shot.Hit ? new Vector3(shot.Point.X, shot.Point.Y, shot.Point.Z) : eye + ToUnity(dir) * 250f;

            SpawnBeam(eye + ToUnity(dir) * 0.4f, end);

            // foro solo se la palla ha davvero colpito una superficie
            if (shot.Hit && shot.Target == -1)
            {
                _marks.Add(end, new Vector3(shot.Normal.X, shot.Normal.Y, shot.Normal.Z));
                if (DebugShots) Debug.Log($"[TIRO] foro a {end} normale {shot.Normal} muro {shot.Surface}");
            }
            else if (DebugShots) Debug.Log($"[TIRO] niente foro: hit={shot.Hit} target={shot.Target}");
        }

        _flash.transform.position = eye + Camera.main.transform.forward * 0.55f;
        _flash.intensity = def.Silenced ? 0.25f : 1.1f;
    }

    private static Vector3 ToUnity(Vec3 v) => new(v.X, v.Y, v.Z);

    private void SpawnBeam(Vector3 a, Vector3 b)
    {
        foreach (var beam in _beams)
        {
            if (beam.Left > 0f) continue;
            beam.Line.SetPosition(0, a);
            beam.Line.SetPosition(1, b);
            beam.Line.enabled = true;
            beam.Left = BeamLife;
            return;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        foreach (var beam in _beams)
        {
            if (beam.Left <= 0f) continue;
            beam.Left -= dt;
            if (beam.Left <= 0f) beam.Line.enabled = false;
        }
        if (_flash != null && _flash.intensity > 0f)
            _flash.intensity = Mathf.MoveTowards(_flash.intensity, 0f, dt * 120f);
    }
}
