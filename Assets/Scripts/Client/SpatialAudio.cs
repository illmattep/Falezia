using System.Collections.Generic;
using System.IO;
using Falezia.Sim;
using UnityEngine;
using Vec3 = System.Numerics.Vector3;

/// <summary>
/// L'AUDIO SPAZIALE DEL MONDO: da dove arriva un suono e fin dove si sente.
///
/// Regola: **la portata non si inventa nel client, viene dal design**. Ogni arma dichiara
/// `WeaponDef.NoiseMeters` (doc 11): la Hush si sente a 40 m, il coltello a 6, tutte le altre
/// "in tutta la mappa" — che qui vuol dire la diagonale della mappa (260 m), non l'infinito.
///
/// Quattro cose rendono un suono "lontano", e sono tutte qui:
///   · **curva di volume** — due curve condivise: `Far` per gli spari (a 60 m restano un rimbombo
///     al 25%, a 150 m al 10%: l'informazione resta, il frastuono no) e `Tight` per le silenziate,
///     che spariscono davvero al limite dichiarato;
///   · **filtro passa-basso** — più sei lontano, meno alte frequenze: 19 kHz da vicino, ~850 Hz
///     al limite della portata. È il "tonfo sordo" che senti dall'altra parte della mappa;
///   · **occlusione** — se un muro sta in mezzo (traccia contro le scatole della simulazione,
///     la stessa dei proiettili) il suono si abbassa e si scurisce ancora;
///   · **voci in pool** — 20 sorgenti: nessuno sparo taglia il precedente.
///
/// L'arma in mano NON passa di qui: è 2D (`WeaponAudio`), perché è a 30 cm dalla faccia.
/// </summary>
public sealed class SpatialAudio : MonoBehaviour
{
    public static SpatialAudio I { get; private set; }

    // ---------------------------------------------------------------- il modello

    /// <summary>Dentro questa distanza il suono è pieno: è "addosso".</summary>
    public const float MinDistance = 3f;

    /// <summary>"Si sente in tutta la mappa": la diagonale di Calanera (180 × 200 m).</summary>
    public const float WorldSpan = 260f;

    /// <summary>Ricariche, otturatori, estrazioni: la meccanica non si sente da lontano.</summary>
    public const float MechanicDistance = 16f;

    /// <summary>Il ferro che sbatte a terra.</summary>
    public const float DropDistance = 26f;

    private const int Voices = 20;

    private AudioSource[] _sources;
    private AudioLowPassFilter[] _lows;
    private int _next;
    private Transform _listener;
    private AnimationCurve _far;
    private AnimationCurve _tight;
    private readonly Dictionary<string, AudioClip> _clips = new();

    // ---------------------------------------------------------------- ciclo di vita

    /// <summary>Crea (una volta sola) il servizio e lo aggancia al mondo.</summary>
    public static SpatialAudio Ensure(Transform parent)
    {
        if (I != null) return I;
        var go = new GameObject("Audio spaziale");
        go.transform.SetParent(parent, false);
        return go.AddComponent<SpatialAudio>();
    }

    private void Awake()
    {
        I = this;

        // Sparo: si sente ovunque, ma lontano è un rimbombo (l'informazione resta).
        _far = new AnimationCurve(
            new Keyframe(0.00f, 1.00f),
            new Keyframe(0.04f, 0.70f),
            new Keyframe(0.12f, 0.42f),
            new Keyframe(0.25f, 0.25f),
            new Keyframe(0.60f, 0.10f),
            new Keyframe(1.00f, 0.05f));

        // Silenziate, coltello, meccanica: portata corta e poi silenzio vero.
        _tight = new AnimationCurve(
            new Keyframe(0.00f, 1.00f),
            new Keyframe(0.30f, 0.55f),
            new Keyframe(0.65f, 0.20f),
            new Keyframe(1.00f, 0.00f));

        _sources = new AudioSource[Voices];
        _lows = new AudioLowPassFilter[Voices];
        for (int i = 0; i < Voices; i++)
        {
            var go = new GameObject($"voce {i + 1:00}");
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;                       // nel mondo
            src.dopplerLevel = 0f;                       // a 64 Hz lo spostamento è a scatti
            src.rolloffMode = AudioRolloffMode.Custom;
            src.minDistance = MinDistance;
            src.priority = 100;

            var low = go.AddComponent<AudioLowPassFilter>();
            low.cutoffFrequency = 20000f;
            low.lowpassResonanceQ = 0.9f;

            _sources[i] = src;
            _lows[i] = low;
        }

        if (Probe) Dump();
    }

    private static bool Probe => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-faleziaSpatialProbe") >= 0;

    // ---------------------------------------------------------------- le portate

    /// <summary>Portata di uno sparo: quella dichiarata dalla scheda, entro la mappa.</summary>
    public static float ShotRange(WeaponDef d) => Mathf.Min(d.NoiseMeters, WorldSpan);

    /// <summary>Portata dei rumori meccanici dell'arma (ricarica, estrazione).</summary>
    public static float MechanicRange(WeaponDef d) => Mathf.Min(MechanicDistance, ShotRange(d));

    /// <summary>Portata di un'arma che cade a terra.</summary>
    public static float DropRange(WeaponDef d) => Mathf.Min(DropDistance, ShotRange(d) * 0.5f + 4f);

    // ---------------------------------------------------------------- riproduzione

