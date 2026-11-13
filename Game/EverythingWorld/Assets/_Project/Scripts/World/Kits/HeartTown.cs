using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Heartbreak Boardwalk's places (part of HeartKit): the Promenade (the old bandstand, a payphone, a closed
    /// ice-cream kiosk, a fortune-teller machine, benches facing the sunset); the Boardwalk (weathered boards, globe
    /// lamps, shuttered stalls, a carousel standing still, a wooden rollercoaster); the Pier (out over the water on
    /// its posts, a telescope, fishing rods, a little lit café at the end); the Heartbreak Motel (half its sign lit,
    /// an empty pool, doors along a balcony); and faded beach houses.
    public partial class HeartKit
    {
        HeartTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.94f, .9f, .84f), new Color(.86f, .6f, .66f), new Color(.5f, .6f, .74f), new Color(.2f, .18f, .22f), new Color(.92f, .8f, .5f), new Color(.6f, .5f, .7f), new Color(.7f, .24f, .3f) };
        static readonly Color[] Bottoms = { new Color(.26f, .3f, .44f), new Color(.86f, .82f, .76f), new Color(.2f, .18f, .22f), new Color(.5f, .4f, .36f) };
        static readonly string[] TownTalk =
        {
            "Season's over. It's the best time. Nobody here but us and the gulls.", "The lamps come on one at a time. I count them. It's something to do.", "Out on the pier you can see the whole sunset. It's the same sunset every night. Still gets me.",
            "Everybody who comes here is getting over somebody. You can tell by the walk.", "Sit with me a while. We don't have to talk.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2012);
            tlife = new GameObject("Heartbreak town life").AddComponent<HeartTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Straw, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None }, new[] { Rose, Cream, SeaBlue, Peach }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Promenade
        void Promenade(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(boards, c - up * .28f, up, 18f, .42f, 30); for (var k = 0; k < 30; k++) { var d = Quaternion.Euler(0, k * 12f, 0) * D; mk.Box(boardsDark, c + up * .16f + d * 9f, new Vector3(.06f, .01f, 17f), Quaternion.LookRotation(d)); }
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 17f, 1);
            // the bandstand: an octagon, white posts, a striped roof, a single lamp
            var bs = c - D * 2f + S * 6f + up * .14f; mk.Prism(white, bs, up, 4f, .6f, 8); solid.Prism(white, bs - up * .3f, up, 4f, .9f, 8); for (var k = 0; k < 8; k++) { var p = bs + Quaternion.Euler(0, k * 45f + 22.5f, 0) * Vector3.forward * 3.7f; mk.Prism(white, p + up * .6f, up, .1f, 3f, 6); ArtistBits.PostCollider(ctx.Root, p, 3.6f, .12f); }
            for (var k = 0; k < 8; k++) { var a = k * 45f + 22.5f; var p0 = bs + up * 3.6f + Quaternion.Euler(0, a, 0) * Vector3.forward * 4.3f; var p1 = bs + up * 3.6f + Quaternion.Euler(0, a + 45f, 0) * Vector3.forward * 4.3f; mk.Tri(k % 2 == 0 ? stripeA : stripeB, bs + up * 5.4f, p0, p1, bs); }
            mk.Cone(iron, bs + up * 5.4f, .1f, .8f, 6); mk.Blob(lampM, bs + up * 3.3f, Vector3.one * .3f, rng, 0f); ArtistBits.PointLight(ctx.Root, bs + up * 3f, new Color(1f, .82f, .6f), 14f, 2.2f); halos.Add(bs + up * 3.3f, 2f, new Color(1f, .86f, .64f), .5f);
            for (var k = 0; k < 3; k++) { var cp = bs + up * .6f + Quaternion.Euler(0, k * 50f - 50f, 0) * (-D) * 1.6f; Chair(mk, cp, (D + Quaternion.Euler(0, k * 50f - 50f, 0) * D * 0f).normalized, "Sit in the bandstand"); }
            Folk(bs + up * .6f - D * .4f + S * 1.4f, D, Resident.Pose.Work, new[] { "The band hasn't come since August. I keep the chairs out in case.", "Somebody plays the old songs on a radio here every night. It might be me.", "Sit, sit. The sunset's in about... it's always in about ten minutes." }, Local(Residents.Tool.Broom)).workRate = .5f;
            // the payphone, the fortune-teller, the kiosk
            var pp = new PlaceFrame(c + D * 10f + S * 11f + up * .14f, (-D - S).normalized); mk.Box(iron, pp.P(0, 1.1f, 0), new Vector3(.7f, 2.2f, .5f), pp.r); mk.Box(Paint(new Color(.7f, .72f, .76f)), pp.P(0, 1.2f, .26f), new Vector3(.5f, .7f, .03f), pp.r); mk.Box(dark, pp.P(-.1f, 1.3f, .32f), new Vector3(.12f, .4f, .1f), pp.r); mk.Box(lampM, pp.P(0, 2.1f, .26f), new Vector3(.5f, .14f, .02f), pp.r); Sign(pp.P(0, 2.1f, .28f), pp.Fwd, "TELEPHONE", .07f, Ink); Col(pp.P(0, 1.1f, 0), new Vector3(.7f, 2.2f, .5f), pp.r);
            string[] call = { "You put in a coin and dial a number you shouldn't still know by heart. It rings out.", "\"The number you have reached...\" You hang up before the end. You always do.", "Nobody picks up. You talk anyway, for a bit. The sea listens." }; var cn = 0;
            Spot("The payphone", pp.P(0, 1.4f, .6f), "Make a call", new Vector3(.8f, 1f, .8f)).onUse = _ => Say(tlife.transform, pp.P(0, 2.6f, .4f), call[cn++ % call.Length], 4.5f);
            var ft = new PlaceFrame(c - D * 12f - S * 8f + up * .14f, (D + S * .6f).normalized); mk.Box(Paint(new Color(.42f, .2f, .3f)), ft.P(0, 1.2f, 0), new Vector3(1.4f, 2.4f, 1f), ft.r); mk.Box(windowM, ft.P(0, 1.6f, .51f), new Vector3(1f, .9f, .02f), ft.r); mk.Blob(Paint(new Color(.86f, .72f, .6f)), ft.P(0, 1.7f, .2f), Vector3.one * .22f, rng, 0f); mk.Box(Paint(Violet), ft.P(0, 1.96f, .2f), new Vector3(.4f, .14f, .4f), ft.r); mk.Box(neonM, ft.P(0, 2.5f, .51f), new Vector3(1.2f, .2f, .02f), ft.r); Sign(ft.P(0, 2.5f, .53f), ft.Fwd, "YOUR FORTUNE", .09f, Ink); Col(ft.P(0, 1.2f, 0), new Vector3(1.4f, 2.4f, 1f), ft.r);
            string[] fort = { "A card slides out: \"You will miss this. You already do.\"", "A card slides out: \"Someone is thinking of you right now. Probably at a red light.\"", "A card slides out: \"The heart is a muscle. Use it.\"", "A card slides out: \"Summer ends. You don't.\"" }; var fn = 0;
            Spot("The fortune machine", ft.P(0, 1.4f, .8f), "Ask for your fortune", new Vector3(1f, 1f, .8f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, ft.P(0, 3f, .6f), fort[fn++ % fort.Length], 4.5f); };
            Stall(ctx, mk, new PlaceFrame(c + D * 6f - S * 13f + up * .14f, (S + D * .2f).normalized), 4);
            // benches facing the sunset, palms, lamps round the edge
            for (var k = 0; k < 4; k++) Bench(ctx.Root, mk, c + D * (5f + (k / 2) * 3f) + S * (k % 2 == 0 ? -4f : -8f) + up * .14f, D, 2f);
            Folk(c + D * 5f - S * 4f + up * .14f, D, Resident.Pose.Sit, new[] { "Shh. Watch. Right... there. That's the part.", "We used to come here every summer. Now I just come.", "I'm not sad. It just looks sad from the outside." });
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * D; GlobeLamp(ctx, mk, c + d * 16.5f + up * .14f, k % 2 == 0); }
            for (var k = 0; k < 4; k++) Palm(ctx, mk, c + Quaternion.Euler(0, k * 90f + 75f, 0) * D * 19.5f, 1.2f, rng);
            Folk(c + D * 3f + S * 2f + up * .14f, (-D - S * .4f).normalized, Resident.Pose.Wave, new[] { "Oh, hello. You found the boardwalk. Everybody does, eventually.", "Down to the water and out along the pier. The café at the end is still open. Just.", "Mind the boards, some of them sing." });
            Mark(c, 19f);
            Shot(ctx, "the promenade", c + D * 14f - S * 9f + up * 2.4f, bs + up * 3f);
        }

        // ---------------------------------------------------------------- the Boardwalk
        void Boardwalk(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 17f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0) + up * .3f); if (pts.Count < 2) return;
            Strip(mk, pts, boards, WalkW, .25f, ctx); foreach (var p in pts) Remember(p, 1);
            for (var i = 0; i + 1 < pts.Count; i += 2) mk.Box(boardsDark, pts[i] + up * .29f, new Vector3(WalkW, .01f, .06f), Quaternion.LookRotation((pts[i + 1] - pts[i]).normalized));
            var n = 0; for (var d = 24f; d <= streetL - 4f; d += 10f, n++) { foreach (var e in new[] { -1f, 1f }) GlobeLamp(ctx, mk, G(ctx, d + (e > 0 ? 0f : 5f), e * (WalkW / 2f + .6f)), n % 2 == 0 && e > 0); }
            // shuttered stalls down one side; the carousel and the coaster down the other
            var i0 = 0; for (var d = 30f; d <= streetL - 10f; d += 9f, i0++) { var p = Wp(d, -(WalkW / 2f + 2.6f)); if (!ctx.IsLand(p.x, p.z, 3f) || InFootprint(ctx, p.x, p.z, 3f) || LaneNear(p.x, p.z, 3f)) continue; Stall(ctx, mk, new PlaceFrame(ctx.OnGround(p.x, p.z), S), i0); Mark(p, 2.4f); }
            var cd = Mathf.Min(streetL * .45f, 50f); var cc = Wp(cd, WalkW / 2f + 8f); if (ctx.IsLand(cc.x, cc.z, 6f) && !InFootprint(ctx, cc.x, cc.z, 7f)) Carousel(ctx, mk, solid, ctx.OnGround(cc.x, cc.z));
            var co = Wp(Mathf.Min(streetL * .55f, 130f), WalkW / 2f + 12f); var c0 = co - D * 24f; var c1 = co + D * 24f; if (ctx.IsLand(co.x, co.z, 10f) && ctx.IsLand(c0.x, c0.z, 4f) && ctx.IsLand(c1.x, c1.z, 4f) && !InFootprint(ctx, co.x, co.z, 12f) && !InNoGrass(co.x, co.z, 8f)) Coaster(ctx, mk, ctx.OnGround(co.x, co.z));
            var route = new List<Vector3>(); for (var d = 22f; d <= streetL - 4f; d += 14f) route.Add(G(ctx, d, 1.4f) + up * .3f); for (var d = streetL - 4f; d >= 22f; d -= 14f) route.Add(G(ctx, d, -1.4f) + up * .3f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "End of the boardwalk and back. Twice. Then home. Then I come back.", "I like it shut. When it's open you can't hear the sea." }, null, route.ToArray()).speed = .8f;
            Shot(ctx, "down the boardwalk", G(ctx, 19f, .6f) + up * 2.4f, G(ctx, streetL + 40f, 0) + up * 3f);
        }

        /// A carousel standing still under its striped roof; you can sit on a horse.
        void Carousel(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g)
        {
            var up = Vector3.up; var rng = new System.Random(9); mk.Prism(white, g, up, 5f, .5f, 16); solid.Prism(white, g - up * .3f, up, 5f, .8f, 12); mk.Prism(Paint(Rose), g + up * .5f, up, .5f, 3.6f, 10); mk.Prism(white, g + up * 3.9f, up, 5.4f, .4f, 16);
            for (var k = 0; k < 16; k++) { var a0 = k * 22.5f; var p0 = g + up * 4.3f + Quaternion.Euler(0, a0, 0) * Vector3.forward * 5.4f; var p1 = g + up * 4.3f + Quaternion.Euler(0, a0 + 22.5f, 0) * Vector3.forward * 5.4f; mk.Tri(k % 2 == 0 ? stripeA : stripeB, g + up * 6.2f, p0, p1, g); mk.Blob(lampM, p0 - up * .1f, Vector3.one * .1f, rng, 0f); }
            mk.Cone(Paint(Rose), g + up * 6.2f, .2f, .9f, 6);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; var hp = g + up * (1.3f + (k % 2) * .4f) + d * 3.6f; var side = Vector3.Cross(up, d); mk.Prism(Paint(new Color(.9f, .8f, .5f)), g + up * .5f + d * 3.6f, up, .04f, 3.4f, 5); var hm = Paint(Faded[k % Faded.Length]); mk.Box(hm, hp, new Vector3(.3f, .4f, 1f), Quaternion.LookRotation(side)); mk.Box(hm, hp + up * .4f + side * .5f, new Vector3(.24f, .6f, .3f), Quaternion.LookRotation(side) * Quaternion.Euler(-30f, 0, 0)); foreach (var q in new[] { -.3f, .3f }) mk.Prism(hm, hp + side * q, -up, .05f, .5f, 4); if (k % 2 == 0) Interact.Seat(tlife.transform, hp + up * .3f, side, "Sit on the horse").seatHeight = .3f; ArtistBits.PostCollider(ctx.Root, g + d * 3.6f, 3.4f, .3f); }
            ArtistBits.PostCollider(ctx.Root, g, 4f, .6f); halos.Add(g + up * 4.2f, 5f, new Color(1f, .8f, .7f), .12f); life.carousel = g; Mark(g, 6f);
            Shot(ctx, "the carousel", g + D * 9f - S * 4f + up * 2f, g + up * 2.4f);
        }

        /// A wooden rollercoaster: a hill of track on a lattice of posts, a car left at the top.
        void Coaster(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var dir = D; var side = S; Vector3 prev = default; var have = false; Vector3 top = g;
            for (var k = 0; k <= 30; k++)
            {
                var t = k / 30f; var h = 3f + Mathf.Sin(t * Mathf.PI * 2f) * 4f + Mathf.Sin(t * Mathf.PI) * 9f; var p = g + dir * (t * 44f - 22f) + up * h; if (h > (top - g).y) top = p;
                foreach (var e in new[] { -.6f, .6f }) if (have) mk.Prism(white, prev + side * e, (p - prev).normalized, .06f, Vector3.Distance(prev, p), 4);
                if (k % 2 == 0) { foreach (var e in new[] { -1.2f, 1.2f }) { var foot = ctx.OnGround((p + side * e).x, (p + side * e).z); mk.Prism(woodM, foot - up * .2f, up, .08f, p.y - foot.y + .2f, 4); } ArtistBits.PostCollider(ctx.Root, ctx.OnGround(p.x, p.z), Mathf.Min(4f, h), 1.3f); mk.Box(woodM, p - up * .2f, new Vector3(2.6f, .08f, .08f), Quaternion.LookRotation(dir)); if (k % 4 == 0) mk.Box(woodM, (p + ctx.OnGround(p.x, p.z)) / 2f, new Vector3(.06f, .06f, Mathf.Max(1f, h) * 1.2f), Quaternion.LookRotation(Vector3.Lerp(up, dir, .6f))); }
                if (k % 3 == 0) mk.Blob(k % 6 == 0 ? lampM : neonM, p + up * .1f + side * .7f, Vector3.one * .08f, new System.Random(k), 0f);
                prev = p; have = true;
            }
            mk.Box(Paint(Rose), top + up * .4f, new Vector3(1.4f, .6f, 1.8f), Quaternion.LookRotation(dir)); halos.Add(top + up * .6f, 2f, Neon, .2f); Mark(g, 14f);
            Shot(ctx, "the rollercoaster", g - dir * 26f + side * 14f + up * 2f, g + up * 8f);
        }

        // ---------------------------------------------------------------- the Pier
        void Pier(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!toSea) return;
            var up = Vector3.up; var deckY = waterY + 2.6f; var a = Wp(streetL - 2f, 0); var len = 70f; var rot = Quaternion.LookRotation(D); var rng = new System.Random(77);
            // the ramp from the boardwalk down to the deck, then the deck out over the water on posts
            var start = ctx.OnGround(a.x, a.z) + up * .3f; var deck0 = new Vector3(a.x, deckY, a.z) + D * 8f; var rampMid = (start + deck0) / 2f; var rampDir = deck0 - start;
            mk.Box(boards, rampMid, new Vector3(5f, .2f, rampDir.magnitude + .2f), Quaternion.LookRotation(rampDir.normalized)); solid.Box(boards, rampMid - up * .2f, new Vector3(5f, .5f, rampDir.magnitude + .2f), Quaternion.LookRotation(rampDir.normalized));
            var mid = deck0 + D * len / 2f; mk.Box(boards, mid, new Vector3(5f, .2f, len), rot); solid.Box(boards, mid - up * .3f, new Vector3(5f, .6f, len), rot);
            for (var d = 0f; d <= len; d += 4f) { foreach (var e in new[] { -2.3f, 2.3f }) { var pp = deck0 + D * d + S * e; mk.Prism(woodM, new Vector3(pp.x, waterY - 3f, pp.z), up, .14f, deckY - waterY + 3f, 5); mk.Prism(woodM, pp, up, .05f, 1f, 4); } mk.Box(woodM, deck0 + D * d + up * 1f, new Vector3(4.7f, .06f, .06f), Quaternion.LookRotation(S)); Remember(deck0 + D * d, 1); }
            foreach (var e in new[] { -2.4f, 2.4f }) { mk.Box(woodM, mid + S * e + up * 1f, new Vector3(.08f, .08f, len), rot); solid.Box(woodM, mid + S * e + up * .6f, new Vector3(.2f, 1.2f, len), rot); }
            for (var d = 6f; d <= len; d += 12f) GlobeLamp(ctx, mk, deck0 + D * d + S * 2.3f * ((int)(d / 12f) % 2 == 0 ? 1f : -1f), ((int)(d / 12f)) % 2 == 0);
            // people fishing; a telescope; a café at the end with its light on
            for (var k = 0; k < 3; k++) { var fp = deck0 + D * (14f + k * 16f) + S * 1.9f * (k % 2 == 0 ? 1f : -1f); Folk(fp, S * (k % 2 == 0 ? 1f : -1f), Resident.Pose.Sit, new[] { "Haven't caught a thing in three summers. That's not why I'm here.", "Listen. The water under the boards. Like breathing.", "Some nights the lights on the coaster come on by themselves. I've stopped asking." }, Local(Residents.Tool.Rod), null, .3f); }
            var ts = deck0 + D * 30f - S * 2f; mk.Prism(iron, ts, up, .05f, 1.2f, 5); mk.Prism(iron, ts + up * 1.2f, (D + up * .3f).normalized, .1f, .6f, 8, 1.3f); ArtistBits.PostCollider(ctx.Root, ts, 1.2f, .08f);
            string[] look = { "Through the telescope: the sun, halfway into the sea, not hurrying.", "A boat on the horizon with one light on. You watch it for a long time.", "You find the boardwalk from the outside, small and lit. It looks happier from here." }; var ln = 0;
            Spot("The telescope", ts + up * 1.3f, "Look through the telescope", new Vector3(.6f, .6f, .6f)).onUse = _ => Say(tlife.transform, ts + up * 2.2f, look[ln++ % look.Length], 4.5f);
            var cf = new PlaceFrame(deck0 + D * (len - 4f) + up * .1f, -D); mk.Box(boards, cf.P(0, 0, 0), new Vector3(10f, .2f, 8f), cf.r); solid.Box(boards, cf.P(0, -.3f, 0), new Vector3(10f, .6f, 8f), cf.r);
            mk.Box(Paint(Faded[1]), cf.P(0, 1.5f, -2f), new Vector3(6f, 3f, 3f), cf.r); solid.Box(woodM, cf.P(0, 1.5f, -2f), new Vector3(6f, 3f, 3f), cf.r); mk.GableRoof(Paint(Rose), cf.P(0, 3f, -2f), new Vector3(3.6f, 0, 6.6f), 1.2f, Quaternion.LookRotation(cf.Right)); mk.Box(windowM, cf.P(0, 1.5f, -.48f), new Vector3(4f, 1.4f, .04f), cf.r); mk.Box(neonM, cf.P(0, 3.3f, -.4f), new Vector3(3f, .3f, .05f), cf.r); Sign(cf.P(0, 3.3f, -.36f), cf.Fwd, "LAST CHANCE CAFÉ", .16f, Ink); halos.Add(cf.P(0, 1.5f, .5f), 3f, new Color(1f, .8f, .6f), .3f); ArtistBits.PointLight(ctx.Root, cf.P(0, 2.4f, 1f), new Color(1f, .8f, .6f), 12f, 2f);
            for (var k = 0; k < 2; k++) { var tp = cf.P(-2f + k * 4f, 0, 1.6f); mk.Prism(white, tp + up * .72f, up, .4f, .04f, 10); mk.Prism(iron, tp, up, .04f, .72f, 4); BoxCol(rootT, tp + up * .38f, new Vector3(.8f, .76f, .8f), cf.r); Chair(mk, tp + cf.Right * .7f, -cf.Right); Chair(mk, tp - cf.Right * .7f, cf.Right); }
            Folk(cf.P(1.2f, 0, -.2f), cf.Fwd, Resident.Pose.Stand, new[] { "Coffee's terrible and the view's free. Fair trade.", "We close when the sun's gone. It never quite goes.", "Table by the rail. Everybody wants the table by the rail." }, Local(Residents.Tool.Mug, true));
            life.pierEnd = deck0 + D * len + up * 1f;
            Shot(ctx, "out on the pier", deck0 + D * 4f + up * 1.8f, deck0 + D * len + up * 2f);
            Shot(ctx, "the pier at sunset", deck0 + D * (len - 1f) + S * 4.4f + up * 2f, deck0 + D * (len + 60f) + S * 4.4f + up * 3f);
        }

        // ---------------------------------------------------------------- the Heartbreak Motel
        void Motel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(55); Vector3 at = default; var ok = false;
            foreach (var (dd, ss) in new[] { (30f, 30f), (-30f, -30f), (-30f, 32f), (40f, -34f), (10f, 40f) }) { var p = Wp(dd, ss); if (ctx.IsLand(p.x, p.z, 16f) && !InFootprint(ctx, p.x, p.z, 16f) && !LaneNear(p.x, p.z, 13f) && !InNoGrass(p.x, p.z, 4f)) { at = ctx.OnGround(p.x, p.z); ok = true; break; } }
            if (!ok) return;
            var face = (center - at); face.y = 0; var f = new PlaceFrame(at, face.normalized); const float W = 20f, Dp = 6f;
            mk.Box(Paint(Faded[0]), f.P(0, 2.8f, -1f), new Vector3(W, 5.6f, Dp), f.r); solid.Box(Paint(Faded[0]), f.P(0, 2.8f, -1f), new Vector3(W, 5.6f, Dp), f.r); mk.Box(white, f.P(0, 5.7f, -1f), new Vector3(W + .4f, .2f, Dp + .4f), f.r);
            mk.Box(boards, f.P(0, 2.9f, 2.6f), new Vector3(W, .16f, 1.4f), f.r); for (var k = 0; k <= 10; k++) { mk.Prism(white, f.P(-W / 2f + k * W / 10f, 0, 3.2f), Vector3.up, .06f, 3.8f, 4); } mk.Box(white, f.P(0, 3.8f, 3.25f), new Vector3(W, .06f, .06f), f.r); ArtistBits.PostCollider(rootT, f.P(-W / 2f, 0, 3.2f), 3.6f, .08f); ArtistBits.PostCollider(rootT, f.P(W / 2f, 0, 3.2f), 3.6f, .08f);
            for (var fl = 0; fl < 2; fl++) for (var k = 0; k < 6; k++) { var x = -W / 2f + 1.7f + k * 3.3f; mk.Box(Paint(Faded[(k + 2) % Faded.Length]), f.P(x, 1.1f + fl * 2.9f, 2.01f), new Vector3(1f, 2f, .04f), f.r); mk.Box(k % 3 == 1 ? windowM : Paint(new Color(.3f, .32f, .4f)), f.P(x + 1.2f, 1.5f + fl * 2.9f, 2.01f), new Vector3(1f, .9f, .04f), f.r); ArtistBits.Label(rootT, (fl * 100 + k + 1).ToString(), f.P(x, 1.9f + fl * 2.9f, 2.05f), f.Fwd, .07f, Ink, TextAnchor.MiddleCenter); }
            // the sign on its pole: HEARTBREAK MOTEL, half the letters out; VACANCY lit
            var sp = f.P(W / 2f + 3f, 0, 4f); mk.Prism(iron, sp - up * .2f, up, .14f, 8f, 6); ArtistBits.PostCollider(rootT, sp, 7.6f, .16f); mk.Box(Paint(Violet), sp + up * 7.6f, new Vector3(.4f, 2.8f, 4.6f), f.r * Quaternion.Euler(0, 90f, 0)); Sign(sp + up * 8.2f + f.Fwd * .0f + f.Right * .22f, f.Right, "HEARTBRE K", .4f, Neon); Sign(sp + up * 7.5f + f.Right * .22f, f.Right, "M TEL", .4f, Neon); mk.Box(neonM, sp + up * 6.6f + f.Right * .22f, new Vector3(.04f, .4f, 3f), f.r * Quaternion.Euler(0, 90f, 0)); Sign(sp + up * 6.6f + f.Right * .25f, f.Right, "VACANCY", .2f, Ink); halos.Add(sp + up * 7.6f, 3f, Neon, .3f);
            // the empty pool, a deck chair, an inflatable left in the deep end
            var pl = f.P(0, 0, 8.4f); mk.Box(white, pl - up * .1f, new Vector3(10f, .3f, 5f), f.r); mk.Box(Paint(new Color(.56f, .76f, .82f)), pl - up * .05f, new Vector3(9f, .3f, 4f), f.r); BoxCol(rootT, pl + up * .1f, new Vector3(10f, .4f, 5f), f.r); mk.Prism(Paint(Rose), pl + up * .15f + f.Right * 2f, Vector3.up, .5f, .2f, 12); mk.Prism(Paint(new Color(.56f, .76f, .82f)), pl + up * .15f + f.Right * 2f, Vector3.up, .25f, .22f, 10);
            foreach (var e in new[] { -4f, 4.6f }) { var dc = pl + f.Right * e + f.Fwd * 3.4f; mk.Box(stripeA, dc + up * .3f, new Vector3(.7f, .06f, 1.6f), f.r * Quaternion.Euler(-10f, 0, 0)); mk.Box(stripeB, dc + up * .7f - f.Fwd * .7f, new Vector3(.7f, .06f, .8f), f.r * Quaternion.Euler(-55f, 0, 0)); Interact.Seat(tlife.transform, dc + up * .4f, f.Fwd, "Lie back").seatHeight = .3f; }
            Folk(f.P(-3f, 0, 4.4f), f.Fwd, Resident.Pose.Lean, new[] { "Room 7's free. Room 7's always free. Don't ask about room 7.", "The pool's been empty since the summer someone cried in it. Kidding. Mostly.", "Check-out's at noon. Nobody checks out." }, Local(Residents.Tool.Broom, true));
            ArtistBits.PointLight(rootT, f.P(0, 3f, 6f), new Color(1f, .5f, .7f), 18f, 1.6f); Mark(at, 15f);
            Shot(ctx, "the heartbreak motel", f.P(-6f, 2f, 16f), f.P(2f, 4f, 0));
        }

        /// Faded beach houses down the Boardwalk's other side, built after the lanes so nothing stands in one.
        void BeachHouses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var rng = new System.Random(12);
            for (var d = 32f; d <= streetL - 8f; d += 11f, i++)
            {
                var p = Wp(d, WalkW / 2f + 7f); if (!ctx.IsLand(p.x, p.z, 6f) || InFootprint(ctx, p.x, p.z, 6f) || LaneNear(p.x, p.z, 6f) || InNoGrass(p.x, p.z, 4.5f)) continue;
                BeachHouse(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S), i, rng); built++;
            }
            Debug.Log($"The Heartbreak Boardwalk: {built} beach houses on the Boardwalk");
        }

        /// A faded clapboard beach house on short stilts: a porch with a swing, one lit window.
        void BeachHouse(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i, System.Random rng)
        {
            var up = Vector3.up; float W = 6.4f, Dp = 5.4f, H = 3.4f; var wall = Paint(Faded[i % Faded.Length]);
            foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Prism(woodM, f.P(x * (W / 2f - .3f), -.4f, zz * (Dp / 2f - .3f)), up, .12f, 1.2f, 5);
            mk.Box(wall, f.P(0, .8f + H / 2f, 0), new Vector3(W, H, Dp), f.r); solid.Box(wall, f.P(0, .2f + H / 2f, 0), new Vector3(W, H + 1.2f, Dp), f.r); for (var k = 0; k < 8; k++) mk.Box(Paint(Faded[i % Faded.Length] * .9f), f.P(0, 1f + k * .42f, Dp / 2f + .01f), new Vector3(W, .04f, .02f), f.r);
            mk.GableRoof(Paint(new Color(.5f, .44f, .5f)), f.P(0, .8f + H, 0), new Vector3(Dp + .8f, 0, W + .8f), 1.6f, Quaternion.LookRotation(f.Right));
            mk.Box(boards, f.P(0, .8f, Dp / 2f + .9f), new Vector3(W, .12f, 1.8f), f.r); solid.Box(boards, f.P(0, .4f, Dp / 2f + .9f), new Vector3(W, .8f, 1.8f), f.r); for (var k = 0; k < 2; k++) { mk.Box(boards, f.P(-W * .25f, .2f + k * .3f, Dp / 2f + 1.9f + (1 - k) * .3f), new Vector3(1.4f, .1f, .34f), f.r); }
            mk.Box(woodM, f.P(-W * .25f, 1.85f, Dp / 2f + .06f), new Vector3(.9f, 2f, .04f), f.r); mk.Box(i % 2 == 0 ? windowM : Paint(new Color(.32f, .34f, .44f)), f.P(W * .22f, 2.2f, Dp / 2f + .06f), new Vector3(1.4f, 1f, .04f), f.r); if (i % 2 == 0) halos.Add(f.P(W * .22f, 2.2f, Dp / 2f + .5f), 1.2f, new Color(1f, .78f, .5f), .3f);
            // the porch swing
            var sw = f.P(W * .22f, .9f, Dp / 2f + 1.2f); mk.Box(woodM, sw + up * .5f, new Vector3(1.6f, .06f, .5f), f.r); mk.Box(woodM, sw + up * .8f - f.Fwd * .24f, new Vector3(1.6f, .5f, .05f), f.r); foreach (var e in new[] { -.75f, .75f }) mk.Prism(dark, sw + up * .5f + f.Right * e, up, .01f, 1.8f, 3); Interact.Seat(tlife.transform, sw + up * .55f, f.Fwd, "Sit on the swing").seatHeight = .5f;
            if (i % 3 == 0) Folk(f.P(-W * .3f, .86f, Dp / 2f + 1f), f.Fwd, Resident.Pose.Lean, new[] { "Lived here forty summers. The paint gave up before I did.", "The wind brings the music from the motel. Same three songs. I don't mind.", "Evening. Sit on the swing if you want. It squeaks in a nice key." });
            Mark(f.o, 5.4f);
        }
    }

    /// The Heartbreak Boardwalk's holder for things you can use and carry.
    public class HeartTownLife : MonoBehaviour { }
}
