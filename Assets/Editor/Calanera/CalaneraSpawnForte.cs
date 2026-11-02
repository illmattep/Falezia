using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Fronte bastionato di Forte San Verano: muro in pietra, merli, portale, feritoie,
    /// bandiera e cannoni davanti allo spawn Sud. Chiude la vista sud con un landmark militare.
    /// </summary>
    public static partial class CalaneraSpawn
    {
        private static void Forte()
        {
            var pietra = B("M_Porto_Pietra");
            var scura = B("M_Porto_PietraScura");
            var ferro = B("M_Porto_Ferro");
            var legno = B("M_Porto_LegnoScuro");
            var bandiera = B("M_Porto_BiancoRotto");

            const float z = 96.0f;
            const float d = 2.4f;
            const float h = 6.4f;

            // cortina e ali del bastione
            pietra.Box(new Vector3(-30f, 0f, z), new Vector3(-3.4f, h, z + d));
            pietra.Box(new Vector3(3.4f, 0f, z), new Vector3(30f, h, z + d));
            pietra.Box(new Vector3(-30f, 0f, z + d), new Vector3(-26f, h - 1.2f, z + 6f));
            pietra.Box(new Vector3(26f, 0f, z + d), new Vector3(30f, h - 1.2f, z + 6f));
            scura.Box(new Vector3(-30f, 0f, z - 0.3f), new Vector3(30f, 1.2f, z));
            scura.Box(new Vector3(-30f, h - 0.2f, z - 0.25f), new Vector3(30f, h + 0.15f, z + d + 0.25f));

            // merli
            for (float x = -29.2f; x < 29.2f; x += 2.6f)
            {
                if (x > -4.0f && x < 4.0f) continue;
                pietra.Box(new Vector3(x, h, z), new Vector3(x + 1.3f, h + 0.85f, z + 0.9f));
            }

            // portale con arco a tutto sesto
            pietra.Box(new Vector3(-3.4f, 0f, z), new Vector3(-2.4f, h, z + d));
            pietra.Box(new Vector3(2.4f, 0f, z), new Vector3(3.4f, h, z + d));
            pietra.Box(new Vector3(-2.4f, 5.4f, z), new Vector3(2.4f, h, z + d));
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.PI * i / 6f;
                var p0 = new Vector3(Mathf.Cos(a) * 2.4f, 5.4f - Mathf.Sin(a) * 1.6f, z);
                var p1 = new Vector3(Mathf.Cos(a + 0.35f) * 2.4f, 5.4f - Mathf.Sin(a + 0.35f) * 1.6f, z);
                pietra.Box(p0, p1 + new Vector3(0, 0.22f, 0));
            }
            // battenti in legno e borchie
            legno.Box(new Vector3(-2.2f, 0f, z + 0.15f), new Vector3(-0.05f, 4.4f, z + 0.35f));
            legno.Box(new Vector3(0.05f, 0f, z + 0.15f), new Vector3(2.2f, 4.4f, z + 0.35f));
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                {
                    float bx = -1.9f + c * 1.05f, by = 0.5f + r * 1.1f;
                    ferro.Disc(new Vector3(bx, by, z + 0.36f), Vector3.back, 0.05f, 6);
                }

            // feritoie
            for (int i = 0; i < 6; i++)
            {
                float fx = -24f + i * 9.6f;
                if (fx > -5f && fx < 5f) continue;
                scura.Box(new Vector3(fx - 0.18f, 3.2f, z - 0.12f), new Vector3(fx + 0.18f, 4.0f, z + 0.02f));
            }

            // bandiera sul portale
            ferro.Cylinder(new Vector3(0f, h, z + 1.2f), new Vector3(0f, h + 4.2f, z + 1.2f), 0.06f, 6);
            bandiera.Quad(new Vector3(0.05f, h + 4.15f, z + 1.2f), new Vector3(2.6f, h + 3.65f, z + 1.25f),
                          new Vector3(2.6f, h + 2.55f, z + 1.25f), new Vector3(0.05f, h + 2.95f, z + 1.2f), Vector3.back);
            bandiera.Quad(new Vector3(1.3f, h + 3.9f, z + 1.24f), new Vector3(1.3f, h + 3.2f, z + 1.24f),
                          new Vector3(1.6f, h + 3.55f, z + 1.24f), new Vector3(1.6f, h + 3.55f, z + 1.24f), Vector3.back);

            // cannoni davanti al forte
            for (int i = 0; i < 2; i++)
            {
                float cx = i == 0 ? -6.4f : 6.4f;
                var c = new Vector3(cx, 0f, z - 2.4f);
                scura.Cylinder(c + new Vector3(0, 0.55f, -0.9f), c + new Vector3(0, 0.85f, 1.4f), 0.28f, 12);
                scura.Cylinder(c + new Vector3(0, 0.85f, 1.4f), c + new Vector3(0, 0.95f, 1.9f), 0.34f, 12);
                legno.Box(c + new Vector3(-0.9f, 0f, -0.4f), c + new Vector3(0.9f, 0.55f, 1.0f));
                ferro.Cylinder(c + new Vector3(-0.8f, 0.28f, -0.3f), c + new Vector3(-0.8f, 0.28f, 0.3f), 0.28f, 8);
                ferro.Cylinder(c + new Vector3(0.8f, 0.28f, -0.3f), c + new Vector3(0.8f, 0.28f, 0.3f), 0.28f, 8);
            }

            // rampa/pavimento in pietra davanti al portale
            pietra.Box(new Vector3(-3.4f, 0f, z - 3.2f), new Vector3(3.4f, 0.08f, z));
        }
    }
}
