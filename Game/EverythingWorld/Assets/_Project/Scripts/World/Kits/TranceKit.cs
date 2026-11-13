using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Trance Steppe (WORLD_GUIDE.md): an endless plain of golden grass under a giant sunrise. You arrive on the
    /// Sunrise Stage, a wide round stage on a low rise, ringed with laser emitters sweeping beams up into the dawn, a crowd
    /// with their hands in the air facing a sun so big it fills the horizon. The Long Plain runs straight at the sun to the
    /// Monolith, a white column throwing a beam of light up into the sky. Statues with raised arms stand out in the grass.
    /// Dawn pink, gold and peach; laser green and white.
    /// Laid out on purpose (the build-land standard): TranceTown.cs has the places and the people.
    public partial class TranceKit : LandKit
    {
        public override string Land => "The Trance Steppe";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Dawn = new Color(1f, .62f, .5f), Gold = new Color(1f, .8f, .4f), Laser = new Color(.4f, 1f, .5f), White = new Color(.94f, .96f, 1f), Violet = new Color(.6f, .4f, .9f), Ink = new Color(.1f, .08f, .1f);
        const float PathW = 3f, PlainW = 5f;

        Material stage, stone, path, iron, dark, white, leafM, trunkM, laserM, beamM, lampM, sunM;
        GlowHalos halos; Vector3 center, D, S; TranceLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            stage = WorldMaterials.Glow(new Color(.3f, .28f, .32f), .08f); stone = WorldMaterials.Glow(new Color(.6f, .54f, .5f), .1f); path = WorldMaterials.Glow(new Color(.56f, .48f, .4f), .08f); iron = WorldMaterials.Solid(new Color(.2f, .2f, .22f), .6f); dark = WorldMaterials.Solid(new Color(.06f, .05f, .06f)); white = WorldMaterials.Glow(new Color(.88f, .86f, .84f), .16f);
            leafM = WorldMaterials.Glow(new Color(.5f, .5f, .3f), .1f); trunkM = WorldMaterials.Glow(new Color(.36f, .28f, .24f), .1f);
            laserM = new Material(WorldMaterials.Glow(Laser, 2f)) { name = "Trance lasers" }; beamM = new Material(WorldMaterials.Glow(new Color(.6f, 1f, .7f), .5f)) { name = "Trance beams" }; lampM = new Material(WorldMaterials.Glow(Gold, 1.6f)) { name = "Trance lamps" }; sunM = new Material(WorldMaterials.Glow(new Color(1f, .6f, .36f), 1.4f)) { name = "Trance sun" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Above & Beyond" ? 12f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // steppe at dawn: dry gold grass going peach in the low light
            if (slope > 22f) color = Color.Lerp(new Color(.54f, .44f, .38f), new Color(.6f, .5f, .42f), patch);
            else color = Color.Lerp(new Color(.62f, .52f, .34f), new Color(.7f, .58f, .38f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 36f; while (len < 100f && Inside(heart2 + dir * (len + 16f), 22f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 7f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Above & Beyond", 6f, -40f, 18f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 18f) avoid.Add(new Vector3(heart2.x + D2.x * d, 16f, heart2.y + D2.y * d)); avoid.Add(new Vector3(heart2.x + D2.x * (streetL + 12f), 12f, heart2.y + D2.y * (streetL + 12f)));
            ArtistPlacement.Spread(data, index, 40f, heart2, 36f, 30f, avoid, keep, Land, 300f);
            Level(data, index, heart2, 18f, 32f, plazaY);
            for (var d = 18f; d <= streetL + 12f; d += 8f) { var p = P2(d, 0); Level(data, index, p, 5f, 11f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 18f) / 30f))); }
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
            life = new GameObject("Trance life").AddComponent<TranceLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lasers = laserM; life.beams = beamM; life.lamps = lampM; life.sun = sunM; life.fwd = D;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            SunriseStage(ctx, mk, solid);
            LongPlain(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Trance Steppe", ctx.Root, false);
            solid.BuildCollider("The Trance Steppe walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Trance glow", ctx.Root); }

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

        /// A low stone bench.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(stone, g + up * .4f, new Vector3(len, .14f, .5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(stone, g + up * .2f + side * e * (len / 2f - .25f), new Vector3(.3f, .4f, .44f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .5f), rot); Interact.Bench(tlife.transform, g + up * .48f - side * (len / 2f - .5f), g + up * .48f + side * (len / 2f - .5f), face);
        }

        /// A slim post with a gold light.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(iron, g - up * .2f, up, .05f, 3f, 6); mk.Prism(lampM, g + up * 2.9f, up, .12f, .4f, 6); halos.Add(g + up * 3.1f, 1.3f, Gold, .4f); ArtistBits.PostCollider(ctx.Root, g, 3f, .08f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.8f, Gold, 11f, 1.2f);
        }

        /// A laser emitter on the ground throwing a beam up into the sky; it sweeps (TranceLife).
        void Emitter(LandContext ctx, MeshKit mk, Vector3 g, float ph, Material beam)
        {
            var up = Vector3.up; mk.Box(dark, g + up * .3f, new Vector3(.7f, .6f, .7f), Quaternion.identity); mk.Box(laserM, g + up * .62f, new Vector3(.3f, .04f, .3f), Quaternion.identity); BoxCol(ctx.Root, g + up * .3f, new Vector3(.7f, .6f, .7f), Quaternion.identity);
            var b = new MeshKit(); b.Prism(beam, Vector3.zero, Vector3.up, .05f, 160f, 4, 6f, 0f, false); var bg = b.BuildSingle("Laser beam", ctx.Root, false); bg.transform.position = g + up * .64f; life.sweepers.Add((bg.transform, ph));
        }

        /// A statue with its arms raised.
        void Raised(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, float s)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var m = stone; mk.Box(m, g + up * .3f * s, new Vector3(1.2f, .6f, 1.2f) * s, Quaternion.LookRotation(face)); mk.Box(m, g + up * 1.4f * s, new Vector3(.5f, 1.6f, .3f) * s, Quaternion.LookRotation(face)); mk.Blob(m, g + up * 2.45f * s, Vector3.one * .22f * s, new System.Random(2), 0f); foreach (var e in new[] { -1f, 1f }) mk.Box(m, g + up * 2.5f * s + side * e * .45f * s, new Vector3(.14f, 1.1f, .14f) * s, Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, -e * 20f)); BoxCol(ctx.Root, g + up * 1.4f * s, new Vector3(1.2f, 2.8f, 1.2f) * s, Quaternion.LookRotation(face));
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

        // ---------------------------------------------------------------- the rest of the plain
        /// Laser emitters out in the grass, statues with raised arms, lone flat-topped trees, standing stones, flags, benches.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int emitters = 0, statues = 0, trees = 0, stones = 0, flags = 0, benches = 0; var rng = new System.Random(1999);
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .12f && emitters < 40) { Emitter(ctx, mk, p, s * 6f, s > .5f ? beamM : GlowM(new Color(1f, .9f, .8f), .4f)); emitters++; }
                else if (roll < .22f) { Raised(ctx, mk, p, D, .9f + s * .5f); Mark(p, 1.4f); statues++; }
                else if (roll < .42f) { var h = 3f + s * 2f; mk.Prism(trunkM, p, (up + side * (s - .5f) * .3f).normalized, .2f, h, 6, .6f); mk.Blob(leafM, p + up * (h + .3f), new Vector3(2.6f + s, .5f, 2.4f + s), rng, .2f); ArtistBits.PostCollider(ctx.Root, p, h, .3f); Mark(p, 2.4f); trees++; }
                else if (roll < .52f) { var h = 2f + s * 3f; mk.Box(stone, p + up * (h / 2f - .3f), new Vector3(.9f, h, .6f), rot * Quaternion.Euler(0, 0, (s - .5f) * 8f)); BoxCol(ctx.Root, p + up * h / 2f, new Vector3(.9f, h, .6f), rot); Mark(p, 1f); stones++; }
                else if (roll < .6f) { for (var k = 0; k < 3; k++) { var fp = p + side * (k - 1) * 1.6f; mk.Prism(iron, fp, up, .04f, 5f + k * .6f, 4); ArtistBits.PostCollider(ctx.Root, fp, 5f, .08f); mk.Box(GlowM(new[] { Dawn, Gold, Violet }[(k + (int)(s * 3f)) % 3], .8f), fp + up * (4.2f + k * .6f) + f * .7f, new Vector3(.02f, 1f, 1.4f), Quaternion.LookRotation(side)); } flags++; }
                else if (roll < .67f) { Bench(ctx.Root, mk, p, D); Lamp(ctx, mk, p - D * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
            }
            Debug.Log($"The Trance Steppe: the Long Plain {streetL:0} m, {emitters} laser emitters, {statues} statues, {trees} lone trees, {stones} standing stones, {flags} flag rows, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Tall gold steppe grass, lit pink at the tips by the dawn.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.4f, .9f, new Color(.66f, .56f, .36f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2000);
            const float st = 2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 5f, pz * .03f + 3f) < .25f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.64f, .54f, .34f), new Color(.76f, .62f, .4f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .5f + (float)rng.NextDouble() * .6f, col, Color.Lerp(col, Dawn, .5f), rng, 4, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Trance grass", ctx.Root, mat);
        }
    }
}
