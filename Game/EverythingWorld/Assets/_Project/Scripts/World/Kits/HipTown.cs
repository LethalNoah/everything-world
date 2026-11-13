using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Hip-Hop Boroughs' places (part of HipKit): the Block (a concrete square with a half court and a ball, a
    /// mural with the land's name, a winners' podium and a trophy to carry, a cypher round a boombox, a lowrider);
    /// Victory Avenue (pennants and sneakers on the wires, blocks with gold windows down both sides); and the Stage
    /// at the foot of the Tower, which has a boombox the size of a house on its roof.
    public partial class HipKit
    {
        HipTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.9f, .7f, .2f), new Color(.85f, .2f, .2f), new Color(.95f, .93f, .86f), new Color(.2f, .3f, .6f), new Color(.14f, .14f, .16f), new Color(.3f, .6f, .4f), new Color(.5f, .3f, .7f) };
        static readonly Color[] Bottoms = { new Color(.16f, .18f, .28f), new Color(.14f, .14f, .16f), new Color(.6f, .56f, .46f), new Color(.3f, .34f, .4f) };
        static readonly string[] TownTalk =
        {
            "You made it up here! Everybody who's up here made it up here.", "Stage is at the end of Victory Avenue. You can't miss it. It's under the boombox.", "Golden hour's the only hour we've got. Nobody's complained.",
            "Hear that? That's the Tower. You feel it in the pavement first.", "Walk like the beat's yours. Up here it is.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1988);
            tlife = new GameObject("Hip-Hop town life").AddComponent<HipTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cap, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.Cap }, new[] { Gold, Red, Cream, Ink }); l.tool = tool; l.apron = apron; return l; }
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

        /// A basketball to carry and throw.
        void Ball(Vector3 at)
        {
            var b = new MeshKit(); var rng = new System.Random(4); b.Blob(Paint(new Color(.9f, .45f, .15f)), new Vector3(0, .13f, 0), Vector3.one * .13f, rng, 0f); b.Prism(dark, new Vector3(0, .002f, 0), Vector3.up, .012f, .26f, 4); Thing(b, "Basketball", "a basketball", at, Quaternion.identity, new Vector3(.27f, .27f, .27f), .5f, false);
        }

        /// A hoop on a post: a white backboard, a gold ring.
        void Hoop(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, float h = 3.05f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Prism(iron, g - up * .2f, up, .1f, h + .9f, 6); mk.Prism(iron, g + up * (h + .5f), face, .06f, .8f, 4); mk.Box(white, g + up * (h + .5f) + face * .8f, new Vector3(1.8f, 1.1f, .06f), rot); mk.Box(redM, g + up * (h + .3f) + face * .84f, new Vector3(.6f, .45f, .02f), rot); mk.Box(white, g + up * (h + .3f) + face * .86f, new Vector3(.5f, .35f, .02f), rot);
            for (var k = 0; k < 12; k++) { var a = Quaternion.Euler(0, k * 30f, 0); mk.Box(goldM, g + up * h + face * 1.08f + a * Vector3.forward * .23f, new Vector3(.13f, .03f, .03f), a); } for (var k = 0; k < 6; k++) mk.Prism(white, g + up * h + face * 1.08f + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * .22f, (Vector3.down + Quaternion.Euler(0, k * 60f, 0) * Vector3.back * .2f).normalized, .008f, .4f, 3);
            ArtistBits.PostCollider(ctx.Root, g, h + .8f, .12f);
        }

        /// A boombox: two cones that beat, a tape deck, a handle.
        void Boombox(MeshKit mk, Vector3 c, Quaternion rot, float s, bool halo = true)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; var r = rot * Vector3.right; mk.Box(Paint(new Color(.15f, .15f, .19f)), c + up * .28f * s, new Vector3(1f, .56f, .3f) * s, rot); mk.Box(goldM, c + up * .57f * s, new Vector3(1.02f, .03f, .32f) * s, rot);
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(dark, c + up * .26f * s + r * e * .31f * s + f * .15f * s, f, .17f * s, .012f * s, 14); mk.Prism(coneM, c + up * .26f * s + r * e * .31f * s + f * .163f * s, f, .12f * s, .012f * s, 12); if (halo && s > 3f) halos.Add(c + up * .26f * s + r * e * .31f * s + f * .4f * s, .3f * s, Gold, .22f); }
            mk.Box(Paint(Cream), c + up * .3f * s + f * .152f * s, new Vector3(.22f, .16f, .01f) * s, rot); foreach (var e in new[] { -1f, 1f }) mk.Prism(dark, c + up * .3f * s + r * e * .05f * s + f * .158f * s, f, .028f * s, .008f * s, 8);
            foreach (var e in new[] { -1f, 1f }) mk.Box(iron, c + up * .66f * s + r * e * .36f * s, new Vector3(.04f, .16f, .04f) * s, rot); mk.Box(iron, c + up * .74f * s, new Vector3(.76f, .04f, .05f) * s, rot); mk.Prism(iron, c + up * .56f * s + r * .44f * s, (up + r * .3f).normalized, .008f * s, .6f * s, 4);
        }

        // ---------------------------------------------------------------- the Block
        void TheBlock(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(concrete, c - up * .28f, up, 20f, .42f, 32); mk.Prism(goldM, c + up * .14f, up, 12.25f, .012f, 32); mk.Prism(concrete, c + up * .17f, up, 12f, .012f, 32);
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            // the half court: a painted floor, a hoop, a ball
            var ct = new PlaceFrame(c - D * 10f - S * 7.5f + up * .2f, (D + S * .6f).normalized); mk.Box(Paint(new Color(.6f, .28f, .2f)), ct.P(0, 0, 0), new Vector3(9f, .02f, 8f), ct.r); mk.Box(Paint(Sky), ct.P(0, .035f, -2.4f), new Vector3(3.2f, .02f, 3.2f), ct.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(white, ct.P(e * 4.4f, .04f, 0), new Vector3(.1f, .02f, 8f), ct.r); mk.Box(white, ct.P(e * 1.6f, .07f, -2.4f), new Vector3(.1f, .02f, 3.2f), ct.r); mk.Box(white, ct.P(0, .04f, e * 3.95f), new Vector3(9f, .02f, .1f), ct.r); } mk.Box(white, ct.P(0, .07f, -.8f), new Vector3(3.3f, .02f, .1f), ct.r);
            for (var k = 0; k <= 10; k++) { var a = Quaternion.Euler(0, -90f + k * 18f, 0); mk.Box(white, ct.P(0, .04f, -2.8f) + ct.r * (a * Vector3.forward * 4f), new Vector3(1.3f, .02f, .1f), ct.r * a); }
            Hoop(ctx, mk, ct.P(0, -.2f, -4.6f), ct.Fwd); Ball(ct.P(1.2f, .06f, -1f)); Ball(ct.P(-2.6f, .06f, 1.8f));
            Folk(ct.P(-1f, .02f, .6f), -ct.Fwd, Resident.Pose.Wave, new[] { "Next basket wins. It's been next basket wins since lunch.", "Take a ball. Pick it up and throw it; the hoop's regulation, the wind's not.", "Up here every shot's a buzzer-beater. Sun's always about to go down." }); Mark(ct.o, 6.4f);
            // the mural: the land's name on a brick wall
            var mu = new PlaceFrame(c - D * 17.4f + S * 6f + up * .14f, (D - S * .2f).normalized); mk.Box(brickM, mu.P(0, 2.2f, 0), new Vector3(11f, 4.4f, .5f), mu.r); solid.Box(brickM, mu.P(0, 2.2f, 0), new Vector3(11f, 4.4f, .5f), mu.r); mk.Box(concreteDark, mu.P(0, 4.5f, 0), new Vector3(11.3f, .2f, .7f), mu.r);
            mk.Box(Paint(Purple), mu.P(0, 2.4f, .27f), new Vector3(10.2f, 3.2f, .03f), mu.r); for (var k = 0; k < 9; k++) { var a = Quaternion.AngleAxis(-80f + k * 20f, mu.Fwd); mk.Box(goldM, mu.P(0, .9f, .31f) + a * up * 2.3f, new Vector3(.26f, 4f, .02f), a * mu.r); } mk.Box(Paint(Purple), mu.P(0, 1.3f, .35f), new Vector3(10.2f, 1f, .02f), mu.r);
            Sign(mu.P(0, 2.9f, .4f), mu.Fwd, "THE HIP-HOP", .8f, Cream); Sign(mu.P(0, 1.9f, .4f), mu.Fwd, "BOROUGHS", 1f, Gold); Sign(mu.P(0, 1.1f, .4f), mu.Fwd, "started from the bottom of the mesa", .2f, Cream); Hydrant(ctx, mk, mu.P(-6.2f, 0, 1f)); Hydrant(ctx, mk, mu.P(6.4f, 0, 1.2f), true);
            // the podium: step up; a trophy to carry
            var po = new PlaceFrame(c + D * 9f + S * 10.5f + up * .14f, (-D - S * .9f).normalized); var hs = new[] { .7f, 1.1f, .45f }; var xs = new[] { -1.3f, 0f, 1.3f }; var nums = new[] { "2", "1", "3" };
            for (var k = 0; k < 3; k++) { mk.Box(k == 1 ? goldM : white, po.P(xs[k], hs[k] / 2f, 0), new Vector3(1.3f, hs[k], 1.3f), po.r); solid.Box(white, po.P(xs[k], hs[k] / 2f - .1f, 0), new Vector3(1.3f, hs[k] + .2f, 1.3f), po.r); Sign(po.P(xs[k], hs[k] * .5f, .66f), po.Fwd, nums[k], .4f, Ink); }
            mk.Box(white, po.P(0, .18f, 1f), new Vector3(1.3f, .36f, .7f), po.r); solid.Box(white, po.P(0, .1f, 1f), new Vector3(1.3f, .5f, .7f), po.r);
            var tm = new MeshKit(); tm.Prism(goldM, Vector3.zero, Vector3.up, .1f, .04f, 8); tm.Prism(goldM, new Vector3(0, .04f, 0), Vector3.up, .025f, .14f, 6); tm.Prism(goldM, new Vector3(0, .18f, 0), Vector3.up, .07f, .2f, 8, 1.7f); foreach (var e in new[] { -1f, 1f }) tm.Box(goldM, new Vector3(e * .15f, .3f, 0), new Vector3(.03f, .12f, .03f), Quaternion.identity); Thing(tm, "Trophy", "the trophy. Yours now", po.P(0, 1.12f, 0), po.r, new Vector3(.3f, .4f, .3f), .6f);
            string[] win = { "You step up. Somewhere a crowd that isn't there loses its mind.", "First place. You'd like to thank everyone who said you couldn't, by name, alphabetically.", "You hold the pose. The sun hits you like a spotlight because up here it is one." }; var wn = 0;
            Spot("The podium", po.P(0, 2.3f, 0), "Take the win", new Vector3(1.3f, 1.6f, 1.3f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, po.P(0, 3.4f, 0), win[wn++ % win.Length], 4.5f); }; Mark(po.o, 3f);
            // the cypher: a boombox on a crate, a ring of people
            var cy = c + D * 7f - S * 10f + up * .14f; mk.Box(Paint(Tags[2]), cy + up * .22f, new Vector3(.7f, .44f, .5f), Quaternion.identity); Boombox(mk, cy + up * .44f, Quaternion.LookRotation(S), 1f, false); Col(cy + up * .5f, new Vector3(1f, 1f, .5f), Quaternion.identity);
            string[] play = { "You press play. The beat drops and four heads start nodding like they're agreeing with it.", "Somebody goes in for sixteen bars. Somebody else goes \"OHHH\" at bar twelve. Correctly.", "Rewind. That line needed hearing twice.", "The tape hisses, then the drums come in dusty and exactly right." }; var pn = 0;
            Spot("The cypher's boombox", cy + up * 1.2f, "Press play", new Vector3(1.1f, .8f, .9f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, cy + up * 2.4f, play[pn++ % play.Length], 4.5f); };
            string[][] bars = { new[] { "Yo, yo. Okay. Yo. ...Give me a second.", "I had a rhyme for 'mesa' and it's gone. 'Lisa.' No. 'Pizza'?" }, new[] { "He's been on 'yo' for a minute. We let him cook.", "Best cypher on the Block. Only cypher on the Block. Still counts." }, new[] { "Bars! Okay, not yet. But soon: bars!", "I'm the hype man. I hype. It's a real job." } };
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 100f - 40f, 0) * S; Folk(cy + d * 1.9f, -d, k == 2 ? Resident.Pose.Wave : Resident.Pose.Stand, bars[k]); } Mark(cy, 2.8f);
            // the lowrider, gold on purple
            var lr = c - D * 2f + S * 16f + up * .16f; Car(ctx.Root, mk, lr, Quaternion.LookRotation((D + S * .2f).normalized), Purple); Interact.Seat(tlife.transform, lr + up * .9f, -S, "Sit on the lowrider").seatHeight = .8f;
            // street lamps round the square, wires between them, sneakers on the wires
            var posts = new Vector3[6]; for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * D; posts[k] = c + d * 18.6f + up * .14f; StreetLamp(ctx, mk, posts[k], -d, k % 2 == 0); }
            for (var k = 0; k < 6; k++) { if (k == 5) continue; Wire(mk, posts[k] + up * 4.7f, posts[(k + 1) % 6] + up * 4.7f, k % 2 == 0 ? 2 : 1, rng); }
            Folk(c + D * 5f + S * 3f + up * .19f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Ayy, you made it! Welcome to the top.", "Victory Avenue's straight ahead. Walk it slow. Let 'em see you.", "Trophy's on the podium if you want it. Everybody gets a turn." });
            Mark(c, 21f);
            Shot(ctx, "the block", c + D * 15f + S * 11f + up * 2.8f, c - D * 12f + up * 2.6f);
        }

        // ---------------------------------------------------------------- Victory Avenue
        void VictoryAvenue(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, concrete, StreetW + 3.6f, .08f, ctx); Strip(mk, pts, asphalt, StreetW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            for (var i = 1; i + 1 < pts.Count; i += 3) Strip(mk, new List<Vector3> { pts[i], pts[i + 1] }, goldM, .28f, .17f, ctx);
            var rng = new System.Random(6); var n = 0;
            for (var d = 28f; d <= streetL - 4f; d += 11f, n++)
            {
                var a = G(ctx, d, -(StreetW / 2f + 1.3f)); var b = G(ctx, d, StreetW / 2f + 1.3f); StreetLamp(ctx, mk, a, S, n % 2 == 0); StreetLamp(ctx, mk, b, -S, n % 2 == 1);
                if (n % 3 == 2) Wire(mk, a + up * 4.8f, b + up * 4.8f, 2, rng); else Pennants(mk, a + up * 4.9f, b + up * 4.9f, n);
                if (n % 4 == 1) Hydrant(ctx, mk, G(ctx, d + 4f, StreetW / 2f + 1.2f), n % 8 == 1);
            }
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 15f) route.Add(G(ctx, d, 2.2f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 15f) route.Add(G(ctx, d, -2.2f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Just doing my lap. Somebody has to walk this avenue like they own it.", "Every time I get to the Stage I turn round and do it again. It doesn't get old." }, null, route.ToArray()).speed = 1.3f;
            Shot(ctx, "up victory avenue", G(ctx, 22f, 1f) + up * 2.2f, G(ctx, streetL + 34f, 0) + up * 26f);
        }

        // ---------------------------------------------------------------- the Stage and the Tower
        void TheStage(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var sc = G(ctx, streetL + 14f, 0); var rot = Quaternion.LookRotation(-D); var rng = new System.Random(88); life.stageAt = sc + D * 6f + up * 6f;
            mk.Prism(concrete, sc - up * .28f, up, 21f, .42f, 32); mk.Prism(goldM, sc + up * .14f, up, 14.65f, .012f, 28); mk.Prism(concrete, sc + up * .17f, up, 14.4f, .012f, 28);
            for (var k = 0; k < 12; k++) Remember(sc + Quaternion.Euler(0, k * 30f, 0) * D * 15f, 1); Remember(sc - D * 6f, 1);
            // the stage: a deck with steps up one side, a truss of lights over it, speakers, a gold backdrop
            var st = sc + D * 9.5f + up * .14f; mk.Box(dark, st + up * .6f, new Vector3(14f, 1.2f, 6f), rot); solid.Box(dark, st + up * .5f, new Vector3(14f, 1.4f, 6f), rot); mk.Box(goldM, st + up * 1.21f - D * 2.9f, new Vector3(14f, .03f, .2f), rot);
            for (var k = 0; k < 3; k++) { var sp = st + S * 7.6f - D * (1.6f - k * .9f) + up * (.2f + k * .4f); mk.Box(concreteDark, sp, new Vector3(1.2f, .4f, .9f), rot); solid.Box(concreteDark, sp - up * .2f, new Vector3(1.2f, .8f, .9f), rot); }
            foreach (var e in new[] { -1f, 1f }) { var tw = st + S * e * 6.6f - D * 2.6f; mk.Box(iron, tw + up * 4.4f, new Vector3(.3f, 7f, .3f), rot); ArtistBits.PostCollider(ctx.Root, tw + up * 1.2f, 6f, .2f); var bw = st + S * e * 6.6f + D * 2.6f; mk.Box(iron, bw + up * 4.4f, new Vector3(.3f, 7f, .3f), rot); mk.Box(iron, st + S * e * 6.6f + up * 7.9f, new Vector3(.3f, .3f, 5.5f), rot); }
            mk.Box(iron, st + up * 7.9f - D * 2.6f, new Vector3(13.5f, .3f, .3f), rot); mk.Box(iron, st + up * 7.9f + D * 2.6f, new Vector3(13.5f, .3f, .3f), rot); for (var k = 0; k < 9; k++) { var lp = st + up * 7.6f - D * 2.6f + S * (-6f + k * 1.5f); mk.Prism(iron, lp + up * .1f, (-up - D * .3f).normalized, .16f, .34f, 6, 1.4f); mk.Blob(stageM, lp - up * .3f - D * .1f, Vector3.one * .14f, rng, 0f); halos.Add(lp - up * .34f - D * .2f, .9f, new Color(1f, .8f, .4f), .4f); }
            mk.Box(Paint(Purple), st + up * 4.5f + D * 2.7f, new Vector3(13f, 6.4f, .1f), rot); for (var k = 0; k < 5; k++) mk.Box(goldM, st + up * (5.4f + (k % 2) * .6f) + D * 2.62f + S * (-1.6f + k * .8f), new Vector3(.5f, 1.4f + (k % 2) * .5f, .04f), rot); mk.Box(goldM, st + up * 4.5f + D * 2.62f, new Vector3(4.4f, .5f, .04f), rot); Sign(st + up * 3.5f + D * 2.6f, -D, "THE STAGE", .8f, Gold);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var sp = st + S * e * 8.4f - D * 2.2f + up * (.75f + k * 1.5f) - up * .14f; mk.Box(Paint(new Color(.13f, .13f, .16f)), sp, new Vector3(1.6f, 1.5f, 1.2f), rot); mk.Prism(dark, sp - D * .6f, -D, .56f, .02f, 14); mk.Prism(coneM, sp - D * .62f, -D, .4f, .02f, 12); if (k == 0) solid.Box(dark, sp + up * 1.5f, new Vector3(1.6f, 4.5f, 1.2f), rot); }
            var mc = st + up * 1.2f - D * 1.4f; mk.Prism(iron, mc, up, .02f, 1.5f, 4); mk.Prism(iron, mc, up, .18f, .02f, 8); mk.Blob(goldM, mc + up * 1.56f, new Vector3(.05f, .08f, .05f), rng, 0f);
            string[] mic = { "You grab the mic. \"MAKE SOME NOISE!\" The whole mesa makes some noise.", "\"When I say victory, you say avenue!\" It works. It actually works.", "One two, one two. Even the mic check gets a cheer up here.", "You say your own name into the mic. The Tower's boombox says it back, bigger." }; var mn = 0;
            Spot("The Stage's microphone", mc + up * 1.4f, "Grab the mic", new Vector3(1f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, mc + up * 2.6f, mic[mn++ % mic.Length], 4.5f); };
            ArtistBits.PointLight(ctx.Root, st + up * 5f - D * 1f, new Color(1f, .82f, .5f), 22f, 1.6f);
            // the crowd, at the barrier
            for (var k = 0; k < 7; k++) { var bp = sc + D * 3.6f + S * (-6f + k * 2f) + up * .19f; mk.Box(iron, bp + up * .55f, new Vector3(1.9f, .06f, .06f), rot); mk.Box(iron, bp + up * .15f, new Vector3(1.9f, .05f, .05f), rot); foreach (var e in new[] { -.9f, .9f }) mk.Box(iron, bp + S * e + up * .3f, new Vector3(.05f, .6f, .05f), rot); } Col(sc + D * 3.6f + up * .5f, new Vector3(14f, 1f, .2f), rot);
            string[][] crowd = { new[] { "ENCORE! They haven't even started. ENCORE ANYWAY!", "I've been at the barrier since the last golden hour. So, since forever." }, new[] { "Hands up! Hands UP! There you go!", "If you get on that stage I will scream your name. What's your name?" }, new[] { "The bass off that Tower rearranged my organs. Five stars.", "Get up there! Steps are on the right!" } };
            for (var k = 0; k < 8; k++) Folk(sc + D * (2.2f - (k % 2) * 1.6f) + S * (-5.6f + k * 1.6f) + up * .19f, D, k % 3 == 1 ? Resident.Pose.Stand : Resident.Pose.Wave, crowd[k % 3]);
            // the Tower: thirty-four metres of concrete with gold in every window, and the boombox on its roof
            var tb = sc + D * 24f; const float TW = 10f, TH = 34f; mk.Box(concrete, tb + up * (TH / 2f - 1f), new Vector3(TW, TH + 2f, TW), rot); solid.Box(concrete, tb + up * (TH / 2f - 1f), new Vector3(TW, TH + 2f, TW), rot); mk.Box(concreteDark, tb + up * (TH + .2f), new Vector3(TW + .6f, .4f, TW + .6f), rot); mk.Box(brickM, tb + up * 2f, new Vector3(TW + .2f, 4f, TW + .2f), rot);
            for (var fl = 0; fl < 9; fl++) for (var side = 0; side < 4; side++) for (var k = 0; k < 4; k++) { var r = rot * Quaternion.Euler(0, side * 90f, 0); mk.Box(rng.NextDouble() < .72 ? windowM : darkGlass, tb + up * (6.4f + fl * 3f) + r * new Vector3(-3.3f + k * 2.2f, 0, TW / 2f + .04f), new Vector3(1.4f, 1.8f, .06f), r); }
            Boombox(mk, tb + up * (TH + .4f) - D * 1f, rot, 14f); halos.Add(tb + up * (TH + 5f) - D * 8f, 12f, Gold, .1f);
            Mark(sc, 22f); Mark(tb, 9f);
            Shot(ctx, "the stage and the tower", sc - D * 15f + S * 7f + up * 2.4f, tb + up * 24f);
            Shot(ctx, "from the stage", st + up * 2.9f + D * 1f, sc - D * 30f + up * 2f);
        }

        /// The blocks down Victory Avenue, built after the lanes so nothing stands in one.
        void Blocks(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var rng = new System.Random(12); var off = StreetW / 2f + 3.4f + BlockD / 2f;
            foreach (var e in new[] { 1f, -1f })
            for (var d = 32f + (e > 0 ? 0f : 5f); d <= streetL - 12f; d += 12.4f, i++)
            {
                var p = Wp(d, e * off); if (!ctx.IsLand(p.x, p.z, 8f) || InFootprint(ctx, p.x, p.z, 7.4f) || LaneNear(p.x, p.z, 7.8f)) continue;
                Block(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e), i, rng, BlockW, BlockD); built++;
            }
            Debug.Log($"The Hip-Hop Boroughs: {built} blocks on Victory Avenue");
        }

        static readonly string[] Shops = { "KICKS", "RECORDS", "BODEGA", "BARBER", "SLICES", "24 HR", "STUDIO", "GOLD", "TAPES", "FADES" };

        /// A city block: brick or concrete, gold in most windows, a shop under an awning, a water tower on the roof, a tag on the side.
        void Block(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i, System.Random rng, float W, float Dp)
        {
            var up = Vector3.up; var H = 8.2f + (i % 4) * 3f; var wall = Paint(Walls[i % Walls.Length]); var trim = (i % 2 == 0) ? concreteDark : brickM; var floors = Mathf.FloorToInt((H - 4.2f) / 2.9f) + 1;
            mk.Box(wall, f.P(0, H / 2f - .6f, 0), new Vector3(W, H + 1.2f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .6f, 0), new Vector3(W, H + 1.2f, Dp), f.r); mk.Box(trim, f.P(0, H + .15f, 0), new Vector3(W + .4f, .3f, Dp + .4f), f.r); mk.Box(trim, f.P(0, 3.5f, Dp / 2f + .05f), new Vector3(W + .1f, .2f, .12f), f.r);
            for (var fl = 0; fl < floors; fl++) for (var side = 0; side < 4; side++) { var r = f.r * Quaternion.Euler(0, side * 90f, 0); var half = (side % 2 == 0 ? Dp : W) / 2f; var span = side % 2 == 0 ? W : Dp; for (var k = 0; k < 3; k++) mk.Box(rng.NextDouble() < .62 ? windowM : darkGlass, f.P(0, 4.9f + fl * 2.9f, 0) + r * new Vector3((k - 1) * span * .3f, 0, half + .03f), new Vector3(1.3f, 1.6f, .06f), r); }
            // the shop: a lit window, a door, an awning, its name
            var tag = Tags[(i * 5 + 2) % Tags.Length]; mk.Box(windowM, f.P(-W * .2f, 1.6f, Dp / 2f + .03f), new Vector3(W * .44f, 1.8f, .06f), f.r); mk.Box(woodM, f.P(W * .28f, 1.2f, Dp / 2f + .03f), new Vector3(1.2f, 2.3f, .06f), f.r); mk.Box(Paint(tag), f.P(0, 3f, Dp / 2f + .7f), new Vector3(W * .86f, .08f, 1.5f), f.r * Quaternion.Euler(14f, 0, 0)); mk.Box(Paint(tag), f.P(0, 2.72f, Dp / 2f + 1.42f), new Vector3(W * .86f, .3f, .04f), f.r);
            Sign(f.P(0, 2.72f, Dp / 2f + 1.45f), f.Fwd, Shops[i % Shops.Length], .2f, tag.grayscale > .55f ? Ink : Cream); halos.Add(f.P(-W * .2f, 1.6f, Dp / 2f + .7f), 1.8f, new Color(1f, .8f, .4f), .22f);
            // a tag sprayed on the side wall
            var sr = f.r * Quaternion.Euler(0, 90f, 0); var sx = W / 2f; for (var k = 0; k < 3; k++) mk.Box(Paint(Tags[(i + k * 2) % Tags.Length]), f.P(sx + .03f + k * .035f, 1.5f + k * .3f, -1f + k * .9f), new Vector3(2.2f - k * .5f, 1.5f - k * .3f, .02f), sr * Quaternion.Euler(0, 0, -10f + k * 12f));
            // the roof: a water tower, or a row of vents
            if (i % 3 != 1) { var wt = f.P(-W * .2f, H + .3f, -Dp * .15f); foreach (var (x, zz) in new[] { (-.6f, -.6f), (.6f, -.6f), (.6f, .6f), (-.6f, .6f) }) mk.Prism(iron, wt + new Vector3(x, 0, zz), up, .05f, 1.6f, 4); mk.Prism(woodM, wt + up * 1.6f, up, 1f, 1.8f, 10); mk.Cone(concreteDark, wt + up * 3.4f, 1.1f, .7f, 10); }
            else for (var k = 0; k < 3; k++) mk.Box(concreteDark, f.P(-2f + k * 2f, H + .7f, 0), new Vector3(1f, .8f, 1f), f.r);
            if (i % 4 == 0) Folk(f.P(W * .28f + 1.2f, 0, Dp / 2f + .7f), f.Fwd, Resident.Pose.Lean, new[] { "Been on this stoop since the block was one storey. Watched it come up.", "You hear the Tower? Rattles my windows. I'd miss it if it stopped.", "Golden hour. Makes even the fire hydrant look like it won something." });
            else if (i % 4 == 2) Hydrant(ctx, mk, f.P(W * .44f, 0, Dp / 2f + 1.6f));
            Mark(f.o, Mathf.Max(W, Dp) * .78f);
        }
    }

    /// The Hip-Hop Boroughs' holder for things you can use and carry.
    public class HipTownLife : MonoBehaviour { }
}
