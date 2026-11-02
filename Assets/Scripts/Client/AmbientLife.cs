using UnityEngine;

/// <summary>
/// IL MONDO CHE SI MUOVE.
///
/// Poche cose, tutte economiche, che tolgono alla mappa l'aria di plastica ferma:
///   · **gabbiani** che girano in tondo sopra la città, con le ali che battono a intervalli;
///   · **polvere** che galleggia nella luce (qualche decina di particelle, nessuna allocazione).
///
/// Non è decorazione fine a sé: il movimento lento in alto dà la scala alla mappa e fa
/// sembrare vivo anche ciò che non si guarda.
/// </summary>
public sealed class AmbientLife : MonoBehaviour
{
    private const int Gulls = 9;

    private Transform[] _gulls;
    private float[] _phase, _radius, _height, _speed, _dir;
    private Mesh _gullMesh;
    private Material _gullMat;
    private ParticleSystem _dust;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("AmbientLife");
        DontDestroyOnLoad(go);
        go.AddComponent<AmbientLife>();
    }

    private void Start()
    {
        BuildGulls();
        BuildDust();
    }

    private void BuildDustSafe() { }

    private void BuildGulls()
    {
        _gullMesh = GullMesh();

        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) { Debug.Log("AmbientLife: shader unlit assente, niente gabbiani"); return; }

        _gullMat = new Material(unlit)
        {
            name = "Gabbiani",
            color = new Color(0.96f, 0.96f, 0.94f)
        };
        _gullMat.SetFloat("_Cull", 0f);

        _gulls = new Transform[Gulls];
        _phase = new float[Gulls];
        _radius = new float[Gulls];
        _height = new float[Gulls];
        _speed = new float[Gulls];
        _dir = new float[Gulls];

        var parent = new GameObject("Gabbiani").transform;
        parent.SetParent(transform, false);

        for (int i = 0; i < Gulls; i++)
        {
            var go = new GameObject($"gabbiano {i}");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = _gullMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _gullMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _gulls[i] = go.transform;
            _phase[i] = Random.Range(0f, 6.28f);
            _radius[i] = Random.Range(28f, 62f);
            _height[i] = Random.Range(38f, 62f);
            _speed[i] = Random.Range(0.10f, 0.22f) * (Random.value > 0.5f ? 1f : -1f);
            _dir[i] = Random.Range(0f, 6.28f);
            _gulls[i].localScale = Vector3.one * Random.Range(0.8f, 1.3f);
        }
    }

    /// <summary>Un gabbiano a due ali: quattro triangoli, niente texture.</summary>
    private static Mesh GullMesh()
    {
        var m = new Mesh { name = "gabbiano" };
        m.vertices = new[]
        {
            new Vector3(0f, 0f, 0.18f),     // 0 punta
            new Vector3(-0.55f, 0.10f, -0.16f), // 1 ala sinistra
            new Vector3(-0.10f, 0f, -0.10f),    // 2 corpo dietro
            new Vector3(0.55f, 0.10f, -0.16f),  // 3 ala destra
            new Vector3(0.10f, 0f, -0.10f),     // 4 corpo
        };
        m.triangles = new[] { 0, 1, 2, 0, 2, 4, 0, 4, 3 };
        m.RecalculateNormals();
        return m;
    }

    private void BuildDust()
    {
        var go = new GameObject("Polvere");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0f, 6f, 0f);

        var particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) { Destroy(go); return; }

        _dust = go.AddComponent<ParticleSystem>();
        var main = _dust.main;
        main.startLifetime = 26f;
        main.startSpeed = 0.22f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.055f);
        main.startColor = new Color(1f, 0.97f, 0.9f, 0.16f);
        main.maxParticles = 220;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _dust.emission;
        emission.rateOverTime = 6f;

        var shape = _dust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(170f, 12f, 190f);

        var vel = _dust.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.03f, 0.05f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.10f, 0.10f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(particleShader)
        {
            name = "Polvere",
            color = new Color(1f, 0.97f, 0.9f, 0.2f)
        };
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private float _fpsTimer, _frames;

    private void Update()
    {
        // misura di controllo: fps medi ogni 5 s, così il costo si vede nei log
        _frames++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 5f)
        {
            Debug.Log($"Perf: {_frames / _fpsTimer:F0} fps · {_fpsTimer / _frames * 1000f:F1} ms/frame");
            _frames = 0f;
            _fpsTimer = 0f;
        }

        if (_gulls == null) return;

        float t = Time.time;
        for (int i = 0; i < _gulls.Length; i++)
        {
            float a = _phase[i] + t * _speed[i];
            var p = new Vector3(
                Mathf.Cos(a) * _radius[i],
                _height[i] + Mathf.Sin(t * 0.35f + _phase[i]) * 2.2f,
                Mathf.Sin(a) * _radius[i] * 0.7f);

            var g = _gulls[i];
            g.position = p;

            var next = new Vector3(
                Mathf.Cos(a + 0.05f * Mathf.Sign(_speed[i])) * _radius[i],
                p.y,
                Mathf.Sin(a + 0.05f * Mathf.Sign(_speed[i])) * _radius[i] * 0.7f);

            g.rotation = Quaternion.LookRotation((next - p).normalized, Vector3.up);

            // colpo d'ala: due battiti, poi planata
            float flap = Mathf.Max(0f, Mathf.Sin(t * 5.2f + _phase[i] * 3f));
            g.localScale = new Vector3(g.localScale.x, 0.75f + flap * 0.5f, 1f);
        }
    }
}
