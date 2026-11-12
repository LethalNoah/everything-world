using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Jam Band Commonwealth laid out on purpose (the build-land standard): festival roads over the whole land
    /// and places along them, with the festival folk who live there.
    /// Roads: a loop of wide dirt road round the artists, three roads out from the gate to it, a long south road
    /// down the empty tail to the sea, a short north road through a covered bridge; lantern posts all along.
    /// Places: the Campground (rows of tents round a bonfire with log seats, a drum circle to play, hammocks to lie in,
    /// a painted bus); the Wheel (a lit Ferris wheel you can ride); the swimming hole (a pond with a dock, a rope
    /// swing, beach balls, a creemee stand); the treehouses (stairs up, rope bridges, a lookout); the mirror dome
    /// (cushions under a turning mirror ball); Sunrise Hill (a gong to ring, mats, prayer flags); the poster gallery;
    /// the sugar shack by the covered bridge; a bonfire on the beach. Glades of brighter grass and flowers round each.
    public partial class JamKit
    {
        static readonly Vector2 CampAt = new Vector2(5440, 3165), WheelAt = new Vector2(5640, 3040), BeachAt = new Vector2(5475, 3000), SwimAt = new Vector2(5380, 3270),
            TreesAt = new Vector2(5010, 3765), DomeAt = new Vector2(5560, 3450), HillAt = new Vector2(5128, 3780), GalleryAt = new Vector2(5015, 3585), SugarAt = new Vector2(5170, 4005);
        static readonly Vector2[] LoopWay = { new Vector2(5230, 3950), new Vector2(5360, 3850), new Vector2(5480, 3700), new Vector2(5500, 3540), new Vector2(5380, 3440), new Vector2(5180, 3470), new Vector2(5040, 3620), new Vector2(5060, 3820) };
        static readonly Vector2[] SouthWay = { new Vector2(5380, 3440), new Vector2(5440, 3330), new Vector2(5500, 3200), new Vector2(5530, 3100), new Vector2(5600, 2990), new Vector2(5620, 2935) };
        static readonly Vector2[] NorthWay = { new Vector2(5230, 3950), new Vector2(5208, 3990), new Vector2(5195, 4016) };
        static readonly int[] Spokes = { 2, 4, 7 };
        static readonly (Vector2 at, float r)[] PlaceRings = { (CampAt, 36f), (WheelAt, 20f), (BeachAt, 14f), (SwimAt, 28f), (TreesAt, 18f), (DomeAt, 13f), (HillAt, 12f), (GalleryAt, 12f), (SugarAt, 12f) };
        const float RoadW = 4.2f;

        List<Vector3> PlaceAvoid(Vector2 heart)
        {
            var avoid = new List<Vector3>();
            foreach (var (at, r) in PlaceRings) avoid.Add(new Vector3(at.x, r + 10f, at.y));
            void Along(Vector2 a, Vector2 b) { var n = Mathf.CeilToInt(Vector2.Distance(a, b) / 18f); for (var k = 0; k <= n; k++) { var p = Vector2.Lerp(a, b, k / (float)n); avoid.Add(new Vector3(p.x, 9f, p.y)); } }
            for (var i = 0; i < LoopWay.Length; i++) Along(LoopWay[i], LoopWay[(i + 1) % LoopWay.Length]);
            foreach (var s in Spokes) Along(heart + (LoopWay[s] - heart).normalized * 34f, LoopWay[s]);
            return avoid;
        }

        JamPlacesLife plife; System.Random prng; Transform rootT;
        readonly List<Vector3> roadPts = new List<Vector3>(); readonly List<List<Vector3>> mainRoads = new List<List<Vector3>>();
        readonly List<Vector3> glades = new List<Vector3>();   // x, radius, z: brighter grass and flowers
        Material roadDirt, rockM, red;

        // ---------------------------------------------------------------- festival folk
        static readonly Color[] Bottoms = { new Color(.3f, .35f, .5f), new Color(.5f, .42f, .3f), new Color(.25f, .25f, .28f), new Color(.6f, .5f, .7f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.Beanie, Residents.Hat.Cap, Residents.Hat.Straw };
        static readonly string[] Heads =
        {
            "Did you catch the second set? Don't answer. Nobody could.", "I've been on tour since '94. My car hasn't.", "Never miss a Sunday show.",
            "Trade you a grilled cheese for a story.", "It's all one long song, really. This is the quiet part.", "Hydrate. That's the whole secret.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false)
        {
            var l = Residents.Random(prng, Dye, Bottoms, Hats, Dye); l.tool = tool; l.apron = apron; l.longHair = prng.NextDouble() < .6; return l;
        }

        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Heads, route, seatH);

        void Ring(Vector3 at, float r, float glade = 10f) { noGrass.Add(new Vector3(at.x, r, at.z)); glades.Add(new Vector3(at.x, r + glade, at.z)); }
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        bool InGlade(float x, float z) { foreach (var g in glades) { float dx = x - g.x, dz = z - g.z; if (dx * dx + dz * dz < g.y * g.y) return true; } return false; }

        /// A lantern on a wooden post (the trail lanterns, which the show plays with).
        void LanternPost(LandContext ctx, MeshKit mk, Vector3 at, int k, bool light)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z);
            mk.Prism(wood, g - up * .3f, up, .1f, 3.6f, 6); ArtistBits.PostCollider(ctx.Root, g, 3.3f, .12f);
            mk.Box(trailMats[k % trailMats.Count], g + up * 3.45f, new Vector3(.3f, .38f, .3f), Quaternion.identity); halos.Add(g + up * 3.45f, 1.3f, Warm, .7f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 3f, Warm, 12f, 1.2f);
        }

        /// A log to sit on.
        void LogSeat(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var tang = Vector3.Cross(up, face).normalized;
            mk.Prism(bark, g + up * .25f - tang * len / 2f, tang, .25f, len, 8); Col(g + up * .25f, new Vector3(len, .5f, .5f), Quaternion.LookRotation(face));
            Interact.Bench(plife.transform, g + up * .5f - tang * (len / 2f - .3f), g + up * .5f + tang * (len / 2f - .3f), face);
        }

        void Tent(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, float yaw, Color c)
        {
            var up = Vector3.up; var tp = ctx.OnGround(at.x, at.z); var tr = Quaternion.Euler(0, yaw, 0); var col = WorldMaterials.Solid(c, .3f); var tc = tp + up * .8f;
            Vector3 T(float x, float y, float z) => tp + tr * new Vector3(x, y, z);
            mk.Quad(col, T(-1.2f, 0, -1.4f), T(0, 1.6f, -1.4f), T(0, 1.6f, 1.4f), T(-1.2f, 0, 1.4f), tc); mk.Quad(col, T(1.2f, 0, 1.4f), T(0, 1.6f, 1.4f), T(0, 1.6f, -1.4f), T(1.2f, 0, -1.4f), tc);
            mk.Tri(col, T(-1.2f, 0, -1.4f), T(1.2f, 0, -1.4f), T(0, 1.6f, -1.4f), tc); mk.Tri(dark, T(-1.2f, 0, 1.4f), T(0, 1.6f, 1.4f), T(1.2f, 0, 1.4f), tc);
            mk.Box(GlowM(Warm, 1.2f), T(0, .5f, 1.38f), new Vector3(.5f, .6f, .02f), tr);
            solid.Box(col, tp + up * .8f, new Vector3(2.4f, 1.6f, 2.8f), tr);
        }

        void SignPost(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.4f, float size = .16f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            mk.Prism(wood, g - up * .2f, up, .08f, 1.9f, 6); mk.Box(wood, g + up * 1.7f, new Vector3(w, .8f, .08f), rot); ArtistBits.PostCollider(ctx.Root, g, 1.8f, .1f);
            ArtistBits.Label(ctx.Root, text, g + up * 1.72f + face * .05f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- the roads
        int roadWalk = 1;
        void FestivalRoad(LandContext ctx, MeshKit mk, List<Vector3> pts)
        {
            if (pts.Count < 2) return;
            Strip(mk, pts, roadDirt, RoadW, .06f);
            var up = Vector3.up; var n = 0;
            for (var i = 6; i < pts.Count; i += 9)
            {
                var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d);
                var lp = pts[i] + side * (n % 2 == 0 ? 1f : -1f) * (RoadW / 2f + .7f);
                if (ctx.IsLand(lp.x, lp.z, 1f)) LanternPost(ctx, mk, lp, n, n % 3 == 0); n++;
            }
            foreach (var p in pts) Remember(p, roadWalk);
            roadWalk++; mainRoads.Add(pts); roadPts.AddRange(pts);
        }

        void FestivalRoads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadDirt = WorldMaterials.Solid(new Color(.66f, .56f, .4f)); rockM = WorldMaterials.Solid(new Color(.55f, .53f, .5f)); red = WorldMaterials.Solid(new Color(.7f, .16f, .14f));
            roadPts.Clear(); mainRoads.Clear(); glades.Clear(); roadWalk = 1; rootT = ctx.Root;
            plife = new GameObject("Commonwealth places life").AddComponent<JamPlacesLife>(); plife.transform.SetParent(life.transform, false);
            prng = new System.Random(1983);
            FestivalRoad(ctx, mk, Smooth(ctx, LoopWay, true));
            var c = new Vector2(center.x, center.z);
            foreach (var s in Spokes)
            {
                var to = LoopWay[s]; var dir = (to - c).normalized; var side = new Vector2(-dir.y, dir.x);
                FestivalRoad(ctx, mk, Smooth(ctx, new[] { c + dir * 14f, Vector2.Lerp(c, to, .5f) + side * 12f, to }, false));
            }
            FestivalRoad(ctx, mk, Smooth(ctx, SouthWay, false)); FestivalRoad(ctx, mk, Smooth(ctx, NorthWay, false));
            foreach (var (at, r) in PlaceRings) { noGrass.Add(new Vector3(at.x, r, at.y)); glades.Add(new Vector3(at.x, r + 12f, at.y)); }
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + RoadW) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * (RoadW / 2f + .1f); var b = new Vector2(from.x, from.z) + d * (len - stopShort); var side = new Vector2(-d.y, d.x);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + side * Mathf.Min(8f, len * .1f), b }, false, 1.5f, 1f);
            if (pts.Count < 2) return;
            Strip(mk, pts, dirt, 2.6f, .05f); foreach (var p in pts) Remember(p, 900);
            for (var i = 8; i < pts.Count; i += 12) LanternPost(ctx, mk, pts[i] + Vector3.Cross(Vector3.up, (pts[i] - pts[i - 1]).normalized) * 1.9f, i, false);
        }

        void Places(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Campground(ctx, mk, solid); Wheel(ctx, mk, solid); SwimmingHole(ctx, mk, solid); Treehouses(ctx, mk, solid); MirrorDome(ctx, mk, solid);
            SunriseHill(ctx, mk, solid); Gallery(ctx, mk, solid); SugarShack(ctx, mk, solid); BeachFire(ctx, mk, solid);
        }

        void Bonfire(LandContext ctx, MeshKit mk, Vector3 pit, float size = 1f)
        {
            var up = Vector3.up;
            for (var k = 0; k < 10; k++) mk.Blob(rockM, pit + Quaternion.Euler(0, k * 36f, 0) * Vector3.forward * 1.1f * size + up * .15f, new Vector3(.3f, .2f, .25f) * size, ctx.Rng, .3f);
            for (var k = 0; k < 4; k++) mk.Prism(bark, pit + up * .2f + Quaternion.Euler(0, k * 90f + 20f, 0) * Vector3.forward * .5f * size, (Quaternion.Euler(0, k * 90f + 20f, 0) * -Vector3.forward + up * 1.4f).normalized, .1f * size, 1.3f * size, 6);
            mk.Blob(GlowM(new Color(1f, .55f, .2f), 2.6f), pit + up * .7f * size, new Vector3(.5f, .8f, .5f) * size, ctx.Rng, .3f); mk.Blob(GlowM(new Color(1f, .85f, .4f), 2.6f), pit + up * .5f * size, new Vector3(.3f, .5f, .3f) * size, ctx.Rng, .3f);
            var fl = ArtistBits.PointLight(ctx.Root, pit + up * 1.4f, new Color(1f, .6f, .25f), 16f * size, 2.2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2.2f;
            halos.Add(pit + up * .8f, 2.6f * size, new Color(1f, .55f, .2f), .7f); ArtistBits.PostCollider(ctx.Root, pit, 1f, 1.2f * size); life.fires.Add(pit + up * .3f);
        }

        // the Campground: tents in rows round a bonfire, a drum circle, hammocks, a painted bus
        void Campground(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, CampAt, 20f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            Bonfire(ctx, mk, g, 1.3f);
            for (var k = 0; k < 6; k++) { var a = k * 60f + 30f; var p = g + Quaternion.Euler(0, a, 0) * Vector3.forward * 4.6f; LogSeat(ctx, mk, p, FaceTo(p, g), 2.4f); }
            // tents in two arcs behind the logs, a lane between them to the fire
            var n = 0;
            foreach (var r in new[] { 13f, 20f })
                for (var a = 40f; a < 330f; a += r > 15f ? 26f : 36f)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 22f) continue;
                    var p = f.P(Mathf.Sin(a * Mathf.Deg2Rad) * r, 0, Mathf.Cos(a * Mathf.Deg2Rad) * r); if (!ctx.IsLand(p.x, p.z, 3f)) continue;
                    Tent(ctx, mk, solid, p, Quaternion.LookRotation(FaceTo(p, g)).eulerAngles.y, Dye[n++ % Dye.Length]);
                }
            for (var k = 0; k < 8; k++) { var a0 = k * 45f; var p0 = g + Quaternion.Euler(0, a0, 0) * Vector3.forward * 8.5f; p0 = ctx.OnGround(p0.x, p0.z); mk.Prism(wood, p0 - up * .3f, up, .09f, 3.6f, 6); ArtistBits.PostCollider(ctx.Root, p0, 3.3f, .1f); var p1 = g + Quaternion.Euler(0, a0 + 45f, 0) * Vector3.forward * 8.5f; p1 = ctx.OnGround(p1.x, p1.z); StringLights(mk, p0 + up * 3.2f, p1 + up * 3.2f, Warm, 9); }
            // the drum circle: four hand drums to play, two drummers at it
            var dc = f.P(-11f, 0, 9f); dc = ctx.OnGround(dc.x, dc.z);
            for (var k = 0; k < 4; k++)
            {
                var p = dc + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * 1.6f; p = ctx.OnGround(p.x, p.z);
                var dm = new MeshKit(); dm.Prism(WorldMaterials.Solid(Dye[k]), Vector3.zero, Vector3.up, .28f, .7f, 10, .7f); dm.Prism(canvas, new Vector3(0, .7f, 0), Vector3.up, .3f, .04f, 10);
                var go = dm.BuildSingle("Hand drum", plife.transform, false); go.transform.position = p;
                var t = Interact.Toy(go, Toy.Kind.Bob, "Play the drum", new Vector3(.7f, 1f, .7f)); go.GetComponent<BoxCollider>().center = new Vector3(0, .4f, 0); t.amount = .12f; t.seconds = .25f; t.onUse = _ => plife.Beat(p + up * 1.2f);
                ArtistBits.PostCollider(ctx.Root, p, .7f, .3f);
                if (k < 2) Folk(p + FaceTo(dc, p) * .7f, FaceTo(p, dc), Resident.Pose.Work, new[] { "Jump in, there's a drum free. There's always a drum free.", "We started this jam on Thursday." }).workRate = 2.4f;
            }
            plife.beatLight = ArtistBits.PointLight(ctx.Root, dc + up * 2.5f, Dye[4], 10f, .6f);
            // hammocks between trees, to lie in
            for (var k = 0; k < 3; k++)
            {
                var hc = f.P(12f + k * 5f, 0, 10f + k * 3f); var d = Quaternion.Euler(0, 30f + k * 40f, 0) * Vector3.forward;
                var a = ctx.OnGround(hc.x - d.x * 3.2f, hc.z - d.z * 3.2f); var b = ctx.OnGround(hc.x + d.x * 3.2f, hc.z + d.z * 3.2f); if (!ctx.IsLand(a.x, a.z, 2f) || !ctx.IsLand(b.x, b.z, 2f)) continue;
                Tree(ctx, mk, a, 1f); Tree(ctx, mk, b, .9f); var cloth = WorldMaterials.Solid(Dye[(k * 2 + 1) % Dye.Length], .3f);
                for (var q = 0; q < 6; q++) { float t0 = .12f + q * .127f, t1 = t0 + .127f; var p0 = Vector3.Lerp(a, b, t0) + up * (1.3f - Mathf.Sin(t0 * Mathf.PI) * .7f); var p1 = Vector3.Lerp(a, b, t1) + up * (1.3f - Mathf.Sin(t1 * Mathf.PI) * .7f); mk.Box(cloth, (p0 + p1) / 2f, new Vector3(1f, .05f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); Col((p0 + p1) / 2f, new Vector3(1f, .3f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
                Interact.Seat(plife.transform, (a + b) / 2f + up * .72f, Vector3.Cross(up, d), "Lie in the hammock").seatHeight = .2f;
                if (k == 1) Folk((a + b) / 2f + up * .72f, Vector3.Cross(up, d), Resident.Pose.Sit, new[] { "Shh. I'm listening to last night.", "Wake me for the encore." }, null, null, .2f);
            }
            // a painted bus, a camp kitchen, people round the fire
            var bus = f.P(-16f, 0, -8f); bus = ctx.OnGround(bus.x, bus.z); var br = Quaternion.LookRotation(f.Right + f.Fwd * .3f);
            mk.Box(WorldMaterials.Solid(Dye[4]), bus + up * 1.5f, new Vector3(2.4f, 2.2f, 7f), br); mk.Box(white, bus + up * 2.75f, new Vector3(2.3f, .3f, 6.8f), br); for (var k = 0; k < 4; k++) { mk.Box(GlowM(Warm, 1.2f), bus + up * 1.9f + br * new Vector3(1.21f, 0, -2.2f + k * 1.5f), new Vector3(.02f, .7f, 1f), br); mk.Blob(WorldMaterials.Solid(Dye[k]), bus + up * 1.1f + br * new Vector3(1.22f, 0, -2.4f + k * 1.6f), new Vector3(.02f, .35f, .35f), ctx.Rng, 0f); }
            foreach (var e in new[] { -1f, 1f }) foreach (var z in new[] { -2.3f, 2.3f }) mk.Prism(dark, bus + br * new Vector3(e * 1.1f, .45f, z), br * Vector3.right * e, .45f, .25f, 10);
            solid.Box(white, bus + up * 1.5f, new Vector3(2.4f, 3f, 7f), br);
            var kt = f.P(9f, 0, -11f); kt = ctx.OnGround(kt.x, kt.z); mk.Box(wood, kt + up * .8f, new Vector3(2.4f, .08f, 1f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(wood, kt + up * .4f + f.Right * e, new Vector3(.1f, .8f, .9f), f.r); Col(kt + up * .45f, new Vector3(2.4f, .9f, 1f), f.r);
            mk.Blob(GlowM(Warm, 2.2f), kt + up * 1.05f, Vector3.one * .14f, ctx.Rng, 0f); halos.Add(kt + up * 1.05f, 1f, Warm, .6f); mk.Prism(metal, kt + up * .84f + f.Right * .6f, up, .2f, .18f, 10);
            Folk(kt - f.Fwd * 1f, f.Fwd, Resident.Pose.Work, new[] { "Veggie burritos, five minutes. Been five minutes for an hour.", "Everybody eats. That's the only rule out here." }, Look(Residents.Tool.Spatula, true));
            Folk(ctx.OnGround(g.x + 4.4f, g.z + 1.2f) + up * .5f, FaceTo(new Vector3(g.x + 4.4f, 0, g.z + 1.2f), g), Resident.Pose.Sit, new[] { "Best fire on the lot. We keep it going all week.", "You look like you need to sit down. Sit down." }, null, null, .5f);
            Folk(ctx.OnGround(g.x - 6.5f, g.z - 3f), FaceTo(new Vector3(g.x - 6.5f, 0, g.z - 3f), g), Resident.Pose.Wave, new[] { "This one's my favourite jam. They all are.", "Can't stop. Won't stop. Might sit down in a bit." });
            SignPost(ctx, mk, f.P(-3f, 0, 24f), f.Fwd, "THE CAMPGROUND\nquiet hours: never", 3f);
            Lane(ctx, mk, f.P(0, 0, 22f)); Ring(g, 26f, 14f);
            Shot(ctx, "the campground", f.P(10f, 3f, 26f), g + up * 1f);
        }

        // the Wheel: a lit Ferris wheel you can ride (sit in a gondola as it comes round)
        void Wheel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, WheelAt, 14f); var toRoad = FaceTo(g, Nearest(roadPts, g)); var f = new PlaceFrame(g, toRoad); const float R = 12f, Hub = 14.6f;
            mk.Prism(rockM, g - up * .5f, up, 9f, .62f, 20); solid.Prism(rockM, g - up * .5f, up, 9f, .62f, 20);
            var hub = g + up * Hub;
            foreach (var z in new[] { -1.4f, 1.4f }) foreach (var x in new[] { -5f, 5f }) { var foot = f.P(x, .1f, z); mk.Box(white, (foot + hub + f.Fwd * z) / 2f, new Vector3(.4f, .4f, Vector3.Distance(foot, hub + f.Fwd * z)), Quaternion.LookRotation(hub + f.Fwd * z - foot)); ArtistBits.PostCollider(ctx.Root, foot, 2.4f, .4f); }
            mk.Prism(metal, hub - f.Fwd * 1.6f, f.Fwd, .5f, 3.2f, 10);
            var wheel = new MeshKit();
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); var d2 = new Vector3(Mathf.Cos(a + Mathf.PI / 8f), Mathf.Sin(a + Mathf.PI / 8f), 0);
                foreach (var z in new[] { -.9f, .9f })
                {
                    wheel.Box(white, d * R / 2f + Vector3.forward * z, new Vector3(.12f, .12f, R), Quaternion.LookRotation(d, Vector3.forward));
                    wheel.Box(white, (d + d2) * R / 2f + Vector3.forward * z, new Vector3(.14f, .14f, Vector3.Distance(d * R, d2 * R)), Quaternion.LookRotation(d2 - d, Vector3.forward));
                    wheel.Blob(GlowM(Dye[k % Dye.Length], 2.6f), d * R + Vector3.forward * z, Vector3.one * .22f, prng, 0f); wheel.Blob(GlowM(Dye[(k + 3) % Dye.Length], 2.4f), d * R * .5f + Vector3.forward * z, Vector3.one * .16f, prng, 0f);
                }
            }
            var wgo = wheel.BuildSingle("The Wheel", plife.transform, false); wgo.transform.SetPositionAndRotation(hub, Quaternion.LookRotation(f.Fwd)); plife.wheel = wgo.transform; plife.wheelRest = wgo.transform.rotation;
            for (var k = 0; k < 8; k++)
            {
                var gk = new MeshKit(); var cm = WorldMaterials.Solid(Dye[k % Dye.Length]);
                gk.Box(metal, new Vector3(0, -.6f, 0), new Vector3(.06f, 1.2f, .06f), Quaternion.identity); gk.Box(cm, new Vector3(0, -2f, 0), new Vector3(1.5f, .12f, 1.3f), Quaternion.identity);
                foreach (var e in new[] { -1f, 1f }) { gk.Box(cm, new Vector3(e * .72f, -1.6f, 0), new Vector3(.08f, .8f, 1.3f), Quaternion.identity); gk.Box(cm, new Vector3(0, -1.6f, e * .62f), new Vector3(1.5f, .8f, .08f), Quaternion.identity); }
                gk.Box(cm, new Vector3(0, -1.2f, 0), new Vector3(1.7f, .08f, 1.5f), Quaternion.identity); gk.Box(white, new Vector3(0, -1.6f, 0), new Vector3(1.1f, .1f, .4f), Quaternion.identity);
                var go = gk.BuildSingle("Gondola", plife.transform, false); go.transform.rotation = Quaternion.LookRotation(f.Right); plife.gondolas.Add(go.transform);
                go.transform.position = hub + (f.Right * Mathf.Cos(k * Mathf.PI / 4f) + up * Mathf.Sin(k * Mathf.PI / 4f)) * R;   // where it starts (JamPlacesLife carries it round)
                var seat = Interact.Seat(go.transform, go.transform.position + up * -1.5f, f.Right, "Ride the Wheel"); seat.transform.localPosition = new Vector3(0, -1.5f, 0); seat.GetComponent<BoxCollider>().size = new Vector3(1.6f, 1.6f, 1.4f); seat.seatHeight = .5f;
            }
            plife.hub = hub; plife.wheelAcross = f.Fwd; plife.wheelR = R;
            ArtistBits.PointLight(ctx.Root, hub + f.Fwd * 3f, Warm, 34f, 2f); halos.Add(hub, 5f, Warm, .35f);
            // the ticket booth and its keeper
            var tb = f.P(-7f, .12f, 5f); mk.Box(red, tb + up * 1.2f, new Vector3(1.8f, 2.4f, 1.8f), f.r); solid.Box(red, tb + up * 1.2f, new Vector3(1.8f, 2.4f, 1.8f), f.r); mk.Cone(white, tb + up * 2.4f, 1.5f, 1f, 4, 45f); mk.Box(GlowM(Warm, 1.6f), tb + up * 1.5f + f.Fwd * .91f, new Vector3(1f, .7f, .02f), f.r);
            ArtistBits.Label(ctx.Root, "THE WHEEL\nrides: free, like everything", tb + up * 2.1f + f.Fwd * .92f, f.Fwd, .1f, Ink, TextAnchor.MiddleCenter);
            Folk(tb + f.Fwd * 1.6f + f.Right * 1.2f, f.Fwd, Resident.Pose.Wave, new[] { "Hop in when one comes round. Mind the step. There isn't one.", "From the top you can see the whole show. And tomorrow, a bit." });
            LogSeat(ctx, mk, f.P(6.5f, 0, 6f), -f.Fwd, 2.4f);
            Lane(ctx, mk, f.P(0, 0, 10f)); Ring(g, 13f, 10f);
            Shot(ctx, "the Wheel", f.P(-8f, 2f, 30f), hub - up * 2f);
        }

        // the swimming hole: a pond with a dock, a rope swing, tubes, beach balls, a creemee stand
        void SwimmingHole(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SwimAt.x, SwimAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float PR = 13f;
            var water = WorldMaterials.Glow(new Color(.2f, .55f, .8f), .7f);
            mk.Prism(rockM, g - up * .4f, up, PR + .8f, .5f, 26); mk.Prism(water, g + up * .11f, up, PR, .02f, 26); solid.Prism(rockM, g - up * .6f, up, PR + .8f, .75f, 26);
            ArtistBits.PointLight(ctx.Root, g + up * 2.5f, new Color(.4f, .75f, 1f), 20f, 1.4f);
            // the dock, out from the road side, a ladder at its end, two people dangling their feet
            for (var z = 0f; z < 10f; z += 1f) mk.Box(wood, f.P(0, .5f, PR + 1.5f - z), new Vector3(2.2f, .1f, .9f), f.r);
            solid.Box(wood, f.P(0, .3f, PR - 3f), new Vector3(2.2f, .5f, 10f), f.r); foreach (var e in new[] { -1f, 1f }) foreach (var z in new[] { PR + 1f, PR - 3f, PR - 7.6f }) mk.Box(bark, f.P(e * 1f, .2f, z), new Vector3(.2f, 1.2f, .2f), f.r);
            Interact.Bench(plife.transform, f.P(-1.05f, .58f, PR - 6.6f), f.P(-1.05f, .58f, PR - 3.4f), -f.Right);
            Folk(f.P(1.05f, .58f, PR - 5f), f.Right, Resident.Pose.Sit, new[] { "Water's perfect. It's always perfect. Nobody knows why.", "No lifeguard. We all sort of keep an eye out." }, null, null, .5f);
            // tubes on the water, beach balls to throw
            for (var k = 0; k < 5; k++) { var p = g + Quaternion.Euler(0, k * 72f + 20f, 0) * Vector3.forward * (4f + (k % 3) * 2.5f) + up * .2f; mk.Prism(WorldMaterials.Solid(Dye[k]), p, up, .9f, .3f, 12); mk.Prism(water, p + up * .02f, up, .45f, .3f, 10); }
            for (var k = 0; k < 3; k++)
            {
                var bm = new MeshKit(); bm.Blob(WorldMaterials.Solid(Dye[k * 2]), Vector3.zero, Vector3.one * .35f, prng, 0f); bm.Blob(white, new Vector3(0, .05f, 0), new Vector3(.36f, .2f, .36f), prng, 0f);
                var go = bm.BuildSingle("Beach ball", plife.transform, false); go.transform.position = f.P(-5f + k * 1.4f, .5f, PR + 5f);
                go.AddComponent<SphereCollider>().radius = .35f; var rb = go.AddComponent<Rigidbody>(); rb.mass = .15f; rb.linearDamping = .8f; var c = go.AddComponent<Carryable>(); c.thing = "the beach ball"; c.home = go.transform.position; c.lowest = g.y - 6f;
            }
            // the rope swing from a big tree on the bank
            var tr = f.P(PR + 3f, 0, -2f); tr = ctx.OnGround(tr.x, tr.z); Tree(ctx, mk, tr, 1.5f); var bough = tr + up * 7f; mk.Prism(bark, bough, -f.Right, .22f, 5f, 6);
            var sw = new MeshKit(); sw.Box(canvas, new Vector3(0, -2.6f, 0), new Vector3(.05f, 5.2f, .05f), Quaternion.identity); sw.Prism(dark, new Vector3(0, -5.4f, -.12f), Vector3.forward, .45f, .24f, 12);
            var sgo = sw.BuildSingle("Rope swing", plife.transform, false); sgo.transform.SetPositionAndRotation(bough - f.Right * 4.2f, f.r);
            var st = Interact.Toy(sgo, Toy.Kind.Custom, "Push the swing", new Vector3(1.2f, 1.4f, 1.2f)); sgo.GetComponent<BoxCollider>().center = new Vector3(0, -5.3f, 0); st.onUse = _ => plife.Swing(sgo.transform);
            // towels to lie on, the creemee stand
            Color[] towel = { Dye[0], Dye[4], Dye[2] };
            for (var k = 0; k < 3; k++) { var p = f.P(5f + k * 2.2f, 0, PR + 4f); p = ctx.OnGround(p.x, p.z); mk.Box(WorldMaterials.Solid(towel[k]), p + up * .04f, new Vector3(1f, .04f, 2f), f.r); Interact.Seat(plife.transform, p + up * .1f, -f.Fwd, "Lie on the towel").seatHeight = .08f; }
            var cs = f.P(-9f, 0, PR + 7f); cs = ctx.OnGround(cs.x, cs.z); var cr = Quaternion.LookRotation(FaceTo(cs, g + f.Fwd * (PR + 14f)));
            mk.Box(white, cs + up * 1.3f, new Vector3(3f, 2.6f, 2.4f), cr); solid.Box(white, cs + up * 1.3f, new Vector3(3f, 2.6f, 2.4f), cr); mk.Box(GlowM(Warm, 1.6f), cs + up * 1.6f + cr * Vector3.forward * 1.21f, new Vector3(1.8f, .8f, .02f), cr);
            for (var k = 0; k < 5; k++) mk.Box(k % 2 == 0 ? WorldMaterials.Solid(Dye[5 % Dye.Length]) : white, cs + up * 2.7f + cr * new Vector3(-1.2f + k * .6f, 0, .9f), new Vector3(.6f, .06f, 1.6f), cr * Quaternion.Euler(12f, 0, 0));
            mk.Cone(WorldMaterials.Solid(new Color(.85f, .65f, .4f)), cs + up * 5.1f, .5f, -1.6f, 8); for (var k = 0; k < 3; k++) mk.Blob(white, cs + up * (5.2f + k * .45f), Vector3.one * (.55f - k * .13f), ctx.Rng, 0f);
            ArtistBits.Label(ctx.Root, "CREEMEES", cs + up * 2.3f + cr * Vector3.forward * 1.22f, cr * Vector3.forward, .26f, new Color(.2f, .25f, .5f), TextAnchor.MiddleCenter);
            Folk(cs + cr * new Vector3(2.2f, 0, 1.4f), cr * Vector3.forward, Resident.Pose.Lean, new[] { "Maple or maple-vanilla twist. That's the menu. It's a good menu.", "Small's large and large is a problem. I recommend the problem." }, Look(Residents.Tool.None, true));
            LogSeat(ctx, mk, f.P(-4f, 0, PR + 9f), -f.Fwd, 2.4f);
            Ring(g, 22f, 12f);
            Shot(ctx, "the swimming hole", f.P(-9f, 2.6f, PR + 16f), g + up * .5f);
        }

        // the treehouses: stairs round a great trunk to a platform, rope bridges to two more, a lookout
        void Treehouses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, TreesAt, 12f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float PH = 6f;
            var trunks = new[] { f.P(0, 0, 0), f.P(-10f, 0, -5f), f.P(9f, 0, -7f) }; var deck = new Vector3[3];
            for (var t = 0; t < 3; t++)
            {
                var b = ctx.OnGround(trunks[t].x, trunks[t].z); deck[t] = new Vector3(b.x, g.y + PH + t * .8f, b.z);
                mk.Prism(bark, b - up * .5f, up, 1.1f, deck[t].y - b.y + 9f, 9, .7f); ArtistBits.PostCollider(ctx.Root, b, deck[t].y - b.y + 6f, 1.1f);
                for (var k = 0; k < 6; k++) soft.Blob(leaf, deck[t] + up * (7f + (k % 3) * 1.4f) + new Vector3(Mathf.Cos(k * 1.05f), 0, Mathf.Sin(k * 1.05f)) * 3.4f, Vector3.one * 3.6f, ctx.Rng, .3f);
                mk.Prism(wood, deck[t] - up * .2f, up, 3.4f, .22f, 12); solid.Prism(wood, deck[t] - up * .2f, up, 3.4f, .22f, 12);
                for (var k = 0; k < 12; k++) { var a = k * 30f; if (t == 0 && Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 20f) continue; var p = deck[t] + Quaternion.Euler(0, a, 0) * Vector3.forward * 3.25f; mk.Box(wood, p + up * .5f, new Vector3(.1f, 1f, .1f), Quaternion.identity); }
                mk.Blob(GlowM(Warm, 2.4f), deck[t] + up * 2.6f + f.Right * 1.3f, Vector3.one * .16f, ctx.Rng, 0f); halos.Add(deck[t] + up * 2.6f + f.Right * 1.3f, 1.2f, Warm, .7f); ArtistBits.PointLight(ctx.Root, deck[t] + up * 2.4f, Warm, 11f, 1.3f);
            }
            // railings as colliders, with gaps where the bridges and the stair arrive
            for (var t = 0; t < 3; t++) for (var k = 0; k < 12; k++) { var a = k * 30f + 15f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var open = false; for (var o = 0; o < 3; o++) if (o != t && Vector3.Angle(d, FaceTo(deck[t], deck[o])) < 24f && !(t != 0 && o != 0)) open = true; if (t == 0 && Vector3.Angle(d, -f.Fwd) < 24f) open = true; if (!open) Col(deck[t] + d * 3.3f + up * .5f, new Vector3(1.8f, 1f, .15f), Quaternion.LookRotation(d)); }
            // the stair, winding up the first trunk
            var steps = 22; for (var k = 0; k < steps; k++)
            {
                var a = (180f - (steps - k) * 16f) * Mathf.Deg2Rad; var d = f.r * new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); var y = Mathf.Lerp(ctx.Ground(trunks[0].x + d.x * 2f, trunks[0].z + d.z * 2f), deck[0].y, (k + 1f) / steps);
                var c = new Vector3(trunks[0].x, 0, trunks[0].z) + d * 2f; var rot = Quaternion.LookRotation(d);
                mk.Box(wood, new Vector3(c.x, y - .1f, c.z), new Vector3(.75f, .2f, 1.7f), rot); solid.Box(wood, new Vector3(c.x, y - .2f, c.z), new Vector3(.8f, .4f, 1.8f), rot);
            }
            // rope bridges from the first deck to the others
            for (var t = 1; t < 3; t++)
            {
                var a = deck[0] + FaceTo(deck[0], deck[t]) * 3.2f; var b = deck[t] + FaceTo(deck[t], deck[0]) * 3.2f; var n = Mathf.CeilToInt(Vector3.Distance(a, b) / .8f);
                for (var k = 0; k < n; k++) { var p0 = Vector3.Lerp(a, b, k / (float)n) - up * Mathf.Sin(k / (float)n * Mathf.PI) * .5f; var p1 = Vector3.Lerp(a, b, (k + 1f) / n) - up * Mathf.Sin((k + 1f) / n * Mathf.PI) * .5f; var rot = Quaternion.LookRotation(p1 - p0); mk.Box(wood, (p0 + p1) / 2f - up * .1f, new Vector3(1.3f, .08f, Vector3.Distance(p0, p1) - .08f), rot); solid.Box(wood, (p0 + p1) / 2f - up * .2f, new Vector3(1.3f, .3f, Vector3.Distance(p0, p1) + .05f), rot); foreach (var e in new[] { -.7f, .7f }) { mk.Box(canvas, (p0 + p1) / 2f + rot * new Vector3(e, .9f, 0), new Vector3(.05f, .05f, Vector3.Distance(p0, p1)), rot); Col((p0 + p1) / 2f + rot * new Vector3(e, .45f, 0), new Vector3(.1f, 1.1f, Vector3.Distance(p0, p1)), rot); } }
            }
            // a little hut on the second deck, a bench and a lookout on the third
            mk.Box(wood, deck[1] + up * 1.3f - f.Fwd * 1.2f, new Vector3(3f, 2.4f, 2.2f), f.r); mk.GableRoof(WorldMaterials.Solid(new Color(.45f, .3f, .22f)), deck[1] + up * 2.5f - f.Fwd * 1.2f, new Vector3(3.4f, 0, 2.8f), 1f, f.r); mk.Box(GlowM(Warm, 1.5f), deck[1] + up * 1.4f - f.Fwd * .09f, new Vector3(.9f, .8f, .02f), f.r); Col(deck[1] + up * 1.3f - f.Fwd * 1.2f, new Vector3(3f, 2.4f, 2.2f), f.r);
            var bd = FaceTo(deck[2], g + (deck[2] - g) * 3f); mk.Box(wood, deck[2] + bd * 2.3f + up * .45f, new Vector3(2f, .07f, .45f), Quaternion.LookRotation(bd)); Interact.Bench(plife.transform, deck[2] + bd * 2.3f + up * .5f - Vector3.Cross(up, bd) * .7f, deck[2] + bd * 2.3f + up * .5f + Vector3.Cross(up, bd) * .7f, bd);
            Folk(deck[0] + f.Right * 1.6f + up * .02f, -f.Right, Resident.Pose.Wave, new[] { "Come on up! Mind the bridge, it has opinions.", "We built it in a weekend. The weekend took three summers." });
            Folk(deck[2] + bd * 2.3f + up * .5f + Vector3.Cross(up, bd) * .5f, bd, Resident.Pose.Sit, new[] { "From here the lights look like a second sky.", "I can hear both stages. I like the bit in between." }, null, null, .5f);
            SignPost(ctx, mk, f.P(3f, 0, 9f), f.Fwd, "THE TREEHOUSES\nshoes optional", 2.6f);
            Lane(ctx, mk, f.P(0, 0, 8f)); Ring(g, 14f, 6f);
            Shot(ctx, "the treehouses", f.P(6f, 2f, 20f), deck[0] + up * 1f);
            Shot(ctx, "up in the treehouses", deck[0] + up * 1.7f + f.Right * 2f, deck[2] + up * 1f);
        }

        // the mirror dome: cushions under a turning mirror ball
        void MirrorDome(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, DomeAt, 10f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float R = 6.5f;
            mk.Prism(WorldMaterials.Solid(new Color(.35f, .2f, .45f)), g - up * .4f, up, R + .4f, .5f, 20); solid.Prism(wood, g - up * .6f, up, R + .4f, .7f, 20);
            var fo = g + up * .1f;
            for (var k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; Vector3 prev = fo + d * R;
                for (var s = 1; s <= 6; s++) { var a = s / 6f * Mathf.PI / 2f; var p = fo + d * Mathf.Cos(a) * R + up * Mathf.Sin(a) * R; mk.Box(white, (prev + p) / 2f, new Vector3(.12f, .12f, Vector3.Distance(prev, p)), Quaternion.LookRotation(p - prev)); if (s % 2 == 0) mk.Blob(GlowM(Dye[(k + s) % Dye.Length], 2.4f), p, Vector3.one * .12f, ctx.Rng, 0f); prev = p; }
                if (Vector3.Angle(d, f.Fwd) > 30f) ArtistBits.PostCollider(ctx.Root, fo + d * R, 2f, .2f);
                var c = fo + d * (R - 2f); var cm = WorldMaterials.Solid(Dye[k % Dye.Length], .3f); mk.Blob(cm, c + up * .22f, new Vector3(.6f, .22f, .6f), ctx.Rng, .1f); Interact.Seat(plife.transform, c + up * .38f, -d, "Sit on the cushion").seatHeight = .3f;
            }
            var ball = new MeshKit(); ball.Blob(WorldMaterials.Solid(new Color(.85f, .88f, .95f), .95f), Vector3.zero, Vector3.one * .8f, prng, .12f); for (var k = 0; k < 14; k++) ball.Blob(GlowM(Color.white, 2f), Quaternion.Euler(k * 47f, k * 83f, 0) * Vector3.up * .8f, Vector3.one * .07f, prng, 0f);
            var bgo = ball.BuildSingle("Mirror ball", plife.transform, false); bgo.transform.position = fo + up * (R - 1.6f); plife.mirror = bgo.transform; mk.Box(metal, fo + up * (R - .5f), new Vector3(.04f, 1.2f, .04f), Quaternion.identity);
            plife.domeLight = ArtistBits.PointLight(ctx.Root, fo + up * (R - 2.6f), Dye[4], 13f, 2f);
            Folk(fo + Quaternion.Euler(0, 90f, 0) * Vector3.forward * (R - 2f) + up * .38f, Quaternion.Euler(0, -90f, 0) * Vector3.forward, Resident.Pose.Sit, new[] { "Watch the lights go round. That's it. That's the activity.", "I came in for a minute in June." }, null, null, .3f);
            Lane(ctx, mk, f.P(0, 0, R + 1f)); Ring(g, R + 1.5f, 8f);
            Shot(ctx, "the mirror dome", f.P(4f, 1.8f, R + 9f), fo + up * 2.5f);
        }

        // Sunrise Hill: a gong to ring, mats, prayer flags, the whole land below
        void SunriseHill(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, HillAt, 10f); var f = new PlaceFrame(g, FaceTo(g, center));
            foreach (var e in new[] { -1.4f, 1.4f }) { mk.Prism(wood, f.P(e, -.3f, -3f), up, .14f, 3.3f, 6); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, -3f), 3f, .16f); }
            mk.Box(wood, f.P(0, 3f, -3f), new Vector3(3.4f, .2f, .2f), f.r);
            var gong = new MeshKit(); gong.Box(dark, new Vector3(0, -.3f, 0), new Vector3(.03f, .6f, .03f), Quaternion.identity); gong.Prism(WorldMaterials.Solid(new Color(.85f, .65f, .25f), .85f), new Vector3(0, -1.5f, -.04f), Vector3.forward, .9f, .08f, 18); gong.Prism(GlowM(new Color(1f, .8f, .35f), 1.2f), new Vector3(0, -1.5f, .03f), Vector3.forward, .25f, .03f, 12);
            var ggo = gong.BuildSingle("Gong", plife.transform, false); ggo.transform.SetPositionAndRotation(f.P(0, 2.95f, -3f), f.r);
            var gt = Interact.Toy(ggo, Toy.Kind.Custom, "Ring the gong", new Vector3(2f, 2f, .6f)); ggo.GetComponent<BoxCollider>().center = new Vector3(0, -1.5f, 0); gt.onUse = _ => plife.Gong(ggo.transform);
            plife.gongLight = ArtistBits.PointLight(ctx.Root, f.P(0, 2f, -1.5f), new Color(1f, .8f, .4f), 16f, 1f);
            // prayer flags from the gong posts out to two stakes
            foreach (var e in new[] { -1f, 1f }) { var a = f.P(e * 1.4f, 3f, -3f); var b = ctx.OnGround(f.P(e * 7f, 0, 3f).x, f.P(e * 7f, 0, 3f).z) + up * 1.2f; mk.Prism(wood, b - up * 1.4f, up, .06f, 1.5f, 6); for (var k = 1; k < 12; k++) { var t = k / 12f; var p = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .5f; mk.Box(WorldMaterials.Solid(Dye[k % Dye.Length], .3f), p - up * .2f, new Vector3(.3f, .36f, .02f), Quaternion.LookRotation(Vector3.Cross(up, b - a))); } }
            for (var k = 0; k < 4; k++) { var p = f.P(-3.3f + k * 2.2f, 0, 2.5f); p = ctx.OnGround(p.x, p.z); mk.Box(WorldMaterials.Solid(Dye[(k + 3) % Dye.Length], .3f), p + up * .03f, new Vector3(.7f, .04f, 1.8f), f.r); Interact.Seat(plife.transform, p + up * .08f, f.Fwd, "Sit on the mat").seatHeight = .06f; }
            Folk(f.P(-3.3f + 2.2f, .08f, 2.5f), f.Fwd, Resident.Pose.Sit, new[] { "Breathe in for four bars. Out for a whole jam.", "Ring the gong if you like. It likes it." }, null, null, .06f);
            Lane(ctx, mk, f.P(0, 0, 7f)); Ring(g, 7f, 8f);
            Shot(ctx, "Sunrise Hill", f.P(5f, 2f, -10f), f.P(0, 1f, 6f));
        }

        // the poster gallery: easels of glowing poster art, a kaleidoscope to spin
        void Gallery(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, GalleryAt, 8f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            for (var k = 0; k < 6; k++)
            {
                var a = (k - 2.5f) * 24f; var d = Quaternion.Euler(0, a, 0) * -f.Fwd; var p = g + d * 6f; p = ctx.OnGround(p.x, p.z); var face = -d; var rot = Quaternion.LookRotation(face);
                foreach (var e in new[] { -.5f, .5f }) mk.Box(wood, p + rot * new Vector3(e, 1f, 0), new Vector3(.07f, 2f, .07f), rot * Quaternion.Euler(-8f, 0, 0)); mk.Box(wood, p + up * 1f - face * .5f, new Vector3(.07f, 2f, .07f), rot * Quaternion.Euler(14f, 0, 0));
                mk.Box(dark, p + up * 1.4f + face * .1f, new Vector3(1.3f, 1.7f, .05f), rot); mk.Box(GlowM(Dye[k], 1.1f), p + up * 1.4f + face * .13f, new Vector3(1.15f, 1.55f, .02f), rot);
                for (var s = 0; s < 3; s++) mk.Prism(GlowM(Dye[(k + 2 + s * 2) % Dye.Length], 1.8f), p + up * (1f + s * .4f) + face * .15f + rot * Vector3.right * ((s - 1) * .25f), face, .28f - s * .06f, .02f, s == 1 ? 3 : 14);
                Col(p + up * 1.1f, new Vector3(1.3f, 2.2f, .6f), rot);
            }
            var ks = g - f.Fwd * 0f + f.Right * 0f; mk.Prism(wood, ks - up * .2f, up, .08f, 1.4f, 6); ArtistBits.PostCollider(ctx.Root, ks, 1.3f, .12f);
            var kal = new MeshKit(); kal.Prism(metal, new Vector3(0, 0, -.4f), Vector3.forward, .12f, .8f, 6); for (var s = 0; s < 6; s++) kal.Blob(GlowM(Dye[s], 2.4f), new Vector3(Mathf.Cos(s * 1.047f) * .2f, Mathf.Sin(s * 1.047f) * .2f, .42f), Vector3.one * .07f, prng, 0f);
            var kgo = kal.BuildSingle("Kaleidoscope", plife.transform, false); kgo.transform.SetPositionAndRotation(ks + up * 1.35f, Quaternion.LookRotation(-f.Fwd));
            var kt = Interact.Toy(kgo, Toy.Kind.Spin, "Turn the kaleidoscope", new Vector3(.6f, .6f, 1f)); kt.axis = Vector3.forward; kt.amount = 720f; kt.seconds = 2.5f;
            Folk(g - f.Fwd * 4.4f + f.Right * 2.6f, -f.Fwd, Resident.Pose.Work, new[] { "Every show gets a poster. Every poster gets argued about.", "I only paint in colours you can hear." }, Look(Residents.Tool.Broom));
            LogSeat(ctx, mk, f.P(0, 0, 3.5f), -f.Fwd, 2.4f);
            ArtistBits.PointLight(ctx.Root, g + up * 3.4f - f.Fwd * 2f, Warm, 13f, 1.3f);
            Lane(ctx, mk, f.P(0, 0, 6f)); Ring(g, 8f, 8f);
            Shot(ctx, "the poster gallery", f.P(3f, 1.8f, 9f), g + up * 1.3f - f.Fwd * 4f);
        }

        // the sugar shack by the covered bridge on the north road
        void SugarShack(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, SugarAt, 6f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            mk.Box(wood, g + up * 1.4f, new Vector3(5f, 3.2f, 4f), f.r); solid.Box(wood, g + up * 1.4f, new Vector3(5f, 3.2f, 4f), f.r); mk.GableRoof(WorldMaterials.Solid(new Color(.3f, .32f, .36f)), g + up * 3f, new Vector3(5.6f, 0, 4.6f), 1.5f, f.r);
            mk.Box(metal, g + up * 4.6f, new Vector3(1.6f, .8f, 1f), f.r); life.fires.Add(g + up * 5.2f);   // the steam vent
            mk.Box(dark, f.P(-1.2f, 1.1f, 2.01f), new Vector3(1f, 2.2f, .04f), f.r); mk.Box(GlowM(Warm, 1.7f), f.P(1.2f, 1.6f, 2.01f), new Vector3(1.2f, .9f, .04f), f.r);
            mk.Blob(GlowM(Warm, 2.2f), f.P(-1.2f, 2.6f, 2.2f), Vector3.one * .14f, ctx.Rng, 0f); halos.Add(f.P(-1.2f, 2.6f, 2.3f), 1.2f, Warm, .7f); ArtistBits.PointLight(ctx.Root, f.P(0, 2.6f, 4f), Warm, 13f, 1.4f);
            for (var k = 0; k < 5; k++) for (var r = 0; r < 3 - (k % 2); r++) mk.Prism(bark, f.P(3f, .2f + r * .34f, -1.6f + k * .36f), f.Right, .17f, 1.2f, 6); Col(f.P(3.6f, .6f, -.9f), new Vector3(1.2f, 1.2f, 2f), f.r);
            // maples with sap buckets
            for (var k = 0; k < 4; k++) { var p = f.P(-6f + k * 4.2f, 0, -5.5f - (k % 2) * 2f); p = ctx.OnGround(p.x, p.z); if (!ctx.IsLand(p.x, p.z, 2f)) continue; Tree(ctx, mk, p, 1.2f); mk.Prism(metal, p + up * 1.1f + f.Fwd * .45f, up, .16f, .3f, 8); }
            Folk(f.P(-2.6f, 0, 3.2f), f.Fwd, Resident.Pose.Lean, new[] { "Forty gallons of sap for one of syrup. Worth every drop.", "Boiling tonight. Boiling every night. It's not a fast business.", "Try the snow. I put syrup on it. That's dinner." }, Look(Residents.Tool.Mug, true));
            LogSeat(ctx, mk, f.P(1.5f, 0, 4.5f), f.Fwd, 2.2f);
            SignPost(ctx, mk, f.P(4.5f, 0, 5f), f.Fwd, "SUGAR SHACK\nboiling tonight", 2.4f);
            // the covered bridge, over the road
            var north = mainRoads[mainRoads.Count - 1]; if (north.Count > 8)
            {
                var i = north.Count / 2; var a = north[Mathf.Max(0, i - 3)]; var b = north[Mathf.Min(north.Count - 1, i + 3)]; var d = (b - a); d.y = 0; d.Normalize(); var c = north[i]; var rot = Quaternion.LookRotation(d); var side = Vector3.Cross(up, d); const float L = 10f, Hh = 4f, Wd = 5.6f;
                foreach (var e in new[] { -1f, 1f }) { mk.Box(red, c + side * e * Wd / 2f + up * .6f, new Vector3(.2f, 1.6f, L), rot); mk.Box(red, c + side * e * Wd / 2f + up * (Hh - .5f), new Vector3(.2f, 1f, L), rot); foreach (var z in new[] { -L / 2f + .2f, -L / 6f, L / 6f, L / 2f - .2f }) mk.Box(red, c + side * e * Wd / 2f + d * z + up * Hh / 2f, new Vector3(.22f, Hh, .3f), rot); Col(c + side * e * Wd / 2f + up * Hh / 2f, new Vector3(.3f, Hh, L), rot); }
                mk.Box(wood, c + up * .12f, new Vector3(Wd, .1f, L), rot); mk.GableRoof(WorldMaterials.Solid(new Color(.3f, .32f, .36f)), c + up * Hh, new Vector3(Wd + .8f, 0, L + .6f), 1.6f, rot);
                foreach (var z in new[] { -3f, 3f }) { mk.Blob(GlowM(Warm, 2.4f), c + d * z + up * (Hh - .5f), Vector3.one * .16f, ctx.Rng, 0f); halos.Add(c + d * z + up * (Hh - .5f), 1.2f, Warm, .7f); }
                ArtistBits.PointLight(ctx.Root, c + up * 3f, Warm, 12f, 1.5f);
                Shot(ctx, "the covered bridge", c - d * 14f + up * 1.8f + side * 1f, c + up * 2f);
            }
            Lane(ctx, mk, f.P(0, 0, 4f)); Ring(g, 8f, 6f);
            Shot(ctx, "the sugar shack", f.P(5f, 1.8f, 12f), g + up * 1.6f);
        }

        // a bonfire on the beach
        void BeachFire(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, BeachAt, 6f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 70f, g.z + d.z * 70f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.left; var f = new PlaceFrame(g, toSea.normalized);
            Bonfire(ctx, mk, g, 1.5f);
            for (var k = 0; k < 5; k++) { var a = 110f + k * 35f; var p = g + f.r * (Quaternion.Euler(0, a, 0) * Vector3.forward) * 5f; LogSeat(ctx, mk, p, FaceTo(p, g), 2.2f); }
            foreach (var e in new[] { -5f, 5f }) { var t = f.P(e, 0, 5f); t = ctx.OnGround(t.x, t.z); mk.Prism(wood, t - up * .2f, up, .07f, 2.2f, 6); mk.Blob(GlowM(new Color(1f, .6f, .25f), 2.6f), t + up * 2.2f, new Vector3(.14f, .22f, .14f), ctx.Rng, .2f); halos.Add(t + up * 2.2f, 1.2f, new Color(1f, .6f, .25f), .7f); ArtistBits.PostCollider(ctx.Root, t, 2.1f, .1f); }
            Folk(ctx.OnGround(f.P(-3.4f, 0, -3.6f).x, f.P(-3.4f, 0, -3.6f).z) + up * .5f, FaceTo(f.P(-3.4f, 0, -3.6f), g), Resident.Pose.Sit, new[] { "The tide keeps time. Slow, but it never drops a beat.", "Somebody always has a guitar. Tonight nobody does. It's nice." }, null, null, .5f);
            var bm = new MeshKit(); bm.Prism(WorldMaterials.Solid(new Color(.4f, .7f, .55f), .8f), Vector3.zero, Vector3.forward, .09f, .3f, 8, .5f); bm.Blob(canvas, new Vector3(0, 0, .1f), new Vector3(.04f, .04f, .1f), prng, 0f);
            var bgo = bm.BuildSingle("Message in a bottle", plife.transform, false); bgo.transform.position = f.P(4f, .2f, 7f); Interact.Carry(bgo, "the bottle with a note in it", .3f, -6f);
            Lane(ctx, mk, f.P(0, 0, -8f)); Ring(g, 8f, 8f);
            Shot(ctx, "the bonfire on the beach", f.P(-5f, 1.8f, -10f), g + up * .8f + f.Fwd * 4f);
        }
    }

    /// The Commonwealth places' moving parts: the Wheel turning (on the clock, so everyone rides together), the mirror
    /// ball, the gong, the rope swing, the drum circle's light.
    public class JamPlacesLife : MonoBehaviour
    {
        public Transform wheel, mirror; public Quaternion wheelRest; public List<Transform> gondolas = new List<Transform>();
        public Vector3 hub, wheelAcross; public float wheelR; public Light domeLight, gongLight, beatLight;
        Transform gong, swing; float gongT = -1f, swingT = -1f, beat; Quaternion gongRest, swingRest;
        static readonly Color[] Dye = { new Color(1f, .3f, .35f), new Color(1f, .62f, .2f), new Color(1f, .9f, .25f), new Color(.3f, .85f, .45f), new Color(.25f, .6f, 1f), new Color(.7f, .4f, 1f) };

        public void Gong(Transform g) { if (gong == null) { gong = g; gongRest = g.rotation; } gongT = 0f; }
        public void Swing(Transform s) { if (swing == null) { swing = s; swingRest = s.rotation; } swingT = 0f; }
        public void Beat(Vector3 at) { beat = 1f; if (beatLight != null) beatLight.color = Dye[Random.Range(0, Dye.Length)]; }

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            if (wheel != null)
            {
                var ang = (float)(System.DateTime.UtcNow.TimeOfDay.TotalSeconds % 90.0) / 90f * 360f;
                wheel.rotation = wheelRest * Quaternion.Euler(0, 0, ang);
                var right = Vector3.Cross(Vector3.up, wheelAcross).normalized;
                for (var k = 0; k < gondolas.Count; k++) { var a = (ang + k * 360f / gondolas.Count) * Mathf.Deg2Rad; gondolas[k].position = hub + (right * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * wheelR; }
            }
            if (mirror != null) mirror.rotation = Quaternion.Euler(0, t * 25f, 0);
            if (domeLight != null) domeLight.color = Color.Lerp(Dye[(int)(t * .25f) % Dye.Length], Dye[((int)(t * .25f) + 1) % Dye.Length], Mathf.Repeat(t * .25f, 1f));
            if (gong != null && gongT >= 0f) { gongT += dt; gong.rotation = gongRest * Quaternion.Euler(Mathf.Sin(gongT * 5f) * 16f * Mathf.Exp(-gongT * .6f), 0, 0); if (gongLight != null) gongLight.intensity = 1f + 4f * Mathf.Exp(-gongT * .7f) * (.7f + .3f * Mathf.Sin(gongT * 14f)); if (gongT > 8f) { gongT = -1f; gong.rotation = gongRest; } }
            if (swing != null && swingT >= 0f) { swingT += dt; swing.rotation = swingRest * Quaternion.Euler(Mathf.Sin(swingT * 2.2f) * 38f * Mathf.Exp(-swingT * .25f), 0, 0); if (swingT > 14f) { swingT = -1f; swing.rotation = swingRest; } }
            if (beatLight != null) { beat = Mathf.MoveTowards(beat, 0f, dt * 2.5f); beatLight.intensity = .6f + beat * 3f; }
        }
    }
}
