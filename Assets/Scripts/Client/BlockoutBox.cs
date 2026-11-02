using UnityEngine;

/// <summary>
/// Una scatola del BLOCKOUT di Calanera dentro la scena Unity (doc 29, F0).
///
/// Il blockout è la verità fisica della mappa: collisione della simulazione, rotte dei bot,
/// spawn. In Unity ogni scatola è un GameObject con un BoxCollider allineato agli assi; la
/// si sposta e si ridimensiona con gli strumenti normali (solo spostamento e scala: la
/// simulazione conosce solo scatole dritte). Il menu Falezia → Calanera → Blockout: esporta
/// riscrive calanera.json.
///
/// <see cref="original"/> è la riga del json da cui è nata: se la scatola non è stata toccata,
/// l'esportazione la riscrive identica (il file non cambia nemmeno di un byte).
/// </summary>
[DisallowMultipleComponent]
public class BlockoutBox : MonoBehaviour
{
    [Tooltip("Tipo della scatola (street, plaster, stone, stairs, ...): deve esistere nella lista 'kinds' del json")]
    public string kind = "plaster";

    [Tooltip("Posizione nel json (-1 = nuova, verrà aggiunta in fondo)")]
    public int index = -1;

    [HideInInspector] public string original;
    [HideInInspector] public Vector3 importedMin;
    [HideInInspector] public Vector3 importedSize;

    /// <summary>Minimo e dimensioni dal collider, in coordinate del mondo (= della simulazione).</summary>
    public void GetBounds(out Vector3 min, out Vector3 size)
    {
        var c = GetComponent<BoxCollider>();
        if (c == null) { min = transform.position; size = Vector3.zero; return; }
        // dal transform e non da Collider.bounds: funziona anche a scena ferma, senza fisica
        size = Vector3.Scale(transform.lossyScale, c.size);
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        min = transform.TransformPoint(c.center) - size * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        GetBounds(out var min, out var size);
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.9f);
        Gizmos.DrawWireCube(min + size * 0.5f, size);
    }
}
