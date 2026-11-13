using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Glitterlands' places (part of GlitKit): the Mirrorball (a disco ball as big as a house turning over a floor
    /// of light, a ring of cloud and light pillars, a glitter bar with drinks to carry, a photo booth); the Glitter Mile
    /// (a runway of glowing tiles under arches of light); and the Sky Club (a round dance floor raised on cloud, a DJ
    /// booth with records to drop, a staircase of light up to a floating balcony).
    public partial class GlitKit
    {
        GlitTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(1f, .5f, .74f), new Color(.86f, .86f, .96f), new Color(.6f, .5f, 1f), new Color(.2f, .18f, .24f), new Color(1f, .84f, .4f), new Color(.5f, .9f, 1f), new Color(.6f, 1f, .4f) };
        static readonly Color[] Bottoms = { new Color(.2f, .18f, .26f), new Color(.92f, .9f, .96f), new Color(.86f, .5f, .8f), new Color(.4f, .4f, .5f) };
        static readonly string[] TownTalk =
        {
            "Don't stop! Okay, stop for one second. Okay, go!", "The Mirrorball never stops turning. Neither do I.", "Up the Mile to the Sky Club. Dance the whole way, it's faster.",
            "I came here alone and I've danced with everybody. That's how it works.", "Glitter's not a mess. It's a lifestyle.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2007);
            tlife = new GameObject("Glitter town life").AddComponent<GlitTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Cap }, new[] { Pink, Silver, Lilac, Gold }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Mirrorball
        void Mirrorball(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(floorM, c - up * .28f, up, 20f, .42f, 32); for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            // the floor of light: a disc of glowing tiles
            for (var x = -8; x <= 8; x++) for (var z = -8; z <= 8; z++) { var p = c + D * (z * 1.3f) + S * (x * 1.3f); if ((p - c).sqrMagnitude > 11f * 11f) continue; mk.Box(tileM[((x + 8) * 3 + (z + 8) * 7) % 4], p + up * .155f, new Vector3(1.2f, .02f, 1.2f), Quaternion.LookRotation(D)); }
            // the Mirrorball, turning, hung from four chains from four light pillars
            var bc = c + up * 17f; var bk = new MeshKit(); Ball(bk, Vector3.zero, 5f); var ball = bk.BuildSingle("The Mirrorball", ctx.Root, false); ball.transform.position = bc; life.ball = ball.transform; halos.Add(bc, 9f, Silver, .14f);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * D; var pp = c + d * 15f + up * .14f; mk.Prism(chrome, pp - up * .2f, up, .4f, 20f, 8); ArtistBits.PostCollider(ctx.Root, pp, 6f, .44f); for (var q = 0; q < 6; q++) mk.Box(tileM[(k + q) % 4], pp + up * (2f + q * 3f) + d * .42f, new Vector3(.9f, 1.6f, .04f), Quaternion.LookRotation(d)); var top = pp + up * 19.6f; mk.Prism(iron, top, (bc + up * 4.8f - top).normalized, .05f, Vector3.Distance(top, bc + up * 4.8f), 4); }
            ArtistBits.PointLight(ctx.Root, c + up * 6f, new Color(1f, .9f, 1f), 30f, 2.4f); life.sparkleAt = bc;
            // a ring of cloud round the edge; the glitter bar; the photo booth
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 10f, 0) * D; if (Vector3.Angle(d, D) < 30f) continue; CloudBank(mk, c + d * 21f, 1.4f, rng); }
            var gb = new PlaceFrame(c - D * 12f + S * 9f + up * .14f, (D - S * .7f).normalized); mk.Prism(chrome, gb.P(0, 0, 0), up, 2.2f, 1.1f, 16); mk.Prism(mirror, gb.P(0, 1.1f, 0), up, 2.3f, .06f, 16); BoxCol(ctx.Root, gb.P(0, .55f, 0), new Vector3(4.4f, 1.1f, 4.4f), gb.r); mk.Prism(chrome, gb.P(0, 1.1f, 0), up, .2f, 2.4f, 8); mk.Prism(Paint(Pink), gb.P(0, 3.5f, 0), up, 2.6f, .12f, 16); Sign(gb.P(0, 3.9f, 2.62f), gb.Fwd, "GLITTER BAR", .24f, Ink);
            for (var k = 0; k < 3; k++) { var dm = new MeshKit(); dm.Prism(glassM, Vector3.zero, Vector3.up, .04f, .16f, 8, 1.6f); dm.Prism(GlowM(Disco[k], 1.6f), new Vector3(0, .1f, 0), Vector3.up, .055f, .05f, 8); Thing(dm, "Drink", new[] { "a pink fizz with a sparkler in it", "something blue and glowing", "a lemon glitter soda" }[k], gb.P(-.6f + k * .6f, 1.14f, 2.1f), Quaternion.identity, new Vector3(.12f, .2f, .12f), .1f, false); }
            Folk(gb.P(0, 0, 0) + gb.Fwd * 0f + gb.Right * 0f, gb.Fwd, Resident.Pose.Stand, new[] { "What can I get you? Everything comes with glitter. Everything.", "The pink one's sweet, the blue one glows, the yellow one fizzes in your nose.", "On the house. The house is a cloud. It's very generous." }, Local(Residents.Tool.None, true));
            for (var k = 0; k < 4; k++) { var d = gb.r * Quaternion.Euler(0, k * 50f - 30f, 0) * Vector3.forward; Chair(mk, gb.o + d * 3f, -d, "Sit at the bar"); }
            var pb = new PlaceFrame(c + D * 11f - S * 11f + up * .14f, (-D + S * .6f).normalized); mk.Box(Paint(new Color(.5f, .3f, .7f)), pb.P(0, 1.2f, 0), new Vector3(1.6f, 2.4f, 1.4f), pb.r); Col(pb.P(0, 1.2f, 0), new Vector3(1.6f, 2.4f, 1.4f), pb.r); mk.Box(Paint(Pink), pb.P(0, 1.3f, .71f), new Vector3(1.2f, 1.6f, .02f), pb.r); mk.Box(lampM, pb.P(0, 2.5f, .71f), new Vector3(1.4f, .2f, .02f), pb.r); Sign(pb.P(0, 2.5f, .73f), pb.Fwd, "PHOTOS ★", .11f, Ink);
            string[] snap = { "Flash! Four little photos: a pose, a pose, a pose, and one where you're laughing. That's the good one.", "You and nobody, four times, looking fantastic.", "The booth prints your strip with a sparkle filter you didn't ask for. It's perfect." }; var sn = 0;
            Spot("The photo booth", pb.P(0, 1.3f, 1.1f), "Take some photos", new Vector3(1.2f, 1.6f, .8f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, pb.P(0, 3.2f, .8f), snap[sn++ % snap.Length], 4.5f); }; Mark(pb.o, 2f);
            // dancers under the ball
            string[][] dance = { new[] { "This is my song! They're all my song!", "Spin! Spin! Don't stop spinning!" }, new[] { "I've been dancing since the sun started setting. It's still setting.", "Copy me! Arms up! Now down! Now you!" }, new[] { "Dance with me? You don't have to. I'll dance near you.", "The floor lights up where you step. Try it!" } };
            for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51f, 0) * D; Folk(c + d * (3f + (k % 3) * 2f) + up * .17f, -d, k % 2 == 0 ? Resident.Pose.Wave : Resident.Pose.Stand, dance[k % 3]); }
            Mark(c, 21f);
            Shot(ctx, "under the mirrorball", c + D * 15f + S * 9f + up * 2.6f, bc - up * 4f);
        }

        // ---------------------------------------------------------------- the Glitter Mile
        void GlitterMile(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, floorM, MileW, .13f, ctx); for (var i = 0; i + 1 < pts.Count; i++) Strip(mk, new List<Vector3> { pts[i], pts[i + 1] }, tileM[i % 4], MileW - 2f, .15f, ctx); foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 26f; d <= streetL - 4f; d += 12f, n++)
            {
                // an arch of light across the Mile
                var a = G(ctx, d, -(MileW / 2f + .4f)); var b = G(ctx, d, MileW / 2f + .4f); foreach (var g in new[] { a, b }) { mk.Prism(chrome, g - up * .2f, up, .1f, 4.2f, 6); ArtistBits.PostCollider(ctx.Root, g, 4f, .12f); }
                for (var k = 0; k <= 12; k++) { var t = Mathf.PI * k / 12f; mk.Blob(tileM[(k + n) % 4], (a + b) / 2f + up * (4f + Mathf.Sin(t) * 2.4f) + (b - a) / 2f * Mathf.Cos(t), Vector3.one * .16f, new System.Random(k), 0f); }
                if (n % 2 == 0) ArtistBits.PointLight(ctx.Root, (a + b) / 2f + up * 4f, new Color(.96f, .86f, 1f), 14f, 1.6f);
            }
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 14f) route.Add(G(ctx, d, 1.2f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 14f) route.Add(G(ctx, d, -1.2f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "It's a runway! Everything's a runway!", "Strut. Turn. Look back over the shoulder. Strut." }, null, route.ToArray()).speed = 1.1f;
            Shot(ctx, "up the glitter mile", G(ctx, 21f, .4f) + up * 2.2f, G(ctx, streetL + 30f, 0) + up * 6f);
        }

        // ---------------------------------------------------------------- the Sky Club
        void SkyClub(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var sc = G(ctx, streetL + 16f, 0); var rot = Quaternion.LookRotation(-D); var rng = new System.Random(88);
            mk.Prism(floorM, sc - up * .28f, up, 18f, .42f, 30); for (var k = 0; k < 12; k++) Remember(sc + Quaternion.Euler(0, k * 30f, 0) * D * 17f, 1); Remember(sc - D * 6f, 1);
            // the round dance floor raised on cloud, ramps up both sides
            var df = sc + D * 2f + up * .14f; mk.Prism(floorM, df, up, 9f, 1f, 28); solid.Prism(floorM, df - up * .3f, up, 9f, 1.3f, 16); for (var x = -6; x <= 6; x++) for (var z = -6; z <= 6; z++) { var p = df + D * (z * 1.3f) + S * (x * 1.3f); if ((p - df).sqrMagnitude > 8.3f * 8.3f) continue; mk.Box(tileM[((x + 6) * 5 + (z + 6) * 3) % 4], p + up * 1.01f, new Vector3(1.2f, .02f, 1.2f), rot); }
            for (var k = 0; k < 12; k++) CloudBank(mk, df + Quaternion.Euler(0, k * 30f, 0) * D * 9.4f - up * .2f, 1f, rng);
            foreach (var e in new[] { -1f, 1f }) { var r0 = df - D * 9.8f + S * e * 3f; var r1 = df - D * 7.6f + S * e * 3f + up * 1f; var mid = (r0 + r1) / 2f; mk.Box(floorM, mid, new Vector3(2.4f, .2f, Vector3.Distance(r0, r1) + .3f), Quaternion.LookRotation((r1 - r0).normalized)); solid.Box(floorM, mid - up * .2f, new Vector3(2.4f, .4f, Vector3.Distance(r0, r1) + .3f), Quaternion.LookRotation((r1 - r0).normalized)); Remember(r0, 1); }
            // the DJ booth at the back: decks, records to drop, a wall of light
            var dj = df + D * 6.4f + up * 1f; mk.Box(chrome, dj + up * .55f, new Vector3(3f, 1.1f, 1f), rot); BoxCol(ctx.Root, dj + up * .55f, new Vector3(3f, 1.1f, 1f), rot); foreach (var e in new[] { -.8f, .8f }) { mk.Prism(dark, dj + up * 1.12f + S * e, up, .32f, .02f, 16); mk.Prism(tileM[e > 0 ? 1 : 2], dj + up * 1.14f + S * e, up, .1f, .01f, 10); }
            var wl = dj + D * 1.2f; mk.Box(dark, wl + up * 3f, new Vector3(10f, 6f, .3f), rot); solid.Box(dark, wl + up * 3f, new Vector3(10f, 6f, .3f), rot); for (var x = 0; x < 10; x++) for (var y = 0; y < 6; y++) mk.Box(tileM[(x + y * 2) % 4], wl + up * (.5f + y) + S * (-4.5f + x) - D * .16f, new Vector3(.9f, .9f, .02f), rot); Sign(wl + up * 6.6f - D * .2f, -D, "SKY CLUB", .7f, Cream);
            string[] drop = { "You drop the record. The whole cloud bounces. Somebody screams \"THIS ONE!\"", "Bass in, hands up, glitter falls from nowhere.", "You cross-fade into the chorus and the wall of light does a little wave at you.", "You hold the drop... hold it... there." }; var dn = 0;
            Spot("The decks", dj + up * 1.4f - D * .6f, "Drop the record", new Vector3(3f, .8f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, dj + up * 2.6f, drop[dn++ % drop.Length], 4.5f); };
            Folk(dj + D * .6f, -D, Resident.Pose.Work, new[] { "Requests? I'll play it. I'll play it twice.", "Watch the wall. It dances when you dance.", "Last song of the night? Babe, there's no last song." }, Local(Residents.Tool.None)).workRate = 1.4f;
            // the floating balcony: a staircase of light, then a little floor in the air with a mirror ball
            var st = df + S * 7f; var bal = st + D * 6f + up * 7f; for (var k = 0; k < 18; k++) { var sp = st + up * (1.2f + k * .36f) + D * (k * .36f); mk.Box(tileM[k % 4], sp, new Vector3(1.6f, .12f, .4f), Quaternion.LookRotation(D)); solid.Box(floorM, sp - up * .06f, new Vector3(1.6f, .12f, .4f), Quaternion.LookRotation(D)); }
            DanceFloor(mk, solid, bal, Quaternion.LookRotation(D), 3, 3, 1.3f, .3f); CloudBank(mk, bal - up * 1.6f, 1.3f, rng); Ball(mk, bal + up * 3.2f, .6f); for (var k = 0; k < 4; k++) { var rp = bal + Quaternion.Euler(0, k * 90f, 0) * D * 2.1f + up * .3f; mk.Box(chrome, rp + up * .5f, new Vector3(4f, .05f, .05f), Quaternion.LookRotation(Quaternion.Euler(0, k * 90f + 90f, 0) * D)); solid.Box(chrome, rp + up * .5f, new Vector3(4f, 1f, .1f), Quaternion.LookRotation(Quaternion.Euler(0, k * 90f + 90f, 0) * D)); }
            ArtistBits.PointLight(ctx.Root, df + up * 6f, new Color(1f, .86f, 1f), 26f, 2.2f);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * D; Folk(df + d * (2.4f + (k % 2) * 2.4f) + up * 1.02f, -d, k % 2 == 0 ? Resident.Pose.Wave : Resident.Pose.Stand, new[] { "Highest dance floor in the world! Probably!", "Don't look down. Actually, look down, it's beautiful.", "Every song's the last song. Then there's another." }); }
            Mark(sc, 19f); life.clubAt = df + up * 3f;
            Shot(ctx, "the sky club", sc - D * 16f + S * 8f + up * 3f, df + up * 2f);
        }
    }

    /// The Glitterlands' holder for things you can use and carry.
    public class GlitTownLife : MonoBehaviour { }
}
