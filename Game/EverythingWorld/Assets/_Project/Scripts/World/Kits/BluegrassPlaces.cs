using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Bluegrass Hollows laid out on purpose (the build-land standard). The town, the creek and every picker stay
    /// where they were; three more trails with jar lamps reach the empty parts without crossing the creek: a ridge
    /// road north up the mountain, a west road down to the low meadow, a short south road up the hill. Along them,
    /// with the valley's people: the Square Dance (a lantern-hung pavilion, four couples and a caller); the Luthier's
    /// Workshop (banjos and fiddles hanging to pluck); the Quilting Porch (a farmhouse with quilts on the line in
    /// every colour); the High Lonesome (a bench under a lone pine on the top of the ridge, a few old stones); the
    /// Horse Pasture (a split-rail paddock, horses to pat, a hitching rail); the Schoolhouse (one room, desks to sit
    /// at, a bell to ring). You can sit in every rocking chair in the valley.
    public partial class BluegrassKit
    {
        static readonly Vector2 DanceAt = new Vector2(6352, 4992), LuthierAt = new Vector2(6232, 5062), QuiltAt = new Vector2(6170, 5140), LonesomeAt = new Vector2(6200, 5226), PastureAt = new Vector2(5822, 4640), SchoolAt = new Vector2(6300, 4600);
        static readonly Vector2[] RidgeWay = { new Vector2(6440, 4905), new Vector2(6385, 4960), new Vector2(6300, 5010), new Vector2(6245, 5045), new Vector2(6200, 5110), new Vector2(6195, 5170), new Vector2(6200, 5212) };
        static readonly Vector2[] WestWay = { new Vector2(6040, 4745), new Vector2(5985, 4690), new Vector2(5910, 4650), new Vector2(5845, 4642) };
        static readonly Vector2[] SouthWay = { new Vector2(6340, 4705), new Vector2(6312, 4650), new Vector2(6302, 4618) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (DanceAt, 12f), (LuthierAt, 9f), (QuiltAt, 11f), (LonesomeAt, 8f), (PastureAt, 16f), (SchoolAt, 10f) };

        BluegrassPlacesLife plife; System.Random prng; Transform rootT; readonly List<Vector3> roadPts = new List<Vector3>();

        static readonly Color[] Shirts = { new Color(.3f, .42f, .62f), new Color(.75f, .25f, .22f), new Color(.9f, .85f, .7f), new Color(.4f, .55f, .35f), new Color(.85f, .65f, .25f), new Color(.6f, .4f, .6f), new Color(.2f, .25f, .35f) };
        static readonly Color[] Denim = { new Color(.25f, .35f, .55f), new Color(.2f, .28f, .45f), new Color(.5f, .42f, .3f), new Color(.3f, .3f, .32f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Straw, Residents.Hat.Cowboy, Residents.Hat.None };
        static readonly string[] Valley =
        {
            "Afternoon. Listen: somebody's always pickin' somewhere.", "You here for the convention? It's every day. That's the convention.", "Creek's up. Banjo's in tune. Good day.",
            "Mine's shut, mostly. The songs about it are still open.", "Fast ones get your feet. Slow ones get the rest of you.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Shirts, Denim, Hats, Shirts); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Valley, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);

        void Board(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.4f, float size = .17f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(wood, g + rot * Vector3.right * e * (w / 2f - .1f) + up * .9f, new Vector3(.1f, 1.8f, .1f), rot);
            mk.Box(trim, g + up * 1.55f, new Vector3(w, .7f, .08f), rot); Col(g + up * 1f, new Vector3(w, 2f, .15f), rot);
            ArtistBits.Label(ctx.Root, text, g + up * 1.57f + face * .05f, face, size, new Color(.5f, .15f, .12f), TextAnchor.MiddleCenter);
        }

        void PlankBench(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(plank, g + up * .45f, new Vector3(len, .07f, .4f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(wood, g + up * .22f + side * e * (len / 2f - .2f), new Vector3(.08f, .45f, .36f), rot);
            Col(g + up * .3f, new Vector3(len, .6f, .4f), rot); Interact.Bench(plife.transform, g + up * .5f - side * (len / 2f - .35f), g + up * .5f + side * (len / 2f - .35f), face);
        }

        // ---------------------------------------------------------------- the trails
        void Bend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                foreach (var c in noGrass) { if (c.y < 4f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
            }
        }

        void JarTrail(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 150f * 150f; foreach (var (p, _) in walkPoints) { var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd && !CreekBetween(new Vector2(p.x, p.z), way[0])) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = Smooth(ctx, full, false, 2f); Bend(ctx, pts); pts.RemoveAll(p => InCreek(p.x, p.z, 3f)); if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.66f, .55f, .38f)), 2.8f, .05f, ctx);
            var up = Vector3.up; var n = 0;
            for (var i = 7; i < pts.Count; i += 14) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 2.1f : -2.1f); if (ctx.IsLand(lp.x, lp.z, 2f) && !InCreek(lp.x, lp.z, 2f)) JarLamp(ctx, mk, ctx.OnGround(lp.x, lp.z)); }
            foreach (var p in pts) Remember(p, 700);
            roadPts.AddRange(pts);
        }

        bool CreekBetween(Vector2 a, Vector2 b) { for (var t = 0f; t <= 1f; t += .05f) { var p = Vector2.Lerp(a, b, t); if (InCreek(p.x, p.y, 2f)) return true; } return false; }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.5f; var b = new Vector2(from.x, from.z) + d * (len - stopShort);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + new Vector2(-d.y, d.x) * Mathf.Min(5f, len * .1f), b }, false, 2f, 1f); pts.RemoveAll(p => InCreek(p.x, p.z, 2f)); if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.66f, .55f, .38f)), 2f, .045f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void HollowPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); rootT = ctx.Root; prng = new System.Random(1945);
            plife = new GameObject("Bluegrass places life").AddComponent<BluegrassPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            JarTrail(ctx, mk, RidgeWay); JarTrail(ctx, mk, WestWay); JarTrail(ctx, mk, SouthWay);
            SquareDance(ctx, mk, solid); Luthier(ctx, mk, solid); Quilting(ctx, mk, solid); HighLonesome(ctx, mk, solid); Pasture(ctx, mk, solid); Schoolhouse(ctx, mk, solid);
            // folk on the main street
            if (street.Count > 4)
            {
                var route = new List<Vector3>(); for (var i = 1; i < street.Count - 1; i += 2) { var d = street[Mathf.Min(i + 1, street.Count - 1)] - street[i - 1]; d.y = 0; d.Normalize(); var p = street[i] + Vector3.Cross(Vector3.up, d) * 2.4f; route.Add(ctx.OnGround(p.x, p.z)); }
                for (var i = street.Count - 2; i > 0; i -= 2) { var d = street[Mathf.Min(i + 1, street.Count - 1)] - street[i - 1]; d.y = 0; d.Normalize(); var p = street[i] - Vector3.Cross(Vector3.up, d) * 2.4f; route.Add(ctx.OnGround(p.x, p.z)); }
                if (route.Count > 3) { Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, null, null, route.ToArray()); var half = route.Count / 2; var r2 = new List<Vector3>(); for (var i = 0; i < route.Count; i++) r2.Add(route[(i + half) % route.Count]); Folk(r2[0], FaceTo(r2[0], r2[1]), Resident.Pose.Walk, null, Look(Residents.Tool.Mug), r2.ToArray()); }
            }
        }

        // the Square Dance
        void SquareDance(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(DanceAt.x, DanceAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float S = 12f;
            mk.Box(plank, g + up * .1f, new Vector3(S, .24f, S), f.r); solid.Box(plank, g + up * .0f, new Vector3(S, .44f, S), f.r); var fo = new PlaceFrame(g + up * .22f, f.Fwd);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { mk.Box(wood, fo.P(x * (S / 2f - .3f), 2f, z * (S / 2f - .3f)), new Vector3(.25f, 4f, .25f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(x * (S / 2f - .3f), 0, z * (S / 2f - .3f)), 4f, .16f); }
            mk.GableRoof(roofM, fo.P(0, 4f, 0), new Vector3(S + 1.2f, 0, S + 1.2f), 2.2f, f.r);
            for (var k = 0; k < 4; k++) { var d = f.r * (Quaternion.Euler(0, k * 90f, 0) * Vector3.forward); var sd = Vector3.Cross(up, d); for (var s = 1; s < 8; s++) { var t = s / 8f; var p = fo.o + d * (S / 2f - .3f) + sd * (S / 2f - .3f) * (t * 2f - 1f) + up * (3.7f - Mathf.Sin(t * Mathf.PI) * .5f); mk.Blob(GlowM(Warm, 2.4f), p, Vector3.one * .1f, ctx.Rng, 0f); if (s % 3 == 0) halos.Add(p, .7f, Warm, .6f); } }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 3.2f, 0), Warm, 16f, 1.8f);
            // four couples in a square, the caller on a crate, a fiddler beside him
            for (var k = 0; k < 4; k++)
            {
                var d = f.r * (Quaternion.Euler(0, k * 90f, 0) * Vector3.forward); var sd = Vector3.Cross(up, d);
                foreach (var e in new[] { -.6f, .6f }) { var l = Look(); if (e > 0) l.longHair = true; Folk(fo.o + d * 2.6f + sd * e, -d, Resident.Pose.Wave, new[] { "Swing your partner! That's all I know. It's enough.", "We've been promenadin' since noon.", "Allemande left! Your other left." }, l); }
            }
            mk.Box(wood, fo.P(-4.4f, .25f, -4.4f), new Vector3(.8f, .5f, .8f), f.r); Folk(fo.P(-4.4f, .5f, -4.4f), FaceTo(fo.P(-4.4f, 0, -4.4f), fo.o), Resident.Pose.Wave, new[] { "Bow to your partner, bow to your corner, bow to the fella who just walked in!", "Circle left! No, the other... well, circle." }, Look());
            Folk(fo.P(-3.2f, 0, -4.8f), FaceTo(fo.P(-3.2f, 0, -4.8f), fo.o), Resident.Pose.Work, new[] { "Same tune since Tuesday. Nobody's complained.", "I only know the fast ones." }, Look(Residents.Tool.Rod)).workRate = 3f;
            foreach (var e in new[] { -1f, 1f }) PlankBench(ctx, mk, f.P(e * (S / 2f + 1.6f), 0, 0), -f.Right * e, 3f);
            Board(ctx, mk, f.P(S / 2f + 1f, 0, S / 2f + 3f), f.Fwd, "SQUARE DANCE\nall afternoon", 2.6f);
            Lane(ctx, mk, f.P(0, 0, S / 2f + 1f)); Ring(g, 10f);
            Shot(ctx, "the square dance", f.P(-7f, 2.4f, S / 2f + 8f), fo.o + up * 1.2f);
        }

        // the Luthier's Workshop: instruments hanging to pluck
        void Luthier(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(LuthierAt.x, LuthierAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            mk.Box(plank, f.P(0, 1.5f, -2.4f), new Vector3(7f, 3f, .15f), f.r); Col(f.P(0, 1.5f, -2.4f), new Vector3(7f, 3f, .2f), f.r); foreach (var e in new[] { -3.4f, 3.4f }) { mk.Box(plank, f.P(e, 1.5f, -.9f), new Vector3(.15f, 3f, 3f), f.r); Col(f.P(e, 1.5f, -.9f), new Vector3(.2f, 3f, 3f), f.r); }
            mk.Box(roofM, f.P(0, 3.2f, -.4f), new Vector3(7.6f, .12f, 4.8f), f.r * Quaternion.Euler(8f, 0, 0)); foreach (var e in new[] { -3.4f, 3.4f }) { mk.Box(wood, f.P(e, 1.4f, 1.6f), new Vector3(.16f, 2.8f, .16f), f.r); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, 1.6f), 2.8f, .1f); }
            mk.Box(wood, f.P(0, .9f, -1.7f), new Vector3(4.4f, .1f, .9f), f.r); foreach (var e in new[] { -2f, 2f }) mk.Box(wood, f.P(e, .45f, -1.7f), new Vector3(.12f, .9f, .8f), f.r); Col(f.P(0, .5f, -1.7f), new Vector3(4.4f, 1f, .9f), f.r);
            // instruments hung along the back wall: pluck one and its strings shine
            string[] names = { "banjo", "fiddle", "mandolin", "guitar", "dulcimer" }; var body = WorldMaterials.Solid(new Color(.72f, .5f, .28f)); var pale = WorldMaterials.Solid(new Color(.93f, .9f, .8f));
            for (var k = 0; k < 5; k++)
            {
                var im = new MeshKit(); var str = new Material(WorldMaterials.Glow(Warm, .25f)) { name = "Strings" }; var round = k == 0 || k == 2;
                if (round) im.Prism(k == 0 ? pale : body, new Vector3(0, -.2f, 0), Vector3.forward, k == 0 ? .26f : .2f, .08f, 12); else im.Box(body, new Vector3(0, -.2f, .04f), new Vector3(k == 3 ? .42f : .26f, k == 4 ? .7f : .5f, .09f), Quaternion.identity);
                im.Box(dark, new Vector3(0, .35f, .05f), new Vector3(.05f, .7f, .03f), Quaternion.identity); im.Box(str, new Vector3(0, .15f, .1f), new Vector3(.03f, 1f, .01f), Quaternion.identity);
                var go = im.BuildSingle("Hanging " + names[k], plife.transform, false); go.transform.SetPositionAndRotation(f.P(-2.6f + k * 1.3f, 2f, -2.25f), f.r);
                var idx = plife.strings.Count; plife.strings.Add((str, -10f)); var sw = go.AddComponent<Sway>(); sw.axis = Vector3.forward; sw.degrees = 8f; sw.speed = 9f; sw.decay = 1.2f; sw.seconds = 4f;
                var t = Interact.Toy(go, Toy.Kind.Custom, "Pluck the " + names[k], new Vector3(.7f, 1.4f, .6f)); go.GetComponent<BoxCollider>().center = new Vector3(0, .1f, .2f); t.onUse = _ => { plife.Pluck(idx); sw.Push(); };
            }
            plife.stringLight = ArtistBits.PointLight(ctx.Root, f.P(0, 2.4f, -.6f), Warm, 11f, 1.3f);
            mk.Box(body, f.P(-.8f, 1f, -1.7f), new Vector3(.5f, .1f, .3f), f.r * Quaternion.Euler(0, 20f, 0)); mk.Box(pale, f.P(.8f, .98f, -1.6f), new Vector3(.7f, .05f, .1f), f.r);
            Folk(f.P(.4f, 0, -.7f), -f.Fwd, Resident.Pose.Work, new[] { "A banjo's just a drum that learned to talk.", "Go on, pluck one. They get lonesome hung up there.", "This fiddle's from a barn door. Still creaks, in the good way." }, Look(Residents.Tool.Rake, true)).workRate = .8f;
            PlankBench(ctx, mk, f.P(-4.6f, 0, 2.6f), f.Right, 1.8f); JarLamp(ctx, mk, ctx.OnGround(f.P(4.6f, 0, 3f).x, f.P(4.6f, 0, 3f).z));
            Board(ctx, mk, f.P(5f, 0, 6f), f.Fwd, "LUTHIER\nstrings & things", 2.2f);
            Lane(ctx, mk, f.P(0, 0, 5f)); Ring(g, 7f);
            Shot(ctx, "the luthier's workshop", f.P(-3f, 1.8f, 9f), f.P(0, 1.6f, -1.5f));
        }

        // the Quilting Porch
        void Quilting(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(QuiltAt.x, QuiltAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 8f, D = 6f, H = 3.2f;
            var fo = new PlaceFrame(g + up * .4f, f.Fwd);
            mk.Box(stone, g + f.Fwd * 1f - up * .2f, new Vector3(W + .4f, 1.2f, D + 2.8f), f.r); solid.Box(stone, g + f.Fwd * 1f - up * .1f, new Vector3(W + .4f, 1f, D + 2.8f), f.r);
            mk.Box(cream, fo.P(0, H / 2f, 0), new Vector3(W, H, D), f.r); solid.Box(cream, fo.P(0, H / 2f, 0), new Vector3(W, H, D), f.r); mk.GableRoof(roofM, fo.P(0, H, 0), new Vector3(D + .6f, 0, W + .6f), 2.2f, Quaternion.LookRotation(f.Right));
            mk.Box(roofM, fo.P(0, H - .2f, D / 2f + 1.2f), new Vector3(W + .4f, .1f, 2.6f), f.r * Quaternion.Euler(8f, 0, 0)); foreach (var e in new[] { -3.8f, 0f, 3.8f }) { mk.Box(trim, fo.P(e, 1.45f, D / 2f + 2.3f), new Vector3(.14f, 2.9f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(e, 0, D / 2f + 2.3f), 2.9f, .1f); }
            mk.Box(dark, fo.P(0, 1.1f, D / 2f + .02f), new Vector3(1f, 2.2f, .04f), f.r); foreach (var e in new[] { -2.5f, 2.5f }) mk.Box(GlowM(Warm, 1.5f), fo.P(e, 1.7f, D / 2f + .02f), new Vector3(1.3f, 1.1f, .04f), f.r);
            // a quilting frame on the porch, two quilters in rockers
            Color[] patch = { new Color(.85f, .2f, .22f), new Color(.2f, .4f, .75f), new Color(.95f, .8f, .25f), new Color(.3f, .6f, .35f), new Color(.95f, .93f, .88f), new Color(.65f, .35f, .65f) };
            for (var x = 0; x < 5; x++) for (var z = 0; z < 3; z++) mk.Box(WorldMaterials.Solid(patch[(x * 2 + z * 3) % patch.Length]), fo.P(-1f + x * .4f + 1.2f, .82f, D / 2f + .7f + z * .4f), new Vector3(.4f, .03f, .4f), f.r);
            foreach (var (x, z) in new[] { (1f, .5f), (3.2f, .5f), (3.2f, 1.9f), (1f, 1.9f) }) mk.Box(wood, fo.P(x, .4f, D / 2f + z), new Vector3(.06f, .8f, .06f), f.r); Col(fo.P(2.1f, .45f, D / 2f + 1.2f), new Vector3(2.3f, .9f, 1.5f), f.r);
            foreach (var e in new[] { -2.6f, -1.2f }) { Rocker(mk, solid, ctx, fo.P(e, 0, D / 2f + 1.2f), f.Fwd); }
            Folk(fo.P(-1.2f, .52f, D / 2f + 1.25f), f.Fwd, Resident.Pose.Sit, new[] { "Every patch is a shirt somebody danced a hole in.", "Sit a spell. That chair's been waitin' on you.", "This one's the wedding ring pattern. That one's just Tuesday." }, Look(Residents.Tool.None, true), null, .48f);
            for (var k = 0; k < 2; k++) { var st = fo.P(0, -.13f - k * .14f, D / 2f + 2.7f + k * .34f); mk.Box(plank, st, new Vector3(2f, .14f, .36f), f.r); solid.Box(plank, st, new Vector3(2f, .14f, .36f), f.r); }
            // quilts on the line, in every colour, bright against the green
            var p0 = ctx.OnGround(f.P(-9f, 0, 2f).x, f.P(-9f, 0, 2f).z); var p1 = ctx.OnGround(f.P(-9f, 0, 12f).x, f.P(-9f, 0, 12f).z);
            foreach (var p in new[] { p0, p1 }) { mk.Box(wood, p + up * 1.1f, new Vector3(.12f, 2.2f, .12f), f.r); mk.Box(wood, p + up * 2.1f, new Vector3(1.4f, .08f, .08f), f.r); ArtistBits.PostCollider(ctx.Root, p, 2.2f, .1f); }
            mk.Box(dark, (p0 + p1) / 2f + up * 2.12f, new Vector3(.02f, .02f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
            for (var q = 0; q < 4; q++) { var c = Vector3.Lerp(p0, p1, (q + .5f) / 4f) + up * 1.25f; for (var x = 0; x < 4; x++) for (var y = 0; y < 4; y++) mk.Box(WorldMaterials.Solid(patch[(x + y * 2 + q * 3) % patch.Length]), c + f.Fwd * (-.75f + x * .5f) + up * (-.6f + y * .42f), new Vector3(.04f, .42f, .5f), f.r); Col(c, new Vector3(.2f, 1.8f, 2f), f.r); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.4f, D / 2f + 1.4f), Warm, 12f, 1.4f);
            Board(ctx, mk, f.P(5.5f, 0, D / 2f + 7f), f.Fwd, "QUILTS", 1.8f);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 5f)); Ring(g, 9f); Ring((p0 + p1) / 2f, 6f);
            Shot(ctx, "the quilting porch", f.P(-5f, 2f, D / 2f + 13f), fo.P(-2f, 1.6f, 2f));
        }

        // the High Lonesome: a bench under a lone pine on top of the ridge, a few old stones
        void HighLonesome(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, LonesomeAt, 8f); var f = new PlaceFrame(g, FaceTo(g, center));
            var pine = ctx.OnGround(f.P(-3f, 0, -2f).x, f.P(-3f, 0, -2f).z); mk.Prism(bark, pine - up * .3f, (up + f.Right * .08f).normalized, .4f, 9f, 7, .5f); ArtistBits.PostCollider(ctx.Root, pine, 6f, .45f);
            var needle = WorldMaterials.Solid(new Color(.14f, .3f, .22f)); for (var k = 0; k < 4; k++) crowns.Cone(needle, pine + up * (5f + k * 1.8f) + f.Right * (.4f + k * .15f), 3.2f - k * .65f, 3f, 7, k * 25f);
            PlankBench(ctx, mk, g, f.Fwd, 2.2f);
            for (var k = 0; k < 5; k++) { var p = ctx.OnGround(f.P(3.5f + (k % 2) * 1.6f, 0, -3f + k * 1.4f).x, f.P(3.5f + (k % 2) * 1.6f, 0, -3f + k * 1.4f).z); var r = f.r * Quaternion.Euler((k * 37 % 9) - 4f, 90f, (k * 53 % 11) - 5f); mk.Box(stone, p + up * .4f, new Vector3(.55f, .9f, .12f), r); Col(p + up * .4f, new Vector3(.55f, .9f, .2f), r); }
            JarLamp(ctx, mk, ctx.OnGround(f.P(1.8f, 0, -.6f).x, f.P(1.8f, 0, -.6f).z));
            Folk(ctx.OnGround(f.P(-1.6f, 0, 2.6f).x, f.P(-1.6f, 0, 2.6f).z), f.Fwd, Resident.Pose.Stand, new[] { "You can hear three counties' worth of pickin' from up here, on a still day.", "My people are in those stones. They liked the view, too.", "High and lonesome. Well. High, anyhow; you're here now." },
                new Residents.Look { top = new Color(.2f, .2f, .22f), bottom = new Color(.2f, .2f, .22f), hat = Residents.Hat.Cowboy, hatColor = new Color(.2f, .18f, .16f), skin = new Color(.88f, .68f, .52f), hair = new Color(.8f, .8f, .78f) });
            Board(ctx, mk, f.P(-5.5f, 0, -5f), -f.Fwd, "THE HIGH LONESOME", 2.8f, .15f);
            Ring(g, 6f);
            Shot(ctx, "the high lonesome", f.P(4f, 2f, -8f), g + up * 1f + f.Fwd * 20f);
        }

        // the Horse Pasture
        void Pasture(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, PastureAt, 12f, (x, z) => !InCreek(x, z, 16f)); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 22f, D = 16f;
            Vector3 C(float x, float z) => ctx.OnGround(f.P(x, 0, z).x, f.P(x, 0, z).z);
            Fence(ctx, mk, solid, C(-W / 2f, -D / 2f), C(W / 2f, -D / 2f)); Fence(ctx, mk, solid, C(W / 2f, -D / 2f), C(W / 2f, D / 2f)); Fence(ctx, mk, solid, C(-W / 2f, D / 2f), C(-W / 2f, -D / 2f)); Fence(ctx, mk, solid, C(W / 2f, D / 2f), C(2f, D / 2f)); Fence(ctx, mk, solid, C(-2f, D / 2f), C(-W / 2f, D / 2f));
            Color[] coats = { new Color(.45f, .28f, .16f), new Color(.2f, .16f, .14f), new Color(.85f, .8f, .72f), new Color(.6f, .42f, .25f) };
            string[] horse = { "The horse breathes warm on your hand.", "She leans in. She'd like the apple you don't have.", "An ear flicks toward the music from town." };
            for (var k = 0; k < 4; k++)
            {
                var p = C(-7f + k * 4.6f, -3f + (k % 2) * 5f); var face = Quaternion.Euler(0, k * 70f + 20f, 0) * f.Fwd; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var coat = WorldMaterials.Solid(coats[k]); var mane = WorldMaterials.Solid(coats[k] * .5f);
                mk.Box(coat, p + up * 1.25f, new Vector3(.6f, .7f, 1.9f), rot); mk.Box(coat, p + up * 1.75f + face * 1.05f, new Vector3(.36f, .9f, .4f), rot * Quaternion.Euler(-35f, 0, 0)); mk.Box(coat, p + up * 2.15f + face * 1.45f, new Vector3(.3f, .34f, .7f), rot * Quaternion.Euler(20f, 0, 0));
                mk.Box(mane, p + up * 1.95f + face * .85f, new Vector3(.1f, .8f, .3f), rot * Quaternion.Euler(-35f, 0, 0)); mk.Box(mane, p + up * 1.2f - face * 1.05f, new Vector3(.12f, .8f, .12f), rot * Quaternion.Euler(-15f, 0, 0));
                foreach (var x in new[] { -.2f, .2f }) foreach (var z in new[] { -.7f, .7f }) mk.Box(coat, p + up * .45f + side * x + face * z, new Vector3(.14f, .9f, .14f), rot);
                Col(p + up * 1.1f + face * .2f, new Vector3(.7f, 2.2f, 2.6f), rot);
                var hgo = new GameObject("Horse"); hgo.transform.SetParent(plife.transform, false); hgo.transform.position = p + up * 1.4f + face * 1.3f; var hn = k;
                Interact.Toy(hgo, Toy.Kind.Custom, "Pat the horse", new Vector3(1.6f, 2f, 1.6f)).onUse = _ => Say(hgo.transform, p + up * 3f, horse[hn++ % horse.Length]);
            }
            // a hitching rail, a water trough, a hand leaning on the fence
            var hr = C(4f, D / 2f + 2.5f); foreach (var e in new[] { -1.2f, 1.2f }) mk.Box(wood, hr + f.Right * e + up * .55f, new Vector3(.12f, 1.1f, .12f), f.r); mk.Box(wood, hr + up * 1.05f, new Vector3(2.8f, .1f, .1f), f.r); Col(hr + up * .55f, new Vector3(2.8f, 1.1f, .15f), f.r);
            var tr = C(-6f, D / 2f - 1.2f); mk.Box(wood, tr + up * .3f, new Vector3(2f, .6f, .7f), f.r); mk.Box(water, tr + up * .56f, new Vector3(1.8f, .02f, .5f), f.r); Col(tr + up * .3f, new Vector3(2f, .6f, .7f), f.r);
            Folk(C(-3.2f, D / 2f + .7f), -f.Fwd, Resident.Pose.Lean, new[] { "The grey one's called Banjo. The others didn't get instruments.", "Hold your hand flat. Flatter. There you go.", "They come to the fence when the fiddlin' starts. Critics, the lot of them." }, Look());
            PlankBench(ctx, mk, f.P(8f, 0, D / 2f + 2.4f), -f.Fwd, 2f); JarLamp(ctx, mk, C(1.2f, D / 2f + 1.4f));
            Board(ctx, mk, f.P(-8f, 0, D / 2f + 5f), f.Fwd, "HORSES\n(friendly, hungry)", 2.4f);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 2f)); Ring(g, 13f);
            Shot(ctx, "the horse pasture", f.P(9f, 2.4f, D / 2f + 9f), g + up * 1.2f);
        }

        // the Schoolhouse: one room, desks to sit at, a bell to ring
        void Schoolhouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SchoolAt.x, SchoolAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 7f, D = 9f, H = 3.4f, T = .3f, Door = HumanScale.DoorWidth;
            var fo = new PlaceFrame(g + up * .3f, f.Fwd);
            mk.Box(stone, g - up * .3f, new Vector3(W + .4f, 1.2f, D + .4f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(barnRed, p, s, f.r); solid.Box(barnRed, p, s, f.r); }
            mk.Box(plank, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(plank, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T));
            foreach (var e in new[] { -1f, 1f }) { Wall(fo.P(e * (W / 2f - T / 2f), .5f, 0), new Vector3(T, 1f, D - T * 2f)); Wall(fo.P(e * (W / 2f - T / 2f), H - .4f, 0), new Vector3(T, .8f, D - T * 2f)); foreach (var z in new[] { -3f, 0f, 3f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, z), new Vector3(T, H, .9f)); foreach (var z in new[] { -1.5f, 1.5f }) { mk.Box(GlowM(Warm, .9f), fo.P(e * (W / 2f - T / 2f), 1.8f, z), new Vector3(.05f, 1.6f, 2.1f), f.r); solid.Box(trim, fo.P(e * (W / 2f - T / 2f), 1.8f, z), new Vector3(.1f, 1.6f, 2.1f), f.r); } }
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T));
            mk.Box(trim, fo.P(0, H + .06f, 0), new Vector3(W - T, .12f, D - T), f.r); mk.GableRoof(roofM, fo.P(0, H + .1f, 0), new Vector3(W + .7f, 0, D + .7f), 2.2f, f.r); solid.Box(roofM, fo.P(0, H + .4f, 0), new Vector3(W, .5f, D), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            for (var k = 0; k < 2; k++) { var st = f.P(0, .1f - k * .0f, D / 2f + .5f + k * .4f); mk.Box(stone, st, new Vector3(Door + .6f, .3f - k * .12f, .45f), f.r); solid.Box(stone, st, new Vector3(Door + .6f, .3f - k * .12f, .45f), f.r); }
            // inside: the chalkboard, the teacher's desk, two rows of desks to sit at
            mk.Box(dark, fo.P(0, 1.9f, -D / 2f + T + .03f), new Vector3(4f, 1.4f, .05f), f.r); ArtistBits.Label(ctx.Root, "Today's lesson:\nG, C and D.\n(That's the lesson.)", fo.P(0, 1.95f, -D / 2f + T + .07f), f.Fwd, .16f, new Color(.95f, .95f, .9f), TextAnchor.MiddleCenter);
            mk.Box(wood, fo.P(-1.6f, .78f, -D / 2f + 2f), new Vector3(1.6f, .08f, .8f), f.r); mk.Box(wood, fo.P(-1.6f, .39f, -D / 2f + 2f), new Vector3(1.4f, .78f, .6f), f.r); Col(fo.P(-1.6f, .4f, -D / 2f + 2f), new Vector3(1.6f, .85f, .8f), f.r); mk.Blob(WorldMaterials.Solid(new Color(.8f, .15f, .12f)), fo.P(-1.2f, .9f, -D / 2f + 2f), Vector3.one * .08f, ctx.Rng, 0f);
            for (var row = 0; row < 3; row++) foreach (var e in new[] { -1.4f, 1.4f })
            {
                var c = fo.P(e, 0, -.8f + row * 1.5f); mk.Box(wood, c + up * .7f - f.Fwd * .35f, new Vector3(1f, .06f, .5f), f.r); mk.Box(dark, c + up * .35f - f.Fwd * .35f, new Vector3(.06f, .7f, .4f), f.r); mk.Box(wood, c + up * .42f + f.Fwd * .25f, new Vector3(1f, .06f, .35f), f.r); mk.Box(dark, c + up * .2f + f.Fwd * .25f, new Vector3(.8f, .4f, .06f), f.r);
                Col(c + up * .37f - f.Fwd * .35f, new Vector3(1f, .74f, .5f), f.r); Interact.Seat(plife.transform, c + up * .48f + f.Fwd * .25f, -f.Fwd, "Sit at the desk");
            }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.6f, 0), Warm, 11f, 1.3f, true);
            Folk(fo.P(1.2f, 0, -D / 2f + 1.8f), f.Fwd, Resident.Pose.Stand, new[] { "Sit anywhere. Class is always in; nobody's ever late.", "Three chords and the truth. We do the chords in the morning.", "Ring the bell on your way out. The children like it. There aren't any children." }, new Residents.Look { top = new Color(.9f, .88f, .8f), bottom = new Color(.3f, .25f, .35f), skin = new Color(.75f, .55f, .4f), hair = new Color(.25f, .17f, .1f), longHair = true });
            // the bell on a post by the door
            var bp = ctx.OnGround(f.P(Door / 2f + 1.8f, 0, D / 2f + 1.4f).x, f.P(Door / 2f + 1.8f, 0, D / 2f + 1.4f).z); mk.Box(wood, bp + up * 1.3f, new Vector3(.14f, 2.6f, .14f), f.r); mk.Box(wood, bp + up * 2.5f - f.Right * .3f, new Vector3(.8f, .1f, .1f), f.r); ArtistBits.PostCollider(ctx.Root, bp, 2.6f, .1f);
            var bell = new MeshKit(); bell.Prism(WorldMaterials.Solid(new Color(.8f, .65f, .3f), .85f), new Vector3(0, -.45f, 0), Vector3.up, .22f, .36f, 10, .5f); var bgo = bell.BuildSingle("School bell", plife.transform, false); bgo.transform.SetPositionAndRotation(bp + up * 2.45f - f.Right * .6f, f.r);
            var bs = bgo.AddComponent<Sway>(); bs.degrees = 35f; bs.speed = 7f; bs.decay = .9f; bs.seconds = 5f; var bt = Interact.Toy(bgo, Toy.Kind.Custom, "Ring the school bell", new Vector3(.8f, 1.2f, .8f)); bgo.GetComponent<BoxCollider>().center = new Vector3(0, -.5f, 0); bt.onUse = _ => bs.Push();
            Board(ctx, mk, f.P(-Door / 2f - 2.4f, 0, D / 2f + 5f), f.Fwd, "SCHOOLHOUSE", 2.2f);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 3f)); Ring(g, 8f);
            Shot(ctx, "the schoolhouse", f.P(6f, 2f, D / 2f + 12f), fo.P(0, 2f, 0));
            Shot(ctx, "inside the schoolhouse", fo.P(0, 1.5f, D / 2f - 1.2f), fo.P(0, 1.6f, -D / 2f));
        }
    }

    /// The Bluegrass places' moving parts: the luthier's strings shining when plucked.
    public class BluegrassPlacesLife : MonoBehaviour
    {
        public List<(Material m, float struck)> strings = new List<(Material, float)>(); public Light stringLight;
        static readonly Color Warm = new Color(1f, .78f, .4f);
        public void Pluck(int i) { var s = strings[i]; s.struck = Time.time; strings[i] = s; }
        void Update()
        {
            var loud = 0f; for (var i = 0; i < strings.Count; i++) { var age = Time.time - strings[i].struck; var ring = age < 4f ? Mathf.Exp(-age * 1.1f) : 0f; loud = Mathf.Max(loud, ring); strings[i].m.SetColor("_EmissionColor", Warm * (.25f + ring * 4f)); }
            if (stringLight != null) stringLight.intensity = 1.3f + loud * 2.5f;
        }
    }
}
