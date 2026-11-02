using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LO SCHELETRO DELL'OPERATORE, condiviso fra il corpo in terza persona
/// (<see cref="OperatorView"/>) e le braccia in prima persona (<see cref="FirstPersonArms"/>).
///
/// Tre scelte, tutte per non dipendere da convenzioni che non si possono verificare:
///
/// 1. **Il lato si riconosce dalla posizione, non dal nome.** Nel modello le ossa ".L" e
///    ".R" sono invertite rispetto all'anatomia (lo scheletro è stato costruito su una
///    scansione che guarda -Y in Blender, e l'import in Unity specchia X): `hand.R` sta a
///    SINISTRA del personaggio. Prima il braccio "destro" andava a cercare un bersaglio a
///    destra attraversando il petto. Qui il destro è quello che sta a destra.
/// 2. **IK a due ossa vero**, con le lunghezze misurate sul modello: la mano arriva esattamente
///    dove deve e il gomito si piega dalla parte del "polo" (in giù e in fuori).
/// 3. **La mano si orienta con assi misurati sul modello a riposo** (direzione delle dita e
///    del palmo), non con assi locali indovinati.
/// </summary>
public sealed class OperatorRig
{
    public sealed class Arm
    {
        public Transform Clav, Upper, Fore, Hand;
        public readonly List<Transform> Fingers = new();     // falangi delle quattro dita
        public readonly List<Transform> Thumb = new();       // falangi del pollice
        public float UpperLen, ForeLen;
        public Vector3 FingerLocal, PalmLocal;               // assi della mano, nello spazio dell'osso
        // rollio: asse dell'osso e lato "anteriore" (braccio) o del palmo (avambraccio), a riposo
        public Vector3 UpperAxis, UpperFront, ForeAxis, ForePalm;
    }

    public sealed class Leg
    {
        public Transform Thigh, Shin, Foot;
    }

    public Transform Root;     // spazio personaggio: +Z avanti, +Y su, +X destra
    public Transform Body;     // l'istanza del modello, figlia di Root
    public Transform Pelvis, Spine, Chest, Neck, Head;
    public readonly Arm Right = new(), Left = new();
    public readonly Leg LegR = new(), LegL = new();
    public SkinnedMeshRenderer Skin;

    /// <summary>Altezza della caviglia a riposo nello spazio personaggio (per poggiare i piedi).</summary>
    public float RestAnkleY;
    /// <summary>Occhio del modello nello spazio personaggio (per agganciare le braccia alla camera).</summary>
    public Vector3 RestEye;

    private readonly Dictionary<Transform, Quaternion> _rest = new();
    private readonly Dictionary<Transform, Vector3> _axX = new(), _axY = new(), _axZ = new();
    private readonly List<Transform> _all = new();

