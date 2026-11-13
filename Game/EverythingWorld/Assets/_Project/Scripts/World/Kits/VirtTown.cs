using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Mount Virtuoso's places (part of VirtKit): the Terrace where you arrive (flat stones among birches and heather, a
    /// grand piano looking out to sea, a cairn and a bench), the Climb (a path cut into the mountain, spiralling up past
    /// the guitar heroes' ledges, lanterns and a rope rail on the drop side) and the Summit (bare rock above the clouds,
    /// a wall of amps, a lightning rod, a riff to play into the storm).
    public partial class VirtKit
    {
        VirtTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.3f, .34f, .4f), new Color(.6f, .56f, .5f), new Color(.2f, .2f, .22f), new Color(.5f, .3f, .3f), new Color(.36f, .44f, .36f) };
        static readonly Color[] Bottoms = { new Color(.2f, .22f, .26f), new Color(.34f, .3f, .26f), new Color(.12f, .12f, .14f) };
        static readonly string[] TownTalk =
        {
            "Down here it's all quiet piano. Up there it's all lightning.", "Take the path round the mountain. Every ledge has someone playing faster than the last.", "The storm never leaves the top. They say the shredders started it.",
            "Sit at the piano. The sea joins in.", "Bring a coat. Bring two.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1990);
            tlife = new GameObject("Virt town life").AddComponent<VirtTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Bucket }, new[] { Heather, Moss, Glow }); l.tool = tool; l.apron = apron; return l; }
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

        /// A black grand piano with its lid up and a bench to sit at.
        void Piano(Transform parent, MeshKit mk, Vector3 g, Vector3 face, string verb)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var body = g - face * .6f;
            mk.Box(dark, body + up * .8f, new Vector3(1.5f, .3f, 1.8f), rot); mk.Blob(dark, body + up * .8f - face * .9f, new Vector3(.75f, .15f, .6f), new System.Random(2), 0f); foreach (var (x, z) in new[] { (-.6f, .8f), (.6f, .8f), (0f, -1.1f) }) mk.Prism(dark, body + side * x + face * z, up, .06f, .66f, 6);
            mk.Box(dark, body + up * 1.4f - side * .1f, new Vector3(.04f, 1f, 1.6f), rot * Quaternion.Euler(0, 0, -30f)); mk.Box(white, body + up * .88f + face * .92f, new Vector3(1.3f, .06f, .2f), rot);
            BoxCol(parent, body + up * 1f, new Vector3(1.6f, 2f, 2.6f), rot); var bp = g + face * .9f; mk.Box(dark, bp + up * .25f, new Vector3(.9f, .5f, .4f), rot); BoxCol(parent, bp + up * .25f, new Vector3(.9f, .5f, .4f), rot); Interact.Seat(tlife.transform, bp + up * .5f, -face, verb).seatHeight = .5f;
        }

        // ---------------------------------------------------------------- the Terrace
        void Terrace(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            // which way is the sea? the piano faces it
            var seaDir = -D; var bestD = float.MaxValue; for (var k = 0; k < 16; k++) { var dir = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward; for (var d = 20f; d < 600f; d += 10f) { var p = c + dir * d; if (!ctx.IsLand(p.x, p.z, 0f)) { if (d < bestD && Vector3.Angle(dir, D) > 60f) { bestD = d; seaDir = dir; } break; } } }
            for (var k = 0; k < 14; k++) { var a = k * 25.7f; var sp = c + Quaternion.Euler(0, a, 0) * Vector3.forward * (3f + (k % 3) * 3.2f); mk.Prism(rock, sp - up * .25f, up, 1.4f + (k % 2) * .6f, .35f, 7, 1f, a); }
            mk.Prism(rockD, c - up * .3f, up, 10f, .34f, 24); mk.Prism(rock, c + up * .03f, up, 9.6f, .02f, 24); for (var k = 0; k < 10; k++) Remember(c + Quaternion.Euler(0, k * 36f, 0) * D * 9.4f, 0);
            var pc = c + seaDir * 6f; Piano(ctx.Root, mk, pc, seaDir, "Play the piano to the sea");
            var cr = c - seaDir * 5f + Vector3.Cross(up, seaDir) * 5f; for (var k = 0; k < 6; k++) mk.Blob(k % 2 == 0 ? rock : rockD, cr + up * (.25f + k * .38f), new Vector3(.7f - k * .09f, .24f, .6f - k * .08f), rng, .2f); ArtistBits.PostCollider(ctx.Root, cr, 2.2f, .6f);
            Bench(ctx.Root, mk, c + seaDir * 3f + Vector3.Cross(up, seaDir) * 4f, seaDir); Bench(ctx.Root, mk, c + seaDir * 3f - Vector3.Cross(up, seaDir) * 4f, seaDir);
            foreach (var e in new[] { -1f, 1f }) Lamp(ctx, mk, c + Vector3.Cross(up, seaDir) * e * 8f, true);
            // a signpost pointing up the mountain
            var sp2 = c + D * 7f + S * 2f; mk.Prism(wood, sp2, up, .06f, 2.4f, 5); mk.Box(wood, sp2 + up * 2f + D * .5f, new Vector3(.08f, .3f, 1.2f), Quaternion.LookRotation(D)); ArtistBits.Label(rootT, "THE SUMMIT", sp2 + up * 2f + D * .5f + S * .05f, S, .1f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(ctx.Root, sp2, 2.4f, .1f);
            Folk(c + D * 4f - S * 3f, (-D + S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to Mount Virtuoso. Calm down here. Not up there.", "The path winds all the way to the top. Every ledge, someone's shredding.", "Sit at the piano first. You'll want something quiet in your head before the climb." });
            Folk(pc + Vector3.Cross(up, seaDir) * 2.2f, seaDir, Resident.Pose.Stand, new[] { "Listen. The sea keeps time.", "Play something slow. The mist likes it." }, Local());
            Mark(c, 10.5f);
            Shot(ctx, "the terrace and the piano", c - seaDir * 6f + up * 2.2f, pc + seaDir * 20f + up * 2f);
            Shot(ctx, "mount virtuoso", c - D * 10f + S * 12f + up * 4f, new Vector3(peak2.x, baseY + Hm * .7f, peak2.y));
        }

        // ---------------------------------------------------------------- the Climb
        void Climb(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; if (climb.Count < 2) return;
            // a path from the terrace to the foot of the climb
            var foot = climb[0]; var c = ctx.OnGround(center.x, center.z); var lead = new List<Vector3>(); var dir = new Vector3(foot.x - c.x, 0, foot.z - c.z); var len = dir.magnitude; dir /= Mathf.Max(.01f, len); for (var d = 9.5f; d < len; d += 2f) { var p = c + dir * d; lead.Add(ctx.OnGround(p.x, p.z)); } lead.Add(ctx.OnGround(foot.x, foot.z)); if (lead.Count > 1) { Strip(mk, lead, path, ClimbW, .1f, ctx); foreach (var p in lead) Remember(p, 1); }
            var pts = new List<Vector3>(); for (var k = 0; k < climb.Count; k += 2) pts.Add(ctx.OnGround(climb[k].x, climb[k].z)); Strip(mk, pts, path, ClimbW, .12f, ctx); foreach (var p in pts) Remember(p, 1);
            // lanterns on the uphill side, a rope rail on the drop side
            var peak = new Vector3(peak2.x, 0, peak2.y);
            for (var i = 3; i < pts.Count; i += 3)
            {
                var p = pts[i]; var outw = new Vector3(p.x - peak.x, 0, p.z - peak.z).normalized; var inw = -outw;
                var nearLedge = false; foreach (var kv in ledges) if ((new Vector3(kv.Value.at.x, 0, kv.Value.at.z) - new Vector3(p.x, 0, p.z)).sqrMagnitude < 15f * 15f) nearLedge = true;
                if (!nearLedge) { var rp = p + outw * (ClimbW / 2f + .3f); mk.Prism(wood, rp - up * .2f, up, .06f, 1.3f, 5); ArtistBits.PostCollider(ctx.Root, rp, 1.1f, .1f); if (i + 3 < pts.Count) { var q = pts[i + 3] + new Vector3(pts[i + 3].x - peak.x, 0, pts[i + 3].z - peak.z).normalized * (ClimbW / 2f + .3f); mk.Box(Paint(new Color(.7f, .6f, .44f)), (rp + q) / 2f + up * 1f, new Vector3(.04f, .04f, (q - rp).magnitude), Quaternion.LookRotation(q - rp)); BoxCol(ctx.Root, (rp + q) / 2f + up * .55f, new Vector3(.15f, 1.1f, (q - rp).magnitude), Quaternion.LookRotation(q - rp)); } }
                if (i % 9 == 0) Lamp(ctx, mk, p + inw * (ClimbW / 2f + .6f), i % 18 == 0);
            }
            for (var k = 1; k < 4; k++) { var ci = pts.Count * k / 4; Folk(pts[ci] + new Vector3(pts[ci].x - peak.x, 0, pts[ci].z - peak.z).normalized * .6f, (pts[Mathf.Min(ci + 1, pts.Count - 1)] - pts[ci]).normalized, Resident.Pose.Walk, k == 2 ? new[] { "Halfway. The wind gets in your ears from here.", "Can you hear them? Every ledge is louder." } : null, Local(), new[] { pts[ci], pts[Mathf.Min(ci + 12, pts.Count - 1)] }).speed = .7f; }
            Shot(ctx, "on the climb", pts[pts.Count / 3] + up * 2f, pts[pts.Count / 3 + 8] + up * 2f);
        }

        // ---------------------------------------------------------------- the Summit
        void Summit(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var top = ctx.OnGround(peak2.x, peak2.y); var rng = new System.Random(9); var last = climb[climb.Count - 1]; var face = new Vector3(last.x - top.x, 0, last.z - top.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Prism(rockD, top - up * .3f, up, 11f, .36f, 18); for (var k = 0; k < 8; k++) Remember(top + Quaternion.Euler(0, k * 45f, 0) * face * 9f, 1);
            // the wall of amps
            var aw = top - face * 6f; for (var x = -3; x <= 3; x++) for (var y = 0; y < 3; y++) { var ap = aw + side * x * 1.3f + up * (y * 1.1f + .55f); mk.Box(dark, ap, new Vector3(1.24f, 1.04f, .9f), rot); mk.Box(Paint(new Color(.26f, .24f, .22f)), ap + face * .46f, new Vector3(1.1f, .9f, .02f), rot); if (y == 2) mk.Box(ampM, ap + up * .3f + face * .47f, new Vector3(.6f, .06f, .01f), rot); } solid.Box(dark, aw + up * 1.7f, new Vector3(9.2f, 3.4f, .9f), rot);
            // the lightning rod
            var lr = top + side * 4f; mk.Prism(iron, lr, up, .1f, 9f, 6, .3f); mk.Prism(GlowM(Lightning, 1.6f), lr + up * 9f, up, .04f, .8f, 4, .1f); ArtistBits.PostCollider(ctx.Root, lr, 9f, .15f); life.rod = lr + up * 9.6f;
            // a guitar on a stand, waiting
            Guitar(mk, ctx.Root, top + face * 2f - side * 2f, face, new Color(.8f, .2f, .2f), GlowM(Lightning, 1.4f), .9f);
            string[] riff = { "You hit the first chord. Thunder answers it, then lightning strikes the rod and the whole mountain lights up.", "A screaming solo into the wind. The storm can't keep up.", "One note, held. The clouds split for a second, then close again." }; var rn = 0;
            Spot("The summit riff", top + face * 2f - side * 2f + up * 2f, "Play the summit riff", new Vector3(1.6f, 3f, 1.6f)).onUse = _ => { life.strike = Time.time; life.played = Time.time; Say(tlife.transform, top + up * 3.4f, riff[rn++ % riff.Length], 5f); };
            for (var k = 0; k < 3; k++) mk.Blob(rock, top + Quaternion.Euler(0, k * 120f + 60f, 0) * face * 9f + up * .4f, new Vector3(1.4f, 1f, 1.2f), rng, .3f);
            ArtistBits.PointLight(ctx.Root, aw + face * 3f + up * 3f, Ember, 16f, 1.6f);
            Folk(top + side * -3f + face * 3f, face, Resident.Pose.Stand, new[] { "Made it! Now play something louder than the storm.", "The rod gets hit every few minutes. Don't stand there.", "It's always like this up here. Always." }, Local());
            Mark(top, 11f);
            Shot(ctx, "the summit", top + face * 9f + up * 2.4f, aw + up * 2f);
        }
    }

    /// Mount Virtuoso's holder for things you can use and carry.
    public class VirtTownLife : MonoBehaviour { }
}
