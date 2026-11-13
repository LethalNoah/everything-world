using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Far Shores' places (part of FarKit): the Compass (a mosaic compass rose, a signpost of far-off cities, a
    /// trolley of suitcases to carry off, a postcard rack to read, an ice-cream cart); the Promenade (black and white
    /// waves underfoot, palms and globe lamps, benches, a coconut stand, a kite seller); the beach (umbrellas and
    /// loungers, a sandcastle to add to, a beach ball, a pier out over the water); and the Caravan Track east into
    /// the dunes (cairns, flags, a well, three camels who will be greeted).
    public partial class FarKit
    {
        FarTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.96f, .95f, .9f), new Color(.94f, .48f, .35f), new Color(.18f, .65f, .63f), new Color(1f, .8f, .3f), new Color(.3f, .5f, .8f), new Color(.9f, .4f, .55f), new Color(.5f, .7f, .4f) };
        static readonly Color[] Bottoms = { new Color(.9f, .86f, .74f), new Color(.2f, .26f, .44f), new Color(.5f, .4f, .3f), new Color(.96f, .95f, .9f) };
        static readonly string[] TownTalk =
        {
            "Lovely day. It's always this exact day. Nobody's complained yet.", "Where are you in from? Doesn't matter. Everybody here is from somewhere else.", "Walk down the Promenade and you'll pass through about nine countries before you reach the water.",
            "The dunes are that way, behind you. Take water. Take a hat. Take your time.", "I came for a week. I forget which year.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1966);
            tlife = new GameObject("Far Shores town life").AddComponent<FarTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Straw, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Cap }, new[] { new Color(.9f, .82f, .6f), Cream, Coral }); l.tool = tool; l.apron = apron; return l; }
        /// Someone of the desert: indigo robes, head wrapped.
        Residents.Look Nomad() { var l = Local(); l.top = Indigo * (.8f + (float)prng.NextDouble() * .5f); l.bottom = new Color(.16f, .18f, .36f); l.hat = Residents.Hat.Beanie; l.hatColor = prng.Next(2) == 0 ? new Color(.14f, .16f, .34f) : Cream; return l; }
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

        // ---------------------------------------------------------------- the Compass
        void Compass(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            // the plaza: pale stone, and a compass rose in black, white, teal and coral set into it
            mk.Prism(stone, c - up * .28f, up, 21f, .4f, 32); mk.Prism(tileB, c + up * .122f, up, 8.4f, .01f, 32); mk.Prism(Paint(new Color(.9f, .87f, .78f)), c + up * .126f, up, 8f, .01f, 32);
            for (var k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f, 0) * U; var len = k % 4 == 0 ? 7.6f : k % 2 == 0 ? 5.2f : 3f; var m = k % 4 == 0 ? tileB : k % 2 == 0 ? Paint(Teal) : Paint(Coral); var side = Vector3.Cross(up, d); var a = c + d * 1.2f + up * .15f; mk.Tri(m, a - side * (len * .1f), a + side * (len * .1f), c + d * len + up * .15f, c - up); mk.Tri(m, a + side * (len * .1f), a - side * (len * .1f), c + d * len + up * .15f, c + up * 9f); }
            mk.Prism(Paint(Sun), c + up * .15f, up, 1.2f, .012f, 16); foreach (var (d, t) in new[] { (U, "N"), (R, "E"), (-U, "S"), (-R, "W") }) { var t2 = ArtistBits.Label(rootT, t, c + d * 9.4f + up * .17f, Vector3.up, .9f, Ink, TextAnchor.MiddleCenter); t2.transform.rotation = Quaternion.LookRotation(Vector3.down, d); }
            // the signpost of far-off cities, north-east of the middle
            var sp = c + U * 7f + R * 7f + up * .12f; mk.Prism(white, sp - up * .2f, up, .1f, 5.2f, 6); mk.Blob(Paint(Sun), sp + up * 5.1f, Vector3.one * .22f, prng, 0f); ArtistBits.PostCollider(ctx.Root, sp, 5f, .14f);
            var cities = new[] { ("RIO  5 min", -100f, Teal), ("KINGSTON  9 min", -70f, Coral), ("TIJUANA  7 min", -120f, Sun), ("ADDIS ABABA  3 min", 40f, Paints[6]), ("TESSALIT  6 min", 95f, Ochre), ("HONOLULU  11 min", -85f, Paints[5]), ("LONDON  far", 0f, Paints[4]) };
            for (var k = 0; k < cities.Length; k++) { var (text, yaw, col) = cities[k]; var d = Quaternion.Euler(0, yaw, 0) * U; var rot = Quaternion.LookRotation(Vector3.Cross(up, d)); var at = sp + up * (4.5f - k * .46f) + d * 1.2f; mk.Box(Paint(col), at, new Vector3(2.2f, .34f, .05f), rot); mk.Tri(Paint(col), at + d * 1.1f + up * .26f, at + d * 1.1f - up * .26f, at + d * 1.5f, at); foreach (var e in new[] { -1f, 1f }) Sign(at + rot * Vector3.forward * e * .035f, rot * Vector3.forward * e, text, .14f, col.grayscale > .6f ? Ink : Cream); }
            // a luggage trolley: suitcases to carry off
            var tr = c - U * 9f + R * 9f + up * .12f; var trot = Quaternion.LookRotation((U + R).normalized); mk.Box(iron, tr + up * .2f, new Vector3(1.2f, .06f, 2f), trot); foreach (var e in new[] { -1f, 1f }) { foreach (var s in new[] { -.8f, .8f }) mk.Prism(dark, tr + up * .1f + trot * new Vector3(e * .56f, 0, s), trot * Vector3.right * e, .1f, .05f, 8); mk.Prism(Paint(Sun), tr + up * .2f + trot * new Vector3(e * .56f, 0, -.96f), up, .025f, 1.1f, 4); } mk.Box(Paint(Sun), tr + up * 1.3f + trot * new Vector3(0, 0, -.96f), new Vector3(1.16f, .05f, .05f), trot); Col(tr + up * .2f, new Vector3(1.2f, .4f, 2f), trot);
            string[] tags = { "a suitcase with too many stickers", "a hat box", "a guitar case going somewhere warm" }; for (var k = 0; k < 3; k++) { var sm = new MeshKit(); var sc = Paint(Paints[(k * 2 + 1) % Paints.Length]); if (k == 1) sm.Prism(sc, Vector3.zero, Vector3.up, .22f, .26f, 12); else { sm.Box(sc, new Vector3(0, k == 2 ? .12f : .2f, 0), k == 2 ? new Vector3(.4f, .24f, 1.1f) : new Vector3(.66f, .4f, .24f), Quaternion.identity); sm.Box(woodM, new Vector3(0, k == 2 ? .25f : .42f, 0), new Vector3(.2f, .03f, .04f), Quaternion.identity); } Thing(sm, "Luggage", tags[k], tr + up * .24f + trot * new Vector3(k == 2 ? .3f : -.2f, 0, -.5f + k * .5f), trot * Quaternion.Euler(0, k * 30f, 0), k == 1 ? new Vector3(.44f, .26f, .44f) : k == 2 ? new Vector3(.4f, .26f, 1.1f) : new Vector3(.66f, .44f, .24f), .6f); }
            // a postcard rack to read, an ice-cream cart
            var pc = c - U * 8f - R * 10f + up * .12f; mk.Prism(iron, pc, up, .3f, .05f, 8); mk.Prism(iron, pc, up, .03f, 1.8f, 5); for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 90f + (k / 4) * 20f, 0) * Vector3.forward; mk.Box(Paint(Paints[k % Paints.Length]), pc + up * (.8f + (k / 4) * .36f) + d * .2f, new Vector3(.22f, .3f, .01f), Quaternion.LookRotation(d) * Quaternion.Euler(-10f, 0, 0)); } ArtistBits.PostCollider(ctx.Root, pc, 1.8f, .3f);
            string[] cards = { "\"Weather is here, wish you were lovely.\"", "\"Arrived Tuesday. It is still Tuesday. Send nothing, I have everything.\"", "\"The band never stops. Neither do I. Love to the dog.\"", "\"Went into the dunes for tea. Stayed for three songs, each one twenty minutes. Back never.\"", "\"Day-O! Counted the bananas. One short. Suspect the tally man.\"" }; var cn = 0;
            Spot("Postcards", pc + up * 1.2f, "Read a postcard", new Vector3(.9f, 1.6f, .9f)).onUse = _ => Say(tlife.transform, pc + up * 2.6f, cards[cn++ % cards.Length], 5.5f);
            var ic = c + U * 11f - R * 8f + up * .12f; var irot = Quaternion.LookRotation((-U + R * .5f).normalized); mk.Box(white, ic + up * .7f, new Vector3(1.4f, .9f, .8f), irot); mk.Box(Paint(Paints[5]), ic + up * .3f, new Vector3(1.44f, .12f, .84f), irot); foreach (var e in new[] { -1f, 1f }) mk.Prism(dark, ic + up * .3f + irot * new Vector3(e * .74f, 0, 0), irot * Vector3.right * e, .3f, .06f, 12); mk.Prism(white, ic + irot * new Vector3(0, 1.15f, -.2f), up, .025f, 1.2f, 4); mk.Cone(Paint(Paints[5]), ic + irot * new Vector3(0, 2.1f, -.2f), 1f, .36f, 8); mk.Cone(white, ic + irot * new Vector3(0, 2.26f, -.2f), .5f, .2f, 8, 22.5f); Col(ic + up * .6f, new Vector3(1.5f, 1.2f, .9f), irot);
            for (var k = 0; k < 3; k++) { var im = new MeshKit(); im.Cone(Paint(new Color(.85f, .68f, .4f)), new Vector3(0, .16f, 0), .045f, -.16f, 8); im.Blob(Paint(Paints[new[] { 5, 2, 0 }[k]]), new Vector3(0, .2f, 0), Vector3.one * .055f, prng, .1f); Thing(im, "Ice cream", new[] { "a guava ice cream", "a mango ice cream", "a pistachio ice cream" }[k], ic + up * 1.16f + irot * new Vector3(-.4f + k * .4f, 0, .1f), Quaternion.identity, new Vector3(.12f, .28f, .12f), .1f, false); }
            Folk(ic + irot * new Vector3(.2f, -.02f, -.9f), irot * Vector3.forward, Resident.Pose.Stand, new[] { "Guava, mango, pistachio. It won't melt. Nothing here is in a hurry, not even ice cream.", "First one's free. They're all the first one." }, Local(Residents.Tool.None, true));
            // benches, lamps, and somebody who knows where everything is
            Bench(mk, c + U * 16f + R * 2f + up * .12f, -U); Bench(mk, c - U * 16f - R * 2f + up * .12f, U); Bench(mk, c + R * 16.5f - U * 5f + up * .12f, -R);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * U; GlobeLamp(ctx, mk, c + d * 19f + up * .12f); }
            var guide = Local(); guide.hat = Residents.Hat.Straw; Folk(c - R * 5f + U * 5f + up * .12f, (R - U).normalized, Resident.Pose.Wave, new[] { "Welcome to the Far Shores! West is the sea: follow the waves in the pavement. East is the desert: follow the flags.", "Every stop on the Promenade is somewhere else. Rio's at the bottom. You'll know it by the drums.", "The camp in the dunes always has tea on. They pour it three times. Stay for all three." }, guide);
            Mark(c, 22f);
            Shot(ctx, "the compass", c + U * 15f + R * 13f + up * 4.2f, c - U * 2f - R * 4f + up * .5f);
        }

        // ---------------------------------------------------------------- the Promenade
        void Promenade(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; if (roadL < 30f) return; const int lanes = 9; var lw = PromW / lanes;
            // the waves: six lanes, each changing colour along its length a little out of step with the next
            for (var j = 0; j < lanes; j++)
            {
                var u = -PromW / 2f + (j + .5f) * lw; var run = new List<Vector3>(); var colour = -1;
                for (var d = 20f; d <= roadL + .01f; d += 1f)
                {
                    var c = Mathf.FloorToInt((d + Mathf.Sin(j * Mathf.PI * 2f / lanes) * 1.5f + 3f) / 2.4f) % 2; var p = G(ctx, u, -d);
                    if (c != colour && run.Count > 0) { run.Add(p); if (run.Count > 1) Strip(mk, run, colour == 0 ? tileW : tileB, lw + .02f, .14f, ctx); run = new List<Vector3>(); }
                    colour = c; run.Add(p);
                }
                if (run.Count > 1) Strip(mk, run, colour == 0 ? tileW : tileB, lw + .02f, .14f, ctx);
            }
            for (var d = 20f; d <= roadL; d += 2f) Remember(G(ctx, 0, -d), 1);
            // palms down both sides, a globe lamp between each pair, benches facing the walk
            var rng = new System.Random(31); var n = 0;
            for (var d = 26f; d <= roadL - 4f; d += 9f, n++)
                foreach (var e in new[] { -1f, 1f })
                {
                    var g = G(ctx, e * (PromW / 2f + 1.4f), -d); if (InFootprint(ctx, g.x, g.z, .5f)) continue;
                    if (n % 2 == 0) PalmTree(ctx, mk, g, 1.25f, rng, true); else GlobeLamp(ctx, mk, g);
                    if (n % 4 == 1 && e > 0 || n % 4 == 3 && e < 0) { var b = G(ctx, e * (PromW / 2f + 1.2f), -d - 3f); if (!InFootprint(ctx, b.x, b.z, 1f)) Bench(mk, b, -U * e); }
                }
            // a coconut stand: take one
            var cs = G(ctx, PromW / 2f + 2.4f, -Mathf.Min(40f, roadL * .4f)); var cr = Quaternion.LookRotation(-U); mk.Box(bleached, cs + up * .5f, new Vector3(2f, 1f, .8f), cr); foreach (var e in new[] { -.9f, .9f }) mk.Prism(woodM, cs + R * e - U * .3f, up, .05f, 2.4f, 4); mk.Box(thatch, cs + up * 2.4f, new Vector3(2.6f, .12f, 1.6f), cr * Quaternion.Euler(-10f, 0, 0)); Col(cs + up * .5f, new Vector3(2f, 1f, .8f), cr); Sign(cs + up * .56f - U * .41f, -U, "COCO FRIO", .2f, Ink);
            for (var k = 0; k < 4; k++) { var cm = new MeshKit(); cm.Blob(Paint(new Color(.4f, .6f, .26f)), new Vector3(0, .14f, 0), Vector3.one * .14f, rng, .06f); cm.Prism(Paint(Coral), new Vector3(.03f, .26f, 0), new Vector3(.3f, 1f, 0).normalized, .01f, .16f, 4); Thing(cm, "Coconut", "a cold coconut with a straw", cs + up * 1.02f + R * (-.6f + k * .4f), Quaternion.identity, new Vector3(.28f, .3f, .28f), .4f, false); }
            Folk(cs + U * .9f, -U, Resident.Pose.Stand, new[] { "Coco frio! Cold as the bottom of the sea.", "I open it, you drink it, the sea does the rest.", "They're all the same coconut. I've never got to the bottom of the pile." }, Local(Residents.Tool.None, true)); Mark(cs, 2.6f);
            // a kite seller further down, kites up on strings
            if (roadL > 110f) { var ks = G(ctx, -(PromW / 2f + 2.6f), -Mathf.Min(96f, roadL * .6f)); for (var k = 0; k < 4; k++) { var top = ks + up * (5f + k * 1.6f) + R * (-1.5f + k * 1f) - U * (k * .8f); mk.Prism(dark, ks + up * 1.2f, (top - ks - up * 1.2f).normalized, .006f, (top - ks - up * 1.2f).magnitude, 3); var kr = Quaternion.LookRotation(U) * Quaternion.Euler(20f, 0, 45f + k * 8f); mk.Box(Paint(Paints[(k * 2) % Paints.Length]), top, new Vector3(.9f, .9f, .02f), kr); mk.Box(Paint(Paints[(k * 2 + 3) % Paints.Length]), top - up * .7f, new Vector3(.06f, .9f, .02f), kr * Quaternion.Euler(0, 0, -45f)); }
                Folk(ks, U, Resident.Pose.Stand, new[] { "Kites! The wind here only blows one way: nicely.", "That top one's been up since spring. I've stopped holding it. It stays out of politeness." }); Mark(ks, 2f); }
            // people out walking
            var route = new List<Vector3>(); for (var d = 26f; d <= roadL - 6f; d += 16f) route.Add(G(ctx, 2.6f, -d) + up * .15f); for (var d = roadL - 6f; d >= 26f; d -= 16f) route.Add(G(ctx, -2.6f, -d) + up * .15f);
            if (route.Count > 3) { Folk(route[0], -R, Resident.Pose.Walk, new[] { "Down to the water and back. Twice a day. It's a different country each time.", "Don't step on the black ones. ...I'm joking. Step on anything." }, null, route.ToArray()).speed = 1.1f; }
            Shot(ctx, "down the promenade", G(ctx, 0, -22f) + up * 2.6f, G(ctx, 0, -Mathf.Min(roadL, 130f)) + up * 1.6f);
        }

        // ---------------------------------------------------------------- the beach
        void Beach(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(beach2.x, beach2.y); var rng = new System.Random(52);
            mk.Prism(sandM, c - up * .26f, up, 23f, .34f, 30); for (var k = 0; k < 8; k++) Remember(c + Quaternion.Euler(0, k * 45f, 0) * U * 12f, 1); Remember(c, 1);
            // umbrellas and loungers, facing the sea
            var un = 0; foreach (var (u, r) in new[] { (9f, -8f), (4f, -13f), (-5f, -12f), (-11f, -7f), (14f, 2f), (-15f, 3f) }) { var g = c + U * u + R * r; g.y = ctx.Ground(g.x, g.z) + .06f; if (InFootprint(ctx, g.x, g.z, 1f)) continue; Umbrella(ctx, mk, g, -R, un++); }
            Folk(c + U * 4f - R * 11.9f + up * .5f, -R, Resident.Pose.Sit, new[] { "I'm not asleep. I'm listening with my eyes closed.", "Wake me when the tide's come all the way in. ...It doesn't, here. That's the idea." }, null, null, .4f);
            // a sandcastle to add to, a beach ball
            var sc = c + U * 1f - R * 6f + up * .08f; var wet = Paint(new Color(.86f, .76f, .56f)); mk.Prism(wet, sc, up, 1.1f, .3f, 12, .8f); foreach (var (x, zz) in new[] { (-.6f, -.6f), (.6f, -.6f), (.6f, .6f), (-.6f, .6f), (0f, 0f) }) { var h = x == 0f ? .9f : .6f; mk.Prism(wet, sc + new Vector3(x, .3f, zz), up, .22f, h, 8, .85f); mk.Cone(wet, sc + new Vector3(x, .3f + h, zz), .2f, .26f, 8); } mk.Prism(Paint(Coral), sc + up * 1.46f, up, .01f, .3f, 3); mk.Box(Paint(Coral), sc + up * 1.7f + R * .1f, new Vector3(.2f, .12f, .01f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, sc, 1f, .9f);
            string[] build = { "You add a tower. It leans. It is now the best tower.", "You dig a moat. The sea politely declines to fill it.", "One more turret. A crab moves in immediately." }; var bn = 0;
            Spot("The sandcastle", sc + up * .9f, "Add to the sandcastle", new Vector3(2.2f, 1.4f, 2.2f)).onUse = _ => Say(tlife.transform, sc + up * 2.4f, build[bn++ % build.Length], 4.5f);
            var bb = new MeshKit(); for (var k = 0; k < 6; k++) bb.Blob(Paint(k % 2 == 0 ? Cream : Paints[(k / 2 * 2 + 1) % Paints.Length]), new Vector3(0, .22f, 0) + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * .04f, new Vector3(.2f, .22f, .2f), rng, 0f); Thing(bb, "Beach ball", "a beach ball", c + U * 7f - R * 3f + up * .1f, Quaternion.identity, new Vector3(.44f, .44f, .44f), .2f, false);
            // surfboards stuck in the sand
            for (var k = 0; k < 3; k++) { var sb = c - U * 8f - R * (1f + k * .9f) + up * .06f; mk.Box(Paint(Paints[(k * 2 + 1) % Paints.Length]), sb + up * 1.1f, new Vector3(.5f, 2.2f, .07f), Quaternion.Euler(0, 20f + k * 12f, 6f)); mk.Box(white, sb + up * 1.1f, new Vector3(.08f, 2.2f, .08f), Quaternion.Euler(0, 20f + k * 12f, 6f)); ArtistBits.PostCollider(ctx.Root, sb, 2f, .2f); }
            Mark(c, 24f);
            Shot(ctx, "the beach", c + R * 17f + U * 5f + up * 2.6f, c - R * 26f + up * 1f);
        }

        // ---------------------------------------------------------------- the Caravan Track
        void CaravanTrack(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(71); var pts = new List<Vector3>();
            for (var d = 20f; d <= trackL - 14f; d += 2f) pts.Add(G(ctx, -d / trackL * 14f + Mathf.Sin(d / 20f) * 3f, d)); if (pts.Count < 2) return;
            Strip(mk, pts, duneM, 4f, .12f, ctx); foreach (var p in pts) Remember(p, 2);
            // flags on poles and cairns to follow
            for (var i = 6; i < pts.Count; i += 9) { var side = Vector3.Cross(up, (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]).normalized) * ((i / 9) % 2 == 0 ? 1f : -1f); var g = pts[i] + side * 3f; g.y = ctx.Ground(g.x, g.z); if ((i / 9) % 2 == 0) { mk.Prism(woodM, g - up * .2f, up, .04f, 3.2f, 4); mk.Tri(Paint(Indigo), g + up * 3f, g + up * 2.4f, g + up * 2.7f + R * 1.1f, g + up * 2.7f - U); mk.Tri(Paint(Indigo), g + up * 3f, g + up * 2.7f + R * 1.1f, g + up * 2.4f, g + up * 2.7f + U); ArtistBits.PostCollider(ctx.Root, g, 3f, .06f); } else Cairn(mk, g, rng); }
            // a well with a bucket, half way
            var wl = pts[pts.Count / 2] + U * 6f; wl.y = ctx.Ground(wl.x, wl.z); mk.Prism(stone, wl - up * .1f, up, 1f, .9f, 12); mk.Prism(dark, wl + up * .7f, up, .8f, .11f, 12); foreach (var e in new[] { -1f, 1f }) mk.Prism(woodM, wl + R * e * 1f, up, .06f, 2.2f, 4); mk.Prism(woodM, wl + up * 2.1f - R * 1.1f, R, .05f, 2.2f, 5); mk.Prism(dark, wl + up * 2.1f, -up, .01f, 1f, 3); ArtistBits.PostCollider(ctx.Root, wl, 1f, 1f);
            var bk = new MeshKit(); bk.Prism(woodM, Vector3.zero, Vector3.up, .16f, .26f, 8, 1.2f); bk.Prism(glassM, new Vector3(0, .22f, 0), Vector3.up, .17f, .02f, 8); Thing(bk, "Bucket", "a bucket of well water", wl + R * 1.6f + up * .05f, Quaternion.identity, new Vector3(.36f, .3f, .36f), .8f); Mark(wl, 2.4f);
            // three camels resting by the track
            for (var k = 0; k < 3; k++) { var cp = pts[Mathf.Min(pts.Count - 1, pts.Count * 3 / 4)] - U * (5f + k * 3.4f) + R * (k * 1.6f); cp.y = ctx.Ground(cp.x, cp.z); Camel(cp, Quaternion.Euler(0, 60f + k * 40f, 0), rng, k); }
            Folk(pts[Mathf.Min(pts.Count - 1, pts.Count * 3 / 4)] - U * 3f, U, Resident.Pose.Stand, new[] { "They are resting. They have been resting since the last song but one.", "Follow the flags. When you smell tea, you are there.", "The desert is not empty. It is just quiet until somebody plugs in." }, Nomad());
            Shot(ctx, "the caravan track", pts[4] + up * 2.4f - U * 2f, pts[Mathf.Min(pts.Count - 1, 40)] + up * 1.5f);
        }

        /// A camel, sitting. Say hello.
        void Camel(Vector3 g, Quaternion rot, System.Random rng, int i)
        {
            var m = new MeshKit(); var fur = Paint(new Color(.8f, .64f, .42f) * (.9f + i * .06f));
            m.Blob(fur, new Vector3(0, .6f, 0), new Vector3(.5f, .44f, 1f), rng, .05f); m.Blob(fur, new Vector3(0, 1.1f, -.1f), new Vector3(.34f, .34f, .4f), rng, .08f); m.Prism(fur, new Vector3(0, .8f, .8f), new Vector3(0, 1f, .5f).normalized, .16f, 1.2f, 6, .7f); m.Blob(fur, new Vector3(0, 1.94f, 1.5f), new Vector3(.16f, .16f, .3f), rng, .04f); foreach (var e in new[] { -.1f, .1f }) m.Cone(fur, new Vector3(e, 2.04f, 1.36f), .04f, .1f, 4);
            foreach (var e in new[] { -.4f, .4f }) foreach (var q in new[] { -.6f, .7f }) m.Box(fur, new Vector3(e, .16f, q), new Vector3(.16f, .2f, .7f), Quaternion.identity); m.Box(Paint(i % 2 == 0 ? Indigo : Coral), new Vector3(0, 1.06f, -.1f), new Vector3(.76f, .1f, .7f), Quaternion.identity);
            var go = m.BuildSingle("Camel", tlife.transform, false); go.transform.SetPositionAndRotation(g, rot); Col(g + Vector3.up * .7f, new Vector3(1f, 1.4f, 2.2f), rot); string[] hm = { "The camel regards you from a great height, sitting down.", "It chews. Slowly. In a rhythm you could play guitar to.", "A long blink. That was a greeting. You've been greeted." }; var n = i;
            Interact.Toy(go, Toy.Kind.Custom, "Greet the camel", new Vector3(1.4f, 2.2f, 2.6f)).onUse = _ => Say(tlife.transform, g + Vector3.up * 2.8f, hm[n++ % hm.Length], 4.5f); noGrass.Add(new Vector3(g.x, 2f, g.z));
        }
    }

    /// The Far Shores' holder for things you can use and carry.
    public class FarTownLife : MonoBehaviour { }
}
