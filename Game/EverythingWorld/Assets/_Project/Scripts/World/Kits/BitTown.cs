using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The 8-Bit Isle's places (part of BitKit): the Arcade (a checkered floor of blue tiles, a giant arcade cabinet with a
    /// pixel heart on its screen, a ring of cabinets with stools, PRESS START blinking in the air, coins spinning), the Level
    /// Road (brick, with gold blocks floating over it, pipes beside it and a flagpole at the end) and the Ring (a vast silver
    /// ring rising out of the ground into the sky, lit blue inside).
    public partial class BitKit
    {
        BitTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.9f, .2f, .2f), new Color(.2f, .5f, .9f), new Color(.2f, .7f, .3f), new Color(.96f, .8f, .2f), new Color(.6f, .3f, .8f) };
        static readonly Color[] Bottoms = { new Color(.2f, .3f, .7f), new Color(.5f, .3f, .2f), new Color(.14f, .14f, .18f) };
        static readonly string[] TownTalk =
        {
            "Every game has its own little world here. Go see them all. Collect the coins. They don't do anything. Collect them anyway.", "Player two has entered the game!",
            "Follow the Level Road to the flag, then keep going. You'll see the Ring.", "The farm over there has all four seasons at once. Don't ask how.", "Press start. Always press start.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1985);
            tlife = new GameObject("Bit town life").AddComponent<BitTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Cap }, new[] { Gold, Pink, Cyan }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Arcade
        void Arcade(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rot = Quaternion.LookRotation(D);
            // the checkered floor
            mk.Box(tileA, c - up * .2f, new Vector3(26f, .5f, 26f), rot); for (var i = -6; i <= 6; i++) for (var j = -6; j <= 6; j++) if (((i + j) & 1) == 0) mk.Box(tileB, c + up * .06f + S * (i * 2f) + D * (j * 2f), new Vector3(2f, .02f, 2f), rot);
            for (var k = 0; k < 4; k++) Remember(c + Quaternion.Euler(0, k * 90f, 0) * D * 12f, 0);
            // the giant cabinet at the back, a pixel heart on its screen
            var gc = c - D * 9f; Cabinet(ctx, mk, solid, gc, D, 0, 5f); var scr = new PlaceFrame(gc + up * 7.5f + D * 2.62f, D); var tilt = Quaternion.Euler(-12f, 0, 0);
            string[] heart = { "..XX...XX..", ".XXXX.XXXX.", "XXXXXXXXXXX", "XXXXXXXXXXX", ".XXXXXXXXX.", "..XXXXXXX..", "...XXXXX...", "....XXX....", ".....X....." };
            var hm = new Material(WorldMaterials.Glow(new Color(1f, .2f, .3f), 1.6f)) { name = "Bit heart" }; life.heart = hm;
            for (var y = 0; y < heart.Length; y++) for (var x = 0; x < heart[y].Length; x++) if (heart[y][x] == 'X') mk.Box(hm, scr.o + scr.r * tilt * new Vector3((x - 5f) * .26f, (4f - y) * .26f, .03f), new Vector3(.24f, .24f, .02f), scr.r * tilt);
            ArtistBits.Label(rootT, "THE 8-BIT ISLE", gc + up * 9.75f + D * 2.32f, D, .5f, Ink, TextAnchor.MiddleCenter);
            halos.Add(gc + up * 7.5f + D * 3f, 5f, Cyan, .25f); ArtistBits.PointLight(ctx.Root, gc + up * 6f + D * 5f, new Color(.6f, .9f, 1f), 22f, 2f);
            // a ring of cabinets with stools down both sides
            var n = 1; foreach (var e in new[] { -1f, 1f }) for (var k = -2; k <= 2; k++) { var cp = c + S * e * 11.2f + D * (k * 2.6f); Cabinet(ctx, mk, solid, cp, -S * e, n++); }
            // PRESS START, blinking in the air
            var ps = c + up * 3.4f + D * 2f; var label = ArtistBits.Label(rootT, "PRESS START", ps, -D, .5f, Gold, TextAnchor.MiddleCenter); var label2 = ArtistBits.Label(rootT, "PRESS START", ps, D, .5f, Gold, TextAnchor.MiddleCenter); life.blink.Add(label); life.blink.Add(label2);
            string[] start = { "PLAYER 1 START! Every coin on the island glints at once.", "1UP! You feel like you could take on the whole island.", "CONTINUE? 10... 9... You already did. You're already playing." }; var sn = 0;
            Spot("Press start", ps, "Press start", new Vector3(3f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, ps + up * 1.2f, start[sn++ % start.Length], 4.5f); };
            for (var k = 0; k < 12; k++) Coin(ctx.Root, c + Quaternion.Euler(0, k * 30f, 0) * D * 7f + up * 2f);
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) Lamp(ctx, mk, c + S * e * 12.6f + D * q * 12.6f, true);
            Folk(c + D * 4f + S * 3f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the 8-Bit Isle! Every game you ever loved, each in its own little world.", "Grab a stool, play a cabinet. Or follow the Level Road to the flag.", "High score's yours if you want it. Nobody's beaten it. Nobody's tried." });
            Folk(c + S * 9f + D * 1.3f, S, Resident.Pose.Sit, null, Local(), null, .68f);
            Mark(c, 18f); Mark(gc, 4f);
            Shot(ctx, "the arcade", c + D * 12f + S * 5f + up * 3f, gc + up * 6f);
        }

        // ---------------------------------------------------------------- the Level Road
        void LevelRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 13f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, brick, RoadW, .13f, ctx); foreach (var e in new[] { -1f, 1f }) { var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(p + S * e * (RoadW / 2f - .25f)); Strip(mk, edge, brickD, .5f, .15f, ctx); } foreach (var p in pts) Remember(p, 1);
            var rot = Quaternion.LookRotation(D); var n = 0;
            for (var d = 22f; d <= streetL - 6f; d += 16f, n++)
            {
                var bp = G(ctx, d, 0) + up * 4.2f; for (var k = -1; k <= 1; k++) { var gold = k == 0; var b = bp + S * k * 1.02f; mk.Box(gold ? blockM : brick, b, Vector3.one, rot); if (gold) foreach (var f in new[] { D, -D }) mk.Box(GlowM(new Color(1f, .96f, .7f), 1.4f), b + f * .51f, new Vector3(.2f, .5f, .02f), Quaternion.LookRotation(f)); }
                var ge = n % 2 == 0 ? 1f : -1f; var pg = G(ctx, d + 6f, ge * (RoadW / 2f + 2.4f)); GreenPipe(ctx, mk, solid, pg, 1f + (n % 3) * .8f);
                Lamp(ctx, mk, G(ctx, d + 2f, -ge * (RoadW / 2f + 1f)), n % 2 == 0);
                for (var k = 0; k < 3; k++) Coin(ctx.Root, G(ctx, d + 8f + k * 1.4f, 0) + up * 1.4f);
            }
            // the flagpole at the end of the road
            var fp = G(ctx, streetL + 2f, RoadW / 2f + 1.6f); mk.Box(stoneM, fp + up * .5f, new Vector3(1f, 1f, 1f), rot); solid.Box(stoneM, fp + up * .5f, new Vector3(1f, 1f, 1f), rot); mk.Prism(white, fp + up * 1f, up, .07f, 9f, 6); mk.Blob(GlowM(Pipe, 1.4f), fp + up * 10.1f, Vector3.one * .26f, new System.Random(1), 0f); ArtistBits.PostCollider(ctx.Root, fp, 10f, .1f);
            var flag = new MeshKit(); flag.Tri(GlowM(new Color(.2f, .9f, .4f), 1f), Vector3.zero, -S * 1.6f + up * -.6f, up * -1.2f, D); flag.Tri(GlowM(new Color(.2f, .9f, .4f), 1f), Vector3.zero, -S * 1.6f + up * -.6f, up * -1.2f, -D); var fg = flag.BuildSingle("Flag", ctx.Root, false); fg.transform.position = fp + up * 9.6f; life.flag = fg.transform; life.flagTop = fp + up * 9.6f; life.flagBottom = fp + up * 2.4f;
            string[] cleared = { "COURSE CLEAR! The flag slides down. Fireworks, probably, somewhere.", "You grab the flag. 5000 points. The Ring's just ahead.", "WORLD CLEAR. Next stop: everywhere else." }; var cn = 0;
            Spot("The flagpole", fp + up * 2f, "Grab the flag", new Vector3(1f, 3f, 1f)).onUse = _ => { life.flagGrab = Time.time; life.played = Time.time; Say(tlife.transform, fp + up * 3.4f, cleared[cn++ % cleared.Length], 4.5f); };
            Folk(G(ctx, 30f, 1.6f), D, Resident.Pose.Walk, new[] { "Hit the gold blocks from underneath. Or don't. They're very high.", "Left to right. Always left to right." }, Local(), new[] { G(ctx, 30f, 1.6f), G(ctx, streetL - 6f, 1.6f), G(ctx, streetL - 6f, -1.6f), G(ctx, 30f, -1.6f) }).speed = 1.2f;
            Shot(ctx, "along the level road", G(ctx, 14f, -1.5f) + up * 2.4f, G(ctx, streetL + 30f, 0) + up * 10f);
        }

        // ---------------------------------------------------------------- the Ring
        void TheRing(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rc = G(ctx, streetL + 48f, 0) - up * 18f; const float R = 48f, W = 6f, T = 2.4f; var silver = Paint(new Color(.66f, .7f, .78f)); var inner = new Material(WorldMaterials.Glow(new Color(.3f, .6f, 1f), 1.1f)) { name = "Bit ring" }; life.ring = inner;
            for (var k = 0; k < 72; k++)
            {
                float a0 = k * 5f * Mathf.Deg2Rad, a1 = (k + 1) * 5f * Mathf.Deg2Rad; Vector3 P(float a, float r) => rc + (S * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
                var mid = P((a0 + a1) / 2f, R); var gy = ctx.Ground(mid.x, mid.z); if (mid.y < gy - 3f) continue;
                var rr = Quaternion.LookRotation(D, (P((a0 + a1) / 2f, R) - rc).normalized);
                var len = 2f * R * Mathf.Sin(2.5f * Mathf.Deg2Rad) + .1f; mk.Box(silver, mid, new Vector3(len, T, W), rr); mk.Box(inner, P((a0 + a1) / 2f, R - T / 2f - .02f), new Vector3(len, .04f, W * .5f), rr); foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(new Color(.3f, .6f, 1f), 1.4f), mid + D * e * (W / 2f + .02f), new Vector3(len, .2f, .04f), rr);
                if (mid.y < gy + 6f) solid.Box(silver, mid, new Vector3(len, T, W), rr);
            }
            ArtistBits.PointLight(ctx.Root, rc + up * (R - 6f), new Color(.5f, .7f, 1f), 40f, 1.6f);
            Shot(ctx, "the ring", G(ctx, streetL - 4f, 6f) + up * 3f, rc + up * R * .7f);
        }
    }

    /// The 8-Bit Isle's holder for things you can use and carry.
    public class BitTownLife : MonoBehaviour { }
}
