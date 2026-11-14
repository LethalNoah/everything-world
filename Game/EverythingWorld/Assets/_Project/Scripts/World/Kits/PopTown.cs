using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Pop Republic's places (part of PopKit): the Plaza (the great fountain, billboards all round, confetti
    /// cannons, a photo frame to stand in, an ice-cream cart); the Red Carpet (velvet ropes on gold posts, a star in
    /// the pavement for every artist, photographers); the Palace (a stage under an arch of lights, a microphone to
    /// take, a crowd, and a ferris wheel turning behind); K-Pop Square (an idol stage, light sticks to carry); and
    /// candy-coloured houses down both sides of the carpet.
    public partial class PopKit
    {
        PopTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(1f, .6f, .78f), new Color(.4f, .8f, .94f), new Color(1f, .9f, .4f), new Color(.98f, .97f, .96f), new Color(.7f, .56f, .96f), new Color(.2f, .2f, .26f), new Color(1f, .5f, .4f) };
        static readonly Color[] Bottoms = { new Color(.24f, .3f, .5f), new Color(.98f, .97f, .96f), new Color(.2f, .2f, .26f), new Color(1f, .7f, .84f) };
        static readonly string[] TownTalk =
        {
            "You know this one! Everybody knows this one!", "The Palace is at the end of the Red Carpet. Walk it like they're all shouting your name.", "It's always a bright day here. Nobody's ever checked what happens after.",
            "I've seen every show. I'm going again tonight. I'm going again tomorrow.", "Sing it! Louder! You're allowed! It's the law here!",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2008);
            tlife = new GameObject("Pop town life").AddComponent<PopTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None }, new[] { Pink, Cyan, Lemon, White }); l.tool = tool; l.apron = apron; return l; }
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

        /// A microphone on a stand.
        void Mic(MeshKit mk, Vector3 g, Material head)
        {
            var up = Vector3.up; mk.Prism(chrome, g, up, .02f, 1.5f, 4); mk.Prism(chrome, g, up, .18f, .02f, 8); mk.Blob(head, g + up * 1.56f, new Vector3(.05f, .08f, .05f), new System.Random(2), 0f);
        }

        /// A light stick to carry, its head glowing in a group's colour.
        void LightStick(Vector3 at, Color c, string label)
        {
            var m = new MeshKit(); m.Prism(white, Vector3.zero, Vector3.up, .02f, .22f, 6); m.Blob(GlowM(c, 1.8f), new Vector3(0, .3f, 0), new Vector3(.08f, .1f, .08f), new System.Random(1), 0f); Thing(m, "Light stick", label, at, Quaternion.identity, new Vector3(.16f, .4f, .16f), .2f, false);
        }

        // ---------------------------------------------------------------- the Plaza
        void ThePlaza(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(paving, c - up * .28f, up, 20f, .42f, 32); mk.Prism(pavingPink, c + up * .14f, up, 13f, .012f, 32); mk.Prism(paving, c + up * .16f, up, 12.2f, .012f, 32);
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * D; mk.Box(k % 2 == 0 ? Paint(Lemon) : Paint(Cyan), c + up * .18f + d * 8f, new Vector3(.5f, .02f, 7f), Quaternion.LookRotation(d)); }
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            // the great fountain
            Fountain(ctx, mk, c + up * .17f, 1.6f); Folk(c + D * 6f - S * 2f + up * .19f, -D, Resident.Pose.Wave, new[] { "Welcome to the Pop Republic! The chorus is coming, get ready!", "Red Carpet's that way, straight to the Palace. Taylor's hall is just there, you'll hear it.", "Throw a coin in the fountain. Wish for the setlist you want." });
            // billboards round the edge, confetti cannons between them
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * D; Billboard(ctx, mk, c + d * 22f + up * .14f, -d, k); }
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 15f, 0) * D; Cannon(ctx, mk, c + d * 16.5f + up * .14f, -d, k); }
            // star lamps, strings of coloured bulbs between them
            var posts = new Vector3[8]; for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * D; posts[k] = c + d * 18.6f + up * .14f; StarLamp(ctx, mk, posts[k], k, k % 2 == 0); } for (var k = 0; k < 8; k++) if (k != 0) Bulbs(mk, posts[k] + up * 4.3f, posts[(k + 1) % 8] + up * 4.3f, k);
            // the photo frame: a giant golden frame to stand in, with a step
            var pf = new PlaceFrame(c - D * 12f + S * 9f + up * .14f, (D - S * .6f).normalized); mk.Box(white, pf.P(0, .1f, 0), new Vector3(3.4f, .2f, 1.4f), pf.r); solid.Box(white, pf.P(0, -.1f, 0), new Vector3(3.4f, .6f, 1.4f), pf.r);
            foreach (var e in new[] { -1.5f, 1.5f }) { mk.Box(goldM, pf.P(e, 1.6f, -.4f), new Vector3(.24f, 3f, .24f), pf.r); Col(pf.P(e, 1.6f, -.4f), new Vector3(.24f, 3f, .24f), pf.r); } mk.Box(goldM, pf.P(0, 3.1f, -.4f), new Vector3(3.24f, .24f, .24f), pf.r); Sign(pf.P(0, 3.5f, -.4f), pf.Fwd, "★ STAR OF THE SHOW ★", .2f, Ink); mk.Box(Paint(White), pf.P(0, 3.5f, -.44f), new Vector3(3.2f, .4f, .04f), pf.r);
            string[] pose = { "Flash! You strike a pose and somebody behind you screams like you're famous.", "You do the peace sign. Then the heart hands. Then the one where you look over your shoulder.", "Click. Framed in gold. Your hair has never looked better and never will again." }; var pn = 0;
            Spot("The photo frame", pf.P(0, 1.4f, 0), "Strike a pose", new Vector3(3f, 2.4f, 1.2f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, pf.P(0, 4.2f, 0), pose[pn++ % pose.Length], 4.5f); }; Mark(pf.o, 2.6f);
            // the ice-cream cart: a cone to carry
            var ic = new PlaceFrame(c + D * 10f + S * 12f + up * .14f, (-D - S * .8f).normalized); mk.Box(Paint(Pink), ic.P(0, .7f, 0), new Vector3(2f, 1f, 1f), ic.r); foreach (var e in new[] { -.7f, .7f }) mk.Prism(dark, ic.P(e, .3f, .52f), ic.Fwd, .3f, .08f, 10); mk.Box(white, ic.P(0, 1.22f, 0), new Vector3(2.1f, .05f, 1.1f), ic.r); Col(ic.P(0, .6f, 0), new Vector3(2f, 1.2f, 1f), ic.r);
            mk.Prism(white, ic.P(.8f, 1.2f, -.3f), Vector3.up, .03f, 1.6f, 4); for (var k = 0; k < 8; k++) mk.Box(Paint(k % 2 == 0 ? Pink : White), ic.P(.8f, 2.9f, -.3f) + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .6f, new Vector3(.5f, .04f, 1.2f), Quaternion.Euler(-14f, k * 45f, 0)); for (var k = 0; k < 3; k++) mk.Blob(Paint(Candy[k]), ic.P(-.5f + k * .4f, 1.3f, 0), Vector3.one * .14f, rng, .1f);
            for (var k = 0; k < 2; k++) { var cm = new MeshKit(); cm.Cone(Paint(new Color(.86f, .66f, .4f)), new Vector3(0, .2f, 0), .06f, -.2f, 8); cm.Blob(Paint(Candy[k * 2]), new Vector3(0, .24f, 0), Vector3.one * .07f, rng, .1f); Thing(cm, "Ice cream", k == 0 ? "a strawberry ice cream" : "a lemon ice cream", ic.P(-.3f + k * .5f, 1.25f, .3f), Quaternion.identity, new Vector3(.14f, .32f, .14f), .1f, false); }
            Folk(ic.P(-.4f, 0, -.9f), ic.Fwd, Resident.Pose.Stand, new[] { "Strawberry or lemon? Both? Both is correct.", "It never melts here. Science can't explain it. Pop can.", "Take one. Sing for it if you feel bad." }, Local(Residents.Tool.None, true)); Mark(ic.o, 3f);
            // benches round the fountain, people singing along
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 70f, 0) * D; Bench(ctx.Root, mk, c + d * 7.6f + up * .17f, -d); }
            Folk(c + Quaternion.Euler(0, 70f, 0) * D * 7.6f + up * .17f, -(Quaternion.Euler(0, 70f, 0) * D), Resident.Pose.Sit, new[] { "Shh, my part's coming up. ...AND I'M NEVER GONNA... no wait that's the next song.", "I've been sitting here since the bridge. Best part of any song. Don't argue." });
            Mark(c, 21f);
            Shot(ctx, "the plaza", c + D * 14f + S * 10f + up * 2.6f, c + up * 3f - D * 4f);
        }

        // ---------------------------------------------------------------- the Red Carpet
        void RedCarpet(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, paving, CarpetW + 4f, .1f, ctx); Strip(mk, pts, carpet, CarpetW, .14f, ctx); foreach (var p in pts) Remember(p, 1);
            // velvet ropes on gold posts down both sides
            foreach (var e in new[] { -1f, 1f })
            {
                Vector3 prev = default; var have = false;
                for (var d = 22f; d <= streetL - 1f; d += 3f)
                {
                    var g = G(ctx, d, e * (CarpetW / 2f + .4f)); mk.Prism(goldM, g - up * .1f, up, .05f, 1.1f, 6); mk.Blob(goldM, g + up * 1.02f, Vector3.one * .08f, new System.Random(1), 0f); ArtistBits.PostCollider(ctx.Root, g, 1f, .07f);
                    if (have) { var mid = (prev + g) / 2f + up * .7f; mk.Prism(ropeM, prev + up * .9f, (mid - prev - up * .9f).normalized, .03f, Vector3.Distance(prev + up * .9f, mid), 4); mk.Prism(ropeM, mid, (g + up * .9f - mid).normalized, .03f, Vector3.Distance(mid, g + up * .9f), 4); }
                    prev = g; have = true;
                }
            }
            // a star in the pavement for every artist, down the sides
            var names = new List<string>(); foreach (var a in ctx.Artists) names.Add(a.name); names.Sort();
            for (var k = 0; k < names.Count; k++)
            {
                var e = k % 2 == 0 ? 1f : -1f; var d = 24f + (k / 2) * (Mathf.Max(20f, streetL - 30f) / Mathf.Max(1, names.Count / 2)); var g = G(ctx, d, e * (CarpetW / 2f + 1.6f)) + up * .11f;
                mk.Prism(Paint(new Color(.95f, .6f, .74f)), g, up, .8f, .02f, 10); Star(mk, goldM, g + up * .03f, Vector3.up, .66f); ArtistBits.Label(rootT, names[k], g + up * .06f - S * e * .0f, Vector3.up, .1f, Ink, TextAnchor.MiddleCenter);
            }
            // photographers behind the ropes; strings of lights across; a red-carpet host
            var flashes = 0;
            for (var d = 30f; d <= streetL - 6f; d += 14f, flashes++)
            {
                var e = flashes % 2 == 0 ? 1f : -1f; var g = G(ctx, d, e * (CarpetW / 2f + 2.6f)); Folk(g, -S * e, Resident.Pose.Work, new[] { "Over here! OVER HERE! Over the shoulder! Perfect!", "Who are you wearing? Doesn't matter. You look incredible.", "Flash flash flash. I've taken nine thousand photos today and they're all of you." }, Local(Residents.Tool.None)); life.flashes.Add(g + up * 1.7f - S * e * .3f);
                var a = G(ctx, d - 6f, -(CarpetW / 2f + 3.4f)); var b = G(ctx, d - 6f, CarpetW / 2f + 3.4f); foreach (var q in new[] { a, b }) { mk.Prism(white, q - up * .2f, up, .07f, 5.4f, 5); ArtistBits.PostCollider(ctx.Root, q, 5f, .09f); } Bulbs(mk, a + up * 5f, b + up * 5f, flashes);
            }
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 15f) route.Add(G(ctx, d, 1.2f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 15f) route.Add(G(ctx, d, -1.2f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Just walking the carpet. Slowly. With intention.", "Every time I get to the Palace end I wave. Nobody's there. I wave anyway." }, Local(), route.ToArray()).speed = .9f;
            Shot(ctx, "down the red carpet", G(ctx, 21f, .4f) + up * 2.2f, G(ctx, streetL + 30f, 0) + up * 12f);
        }

        // ---------------------------------------------------------------- the Palace and the ferris wheel
        void ThePalace(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pc = G(ctx, streetL + 22f, 0); var rot = Quaternion.LookRotation(-D); var rng = new System.Random(88);
            mk.Prism(paving, pc - up * .28f, up, 26f, .42f, 32); mk.Prism(pavingPink, pc + up * .14f, up, 19f, .012f, 30); mk.Prism(paving, pc + up * .17f, up, 18.3f, .012f, 30);
            for (var k = 0; k < 12; k++) Remember(pc + Quaternion.Euler(0, k * 30f, 0) * D * 21f, 1); Remember(pc - D * 8f, 1); life.stageAt = pc + D * 8f + up * 6f;
            // the stage: a wide deck with steps, a white palace front with pink domes, an arch of bulbs
            var st = pc + D * 10f + up * .14f; mk.Box(white, st + up * .7f, new Vector3(18f, 1.4f, 7f), rot); solid.Box(white, st + up * .6f, new Vector3(18f, 1.6f, 7f), rot); mk.Box(goldM, st + up * 1.41f - D * 3.4f, new Vector3(18f, .03f, .2f), rot);
            for (var k = 0; k < 4; k++) { var sp = st - D * (3.8f + k * .5f) + up * (.18f + k * .32f) - up * 0f; mk.Box(white, st - D * (3.5f + (3 - k) * .45f) + up * (.18f + k * .32f), new Vector3(4f, .36f, .45f), rot); solid.Box(white, st - D * (3.5f + (3 - k) * .45f) + up * (.18f + k * .32f) - up * .2f, new Vector3(4f, .6f, .45f), rot); }
            var back = st + D * 3.6f; mk.Box(white, back + up * 6f, new Vector3(22f, 12f, 1.2f), rot); solid.Box(white, back + up * 6f, new Vector3(22f, 12f, 1.2f), rot); mk.Box(Paint(Pink), back + up * 6f - D * .62f, new Vector3(14f, 8f, .04f), rot);
            foreach (var e in new[] { -1f, 1f }) { var tw = back + S * e * 11.4f; mk.Prism(white, tw - up * .2f, up, 2.2f, 15f, 10); solid.Prism(white, tw - up * .2f, up, 2.2f, 15f, 10); mk.Blob(Paint(Pink), tw + up * 15.6f, new Vector3(2.4f, 2.6f, 2.4f), rng, 0f); mk.Cone(goldM, tw + up * 17.6f, .3f, 1.6f, 6); for (var k = 0; k < 4; k++) mk.Box(windowM, tw + up * (3f + k * 3f) - D * 2.21f, new Vector3(.8f, 1.4f, .05f), rot); }
            mk.Blob(Paint(Pink), back + up * 13.6f, new Vector3(4f, 3.6f, 3f), rng, 0f); mk.Cone(goldM, back + up * 16.8f, .4f, 2.2f, 6); Star(mk, goldM, back + up * 19.4f, -D, 1f);
            for (var k = 0; k <= 24; k++) { var a = Mathf.PI * k / 24f; mk.Blob(bulbM[k % 3], st + up * (1.4f + Mathf.Sin(a) * 9f) + S * Mathf.Cos(a) * 8.6f - D * 2.8f, Vector3.one * .2f, rng, 0f); }
            Sign(back + up * 10.6f - D * .66f, -D, "THE PALACE", 1.1f, White); Sign(back + up * 9.4f - D * .66f, -D, "everybody sings", .4f, Lemon);
            for (var k = 0; k < 6; k++) { var lp = st + up * 1.5f - D * 3f + S * (-7.5f + k * 3f); mk.Prism(dark, lp, (up * 1f - D * .4f).normalized, .16f, .4f, 8, 1.4f); mk.Blob(bulbM[k % 3], lp + (up - D * .4f).normalized * .42f, Vector3.one * .14f, rng, 0f); }
            ArtistBits.PointLight(ctx.Root, st + up * 6f - D * 4f, new Color(1f, .9f, .9f), 26f, 1.4f);
            var mc = st + up * 1.4f - D * 2f; Mic(mk, mc, goldM);
            string[] sing = { "You grab the mic. You don't even have to sing. Ten thousand people sing it for you.", "\"Everybody!\" And everybody does. The fountain on the Plaza jumps in time.", "Key change! You go up with it. Nobody knows how, least of all you.", "You hold the mic out to the crowd. They know every word. Every single word." }; var sn = 0;
            Spot("The Palace microphone", mc + up * 1.4f, "Take the mic", new Vector3(1f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, mc + up * 2.6f, sing[sn++ % sing.Length], 4.5f); };
            // the crowd with hands up; a barrier
            for (var k = 0; k < 7; k++) { var bp = pc + D * 3.6f + S * (-7f + k * 2.3f) + up * .19f; mk.Box(white, bp + up * .55f, new Vector3(2.2f, .06f, .06f), rot); foreach (var e in new[] { -1.05f, 1.05f }) mk.Box(white, bp + S * e + up * .3f, new Vector3(.05f, .6f, .05f), rot); } Col(pc + D * 3.6f + up * .5f, new Vector3(16f, 1f, .2f), rot);
            string[][] crowd = { new[] { "I KNOW EVERY WORD! EVERY WORD!", "I made a sign. It says I made a sign." }, new[] { "This is my fourteenth show this week. Tomorrow's is the fifteenth.", "Get up there! Steps on the left! Go!" }, new[] { "The bridge is coming. Brace.", "My voice is gone. I'm going to keep using it." } };
            for (var k = 0; k < 10; k++) Folk(pc + D * (2.2f - (k % 2) * 1.6f) + S * (-7f + k * 1.55f) + up * .19f, D, k % 3 == 1 ? Resident.Pose.Stand : Resident.Pose.Wave, crowd[k % 3]);
            // the ferris wheel behind, turning
            var wb = back + D * 14f + S * 6f; var wy = 22f; var hub = wb + up * wy; foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) { var foot = wb + S * q * 7f + D * e * 1.4f; mk.Prism(white, foot - up * .2f, (hub + D * e * 1.4f - foot + up * .2f).normalized, .3f, (hub - foot).magnitude + .4f, 6); ArtistBits.PostCollider(ctx.Root, foot, 3f, .4f); }
            mk.Prism(chrome, hub - D * 1.6f, D, .5f, 3.2f, 10);
            var wk = new MeshKit(); const float R = 18f; var cabs = new List<(Vector3, int)>();
            for (var k = 0; k < 36; k++) { var a0 = k * 10f * Mathf.Deg2Rad; var a1 = (k + 1) * 10f * Mathf.Deg2Rad; foreach (var z in new[] { -.8f, .8f }) { var p0 = new Vector3(Mathf.Cos(a0) * R, Mathf.Sin(a0) * R, z); var p1 = new Vector3(Mathf.Cos(a1) * R, Mathf.Sin(a1) * R, z); wk.Prism(white, p0, (p1 - p0).normalized, .16f, Vector3.Distance(p0, p1) + .05f, 5); } }
            for (var k = 0; k < 16; k++) { var a = k * 22.5f * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); foreach (var z in new[] { -.8f, .8f }) wk.Prism(white, new Vector3(0, 0, z), dir, .07f, R, 4); wk.Blob(bulbM[k % 3], dir * (R + .3f), Vector3.one * .3f, rng, 0f); cabs.Add((dir * R, k)); }
            var wheel = wk.BuildSingle("Ferris wheel", ctx.Root, false); wheel.transform.SetPositionAndRotation(hub, Quaternion.LookRotation(D)); life.wheel = wheel.transform;
            foreach (var (p, k) in cabs) { var cm = new MeshKit(); cm.Prism(white, Vector3.zero, Vector3.down, .04f, .9f, 4); cm.Box(Paint(Candy[k % Candy.Length]), new Vector3(0, -1.6f, 0), new Vector3(1.6f, 1.4f, 1.4f), Quaternion.identity); cm.Box(white, new Vector3(0, -.86f, 0), new Vector3(1.8f, .1f, 1.6f), Quaternion.identity); var go = cm.BuildSingle("Car", wheel.transform, false); go.transform.localPosition = p; life.cars.Add(go.transform); }
            halos.Add(hub, 14f, Pink, .06f); Mark(pc, 27f); Mark(wb, 10f);
            Shot(ctx, "the palace", pc - D * 20f + S * 8f + up * 2.4f, back + up * 8f);
            Shot(ctx, "on the palace stage", st + up * 3f - D * 1.6f + S * 3f, pc - D * 40f + up * 2f);
        }

        // ---------------------------------------------------------------- K-Pop Square
        void KPopSquare(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(k2.x, k2.y); kCenter = c; var rng = new System.Random(2013); var face = (center - c); face.y = 0; face.Normalize(); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Prism(paving, c - up * .28f, up, 20f, .42f, 30); for (var k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f, 0) * face; mk.Box(k % 2 == 0 ? Paint(Lilac) : Paint(White), c + up * .15f + d * 9f, new Vector3(1.2f, .02f, 15f), Quaternion.LookRotation(d)); }
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * face * 18f, 1);
            // a lane from the Plaza
            var from = center + face * 0f; var dir = c - center; dir.y = 0; var total = dir.magnitude; dir /= total; var pts = new List<Vector3>(); for (var d = 19f; d <= total - 17f; d += 2f) { var q = center + dir * d; if (!ctx.IsLand(q.x, q.z, 0f)) break; pts.Add(ctx.OnGround(q.x, q.z)); }
            if (pts.Count > 1) { Strip(mk, pts, pavingPink, 4f, .13f, ctx); foreach (var p in pts) Remember(p, 2); for (var i = 4; i < pts.Count; i += 8) StarLamp(ctx, mk, pts[i] + Vector3.Cross(up, dir) * ((i / 8) % 2 == 0 ? 2.6f : -2.6f), i, true); }
            // the idol stage: a glossy black deck, a screen of light, a lit arch, six beams
            var st = c - face * 8f + up * .14f; mk.Box(dark, st + up * .5f, new Vector3(10f, 1f, 5f), rot); solid.Box(dark, st + up * .4f, new Vector3(10f, 1.2f, 5f), rot); mk.Box(GlowM(Lilac, 1.2f), st + up * 1.01f, new Vector3(9.4f, .02f, 4.4f), rot);
            var scr = st - face * 2.4f; mk.Box(dark, scr + up * 3.6f, new Vector3(10f, 5.4f, .3f), rot); solid.Box(dark, scr + up * 3.6f, new Vector3(10f, 5.4f, .3f), rot); var screenM = new Material(GlowM(Lilac, 1.4f)) { name = "K-pop screen" }; mk.Box(screenM, scr + up * 3.6f + face * .16f, new Vector3(9.4f, 4.8f, .02f), rot); life.screen = screenM;
            Sign(scr + up * 4.2f + face * .2f, face, "K-POP SQUARE", .8f, White); Sign(scr + up * 3.1f + face * .2f, face, "fighting!", .34f, Lemon);
            for (var k = 0; k < 6; k++) { var bp = st + side * (-4.4f + k * 1.76f) - face * 2.2f; mk.Prism(dark, bp + up * 1f, (up + face * .5f).normalized, .14f, .4f, 8); var beam = new Material(GlowM(new[] { Pink, Cyan, Lilac, Lemon, White, Pink }[k], 1.1f)) { name = "K-pop beam" }; mk.Prism(beam, bp + up * 1.4f, (up * 3f + face * 1f + side * (k - 2.5f) * .3f).normalized, .05f, 9f, 5, 3f); life.beams.Add(beam); }
            Mic(mk, st + up * 1f + face * 1f, GlowM(Lilac, 1.4f));
            string[] dance = { "Point choreography! Arm, arm, hip, turn. Everyone in the square does it with you, perfectly.", "The fan chant comes in exactly on time. Twelve thousand people, one voice.", "You hit the final pose. The beams freeze. Somebody faints. Not you. Probably.", "Five, six, seven, eight! It's the killing part. Nobody misses it." }; var dn = 0;
            Spot("The idol stage", st + up * 1.8f, "Do the choreography", new Vector3(8f, 1.6f, 4f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, st + up * 3.2f + face * 1f, dance[dn++ % dance.Length], 4.5f); };
            ArtistBits.PointLight(ctx.Root, st + up * 5f + face * 2f, new Color(.9f, .8f, 1f), 20f, 1.4f);
            // a table of light sticks, fans holding them up
            var tb = c + face * 6f + side * 6f + up * .14f; mk.Box(white, tb + up * .74f, new Vector3(2f, .06f, .8f), rot); foreach (var e in new[] { -.9f, .9f }) mk.Box(white, tb + up * .36f + side * e, new Vector3(.06f, .72f, .7f), rot); Col(tb + up * .4f, new Vector3(2f, .8f, .8f), rot);
            string[] sticks = { "a purple light stick", "a candy-pink light stick", "a black and pink light stick", "a red light stick", "a blue bunny light stick", "a silver light stick" }; var stc = new[] { Lilac, Pink, new Color(1f, .4f, .6f), RedC, Cyan, new Color(.85f, .9f, 1f) };
            for (var k = 0; k < 6; k++) LightStick(tb + up * .78f + side * (-.8f + k * .32f), stc[k], sticks[k]);
            for (var k = 0; k < 7; k++) { var p = c + face * (2f + (k % 2) * 1.6f) + side * (-5f + k * 1.6f) + up * .14f; Folk(p, -face, Resident.Pose.Wave, new[] { "Saranghae! I mean, I love this one!", "I learned the whole dance from a video at half speed. Watch!", "Bias? All of them. Every group. Don't make me choose." }, Local()); }
            Mark(c, 20f);
            Shot(ctx, "k-pop square", c + face * 14f + side * 6f + up * 2.4f, st + up * 3f);
        }

        /// The houses down both sides of the Red Carpet, built after the lanes so nothing stands in one.
        void Houses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var rng = new System.Random(12);
            foreach (var e in new[] { 1f, -1f })
            for (var d = 30f + (e > 0 ? 0f : 5f); d <= streetL - 4f; d += 10.5f, i++)
            {
                var p = Wp(d, e * (CarpetW / 2f + 10f)); if (!ctx.IsLand(p.x, p.z, 6f) || InFootprint(ctx, p.x, p.z, 6f) || LaneNear(p.x, p.z, 6.4f)) continue;
                House(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e), i, rng); built++;
            }
            Debug.Log($"The Pop Republic: {built} candy houses on the Red Carpet");
        }

        /// A candy-coloured townhouse: flat roof with a rail, big windows, an awning, a shop or a stoop.
        void House(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i, System.Random rng)
        {
            var up = Vector3.up; float W = 7.4f, Dp = 6.4f, H = 6f + (i % 3) * 2.4f; var wall = Paint(Candy[i % Candy.Length]); var trim = white;
            // a footing down to the lowest corner, so on a hillside no wall hangs in the air
            var low = f.o.y; foreach (var (cx, cz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var q = f.P(cx * W / 2f, 0, cz * Dp / 2f); low = Mathf.Min(low, ctx.Ground(q.x, q.z)); } var sink = f.o.y - low + 1f;
            mk.Box(wall, f.P(0, (H - sink) / 2f, 0), new Vector3(W, H + sink, Dp), f.r); solid.Box(wall, f.P(0, (H - sink) / 2f, 0), new Vector3(W, H + sink, Dp), f.r); mk.Box(trim, f.P(0, H + .1f, 0), new Vector3(W + .3f, .2f, Dp + .3f), f.r); for (var k = 0; k < 9; k++) mk.Box(trim, f.P(-W / 2f + .2f + k * (W - .4f) / 8f, H + .5f, Dp / 2f), new Vector3(.05f, .7f, .05f), f.r); mk.Box(trim, f.P(0, H + .86f, Dp / 2f), new Vector3(W, .05f, .06f), f.r);
            var floors = Mathf.FloorToInt((H - 3f) / 2.4f); for (var fl = 0; fl <= floors; fl++) for (var k = 0; k < 2; k++) { var at = f.P((k - .5f) * W * .5f, 1.6f + fl * 2.4f, Dp / 2f + .02f); if (fl == 0 && k == 1) continue; mk.Box(trim, at, new Vector3(1.7f, 1.6f, .05f), f.r); mk.Box(fl == 0 ? windowM : glassM, at + f.Fwd * .03f, new Vector3(1.5f, 1.4f, .05f), f.r); }
            mk.Box(trim, f.P(W * .25f, 1.15f, Dp / 2f + .02f), new Vector3(1.3f, 2.3f, .05f), f.r); mk.Box(Paint(Candy[(i + 2) % Candy.Length]), f.P(W * .25f, 1.1f, Dp / 2f + .05f), new Vector3(1.1f, 2.1f, .05f), f.r);
            for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? white : Paint(Candy[(i + 3) % Candy.Length]), f.P(-W / 2f + .62f + k * 1.23f, 2.7f, Dp / 2f + .6f), new Vector3(1.23f, .06f, 1.3f), f.r * Quaternion.Euler(14f, 0, 0));
            if (i % 3 == 0) Folk(f.P(W * .25f + 1f, 0, Dp / 2f + .8f), f.Fwd, Resident.Pose.Lean, new[] { "My window's right on the Carpet. I've seen every arrival. I wave at all of them.", "Can you hear it? That's the chorus coming round again. It always comes round again.", "This street has a song for every door." });
            else if (i % 3 == 1) Bench(ctx.Root, mk, f.P(-W * .22f, 0, Dp / 2f + 1.2f), f.Fwd, 1.6f);
            Mark(f.o, 5.6f);
        }
    }

    /// The Pop Republic's holder for things you can use and carry.
    public class PopTownLife : MonoBehaviour { }
}
