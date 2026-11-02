using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Dettagli e macchie del Porto Vecchio: cavi, assi, pallet, tavolini già nelle facciate,
    /// segnaletica consumata, macchie d'olio, pozzanghere, tombini. Tutto ciò che non è un
    /// volume solido: sta a terra o è appeso sopra i 2,3 m (doc 30, regola 2).
    /// </summary>
    public static partial class CalaneraSpawn
    {
        private static void Dettagli()
        {
            // cavi elettrici attraverso la Salita del Porto
            var ferro = B("M_Porto_Ferro");
            ferro.Catenary(new Vector3(-4, 6.9f, -81.7f), new Vector3(4, 6.6f, -81.7f), 0.45f, 0.014f, 12);
            ferro.Catenary(new Vector3(-4, 6.4f, -81.9f), new Vector3(4, 6.1f, -81.9f), 0.40f, 0.011f, 12);
            ferro.Catenary(new Vector3(-4, 5.7f, -81.8f), new Vector3(4, 5.9f, -81.8f), 0.35f, 0.009f, 10);

            // assi appoggiate al magazzino
            var legno = B("M_Porto_LegnoChiaro");
            for (int i = 0; i < 3; i++)
                legno.BoxR(new Vector3(-18.7f + i * 0.24f, 0.9f, -98.55f), new Vector3(0.12f, 1.9f, 0.04f),
                           Quaternion.Euler(0, 0, 12f));

            // pallet davanti ai container
            Pallet(new Vector3(-14.2f, 0, -92.9f), 14f);
            Pallet(new Vector3(12.8f, 0, -92.6f), -8f);

            // bitte dipinte a terra (non sporgono: doc 30)
            var nero = B("M_Porto_Acciaio");
            var cerchio = B("M_Porto_Ferro");
            foreach (float x in new[] { -17.5f, -6.2f, 6.2f, 17.5f })
            {
                Vector3 c = new(x, 0.012f, -98.55f);
                nero.Disc(c, Vector3.up, 0.19f, 16);
                cerchio.Disc(c + Vector3.up * 0.001f, Vector3.up, 0.09f, 12);
                cerchio.Disc(c + Vector3.up * 0.002f, Vector3.up, 0.032f, 8);
                // corda dipinta che parte dalla bitta
                cerchio.Beam(c + new Vector3(0.2f, 0.004f, 0), c + new Vector3(0.9f, 0.004f, 0.35f), 0.035f, 0.008f, Vector3.up);
            }

            // lampada a braccio sui magazzini, sopra i portoni
            foreach (float x in new[] { -17f, -3.2f, 7.1f, 13.3f })
            {
                Vector3 wall = new(x, 4.4f, -98.94f);
                ferro.Cylinder(wall, wall + new Vector3(0, 0, 0.75f), 0.03f, 6);
                Vector3 head = wall + new Vector3(0, -0.10f, 0.75f);
                B("M_Porto_Ferro").Cylinder(head, head + Vector3.down * 0.16f, 0.16f, 10, true, 0.06f);
                B("M_Porto_BiancoRotto").Disc(head + Vector3.down * 0.165f, Vector3.down, 0.13f, 10);
            }

            // cartello di divieto d'ormeggio, appeso a un pilastro
            ferro.Box(new Vector3(-20.0f, 2.2f, -98.80f), new Vector3(-19.9f, 2.42f, -98.78f));
            B("M_Porto_BiancoRotto").Box(new Vector3(-20.06f, 2.16f, -98.82f), new Vector3(-20.0f, 2.46f, -98.76f));
        }

        /// <summary>
        /// Rete da pesca procedurale (il GLB Meshy della rete è corrotto): cime intrecciate,
        /// galleggianti in sughero e due boe. Collisione dalle stesse mesh.
        /// </summary>
        /// <summary>Panche, vasi e lampade del cortile della piazza: la piazza diventa un posto vissuto.</summary>
        private static void PiazzaSet()
        {
            var legno = B("M_Porto_LegnoChiaro");
            var ferro = B("M_Porto_Ferro");
            var terracotta = B("M_Porto_Cotto");
            var verde = B("M_Porto_VerdeLanterna");
            var terra = B("M_Porto_Terra");

            void Panca(Vector3 p, float yaw)
            {
                var r = Quaternion.Euler(0, yaw, 0);
                for (int i = 0; i < 4; i++)
                {
                    float off = -0.72f + i * 0.36f;
                    var a = p + r * new Vector3(off, 0.42f, 0f);
                    var b = p + r * new Vector3(off + 0.30f, 0.46f, 0.48f);
                    legno.Box(a, b);
                }
                foreach (var s in new[] { -0.7f, 0.7f })
                {
                    var l0 = p + r * new Vector3(s, 0f, 0.05f);
                    ferro.Box(l0, l0 + r * new Vector3(0.06f, 0.42f, 0.05f));
                    var l1 = p + r * new Vector3(s, 0f, 0.43f);
                    ferro.Box(l1, l1 + r * new Vector3(0.06f, 0.42f, 0.05f));
                    var sp = p + r * new Vector3(s, 0.40f, 0.24f);
                    ferro.Box(sp, sp + r * new Vector3(0.05f, 0.05f, 0.28f));
                }
            }

            void Vaso(Vector3 p)
            {
                terracotta.Cylinder(p, p + new Vector3(0, 0.55f, 0), 0.30f, 10);
                terra.Cylinder(p + new Vector3(0, 0.53f, 0), p + new Vector3(0, 0.56f, 0), 0.26f, 10);
                for (int i = 0; i < 4; i++)
                {
                    var b = p + new Vector3(Mathf.Cos(i * 1.7f) * 0.12f, 0.56f, Mathf.Sin(i * 1.7f) * 0.12f);
                    verde.Tri(b, b + new Vector3(0.05f, 0.55f, 0.05f), b + new Vector3(-0.04f, 0.48f, 0.02f), Vector3.up);
                }
            }

            Panca(new Vector3(2.0f, 0f, -11.6f), 0f);
            Panca(new Vector3(10.0f, 0f, -20.2f), 180f);
            Vaso(new Vector3(1.8f, 0f, -15.2f));
            Vaso(new Vector3(10.2f, 0f, -16.4f));
            Vaso(new Vector3(5.0f, 0f, -20.0f));
        }

        /// <summary>Poster e stencil sui muri ciechi della salita: niente tele beige.</summary>
        /// <summary>Mercato: cassette e ceste attorno al banco, con prodotti colorati (D4 solo su oggetti).</summary>
        private static void MercatoSet()
        {
            Cassa(new Vector3(20.4f, 0f, -1.2f), 0.85f, 0.32f, 0.55f, 18f);
            Cassa(new Vector3(20.5f, 0.32f, -1.2f), 0.85f, 0.32f, 0.55f, -12f);
            Cassa(new Vector3(23.7f, 0f, 1.0f), 0.85f, 0.32f, 0.55f, 70f);
            Cassa(new Vector3(23.7f, 0.32f, 1.0f), 0.85f, 0.32f, 0.55f, 55f);
            Cassa(new Vector3(21.2f, 0f, 1.6f), 0.85f, 0.32f, 0.55f, 5f);
            var limone = B("M_Porto_GialloSpento");
            var pomodoro = B("M_Porto_RossoSpento");
            var verdura = B("M_Porto_VerdeLanterna");
            for (int i = 0; i < 12; i++)
            {
                var g = i % 3 == 0 ? limone : i % 3 == 1 ? pomodoro : verdura;
                float x = 20.1f + (i % 4) * 0.22f, z = -1.55f + (i / 4) * 0.20f;
                g.Cylinder(new Vector3(x, 0.35f, z), new Vector3(x, 0.42f, z), 0.075f, 8);
            }
        }

        private static void SalitaSegni()
        {
            var stencil = B("D_Stencil");
            void Muro(float x, float z, float yaw, float w, float h, float u)
            {
                var r = Quaternion.Euler(0, yaw, 0);
                var p = new Vector3(x, 0, z);
                stencil.QuadUv(p + r * new Vector3(u, 1.6f, 0.04f), p + r * new Vector3(u + w, 1.6f, 0.04f),
                               p + r * new Vector3(u + w, 1.6f + h, 0.04f), p + r * new Vector3(u, 1.6f + h, 0.04f), r * Vector3.forward);
            }
            // parete nord del bar: è quella che si vede dalla salita
            Muro(-20.0f, -82.0f, 0f, 1.2f, 1.5f, 1.1f);
            Muro(-20.0f, -82.0f, 0f, 0.8f, 1.0f, 3.0f);
            Muro(-20.0f, -82.0f, 0f, 1.0f, 1.3f, 5.3f);
            Muro(-33.0f, -80.0f, 90f, 1.2f, 1.6f, 1.4f);
            Muro(-33.0f, -64.0f, 90f, 1.4f, 1.8f, 2.0f);
            Muro(-26.0f, -88.0f, 180f, 1.1f, 1.5f, 2.0f);

            // poster come mesh sottile: il decal non si vedeva su questa faccia
            var legno = B("M_Porto_LegnoChiaro");
            var carta = B("M_Porto_BiancoRotto");
            var ferro = B("M_Porto_Ferro");
            void Poster(Vector3 p, float yaw, float w, float h)
            {
                var r = Quaternion.Euler(0, yaw, 0);
                var n = r * Vector3.forward;
                var right = r * Vector3.right;
                legno.Box(p - right * (w * 0.5f + 0.06f) - Vector3.up * 0.05f, p + right * (w * 0.5f + 0.06f) + Vector3.up * (h + 0.05f));
                carta.Quad(p - right * (w * 0.5f) + n * -0.03f, p + right * (w * 0.5f) + n * -0.03f,
                           p + right * (w * 0.5f) + Vector3.up * h + n * -0.03f, p - right * (w * 0.5f) + Vector3.up * h + n * -0.03f, n);
                ferro.Cylinder(p + Vector3.up * (h + 0.3f), p + Vector3.up * (h + 0.55f), 0.03f, 5);
            }
            // il muro visibile dalla salita è a x -30,98, non -33
            Poster(new Vector3(-30.92f, 2.05f, -79.55f), 90f, 0.85f, 1.15f);
            Poster(new Vector3(-30.92f, 1.95f, -80.85f), 90f, 0.65f, 0.9f);
            Poster(new Vector3(-30.92f, 2.15f, -78.35f), 90f, 0.55f, 0.75f);
        }

        private static void RetePorto(Vector3 p)
        {
            var verde = B("M_Porto_VerdeLanterna");
            var ocra = B("M_Porto_TendaOcra");
            var sughero = B("M_Porto_LegnoChiaro");
            var cima = B("M_Porto_BiancoRotto");
            var rnd = new System.Random(77);
            for (int i = 0; i < 14; i++)
            {
                var a = p + new Vector3((float)rnd.NextDouble() * 2.0f - 1.0f, 0.10f + (float)rnd.NextDouble() * 0.12f, (float)rnd.NextDouble() * 1.4f - 0.7f);
                var b = p + new Vector3((float)rnd.NextDouble() * 2.0f - 1.0f, 0.10f + (float)rnd.NextDouble() * 0.12f, (float)rnd.NextDouble() * 1.4f - 0.7f);
                var g = i % 2 == 0 ? verde : ocra;
                g.Catenary(a, b, 0.12f + (float)rnd.NextDouble() * 0.16f, 0.022f, 7);
            }
            for (int i = 0; i < 9; i++)
            {
                var c = p + new Vector3((float)rnd.NextDouble() * 1.9f - 0.95f, 0.08f, (float)rnd.NextDouble() * 1.2f - 0.6f);
                sughero.Cylinder(c, c + new Vector3(0, 0.10f, 0), 0.055f, 6);
            }
            foreach (var bx in new[] { new Vector3(-0.7f, 0.16f, -0.35f), new Vector3(0.55f, 0.16f, 0.25f) })
            {
                var c = p + bx;
                B("M_Porto_GialloSpento").Cylinder(c + new Vector3(0, -0.08f, 0), c + new Vector3(0, 0.30f, 0), 0.13f, 10);
                cima.Cylinder(c + new Vector3(0, 0.30f, 0), c + new Vector3(0, 0.38f, 0.15f), 0.015f, 5);
            }
        }

        private static void Pallet(Vector3 p, float rot)
        {
            var legno = B("M_Porto_LegnoChiaro");
            var q = Quaternion.Euler(0, rot, 0);
            for (int i = 0; i < 3; i++)
                legno.BoxR(p + q * new Vector3(0, 0.06f, -0.36f + i * 0.36f), new Vector3(1.2f, 0.10f, 0.10f), q);
            for (int i = 0; i < 5; i++)
                legno.BoxR(p + q * new Vector3(0, 0.13f, -0.40f + i * 0.20f), new Vector3(1.2f, 0.03f, 0.14f), q);
        }

        // ------------------------------------------------------------------ macchie e segni
        private static void MacchieESegni()
        {
            // segnaletica di corsia: tratteggiate consumate
            for (float x = -18.6f; x < 19f; x += 2.9f)
            {
                Segno("D_Linea", new Vector3(x, 0.009f, -88.30f), Vector3.right, 1.7f, 0.14f);
                Segno("D_Linea", new Vector3(x + 0.4f, 0.009f, -93.30f), Vector3.right, 1.7f, 0.14f);
            }
            // frecce verso il varco est e la salita
            Segno("D_Linea", new Vector3(15.5f, 0.009f, -90.8f), Vector3.right, 2.2f, 0.18f);
            Segno("D_Linea", new Vector3(2.4f, 0.009f, -82.9f), Vector3.forward, 2.0f, 0.16f);

            // macchie d'olio sotto i container e il carrello
            Segno("D_Olio", new Vector3(-14.6f, 0.010f, -92.1f), Vector3.forward, 2.2f, 1.6f, 12f);
            Segno("D_Olio2", new Vector3(-11.2f, 0.010f, -93.4f), Vector3.forward, 1.5f, 1.2f, -22f);
            Segno("D_Olio", new Vector3(12.2f, 0.010f, -92.3f), Vector3.forward, 1.8f, 1.3f, 40f);
            Segno("D_Olio2", new Vector3(2.5f, 0.010f, -85.0f), Vector3.forward, 1.4f, 1.1f, 8f);

            // pozzanghere: decal piatte più due veli d'acqua
            Segno("D_Pozza", new Vector3(4.6f, 0.012f, -95.4f), Vector3.forward, 2.5f, 1.7f, -8f);
            Segno("D_Pozza2", new Vector3(-15.3f, 0.012f, -89.3f), Vector3.forward, 2.0f, 1.5f, 24f);
            Segno("D_Pozza2", new Vector3(17.6f, 0.012f, -92.0f), Vector3.forward, 1.8f, 1.3f, 60f);

            // griglie di scolo e tombini
            Segno("D_Griglia", new Vector3(-2.1f, 0.011f, -87.2f), Vector3.forward, 0.5f, 0.5f);
            Segno("D_Griglia", new Vector3(13.6f, 0.011f, -95.6f), Vector3.forward, 0.5f, 0.5f);
            var ferro = B("M_Porto_Ferro");
            var acciaio = B("M_Porto_Acciaio");
            foreach (var (x, z) in new[] { (6.2f, -89.8f), (-8.4f, -94.6f) })
            {
                ferro.Disc(new Vector3(x, 0.006f, z), Vector3.up, 0.36f, 20);
                acciaio.Disc(new Vector3(x, 0.007f, z), Vector3.up, 0.31f, 20);
                ferro.Disc(new Vector3(x, 0.008f, z), Vector3.up, 0.30f, 20, 0.27f);
            }

            // targa stradale sul cantonale della Salita
            var targa = new Facciata(new Vector3(-4, 0, -76), Vector3.back, 6f, 6.2f, "M_Porto_MuroAvorio", "M_Porto_PersianaArdesia");
            targa.Insegna("D_Targa", 2.0f, 3.05f, 0.30f, false);

            // ruggine sotto le finestre alte del magazzino e sui container
            for (int i = 0; i < 3; i++)
            {
                Ruggine(new Vector3(1.8f + i * 2.1f, 5.55f, -98.946f), Vector3.forward, 0.30f, 0.7f);
                Ruggine(new Vector3(11.9f + i * 2.0f, 5.55f, -98.946f), Vector3.forward, 0.30f, 0.6f);
            }
        }

        /// <summary>Decal piatto appoggiato sul suolo (o su una parete), ruotato di rotDegradi.</summary>
        private static void Segno(string texKey, Vector3 center, Vector3 dir, float w, float h, float rotDeg = 0f)
        {
            var g = B(texKey);
            Vector3 right = Quaternion.Euler(0, rotDeg, 0) * dir.normalized;
            Vector3 normal = Vector3.Cross(right, Vector3.up).normalized;
            // per un decal a terra la normale è l'up: si ruota il "dir" nel piano
            if (Mathf.Abs(center.y) < 0.2f || true)
            {
                Vector3 r = right;
                Vector3 u = Vector3.Cross(Vector3.up, r).normalized;   // direzione "avanti" del decal a terra
                g.QuadUv(center - r * w * 0.5f - u * h * 0.5f, center + r * w * 0.5f - u * h * 0.5f,
                         center + r * w * 0.5f + u * h * 0.5f, center - r * w * 0.5f + u * h * 0.5f, Vector3.up);
            }
        }


    }
}
