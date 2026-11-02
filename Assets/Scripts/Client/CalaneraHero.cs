using UnityEngine;

/// <summary>
/// Marcatore per gli asset hero (fontana, banco, barca): il builder Unity li mette
/// nella scena con LODGroup e MeshCollider per i fori; l'export collisioni usa questo
/// componente per aggiungere la scatola AABB alla simulazione, così il giocatore
/// non attraversa i modelli nuovi.
/// </summary>
public class CalaneraHero : MonoBehaviour
{
    public Vector3 size = new Vector3(3f, 2f, 3f);
    public Vector3 center = new Vector3(0f, 1f, 0f);
}