    /// <summary>Lega lo scheletro di un'istanza appena creata (in posa di riposo, sotto <paramref name="root"/>).</summary>
    public static OperatorRig Bind(GameObject body, Transform root)
    {
        var r = new OperatorRig { Root = root, Body = body.transform };
        var t = body.transform;
        r.Skin = body.GetComponentInChildren<SkinnedMeshRenderer>(true);

        r.Pelvis = Find(t, "pelvis"); r.Spine = Find(t, "spine"); r.Chest = Find(t, "chest");
        r.Neck = Find(t, "neck"); r.Head = Find(t, "head");
        if (r.Pelvis == null || r.Chest == null) return null;

        // i due bracci col nome del file; quale sia il destro lo dice la posizione
        var a = new Arm { Clav = Find(t, "clav.L"), Upper = Find(t, "arm.L"), Fore = Find(t, "fore.L"), Hand = Find(t, "hand.L") };
        var b = new Arm { Clav = Find(t, "clav.R"), Upper = Find(t, "arm.R"), Fore = Find(t, "fore.R"), Hand = Find(t, "hand.R") };
        var la = new Leg { Thigh = Find(t, "thigh.L"), Shin = Find(t, "shin.L"), Foot = Find(t, "foot.L") };
        var lb = new Leg { Thigh = Find(t, "thigh.R"), Shin = Find(t, "shin.R"), Foot = Find(t, "foot.R") };
        if (a.Hand == null || b.Hand == null) return null;
        foreach (var f in new[] { "index", "middle", "ring", "pinky" })
            for (int s = 1; s <= 2; s++)
            {
                var fa = Find(t, $"{f}.0{s}.L"); if (fa != null) a.Fingers.Add(fa);
                var fb = Find(t, $"{f}.0{s}.R"); if (fb != null) b.Fingers.Add(fb);
            }
        for (int s = 1; s <= 2; s++)
        {
            var ta = Find(t, $"thumb.0{s}.L"); if (ta != null) a.Thumb.Add(ta);
            var tb = Find(t, $"thumb.0{s}.R"); if (tb != null) b.Thumb.Add(tb);
        }

        bool aIsRight = root.InverseTransformPoint(a.Hand.position).x > 0f;
        Copy(aIsRight ? a : b, r.Right);
        Copy(aIsRight ? b : a, r.Left);
        bool laIsRight = la.Thigh != null && root.InverseTransformPoint(la.Thigh.position).x > 0f;
        CopyLeg(laIsRight ? la : lb, r.LegR);
        CopyLeg(laIsRight ? lb : la, r.LegL);

        foreach (var tr in t.GetComponentsInChildren<Transform>(true)) r.Capture(tr);
        r.Measure(r.Right, +1f);
        r.Measure(r.Left, -1f);

        float ankle = float.MaxValue;
        if (r.LegR.Foot != null) ankle = Mathf.Min(ankle, root.InverseTransformPoint(r.LegR.Foot.position).y);
        if (r.LegL.Foot != null) ankle = Mathf.Min(ankle, root.InverseTransformPoint(r.LegL.Foot.position).y);
        r.RestAnkleY = ankle == float.MaxValue ? 0.09f : ankle;

        // occhio: 60 % dell'altezza della testa, 9 cm davanti al suo asse
        var hp = r.Head != null ? root.InverseTransformPoint(r.Head.position) : new Vector3(0f, 1.45f, 0f);
        r.RestEye = hp + new Vector3(0f, 0.12f, 0.09f);
        return r;
    }

    private static void Copy(Arm from, Arm to)
    {
        to.Clav = from.Clav; to.Upper = from.Upper; to.Fore = from.Fore; to.Hand = from.Hand;
        to.Fingers.AddRange(from.Fingers);
        to.Thumb.AddRange(from.Thumb);
    }

    private static void CopyLeg(Leg from, Leg to)
    {
        to.Thigh = from.Thigh; to.Shin = from.Shin; to.Foot = from.Foot;
    }

    /// <summary>Lunghezze e assi della mano, dalla posa di riposo (braccia in A, palmi verso le cosce).</summary>
    private void Measure(Arm arm, float side)
    {
        if (arm.Upper == null || arm.Fore == null || arm.Hand == null) return;
        arm.UpperLen = Vector3.Distance(arm.Upper.position, arm.Fore.position);
        arm.ForeLen = Vector3.Distance(arm.Fore.position, arm.Hand.position);

        // dita: dal polso alla nocca del medio (se manca, lungo l'osso)
        Transform knuckle = null;
        foreach (var f in arm.Fingers) if (Norm(f.name).StartsWith("middle01")) knuckle = f;
        Vector3 fingers = knuckle != null ? (knuckle.position - arm.Hand.position).normalized : arm.Hand.up;
        // palmo: verso la coscia, cioè verso l'asse del corpo
        Vector3 palm = Root.TransformDirection(new Vector3(-side, 0f, 0f));
        palm = (palm - Vector3.Dot(palm, fingers) * fingers).normalized;

        var inv = Quaternion.Inverse(arm.Hand.rotation);
        arm.FingerLocal = inv * fingers;
        arm.PalmLocal = inv * palm;

        // rollio di braccio e avambraccio: a riposo il gomito si piega in AVANTI e il palmo
        // guarda la coscia. Servono per girare le ossa attorno al loro asse in modo anatomico
        // (senza, l'IK lasciava l'avambraccio ruotato di 180° rispetto alla mano: polso a caramella).
        Vector3 up = (arm.Fore.position - arm.Upper.position).normalized;
        Vector3 fa = (arm.Hand.position - arm.Fore.position).normalized;
        Vector3 front = Root.forward; front = (front - Vector3.Dot(front, up) * up).normalized;
        Vector3 foreP = Root.TransformDirection(new Vector3(-side, 0f, 0f)); foreP = (foreP - Vector3.Dot(foreP, fa) * fa).normalized;
        var iu = Quaternion.Inverse(arm.Upper.rotation);
        var ifo = Quaternion.Inverse(arm.Fore.rotation);
        arm.UpperAxis = iu * up; arm.UpperFront = iu * front;
        arm.ForeAxis = ifo * fa; arm.ForePalm = ifo * foreP;
    }

