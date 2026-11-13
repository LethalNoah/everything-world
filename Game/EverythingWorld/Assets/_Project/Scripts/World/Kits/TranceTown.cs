using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Trance Steppe's places (part of TranceKit): the Sunrise Stage (a wide round stage on a low rise, ringed with
    /// laser emitters sweeping beams up into the dawn, a DJ riser at the back, a crowd with their hands in the air facing
    /// the sun), the giant sun itself, rising on the horizon, and the Long Plain (a road straight at the sun to the
    /// Monolith, a white column throwing a beam of light into the sky).
    public partial class TranceKit
    {
        TranceTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.94f, .9f, .86f), Dawn, Violet, new Color(.3f, .3f, .36f), Gold };
        static readonly Color[] Bottoms = { new Color(.2f, .2f, .26f), new Color(.5f, .44f, .4f), new Color(.86f, .84f, .8f) };
        static readonly string[] TownTalk =
        {
            "We've been here all night. Here comes the sun. Here it comes!", "Hands up when the breakdown ends. You'll know.", "Walk the Long Plain right into the sunrise.",
            "Everyone up here is a little bit in love. With everyone.", "The lasers point at the sky so the sky knows we're here.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1999);
            tlife = new GameObject("Trance town life").AddComponent<TranceTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None }, new[] { Laser, Dawn, Gold }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Sunrise Stage
        void SunriseStage(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(4);
            mk.Prism(stage, c - up * .3f, up, 16f, .42f, 40); mk.Prism(laserM, c + up * .12f, up, 15.6f, .02f, 40); mk.Prism(stage, c + up * .13f, up, 15.3f, .02f, 40); for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * D; mk.Box(GlowM(Gold, .9f), c + up * .15f + d * 7.5f, new Vector3(.1f, .02f, 15f), Quaternion.LookRotation(d)); Remember(c + d * 15f, 0); }
            // the DJ riser at the back, facing the sun with the crowd
            var dj = c - D * 11f; mk.Box(dark, dj + up * .5f, new Vector3(6f, 1f, 3f), Quaternion.LookRotation(D)); solid.Box(dark, dj + up * .5f, new Vector3(6f, 1f, 3f), Quaternion.LookRotation(D)); mk.Box(dark, dj + up * 1.5f, new Vector3(3f, 1f, 1f), Quaternion.LookRotation(D)); mk.Box(laserM, dj + up * 2.02f, new Vector3(2.6f, .04f, .8f), Quaternion.LookRotation(D)); BoxCol(ctx.Root, dj + up * 1.5f, new Vector3(3f, 1f, 1f), Quaternion.LookRotation(D));
            for (var k = 0; k < 3; k++) mk.Box(dark, dj + up * (.17f + k * .33f) + D * (2f - k * .4f) + S * 3.6f, new Vector3(1.2f, .33f + k * .66f, .4f), Quaternion.LookRotation(D));
            // the ring of laser emitters
            for (var k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f + 11f, 0) * D; Emitter(ctx, mk, c + d * 16.6f, k * .4f, k % 2 == 0 ? beamM : GlowM(new Color(1f, .9f, .8f), .4f)); }
            string[] hands = { "You throw your hands up. Every laser on the plain swings to meet in one point in the sky, right over the sun.", "The breakdown ends. The whole crowd jumps at once.", "Somebody hugs you. You don't know them. You do now." }; var hn = 0;
            Spot("The front of the stage", c + D * 4f + up * 1.2f, "Put your hands up", new Vector3(4f, 2f, 2f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, c + D * 4f + up * 2.8f, hands[hn++ % hands.Length], 5f); };
            // the crowd, hands up, facing the sun
            for (var k = 0; k < 9; k++) Folk(c + D * (2f + (k % 3) * 2.4f) + S * ((k / 3) * 3f - 3f) + up * .15f, D, k % 2 == 0 ? Resident.Pose.Wave : Resident.Pose.Stand, k == 0 ? TownTalk : null, Local());
            Folk(dj + up * 1f + D * .8f, D, Resident.Pose.Wave, new[] { "Here it comes. Here it comes. Here it COMES.", "This one's for the sunrise." }, Local());
            ArtistBits.PointLight(ctx.Root, c + up * 6f, Gold, 30f, 1.6f);
            Mark(c, 17f);
            Shot(ctx, "the sunrise stage", dj + up * 3f, c + D * 60f + up * 10f);
        }

        // ---------------------------------------------------------------- the Long Plain, the Monolith and the sun
        void LongPlain(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 16.5f; d <= streetL + 6.01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, path, PlainW, .1f, ctx); foreach (var p in pts) Remember(p, 1); for (var i = 2; i < pts.Count; i += 3) mk.Box(GlowM(Gold, .7f), pts[i] + up * .12f, new Vector3(.3f, .02f, .8f), Quaternion.LookRotation(D));
            var n = 0; for (var d = 22f; d <= streetL; d += 14f, n++) foreach (var e in new[] { -1f, 1f }) { if (n % 2 == 0) Raised(ctx, mk, G(ctx, d, e * (PlainW / 2f + 2f)), D, 1.2f); else Lamp(ctx, mk, G(ctx, d, e * (PlainW / 2f + 1f)), e > 0); }
            // the Monolith
            var mo = G(ctx, streetL + 12f, 0); mk.Prism(stone, mo - up * .3f, up, 6f, .4f, 20); for (var k = 0; k < 8; k++) Remember(mo + Quaternion.Euler(0, k * 45f, 0) * D * 6.6f, 1);
            mk.Box(white, mo + up * 6f, new Vector3(1.6f, 12f, 1.6f), Quaternion.LookRotation(D)); solid.Box(white, mo + up * 6f, new Vector3(1.6f, 12f, 1.6f), Quaternion.LookRotation(D)); for (var k = 0; k < 4; k++) mk.Box(laserM, mo + up * 6f + Quaternion.Euler(0, k * 90f, 0) * D * .81f, new Vector3(.1f, 11f, .02f), Quaternion.LookRotation(Quaternion.Euler(0, k * 90f, 0) * D));
            var bm = new MeshKit(); bm.Prism(GlowM(White, .6f), Vector3.zero, Vector3.up, .5f, 300f, 8, 3f, 0f, false); var bg = bm.BuildSingle("Monolith beam", ctx.Root, false); bg.transform.position = mo + up * 12f; life.monolith = bg.transform; halos.Add(mo + up * 12f, 4f, White, .4f); ArtistBits.PointLight(ctx.Root, mo + up * 4f, White, 20f, 1.6f);
            Folk(mo - D * 7f + S * 2f, D, Resident.Pose.Wave, new[] { "Stand at the bottom and look straight up.", "That beam reaches the sun. I'm almost sure." }, Local());
            Folk(G(ctx, 24f, 1.2f), D, Resident.Pose.Walk, new[] { "Walking into the sunrise. Best thing there is.", "Hands up! Sorry. Can't help it." }, Local(), new[] { G(ctx, 24f, 1.2f), G(ctx, streetL, 1.2f), G(ctx, streetL, -1.2f), G(ctx, 24f, -1.2f) }).speed = 1f;
            // the giant sun rising on the horizon, haloed
            var sc = center + D * 520f + up * 40f; var sun = new MeshKit(); sun.Prism(sunM, Vector3.zero, Vector3.back, 150f, 2f, 48); for (var k = 1; k <= 3; k++) sun.Prism(GlowM(Color.Lerp(new Color(1f, .6f, .4f), new Color(1f, .4f, .5f), k / 3f), .5f - k * .1f), Vector3.forward * k * 4f, Vector3.back, 150f + k * 30f, 1f, 48); var sg = sun.BuildSingle("The sun", ctx.Root, false); sg.transform.SetPositionAndRotation(sc, Quaternion.LookRotation(D)); life.sunT = sg.transform; life.sunHome = sc;
            Shot(ctx, "along the long plain", G(ctx, 17f, -1.4f) + up * 2f, sc);
            Shot(ctx, "the monolith", mo - D * 14f + S * 6f + up * 2f, mo + up * 14f);
        }
    }

    /// The Trance Steppe's holder for things you can use and carry.
    public class TranceTownLife : MonoBehaviour { }
}
