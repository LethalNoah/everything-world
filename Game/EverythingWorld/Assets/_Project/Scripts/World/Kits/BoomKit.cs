using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Boom Bap Harbour (WORLD_GUIDE.md): the Boroughs' one sunny island, on a hot summer afternoon. You arrive at the
    /// top of the Block, a street of brownstones with stoops, and it runs downhill to the harbour. Half way down, an
    /// elevated train line crosses on rusted green iron legs, the train rumbling over every minute, and under it the
    /// street is shut for a block party: a DJ on a flatbed, speaker stacks, a hydrant spraying, bunting, an ice-cream
    /// truck. At the bottom: a scrap of beach, a pier, stacked shipping containers, a crane, an orange ferry.
    /// It is a party, and it is hard too: iron, concrete, chain-link, graffiti.
    /// Laid out on purpose (the build-land standard): BoomTown.cs has the places and the people.
    public partial class BoomKit : LandKit
    {
        public override string Land => "Boom Bap Harbour";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Gold = new Color(1f, .78f, .25f), Red = new Color(.85f, .2f, .16f), Green = new Color(.18f, .58f, .3f), Cream = new Color(.96f, .93f, .84f), Ink = new Color(.09f, .09f, .1f), Sky = new Color(.3f, .6f, 1f), Yellow = new Color(1f, .9f, .15f);
        static readonly Color[] Browns = { new Color(.48f, .3f, .22f), new Color(.4f, .25f, .2f), new Color(.56f, .36f, .26f), new Color(.34f, .22f, .2f), new Color(.6f, .44f, .34f) };
        static readonly Color[] Tags = { new Color(1f, .3f, .5f), new Color(.2f, .8f, .9f), new Color(1f, .85f, .2f), new Color(.5f, .9f, .3f), new Color(.7f, .4f, 1f), new Color(1f, .5f, .15f) };
        const float PathW = 3f, StreetW = 8f, ElOff = 66f, ElH = 8.6f, HouseW = 8f, HouseD = 9f;

        Material asphalt, walk, concrete, steelEl, iron, dark, white, woodM, chain, glassM, windowM, bulbM, goldM, redM, blueM, leafM, trunkM, sandM;
        GlowHalos halos; Vector3 center, D, S; BoomLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(1, 0), S2 = new Vector2(0, 1), beach2; float plazaY, waterY, roadL, elL;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Solid(new Color(.3f, .3f, .32f)); walk = WorldMaterials.Solid(new Color(.72f, .7f, .66f)); concrete = WorldMaterials.Solid(new Color(.62f, .6f, .57f));
            steelEl = WorldMaterials.Solid(new Color(.2f, .32f, .28f), .5f); iron = WorldMaterials.Solid(new Color(.14f, .14f, .16f), .5f); dark = WorldMaterials.Solid(new Color(.07f, .07f, .08f)); white = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
            woodM = WorldMaterials.Solid(new Color(.5f, .36f, .24f)); chain = WorldMaterials.Solid(new Color(.6f, .62f, .64f), .7f); glassM = WorldMaterials.Solid(new Color(.2f, .26f, .32f), .9f);
            leafM = WorldMaterials.Solid(new Color(.28f, .5f, .26f)); trunkM = WorldMaterials.Solid(new Color(.34f, .26f, .2f)); sandM = WorldMaterials.Solid(new Color(.9f, .82f, .62f));
            // the windows are dark glass by day; "Empire State of Mind" lights every one of them
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .85f, .5f), .12f)) { name = "Boom windows" };
            bulbM = new Material(WorldMaterials.Glow(new Color(1f, .9f, .6f), 1.4f)) { name = "Boom bulbs" }; goldM = new Material(WorldMaterials.Glow(Gold, .9f)) { name = "Boom gold" };
            redM = new Material(WorldMaterials.Glow(new Color(1f, .1f, .1f), .4f)) { name = "Boom siren red" }; blueM = new Material(WorldMaterials.Glow(new Color(.1f, .3f, 1f), .4f)) { name = "Boom siren blue" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Solid(c);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Wu-Tang Clan": return 16f;
                case "The Notorious B.I.G.": case "Outkast": return 13f;
                case "Kurtis Blow": return 12f;
                case "JAŸ-Z": return 11f;
                default: return HasStation(a) ? 12f : 9.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // vacant lots and worn grass; sand at the water's edge
            if (height < 3.2f) color = Color.Lerp(new Color(.88f, .8f, .6f), new Color(.8f, .72f, .54f), patch);
            else if (slope > 26f) color = Color.Lerp(new Color(.42f, .4f, .38f), new Color(.5f, .47f, .44f), patch);
            else color = patch > .55f ? Color.Lerp(new Color(.4f, .56f, .3f), new Color(.34f, .5f, .28f), patch) : Color.Lerp(new Color(.58f, .55f, .5f), new Color(.66f, .62f, .55f), Mathf.SmoothStep(0f, 1f, patch / .55f));
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float d, float s) => heart2 + D2 * d + S2 * s;
        Vector3 Wp(float d, float s) => center + D * d + S * s;
        Vector3 G(LandContext ctx, float d, float s) { var p = Wp(d, s); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the Block runs downhill to the nearest shore that is far enough away to hold a street
            var best = float.MaxValue;
            for (var k = 0; k < 8; k++)
            {
                var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 60f;
                while (len < 340f && data.IslandAt(heart2.x + dir.x * (len + 30f), heart2.y + dir.y * (len + 30f)) == isl && data.HeightAt(heart2.x + dir.x * len, heart2.y + dir.y * len) > waterY + 3f) len += 6f;
                if (len < 170f || len >= 340f) continue; if (!Inside(heart2 + dir * ElOff, 70f)) continue;
                if (len < best) { best = len; D2 = dir; roadL = len; }
            }
            if (best == float.MaxValue) { D2 = new Vector2(1, 0); roadL = 60f; while (roadL < 300f && Inside(heart2 + D2 * (roadL + 10f), 30f)) roadL += 10f; }
            S2 = new Vector2(-D2.y, D2.x); beach2 = heart2 + D2 * (roadL + 14f);
            elL = 30f; while (elL < 110f && Inside(P2(ElOff, elL + 10f), 22f) && Inside(P2(ElOff, -(elL + 10f)), 22f)) elL += 10f;
            plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);

            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // at the top of the Block
            Put("The Notorious B.I.G.", -12f, 48f, 28f); Put("Diddy", -50f, 30f, 22f); Put("Outkast", -14f, -50f, 28f); Put("Beastie Boys", -62f, -22f, 22f);
            Put("JAŸ-Z", 28f, -86f, 24f); Put("Fugees", 34f, 88f, 24f); Put("AZ", -58f, 86f, 22f); Put("Shabazz Palaces", -66f, -78f, 22f); Put("Ceren Yaldız", -8f, -112f, 22f);
            // below the El
            Put("Kurtis Blow", 114f, 42f, 26f); Put("A Tribe Called Quest", 114f, -42f, 24f); Put("T La Rock", 90f, 92f, 22f); Put("Cannibal Ox", 88f, -96f, 22f);
            if (roadL > 190f) Put("Grandmaster Flash", 164f, 44f, 22f);
            // the harbour
            Put("Wu-Tang Clan", roadL - 8f, -48f, 26f); Put("RZA", roadL - 56f, -78f, 22f);
            var avoid = new List<Vector3> { new Vector3(beach2.x, 30f, beach2.y) };
            for (var d = 0f; d <= roadL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 13f, heart2.y + D2.y * d));
            for (var s = -elL; s <= elL; s += 22f) { var p = P2(ElOff, s); avoid.Add(new Vector3(p.x, 14f, p.y)); }
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);

            Level(data, index, heart2, 20f, 40f, plazaY);
            // the block party's patch of street under the El: level, so the El can stand on it
            var party = P2(ElOff, 0); Level(data, index, party, 22f, 46f, Mathf.Max(data.HeightAt(party.x, party.y), waterY + 4f));
            Level(data, index, beach2, 18f, 44f, waterY + 1.7f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend, float level)
        {
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Ease(0f, 1f, (d - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Boom Bap life").AddComponent<BoomLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.windows = windowM; life.bulbs = bulbM; life.gold = goldM; life.sirenRed = redM; life.sirenBlue = blueM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            TopOfTheBlock(ctx, mk, solid);
            TheBlock(ctx, mk, solid);
            TheEl(ctx, mk, solid);
            BlockParty(ctx, mk, solid);
            Harbour(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Brownstones(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Boom Bap Harbour", ctx.Root, false);
            solid.BuildCollider("Boom Bap Harbour walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Boom Bap glow", ctx.Root); }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f); var n = Mathf.CeilToInt(within / 8f);
            for (var i = -n; i <= n; i++)
            for (var j = -n; j <= n; j++)
                if (pathCells.TryGetValue((cx + i, cz + j), out var l))
                    foreach (var p in l) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        bool LaneNear(float x, float z, float within) { foreach (var (p, w) in walkPoints) { if (w < 10) continue; float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; } return false; }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool InFootprint(LandContext ctx, float x, float z, float pad)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        /// Under the El's line (plus 'pad').
        bool UnderEl(float x, float z, float pad) { var rel = new Vector2(x, z) - heart2; var d = Vector2.Dot(rel, D2); var s = Vector2.Dot(rel, S2); return Mathf.Abs(d - ElOff) < 6f + pad && Mathf.Abs(s) < elL + 6f + pad; }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !UnderEl(x, z, pad) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            var n = pts.Count; var L = new Vector3[n]; var Rr = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] - side * (w / 2f * miter); Rr[i] = pts[i] + side * (w / 2f * miter);
                float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(Rr[i].x, Rr[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f);
                L[i].y = gl + bulge + lift; Rr[i].y = gr + bulge + lift;
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], Rr[i], Rr[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A cobra-head street light.
        void StreetLight(LandContext ctx, MeshKit mk, Vector3 g, Vector3 arm)
        {
            var up = Vector3.up; arm.y = 0; arm.Normalize(); mk.Prism(chain, g - up * .2f, up, .08f, 6f, 6); mk.Prism(chain, g + up * 5.7f, (arm + up * .12f).normalized, .05f, 1.8f, 5); mk.Box(chain, g + up * 5.9f + arm * 1.9f, new Vector3(.3f, .12f, .7f), Quaternion.LookRotation(arm)); mk.Box(bulbM, g + up * 5.83f + arm * 1.9f, new Vector3(.22f, .03f, .5f), Quaternion.LookRotation(arm)); ArtistBits.PostCollider(ctx.Root, g, 5.6f, .1f);
        }

        /// A boombox, built round its own middle, facing +z.
        void Boombox(MeshKit m, Color body, Material lit = null)
        {
            m.Box(Paint(body), new Vector3(0, .18f, 0), new Vector3(.7f, .36f, .18f), Quaternion.identity); foreach (var e in new[] { -.22f, .22f }) { m.Prism(dark, new Vector3(e, .16f, .09f), Vector3.forward, .12f, .02f, 12); m.Prism(chain, new Vector3(e, .16f, .1f), Vector3.forward, .05f, .02f, 8); }
            m.Box(dark, new Vector3(0, .26f, .095f), new Vector3(.18f, .1f, .01f), Quaternion.identity); m.Box(lit ?? chain, new Vector3(0, .32f, .095f), new Vector3(.3f, .02f, .01f), Quaternion.identity); m.Box(chain, new Vector3(0, .42f, 0), new Vector3(.56f, .03f, .03f), Quaternion.identity); foreach (var e in new[] { -.28f, .28f }) m.Box(chain, new Vector3(e, .38f, 0), new Vector3(.03f, .08f, .03f), Quaternion.identity);
        }

        /// A stack of speakers.
        void Speakers(Transform parent, MeshKit mk, Vector3 g, Quaternion rot, int high, Material cone, Color body)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; for (var k = 0; k < high; k++) { var p = g + up * (.6f + k * 1.2f); mk.Box(Paint(body), p, new Vector3(1.1f, 1.16f, .8f), rot); mk.Prism(dark, p + f * .4f - up * .12f, f, .4f, .03f, 14); mk.Prism(cone, p + f * .42f - up * .12f, f, .16f, .03f, 10); mk.Prism(dark, p + f * .4f + up * .38f, f, .13f, .03f, 10); }
            BoxCol(parent, g + up * (high * .6f), new Vector3(1.1f, high * 1.2f, .8f), rot);
        }

        /// A graffiti piece on a wall: fat blocks of colour, an outline, a highlight. 'c' is its middle, 'face' the way it looks.
        void Graffiti(MeshKit mk, Vector3 c, Vector3 face, float w, float h, int i)
        {
            var rot = Quaternion.LookRotation(face); var right = rot * Vector3.right; var up = Vector3.up; var n = 4 + i % 3; var rng = new System.Random(i * 7 + 1);
            for (var k = 0; k < n; k++) { var x = (-.5f + (k + .5f) / n) * w; var hh = h * (.6f + (float)rng.NextDouble() * .4f); var tilt = Quaternion.Euler(0, 0, (float)rng.NextDouble() * 24f - 12f); mk.Box(dark, c + right * x + face * .02f, new Vector3(w / n * 1.06f, hh + .14f, .02f), rot * tilt); mk.Box(Paint(Tags[(i + k) % Tags.Length]), c + right * x + face * .035f, new Vector3(w / n * .9f, hh, .02f), rot * tilt); mk.Box(white, c + right * (x - w / n * .22f) + up * hh * .22f + face * .05f, new Vector3(w / n * .16f, hh * .3f, .01f), rot * tilt); }
        }

        /// A length of chain-link fence between two points.
        void Fence(LandContext ctx, MeshKit mk, Vector3 a, Vector3 b)
        {
            var up = Vector3.up; var d = b - a; d.y = 0; var len = d.magnitude; var rot = Quaternion.LookRotation(d.normalized); var n = Mathf.Max(1, Mathf.RoundToInt(len / 2.6f));
            for (var k = 0; k <= n; k++) { var p = Vector3.Lerp(a, b, k / (float)n); p.y = ctx.Ground(p.x, p.z); mk.Prism(chain, p - up * .2f, up, .04f, 2.6f, 5); }
            var mid = (a + b) / 2f; mid.y = (ctx.Ground(a.x, a.z) + ctx.Ground(b.x, b.z)) / 2f; mk.Box(chain, mid + up * 2.36f, new Vector3(.04f, .04f, len), rot); for (var k = 0; k < 7; k++) mk.Box(chain, mid + up * (.3f + k * .32f), new Vector3(.015f, .015f, len), rot); BoxCol(ctx.Root, mid + up * 1.2f, new Vector3(.1f, 2.4f, len), rot);
        }

        /// A street tree in a square of earth.
        void StreetTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Box(Paint(new Color(.32f, .24f, .18f)), g + up * .06f, new Vector3(1.3f, .04f, 1.3f), Quaternion.identity); mk.Prism(trunkM, g - up * .2f, up, .16f * s, 3f * s, 6, .7f); ArtistBits.PostCollider(ctx.Root, g, 2.8f * s, .18f * s);
            for (var k = 0; k < 3; k++) mk.Blob(leafM, g + up * (3.6f + k * .5f) * s + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .8f * s, new Vector3(1.5f, 1.1f, 1.5f) * s, rng, .25f);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var n = 0;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort; if (end < PathW) return;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(1.4f, end * .03f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, walk0, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 14, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f) || UnderEl(lp.x, lp.z, 1f)) continue;
                    StreetLight(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s);
                }
                walk++;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        Material walk0 => walk;

        // ---------------------------------------------------------------- the rest of the island
        /// Vacant lots: trees, chain-link, burnt-out and working cars, tyre piles, a handball wall, pigeons on a wire.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, walls = 0, cars = 0, tyres = 0, palms = 0, hoops = 0; var rng = new System.Random(1993);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var shore = p.y < waterY + 4.5f;
                if (shore) { if (roll < .3f) { Palm(ctx, mk, p, .9f + s * .4f, rng); palms++; } continue; }
                if (roll < .2f) { StreetTree(ctx, mk, p, 1f + s * .5f, rng); trees++; }
                else if (roll < .25f) { var f = Quaternion.Euler(0, s * 360f, 0) * Vector3.forward; var rot = Quaternion.LookRotation(f); mk.Box(concrete, p + Vector3.up * 1.4f, new Vector3(6f, 3.4f, .3f), rot); BoxCol(ctx.Root, p + Vector3.up * 1.4f, new Vector3(6f, 3.4f, .3f), rot); Graffiti(mk, p + Vector3.up * 1.5f + f * .16f, f, 5f, 2f, walls); Graffiti(mk, p + Vector3.up * 1.5f - f * .16f, -f, 5f, 2f, walls + 3); walls++; noGrass.Add(new Vector3(p.x, 3.4f, p.z)); }
                else if (roll < .28f) { if (cars < 6) { Car(ctx, mk, p, Quaternion.Euler(0, s * 360f, 0), cars, cars % 3 == 2); cars++; } }
                else if (roll < .32f) { for (var k = 0; k < 4; k++) mk.Prism(dark, p + Vector3.up * (.1f + k * .2f) + new Vector3((float)rng.NextDouble() * .2f, 0, (float)rng.NextDouble() * .2f), Vector3.up, .4f, .18f, 10); ArtistBits.PostCollider(ctx.Root, p, .9f, .42f); tyres++; }
                else if (roll < .335f) { if (hoops < 3) { Hoop(ctx, mk, p, FaceTo(p, NearestWalk(p))); hoops++; } }
            }
            Debug.Log($"Boom Bap Harbour: the Block {roadL:0} m, the El {elL * 2f:0} m, {trees} trees, {walls} graffiti walls, {cars} cars, {tyres} tyre piles, {palms} palms, {hoops} hoops");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        void Palm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var lean = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward * .16f; var p = g - up * .3f; var d = (up + lean).normalized;
            for (var k = 0; k < 5; k++) { var len = 1.4f * s; mk.Prism(trunkM, p, d, (.2f - k * .02f) * s, len + .05f, 6, .9f); p += d * len; d = (d + lean * .3f).normalized; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; mk.Box(leafM, p + fd * 1.2f * s - up * .2f * s, new Vector3(.5f * s, .05f, 2.6f * s), Quaternion.LookRotation(fd) * Quaternion.Euler(22f, 0, 0)); }
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s);
        }

        /// A boxy old sedan; 'wreck' strips it to primer and puts it on blocks.
        void Car(LandContext ctx, MeshKit mk, Vector3 g, Quaternion rot, int i, bool wreck = false)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; var s = rot * Vector3.right; var body = Paint(wreck ? new Color(.5f, .34f, .26f) : Tags[(i * 2 + 1) % Tags.Length] * .8f);
            mk.Box(body, g + up * .7f, new Vector3(1.8f, .6f, 4.5f), rot); mk.Box(body, g + up * 1.24f - f * .2f, new Vector3(1.6f, .56f, 2.3f), rot); mk.Box(wreck ? dark : glassM, g + up * 1.26f - f * .2f, new Vector3(1.64f, .4f, 2f), rot);
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1.4f, 1.4f }) { if (wreck) mk.Box(concrete, g + up * .2f + s * e * .7f + f * q, new Vector3(.4f, .4f, .4f), rot); else mk.Prism(dark, g + up * .36f + s * e * .82f + f * q, s * e, .36f, .2f, 10); }
            if (!wreck) { mk.Box(chain, g + up * .5f + f * 2.28f, new Vector3(1.8f, .14f, .08f), rot); mk.Box(chain, g + up * .5f - f * 2.28f, new Vector3(1.8f, .14f, .08f), rot); }
            BoxCol(ctx.Root, g + up * .8f, new Vector3(1.8f, 1.5f, 4.5f), rot); noGrass.Add(new Vector3(g.x, 3f, g.z));
        }

        /// A basketball hoop on a pole with a backboard.
        void Hoop(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Prism(chain, g - up * .2f, up, .08f, 3.7f, 6); mk.Box(chain, g + up * 3.4f + face * .4f, new Vector3(.08f, .08f, .8f), rot); mk.Box(white, g + up * 3.5f + face * .8f, new Vector3(1.8f, 1.1f, .05f), rot); mk.Box(Paint(Red), g + up * 3.3f + face * .83f, new Vector3(.6f, .45f, .02f), rot); mk.Box(white, g + up * 3.3f + face * .84f, new Vector3(.5f, .35f, .02f), rot);
            for (var k = 0; k < 10; k++) { var a = k * 36f * Mathf.Deg2Rad; mk.Box(Paint(new Color(1f, .45f, .1f)), g + up * 3.05f + face * 1.08f + (rot * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))) * .23f, new Vector3(.15f, .03f, .03f), rot * Quaternion.Euler(0, -k * 36f + 90f, 0)); } ArtistBits.PostCollider(ctx.Root, g, 3.6f, .1f);
        }

        /// Worn grass and weeds in the lots; nothing on the sand.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, .8f, new Color(.4f, .56f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1994);
            const float st = 2.8f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 6.6f) || UnderEl(px, pz, 0f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                var gy = ctx.Ground(px, pz); if (gy < waterY + 3.4f) continue;
                if (Mathf.PerlinNoise(px * .05f + 4f, pz * .05f + 6f) < .5f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.36f, .5f, .26f), new Color(.52f, .58f, .3f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, gy - .04f, pz), .22f + (float)rng.NextDouble() * .3f, col, rng.NextDouble() < .1 ? new Color(1f, .9f, .3f) : Color.Lerp(col, Cream, .3f), rng, 3, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Boom Bap weeds", ctx.Root, mat);
        }
    }
}
