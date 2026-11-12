using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Math Rock Escarpment laid out on purpose (the build-land standard). The deck, the scarp, the streams and
    /// every terrace and clearing stay where they were; three more packed-earth paths with lanterns reach the empty
    /// east, south and west. On the warm, busy math-rock side above the scarp: the Yatai (a lantern-hung food stall
    /// with stools and bowls of ramen), the Counting Floor (a deck of tiles that light where you stand, in sevens
    /// and elevens) and the Persimmon Orchard. On the quiet post-rock side below it: the Crescendo Field (one old
    /// piano in the long grass and forty lamps on poles; press a key and the field builds, slowly, to a blaze).
    public partial class MathKit
    {
        static readonly Vector2 YataiAt = new Vector2(7128, 6330), FloorAt = new Vector2(7180, 6394), OrchardAt = new Vector2(6800, 6164), FieldAt = new Vector2(6540, 6392);
        static readonly Vector2[] EastPath = { new Vector2(7060, 6350), new Vector2(7100, 6352), new Vector2(7146, 6366), new Vector2(7166, 6380) };
        static readonly Vector2[] SouthPath = { new Vector2(6800, 6205), new Vector2(6800, 6186) };
        static readonly Vector2[] WestPath = { new Vector2(6604, 6400), new Vector2(6568, 6394) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (YataiAt, 9f), (FloorAt, 10f), (OrchardAt, 15f), (FieldAt, 16f) };

        MathPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; Material earthM; readonly List<Vector3> earthP = new List<Vector3>();

        static readonly Color[] Jumpers = { new Color(.75f, .35f, .2f), new Color(.85f, .7f, .35f), new Color(.4f, .45f, .35f), new Color(.9f, .87f, .8f), new Color(.5f, .25f, .25f), new Color(.3f, .35f, .45f) };
        static readonly Color[] Cords = { new Color(.35f, .28f, .22f), new Color(.25f, .28f, .35f), new Color(.5f, .45f, .35f) };
        static readonly string[] ScarpTalk =
        {
            "It's in eleven. You count it as six and five. Or three, three, three and two. Or you just nod and hope.", "Listen: no singer. You get used to it and then singers start to seem like a lot.", "Above the scarp it's all notes. Below it it's all one note, getting bigger.",
            "The leaves have been falling for years. The trees don't seem to run out.", "Somebody's tapping somewhere. Somebody's always tapping.",
        };

        Residents.Look Tapper(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Jumpers, Cords, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Cap }, Jumpers); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Tapper(), pose, prng, lines ?? ScarpTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeScarp(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 14f);
        }

        /// A carved wooden sign like the arrival's. Signs on the +x side of a place's path; the photo spot on the −x side.
        void CarvedSign(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.6f, float size = .17f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(woodDark, g + rot * Vector3.right * e * (w / 2f - .1f) + up * .9f, new Vector3(.12f, 1.8f, .12f), rot); mk.Box(wood, g + up * 1.5f, new Vector3(w, .7f, .08f), rot); Col(g + up * 1f, new Vector3(w, 2f, .14f), rot);
            ArtistBits.Label(rootT, text, g + up * 1.5f + face * .05f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        void SeatBench(MeshKit mk, Vector3 at, Vector3 face, bool lantern = false)
        {
            Bench(ctx0, mk, G(at), face, lantern);   // the land's own bench (it is sittable now)
        }

        List<Vector3> EarthPath(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 200f * 200f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2f);
            for (var i = 0; i < pts.Count; i++) foreach (var a in ctx.Artists) { var r = Foot(a) + 6f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
            pts.RemoveAll(p => NearWater(p.x, p.z, 2.5f)); if (pts.Count < 2) return pts;
            Strip(mk, pts, earthM, PathW, .16f, ctx); foreach (var p in pts) Remember(p, 700);
            var up = Vector3.up; var n = 0;
            for (var i = 6; i < pts.Count; i += 14) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d) * (n++ % 2 == 0 ? 1f : -1f); var lp = pts[i] + sd * (PathW / 2f + .8f); if (ctx.IsLand(lp.x, lp.z, 2f) && !InNoGrass(lp.x, lp.z, 1f)) Lantern(ctx, mk, ctx.OnGround(lp.x, lp.z)); for (var k = 0; k < 3; k++) mk.Blob(WorldMaterials.Solid(LeafC[(n + k) % LeafC.Length]), ctx.OnGround(pts[i].x - sd.x * (1.5f + k * .25f), pts[i].z - sd.z * (1.5f + k * .25f) + k * .6f) + up * .05f, new Vector3(.3f, .04f, .22f), prng, .4f); }
            earthP.AddRange(pts); return pts;
        }

        void Trod(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(earthP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.5f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, earthM, 2f, .15f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void ScarpPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            earthP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2005); earthM = WorldMaterials.Solid(new Color(.45f, .34f, .25f));
            plife = new GameObject("Escarpment places life").AddComponent<MathPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            EarthPath(ctx, mk, EastPath); EarthPath(ctx, mk, SouthPath); EarthPath(ctx, mk, WestPath);
            softKit = null;
            Yatai(ctx, mk, solid); CountingFloor(ctx, mk, solid); Orchard(ctx, mk, solid); CrescendoField(ctx, mk, solid);
            if (softKit != null) { softKit.Build("Soft escarpment places", ctx.Root, false); softKit = null; }   // steam, curtains, drying fruit: walked through
        }

        // ---------------------------------------------------------------- the Yatai
        void Yatai(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(YataiAt.x, YataiAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(earthP, g))); var red = new Color(.85f, .2f, .15f);
            mk.Box(wood, g + up * .08f, new Vector3(9f, .2f, 7f), f.r); solid.Box(wood, g - up * .05f, new Vector3(9f, .4f, 7f), f.r); var fo = new PlaceFrame(g + up * .18f, f.Fwd);
            // the stall: a cart with a tiled roof, a counter on three sides, noren curtains, red lanterns, steam
            foreach (var (x, z) in new[] { (-2.2f, -2f), (2.2f, -2f), (2.2f, 0f), (-2.2f, 0f) }) { mk.Box(woodDark, fo.P(x, 1.3f, z), new Vector3(.12f, 2.6f, .12f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(x, 0, z), 2.6f, .09f); }
            mk.GableRoof(WorldMaterials.Solid(new Color(.25f, .28f, .34f)), fo.P(0, 2.6f, -1f), new Vector3(5.6f, 0, 3.4f), .9f, f.r); mk.Box(wood, fo.P(0, 1f, .1f), new Vector3(4.6f, .08f, .6f), f.r); mk.Box(woodDark, fo.P(0, .5f, .1f), new Vector3(4.4f, 1f, .1f), f.r); Col(fo.P(0, .55f, .1f), new Vector3(4.6f, 1.1f, .6f), f.r); mk.Box(woodDark, fo.P(0, .5f, -1.9f), new Vector3(4.4f, 1f, .4f), f.r); Col(fo.P(0, 1f, -1.9f), new Vector3(4.4f, 2f, .4f), f.r);
            for (var k = 0; k < 4; k++) soft().Box(WorldMaterials.Solid(k % 2 == 0 ? new Color(.2f, .25f, .45f) : new Color(.9f, .87f, .8f)), fo.P(-1.65f + k * 1.1f, 2.15f, .02f), new Vector3(1.05f, .8f, .03f), f.r);
            for (var k = 0; k < 5; k++) { var lp = fo.P(-2.4f + k * 1.2f, 2.35f, .5f); mk.Prism(WorldMaterials.Glow(red, 1.5f), lp - up * .3f, up, .2f, .36f, 8); mk.Box(woodDark, lp + up * .1f, new Vector3(.24f, .05f, .24f), f.r); if (k % 2 == 0) halos.Add(lp - up * .1f, 1f, new Color(1f, .4f, .25f), .6f); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2f, .8f), new Color(1f, .65f, .4f), 12f, 2f);
            mk.Prism(metal, fo.P(-1.1f, 1f, -1.2f), up, .35f, .45f, 10); mk.Prism(WorldMaterials.Glow(new Color(1f, .85f, .6f), .5f), fo.P(-1.1f, 1.44f, -1.2f), up, .32f, .03f, 10); for (var k = 0; k < 3; k++) soft().Blob(WorldMaterials.Glow(new Color(.95f, .95f, 1f), .3f), fo.P(-1.1f + k * .1f, 1.8f + k * .35f, -1.2f), Vector3.one * (.18f + k * .06f), prng, .2f);
            // stools along the front, bowls on the counter to carry
            for (var k = 0; k < 4; k++) { var sp = fo.P(-1.65f + k * 1.1f, 0, 1f); mk.Prism(woodDark, sp, up, .05f, .62f, 5); mk.Prism(wood, sp + up * .62f, up, .2f, .07f, 8); ArtistBits.PostCollider(ctx.Root, sp, .68f, .18f); if (k == 1) Folk(sp + up * .7f, -f.Fwd, Resident.Pose.Sit, new[] { "After a show you want noodles. After noodles you want another show. It's a closed loop.", "He times the eggs to a record. Six minutes forty. Don't ask which record." }, Tapper(Residents.Tool.Mug), null, .7f); else Interact.Seat(plife.transform, sp + up * .72f, -f.Fwd, "Sit at the stall"); }
            for (var k = 0; k < 3; k++) { var bm = new MeshKit(); bm.Prism(WorldMaterials.Solid(k == 1 ? new Color(.2f, .25f, .45f) : new Color(.9f, .87f, .8f)), Vector3.zero, Vector3.up, .1f, .1f, 10, 1.5f); bm.Prism(WorldMaterials.Solid(new Color(.8f, .6f, .3f)), new Vector3(0, .09f, 0), Vector3.up, .14f, .01f, 10); bm.Blob(WorldMaterials.Solid(new Color(.98f, .92f, .7f)), new Vector3(.04f, .11f, 0), new Vector3(.05f, .02f, .05f), prng, 0f); var go = bm.BuildSingle("Ramen bowl", plife.transform, false); go.transform.position = fo.P(-.6f + k * 1.1f, 1.05f, .1f); var c = Interact.Carry(go, "a bowl of ramen", .4f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.3f, .14f, .3f); bc.center = new Vector3(0, .06f, 0); } }
            Folk(fo.P(.6f, 0, -1f), f.Fwd, Resident.Pose.Work, new[] { "Shoyu, miso, or the one I'm not allowed to describe. All in seven minutes, which is not a time signature. Yet.", "Sit. Eat. The band's on a break. The band's always on a break; that's what the rests are.", "Slurp in time if you can. It's 5/4 tonight." }, Tapper(Residents.Tool.Spatula, true)).workRate = 1f;
            SeatBench(mk, f.P(-3.6f, 0, 2.6f), f.Right, true);
            CarvedSign(mk, f.P(4.2f, 0, 5.4f), f.Fwd, "YATAI\nopen till the last chord", 2.8f, .15f);
            Trod(ctx, mk, f.P(0, 0, 3.6f)); Ring(g, 8f);
            Shot(ctx, "the yatai", f.P(-5f, 2.4f, 9f), fo.P(0, 1.6f, -.5f));
        }

        MeshKit softKit; MeshKit soft() => softKit ?? (softKit = new MeshKit());

        // ---------------------------------------------------------------- the Counting Floor
        void CountingFloor(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(FloorAt.x, FloorAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(earthP, g))); const int NX = 7, NZ = 11; const float T = 1.1f;
            // a wooden deck of square tiles, seven across and eleven long; a tile lights when you stand on it, and a slow
            // pulse walks the rows on its own, counting
            mk.Box(woodDark, g + up * .1f, new Vector3(NX * T + .8f, .3f, NZ * T + .8f), f.r); solid.Box(woodDark, g, new Vector3(NX * T + .8f, .5f, NZ * T + .8f), f.r);
            for (var x = 0; x < NX; x++) for (var z = 0; z < NZ; z++) { var c = LeafC[(x * 3 + z * 5) % 4]; var tm = new Material(WorldMaterials.Glow(c, .1f)) { name = "Counting tile" }; var p = g + f.r * new Vector3((x - (NX - 1) / 2f) * T, .26f, (z - (NZ - 1) / 2f) * T); mk.Box(tm, p, new Vector3(T - .12f, .03f, T - .12f), f.r); plife.tiles.Add((tm, c, p, x, z)); }
            plife.floorLight = ArtistBits.PointLight(ctx.Root, g + up * 3f, Warm, 18f, 1.2f);
            // four posts with string lights, a metronome post to set the count, wind chimes, a bench
            var corners = new List<Vector3>(); foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var p = G(g + f.r * new Vector3(x * (NX * T / 2f + 1f), 0, z * (NZ * T / 2f + 1f))); mk.Box(woodDark, p + up * 2f, new Vector3(.14f, 4f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, p, 4f, .1f); corners.Add(p + up * 3.9f); }
            for (var k = 0; k < 4; k++) StringLights(mk, corners[k], corners[(k + 1) % 4]);
            var mp = G(g + f.Fwd * (NZ * T / 2f + 1.6f) + f.Right * 2.6f); mk.Prism(woodDark, mp, up, .3f, 1f, 4, .7f, 45f); ArtistBits.PostCollider(ctx.Root, mp, 1f, .3f); var arm = new MeshKit(); arm.Box(metal, new Vector3(0, .35f, 0), new Vector3(.03f, .7f, .03f), Quaternion.identity); arm.Box(GlowM(Gold, 2f), new Vector3(0, .55f, 0), new Vector3(.1f, .1f, .06f), Quaternion.identity);
            var ago = arm.BuildSingle("Metronome", plife.transform, false); ago.transform.SetPositionAndRotation(mp + up * 1f, f.r); plife.metro = ago.transform; plife.metroRot = f.r; string[] counts = { "Seven: 1-2, 1-2, 1-2-3.", "Eleven: 1-2-3, 1-2-3, 1-2-3, 1-2.", "Five: 1-2-3, 1-2. The easy one. Relatively.", "Four. Plain four. The tiles look almost embarrassed." }; int[] ns = { 7, 11, 5, 4 };
            Interact.Toy(ago, Toy.Kind.Custom, "Change the count", new Vector3(.8f, 1f, .8f)).onUse = _ => { plife.countIdx = (plife.countIdx + 1) % 4; plife.count = ns[plife.countIdx]; Say(plife.transform, mp + up * 2.4f, counts[plife.countIdx], 5f); };
            WindChime(ctx, mk, G(g - f.Fwd * (NZ * T / 2f + 1.8f) - f.Right * 2f)); SeatBench(mk, g + f.Right * (NX * T / 2f + 2.2f), -f.Right, true);
            Folk(G(g - f.Right * (NX * T / 2f + 1.6f) + f.Fwd * 2f), f.Right, Resident.Pose.Wave, new[] { "Walk it in sevens! No, start on the... okay, you're in nine now. Nine's fine. Nine's a real one.", "Stand still and it counts round you. Move and you're the accent.", "I once got all the way across in eleven without thinking. I thought about it. Then I fell off." }, Tapper()).workRate = 1.2f;
            CarvedSign(mk, f.P(5.5f, 0, NZ * T / 2f + 5f), f.Fwd, "THE COUNTING FLOOR\n7 × 11", 2.8f, .16f);
            Trod(ctx, mk, f.P(0, 0, NZ * T / 2f + 1f)); Ring(g, 9f);
            Shot(ctx, "the counting floor", f.P(-7f, 4.4f, NZ * T / 2f + 8f), g + up * .5f);
        }

        // ---------------------------------------------------------------- the Persimmon Orchard
        void Orchard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(OrchardAt.x, OrchardAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(earthP, g))); var fruit = WorldMaterials.Glow(new Color(1f, .5f, .12f), .5f); var bare = WorldMaterials.Solid(new Color(.3f, .22f, .18f));
            // twelve bare-branched trees in three rows, hung with bright orange fruit (the leaves are all on the ground)
            for (var x = 0; x < 4; x++) for (var z = 0; z < 3; z++)
            {
                if (x == 1 && z == 1) continue;   // the gap where the drying shed stands
                var p = G(g + f.r * new Vector3(-9f + x * 6f, 0, -7f + z * 6f)); mk.Prism(bare, p - up * .2f, up, .22f, 2.2f, 6, .7f); ArtistBits.PostCollider(ctx.Root, p, 2.2f, .22f);
                for (var b = 0; b < 5; b++) { var d = Quaternion.Euler(0, b * 72f + x * 20f, 0) * new Vector3(.8f, .7f, 0).normalized; mk.Prism(bare, p + up * 2f, d, .07f, 1.9f, 4, .4f); for (var s = 0; s < 3; s++) { var fp = p + up * 2f + d * (.8f + s * .5f) - up * .15f; mk.Blob(fruit, fp, Vector3.one * .13f, prng, 0f); } }
                halos.Add(p + up * 2.8f, 2f, new Color(1f, .55f, .2f), .25f); mk.Blob(leafPile, p + up * .08f + f.Right * .9f, new Vector3(1.1f, .16f, .9f), prng, .3f);
            }
            // the drying shed: an open frame hung with strings of peeled persimmons, glowing like lanterns
            var sc = G(g + f.r * new Vector3(-3f, 0, -1f)); foreach (var (x, z) in new[] { (-1.6f, -1.2f), (1.6f, -1.2f), (1.6f, 1.2f), (-1.6f, 1.2f) }) { var p = sc + f.r * new Vector3(x, 0, z); mk.Box(woodDark, p + up * 1.3f, new Vector3(.12f, 2.6f, .12f), f.r); ArtistBits.PostCollider(ctx.Root, p, 2.6f, .09f); }
            mk.GableRoof(WorldMaterials.Solid(new Color(.3f, .26f, .24f)), sc + up * 2.6f, new Vector3(3.8f, 0, 3f), .8f, f.r); for (var k = 0; k < 6; k++) { var top = sc + f.r * new Vector3(-1.4f + k * .56f, 2.5f, 1.2f); soft().Box(cloth, top - up * .7f, new Vector3(.02f, 1.4f, .02f), f.r); for (var s = 0; s < 5; s++) soft().Blob(WorldMaterials.Glow(new Color(1f, .6f, .25f), .9f), top - up * (.25f + s * .26f), new Vector3(.08f, .1f, .08f), prng, 0f); }
            halos.Add(sc + up * 1.8f + f.Fwd * 1.4f, 2f, new Color(1f, .6f, .3f), .4f); ArtistBits.PointLight(ctx.Root, sc + up * 2f + f.Fwd * 1f, new Color(1f, .65f, .35f), 10f, 1.6f);
            // a ladder against a tree, baskets of fruit to carry, a picker, a bench
            var lt = G(g + f.r * new Vector3(3f, 0, -1f)); foreach (var e in new[] { -.25f, .25f }) mk.Box(wood, lt + f.Right * e + up * 1.2f + f.Fwd * .5f, new Vector3(.05f, 2.6f, .05f), f.r * Quaternion.Euler(-18f, 0, 0)); for (var k = 0; k < 6; k++) mk.Box(wood, lt + up * (.3f + k * .38f) + f.Fwd * (.86f - k * .125f), new Vector3(.5f, .04f, .04f), f.r); Col(lt + up * 1.2f + f.Fwd * .5f, new Vector3(.6f, 2.4f, .9f), f.r);
            for (var b = 0; b < 2; b++) { var bp = G(g + f.r * new Vector3(4.6f + b * 1.2f, 0, 1.2f)); mk.Prism(WorldMaterials.Solid(new Color(.7f, .58f, .38f)), bp, up, .35f, .3f, 8, 1.2f); ArtistBits.PostCollider(ctx.Root, bp, .3f, .38f); for (var k = 0; k < 3; k++) { var pm = new MeshKit(); pm.Blob(fruit, new Vector3(0, .09f, 0), new Vector3(.1f, .09f, .1f), prng, 0f); pm.Box(WorldMaterials.Solid(new Color(.35f, .4f, .2f)), new Vector3(0, .18f, 0), new Vector3(.08f, .015f, .08f), Quaternion.Euler(0, 45f, 0)); var go = pm.BuildSingle("Persimmon", plife.transform, false); go.transform.position = bp + up * .34f + new Vector3(Mathf.Cos(k * 2.1f), 0, Mathf.Sin(k * 2.1f)) * .15f; var c = Interact.Carry(go, "a persimmon", .2f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.2f, .2f, .2f); bc.center = new Vector3(0, .09f, 0); } } }
            Folk(G(lt + f.Right * 1.2f + f.Fwd * .8f), -f.Right, Resident.Pose.Work, new[] { "You pick them hard, hang them a month, and they turn to jam on a string. Patience. It's the same with a riff.", "Take one. Not that one. That one's got a week to go. That one.", "The leaves fall first so you can see the fruit. Decent of them." }, Tapper(Residents.Tool.Rake, true)).workRate = .6f;
            SeatBench(mk, G(g + f.r * new Vector3(-6f, 0, 2f)), f.Fwd, true); Lantern(ctx, mk, G(g + f.r * new Vector3(6.5f, 0, 6f)));
            CarvedSign(mk, f.P(7.5f, 0, 12f), f.Fwd, "PERSIMMON ORCHARD", 2.8f, .16f);
            Trod(ctx, mk, f.P(0, 0, 10f)); Ring(g, 14f);
            Shot(ctx, "the persimmon orchard", f.P(-10f, 4f, 15f), g + up * 1.6f);
        }

        // ---------------------------------------------------------------- the Crescendo Field
        void CrescendoField(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(FieldAt.x, FieldAt.y); var seaward = new Vector3(sea2.x, 0, sea2.y).normalized; var f = new PlaceFrame(g, -seaward);   // you stand at the piano looking out to sea over the lamps
            // an upright piano alone in the long grass, weathered, a stool; in front of it, toward the sea, forty pale lamps
            // on thin poles, dark. Press a key: they come on one by one from the piano outward, over most of a minute, to
            // a blaze, hold, and go out together.
            var pp = g; var pq = Quaternion.LookRotation(-seaward); var pale = WorldMaterials.Solid(new Color(.5f, .45f, .4f)); mk.Box(pale, pp + up * .65f, new Vector3(1.5f, 1.2f, .6f), pq); mk.Box(WorldMaterials.Solid(new Color(.92f, .9f, .85f)), pp + up * .8f - seaward * .0f + pq * Vector3.forward * .36f, new Vector3(1.3f, .04f, .2f), pq); mk.Box(pale, pp + up * 1.26f, new Vector3(1.5f, .05f, .64f), pq); Col(pp + up * .65f, new Vector3(1.5f, 1.3f, .8f), pq);
            var st = pp + pq * Vector3.forward * 1f; mk.Prism(woodDark, st, up, .2f, .5f, 8); ArtistBits.PostCollider(ctx.Root, st, .5f, .2f); Interact.Seat(plife.transform, st + up * .56f, seaward, "Sit at the piano");
            var kgo = new GameObject("Piano key"); kgo.transform.SetParent(plife.transform, false); kgo.transform.position = pp + up * .85f + pq * Vector3.forward * .5f;
            Interact.Toy(kgo, Toy.Kind.Custom, "Press one key", new Vector3(1.4f, .5f, .6f)).onUse = _ => { if (Time.time - plife.pressed > 70f) { plife.pressed = Time.time; Say(plife.transform, pp + up * 2.4f, "One note. It doesn't stop. Out in the grass, a lamp comes on.", 6f); } else Say(plife.transform, pp + up * 2.4f, "It's already building. Let it.", 3f); };
            var rng = new System.Random(41);
            for (var k = 0; k < 40; k++)
            {
                var dist = 5f + k * .9f + (float)rng.NextDouble() * 2f; var ang = ((float)rng.NextDouble() - .5f) * 80f; var p = G(g + Quaternion.Euler(0, ang, 0) * seaward * dist); if (!ctx.IsLand(p.x, p.z, 2f) || NearWater(p.x, p.z, 2f)) continue; var h = 2.4f + (float)rng.NextDouble() * 2.4f;
                mk.Box(metal, p + up * (h / 2f), new Vector3(.04f, h, .04f), Quaternion.identity); var lm = new Material(WorldMaterials.Glow(Mist, 0f)) { name = "Crescendo lamp" }; var lk = new MeshKit(); lk.Blob(lm, Vector3.zero, Vector3.one * .2f, prng, 0f); var go = lk.BuildSingle("Soft crescendo lamp", plife.transform, false); go.transform.position = p + up * h; plife.lamps.Add((lm, k / 40f));
            }
            plife.fieldLight = ArtistBits.PointLight(ctx.Root, g + seaward * 18f + up * 6f, Mist, 60f, 0f); Lantern(ctx, mk, G(g - seaward * 2.5f + f.Right * 3f), 1.6f);
            SeatBench(mk, g - seaward * 4f - f.Right * 3f, seaward);
            Folk(G(g - seaward * 4f - f.Right * 3f) + up * .52f + Vector3.Cross(up, seaward) * .5f, seaward, Resident.Pose.Sit, new[] { "Press it and sit down. It takes a minute. It's meant to. The waiting's most of it.", "It starts as nothing. It's still nothing at twenty seconds. Then, somewhere in there, it isn't.", "I've watched it four hundred times. I cry at about lamp thirty." }, Tapper(), null, .52f);
            CarvedSign(mk, G(g - seaward * 8f + f.Right * 5f), -seaward, "THE CRESCENDO FIELD\none note", 2.8f, .16f);
            Ring(g, 6f);
            Shot(ctx, "the crescendo field", g - seaward * 9f - f.Right * 5f + up * 3.4f, g + seaward * 14f + up * 2.4f);
        }
    }

    /// The Escarpment places' moving parts: the counting floor (tiles under your feet, and a pulse counting the rows),
    /// its metronome, and the crescendo field.
    public class MathPlacesLife : MonoBehaviour
    {
        public List<(Material m, Color c, Vector3 at, int x, int z)> tiles = new List<(Material, Color, Vector3, int, int)>(); public Light floorLight, fieldLight; public Transform metro; public Quaternion metroRot; public int count = 7, countIdx;
        public List<(Material m, float u)> lamps = new List<(Material, float)>(); public float pressed = -999f;
        static readonly Color Mist = new Color(.85f, .9f, 1f);
        Transform player; readonly List<float> glow = new List<float>();

        void Update()
        {
            var t = Time.time;
            if (player == null) { var p = FindAnyObjectByType<EverythingWorld.Player.SimplePlayerController>(); if (p != null) player = p.transform; }
            while (glow.Count < tiles.Count) glow.Add(0f);
            var beat = Mathf.FloorToInt(t * 2.4f); var step = beat % count; var any = 0f;
            for (var i = 0; i < tiles.Count; i++)
            {
                var (m, c, at, x, z) = tiles[i]; var under = player != null && Mathf.Abs(player.position.x - at.x) < .7f && Mathf.Abs(player.position.z - at.z) < .7f && Mathf.Abs(player.position.y - at.y) < 2.2f;
                var counted = (x + z * 7) % count == step ? (step == 0 ? 1f : .45f) : 0f;   // the count walks the floor; the one is brightest
                glow[i] = Mathf.Max(under ? 1.6f : counted, glow[i] - Time.deltaTime * 2.5f); any = Mathf.Max(any, under ? 1f : 0f); m.SetColor("_EmissionColor", c * (.1f + glow[i] * 2.6f));
            }
            if (floorLight != null) floorLight.intensity = 1.2f + any * 1.2f + (step == 0 ? .4f : 0f);
            if (metro != null) metro.rotation = metroRot * Quaternion.Euler(0, 0, Mathf.Sin(t * 2.4f * Mathf.PI) * 24f);
            // the crescendo: 0-45 s the lamps come on outward from the piano and brighten; 45-58 s the blaze holds; then out
            var a = t - pressed; var build = a < 45f ? a / 45f : a < 58f ? 1f : 0f; var hold = a >= 45f && a < 58f ? 1f : 0f;
            foreach (var (m, u) in lamps) { var on = Mathf.Clamp01((build - u * .85f) * 6f); m.SetColor("_EmissionColor", Mist * (on * (1.2f + build * 2.2f + hold * 2f * (.8f + .2f * Mathf.Sin(t * 9f + u * 30f))))); }
            if (fieldLight != null) fieldLight.intensity = build * build * 3f + hold * 3f;
        }
    }
}
