using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The IDM Glitchlands' places (part of GlitchKit): the Kernel (a square plaza of dark tiles laced with glowing grid
    /// lines, a tall monolith pixel-sorted into running columns of colour, a terminal to corrupt the data, lines of code
    /// rising out of the floor, blocks hanging and jumping in the air), the Bus (a road of glowing circuit traces with
    /// pulses of data racing along it) and the Stack (a crooked tower of floating blocks slipping in and out of line).
    public partial class GlitchKit
    {
        GlitchTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.06f, .06f, .08f), new Color(.86f, .9f, .92f), Term, Red, new Color(.3f, .32f, .36f) };
        static readonly Color[] Bottoms = { new Color(.06f, .06f, .08f), new Color(.14f, .16f, .2f) };
        static readonly string[] TownTalk =
        {
            "Something's wrong with the render distance. I think it's me.", "Don't stand still too long. You'll get garbage collected.", "The Stack keeps slipping. Nobody's patched it.",
            "If you see yourself twice, that's normal here.", "Every song here is a slightly different error. That's the point.",
        };
        static readonly string[] CodeBits = { "0x3F2A", "NaN", "segfault", "01101001", "while(1)", "ERR 0x00", "null", "glitch()", "bpm=173", "// TODO", "∞", "sudo", "{ }", "0xDEAD", ">_", "EOF", "rand()", "%%%%", "&&", "0b1011" };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2001);
            tlife = new GameObject("Glitch town life").AddComponent<GlitchTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Cap }, new[] { Term, Red, Cyan }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Kernel
        void Kernel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rot = Quaternion.LookRotation(D); var rng = new System.Random(3);
            mk.Box(tile, c - up * .2f, new Vector3(34f, .5f, 34f), rot); for (var i = -8; i <= 8; i++) { mk.Box(gridM, c + up * .06f + S * i * 2f, new Vector3(.05f, .02f, 34f), rot); mk.Box(gridM, c + up * .06f + D * i * 2f, new Vector3(34f, .02f, .05f), rot); }
            for (var k = -2; k <= 2; k++) { Remember(c + D * 16f + S * k * 6f, 0); Remember(c + S * 16f + D * k * 6f, 0); Remember(c - S * 16f + D * k * 6f, 0); }
            // the monolith, pixel-sorted
            var mf = new PlaceFrame(c - D * 10f, D); mk.Box(dark, mf.P(0, 8f, 0), new Vector3(7f, 16f, 1.4f), mf.r); solid.Box(dark, mf.P(0, 8f, 0), new Vector3(7f, 16f, 1.4f), mf.r);
            var sortA = new Material(WorldMaterials.Glow(Sort[0], 1.2f)) { name = "Glitch sort A" }; var sortB = new Material(WorldMaterials.Glow(Sort[2], 1.2f)) { name = "Glitch sort B" }; var sortC = new Material(WorldMaterials.Glow(Sort[1], 1.2f)) { name = "Glitch sort C" }; life.sorts.Add(sortA); life.sorts.Add(sortB); life.sorts.Add(sortC);
            for (var x = 0; x < 14; x++) { var h = 4f + (x * 37 % 11); var y0 = 1f; for (var y = 0; y < (int)(h / .5f); y++) mk.Box(new[] { sortA, sortB, sortC }[(x + y / 3) % 3], mf.P(-3.25f + x * .5f, y0 + y * .5f, .71f), new Vector3(.46f, .46f, .02f), mf.r); }
            for (var k = 0; k < 4; k++) { var bm = new MeshKit(); bm.Box(new[] { sortA, sortB, sortC }[k % 3], Vector3.zero, new Vector3(1.6f, .5f, 1.4f), Quaternion.identity); var bg = bm.BuildSingle("Displaced slice", ctx.Root, false); var home = mf.P((k % 2 == 0 ? 1f : -1f) * (4.4f + k * .3f), 4f + k * 3f, 0); bg.transform.SetPositionAndRotation(home, mf.r); life.jumpers.Add((bg.transform, home, k * 1.3f)); }
            halos.Add(mf.P(0, 8f, 2f), 8f, Term, .2f); ArtistBits.PointLight(ctx.Root, mf.P(0, 6f, 5f), new Color(.6f, 1f, .8f), 24f, 1.8f);
            // the terminal
            var tm = c + D * 4f + S * 5f; mk.Box(dark, tm + up * .55f, new Vector3(1.2f, 1.1f, .8f), Quaternion.LookRotation(-S)); mk.Box(iron, tm + up * 1.5f, new Vector3(1.1f, .8f, .14f), Quaternion.LookRotation(-S) * Quaternion.Euler(-12f, 0, 0)); mk.Box(codeM, tm + up * 1.52f - S * .08f, new Vector3(.9f, .6f, .02f), Quaternion.LookRotation(-S) * Quaternion.Euler(-12f, 0, 0)); BoxCol(ctx.Root, tm + up * .7f, new Vector3(1.2f, 1.4f, .8f), Quaternion.LookRotation(-S));
            string[] corrupt = { "You type one wrong character. The whole Kernel stutters, the monolith tears sideways, and every block in the sky jumps at once.", "rm -rf /sky. The sky flickers. It's still there. Mostly.", "Buffer overflow. For a second the music plays backwards." }; var cn = 0;
            Spot("The terminal", tm + up * 1.4f, "Corrupt the data", new Vector3(1.2f, 1f, 1.2f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, tm + up * 2.6f, corrupt[cn++ % corrupt.Length], 5f); };
            // code rising out of the floor; blocks hanging in the air
            for (var k = 0; k < 24; k++) { var p = c + S * ((float)rng.NextDouble() - .5f) * 30f + D * ((float)rng.NextDouble() - .5f) * 30f; var tmx = ArtistBits.Label(rootT, CodeBits[k % CodeBits.Length], p + up * ((float)rng.NextDouble() * 8f), Quaternion.Euler(0, k * 47f, 0) * D, .3f, k % 5 == 0 ? Red : Term, TextAnchor.MiddleCenter); life.codes.Add((tmx.transform, p, k * .37f)); }
            for (var k = 0; k < 14; k++) Jumper(ctx.Root, c + S * ((k * 37 % 13) - 6f) * 2f + D * ((k * 23 % 11) - 5f) * 2f + up * (5f + (k % 4) * 2f), .5f + (k % 3) * .4f, k % 3 == 0 ? blockM : GlowM(Sort[k % Sort.Length], 1f));
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) Lamp(ctx, mk, c + S * e * 15f + D * q * 15f, true);
            Folk(c + D * 6f - S * 4f, (-D + S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Glitchlands. Mind the missing textures.", "The Bus runs to the Stack. The data goes faster than you.", "Try the terminal. Break something. It's fine. It's always been broken." });
            Folk(tm - S * 1.1f, S, Resident.Pose.Stand, new[] { "I'm debugging the sky. It's going badly.", "Don't press enter. Okay, press enter." }, Local());
            Mark(c, 19f);
            Shot(ctx, "the kernel", c + D * 14f + S * 6f + up * 3f, mf.P(0, 8f, 0));
        }

        // ---------------------------------------------------------------- the Bus
        void Bus(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 17.5f; d <= streetL + 4.01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, tileD, BusW, .1f, ctx); foreach (var p in pts) Remember(p, 1);
            for (var k = 0; k < 5; k++) { var o = (k - 2f) * 1.1f; var lane = new List<Vector3>(); foreach (var p in pts) lane.Add(p + S * o); Strip(mk, lane, gridM, .08f, .13f, ctx); for (var j = 0; j < 3; j++) { var pm = new MeshKit(); pm.Box(codeM, Vector3.zero, new Vector3(.2f, .1f, 1.2f), Quaternion.identity); var pg = pm.BuildSingle("Data pulse", ctx.Root, false); life.pulses.Add((pg.transform, G(ctx, 17.5f, o) + up * .2f, G(ctx, streetL + 4f, o) + up * .2f, k * .23f + j * .33f)); } }
            var n = 0; for (var d = 24f; d <= streetL; d += 12f, n++) foreach (var e in new[] { -1f, 1f }) { var g = G(ctx, d + (e > 0 ? 0f : 6f), e * (BusW / 2f + 1f)); if (n % 2 == 0) Lamp(ctx, mk, g, (n / 2) % 2 == 0); else { mk.Box(dark, g + up * 1f, new Vector3(1.2f, 2f, .8f), Quaternion.LookRotation(S)); for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? codeM : GlowM(Red, 1.4f), g + up * (.5f + k * .25f) - S * e * .41f, new Vector3(.12f, .06f, .02f), Quaternion.LookRotation(S)); BoxCol(ctx.Root, g + up * 1f, new Vector3(1.2f, 2f, .8f), Quaternion.LookRotation(S)); } }
            Folk(G(ctx, 26f, 1.6f), D, Resident.Pose.Walk, new[] { "I'm a packet. I'm late.", "Follow the pulses. They know the way. They don't, actually." }, Local(), new[] { G(ctx, 26f, 1.6f), G(ctx, streetL, 1.6f), G(ctx, streetL, -1.6f), G(ctx, 26f, -1.6f) }).speed = 1.4f;
            Shot(ctx, "along the bus", G(ctx, 18f, -1f) + up * 2f, G(ctx, streetL + 14f, 0) + up * 10f);
        }

        // ---------------------------------------------------------------- the Stack
        void Stack(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var sc = G(ctx, streetL + 14f, 0);
            mk.Box(tile, sc - up * .2f, new Vector3(18f, .5f, 18f), Quaternion.LookRotation(D)); for (var i = -4; i <= 4; i++) { mk.Box(gridM, sc + up * .06f + S * i * 2f, new Vector3(.05f, .02f, 18f), Quaternion.LookRotation(D)); mk.Box(gridM, sc + up * .06f + D * i * 2f, new Vector3(18f, .02f, .05f), Quaternion.LookRotation(D)); } for (var k = 0; k < 8; k++) Remember(sc + Quaternion.Euler(0, k * 45f, 0) * D * 10f, 1);
            mk.Box(dark, sc + up * 1.5f, new Vector3(4f, 3f, 4f), Quaternion.LookRotation(D)); solid.Box(dark, sc + up * 1.5f, new Vector3(4f, 3f, 4f), Quaternion.LookRotation(D));
            for (var k = 0; k < 14; k++) { var bm = new MeshKit(); var w = 3.6f - k * .14f; bm.Box(k % 4 == 3 ? blockM : k % 4 == 1 ? GlowM(Sort[k % Sort.Length], .8f) : tile, Vector3.zero, new Vector3(w, 1.9f, w), Quaternion.identity); WireCube(bm, gridM, Vector3.zero, w + .02f, Quaternion.identity); var bg = bm.BuildSingle("Stack block", ctx.Root, false); var home = sc + up * (4f + k * 2f); bg.transform.SetPositionAndRotation(home, Quaternion.Euler(0, k * 7f, 0)); life.stack.Add((bg.transform, home, k)); }
            halos.Add(sc + up * 18f, 10f, Cyan, .15f); ArtistBits.PointLight(ctx.Root, sc + up * 6f + D * -6f, Cyan, 26f, 1.8f);
            Folk(sc - D * 9f + S * 3f, D, Resident.Pose.Stand, new[] { "It's held together by nothing. Like most code.", "Every block is a different version of the same thing.", "When the drop hits it all slides out of line. Then back. Usually." }, Local());
            Mark(sc, 9.5f);
            Shot(ctx, "the stack", sc - D * 18f + S * 8f + up * 3f, sc + up * 14f);
        }
    }

    /// The IDM Glitchlands' holder for things you can use and carry.
    public class GlitchTownLife : MonoBehaviour { }
}
