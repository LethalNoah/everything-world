using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Shoegaze Fens (WORLD_GUIDE.md): a reverb-drowned marsh at dusk. Where you arrive, the hills fall away
    /// into a wide shallow fen of still, glowing water; a ruined cathedral stands half sunk in the middle of it,
    /// its stained glass lit from inside. Every artist has a little island of moss in the marsh, joined to the
    /// cathedral by old stone causeways or by giant guitar pedals laid as stepping stones. Weeping willows,
    /// reed beds, lily pads with glowing flowers, amps half sunk in the water, petals drifting, rings of light
    /// spreading over the water with the music. Dusty pink, lavender haze, fen green, pale fog, heavy bloom.
    /// Unlike the cold grey Driftwood Shore it is warm, saturated and dreamy, a little cheesy and a little heavy.
    /// When Slowdive's "When the Sun Hits" plays, the sun breaks through and floods the fen with gold.
    /// Files: FensKit (the marsh, causeways, pedals, trails, ground cover), FensCathedral, FensArtists,
    /// FensWilds, FensLife.
    public partial class FensKit : LandKit
    {
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) =>
            facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);
        public override string Land => "The Shoegaze Fens";
        public override (string song, string label, string artist)[] SpecialSongs => new[] { (SunSong, "When the Sun Hits", "Slowdive") };

        static readonly Color DustyPink = new Color(.84f, .65f, .7f), Lavender = new Color(.65f, .61f, .78f), FenGreen = new Color(.44f, .55f, .48f), PaleFog = new Color(.91f, .9f, .93f);
        static readonly Color Mud = new Color(.27f, .22f, .27f), Moss = new Color(.42f, .52f, .42f), MossPink = new Color(.6f, .55f, .58f);
        static readonly Color HotPink = new Color(1f, .35f, .7f), Violet = new Color(.68f, .42f, 1f), Lilac = new Color(.85f, .7f, 1f), Gold = new Color(1f, .78f, .4f), Ink = new Color(.96f, .93f, .97f);
        static readonly Color[] Glows = { new Color(1f, .35f, .7f), new Color(.68f, .42f, 1f), new Color(.5f, .7f, 1f), new Color(1f, .55f, .5f), new Color(.85f, .7f, 1f) };
        public const string SunSong = "0oxYB9GoOIDrdzniNdKC44";      // Slowdive, "When the Sun Hits"

        const float CathR = 34f, Blend = 55f, IslandLift = .35f, FloorDrop = .5f, WaterDrop = .05f, PathW = 2.6f;

        // set in ShapeGround, used when the terrain is coloured and when the land is dressed
        float waterY = 15f; Vector2 heart; readonly float[] edge = new float[64]; bool shaped;
        readonly List<Vector3> islands = new List<Vector3>();          // x, radius, z (the cathedral's first)

        Material stoneM, stoneDark, wood, iron, black, water, leafM, mossM, cloth;
        GlowHalos halos;
        Vector3 center;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        FensLife life;
        MeshKit soft;                                                   // leaves and blossom you walk through (no collider; the land audit skips "Soft" meshes)

        void MakeMaterials()
        {
            stoneM = WorldMaterials.Solid(new Color(.62f, .58f, .64f));
            stoneDark = WorldMaterials.Solid(new Color(.42f, .39f, .45f));
            wood = WorldMaterials.Solid(new Color(.36f, .3f, .3f));
            iron = WorldMaterials.Solid(new Color(.16f, .15f, .18f));
            black = WorldMaterials.Solid(new Color(.07f, .07f, .08f), .6f);
            water = new Material(WorldMaterials.Glow(new Color(.34f, .28f, .46f), .12f)) { name = "Fen water" };     // dark lilac, a faint glow: brighter and the fog washes it white
            water.SetFloat("_Smoothness", .55f);                                   // glossier mirrors the pale sky and reads white
            leafM = WorldMaterials.Solid(new Color(.52f, .6f, .5f));
            mossM = WorldMaterials.Solid(Moss);
            cloth = WorldMaterials.Solid(DustyPink);
        }

        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;

        /// The island an artist stands on (its radius): the radio stations biggest.
        static float IslandR(IslandWorldData.Artist a) => HasStation(a) ? 17f : a.songs >= 2 ? 13f : 10f;

        float Edge(float x, float z) => edge[Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(z - heart.y, x - heart.x) / (Mathf.PI * 2f) * 64f, 64f)) % 64];
        bool InMarsh(float x, float z, float margin = 0f) => shaped && Vector2.Distance(new Vector2(x, z), heart) < Edge(x, z) - margin;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (!shaped) { color = default; return false; }
            // only the fen's own floor is mud (it sits exactly half a metre under the water); low ground elsewhere,
            // like the coast, is ordinary hillside, or it showed up as a black band. The floor is dead flat, so a
            // sloping coast facet that only passes through the floor's height stays hillside (it read as a black
            // triangle or a dark crease in the hills)
            if (height > waterY - FloorDrop - .05f && height < waterY - .1f && slope < .05f) color = Color.Lerp(Mud, new Color(.33f, .27f, .34f), patch);
            else if (height >= waterY - .1f && height < waterY + 1f) color = Color.Lerp(Moss, MossPink, Mathf.SmoothStep(0, 1, patch));
            else if (height < 4f) color = Color.Lerp(new Color(.7f, .64f, .62f), new Color(.62f, .58f, .6f), patch);
            else if (slope > 6f) color = Color.Lerp(new Color(.5f, .47f, .52f), new Color(.6f, .56f, .6f), patch);
            else color = Color.Lerp(FenGreen, Color.Lerp(FenGreen, DustyPink, .45f), Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        /// Gather the artists round the cathedral (radio stations first, on different sides), then sink a wide
        /// shallow fen round them all: the floor half a metre under the water, each artist on a low island of
        /// moss, the cathedral on its own, and the hills rising gently from the water's edge. The fen stops
        /// well short of the next lands, so nothing changes at the borders.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var sid = isl.sceneId;
            heart = new Vector2(isl.labelX, isl.labelZ);
            waterY = data.HeightAt(heart.x, heart.y);
            int Id(float x, float z)
            {
                var cc = Mathf.RoundToInt(x / data.Step); var rr = Mathf.RoundToInt((data.layout.depth - z) / data.Step);
                if (cc < 0 || rr < 0 || cc >= data.Nx || rr >= data.Nz) return 255;
                var i = rr * data.Nx + cc;
                return data.heights[i] > 1.6f ? data.islandIds[i] : 254;
            }
            // how far the land goes in each direction before another land or the sea
            var border = new float[64];
            for (var k = 0; k < 64; k++)
            {
                var a = k * Mathf.PI * 2f / 64f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                border[k] = 600f;
                for (var s = 8f; s < 600f; s += 4f) if (Id(heart.x + d.x * s, heart.y + d.y * s) != index) { border[k] = s; break; }
            }
            float BorderAt(Vector2 p) => border[Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(p.y - heart.y, p.x - heart.x) / (Mathf.PI * 2f) * 64f, 64f)) % 64];

            // the artists, as close round the cathedral as they fit
            var arts = new List<IslandWorldData.Artist>();
            foreach (var a in data.layout.artists) if (a.sceneId == sid) arts.Add(a);
            arts.Sort((p, q) => HasStation(p) != HasStation(q) ? (HasStation(p) ? -1 : 1) : p.songs != q.songs ? q.songs.CompareTo(p.songs) : string.CompareOrdinal(p.name, q.name));
            var placed = new List<Vector3>(); var stations = new List<Vector2>();
            islands.Clear(); islands.Add(new Vector3(heart.x, CathR, heart.y));
            foreach (var a in arts)
            {
                var best = float.MaxValue; var at = Vector2.zero;
                var need = Mathf.Max(a.r, IslandR(a) + 6f);
                for (var r = CathR + 8f + need; r < 330f; r += 4f)
                {
                    var steps = Mathf.Max(24, Mathf.RoundToInt(r * Mathf.PI * 2f / 6f));
                    for (var k = 0; k < steps; k++)
                    {
                        var ang = k * Mathf.PI * 2f / steps + r * .013f;
                        var cand = heart + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                        if (r + IslandR(a) + Blend * .5f + 45f > BorderAt(cand)) continue;          // keep the fen well inside the land
                        var ok = true;
                        foreach (var p in placed) if (Vector2.Distance(cand, new Vector2(p.x, p.z)) < p.y + need + 6f) { ok = false; break; }
                        if (!ok) continue;
                        var score = r;
                        if (HasStation(a)) foreach (var s in stations) score -= Mathf.Min(Vector2.Distance(cand, s), 200f) * .4f;
                        if (score < best) { best = score; at = cand; }
                    }
                    if (best < float.MaxValue && !HasStation(a) && r > best + 30f) break;
                }
                if (best == float.MaxValue) { Debug.LogWarning("The Shoegaze Fens: no room in the fen for " + a.name); continue; }
                a.x = at.x; a.z = at.y;
                placed.Add(new Vector3(at.x, need, at.y));
                islands.Add(new Vector3(at.x, IslandR(a), at.y));
                if (HasStation(a)) stations.Add(at);
            }

            // the water's edge: round everything, gently wavy, never near the border
            for (var k = 0; k < 64; k++)
            {
                var a = k * Mathf.PI * 2f / 64f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var reach = CathR + 20f;
                foreach (var p in islands)
                {
                    var rel = new Vector2(p.x, p.z) - heart; var along = Vector2.Dot(rel, d);
                    var off = Mathf.Abs(rel.x * d.y - rel.y * d.x);
                    if (along > 0 && off < p.y + 30f) reach = Mathf.Max(reach, along + Mathf.Sqrt(Mathf.Max(0f, (p.y + 30f) * (p.y + 30f) - off * off)) * .8f + 10f);
                }
                reach *= 1f + .06f * Mathf.Sin(a * 3f + 1.3f) + .04f * Mathf.Sin(a * 7f + .4f);
                edge[k] = Mathf.Min(reach, border[k] - Blend - 30f);
            }
            for (var k = 0; k < 64; k++) edge[k] = Mathf.Max(edge[k], Mathf.Min(edge[(k + 63) % 64], edge[(k + 1) % 64]) * .9f);
            shaped = true;

            var maxR = 0f; foreach (var e in edge) maxR = Mathf.Max(maxR, e);
            var reachAll = maxR + Blend + 5f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                if (Mathf.Abs(z - heart.y) > reachAll) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step;
                    if (Mathf.Abs(x - heart.x) > reachAll) continue;
                    var i = r * data.Nx + col;
                    if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart); var e = Edge(x, z);
                    if (d > e + Blend) continue;
                    float h;
                    if (d < e)
                    {
                        h = waterY - FloorDrop;
                        foreach (var p in islands)
                        {
                            var di = Vector2.Distance(new Vector2(x, z), new Vector2(p.x, p.z));
                            if (di < p.y) h = Mathf.Max(h, waterY + IslandLift);
                            else if (di < p.y + 6f) h = Mathf.Max(h, Mathf.Lerp(waterY + IslandLift, waterY - FloorDrop, (di - p.y) / 6f));
                        }
                    }
                    else h = Mathf.Lerp(waterY + .4f, Mathf.Max(data.heights[i], waterY + .4f), Mathf.SmoothStep(0, 1, (d - e) / Blend));
                    data.heights[i] = h;
                }
            }
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Fens life").AddComponent<FensLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.waterY = waterY;
            var mk = new MeshKit(); var solid = new MeshKit(); soft = new MeshKit();
            Water(ctx, mk);
            Cathedral(ctx, mk, solid);
            Causeways(ctx, mk, solid);
            Trails(ctx, mk, solid);
            SongPlaces(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Fens scenery", ctx.Root, false);
            soft.Build("Soft foliage", ctx.Root, false);
            solid.BuildCollider("Fens walls", ctx.Root);
        }

        public override void Finish(LandContext ctx)
        {
            GroundCover(ctx);
            halos.Build("Fens glow", ctx.Root);
        }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f);
            for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (pathCells.TryGetValue((cx + i, cz + j), out var l))
                    foreach (var p in l) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        bool NearOtherWalk(Vector3 q, int walk, float within)
        {
            foreach (var (p, w) in walkPoints)
            {
                if (w == walk) continue;
                float dx = p.x - q.x, dz = p.z - q.z;
                if (dx * dx + dz * dz < within * within) return true;
            }
            return false;
        }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        /// On an island (the cathedral's or an artist's), and how far inside its edge.
        bool OnIsland(float x, float z, float inside = 0f)
        {
            foreach (var p in islands) { float dx = x - p.x, dz = z - p.z, r = p.y - inside; if (r > 0 && dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool InArtist(LandContext ctx, float x, float z, float pad)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = IslandR(a) + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool Wet(LandContext ctx, float x, float z) => ctx.Ground(x, z) < waterY - .1f;

        /// Free for scenery: on this land, not on an artist's island or the cathedral's, off the paths.
        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 3f) && !OnIsland(x, z, -pad) && !NearPath(x, z, pad + 1.6f) && !InNoGrass(x, z, pad);

        Material GlowM(Color c, float s = 2.4f) => WorldMaterials.Glow(c, s * .7f);      // kept low so colours stay saturated under the heavy bloom

        /// A lamp of the fens: a slim black post with a curled top and a glowing orb hanging from it.
        Renderer Lamp(LandContext ctx, MeshKit mk, Transform parent, Vector3 ground, Color glow, float h = 2.8f)
        {
            mk.Prism(iron, ground - Vector3.up * .3f, Vector3.up, .06f, h + .3f, 6);
            var top = ground + Vector3.up * h;
            var arm = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
            mk.Box(iron, top + arm * .3f, new Vector3(.04f, .04f, .6f), Quaternion.LookRotation(arm));
            mk.Box(iron, top + arm * .58f - Vector3.up * .15f, new Vector3(.02f, .3f, .02f), Quaternion.identity);
            var go = new GameObject("Lamp orb"); go.transform.SetParent(parent, false);
            var orb = new MeshKit(); orb.Blob(GlowM(glow, 3f), Vector3.zero, Vector3.one * .2f, ctx.Rng, 0f);
            var built = orb.Build("Orb", go.transform, false);
            go.transform.position = top + arm * .58f - Vector3.up * .45f;
            halos.Add(go.transform.position, 1.6f, glow, 1f);
            ArtistBits.PostCollider(ctx.Root, ground, h, .1f);
            return built.GetComponent<Renderer>();
        }

        // ---------------------------------------------------------------- the water
        /// One sheet of still water over the whole fen (the islands and banks rise through it).
        void Water(LandContext ctx, MeshKit mk)
        {
            var maxR = 0f; foreach (var e in edge) maxR = Mathf.Max(maxR, e);
            var c = new Vector3(heart.x, waterY - WaterDrop, heart.y);
            Vector3 Rim(int k) { var a = k * Mathf.PI * 2f / 64f; var r = edge[k % 64] + 6f; return c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); }
            for (var k = 0; k < 64; k++) mk.Tri(water, c, Rim(k), Rim(k + 1), c - Vector3.up);
            life.waterRadius = maxR;
            life.islands = new List<Vector3>(islands);
        }

        // ---------------------------------------------------------------- causeways and pedal stepping stones
        /// From a ring round the cathedral's island, a causeway or a line of giant guitar pedals out to every
        /// artist's island, each joining the network where it's closest, so they branch and never cross.
        void Causeways(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var ring = new List<Vector3>();
            for (var k = 0; k < 48; k++)
            {
                var a = k * Mathf.PI * 2f / 48f;
                ring.Add(new Vector3(heart.x + Mathf.Cos(a) * (CathR - 4f), waterY, heart.y + Mathf.Sin(a) * (CathR - 4f)));
            }
            foreach (var p in ring) Remember(p, 0);
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            var walkId = 1; var lamps = new List<Vector3>();
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, _) in walkPoints)
                    {
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - IslandR(a);
                        if (d < best) { best = d; next = a; join = p; }
                    }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 1f) continue;
                dir /= total;
                facing[next.name] = -dir;
                // from the edge of what it joins to just onto the artist's island
                var start = join == ring[0] || walkPoints.FindIndex(w => w.p == join && w.w == 0) >= 0 ? 0f : PathW / 2f + .1f;
                var end = total - IslandR(next) + 2.5f;
                var pts = new List<Vector3>();
                for (var d = start; ; d += 2.5f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd; q.y = waterY;
                    if (dd > start + 4f && dd < end - 3f && NearOtherWalk(q, walkId, PathW + .8f)) break;
                    pts.Add(q);
                    if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                var pedals = walkId % 3 == 0 && total < 70f;
                if (pedals) PedalPath(ctx, mk, solid, pts); else Causeway(mk, solid, pts);
                foreach (var p in pts) Remember(p, walkId);
                // lamps along the longer ones
                for (var i = 3; i < pts.Count - 2; i += 7)
                {
                    var side = Vector3.Cross(Vector3.up, dir) * ((i / 7) % 2 == 0 ? 1f : -1f);
                    var lp = pts[i] + side * (PathW / 2f + .5f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 18f * 18f) near = true;
                    if (near || NearOtherWalk(lp, walkId, PathW)) continue;
                    lamps.Add(lp);
                    var g = new Vector3(lp.x, Mathf.Max(ctx.Ground(lp.x, lp.z), waterY - FloorDrop), lp.z);
                    var glow = Glows[lamps.Count % Glows.Length];
                    Lamp(ctx, mk, ctx.Root, g, glow, 3f + (waterY - g.y));
                    if (lamps.Count % 3 == 0) ArtistBits.PointLight(ctx.Root, g + Vector3.up * 2.6f, glow, 12f, 1.3f);
                }
                walkId++;
            }
        }

        /// An old stone causeway just above the water: one seamless strip (mitred at the ends of each piece),
        /// mossy on top, with stone sides down into the water and a collider.
        void Causeway(MeshKit mk, MeshKit solid, List<Vector3> pts)
        {
            var top = waterY + .3f; var n = pts.Count;
            var dir = pts[n - 1] - pts[0]; dir.y = 0; dir.Normalize();
            var side = Vector3.Cross(Vector3.up, dir);
            var a = pts[0]; var b = pts[n - 1]; a.y = b.y = top;
            var len = Vector3.Distance(a, b); var rot = Quaternion.LookRotation(dir);
            var mid = (a + b) / 2f;
            mk.Box(mossM, mid, new Vector3(PathW, .04f, len), rot);
            mk.Box(stoneDark, mid - Vector3.up * .55f, new Vector3(PathW + .2f, 1.06f, len), rot);
            solid.Box(stoneM, mid - Vector3.up * .5f, new Vector3(PathW, 1.04f, len), rot);
            // paving stones in the moss, and kerb stones along the edges
            for (var d = .6f; d < len - .4f; d += 1.2f)
            {
                var p = a + dir * d;
                mk.Box(stoneM, p + Vector3.up * .025f + side * ((int)(d / 1.2f) % 2 == 0 ? .35f : -.35f), new Vector3(1.1f, .03f, .9f), rot);
                foreach (var s in new[] { -1f, 1f })
                    mk.Box(stoneDark, p + side * s * (PathW / 2f + .02f) + Vector3.up * .05f, new Vector3(.25f, .14f, 1.1f), rot);
            }
        }

        /// Giant guitar pedals laid in a line as stepping stones across the water: chunky coloured boxes with
        /// knobs, a footswitch and a little glowing light, each slightly askew.
        void PedalPath(LandContext ctx, MeshKit mk, MeshKit solid, List<Vector3> pts)
        {
            Color[] paint = { new Color(.85f, .35f, .55f), new Color(.45f, .55f, .85f), new Color(.95f, .75f, .3f), new Color(.4f, .75f, .6f), new Color(.75f, .5f, .9f), new Color(.85f, .85f, .82f), new Color(.2f, .2f, .22f) };
            var dir = pts[pts.Count - 1] - pts[0]; dir.y = 0; dir.Normalize();
            var len = Vector3.Distance(new Vector3(pts[0].x, 0, pts[0].z), new Vector3(pts[pts.Count - 1].x, 0, pts[pts.Count - 1].z));
            var knob = WorldMaterials.Solid(new Color(.1f, .1f, .1f)); var metal = WorldMaterials.Solid(new Color(.75f, .75f, .78f), .8f);
            var k = 0;
            for (var d = 0f; d <= len; d += 1.55f, k++)
            {
                var p = pts[0] + dir * d; p.y = waterY;
                var rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, ctx.Range(-9f, 9f), 0);
                var col = paint[(k * 3 + Mathf.FloorToInt(p.x)) % paint.Length];
                var floor = waterY - FloorDrop; var topY = waterY + .3f;
                var c = new Vector3(p.x, (floor + topY) / 2f, p.z);
                var size = new Vector3(1.9f, topY - floor, 1.3f);
                mk.Box(WorldMaterials.Solid(col, .5f), c, size, rot);
                solid.Box(stoneM, c, size, rot);
                var top = new Vector3(p.x, topY, p.z);
                var fwd = rot * Vector3.forward; var rt = rot * Vector3.right;
                for (var j = 0; j < 3; j++) mk.Prism(knob, top + fwd * .38f + rt * (j - 1) * .5f, Vector3.up, .1f, .09f, 8);
                mk.Prism(metal, top - fwd * .25f, Vector3.up, .14f, .06f, 8);
                var led = top + fwd * .12f + rt * .55f;
                var ledCol = Glows[k % Glows.Length];
                mk.Prism(GlowM(ledCol, 3f), led, Vector3.up, .05f, .04f, 6);
                if (k % 2 == 0) halos.Add(led + Vector3.up * .05f, .35f, ledCol, .9f);
            }
        }

        // ---------------------------------------------------------------- trails into the hills
        /// A few mossy trails from the fen's edge up into the hills, each to something worth finding.
        void Trails(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var dirt = WorldMaterials.Solid(new Color(.5f, .44f, .46f));
            var spots = new List<Vector3>();
            for (var k = 0; k < 4; k++)
            {
                // the open hill farthest round from the others, 90-200 m beyond the water
                Vector3 bestP = default; var best = -1f;
                for (var tries = 0; tries < 300; tries++)
                {
                    var a = ctx.Range(0, Mathf.PI * 2f); var e = edge[Mathf.RoundToInt(a / (Mathf.PI * 2f) * 64f) % 64];
                    var r = e + ctx.Range(70f, 190f);
                    float x = heart.x + Mathf.Cos(a) * r, z = heart.y + Mathf.Sin(a) * r;
                    if (!ctx.IsLand(x, z, 40f) || NearPath(x, z, 20f)) continue;
                    var sep = 400f; foreach (var s in spots) sep = Mathf.Min(sep, Vector2.Distance(new Vector2(s.x, s.z), new Vector2(x, z)));
                    if (sep > best) { best = sep; bestP = ctx.OnGround(x, z); }
                }
                if (best < 90f) break;
                spots.Add(bestP);
            }
            life.hillSpots = spots;
            var id = 1000;
            foreach (var s in spots)
            {
                // from the nearest point of the fen's network
                Vector3 from = center; var bd = float.MaxValue;
                foreach (var (p, _) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(s.x, s.z)); if (d < bd) { bd = d; from = p; } }
                var dir = new Vector3(s.x - from.x, 0, s.z - from.z); var len = dir.magnitude - 9f; dir.Normalize();
                var pts = new List<Vector3>();
                var side = Vector3.Cross(Vector3.up, dir);
                for (var d = 2f; d < len; d += 3f)
                {
                    var q = from + dir * d + side * Mathf.Sin(d * .03f) * 6f;
                    if (InMarsh(q.x, q.z, -2f) && !OnIsland(q.x, q.z)) continue;          // the trail starts where the water ends
                    pts.Add(ctx.OnGround(q.x, q.z));
                }
                if (pts.Count < 3) continue;
                Strip(mk, solid, pts, dirt);
                for (var i = 0; i < pts.Count; i++) Remember(pts[i], id);
                for (var i = 4; i < pts.Count; i += 9)
                {
                    var lp = pts[i] + side * ((i / 9) % 2 == 0 ? 2.2f : -2.2f);
                    Lamp(ctx, mk, ctx.Root, ctx.OnGround(lp.x, lp.z), Glows[i % Glows.Length]);
                }
                id++;
            }
        }

        /// A trail laid on the ground as one seamless strip, mitred at the bends.
        void Strip(MeshKit mk, MeshKit solid, List<Vector3> pts, Material m)
        {
            const float w = 2.2f, lift = .06f;
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] + Vector3.up * lift - side * (w / 2f * miter); R[i] = pts[i] + Vector3.up * lift + side * (w / 2f * miter);
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        // ---------------------------------------------------------------- ground cover
        /// Soft grass and pink flowers on the hills, moss and tiny flowers on the islands, and tall reeds
        /// standing in the shallows round every island.
        void GroundCover(LandContext ctx)
        {
            var grassMat = WindMesh.Material(.3f, 1.1f, FenGreen);
            var reedMat = WindMesh.Material(.45f, .8f, new Color(.4f, .45f, .38f));
            var grass = new Dictionary<(int, int), WindMesh>(); var reeds = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(8181);
            const float step = 1.8f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 1.8f) || InNoGrass(px, pz, 0f) || InArtist(ctx, px, pz, -5f)) continue;
                var g = ctx.Ground(px, pz);
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (g < waterY - .1f && InMarsh(px, pz, -6f))        // low hillside out of the fen (the coast) gets grass, not reeds
                {
                    // reeds only in the shallows near an edge (an island or the bank)
                    if (!OnIsland(px, pz, -3.5f) && Vector2.Distance(new Vector2(px, pz), heart) < Edge(px, pz) - 4f) continue;
                    if (rng.NextDouble() < .55) continue;
                    if (!reeds.TryGetValue(key, out var wr)) reeds[key] = wr = new WindMesh();
                    var col = Color.Lerp(new Color(.33f, .42f, .33f), new Color(.48f, .52f, .42f), (float)rng.NextDouble());
                    // rooted in the bed and tall enough to stand out of the water (not floating at the water's height
                    // where a hollow dips below it with no water in it)
                    if (waterY - g > 2.5f) continue;   // too deep for reeds, or a dry hollow below the lake's level
                    var depth = waterY - g;
                    wr.Tuft(new Vector3(px, g - .04f, pz), depth + .9f + (float)rng.NextDouble() * .8f, col, Color.Lerp(col, DustyPink, .25f), rng, 3, .04f);
                    continue;
                }
                var n = Mathf.PerlinNoise(px * .025f + 2f, pz * .025f + 9f);
                if (n < .28f) continue;
                if (!grass.TryGetValue(key, out var wm)) grass[key] = wm = new WindMesh();
                var c2 = Color.Lerp(new Color(.4f, .52f, .44f), new Color(.6f, .6f, .58f), (float)rng.NextDouble());
                var at = new Vector3(px, g - .04f, pz);
                wm.Tuft(at, .35f + (float)rng.NextDouble() * .4f, c2, Color.Lerp(c2, Lilac, .35f), rng, 4, .07f);
                var fl = rng.NextDouble();
                if (fl < .1) wm.Flower(at + new Vector3(.2f, 0, .1f), .35f, DustyPink, rng);
                else if (fl < .16) wm.Flower(at + new Vector3(-.15f, 0, .2f), .4f, Lilac, rng);
                else if (fl < .19) wm.Flower(at + new Vector3(.1f, 0, -.2f), .3f, new Color(1f, .95f, .9f), rng);
            }
            foreach (var kv in grass) kv.Value.Build("Fen grass", ctx.Root, grassMat);
            foreach (var kv in reeds) kv.Value.Build("Reeds", ctx.Root, reedMat);
        }
    }
}
