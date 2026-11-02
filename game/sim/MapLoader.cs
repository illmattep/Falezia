using System;
using System.IO;
using Vec3 = System.Numerics.Vector3;

namespace Falezia.Sim
{
    /// <summary>
    /// Legge una mappa (formato 1, vedi tools/mapgen/calanera.py) e costruisce il
    /// <see cref="WorldState"/>. Riceve il TESTO, non un percorso: chi conosce il
    /// filesystem (Godot sul client e sul server, System.IO nei test) sta fuori da qui.
    /// </summary>
    public static class MapLoader
    {
        public const int SupportedFormat = 1;

        public static WorldState Parse(string json) => Parse(json, null);

        /// <summary>
        /// Come <see cref="Parse(string)"/>, più un sidecar opzionale con la collisione del
        /// vestito: `remove` toglie le scatole del blockout coperte dall'arte, `boxes` aggiunge
        /// le scatole delle mesh visibili. Il file base resta quello generato dalla mappa.
        /// </summary>
        public static WorldState Parse(string json, string artJson)
        {
            var root = Json.Parse(json);
            var remove = new System.Collections.Generic.HashSet<int>();
            var artBoxes = new System.Collections.Generic.List<Box>();

            if (!string.IsNullOrEmpty(artJson))
            {
                var art = Json.Parse(artJson);
                if (art.TryGet("remove", out var rem))
                    foreach (var e in rem.Array) remove.Add(e.Int());
                if (art.TryGet("boxes", out var ab))
                    foreach (var e in ab.Array) artBoxes.Add(ReadBox(e, root["kinds"].Count));
            }

            int format = root["format"].Int();
            if (format != SupportedFormat)
                throw new InvalidDataException($"formato mappa {format} non supportato (atteso {SupportedFormat})");

            var w = new WorldState { Name = root["name"].Text() };

            var kinds = root["kinds"];
            w.Kinds = new string[kinds.Count];
            for (int i = 0; i < kinds.Count; i++) w.Kinds[i] = kinds.Items[i].Text();

            int boxIndex = 0;
            foreach (var e in root["boxes"].Array)
            {
                if (!remove.Contains(boxIndex))
                {
                    var b = ReadBox(e, w.Kinds.Length);
                    w.AddBox(b.Min, b.Size, b.Kind);
                }
                boxIndex++;
            }

            foreach (var b in artBoxes) w.AddBox(b.Min, b.Size, b.Kind);

            if (root.TryGet("decor", out var decor))
                foreach (var e in decor.Array)
                    w.Decor.Add(ReadBox(e, w.Kinds.Length));

            if (root.TryGet("spawns", out var spawns))
            {
                if (spawns.TryGet("north", out var n))
                    foreach (var e in n.Array) w.SpawnsNorth.Add(ReadVec(e));
                if (spawns.TryGet("south", out var s))
                    foreach (var e in s.Array) w.SpawnsSouth.Add(ReadVec(e));
            }

            if (root.TryGet("points", out var points))
                foreach (var e in points.Array)
                    w.Points.Add(new NamedPoint
                    {
                        Name = e["name"].Text(),
                        Pos = ReadVec(e["pos"])
                    });

            if (root.TryGet("routes", out var routes))
                foreach (var e in routes.Array)
                {
                    var r = new Route { Name = e["name"].Text() };
                    if (e.TryGet("from", out var from)) r.From = ReadVec(from);
                    if (e.TryGet("waypoints", out var wps))
                        foreach (var wp in wps.Array)
                        {
                            // i waypoint sono coppie [x, z]: la quota la dà il pavimento
                            float x = wp.Items[0].Float();
                            float z = wp.Items[1].Float();
                            r.Waypoints.Add(new Vec3(x, r.From.Y, z));
                        }
                    if (r.Waypoints.Count > 0) w.Routes.Add(r);
                }

            // chi cade fuori dal mondo rientra al proprio lato del molo, non al centro
            if (w.SpawnsNorth.Count > 0) w.FallbackFeet = w.SpawnsNorth[0];
            return w;
        }

        private static Box ReadBox(Json e, int kindCount)
        {
            if (e.Count != 7)
                throw new InvalidDataException("una scatola ha 7 valori: min x,y,z · dimensioni x,y,z · tipo");
            var v = new float[7];
            for (int i = 0; i < 7; i++) v[i] = e.Items[i].Float();
            int kind = (int)v[6];
            if (kind < 0 || kind >= Math.Max(1, kindCount))
                throw new InvalidDataException($"tipo di scatola {kind} fuori elenco");
            if (v[3] <= 0f || v[4] <= 0f || v[5] <= 0f)
                throw new InvalidDataException("scatola con una dimensione nulla o negativa");
            return new Box { Min = new Vec3(v[0], v[1], v[2]), Size = new Vec3(v[3], v[4], v[5]), Kind = (byte)kind };
        }

        private static Vec3 ReadVec(Json e)
        {
            if (e.Count != 3) throw new InvalidDataException("un punto ha 3 coordinate");
            return new Vec3(e.Items[0].Float(), e.Items[1].Float(), e.Items[2].Float());
        }
    }
}
