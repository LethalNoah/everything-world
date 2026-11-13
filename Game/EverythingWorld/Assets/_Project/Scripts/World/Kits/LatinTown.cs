using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Latin Coast's places (part of LatinKit): La Plaza (a jacaranda wrapped in lights, a tiled dance floor with
    /// people dancing, a mariachi band, papel picado from the tree to the lamps, the church with its yellow bell tower and
    /// a bell to ring, a stall of aguas frescas to carry); Calle Mayor (cobbles, painted houses with flowering balconies,
    /// papel picado overhead) running down the hill; and the Harbour (a stone quay, painted fishing boats on the water, a
    /// striped lighthouse, nets, a little beach).
    public partial class LatinKit
    {
        LatinTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.96f, .3f, .4f), new Color(1f, .7f, .2f), new Color(.98f, .96f, .9f), new Color(.2f, .6f, .66f), new Color(.3f, .3f, .7f), new Color(.86f, .3f, .7f), new Color(.6f, .8f, .3f) };
        static readonly Color[] Bottoms = { new Color(.2f, .2f, .3f), new Color(.98f, .96f, .9f), new Color(.5f, .36f, .26f), new Color(.14f, .14f, .16f) };
        static readonly string[] TownTalk =
        {
            "¡Bienvenidos! The party started at lunch. You're right on time.", "Down Calle Mayor to the harbour. The boats are painted every colour, like the houses.", "Dance! Nobody here cares if you know how. That's how you learn.",
            "The bell rings for weddings, birthdays and good songs. It rings a lot.", "Sit, eat something, the sun will wait.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1999);
            tlife = new GameObject("Latin town life").AddComponent<LatinTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Straw, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None }, new[] { Fuchsia, Marigold, Turquoise, Cream }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- La Plaza
        void LaPlaza(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(cobble, c - up * .28f, up, 20f, .42f, 32); for (var k = 0; k < 24; k++) { var d = Quaternion.Euler(0, k * 15f, 0) * D; mk.Box(cobbleDark, c + up * .145f + d * 13f, new Vector3(.1f, .01f, 13f), Quaternion.LookRotation(d)); }
            for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            // the jacaranda in the middle, wrapped in lights, a ring bench round it
            var jt = c + up * .14f; mk.Prism(white, jt, up, 2.4f, .5f, 16); solid.Prism(white, jt - up * .2f, up, 2.4f, .7f, 12); mk.Prism(Paint(Cobalt), jt + up * .5f, up, 2.45f, .04f, 16); mk.Prism(trunkM, jt + up * .5f, up, .5f, 4.6f, 8, .6f); ArtistBits.PostCollider(ctx.Root, jt, 5f, .6f);
            for (var k = 0; k < 8; k++) mk.Blob(k % 3 == 2 ? leafM : purpleM, jt + up * (6f + (k % 3) * .8f) + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * (k == 0 ? 0f : 3f), new Vector3(3.2f, 2f, 3.2f), rng, .2f); for (var k = 0; k < 30; k++) { var a = k * 24f; mk.Blob(lampM, jt + up * (1.6f + k * .12f) + Quaternion.Euler(0, a, 0) * Vector3.forward * .56f, Vector3.one * .06f, rng, 0f); } halos.Add(jt + up * 6f, 6f, Jacaranda, .12f);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * D; Interact.Seat(tlife.transform, jt + d * 2.2f + up * .54f, d, "Sit under the jacaranda").seatHeight = .5f; }
            for (var k = 0; k < 4; k++) Folk(jt + Quaternion.Euler(0, k * 90f + 20f, 0) * D * 2.2f + up * .5f, Quaternion.Euler(0, k * 90f + 20f, 0) * D, Resident.Pose.Sit, new[] { "The tree flowers purple every spring and somebody always cries. Good crying.", "My grandmother danced here. My granddaughter dances here. I sit here.", "Listen to the trumpets. They start the night." }, null, null, .5f);
            // the dance floor: a square of painted tiles, people dancing; the mariachi band on a little stage
            var df = new PlaceFrame(c + D * 9f - S * 7f + up * .15f, (-D + S * .4f).normalized); for (var x = 0; x < 6; x++) for (var z = 0; z < 6; z++) mk.Box(Paint(new[] { Cobalt, Cream, Marigold, Turquoise }[(x + z * 2) % 4]), df.P(-2.75f + x * 1.1f, 0, -2.75f + z * 1.1f), new Vector3(1.06f, .02f, 1.06f), df.r);
            string[][] dance = { new[] { "¡Eso! Feet like that, you're from here now.", "Spin! The skirt does the work, you just spin." }, new[] { "One-two, one-two-three. Or don't count. Counting is for later.", "My feet hurt and I'm not stopping." }, new[] { "This song! Everybody, this song!", "Dance with me, it's rude not to." } };
            for (var k = 0; k < 6; k++) { var p = df.P(-2f + (k % 3) * 2f, 0, -1.4f + (k / 3) * 2.6f); Folk(p, (k % 2 == 0 ? df.Right : -df.Right), Resident.Pose.Wave, dance[k % 3], Local()); }
            var ms = new PlaceFrame(c - D * 9f - S * 9f + up * .14f, (D + S * .7f).normalized); mk.Box(woodM, ms.P(0, .3f, 0), new Vector3(5f, .6f, 3f), ms.r); solid.Box(woodM, ms.P(0, .1f, 0), new Vector3(5f, .8f, 3f), ms.r); for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? Paint(Fuchsia) : Paint(Marigold), ms.P(-2.1f + k * .84f, 2.8f, -1.2f), new Vector3(.84f, .06f, 3f), ms.r * Quaternion.Euler(-8f, 0, 0)); foreach (var e in new[] { -2.4f, 2.4f }) { mk.Prism(woodM, ms.P(e, .6f, -1.4f), up, .06f, 2.2f, 4); ArtistBits.PostCollider(rootT, ms.P(e, 0, -1.4f), 2.8f, .08f); }
            var band = new[] { "Trumpets up! ¡Ajúa!", "Request? We know them all. We know them better than the radio.", "La Cucaracha? Again? Of course again." };
            for (var k = 0; k < 4; k++) { var bp = ms.P(-1.8f + k * 1.2f, .6f, 0); Folk(bp, ms.Fwd, Resident.Pose.Work, band, Local()).workRate = 1.4f; } mk.Box(Paint(new Color(.1f, .1f, .12f)), ms.P(1.8f, 1.2f, .5f), new Vector3(.5f, .7f, .26f), ms.r); mk.Prism(Paint(new Color(.86f, .66f, .4f)), ms.P(1.8f, .9f, .66f), ms.Fwd, .3f, .1f, 10);
            string[] grito = { "You let out a grito. The band answers with a trumpet run. The whole plaza cheers.", "¡Ajúa! You didn't know you could make that noise. Now everyone knows.", "The violins swell. Somebody throws a flower. It hits you. You keep it." }; var gn = 0;
            Spot("The mariachi stage", ms.P(0, 1.4f, 1.8f), "Shout a grito", new Vector3(4f, 1.4f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, ms.P(0, 3.4f, 1.4f), grito[gn++ % grito.Length], 4.5f); };
            // the church: white walls, a yellow bell tower, a bell to ring
            var ch = new PlaceFrame(c - D * 13f + S * 8f + up * .14f, (D - S * .6f).normalized); const float W = 8f, Dp = 10f, H = 6f;
            mk.Box(white, ch.P(0, H / 2f, -Dp / 2f), new Vector3(W, H, Dp), ch.r); solid.Box(white, ch.P(0, H / 2f, -Dp / 2f), new Vector3(W, H, Dp), ch.r); mk.GableRoof(tile, ch.P(0, H, -Dp / 2f), new Vector3(W + .4f, 0, Dp + .4f), 2f, ch.r); mk.Box(white, ch.P(0, H + 1f, .02f), new Vector3(W, 2f, .3f), ch.r); mk.Prism(white, ch.P(0, H + 2f, 0), ch.Fwd, 1.4f, .3f, 12);
            mk.Box(woodM, ch.P(0, 1.6f, .04f), new Vector3(1.8f, 3.2f, .06f), ch.r); mk.Prism(windowM, ch.P(0, 4.4f, .02f), ch.Fwd, .7f, .06f, 14); halos.Add(ch.P(0, 4.4f, .5f), 1.4f, new Color(1f, .78f, .46f), .3f);
            var tw = ch.P(W / 2f + 1.6f, 0, -1.6f); mk.Box(Paint(new Color(1f, .78f, .24f)), tw + up * 5f, new Vector3(3f, 10f, 3f), ch.r); solid.Box(woodM, tw + up * 5f, new Vector3(3f, 10f, 3f), ch.r); mk.Box(white, tw + up * 10.2f, new Vector3(3.3f, .4f, 3.3f), ch.r); mk.Box(Paint(new Color(1f, .78f, .24f)), tw + up * 11.6f, new Vector3(2.4f, 2.4f, 2.4f), ch.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(dark, tw + up * 11.6f + ch.Fwd * 1.21f, new Vector3(1.2f, 1.6f, .02f), ch.r); mk.Box(dark, tw + up * 11.6f + ch.Right * 1.21f * e, new Vector3(.02f, 1.6f, 1.2f), ch.r); }
            mk.Cone(tile, tw + up * 12.8f, 1.6f, 2f, 4, 45f); mk.Box(Paint(new Color(1f, .86f, .4f)), tw + up * 15.3f, new Vector3(.12f, 1f, .12f), ch.r); mk.Box(Paint(new Color(1f, .86f, .4f)), tw + up * 15.5f, new Vector3(.6f, .12f, .12f), ch.r);
            mk.Prism(Paint(new Color(.8f, .6f, .26f)), tw + up * 12.2f, -up, .4f, .7f, 10, 1.5f); halos.Add(tw + up * 11.4f, 1.6f, new Color(1f, .8f, .4f), .3f); life.bellAt = tw + up * 11.6f;
            string[] bell = { "You pull the rope. DONG. Every pigeon in town takes off at once and the plaza cheers.", "DONG. DONG. Somebody somewhere yells \"¡Felicidades!\" — for what, nobody knows.", "The bell rings out over the harbour. A boat answers with its horn." }; var bn = 0;
            Spot("The bell rope", tw + up * 1.2f + ch.Fwd * 1.6f, "Ring the bell", new Vector3(.8f, 1.2f, .8f)).onUse = _ => { life.played = Time.time; life.rang = Time.time; Say(tlife.transform, tw + up * 3f + ch.Fwd * 1.8f, bell[bn++ % bell.Length], 4.5f); };
            // aguas frescas: a stall with big glass jars and cups to carry
            var af = new PlaceFrame(c + D * 11f + S * 10f + up * .14f, (-D - S * .7f).normalized); mk.Box(Paint(Turquoise), af.P(0, .5f, 0), new Vector3(2.4f, 1f, .9f), af.r); mk.Box(white, af.P(0, 1.02f, 0), new Vector3(2.5f, .05f, 1f), af.r); Col(af.P(0, .5f, 0), new Vector3(2.4f, 1f, .9f), af.r); foreach (var e in new[] { -1.1f, 1.1f }) mk.Prism(woodM, af.P(e, 1f, -.4f), up, .03f, 1.4f, 4); for (var k = 0; k < 5; k++) mk.Box(k % 2 == 0 ? Paint(Fuchsia) : white, af.P(-1f + k * .5f, 2.5f, -.1f), new Vector3(.5f, .04f, 1.2f), af.r * Quaternion.Euler(12f, 0, 0));
            var drinks = new[] { (new Color(.9f, .2f, .4f), "agua de jamaica, deep red and cold"), (new Color(.96f, .92f, .82f), "horchata with cinnamon on top"), (new Color(.5f, .8f, .3f), "agua de limón") };
            for (var k = 0; k < 3; k++) { var (col, label) = drinks[k]; mk.Prism(WorldMaterials.Solid(new Color(.8f, .9f, .94f), .9f), af.P(-.8f + k * .8f, 1.05f, -.2f), up, .18f, .5f, 10); mk.Prism(GlowM(col, .5f), af.P(-.8f + k * .8f, 1.07f, -.2f), up, .17f, .4f, 10); var cm = new MeshKit(); cm.Prism(WorldMaterials.Solid(new Color(.8f, .9f, .94f), .9f), Vector3.zero, Vector3.up, .045f, .14f, 8); cm.Prism(Paint(col), new Vector3(0, .01f, 0), Vector3.up, .04f, .1f, 8); Thing(cm, "Agua fresca", label, af.P(-.8f + k * .8f, 1.06f, .3f), Quaternion.identity, new Vector3(.1f, .16f, .1f), .1f, false); }
            Folk(af.P(0, 0, -.9f), af.Fwd, Resident.Pose.Stand, new[] { "Jamaica, horchata or limón? Wrong answer: only one.", "It's hot even when the sun's going down. Drink!", "On the house. The house is my grandmother. She insists." }, Local(Residents.Tool.None, true));
            // papel picado from the tree out to lanterns round the edge
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * D; var lp = c + d * 18.4f + up * .14f; Lantern(ctx, mk, lp, -d, k % 2 == 0); Picado(mk, jt + up * 4.6f + d * 1f, lp + up * 3.6f, k); }
            Folk(c + D * 5f + S * 2f + up * .14f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "¡Hola! Welcome, welcome! Come in, there's room on the floor.", "Calle Mayor goes down to the harbour. Mind the cobbles, they're older than God.", "Ring the church bell if you're happy. Everybody does." });
            Mark(c, 21f); Mark(ch.o, 9f); Mark(tw, 3f);
            Shot(ctx, "la plaza", c + D * 15f + S * 7f + up * 2.6f, jt + up * 4f);
        }

        // ---------------------------------------------------------------- Calle Mayor
        void CalleMayor(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, Mathf.Sin(d / 30f) * 2f)); if (pts.Count < 2) return;
            Strip(mk, pts, cobble, StreetW, .13f, ctx); var mid = new List<Vector3>(); foreach (var p in pts) mid.Add(p); Strip(mk, mid, cobbleDark, .5f, .15f, ctx); foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 26f; d <= streetL - 4f; d += 10f, n++) { var a = G(ctx, d, -(StreetW / 2f + .6f) + Mathf.Sin(d / 30f) * 2f); var b = G(ctx, d + 3f, StreetW / 2f + .6f + Mathf.Sin(d / 30f) * 2f); Lantern(ctx, mk, a, S, n % 2 == 0); Lantern(ctx, mk, b, -S, n % 2 == 1); Picado(mk, a + up * 3.7f, b + up * 3.7f, n); }
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 14f) route.Add(G(ctx, d, 1.4f + Mathf.Sin(d / 30f) * 2f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 14f) route.Add(G(ctx, d, -1.4f + Mathf.Sin(d / 30f) * 2f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Down to the boats and back up for dinner. Every evening.", "Buenas tardes! Buenas tardes! You say it to everyone. It's free." }, null, route.ToArray()).speed = .9f;
            Shot(ctx, "down calle mayor", G(ctx, 21f, .4f) + up * 2.2f, G(ctx, streetL + 40f, 0) + up * 2f);
        }

        // ---------------------------------------------------------------- the Harbour
        void Harbour(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var hc = G(ctx, streetL + 6f, 0); var rng = new System.Random(77); var rot = Quaternion.LookRotation(D);
            mk.Prism(cobble, hc - up * .28f, up, 14f, .42f, 26); for (var k = 0; k < 10; k++) Remember(hc + Quaternion.Euler(0, k * 36f, 0) * D * 13f, 1);
            if (toSea)
            {
                // the quay: a stone pier out over the water, bollards, nets drying
                var q0 = hc + D * 10f; var qy = Mathf.Max(waterY + 1.4f, hc.y); var qa = new Vector3(q0.x, qy, q0.z); var qb = qa + D * 34f; var qm = (qa + qb) / 2f;
                mk.Box(cobbleDark, qm, new Vector3(6f, .3f, 34f), rot); solid.Box(cobbleDark, qm - up * 1.4f, new Vector3(6f, 3f, 34f), rot); mk.Box(Paint(new Color(.6f, .54f, .5f)), qm - up * 2f, new Vector3(6.2f, 4f, 34.2f), rot); for (var d = 2f; d < 34f; d += 4f) Remember(qa + D * d, 1);
                var ramp = hc + D * 6f; mk.Box(cobbleDark, (ramp + qa) / 2f, new Vector3(6f, .3f, Vector3.Distance(ramp, qa) + .4f), Quaternion.LookRotation((qa - ramp).normalized)); solid.Box(cobbleDark, (ramp + qa) / 2f - up * .3f, new Vector3(6f, .6f, Vector3.Distance(ramp, qa) + .4f), Quaternion.LookRotation((qa - ramp).normalized));
                for (var d = 4f; d < 34f; d += 6f) foreach (var e in new[] { -2.6f, 2.6f }) { var bp = qa + D * d + S * e; mk.Prism(iron, bp + up * .15f, up, .16f, .5f, 8, .8f); ArtistBits.PostCollider(ctx.Root, bp, .6f, .18f); }
                foreach (var e in new[] { -2.95f, 2.95f }) Col(qm + S * e + up * .6f, new Vector3(.1f, 1.2f, 34f), rot);
                // the painted boats on the water either side
                for (var k = 0; k < 6; k++) { var e = k % 2 == 0 ? 1f : -1f; var bp = qa + D * (6f + (k / 2) * 9f) + S * e * 6f; bp.y = waterY + .1f; Boat(mk, bp, Quaternion.LookRotation(D) * Quaternion.Euler(0, e * 12f + k * 5f, 0), k); }
                // the lighthouse at the end of the quay, striped red and white
                var lh = qb - D * 2f; for (var k = 0; k < 8; k++) mk.Prism(k % 2 == 0 ? Paint(new Color(.86f, .2f, .2f)) : white, lh + up * (.15f + k * 1.5f), up, 1.4f - k * .08f, 1.5f, 12, .95f); solid.Prism(white, lh, up, 1.4f, 12f, 12); mk.Prism(iron, lh + up * 12.1f, up, 1f, .2f, 12); mk.Box(lampM, lh + up * 12.8f, new Vector3(1f, 1.2f, 1f), rot); mk.Cone(Paint(new Color(.86f, .2f, .2f)), lh + up * 13.4f, 1f, 1f, 8); life.beam = lh + up * 12.8f; halos.Add(lh + up * 12.8f, 3f, new Color(1f, .9f, .6f), .4f); ArtistBits.PointLight(ctx.Root, lh + up * 12f, new Color(1f, .86f, .6f), 30f, 1.6f);
                Folk(qa + D * 14f + S * 2.2f, S, Resident.Pose.Sit, new[] { "Caught nothing. Sang the whole time though. The fish liked it.", "That one's my boat. The pink one. They're all the pink one, if you ask the owners.", "When the lighthouse comes on, that's dinner." }, Local(Residents.Tool.Rod), null, .3f);
                Shot(ctx, "the harbour", hc + S * 8f + up * 2.4f, lh + up * 6f);
            }
            // nets on poles, crates of fish, a little seafood stand
            var na = hc - S * 9f; var nb = na - D * 4f; foreach (var g in new[] { na, nb }) { mk.Prism(woodM, g - up * .2f, up, .05f, 2.4f, 4); ArtistBits.PostCollider(ctx.Root, g, 2.2f, .07f); } for (var k = 0; k < 7; k++) mk.Prism(Paint(new Color(.4f, .56f, .54f)), Vector3.Lerp(na, nb, k / 6f) + up * 2.2f, -up, .01f, 1.6f, 3);
            var ss = new PlaceFrame(hc + S * 9f + up * .14f, -S); mk.Box(Paint(Marigold), ss.P(0, .5f, 0), new Vector3(2.4f, 1f, .9f), ss.r); mk.Box(white, ss.P(0, 1.02f, 0), new Vector3(2.5f, .05f, 1f), ss.r); Col(ss.P(0, .5f, 0), new Vector3(2.4f, 1f, .9f), ss.r); for (var k = 0; k < 4; k++) mk.Box(k % 2 == 0 ? Paint(Cobalt) : white, ss.P(-.9f + k * .6f, 2.4f, -.1f), new Vector3(.6f, .04f, 1.2f), ss.r * Quaternion.Euler(12f, 0, 0)); foreach (var e in new[] { -1.1f, 1.1f }) mk.Prism(woodM, ss.P(e, 1f, -.4f), up, .03f, 1.4f, 4); Sign(ss.P(0, 1.9f, -.45f), ss.Fwd, "MARISCOS", .2f, Ink); mk.Box(Paint(Cream), ss.P(0, 1.9f, -.48f), new Vector3(1.8f, .34f, .02f), ss.r);
            Folk(ss.P(0, 0, -.9f), ss.Fwd, Resident.Pose.Stand, new[] { "Fish came in this morning, ceviche went out this afternoon. Circle of life.", "Lime, salt, chili. That's the recipe for everything.", "Sit on the wall and eat it watching the boats. That's the only way." }, Local(Residents.Tool.None, true));
            Mark(hc, 14f);
        }

        /// A painted fishing boat on the water: two colours, a stripe, a name on the bow.
        void Boat(MeshKit mk, Vector3 g, Quaternion rot, int k)
        {
            var up = Vector3.up; var a = Paint(Walls[k % Walls.Length]); var b = Paint(Walls[(k + 3) % Walls.Length]); var f = rot * Vector3.forward; var r = rot * Vector3.right;
            mk.Box(a, g + up * .3f, new Vector3(1.6f, .6f, 5.4f), rot); mk.Box(b, g + up * .64f, new Vector3(1.64f, .1f, 5.44f), rot); mk.Box(woodM, g + up * .56f, new Vector3(1.4f, .04f, 5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Prism(a, g + up * .3f + f * e * 2.7f, (f * e + up * .4f).normalized, .5f, 1f, 4, .2f); mk.Box(woodM, g + up * .7f, new Vector3(1.4f, .08f, .3f), rot);
            if (k % 2 == 0) { mk.Prism(woodM, g + up * .6f + f * .6f, up, .05f, 3f, 4); mk.Box(white, g + up * 2.4f + f * .6f, new Vector3(.05f, 1.6f, 1.4f), rot); }
            life.boats.Add((g, rot));
        }

        /// The houses down Calle Mayor, built after the lanes so nothing stands in one.
        void Houses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var rng = new System.Random(12);
            foreach (var e in new[] { 1f, -1f })
            for (var d = 28f + (e > 0 ? 0f : 4.5f); d <= streetL - 6f; d += 9f, i++)
            {
                var p = Wp(d, e * (StreetW / 2f + 5f) + Mathf.Sin(d / 30f) * 2f); if (!ctx.IsLand(p.x, p.z, 5f) || InFootprint(ctx, p.x, p.z, 5.4f) || LaneNear(p.x, p.z, 5.4f) || InNoGrass(p.x, p.z, 3f)) continue;
                House(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e), i, rng); built++;
            }
            Debug.Log($"The Latin Coast: {built} painted houses on Calle Mayor");
        }

        /// A painted house: two storeys, flat roof with a parapet, a balcony of flowers, shutters, a door with a lamp.
        void House(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i, System.Random rng)
        {
            var up = Vector3.up; float W = 7f, Dp = 6f, H = i % 3 == 1 ? 3.6f : 6.4f; var wall = Paint(Walls[i % Walls.Length]); var trim = Paint(Walls[(i + 4) % Walls.Length]);
            mk.Box(wall, f.P(0, H / 2f - .4f, 0), new Vector3(W, H + .8f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .4f, 0), new Vector3(W, H + .8f, Dp), f.r); mk.Box(white, f.P(0, H + .2f, 0), new Vector3(W + .2f, .4f, Dp + .2f), f.r); mk.Box(trim, f.P(0, .3f, Dp / 2f + .02f), new Vector3(W, .6f, .04f), f.r);
            var dx = (i % 2 == 0 ? -1f : 1f) * W * .25f; mk.Box(Paint(Walls[(i + 2) % Walls.Length]), f.P(dx, 1.2f, Dp / 2f + .04f), new Vector3(1.1f, 2.2f, .06f), f.r); mk.Box(white, f.P(dx, 2.38f, Dp / 2f + .04f), new Vector3(1.4f, .14f, .1f), f.r); mk.Box(lampM, f.P(dx + .8f, 2.2f, Dp / 2f + .14f), new Vector3(.16f, .24f, .16f), f.r); halos.Add(f.P(dx + .8f, 2.2f, Dp / 2f + .4f), 1f, new Color(1f, .8f, .5f), .4f);
            void Win(float x, float y, bool lit) { mk.Box(lit ? windowM : Paint(new Color(.2f, .2f, .26f)), f.P(x, y, Dp / 2f + .04f), new Vector3(.8f, 1.2f, .06f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(trim, f.P(x + e * .62f, y, Dp / 2f + .08f), new Vector3(.4f, 1.3f, .04f), f.r * Quaternion.Euler(0, e * -20f, 0)); }
            Win(-dx, 1.4f, i % 2 == 0); if (H > 4f) { Win(-W * .25f, 4.4f, i % 3 == 0); Win(W * .25f, 4.4f, false); var bx = 0f; mk.Box(white, f.P(bx, 3.6f, Dp / 2f + .5f), new Vector3(3f, .1f, 1f), f.r); for (var k = 0; k < 11; k++) mk.Box(iron, f.P(bx - 1.45f + k * .29f, 4f, Dp / 2f + .98f), new Vector3(.03f, .8f, .03f), f.r); mk.Box(iron, f.P(bx, 4.4f, Dp / 2f + .98f), new Vector3(3f, .04f, .04f), f.r); for (var k = 0; k < 4; k++) { var pp = f.P(bx - 1.1f + k * .73f, 3.66f, Dp / 2f + .7f); mk.Prism(tile, pp, up, .14f, .22f, 7, 1.2f); mk.Blob(k % 2 == 0 ? Paint(Fuchsia) : Paint(new Color(.94f, .3f, .2f)), pp + up * .34f, new Vector3(.26f, .16f, .2f), rng, .3f); } for (var k = 0; k < 3; k++) mk.Box(leafM, f.P(bx - 1f + k * 1f, 3.2f, Dp / 2f + 1f), new Vector3(.3f, .9f, .03f), f.r); }
            if (i % 4 == 0) Folk(f.P(dx + 1f, 0, Dp / 2f + .7f), f.Fwd, Resident.Pose.Lean, new[] { "Painted it pink last year. The neighbours painted theirs pinker. It's a war.", "Come in! No? Then wave! Thank you!", "The bougainvillea's growing into my bedroom. I let it." });
            else if (i % 4 == 2) { var ch = f.P(-dx, 0, Dp / 2f + .8f); mk.Box(woodM, ch + up * .45f, new Vector3(.46f, .05f, .46f), f.r); mk.Box(Paint(Walls[(i + 5) % Walls.Length]), ch + up * .8f - f.Fwd * .21f, new Vector3(.46f, .7f, .05f), f.r); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) mk.Box(woodM, ch + up * .22f + f.r * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), f.r); Interact.Seat(tlife.transform, ch + up * .5f, f.Fwd); }
            Mark(f.o, 5.4f);
        }
    }

    /// The Latin Coast's holder for things you can use and carry.
    public class LatinTownLife : MonoBehaviour { }
}
