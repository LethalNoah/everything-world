using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Oddball Backroads laid out on purpose, not scattered: a loop road round the whole land with the
    /// crossroads (and the artists) in the middle, and along it a string of places, each made to stop at, with
    /// somewhere to sit, something to play with and the people who live there:
    /// - the Oddball Diner at the crossroads (booths, counter stools, a jukebox, the cook and a waitress);
    /// - a giant chair on the highest hill (climb up and sit: the whole land below);
    /// - the world's largest ball of twine (please do not push it down the hill);
    /// - the upside-down house (you walk on its ceiling; its owner reads the paper overhead);
    /// - Littleville, a model village at knee height (tiny folk, tiny cars you can pick up);
    /// - the drive-in by the sea (sit in the cars, the screen shows shapes; a bonfire with log seats);
    /// - a lighthouse that shines inland, its lifeguard chair facing the fields;
    /// - the Maybe Motel (a pool of rubber ducks, loungers, and room 6, which isn't there);
    /// - a field of doors (open any; one opens onto the sky);
    /// - a stretch of road that loops the loop.
    /// Between them farmland on purpose: wheat, corn, sunflowers and kudzu in fields along the road behind white
    /// fences, hedgerows behind, kudzu tunnels the road runs through, one lone tree on each hilltop, dark kudzu in
    /// the hollows (bright gold fields against near-black hollows, for contrast).
    public partial class OddballKit
    {
        // sites, chosen from the land's heights (the highest hill, a flat strip by the sea...); nudged onto land if
        // the land ever changes shape
        static readonly Vector2 DinerOff = new Vector2(-34f, -32f);
        static readonly Vector2 SummitAt = new Vector2(9032, 9270), TwineAt = new Vector2(8850, 9196), UpsideAt = new Vector2(8676, 9160),
            LittleAt = new Vector2(9180, 9030), DriveAt = new Vector2(9312, 8990), LightAt = new Vector2(9196, 9452), MotelAt = new Vector2(9130, 8615),
            DoorsAt = new Vector2(8820, 8600), LoopAt = new Vector2(8690, 8790), FarmAt = new Vector2(8930, 8540),
            UfoAt = new Vector2(8424, 8740), FruitAt = new Vector2(8520, 9084), ShoeAt = new Vector2(8580, 8630), BeachAt = new Vector2(9500, 8478),
            PierAt = new Vector2(9440, 8780), MillAt = new Vector2(9304, 8436);
        // the west road (out over the gold plateau and back) and the coast road (from the loop down the shore)
        static readonly Vector2[] WestWay = { new Vector2(8720, 8800), new Vector2(8560, 8770), new Vector2(8440, 8800), new Vector2(8360, 8900), new Vector2(8430, 9010), new Vector2(8560, 9060), new Vector2(8700, 9095), new Vector2(8800, 9110) };
        static readonly Vector2[] WestSouth = { new Vector2(8560, 8770), new Vector2(8540, 8680), new Vector2(8600, 8600), new Vector2(8720, 8580), new Vector2(8820, 8650) };
        static readonly Vector2[] CoastWay = { new Vector2(9235, 9000), new Vector2(9360, 8900), new Vector2(9410, 8760), new Vector2(9420, 8600), new Vector2(9390, 8470), new Vector2(9330, 8380) };
        static readonly Vector2[] NorthWay = { new Vector2(8960, 9185), new Vector2(9040, 9330), new Vector2(9130, 9395), new Vector2(9172, 9412) };
        static readonly Vector2[] LoopWay =
        {
            new Vector2(8800, 9110), new Vector2(8960, 9185), new Vector2(9120, 9150), new Vector2(9235, 9000), new Vector2(9225, 8790),
            new Vector2(9150, 8665), new Vector2(8975, 8590), new Vector2(8820, 8650), new Vector2(8720, 8800), new Vector2(8715, 8990),
        };
        static readonly (Vector2 at, float r)[] PlaceRings =
        {
            (SummitAt, 26f), (TwineAt, 14f), (UpsideAt, 20f), (LittleAt, 22f), (DriveAt, 40f), (LightAt, 22f), (MotelAt, 34f),
            (DoorsAt, 22f), (LoopAt, 14f), (FarmAt, 26f), (UfoAt, 28f), (FruitAt, 20f), (ShoeAt, 16f), (BeachAt, 18f), (PierAt, 18f), (MillAt, 16f),
        };

        /// The places, for ArtistPlacement.Spread to keep artists clear of (x, radius, z).
        static List<Vector3> PlaceAvoid()
        {
            var l = new List<Vector3>(); foreach (var (at, r) in PlaceRings) l.Add(new Vector3(at.x, r + 12f, at.y)); return l;
        }

        struct Frame
        {
            public Vector3 o; public Quaternion r;
            public Frame(Vector3 o, Vector3 face) { this.o = o; face.y = 0; r = Quaternion.LookRotation(face.normalized); }
            public Vector3 P(float x, float y, float z) => o + r * new Vector3(x, y, z);
            public Vector3 Fwd => r * Vector3.forward; public Vector3 Right => r * Vector3.right;
        }

        OddballPlacesLife plife; System.Random prng;
        readonly List<Vector3> loopPts = new List<Vector3>();
        readonly List<List<Vector3>> mainRoads = new List<List<Vector3>>();
        Material gravel, fenceM, twineM, chrome, teal, pool, neonPink, neonBlue, screenM, stripeRed;

        void PlaceMaterials()
        {
            gravel = WorldMaterials.Solid(new Color(.72f, .68f, .58f)); fenceM = WorldMaterials.Solid(new Color(.97f, .96f, .92f));
            twineM = WorldMaterials.Solid(new Color(.8f, .66f, .42f)); chrome = WorldMaterials.Solid(new Color(.82f, .84f, .86f), .8f);
            teal = WorldMaterials.Solid(new Color(.3f, .75f, .75f)); pool = WorldMaterials.Glow(new Color(.25f, .65f, 1f), .9f);
            neonPink = GlowM(Flamingo, 2.6f); neonBlue = GlowM(Sky, 2.4f); screenM = new Material(WorldMaterials.Glow(new Color(.95f, .95f, 1f), 1.1f)) { name = "Drive-in screen" };
            stripeRed = WorldMaterials.Solid(new Color(.85f, .15f, .15f));
        }

        Vector3 Site(LandContext ctx, Vector2 want, float inland)
        {
            if (ctx.IsLand(want.x, want.y, inland)) return ctx.OnGround(want.x, want.y);
            for (var r = 6f; r < 200f; r += 6f)
                for (var a = 0f; a < 360f; a += 20f)
                {
                    var x = want.x + Mathf.Cos(a * Mathf.Deg2Rad) * r; var z = want.y + Mathf.Sin(a * Mathf.Deg2Rad) * r;
                    if (ctx.IsLand(x, z, inland)) return ctx.OnGround(x, z);
                }
            return ctx.OnGround(want.x, want.y);
        }

        Vector3 FaceTo(Vector3 from, Vector3 to) { var d = to - from; d.y = 0; return d.sqrMagnitude > .01f ? d.normalized : Vector3.forward; }

        void Shot(LandContext ctx, string name, Vector3 cam, Vector3 look)
        {
            var mark = new GameObject("Shot: " + name).transform; mark.SetParent(ctx.Root, false);
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized));
        }

        // ---------------------------------------------------------------- residents
        static readonly Color[] Tops = { Flamingo, Sky, Sun, new Color(.95f, .95f, .92f), new Color(.5f, .85f, .6f), new Color(.85f, .4f, .3f), new Color(.6f, .45f, .85f) };
        static readonly Color[] Bottoms = { new Color(.25f, .35f, .55f), new Color(.7f, .62f, .45f), new Color(.3f, .3f, .32f), new Color(.45f, .55f, .7f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Straw, Residents.Hat.Bucket, Residents.Hat.Cowboy };
        static readonly Color[] HatColors = { new Color(.9f, .2f, .25f), Sky, new Color(.9f, .85f, .65f), Flamingo, new Color(.2f, .25f, .35f) };

        static readonly string[] Locals =
        {
            "Lovely day for it. Whatever it is.", "Have you seen a flamingo in a hat? He owes me money.", "The road goes round. Mostly.",
            "Don't trust the stop sign. It's undecided.", "I've lived here my whole life, give or take.", "The head's watching you. It does that.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false)
        {
            var l = Residents.Random(prng, Tops, Bottoms, Hats, HatColors); l.tool = tool; l.apron = apron; return l;
        }

        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Locals, route, seatH);

        // ---------------------------------------------------------------- roads round the land
        List<Vector3> Smooth(LandContext ctx, IList<Vector2> way, bool closed, float step = 2f)
        {
            var pts = new List<Vector3>(); var n = way.Count; var segs = closed ? n : n - 1;
            for (var i = 0; i < segs; i++)
            {
                Vector2 p0 = way[closed ? (i - 1 + n) % n : Mathf.Max(i - 1, 0)], p1 = way[i], p2 = way[(i + 1) % n], p3 = way[closed ? (i + 2) % n : Mathf.Min(i + 2, n - 1)];
                var len = Vector2.Distance(p1, p2); var k = Mathf.Max(2, Mathf.CeilToInt(len / step));
                for (var j = 0; j < k; j++)
                {
                    var t = j / (float)k; var t2 = t * t; var t3 = t2 * t;
                    var q = .5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                    if (ctx.IsLand(q.x, q.y, 3f)) pts.Add(ctx.OnGround(q.x, q.y));
                }
            }
            if (closed && pts.Count > 0) pts.Add(pts[0]); else if (!closed) pts.Add(ctx.OnGround(way[n - 1].x, way[n - 1].y));
            return pts;
        }

        /// The big loop and the four roads out of the crossroads to it; then gravel lanes up to the places.
        void LoopRoads(LandContext ctx, MeshKit mk, MeshKit solid, System.Action<List<Vector3>, bool> lay)
        {
            loopPts.Clear(); mainRoads.Clear(); loopPts.AddRange(Smooth(ctx, LoopWay, true));
            lay(loopPts, true); mainRoads.Add(new List<Vector3>(loopPts));
            foreach (var way in new[] { WestWay, WestSouth, CoastWay, NorthWay }) { var pts = Smooth(ctx, way, false); lay(pts, false); mainRoads.Add(pts); loopPts.AddRange(pts); }
            var c = new Vector2(center.x, center.z);
            foreach (var w in new[] { 1, 4, 6, 9 })
            {
                var to = LoopWay[w]; var dir = (to - c).normalized; var side = new Vector2(-dir.y, dir.x);
                var mid = Vector2.Lerp(c, to, .5f) + side * (w % 2 == 0 ? 18f : -18f);
                var pts = Smooth(ctx, new[] { c + dir * 10f, mid, to }, false); lay(pts, false);
            }
        }

        Vector3 NearestLoop(Vector3 p)
        {
            var best = loopPts.Count > 0 ? loopPts[0] : p; var bd = float.MaxValue;
            foreach (var q in loopPts) { var d = (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z); if (d < bd) { bd = d; best = q; } }
            return best;
        }

        /// A gravel lane from the loop to a place, gently curved.
        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort)
        {
            var from = NearestLoop(to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 4f) return; d /= len;
            var end = new Vector2(from.x, from.z) + d * (len - stopShort); var side = new Vector2(-d.y, d.x);
            var mid = Vector2.Lerp(new Vector2(from.x, from.z), end, .5f) + side * Mathf.Min(10f, len * .12f);
            var pts = Smooth(ctx, new[] { new Vector2(from.x, from.z) + d * (RoadW / 2f + .2f), mid, end }, false, 1.5f);
            if (pts.Count < 2) return;
            Strip(mk, pts, gravel, 3.2f, .12f, ctx); foreach (var p in pts) Remember(p, 99);
        }

        // ---------------------------------------------------------------- all the places
        void Places(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            PlaceMaterials();
            plife = new GameObject("Backroads places life").AddComponent<OddballPlacesLife>(); plife.transform.SetParent(life.transform, false);
            prng = new System.Random(4242);
            Diner(ctx, mk, solid);
            GiantChair(ctx, mk, solid); Twine(ctx, mk, solid); UpsideDown(ctx, mk, solid); Littleville(ctx, mk, solid);
            DriveIn(ctx, mk, solid); Lighthouse(ctx, mk, solid); Motel(ctx, mk, solid); Doors(ctx, mk, solid); LoopTheLoop(ctx, mk, solid);
            Ufo(ctx, mk, solid); FruitStand(ctx, mk, solid); ShoeTree(ctx, mk, solid); BeachRoom(ctx, mk, solid); Pier(ctx, mk, solid); Windmill(ctx, mk, solid);
            Farm(ctx, mk, solid);
        }

        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        // the Oddball Diner: walk in, sit in a booth or at the counter, press the jukebox
        void Diner(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(center.x + DinerOff.x, center.z + DinerOff.y); var f = new Frame(g, FaceTo(g, center));
            const float W = 18f, D = 10f, H = 4.2f, T = .25f, Door = HumanScale.DoorWidth;
            var floorY = g.y + .3f; var fo = new Frame(new Vector3(g.x, floorY, g.z), f.Fwd);
            mk.Box(WorldMaterials.Solid(new Color(.4f, .38f, .36f)), f.P(0, -.6f, 0) + up * .3f, new Vector3(W + 1f, 1.2f, D + 1f), f.r);   // footing
            mk.Box(white, fo.P(0, -.05f, 0), new Vector3(W, .1f, D), f.r); solid.Box(white, fo.P(0, -.05f, 0), new Vector3(W, .1f, D), f.r);
            for (var x = 0; x < (int)W; x++) for (var z = 0; z < (int)D; z++) if ((x + z) % 2 == 0) mk.Box(dark, fo.P(-W / 2f + x + .5f, .006f, -D / 2f + z + .5f), new Vector3(1f, .01f, 1f), f.r);
            // walls: back, two sides between them, the front split round the door with long windows
            void Wall(Vector3 c, Vector3 s, Material m) { mk.Box(m, c, s, f.r); solid.Box(m, c, s, f.r); }
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T), chrome);
            foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f), chrome);
            var seg = (W - Door) / 2f;
            foreach (var e in new[] { -1f, 1f })
            {
                var cx = e * (Door / 2f + seg / 2f);
                Wall(fo.P(cx, .45f, D / 2f - T / 2f), new Vector3(seg, .9f, T), teal);
                Wall(fo.P(cx, H - .5f, D / 2f - T / 2f), new Vector3(seg, 1f, T), teal);
                mk.Box(glassM, fo.P(cx, 2.1f, D / 2f - T / 2f), new Vector3(seg - .2f, 2.3f, .06f), f.r); solid.Box(glassM, fo.P(cx, 2.1f, D / 2f - T / 2f), new Vector3(seg, 2.3f, .1f), f.r);
            }
            Wall(fo.P(0, H - (H - HumanScale.DoorHeight) / 2f, D / 2f - T / 2f), new Vector3(Door, H - HumanScale.DoorHeight, T), teal);
            mk.Box(chrome, fo.P(0, H + .15f, 0), new Vector3(W + .6f, .3f, D + .6f), f.r); solid.Box(chrome, fo.P(0, H + .15f, 0), new Vector3(W + .6f, .3f, D + .6f), f.r);
            mk.Box(neonPink, fo.P(0, H - .1f, D / 2f + .02f), new Vector3(W, .12f, .04f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .6f, H, D - .6f));
            // steps up to the door
            mk.Box(gravel, f.P(0, .15f, D / 2f + .8f) + up * (floorY - g.y - .3f), new Vector3(Door + 1f, .3f, 1.4f), f.r); solid.Box(gravel, f.P(0, .15f, D / 2f + .8f) + up * (floorY - g.y - .3f), new Vector3(Door + 1f, .3f, 1.4f), f.r);
            // the counter along the back with stools, the cook behind it
            var red = WorldMaterials.Solid(new Color(.85f, .15f, .18f));
            Wall(fo.P(-1f, .55f, -D / 2f + 2.4f), new Vector3(11f, 1.1f, .7f), chrome);
            mk.Box(white, fo.P(-1f, 1.12f, -D / 2f + 2.4f), new Vector3(11.2f, .06f, .8f), f.r);
            for (var k = 0; k < 6; k++)
            {
                var s = fo.P(-5.5f + k * 1.8f, 0, -D / 2f + 3.4f); mk.Prism(chrome, s, up, .06f, .7f, 6); mk.Prism(red, s + up * .7f, up, .25f, .1f, 12);
                Interact.Seat(plife.transform, s + up * .8f, f.Fwd, "Sit at the counter").seatHeight = .8f; ArtistBits.PostCollider(ctx.Root, s, .75f, .26f);
            }
            Folk(fo.P(-1f, 0, -D / 2f + 1.3f), f.Fwd, Resident.Pose.Work, new[] { "Pie of the day is yesterday's.", "We're open 25 hours. Don't ask about the extra one.", "Order up! Nobody ordered. Order up anyway." }, Look(Residents.Tool.Spatula, true));
            Folk(fo.P(-3.3f, 0, -D / 2f + 3.4f) + up * .8f, f.Fwd, Resident.Pose.Sit, new[] { "Coffee's free if you can tell me what day it is.", "I came in for one egg. That was 1994." }, Look(Residents.Tool.Mug), null, .8f);
            // booths along the front windows
            foreach (var e in new[] { -1f, 1f })
                foreach (var b in new[] { 4.2f, 6.6f })
                {
                    var bc = fo.P(e * b, 0, D / 2f - 1.5f);
                    Wall(bc + up * .375f, new Vector3(1.1f, .75f, 1f), red);   // the table base
                    mk.Box(white, bc + up * .76f, new Vector3(1.2f, .05f, 1.1f), f.r);
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var seatC = bc + f.Right * s * 1.05f; var rot = Quaternion.LookRotation(f.Right * -s);
                        mk.Box(red, seatC + up * .22f, new Vector3(.6f, .45f, 1.1f), f.r); mk.Box(red, seatC + up * .75f + f.Right * s * .25f, new Vector3(.15f, .7f, 1.1f), f.r);
                        Interact.Seat(plife.transform, seatC + up * .47f, -f.Right * s, "Sit in the booth");
                    }
                }
            // the jukebox: press it and it lights up and dances
            var jb = fo.P(W / 2f - 1.2f, 0, -D / 2f + 1.2f); var jbm = new MeshKit();
            jbm.Box(red, new Vector3(0, .8f, 0), new Vector3(1f, 1.6f, .6f), Quaternion.identity); jbm.Prism(chrome, new Vector3(0, 1.6f, -.3f), Vector3.forward, .5f, .6f, 12);
            var jglow = new Material(WorldMaterials.Glow(Sun, .8f)) { name = "Jukebox" }; jbm.Box(jglow, new Vector3(0, 1f, .31f), new Vector3(.7f, .9f, .02f), Quaternion.identity);
            var jgo = jbm.BuildSingle("Jukebox", plife.transform, false); jgo.transform.SetPositionAndRotation(jb, Quaternion.LookRotation(f.Fwd + f.Right * -.4f));
            var toy = Interact.Toy(jgo, Toy.Kind.Glow, "Play the jukebox", new Vector3(1.1f, 2.1f, .8f)); toy.glow = jglow; toy.glowColor = Sun;
            jgo.GetComponent<BoxCollider>().center = new Vector3(0, 1f, 0); plife.jukebox = toy;
            BoxCol(ctx.Root, jb + up * .9f, new Vector3(1f, 1.8f, .7f), Quaternion.LookRotation(f.Fwd));
            ArtistBits.PointLight(ctx.Root, fo.P(0, H - .6f, 0), new Color(1f, .85f, .65f), 14f, 1.6f, true);
            // the waitress, between the counter and the booths
            Folk(fo.P(-6f, 0, 0), f.Right, Resident.Pose.Walk, new[] { "Sit anywhere. Not there.", "The jukebox only takes buttons.", "More coffee? There's always more coffee." }, Look(Residents.Tool.Mug, true),
                new[] { fo.P(-6.5f, 0, .2f), fo.P(6.5f, 0, .2f) });
            // the sign on the roof and the pie spinning on its pole
            ArtistBits.Label(ctx.Root, "ODDBALL DINER", fo.P(0, H + .9f, D / 2f - .2f), f.Fwd, .9f, Flamingo, TextAnchor.MiddleCenter);
            mk.Box(dark, fo.P(0, H + .9f, D / 2f - .3f), new Vector3(9f, 1.4f, .12f), f.r);
            ArtistBits.Label(ctx.Root, "open 25 hours", fo.P(0, 1.1f, D / 2f + .02f) + f.Right * 3.6f, f.Fwd, .22f, Ink, TextAnchor.MiddleCenter);
            var pole = fo.P(W / 2f - 2f, H + .3f, 0); mk.Prism(chrome, pole, up, .12f, 4f, 8);
            var pie = new MeshKit(); pie.Prism(WorldMaterials.Solid(new Color(.85f, .6f, .3f)), Vector3.zero, Vector3.up, 2f, .6f, 16, .9f); pie.Prism(WorldMaterials.Solid(new Color(.75f, .15f, .25f)), new Vector3(0, .6f, 0), Vector3.up, 1.7f, .05f, 16);
            var pgo = pie.BuildSingle("Giant pie", plife.transform, false); pgo.transform.position = pole + up * 4f; plife.pie = pgo.transform;
            halos.Add(pole + up * 4.3f, 3f, Sun, .4f);
            // picnic tables outside
            foreach (var e in new[] { -5.5f, 5.5f }) PicnicTable(ctx, mk, f.P(e, 0, D / 2f + 4f), f.Right);
            Ring(g, 14f);
            Shot(ctx, "the diner", f.P(4f, 1.7f, D / 2f + 14f), fo.P(0, 2f, 0));
            Shot(ctx, "inside the diner", fo.P(6f, 1.6f, 2.8f), fo.P(-4f, 1.2f, -2f));
        }

        void PicnicTable(LandContext ctx, MeshKit mk, Vector3 at, Vector3 along)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(along); var side = Vector3.Cross(up, along);
            mk.Box(wood, g + up * .75f, new Vector3(.9f, .06f, 2f), rot); foreach (var e in new[] { -.7f, .7f }) mk.Box(woodDark, g + up * .37f + along * e, new Vector3(.8f, .74f, .1f), rot);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(wood, g + up * .45f + side * s * .7f, new Vector3(.3f, .05f, 2f), rot); Interact.Bench(plife.transform, g + up * .47f + side * s * .7f - along * .8f, g + up * .47f + side * s * .7f + along * .8f, -side * s); }
            BoxCol(ctx.Root, g + up * .4f, new Vector3(1.7f, .8f, 2f), rot);
        }

        // a chair the size of a house on the highest hill: climb the crates, sit, look out over everything
        void GiantChair(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, SummitAt, 10f); var f = new Frame(g, FaceTo(g, center)); const float S = 5f, SW = 5f;
            var chairM = WorldMaterials.Solid(new Color(.95f, .3f, .4f)); var legM = WorldMaterials.Solid(new Color(.95f, .9f, .82f));
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var p = f.P(x * (SW / 2f - .4f), 0, z * (SW / 2f - .4f)); mk.Box(legM, p + up * (S / 2f - .5f), new Vector3(.7f, S + 1f, .7f), f.r); solid.Box(legM, p + up * (S / 2f - .5f), new Vector3(.7f, S + 1f, .7f), f.r); }
            mk.Box(chairM, f.P(0, S + .3f, 0), new Vector3(SW, .6f, SW), f.r); solid.Box(chairM, f.P(0, S + .3f, 0), new Vector3(SW, .6f, SW), f.r);
            mk.Box(chairM, f.P(0, S + 3.6f, -SW / 2f + .3f), new Vector3(SW, 6f, .6f), f.r); solid.Box(chairM, f.P(0, S + 3.6f, -SW / 2f + .3f), new Vector3(SW, 6f, .6f), f.r);
            for (var k = 0; k < 4; k++) mk.Box(legM, f.P(-SW / 2f + .6f + k * 1.25f, S + 3.6f, -SW / 2f + .62f), new Vector3(.3f, 5.6f, .05f), f.r);
            // crates stacked as stairs up the side
            for (var k = 0; k < 14; k++)
            {
                var h = (k + 1) * (S + .6f) / 14f; var p = f.P(SW / 2f + 1f, 0, SW / 2f - .3f - k * .62f);
                var top = ctx.OnGround(p.x, p.z).y; var hh = g.y + h - top + .2f; var c = new Vector3(p.x, top + hh / 2f - .2f, p.z);
                mk.Box(k % 2 == 0 ? wood : woodDark, c, new Vector3(1.6f, hh, .62f), f.r); solid.Box(wood, c, new Vector3(1.6f, hh, .62f), f.r);
            }
            for (var k = -1; k <= 1; k++) Interact.Seat(plife.transform, f.P(k * 1.4f, S + .6f, .4f), f.Fwd, "Sit on the giant chair").seatHeight = .1f;
            var sign = f.P(-SW / 2f - 2.5f, 0, SW / 2f + 2f); mk.Prism(woodDark, sign - up * .2f, up, .07f, 1.8f, 6); ArtistBits.PostCollider(ctx.Root, sign, 1.6f, .1f);
            mk.Box(Cream.r > 0 ? white : white, sign + up * 1.6f, new Vector3(1.8f, .7f, .06f), f.r);
            ArtistBits.Label(ctx.Root, "SIT HERE\n(if you dare)", sign + up * 1.75f + f.Fwd * .04f, f.Fwd, .16f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, f.P(0, S + 7f, 3f), Sun, 22f, 1.4f);
            // a couple picnicking at its foot
            var blanket = f.P(-6f, 0, 7f); mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .25f)), ctx.OnGround(blanket.x, blanket.z) + up * .03f, new Vector3(2.4f, .03f, 2f), f.r);
            Folk(ctx.OnGround(blanket.x - .5f, blanket.z) + up * .1f, f.Fwd, Resident.Pose.Sit, new[] { "Up there you can see three counties and a feeling.", "We climbed it once. Came down different." }, null, null, .1f);
            Folk(ctx.OnGround(blanket.x + .6f, blanket.z) + up * .1f, f.Fwd + f.Right * -.4f, Resident.Pose.Sit, new[] { "He proposed to me on that chair. To the chair, actually.", "Pass the lemonade?" }, null, null, .1f);
            Ring(g, 12f);
            Shot(ctx, "the giant chair", f.P(10f, 2f, 16f), f.P(0, 5f, 0));
            Shot(ctx, "the view from the giant chair", f.P(0, S + 1.5f, .3f), f.P(0, S - 6f, 60f));
        }

        // the world's largest ball of twine, resting on a slope; push it and it rolls
        void Twine(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, TwineAt, 8f); var f = new Frame(g, FaceTo(g, NearestLoop(g)));
            const float R = 1.7f; var ball = new MeshKit(); ball.Blob(twineM, Vector3.zero, Vector3.one * R, prng, .05f);
            for (var k = 0; k < 7; k++) ball.Prism(WorldMaterials.Solid(new Color(.7f, .56f, .34f)), Quaternion.Euler(k * 26f, k * 40f, 0) * new Vector3(0, -R * .2f, 0) - Quaternion.Euler(k * 26f, k * 40f, 0) * Vector3.forward * .05f, Quaternion.Euler(k * 26f, k * 40f, 0) * Vector3.forward, R * 1.01f, .12f, 18);
            var go = ball.BuildSingle("The world's largest ball of twine", plife.transform, false); go.transform.position = g + up * (R + .05f);
            go.AddComponent<SphereCollider>().radius = R; var rb = go.AddComponent<Rigidbody>(); rb.mass = 80f; rb.linearDamping = .2f; rb.angularDamping = .6f;
            var p = go.AddComponent<Pushable>(); p.home = go.transform.position; p.lowest = 1f;
            foreach (var e in new[] { -1f, 1f }) mk.Box(woodDark, g + f.Fwd * e * 1.4f + up * .2f, new Vector3(1.6f, .4f, .4f), f.r);
            var sign = f.P(3f, 0, 2.5f); mk.Prism(woodDark, sign - up * .2f, up, .07f, 2f, 6); ArtistBits.PostCollider(ctx.Root, sign, 1.8f, .1f);
            mk.Box(white, sign + up * 1.8f, new Vector3(2.4f, .9f, .06f), f.r);
            ArtistBits.Label(ctx.Root, "WORLD'S LARGEST\nBALL OF TWINE\nplease do not push", sign + up * 1.95f + f.Fwd * .04f, f.Fwd, .14f, Ink, TextAnchor.MiddleCenter);
            Lane(ctx, mk, g, 4f); Ring(g, 6f);
            Shot(ctx, "the ball of twine", f.P(2f, 1.7f, 9f), g + up * 1.5f);
        }

        // the upside-down house: in through the door, you walk on its ceiling; the furniture is all overhead
        void UpsideDown(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, UpsideAt, 12f); var f = new Frame(g, FaceTo(g, NearestLoop(g))); const float W = 10f, D = 8f, H = 5f, T = .25f;
            var low = g.y; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) low = Mathf.Min(low, ctx.Ground(f.P(x * W / 2f, 0, z * D / 2f).x, f.P(x * W / 2f, 0, z * D / 2f).z));
            var b = new Frame(new Vector3(g.x, low + 2.2f, g.z), f.Fwd);   // where the ceiling (now the floor) is
            var paint = WorldMaterials.Solid(new Color(.6f, .8f, .95f)); var trim = white; var roofM = WorldMaterials.Solid(new Color(.35f, .3f, .45f));
            // the roof, upside down, its ridge buried in the lawn
            mk.Box(WorldMaterials.Solid(new Color(.4f, .38f, .36f)), new Vector3(g.x, low + 1.1f, g.z), new Vector3(W - 1f, 2.2f, D - 1f), f.r); solid.Box(roofM, new Vector3(g.x, low + 1.1f, g.z), new Vector3(W, 2.2f, D), f.r);
            mk.GableRoof(roofM, b.P(0, -.02f, 0), new Vector3(W + .8f, 0, D + .8f), -2.4f, f.r);
            void Wall(Vector3 c, Vector3 s, Material m) { mk.Box(m, c, s, f.r); solid.Box(m, c, s, f.r); }
            mk.Box(WorldMaterials.Solid(new Color(.96f, .95f, .9f)), b.P(0, .02f, 0), new Vector3(W - .1f, .1f, D - .1f), f.r);
            Wall(b.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T), paint);
            foreach (var e in new[] { -1f, 1f }) Wall(b.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f), paint);
            var door = HumanScale.DoorWidth; var seg = (W - door) / 2f;
            foreach (var e in new[] { -1f, 1f }) Wall(b.P(e * (door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T), paint);
            Wall(b.P(0, HumanScale.DoorHeight + (H - HumanScale.DoorHeight) / 2f, D / 2f - T / 2f), new Vector3(door, H - HumanScale.DoorHeight, T), paint);
            Wall(b.P(0, H + .1f, 0), new Vector3(W, .2f, D), WorldMaterials.Solid(new Color(.55f, .4f, .28f)));   // the floor, now the ceiling
            // windows hung upside down (sills on top), a doorstep up to the door
            foreach (var e in new[] { -1f, 1f }) { mk.Box(glassM, b.P(e * 3f, 2.6f, D / 2f + .01f), new Vector3(1.4f, 1.2f, .04f), f.r); mk.Box(trim, b.P(e * 3f, 3.3f, D / 2f + .06f), new Vector3(1.7f, .12f, .14f), f.r); }
            for (var k = 0; k < 4; k++) { var st = f.P(0, 0, D / 2f + 2.4f - k * .55f); var top = low + (k + 1) * 2.2f / 4f; var gy = ctx.Ground(st.x, st.z); mk.Box(gravel, new Vector3(st.x, (gy + top) / 2f, st.z), new Vector3(door + .6f, top - gy + .2f, .56f), f.r); solid.Box(gravel, new Vector3(st.x, (gy + top) / 2f, st.z), new Vector3(door + .6f, top - gy + .2f, .56f), f.r); }
            IndoorSpaces.Add(b.P(0, H / 2f, 0), f.r, new Vector3(W - .6f, H, D - .6f));
            // the furniture, on the ceiling: a table and chairs, a sofa, a rug, a lamp, all hanging
            var top_ = H; var rug = WorldMaterials.Solid(new Color(.85f, .35f, .35f));
            mk.Box(rug, b.P(0, top_ - .02f, -.5f), new Vector3(4f, .02f, 3f), f.r);
            mk.Box(wood, b.P(-1.5f, top_ - .78f, -.5f), new Vector3(1.6f, .06f, 1f), f.r); foreach (var (x, z) in new[] { (-.7f, -.4f), (.7f, -.4f), (.7f, .4f), (-.7f, .4f) }) mk.Box(woodDark, b.P(-1.5f + x, top_ - .38f, -.5f + z), new Vector3(.06f, .75f, .06f), f.r);
            var sofa = WorldMaterials.Solid(new Color(.95f, .75f, .3f)); mk.Box(sofa, b.P(2.6f, top_ - .3f, -2.8f), new Vector3(2.4f, .5f, .9f), f.r); mk.Box(sofa, b.P(2.6f, top_ - .75f, -3.2f), new Vector3(2.4f, .7f, .25f), f.r);
            mk.Prism(metal, b.P(-3.8f, top_, -2.8f), -up, .03f, 1.4f, 6); mk.Prism(GlowM(Sun, 1.6f), b.P(-3.8f, top_ - 1.7f, -2.8f), up, .3f, .35f, 10, .5f);
            ArtistBits.PointLight(ctx.Root, b.P(0, H - 1.2f, 0), new Color(1f, .85f, .6f), 12f, 1.4f, true);
            // the owner, reading the paper upside down in a chair on the ceiling
            var chairAt = b.P(-1.5f, top_ - .47f, .6f);
            mk.Box(wood, chairAt + up * .02f, new Vector3(.55f, .05f, .55f), f.r); mk.Box(wood, chairAt + up * .35f - f.Fwd * -.25f, new Vector3(.55f, .7f, .05f), f.r);
            var owner = Folk(chairAt, -f.Fwd, Resident.Pose.Sit, new[] { "Gravity's more of a suggestion in this house.", "Mind the chandelier. It's in the floor.", "I like it this way. The dust falls up." });
            owner.transform.rotation = Quaternion.LookRotation(-f.Fwd, -up);
            // a chandelier growing out of the floor like a plant, and a ceiling fan on the floor you can spin
            var ch = b.P(1.5f, 0, 1f); mk.Prism(metal, ch, up, .04f, .9f, 6); for (var k = 0; k < 6; k++) { var a = k * 60f * Mathf.Deg2Rad; var arm = ch + up * .9f + (f.Fwd * Mathf.Cos(a) + f.Right * Mathf.Sin(a)) * .5f; mk.Blob(GlowM(Sun, 1.4f), arm + up * .15f, Vector3.one * .1f, ctx.Rng, 0f); }
            ArtistBits.PostCollider(ctx.Root, ch, 1f, .5f);
            var fan = new MeshKit(); fan.Prism(metal, Vector3.zero, Vector3.up, .15f, .3f, 8); for (var k = 0; k < 4; k++) fan.Box(woodDark, Quaternion.Euler(0, k * 90f, 0) * new Vector3(0, .3f, .8f), new Vector3(.25f, .03f, 1.3f), Quaternion.Euler(0, k * 90f, 0));
            var fgo = fan.BuildSingle("Ceiling fan on the floor", plife.transform, false); fgo.transform.position = b.P(2.4f, 0, 1.2f);
            var ft = Interact.Toy(fgo, Toy.Kind.Spin, "Spin the fan", new Vector3(3f, .8f, 3f)); ft.amount = 1080f; ft.seconds = 3f;
            // outside: an upside-down mailbox and a flamingo standing on its head
            var mb = f.P(-4f, 0, D / 2f + 4f); mk.Box(WorldMaterials.Solid(new Color(.25f, .45f, .65f)), mb + up * .3f, new Vector3(.3f, .32f, .55f), f.r); mk.Prism(woodDark, mb + up * .45f, up, .05f, 1.3f, 6); ArtistBits.PostCollider(ctx.Root, mb, 1.7f, .2f);
            Flamingo_(ctx, mk, f.P(4f, 0, D / 2f + 3f) + up * 1.5f, 90f);   // up a little: it's on its head, near enough
            Lane(ctx, mk, f.P(0, 0, D / 2f + 3f), 0f); Ring(g, 10f);
            Shot(ctx, "the upside-down house", f.P(6f, 2f, D / 2f + 12f), b.P(0, 2f, 0));
            Shot(ctx, "inside the upside-down house", b.P(3.6f, 1.6f, 2.6f), b.P(-2f, H - .6f, -1.5f));
        }

        // Littleville: a model village at knee height with its own tiny folk and tiny cars to pick up
        void Littleville(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, LittleAt, 16f); var f = new Frame(g, FaceTo(g, NearestLoop(g))); const float R = 9f;
            var lawn = WorldMaterials.Solid(new Color(.45f, .75f, .35f)); mk.Prism(lawn, g - up * .3f, up, R + 2f, .45f, 28);
            var ring = new List<Vector3>(); for (var k = 0; k <= 40; k++) { var a = k * Mathf.PI * 2f / 40f; ring.Add(g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R * .6f) + up * .16f); }
            for (var k = 0; k < 40; k++) mk.Box(asphalt, (ring[k] + ring[k + 1]) / 2f, new Vector3(.8f, .02f, Vector3.Distance(ring[k], ring[k + 1]) + .05f), Quaternion.LookRotation(ring[k + 1] - ring[k]));
            Color[] walls = { new Color(.95f, .9f, .75f), new Color(.6f, .8f, .95f), new Color(1f, .7f, .75f), new Color(.75f, .9f, .6f), new Color(1f, .85f, .45f) };
            for (var k = 0; k < 10; k++)
            {
                var a = (k + .5f) * Mathf.PI * 2f / 10f; var rr = (k % 2 == 0) ? R * .85f : R * .35f; var p = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rr + up * .15f;
                var face = FaceTo(p, g); var rot = Quaternion.LookRotation(face); var w = .8f + (k % 3) * .2f; var h = .6f + (k % 2) * .3f;
                if (k == 3) { mk.Box(white, p + up * .6f, new Vector3(.8f, 1.2f, 1.2f), rot); mk.Prism(white, p + up * 1.2f - face * .3f, up, .2f, .8f, 4); mk.Cone(roofM2, p + up * 2f - face * .3f, .25f, .6f, 4); BoxCol(ctx.Root, p + up * .6f, new Vector3(.8f, 1.2f, 1.2f), rot); continue; }   // the church
                mk.Box(WorldMaterials.Solid(walls[k % walls.Length]), p + up * h / 2f, new Vector3(w, h, w), rot); mk.GableRoof(roofM2, p + up * h, new Vector3(w + .1f, 0, w + .1f), .35f, rot);
                mk.Box(glassM, p + up * h * .5f + face * (w / 2f + .01f), new Vector3(.2f, .18f, .02f), rot);
                BoxCol(ctx.Root, p + up * h / 2f, new Vector3(w, h, w), rot);
            }
            // a tiny crooked water tower, like the big one
            var wt = g + f.Right * 2f; var lean = Quaternion.Euler(9f, 0, 7f); mk.Prism(metal, wt, lean * up, .05f, 1.6f, 6); mk.Prism(WorldMaterials.Solid(new Color(.75f, .8f, .82f)), wt + lean * (up * 1.6f), lean * up, .35f, .45f, 12);
            // tiny folk (a fifth of life size), and tiny cars you can pick up
            string[] tiny = { "Don't step on the mayor!", "We're normal size. You're just big.", "Welcome to Littleville! Mind your feet.", "Tiny traffic today." };
            for (var k = 0; k < 7; k++)
            {
                var a = k * Mathf.PI * 2f / 7f + .3f; var p = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R * (k % 2 == 0 ? .48f : .72f)) + up * .15f;
                var res = Folk(p, FaceTo(p, g) + new Vector3(Mathf.Sin(k), 0, 0), k == 2 ? Resident.Pose.Wave : Resident.Pose.Stand, tiny); res.transform.localScale = Vector3.one * .2f;
            }
            Color[] cars = { new Color(.9f, .2f, .2f), Sky, Sun };
            for (var k = 0; k < 3; k++)
            {
                var a = k * 2.1f + .8f; var p = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R * .6f) + up * .2f; var car = new MeshKit(); var cm = WorldMaterials.Solid(cars[k]);
                car.Box(cm, new Vector3(0, .08f, 0), new Vector3(.32f, .12f, .65f), Quaternion.identity); car.Box(cm, new Vector3(0, .2f, -.05f), new Vector3(.28f, .12f, .32f), Quaternion.identity); car.Box(dark, new Vector3(0, .2f, -.05f), new Vector3(.29f, .08f, .25f), Quaternion.identity);
                var go = car.BuildSingle("Tiny car", plife.transform, false); go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(Vector3.Cross(up, p - g)));
                Interact.Carry(go, "the tiny car", .5f, g.y - 5f);
            }
            PicnicTable(ctx, mk, f.P(0, 0, R + 4f), f.Right);
            var sign = f.P(R * .2f, 0, R + 1.6f); mk.Prism(woodDark, sign - up * .2f, up, .05f, 1.2f, 6); mk.Box(white, sign + up * 1f, new Vector3(1.6f, .6f, .05f), f.r); ArtistBits.PostCollider(ctx.Root, sign, 1f, .08f);
            ArtistBits.Label(ctx.Root, "LITTLEVILLE\npop. 9 (tiny)", sign + up * 1.1f + f.Fwd * .03f, f.Fwd, .13f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, g + up * 6f, new Color(1f, .9f, .7f), 16f, 1.1f);
            Lane(ctx, mk, f.P(0, 0, R + 2f), 0f); Ring(g, R + 2f);
            Shot(ctx, "Littleville", f.P(3f, 2.4f, R + 6f), g + up * .5f);
        }
        Material roofM2 => roofTiny != null ? roofTiny : (roofTiny = WorldMaterials.Solid(new Color(.55f, .25f, .22f)));
        Material roofTiny;

        // the drive-in by the sea: cars to sit in facing a screen of shapes, a snack shack, a bonfire with log seats
        void DriveIn(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, DriveAt, 20f); var screenAt = g + Vector3.right * 14f; var f = new Frame(g, Vector3.right);   // looking east, out to sea
            var s = ctx.OnGround(screenAt.x, screenAt.z); const float SW = 18f, SH = 9f;
            foreach (var e in new[] { -1f, 1f }) { var p = s + f.Right * e * (SW / 2f - 1f); mk.Box(woodDark, p + up * 5f, new Vector3(.6f, 10f, .6f), f.r); solid.Box(woodDark, p + up * 5f, new Vector3(.6f, 10f, .6f), f.r); }
            mk.Box(dark, s + up * (3f + SH / 2f) + f.Fwd * .2f, new Vector3(SW + .6f, SH + .6f, .3f), f.r); solid.Box(dark, s + up * (3f + SH / 2f) + f.Fwd * .2f, new Vector3(SW + .6f, SH + .6f, .4f), f.r);
            mk.Box(screenM, s + up * (3f + SH / 2f) - f.Fwd * .02f, new Vector3(SW, SH, .02f), f.r);
            // the shapes on the screen (a few glowing pieces OddballPlacesLife moves about)
            Color[] shapeC = { Flamingo, Sky, Sun, new Color(.5f, 1f, .6f) };
            for (var k = 0; k < 4; k++)
            {
                var sk = new MeshKit(); var m = new Material(WorldMaterials.Glow(shapeC[k], 1.6f)) { name = "Screen shape" };
                if (k % 2 == 0) sk.Prism(m, Vector3.zero, Vector3.back, 1.2f + k * .3f, .02f, k == 0 ? 3 : 16); else sk.Box(m, Vector3.zero, new Vector3(2f, 2f, .02f), Quaternion.Euler(0, 0, 45f));
                var go = sk.BuildSingle("Screen shape", plife.transform, false); go.transform.rotation = f.r; plife.shapes.Add((go.transform, s + up * (3f + SH / 2f) - f.Fwd * .06f, f.Right, k));
            }
            ArtistBits.PointLight(ctx.Root, s + up * 7f - f.Fwd * 6f, new Color(.85f, .9f, 1f), 26f, 1.6f);
            // the cars, three rows facing the screen, a speaker post by each, seats inside
            Color[] paint = { new Color(.9f, .3f, .3f), Sky, new Color(.95f, .85f, .5f), new Color(.5f, .85f, .7f), Flamingo, new Color(.95f, .95f, .92f) };
            var n = 0;
            for (var row = 0; row < 3; row++)
                for (var col = -1; col <= 1; col++)
                {
                    if (row == 1 && col == 0) continue;
                    var c = s - f.Fwd * (16f + row * 8f) + f.Right * (col * 5.5f + (row % 2) * 1.5f); c = ctx.OnGround(c.x, c.z);
                    var pm = WorldMaterials.Solid(paint[n++ % paint.Length]); var rot = f.r;
                    mk.Box(pm, c + up * .7f, new Vector3(2f, .7f, 4.6f), rot); mk.Box(pm, c + up * 1.05f + f.Fwd * 1.6f, new Vector3(2f, .2f, 1.2f), rot);
                    mk.Box(chrome, c + up * .5f + f.Fwd * 2.32f, new Vector3(2f, .3f, .1f), rot); mk.Box(glassM, c + up * 1.35f + f.Fwd * .9f, new Vector3(1.8f, .55f, .04f), rot * Quaternion.Euler(-25f, 0, 0));
                    foreach (var e in new[] { -1f, 1f }) foreach (var z in new[] { -1.5f, 1.5f }) mk.Prism(dark, c + f.Right * e * 1.02f + f.Fwd * z + up * .38f, f.Right * e, .38f, .22f, 10);
                    var red = WorldMaterials.Solid(new Color(.6f, .15f, .15f));
                    foreach (var e in new[] { -.45f, .45f }) { mk.Box(red, c + up * 1.12f + f.Right * e + f.Fwd * .1f, new Vector3(.7f, .2f, .7f), rot); mk.Box(red, c + up * 1.3f + f.Right * e - f.Fwd * .3f, new Vector3(.7f, .6f, .15f), rot); Interact.Seat(plife.transform, c + up * 1.22f + f.Right * e + f.Fwd * .1f, f.Fwd, "Sit in the car").seatHeight = .55f; }
                    BoxCol(ctx.Root, c + up * .75f, new Vector3(2.1f, 1.5f, 4.7f), rot);
                    var post = c + f.Right * 1.9f; mk.Prism(metal, post, up, .05f, 1.2f, 6); mk.Box(dark, post + up * 1.3f, new Vector3(.3f, .35f, .15f), rot); ArtistBits.PostCollider(ctx.Root, post, 1.4f, .1f);
                    if (n == 2) Folk(c + up * 1.22f + f.Right * .45f + f.Fwd * .1f, f.Fwd, Resident.Pose.Sit, new[] { "Tonight's picture is just shapes. Last night too.", "Shh. This is the good part. The triangle." }, null, null, .55f);
                    if (n == 5) Folk(c + up * 1.22f - f.Right * .45f + f.Fwd * .1f, f.Fwd, Resident.Pose.Sit, new[] { "I've seen this one. The square wins." }, null, null, .55f);
                }
            // the snack shack, the bonfire, log seats
            var sh = s - f.Fwd * 38f + f.Right * 12f; sh = ctx.OnGround(sh.x, sh.z); var shr = Quaternion.LookRotation(f.Right * -1f);
            mk.Box(white, sh + up * 1.5f, new Vector3(5f, 3f, 3.5f), shr); solid.Box(white, sh + up * 1.5f, new Vector3(5f, 3f, 3.5f), shr);
            for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? stripeRed : white, sh + up * 3.2f - f.Right * 2.1f + Quaternion.LookRotation(-f.Right) * new Vector3(-2.2f + k * .88f, 0, 0), new Vector3(.88f, .1f, 1.2f), shr * Quaternion.Euler(-12f, 0, 0));
            mk.Box(glassM, sh + up * 1.6f - f.Right * 1.77f, new Vector3(3f, 1f, .04f), shr);
            ArtistBits.Label(ctx.Root, "SNACKS (probably)", sh + up * 2.6f - f.Right * 1.78f, -f.Right, .3f, Flamingo, TextAnchor.MiddleCenter);
            Folk(sh - f.Right * 2.4f, -f.Right, Resident.Pose.Lean, new[] { "Popcorn's buttered on the inside.", "Hot dogs are cold. Cold dogs are hot. Don't ask." }, Look(Residents.Tool.None, true));
            var pit = s - f.Fwd * 42f - f.Right * 10f; pit = ctx.OnGround(pit.x, pit.z);
            mk.Prism(WorldMaterials.Solid(new Color(.45f, .43f, .4f)), pit - up * .1f, up, 1f, .35f, 10, .9f); mk.Blob(GlowM(new Color(1f, .55f, .2f), 2.4f), pit + up * .5f, new Vector3(.5f, .6f, .5f), ctx.Rng, .3f);
            var fl = ArtistBits.PointLight(ctx.Root, pit + up * 1f, new Color(1f, .55f, .2f), 12f, 1.8f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.8f; halos.Add(pit + up * .7f, 2.4f, new Color(1f, .55f, .2f), .7f);
            ArtistBits.PostCollider(ctx.Root, pit, .6f, 1f);
            for (var k = 0; k < 4; k++)
            {
                var a = k * Mathf.PI / 2f + .4f; var lp = pit + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3f; lp = ctx.OnGround(lp.x, lp.z); var tang = Vector3.Cross(up, lp - pit).normalized;
                mk.Prism(woodDark, lp + up * .25f - tang * 1f, tang, .25f, 2f, 8); Interact.Bench(plife.transform, lp + up * .5f - tang * .8f, lp + up * .5f + tang * .8f, FaceTo(lp, pit));
                BoxCol(ctx.Root, lp + up * .25f, new Vector3(2f, .5f, .5f), Quaternion.LookRotation(tang));
            }
            Folk(ctx.OnGround(pit.x + 3f, pit.z) + up * .5f, FaceTo(new Vector3(pit.x + 3f, 0, pit.z), pit), Resident.Pose.Sit, new[] { "Marshmallow? It's a little burnt. On purpose.", "Best seat in the house is the one by the fire." }, null, null, .5f);
            Lane(ctx, mk, s - f.Fwd * 30f, 0f); Ring(g + f.Fwd * 4f, 30f);
            Shot(ctx, "the drive-in", s - f.Fwd * 46f + up * 2.2f + f.Right * 3f, s + up * 6f);
        }

        // a lighthouse on the coast that shines inland, its lifeguard chair facing the fields, the keeper fishing in the grass
        void Lighthouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, LightAt, 6f); var inland = FaceTo(g, center); var f = new Frame(g, inland);
            const float H = 17f;
            for (var k = 0; k < 5; k++) { var r0 = 2.4f - k * .3f; mk.Prism(k % 2 == 0 ? white : stripeRed, g + up * (k * H / 5f - (k == 0 ? .5f : 0f)), up, r0, H / 5f + (k == 0 ? .5f : 0f), 16, (r0 - .3f) / r0); }
            ArtistBits.PostCollider(ctx.Root, g, H, 2.4f);
            mk.Prism(dark, g + up * H, up, 1.7f, .3f, 16); mk.Prism(GlowM(new Color(1f, .95f, .7f), 2.6f), g + up * (H + .3f), up, 1.1f, 1.6f, 12); mk.Cone(stripeRed, g + up * (H + 1.9f), 1.5f, 1.4f, 12);
            ArtistBits.PointLight(ctx.Root, g + up * (H + 1f), new Color(1f, .95f, .75f), 30f, 2f);
            plife.beamAt = g + up * (H + 1f); plife.beamInland = inland;
            // the lifeguard chair, facing the land
            var lg = f.P(5f, 0, 6f); lg = ctx.OnGround(lg.x, lg.z);
            foreach (var (x, z) in new[] { (-.6f, -.6f), (.6f, -.6f), (.6f, .6f), (-.6f, .6f) }) { mk.Box(white, lg + f.r * new Vector3(x, 1.1f, z), new Vector3(.12f, 2.2f, .12f), f.r); }
            mk.Box(stripeRed, lg + up * 2.25f, new Vector3(1.4f, .12f, 1.4f), f.r); mk.Box(stripeRed, lg + up * 2.8f - f.Fwd * .6f, new Vector3(1.4f, 1f, .1f), f.r);
            for (var k = 0; k < 4; k++) { mk.Box(white, lg + f.Fwd * (1f + k * .45f) + up * (.3f + k * .5f), new Vector3(1f, .1f, .4f), f.r); solid.Box(white, lg + f.Fwd * (1f + k * .45f) + up * (.15f + k * .25f), new Vector3(1f, .3f + k * .5f, .4f), f.r); }
            solid.Box(white, lg + up * 1.1f, new Vector3(1.4f, 2.2f, 1.4f), f.r);
            Interact.Seat(plife.transform, lg + up * 2.35f, f.Fwd, "Climb into the lifeguard chair");
            ArtistBits.Label(ctx.Root, "LIGHTHOUSE\n(INLAND)", g + up * 3f + inland * 2.45f, inland, .3f, Ink, TextAnchor.MiddleCenter);
            // the keeper, fishing into the field
            var kp = f.P(-4f, 0, 7f); Folk(ctx.OnGround(kp.x, kp.z), inland, Resident.Pose.Work, new[] { "Ships keep getting lost inland. I don't know why either.", "Biggest catch? A sofa. Lovely sofa.", "Nothing biting today. Grass is quiet." }, Look(Residents.Tool.Rod)).workRate = .3f;
            var bucket = f.P(-3.2f, 0, 6.4f); mk.Prism(Paint(1), ctx.OnGround(bucket.x, bucket.z), up, .2f, .35f, 10);
            Lane(ctx, mk, f.P(0, 0, 4f), 0f); Ring(g, 10f);
            Shot(ctx, "the lighthouse", f.P(-6f, 2f, 22f), g + up * 9f);
        }

        // the Maybe Motel: a row of rooms, a pool full of rubber ducks, loungers, and room 6
        void Motel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, MotelAt, 22f); var f = new Frame(g, FaceTo(g, NearestLoop(g))); const float L = 30f, D = 8f, H = 3.6f;
            var row = f.P(0, 0, -10f); var paintM = WorldMaterials.Solid(new Color(.98f, .85f, .55f)); var doorM = WorldMaterials.Solid(new Color(.3f, .6f, .65f));
            mk.Box(paintM, row + up * H / 2f, new Vector3(L, H, D), f.r); solid.Box(paintM, row + up * H / 2f, new Vector3(L, H, D), f.r);
            mk.Box(WorldMaterials.Solid(new Color(.4f, .38f, .36f)), row - up * .6f, new Vector3(L + .4f, 1.2f, D + .4f), f.r);
            mk.Box(WorldMaterials.Solid(new Color(.85f, .35f, .3f)), row + up * (H + .2f), new Vector3(L + 1f, .4f, D + 2.4f), f.r);
            for (var k = 0; k < 6; k++)
            {
                var dx = -L / 2f + 3f + k * 4.6f; var dp = row + f.Fwd * (D / 2f + .02f) + f.Right * dx;
                ArtistBits.Label(ctx.Root, (k + 1).ToString(), dp + up * 2.75f, f.Fwd, .25f, Ink, TextAnchor.MiddleCenter);
                mk.Box(glassM, dp + up * 1.6f + f.Right * 1.6f, new Vector3(1.2f, .9f, .04f), f.r);
                mk.Blob(GlowM(Sun, 1.4f), dp + up * 2.7f + f.Right * .9f, Vector3.one * .12f, ctx.Rng, 0f);
                if (k < 5) { mk.Box(doorM, dp + up * 1.1f, new Vector3(1f, 2.2f, .06f), f.r); continue; }
                // room 6: the door opens onto somewhere that isn't here
                mk.Box(WorldMaterials.Glow(new Color(.35f, .2f, .7f), .8f), dp + up * 1.1f - f.Fwd * .015f, new Vector3(1f, 2.2f, .01f), f.r);
                for (var st = 0; st < 12; st++) mk.Box(GlowM(Color.white, 2f), dp + up * (.2f + (st * 37 % 19) / 19f * 1.9f) + f.Right * (-.4f + (st * 23 % 11) / 11f * .8f) + f.Fwd * .002f, Vector3.one * .03f, f.r);
                var leaf = new MeshKit(); leaf.Box(doorM, new Vector3(.5f, 1.1f, 0), new Vector3(1f, 2.2f, .06f), Quaternion.identity); leaf.Blob(chrome, new Vector3(.85f, 1.05f, .06f), Vector3.one * .05f, ctx.Rng, 0f);
                var hinge = leaf.BuildSingle("Room 6 door", plife.transform, false); hinge.transform.SetPositionAndRotation(dp - f.Right * .5f + f.Fwd * .04f, f.r);
                var t = Interact.Toy(hinge, Toy.Kind.Swing, "Open room 6", null); var bc = hinge.AddComponent<BoxCollider>(); bc.center = new Vector3(.5f, 1.1f, 0); bc.size = new Vector3(1f, 2.2f, .1f);
                t.axis = Vector3.up; t.amount = -100f; t.seconds = .8f;
            }
            // the office and its sign: MAYBE MOTEL, VACANCY / NO VACANCY / MAYBE taking turns
            var office = row + f.Right * (L / 2f + 3f) + f.Fwd * 1f; mk.Box(white, office + up * 1.7f, new Vector3(5f, 3.4f, 6f), f.r); solid.Box(white, office + up * 1.7f, new Vector3(5f, 3.4f, 6f), f.r);
            mk.Box(glassM, office + up * 1.7f + f.Fwd * 3.02f, new Vector3(3f, 1.4f, .04f), f.r);
            Folk(office + f.Fwd * 3.6f - f.Right * 1.2f, f.Fwd, Resident.Pose.Lean, new[] { "Vacancy? Maybe. Depends on the vibe.", "Room 6 is booked till last Tuesday.", "Mind the ducks. They unionised." }, Look(Residents.Tool.Mug));
            var sp = f.P(L / 2f + 4f, 0, 3f); mk.Prism(metal, sp, up, .2f, 8f, 8); ArtistBits.PostCollider(ctx.Root, sp, 8f, .25f);
            mk.Box(dark, sp + up * 7.5f, new Vector3(5f, 2.2f, .3f), f.r);
            ArtistBits.Label(ctx.Root, "MAYBE MOTEL", sp + up * 7.9f + f.Fwd * .17f, f.Fwd, .55f, Flamingo, TextAnchor.MiddleCenter);
            mk.Box(dark, sp + up * 5.7f, new Vector3(3.6f, .9f, .3f), f.r);
            plife.vacancy = ArtistBits.Label(ctx.Root, "VACANCY", sp + up * 5.75f + f.Fwd * .17f, f.Fwd, .35f, Sky, TextAnchor.MiddleCenter);
            mk.Box(neonPink, sp + up * 6.6f, new Vector3(5f, .08f, .34f), f.r); halos.Add(sp + up * 7.5f, 3f, Flamingo, .5f);
            ArtistBits.PointLight(ctx.Root, sp + up * 6f + f.Fwd * 2f, Flamingo, 16f, 1.4f);
            // the pool of rubber ducks, loungers round it
            var pc = f.P(0, 0, 4f); pc = new Vector3(pc.x, g.y, pc.z); const float PW = 14f, PD = 7f;
            foreach (var (x, z, sx, sz) in new[] { (0f, -PD / 2f, PW + .8f, .4f), (0f, PD / 2f, PW + .8f, .4f), (-PW / 2f, 0f, .4f, PD), (PW / 2f, 0f, .4f, PD) })
            { var c = pc + f.r * new Vector3(x, .35f, z); mk.Box(white, c, new Vector3(sx, .7f, sz), f.r); solid.Box(white, c, new Vector3(sx, .7f, sz), f.r); }
            mk.Box(pool, pc + up * .5f, new Vector3(PW, .02f, PD), f.r); solid.Box(pool, pc + up * .3f, new Vector3(PW, .4f, PD), f.r);
            ArtistBits.PointLight(ctx.Root, pc + up * 1.5f, Sky, 12f, 1.2f);
            var duckM = WorldMaterials.Solid(new Color(1f, .85f, .15f)); var beak = WorldMaterials.Solid(new Color(1f, .5f, .1f));
            for (var k = 0; k < 40; k++)
            {
                var p = pc + f.r * new Vector3(((k * 37) % 26) / 26f * (PW - 1.2f) - (PW - 1.2f) / 2f, .56f, ((k * 53) % 17) / 17f * (PD - 1f) - (PD - 1f) / 2f); var yaw = Quaternion.Euler(0, k * 47f, 0);
                if (k < 6)
                {
                    var dk = new MeshKit(); dk.Blob(duckM, new Vector3(0, .1f, 0), new Vector3(.18f, .13f, .24f), prng, 0f); dk.Blob(duckM, new Vector3(0, .26f, .12f), Vector3.one * .1f, prng, 0f); dk.Box(beak, new Vector3(0, .25f, .24f), new Vector3(.08f, .03f, .08f), Quaternion.identity);
                    var go = dk.BuildSingle("Rubber duck", plife.transform, false); go.transform.SetPositionAndRotation(p + up * .02f, yaw); Interact.Carry(go, "the rubber duck", .2f, g.y - 5f).GetComponent<Rigidbody>().isKinematic = true;
                    continue;
                }
                mk.Blob(duckM, p + up * .1f, new Vector3(.18f, .13f, .24f), ctx.Rng, 0f); mk.Blob(duckM, p + up * .26f + yaw * Vector3.forward * .12f, Vector3.one * .1f, ctx.Rng, 0f);
            }
            for (var k = 0; k < 4; k++)
            {
                var lp = pc + f.Fwd * (PD / 2f + 2.2f) + f.Right * (-5.4f + k * 3.6f); lp = ctx.OnGround(lp.x, lp.z); var lr = Quaternion.LookRotation(-f.Fwd);
                mk.Box(white, lp + up * .3f, new Vector3(.7f, .1f, 1.8f), lr); mk.Box(white, lp + up * .55f + f.Fwd * .8f, new Vector3(.7f, .1f, .6f), lr * Quaternion.Euler(-50f, 0, 0));
                mk.Box(k % 2 == 0 ? neonBlue : stripeRed, lp + up * .37f, new Vector3(.6f, .04f, 1.7f), lr);
                Interact.Seat(plife.transform, lp + up * .4f, -f.Fwd, "Lie on the lounger").seatHeight = .3f;
                BoxCol(ctx.Root, lp + up * .3f, new Vector3(.7f, .6f, 1.8f), lr);
                if (k == 1) Folk(lp + up * .4f, -f.Fwd, Resident.Pose.Sit, new[] { "I checked in on a Thursday. Haven't checked out of Thursday.", "The ducks are friendly. Mostly the yellow ones." }, Look(), null, .3f);
            }
            Folk(pc + f.r * new Vector3(-PW / 2f - 1.5f, 0, 0), f.Fwd, Resident.Pose.Walk, new[] { "Pool's closed. Pool's open. Pool's thinking about it." }, Look(Residents.Tool.None),
                new[] { pc + f.r * new Vector3(-PW / 2f - 1.4f, 0, -PD / 2f - 1.4f), pc + f.r * new Vector3(PW / 2f + 1.4f, 0, -PD / 2f - 1.4f), pc + f.r * new Vector3(PW / 2f + 1.4f, 0, PD / 2f + 1.4f), pc + f.r * new Vector3(-PW / 2f - 1.4f, 0, PD / 2f + 1.4f) });
            for (var k = 0; k < 2; k++) Pickup(ctx, mk, ctx.OnGround(f.P(-L / 2f + 4f + k * 7f, 0, -1f).x, f.P(-L / 2f + 4f + k * 7f, 0, -1f).z), -f.Fwd, k + 1);
            for (var k = 0; k < 7; k++) Flamingo_(ctx, mk, f.P(-L / 2f - 3f + (k % 3) * .9f, 0, 2f + k * .8f), k * 40f);
            Lane(ctx, mk, f.P(0, 0, 10f), 0f); Ring(f.P(0, 0, -2f), 24f);
            Shot(ctx, "the Maybe Motel", f.P(-8f, 2.2f, 18f), f.P(2f, 2f, -4f));
        }

        // a field of doors: open any of them; one opens onto the sky
        void Doors(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, DoorsAt, 16f); var f = new Frame(g, FaceTo(g, NearestLoop(g)));
            Color[] doorC = { new Color(.85f, .2f, .2f), Sky, Sun, new Color(.95f, .95f, .92f), new Color(.4f, .7f, .45f), Flamingo, new Color(.5f, .35f, .25f) };
            var n = 0;
            for (var i = -3; i <= 3; i++)
                for (var j = -2; j <= 2; j++)
                {
                    if ((i + j) % 2 != 0 && n > 3) { n++; continue; }
                    var p = f.P(i * 4.2f + (j % 2) * 1.3f, 0, j * 4.5f); p = ctx.OnGround(p.x, p.z); var rot = Quaternion.LookRotation(Quaternion.Euler(0, (i * 7 + j * 13) % 30 - 15, 0) * f.Fwd); var fw = rot * Vector3.forward; var rt = rot * Vector3.right;
                    mk.Box(white, p + up * 1.15f - rt * .58f, new Vector3(.12f, 2.4f, .16f), rot); mk.Box(white, p + up * 1.15f + rt * .58f, new Vector3(.12f, 2.4f, .16f), rot); mk.Box(white, p + up * 2.35f, new Vector3(1.28f, .12f, .16f), rot);
                    BoxCol(ctx.Root, p + up * 1.2f, new Vector3(1.3f, 2.4f, .2f), rot);
                    var special = i == 2 && j == -1;
                    if (special) { mk.Box(WorldMaterials.Glow(new Color(.55f, .8f, 1f), 1.6f), p + up * 1.1f, new Vector3(1f, 2.2f, .02f), rot); for (var c = 0; c < 3; c++) mk.Blob(WorldMaterials.Glow(Color.white, 1.2f), p + up * (1.5f + c * .2f) + rt * (-.25f + c * .25f) + fw * .02f, new Vector3(.18f, .08f, .01f), ctx.Rng, 0f); ArtistBits.PointLight(ctx.Root, p + up * 1.2f + fw * .6f, new Color(.6f, .8f, 1f), 6f, 1.2f); }
                    var leaf = new MeshKit(); var dm = WorldMaterials.Solid(doorC[n % doorC.Length]);
                    leaf.Box(dm, new Vector3(.52f, 1.1f, 0), new Vector3(1.04f, 2.2f, .06f), Quaternion.identity); leaf.Blob(chrome, new Vector3(.9f, 1.05f, .05f), Vector3.one * .05f, prng, 0f);
                    var go = leaf.BuildSingle("Door in a field", plife.transform, false); go.transform.SetPositionAndRotation(p - rt * .52f, rot);
                    var t = Interact.Toy(go, Toy.Kind.Swing, special ? "Open the blue door" : "Open the door", null); var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(.52f, 1.1f, 0); bc.size = new Vector3(1.04f, 2.2f, .1f);
                    t.axis = Vector3.up; t.amount = -105f; t.seconds = .7f; n++;
                }
            Folk(f.P(-6.5f, 0, -4.2f), f.Fwd, Resident.Pose.Work, new[] { "I knocked on all of them. One knocked back.", "Lost my keys in here. Which door, though?" }, Look()).workRate = 1.6f;
            var sign = f.P(-9f, 0, 7f); mk.Prism(woodDark, sign - up * .2f, up, .07f, 1.8f, 6); mk.Box(white, sign + up * 1.6f, new Vector3(2f, .6f, .06f), f.r); ArtistBits.PostCollider(ctx.Root, sign, 1.6f, .1f);
            ArtistBits.Label(ctx.Root, "DOORS: TRY ONE", sign + up * 1.6f + f.Fwd * .04f, f.Fwd, .16f, Ink, TextAnchor.MiddleCenter);
            Lane(ctx, mk, f.P(0, 0, 12f), 0f); Ring(g, 18f);
            Shot(ctx, "the field of doors", f.P(4f, 1.8f, 16f), g + up * 1.2f);
        }

        // a stretch of road beside the real one that loops the loop, a car stuck at the top
        void LoopTheLoop(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, LoopAt, 10f); var road = NearestLoop(g); var along = Vector3.Cross(up, FaceTo(g, road)); var f = new Frame(g, along); const float R = 6f, W = 4f;
            var prevL = Vector3.zero; var prevR = Vector3.zero;
            for (var k = 0; k <= 36; k++)
            {
                var a = k / 36f * Mathf.PI * 2f; var off = (k / 36f - .5f) * (W + .6f);
                var c = f.P(off, R - Mathf.Cos(a) * R + .2f, Mathf.Sin(a) * R);
                var L = c - f.Right * W / 2f; var Rr = c + f.Right * W / 2f;
                if (k > 0) { var inside = f.P(off, R + .2f, 0); mk.Quad(asphalt, prevL, prevR, Rr, L, inside); mk.Quad(asphalt, L, Rr, prevR, prevL, c + (c - inside).normalized * 2f); }
                if (k % 3 == 0) mk.Box(line, c + (f.P(off, R + .2f, 0) - c).normalized * .02f, new Vector3(.15f, .02f, .8f), Quaternion.LookRotation(Vector3.Cross(f.Right, (f.P(off, R + .2f, 0) - c).normalized), (f.P(off, R + .2f, 0) - c).normalized));
                if (Mathf.Cos(a) > .3f && k % 2 == 0) BoxCol(ctx.Root, c - up * .1f, new Vector3(W, .3f, 1.2f), Quaternion.LookRotation(Vector3.Cross(f.Right, up)));
                prevL = L; prevR = Rr;
            }
            // the car at the top, upside down, quite happy
            var top = f.P(0, R * 2f + .2f, 0); var car = WorldMaterials.Solid(Sun);
            mk.Box(car, top - up * .55f, new Vector3(1.8f, .6f, 3.8f), f.r); mk.Box(car, top - up * 1.1f, new Vector3(1.7f, .6f, 1.8f), f.r); mk.Box(dark, top - up * 1.12f + f.Fwd * .4f, new Vector3(1.72f, .4f, 1.2f), f.r);
            var sign = f.P(-W - 1f, 0, R + 3f); mk.Prism(metal, sign - up * .2f, up, .05f, 2.4f, 6); mk.Box(WorldMaterials.Solid(new Color(1f, .55f, .1f)), sign + up * 2.2f, new Vector3(1.8f, .9f, .05f), f.r * Quaternion.Euler(0, 180f, 0)); ArtistBits.PostCollider(ctx.Root, sign, 2.3f, .08f);
            ArtistBits.Label(ctx.Root, "ROAD CLOSED\nFOR LOOPING", sign + up * 2.25f - f.Fwd * .04f, -f.Fwd, .15f, Ink, TextAnchor.MiddleCenter);
            Ring(g, 9f);
            Shot(ctx, "the road that loops the loop", f.P(10f, 2f, -14f), g + up * 5f);
        }

        // ---------------------------------------------------------------- the countryside, on purpose
        /// A farm on the south loop (barn, tractor, the farmer), then fields along the whole loop: wheat, corn,
        /// sunflowers, kudzu, a flamingo pasture, each behind a white fence with a hedgerow behind; two kudzu tunnels the
        /// road runs through; one lone tree on each hilltop; dark kudzu in the hollows.
        void Farm(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, FarmAt, 14f); var f = new Frame(g, FaceTo(g, NearestLoop(g)));
            Barn(ctx, mk, solid, f.P(-8f, 0, -6f), f.Fwd);
            // a tractor with the farmer in it
            var tr = f.P(8f, 0, 2f); tr = ctx.OnGround(tr.x, tr.z); var trr = Quaternion.LookRotation(f.Right); var green = WorldMaterials.Solid(new Color(.25f, .6f, .25f));
            mk.Box(green, tr + up * 1.3f, new Vector3(1.4f, 1f, 3f), trr); mk.Box(green, tr + up * 2.3f - f.Right * .6f, new Vector3(1.4f, 1.2f, 1.4f), trr); mk.Box(glassM, tr + up * 2.4f - f.Right * .6f, new Vector3(1.42f, .8f, 1.2f), trr);
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(dark, tr - f.Right * .9f + f.Fwd * e * .75f + up * .9f, f.Fwd * e, .9f, .4f, 12); mk.Prism(dark, tr + f.Right * 1f + f.Fwd * e * .75f + up * .5f, f.Fwd * e, .5f, .3f, 10); }
            mk.Prism(metal, tr + f.Right * .8f + up * 1.8f, up, .07f, 1.2f, 6);
            BoxCol(ctx.Root, tr + up * 1.4f, new Vector3(2.2f, 2.8f, 3.4f), trr);
            Folk(tr + up * 1.9f - f.Right * .6f, f.Right, Resident.Pose.Sit, new[] { "Corn's coming up nicely. Sideways, but nicely.", "I grow mostly feelings out here. Some beans.", "This tractor's been in the family since Tuesday." }, Look(Residents.Tool.None), null, .5f).GetComponent<Resident>();
            Ring(g, 18f);
            Fields(ctx, mk, solid);
            Hilltops(ctx, mk);
            Hollows(ctx, mk);
            Shot(ctx, "the farm and the fields", f.P(0, 3f, 22f), f.P(0, 1f, -6f));
        }

        bool NearPlace(float x, float z, float pad)
        {
            foreach (var (at, r) in PlaceRings) if ((at.x - x) * (at.x - x) + (at.y - z) * (at.y - z) < (r + pad) * (r + pad)) return true;
            return Vector2.Distance(new Vector2(x, z), new Vector2(center.x + DinerOff.x, center.z + DinerOff.y)) < 16f + pad;
        }

        bool FieldOk(LandContext ctx, float x, float z, float pad) => ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad + 4f) && !NearPlace(x, z, pad) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 30f;

        void Fields(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(77);
            var wheat = new WindMesh(); var wheatMat = WindMesh.Material(.5f, 1f, new Color(.9f, .78f, .38f));
            Material stalk = WorldMaterials.Solid(new Color(.35f, .55f, .2f)), cob = WorldMaterials.Solid(new Color(.95f, .82f, .3f)), petal = WorldMaterials.Solid(new Color(1f, .82f, .1f)), seedM = WorldMaterials.Solid(new Color(.25f, .15f, .08f));
            var crops = new MeshKit();   // corn and sunflowers: walked into on purpose, like leaves
            var kinds = new[] { 0, 1, 0, 2, 3, 0, 1, 4, 2, 0, 3, 1 };   // wheat, corn, sunflowers, kudzu, flamingo pasture
            const float Seg = 40f, Near = RoadW / 2f + 3.5f, Far = 30f;
            var segN = 0; var tunnels = new HashSet<int> { 3, 13, 24 };
            foreach (var road in mainRoads)
            {
            float run = 0f;
            for (var i = 1; i < road.Count; i++)
            {
                var a = road[i - 1]; var b = road[i]; var d = b - a; d.y = 0; var len = d.magnitude; if (len < .01f || len > 10f) continue; d /= len;
                run += len; if (run < Seg) continue; run = 0f; segN++;
                var side = Vector3.Cross(up, d); var mid = b;
                if (tunnels.Contains(segN)) { Tunnel(ctx, mk, road, i); continue; }
                foreach (var s in new[] { -1f, 1f })
                {
                    var kind = kinds[(segN * 2 + (s > 0 ? 1 : 0)) % kinds.Length];
                    var cells = 0; var fenceFrom = -1f; var fenceTo = -1f;
                    for (var u = -Seg / 2f + 2f; u <= Seg / 2f - 2f; u += 2f)
                        for (var v = Near; v <= Far; v += 2f)
                        {
                            var p = mid - d * u + side * s * v; if (!FieldOk(ctx, p.x, p.z, 1f) || NearPath(p.x, p.z, RoadW / 2f + 2.5f)) continue;
                            if (v <= Near + .1f) { if (fenceFrom < 0f) fenceFrom = u; fenceTo = u; }
                            var q = ctx.OnGround(p.x + (float)(rng.NextDouble() - .5), p.z + (float)(rng.NextDouble() - .5)); cells++;
                            switch (kind)
                            {
                                case 0: for (var k = 0; k < 5; k++) { var w = ctx.OnGround(q.x + (float)rng.NextDouble() * 2f - 1f, q.z + (float)rng.NextDouble() * 2f - 1f); var c = Color.Lerp(new Color(.92f, .78f, .35f), new Color(1f, .9f, .55f), (float)rng.NextDouble()); wheat.Tuft(w - up * .04f, .8f + (float)rng.NextDouble() * .3f, c, new Color(1f, .95f, .7f), rng, 4, .05f); } break;
                                case 1: if (((int)(v / 2f)) % 2 == 0) { crops.Prism(stalk, q, up, .05f, 2.3f, 4); crops.Box(cob, q + up * 1.5f + side * .1f, new Vector3(.1f, .3f, .1f), Quaternion.identity); crops.Box(stalk, q + up * 1.9f, new Vector3(.6f, .04f, .12f), Quaternion.LookRotation(d)); } break;
                                case 2: if (((int)(u / 2f) + (int)(v / 2f)) % 2 == 0) { crops.Prism(stalk, q, up, .05f, 2f, 4); var head = q + up * 2.05f - side * s * .1f; crops.Prism(petal, head, -side * s, .4f, .06f, 10); crops.Prism(seedM, head - side * s * .05f, -side * s, .2f, .04f, 10); } break;
                                case 3: if (rng.NextDouble() < .35) { var kr = 1f + (float)rng.NextDouble(); mk.Blob(rng.NextDouble() < .5 ? kudzu : kudzuDark, q + up * .5f, Vector3.one * kr, ctx.Rng, .35f); BoxCol(ctx.Root, q + up * (kr * .5f + .2f), new Vector3(kr * 1.7f, kr + 1f, kr * 1.7f), Quaternion.identity); } break;
                                case 4: if (rng.NextDouble() < .12) Flamingo_(ctx, mk, q, (float)rng.NextDouble() * 360f); break;
                            }
                        }
                    if (cells < 20) continue;
                    // a white fence along the road, a hedgerow along the back; a scarecrow in the wheat
                    if (fenceFrom > -Seg) Fence(ctx, mk, mid - d * fenceFrom + side * s * (Near - 1f), mid - d * fenceTo + side * s * (Near - 1f));
                    for (var u = -Seg / 2f + 2f; u <= Seg / 2f - 2f; u += 4f) { var h = mid - d * u + side * s * (Far + 3f); if (FieldOk(ctx, h.x, h.z, 1f)) { var hg = ctx.OnGround(h.x, h.z); mk.Blob(kudzuDark, hg + up * 1.6f, new Vector3(2.4f, 2f, 2.4f), ctx.Rng, .35f); mk.Blob(kudzu, hg + up * 2.8f, Vector3.one * 1.5f, ctx.Rng, .3f); BoxCol(ctx.Root, hg + up * 1.6f, new Vector3(3.6f, 3.2f, 3.6f), Quaternion.identity); } }
                    if (kind == 0 && segN % 2 == 0)
                    {
                        var sc = mid + side * s * (Near + 10f); if (FieldOk(ctx, sc.x, sc.z, 1f))
                        {
                            var l = Residents.Random(prng, new[] { new Color(.55f, .4f, .65f), new Color(.75f, .35f, .25f) }, new[] { new Color(.35f, .4f, .55f) }, new[] { Residents.Hat.Straw }, new[] { new Color(.9f, .8f, .5f) });
                            l.skin = new Color(.9f, .8f, .5f); var sres = Folk(ctx.OnGround(sc.x, sc.z), -side * s, Resident.Pose.Wave, new[] { "I'm outstanding in my field.", "Crows? Never met 'em. Lovely people, I hear." }, l);
                            sres.speed = 0f;
                        }
                    }
                }
            }
            }
            wheat.Build("Backroads wheat", ctx.Root, wheatMat);
            crops.Build("Soft crops", ctx.Root, false);
        }

        void Fence(LandContext ctx, MeshKit mk, Vector3 a, Vector3 b)
        {
            var up = Vector3.up; var d = b - a; d.y = 0; var len = d.magnitude; if (len < 3f) return; var dir = d / len; var n = Mathf.CeilToInt(len / 3f);
            Vector3 prev = default;
            for (var k = 0; k <= n; k++)
            {
                var p = a + dir * (len * k / n); p = ctx.OnGround(p.x, p.z);
                mk.Box(fenceM, p + up * .55f, new Vector3(.12f, 1.1f, .12f), Quaternion.LookRotation(dir));
                if (k > 0) foreach (var h in new[] { .45f, .9f }) { var s = prev + up * h; var e = p + up * h; mk.Box(fenceM, (s + e) / 2f, new Vector3(.06f, .1f, Vector3.Distance(s, e)), Quaternion.LookRotation(e - s)); }
                if (k > 0) BoxCol(ctx.Root, (prev + p) / 2f + up * .55f, new Vector3(.15f, 1.1f, Vector3.Distance(prev, p)), Quaternion.LookRotation(p - prev));
                prev = p;
            }
        }

        /// Kudzu arches over the road, so it runs through a green tunnel for a while.
        void Tunnel(LandContext ctx, MeshKit mk, List<Vector3> road, int at)
        {
            var up = Vector3.up;
            for (var i = Mathf.Max(1, at - 14); i < Mathf.Min(road.Count, at + 14); i += 2)
            {
                var a = road[i - 1]; var b = road[i]; var d = b - a; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d);
                for (var k = 0; k <= 8; k++)
                {
                    var t = k / 8f * Mathf.PI; var p = b + side * Mathf.Cos(t) * (RoadW / 2f + 1.6f) + up * (Mathf.Sin(t) * 5.2f + .4f);
                    var br = 1.1f + (k == 0 || k == 8 ? .4f : 0f);
                    mk.Blob(k % 2 == 0 ? kudzu : kudzuDark, p, Vector3.one * br, ctx.Rng, .35f);
                    if (p.y - br < b.y + 2.3f) BoxCol(ctx.Root, p, Vector3.one * br * 1.8f, Quaternion.LookRotation(d));   // the low parts of the arch, at walking height
                }
            }
            ArtistBits.PointLight(ctx.Root, road[at] + up * 3.5f, new Color(.6f, 1f, .5f), 14f, .9f);
        }

        /// One lone tree on each hilltop (the highest point in each 70 m stretch, if it stands above its surroundings).
        void Hilltops(LandContext ctx, MeshKit mk)
        {
            var up = Vector3.up; int count = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += 70f)
                for (var z = ctx.MinZ; z < ctx.MaxZ; z += 70f)
                {
                    float bx = 0, bz = 0, bh = -1f;
                    for (var u = 0f; u < 70f; u += 8f) for (var v = 0f; v < 70f; v += 8f) { var h = ctx.Ground(x + u, z + v); if (h > bh && ctx.IsLand(x + u, z + v, 6f)) { bh = h; bx = x + u; bz = z + v; } }
                    if (bh < 0f) continue;
                    var around = Mathf.Max(Mathf.Max(ctx.Ground(bx + 35f, bz), ctx.Ground(bx - 35f, bz)), Mathf.Max(ctx.Ground(bx, bz + 35f), ctx.Ground(bx, bz - 35f)));
                    if (bh - around < 3f) continue;   // a real hilltop, standing above everything round it
                    if (!FieldOk(ctx, bx, bz, 4f) || NearPath(bx, bz, 8f)) continue;
                    var g = ctx.OnGround(bx, bz); var h0 = 9f + (count % 3) * 2f;
                    mk.Prism(woodDark, g - up * .3f, up, .45f, h0 * .55f, 7, .6f);
                    for (var k = 0; k < 6; k++) mk.Blob(k % 2 == 0 ? WorldMaterials.Solid(new Color(.3f, .52f, .22f)) : kudzu, g + up * h0 * (.6f + (k % 3) * .1f) + new Vector3(Mathf.Cos(k * 1.1f), 0, Mathf.Sin(k * 1.1f)) * h0 * .22f, Vector3.one * h0 * .26f, ctx.Rng, .25f);
                    BoxCol(ctx.Root, g + up * 2.5f, new Vector3(1.2f, 5f, 1.2f), Quaternion.identity); count++;
                }
            Debug.Log($"The Oddball Backroads: {count} hilltop trees");
        }

        /// The hollows (low ground off the roads) filled with dark kudzu-swallowed trees: the near-black against the gold.
        void Hollows(LandContext ctx, MeshKit mk)
        {
            var count = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += 11f)
                for (var z = ctx.MinZ; z < ctx.MaxZ; z += 11f)
                {
                    float px = x + ctx.Range(0f, 11f), pz = z + ctx.Range(0f, 11f); var h = ctx.Ground(px, pz);
                    var around = (ctx.Ground(px + 40f, pz) + ctx.Ground(px - 40f, pz) + ctx.Ground(px, pz + 40f) + ctx.Ground(px, pz - 40f)) / 4f;
                    if (h > around - 2.5f || h < 4f || !FieldOk(ctx, px, pz, 4f) || NearPath(px, pz, 9f)) continue;   // a hollow: lower than the land round it
                    if (Mathf.PerlinNoise(px * .02f, pz * .02f) < .45f) continue;
                    KudzuTree(ctx, mk, ctx.OnGround(px, pz)); count++;
                }
            Debug.Log($"The Oddball Backroads: {count} trees in the hollows");
        }
    }

    /// Pushed (not carried): it finds its way home if it's lost.
    [RequireComponent(typeof(Rigidbody))]
    public class Pushable : MonoBehaviour
    {
        public Vector3 home; public float lowest = -5f; float lostFor;
        void Update()
        {
            var lost = transform.position.y < lowest || (transform.position - home).sqrMagnitude > 200f * 200f;
            lostFor = lost ? lostFor + Time.deltaTime : 0f;
            if (lostFor > 6f) { var rb = GetComponent<Rigidbody>(); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; transform.position = home; lostFor = 0f; }
        }
    }

    /// The places' moving parts: the diner's pie spinning, the jukebox dancing, shapes drifting on the drive-in screen,
    /// the lighthouse beam sweeping inland, the motel sign changing its mind.
    public class OddballPlacesLife : MonoBehaviour
    {
        public Transform pie; public Toy jukebox;
        public List<(Transform t, Vector3 at, Vector3 right, int k)> shapes = new List<(Transform, Vector3, Vector3, int)>();
        public Vector3 beamAt, beamInland; public TextMesh vacancy; public Transform sails;
        float sailSpeed = -12f;   // backwards
        public void CrankSails() => sailSpeed -= 90f;
        Transform beam; float signT; int signN;
        static readonly string[] SignWords = { "VACANCY", "NO VACANCY", "MAYBE", "PROBABLY", "VACANCY?" };

        void Start()
        {
            if (beamAt == Vector3.zero) return;
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Lighthouse beam" };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; m.SetColor("_BaseColor", new Color(1f, .95f, .7f, .18f));
            var go = new GameObject("Lighthouse beam"); go.transform.SetParent(transform, false); go.transform.position = beamAt; beam = go.transform;
            var mesh = new Mesh(); mesh.vertices = new[] { new Vector3(0, .4f, 0), new Vector3(0, -.4f, 0), new Vector3(-6f, -3f, 90f), new Vector3(6f, -3f, 90f), new Vector3(0, 3f, 90f) };
            mesh.triangles = new[] { 0, 2, 3, 0, 3, 4, 0, 4, 2, 1, 3, 2 }; mesh.RecalculateNormals();
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Update()
        {
            var t = Time.time;
            if (pie != null) pie.rotation = Quaternion.Euler(0, t * 40f, 0);
            if (jukebox != null) jukebox.transform.localScale = jukebox.On ? new Vector3(1f + Mathf.Sin(t * 8f) * .03f, 1f + Mathf.Abs(Mathf.Sin(t * 8f)) * .06f, 1f) : Vector3.one;
            foreach (var (tr, at, right, k) in shapes)
            {
                if (tr == null) continue;
                var x = Mathf.Sin(t * (.2f + k * .07f) + k * 2f) * 6f; var y = Mathf.Sin(t * (.3f + k * .05f) + k) * 2.6f;
                tr.position = at + right * x + Vector3.up * y; tr.rotation = Quaternion.LookRotation(Vector3.Cross(right, Vector3.up)) * Quaternion.Euler(0, 0, t * (10f + k * 8f));
            }
            if (beam != null)
            {
                var yaw = Mathf.Atan2(beamInland.x, beamInland.z) * Mathf.Rad2Deg + Mathf.Sin(t * .35f) * 80f;
                beam.rotation = Quaternion.Euler(4f, yaw, 0);
            }
            if (sails != null) { sails.Rotate(0, 0, sailSpeed * Time.deltaTime, Space.Self); sailSpeed = Mathf.MoveTowards(sailSpeed, -12f, 25f * Time.deltaTime); }
            if (vacancy != null && t > signT) { signT = t + Random.Range(2f, 5f); vacancy.text = SignWords[signN++ % SignWords.Length]; }
        }
    }
}