    private void Capture(Transform b)
    {
        if (b == null || _rest.ContainsKey(b)) return;
        _rest[b] = b.localRotation;
        _all.Add(b);
        var inv = Quaternion.Inverse(b.rotation);
        _axX[b] = inv * (Root.rotation * Vector3.right);
        _axY[b] = inv * (Root.rotation * Vector3.up);
        _axZ[b] = inv * (Root.rotation * Vector3.forward);
    }

    public static Transform Find(Transform root, string name)
    {
        string want = Norm(name);
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (Norm(t.name) == want) return t;
        return null;
    }

    private static string Norm(string s) => s.Replace(".", "").Replace("_", "").Replace(" ", "").ToLowerInvariant();

    // ------------------------------------------------------------------ posa

    /// <summary>Tutte le ossa tornano alla posa di riposo: ogni fotogramma riparte da qui (niente deriva).</summary>
    public void ResetPose()
    {
        foreach (var b in _all) b.localRotation = _rest[b];
        Body.localPosition = Vector3.zero;
        Body.localRotation = Quaternion.identity;
    }

    /// <summary>Ruota un osso sopra la posa di riposo, di tre angoli attorno agli assi del PERSONAGGIO (gradi).</summary>
    public void Rotate(Transform b, float x, float y, float z)
    {
        if (b == null || !_rest.ContainsKey(b)) return;
        var q = Quaternion.identity;
        if (Mathf.Abs(x) > 0.01f) q *= Quaternion.AngleAxis(x, _axX[b]);
        if (Mathf.Abs(y) > 0.01f) q *= Quaternion.AngleAxis(y, _axY[b]);
        if (Mathf.Abs(z) > 0.01f) q *= Quaternion.AngleAxis(z, _axZ[b]);
        b.localRotation = b.localRotation * q;
    }

    /// <summary>Abbassa il corpo finché il piede d'appoggio tocca terra (accovacciati le gambe si piegano: il bacino deve scendere).</summary>
    public void Ground()
    {
        float y = float.MaxValue;
        if (LegR.Foot != null) y = Mathf.Min(y, Root.InverseTransformPoint(LegR.Foot.position).y);
        if (LegL.Foot != null) y = Mathf.Min(y, Root.InverseTransformPoint(LegL.Foot.position).y);
        if (y == float.MaxValue) return;
        Body.localPosition += new Vector3(0f, RestAnkleY - y, 0f);
    }

    // ------------------------------------------------------------------ braccia

    /// <summary>
    /// IK a due ossa: porta il polso su <paramref name="wrist"/>, con il gomito verso
    /// <paramref name="pole"/>. Le ossa si girano anche attorno al proprio asse: il braccio con
    /// la piega del gomito in avanti, l'avambraccio col palmo verso <paramref name="palm"/>
    /// (la pronazione sta nell'avambraccio, come nel corpo vero: il polso non si attorciglia).
    /// </summary>
    public void SolveArm(Arm arm, Vector3 wrist, Vector3 pole, Vector3 palm)
    {
        if (arm.Upper == null || arm.Fore == null || arm.Hand == null) return;
        float a = arm.UpperLen, b = arm.ForeLen;
        float reach = (a + b) * 0.999f;

        Vector3 s = arm.Upper.position;
        Vector3 to = wrist - s;
        float d = to.magnitude;
        if (d < 1e-4f) return;
        Vector3 dir = to / d;
        d = Mathf.Clamp(d, Mathf.Abs(a - b) + 0.01f, reach);

        float cosA = Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f);
        float sinA = Mathf.Sqrt(1f - cosA * cosA);
        Vector3 bend = pole - s;
        bend -= Vector3.Dot(bend, dir) * dir;
        if (bend.sqrMagnitude < 1e-6f) bend = -Root.up - Vector3.Dot(-Root.up, dir) * dir;
        bend.Normalize();
        Vector3 elbow = s + dir * (cosA * a) + bend * (sinA * a);
        Vector3 reached = s + dir * d;

