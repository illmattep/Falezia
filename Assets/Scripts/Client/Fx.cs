using UnityEngine;

/// <summary>
/// Materiali e texture senza dipendere dal pipeline: prova HDRP, poi URP, poi Built-in.
/// Serve a non avere materiali magenta quando si cambia pipeline nel progetto.
/// </summary>
public static class Fx
{
    private static Shader _lit;

    public static Shader Lit
    {
        get
        {
            if (_lit != null) return _lit;
            foreach (var name in new[] { "HDRP/Lit", "Universal Render Pipeline/Lit", "Standard" })
            {
                var s = Shader.Find(name);
                if (s != null) { _lit = s; break; }
            }
            if (_lit == null) _lit = Shader.Find("Sprites/Default");
            return _lit;
        }
    }

    public static Shader Unlit
    {
        get
        {
            // prima il nostro shader: blend scritto nel SubShader, nessuna keyword da
            // indovinare, nessuna variante da perdere nella build
            foreach (var name in new[] { "Falezia/FxUnlit", "HDRP/Unlit", "Universal Render Pipeline/Unlit", "Unlit/Color" })
            {
                var s = Shader.Find(name);
                if (s != null) return s;
            }
            return Lit;
        }
    }

    public static Material Make(Color color, float smoothness = 0.2f, Texture2D tex = null)
    {
        var m = new Material(Lit);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);

        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            // colore 1: il colore vero lo porta la texture
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        }
        return m;
    }

    /// <summary>Rumore tileable 256² per la scia dei proiettili (una volta sola, poi è cache).</summary>
    private static Texture2D _noise;
    public static Texture2D Noise
    {
        get
        {
            if (_noise != null) return _noise;

            const int N = 256;
            _noise = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N * 2f * Mathf.PI, v = y / (float)N * 2f * Mathf.PI;
                    float n = Fbm(Mathf.Cos(u) * 2f, Mathf.Sin(u) * 2f) * 0.5f
                            + Fbm(Mathf.Cos(v) * 3f, Mathf.Sin(v) * 3f) * 0.5f;
                    byte b = (byte)(Mathf.Clamp01(n) * 255);
                    px[y * N + x] = new Color32(b, b, b, 255);
                }
            }
            _noise.SetPixels32(px);
            _noise.Apply(true);
            return _noise;
        }
    }

    private static float Hash(float x, float y)
    {
        float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        return h - Mathf.Floor(h);
    }

    private static float Fbm(float x, float y)
    {
        float sum = 0f, amp = 0.5f;
        for (int i = 0; i < 5; i++)
        {
            float xi = Mathf.Floor(x), yi = Mathf.Floor(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf), v = yf * yf * (3f - 2f * yf);
            float a = Hash(xi, yi), b = Hash(xi + 1f, yi);
            float c = Hash(xi, yi + 1f), d = Hash(xi + 1f, yi + 1f);
            sum += Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v) * amp;
            x *= 2.03f; y *= 2.03f; amp *= 0.5f;
        }
        return sum;
    }

    /// <summary>Materiale non illuminato e trasparente (tracce, fori dei proiettili).</summary>
    public static Material Transparent(Color color, Texture2D tex = null)
    {
        var m = new Material(Unlit);
        // Falezia/FxUnlit: _MainTex + _Color, sempre in blend
        if (tex != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.shader != null && m.shader.name == "Falezia/FxUnlit")
        {
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        if (tex != null && m.HasProperty("_UnlitColorMap")) m.SetTexture("_UnlitColorMap", tex);
        if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (tex != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        if (m.HasProperty("_UnlitColor")) m.SetColor("_UnlitColor", color);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);

        // URP Unlit nasce OPACO: senza questi campi la texture alpha viene ignorata
        // e i fori dei proiettili diventano rettangoli pieni.
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }
}
