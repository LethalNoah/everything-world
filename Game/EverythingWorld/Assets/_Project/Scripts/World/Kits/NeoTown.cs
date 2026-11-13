using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Neo Soul Quarter's places (part of NeoKit): the Courtyard (a tiled court under strings of lights, a
    /// monstera the size of a house in one great pot, a book and record stall with things to carry, a circle of
    /// floor cushions round a low table of candles, a tea and incense table); Honey Street (lights strung across
    /// it from pole to pole); and the terracotta houses (lit windows, balconies full of plants, candles on the stoop).
    public partial class NeoKit
    {
        NeoTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.8f, .5f, .3f), new Color(.5f, .22f, .32f), new Color(.92f, .86f, .72f), new Color(.24f, .4f, .3f), new Color(.86f, .66f, .3f), new Color(.3f, .22f, .36f), new Color(.6f, .34f, .24f) };
        static readonly Color[] Bottoms = { new Color(.22f, .18f, .22f), new Color(.5f, .4f, .3f), new Color(.86f, .8f, .68f), new Color(.3f, .26f, .4f) };
        static readonly string[] TownTalk =
        {
            "Evening. No hurry here. Nobody's ever in one.", "Mind the candles. And the plants. They were here first, both of them.", "The Listening Room's off the Courtyard. Take your shoes off in your head before you go in.",
            "We stopped pruning years ago. The plants seemed to have a plan.", "Sit a while. The record's only on side one.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1997);
            tlife = new GameObject("Neo Soul town life").AddComponent<NeoTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None }, new[] { Honey, Clay, Rose, Cream }); l.tool = tool; l.apron = apron; return l; }
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

        /// A floor cushion to sit on.
        void Cushion(MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            mk.Prism(Paint(new[] { new Color(.6f, .24f, .3f), new Color(.86f, .62f, .26f), new Color(.3f, .42f, .34f), new Color(.42f, .26f, .46f) }[i % 4]), g, Vector3.up, .42f, .22f, 10, .86f); Interact.Seat(tlife.transform, g + Vector3.up * .3f, face, "Sit on the cushion").seatHeight = .26f;
        }

        /// A crate of records standing open.
        void Crate(MeshKit mk, Vector3 g, Quaternion rot, System.Random rng)
        {
            var up = Vector3.up; mk.Box(woodM, g + up * .04f, new Vector3(.5f, .04f, .4f), rot); foreach (var e in new[] { -1f, 1f }) { mk.Box(woodM, g + up * .2f + rot * Vector3.right * e * .24f, new Vector3(.03f, .34f, .4f), rot); mk.Box(woodM, g + up * .2f + rot * Vector3.forward * e * .19f, new Vector3(.5f, .34f, .03f), rot); }
            for (var k = 0; k < 7; k++) mk.Box(Paint(Color.HSVToRGB((float)rng.NextDouble(), .45f, .75f)), g + up * (.26f + (k % 3) * .012f) + rot * Vector3.right * (-.19f + k * .063f), new Vector3(.02f, .36f, .34f), rot * Quaternion.Euler(0, 0, -6f));
        }

        // ---------------------------------------------------------------- the Courtyard
        void Courtyard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            // the tiled court: a clay disc, a darker ring, a pale star of tiles in the middle
            mk.Prism(tile, c - up * .28f, up, 20f, .42f, 32); mk.Prism(tileDark, c + up * .14f, up, 14f, .012f, 32); mk.Prism(tile, c + up * .16f, up, 13.2f, .012f, 32);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * D; var sd = Vector3.Cross(up, d); var sy = up * (k % 2 == 0 ? .2f : .24f); mk.Tri(k % 2 == 0 ? Paint(Honey) : tileDark, c + sy + d * (k % 2 == 0 ? 3.4f : 2.2f), c + sy + sd * .5f, c + sy - sd * .5f, c - up); }
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            // the great monstera, in a pot you could live in
            var mp = c - D * 10f + S * 4f + up * .14f; Monstera(ctx, mk, mp, 2.4f, rng); Candles(mk, mp + D * 3f + S * 1.4f, 7, rng); Candles(mk, mp + D * 2.2f - S * 2.6f, 5, rng);
            Folk(mp + D * 3.4f - S * .8f, -D, Resident.Pose.Work, new[] { "She was a cutting on a windowsill once. I gave her a bigger pot. Then a bigger pot.", "I water her on Sundays. It takes most of Sunday.", "New leaf coming. You can hear it unroll if the record's between songs." }, Local(Residents.Tool.Hose, true)).workRate = .6f;
            // poles round the court; strings of lights from pole to pole and across the middle
            var poles = new Vector3[8]; for (var k = 0; k < 8; k++) { var g = c + Quaternion.Euler(0, k * 45f + 22.5f, 0) * D * 18f + up * .14f; poles[k] = g + up * 5.2f; mk.Prism(iron, g - up * .2f, up, .07f, 5.4f, 5); ArtistBits.PostCollider(ctx.Root, g, 5f, .1f); mk.Box(lampM, g + up * 2.6f, new Vector3(.26f, .4f, .26f), Quaternion.identity); halos.Add(g + up * 2.6f, 1.6f, Honey, .5f); }
            for (var k = 0; k < 8; k++) BulbString(mk, poles[k], poles[(k + 1) % 8], k); for (var k = 0; k < 4; k++) BulbString(mk, poles[k], poles[k + 4], k + 1);
            ArtistBits.PointLight(ctx.Root, c + up * 5f, new Color(1f, .8f, .5f), 34f, 2.2f); ArtistBits.PointLight(ctx.Root, mp + up * 3f + D * 4f, new Color(1f, .74f, .46f), 22f, 2.2f);
            // the book and record stall: a counter under an awning, shelves behind, books to carry, crates to flip through
            var bs = new PlaceFrame(c - D * 12f - S * 11f + up * .14f, (D + S * .8f).normalized);
            mk.Box(Paint(Walls[2]), bs.P(0, 1.5f, -1.1f), new Vector3(4.6f, 3f, .2f), bs.r); Col(bs.P(0, 1.5f, -1.1f), new Vector3(4.6f, 3f, .2f), bs.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(Paint(Walls[2]), bs.P(e * 2.2f, 1.5f, -.4f), new Vector3(.2f, 3f, 1.2f), bs.r); Col(bs.P(e * 2.2f, 1.5f, -.4f), new Vector3(.2f, 3f, 1.2f), bs.r); }
            for (var row = 0; row < 3; row++) { mk.Box(woodM, bs.P(0, .7f + row * .7f, -.86f), new Vector3(4.1f, .05f, .3f), bs.r); for (var k = 0; k < 26; k++) { var h = .34f + (float)rng.NextDouble() * .2f; mk.Box(Paint(Color.HSVToRGB((float)rng.NextDouble(), .5f, .55f + (float)rng.NextDouble() * .3f)), bs.P(-1.95f + k * .155f, .73f + row * .7f + h / 2f, -.86f), new Vector3(.12f, h, .22f), bs.r); } }
            mk.Box(woodM, bs.P(0, .5f, .5f), new Vector3(3.6f, 1f, .7f), bs.r); mk.Box(Paint(Honey), bs.P(0, 1.02f, .5f), new Vector3(3.7f, .05f, .8f), bs.r); Col(bs.P(0, .5f, .5f), new Vector3(3.6f, 1f, .7f), bs.r);
            for (var k = 0; k < 5; k++) mk.Box(clothM, bs.P(-1.9f + k * .95f, 3.1f + (k % 2) * .012f, .5f), new Vector3(.93f, .05f, 2.6f), bs.r * Quaternion.Euler(10f, 0, 0)); mk.Box(Paint(Cream), bs.P(0, 2.5f, 1.7f), new Vector3(3.4f, .4f, .05f), bs.r); Sign(bs.P(0, 2.5f, 1.74f), bs.Fwd, "BOOKS  ·  RECORDS  ·  TEA", .16f, Ink); mk.Box(lampM, bs.P(0, 2.7f, -.7f), new Vector3(.2f, .2f, .2f), bs.r); ArtistBits.PointLight(ctx.Root, bs.P(0, 2.3f, .2f), new Color(1f, .8f, .5f), 9f, 2f);
            string[] names = { "a book of poems, the spine cracked at one page", "a novel with a train ticket for a bookmark", "a cookbook, splashed" };
            for (var k = 0; k < 3; k++) { var bm = new MeshKit(); bm.Box(Paint(new[] { new Color(.6f, .2f, .26f), new Color(.2f, .36f, .42f), new Color(.86f, .66f, .3f) }[k]), new Vector3(0, .03f, 0), new Vector3(.2f, .05f, .28f), Quaternion.identity); bm.Box(white, new Vector3(.005f, .03f, 0), new Vector3(.19f, .04f, .26f), Quaternion.identity); Thing(bm, "Book", names[k], bs.P(-1.1f + k * .5f, 1.06f, .5f), bs.r * Quaternion.Euler(0, k * 20f - 20f, 0), new Vector3(.22f, .08f, .3f), .3f, false); }
            Crate(mk, bs.P(1.1f, 1.05f, .5f), bs.r, rng); Crate(mk, bs.P(2.9f, 0, 1f), bs.r * Quaternion.Euler(0, 20f, 0), rng); Crate(mk, bs.P(3.5f, 0, .3f), bs.r * Quaternion.Euler(0, -14f, 0), rng);
            string[] flip = { "You flip through. Every sleeve is somebody looking straight at you, in soft focus.", "One with a coffee ring on it. One with a name in biro. One still sealed, which feels wrong.", "You find the one you were looking for. It was behind the one you weren't.", "The sleeves are worn white at the corners. Loved to bits." }; var fn = 0;
            Spot("The record crates", bs.P(3.2f, .8f, .7f), "Flip through the records", new Vector3(1.4f, 1f, 1.2f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, bs.P(3.2f, 2.2f, .7f), flip[fn++ % flip.Length], 4.5f); };
            Folk(bs.P(.4f, 0, -.2f), bs.Fwd, Resident.Pose.Stand, new[] { "Take a book. Bring it back or bring a different one. That's the whole system.", "Records are in the crates. Tea's when I get round to it.", "People come in for one thing and stay for a side and a half." }, Local(Residents.Tool.Mug, true)); Mark(bs.o, 4f);
            // a circle of floor cushions round a low table of candles
            var ct = c + D * 8f - S * 10f + up * .16f; mk.Prism(clothM, ct, up, 2.6f, .02f, 20); mk.Prism(Paint(Honey), ct + up * .022f, up, 2.2f, .012f, 20); mk.Prism(woodM, ct + up * .04f, up, .7f, .3f, 10); BoxCol(ctx.Root, ct + up * .2f, new Vector3(1.2f, .36f, 1.2f), Quaternion.identity); Candles(mk, ct + up * .34f, 6, rng);
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + 20f, 0) * D; Cushion(mk, ct + d * 1.7f, -d, k); }
            Folk(ct + Quaternion.Euler(0, 20f, 0) * D * 1.7f + up * .22f, -(Quaternion.Euler(0, 20f, 0) * D), Resident.Pose.Sit, new[] { "We were talking about nothing. It was going really well.", "Sit. You don't have to say anything. That's the good part.", "Somebody put side two on an hour ago and none of us has moved." }, null, null, .26f);
            // the tea and incense table
            var tt = c + D * 9f + S * 11f + up * .14f; var tr = Quaternion.LookRotation((-D - S).normalized); mk.Box(woodM, tt + up * .7f, new Vector3(1.6f, .06f, .8f), tr); foreach (var (x, zz) in new[] { (-.7f, -.3f), (.7f, -.3f), (.7f, .3f), (-.7f, .3f) }) mk.Box(woodM, tt + up * .34f + tr * new Vector3(x, 0, zz), new Vector3(.06f, .68f, .06f), tr); Col(tt + up * .4f, new Vector3(1.6f, .8f, .8f), tr);
            mk.Prism(pot, tt + up * .73f + tr * Vector3.right * .4f, up, .12f, .16f, 8, 1.2f); mk.Prism(pot, tt + up * .8f + tr * Vector3.right * .52f, (tr * Vector3.right + up * .5f).normalized, .02f, .14f, 4); for (var k = 0; k < 3; k++) mk.Prism(white, tt + up * .73f + tr * new Vector3(-.1f - k * .16f, 0, .14f - (k % 2) * .2f), up, .04f, .06f, 6);
            var inc = tt + up * .73f - tr * Vector3.right * .6f; mk.Prism(pot, inc, up, .07f, .03f, 6); for (var k = 0; k < 3; k++) { var d = (up + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .25f).normalized; mk.Prism(dark, inc, d, .006f, .34f, 3); mk.Blob(candleM, inc + d * .34f, Vector3.one * .012f, rng, 0f); } life.smokes.Add(inc + up * .4f);
            Folk(tt - tr * Vector3.forward * .9f, tr * Vector3.forward, Resident.Pose.Stand, new[] { "Hibiscus, ginger, or the one I don't tell people what's in.", "The incense is sandalwood. The other smell is the plants, thinking.", "Let it steep. Everything here is better steeped." }, Local(Residents.Tool.Mug)); Mark(tt, 2f);
            // two sofas, the name over the mouth of Honey Street, a welcome
            Sofa(ctx.Root, mk, c - D * 2f + S * 15.5f + up * .14f, -S, clothM, 2.4f); Sofa(ctx.Root, mk, c + D * 15f - S * 4f + up * .14f, (-D + S * .2f).normalized, Paint(new Color(.3f, .42f, .34f)), 2.2f);
            var ar = c + D * 20.5f + up * .1f; foreach (var e in new[] { -3.6f, 3.6f }) { mk.Prism(Paint(Walls[0]), ar + S * e - up * .2f, up, .3f, 4.6f, 6); ArtistBits.PostCollider(ctx.Root, ar + S * e, 4.4f, .32f); Candles(mk, ar + S * (e + Mathf.Sign(e) * .8f) + up * .05f, 4, rng); }
            mk.Box(Paint(Walls[0]), ar + up * 4.5f, new Vector3(8f, .9f, .4f), Quaternion.LookRotation(D)); Sign(ar + up * 4.5f - D * .22f, -D, "HONEY STREET", .42f, Honey); Sign(ar + up * 4.5f + D * .22f, D, "THE NEO SOUL QUARTER", .36f, Honey);
            Folk(c + D * 5f + S * 3f + up * .16f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Evening. Come in, come in. Mind the fern.", "Honey Street's through the arch. The Listening Room's just there, with the crates on the step.", "Nobody's going to rush you. We wouldn't know how." });
            Mark(c, 21f);
            Shot(ctx, "the courtyard", c + D * 15f + S * 10f + up * 2.6f, mp + up * 4.4f);
        }

        // ---------------------------------------------------------------- Honey Street
        void HoneyStreet(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, tileDark, StreetW, .13f, ctx); Strip(mk, pts, tile, StreetW - 1.4f, .15f, ctx); foreach (var p in pts) Remember(p, 1);
            // lights strung across from pole to pole, all the way down
            var n = 0; for (var d = 30f; d <= streetL - 4f; d += 10f, n++)
            {
                var a = G(ctx, d, -(StreetW / 2f + .5f)); var b = G(ctx, d + 3f * (n % 2 == 0 ? 1f : -1f), StreetW / 2f + .5f);
                foreach (var g in new[] { a, b }) { mk.Prism(iron, g - up * .2f, up, .06f, 4.8f, 5); ArtistBits.PostCollider(ctx.Root, g, 4.4f, .09f); }
                BulbString(mk, a + up * 4.5f, b + up * 4.5f, n); if (n % 2 == 0) ArtistBits.PointLight(ctx.Root, (a + b) / 2f + up * 3.4f, new Color(1f, .78f, .48f), 18f, 2.2f);
            }
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 15f) route.Add(G(ctx, d, 1.3f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 15f) route.Add(G(ctx, d, -1.3f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Just walking. It's the hour for it.", "Every window's somebody's evening. I like to guess the record." }, null, route.ToArray()).speed = .8f;
            Shot(ctx, "down honey street", G(ctx, 23f, .6f) + up * 2.2f, G(ctx, Mathf.Min(streetL, 120f), 0) + up * 5f);
        }

        /// The houses, built after the lanes so nothing stands in one: down both sides of Honey Street and round the Courtyard.
        void Houses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var rng = new System.Random(12); var off = StreetW / 2f + 2.2f + HouseD / 2f;
            bool Fits(Vector3 p) => ctx.IsLand(p.x, p.z, 7f) && !InFootprint(ctx, p.x, p.z, 7f) && !LaneNear(p.x, p.z, 7.4f);
            foreach (var e in new[] { 1f, -1f })
            for (var d = 32f + (e > 0 ? 0f : 4f); d <= streetL - 8f; d += 11.4f, i++)
            {
                var p = Wp(d, e * off); if (!Fits(p)) continue;
                House(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e), i, rng); built++;
            }
            for (var k = 0; k < 12; k++, i++)
            {
                var dir = Quaternion.Euler(0, k * 30f + 15f, 0) * D; if (Vector3.Angle(dir, D) < 40f) continue;
                var p = center + dir * 29f; if (!Fits(p)) continue;
                House(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -dir), i, rng); built++;
            }
            Debug.Log($"The Neo Soul Quarter: {built} terracotta houses");
        }

        /// A terracotta house: warm windows, a balcony full of plants, a stoop with candles, vines over the parapet.
        void House(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i, System.Random rng)
        {
            var up = Vector3.up; float W = HouseW, Dp = HouseD, H = 6.2f + (i % 3) * 1.3f; var wall = Paint(Walls[i % Walls.Length]); var trim = Paint(Walls[(i + 3) % Walls.Length]);
            mk.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); mk.Box(trim, f.P(0, H + .1f, 0), new Vector3(W + .3f, .3f, Dp + .3f), f.r); mk.Box(trim, f.P(0, 3.25f, Dp / 2f + .04f), new Vector3(W + .1f, .16f, .1f), f.r);
            // the door on its stoop
            var dx = (i % 2 == 0 ? -1f : 1f) * W / 4f; mk.Box(woodM, f.P(dx, 1.25f, Dp / 2f + .04f), new Vector3(1.2f, 2.3f, .08f), f.r); mk.Box(windowM, f.P(dx, 2.66f, Dp / 2f + .04f), new Vector3(1.2f, .4f, .08f), f.r); mk.Blob(lampM, f.P(dx + .36f, 1.2f, Dp / 2f + .11f), Vector3.one * .04f, rng, 0f);
            mk.Box(tileDark, f.P(dx, 0f, Dp / 2f + .7f), new Vector3(2f, .3f, 1.4f), f.r); solid.Box(tileDark, f.P(dx, -.2f, Dp / 2f + .7f), new Vector3(2f, .7f, 1.4f), f.r); Candles(mk, f.P(dx + .72f, .15f, Dp / 2f + 1f), 3, rng);
            // windows: one by the door, two above, one on each other wall
            void Window(Vector3 at, Quaternion r, float w, float h, bool halo) { mk.Box(white, at, new Vector3(w + .24f, h + .24f, .06f), r); mk.Box(windowM, at + r * Vector3.forward * .05f, new Vector3(w, h, .06f), r); mk.Box(white, at + r * Vector3.forward * .09f, new Vector3(.06f, h, .03f), r); if (halo) halos.Add(at + r * Vector3.forward * .5f, 1.5f, Honey, .32f); }
            Window(f.P(-dx, 1.7f, Dp / 2f + .02f), f.r, 1.7f, 1.5f, true); foreach (var e in new[] { -1f, 1f }) Window(f.P(e * W / 4f, 4.5f, Dp / 2f + .02f), f.r, 1.2f, 1.7f, e * dx < 0f);
            Window(f.P(0, 4.5f, -Dp / 2f - .02f), f.r * Quaternion.Euler(0, 180f, 0), 1.4f, 1.6f, false); foreach (var e in new[] { -1f, 1f }) Window(f.P(e * (W / 2f + .02f), 4.5f, 0), f.r * Quaternion.Euler(0, e * 90f, 0), 1.2f, 1.6f, false);
            // a balcony over the ground-floor window, full of plants, a vine hanging from it
            var bx = -dx; mk.Box(trim, f.P(bx, 3.5f, Dp / 2f + .6f), new Vector3(2.4f, .1f, 1.1f), f.r); for (var k = 0; k < 9; k++) mk.Box(iron, f.P(bx - 1.16f + k * .29f, 3.95f, Dp / 2f + 1.12f), new Vector3(.03f, .8f, .03f), f.r); mk.Box(iron, f.P(bx, 4.36f, Dp / 2f + 1.12f), new Vector3(2.4f, .04f, .05f), f.r);
            for (var k = 0; k < 3; k++) { var pp = f.P(bx - .8f + k * .8f, 3.55f, Dp / 2f + .8f); mk.Prism(pot, pp, up, .14f, .24f, 7, 1.2f); for (var q = 0; q < 4; q++) mk.Box(q % 2 == 0 ? leafM : leafPale, pp + up * (.4f + q * .1f) + Quaternion.Euler(0, q * 90f + k * 30f, 0) * Vector3.forward * .16f, new Vector3(.2f, .03f, .46f), Quaternion.Euler(-30f, q * 90f + k * 30f, 0)); }
            for (var k = 0; k < 4; k++) { var len = .8f + (float)rng.NextDouble() * 1.3f; mk.Box(k % 2 == 0 ? leafDark : leafM, f.P(bx - 1f + k * .6f, 3.4f - len / 2f, Dp / 2f + 1.16f + (k % 2) * .04f), new Vector3(.26f, len, .03f), f.r); }
            // over the parapet: a vine, and the tops of whatever's growing on the roof
            for (var k = 0; k < 5; k++) { var len = .6f + (float)rng.NextDouble() * 1.6f; mk.Box(k % 2 == 0 ? leafM : leafDark, f.P(-W / 2f + .8f + k * 1.9f + (float)rng.NextDouble() * .6f, H - len / 2f, Dp / 2f + .2f + (k % 2) * .04f), new Vector3(.5f, len, .03f), f.r); }
            for (var k = 0; k < 6; k++) { var rp = f.P(-W / 2f + 1.2f + (float)rng.NextDouble() * (W - 2.4f), H + .2f, -Dp / 2f + 1.2f + (float)rng.NextDouble() * (Dp - 2.4f)); mk.Prism(k % 2 == 0 ? leafDark : leafPale, rp, (up + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .4f).normalized, .14f, 1.2f + (float)rng.NextDouble() * 1.6f, 4, .2f); }
            if (i % 3 == 0) Folk(f.P(dx - .5f, .16f, Dp / 2f + .9f), f.Fwd, Resident.Pose.Sit, new[] { "Just out on the step. It's that kind of evening. They all are.", "You can hear four different records from here and somehow they agree.", "I've lived here since before the fern got ideas." }, null, null, .3f);
            else if (i % 3 == 1) { var cp = f.P(dx + 1.7f, 0, Dp / 2f + .9f); Crate(mk, cp, f.r * Quaternion.Euler(0, 16f, 0), rng); }
            else { var sp = f.P(-dx, 0, Dp / 2f + 1.6f); var top = Pot(ctx, mk, ctx.OnGround(sp.x, sp.z), .5f); for (var q = 0; q < 6; q++) mk.Prism(q % 2 == 0 ? leafM : leafDark, top, (up + Quaternion.Euler(0, q * 60f, 0) * Vector3.forward * .22f).normalized, .08f, 1.3f + (q % 3) * .4f, 4, .2f); }
            Mark(f.o, 7f);
        }
    }

    /// The Neo Soul Quarter's holder for things you can use and carry.
    public class NeoTownLife : MonoBehaviour { }
}