        // braccio: asse verso il gomito, lato anteriore verso l'avambraccio
        Vector3 ua = (elbow - s).normalized;
        Vector3 fa = (reached - elbow).normalized;
        Vector3 inner = fa - Vector3.Dot(fa, ua) * ua;
        if (inner.sqrMagnitude < 1e-6f) inner = Root.forward - Vector3.Dot(Root.forward, ua) * ua;
        arm.Upper.rotation = Quaternion.LookRotation(ua, inner.normalized) *
                             Quaternion.Inverse(Quaternion.LookRotation(arm.UpperAxis, arm.UpperFront));

        // avambraccio: asse verso il polso, rollio dal palmo voluto
        var fore = Quaternion.FromToRotation(arm.Fore.rotation * arm.ForeAxis, fa) * arm.Fore.rotation;
        Vector3 p0 = fore * arm.ForePalm; p0 = (p0 - Vector3.Dot(p0, fa) * fa).normalized;
        Vector3 pw = palm - Vector3.Dot(palm, fa) * fa;
        if (pw.sqrMagnitude > 1e-6f)
            fore = Quaternion.AngleAxis(Vector3.SignedAngle(p0, pw.normalized, fa), fa) * fore;
        arm.Fore.rotation = fore;
    }

    /// <summary>Orienta la mano: dita lungo <paramref name="fingers"/>, palmo verso <paramref name="palm"/> (mondo).</summary>
    public void OrientHand(Arm arm, Vector3 fingers, Vector3 palm)
    {
        if (arm.Hand == null || fingers.sqrMagnitude < 1e-6f) return;
        var want = Quaternion.LookRotation(fingers, palm);
        var local = Quaternion.LookRotation(arm.FingerLocal, arm.PalmLocal);
        arm.Hand.rotation = want * Quaternion.Inverse(local);
    }

    /// <summary>
    /// Chiude la mano attorno a un'impugnatura: ogni falange punta verso un punto davanti al
    /// palmo. <paramref name="amount"/> 0 = aperta, 1 = pugno.
    /// </summary>
    public void Curl(Arm arm, float amount)
    {
        if (arm.Hand == null) return;
        amount = Mathf.Clamp01(amount);
        var fingers = arm.Hand.rotation * arm.FingerLocal;
        var palm = arm.Hand.rotation * arm.PalmLocal;
        Vector3 inside = arm.Hand.position + fingers * 0.055f + palm * 0.045f;
        foreach (var f in arm.Fingers) Bend(f, inside, amount);
        // il pollice si chiude sopra l'indice
        Vector3 thumbTo = arm.Hand.position + fingers * 0.07f + palm * 0.03f;
        foreach (var f in arm.Thumb) Bend(f, thumbTo, amount * 0.8f);
    }

    private static void Bend(Transform f, Vector3 target, float amount)
    {
        Quaternion open = f.rotation;
        Vector3 dir = target - f.position;
        if (dir.sqrMagnitude < 1e-6f) return;
        // le ossa di Blender puntano lungo il loro +Y
        var aimed = Quaternion.FromToRotation(f.up, dir.normalized) * f.rotation;
        f.rotation = Quaternion.Slerp(open, aimed, amount);
    }

    // ------------------------------------------------------------------ prima persona

    /// <summary>
    /// Lascia nel mesh SOLO le braccia (maniche comprese, spalle e busto esclusi) e le stacca
    /// dal corpo: il peso che i vertici tenuti avevano su clavicola e petto passa al braccio
    /// dello stesso lato. Così in prima persona le spalle si possono mettere dove serve
    /// (sotto il bordo dello schermo) senza stirare il mesh.
    ///
    /// Si tiene un triangolo se tutti i suoi vertici pesano almeno 0,6 sulle ossa del braccio
    /// E stanno entro 10 cm dall'asse di braccio/avambraccio a riposo (fuori restano le
    /// cinghie dello zaino che passano sulle spalle). glTFast lascia i mesh leggibili.
    /// </summary>
    public bool KeepOnlyArms()
    {
        if (Skin == null || Skin.sharedMesh == null || !Skin.sharedMesh.isReadable) return false;
        var src = Skin.sharedMesh;
        var bones = Skin.bones;
        var bind = src.bindposes;
        if (bind == null || bind.Length != bones.Length) return false;

        var isArm = new bool[bones.Length];
        var upperOf = new int[bones.Length];       // per ogni osso del braccio: il "braccio" del suo lato
        int upperL = -1, upperR = -1;
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null) continue;
            string n = Norm(bones[i].name);
            if (n == "arml") upperL = i;
            if (n == "armr") upperR = i;
        }
        if (upperL < 0 || upperR < 0) return false;
        for (int i = 0; i < bones.Length; i++)
        {
            upperOf[i] = -1;
            if (bones[i] == null) continue;
            string n = Norm(bones[i].name);
            isArm[i] = n.StartsWith("arm") || n.StartsWith("fore") || n.StartsWith("hand") ||
                       n.StartsWith("thumb") || n.StartsWith("index") || n.StartsWith("middle") ||
                       n.StartsWith("ring") || n.StartsWith("pinky");
            if (isArm[i]) upperOf[i] = n.EndsWith("l") ? upperL : upperR;
        }

        // assi delle braccia a riposo, nello spazio del mesh (dalla bind pose)
        Vector3 Head(string name)
        {
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null && Norm(bones[i].name) == name) return bind[i].inverse.GetColumn(3);
            return new Vector3(float.NaN, 0f, 0f);
        }
        var segs = new List<(Vector3 a, Vector3 b)>();
        foreach (var sd in new[] { "l", "r" })
        {
            Vector3 up = Head("arm" + sd), el = Head("fore" + sd), wr = Head("hand" + sd);
            if (float.IsNaN(up.x) || float.IsNaN(el.x) || float.IsNaN(wr.x)) return false;
            segs.Add((up, el));
            segs.Add((el, wr));
        }

        var verts = src.vertices;
        var weights = src.boneWeights;
        var ok = new bool[weights.Length];
        for (int v = 0; v < weights.Length; v++)
        {
            var w = weights[v];
            float s = 0f;
            int arm = -1;
            void Take(int bi, float wt)
            {
                if (bi < isArm.Length && isArm[bi]) { s += wt; if (arm < 0 && wt > 0f) arm = bi; }
            }
            Take(w.boneIndex0, w.weight0); Take(w.boneIndex1, w.weight1);
            Take(w.boneIndex2, w.weight2); Take(w.boneIndex3, w.weight3);
            if (s < 0.6f || arm < 0) continue;

            float dist = float.MaxValue;
            foreach (var (a, b) in segs) dist = Mathf.Min(dist, SegDist(verts[v], a, b));
            bool handBone = Norm(bones[arm].name).StartsWith("hand") || !Norm(bones[arm].name).StartsWith("arm") && !Norm(bones[arm].name).StartsWith("fore");
            if (dist > 0.10f && !(handBone && s > 0.999f)) continue;
            ok[v] = true;

            // il peso fuori dal braccio va al braccio dello stesso lato
            int up = upperOf[arm];
            if (w.boneIndex0 >= isArm.Length || !isArm[w.boneIndex0]) w.boneIndex0 = up;
            if (w.boneIndex1 >= isArm.Length || !isArm[w.boneIndex1]) w.boneIndex1 = up;
            if (w.boneIndex2 >= isArm.Length || !isArm[w.boneIndex2]) w.boneIndex2 = up;
            if (w.boneIndex3 >= isArm.Length || !isArm[w.boneIndex3]) w.boneIndex3 = up;
            weights[v] = w;
        }

        var mesh = Object.Instantiate(src);
        mesh.name = src.name + " (braccia)";
        mesh.boneWeights = weights;
        for (int sm = 0; sm < mesh.subMeshCount; sm++)
        {
            var tris = mesh.GetTriangles(sm);
            var keep = new List<int>(tris.Length / 4);
            for (int i = 0; i + 2 < tris.Length; i += 3)
                if (ok[tris[i]] && ok[tris[i + 1]] && ok[tris[i + 2]])
                {
                    keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]);
                }
            mesh.SetTriangles(keep, sm);
        }
        Skin.sharedMesh = mesh;
        return true;
    }

    private static float SegDist(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-9f, ab.sqrMagnitude));
        return Vector3.Distance(p, a + ab * t);
    }
}
