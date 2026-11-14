using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Lo-Fi Hills' places (part of LofiKit): the Corner (a late-night shop to walk into, with snacks and an
    /// umbrella to carry; vending machines; a bus stop with the night bus waiting, lights on); Lamp Lane (lamps,
    /// power lines, puddles, houses with one lit window); and the Study, a tower with one enormous lit window and,
    /// inside, a desk under a lamp, looking out at the rain.
    public partial class LofiKit
    {
        LofiTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.8f, .74f, .62f), new Color(.46f, .4f, .56f), new Color(.4f, .5f, .62f), new Color(.52f, .6f, .5f), new Color(.86f, .6f, .5f), new Color(.3f, .32f, .4f), new Color(.9f, .8f, .5f) };
        static readonly Color[] Bottoms = { new Color(.2f, .22f, .32f), new Color(.34f, .32f, .36f), new Color(.5f, .46f, .42f), new Color(.24f, .28f, .3f) };
        static readonly string[] TownTalk =
        {
            "Evening. Still raining. It's the good kind.", "Shop's open. Shop's always open.", "The Study's at the top of Lamp Lane. The light's on; it's always on.",
            "Mind the cats. They don't move for anyone.", "Nowhere to be. That's the nice thing about here.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1999);
            tlife = new GameObject("Lo-Fi town life").AddComponent<LofiTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Cap }, new[] { Mauve, RainBlue, Cream, Sage }); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Local(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);

        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Quaternion rot, Vector3 size, float mass, bool still = true)
        {
            var go = m.BuildSingle(name, tlife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            if (still) { var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            return c;
        }

        /// Something to use that is only a spot in the air.
        Toy Spot(string name, Vector3 at, string verb, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(tlife.transform, false); go.transform.position = at; return Interact.Toy(go, Toy.Kind.Custom, verb, size);
        }

        /// A room to walk into: four walls with a doorway in the front, a floor, a ceiling. The back wall can have one wide open window.
        void Room(MeshKit mk, MeshKit solid, PlaceFrame f, float W, float Dp, float H, Material wall, float doorX, bool backWindow)
        {
            void Wall(Vector3 c, Vector3 size) { mk.Box(wall, c, size, f.r); solid.Box(wall, c, size, f.r); }
            float l = doorX - .8f + W / 2f, r = W / 2f - doorX - .8f, fz = Dp / 2f - .15f, bz = -Dp / 2f + .15f;
            if (l > .05f) Wall(f.P(-W / 2f + l / 2f, H / 2f, fz), new Vector3(l, H, .3f)); if (r > .05f) Wall(f.P(W / 2f - r / 2f, H / 2f, fz), new Vector3(r, H, .3f)); Wall(f.P(doorX, (H + 2.4f) / 2f, fz), new Vector3(1.6f, H - 2.4f, .3f));
            foreach (var e in new[] { -1f, 1f }) Wall(f.P(e * (W / 2f - .15f), H / 2f, 0), new Vector3(.3f, H, Dp - .6f));
            if (!backWindow) Wall(f.P(0, H / 2f, bz), new Vector3(W, H, .3f));
            else { Wall(f.P(0, .45f, bz), new Vector3(W, .9f, .3f)); Wall(f.P(0, (H + 2.7f) / 2f, bz), new Vector3(W, H - 2.7f, .3f)); var sw = W / 2f - 2.2f; foreach (var e in new[] { -1f, 1f }) Wall(f.P(e * (2.2f + sw / 2f), 1.8f, bz), new Vector3(sw, 1.8f, .3f)); mk.Box(woodM, f.P(0, .92f, bz), new Vector3(4.5f, .06f, .44f), f.r); }
            mk.Box(woodM, f.P(0, .03f, 0), new Vector3(W - .6f, .06f, Dp - .6f), f.r); mk.Box(wall, f.P(0, H + .1f, 0), new Vector3(W, .2f, Dp), f.r); solid.Box(wall, f.P(0, H + .1f, 0), new Vector3(W, .2f, Dp), f.r);
            IndoorSpaces.Add(f.P(0, H / 2f, 0), f.r, new Vector3(W, H, Dp));
        }

        // ---------------------------------------------------------------- the Corner
        void TheCorner(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(paving, c - up * .28f, up, 18f, .42f, 30);
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 17f, 1);
            foreach (var (a, r, s) in new[] { (40f, 6f, 1.6f), (150f, 9f, 1.1f), (215f, 5f, .9f), (300f, 11f, 1.4f), (95f, 13f, .8f) }) Puddle(mk, c + Quaternion.Euler(0, a, 0) * D * r + up * .14f, s);
            // the late-night shop: walk in
            var sh = new PlaceFrame(c - D * 11.5f - S * 5f + up * .14f, (D + S * .36f).normalized); const float W = 8f, Dp = 6f, H = 3.2f; Room(mk, solid, sh, W, Dp, H, Paint(Walls[0]), -2.4f, false);
            mk.Box(Paint(RainBlue), sh.P(0, H + .5f, Dp / 2f + .1f), new Vector3(W + .2f, .9f, .2f), sh.r); mk.Box(vendM, sh.P(0, H + .5f, Dp / 2f + .22f), new Vector3(5f, .6f, .04f), sh.r); Sign(sh.P(0, H + .5f, Dp / 2f + .26f), sh.Fwd, "OPEN LATE", .4f, Ink); halos.Add(sh.P(0, H + .5f, Dp / 2f + 1f), 3f, new Color(.8f, .92f, 1f), .26f);
            mk.Box(white, sh.P(1.2f, 1.6f, Dp / 2f + .02f), new Vector3(4.6f, 2.1f, .04f), sh.r); mk.Box(windowM, sh.P(1.2f, 1.6f, Dp / 2f + .06f), new Vector3(4.3f, 1.8f, .04f), sh.r); for (var k = 0; k < 3; k++) mk.Box(Paint(Tops[k + 1]), sh.P(.2f + k * 1f, 1.1f + (k % 2) * .3f, Dp / 2f + .1f), new Vector3(.5f, .7f, .02f), sh.r); mk.Box(Paint(RainBlue), sh.P(0, 2.9f, Dp / 2f + .7f), new Vector3(W, .06f, 1.4f), sh.r * Quaternion.Euler(8f, 0, 0));
            ArtistBits.PointLight(ctx.Root, sh.P(0, 2.6f, 0), new Color(1f, .88f, .7f), 11f, 1.7f, true); ArtistBits.PointLight(ctx.Root, sh.P(0, 2.4f, Dp / 2f + 2f), new Color(.86f, .92f, 1f), 13f, 2.2f);
            // inside: shelves of snacks, a counter, the fridge humming
            for (var row = 0; row < 3; row++) { mk.Box(white, sh.P(1.2f, .5f + row * .6f, -Dp / 2f + .5f), new Vector3(4.6f, .04f, .4f), sh.r); for (var k = 0; k < 22; k++) mk.Box(Paint(Color.HSVToRGB(((row * 7 + k * 3) % 12) / 12f, .5f, .9f)), sh.P(-1f + k * .2f, .62f + row * .6f, -Dp / 2f + .5f), new Vector3(.15f, .2f + (k % 3) * .05f, .22f), sh.r); } Col(sh.P(1.2f, 1f, -Dp / 2f + .5f), new Vector3(4.6f, 2f, .4f), sh.r);
            mk.Box(white, sh.P(W / 2f - .7f, 1f, .2f), new Vector3(.7f, 2f, 2f), sh.r); mk.Box(vendM, sh.P(W / 2f - 1.06f, 1.1f, .2f), new Vector3(.02f, 1.6f, 1.8f), sh.r); for (var k = 0; k < 12; k++) mk.Prism(Paint(Color.HSVToRGB((k % 6) / 6f, .6f, .9f)), sh.P(W / 2f - 1.1f, .5f + (k / 6) * .7f, -.5f + (k % 6) * .24f), up, .05f, .2f, 6); Col(sh.P(W / 2f - .7f, 1f, .2f), new Vector3(.7f, 2f, 2f), sh.r);
            mk.Box(woodM, sh.P(-2.6f, .5f, -1.2f), new Vector3(2f, 1f, .7f), sh.r); mk.Box(white, sh.P(-2.6f, 1.02f, -1.2f), new Vector3(2.1f, .05f, .8f), sh.r); Col(sh.P(-2.6f, .5f, -1.2f), new Vector3(2f, 1f, .7f), sh.r); mk.Box(dark, sh.P(-3f, 1.16f, -1.2f), new Vector3(.4f, .24f, .36f), sh.r);
            var can = new MeshKit(); can.Prism(Paint(new Color(.5f, .34f, .26f)), Vector3.zero, Vector3.up, .035f, .12f, 8); can.Prism(white, new Vector3(0, .12f, 0), Vector3.up, .033f, .006f, 8); Thing(can, "Coffee", "a can of coffee, still warm", sh.P(-2.2f, 1.06f, -1.1f), Quaternion.identity, new Vector3(.1f, .14f, .1f), .2f, false);
            var bun = new MeshKit(); bun.Blob(Paint(new Color(.92f, .82f, .5f)), new Vector3(0, .05f, 0), new Vector3(.09f, .05f, .09f), rng, .1f); Thing(bun, "Melon bun", "a sweet bun in a wrapper", sh.P(-2.6f, 1.06f, -1f), Quaternion.identity, new Vector3(.18f, .1f, .18f), .15f, false);
            var um = new MeshKit(); um.Prism(woodM, Vector3.zero, Vector3.up, .012f, .9f, 4); um.Cone(Paint(new Color(.9f, .86f, .5f)), new Vector3(0, .62f, 0), .46f, .26f, 8); Thing(um, "Umbrella", "a yellow umbrella", sh.P(-1.2f, .08f, Dp / 2f - .6f), Quaternion.identity, new Vector3(.3f, 1f, .3f), .4f);
            Folk(sh.P(-2.8f, .06f, -2f), sh.Fwd, Resident.Pose.Stand, new[] { "Evening. Take your time. Nobody's ever come in here in a hurry.", "Coffee's warm, bun's on the counter, umbrella's by the door. Bring the umbrella back.", "I do the night shift. They're all the night shift." }, Local(Residents.Tool.None, true));
            Cat(mk, sh.P(-1.9f, 1.05f, -1.3f), sh.Fwd, true, true);
            // outside: two vending machines, a bin, a cat on top
            Vending(ctx, mk, sh.P(W / 2f + .9f, -.14f, Dp / 2f - .2f) + up * .14f, sh.Fwd, 0); Vending(ctx, mk, sh.P(W / 2f + 2.1f, -.14f, Dp / 2f - .2f) + up * .14f, sh.Fwd, 1, false); Cat(mk, sh.P(W / 2f + 2.1f, 1.9f, Dp / 2f - .2f), sh.Fwd, true, false); Mark(sh.o, 6.4f);
            // the bus stop: a shelter, a bench, a timetable nobody reads; the night bus, waiting with its lights on
            var bs = new PlaceFrame(c + D * 3f + S * 13f + up * .14f, -S); foreach (var e in new[] { -1.5f, 1.5f }) { mk.Prism(iron, bs.P(e, -.2f, -.6f), up, .05f, 2.7f, 5); ArtistBits.PostCollider(ctx.Root, bs.P(e, 0, -.6f), 2.5f, .07f); } mk.Box(Paint(new Color(.6f, .7f, .8f)), bs.P(0, 1.3f, -.6f), new Vector3(3f, 2f, .04f), bs.r); mk.Box(iron, bs.P(0, 2.5f, -.1f), new Vector3(3.4f, .08f, 1.5f), bs.r); mk.Box(lampM, bs.P(0, 2.44f, -.1f), new Vector3(1.6f, .03f, .1f), bs.r); Col(bs.P(0, 1.3f, -.6f), new Vector3(3f, 2.4f, .1f), bs.r);
            Bench(ctx.Root, mk, bs.P(0, 0, -.2f), bs.Fwd, 2f); var sp = bs.P(2.3f, 0, .3f); mk.Prism(iron, sp - up * .2f, up, .04f, 2.8f, 5); mk.Prism(Paint(RainBlue), sp + up * 2.5f - bs.Fwd * .03f, bs.Fwd, .3f, .06f, 14); Sign(sp + up * 2.5f + bs.Fwd * .04f, bs.Fwd, "BUS", .16f, Cream); mk.Box(white, sp + up * 1.5f + bs.Fwd * .05f, new Vector3(.4f, .6f, .02f), bs.r); ArtistBits.PostCollider(ctx.Root, sp, 2.6f, .06f); ArtistBits.PointLight(ctx.Root, bs.P(0, 2.2f, .3f), new Color(1f, .82f, .56f), 9f, 2.2f);
            Folk(bs.P(.5f, 0, -.1f), bs.Fwd, Resident.Pose.Sit, new[] { "I'm not waiting for the bus. I just like the bench.", "It leaves in five minutes. It has left in five minutes for as long as I've sat here.", "Listen to it on the shelter roof. Best sound there is." }); Cat(mk, bs.P(-.7f, .5f, -.2f), bs.Fwd, true, true);
            var bp = c + D * 10f + S * 15.5f + up * .14f; var br = Quaternion.LookRotation(D); mk.Box(road, bp - up * .2f, new Vector3(3.6f, .42f, 10.4f), br); var bus = Paint(new Color(.42f, .56f, .6f)); mk.Box(bus, bp + up * 1.7f, new Vector3(2.4f, 2.6f, 8.4f), br); mk.Box(Paint(Cream), bp + up * 1.2f, new Vector3(2.44f, .5f, 8.44f), br); mk.Box(Paint(Cream), bp + up * 3.04f, new Vector3(2.2f, .1f, 8f), br);
            foreach (var e in new[] { -1f, 1f }) { for (var k = 0; k < 5; k++) mk.Box(windowM, bp + up * 2.1f + br * new Vector3(e * 1.21f, 0, -2.9f + k * 1.4f), new Vector3(.02f, .9f, 1.2f), br); mk.Blob(lampM, bp + up * .9f + br * new Vector3(e * .8f, 0, 4.21f), new Vector3(.16f, .12f, .03f), rng, 0f); mk.Box(GlowM(new Color(1f, .2f, .2f), .9f), bp + up * .9f + br * new Vector3(e * .8f, 0, -4.21f), new Vector3(.3f, .14f, .03f), br); foreach (var q in new[] { -2.6f, 2.6f }) mk.Prism(dark, bp + up * .46f + br * new Vector3(e * 1.05f, 0, q), br * Vector3.right * e, .46f, .24f, 12); }
            mk.Box(windowM, bp + up * 2.1f + br * new Vector3(0, 0, 4.21f), new Vector3(2.1f, 1f, .02f), br); mk.Box(dark, bp + up * 2.86f + br * new Vector3(0, 0, 4.22f), new Vector3(1.6f, .3f, .02f), br); Sign(bp + up * 2.86f + D * 4.24f, D, "LAST BUS", .16f, Lamp); mk.Box(dark, bp + up * 1.5f + br * new Vector3(-1.21f, 0, 3f), new Vector3(.03f, 2f, .9f), br); Col(bp + up * 1.6f, new Vector3(2.4f, 3f, 8.4f), br); halos.Add(bp + up * .9f + D * 4.8f, 1.6f, Lamp, .4f); ArtistBits.PointLight(ctx.Root, bp + up * 1.6f - S * 2.6f, new Color(1f, .8f, .5f), 9f, 2f);
            Folk(bp - S * 2f + D * 3.2f, -S, Resident.Pose.Lean, new[] { "Leaving in five. Always leaving in five.", "I don't mind waiting. Engine's warm, radio's on, nobody's cross.", "Where does it go? Round the hills and back here. Best route I've ever had." }, Local(Residents.Tool.Mug)); Mark(bp, 5f);
            // lamps, a bench, a welcome, somebody under the awning
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + 36f, 0) * D; StreetLamp(ctx, mk, c + d * 16.6f + up * .14f, -d, true); }
            Bench(ctx.Root, mk, c - D * 2f - S * 14f + up * .14f, S, 2f); PotPlant(ctx.Root, mk, c - D * 4f - S * 14.4f + up * .14f, 1.2f, 1);
            Folk(c + D * 5f - S * 3f + up * .16f, (-D + S * .3f).normalized, Resident.Pose.Wave, new[] { "Oh, hello. You got caught in it too. Come under.", "Shop's just there. Lamp Lane's straight up, to the Study with the big window.", "You don't have to do anything here. Sit, if you like. Listen to it come down." });
            Mark(c, 19f);
            Shot(ctx, "the corner", c + D * 14f - S * 9f + up * 2.4f, sh.P(1f, 2f, 0));
            Shot(ctx, "inside the shop", sh.P(-2.4f, 1.7f, Dp / 2f - .8f), sh.P(1.4f, 1.2f, -Dp / 2f));
        }

        // ---------------------------------------------------------------- Lamp Lane
        void LampLane(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 17f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, Mathf.Sin(d / 26f) * 2.4f)); if (pts.Count < 2) return;
            Strip(mk, pts, road, StreetW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            var rng = new System.Random(6); var n = 0; Vector3 last = default; var have = false;
            for (var i = 4; i < pts.Count - 2; i += 5, n++)
            {
                var along = (pts[i + 1] - pts[i]).normalized; var side = Vector3.Cross(up, along);
                var lp = pts[i] + side * (n % 2 == 0 ? 1f : -1f) * (StreetW / 2f + .7f); StreetLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * (n % 2 == 0 ? 1f : -1f), true);
                if (n % 2 == 1) { var pp = pts[i] + side * (StreetW / 2f + 1.1f) + along * 2f; var top = Pole(ctx, mk, ctx.OnGround(pp.x, pp.z), side); if (have) Wires(mk, last, top, side); last = top; have = true; }
                Puddle(mk, pts[i + 2] + side * ((float)rng.NextDouble() * 2.4f - 1.2f) + up * .15f, .5f + (float)rng.NextDouble() * .7f);
            }
            var route = new List<Vector3>(); for (var d = 22f; d <= streetL - 6f; d += 14f) route.Add(G(ctx, d, Mathf.Sin(d / 26f) * 2.4f + 1f) + up * .16f); for (var d = streetL - 6f; d >= 22f; d -= 14f) route.Add(G(ctx, d, Mathf.Sin(d / 26f) * 2.4f - 1f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Just walking. I like it when the road shines.", "Up to the Study and back. Clears the head." }, null, route.ToArray()).speed = .8f;
            Shot(ctx, "up lamp lane", G(ctx, 20f, .6f) + up * 2.2f, G(ctx, streetL + 14f, 0) + up * 9f);
        }

        // ---------------------------------------------------------------- the Study
        void TheStudy(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var sc = G(ctx, streetL + 12f, 0); var rng = new System.Random(77);
            mk.Prism(paving, sc - up * .28f, up, 13f, .42f, 26); for (var k = 0; k < 8; k++) Remember(sc + Quaternion.Euler(0, k * 45f, 0) * D * 12f, 1); Remember(sc - D * 6f, 1);
            // the tower: a room at the bottom to walk into, and above it one enormous lit window
            var f = new PlaceFrame(sc + D * 5f + up * .14f, -D); const float W = 8f, Dp = 7f, H = 3.2f, TH = 16f; var wall = Paint(Walls[2]); Room(mk, solid, f, W, Dp, H, wall, -2.6f, true);
            mk.Box(wall, f.P(0, (TH + H) / 2f + .2f, 0), new Vector3(W, TH - H - .2f, Dp), f.r); solid.Box(wall, f.P(0, (TH + H) / 2f + .2f, 0), new Vector3(W, TH - H - .2f, Dp), f.r); mk.GableRoof(Paint(Roofs[0]), f.P(0, TH, 0), new Vector3(Dp + 1.2f, 0, W + 1.2f), 3f, Quaternion.LookRotation(f.Right)); mk.Box(Paint(Roofs[0]), f.P(0, H + .5f, Dp / 2f + .5f), new Vector3(W + .4f, .1f, 1.3f), f.r * Quaternion.Euler(12f, 0, 0));
            mk.Box(white, f.P(0, 9.6f, Dp / 2f + .02f), new Vector3(5.6f, 6.6f, .06f), f.r); mk.Box(windowM, f.P(0, 9.6f, Dp / 2f + .07f), new Vector3(5.2f, 6.2f, .06f), f.r); mk.Box(white, f.P(0, 9.6f, Dp / 2f + .12f), new Vector3(.12f, 6.2f, .04f), f.r); mk.Box(white, f.P(0, 10.6f, Dp / 2f + .12f), new Vector3(5.2f, .12f, .04f), f.r); halos.Add(f.P(0, 9.6f, Dp / 2f + 1.6f), 7f, Lamp, .2f);
            // in the window, dark against the light: a desk lamp, a stack of books, a cat
            mk.Box(dark, f.P(0, 6.7f, Dp / 2f + .16f), new Vector3(5.2f, .2f, .04f), f.r); mk.Box(dark, f.P(-1.4f, 7.4f, Dp / 2f + .16f), new Vector3(.1f, 1.3f, .04f), f.r * Quaternion.Euler(0, 0, -16f)); mk.Box(dark, f.P(-1f, 8.3f, Dp / 2f + .16f), new Vector3(.1f, 1f, .04f), f.r * Quaternion.Euler(0, 0, 50f)); mk.Box(dark, f.P(-.4f, 8.5f, Dp / 2f + .16f), new Vector3(.7f, .4f, .04f), f.r * Quaternion.Euler(0, 0, 30f)); for (var k = 0; k < 4; k++) mk.Box(dark, f.P(1.3f + (k % 2) * .08f, 6.95f + k * .2f, Dp / 2f + .16f), new Vector3(.9f - k * .08f, .18f, .04f), f.r); mk.Box(dark, f.P(1.3f, 7.9f, Dp / 2f + .16f), new Vector3(.5f, .3f, .04f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(dark, f.P(1.3f + e * .16f, 8.12f, Dp / 2f + .16f), new Vector3(.12f, .16f, .04f), f.r * Quaternion.Euler(0, 0, e * 20f));
            foreach (var e in new[] { -1f, 1f }) { var r = f.r * Quaternion.Euler(0, e * 90f, 0); mk.Box(windowM, f.P(e * (W / 2f + .03f), 6f, 0), new Vector3(1.2f, 1.5f, .06f), r); mk.Box(windowM, f.P(e * (W / 2f + .03f), 11f, 1f), new Vector3(1.2f, 1.5f, .06f), r); }
            mk.Box(Paint(Walls[4]), f.P(2.6f, TH + 2.4f, -1f), new Vector3(.8f, 2.6f, .8f), f.r); life.smokes.Add(f.P(2.6f, TH + 3.9f, -1f)); Sign(f.P(-2.6f, 2.72f, Dp / 2f + .02f), f.Fwd, "the study", .16f, Cream);
            ArtistBits.PointLight(ctx.Root, f.P(0, 4f, Dp / 2f + 4f), new Color(1f, .8f, .5f), 22f, 2.4f);
            // inside: the desk under the open window, looking out over the hills at the rain
            var dz = -Dp / 2f + .9f; mk.Box(woodM, f.P(0, .76f, dz), new Vector3(3.4f, .06f, .9f), f.r); foreach (var e in new[] { -1.6f, 1.6f }) mk.Box(woodM, f.P(e, .38f, dz), new Vector3(.08f, .74f, .8f), f.r); Col(f.P(0, .4f, dz), new Vector3(3.4f, .82f, .9f), f.r);
            var lb = f.P(-1.2f, .79f, dz - .1f); mk.Prism(iron, lb, up, .1f, .03f, 8); mk.Prism(iron, lb, (up - f.Right * -.3f).normalized, .015f, .5f, 4); mk.Prism(iron, lb + (up + f.Right * .3f).normalized * .5f, (f.Right + up * .2f).normalized, .015f, .4f, 4); var lh = lb + (up + f.Right * .3f).normalized * .5f + (f.Right + up * .2f).normalized * .4f; mk.Cone(Paint(Sage), lh - up * .1f, .14f, .16f, 8); mk.Blob(lampM, lh - up * .1f, Vector3.one * .06f, rng, 0f); ArtistBits.PointLight(ctx.Root, lh - up * .3f, new Color(1f, .8f, .5f), 7f, 2.6f, true); ArtistBits.PointLight(ctx.Root, f.P(1f, 2.6f, 1f), new Color(1f, .78f, .52f), 9f, 2f, true);
            mk.Box(white, f.P(.1f, .8f, dz + .1f), new Vector3(.5f, .02f, .36f), f.r * Quaternion.Euler(0, 8f, 0)); mk.Box(Paint(Mauve), f.P(.7f, .82f, dz - .1f), new Vector3(.34f, .05f, .26f), f.r * Quaternion.Euler(0, -14f, 0)); mk.Prism(Paint(Cream), f.P(1.3f, .79f, dz + .1f), up, .05f, .1f, 8); life.smokes.Add(f.P(1.3f, .96f, dz + .1f)); mk.Prism(woodM, f.P(.3f, .8f, dz + .2f), f.Right, .008f, .16f, 4);
            var hp = new MeshKit(); foreach (var e in new[] { -1f, 1f }) hp.Prism(Paint(Mauve), new Vector3(e * .09f, .06f, 0), Vector3.right * e, .06f, .04f, 8); for (var k = 0; k < 5; k++) { var a = (k * 45f) * Mathf.Deg2Rad; hp.Blob(dark, new Vector3(Mathf.Cos(a) * .1f, .06f + Mathf.Sin(a) * .1f, 0), Vector3.one * .018f, rng, 0f); } Thing(hp, "Headphones", "a pair of headphones, still playing", f.P(-.5f, .8f, dz + .16f), f.r, new Vector3(.24f, .2f, .12f), .2f);
            Cat(mk, f.P(1.1f, .79f, dz - .2f), f.Right * -1f, true, true); Chair(mk, f.P(0, .06f, dz + .9f), -f.Fwd, "Sit at the desk");
            var tp = f.P(-.5f, .79f, dz - .2f); mk.Box(Paint(new Color(.3f, .3f, .36f)), tp + up * .06f, new Vector3(.4f, .12f, .24f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Prism(white, tp + up * .07f + f.Right * e * .1f + f.Fwd * .121f, f.Fwd, .04f, .004f, 8); mk.Blob(GlowM(new Color(1f, .4f, .3f), 1.4f), tp + up * .1f + f.Right * .16f + f.Fwd * .122f, Vector3.one * .012f, rng, 0f);
            string[] tape = { "Click. Hiss. A soft kick, a softer snare, a piano somebody sampled off a warped record.", "You turn the tape over. Side B is the same, but later at night.", "The beat loops. The rain keeps time. You get one more page done.", "A voice in the sample says something you can't quite catch. You never will. It's better." }; var tn = 0;
            Spot("The tape player", tp + up * .4f + f.Fwd * .3f, "Press play", new Vector3(.6f, .5f, .6f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, tp + up * 1.3f, tape[tn++ % tape.Length], 4.5f); };
            // a bed, a shelf of books, a rug, a plant, pictures
            var bd = f.P(W / 2f - 1.2f, .06f, .6f); mk.Box(woodM, bd + up * .2f, new Vector3(1.4f, .4f, 2.6f), f.r); mk.Box(white, bd + up * .46f, new Vector3(1.3f, .14f, 2.5f), f.r); mk.Box(quilt, bd + up * .52f + f.Fwd * .3f, new Vector3(1.36f, .14f, 1.8f), f.r); mk.Box(white, bd + up * .6f - f.Fwd * .95f, new Vector3(.9f, .14f, .46f), f.r); Col(bd + up * .34f, new Vector3(1.4f, .7f, 2.6f), f.r); Interact.Seat(tlife.transform, bd + up * .66f - f.Right * .5f, -f.Right, "Sit on the bed").seatHeight = .6f;
            var bk = f.P(-W / 2f + .5f, .06f, -.4f); mk.Box(woodM, bk + up * 1f, new Vector3(.4f, 2f, 1.8f), f.r); for (var row = 0; row < 4; row++) for (var k = 0; k < 9; k++) mk.Box(Paint(Color.HSVToRGB(((row * 5 + k * 2) % 11) / 11f, .4f, .7f)), bk + up * (.36f + row * .46f) + f.Right * .12f + f.Fwd * (-.72f + k * .18f), new Vector3(.24f, .32f + (k % 3) * .04f, .13f), f.r); Col(bk + up * 1f, new Vector3(.4f, 2f, 1.8f), f.r);
            mk.Prism(quilt, f.P(0, .07f, .6f), up, 1.7f, .012f, 18); PotPlant(ctx.Root, mk, f.P(-W / 2f + .7f, .06f, 2.2f), 1.1f, 2); for (var k = 0; k < 3; k++) mk.Box(Paint(new[] { RainBlue, Pink, Sage }[k]), f.P(W / 2f - .32f, 1.9f + (k % 2) * .2f, -1.4f + k * .9f), new Vector3(.02f, .6f, .46f), f.r);
            StreetLamp(ctx, mk, sc - D * 3f + S * 6f, -S, true); Bench(ctx.Root, mk, sc - D * 2f - S * 7f, S); Vending(ctx, mk, sc + D * 2f - S * 8.6f + up * .14f, S, 2); Cat(mk, sc - D * 2f - S * 7f + up * .5f + D * .5f, S, false, true);
            Mark(sc, 14f); Mark(f.o, 6f);
            Shot(ctx, "the study", sc - D * 17f + S * 4f + up * 2.2f, f.P(0, 8f, 0));
            Shot(ctx, "at the desk", f.P(0, 1.3f, dz + 1.3f), f.P(0, 1.5f, -Dp / 2f - 20f));
            Shot(ctx, "inside the study", f.P(-2.4f, 1.7f, Dp / 2f - .8f), f.P(1.4f, 1f, -Dp / 2f));
        }

        /// The houses up Lamp Lane, built after the lanes so nothing stands in one.
        void Cottages(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var rng = new System.Random(12);
            foreach (var e in new[] { 1f, -1f })
            for (var d = 30f + (e > 0 ? 0f : 6f); d <= streetL - 10f; d += 12f, i++)
            {
                var p = Wp(d, e * (StreetW / 2f + 6.6f) + Mathf.Sin(d / 26f) * 2.4f); if (!ctx.IsLand(p.x, p.z, 6f) || InFootprint(ctx, p.x, p.z, 6f) || LaneNear(p.x, p.z, 6f)) continue;
                Cottage(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e), i, rng); built++;
            }
            Debug.Log($"The Lo-Fi Hills: {built} houses on Lamp Lane");
        }

        /// A small house: one warm window with a curtain, a porch lamp, a chimney. Usually a cat.
        void Cottage(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i, System.Random rng)
        {
            var up = Vector3.up; float W = 6.4f, Dp = 5.4f, H = i % 3 == 0 ? 5.4f : 3f; var wall = Paint(Walls[i % Walls.Length]); var roof = Paint(Roofs[i % Roofs.Length]);
            mk.Box(wall, f.P(0, H / 2f - .4f, 0), new Vector3(W, H + .8f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .4f, 0), new Vector3(W, H + .8f, Dp), f.r); mk.GableRoof(roof, f.P(0, H, 0), new Vector3(Dp + .9f, 0, W + .9f), 1.7f, Quaternion.LookRotation(f.Right)); mk.Box(Paint(Walls[4]), f.P(W * .26f, H + 1.3f, -.6f), new Vector3(.6f, 1.6f, .6f), f.r); if (i % 2 == 0) life.smokes.Add(f.P(W * .26f, H + 2.3f, -.6f));
            var dx = (i % 2 == 0 ? -1f : 1f) * W * .26f; mk.Box(woodM, f.P(dx, 1.05f, Dp / 2f + .03f), new Vector3(1f, 2.1f, .06f), f.r); mk.Box(paving, f.P(dx, .06f, Dp / 2f + .6f), new Vector3(1.6f, .16f, 1.1f), f.r); mk.Blob(lampM, f.P(dx + .8f, 2.2f, Dp / 2f + .16f), Vector3.one * .1f, rng, 0f); halos.Add(f.P(dx + .8f, 2.2f, Dp / 2f + .4f), 1f, Lamp, .5f);
            void Window(Vector3 at, float w, float h) { mk.Box(white, at, new Vector3(w + .2f, h + .2f, .05f), f.r); mk.Box(windowM, at + f.Fwd * .04f, new Vector3(w, h, .05f), f.r); mk.Box(curtain, at + f.Fwd * .075f - f.Right * (w / 2f - w * .14f), new Vector3(w * .28f, h, .02f), f.r); mk.Box(white, at + f.Fwd * .08f, new Vector3(.05f, h, .03f), f.r); halos.Add(at + f.Fwd * .5f, 1.5f, Lamp, .34f); }
            Window(f.P(-dx, 1.6f, Dp / 2f + .02f), 1.6f, 1.2f); if (H > 4f) Window(f.P(0, 4.2f, Dp / 2f + .02f), 1.2f, 1.1f);
            var bw = f.r * Quaternion.Euler(0, 180f, 0); mk.Box(windowM, f.P(0, 1.6f, -Dp / 2f - .03f), new Vector3(1.2f, 1f, .05f), bw);
            mk.Box(woodM, f.P(-dx, .94f, Dp / 2f + .16f), new Vector3(1.7f, .05f, .26f), f.r); for (var k = 0; k < 3; k++) { var pp = f.P(-dx - .6f + k * .6f, .97f, Dp / 2f + .16f); mk.Prism(Paint(new Color(.7f, .46f, .36f)), pp, up, .08f, .12f, 6, 1.2f); mk.Blob(k % 2 == 0 ? leafM : leafMauve, pp + up * .22f, Vector3.one * .12f, rng, .3f); }
            if (i % 3 != 2) Cat(mk, i % 2 == 0 ? f.P(dx + .5f, .14f, Dp / 2f + .7f) : f.P(-dx + .5f, .97f, Dp / 2f + .2f), f.Fwd, i % 2 == 1, i % 4 == 0);
            if (i % 4 == 1) { var bk = f.P(W / 2f + .5f, 0, 1f); foreach (var q in new[] { -.5f, .5f }) mk.Prism(dark, bk + up * .34f + f.Fwd * q - f.Right * .02f, f.Right, .32f, .04f, 12); mk.Box(Paint(RainBlue), bk + up * .6f, new Vector3(.05f, .06f, 1f), f.r); mk.Prism(iron, bk + up * .6f + f.Fwd * .4f, up, .015f, .36f, 4); mk.Box(dark, bk + up * .82f - f.Fwd * .3f, new Vector3(.12f, .04f, .24f), f.r); Col(bk + up * .5f, new Vector3(.3f, 1f, 1.6f), f.r); }
            Mark(f.o, 5.2f);
        }
    }

    /// The Lo-Fi Hills' holder for things you can use and carry.
    public class LofiTownLife : MonoBehaviour { }
}
