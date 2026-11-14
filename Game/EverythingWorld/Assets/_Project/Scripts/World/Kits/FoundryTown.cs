using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Electro Foundry's places (part of FoundryKit): the Assembly Floor (a steel floor under an open shed of girders,
    /// two conveyor belts carrying glowing crates, robot arms swinging over them, welders throwing sparks, turning beacons,
    /// the big lever that runs the line), the Line (a walkway beside a long conveyor under a travelling gantry crane) and
    /// the Furnace (a great steel furnace with a glowing mouth, a trough of molten metal and a chimney).
    public partial class FoundryKit
    {
        FoundryTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(1f, .5f, .1f), new Color(.2f, .3f, .5f), new Color(.3f, .3f, .32f), Hazard, new Color(.1f, .1f, .12f) };
        static readonly Color[] Bottoms = { new Color(.2f, .24f, .36f), new Color(.14f, .14f, .16f), new Color(.3f, .3f, .32f) };
        static readonly string[] TownTalk =
        {
            "Night shift. Every shift's the night shift.", "The robots keep better time than I do. They've got the beat.", "Pull the lever on the floor. Everything speeds up. Everyone loves it.",
            "Down the Line to the furnace. Don't touch anything orange.", "We make music here. Out of sparks.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2015);
            tlife = new GameObject("Foundry town life").AddComponent<FoundryTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cap, Residents.Hat.Cap, Residents.Hat.Beanie }, new[] { Hazard, Green, Blue }); l.tool = tool; l.apron = apron; return l; }
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

        /// A conveyor belt from a to b: a black belt on rollers in a steel frame, with glowing crates carried along it.
        void Conveyor(LandContext ctx, MeshKit mk, Vector3 a, Vector3 b, int crates, Material glow)
        {
            var up = Vector3.up; var dir = (b - a); var len = dir.magnitude; dir /= len; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir); var mid = (a + b) / 2f;
            mk.Box(steelD, mid + up * .5f, new Vector3(1.6f, .3f, len), rot); mk.Box(dark, mid + up * .67f, new Vector3(1.3f, .04f, len), rot); foreach (var e in new[] { -.8f, .8f }) mk.Box(hazard, mid + up * .66f + side * e, new Vector3(.06f, .1f, len), rot); for (var d = 0f; d <= len; d += 3f) foreach (var e in new[] { -.7f, .7f }) mk.Box(steelD, a + dir * d + side * e + up * .2f, new Vector3(.1f, .4f, .1f), rot);
            BoxCol(ctx.Root, mid + up * .4f, new Vector3(1.6f, .8f, len), rot);
            for (var k = 0; k < crates; k++) { var cm = new MeshKit(); cm.Box(Paint(new Color(.4f, .32f, .22f)), Vector3.zero, new Vector3(.7f, .5f, .7f), Quaternion.identity); cm.Box(glow, Vector3.up * .26f, new Vector3(.5f, .02f, .5f), Quaternion.identity); var cg = cm.BuildSingle("Crate", ctx.Root, false); life.crates.Add((cg.transform, a + up * .95f, b + up * .95f, k / (float)crates)); }
        }

        // ---------------------------------------------------------------- the Assembly Floor
        void Assembly(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rot = Quaternion.LookRotation(D); var rng = new System.Random(3);
            var fc = c - D * 6f; mk.Box(plate, fc - up * .2f, new Vector3(30f, .5f, 30f), rot); for (var k = -7; k <= 7; k++) mk.Box(steelD, fc + up * .06f + S * k * 2f, new Vector3(.04f, .02f, 30f), rot);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 15; k++) mk.Box(k % 2 == 0 ? hazard : dark, fc + up * .07f + S * e * 14.6f + D * (-14f + k * 2f), new Vector3(.6f, .02f, 2f), rot);
            for (var k = -3; k <= 3; k++) { Remember(c + D * 9f + S * k * 3.6f, 0); Remember(c + S * 15.4f - D * (6f + k * 3f), 0); Remember(c - S * 15.4f - D * (6f + k * 3f), 0); }
            // the shed: columns and roof trusses, open on all sides
            for (var d = -20f; d <= 8.01f; d += 7f) foreach (var e in new[] { -1f, 1f }) { var cp = c + D * d + S * e * 13.4f; mk.Box(steel, cp + up * 5f, new Vector3(.5f, 10f, .5f), rot); ArtistBits.PostCollider(ctx.Root, cp, 10f, .4f); }
            for (var d = -20f; d <= 8.01f; d += 7f) { for (var k = 0; k <= 10; k++) { var x = -13.4f + k * 2.68f; var y = 10f + (1f - Mathf.Abs(x) / 13.4f) * 2.4f; mk.Box(steel, c + D * d + S * x + up * y, new Vector3(2.8f, .2f, .2f), Quaternion.LookRotation(D) * Quaternion.Euler(0, 0, -Mathf.Sign(x) * 10f)); } mk.Box(steelD, c + D * d + up * 10f, new Vector3(26.8f, .2f, .2f), rot); }
            foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(new Color(.2f, .24f, .3f), .3f), c - D * 6f + S * e * 6.7f + up * 11.2f, new Vector3(13.6f, .1f, 28f), rot * Quaternion.Euler(0, 0, e * 10f));
            for (var k = 0; k < 4; k++) { var lp = c - D * (18f - k * 7f) + up * 9.4f; mk.Box(lampM, lp, new Vector3(4f, .1f, .4f), rot * Quaternion.Euler(0, 90f, 0)); ArtistBits.PointLight(ctx.Root, lp - up * .5f, new Color(.86f, .94f, 1f), 18f, 1.6f); }
            // the conveyors and the robot arms
            Conveyor(ctx, mk, c - D * 6f - S * 12f, c - D * 6f + S * 12f, 8, greenM); Conveyor(ctx, mk, c - D * 15f + S * 12f, c - D * 15f - S * 12f, 8, blueM);
            for (var k = 0; k < 6; k++) { var side = k % 2 == 0 ? 1f : -1f; var ap = c - D * (k < 3 ? 6f : 15f) + D * side * 2.4f + S * (-8f + (k % 3) * 8f); RobotArm(ctx, ctx.Root, ctx.OnGround(ap.x, ap.z), 1.1f, k % 2 == 0 ? greenM : blueM, k); }
            // welders' corner and the beacons
            for (var k = 0; k < 3; k++) { var wp = c - D * 22f + S * (k - 1f) * 5f; mk.Box(steelD, wp + up * .5f, new Vector3(1.6f, 1f, 1f), rot); BoxCol(ctx.Root, wp + up * .5f, new Vector3(1.6f, 1f, 1f), rot); life.welds.Add(wp + up * 1.05f); Folk(wp + D * 1.2f, -D, Resident.Pose.Stand, null, Local()); }
            for (var k = 0; k < 4; k++) { var bp = c - D * (k < 2 ? -8f : 20f) + S * (k % 2 == 0 ? -13.4f : 13.4f) + up * 8f; var bm = new MeshKit(); bm.Box(beaconM, Vector3.forward * .2f, new Vector3(.3f, .3f, .1f), Quaternion.identity); bm.Prism(GlowM(new Color(1f, .6f, .1f), .6f), Vector3.zero, Vector3.up, .25f, .4f, 10); var bg = bm.BuildSingle("Beacon", ctx.Root, false); bg.transform.position = bp; life.beacons2.Add(bg.transform); }
            // the big lever
            var lv = c + D * 4f + S * 6f; mk.Box(steelD, lv + up * .6f, new Vector3(1.2f, 1.2f, .8f), rot); mk.Box(hazard, lv + up * 1.22f, new Vector3(1.2f, .04f, .8f), rot); var lm = new MeshKit(); lm.Box(steel, Vector3.up * .5f, new Vector3(.1f, 1f, .1f), Quaternion.identity); lm.Blob(GlowM(new Color(1f, .2f, .1f), 1.6f), Vector3.up * 1.05f, Vector3.one * .14f, rng, 0f); var lg = lm.BuildSingle("The lever", ctx.Root, false); lg.transform.SetPositionAndRotation(lv + up * 1.2f, rot * Quaternion.Euler(-30f, 0, 0)); life.lever = lg.transform; life.leverRot = rot; BoxCol(ctx.Root, lv + up * .6f, new Vector3(1.2f, 1.2f, .8f), rot);
            string[] pull = { "You haul the lever down. The belts speed up, every robot arm snaps into double time, and the sparks go everywhere.", "CLUNK. The whole floor shifts gear. Somebody cheers.", "The line runs flat out. The beacons spin like mad." }; var pn = 0;
            Spot("The big lever", lv + up * 1.6f, "Pull the lever", new Vector3(1f, 1.2f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, lv + up * 2.8f, pull[pn++ % pull.Length], 5f); };
            Folk(c + D * 6f - S * 4f, (-D + S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Foundry. Hard hats optional. Ear plugs not.", "Pull the lever over there. The whole line goes double speed.", "Follow the Line down to the furnace. It never goes out." }, Local());
            Mark(fc, 16f);
            Shot(ctx, "the assembly floor", c + D * 10f + S * 8f + up * 3.4f, c - D * 10f + up * 2f);
        }

        // ---------------------------------------------------------------- the Line and the Furnace
        void TheLine(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 9.5f; d <= streetL + 6.01f; d += 2f) pts.Add(G(ctx, d, -2.5f)); if (pts.Count < 2) return;
            Strip(mk, pts, plate, 3.4f, .1f, ctx); var ed = new List<Vector3>(); foreach (var p in pts) ed.Add(p - S * 1.6f); Strip(mk, ed, hazard, .2f, .12f, ctx); foreach (var p in pts) Remember(p, 1);
            // the gantry crane, travelling up and down over the Line
            var gm = new MeshKit(); foreach (var e in new[] { -5f, 5f }) { gm.Box(hazard, new Vector3(e, 4f, 0), new Vector3(.4f, 8f, .4f), Quaternion.identity); gm.Box(steelD, new Vector3(e, .3f, 0), new Vector3(.6f, .6f, 2f), Quaternion.identity); } gm.Box(hazard, new Vector3(0, 8f, 0), new Vector3(10.6f, .6f, .6f), Quaternion.identity); gm.Box(steelD, new Vector3(1.6f, 7.4f, 0), new Vector3(1f, .6f, 1f), Quaternion.identity); gm.Prism(steel, new Vector3(1.6f, 7f, 0), Vector3.down, .02f, 4f, 3); gm.Box(steel, new Vector3(1.6f, 3f, 0), new Vector3(1.2f, .3f, 1.2f), Quaternion.identity); gm.Blob(beaconM, new Vector3(-5f, 8.5f, 0), Vector3.one * .2f, new System.Random(2), 0f);
            var crane = gm.BuildSingle("Gantry crane", ctx.Root, false); life.crane = crane.transform; life.craneA = G(ctx, 16f, -.4f); life.craneB = G(ctx, streetL, -.4f); life.craneRot = Quaternion.LookRotation(D);
            // the Furnace
            var fz = new PlaceFrame(G(ctx, streetL + 16f, 0), -D); mk.Box(steelD, fz.P(0, 3f, 0), new Vector3(9f, 6f, 7f), fz.r); solid.Box(steelD, fz.P(0, 3f, 0), new Vector3(9f, 6f, 7f), fz.r); mk.Box(furnaceM, fz.P(0, 2f, 3.51f), new Vector3(3.6f, 2.4f, .02f), fz.r); mk.Box(dark, fz.P(0, 3.4f, 3.52f), new Vector3(4f, .3f, .04f), fz.r); for (var k = 0; k < 4; k++) mk.Box(hazard, fz.P(-4.5f + k * 3f, 6.05f, 3.4f), new Vector3(1.4f, .1f, .3f), fz.r); mk.Prism(steelD, fz.P(2.4f, 6f, -1.4f), up, 1.2f, 14f, 12, .8f); life.smoke.Add(fz.P(2.4f, 20.2f, -1.4f)); mk.Box(steel, fz.P(0, .4f, 6f), new Vector3(5f, .8f, 1.6f), fz.r); mk.Box(furnaceM, fz.P(0, .81f, 6f), new Vector3(4.4f, .02f, 1.1f), fz.r); BoxCol(ctx.Root, fz.P(0, .4f, 6f), new Vector3(5f, .8f, 1.6f), fz.r);
            halos.Add(fz.P(0, 2f, 5f), 6f, Furnace, .35f); ArtistBits.PointLight(ctx.Root, fz.P(0, 2f, 7f), Furnace, 22f, 2.4f); life.welds.Add(fz.P(0, 1f, 6f)); for (var k = 0; k < 6; k++) Remember(fz.P(-6f + k * 2.4f, 0, 9f), 1);
            Folk(fz.P(4f, 0, 8f), -fz.Fwd + fz.Right * .2f, Resident.Pose.Stand, new[] { "Fifteen hundred degrees. Mind your eyebrows.", "It's never gone out. Not once." }, Local());
            Folk(G(ctx, 20f, -3f), D, Resident.Pose.Walk, new[] { "Clocking on. Clocking off. Same thing here." }, Local(), new[] { G(ctx, 20f, -3f), G(ctx, streetL, -3f), G(ctx, streetL, -2f), G(ctx, 20f, -2f) }).speed = 1.2f;
            Mark(fz.o, 6f);
            Shot(ctx, "along the line", G(ctx, 10f, -3.4f) + up * 2f, G(ctx, streetL + 16f, 0) + up * 3f);
            Shot(ctx, "the furnace", fz.P(5f, 2f, 12f), fz.P(0, 2f, 3f));
        }

        /// The Line's long conveyor and its lamps, built once the lanes are down: the belt leaves a gap wherever a lane
        /// crosses it (each length of belt carries its own crates), and no lamp stands on a lane.
        void LineBelt(LandContext ctx, MeshKit mk)
        {
            var a = Wp(12f, 1.6f); var b = Wp(streetL + 6f, 1.6f); var len = Vector3.Distance(a, b);
            var cuts = new List<float>();
            foreach (var (p, w) in walkPoints)
            {
                if (w < 10) continue;
                var t = Vector3.Dot(p - a, D); var s = Vector3.Dot(p - a, S);
                if (t > -2.5f && t < len + 2.5f && Mathf.Abs(s) < PathW / 2f + 1.2f) cuts.Add(t);
            }
            cuts.Sort();
            var from = 0f;
            void Piece(float t0, float t1) { if (t1 - t0 < 3f) return; Conveyor(ctx, mk, G(ctx, 12f + t0, 1.6f), G(ctx, 12f + t1, 1.6f), Mathf.Max(1, (int)((t1 - t0) / 6f)), greenM); }
            foreach (var t in cuts) { if (t - 2.4f > from) Piece(from, Mathf.Min(t - 2.4f, len)); from = Mathf.Max(from, t + 2.4f); }
            Piece(from, len);
            var n = 0; for (var d = 18f; d <= streetL; d += 12f, n++) { var lp = Wp(d, -5f); if (LaneNear(lp.x, lp.z, PathW / 2f + .6f)) continue; Lamp(ctx, mk, G(ctx, d, -5f), n % 2 == 0); }
        }
    }

    /// The Electro Foundry's holder for things you can use and carry.
    public class FoundryTownLife : MonoBehaviour { }
}
