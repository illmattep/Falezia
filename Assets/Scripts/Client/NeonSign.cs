using UnityEngine;

/// <summary>
/// Insegna al neon che sfarfalla. Le lettere sono renderer separati e condividono un
/// materiale; l'intensità si pilota per renderer con un MaterialPropertyBlock, quindi
/// solo le lettere scelte sfarfallano, senza istanziare materiali.
///
/// La luce dell'insegna segue le lettere che sfarfallano: quando una si spegne per un
/// istante, la proiezione sulla facciata cala con lei. In editor (non Play) resta accesa.
/// </summary>
public class NeonSign : MonoBehaviour
{
    public Renderer[] letters;
    public Light glow;
    public int[] flickerLetters = { 1, 5 };
    [Range(0f, 1f)] public float glowFlicker = 0.35f;
    public float minWait = 3f;
    public float maxWait = 9f;

    private MaterialPropertyBlock _mpb;
    private float[] _next;
    private float[] _end;
    private Color[] _emission;
    private float _glowBase;

    private void Start()
    {
        _mpb = new MaterialPropertyBlock();
        _glowBase = glow != null ? glow.intensity : 0f;
        int n = letters != null ? letters.Length : 0;
        _emission = new Color[n];
        for (int i = 0; i < n; i++)
        {
            var m = letters[i] != null ? letters[i].sharedMaterial : null;
            if (m != null && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                _emission[i] = m.GetColor("_EmissionColor");
            }
        }
        _next = new float[n];
        _end = new float[_next.Length];
        for (int i = 0; i < _next.Length; i++) _next[i] = Time.time + Random.Range(minWait, maxWait);
    }

    private void Update()
    {
        if (letters == null || _mpb == null) return;

        float dim = 1f;
        for (int k = 0; k < flickerLetters.Length; k++)
        {
            int i = flickerLetters[k];
            if (i < 0 || i >= letters.Length || letters[i] == null) continue;

            float t = Time.time;
            if (t >= _next[i] && t >= _end[i])
            {
                _end[i] = t + Random.Range(0.08f, 0.45f);
                _next[i] = t + Random.Range(minWait, maxWait);
            }

            bool off = t < _end[i] && Mathf.Repeat(t * 22f + i * 1.7f, 1f) < 0.35f;
            Set(i, off ? 0.03f : 1f);
            if (off) dim = Mathf.Min(dim, glowFlicker);
        }

        if (glow != null) glow.intensity = _glowBase * Mathf.Lerp(glow.intensity / Mathf.Max(0.001f, _glowBase), dim, 0.35f);
    }

    private void Set(int i, float intensity)
    {
        letters[i].GetPropertyBlock(_mpb);
        if (_emission != null && _emission[i].maxColorComponent > 0.001f)
            _mpb.SetColor("_EmissionColor", _emission[i] * intensity);
        else
            _mpb.SetFloat("_Intensity", intensity);
        letters[i].SetPropertyBlock(_mpb);
    }
}
