using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Concert Isles' places (part of ConcertKit): the Moon Terrace (a marble round with a balustrade on the sea side,
    /// a tiered fountain, low hedges with white flowers, a grand piano at the edge facing a huge pale moon), the Palace Walk
    /// (gravel between cone topiaries, iron arches hung with candle chandeliers) and the Concert Hall (a white colonnade on
    /// a stepped podium under a dome, rows of chairs, chandeliers, and a golden pipe organ at the back to play).
    public partial class ConcertKit
    {
        ConcertTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.08f, .08f, .1f), new Color(.86f, .86f, .88f), new Color(.3f, .1f, .16f), new Color(.16f, .2f, .3f) };
        static readonly Color[] Bottoms = { new Color(.08f, .08f, .1f), new Color(.14f, .14f, .18f) };
        static readonly string[] TownTalk =
        {
            "Every hedge here hides a composer. Go and find them all.", "The hall's always open. Somebody's always playing the organ. Usually badly.", "Sit at the piano on the terrace. The moon listens.",
            "Four hundred years of music, and it still sounds like tonight.", "Mind the fountain. The fish are very cultured.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1791);
            tlife = new GameObject("Concert town life").AddComponent<ConcertTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.None }, new[] { Gold, Candle, Silver }); l.tool = tool; l.apron = apron; return l; }
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

        /// A black grand piano, lid up, with its bench to sit on.
        void Piano(Transform parent, MeshKit mk, Vector3 g, Vector3 face, string verb = "Sit at the piano")
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var body = g - face * .6f; mk.Box(dark, body + up * .8f, new Vector3(1.5f, .3f, 1.8f), rot); mk.Blob(dark, body + up * .8f - face * .9f, new Vector3(.75f, .15f, .6f), new System.Random(2), 0f); foreach (var (x, z) in new[] { (-.6f, .8f), (.6f, .8f), (0f, -1.1f) }) mk.Prism(dark, body + side * x + face * z, up, .06f, .66f, 6);
            mk.Box(dark, body + up * 1.4f - side * .1f, new Vector3(.04f, 1f, 1.6f), rot * Quaternion.Euler(0, 0, -30f)); mk.Box(white, body + up * .88f + face * .92f, new Vector3(1.3f, .06f, .2f), rot); mk.Prism(candleM, body + up * .96f + face * .7f + side * .6f, up, .03f, .2f, 5);
            BoxCol(parent, body + up * 1f, new Vector3(1.6f, 2f, 2.6f), rot); var bp = g + face * .9f; mk.Box(dark, bp + up * .25f, new Vector3(.9f, .5f, .4f), rot); BoxCol(parent, bp + up * .25f, new Vector3(.9f, .5f, .4f), rot); Interact.Seat(tlife.transform, bp + up * .5f, -face, verb).seatHeight = .5f;
        }

        // ---------------------------------------------------------------- the Moon Terrace
        void MoonTerrace(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(4);
            mk.Prism(marbleD, c - up * .3f, up, 19f, .44f, 40); mk.Prism(marble, c + up * .15f, up, 18f, .02f, 40); mk.Prism(marbleD, c + up * .16f, up, 12f, .02f, 32); mk.Prism(marble, c + up * .17f, up, 11.6f, .02f, 32);
            for (var k = -2; k <= 2; k++) Remember(c + Quaternion.Euler(0, k * 40f, 0) * D * 17f, 0);
            // the balustrade on the sea side
            var rail = Paint(Marble);
            for (var a = 100f; a < 260f; a += 4f) { var d0 = Quaternion.Euler(0, a, 0) * D; var d1 = Quaternion.Euler(0, a + 4f, 0) * D; var p0 = c + d0 * 18.4f; var p1 = c + d1 * 18.4f; mk.Prism(marble, p0 + up * .15f, up, .1f, .8f, 6, .7f); var mid = (p0 + p1) / 2f; var rr = Quaternion.LookRotation(p1 - p0); mk.Box(rail, mid + up * 1f, new Vector3(.3f, .12f, (p1 - p0).magnitude + .02f), rr); solid.Box(rail, mid + up * .6f, new Vector3(.3f, 1.2f, (p1 - p0).magnitude), rr); }
            // the fountain
            var fc = c + D * 7f; mk.Prism(marble, fc - up * .2f, up, 3.4f, .8f, 20); mk.Prism(waterM, fc + up * .58f, up, 3.1f, .02f, 20); mk.Prism(marble, fc, up, .3f, 1.8f, 8); mk.Prism(marble, fc + up * 1.8f, up, 1.2f, .16f, 14); mk.Prism(waterM, fc + up * 1.97f, up, 1.06f, .02f, 14); mk.Prism(marble, fc + up * 2f, up, .16f, .9f, 8); mk.Prism(marble, fc + up * 2.9f, up, .6f, .12f, 12); mk.Prism(waterM, fc + up * 3.03f, up, .5f, .02f, 12); BoxCol(ctx.Root, fc + up * .3f, new Vector3(6.6f, .9f, 6.6f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, fc, 3f, .4f); life.fountains.Add(fc + up * 3.1f); life.fountains.Add(fc + up * 2f);
            ArtistBits.PointLight(ctx.Root, fc + up * 4f, new Color(.7f, .9f, 1f), 14f, 1.2f);
            // low hedges with white flowers either side
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) { var hp = c + S * e * 12f + D * q * 4f; var hr = Quaternion.LookRotation(D); mk.Box(hedgeM, hp + up * .5f, new Vector3(3f, .8f, 6f), hr); solid.Box(hedgeM, hp + up * .5f, new Vector3(3f, .8f, 6f), hr); for (var k = 0; k < 12; k++) mk.Blob(Paint(new Color(.94f, .94f, .96f)), hp + up * .94f + S * ((k % 3) - 1) * .9f + D * ((k / 3) - 1.5f) * 1.4f, Vector3.one * .12f, rng, 0f); Topiary(ctx, mk, hp + D * q * 4.4f, .8f); }
            // the piano at the edge, facing the moon over the sea
            var pc = c - D * 14f; mk.Prism(marble, pc - up * .1f, up, 2.6f, .36f, 18); Piano(ctx.Root, mk, pc + up * .26f, D, "Play the piano under the moon");
            // the moon, huge and low over the sea
            var moon = new Material(WorldMaterials.Glow(new Color(.9f, .94f, 1f), 1.6f)) { name = "Concert moon" }; life.moon = moon; var mp = c - D * 170f + up * 46f; mk.Prism(moon, mp, D, 20f, .4f, 40); halos.Add(mp + D * 4f, 50f, Silver, .2f);
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) Lamp(ctx, mk, c + S * e * 7f + D * q * 15.8f * (q > 0 ? 1f : .2f) + D * (q > 0 ? 0f : -6f), true);
            Folk(c + D * 3f + S * 3f + up * .17f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Concert Isles. Every hedge hides a garden, and every garden a composer.", "The Palace Walk goes up to the hall. Somebody's at the organ.", "Sit at the piano. The moon doesn't mind wrong notes." });
            Folk(pc + S * 2.4f + up * .26f, -S, Resident.Pose.Stand, new[] { "I come out here for the moonlight. And the piano. Mostly the moonlight.", "Play something slow." }, Local());
            Mark(c, 19.5f);
            Shot(ctx, "the moon terrace", c + D * 15f + S * 6f + up * 3f, pc + up * 3f);
        }

        // ---------------------------------------------------------------- the Palace Walk
        void PalaceWalk(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 18.5f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, gravel, WalkW, .13f, ctx); foreach (var e in new[] { -1f, 1f }) { var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(p + S * e * (WalkW / 2f + .2f)); Strip(mk, edge, marble, .4f, .16f, ctx); } foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 24f; d <= streetL - 2f; d += 8f, n++) { foreach (var e in new[] { -1f, 1f }) Topiary(ctx, mk, G(ctx, d, e * (WalkW / 2f + 1.4f))); if (n % 2 == 1) { var a0 = G(ctx, d, -(WalkW / 2f + .4f)); var a1 = G(ctx, d, WalkW / 2f + .4f); foreach (var ap in new[] { a0, a1 }) { mk.Prism(iron, ap, up, .08f, 4.4f, 6); ArtistBits.PostCollider(ctx.Root, ap, 4.4f, .1f); } for (var k = 0; k <= 12; k++) { var a = Mathf.PI * k / 12f; var p = (a0 + a1) / 2f + S * Mathf.Cos(a) * (WalkW / 2f + .4f) + up * (4.4f + Mathf.Sin(a) * 1.6f); mk.Box(iron, p, new Vector3(.1f, .1f, .5f), Quaternion.LookRotation(S) * Quaternion.Euler(0, 0, 0) * Quaternion.Euler(-a * Mathf.Rad2Deg + 90f, 0, 0)); } Chandelier(mk, (a0 + a1) / 2f + up * 4f, .7f); if (n % 4 == 1) ArtistBits.PointLight(ctx.Root, (a0 + a1) / 2f + up * 3.6f, Candle, 12f, 1.4f); } }
            Folk(G(ctx, 28f, 1.4f), D, Resident.Pose.Walk, new[] { "The candles never burn down. I've checked. Every night.", "Evening, evening. Lovely night for a nocturne." }, Local(), new[] { G(ctx, 28f, 1.4f), G(ctx, streetL - 4f, 1.4f), G(ctx, streetL - 4f, -1.4f), G(ctx, 28f, -1.4f) }).speed = .9f;
            Shot(ctx, "along the palace walk", G(ctx, 20f, -1f) + up * 2f, G(ctx, streetL + 14f, 0) + up * 6f);
        }

        // ---------------------------------------------------------------- the Concert Hall
        void ConcertHall(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var hc = G(ctx, streetL + 14f, 0); var f = new PlaceFrame(hc, -D); const float W = 18f, L = 14f, H = 8f, P = .9f; var rng = new System.Random(6);
            // the podium, with three steps up at the front
            mk.Box(marbleD, f.P(0, P / 2f - .3f, 0), new Vector3(W + 1f, P + .6f, L + 1f), f.r); solid.Box(marbleD, f.P(0, P / 2f - .3f, 0), new Vector3(W + 1f, P + .6f, L + 1f), f.r); mk.Box(marble, f.P(0, P + .01f, 0), new Vector3(W, .02f, L), f.r);
            for (var k = 0; k < 3; k++) { var sz = new Vector3(8f, (k + 1) * .3f, .6f); var sp = f.P(0, sz.y / 2f - .05f, L / 2f + .5f + (2 - k) * .6f); mk.Box(marble, sp, sz, f.r); solid.Box(marble, sp, sz, f.r); }
            Remember(f.P(0, 0, L / 2f + 3f), 1); Remember(f.P(0, 0, L / 2f + 6f), 1);
            // columns all round, entablature on top
            for (var x = -W / 2f; x <= W / 2f + .01f; x += 2.25f) foreach (var z in new[] { -L / 2f, L / 2f }) Column(ctx, mk, f.P(x, P, z), H);
            for (var z = -L / 2f + 2.33f; z < L / 2f - .1f; z += 2.33f) foreach (var x in new[] { -W / 2f, W / 2f }) Column(ctx, mk, f.P(x, P, z), H);
            mk.Box(marble, f.P(0, P + H + .5f, 0), new Vector3(W + 1.2f, 1f, L + 1.2f), f.r); mk.Box(goldM, f.P(0, P + H + .05f, L / 2f + .62f), new Vector3(W + 1.2f, .1f, .04f), f.r); ArtistBits.Label(rootT, "THE CONCERT HALL", f.P(0, P + H + .5f, L / 2f + .62f), f.Fwd, .4f, Ink, TextAnchor.MiddleCenter);
            // the dome
            var dc = f.P(0, P + H + 1f, 0); for (var k = 0; k < 8; k++) { var r = 6.4f * Mathf.Cos(k * Mathf.PI / 16f); var h = 6.4f * Mathf.Sin(k * Mathf.PI / 16f); var r1 = 6.4f * Mathf.Cos((k + 1) * Mathf.PI / 16f); mk.Prism(k % 2 == 0 ? marble : white, dc + up * h, up, r, 6.4f * (Mathf.Sin((k + 1) * Mathf.PI / 16f) - Mathf.Sin(k * Mathf.PI / 16f)) + .02f, 24, r1 / r); } mk.Prism(goldM, dc + up * 6.4f, up, .3f, 1.2f, 8, .2f); halos.Add(dc + up * 7.6f, 1.4f, Gold, .4f);
            // the back wall with the pipe organ
            mk.Box(marbleD, f.P(0, P + H / 2f, -L / 2f + .4f), new Vector3(W - .6f, H, .4f), f.r); solid.Box(marbleD, f.P(0, P + H / 2f, -L / 2f + .4f), new Vector3(W - .6f, H, .4f), f.r);
            var organ = new Material(WorldMaterials.Glow(Gold, .9f)) { name = "Concert organ" }; life.organ = organ;
            for (var k = 0; k < 23; k++) { var x = (k - 11f) * .5f; var h = 2.6f + (6f - Mathf.Abs(k - 11f) * .5f) * .7f + (k % 2) * .4f; mk.Prism(organ, f.P(x, P + 1.6f, -L / 2f + .9f), up, .18f, h, 8); mk.Box(dark, f.P(x, P + 2.1f, -L / 2f + 1.09f), new Vector3(.14f, .08f, .02f), f.r); }
            var con = f.P(0, P, -L / 2f + 2.6f); mk.Box(Paint(new Color(.3f, .2f, .14f)), con + up * .6f, new Vector3(2.4f, 1.2f, .9f), f.r); mk.Box(white, con + up * 1.22f + f.Fwd * .2f, new Vector3(2f, .04f, .3f), f.r); BoxCol(rootT, con + up * .6f, new Vector3(2.4f, 1.2f, .9f), f.r); var ob = con + f.Fwd * 1f; mk.Box(Paint(new Color(.3f, .2f, .14f)), ob + up * .25f, new Vector3(1.6f, .5f, .4f), f.r); BoxCol(rootT, ob + up * .25f, new Vector3(1.6f, .5f, .4f), f.r);
            string[] play = { "You press a chord. The whole hall answers, every pipe at once, and the candles shiver.", "A great low note rolls out over the gardens. Somewhere a hedge trembles.", "You play the only bit of Bach you know. It sounds enormous." }; var pn = 0;
            Spot("The organ", con + up * 1.4f, "Play the organ", new Vector3(2.4f, 1.2f, 1.2f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, con + up * 3f, play[pn++ % play.Length], 5f); };
            Interact.Seat(tlife.transform, ob + up * .5f, -f.Fwd, "Sit at the organ").seatHeight = .5f;
            // rows of chairs, chandeliers overhead
            for (var r = 0; r < 4; r++) for (var k = -3; k <= 3; k++) { if (k == 0) continue; var cp = f.P(k * 1.1f, P, -1f + r * 1.5f); mk.Box(Paint(new Color(.5f, .1f, .16f)), cp + up * .45f, new Vector3(.5f, .08f, .5f), f.r); mk.Box(Paint(new Color(.5f, .1f, .16f)), cp + up * .75f + f.Fwd * .24f, new Vector3(.5f, .6f, .06f), f.r); if (k == 1 || k == -1) Interact.Seat(tlife.transform, cp + up * .5f, -f.Fwd, "Sit and listen").seatHeight = .5f; }
            foreach (var e in new[] { -1f, 1f }) BoxCol(rootT, f.P(e * 2.2f, P + .5f, 1.25f), new Vector3(2.8f, 1f, 6.2f), f.r);
            foreach (var e in new[] { -1f, 0f, 1f }) Chandelier(mk, f.P(e * 5f, P + H - 2.2f, 0), 1.1f);
            ArtistBits.PointLight(ctx.Root, f.P(0, P + H - 2f, 0), Candle, 22f, 2f, true); ArtistBits.PointLight(ctx.Root, f.P(0, P + 4f, -L / 2f + 3f), Gold, 10f, 1.4f, true);
            Folk(f.P(3f, P, 2f), f.Fwd, Resident.Pose.Sit, null, Local(), null, .5f);
            Folk(f.P(-6f, P, 4f), -f.Right, Resident.Pose.Stand, new[] { "The organ has a thousand pipes. I've counted twenty-three. The rest are shy.", "Hush. Or don't. It's a garden party." }, Local());
            Mark(hc, 13f);
            Shot(ctx, "the concert hall", f.P(6f, 2f, L / 2f + 12f), f.P(0, 6f, 0));
            Shot(ctx, "inside the concert hall", f.P(0, P + 1.8f, L / 2f - 1f), f.P(0, P + 3f, -L / 2f));
        }

        void Column(LandContext ctx, MeshKit mk, Vector3 g, float h)
        {
            var up = Vector3.up; mk.Box(marble, g + up * .15f, new Vector3(.9f, .3f, .9f), Quaternion.identity); mk.Prism(white, g + up * .3f, up, .34f, h - .6f, 12, .9f); mk.Box(marble, g + up * (h - .15f), new Vector3(.9f, .3f, .9f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, g, h, .4f);
        }
    }

    /// The Concert Isles' holder for things you can use and carry.
    public class ConcertTownLife : MonoBehaviour { }
}
