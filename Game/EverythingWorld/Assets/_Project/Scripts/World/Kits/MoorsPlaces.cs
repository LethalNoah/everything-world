using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Alt Rock Moors laid out on purpose (the build-land standard): moor roads over the whole land and places
    /// along them made to stop at, cosy ones to settle into and a few quietly uncanny ones, with the quiet locals
    /// who live there.
    /// Roads: a loop right round the artists, three roads out from the crossroads to it, an east road along the
    /// coastal strip to the far tip, a north-west road up the headland, a south road down the tongue; benches with
    /// lamps along them, cat's eyes glinting on the verges, walled fields either side (bright grass and sheep inside,
    /// against the dark heather of the tops), pale tors crowning the real hilltops.
    /// Places here: the village (a walk-in pub with a fire, darts and regulars; a green with an oak, a pond and a
    /// football; a chapel whose bell you can ring; a shop, cottages, a bus stop where someone is waiting) and the sheep
    /// farm. The rest are in MoorsPlacesMore.
    public partial class MoorsKit
    {
        static readonly Vector2 VillageAt = new Vector2(8410, 9236), FarmAt = new Vector2(8195, 8945), HaltAt = new Vector2(8752, 9388), EastEndAt = new Vector2(8925, 9530),
            LookoutAt = new Vector2(7744, 9572), CircleAt = new Vector2(7772, 9330), TeaAt = new Vector2(8200, 9486), PhoneAt = new Vector2(7870, 9105),
            WaitingAt = new Vector2(8008, 9080), DeskAt = new Vector2(8256, 9086), BogAt = new Vector2(8512, 9398);
        static readonly Vector2[] LoopWay =
        {
            new Vector2(8110, 9425), new Vector2(8230, 9445), new Vector2(8370, 9440), new Vector2(8440, 9340), new Vector2(8410, 9238), new Vector2(8320, 9140),
            new Vector2(8260, 9040), new Vector2(8130, 9010), new Vector2(8010, 9040), new Vector2(7900, 9130), new Vector2(7850, 9260), new Vector2(7880, 9380), new Vector2(7990, 9425),
        };
        static readonly Vector2[] EastWay = { new Vector2(8440, 9340), new Vector2(8540, 9335), new Vector2(8640, 9360), new Vector2(8745, 9405), new Vector2(8840, 9470), new Vector2(8915, 9528) };
        static readonly Vector2[] TrackWay = { new Vector2(8660, 9335), new Vector2(8760, 9372), new Vector2(8850, 9425), new Vector2(8920, 9490) };
        static readonly Vector2[] NorthWestWay = { new Vector2(7880, 9380), new Vector2(7800, 9440), new Vector2(7740, 9510), new Vector2(7750, 9590), new Vector2(7775, 9638) };
        static readonly Vector2[] SouthWay = { new Vector2(8130, 9010), new Vector2(8185, 8960), new Vector2(8200, 8900), new Vector2(8186, 8862) };
        static readonly int[] Spokes = { 0, 4, 7 };   // loop waypoints the crossroads' three roads run to (north, the village, south)
        static readonly (Vector2 at, float r)[] PlaceRings =
        {
            (VillageAt, 52f), (FarmAt, 30f), (HaltAt, 22f), (EastEndAt, 8f), (LookoutAt, 14f), (CircleAt, 16f), (TeaAt, 18f), (PhoneAt, 10f), (WaitingAt, 14f), (DeskAt, 12f), (BogAt, 26f),
        };
        const float RoadW = 4f;

        /// For ArtistPlacement.Spread: keep artists off the places and the main roads (x, radius, z).
        void AvoidPlaces(List<Vector3> avoid)
        {
            foreach (var (at, r) in PlaceRings) avoid.Add(new Vector3(at.x, r + 10f, at.y));
            void Along(IList<Vector2> way, bool closed)
            {
                for (var i = 0; i < (closed ? way.Count : way.Count - 1); i++)
                {
                    var a = way[i]; var b = way[(i + 1) % way.Count]; var n = Mathf.CeilToInt(Vector2.Distance(a, b) / 18f);
                    for (var k = 0; k <= n; k++) { var p = Vector2.Lerp(a, b, k / (float)n); avoid.Add(new Vector3(p.x, 9f, p.y)); }
                }
            }
            Along(LoopWay, true);
            foreach (var s in Spokes) Along(new[] { heart2 + (LoopWay[s] - heart2).normalized * 30f, LoopWay[s] }, false);
        }

        MoorsPlacesLife plife; System.Random prng;
        readonly List<Vector3> roadPts = new List<Vector3>(); readonly List<List<Vector3>> mainRoads = new List<List<Vector3>>();
        readonly List<Vector3> marvelSpots = new List<Vector3>();
        readonly List<(Vector3 c, Vector3 f, float w, float d)> fields = new List<(Vector3, Vector3, float, float)>();
        List<Vector3> trackPts = new List<Vector3>();
        Material roadM, paleStone, wool, brass, maroon, cream;

        void PlaceMaterials()
        {
            roadM = WorldMaterials.Solid(new Color(.4f, .4f, .42f)); paleStone = WorldMaterials.Solid(new Color(.8f, .8f, .76f)); wool = WorldMaterials.Solid(new Color(.9f, .88f, .82f));
            brass = WorldMaterials.Solid(new Color(.85f, .68f, .3f), .8f); maroon = WorldMaterials.Solid(new Color(.5f, .12f, .14f)); cream = WorldMaterials.Solid(new Color(.93f, .9f, .78f));
        }

        // ---------------------------------------------------------------- residents: quiet locals
        static readonly Color[] Coats = { new Color(1f, .82f, .15f), new Color(.2f, .25f, .4f), new Color(.38f, .42f, .3f), new Color(.5f, .5f, .52f), new Color(.5f, .15f, .18f), new Color(.85f, .85f, .8f), new Color(.25f, .45f, .5f) };
        static readonly Color[] Trousers = { new Color(.22f, .24f, .3f), new Color(.35f, .3f, .25f), new Color(.3f, .32f, .34f), new Color(.2f, .2f, .22f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Beanie, Residents.Hat.Beanie, Residents.Hat.Bucket };
        static readonly Color[] HatColors = { new Color(.35f, .35f, .3f), new Color(.6f, .15f, .15f), new Color(.2f, .25f, .4f), new Color(.9f, .8f, .2f) };
        static readonly string[] Locals =
        {
            "Weather's coming in. It's always coming in.", "Mustn't grumble. I do, though.", "You'll want a coat. You've got one. Good.",
            "The mast? You stop hearing it after a year or two.", "Lovely and quiet. Too quiet, some say. Not me.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false)
        {
            var l = Residents.Random(prng, Coats, Trousers, Hats, HatColors); l.tool = tool; l.apron = apron; return l;
        }

        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Locals, route, seatH);

        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);

        // ---------------------------------------------------------------- the roads
        /// Pushes a road out of the things it must go round (the sunken pool, the district, the transmitter hill).
        void Bend(LandContext ctx, List<Vector3> pts)
        {
            var rounds = new List<Vector3>();
            if (hasPool) rounds.Add(new Vector3(pool2.x, PoolR * 1.5f + 16f, pool2.y));
            if (hasDistrict) rounds.Add(new Vector3(district2.x, Cell * 2.2f + 8f, district2.y));
            if (hasHill) rounds.Add(new Vector3(hill2.x, 46f, hill2.y));
            for (var i = 0; i < pts.Count; i++)
                foreach (var c in rounds)
                {
                    var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude;
                    if (d < c.y) { v = d > .01f ? v / d * c.y : new Vector2(c.y, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); }
                }
        }

        int roadWalk = 1;
        void MoorRoad(LandContext ctx, MeshKit mk, List<Vector3> pts)
        {
            Bend(ctx, pts); if (pts.Count < 2) return;
            Strip(mk, pts, roadM, RoadW, .2f, ctx);
            var up = Vector3.up; var eye = GlowM(new Color(1f, .95f, .75f), 1.6f);
            for (var i = 1; i < pts.Count; i++)
            {
                var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d);
                if (i % 3 == 0) mk.Box(white, pts[i] + up * .34f, new Vector3(.12f, .02f, 1.5f), Quaternion.LookRotation(d));   // the broken white line
                if (i % 6 == 0) foreach (var e in new[] { -1f, 1f }) { var c = pts[i] + side * e * (RoadW / 2f + .25f); mk.Box(eye, new Vector3(c.x, ctx.Ground(c.x, c.z) + .12f, c.z), new Vector3(.1f, .1f, .1f), Quaternion.identity); }   // cat's eyes
            }
            foreach (var p in pts) Remember(p, roadWalk);
            roadWalk++; mainRoads.Add(pts); roadPts.AddRange(pts);
        }

        /// The loop, the three roads from the crossroads, the east, north-west and south roads; benches along them.
        void MoorRoads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            PlaceMaterials(); roadPts.Clear(); mainRoads.Clear(); fields.Clear(); marvelSpots.Clear(); roadWalk = 1;
            plife = new GameObject("Moors places life").AddComponent<MoorsPlacesLife>(); plife.transform.SetParent(life.transform, false);
            prng = new System.Random(1993);
            MoorRoad(ctx, mk, Smooth(ctx, LoopWay, true));
            var c = new Vector2(center.x, center.z);
            foreach (var s in Spokes)
            {
                var to = LoopWay[s]; var dir = (to - c).normalized; var side = new Vector2(-dir.y, dir.x);
                MoorRoad(ctx, mk, Smooth(ctx, new[] { c + dir * 9f, Vector2.Lerp(c, to, .5f) + side * (s == 4 ? -14f : 12f), to }, false));
            }
            MoorRoad(ctx, mk, Smooth(ctx, EastWay, false)); MoorRoad(ctx, mk, Smooth(ctx, NorthWestWay, false)); MoorRoad(ctx, mk, Smooth(ctx, SouthWay, false));
            trackPts = Smooth(ctx, TrackWay, false, 2f, 2f);
            // the places' ground is spoken for before anything else is put down
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
        }

        /// A gravel lane from the nearest road to a place.
        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + RoadW) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * (RoadW / 2f + .1f); var b = new Vector2(from.x, from.z) + d * (len - stopShort); var side = new Vector2(-d.y, d.x);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + side * Mathf.Min(8f, len * .1f), b }, false, 1.5f, 1f);
            if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.62f, .6f, .55f)), 2.4f, .16f, ctx); foreach (var p in pts) Remember(p, 400);
        }

        /// A wooden bench (seats along it) with a street lamp beside it.
        void LampBench(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, bool lamp = true)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .45f, new Vector3(1.9f, .07f, .45f), rot); mk.Box(wood, g + up * .8f - face * .22f, new Vector3(1.9f, .5f, .06f), rot);
            foreach (var e in new[] { -.8f, .8f }) mk.Box(dark, g + up * .22f + side * e, new Vector3(.08f, .45f, .4f), rot);
            Col(g + up * .5f - face * .05f, new Vector3(1.9f, 1f, .5f), rot);
            Interact.Bench(plife.transform, g + up * .5f - side * .7f, g + up * .5f + side * .7f, face);
            if (lamp) StreetLamp(ctx, mk, ctx.OnGround(g.x + side.x * 1.8f - face.x * .4f, g.z + side.z * 1.8f - face.z * .4f));
        }

        void RoadBenches(LandContext ctx, MeshKit mk)
        {
            var up = Vector3.up; var n = 0;
            foreach (var road in mainRoads)
            {
                var run = 60f;
                for (var i = 1; i < road.Count; i++)
                {
                    run += Vector3.Distance(road[i], road[i - 1]); if (run < 170f) continue;
                    var d = road[i] - road[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d);
                    // on the side that looks downhill, facing the view
                    var s = ctx.Ground(road[i].x + side.x * 12f, road[i].z + side.z * 12f) < ctx.Ground(road[i].x - side.x * 12f, road[i].z - side.z * 12f) ? 1f : -1f;
                    var at = road[i] - side * s * (RoadW / 2f + 1.6f);
                    if (!ctx.IsLand(at.x, at.z, 3f) || InNoGrass(at.x, at.z, 3f) || InFootprint(ctx, at.x, at.z, 4f)) continue;
                    LampBench(ctx, mk, at, side * s); Ring(at, 2.5f); run = 0f; n++;
                }
            }
            Debug.Log($"The Alt Rock Moors: {n} roadside benches");
        }

        // ---------------------------------------------------------------- the village
        void Village(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var want = ctx.OnGround(VillageAt.x, VillageAt.y); var on = Nearest(roadPts, want);
            // the street is the loop road: the village faces across it
            var i0 = roadPts.IndexOf(on); var tan = roadPts[Mathf.Min(i0 + 3, roadPts.Count - 1)] - roadPts[Mathf.Max(i0 - 3, 0)]; tan.y = 0; tan.Normalize();
            var f = new PlaceFrame(on, Vector3.Cross(up, tan));   // x along the street, z across it
            // the village sits on mown green (its ground is levelled), bright against the moor
            var lawn = WorldMaterials.Solid(new Color(.5f, .66f, .36f)); mk.Prism(lawn, new Vector3(want.x, want.y - .5f, want.z), up, 43f, .56f, 40);
            Pub(ctx, mk, solid, new PlaceFrame(ctx.OnGround(f.P(-16f, 0, 13f).x, f.P(-16f, 0, 13f).z), -f.Fwd));
            Chapel(ctx, mk, solid, new PlaceFrame(ctx.OnGround(f.P(22f, 0, 16f).x, f.P(22f, 0, 16f).z), -f.Fwd));
            Shop(ctx, mk, solid, new PlaceFrame(ctx.OnGround(f.P(4f, 0, 11f).x, f.P(4f, 0, 11f).z), -f.Fwd));
            Green(ctx, mk, solid, new PlaceFrame(ctx.OnGround(f.P(0, 0, -20f).x, f.P(0, 0, -20f).z), f.Fwd));
            // a terrace of cottages down the street, smoke from the chimneys
            for (var k = 0; k < 4; k++) { var p = f.P(-34f + k * -8.5f + 8f, 0, -9.5f); Cottage(ctx, mk, solid, ctx.OnGround(p.x, p.z), f.Fwd); }
            for (var k = 0; k < 2; k++) { var p = f.P(38f + k * 9f, 0, -10f); Cottage(ctx, mk, solid, ctx.OnGround(p.x, p.z), f.Fwd); }
            // the bus stop, someone waiting; a postbox; lamps along the street
            var bs = f.P(16f, 0, -6f); bs = ctx.OnGround(bs.x, bs.z); BusShelter(ctx, mk, solid, bs, f.Fwd);
            Interact.Bench(plife.transform, bs + up * .56f - f.Fwd * .45f - f.Right * .9f, bs + up * .56f - f.Fwd * .45f + f.Right * .9f, f.Fwd);
            Folk(bs + up * .56f - f.Fwd * .45f + f.Right * .6f, f.Fwd, Resident.Pose.Sit, new[] { "It's due.", "It was due yesterday as well.", "You get used to waiting. It's quite nice, actually." }, null, null, .5f);
            var pb = f.P(9f, 0, 5.5f); pb = ctx.OnGround(pb.x, pb.z); mk.Prism(redPaint, pb - up * .2f, up, .28f, 1.5f, 10); mk.Prism(redPaint, pb + up * 1.3f, up, .32f, .15f, 10, .7f); mk.Box(dark, pb + up * 1.05f - f.Fwd * .27f, new Vector3(.25f, .05f, .04f), f.r); ArtistBits.PostCollider(ctx.Root, pb, 1.5f, .3f);
            foreach (var x in new[] { -40f, -12f, 14f, 40f }) { var lp = f.P(x, 0, (x > 0 ? 1 : -1) * (RoadW / 2f + 1.2f)); StreetLamp(ctx, mk, ctx.OnGround(lp.x, lp.z)); }
            // someone walking the street
            Folk(f.P(-30f, 0, 3.6f), tan, Resident.Pose.Walk, null, null, new[] { ctx.OnGround(f.P(-34f, 0, 3.6f).x, f.P(-34f, 0, 3.6f).z), ctx.OnGround(f.P(30f, 0, 3.6f).x, f.P(30f, 0, 3.6f).z) });
            var sign = f.P(-52f, 0, 4f); sign = ctx.OnGround(sign.x, sign.z); mk.Prism(white, sign - up * .2f, up, .06f, 1.7f, 6); mk.Box(white, sign + up * 1.5f, new Vector3(2.6f, .7f, .06f), Quaternion.LookRotation(tan)); ArtistBits.PostCollider(ctx.Root, sign, 1.6f, .1f);
            ArtistBits.Label(ctx.Root, "LITTLE STATIC\nplease drive quietly", sign + up * 1.55f - tan * .04f, -tan, .17f, new Color(.1f, .1f, .1f), TextAnchor.MiddleCenter);
            Shot(ctx, "the village street", f.P(-40f, 2f, -2f), f.P(0, 2f, 4f));
        }

        /// The pub: stone, slate roof, a hanging sign; inside a fire, a bar with stools, two tables with benches, a
        /// dartboard and darts to throw, the publican and two regulars.
        void Pub(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f)
        {
            var up = Vector3.up; const float W = 13f, D = 8.5f, H = 3.4f, T = .4f, Door = HumanScale.DoorWidth;
            var fo = new PlaceFrame(f.o + up * .25f, f.Fwd);
            mk.Box(copeStone, f.P(0, -.5f, 0), new Vector3(W + .6f, 1.5f, D + .6f), f.r);
            void Wall(Vector3 c, Vector3 s, Material m) { mk.Box(m, c, s, f.r); solid.Box(m, c, s, f.r); }
            mk.Box(wood, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(wood, fo.P(0, -.1f, 0), new Vector3(W, .2f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T), stone);
            foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f), stone);
            var seg = (W - Door) / 2f;
            foreach (var e in new[] { -1f, 1f })
            {
                var cx = e * (Door / 2f + seg / 2f);
                Wall(fo.P(cx, .5f, D / 2f - T / 2f), new Vector3(seg, 1f, T), stone); Wall(fo.P(cx, H - .55f, D / 2f - T / 2f), new Vector3(seg, 1.1f, T), stone);
                Wall(fo.P(cx - e * (seg / 2f - .5f), H / 2f, D / 2f - T / 2f), new Vector3(1f, H, T), stone); Wall(fo.P(cx + e * (seg / 2f - .5f), H / 2f, D / 2f - T / 2f), new Vector3(1f, H, T), stone);
                mk.Box(GlowM(Lamp, 1.5f), fo.P(cx, 1.65f, D / 2f - T / 2f), new Vector3(seg - 2f, 1.3f, .08f), f.r); solid.Box(stone, fo.P(cx, 1.65f, D / 2f - T / 2f), new Vector3(seg - 2f, 1.3f, .1f), f.r);
            }
            Wall(fo.P(0, H - .05f, D / 2f - T / 2f), new Vector3(Door, .1f, T), stone);
            mk.Box(cream, fo.P(0, H + .08f, 0), new Vector3(W - T, .12f, D - T), f.r);   // the ceiling
            mk.GableRoof(slate, fo.P(0, H + .15f, 0), new Vector3(D + .8f, 0, W + .8f), 2.6f, Quaternion.LookRotation(f.Right)); solid.Box(slate, fo.P(0, H + .5f, 0), new Vector3(W, .6f, D), f.r);
            var chim = fo.P(-W / 2f + 1f, H + 2.4f, -D / 2f + 1.2f); mk.Box(stone, chim, new Vector3(.9f, 2.6f, .9f), f.r); life.chimneys.Add(chim + up * 1.4f);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .8f, H, D - .8f));
            mk.Box(copeStone, f.P(0, .1f, D / 2f + .7f), new Vector3(Door + .8f, .3f, 1.2f), f.r); solid.Box(copeStone, f.P(0, .1f, D / 2f + .7f), new Vector3(Door + .8f, .3f, 1.2f), f.r);
            // the fire, in the end wall
            var fire = fo.P(-W / 2f + T + .3f, 0, -1f); mk.Box(dark, fire + up * .7f, new Vector3(.5f, 1.4f, 1.8f), f.r); mk.Box(copeStone, fire + up * 1.5f + f.Right * .1f, new Vector3(.8f, .15f, 2.2f), f.r);
            mk.Blob(GlowM(new Color(1f, .55f, .2f), 2.6f), fire + up * .4f + f.Right * .25f, new Vector3(.25f, .4f, .5f), ctx.Rng, .3f);
            var fl = ArtistBits.PointLight(ctx.Root, fire + up * .8f + f.Right * 1f, new Color(1f, .6f, .3f), 10f, 2f, true); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2f;
            foreach (var z in new[] { -2.2f, .2f }) { var ch = fire + f.Right * 1.8f + f.Fwd * (z + 1f); mk.Box(maroon, ch + up * .25f, new Vector3(.8f, .5f, .8f), f.r); mk.Box(maroon, ch + up * .7f + f.Right * .35f, new Vector3(.15f, .6f, .8f), f.r); Interact.Seat(plife.transform, ch + up * .52f, -f.Right, "Sit by the fire"); Col(ch + up * .5f + f.Right * .1f, new Vector3(.95f, 1f, .8f), f.r); }
            // the bar along the back, stools, the publican
            Wall(fo.P(2.5f, .55f, -D / 2f + 2.2f), new Vector3(6f, 1.1f, .6f), wood); mk.Box(dark, fo.P(2.5f, 1.13f, -D / 2f + 2.2f), new Vector3(6.2f, .06f, .75f), f.r);
            for (var k = 0; k < 4; k++) { var s = fo.P(.4f + k * 1.4f, 0, -D / 2f + 3.1f); mk.Prism(dark, s, up, .05f, .72f, 6); mk.Prism(maroon, s + up * .72f, up, .22f, .08f, 10); ArtistBits.PostCollider(ctx.Root, s, .75f, .23f); Interact.Seat(plife.transform, s + up * .82f, -f.Fwd, "Sit at the bar").seatHeight = .8f; }
            for (var k = 0; k < 5; k++) mk.Prism(k % 2 == 0 ? brass : WorldMaterials.Solid(new Color(.3f, .5f, .3f)), fo.P(.6f + k * 1f, 1.9f, -D / 2f + T + .15f), up, .06f, .3f, 6);
            mk.Box(wood, fo.P(2.5f, 1.85f, -D / 2f + T + .15f), new Vector3(5.6f, .06f, .3f), f.r);
            Folk(fo.P(2.5f, 0, -D / 2f + 1.3f), f.Fwd, Resident.Pose.Stand, new[] { "Usual? No, you're new. Usual anyway.", "We close at eleven. Or when the fire goes out. Never has yet.", "Quiz night's Thursday. Nobody's ever won." }, Look(Residents.Tool.Mug, true));
            Folk(fo.P(.4f + 1.4f, .82f, -D / 2f + 3.1f), -f.Fwd, Resident.Pose.Sit, new[] { "I've had this pint since March. Pacing myself.", "Don't mind me. I'm part of the furniture. They dust me." }, null, null, .8f);
            // two tables with benches by the windows
            foreach (var e in new[] { -1f, 1f })
            {
                var tc = fo.P(e * 3.8f + (e < 0 ? 1.2f : .4f), 0, D / 2f - 1.6f); mk.Box(wood, tc + up * .74f, new Vector3(1.6f, .07f, .8f), f.r); mk.Box(dark, tc + up * .37f, new Vector3(.12f, .74f, .12f), f.r); Col(tc + up * .4f, new Vector3(1.6f, .8f, .8f), f.r);
                foreach (var s in new[] { -1f, 1f }) { var bc = tc + f.Right * s * 1.15f; mk.Box(wood, bc + up * .45f, new Vector3(.4f, .07f, .9f), f.r); mk.Box(dark, bc + up * .2f, new Vector3(.3f, .4f, .7f), f.r); Interact.Seat(plife.transform, bc + up * .5f, -f.Right * s); }
            }
            Folk(fo.P(3.8f + .4f + 1.15f, .5f, D / 2f - 1.6f), -f.Right, Resident.Pose.Sit, new[] { "Weather's coming in. It's always coming in.", "This table's taken. By me. Has been for years." }, Look(Residents.Tool.Mug));
            // the dartboard, and darts to pick up and throw
            var board = fo.P(W / 2f - T - .03f, 1.7f, -1f); mk.Prism(dark, board, -f.Right, .32f, .05f, 16); mk.Prism(redPaint, board - f.Right * .05f, -f.Right, .2f, .01f, 16); mk.Prism(cream, board - f.Right * .06f, -f.Right, .1f, .01f, 16); mk.Prism(redPaint, board - f.Right * .07f, -f.Right, .03f, .01f, 8);
            for (var k = 0; k < 3; k++)
            {
                var dk = new MeshKit(); dk.Box(steel, new Vector3(0, 0, .05f), new Vector3(.015f, .015f, .12f), Quaternion.identity); dk.Box(k == 0 ? redPaint : k == 1 ? brass : white, new Vector3(0, 0, -.05f), new Vector3(.06f, .06f, .06f), Quaternion.identity);
                var go = dk.BuildSingle("Dart", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(W / 2f - 2.2f + k * .15f, 1.2f, -2.6f), Quaternion.LookRotation(f.Right));
                Interact.Carry(go, "a dart", .05f, f.o.y - 5f).GetComponent<BoxCollider>().size = new Vector3(.1f, .1f, .25f);
            }
            mk.Box(wood, fo.P(W / 2f - 2f, .55f, -2.6f), new Vector3(.9f, 1.1f, .5f), f.r); Col(fo.P(W / 2f - 2f, .55f, -2.6f), new Vector3(.9f, 1.1f, .5f), f.r);
            ArtistBits.PointLight(ctx.Root, fo.P(1.5f, H - .7f, .5f), Lamp, 11f, 1.3f, true);
            // the sign outside
            var sp = f.P(W / 2f + 1.6f, 0, D / 2f + 1.5f); mk.Prism(dark, sp - up * .2f, up, .08f, 3.6f, 6); mk.Box(dark, sp + up * 3.3f - f.Right * .6f, new Vector3(1.3f, .08f, .08f), f.r); ArtistBits.PostCollider(ctx.Root, sp, 3.4f, .1f);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .25f, .2f)), sp + up * 2.6f - f.Right * .7f, new Vector3(1.1f, 1.2f, .06f), f.r);
            ArtistBits.Label(ctx.Root, "THE LAMB\n& ANTENNA", sp + up * 2.65f - f.Right * .7f + f.Fwd * .04f, f.Fwd, .17f, new Color(.95f, .85f, .5f), TextAnchor.MiddleCenter);
            halos.Add(f.P(0, 2.6f, D / 2f + .6f), 1.6f, Lamp, .5f); mk.Blob(GlowM(Lamp, 2f), f.P(0, 3f, D / 2f + .3f), Vector3.one * .15f, ctx.Rng, 0f);
            LampBench(ctx, mk, f.P(-4.5f, 0, D / 2f + 1.4f), f.Fwd, false);
            Ring(f.o, 9f);
            Shot(ctx, "the pub", f.P(5f, 1.8f, D / 2f + 13f), fo.P(0, 2f, 0));
            Shot(ctx, "inside the pub", fo.P(4.6f, 1.6f, 2.6f), fo.P(-4f, 1.1f, -1.6f));
        }

        void Chapel(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f)
        {
            var up = Vector3.up; const float W = 6f, D = 10f, H = 4.2f;
            mk.Box(stone, f.P(0, H / 2f - .4f, 0), new Vector3(W, H + .8f, D), f.r); solid.Box(stone, f.P(0, H / 2f, 0), new Vector3(W, H, D), f.r);
            mk.GableRoof(slate, f.P(0, H, 0), new Vector3(W + .6f, 0, D + .6f), 2.8f, f.r);
            mk.Box(dark, f.P(0, 1.2f, D / 2f + .02f), new Vector3(1.3f, 2.4f, .05f), f.r); mk.Prism(GlowM(new Color(.6f, .75f, 1f), 1.4f), f.P(0, 3.4f, D / 2f + .02f), f.Fwd, .5f, .04f, 12);
            foreach (var z in new[] { -2.5f, 0f, 2.5f }) foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(new Color(1f, .8f, .45f), 1.2f), f.P(e * (W / 2f + .02f), 2.2f, z), new Vector3(.04f, 1.6f, .6f), f.r);
            // the bell-cote: a little arch on the front gable with a bell to ring
            var bc = f.P(0, H + 2.9f, D / 2f - .3f); foreach (var e in new[] { -.5f, .5f }) mk.Box(stone, bc + f.Right * e + up * .5f, new Vector3(.25f, 1.4f, .5f), f.r); mk.Box(stone, bc + up * 1.3f, new Vector3(1.3f, .25f, .6f), f.r);
            var bell = new MeshKit(); bell.Prism(brass, new Vector3(0, -.55f, 0), Vector3.up, .32f, .5f, 10, .5f); bell.Box(dark, new Vector3(0, -.65f, 0), new Vector3(.06f, .3f, .06f), Quaternion.identity);
            var bgo = bell.BuildSingle("Chapel bell", plife.transform, false); bgo.transform.SetPositionAndRotation(bc + up * 1.15f, f.r);
            // its rope hangs by the door
            var rope = f.P(1.3f, 0, D / 2f + .25f); mk.Box(WorldMaterials.Solid(new Color(.8f, .72f, .5f)), rope + up * 3.6f, new Vector3(.04f, 7.2f, .04f), f.r);
            var pull = new GameObject("Bell rope"); pull.transform.SetParent(plife.transform, false); pull.transform.position = rope + up * 1.3f;
            var toy = Interact.Toy(pull, Toy.Kind.Custom, "Ring the bell", new Vector3(.5f, 1.6f, .5f)); toy.onUse = t => plife.Ring(bgo.transform);
            Folk(f.P(-1.6f, 0, D / 2f + 1.6f), f.Fwd, Resident.Pose.Stand, new[] { "The bell's for anyone. Give it a pull.", "Congregation of four. Five if it rains.", "We pray for good weather. Then for a sense of humour." }, new Residents.Look { top = new Color(.1f, .1f, .12f), bottom = new Color(.1f, .1f, .12f), hair = new Color(.75f, .75f, .75f), skin = new Color(.9f, .72f, .6f) });
            // a few leaning headstones, a yew
            for (var k = 0; k < 6; k++) { var p = f.P(-W / 2f - 2.5f - (k % 2) * 1.8f, 0, -3.5f + k * 1.5f); p = ctx.OnGround(p.x, p.z); var r = f.r * Quaternion.Euler(0, 90f, (k * 37 % 13) - 6f); mk.Box(copeStone, p + up * .45f, new Vector3(.6f, .9f, .12f), r); Col(p + up * .45f, new Vector3(.6f, .9f, .2f), r); }
            var yew = f.P(W / 2f + 3f, 0, -2f); yew = ctx.OnGround(yew.x, yew.z); mk.Prism(wood, yew - up * .2f, up, .3f, 2f, 6); soft.Blob(WorldMaterials.Solid(new Color(.14f, .22f, .14f)), yew + up * 3.2f, new Vector3(1.8f, 2.4f, 1.8f), ctx.Rng, .25f); ArtistBits.PostCollider(ctx.Root, yew, 2.4f, .35f);
            Ring(f.o, 8f);
            Shot(ctx, "the chapel", f.P(-5f, 1.8f, D / 2f + 12f), f.P(0, 4f, 0));
        }

        void Shop(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f)
        {
            var up = Vector3.up; const float W = 7f, D = 6f, H = 3.2f;
            mk.Box(stone, f.P(0, H / 2f - .4f, 0), new Vector3(W, H + .8f, D), f.r); solid.Box(stone, f.P(0, H / 2f, 0), new Vector3(W, H, D), f.r);
            mk.GableRoof(slate, f.P(0, H, 0), new Vector3(D + .6f, 0, W + .6f), 2f, Quaternion.LookRotation(f.Right));
            mk.Box(GlowM(new Color(1f, .9f, .65f), 1.6f), f.P(-1.2f, 1.5f, D / 2f + .02f), new Vector3(3.2f, 1.6f, .05f), f.r); mk.Box(dark, f.P(2.2f, 1.1f, D / 2f + .02f), new Vector3(1f, 2.2f, .05f), f.r);
            mk.Box(WorldMaterials.Solid(new Color(.2f, .4f, .3f)), f.P(0, 2.75f, D / 2f + .06f), new Vector3(W - .4f, .6f, .1f), f.r);
            ArtistBits.Label(ctx.Root, "SHOP (OPEN-ISH)", f.P(0, 2.75f, D / 2f + .13f), f.Fwd, .28f, new Color(.95f, .9f, .7f), TextAnchor.MiddleCenter);
            // crates of veg outside, a noticeboard
            for (var k = 0; k < 3; k++) { var c = f.P(-2.6f + k * 1.1f, .25f, D / 2f + .8f); mk.Box(wood, c, new Vector3(.9f, .5f, .6f), f.r); for (var v = 0; v < 4; v++) mk.Blob(WorldMaterials.Solid(new[] { new Color(.9f, .5f, .1f), new Color(.5f, .7f, .2f), new Color(.8f, .15f, .15f) }[k]), c + up * .3f + f.Right * (-.25f + v * .17f), Vector3.one * .1f, ctx.Rng, 0f); }
            Col(f.P(-1.5f, .3f, D / 2f + .8f), new Vector3(3.2f, .7f, .7f), f.r);
            var nb = f.P(W / 2f + 1.6f, 0, D / 2f + .5f); nb = ctx.OnGround(nb.x, nb.z); foreach (var e in new[] { -.7f, .7f }) mk.Box(wood, nb + f.Right * e + up * .9f, new Vector3(.1f, 1.8f, .1f), f.r); mk.Box(WorldMaterials.Solid(new Color(.3f, .45f, .35f)), nb + up * 1.35f, new Vector3(1.5f, 1f, .06f), f.r); Col(nb + up * .9f, new Vector3(1.6f, 1.8f, .15f), f.r);
            ArtistBits.Label(ctx.Root, "LOST: one umbrella, grey.\nFOUND: one umbrella, grey.\nNot the same one.\n\nChoir practice cancelled\n(choir on holiday, both of them)", nb + up * 1.38f + f.Fwd * .04f, f.Fwd, .07f, Ink, TextAnchor.MiddleCenter);
            Folk(f.P(2.2f, 0, D / 2f + 1.1f), f.Fwd, Resident.Pose.Lean, new[] { "We've got milk, bread, and one of everything else.", "Paper's yesterday's. News is the same anyway." }, Look(Residents.Tool.None, true));
            Ring(f.o, 6f);
        }

        /// The green: an old oak with a bench round it, a pond with ducks, a football to kick about, a kid waiting for a game.
        void Green(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f)
        {
            var up = Vector3.up; var g = f.o;
            mk.Prism(wood, g - up * .3f, up, .8f, 5f, 8, .7f); ArtistBits.PostCollider(ctx.Root, g, 5f, .9f);
            for (var k = 0; k < 7; k++) soft.Blob(WorldMaterials.Solid(k % 2 == 0 ? new Color(.25f, .42f, .22f) : new Color(.32f, .5f, .25f)), g + up * (6f + (k % 3) * 1.1f) + new Vector3(Mathf.Cos(k * .9f), 0, Mathf.Sin(k * .9f)) * 2.6f, Vector3.one * 2.6f, ctx.Rng, .25f);
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var p = g + d * 1.6f; var rot = Quaternion.LookRotation(d);
                mk.Box(wood, p + up * .45f, new Vector3(1.3f, .07f, .45f), rot); Interact.Seat(plife.transform, p + up * .5f, d, "Sit under the oak");
            }
            Col(g + up * .3f, new Vector3(3.6f, .6f, 3.6f), Quaternion.identity);
            // the pond
            var pond = f.P(-13f, 0, -2f); pond = ctx.OnGround(pond.x, pond.z); mk.Prism(copeStone, pond - up * .3f, up, 4.4f, .45f, 16); mk.Prism(WorldMaterials.Glow(new Color(.3f, .5f, .7f), .5f), pond + up * .16f, up, 4f, .02f, 16);
            for (var k = 0; k < 4; k++) { var d = pond + new Vector3(Mathf.Cos(k * 1.7f), 0, Mathf.Sin(k * 1.7f)) * (1f + k * .6f) + up * .22f; mk.Blob(k == 0 ? white : WorldMaterials.Solid(new Color(.4f, .3f, .22f)), d + up * .08f, new Vector3(.15f, .12f, .22f), ctx.Rng, 0f); mk.Blob(WorldMaterials.Solid(new Color(.15f, .35f, .2f)), d + up * .22f + Vector3.forward * .12f, Vector3.one * .07f, ctx.Rng, 0f); }
            solid.Prism(copeStone, pond - up * .3f, up, 4.4f, .7f, 16);
            LampBench(ctx, mk, pond + f.Right * 6.2f * 0f + f.Fwd * -6f, f.Fwd);
            // the football, and jumpers for goalposts
            var pitch = f.P(12f, 0, -3f); pitch = ctx.OnGround(pitch.x, pitch.z);
            var ball = new MeshKit(); ball.Blob(white, Vector3.zero, Vector3.one * .22f, prng, 0f); for (var k = 0; k < 5; k++) ball.Box(dark, Quaternion.Euler(k * 72f, k * 50f, 0) * Vector3.up * .2f, Vector3.one * .1f, Quaternion.Euler(k * 72f, k * 50f, 0));
            var bgo = ball.BuildSingle("Football", plife.transform, false); bgo.transform.position = pitch + up * .3f;
            bgo.AddComponent<SphereCollider>().radius = .22f; var rb = bgo.AddComponent<Rigidbody>(); rb.mass = .6f; rb.linearDamping = .4f; rb.angularDamping = .5f;
            var c = bgo.AddComponent<Carryable>(); c.thing = "the football"; c.home = bgo.transform.position; c.lowest = g.y - 6f;
            foreach (var e in new[] { -2f, 2f }) mk.Blob(e < 0 ? redPaint : WorldMaterials.Solid(new Color(.2f, .3f, .6f)), ctx.OnGround(pitch.x + f.Right.x * 8f + f.Fwd.x * e, pitch.z + f.Right.z * 8f + f.Fwd.z * e) + up * .12f, new Vector3(.4f, .15f, .3f), ctx.Rng, .2f);
            Folk(pitch + f.Right * 6.5f, -f.Right, Resident.Pose.Wave, new[] { "Jumpers for goalposts. Go on, have a shot.", "Next goal wins. It's been next goal wins since lunch." }, new Residents.Look { top = new Color(.85f, .15f, .15f), bottom = new Color(.1f, .1f, .12f), height = 1.35f, skin = new Color(.88f, .68f, .52f), hair = new Color(.3f, .2f, .12f) });
            ArtistBits.PointLight(ctx.Root, g + up * 4f + f.Fwd * 4f, Lamp, 14f, 1f);
            Ring(g, 5f); Ring(pond, 6f);
            Shot(ctx, "the village green", f.P(8f, 2f, -14f), g + up * 3f);
        }

        // ---------------------------------------------------------------- the sheep farm
        void SheepAt(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face)
        {
            var up = Vector3.up;
            mk.Blob(wool, p + up * .7f, new Vector3(.55f, .45f, .8f), ctx.Rng, .2f); mk.Blob(dark, p + up * .85f + face * .7f, new Vector3(.18f, .2f, .25f), ctx.Rng, 0f);
            foreach (var s in new[] { -.2f, .2f }) foreach (var z in new[] { -.4f, .4f }) mk.Box(dark, p + up * .25f + Vector3.Cross(up, face) * s + face * z, new Vector3(.08f, .5f, .08f), Quaternion.identity);
            solid.Box(wool, p + up * .55f, new Vector3(.8f, 1.1f, 1.6f), Quaternion.LookRotation(face));
        }

        void Farm(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, FarmAt, 16f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            mk.Prism(WorldMaterials.Solid(new Color(.5f, .66f, .36f)), new Vector3(FarmAt.x, ctx.Ground(FarmAt.x, FarmAt.y) - .5f, FarmAt.y), up, 17f, .56f, 28);   // the farmyard's green
            Cottage(ctx, mk, solid, ctx.OnGround(f.P(-9f, 0, -4f).x, f.P(-9f, 0, -4f).z), f.Fwd);
            // the stone barn, its door open and dark, a lamp over it
            var b = f.P(8f, 0, -8f); b = ctx.OnGround(b.x, b.z); const float W = 9f, D = 12f, H = 4.5f;
            mk.Box(stone, b + up * (H / 2f - .5f), new Vector3(W, H + 1f, D), f.r); solid.Box(stone, b + up * H / 2f, new Vector3(W, H, D), f.r); mk.GableRoof(slate, b + up * H, new Vector3(W + .6f, 0, D + .6f), 2.6f, f.r);
            mk.Box(dark, b + up * 1.7f + f.Fwd * (D / 2f + .02f), new Vector3(3.4f, 3.4f, .05f), f.r); mk.Blob(GlowM(Lamp, 2f), b + up * 3.8f + f.Fwd * (D / 2f + .2f), Vector3.one * .16f, ctx.Rng, 0f); halos.Add(b + up * 3.7f + f.Fwd * (D / 2f + .4f), 1.4f, Lamp, .6f);
            ArtistBits.PointLight(ctx.Root, b + up * 3.4f + f.Fwd * (D / 2f + 2f), Lamp, 13f, 1.3f);
            // the fold: a walled pen full of sheep, a gate you can open
            var fc = f.P(2f, 0, 14f); fc = ctx.OnGround(fc.x, fc.z); const float FW = 20f, FD = 14f;
            Field(ctx, mk, solid, fc, -f.Fwd, FW, FD); fields.Add((fc, -f.Fwd, FW, FD));
            for (var k = 0; k < 9; k++) { var p = fc + f.Right * (-7f + (k % 5) * 3.4f) + f.Fwd * (-3.5f + (k / 5) * 4f + (k % 2)); SheepAt(ctx, mk, solid, ctx.OnGround(p.x, p.z), Quaternion.Euler(0, k * 77f, 0) * Vector3.forward); }
            var gate = new MeshKit(); foreach (var y in new[] { .3f, .65f, 1f }) gate.Box(wood, new Vector3(1.6f, y, 0), new Vector3(3.2f, .1f, .06f), Quaternion.identity); foreach (var x in new[] { .05f, 1.6f, 3.15f }) gate.Box(wood, new Vector3(x, .65f, 0), new Vector3(.1f, 1f, .07f), Quaternion.identity);
            var gp = fc - f.Fwd * (FD / 2f) - f.Right * 1.6f; gp = ctx.OnGround(gp.x, gp.z);
            var ggo = gate.BuildSingle("Fold gate", plife.transform, false); ggo.transform.SetPositionAndRotation(gp, f.r);
            var gt = Interact.Toy(ggo, Toy.Kind.Swing, "Open the gate", null); var gbc = ggo.AddComponent<BoxCollider>(); gbc.center = new Vector3(1.6f, .65f, 0); gbc.size = new Vector3(3.2f, 1.1f, .12f); gt.axis = Vector3.up; gt.amount = 95f; gt.seconds = 1f;
            // the shepherd and the dog
            var sp = f.P(-2f, 0, 5f); Folk(ctx.OnGround(sp.x, sp.z), f.Fwd, Resident.Pose.Lean, new[] { "Counted them twice. Different number each time. Fell asleep both times.", "That one's called Kevin. They're all called Kevin.", "The dog does the work. I do the standing." }, Look(Residents.Tool.Rake));
            var dg = f.P(-.6f, 0, 5.6f); dg = ctx.OnGround(dg.x, dg.z); mk.Box(dark, dg + up * .4f, new Vector3(.28f, .3f, .8f), f.r); mk.Box(white, dg + up * .38f + f.Fwd * .3f, new Vector3(.3f, .28f, .25f), f.r); mk.Box(dark, dg + up * .62f + f.Fwd * .5f, new Vector3(.22f, .22f, .28f), f.r); foreach (var e in new[] { -.1f, .1f }) foreach (var z in new[] { -.3f, .3f }) mk.Box(dark, dg + up * .13f + f.Right * e + f.Fwd * z, new Vector3(.07f, .26f, .07f), f.r);
            Col(dg + up * .35f, new Vector3(.35f, .7f, 1f), f.r);
            LampBench(ctx, mk, f.P(-9f, 0, 1.5f), f.Fwd);
            Lane(ctx, mk, f.P(0, 0, 3f)); Ring(g, 12f);
            Shot(ctx, "the sheep farm", f.P(-12f, 2.2f, 24f), f.P(2f, 1.5f, 2f));
        }

        // ---------------------------------------------------------------- walled fields along the roads; tors on the tops
        /// Walled fields either side of the main roads, gates to the road, sheep in some: the bright in-bye land.
        void RoadFields(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var n = 0; var flock = 0;
            foreach (var road in mainRoads)
            {
                var run = 20f; var k = 0;
                for (var i = 1; i < road.Count; i++)
                {
                    run += Vector3.Distance(road[i], road[i - 1]); if (run < 44f) continue; run = 0f; k++;
                    var d = road[i] - road[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d);
                    foreach (var s in new[] { -1f, 1f })
                    {
                        if ((k + (s > 0 ? 1 : 0)) % 3 == 0) continue;   // gaps, so it isn't a corridor
                        const float W = 34f, D = 24f; var c = road[i] + side * s * (RoadW / 2f + 3.5f + D / 2f); var ok = true; float lo = float.MaxValue, hi = float.MinValue;
                        foreach (var (x, z) in new[] { (0f, 0f), (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 1f), (0f, -1f), (1f, 0f), (-1f, 0f) })
                        {
                            var p = c + d * x * W / 2f + side * z * D / 2f;
                            if (!ctx.IsLand(p.x, p.z, 4f) || InFootprint(ctx, p.x, p.z, 6f) || InNoGrass(p.x, p.z, 3f) || (z * s > -.5f && NearPath(p.x, p.z, 4f))) { ok = false; break; }
                            var h = ctx.Ground(p.x, p.z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                        }
                        if (!ok || hi - lo > 6f) continue;
                        foreach (var (fc, ff, fw, fd) in fields) if ((fc - c).sqrMagnitude < 40f * 40f) ok = false;
                        if (!ok) continue;
                        c = ctx.OnGround(c.x, c.z); Field(ctx, mk, solid, c, -side * s, W, D); fields.Add((c, -side * s, W, D)); n++;
                        if (n % 2 == 0) { for (var q = 0; q < 4; q++) { var p = c + d * (-10f + q * 6.5f) + side * ((q % 2) * 6f - 3f); SheepAt(ctx, mk, solid, ctx.OnGround(p.x, p.z), Quaternion.Euler(0, q * 95f + n * 40f, 0) * Vector3.forward); } flock++; }
                        else if (n % 5 == 1) { var t = c + d * (W / 2f - 3f) + side * s * (D / 2f - 3f); t = ctx.OnGround(t.x, t.z); var lean = (Quaternion.Euler(0, 40f, 0) * Vector3.forward + up * 1.6f).normalized; mk.Prism(wood, t - up * .2f, lean, .25f, 3.2f, 6, .6f); solid.Prism(wood, t - up * .2f, lean, .25f, 3.2f, 6, .6f); for (var q = 0; q < 4; q++) soft.Blob(WorldMaterials.Solid(new Color(.32f, .4f, .26f)), t + lean * 3f + Quaternion.Euler(0, q * 90f, 0) * Vector3.forward, new Vector3(1.4f, .9f, 1.2f), ctx.Rng, .35f); }
                    }
                }
            }
            Debug.Log($"The Alt Rock Moors: {n} walled fields along the roads, {flock} with sheep");
        }

        bool InField(float x, float z)
        {
            foreach (var (c, f, w, d) in fields)
            {
                var v = new Vector3(x - c.x, 0, z - c.z); var s = Vector3.Cross(Vector3.up, f);
                if (Mathf.Abs(Vector3.Dot(v, s)) < w / 2f - .6f && Mathf.Abs(Vector3.Dot(v, f)) < d / 2f - .6f) return true;
            }
            return false;
        }

        /// A pale tor crowning each real hilltop (the highest point of each 90 m stretch that stands above the land round it).
        void HilltopTors(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var count = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += 90f)
                for (var z = ctx.MinZ; z < ctx.MaxZ; z += 90f)
                {
                    float bx = 0, bz = 0, bh = -1f;
                    for (var u = 0f; u < 90f; u += 8f) for (var v = 0f; v < 90f; v += 8f) { var h = ctx.Ground(x + u, z + v); if (h > bh && ctx.IsLand(x + u, z + v, 8f)) { bh = h; bx = x + u; bz = z + v; } }
                    if (bh < 0f) continue;
                    var around = Mathf.Max(Mathf.Max(ctx.Ground(bx + 36f, bz), ctx.Ground(bx - 36f, bz)), Mathf.Max(ctx.Ground(bx, bz + 36f), ctx.Ground(bx, bz - 36f)));
                    if (bh - around < 2.5f || !Open(ctx, bx, bz, 5f) || InField(bx, bz) || (hasHill && Vector2.Distance(new Vector2(bx, bz), hill2) < 40f)) continue;
                    var p = ctx.OnGround(bx, bz); for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; p.y = Mathf.Min(p.y, ctx.Ground(bx + Mathf.Cos(a) * 3.5f, bz + Mathf.Sin(a) * 3.5f)); }
                    var n = 3 + count % 3;
                    for (var k = 0; k < n; k++)
                    {
                        var bp = p + new Vector3(ctx.Range(-2f, 2f), .8f + k * .7f, ctx.Range(-2f, 2f)); var bs = new Vector3(ctx.Range(2f, 3.6f), ctx.Range(1f, 1.8f), ctx.Range(1.8f, 3f)); var br = Quaternion.Euler(0, ctx.Range(0, 90f), ctx.Range(-6f, 6f));
                        mk.Box(paleStone, bp, bs, br); solid.Box(paleStone, bp, bs, br);
                    }
                    Ring(p, 4f); count++;
                }
            Debug.Log($"The Alt Rock Moors: {count} hilltop tors");
        }
    }
}
