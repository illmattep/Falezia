using System.Collections.Generic;
using System.IO;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// I SUONI DELL'ARMA IN PUGNO.
///
/// Ogni arma ha sei suoni in `StreamingAssets/audio/weapons/&lt;id&gt;/`: sparo, sparo
/// dell'ultimo colpo (otturatore che resta indietro), ricarica tattica, ricarica a vuoto,
/// estrazione e colpo a vuoto. Il coltello ha il fendente al posto dello sparo.
///
/// I file li genera `tools/audio/make_weapon_sounds.py` (`./run.sh sound`): sono sintetizzati,
/// non campionati, quindi sono nostri e ognuno ha il carattere della sua arma. Le durate
/// delle ricariche vengono **lette dalla simulazione**, così il suono finisce quando finisce
/// l'animazione.
///
/// Tre scelte che si sentono:
///  - **voci multiple**: a 800 RPM due colpi si sovrappongono, e ognuno ha il suo pitch
///    leggermente diverso (mai due colpi identici);
///  - **volume che compensa la cadenza**: chi spara tanto per secondo suona più piano per
///    colpo, altrimenti il mix satura (`FireVolume`);
///  - **la ricarica ha una voce sua**, che si può fermare: se cambi arma a metà, il suono
///    del caricatore non resta appeso.
///
/// È tutta roba 2D (`spatialBlend = 0`): l'arma è in mano, non nel mondo.
/// </summary>
public class WeaponAudio : MonoBehaviour
{
    private const int ShotVoices = 6;      // colpi sovrapposti
    private const int MechVoices = 3;      // click, tonfi, estrazione

    private sealed class Clips
    {
        public AudioClip Fire, FireLast, Reload, ReloadEmpty, Draw, Empty, Swing;
        public bool Any => Fire != null || Swing != null;
    }

    private readonly Dictionary<string, Clips> _cache = new();
    private AudioSource[] _shots;
    private AudioSource[] _mech;
    private AudioSource _reload;
    private int _nextShot, _nextMech;

    private Clips _set;
    private WeaponDef _def;
    private float _fireVolume = 0.6f;

    /// <summary>L'arma in mano ha trovato i suoi file? (diagnostica e `-faleziaAudioProbe`)</summary>
    public bool Ready => _set != null && _set.Any;
    public string Current => _def == null ? "—" : $"{_def.Id} ({(Ready ? "caricata" : "senza file")})";

    private static bool Probe => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-faleziaAudioProbe") >= 0;

    public void Init()
    {
        var go = new GameObject("Audio armi");
        go.transform.SetParent(transform, false);

        _shots = new AudioSource[ShotVoices];
        for (int i = 0; i < ShotVoices; i++) _shots[i] = AddSource(go, $"sparo {i + 1}");
        _mech = new AudioSource[MechVoices];
        for (int i = 0; i < MechVoices; i++) _mech[i] = AddSource(go, $"meccanica {i + 1}");
        _reload = AddSource(go, "ricarica");

        // `-faleziaAudioProbe`: carica TUTTE le armi e stampa cosa ha trovato.
        // Serve al controllo qualità: 17 armi × 6 file sono 99 file che nessuno apre a mano.
        if (Probe)
            foreach (var d in Arsenal.All)
            {
                SetWeapon(d);
                string Len(AudioClip c) => c != null ? c.length.ToString("F2") + "s" : "MANCA";

                // il coltello non spara e non ricarica: ha il fendente e basta
                Debug.Log(d.Melee
                    ? $"[AUDIO] {d.Id,-9} fendente={Len(FireClip(d, false))}"
                      + $" estrazione={Len(Dr(d))} volume={FireVolume(d):F2}"
                    : $"[AUDIO] {d.Id,-9} sparo={Len(FireClip(d, false))}"
                      + $" ultimo={Len(FireClip(d, true))}"
                      + $" ricarica={Len(Rc(d, false))}"
                      + $" aVuoto={Len(Rc(d, true))}"
                      + $" estrazione={Len(Dr(d))}"
                      + $" volume={FireVolume(d):F2}");
            }
    }

    private AudioClip FireClip(WeaponDef d, bool last) =>
        _cache.TryGetValue(d.Id, out var c) ? (last ? c.FireLast : c.Fire ?? c.Swing) : null;
    private AudioClip Rc(WeaponDef d, bool empty) =>
        _cache.TryGetValue(d.Id, out var c) ? (empty ? c.ReloadEmpty : c.Reload) : null;
    private AudioClip Dr(WeaponDef d) => _cache.TryGetValue(d.Id, out var c) ? c.Draw : null;

