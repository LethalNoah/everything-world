using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Small Hours (WORLD_GUIDE.md): the making half of bedroom music. 3 am, deep indigo, a quiet drizzle.
    /// A hamlet of tiny homes with lit windows where small, newer artists are making music right now, joined by
    /// wet lanes under sodium street lamps, with an all-night laundrette by the way in. Apart from the hamlet,
    /// Noah Gallagher's own place (SmallHoursNoah.cs): the house where the first album was made, the forest from
    /// that album's cover, and the twelve-gondola wheel from the second, with a light for every one of his songs.
    public partial class SmallHoursKit : LandKit
    {
        public override string Land => "The Small Hours";

        static readonly Color Moss = new Color(.2f, .27f, .25f), MossB = new Color(.24f, .3f, .27f), WetStone = new Color(.27f, .28f, .33f);
        static readonly Color WindowGlow = new Color(1f, .78f, .48f), Sodium = new Color(1f, .62f, .3f), Ink = new Color(.95f, .9f, .82f);
        static readonly Color[] Paint = { new Color(.62f, .6f, .7f), new Color(.7f, .62f, .58f), new Color(.55f, .62f, .66f), new Color(.68f, .66f, .58f), new Color(.6f, .55f, .62f) };

        Material wood, darkWood, iron, board, stone, slate, glassDark, lane, bark, leaf;
        GlowHalos halos;
        Vector3 center;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        SmallHoursLife life;
        static readonly System.Random Smooth = new System.Random(0);   // for collider blobs: no lumps, and the land's own random stays as it was

        /// A plain box collider on its own object (a sign board, things the merged walls don't cover).
        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        void MakeMaterials()
        {
            wood = WorldMaterials.Solid(new Color(.45f, .36f, .3f));
            darkWood = WorldMaterials.Solid(new Color(.24f, .2f, .2f));
            iron = WorldMaterials.Solid(new Color(.16f, .16f, .19f));
            board = WorldMaterials.Solid(new Color(.14f, .13f, .16f));
            stone = WorldMaterials.Solid(new Color(.42f, .42f, .46f));
            slate = WorldMaterials.Solid(new Color(.22f, .23f, .28f));
            glassDark = WorldMaterials.Solid(new Color(.12f, .14f, .2f), .7f);
            lane = WorldMaterials.Solid(WetStone, .55f);
            bark = WorldMaterials.Solid(new Color(.18f, .16f, .18f));
            leaf = WorldMaterials.Solid(new Color(.16f, .24f, .22f));
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            color = slope > 6f ? new Color(.22f, .22f, .26f) : Color.Lerp(Moss, MossB, Mathf.SmoothStep(0, 1, patch));
            color.a = 1f;
            return true;
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); facing.Clear();
            center = ctx.Plaza;
            var mk = new MeshKit(); var solid = new MeshKit();
            var labels = new GameObject("Small Hours words").transform; labels.SetParent(ctx.Root, false);
            life = new GameObject("Small Hours life").AddComponent<SmallHoursLife>();
            life.transform.SetParent(ctx.Root, false);
            Lanes(ctx, mk);
            Laundrette(ctx, mk, solid, labels);
            NightPlaces(ctx, mk, solid);
            Hedges(ctx, mk, solid);
            SongPlaces(ctx, mk, solid);
            Night(ctx, mk, solid);
            mk.Build("Small Hours scenery", ctx.Root, false);
            solid.BuildCollider("Small Hours walls", ctx.Root);
        }

        public override void Finish(LandContext ctx)
        {
            GroundCover(ctx);
            halos.Build("Small Hours glow", ctx.Root);
        }

        // ---------------------------------------------------------------- lanes and lamps
        /// Wet stone lanes from the laundrette by the way in to every home, branching off one another; sodium
        /// street lamps along them, never two close together.
        void Lanes(LandContext ctx, MeshKit mk)
        {
            var network = new List<Vector3> { center };
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            var lamps = new List<Vector3>();
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                {
                    var c = new Vector3(a.x, 0, a.z);
                    foreach (var p in network)
                    {
                        var d = Vector3.Distance(new Vector3(p.x, 0, p.z), c) - a.r;
                        if (d < best) { best = d; next = a; join = p; }
                    }
                }
                todo.Remove(next);
                var to = new Vector3(next.x, 0, next.z);
                var dir = to - new Vector3(join.x, 0, join.z);
                var len = dir.magnitude - next.r * (next.name == NoahName ? .35f : .6f);
                dir.Normalize();
                var side = Vector3.Cross(Vector3.up, dir);
                facing[next.name] = -dir;
                var pts = new List<Vector3>();
                for (var d = 0f; d <= Mathf.Max(0f, len); d += 3f)
                {
                    var t = len > 0 ? d / len : 0f;
                    var q = new Vector3(join.x, 0, join.z) + dir * d + side * Mathf.Sin(t * Mathf.PI) * Mathf.Min(5f, len * .07f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z));
                }
                if (pts.Count < 2) continue;
                Ribbon(ctx, mk, lane, pts, 2.6f, .07f);
                foreach (var p in pts) Remember(p);
                network.AddRange(pts);
                for (var i = 2; i < pts.Count; i += 4)
                {
                    var post = pts[i] + side * ((i / 4) % 2 == 0 ? 2.2f : -2.2f);
                    var near = false;
                    foreach (var o in lamps) if ((o - post).sqrMagnitude < 26f * 26f) { near = true; break; }
                    if (near || !ctx.IsFree(post.x, post.z, 1f)) continue;
                    lamps.Add(post);
                    StreetLamp(ctx, mk, ctx.OnGround(post.x, post.z), (pts[i] - post).normalized);
                }
            }
        }

        void StreetLamp(LandContext ctx, MeshKit mk, Vector3 at, Vector3 arm)
        {
            arm.y = 0; arm.Normalize();
            mk.Prism(iron, at, Vector3.up, .1f, 5f, 8);
            mk.Box(iron, at + Vector3.up * 5f + arm * .5f, new Vector3(.08f, .08f, 1.1f), Quaternion.LookRotation(arm));
            var head = at + Vector3.up * 4.85f + arm * 1f;
            mk.Box(iron, head + Vector3.up * .12f, new Vector3(.45f, .12f, .3f), Quaternion.LookRotation(arm));
            mk.Box(WorldMaterials.Glow(Sodium, 2.4f), head, new Vector3(.36f, .1f, .22f), Quaternion.LookRotation(arm));
            halos.Add(head - Vector3.up * .1f, 2.2f, Sodium, .9f);
            ArtistBits.PointLight(ctx.Root, head - Vector3.up * .4f, Sodium, 14f, 1.4f);
            ArtistBits.PostCollider(ctx.Root, at, 5f, .15f);
        }

        void Remember(Vector3 p)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p);
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

        static void Ribbon(LandContext ctx, MeshKit mk, Material m, List<Vector3> pts, float width, float lift)
        {
            for (var i = 0; i + 1 < pts.Count; i++)
            {
                var dir = pts[i + 1] - pts[i]; dir.y = 0;
                if (dir.sqrMagnitude < .01f) continue;
                var side = Vector3.Cross(Vector3.up, dir.normalized) * width / 2f;
                Vector3 E(Vector3 p, Vector3 s) { var q = p + s; q.y = Mathf.Max(ctx.Ground(q.x, q.z), ctx.Ground(p.x, p.z)) + lift; return q; }
                Vector3 a = E(pts[i], -side), b = E(pts[i], side), c = E(pts[i + 1], side), d = E(pts[i + 1], -side);
                mk.Quad(m, a, b, c, d, (a + c) / 2f - Vector3.up);
            }
        }

        // ---------------------------------------------------------------- the laundrette
        /// An all-night laundrette by the way in: bright windows, a row of machines with their drums turning.
        void Laundrette(LandContext ctx, MeshKit mk, MeshKit solid, Transform labels)
        {
            var at = center;
            var face = Vector3.forward;
            foreach (var a in ctx.Artists) { var d = new Vector3(a.x - at.x, 0, a.z - at.z); if (d.sqrMagnitude > 1f) { face = d.normalized; break; } }
            var rot = Quaternion.LookRotation(face);
            var side = Vector3.Cross(Vector3.up, face);
            const float W = 10f, D = 8f, H = 4.2f, t = .3f;
            var (lo, hi) = Footprint(ctx, at, rot, W, D);
            var c = new Vector3(at.x, hi + .1f, at.z);
            var paint = WorldMaterials.Solid(new Color(.75f, .76f, .8f));
            mk.Box(stone, new Vector3(c.x, (lo + c.y) / 2f - .2f, c.z), new Vector3(W + .3f, c.y - lo + .5f, D + .3f), rot);
            void Wall(Vector3 p, Vector3 s, Quaternion r) { mk.Box(paint, p, s, r); solid.Box(paint, p, s, r); }
            Wall(c - face * (D / 2f - t / 2f) + Vector3.up * H / 2f, new Vector3(W, H, t), rot);
            foreach (var s in new[] { -1f, 1f }) Wall(c + side * s * (W / 2f - t / 2f) + Vector3.up * H / 2f, new Vector3(t, H, D - 2f * t), rot);
            // the front: glass from sill to lintel with a door in it
            var front = c + face * (D / 2f - t / 2f);
            Wall(front + Vector3.up * .45f, new Vector3(W, .9f, t), rot);
            Wall(front + Vector3.up * (H - .45f), new Vector3(W, .9f, t), rot);
            foreach (var s in new[] { -1f, 1f }) Wall(front + side * s * (W / 2f - .2f) + Vector3.up * H / 2f, new Vector3(.4f, H, t), rot);
            var light = new Color(.86f, .93f, 1f);
            mk.Box(WorldMaterials.Glow(light, .9f), front + Vector3.up * (H / 2f) + side * 2.5f, new Vector3(3.8f, H - 1.8f, .05f), rot);
            mk.Box(WorldMaterials.Glow(light, .9f), front + Vector3.up * (H / 2f) - side * 3f, new Vector3(3f, H - 1.8f, .05f), rot);
            mk.Box(slate, c + Vector3.up * (H + .12f), new Vector3(W + .5f, .25f, D + .5f), rot);
            var sign = front + face * .2f + Vector3.up * (H - .45f);
            mk.Box(board, sign, new Vector3(5f, .6f, .1f), rot);
            mk.Box(WorldMaterials.Glow(new Color(.5f, .85f, 1f), 2.2f), sign + face * .06f, new Vector3(4.6f, .08f, .02f), rot);
            ArtistBits.Label(labels, "LAUNDRETTE · OPEN ALL NIGHT", sign + face * .08f + Vector3.up * .15f, face, .26f, new Color(.7f, .92f, 1f));
            // machines along the back wall, drums turning (SmallHoursLife), and a bench to wait on
            var drums = new List<Transform>();
            for (var i = 0; i < 5; i++)
            {
                var m = c - face * (D / 2f - 1.1f) + side * ((i - 2f) * 1.7f);
                mk.Box(WorldMaterials.Solid(new Color(.92f, .92f, .94f)), m + Vector3.up * .55f, new Vector3(1.3f, 1.1f, 1.2f), rot);
                solid.Box(paint, m + Vector3.up * .55f, new Vector3(1.3f, 1.1f, 1.2f), rot);
                var drum = new GameObject("Drum").transform; drum.SetParent(ctx.Root, false);
                drum.SetPositionAndRotation(m + Vector3.up * .58f + face * .62f, rot);
                var dk = new MeshKit();
                dk.Prism(WorldMaterials.Glow(new Color(.75f, .88f, 1f), 1.2f), Vector3.zero, Vector3.forward, .4f, .03f, 12);
                dk.Box(WorldMaterials.Solid(i % 2 == 0 ? new Color(.85f, .45f, .5f) : new Color(.45f, .6f, .85f)), Vector3.forward * .035f + Vector3.up * .12f, new Vector3(.3f, .1f, .01f), Quaternion.identity);
                dk.Build("Drum", drum, false).isStatic = false;
                drums.Add(drum);
            }
            life.drums = drums.ToArray();
            var bench = c + face * 1.5f + Vector3.up * .45f;
            mk.Box(wood, bench, new Vector3(3.5f, .08f, .5f), rot);
            solid.Box(wood, bench, new Vector3(3.5f, .9f, .5f), rot);
            ArtistBits.PointLight(ctx.Root, c + Vector3.up * (H - .5f), light, 12f, 1.6f, true);
            halos.Add(front + face * .5f + Vector3.up * H / 2f, 4f, light, .6f);
            IndoorSpaces.Add(c + Vector3.up * H / 2f, rot, new Vector3(W, H, D));
        }

        static (float lo, float hi) Footprint(LandContext ctx, Vector3 c, Quaternion rot, float w, float d)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var i = -2; i <= 2; i++)
            for (var j = -2; j <= 2; j++)
            {
                var p = c + rot * new Vector3(i * w / 4f, 0, j * d / 4f);
                var g = ctx.Ground(p.x, p.z);
                lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g);
            }
            return (lo, hi);
        }

        // ---------------------------------------------------------------- garden hedges and the ground
        void Hedges(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            for (var i = 0; i < 40; i++)
            {
                if (!ctx.TrySpot(out var p, 6f, 4f) || NearPath(p.x, p.z, 3f) || InNoah(p, 6f)) continue;
                var r = ctx.Range(1.2f, 2.4f);
                mk.Blob(i % 3 == 0 ? leaf : WorldMaterials.Solid(new Color(.14f, .2f, .2f)), p + Vector3.up * r * .6f, new Vector3(r, r * .8f, r), ctx.Rng, .2f);
                solid.Blob(leaf, p + Vector3.up * r * .6f, new Vector3(r, r * .8f, r) * 1.1f, Smooth, 0f);   // the hedge's own round shape
                solid.Box(leaf, p + Vector3.up * r * .6f, new Vector3(r * 1.9f, r * 1.6f, r * 1.9f), Quaternion.identity);
                if (i % 4 == 0)
                {
                    var h = ctx.Range(4f, 6f);
                    mk.Prism(bark, p, Vector3.up, .25f, h, 6, .7f);
                    mk.Blob(leaf, p + Vector3.up * (h + 1.5f), new Vector3(3f, 2.6f, 3f), ctx.Rng, .15f);
                    ArtistBits.PostCollider(ctx.Root, p, h, .3f);
                }
            }
        }

        /// Moss, low garden plants and ferns, damp with drizzle. Off the lanes.
        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.12f, .6f, Moss);
            var chunks = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(303);
            const float step = 2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (Mathf.PerlinNoise(px * .03f + 4f, pz * .03f + 1f) < .4f) continue;
                if (!ctx.IsLand(px, pz, 3f) || !ctx.IsFree(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoah(new Vector3(px, 0, pz), 0f)) continue;
                var g = ctx.Ground(px, pz);
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!chunks.TryGetValue(key, out var wm)) chunks[key] = wm = new WindMesh();
                var at = new Vector3(px, g - .04f, pz);
                if (rng.NextDouble() < .8)
                {
                    var col = Color.Lerp(new Color(.16f, .24f, .22f), new Color(.24f, .32f, .28f), (float)rng.NextDouble());
                    wm.Tuft(at, .25f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, new Color(.5f, .6f, .62f), .3f), rng, 4, .08f);
                }
                else wm.Bracken(at, .5f + (float)rng.NextDouble() * .3f, new Color(.16f, .26f, .22f), new Color(.26f, .36f, .3f), rng);
            }
            foreach (var kv in chunks) kv.Value.Build("Moss and ferns", ctx.Root, mat);
        }
    }
}
