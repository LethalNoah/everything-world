using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Oddball Backroads (WORLD_GUIDE.md): weird and funny, a wink in everything. Hazy summer back roads winding
    /// over low hills, telephone poles leaning along them, kudzu swallowing trees, poles and an old barn. You arrive at
    /// a crossroads where a giant fibreglass head grins beside a crooked water tower (and its eyes follow you). Down
    /// every road a roadside attraction, one per artist, things a little too big: a pink robot on a hill, a giant hot
    /// dog, a sun in sunglasses, a meteor in a field. Lawn flamingos, junk sculptures, a thrift-store couch in a field,
    /// mailboxes, rusted pickups, odd road signs, cornfields. Reads by hazy green-gold light, pink flamingos and sky
    /// blue (the Quarry next door is grey, the Moors moody). Files: OddballKit (ground, arrival, roads, wilds),
    /// OddballArtists, OddballSongIds, OddballLife.
    public partial class OddballKit : LandKit
    {
        public override string Land => "The Oddball Backroads";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Flamingo = new Color(1f, .45f, .65f), Sky = new Color(.45f, .8f, 1f), Sun = new Color(1f, .85f, .3f), Kudzu = new Color(.3f, .5f, .2f), Ink = new Color(.12f, .12f, .14f), Cream = new Color(.95f, .92f, .82f);
        const float RoadW = 5f;

        Material asphalt, line, wood, woodDark, metal, rust, kudzu, kudzuDark, flamingo, white, dark, fibre, barnRed, glassM;
        GlowHalos halos; Vector3 center; OddballLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Solid(new Color(.28f, .28f, .29f)); line = WorldMaterials.Solid(new Color(.95f, .8f, .25f));
            wood = WorldMaterials.Solid(new Color(.55f, .42f, .3f)); woodDark = WorldMaterials.Solid(new Color(.32f, .24f, .18f));
            metal = WorldMaterials.Solid(new Color(.6f, .62f, .64f), .6f); rust = WorldMaterials.Solid(new Color(.55f, .32f, .2f));
            kudzu = WorldMaterials.Solid(new Color(.3f, .5f, .2f)); kudzuDark = WorldMaterials.Solid(new Color(.22f, .38f, .16f));
            flamingo = WorldMaterials.Solid(Flamingo); white = WorldMaterials.Solid(new Color(.94f, .93f, .9f)); dark = WorldMaterials.Solid(new Color(.08f, .08f, .09f));
            fibre = WorldMaterials.Solid(new Color(.95f, .78f, .62f), .7f); barnRed = WorldMaterials.Solid(new Color(.6f, .18f, .14f));
            glassM = new Material(WorldMaterials.Glow(new Color(1f, .85f, .55f), 1.2f)) { name = "Windows" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Pixies": return 15f;
                case "Ween": return 14f;
                case "Violent Femmes": return 12f;
                case "Wilco": case "MJ Lenderman": case "Silver Jews": case "The Flaming Lips": return 10f;
                default: return HasStation(a) ? 12f : 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < 2.6f) color = Color.Lerp(new Color(.7f, .65f, .5f), new Color(.76f, .7f, .55f), patch);
            else if (slope > 24f) color = Color.Lerp(new Color(.45f, .42f, .3f), new Color(.52f, .5f, .35f), patch);
            else
            {
                // contrast by height: near-black green in the hollows, sun-bleached gold on the hills
                var low = Color.Lerp(new Color(.16f, .26f, .1f), new Color(.22f, .32f, .13f), patch);
                var mid = Color.Lerp(new Color(.5f, .58f, .28f), new Color(.62f, .64f, .32f), patch);
                var high = Color.Lerp(new Color(.82f, .72f, .38f), new Color(.9f, .8f, .45f), patch);
                // (the low coast stays a fresh meadow green; the dark is for the hollows inland)
                color = height < 24f ? Color.Lerp(Color.Lerp(low, mid, .55f), mid, Ease(10f, 22f, height)) : Color.Lerp(mid, high, Ease(24f, 34f, height));   // the darkest comes from the kudzu groves in the hollows
            }
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            ArtistPlacement.Spread(data, index, 62f, heart2, 46f, 58f, PlaceAvoid(), null, Land, 460f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
            Level(data, index, heart2, 20f, 34f);
            // level ground for the places that need it flat
            Level(data, index, heart2 + DinerOff, 14f, 24f);
            foreach (var (at, flat, blend) in new[] { (MotelAt, 26f, 38f), (LittleAt, 12f, 20f), (DriveAt, 34f, 50f), (DoorsAt, 16f, 26f), (FarmAt, 16f, 26f), (UfoAt, 18f, 28f), (FruitAt, 14f, 22f) }) Level(data, index, at, flat, blend);
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = Mathf.Max(data.HeightAt(c.x, c.y), data.layout.waterLevel + 2.6f);
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
            center = ctx.Plaza;
            life = new GameObject("Backroads life").AddComponent<OddballLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.windows = glassM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            Roads(ctx, mk, solid);
            Places(ctx, mk, solid);
            mk.Build("The Oddball Backroads", ctx.Root, false);
            solid.BuildCollider("The Oddball Backroads walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Grass(ctx); halos.Build("Backroads glow", ctx.Root); }

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
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + RoadW / 2f + 1f) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 26f + pad;

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] - side * (w / 2f * miter); R[i] = pts[i] + side * (w / 2f * miter);
                float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f);
                L[i].y = gl + bulge + lift; R[i].y = gr + bulge + lift;
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A plastic lawn flamingo on wire legs, head tipped (OddballLife turns some heads to watch you).
        void Flamingo_(LandContext ctx, MeshKit mk, Vector3 g, float yaw)
        {
            var up = Vector3.up; var rot = Quaternion.Euler(0, yaw, 0); var fwd = rot * Vector3.forward;
            foreach (var e in new[] { -.05f, .05f }) mk.Box(metal, g + up * .35f + rot * new Vector3(e, 0, 0), new Vector3(.02f, .7f, .02f), rot);
            mk.Blob(flamingo, g + up * .8f, new Vector3(.22f, .18f, .35f), ctx.Rng, 0f);
            mk.Box(flamingo, g + up * 1.05f + fwd * .22f, new Vector3(.05f, .45f, .05f), rot * Quaternion.Euler(-15f, 0, 0));
            mk.Blob(flamingo, g + up * 1.3f + fwd * .3f, Vector3.one * .08f, ctx.Rng, 0f); mk.Box(dark, g + up * 1.28f + fwd * .42f, new Vector3(.03f, .03f, .1f), rot * Quaternion.Euler(30f, 0, 0));
            BoxCol(ctx.Root, g + up * .7f + fwd * .1f, new Vector3(.45f, 1.4f, .8f), rot);
        }

        /// A kudzu mound: vines swallowing whatever was there, a heap of leafy blobs, a pole or a stump poking out.
        void KudzuMound(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var s = ctx.Range(2f, 4.5f); var poke = ctx.Chance(.5f);
            if (poke) mk.Prism(woodDark, g - up * .3f, (up + new Vector3(ctx.Range(-.15f, .15f), 0, ctx.Range(-.15f, .15f))).normalized, .14f, s * 1.6f, 6);
            for (var k = 0; k < 6; k++) mk.Blob(k % 2 == 0 ? kudzu : kudzuDark, g + new Vector3(ctx.Range(-s * .5f, s * .5f), ctx.Range(.2f, s * (poke ? 1.2f : .6f)), ctx.Range(-s * .5f, s * .5f)), Vector3.one * ctx.Range(s * .35f, s * .55f), ctx.Rng, .35f);
            BoxCol(ctx.Root, g + up * s * .45f, new Vector3(s * 1.4f, s * .9f, s * 1.4f), Quaternion.identity);
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a crossroads with a stop sign, a giant fibreglass head grinning on a plinth (its eyes
        /// follow you), a water tower leaning badly beside it, a gas pump, a bench, flamingos on the verge.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(asphalt, c - up * .25f, up, 9f, .32f, 20);
            // the giant head
            var hp = ctx.OnGround(c.x + 14f, c.z + 6f); var toC = new Vector3(c.x - hp.x, 0, c.z - hp.z).normalized; var hr = Quaternion.LookRotation(toC); var hs = Vector3.Cross(up, toC);
            mk.Box(white, hp + up * 1.2f, new Vector3(5f, 2.4f, 5f), hr); solid.Box(white, hp + up * 1.2f, new Vector3(5f, 2.4f, 5f), hr);
            mk.Blob(fibre, hp + up * 6f, new Vector3(3.4f, 3.8f, 3.2f), ctx.Rng, .05f); BoxCol(ctx.Root, hp + up * 6f, new Vector3(5.6f, 6.4f, 5.4f), hr);
            mk.Blob(fibre, hp + up * 5.4f + toC * 3.1f, new Vector3(.6f, .9f, .7f), ctx.Rng, 0f);   // the nose
            mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .25f)), hp + up * 4.1f + toC * 2.75f, new Vector3(2.2f, .35f, .3f), hr * Quaternion.Euler(0, 0, 0));   // the grin
            foreach (var e in new[] { -1f, 1f }) mk.Blob(fibre, hp + up * 6f + hs * e * 3.3f, new Vector3(.4f, .9f, .6f), ctx.Rng, 0f);   // ears
            mk.Blob(WorldMaterials.Solid(new Color(.4f, .25f, .15f)), hp + up * 9.2f, new Vector3(3f, 1.2f, 3f), ctx.Rng, .2f);   // the hair
            var eyes = new MeshKit(); foreach (var e in new[] { -1f, 1f }) { eyes.Blob(white, new Vector3(e * 1.1f, 0, 0), Vector3.one * .65f, ctx.Rng, 0f); eyes.Blob(dark, new Vector3(e * 1.1f, 0, .5f), Vector3.one * .28f, ctx.Rng, 0f); }
            var eg = eyes.BuildSingle("The head's eyes", life.transform, false); if (eg != null) { eg.transform.SetPositionAndRotation(hp + up * 6.8f + toC * 2.55f, hr); life.eyes = eg.transform; }
            // the crooked water tower
            var wt = ctx.OnGround(c.x + 6f, c.z + 18f); var lean = Quaternion.Euler(9f, 0, 7f);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var f = wt + new Vector3(x, 0, z) * 2.4f; mk.Prism(metal, f - up * .3f, lean * up, .18f, 14.5f, 6); ArtistBits.PostCollider(ctx.Root, f, 10f, .22f); }
            var tank = wt + lean * (up * 14f);
            mk.Prism(WorldMaterials.Solid(new Color(.75f, .8f, .82f)), tank, lean * up, 3.6f, 4.4f, 14); mk.Cone(WorldMaterials.Solid(new Color(.6f, .65f, .68f)), tank + lean * (up * 4.4f), 3.8f, 1.8f, 14);
            var face = new Vector3(c.x - wt.x, 0, c.z - wt.z).normalized;
            ArtistBits.Label(ctx.Root, "ODDBALL", tank + lean * (up * 2.4f) + face * 3.65f, face, 1f, new Color(.85f, .2f, .45f), TextAnchor.MiddleCenter);
            // a stop sign, a gas pump, flamingos
            var stop = ctx.OnGround(c.x - 8f, c.z + 7f); mk.Prism(metal, stop - up * .2f, up, .05f, 2.6f, 6); ArtistBits.PostCollider(ctx.Root, stop, 2.5f, .08f);
            mk.Prism(WorldMaterials.Solid(new Color(.85f, .1f, .1f)), stop + up * 2.6f, toC, .45f, .05f, 8);
            ArtistBits.Label(ctx.Root, "MAYBE", stop + up * 2.6f + toC * .06f, toC, .2f, Cream, TextAnchor.MiddleCenter);
            var pump = ctx.OnGround(c.x - 12f, c.z - 6f); mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .7f)), pump + up * 1f, new Vector3(.8f, 2f, .6f), Quaternion.identity); mk.Blob(white, pump + up * 2.2f, new Vector3(.35f, .35f, .2f), ctx.Rng, 0f); BoxCol(ctx.Root, pump + up * 1f, new Vector3(.8f, 2f, .6f), Quaternion.identity);
            for (var k = 0; k < 6; k++) Flamingo_(ctx, mk, ctx.OnGround(c.x - 4f + k * 1.3f, c.z - 11f + (k % 2) * .6f), k * 20f);
            var sign = ctx.OnGround(c.x - 14f, c.z + 2f); var srot = Quaternion.LookRotation(new Vector3(c.x - sign.x, 0, c.z - sign.z).normalized);
            foreach (var e in new[] { -1.8f, 1.8f }) { var p = sign + srot * new Vector3(e, 0, 0); mk.Prism(woodDark, p - up * .2f, up, .1f, 3.2f, 6); ArtistBits.PostCollider(ctx.Root, p, 3f, .12f); }
            mk.Box(WorldMaterials.Solid(new Color(.95f, .9f, .7f)), sign + up * 2.6f, new Vector3(4.4f, 1.4f, .1f), srot);
            ArtistBits.Label(ctx.Root, "THE ODDBALL BACKROADS", sign + up * 2.8f + srot * Vector3.forward * .06f, srot * Vector3.forward, .28f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "you are probably here", sign + up * 2.3f + srot * Vector3.forward * .06f, srot * Vector3.forward, .16f, new Color(.5f, .2f, .3f), TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, c + up * 6f, Sun, 24f, 1.2f);
            noGrass.Add(new Vector3(c.x, 18f, c.z));
            var mark = new GameObject("Shot: the arrival and the head").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toC * 6f + hs * 4f + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((hp + up * 6f - cam).normalized));
        }

        // ---------------------------------------------------------------- roads
        /// Back roads winding from the crossroads to every attraction: two-lane asphalt with a yellow line, gravel
        /// shoulders, telephone poles leaning along one side with sagging wires, a mailbox now and then, odd signs.
        void Roads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var walk = 1; var poles = new List<Vector3>();
            string[] signs = { "NO OUTLET (MOSTLY)", "DEER?", "SLOW: THOUGHTS", "WRONG WAY (MAYBE)", "BRIDGE MAY BE A DREAM", "SPEED LIMIT: VIBES", "SOFT SHOULDERS", "YOU ARE HERE?" };
            var signN = 0;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>(); var wob = ctx.Range(10f, 18f); var amp = Mathf.Min(9f, end * .1f);
                for (var d = walk == 1 ? 10f : RoadW / 2f + .1f; ; d += 2f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / wob) * amp * Mathf.Clamp01(dd / 20f) * Mathf.Clamp01((end - dd) / 20f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                DressRoad(pts);
            }
            void DressRoad(List<Vector3> pts)
            {
                if (pts.Count < 2) return;
                Strip(mk, pts, WorldMaterials.Solid(new Color(.6f, .56f, .48f)), RoadW + 1.6f, .03f, ctx);
                Strip(mk, pts, asphalt, RoadW, .16f, ctx);
                for (var i = 1; i < pts.Count; i += 2) { var a = pts[i] - pts[i - 1]; a.y = 0; if (a.sqrMagnitude < .01f) continue; mk.Box(line, pts[i] + up * .2f, new Vector3(.15f, .02f, 1.4f), Quaternion.LookRotation(a)); }
                foreach (var p in pts) Remember(p, walk);
                // telephone poles along the right side, leaning, wires sagging between them
                Vector3? last = null;
                for (var i = 4; i < pts.Count; i += 12)
                {
                    var a = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]; a.y = 0; a.Normalize(); var sdd = Vector3.Cross(up, a);
                    var pp = pts[i] + sdd * (RoadW / 2f + 2.2f); if (InFootprint(ctx, pp.x, pp.z, 1f)) { last = null; continue; }
                    var near = false; foreach (var o in poles) if ((o - pp).sqrMagnitude < 10f * 10f) near = true; if (near) { last = null; continue; }
                    var g = ctx.OnGround(pp.x, pp.z); var lean = Quaternion.Euler(ctx.Range(-6f, 6f), 0, ctx.Range(-6f, 6f));
                    mk.Prism(woodDark, g - up * .3f, lean * up, .14f, 8.3f, 6); var top = g + lean * (up * 7.6f);
                    mk.Box(woodDark, top, new Vector3(1.8f, .12f, .12f), Quaternion.LookRotation(sdd) * Quaternion.Euler(0, 90f, 0));
                    ArtistBits.PostCollider(ctx.Root, g, 7f, .16f); poles.Add(pp);
                    if (ctx.Chance(.35f)) { var kz = g; for (var k = 0; k < 4; k++) mk.Blob(k % 2 == 0 ? kudzu : kudzuDark, kz + up * (1f + k * 1.4f) + new Vector3(ctx.Range(-.6f, .6f), 0, ctx.Range(-.6f, .6f)), Vector3.one * ctx.Range(.7f, 1.1f), ctx.Rng, .35f); BoxCol(ctx.Root, kz + up * 1.6f, new Vector3(2.6f, 3.2f, 2.6f), Quaternion.identity); }
                    if (last.HasValue) foreach (var o in new[] { -.7f, .7f }) { var w0 = last.Value + a * 0f + Vector3.Cross(up, a) * 0f; var p0 = last.Value + (top - last.Value).normalized * 0f + new Vector3(0, 0, 0); var s0 = last.Value + Vector3.Cross(up, (top - last.Value).normalized) * o; var s1 = top + Vector3.Cross(up, (top - last.Value).normalized) * o; var prev = s0; for (var k = 1; k <= 6; k++) { var t = k / 6f; var q = Vector3.Lerp(s0, s1, t) - up * Mathf.Sin(t * Mathf.PI) * 1f; mk.Box(dark, (prev + q) / 2f, new Vector3(.03f, .03f, Vector3.Distance(prev, q)), Quaternion.LookRotation(q - prev)); prev = q; } }
                    last = top;
                    // a mailbox or an odd sign on the other side now and then
                    var op = pts[i] - sdd * (RoadW / 2f + 1.6f); var og = ctx.OnGround(op.x, op.z);
                    if (i % 36 == 16) { mk.Prism(woodDark, og - up * .2f, up, .05f, 1.3f, 6); mk.Box(WorldMaterials.Solid(new Color(.25f, .45f, .65f)), og + up * 1.25f, new Vector3(.3f, .32f, .55f), Quaternion.LookRotation(a)); ArtistBits.PostCollider(ctx.Root, og, 1.3f, .2f); }
                    else if (i % 36 == 28 && signN < 20)
                    {
                        mk.Prism(metal, og - up * .2f, up, .05f, 2.6f, 6); ArtistBits.PostCollider(ctx.Root, og, 2.5f, .08f);
                        var fw = -a; mk.Box(WorldMaterials.Solid(new Color(.95f, .82f, .2f)), og + up * 2.4f, new Vector3(1.2f, 1.2f, .04f), Quaternion.LookRotation(fw) * Quaternion.Euler(0, 0, 45f));
                        ArtistBits.Label(ctx.Root, signs[signN % signs.Length], og + up * 2.4f + fw * .03f, fw, .09f, Ink, TextAnchor.MiddleCenter); signN++;
                    }
                }
                walk++;
            }
            LoopRoads(ctx, mk, solid, (pts, closed) => DressRoad(pts));
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

        // ---------------------------------------------------------------- the wilds
        /// Hazy summer country gone a little strange: trees half swallowed by kudzu, kudzu mounds, flamingo flocks, junk
        /// sculptures, a thrift-store couch alone in a field, rusted pickups, an old barn wrapped in vines, cornfield
        /// patches, a lone door standing in a field, hay bales, the odd tire swing.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, mounds = 0, flocks = 0, junk = 0, couches = 0, trucks = 0, barns = 0, corn = 0, doors = 0, bales = 0;
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f); var face = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
                if (roll < .22f) { KudzuTree(ctx, mk, p); trees++; }
                else if (roll < .3f) { KudzuMound(ctx, mk, p); mounds++; }
                else if (roll < .34f) { var n = ctx.Rng.Next(3, 9); for (var k = 0; k < n; k++) Flamingo_(ctx, mk, ctx.OnGround(px + ctx.Range(-3f, 3f), pz + ctx.Range(-3f, 3f)), ctx.Range(0, 360f)); flocks++; }
                else if (roll < .38f) { JunkSculpture(ctx, mk, p); junk++; }
                else if (roll < .4f) { Couch(ctx, mk, p, face, couches); couches++; }
                else if (roll < .44f) { Pickup(ctx, mk, p, face, trucks); trucks++; }
                else if (roll < .46f && barns < 6) { if (Barn(ctx, mk, solid, p, face)) barns++; }
                else if (roll < .52f) { for (var k = 0; k < 30; k++) { var q = ctx.OnGround(px + ctx.Range(-4f, 4f), pz + ctx.Range(-4f, 4f)); mk.Prism(WorldMaterials.Solid(new Color(.55f, .6f, .28f)), q, Vector3.up, .04f, ctx.Range(1.8f, 2.4f), 4); mk.Box(WorldMaterials.Solid(new Color(.95f, .82f, .35f)), q + Vector3.up * 1.5f, new Vector3(.08f, .3f, .08f), Quaternion.identity); } BoxCol(ctx.Root, p + Vector3.up * 1.1f, new Vector3(8.6f, 3.4f, 8.6f), Quaternion.identity); corn++; }
                else if (roll < .53f) { var rot = Quaternion.LookRotation(face); mk.Box(wood, p + Vector3.up * 1.1f, new Vector3(1f, 2.2f, .08f), rot); mk.Box(woodDark, p + Vector3.up * 1.1f, new Vector3(1.2f, 2.4f, .06f), rot); mk.Blob(metal, p + Vector3.up * 1.05f + Vector3.Cross(Vector3.up, face) * .38f + face * .06f, Vector3.one * .05f, ctx.Rng, 0f); BoxCol(ctx.Root, p + Vector3.up * 1.2f, new Vector3(1.2f, 2.4f, .2f), rot); doors++; }
                else if (roll < .57f) { for (var k = 0; k < 3; k++) { var q = ctx.OnGround(px + ctx.Range(-3f, 3f), pz + ctx.Range(-3f, 3f)); mk.Prism(WorldMaterials.Solid(new Color(.85f, .75f, .45f)), q + Vector3.up * .6f - Vector3.Cross(Vector3.up, face) * .7f, Vector3.Cross(Vector3.up, face), .6f, 1.4f, 10); BoxCol(ctx.Root, q + Vector3.up * .6f, new Vector3(1.4f, 1.2f, 1.4f), Quaternion.LookRotation(face)); } bales++; }
            }
            Debug.Log($"The Oddball Backroads: {trees} kudzu trees, {mounds} mounds, {flocks} flamingo flocks, {junk} junk sculptures, {couches} couches, {trucks} pickups, {barns} barns, {corn} corn patches, {doors} lone doors, {bales} hay bales");
        }

        void KudzuTree(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var h = ctx.Range(6f, 11f);
            mk.Prism(woodDark, g - up * .3f, up, .3f, h * .6f, 6, .6f);
            for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? kudzu : kudzuDark, g + up * h * (.62f + k * .07f) + new Vector3(ctx.Range(-1f, 1f), 0, ctx.Range(-1f, 1f)) * h * .12f, Vector3.one * h * ctx.Range(.16f, .22f), ctx.Rng, .3f);
            // the vines spilling down the trunk to the ground
            for (var k = 0; k < 3; k++) mk.Blob(kudzuDark, g + up * (2.4f + k * 1.2f) + new Vector3(ctx.Range(-.4f, .4f), 0, ctx.Range(-.4f, .4f)), new Vector3(.9f, 1f, .9f), ctx.Rng, .3f);
            BoxCol(ctx.Root, g + up * 2.6f, new Vector3(2.2f, 5.2f, 2.2f), Quaternion.identity);
        }

        void JunkSculpture(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var y = 0f;
            for (var k = 0; k < ctx.Rng.Next(3, 7); k++)
            {
                var roll = ctx.Rng.Next(4); var r = Quaternion.Euler(ctx.Range(-20f, 20f), ctx.Range(0, 360f), ctx.Range(-20f, 20f));
                if (roll == 0) { mk.Prism(dark, g + up * (y + .3f), r * Vector3.right, .5f, .3f, 12); y += .6f; }
                else if (roll == 1) { mk.Box(WorldMaterials.Solid(new Color(.8f, .8f, .78f)), g + up * (y + .4f), new Vector3(.7f, .8f, .7f), r); y += .8f; }
                else if (roll == 2) { mk.Box(rust, g + up * (y + .25f), new Vector3(1f, .5f, .6f), r); y += .5f; }
                else { mk.Prism(Paint(k + (int)g.x), g + up * (y + .3f), up, .3f, .6f, 8); y += .6f; }
            }
            mk.Blob(flamingo, g + up * (y + .3f), new Vector3(.25f, .3f, .25f), ctx.Rng, 0f);
            BoxCol(ctx.Root, g + up * y * .5f, new Vector3(1.4f, y, 1.4f), Quaternion.identity);
        }

        Material Paint(int i) => WorldMaterials.Solid(new[] { Flamingo, Sky, Sun, new Color(.5f, .85f, .4f), new Color(.7f, .45f, .9f) }[Mathf.Abs(i) % 5]);

        void Couch(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var cl = WorldMaterials.Solid(new[] { new Color(.75f, .6f, .3f), new Color(.45f, .6f, .5f), new Color(.7f, .4f, .45f) }[i % 3]);
            mk.Box(cl, g + up * .3f, new Vector3(2.2f, .5f, .9f), rot); mk.Box(cl, g + up * .75f - face * .35f, new Vector3(2.2f, .7f, .25f), rot);
            foreach (var e in new[] { -1f, 1f }) mk.Box(cl, g + up * .55f + Vector3.Cross(up, face) * e * 1f, new Vector3(.25f, .55f, .9f), rot);
            mk.Prism(metal, g + up * .1f + face * 1.4f, up, .03f, 1.2f, 6); mk.Prism(GlowM(Sun, 1.6f), g + up * 1.3f + face * 1.4f, up, .25f, .3f, 10, .5f);   // a standing lamp beside it, lit
            BoxCol(ctx.Root, g + up * .55f, new Vector3(2.3f, 1.1f, 1f), rot); ArtistBits.PostCollider(ctx.Root, g + face * 1.4f, 1.6f, .3f);
            halos.Add(g + up * 1.4f + face * 1.4f, 1.2f, Sun, .4f);
        }

        void Pickup(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, i % 3 == 0 ? 6f : 0f); var paint = WorldMaterials.Solid(Color.Lerp(new[] { new Color(.3f, .5f, .7f), new Color(.75f, .3f, .2f), new Color(.85f, .8f, .65f) }[i % 3], new Color(.55f, .35f, .2f), .35f));
            mk.Box(paint, g + up * .8f, new Vector3(1.9f, .7f, 5f), rot); mk.Box(paint, g + up * 1.4f + face * .9f, new Vector3(1.8f, .65f, 1.8f), rot);
            mk.Box(dark, g + up * 1.45f + face * .9f, new Vector3(1.84f, .45f, 1.7f), rot);
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1.6f, 1.6f }) mk.Prism(dark, g + Vector3.Cross(up, face) * e * .95f + face * f + up * .4f, Vector3.Cross(up, face) * e, .4f, .25f, 10);
            for (var k = 0; k < 4; k++) mk.Blob(k % 2 == 0 ? kudzu : kudzuDark, g + up * (1f + (k % 2) * .4f) - face * (1f + k * .4f), Vector3.one * .6f, ctx.Rng, .3f);
            BoxCol(ctx.Root, g + up * .9f, new Vector3(2f, 1.8f, 5f), rot);
        }

        bool Barn(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); const float W = 10f, D = 14f, H = 6f;
            float lo = g.y, hi = g.y; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var h = ctx.Ground(g.x + sd.x * x * W / 2f + face.x * z * D / 2f, g.z + sd.z * x * W / 2f + face.z * z * D / 2f); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            if (hi - lo > 3f) return false;
            var b = new Vector3(g.x, hi, g.z);
            mk.Box(WorldMaterials.Solid(new Color(.4f, .38f, .35f)), new Vector3(b.x, (lo + hi) / 2f, b.z), new Vector3(W + .2f, hi - lo + .4f, D + .2f), rot);
            mk.Box(barnRed, b + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(barnRed, b + up * H / 2f, new Vector3(W, H, D), rot);
            mk.GableRoof(woodDark, b + up * H, new Vector3(W + .8f, 0, D + .8f), 4f, rot);
            mk.Box(white, b + up * 2f + face * (D / 2f + .02f), new Vector3(4f, 4f, .05f), rot); mk.Box(barnRed, b + up * 2f + face * (D / 2f + .03f), new Vector3(3.6f, 3.6f, .05f), rot);
            mk.Box(glassM, b + up * 7.6f + face * (D / 2f + .1f), new Vector3(1.2f, 1f, .05f), rot);
            for (var k = 0; k < 12; k++) mk.Blob(k % 2 == 0 ? kudzu : kudzuDark, b + up * ctx.Range(.5f, H + 1.5f) + sd * (ctx.Range(-1f, 1f) * W * .55f) + face * ctx.Range(-D * .5f, D * .5f), Vector3.one * ctx.Range(1.2f, 2.2f), ctx.Rng, .35f);
            noGrass.Add(new Vector3(b.x, 9f, b.z));
            return true;
        }

        void Grass(LandContext ctx)
        {
            var mat = WindMesh.Material(.4f, 1f, new Color(.55f, .6f, .32f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1988);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, RoadW / 2f + 1.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 4f, pz * .03f + 8f) < .38f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var gh = ctx.Ground(px, pz); var r0 = (float)rng.NextDouble();
                var col = gh < 18f ? Color.Lerp(new Color(.2f, .32f, .14f), new Color(.3f, .42f, .18f), r0) : gh > 30f ? Color.Lerp(new Color(.85f, .74f, .4f), new Color(.95f, .86f, .5f), r0) : Color.Lerp(new Color(.48f, .58f, .28f), new Color(.75f, .72f, .38f), r0);
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .4f + (float)rng.NextDouble() * .5f, col, Color.Lerp(col, new Color(.95f, .9f, .6f), .35f), rng, 4, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Backroads grass", ctx.Root, mat);
        }
    }
}
