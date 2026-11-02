using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BOSSOLI. Ogni colpo espelle un bossolo dal finestrino: vola a destra, cade, rimbalza
/// sul pavimento e sparisce dopo qualche secondo.
///
/// Niente fisica del motore: quattro integrali a mano su una pool fissa (come nel client
/// Godot), così non c'è una sola allocazione per colpo e non si dipende da Rigidbody.
/// Il pavimento si trova con <see cref="Falezia.Sim.Trace"/>: stessa geometria della
/// simulazione, quindi il bossolo non attraversa i volumi.
/// </summary>
public class ShellCasings : MonoBehaviour
{
    private const int Pool = 20;
    private const float Life = 4.5f;
    private const float Gravity = 9.81f;

    private sealed class Shell
    {
        public Transform T;
        public MeshRenderer R;
        public Material Mat;
        public Vector3 Pos, Vel, Spin;
        public Quaternion Rot;
        public float Age;
        public bool Alive;
        public float FloorY;
    }

    private readonly List<Shell> _shells = new();
    private Falezia.Sim.WorldState _world;
    private Mesh _mesh;

    public void Init(Falezia.Sim.WorldState world)
    {
        _world = world;
        _mesh = CasingMesh();

        var mat = Fx.Make(new Color(0.72f, 0.55f, 0.24f), 0.55f);   // ottone

        var root = new GameObject("Bossoli").transform;
        root.SetParent(null, false);

        for (int i = 0; i < Pool; i++)
        {
            var go = new GameObject("Bossolo");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            _shells.Add(new Shell { T = go.transform, R = mr, Mat = mat });
        }
    }

    /// <summary>Espelle un bossolo dall'arma (posizione occhio + destra della camera).</summary>
    public void Spawn(Vector3 eye, Vector3 right, float ads)
    {
        Shell s = null;
        foreach (var k in _shells) if (!k.Alive) { s = k; break; }
        if (s == null) s = _shells[0];

        s.Alive = true;
        s.Age = 0f;
        s.Pos = eye + right * (0.14f - ads * 0.04f) + Vector3.up * -0.02f;
        s.Vel = right * Random.Range(2.2f, 3.4f) + Vector3.up * Random.Range(1.4f, 2.2f)
                + Vector3.forward * Random.Range(-0.2f, 0.4f);
        s.Spin = new Vector3(Random.Range(-900f, 900f), Random.Range(-900f, 900f), Random.Range(-900f, 900f));
        s.Rot = Random.rotation;
        s.FloorY = 0.02f;

        var origin = new System.Numerics.Vector3(s.Pos.x, s.Pos.y, s.Pos.z);
        var hit = Falezia.Sim.Trace.Ray(_world, origin, new System.Numerics.Vector3(0f, -1f, 0f), 6f);
        if (hit.Hit) s.FloorY = hit.Point.Y + 0.012f;

        s.T.position = s.Pos;
        s.T.rotation = s.Rot;
        s.T.gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        foreach (var s in _shells)
        {
            if (!s.Alive) continue;
            s.Age += dt;

            s.Vel.y -= Gravity * dt;
            s.Pos += s.Vel * dt;

            if (s.Pos.y < s.FloorY)
            {
                s.Pos.y = s.FloorY;
                s.Vel.y = -s.Vel.y * 0.32f;                 // rimbalzo smorzato
                s.Vel.x *= 0.55f;
                s.Vel.z *= 0.55f;
                s.Spin *= 0.4f;
            }

            s.Rot = Quaternion.Euler(s.Spin * dt) * s.Rot;
            s.T.position = s.Pos;
            s.T.rotation = s.Rot;

            if (s.Age > Life)
            {
                s.Alive = false;
                s.T.gameObject.SetActive(false);
            }
        }
    }

    private static Mesh CasingMesh()
    {
        var mesh = new Mesh { name = "Bossolo" };
        // parallelepipedo 9×19 mm (in metri, scala reale)
        var v = new Vector3[8];
        for (int i = 0; i < 8; i++)
            v[i] = new Vector3((i & 1) == 0 ? -0.0045f : 0.0045f,
                               (i & 2) == 0 ? -0.0095f : 0.0095f,
                               (i & 4) == 0 ? -0.0045f : 0.0045f);

        int[] tris =
        {
            0,2,3, 0,3,1,  4,5,7, 4,7,6,
            0,1,5, 0,5,4,  2,6,7, 2,7,3,
            0,4,6, 0,6,2,  1,3,7, 1,7,5,
        };

        mesh.vertices = v;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