    private static AudioSource AddSource(GameObject go, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(go.transform, false);
        var src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;        // in mano: niente distanza, niente panning
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        return src;
    }

    // ------------------------------------------------------------------ arma in mano

    /// <summary>Cambia l'arma: carica i suoi suoni (una volta sola, poi restano in cache).</summary>
    public void SetWeapon(WeaponDef def)
    {
        if (def == null) return;
        _def = def;
        _fireVolume = FireVolume(def);

        if (!_cache.TryGetValue(def.Id, out _set))
        {
            _set = Load(def.Id);
            _cache[def.Id] = _set;
        }
    }

    /// <summary>
    /// Il volume di un colpo compensa la cadenza: 0,85 al ritmo di una pistola lenta,
    /// giù fino a ~0,35 per una SMG che spara 14 volte al secondo. Le silenziate stanno
    /// molto più basse — è il loro vantaggio, e si deve sentire.
    /// </summary>
    public static float FireVolume(WeaponDef d)
    {
        float v = Mathf.Clamp(0.85f * Mathf.Sqrt(d.Cycle / 0.25f), 0.30f, 0.90f);
        if (d.Silenced && !d.Melee) v *= 0.45f;
        if (d.Pellets > 1) v = Mathf.Min(0.95f, v * 1.20f);   // le pompe devono dominare
        if (d.Melee) v = 0.55f;
        return v;
    }

    private static Clips Load(string id)
    {
        string dir = Path.Combine(Application.streamingAssetsPath, "audio", "weapons", id);
        return new Clips
        {
            Fire = Wav.Load(Path.Combine(dir, "fire.wav")),
            FireLast = Wav.Load(Path.Combine(dir, "fire_last.wav")),
            Reload = Wav.Load(Path.Combine(dir, "reload.wav")),
            ReloadEmpty = Wav.Load(Path.Combine(dir, "reload_empty.wav")),
            Draw = Wav.Load(Path.Combine(dir, "draw.wav")),
            Empty = Wav.Load(Path.Combine(dir, "empty.wav")),
            Swing = Wav.Load(Path.Combine(dir, "swing.wav")),
        };
    }

    // ------------------------------------------------------------------ eventi

    /// <summary>Uno sparo. `last` = era l'ultimo colpo del caricatore (otturatore indietro).</summary>
    public void Fire(bool last)
    {
        if (_set == null) return;
        var clip = last && _set.FireLast != null ? _set.FireLast : _set.Fire;
        if (clip == null) return;

        var src = Next(_shots, ref _nextShot);
        src.pitch = Random.Range(0.965f, 1.035f);     // mai due colpi identici
        src.PlayOneShot(clip, _fireVolume * Random.Range(0.93f, 1.0f));
    }

    /// <summary>Il fendente del coltello: non c'è sparo, c'è l'aria.</summary>
    public void MeleeSwing()
    {
        if (_set?.Swing == null) return;
        var src = Next(_mech, ref _nextMech);
        src.pitch = Random.Range(0.94f, 1.07f);
        src.PlayOneShot(_set.Swing, 0.55f);
    }

    /// <summary>Grilletto a vuoto: il percussore che batte sul nulla.</summary>
    public void DryFire()
    {
        if (_set?.Empty == null) return;
        var src = Next(_mech, ref _nextMech);
        src.pitch = Random.Range(0.97f, 1.04f);
        src.PlayOneShot(_set.Empty, 0.45f);
    }

    /// <summary>La ricarica parte: la sua voce si ferma se cambi arma o interrompi.</summary>
    public void Reload(bool empty)
    {
        if (_set == null) return;
        var clip = empty && _set.ReloadEmpty != null ? _set.ReloadEmpty : _set.Reload;
        if (clip == null) return;

        _reload.Stop();
        _reload.clip = clip;
        _reload.pitch = 1f;        // la clip dura quanto la ricarica: niente scarti
        _reload.volume = 0.55f;
        _reload.Play();
    }

    public void StopReload()
    {
        if (_reload != null && _reload.isPlaying) _reload.Stop();
    }

    /// <summary>L'arma entra in scena.</summary>
    public void Draw()
    {
        if (_set?.Draw == null) return;
        var src = Next(_mech, ref _nextMech);
        src.pitch = Random.Range(0.97f, 1.03f);
        src.PlayOneShot(_set.Draw, 0.45f);
    }

    // ------------------------------------------------------------------ voci

    /// <summary>Gira fra le voci: nessuno sparo taglia il precedente.</summary>
    private static AudioSource Next(AudioSource[] pool, ref int index)
    {
        var src = pool[index];
        index = (index + 1) % pool.Length;
        return src;
    }
}
