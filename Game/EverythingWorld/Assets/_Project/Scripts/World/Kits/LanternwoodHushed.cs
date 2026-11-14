using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Lanternwood made to settle into (the build-land standard, done gently: this land was already laid out with
    /// care). You can sit on every bench, pew and fire log. Five more hushed places, each down its own lantern
    /// path, mostly in the quieter east: a little lending library with a stove, armchairs and a cat; a greenhouse
    /// glowing warm in the snow; a garden of ice lanterns to light one by one; a hot spring to sit in; a stargazing
    /// deck with two long chairs and a telescope. On the lake a fishing hole with a stool, and someone skating slow
    /// circles. Piles of snowballs to throw, wind chimes to ring, and a few quiet people who keep the place:
    /// the candle-keeper at the chapel, someone by the fire, the librarian.
    public partial class LanternwoodKit
    {
        System.Random hrng; Vector3 fireCircleAt; bool hasFireCircle;
        static readonly Color[] Coats = { new Color(.85f, .8f, .7f), new Color(.2f, .26f, .4f), new Color(.6f, .28f, .2f), new Color(.22f, .36f, .3f), new Color(.5f, .45f, .55f), new Color(.75f, .6f, .3f) };
        static readonly Color[] Legs = { new Color(.2f, .22f, .28f), new Color(.3f, .26f, .22f), new Color(.25f, .25f, .27f) };
        static readonly Residents.Hat[] WinterHats = { Residents.Hat.Beanie, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Bucket };
        static readonly Color[] HatC = { new Color(.7f, .25f, .2f), new Color(.85f, .8f, .7f), new Color(.2f, .3f, .45f), new Color(.8f, .65f, .3f) };
        static readonly string[] Quiet = { "Mind the ice by the creek.", "Quiet tonight. It's always quiet. That's why we stay.", "The lanterns light themselves. Nobody's ever caught them at it." };

        Residents.Look WinterLook(Residents.Tool tool = Residents.Tool.None) { var l = Residents.Random(hrng, Coats, Legs, WinterHats, HatC); l.tool = tool; return l; }
        Resident Soul(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(life.transform, feet, face, look ?? WinterLook(), pose, hrng, lines ?? Quiet, route, seatH);

        void HCol(LandContext ctx, Vector3 c, Vector3 size, Quaternion rot) => BoxCol(ctx.Root, c, size, rot);

        void SmallLantern(LandContext ctx, MeshKit mk, Vector3 at, bool light = true)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z);
            mk.Prism(darkWood, g - up * .2f, up, .06f, 2.3f, 5); ArtistBits.PostCollider(ctx.Root, g, 2.2f, .1f);
            mk.Box(WorldMaterials.Glow(LanternGlass, 1.6f), g + up * 2.2f, new Vector3(.2f, .28f, .2f), Quaternion.identity); halos.Add(g + up * 2.2f, 1.3f, LanternGlass, .8f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 2f, new Color(1f, .7f, .42f), 9f, 1.2f);
        }

        void Snowballs(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z);
            mk.Blob(snowTop, g + up * .12f, new Vector3(.5f, .2f, .5f), ctx.Rng, .2f);
            for (var k = 0; k < 5; k++)
            {
                var sb = new MeshKit(); sb.Blob(snowTop, Vector3.zero, Vector3.one * .13f, hrng, .12f);
                var go = sb.BuildSingle("Snowball", life.transform, false); go.transform.position = g + up * (.36f + (k / 3) * .2f) + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * (k < 3 ? .18f : .06f);
                var c = Interact.Carry(go, "a snowball", .2f, g.y - 8f); c.GetComponent<Rigidbody>().isKinematic = true;   // they sit still in the pile until you take one
            }
        }

        /// Everything added by the standard: called last in Dress, so nothing that was already there moves.
        void Hushed(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 chapelFront)
        {
            hrng = new System.Random(2020); var up = Vector3.up;
            void Place(Vector2 near, float clearR, string sign, string mark, Color col, System.Action<Vector3, Vector3> build, bool onIce = false)
            {
                var c = PlaceSpot(ctx, ctx.OnGround(near.x, near.y), 0f, clearR, onIce); if (float.IsInfinity(c.x)) { Debug.Log("Lanternwood: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clearR, sign, mark, col, onIce));
            }
            Place(new Vector2(8330, 6400), 7f, "THE LENDING LIBRARY", "the lending library", Warm, (c, f) => Library(ctx, mk, solid, c, f));
            Place(new Vector2(8420, 6290), 5f, "THE GREENHOUSE", "the greenhouse", Gold, (c, f) => Greenhouse(ctx, mk, solid, c, f));
            Place(new Vector2(8280, 6520), 6f, "ICE LANTERNS", "the ice lantern garden", Frost, (c, f) => IceLanterns(ctx, mk, solid, c, f));
            Place(new Vector2(8250, 6160), 6f, "THE HOT SPRING", "the hot spring", Frost, (c, f) => HotSpring(ctx, mk, solid, c, f));
            Place(new Vector2(7720, 6330), 5f, "THE STAR DECK", "the star deck", Frost, (c, f) => StarDeck(ctx, mk, solid, c, f));
            Place(Vector2.zero, 3f, "GONE FISHING", "the fishing hole", Warm, (c, f) => FishingHole(ctx, mk, solid, c, f), true);

            // the people who keep the place
            var outDir = FaceTo(ctx.Plaza, chapelFront); var side = Vector3.Cross(up, outDir);
            Soul(ctx.OnGround(chapelFront.x + side.x * 2.6f + outDir.x * .6f, chapelFront.z + side.z * 2.6f + outDir.z * .6f), outDir, Resident.Pose.Stand,
                new[] { "I keep the candles lit. Somebody should.", "Sit as long as you like. Nobody's counting.", "It's warmer inside. Not much. Enough." });
            var sm = ctx.OnGround(chapelFront.x - side.x * 6f + outDir.x * 5f, chapelFront.z - side.z * 6f + outDir.z * 5f);
            if (ctx.IsLand(sm.x, sm.z, 3f) && !NearClearPath(sm))
            {
                mk.Blob(snowTop, sm + up * .45f, Vector3.one * .55f, ctx.Rng, .1f); mk.Blob(snowTop, sm + up * 1.15f, Vector3.one * .38f, ctx.Rng, .1f); ArtistBits.PostCollider(ctx.Root, sm, 1.5f, .55f);
                Soul(sm + side * 1.1f, -side, Resident.Pose.Work, new[] { "He'll be somewhere else tomorrow. They always are.", "Needs a head. They all need a head, eventually." }).workRate = .8f;
                Snowballs(ctx, mk, sm + outDir * 1.6f);
            }
            if (hasFireCircle)
            {
                var a = .4f; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var seat = fireCircleAt + dir * 4.2f; seat.y = ctx.Ground(seat.x, seat.z) + .58f;
                Soul(seat, -dir, Resident.Pose.Sit, new[] { "Room on the log. There's always room on the log.", "I don't say much. The fire does the talking." }, WinterLook(Residents.Tool.Mug), null, .55f);
                Snowballs(ctx, mk, fireCircleAt - dir * 7f);
            }
            if (hasLake)
            {
                var route = new Vector3[12]; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; var r = LakeEdge(a) * .5f; route[k] = new Vector3(lakeC.x + Mathf.Cos(a) * r, lakeLevel + .03f, lakeC.z + Mathf.Sin(a) * r); }
                var sk = Soul(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, new[] { "The ice sings when it's cold enough. Listen.", "Round and round. It's the only way I think straight." }, null, route); sk.speed = 2.6f;
            }
            // wind chimes along the paths, to ring as you pass
            var hung = 0; Vector3 last = Vector3.positiveInfinity;
            for (var i = 40; i < pathPoints.Count && hung < 4; i += 55)
            {
                var p = pathPoints[i]; if (!float.IsInfinity(last.x) && Vector3.Distance(p, last) < 90f) continue;
                var at = ctx.OnGround(p.x + 1.8f, p.z + 1.2f); if (!ctx.IsLand(at.x, at.z, 2f) || OnLake(at, 2f)) continue;
                Chimes(ctx, mk, at); last = p; hung++;
            }
        }

        bool NearClearPath(Vector3 p) { foreach (var q in pathPoints) if ((q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z) < 4f) return true; return false; }

        void Chimes(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; mk.Prism(darkWood, g - up * .2f, up, .06f, 2.6f, 5); mk.Box(darkWood, g + up * 2.35f + Vector3.right * .35f, new Vector3(.8f, .05f, .05f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, g, 2.4f, .1f);
            var ch = new MeshKit(); var brass = WorldMaterials.Solid(new Color(.85f, .75f, .5f), .85f);
            for (var k = 0; k < 5; k++) { var x = (k - 2) * .09f; var len = .35f + (k * 7 % 5) * .07f; ch.Box(brass, new Vector3(x, -.12f - len / 2f, 0), new Vector3(.03f, len, .03f), Quaternion.identity); }
            ch.Box(darkWood, new Vector3(0, -.05f, 0), new Vector3(.5f, .03f, .06f), Quaternion.identity);
            var go = ch.BuildSingle("Wind chimes", life.transform, false); go.transform.position = g + up * 2.3f + Vector3.right * .6f;
            var t = Interact.Toy(go, Toy.Kind.Custom, "Ring the chimes", new Vector3(.8f, 1.6f, .8f)); go.GetComponent<BoxCollider>().center = new Vector3(0, -.8f, 0);
            var sway = go.AddComponent<ChimeSway>(); t.onUse = _ => sway.Ring();
        }

        // a little lending library: a stove, two armchairs, shelves of books, a cat
        void Library(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var f = new PlaceFrame(c - face * 1.5f, face); const float W = 7f, D = 6f, H = 3.1f, T = .35f, Door = HumanScale.DoorWidth;
            float lo = float.MaxValue, hi = float.MinValue; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 0f) }) { var h = ctx.Ground(f.P(x * W / 2f, 0, z * D / 2f).x, f.P(x * W / 2f, 0, z * D / 2f).z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            var fo = new PlaceFrame(new Vector3(f.o.x, hi + .2f, f.o.z), face);
            mk.Box(stone, new Vector3(f.o.x, (lo + hi) / 2f - .1f, f.o.z), new Vector3(W + .3f, hi - lo + .7f, D + .3f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(logs, p, s, f.r); solid.Box(logs, p, s, f.r); }
            mk.Box(planks, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(planks, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T));
            foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T));
            mk.Box(planks, fo.P(0, H + .06f, 0), new Vector3(W - T, .12f, D - T), f.r);
            mk.GableRoof(darkWood, fo.P(0, H + .1f, 0), new Vector3(W + .8f, 0, D + .8f), 1.9f, f.r); mk.GableRoof(snowTop, fo.P(0, H + .22f, 0), new Vector3(W + .5f, 0, D + .5f), 1.9f, f.r); solid.Box(darkWood, fo.P(0, H + .4f, 0), new Vector3(W, .5f, D), f.r);
            foreach (var e in new[] { -1f, 1f }) mk.Box(WorldMaterials.Glow(LanternGlass, 1.3f), fo.P(e * (W / 2f + .01f), 1.7f, .4f), new Vector3(.04f, 1f, 1.3f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            var st = ctx.OnGround(f.P(0, 0, D / 2f + .7f).x, f.P(0, 0, D / 2f + .7f).z); mk.Box(stone, new Vector3(st.x, (st.y + fo.o.y) / 2f - .1f, st.z), new Vector3(Door + .6f, fo.o.y - st.y + .2f, 1.3f), f.r); solid.Box(stone, new Vector3(st.x, (st.y + fo.o.y) / 2f - .1f, st.z), new Vector3(Door + .6f, fo.o.y - st.y + .2f, 1.3f), f.r);
            // the stove in the back corner, glowing
            var sv = fo.P(-W / 2f + 1.1f, 0, -D / 2f + 1.1f); mk.Box(iron, sv + up * .55f, new Vector3(.8f, 1.1f, .8f), f.r); mk.Box(iron, sv + up * 2.1f, new Vector3(.2f, 2f, .2f), f.r); mk.Box(WorldMaterials.Glow(Ember, 2.4f), sv + up * .5f + f.Fwd * .41f, new Vector3(.4f, .3f, .02f), f.r);
            HCol(ctx, sv + up * .6f, new Vector3(.8f, 1.2f, .8f), f.r); var fl = ArtistBits.PointLight(ctx.Root, sv + up * .9f + f.Fwd * .9f + f.Right * .5f, new Color(1f, .6f, .35f), 8f, 1.7f, true); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.7f;
            // shelves of books along the back and one side
            Color[] books = { new Color(.6f, .2f, .18f), new Color(.2f, .35f, .5f), new Color(.75f, .65f, .35f), new Color(.25f, .42f, .3f), new Color(.45f, .3f, .5f), new Color(.85f, .8f, .7f) };
            for (var sh = 0; sh < 4; sh++)
            {
                mk.Box(darkWood, fo.P(1.2f, .5f + sh * .6f, -D / 2f + T + .18f), new Vector3(3.6f, .05f, .34f), f.r);
                for (var b = 0; b < 16; b++) mk.Box(WorldMaterials.Solid(books[(b * 5 + sh * 3) % books.Length]), fo.P(-.5f + b * .21f, .74f + sh * .6f - ((b * 7 + sh) % 3) * .03f, -D / 2f + T + .18f), new Vector3(.16f, .42f - ((b * 7 + sh) % 3) * .06f, .26f), f.r);
            }
            HCol(ctx, fo.P(1.2f, 1.3f, -D / 2f + T + .18f), new Vector3(3.6f, 2.6f, .4f), f.r);
            // two armchairs by the stove, a lamp, a rug, the cat
            var chairM = WorldMaterials.Solid(new Color(.45f, .3f, .28f));
            foreach (var (x, z, yaw) in new[] { (-1.2f, .3f, 150f), (.4f, -.9f, 200f) })
            {
                var p = fo.P(x, 0, z); var r = f.r * Quaternion.Euler(0, yaw, 0); var fw = r * Vector3.forward;
                mk.Box(chairM, p + up * .25f, new Vector3(.85f, .5f, .85f), r); mk.Box(chairM, p + up * .75f - fw * .36f, new Vector3(.85f, .7f, .15f), r); foreach (var e in new[] { -1f, 1f }) mk.Box(chairM, p + up * .5f + r * Vector3.right * e * .4f, new Vector3(.12f, .5f, .8f), r);
                Interact.Seat(ctx.Root, p + up * .52f + fw * .05f, fw, "Sit in the armchair");
            }
            mk.Box(WorldMaterials.Solid(new Color(.55f, .25f, .22f)), fo.P(0, .01f, .6f), new Vector3(2.6f, .02f, 1.8f), f.r);
            var lp = fo.P(1.6f, 0, .3f); mk.Prism(iron, lp, up, .03f, 1.5f, 5); mk.Prism(WorldMaterials.Glow(Warm, 1.8f), lp + up * 1.5f, up, .22f, .28f, 8, .5f); halos.Add(lp + up * 1.6f, .9f, Warm, .6f); ArtistBits.PostCollider(ctx.Root, lp, 1.6f, .15f);
            ArtistBits.PointLight(ctx.Root, fo.P(.6f, 2.2f, .4f), new Color(1f, .8f, .55f), 8f, 1.2f, true);
            var catAt = fo.P(.6f, .12f, 1.1f); var catM = WorldMaterials.Solid(new Color(.3f, .28f, .27f));
            mk.Blob(catM, catAt, new Vector3(.28f, .13f, .2f), ctx.Rng, 0f); mk.Blob(catM, catAt + f.Right * .22f + up * .06f, Vector3.one * .1f, ctx.Rng, 0f); foreach (var e in new[] { -.05f, .05f }) mk.Box(catM, catAt + f.Right * .24f + up * .16f + f.Fwd * e, new Vector3(.04f, .07f, .04f), f.r);
            var cgo = new GameObject("The library cat"); cgo.transform.SetParent(life.transform, false); cgo.transform.position = catAt;
            var purrs = new[] { "...purr.", "The cat opens one eye. Closes it.", "...purr. (Louder.)", "The cat allows it." }; var pn = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Stroke the cat", new Vector3(.8f, .6f, .7f)).onUse = _ => Say(cgo.transform, catAt + up * .7f, purrs[pn++ % purrs.Length]);
            Soul(fo.P(-1.2f, .52f, .3f) + (f.r * Quaternion.Euler(0, 150f, 0) * Vector3.forward) * .05f, f.r * Quaternion.Euler(0, 150f, 0) * Vector3.forward, Resident.Pose.Sit,
                new[] { "Take any book. Bring back a different one, if you like.", "The cat chose me. I had no say.", "I've read them all. Some of them twice, by accident." }, WinterLook(Residents.Tool.Mug));
            SmallLantern(ctx, mk, f.P(Door / 2f + 1.2f, 0, D / 2f + 1f));
        }

        // a greenhouse glowing warm in the snow
        void Greenhouse(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var f = new PlaceFrame(c, face); const float W = 4.4f, D = 6.4f, H = 2.3f; var frame = WorldMaterials.Solid(new Color(.93f, .94f, .93f));
            mk.Box(stone, c + up * .1f, new Vector3(W + .2f, .5f, D + .2f), f.r); var fo = new PlaceFrame(c + up * .36f, face);
            foreach (var e in new[] { -1f, 1f })
            {
                for (var k = 0; k <= 4; k++) { var z = -D / 2f + k * D / 4f; mk.Box(frame, fo.P(e * W / 2f, H / 2f, z), new Vector3(.08f, H, .08f), f.r); var a = fo.P(e * W / 2f, H, z); var b = fo.P(0, H + 1.3f, z); mk.Box(frame, (a + b) / 2f, new Vector3(.07f, .07f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a)); }
                mk.Box(frame, fo.P(e * W / 2f, H, 0), new Vector3(.08f, .08f, D), f.r); mk.Box(frame, fo.P(e * W / 2f, .5f, 0), new Vector3(.1f, 1f, D), f.r); solid.Box(frame, fo.P(e * W / 2f, H / 2f, 0), new Vector3(.12f, H, D), f.r);
                var pa = fo.P(e * W / 4f, H + .68f, 0); mk.Box(ice, pa, new Vector3(Mathf.Sqrt(W * W / 4f + 1.69f) - .1f, .03f, D - .1f), f.r * Quaternion.Euler(0, 0, -e * Mathf.Atan2(1.3f, W / 2f) * Mathf.Rad2Deg));
            }
            mk.Box(frame, fo.P(0, H + 1.3f, 0), new Vector3(.08f, .08f, D), f.r); mk.Box(frame, fo.P(0, H / 2f, -D / 2f), new Vector3(W, H, .08f), f.r); solid.Box(frame, fo.P(0, H / 2f, -D / 2f), new Vector3(W, H, .12f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(frame, fo.P(e * (W / 2f - .6f), H / 2f, D / 2f), new Vector3(1.2f, H, .08f), f.r); solid.Box(frame, fo.P(e * (W / 2f - .6f), H / 2f, D / 2f), new Vector3(1.2f, H, .12f), f.r); }
            // benches of plants either side of a brick path, flowers, tomatoes
            Color[] bloom = { new Color(1f, .45f, .5f), new Color(1f, .8f, .3f), new Color(.75f, .5f, 1f), new Color(.95f, .3f, .25f) };
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(darkWood, fo.P(e * 1.5f, .75f, -.3f), new Vector3(.9f, .08f, 5f), f.r); HCol(ctx, fo.P(e * 1.5f, .45f, -.3f), new Vector3(.9f, .9f, 5f), f.r);
                for (var k = 0; k < 8; k++) { var p = fo.P(e * 1.5f, .95f, -2.5f + k * .62f); mk.Prism(WorldMaterials.Solid(new Color(.65f, .38f, .28f)), p - up * .16f, up, .14f, .18f, 7, 1.2f); mk.Blob(WorldMaterials.Solid(new Color(.25f, .5f, .28f)), p + up * .2f, new Vector3(.2f, .25f, .2f), ctx.Rng, .3f); if (k % 2 == 0) mk.Blob(WorldMaterials.Glow(bloom[(k / 2 + (e > 0 ? 1 : 0)) % bloom.Length], .7f), p + up * .42f, Vector3.one * .08f, ctx.Rng, 0f); }
            }
            mk.Box(WorldMaterials.Solid(new Color(.6f, .35f, .28f)), fo.P(0, .01f, 0), new Vector3(1.4f, .02f, D - .3f), f.r);
            ArtistBits.PointLight(ctx.Root, fo.P(0, H, 0), new Color(1f, .85f, .55f), 9f, 1.8f, true); mk.Blob(WorldMaterials.Glow(Warm, 2f), fo.P(0, H + .9f, 0), Vector3.one * .14f, ctx.Rng, 0f); halos.Add(fo.P(0, H + .9f, 0), 1.6f, Warm, .6f);
            var stool = fo.P(0, 0, -2.4f); mk.Prism(darkWood, stool, up, .22f, .45f, 8); ArtistBits.PostCollider(ctx.Root, stool, .45f, .22f); Interact.Seat(ctx.Root, stool + up * .5f, f.Fwd, "Sit among the plants");
            var can = new MeshKit(); var tin = WorldMaterials.Solid(new Color(.45f, .6f, .55f), .7f); can.Prism(tin, Vector3.zero, Vector3.up, .12f, .26f, 8); can.Box(tin, new Vector3(0, .2f, .2f), new Vector3(.04f, .04f, .28f), Quaternion.Euler(-30f, 0, 0)); can.Box(tin, new Vector3(0, .22f, -.14f), new Vector3(.03f, .2f, .03f), Quaternion.identity);
            var cgo = can.BuildSingle("Watering can", life.transform, false); cgo.transform.position = fo.P(.5f, .05f, 1.6f); Interact.Carry(cgo, "the watering can", .6f, c.y - 8f);
            Soul(fo.P(-.3f, 0, 1f), -f.Right, Resident.Pose.Work, new[] { "Tomatoes in the snow. Don't tell them it's winter.", "Everything in here thinks it's June. I let it." }, WinterLook(Residents.Tool.Hose)).workRate = .5f;
        }

        // a garden of ice lanterns: light them one by one
        void IceLanterns(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var f = new PlaceFrame(c, face);
            for (var k = 0; k < 11; k++)
            {
                var a = k * 1.1f; var r = 1.2f + k * .38f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r);
                var glow = new Material(WorldMaterials.Glow(k % 3 == 0 ? Frost : Warm, .12f)) { name = "Ice lantern" };
                var il = new MeshKit(); il.Prism(ice, Vector3.zero, Vector3.up, .32f, .6f + (k % 3) * .15f, 7, .85f); il.Prism(glow, new Vector3(0, .12f, 0), Vector3.up, .16f, .34f, 6);
                var go = il.BuildSingle("Ice lantern", life.transform, false); go.transform.position = p; var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, .4f, 0); bc.size = new Vector3(.7f, .8f, .7f);
                var t = go.AddComponent<Toy>(); t.kind = Toy.Kind.Glow; t.verb = "Light the lantern"; t.glow = glow; t.glowColor = (k % 3 == 0 ? Frost : Warm) * .9f;
            }
            Bench(ctx, mk, solid, c + face * 5.2f - Vector3.Cross(up, face) * 2.4f, -face);   // beside the end of the path, not across it
            ArtistBits.PointLight(ctx.Root, c + up * 2.5f, new Color(.75f, .85f, 1f), 10f, .9f);
        }

        // a hot spring to sit in, lanterns round it
        void HotSpring(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; const float R = 3f; var lo = c.y; for (var k = 0; k < 8; k++) lo = Mathf.Min(lo, ctx.Ground(c.x + Mathf.Cos(k * .785f) * (R + 1f), c.z + Mathf.Sin(k * .785f) * (R + 1f)));
            var water = WorldMaterials.Glow(new Color(.45f, .85f, .8f), .9f);
            mk.Prism(stone, new Vector3(c.x, lo - .4f, c.z), up, R + .9f, c.y - lo + .75f, 14); solid.Prism(stone, new Vector3(c.x, lo - .4f, c.z), up, R + .9f, c.y - lo + .5f, 14);
            mk.Prism(water, c + up * .36f, up, R, .02f, 14);
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; mk.Blob(stone, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R + .35f) + up * .45f, new Vector3(.5f, .3f, .45f), ctx.Rng, .3f); }
            for (var k = 0; k < 5; k++) { var a = k * 1.2f + .5f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); if (Vector3.Angle(d, face) < 25f) continue; Interact.Seat(ctx.Root, c + d * (R - .7f) + up * .3f, -d, "Sit in the hot spring").seatHeight = .35f; }
            for (var k = 0; k < 4; k++) mk.Blob(WorldMaterials.Glow(new Color(.9f, .95f, 1f), .5f), c + up * (1f + k * .5f) + new Vector3(Mathf.Sin(k * 2f) * .8f, 0, Mathf.Cos(k * 2f) * .8f), new Vector3(.5f, .25f, .5f) * (1f - k * .15f), ctx.Rng, .3f);   // steam
            ArtistBits.PointLight(ctx.Root, c + up * 1.4f, new Color(.5f, .9f, .85f), 10f, 1.6f); halos.Add(c + up * .6f, 3f, new Color(.5f, .9f, .85f), .35f);
            foreach (var e in new[] { -1f, 1f }) SmallLantern(ctx, mk, c + face * (R + 1.6f) + Vector3.Cross(up, face) * e * 1.6f, false);
            var d0 = new Vector3(Mathf.Cos(.5f), 0, Mathf.Sin(.5f)); if (Vector3.Angle(d0, face) >= 25f) Soul(c + d0 * (R - .7f) + up * .3f, -d0, Resident.Pose.Sit, new[] { "First warm I've felt since October.", "Come in. The snow lands on your head and nowhere else." }, null, null, .35f);
            var rack = ctx.OnGround(c.x - face.x * (R + 1.8f), c.z - face.z * (R + 1.8f)); foreach (var e in new[] { -.6f, .6f }) mk.Box(darkWood, rack + Vector3.Cross(up, face) * e + up * .6f, new Vector3(.06f, 1.2f, .06f), Quaternion.identity); mk.Box(darkWood, rack + up * 1.15f, new Vector3(1.3f, .05f, .05f), Quaternion.LookRotation(face)); mk.Box(WorldMaterials.Solid(new Color(.7f, .3f, .25f)), rack + up * .85f, new Vector3(.5f, .6f, .06f), Quaternion.LookRotation(face));
            HCol(ctx, rack + up * .6f, new Vector3(1.3f, 1.2f, .2f), Quaternion.LookRotation(face));
        }

        // a deck for watching the sky: two long chairs, a telescope
        void StarDeck(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var f = new PlaceFrame(c, -face); var lo = c.y; foreach (var (x, z) in new[] { (-2.5f, -2f), (2.5f, -2f), (2.5f, 2f), (-2.5f, 2f) }) lo = Mathf.Min(lo, ctx.Ground(f.P(x, 0, z).x, f.P(x, 0, z).z));
            var top = c.y + .3f; mk.Box(planks, new Vector3(c.x, top - .06f, c.z), new Vector3(5.4f, .12f, 4.4f), f.r); mk.Box(darkWood, new Vector3(c.x, (lo + top) / 2f - .2f, c.z), new Vector3(5f, top - lo + .3f, 4f), f.r); solid.Box(planks, new Vector3(c.x, (lo + top) / 2f, c.z), new Vector3(5.4f, top - lo + .02f, 4.4f), f.r);
            var fo = new PlaceFrame(new Vector3(c.x, top, c.z), f.Fwd);
            foreach (var e in new[] { -1.1f, 1.1f })
            {
                var p = fo.P(e, 0, -.4f); mk.Box(darkWood, p + up * .3f, new Vector3(.7f, .08f, 1.7f), f.r); mk.Box(darkWood, p + up * .62f - f.Fwd * .95f, new Vector3(.7f, .08f, .9f), f.r * Quaternion.Euler(50f, 0, 0));
                mk.Box(WorldMaterials.Solid(e < 0 ? new Color(.6f, .28f, .2f) : new Color(.2f, .3f, .45f)), p + up * .36f + f.Fwd * .2f, new Vector3(.6f, .05f, 1.2f), f.r);
                HCol(ctx, p + up * .25f, new Vector3(.7f, .5f, 1.7f), f.r); Interact.Seat(ctx.Root, p + up * .42f, f.Fwd, "Lie back and watch the sky").seatHeight = .35f;
            }
            var tp = fo.P(0, 0, 1.5f); mk.Prism(iron, tp, up, .05f, 1.2f, 5); ArtistBits.PostCollider(ctx.Root, tp, 1.2f, .1f);
            var tel = new MeshKit(); var brass = WorldMaterials.Solid(new Color(.8f, .65f, .35f), .85f); tel.Prism(brass, new Vector3(0, 0, -.5f), Vector3.forward, .08f, 1f, 8, .8f);
            var tgo = tel.BuildSingle("Telescope", life.transform, false); tgo.transform.SetPositionAndRotation(tp + up * 1.3f, Quaternion.LookRotation(f.Fwd + up * .7f));
            var tt = Interact.Toy(tgo, Toy.Kind.Spin, "Turn the telescope", new Vector3(.5f, .5f, 1.2f)); tt.amount = 360f; tt.seconds = 4f;
            mk.Box(WorldMaterials.Glow(LanternGlass, .8f), fo.P(2.3f, .14f, -1.8f), new Vector3(.18f, .26f, .18f), Quaternion.identity); halos.Add(fo.P(2.3f, .2f, -1.8f), .8f, LanternGlass, .5f);
            Soul(fo.P(1.1f, .42f, -.4f), f.Fwd, Resident.Pose.Sit, new[] { "They say the lights come out when the right song plays. I believe it.", "I've counted to a thousand twice. Lost my place both times. Happily." }, null, null, .35f);
        }

        // a fishing hole on the lake: a stool, a lantern, a line down through the ice
        void FishingHole(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up;
            mk.Prism(WorldMaterials.Solid(new Color(.05f, .1f, .16f)), c + up * .045f, up, .5f, .02f, 10); mk.Prism(snowTop, c - up * .02f, up, .7f, .06f, 10);
            var st = c + face * 1.1f; mk.Prism(darkWood, st, up, .2f, .42f, 8); ArtistBits.PostCollider(ctx.Root, st, .42f, .2f); Interact.Seat(ctx.Root, st + up * .47f, -face, "Sit and fish");
            var rod = st + Vector3.Cross(up, face) * .5f; mk.Box(darkWood, rod + up * .6f - face * .6f, new Vector3(.03f, .03f, 1.5f), Quaternion.LookRotation(-face + up * .5f)); mk.Box(iron, c + up * .55f, new Vector3(.01f, 1.1f, .01f), Quaternion.identity);
            var lp = c - face * .2f + Vector3.Cross(up, face) * -1.2f; mk.Box(iron, lp + up * .05f, new Vector3(.22f, .04f, .22f), Quaternion.identity); mk.Box(WorldMaterials.Glow(LanternGlass, 1.6f), lp + up * .22f, new Vector3(.16f, .26f, .16f), Quaternion.identity); halos.Add(lp + up * .25f, 1.2f, LanternGlass, .8f);
            ArtistBits.PointLight(ctx.Root, lp + up * .8f, new Color(1f, .7f, .42f), 7f, 1.1f);
            mk.Prism(WorldMaterials.Solid(new Color(.3f, .45f, .55f), .7f), st + Vector3.Cross(up, face) * -.6f, up, .16f, .28f, 8);   // a bucket
        }
    }

    /// Wind chimes: a nudge sets them swaying, then they settle.
    public class ChimeSway : MonoBehaviour
    {
        float t = -1f; Quaternion rest;
        void Start() { rest = transform.rotation; }
        public void Ring() { t = 0f; }
        void Update()
        {
            if (t < 0f) return; t += Time.deltaTime;
            transform.rotation = rest * Quaternion.Euler(Mathf.Sin(t * 6f) * 22f * Mathf.Exp(-t * .5f), 0, Mathf.Sin(t * 4.3f) * 14f * Mathf.Exp(-t * .5f));
            if (t > 8f) { t = -1f; transform.rotation = rest; }
        }
    }
}
