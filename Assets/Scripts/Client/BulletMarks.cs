using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// I FORI DEI PROIETTILI.
///
/// Ogni palla che tocca un muro, un tetto o il pavimento lascia un foro **appoggiato
/// sulla superficie**: il quad ha la stessa normale della faccia colpita, quindi sta
/// disteso sul muro e non segue mai la camera. (Prima erano billboard: restavano
/// agganciati al mirino e sembravano incollati allo schermo.)
///
/// Il foro nasce 6 mm fuori dalla faccia per non litigare con lo z-buffer, ha una
/// rotazione casuale attorno alla normale, resta 25 secondi e svanisce in 3.
/// Pool fisso: nessuna allocazione per colpo.
///
/// **Grandi e leggibili**: 16–24 cm in tutto (cratere scuro, scheggiatura chiara,
/// alone di polvere e fumo), così si vede dove si è colpito anche a 20 m, su un
/// intonaco chiaro come su un container scuro.
/// </summary>
public class BulletMarks : MonoBehaviour
{
    private const int Pool = 160;
    private const float Life = 25f;
    private const float Fade = 3f;
    private const float Offset = 0.006f;

    private sealed class Mark
    {
        public Transform T;
        public Material Mat;
        public float Age;
        public bool Alive;
    }

    private readonly List<Mark> _marks = new();
    private Mesh _quad;
    private Texture2D[] _textures;
    private Transform _root;

    private void Awake()
    {
        // RADICE IN SPAZIO MONDO: se i fori fossero figli del giocatore seguirebbero
        // la camera e sembrerebbero appiccicati al mirino (era esattamente il bug).
        _root = new GameObject("Fori sui muri").transform;
        _root.SetParent(null, false);

        _quad = QuadMesh();
        _textures = new Texture2D[8];
        for (int i = 0; i < _textures.Length; i++) _textures[i] = MakeTexture(i * 19.7f + 3.1f);

        for (int i = 0; i < Pool; i++)
        {
            var go = new GameObject("Foro");
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = Fx.Transparent(Color.white, _textures[i % _textures.Length]);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.SetActive(false);
            _marks.Add(new Mark { T = go.transform, Mat = mat });
        }
    }

    /// <summary>Foro nel punto colpito, con la normale della faccia.</summary>
    public void Add(Vector3 point, Vector3 normal)
    {
        if (normal.sqrMagnitude < 1e-6f) normal = Vector3.up;
        normal.Normalize();

        Mark m = null;
        float oldest = -1f;
        foreach (var k in _marks)
        {
            if (!k.Alive) { m = k; break; }
            if (k.Age > oldest) { oldest = k.Age; m = k; }
        }

        m.Alive = true;
        m.Age = 0f;

        float size = Random.Range(0.16f, 0.24f);
        m.T.position = point + normal * Offset;
        // il quad ha la faccia utile su -Z: guardando -normale resta rivolto verso la stanza
        var up = Mathf.Abs(normal.y) > 0.95f ? Vector3.forward : Vector3.up;
        m.T.rotation = Quaternion.LookRotation(-normal, up) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        m.T.localScale = new Vector3(size, size, size);

        m.Mat.color = new Color(1f, 1f, 1f, 1f);
        m.T.gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        foreach (var m in _marks)
        {
            if (!m.Alive) continue;
            m.Age += dt;
            if (m.Age < Life) continue;

            float k = 1f - Mathf.Clamp01((m.Age - Life) / Fade);
            var c = m.Mat.color;
            m.Mat.color = new Color(c.r, c.g, c.b, k);
            if (k <= 0f)
            {
                m.Alive = false;
                m.T.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (_root != null) Destroy(_root.gameObject);
    }

    private static Mesh QuadMesh()
    {
        var mesh = new Mesh { name = "ForoQuad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 0.01f));
        return mesh;
    }

    /// <summary>
    /// Foro grande e leggibile, 160², generato in codice. Dal centro verso fuori:
    /// cratere nero, pareti del cratere, scheggiatura chiara frastagliata, crepe a raggiera,
    /// alone di polvere e fumo che sfuma. Il contrasto chiaro/scuro lo rende visibile su
    /// qualunque superficie.
    /// </summary>
    private static Texture2D MakeTexture(float seed)
    {
        const int N = 160;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f;
                float v = (y + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float ang = Mathf.Atan2(v, u);

                float wob = Fbm(Mathf.Cos(ang) * 2.6f + seed, Mathf.Sin(ang) * 2.6f + seed);
                float hole = 0.16f + 0.05f * wob;                       // cratere: ~1/3 del diametro
                float chip = 0.30f + 0.14f * Fbm(Mathf.Cos(ang) * 4f + seed * 2f, Mathf.Sin(ang) * 4f);

                float crater = 1f - Smooth(hole * 0.75f, hole, r);
                float wall = Smooth(hole * 0.7f, hole, r) * (1f - Smooth(hole, hole * 1.35f, r));
                float spall = Smooth(hole * 1.05f, hole * 1.35f, r) * (1f - Smooth(chip * 0.9f, chip, r));
                float spikes = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(ang * 7f + seed * 23f + wob * 4f)), 18f);
                float crack = spikes * Smooth(hole, hole * 1.2f, r) * (1f - Smooth(chip, chip * 2.3f, r));
                float soot = 1f - Smooth(chip * 0.9f, 0.95f, r);
                float grain = Noise(u * 22f + seed, v * 22f - seed);

                var col = new Color(0.30f, 0.29f, 0.27f);                                   // alone di polvere e fumo
                col = Color.Lerp(col, new Color(0.86f, 0.84f, 0.80f), spall * (0.65f + 0.35f * grain));   // scheggiatura chiara
                col = Color.Lerp(col, new Color(0.10f, 0.095f, 0.09f), wall);               // pareti del cratere
                col = Color.Lerp(col, new Color(0.008f, 0.008f, 0.008f), crater);           // fondo nero
                col = Color.Lerp(col, new Color(0.04f, 0.04f, 0.04f), crack * 0.85f);

                float a = Mathf.Max(crater, wall);
                a = Mathf.Max(a, spall * (0.82f + 0.18f * grain));
                a = Mathf.Max(a, crack * 0.8f);
                a = Mathf.Max(a, soot * 0.42f * (0.7f + 0.3f * grain));
                a *= 1f - Smooth(0.9f, 1.0f, r);
                px[y * N + x] = new Color(col.r, col.g, col.b, Mathf.Clamp01(a));
            }
        }
        tex.SetPixels(px);
        tex.Apply(true);
        return tex;
    }

    private static float Hash(float x, float y)
    {
        float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    private static float Noise(float x, float y)
    {
        float xi = Mathf.Floor(x), yi = Mathf.Floor(y);
        float xf = x - xi, yf = y - yi;
        float u = xf * xf * (3f - 2f * xf);
        float v = yf * yf * (3f - 2f * yf);
        float a = Hash(xi, yi), b = Hash(xi + 1f, yi);
        float c = Hash(xi, yi + 1f), d = Hash(xi + 1f, yi + 1f);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    private static float Fbm(float x, float y)
    {
        float sum = 0f, amp = 0.5f;
        for (int i = 0; i < 4; i++)
        {
            sum += Noise(x, y) * amp;
            x *= 2.03f; y *= 2.03f; amp *= 0.5f;
        }
        return sum;
    }

    private static float Smooth(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / Mathf.Max(1e-6f, b - a));
        return t * t * (3f - 2f * t);
    }
}