    /// <summary>Uno sparo di un ALTRO giocatore, dal punto in cui esce il fumo.</summary>
    public void PlayShot(WeaponDef d, Vector3 muzzle)
    {
        var clip = Clip(d.Id, "fire");
        if (clip == null) return;

        bool tight = d.NoiseMeters < WorldSpan;          // silenziate e coltello: portata vera

        // La voce di uno sparo è grossa quanto il suo danno: una pistola a 60 m è un crepitio,
        // un Longshot a 60 m è un tuono. Non cambia la PORTATA (quella è del design), cambia il peso.
        float loud = Mathf.Clamp(0.75f + d.ShotDamage / 600f, 0.75f, 1f);

        Play(clip, muzzle, ShotRange(d), tight ? 0.95f : loud, Random.Range(0.96f, 1.04f), tight);
    }

    /// <summary>Il fendente di un altro giocatore (il coltello non spara).</summary>
    public void PlaySwing(WeaponDef d, Vector3 at)
    {
        var clip = Clip(d.Id, "swing");
        if (clip == null) return;
        Play(clip, at, ShotRange(d), 0.8f, Random.Range(0.94f, 1.06f), tight: true);
    }

    /// <summary>Ricarica di un altro giocatore: corta, e solo se sei ragionevolmente vicino.</summary>
    public void PlayReload(WeaponDef d, Vector3 at, bool empty)
    {
        var clip = Clip(d.Id, empty ? "reload_empty" : "reload");
        if (clip == null) return;
        Play(clip, at, MechanicRange(d), 0.55f, Random.Range(0.98f, 1.02f), tight: true);
    }

    /// <summary>Un'arma che si posa a terra.</summary>
    public void PlayDrop(WeaponDef d, Vector3 at)
    {
        var clip = Clip(d.Id, "drop");
        if (clip == null) return;
        Play(clip, at, DropRange(d), 0.8f, Random.Range(0.94f, 1.06f), tight: true);
    }

    /// <summary>
    /// Il cuore: posiziona una voce, le dà la curva giusta, la scurisce con la distanza
    /// e — se c'è un muro in mezzo — la scurisce ancora.
    /// </summary>
    public void Play(AudioClip clip, Vector3 pos, float range, float volume = 1f, float pitch = 1f, bool tight = true)
    {
        if (clip == null || _sources == null) return;

        Vector3 listener = ListenerPos();
        float d = Vector3.Distance(listener, pos);
        range = Mathf.Max(MinDistance + 1f, range);

        // fuori portata: non parte proprio (è la differenza fra "lontano" e "non sentito")
        float t = d / range;
        if (t > 1f) return;

        int i = _next;
        _next = (_next + 1) % _sources.Length;
        var src = _sources[i];
        var low = _lows[i];

        src.transform.position = pos;
        src.minDistance = MinDistance;
        src.maxDistance = range;
        src.rolloffMode = AudioRolloffMode.Custom;
        src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, tight ? _tight : _far);
        src.pitch = pitch;

        // distanza → sordità: 19 kHz da vicino, ~900 Hz al limite della portata
        float cutoff = Mathf.Lerp(19000f, 900f, Mathf.Pow(t, 0.55f));
        float gain = volume;

        if (Blocked(listener, pos, d))
        {
            cutoff *= 0.30f;        // il muro toglie il brillio
            gain *= 0.55f;          // e anche un bel po' di volume
        }

        src.volume = Mathf.Clamp01(gain);
        low.cutoffFrequency = Mathf.Clamp(cutoff, 300f, 22000f);
        src.PlayOneShot(clip);
    }

    /// <summary>Una clip dell'arma, letta una volta sola da StreamingAssets.</summary>
    public AudioClip Clip(string weaponId, string file)
    {
        string key = weaponId + "/" + file;
        if (_clips.TryGetValue(key, out var c)) return c;

        string path = Path.Combine(Application.streamingAssetsPath, "audio", "weapons", weaponId, file + ".wav");
        c = Wav.Load(path);
        _clips[key] = c;
        return c;
    }

    private Vector3 ListenerPos()
    {
        if (_listener == null)
        {
            var l = FindAnyObjectByType<AudioListener>();
            if (l != null) _listener = l.transform;
            else if (Camera.main != null) _listener = Camera.main.transform;
        }
        return _listener != null ? _listener.position : transform.position;
    }

    /// <summary>Un muro fra ascoltatore e sorgente: la stessa traccia dei proiettili.</summary>
    private static bool Blocked(Vector3 from, Vector3 to, float distance)
    {
        var world = GameRoot.World;
        if (world == null || distance < 1.5f) return false;

        var a = new Vec3(from.x, from.y, from.z);
        var dir = new Vec3(to.x - from.x, to.y - from.y, to.z - from.z);
        var hit = Trace.Ray(world, a, dir, distance);
        return hit.Hit && hit.Distance < distance - 0.4f;
    }

    /// <summary>`-faleziaSpatialProbe`: la tabella delle portate, per il controllo qualità.</summary>
    private static void Dump()
    {
        Debug.Log("[AUDIO 3D] portate · sparo / meccanica / caduta (m)");
        foreach (var d in Arsenal.All)
            Debug.Log($"[AUDIO 3D] {d.Id,-9} sparo {ShotRange(d),6:F0} · meccanica {MechanicRange(d),5:F0} · caduta {DropRange(d),5:F0}"
                      + $"  (NoiseMeters {d.NoiseMeters:F0}{(d.Silenced ? ", silenziata" : "")})");
        Debug.Log($"[AUDIO 3D] minimo {MinDistance:F0} m · mappa {WorldSpan:F0} m · occlusione: sì · voci {Voices}");
    }
}
