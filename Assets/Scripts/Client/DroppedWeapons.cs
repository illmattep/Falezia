using System.Collections.Generic;
using System.IO;
using Falezia.Sim;
using UnityEngine;

/// <summary>
/// LE ARMI A TERRA.
///
/// Le regole stanno nella simulazione (`SimState.Drops`): posizione, munizioni, chi le ha
/// lasciate, quando si sono posate. Qui si disegna soltanto — un volume grigio con la forma
/// della classe, sdraiato sul pavimento — e si suona il **ferro che sbatte** quando tocca
/// terra (`LandTick`), una volta sola per arma.
///
/// Il suono del drop è 3D: la posizione è quella dell'arma, quindi si sente da dove arriva.
/// Le altre armi non hanno una voce loro finché non ci sarà audio dei remoti (doc 17).
/// </summary>
public class DroppedWeapons : MonoBehaviour
{
    private sealed class Visual
    {
        public GameObject Root;
        public bool Settled;
    }

    private readonly Dictionary<int, Visual> _live = new();

    private PlayerRig _rig;
    private Material _material;
    private AudioSource _pickupVoice;      // la raccolta è in prima persona: 2D

    public void Init(PlayerRig rig)
    {
        _rig = rig;

        // un materiale solo per tutte le armi a terra: grigio ferro, un filo di riflesso
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        _material = new Material(shader) { name = "Armi a terra" };
        if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", new Color(0.20f, 0.21f, 0.23f));
        if (_material.HasProperty("_Color")) _material.SetColor("_Color", new Color(0.20f, 0.21f, 0.23f));
        if (_material.HasProperty("_Metallic")) _material.SetFloat("_Metallic", 0.75f);
        if (_material.HasProperty("_Smoothness")) _material.SetFloat("_Smoothness", 0.45f);

        // le cadute passano dal servizio spaziale (distanza, filtro, occlusione);
        // la raccolta no: è la TUA mano, quindi 2D.
        var go = new GameObject("Voce raccolta");
        go.transform.SetParent(transform, false);
        _pickupVoice = go.AddComponent<AudioSource>();
        _pickupVoice.playOnAwake = false;
        _pickupVoice.spatialBlend = 0f;
    }

    private void Update()
    {
        if (_rig == null) return;
        var drops = _rig.Drops;
        if (drops == null) return;

        // 1. via gli oggetti delle armi che non ci sono più (raccolte, round finito)
        var seen = new HashSet<int>();
        for (int i = 0; i < drops.Count; i++) seen.Add(drops[i].Id);
        var gone = new List<int>();
        foreach (var kv in _live) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
        foreach (int id in gone)
        {
            Destroy(_live[id].Root);
            _live.Remove(id);
        }

        // 2. crea/aggiorna, e suona quando un'arma SI POSA
        for (int i = 0; i < drops.Count; i++)
        {
            var g = drops[i];
            if (!_live.TryGetValue(g.Id, out var v))
            {
                v = new Visual { Root = Build(g), Settled = g.Settled };
                _live[g.Id] = v;
            }
            else if (!v.Settled && g.Settled)
            {
                v.Settled = true;
                PlayDrop(g);
            }

            var t = v.Root.transform;
            t.position = new Vector3(g.Pos.X, g.Pos.Y + 0.035f, g.Pos.Z);
            t.rotation = Quaternion.Euler(0f, g.Spin * Mathf.Rad2Deg, 90f);
        }
    }

    /// <summary>Il volume grigio che rappresenta l'arma: forma e misura per classe.</summary>
    private GameObject Build(GroundWeapon g)
    {
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = $"A terra · {Arsenal.Get(g.Slot).Name}";

        var col = root.GetComponent<Collider>();
        if (col != null) Destroy(col);                  // non è fisica: non deve spingere nessuno

        var r = root.GetComponent<Renderer>();
        r.sharedMaterial = _material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        float len = g.Slot switch
        {
            WeaponSlot.Standard or WeaponSlot.Hush or WeaponSlot.Chatter or WeaponSlot.Snub or WeaponSlot.Hammer => 0.26f,
            WeaponSlot.Cadence or WeaponSlot.Scrap or WeaponSlot.Blitz => 0.44f,
            WeaponSlot.Choke or WeaponSlot.Sweeper => 0.72f,
            WeaponSlot.Anvil or WeaponSlot.Service => 0.82f,
            WeaponSlot.Vantage => 0.86f,
            WeaponSlot.Spotter => 0.90f,
            WeaponSlot.Longshot => 0.98f,
            WeaponSlot.Heavy => 0.88f,
            _ => 0.30f,
        };
        // il cubo è lungo su Z: l'arma giace con la canna verso l'orizzonte
        root.transform.localScale = new Vector3(0.09f, 0.09f, len);
        return root;
    }

    /// <summary>La caduta, dal punto in cui l'arma tocca terra.</summary>
    private void PlayDrop(GroundWeapon g)
    {
        var def = Arsenal.Get(g.Slot);
        var at = new Vector3(g.Pos.X, g.Pos.Y + 0.1f, g.Pos.Z);
        SpatialAudio.I?.PlayDrop(def, at);
    }

    /// <summary>Raccolta: suono in prima persona, perché l'arma è appena entrata in mano.</summary>
    public void PlayPickup(WeaponSlot slot)
    {
        var clip = SpatialAudio.I?.Clip(Arsenal.Get(slot).Id, "pickup");
        if (clip == null) return;
        _pickupVoice.pitch = Random.Range(0.97f, 1.04f);
        _pickupVoice.PlayOneShot(clip, 0.55f);
    }
}
