using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Dubstep Shards (WORLD_GUIDE.md): a shattered crystal canyon at night with lasers. You arrive in the Drop Zone in
    /// front of the Speaker Cliff, a wall of speakers the size of a cliff face, its cones pumping, lasers sweeping from its
    /// top; giant crystal shards jut from the ground all round, glowing cyan and magenta and pulsing with the bass wobble.
    /// The Rift runs between leaning slabs of black rock veined with crystal to the Core: a vast crystal turning slowly in
    /// the air over a glowing crater. Black rock, deep violet, cyan and magenta light.
    /// Laid out on purpose (the build-land standard): ShardTown.cs has the places and the people.
    public partial class ShardKit : LandKit
    {
        public override string Land => "The Dubstep Shards";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Cyan = new Color(.2f, .9f, 1f), Magenta = new Color(1f, .2f, .8f), Violet = new Color(.5f, .2f, 1f), Lime = new Color(.6f, 1f, .2f), Ink = new Color(.04f, .03f, .06f), White = new Color(.9f, .92f, 1f);
        const float PathW = 3f, RiftW = 6f;

        Material rock, rockD, path, dark, iron, white, shardM, shardM2, lampM, coneM;
        GlowHalos halos; Vector3 center, D, S; ShardLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            rock = WorldMaterials.Glow(new Color(.22f, .2f, .28f), .08f); rockD = WorldMaterials.Glow(new Color(.12f, .1f, .16f), .06f); path = WorldMaterials.Glow(new Color(.18f, .16f, .22f), .06f); dark = WorldMaterials.Solid(new Color(.04f, .03f, .06f)); iron = WorldMaterials.Solid(new Color(.16f, .16f, .2f), .6f); white = WorldMaterials.Glow(new Color(.84f, .84f, .9f), .16f);
            shardM = new Material(WorldMaterials.Glow(Cyan, 1.3f)) { name = "Shard crystals" }; shardM2 = new Material(WorldMaterials.Glow(Magenta, 1.3f)) { name = "Shard crystals 2" }; lampM = new Material(WorldMaterials.Glow(Violet, 1.6f)) { name = "Shard lamps" }; coneM = new Material(WorldMaterials.Glow(Cyan, .9f)) { name = "Shard speaker cones" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Virtual Riot" ? 15f : a.name == "Bassnectar" ? 13f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // canyon floor: dark violet-grey rock and grit, cracked with faint cyan
            if (slope > 20f) color = Color.Lerp(new Color(.26f, .24f, .32f), new Color(.34f, .3f, .4f), patch);
            else color = Color.Lerp(new Color(.24f, .22f, .3f), new Color(.3f, .28f, .36f), patch);
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
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 110f && Inside(heart2 + dir * (len + 22f), 28f) && Inside(heart2 - dir * 28f, 22f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Virtual Riot", 6f, -58f, 24f); Put("Bassnectar", 4f, 54f, 22f);
            var core = P2(streetL + 18f, 0);
            var avoid = new List<Vector3>(); for (var d = -24f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 20f, heart2.y + D2.y * d)); avoid.Add(new Vector3(core.x, 22f, core.y));
            ArtistPlacement.Spread(data, index, 48f, heart2, 42f, 38f, avoid, keep, Land, 320f);
            Level(data, index, heart2 - D2 * 8f, 24f, 38f, plazaY);
            for (var d = 18f; d <= streetL + 4f; d += 8f) { var p = P2(d, 0); Level(data, index, p, 6f, 12f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 18f) / 30f))); }
            Level(data, index, core, 16f, 26f, Mathf.Max(data.HeightAt(core.x, core.y), waterY + 3f));
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 9f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
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
            life = new GameObject("Shard life").AddComponent<ShardLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.shards = shardM; life.shards2 = shardM2; life.lamps = lampM; life.cones = coneM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            DropZone(ctx, mk, solid);
            Rift(ctx, mk, solid);
            Core(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Dubstep Shards", ctx.Root, false);
            solid.BuildCollider("The Dubstep Shards walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Shard glow", ctx.Root); }

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

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

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

        /// A slab of black rock to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(rock, g + up * .25f, new Vector3(len, .5f, .7f), rot * Quaternion.Euler(0, 0, 3f)); mk.Box(shardM, g + up * .51f + face * .36f, new Vector3(len * .8f, .02f, .02f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .7f), rot); var side = Vector3.Cross(up, face); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
        }

        /// A glowing crystal set on a black post.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(dark, g - up * .2f, up, .08f, 2.8f, 5); mk.Prism(lampM, g + up * 2.6f, up, .2f, .8f, 5, .05f); mk.Prism(lampM, g + up * 2.6f, -up, .2f, .3f, 5, .05f); halos.Add(g + up * 2.9f, 1.4f, Violet, .45f); ArtistBits.PostCollider(ctx.Root, g, 2.8f, .1f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.8f, Violet, 12f, 1.4f);
        }

        /// A great shard of crystal jutting from the ground at a lean.
        void Shard(Transform parent, MeshKit mk, Vector3 g, float h, float r, Vector3 lean, Material m, MeshKit solid)
        {
            var dir = (Vector3.up + lean).normalized; mk.Prism(m, g - Vector3.up * .5f, dir, r, h, 5, .05f, (g.x * 7f) % 72f); mk.Prism(rockD, g - Vector3.up * .5f, dir, r * 1.1f, h * .25f, 5, .7f);
            ArtistBits.PostCollider(parent, g, Mathf.Min(h, 4f), r * .9f);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 18f) * Mathf.Min(1.4f, end * .03f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, path, PathW, .1f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 3 == 0);
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

        // ---------------------------------------------------------------- the rest of the canyon
        /// Crystal shard clusters, black rock spires veined with crystal, glowing cracks, little speaker stacks, rock slabs under crystal lamps.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int shards = 0, spires = 0, cracks = 0, stacks = 0, benches = 0; var rng = new System.Random(2010);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .34f) { var n = 2 + (int)(s * 4f); for (var k = 0; k < n; k++) { var sp = p + Quaternion.Euler(0, k * 360f / n + s * 50f, 0) * f * (k == 0 ? 0f : 1.4f); Shard(ctx.Root, mk, ctx.OnGround(sp.x, sp.z), (k == 0 ? 6f : 2.5f) + s * 6f, (k == 0 ? .9f : .5f) + s * .4f, new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .7f, (k + (int)(s * 3f)) % 2 == 0 ? shardM : shardM2, solid); } Mark(p, 3f); shards++; }
                else if (roll < .52f) { var h = 8f + s * 10f; mk.Prism(rock, p - up * .5f, (up + side * (s - .5f) * .3f).normalized, 1.6f + s, h, 5, .15f, s * 60f); for (var k = 0; k < 3; k++) mk.Box(k % 2 == 0 ? shardM : shardM2, p + up * (1.5f + k * h * .22f) + f * (1.2f + s) * (1f - k * .2f), new Vector3(.12f, h * .2f, .05f), rot * Quaternion.Euler(0, 0, 20f - k * 25f)); ArtistBits.PostCollider(ctx.Root, p, 5f, 1.6f + s); Mark(p, 2.4f); spires++; }
                else if (roll < .62f) { for (var k = 0; k < 6; k++) { var a = p + side * (k - 2.5f) * 1.1f + f * Mathf.Sin(k * 1.7f) * .8f; var b = a + side * 1.1f + f * Mathf.Sin(k * 2.3f) * .6f; a = ctx.OnGround(a.x, a.z); b = ctx.OnGround(b.x, b.z); mk.Box(k % 2 == 0 ? shardM : shardM2, (a + b) / 2f + up * .03f, new Vector3(.1f, .02f, (b - a).magnitude), Quaternion.LookRotation(b - a)); } cracks++; }
                else if (roll < .7f) { for (var k = 0; k < 3; k++) { var sp = p + up * (k * 1.1f + .55f); mk.Box(dark, sp, new Vector3(1.6f, 1f, 1f), rot); foreach (var e in new[] { -.4f, .4f }) mk.Prism(coneM, sp + side * e + f * .51f, f, .34f, .02f, 12); } BoxCol(ctx.Root, p + up * 1.65f, new Vector3(1.6f, 3.3f, 1f), rot); Mark(p, 1.4f); stacks++; }
                else if (roll < .77f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
            }
            Debug.Log($"The Dubstep Shards: the Rift {streetL:0} m, {shards} shard clusters, {spires} rock spires, {cracks} glowing cracks, {stacks} speaker stacks, {benches} rock seats");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Sparse wiry grass in the cracks, tipped with cyan.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.3f, .3f, .36f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2016);
            const float st = 3f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .05f + 1f, pz * .05f + 9f) < .55f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.28f, .28f, .34f), new Color(.36f, .34f, .42f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .24f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, Cyan, .5f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Shard grass", ctx.Root, mat);
        }
    }
}
