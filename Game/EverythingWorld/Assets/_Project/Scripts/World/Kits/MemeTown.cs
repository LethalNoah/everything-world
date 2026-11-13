using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Isle of Memes' places (part of MemeKit): the Homepage (a checkered marble plaza under a giant glossy
    /// orb, a pink marble bust looking out over it, an aqua fountain throwing up bubbles, a loading bar arching over the
    /// way out, a cursor as tall as a house, a hit counter); the Superhighway (a road of glass with old desktop icons
    /// floating along it); and the Dream Mall (an open atrium ringed with columns, a fountain, palms in planters, a
    /// food court, and a little theatre with red curtains for the musicals).
    public partial class MemeKit
    {
        MemeTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(1f, .6f, .8f), new Color(.4f, .86f, .86f), new Color(.96f, .94f, .96f), new Color(.7f, .6f, 1f), new Color(1f, .86f, .4f), new Color(.3f, .3f, .5f), new Color(.5f, .9f, .6f) };
        static readonly Color[] Bottoms = { new Color(.3f, .3f, .5f), new Color(.96f, .94f, .96f), new Color(.6f, .5f, .8f), new Color(.2f, .2f, .3f) };
        static readonly string[] TownTalk =
        {
            "Welcome to the internet. Well, a nice bit of it. The bits we kept.", "The Dream Mall is open. It's always open. Nobody's ever found the exit.", "Do you remember this one? Of course you do. Everybody does.",
            "I've been here since the dial-up days. The noise still haunts me. Fondly.", "If you see a door in a field, go through it. Or don't. Both are fine.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2000);
            tlife = new GameObject("Meme town life").AddComponent<MemeTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Beanie }, new[] { Pink, Teal, Lilac, Sunny }); l.tool = tool; l.apron = apron; return l; }
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

        /// A bench to sit on, glossy.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(marble, g + up * .22f, new Vector3(len, .44f, .5f), rot); mk.Box(Paint(Teal), g + up * .46f, new Vector3(len + .04f, .04f, .54f), rot); mk.Box(marble, g + up * .7f - face * .24f, new Vector3(len, .5f, .1f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
        }

        // ---------------------------------------------------------------- the Homepage
        void Homepage(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(marble, c - up * .28f, up, 20f, .42f, 32); Checker(mk, c + up * .12f, Quaternion.LookRotation(D), 14, 2.4f, .03f); for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            // the giant glossy orb, hovering, turning slowly
            var ok = new MeshKit(); ok.Blob(bubbleM, Vector3.zero, Vector3.one * 4.6f, rng, 0f); ok.Blob(white, new Vector3(-1.6f, 2.6f, -1.2f), new Vector3(1.4f, .7f, 1.4f), rng, 0f); for (var k = 0; k < 8; k++) ok.Box(Paint(Pink), Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 4.8f, new Vector3(.4f, .4f, .1f), Quaternion.Euler(0, k * 45f, 0)); var orb = ok.BuildSingle("The glossy orb", ctx.Root, false); orb.transform.position = c + up * 14f; life.orb = orb.transform; life.orbAt = c + up * 14f; halos.Add(c + up * 14f, 8f, Aqua, .12f);
            // the bust in the middle, pink-haired, on a tall plinth; the aqua fountain round it
            mk.Prism(marble, c + up * .14f, up, 4f, .6f, 20); mk.Prism(aquaM, c + up * .7f, up, 3.7f, .02f, 20); solid.Prism(marble, c - up * .2f, up, 4f, 1f, 12); Bust(ctx, mk, c + up * .7f, D, 2f, rng); life.fountain = c + up * 1.2f;
            for (var k = 0; k < 6; k++) Bubble(mk, c + up * (2f + k * .6f) + Quaternion.Euler(0, k * 60f, 0) * D * 2.6f, .2f + k * .05f);
            // the loading bar, arched over the way to the Superhighway
            var lb = c + D * 17f; foreach (var e in new[] { -4f, 4f }) { mk.Prism(chrome, lb + S * e - up * .2f, up, .2f, 6f, 8); ArtistBits.PostCollider(ctx.Root, lb + S * e, 5.8f, .22f); }
            mk.Box(white, lb + up * 5.6f, new Vector3(8.4f, 1.2f, .3f), Quaternion.LookRotation(D)); mk.Box(dark, lb + up * 5.6f - D * .16f, new Vector3(8f, .8f, .02f), Quaternion.LookRotation(D)); for (var k = 0; k < 15; k++) mk.Box(k < 14 ? GlowM(new Color(.3f, .9f, .4f), 1.2f) : dark, lb + up * 5.6f - D * .18f + S * (-3.75f + k * .5f), new Vector3(.44f, .64f, .02f), Quaternion.LookRotation(D)); Sign(lb + up * 6.5f - D * .17f, -D, "LOADING... 99%", .32f, Ink);
            // a cursor as tall as a house; a hit counter on a post
            var cu = c - D * 9f + S * 11f; var cm = Paint(Color.white); var cdir = (D + S * .3f).normalized; var cr = Quaternion.LookRotation(cdir); var cu0 = cu + up * 6.4f; mk.Tri(cm, cu0, cu0 - up * 5f, cu0 - up * 3.6f + cr * Vector3.right * 2.2f, cu0 - cdir); mk.Tri(cm, cu0, cu0 - up * 5f, cu0 - up * 3.6f + cr * Vector3.right * 2.2f, cu0 + cdir); mk.Box(cm, cu0 - up * 4.4f + cr * Vector3.right * .9f, new Vector3(.4f, 2.4f, .1f), cr * Quaternion.Euler(0, 0, 40f)); mk.Box(dark, cu + up * 1.6f, new Vector3(.2f, 3.2f, .2f), cr); ArtistBits.PostCollider(ctx.Root, cu, 3f, .3f); halos.Add(cu0 - up * 2.4f, 3f, Color.white, .14f);
            var hc = new PlaceFrame(c + D * 9f + S * 12f + up * .14f, (-D - S).normalized); mk.Prism(chrome, hc.P(0, -.2f, 0), up, .06f, 2.4f, 5); mk.Box(dark, hc.P(0, 2.4f, 0), new Vector3(1.6f, .5f, .2f), hc.r); for (var k = 0; k < 6; k++) mk.Box(GlowM(new Color(.3f, 1f, .4f), 1.2f), hc.P(-.6f + k * .24f, 2.4f, .11f), new Vector3(.18f, .32f, .01f), hc.r); Sign(hc.P(0, 2.9f, .11f), hc.Fwd, "YOU ARE VISITOR", .1f, Ink); ArtistBits.Label(rootT, "000001", hc.P(0, 2.4f, .13f), hc.Fwd, .16f, new Color(.2f, .2f, .2f), TextAnchor.MiddleCenter); ArtistBits.PostCollider(ctx.Root, hc.o, 2.4f, .08f);
            string[] count = { "You click the counter. It goes up by one. You are the first visitor. You were always the first visitor.", "Congratulations! You are the one millionth visitor! (You are not.)", "The counter flips over to 000000. Somewhere a webmaster weeps." }; var cn = 0;
            Spot("The hit counter", hc.P(0, 2f, .5f), "Click the counter", new Vector3(1.6f, 1f, .8f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, hc.P(0, 3.6f, .5f), count[cn++ % count.Length], 4.5f); };
            // palms, busts and benches round the edge
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * D; if (Vector3.Angle(d, D) < 30f) continue; if (k % 2 == 0) Palm(ctx, mk, c + d * 19f, 1.2f, rng); else GlobeLamp(ctx, mk, c + d * 18.6f + up * .14f, true); }
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * D; Bench(ctx.Root, mk, c + d * 9f + up * .15f, -d); }
            Folk(c + Quaternion.Euler(0, 45f, 0) * D * 9f + up * .15f, -(Quaternion.Euler(0, 45f, 0) * D), Resident.Pose.Sit, new[] { "I come here to remember the good videos. They were all the good videos.", "Somebody made a song about a duck asking for grapes and I think about it every day.", "Everything's a bit pink here. You stop noticing. Then you start again." }, Local());
            Folk(c + D * 5f + S * 2f + up * .15f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome! Mind the bubbles, they pop if you look at them wrong.", "The Superhighway goes to the Dream Mall. Follow the loading bar. It's nearly done.", "Every song here is either a joke or a musical. Some of them are both." });
            Mark(c, 21f);
            Shot(ctx, "the homepage", c + D * 15f + S * 10f + up * 2.6f, c + up * 8f);
        }

        // ---------------------------------------------------------------- the Superhighway
        void Superhighway(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, glassM, RoadW, .13f, ctx); foreach (var e in new[] { -1f, 1f }) { var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(p + S * e * (RoadW / 2f - .2f)); Strip(mk, edge, aquaM, .2f, .15f, ctx); } foreach (var p in pts) Remember(p, 1);
            // old desktop icons floating along it, bobbing
            string[] icons = { "folder", "mail", "hourglass", "bin", "note", "globe", "disk" }; var n = 0;
            for (var d = 26f; d <= streetL - 4f; d += 13f, n++)
            {
                var e = n % 2 == 0 ? 1f : -1f; var ip = G(ctx, d, e * (RoadW / 2f + 3f)) + up * 3f; var ik = new MeshKit(); Icon(ik, icons[n % icons.Length]); var go = ik.BuildSingle("Icon: " + icons[n % icons.Length], ctx.Root, false); go.transform.SetPositionAndRotation(ip, Quaternion.LookRotation(-S * e)); life.icons.Add((go.transform, ip));
                GlobeLamp(ctx, mk, G(ctx, d + 6f, -e * (RoadW / 2f + 1f)), n % 2 == 0);
            }
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 14f) route.Add(G(ctx, d, 1.4f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 14f) route.Add(G(ctx, d, -1.4f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Browsing. Just browsing. On foot.", "I'm downloading a song. On foot. It takes a while." }, null, route.ToArray()).speed = .9f;
            Shot(ctx, "along the superhighway", G(ctx, 21f, .4f) + up * 2.2f, G(ctx, streetL + 30f, 0) + up * 6f);
        }

        /// An old desktop icon, about a metre and a half across, glossy.
        void Icon(MeshKit mk, string kind)
        {
            var up = Vector3.up; var f = Vector3.forward; var r = Vector3.right; var rng = new System.Random(kind.Length);
            switch (kind)
            {
                case "folder": mk.Box(Paint(new Color(1f, .84f, .4f)), Vector3.zero, new Vector3(1.6f, 1.1f, .14f), Quaternion.identity); mk.Box(Paint(new Color(1f, .8f, .34f)), up * .62f - r * .4f, new Vector3(.7f, .2f, .14f), Quaternion.identity); break;
                case "mail": mk.Box(white, Vector3.zero, new Vector3(1.6f, 1f, .1f), Quaternion.identity); mk.Tri(Paint(new Color(.8f, .8f, .86f)), up * .5f - r * .8f, up * .5f + r * .8f, -up * .1f, -f); mk.Tri(Paint(new Color(.8f, .8f, .86f)), up * .5f - r * .8f, up * .5f + r * .8f, -up * .1f, f); break;
                case "hourglass": mk.Prism(Paint(Sunny), -up * .7f, up, .5f, .1f, 10); mk.Prism(Paint(Sunny), up * .6f, up, .5f, .1f, 10); mk.Prism(glassM, -up * .6f, up, .4f, .6f, 10, .1f); mk.Prism(glassM, up * .6f, -up, .4f, .6f, 10, .1f); mk.Cone(Paint(new Color(1f, .9f, .6f)), -up * .6f, .3f, .3f, 8); break;
                case "bin": mk.Prism(glassM, -up * .7f, up, .5f, 1.3f, 12, 1.2f); mk.Prism(Paint(Teal), up * .6f, up, .64f, .1f, 12); for (var k = 0; k < 3; k++) mk.Box(white, -up * .2f + Quaternion.Euler(0, k * 120f, 0) * f * .3f, new Vector3(.2f, .3f, .02f), Quaternion.Euler(k * 20f, k * 120f, 0)); break;
                case "note": mk.Blob(Paint(Pink), -up * .5f, new Vector3(.36f, .26f, .2f), rng, 0f); mk.Prism(Paint(Pink), -up * .5f + r * .3f, up, .06f, 1.4f, 6); mk.Box(Paint(Pink), up * .8f + r * .55f, new Vector3(.5f, .2f, .1f), Quaternion.Euler(0, 0, -20f)); break;
                case "globe": mk.Blob(aquaM, Vector3.zero, Vector3.one * .7f, rng, 0f); for (var k = 0; k < 3; k++) mk.Blob(Paint(Bliss), Quaternion.Euler(k * 50f, k * 80f, 0) * f * .6f, new Vector3(.3f, .24f, .14f), rng, .3f); break;
                default: mk.Box(Paint(new Color(.3f, .3f, .5f)), Vector3.zero, new Vector3(1.3f, 1.3f, .1f), Quaternion.identity); mk.Box(white, up * .3f, new Vector3(.8f, .6f, .12f), Quaternion.identity); mk.Box(chrome, -up * .4f + r * .2f, new Vector3(.5f, .4f, .12f), Quaternion.identity); break;
            }
            mk.Blob(white, up * .4f - r * .4f + f * .1f, new Vector3(.3f, .1f, .04f), rng, 0f);
        }

        // ---------------------------------------------------------------- the Dream Mall
        void DreamMall(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var mc = G(ctx, streetL + 18f, 0); var rot = Quaternion.LookRotation(-D); var rng = new System.Random(88);
            mk.Prism(marble, mc - up * .28f, up, 20f, .42f, 32); Checker(mk, mc + up * .12f, rot, 12, 2.4f, .03f); for (var k = 0; k < 12; k++) Remember(mc + Quaternion.Euler(0, k * 30f, 0) * D * 18f, 1); Remember(mc - D * 8f, 1);
            // a ring of columns holding up a pink lintel, open to the sky; gaps on the way in
            for (var k = 0; k < 20; k++) { var d = Quaternion.Euler(0, k * 18f, 0) * D; if (Vector3.Angle(d, -D) < 20f) continue; Column(ctx, mk, mc + d * 16f + up * .14f, 5f, false); var d2 = Quaternion.Euler(0, (k + 1) * 18f, 0) * D; if (Vector3.Angle(d2, -D) < 20f) continue; var a = mc + d * 16f + up * 6f; var b = mc + d2 * 16f + up * 6f; mk.Box(Paint(Pink), (a + b) / 2f, new Vector3(Vector3.Distance(a, b) + .6f, .6f, 1f), Quaternion.LookRotation((b - a).normalized) * Quaternion.Euler(0, 90f, 0)); }
            var sg = mc - D * 15.6f + up * 7f; mk.Box(dark, sg, new Vector3(9f, 1.6f, .3f), rot); mk.Box(GlowM(Pink, 1.6f), sg + up * .76f - D * .16f, new Vector3(9f, .06f, .02f), rot); mk.Box(GlowM(Teal, 1.6f), sg - up * .76f - D * .16f, new Vector3(9f, .06f, .02f), rot); Sign(sg - D * .17f, -D, "DREAM MALL", .8f, Pink); foreach (var e in new[] { -4.4f, 4.4f }) mk.Prism(chrome, sg + S * e - up * 7f, up, .14f, 6.2f, 6);
            // the fountain in the middle, bubbles rising; palms in planters; benches
            mk.Prism(marble, mc + up * .14f, up, 4f, .6f, 20); mk.Prism(aquaM, mc + up * .7f, up, 3.7f, .02f, 20); solid.Prism(marble, mc - up * .2f, up, 4f, 1f, 12); mk.Prism(marble, mc + up * .7f, up, .5f, 2.6f, 10); mk.Prism(marble, mc + up * 3.2f, up, 1.4f, .2f, 14); mk.Prism(aquaM, mc + up * 3.36f, up, 1.3f, .02f, 14); life.fountain2 = mc + up * 3.6f;
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * D; var pp = mc + d * 9f + up * .14f; mk.Box(marble, pp + up * .5f, new Vector3(1.6f, 1f, 1.6f), Quaternion.identity); Col(pp + up * .5f, new Vector3(1.6f, 1f, 1.6f), Quaternion.identity); Palm(ctx, mk, pp + up * 1f, 1f, rng); Bench(ctx.Root, mk, pp + Vector3.Cross(up, d) * 2.4f, -d, 2f); }
            // the food court: tables, a sign, a tray of food to carry
            var fc = new PlaceFrame(mc + S * 9f + up * .14f, -S); for (var k = 0; k < 2; k++) { var tp = fc.P(-1.6f + k * 3.2f, 0, 0); mk.Prism(white, tp + up * .72f, up, .6f, .04f, 14); mk.Prism(chrome, tp, up, .05f, .72f, 5); Col(tp + up * .38f, new Vector3(1.2f, .76f, 1.2f), fc.r); foreach (var e in new[] { -1f, 1f }) { var sp = tp + fc.Right * e * 1f; mk.Prism(Paint(Teal), sp + up * .45f, up, .22f, .06f, 10); mk.Prism(chrome, sp, up, .03f, .45f, 4); Interact.Seat(tlife.transform, sp + up * .52f, -fc.Right * e, "Sit in the food court"); } }
            var tray = new MeshKit(); tray.Box(Paint(new Color(.9f, .4f, .5f)), new Vector3(0, .02f, 0), new Vector3(.44f, .03f, .32f), Quaternion.identity); tray.Box(Paint(new Color(.9f, .7f, .4f)), new Vector3(-.1f, .07f, 0), new Vector3(.16f, .08f, .16f), Quaternion.identity); tray.Prism(Paint(new Color(.9f, .2f, .2f)), new Vector3(.12f, .04f, 0), Vector3.up, .05f, .16f, 8); Thing(tray, "Food tray", "a tray: a burger, a cola, a very small toy", fc.P(-1.6f, .76f, 0), fc.r, new Vector3(.46f, .2f, .34f), .4f, false);
            Folk(fc.P(0, 0, -1.6f), fc.Fwd, Resident.Pose.Stand, new[] { "Welcome to the food court. Everything is a slightly different shade of pink.", "We've been closing in five minutes since 1998.", "The music in here is the music you hear in dreams about shopping." }, Local(Residents.Tool.None, true));
            // the little theatre for the musicals: a stage with red curtains and footlights
            var th = new PlaceFrame(mc - S * 9f + up * .14f, S); mk.Box(woodM(), th.P(0, .45f, 0), new Vector3(6f, .9f, 4f), th.r); solid.Box(woodM(), th.P(0, .35f, 0), new Vector3(6f, 1.1f, 4f), th.r); for (var k = 0; k < 8; k++) mk.Blob(lampM, th.P(-2.6f + k * .74f, .96f, 1.9f), Vector3.one * .1f, rng, 0f);
            mk.Box(Paint(new Color(.66f, .1f, .16f)), th.P(0, 3.6f, -1.8f), new Vector3(6.4f, 5.4f, .2f), th.r); solid.Box(woodM(), th.P(0, 3.6f, -1.8f), new Vector3(6.4f, 5.4f, .2f), th.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(Paint(new Color(.8f, .14f, .2f)), th.P(e * 2.6f, 3.4f, -1.5f), new Vector3(1.4f, 5f, .14f), th.r); mk.Box(Paint(Sunny), th.P(e * 3.2f, 3.4f, -.8f), new Vector3(.3f, 5f, .3f), th.r); ArtistBits.PostCollider(rootT, th.P(e * 3.2f, .9f, -.8f), 5f, .2f); } mk.Box(Paint(Sunny), th.P(0, 6.1f, -.8f), new Vector3(6.8f, .6f, .3f), th.r); Sign(th.P(0, 6.1f, -.64f), th.Fwd, "TONIGHT: THE MUSICAL", .3f, Ink);
            string[] sing = { "You step into the footlights and belt out the big number. Somebody in the food court stands up and applauds.", "Jazz hands! The curtain sways. You are fabulous, and you know it.", "The key change hits. You go up with it. The whole mall gasps.", "Bow. Bow again. One more bow. They're still clapping." }; var sn = 0;
            Spot("The theatre stage", th.P(0, 1.8f, .6f), "Sing the big number", new Vector3(5f, 1.6f, 3f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, th.P(0, 3.4f, 1.2f), sing[sn++ % sing.Length], 4.5f); };
            ArtistBits.PointLight(ctx.Root, th.P(0, 3f, 2.4f), new Color(1f, .86f, .7f), 14f, 2f); ArtistBits.PointLight(ctx.Root, mc + up * 6f, new Color(1f, .86f, 1f), 28f, 1.6f);
            for (var k = 0; k < 4; k++) Folk(mc + Quaternion.Euler(0, k * 90f + 20f, 0) * D * 5.4f + up * .14f, Quaternion.Euler(0, k * 90f + 20f, 0) * -D, k % 2 == 0 ? Resident.Pose.Stand : Resident.Pose.Wave, new[] { "Have you been to the theatre yet? Tonight's musical has been tonight for years.", "I came for one thing and I can't remember what it was. That's malls.", "Listen to the fountain. It's on loop." });
            Mark(mc, 20f);
            Shot(ctx, "the dream mall", mc - D * 20f + S * 4f + up * 2.4f, mc + up * 3f);
        }

        Material woodM() => Paint(new Color(.56f, .4f, .32f));
    }

    /// The Isle of Memes' holder for things you can use and carry.
    public class MemeTownLife : MonoBehaviour { }
}
