using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Four-on-the-Floor Steppe's places (part of FloorKit): the Main Stage (a deck between truss towers, a huge LED
    /// wall, speaker stacks, a DJ booth to step up to, moving lights on the roof truss and lasers fanning from the deck, a
    /// crowd dancing in front, a barrier and a sound desk), Festival Avenue (banners, food trucks, festoon lights) and the
    /// Ferris wheel at its end, turning slowly with its lights on.
    public partial class FloorKit
    {
        FloorTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.1f, .1f, .12f), new Color(.86f, .86f, .9f), Magenta, Blue, Laser, Amber };
        static readonly Color[] Bottoms = { new Color(.1f, .1f, .12f), new Color(.2f, .24f, .4f), new Color(.3f, .3f, .3f) };
        static readonly string[] TownTalk =
        {
            "Hands up! Sorry. Habit.", "Main stage goes all night. Then it goes some more.", "Meet you by the Ferris wheel. Everybody says that. Nobody finds anybody.",
            "I've lost my tent. I've lost my friends. Best night ever.", "Feel that kick drum? That's the floor. Four on it.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2011);
            tlife = new GameObject("Floor town life").AddComponent<FloorTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None }, new[] { Laser, Magenta, Blue, Amber }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Main Stage
        void MainStage(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            var st = new PlaceFrame(c - D * 18f, D); life.stageFwd = D; const float W = 24f, Dp = 10f, H = 1.6f, T = 14f;
            mk.Prism(ground, c - D * 4f - up * .3f, up, 22f, .36f, 32); for (var k = -3; k <= 3; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 12f, 0);
            // the deck
            mk.Box(dark, st.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), st.r); solid.Box(dark, st.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), st.r); mk.Box(GlowM(Laser, 1f), st.P(0, H + .01f, Dp / 2f - .1f), new Vector3(W, .02f, .1f), st.r);
            // truss towers and the roof truss
            foreach (var e in new[] { -1f, 1f }) { var tw = st.P(e * (W / 2f + .6f), 0, 0); foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Prism(truss, tw + st.Right * a * .5f + st.Fwd * b * .5f, up, .06f, T + 1f, 4); for (var k = 1; k < 14; k++) mk.Box(truss, tw + up * k, new Vector3(1.1f, .04f, 1.1f), st.r); solid.Box(truss, tw + up * (T / 2f), new Vector3(1.2f, T, 1.2f), st.r); }
            for (var k = 0; k < 2; k++) mk.Box(truss, st.P(0, T + .4f, k == 0 ? -.5f : .5f), new Vector3(W + 2f, .1f, .1f), st.r);
            // the LED wall
            var led = new Material(WorldMaterials.Glow(Blue, 1.3f)) { name = "Floor LED wall" }; var led2 = new Material(WorldMaterials.Glow(Magenta, 1.3f)) { name = "Floor LED wall 2" }; life.led = led; life.led2 = led2;
            mk.Box(dark, st.P(0, H + 5.6f, -Dp / 2f + .4f), new Vector3(W - 2f, 11f, .4f), st.r); solid.Box(dark, st.P(0, H + 5.6f, -Dp / 2f + .4f), new Vector3(W - 2f, 11f, .4f), st.r);
            for (var x = 0; x < 20; x++) for (var y = 0; y < 10; y++) mk.Box((x + y) % 2 == 0 ? led : led2, st.P(-W / 2f + 1.6f + x * 1.04f, H + .8f + y * 1.04f, -Dp / 2f + .62f), new Vector3(1f, 1f, .02f), st.r);
            // speaker stacks
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 4; k++) { var sp = st.P(e * (W / 2f - 1.4f), H + .6f + k * 1.2f, Dp / 2f - 1f); mk.Box(dark, sp, new Vector3(2.2f, 1.1f, 1.2f), st.r); foreach (var q in new[] { -.5f, .5f }) mk.Prism(Paint(new Color(.2f, .2f, .22f)), sp + st.Right * q + st.Fwd * .61f, st.Fwd, .4f, .02f, 14); }
            // the DJ booth
            var dj = st.P(0, H, 1f); mk.Box(dark, dj + up * .55f, new Vector3(4f, 1.1f, 1.2f), st.r); mk.Box(GlowM(Laser, 1.4f), dj + up * .55f + st.Fwd * .61f, new Vector3(3.6f, .1f, .02f), st.r); foreach (var e in new[] { -1f, 1f }) mk.Prism(Paint(new Color(.3f, .3f, .34f)), dj + up * 1.12f + st.Right * e * 1f, up, .4f, .03f, 16); BoxCol(rootT, dj + up * .55f, new Vector3(4f, 1.1f, 1.2f), st.r);
            string[] drop = { "You hit the button. Everything goes dark for one beat, then the whole stage explodes with light and the crowd screams.", "Three, two, one... the lasers fan out over every head on the steppe.", "You throw your hands up. Fifty thousand people throw theirs up back." }; var dn = 0;
            Spot("The DJ booth", dj + up * 1.4f, "Drop the beat", new Vector3(4f, 1f, 1.4f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, dj + up * 3f, drop[dn++ % drop.Length], 5f); };
            var stairs = st.P(W / 2f - 3f, 0, Dp / 2f + .8f); for (var k = 0; k < 5; k++) { mk.Box(dark, stairs + up * (k * .32f + .16f) - st.Fwd * (k * .3f), new Vector3(1.6f, .32f + k * .64f, .4f), st.r); solid.Box(dark, stairs + up * (k * .32f + .16f) - st.Fwd * (k * .3f), new Vector3(1.6f, .32f + k * .64f, .4f), st.r); }
            // moving lights on the roof truss, lasers from the deck
            for (var k = 0; k < 8; k++) { var lp = st.P(-W / 2f + 1.5f + k * (W - 3f) / 7f, T + .2f, 0); mk.Box(dark, lp, new Vector3(.5f, .4f, .5f), st.r); var go = new GameObject("Moving light"); go.transform.SetParent(ctx.Root, false); go.transform.position = lp - up * .3f; var l = go.AddComponent<Light>(); l.type = LightType.Spot; l.range = 60f; l.spotAngle = 14f; l.intensity = 5f; l.color = Neon[k % Neon.Length]; life.movers.Add((go.transform, k * .8f)); var beam = new MeshKit(); beam.Prism(GlowM(Neon[k % Neon.Length], .25f), Vector3.zero, Vector3.forward, .03f, 30f, 6, 6f, 0f, false); var bg = beam.BuildSingle("Light beam", go.transform, false); bg.transform.localPosition = Vector3.zero; }
            for (var k = 0; k < 6; k++) { var lp = st.P(-6f + k * 2.4f, H + .1f, Dp / 2f - .3f); mk.Box(dark, lp, new Vector3(.4f, .3f, .4f), st.r); var lz = new MeshKit(); lz.Prism(stickM, Vector3.zero, Vector3.forward, .02f, 70f, 4); var lg = lz.BuildSingle("Laser", ctx.Root, false); lg.transform.position = lp + up * .2f; life.lasers.Add((lg.transform, k * 1.1f)); }
            ArtistBits.PointLight(ctx.Root, st.P(0, H + 6f, Dp), new Color(.6f, .7f, 1f), 40f, 2.4f);
            // the crowd, the barrier and the sound desk
            for (var k = 0; k < 4; k++) Folk(c - D * (6f + (k % 2) * 2f) + S * (k * 3.6f - 5.4f), -D, Resident.Pose.Wave, k == 0 ? new[] { "This is the one! This is the one!", "I can't hear you! I love you!", "Don't you worry. Don't you worry, child." } : null, Local());
            for (var k = 0; k < 3; k++) Folk(c - D * 3f + S * (k * 4f - 4f), -D, Resident.Pose.Stand, null, Local());
            var bar = st.P(0, 0, Dp / 2f + 3f); for (var k = -8; k <= 8; k++) mk.Box(truss, bar + st.Right * k * 1.2f + up * .55f, new Vector3(1.1f, 1.1f, .1f), st.r); solid.Box(truss, bar + up * .55f, new Vector3(20.4f, 1.1f, .3f), st.r);
            var desk = c + D * 8f; mk.Box(dark, desk + up * .5f, new Vector3(3f, 1f, 1.6f), Quaternion.LookRotation(-D)); mk.Box(GlowM(Blue, 1f), desk + up * 1.02f, new Vector3(2.6f, .02f, 1.2f), Quaternion.LookRotation(-D)); foreach (var e in new[] { -1.4f, 1.4f }) mk.Prism(truss, desk + S * e, up, .05f, 3.4f, 4); mk.Box(Paint(new Color(.2f, .2f, .22f)), desk + up * 3.4f, new Vector3(3.4f, .1f, 2f), Quaternion.LookRotation(-D)); BoxCol(ctx.Root, desk + up * .5f, new Vector3(3f, 1f, 1.6f), Quaternion.LookRotation(-D));
            Folk(desk + D * 1.3f, -D, Resident.Pose.Stand, new[] { "Sound desk. Don't touch anything. Especially that.", "We're running it hot tonight." }, Local());
            Folk(c + D * 4f + S * 6f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Steppe! Main stage is right there. You can't miss it. You literally can't.", "Festival Avenue goes to the Ferris wheel. Food trucks all the way.", "Get up on the stage and hit the button. Go on." });
            Mark(c, 23f); Mark(st.o, 14f);
            Shot(ctx, "the main stage", c + D * 12f + S * 6f + up * 3f, st.P(0, 7f, 0));
        }

        // ---------------------------------------------------------------- Festival Avenue
        void Avenue(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 14f; d <= streetL + 4.01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, ground, AveW, .1f, ctx); foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 20f; d <= streetL - 2f; d += 12f, n++)
            {
                foreach (var e in new[] { -1f, 1f }) { var bp = G(ctx, d, e * (AveW / 2f + .6f)); mk.Prism(truss, bp, up, .05f, 6f, 4); ArtistBits.PostCollider(ctx.Root, bp, 6f, .08f); mk.Box(GlowM(Neon[(n + (e > 0 ? 1 : 3)) % Neon.Length], .9f), bp + up * 4.6f + S * e * .5f, new Vector3(.04f, 2.4f, 1f), Quaternion.LookRotation(D)); }
                var a0 = G(ctx, d, -(AveW / 2f + .6f)) + up * 5.4f; var a1 = G(ctx, d, AveW / 2f + .6f) + up * 5.4f; for (var k = 0; k < 9; k++) mk.Blob(lampM, Vector3.Lerp(a0, a1, (k + .5f) / 9f) - up * Mathf.Sin((k + .5f) / 9f * Mathf.PI) * .6f, Vector3.one * .1f, new System.Random(k), 0f); halos.Add((a0 + a1) / 2f - up * .5f, 3f, Amber, .3f); if (n % 2 == 0) ArtistBits.PointLight(ctx.Root, (a0 + a1) / 2f - up * 1f, Amber, 12f, 1.3f);
                if (n % 2 == 1) { var e = n % 4 == 1 ? 1f : -1f; var tp = G(ctx, d + 6f, e * (AveW / 2f + 3.2f)); var f = -S * e; var col = Neon[n % Neon.Length]; var rot = Quaternion.LookRotation(f); mk.Box(white, tp + up * 1.6f, new Vector3(5f, 2.4f, 2.4f), Quaternion.LookRotation(S)); solid.Box(white, tp + up * 1.6f, new Vector3(5f, 2.4f, 2.4f), Quaternion.LookRotation(S)); mk.Box(GlowM(col, 1.2f), tp + up * 1.9f + f * 1.21f, new Vector3(3f, .9f, .02f), rot); mk.Box(Paint(col * .7f), tp + up * 3f + f * 1.6f, new Vector3(4f, .06f, 1f), rot * Quaternion.Euler(-15f, 0, 0)); foreach (var q in new[] { -1.8f, 1.8f }) mk.Prism(dark, tp + up * .4f + Vector3.Cross(up, f) * q + f * 1.25f, f, .4f, .1f, 10); Folk(tp + f * 2.4f, -f, Resident.Pose.Stand, null, Local()); }
            }
            Folk(G(ctx, 24f, 1.6f), D, Resident.Pose.Walk, new[] { "Which way's the main stage? Never mind, I can hear it.", "Ferris wheel! Ferris wheel!" }, Local(), new[] { G(ctx, 24f, 1.6f), G(ctx, streetL - 2f, 1.6f), G(ctx, streetL - 2f, -1.6f), G(ctx, 24f, -1.6f) }).speed = 1.3f;
            Shot(ctx, "along festival avenue", G(ctx, 15f, -1.5f) + up * 2f, G(ctx, streetL + 16f, 0) + up * 10f);
        }

        // ---------------------------------------------------------------- the Ferris wheel
        void Wheel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var wc = G(ctx, streetL + 16f, 0); const float R = 13f; var hub = wc + up * (R + 2f);
            mk.Prism(ground, wc - up * .3f, up, 10f, .36f, 24); for (var k = 0; k < 8; k++) Remember(wc + Quaternion.Euler(0, k * 45f, 0) * D * 10.6f, 1);
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) { var foot = wc + D * e * 1.4f + S * q * 6f; mk.Prism(truss, foot, (hub + D * e * 1.4f - foot).normalized, .2f, Vector3.Distance(hub + D * e * 1.4f, foot), 6); ArtistBits.PostCollider(ctx.Root, foot, 3f, .3f); }
            var wm = new MeshKit(); var bulb = new Material(WorldMaterials.Glow(Amber, 1.8f)) { name = "Floor wheel bulbs" }; life.wheelBulbs = bulb;
            for (var k = 0; k < 48; k++) { float a0 = k * Mathf.PI * 2f / 48f, a1 = (k + 1) * Mathf.PI * 2f / 48f; foreach (var e in new[] { -1f, 1f }) { var p0 = new Vector3(e * 1.2f, Mathf.Sin(a0) * R, Mathf.Cos(a0) * R); var p1 = new Vector3(e * 1.2f, Mathf.Sin(a1) * R, Mathf.Cos(a1) * R); wm.Box(truss, (p0 + p1) / 2f, new Vector3(.12f, .12f, (p1 - p0).magnitude), Quaternion.LookRotation(p1 - p0)); if (k % 2 == 0) wm.Blob(bulb, p0, Vector3.one * .12f, new System.Random(k), 0f); } }
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI * 2f / 16f; var rim = new Vector3(0, Mathf.Sin(a) * R, Mathf.Cos(a) * R); foreach (var e in new[] { -1f, 1f }) wm.Prism(truss, new Vector3(e * 1.2f, 0, 0), (rim + new Vector3(e * 1.2f, 0, 0) - new Vector3(e * 1.2f, 0, 0)).normalized, .05f, R, 4); for (var j = 0; j < 4; j++) wm.Blob(bulb, rim * (j + 1) / 5f + new Vector3(1.2f, 0, 0), Vector3.one * .08f, new System.Random(j), 0f); }
            wm.Prism(truss, new Vector3(-1.6f, 0, 0), Vector3.right, .5f, 3.2f, 12);
            var wheel = wm.BuildSingle("Ferris wheel", ctx.Root, false); wheel.transform.SetPositionAndRotation(hub, Quaternion.LookRotation(S)); life.wheelBase = Quaternion.LookRotation(S); life.wheel = wheel.transform;
            for (var k = 0; k < 16; k++) { var gm = new MeshKit(); var col = Neon[k % Neon.Length]; gm.Box(Paint(col * .8f), new Vector3(0, -1.2f, 0), new Vector3(1.4f, 1.2f, 1.4f), Quaternion.identity); gm.Box(GlowM(col, .6f), new Vector3(0, -.5f, 0), new Vector3(1.6f, .1f, 1.6f), Quaternion.identity); gm.Prism(truss, Vector3.zero, Vector3.down, .03f, .6f, 4); var gg = gm.BuildSingle("Gondola", ctx.Root, false); life.gondolas.Add((gg.transform, k * Mathf.PI * 2f / 16f)); }
            life.wheelHub = hub; life.wheelRadius = R; life.wheelAxis = D; life.wheelPlaneA = S; life.wheelPlaneB = up;
            halos.Add(hub, R * 1.2f, Amber, .12f); ArtistBits.PointLight(ctx.Root, hub - up * 6f + D * -3f, Amber, 30f, 1.6f);
            var booth = wc - D * 7f + S * 4f; mk.Box(white, booth + up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f), Quaternion.LookRotation(-D)); solid.Box(white, booth + up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f), Quaternion.LookRotation(-D)); mk.Box(GlowM(Magenta, 1.4f), booth + up * 2.6f, new Vector3(1.8f, .4f, 1.8f), Quaternion.LookRotation(-D));
            Folk(booth - D * 1.2f, -D, Resident.Pose.Stand, new[] { "One turn, all the way round. You can see every stage from the top.", "It never stops. You just step on. Mind the gap." }, Local());
            Mark(wc, 10f);
            Shot(ctx, "the ferris wheel", wc - D * 24f + S * 10f + up * 3f, hub);
        }
    }

    /// The Four-on-the-Floor Steppe's holder for things you can use and carry.
    public class FloorTownLife : MonoBehaviour { }
}
