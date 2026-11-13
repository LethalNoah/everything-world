using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Melodic Bass Highlands' places (part of HighKit): the Rune Circle (a ring of standing stones carved with glowing
    /// runes round a turf floor inlaid with a rune ring, a great floating island overhead pouring a waterfall into a pool,
    /// a rune stone to touch), the Rune Road (stone flags between rune stones and lanterns) and the Sky Stair (a spiral of
    /// floating stone steps round a glowing crystal pillar, up to a lookout island high above the land).
    public partial class HighKit
    {
        HighTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { Violet, Indigo, Pink, new Color(.3f, .3f, .4f), Gold };
        static readonly Color[] Bottoms = { Indigo, new Color(.2f, .18f, .26f), new Color(.3f, .26f, .36f) };
        static readonly string[] TownTalk =
        {
            "When the drop hits, the islands lift a little. Watch.", "Climb the Sky Stair at the end of the road. You can see every island from the top.", "The runes glow brighter when the music swells.",
            "Nobody knows what holds the islands up. I think it's the bass.", "Stand under a waterfall. It doesn't land. It never lands.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2013);
            tlife = new GameObject("High town life").AddComponent<HighTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None }, new[] { Gold, Pink, Rune }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Rune Circle
        void RuneCircle(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(7);
            mk.Prism(grassTop, c - up * .3f, up, 16f, .4f, 36); mk.Prism(runeM, c + up * .11f, up, 10f, .02f, 36); mk.Prism(grassTop, c + up * .12f, up, 9.6f, .02f, 36);
            for (var k = 0; k < 24; k++) { var d = Quaternion.Euler(0, k * 15f + 7f, 0) * D; mk.Box(runeM, c + d * 9.8f + up * .14f, new Vector3(.5f, .02f, .12f), Quaternion.LookRotation(d) * Quaternion.Euler(0, k * 37f, 0)); }
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * D; Remember(c + d * 13f, 0); if (k == 0 || k == 6) continue; var sp = c + Quaternion.Euler(0, 15f, 0) * d * 14.5f; var h = 4.4f + (k % 3) * .8f; var sr = Quaternion.LookRotation(-d) * Quaternion.Euler(0, 0, (k % 2 - .5f) * 4f); mk.Box(rock, sp + up * (h / 2f - .3f), new Vector3(1.4f, h, .9f), sr); solid.Box(rock, sp + up * (h / 2f - .3f), new Vector3(1.4f, h, .9f), sr); for (var j = 0; j < 5; j++) mk.Box(runeM, sp + up * (.8f + j * .7f) - (Quaternion.Euler(0, 15f, 0) * d) * .46f, new Vector3(.4f - (j % 2) * .16f, .07f, .02f), sr * Quaternion.Euler(0, 0, j * 40f)); halos.Add(sp + up * h * .6f - (Quaternion.Euler(0, 15f, 0) * d) * .8f, 1.6f, Rune, .25f); }
            // the great floating island overhead, its waterfall falling into a pool
            var pool = c - D * 20f; pool = ctx.OnGround(pool.x, pool.z); mk.Prism(rockD, pool - up * .4f, up, 5f, .7f, 20); mk.Prism(waterM, pool + up * .26f, up, 4.6f, .02f, 20); BoxCol(ctx.Root, pool, new Vector3(9.6f, .6f, 9.6f), Quaternion.identity); life.splash = pool + up * .3f; Mark(pool, 5.6f);
            var big = new MeshKit(); big.Prism(rock, Vector3.zero, -up, 11f, 15f, 11, .05f, 10f); big.Prism(rockD, -up * 5f, -up, 6f, 12f, 8, .05f); big.Prism(grassTop, Vector3.zero, up, 11.2f, .4f, 11, .96f, 10f); for (var k = 0; k < 3; k++) { var tp = Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * 5f; big.Prism(trunkM, tp, up, .4f, 3f, 6, .6f); big.Blob(GlowM(new Color(1f, .6f, .74f), .5f), tp + up * 4f, new Vector3(2.4f, 1.6f, 2.4f), rng, .25f); } big.Prism(rock, Vector3.zero + Vector3.right * 2f, up, 1.2f, 5f, 6, .3f); big.Blob(runeM, Vector3.right * 2f + up * 5.4f, Vector3.one * .6f, rng, 0f);
            var bigAt = pool + up * 30f + D * -6f; var bg = big.BuildSingle("The great floating island", ctx.Root, false); bg.transform.position = bigAt; life.floaters.Add((bg.transform, bigAt, 0f));
            var wf = new MeshKit(); var top = bigAt + D * 6f - up * 1f; wf.Box(waterM, (top + pool) / 2f + up * .2f, new Vector3(1.6f, top.y - pool.y, .14f), Quaternion.LookRotation(D));  wf.Build("The great waterfall", ctx.Root, false); halos.Add(pool + up * 1.5f, 5f, Water, .3f);
            ArtistBits.PointLight(ctx.Root, pool + up * 3f, Water, 16f, 1.6f);
            // a rune stone in the middle to touch
            var rs = c + D * 3f; mk.Box(rock, rs + up * .7f, new Vector3(1f, 1.4f, 1f), Quaternion.Euler(0, 45f, 0)); mk.Box(runeM, rs + up * 1.42f, new Vector3(.6f, .02f, .6f), Quaternion.Euler(0, 45f, 0)); BoxCol(ctx.Root, rs + up * .7f, new Vector3(1f, 1.4f, 1f), Quaternion.Euler(0, 45f, 0));
            string[] touch = { "The runes flare violet all the way round the circle, and every floating island rises a little.", "A low hum comes up through your feet. The waterfall glitters.", "The stones answer you, one after another, like a drop building." }; var tn = 0;
            Spot("The rune stone", rs + up * 1.4f, "Touch the rune stone", new Vector3(1.2f, 1f, 1.2f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, rs + up * 2.6f, touch[tn++ % touch.Length], 4.5f); };
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1f, 1f }) Lamp(ctx, mk, c + S * e * 8f + D * q * 8f, true);
            Folk(c + D * 6f + S * 3f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Highlands. Look up. Always look up.", "The Rune Road runs to the Sky Stair. Climb it. Trust the steps.", "Touch the stone in the middle. The whole circle wakes up." });
            Folk(pool + S * 6f, -S, Resident.Pose.Stand, new[] { "It falls from up there and never runs out.", "I come here to think. Mostly I just watch the water." }, Local());
            Mark(c, 16.5f);
            Shot(ctx, "the rune circle", c + D * 14f + S * 6f + up * 3f, pool + up * 12f);
        }

        // ---------------------------------------------------------------- the Rune Road
        void RuneRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 13f; d <= streetL + 4.01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, path, RoadW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            for (var i = 2; i < pts.Count; i += 3) mk.Box(runeM, pts[i] + up * .16f, new Vector3(.6f, .02f, .14f), Quaternion.LookRotation(D) * Quaternion.Euler(0, i * 47f, 0));
            var n = 0; for (var d = 22f; d <= streetL - 2f; d += 12f, n++) foreach (var e in new[] { -1f, 1f }) { var g = G(ctx, d + (e > 0 ? 0f : 6f), e * (RoadW / 2f + 1f)); if (n % 2 == 0) Lamp(ctx, mk, g, (n + (e > 0 ? 0 : 1)) % 2 == 0); else { mk.Box(rock, g + up * 1.3f, new Vector3(.8f, 2.6f, .6f), Quaternion.LookRotation(S)); for (var j = 0; j < 3; j++) mk.Box(runeM, g + up * (.7f + j * .6f) - S * e * .31f, new Vector3(.3f, .06f, .02f), Quaternion.LookRotation(S)); BoxCol(ctx.Root, g + up * 1.3f, new Vector3(.8f, 2.6f, .6f), Quaternion.LookRotation(S)); } }
            Folk(G(ctx, 26f, 1.4f), D, Resident.Pose.Walk, new[] { "Every rune on the road is a different note. I've been trying to read them for years.", "Evening! Or morning. It's always this time here." }, Local(), new[] { G(ctx, 26f, 1.4f), G(ctx, streetL - 4f, 1.4f), G(ctx, streetL - 4f, -1.4f), G(ctx, 26f, -1.4f) }).speed = 1f;
            Shot(ctx, "along the rune road", G(ctx, 14f, -1f) + up * 2f, G(ctx, streetL + 12f, 0) + up * 10f);
        }

        // ---------------------------------------------------------------- the Sky Stair
        void SkyStair(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var sc = G(ctx, streetL + 12f, 0); var rng = new System.Random(9); const int N = 40; const float rise = .35f, R = 5f, H = N * rise;
            mk.Prism(rockD, sc - up * .3f, up, 8f, .44f, 24); for (var k = 0; k < 10; k++) Remember(sc + Quaternion.Euler(0, k * 36f, 0) * D * 8.6f, 1);
            // the crystal pillar
            mk.Prism(crystalM, sc, up, 1.2f, H + 1f, 6, .7f); solid.Prism(rock, sc, up, 1.3f, H, 6); halos.Add(sc + up * H * .5f, 4f, Pink, .25f); ArtistBits.PointLight(ctx.Root, sc + up * 3f, Pink, 16f, 1.6f);
            // the steps, a spiral of floating stones
            var start = Mathf.Atan2(-D.z, -D.x);
            for (var k = 0; k < N; k++)
            {
                var a = start + k * 13.5f * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var tang = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)); var sp = sc + dir * R + up * (rise * (k + 1) - .2f);
                var sr = Quaternion.LookRotation(tang); mk.Box(rock, sp, new Vector3(2.2f, .4f, 1.25f), sr); solid.Box(rock, sp, new Vector3(2.2f, .4f, 1.25f), sr); mk.Prism(rockD, sp - up * .2f, -up, .7f, .8f, 5, .1f);
                if (k % 5 == 2) { mk.Box(runeM, sp + up * .21f, new Vector3(.8f, .02f, .2f), sr); }
                if (k % 8 == 4) { mk.Prism(lampM, sp + dir * 1.3f + up * .8f, up, .12f, .3f, 6); halos.Add(sp + dir * 1.3f + up * .95f, 1f, Gold, .4f); }
            }
            // the lookout island at the top
            var top = sc + up * H; var endA = start + (N - 1) * 13.5f * Mathf.Deg2Rad; var endDir = new Vector3(Mathf.Cos(endA), 0, Mathf.Sin(endA));
            mk.Prism(grassTop, top - up * .3f, up, 4.2f, .5f, 18); solid.Prism(grassTop, top - up * .6f, up, 4.2f, .8f, 12); mk.Prism(rock, top - up * .3f, -up, 4.2f, 5f, 10, .05f, 12f);
            for (var k = 0; k < 18; k++) { var a = k * 20f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); if (Vector3.Angle(d, endDir) < 30f) continue; var pp = top + d * 3.9f; mk.Prism(iron, pp, up, .04f, 1f, 4); ArtistBits.PostCollider(ctx.Root, pp, 1.1f, .12f); var d2 = new Vector3(Mathf.Cos(a + 20f * Mathf.Deg2Rad), 0, Mathf.Sin(a + 20f * Mathf.Deg2Rad)); if (Vector3.Angle(d2, endDir) >= 30f) { var p2 = top + d2 * 3.9f; mk.Box(lampM, (pp + p2) / 2f + up * 1f, new Vector3(.05f, .05f, (p2 - pp).magnitude), Quaternion.LookRotation(p2 - pp)); BoxCol(ctx.Root, (pp + p2) / 2f + up * .55f, new Vector3(.1f, 1.1f, (p2 - pp).magnitude), Quaternion.LookRotation(p2 - pp)); } }
            Bench(ctx.Root, mk, top - endDir * 2f, -(-endDir)); var tel = top + Vector3.Cross(up, endDir) * 2.2f; mk.Prism(iron, tel, up, .05f, 1.2f, 5); mk.Prism(Paint(Gold), tel + up * 1.3f, (endDir + up * .3f).normalized, .1f, .9f, 8); ArtistBits.PostCollider(ctx.Root, tel, 1.4f, .2f);
            ArtistBits.PointLight(ctx.Root, top + up * 2f, Gold, 12f, 1.4f);
            Folk(top + endDir * -.5f + Vector3.Cross(up, endDir) * -1.6f, endDir, Resident.Pose.Stand, new[] { "You made it! Look: every island, all at once.", "On a clear night you can hear the music of the next land over.", "Don't look down. Actually, do. It's lovely." }, Local());
            Mark(sc, 8.6f);
            Shot(ctx, "the sky stair", sc + D * -16f + S * 10f + up * 4f, sc + up * H * .6f);
            Shot(ctx, "from the top of the sky stair", top + up * 1.8f - endDir * 3f, center + up * 4f);
        }
    }

    /// The Melodic Bass Highlands' holder for things you can use and carry.
    public class HighTownLife : MonoBehaviour { }
}
