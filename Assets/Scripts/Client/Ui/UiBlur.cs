using System.Collections;
using UnityEngine;

/// <summary>
/// IL VETRO SMERIGLIATO DIETRO I MENU.
///
/// Un menu è una schermata ferma: non serve sfocare la scena in tempo reale. Si cattura
/// **una volta** il frame (senza l'interfaccia), lo si passa due volte dallo shader
/// <c>Falezia/UiBlur</c> — orizzontale, poi verticale — e quella texture diventa il fondo
/// dei pannelli. Costo: un readback per apertura, zero per frame.
///
/// Perché non una seconda camera: in URP una camera creata a runtime con targetTexture
/// non disegnava (schermo nero), e chiamare Camera.Render() da OnGUI fa esplodere la
/// pipeline. La cattura del frame evita entrambi i problemi.
///
/// La UI resta nascosta per un frame mentre si cattura: così il fondo non contiene
/// il menu stesso. Con lo shader assente resta il frame non sfocato, scurito dal velo.
/// </summary>
public sealed class UiBlur : MonoBehaviour
{
    private static UiBlur _instance;

    public static UiBlur I
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("UiBlur");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UiBlur>();
            return _instance;
        }
    }

    private Material _mat;
    private RenderTexture _a, _b;
    private Texture2D _shot;
    private bool _dirty = true, _busy, _suppress, _failed;

    /// <summary>La texture di fondo, o null se non è ancora pronta (in quel caso si vede la scena).</summary>
    public Texture Result => _suppress || _failed ? null : _b;

    /// <summary>
    /// True nel frame in cui si sta catturando: chi disegna deve saltare quel frame, così
    /// la foto non contiene il menu stesso.
    /// </summary>
    public bool Suppress => _suppress;

    private void Awake()
    {
        _instance = this;
        var shader = Shader.Find("Falezia/UiBlur");
        if (shader != null && shader.isSupported)
            _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        Release(_a);
        Release(_b);
        if (_shot) Destroy(_shot);
        if (_mat) Destroy(_mat);
    }

    private static void Release(RenderTexture rt)
    {
        if (rt == null) return;
        rt.Release();
        Destroy(rt);
    }

    /// <summary>Chiede di rifare il fondo: quando si apre un pannello, o quando la scena cambia.</summary>
    public void Invalidate()
    {
        _dirty = true;
        _failed = false;
    }

    /// <summary>Da chiamare ogni frame mentre un pannello è aperto.</summary>
    public void Refresh(bool force = false)
    {
        if (force) _dirty = true;
        if (!_dirty || _busy || _failed) return;

        _dirty = false;
        _busy = true;
        _suppress = true;          // questo frame niente UI: si cattura solo la scena
        StartCoroutine(Capture());
    }

    private IEnumerator Capture()
    {
        yield return new WaitForEndOfFrame();

        try
        {
            if (_shot != null) { Destroy(_shot); _shot = null; }
            _shot = ScreenCapture.CaptureScreenshotAsTexture();

            if (_shot != null) Blur(_shot);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"UiBlur: cattura non riuscita ({e.Message})");
            _failed = true;
        }

        _suppress = false;
        _busy = false;
    }

    private void Blur(Texture source)
    {
        int w = Mathf.Max(64, Screen.width / 4);
        int h = Mathf.Max(36, Screen.height / 4);

        if (_a == null || _a.width != w || _a.height != h)
        {
            Release(_a);
            Release(_b);
            _a = NewRt(w, h, "uiBlurA");
            _b = NewRt(w, h, "uiBlurB");
        }

        if (_mat == null)
        {
            // niente shader: almeno il sottocampionamento, che sfoca già di suo
            Graphics.Blit(source, _b);
            return;
        }

        var previous = RenderTexture.active;
        try
        {
            // due giri di (orizzontale + verticale): un vetro smerigliato, non solo un po' morbido
            _mat.SetFloat("_Radius", 2.6f);

            _mat.SetVector("_Direction", new Vector4(1f, 0f, 0f, 0f));
            Graphics.Blit(source, _a, _mat);

            _mat.SetVector("_Direction", new Vector4(0f, 1f, 0f, 0f));
            Graphics.Blit(_a, _b, _mat);

            _mat.SetVector("_Direction", new Vector4(1f, 0f, 0f, 0f));
            Graphics.Blit(_b, _a, _mat);

            _mat.SetVector("_Direction", new Vector4(0f, 1f, 0f, 0f));
            Graphics.Blit(_a, _b, _mat);
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    private static RenderTexture NewRt(int w, int h, string name) => new(w, h, 0)
    {
        name = name,
        filterMode = FilterMode.Bilinear,
        wrapMode = TextureWrapMode.Clamp
    };
}
