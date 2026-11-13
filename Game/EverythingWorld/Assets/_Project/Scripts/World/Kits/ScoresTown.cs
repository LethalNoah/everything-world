using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Isle of Scores' places (part of ScoresKit): the Amphitheatre (stone tiers in a horseshoe round an orchestra of
    /// music stands and chairs, a great royal-blue shell with gold ribs behind them, the conductor's podium to step up to,
    /// braziers at the ends of the tiers) and the Star Road (a dark stone road set with glowing stars, lit by tall gold
    /// lanterns, running from the aisle out to the starship in the desert).
    public partial class ScoresKit
    {
        ScoresTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.1f, .1f, .12f), new Color(.86f, .86f, .9f), Velvet, Royal, new Color(.3f, .3f, .34f) };
        static readonly Color[] Bottoms = { new Color(.1f, .1f, .12f), new Color(.16f, .16f, .2f), new Color(.3f, .3f, .34f) };
        static readonly string[] TownTalk =
        {
            "Every film you ever loved has a tune you can hum. That's what this place is.", "Step up on the podium. Raise your arms. Go on, nobody's watching. Everybody's watching.",
            "Follow the Star Road to the end. Something landed out there.", "The stars come out when the strings come in.", "There's a town over the hill where it's always Halloween.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1977);
            tlife = new GameObject("Scores town life").AddComponent<ScoresTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Beanie }, new[] { Gold, Velvet, Royal }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Amphitheatre
        void Amphitheatre(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            // the orchestra floor: a round of stone with a gold ring and a compass star
            mk.Prism(stone, c - up * .28f, up, 13f, .42f, 36); mk.Prism(goldM, c + up * .14f, up, 12.4f, .02f, 36); mk.Prism(stone, c + up * .15f, up, 12.1f, .02f, 36); for (var k = 0; k < 8; k++) { var a0 = Quaternion.Euler(0, k * 45f, 0) * D; var a1 = Quaternion.Euler(0, k * 45f + 22.5f, 0) * D; var a2 = Quaternion.Euler(0, k * 45f - 22.5f, 0) * D; var len = k % 2 == 0 ? 3.4f : 2f; mk.Tri(goldM, c + up * .17f + a0 * len, c + up * .17f + a1 * .6f, c + up * .17f + a2 * .6f, c - up); }
            Remember(c + D * 11f, 0);
            // a ring walk round the outside of the tiers, so every lane leaves from it and none climbs the steps
            var ring = new List<Vector3>(); for (var k = 0; k <= 72; k++) { var rp = c + Quaternion.Euler(0, k * 5f, 0) * D * 26f; if (!ctx.IsLand(rp.x, rp.z, 1f)) { if (ring.Count > 1) Strip(mk, ring, stoneD, PathW, .13f, ctx); ring.Clear(); continue; } ring.Add(ctx.OnGround(rp.x, rp.z)); Remember(ctx.OnGround(rp.x, rp.z), 1); }
            if (ring.Count > 1) Strip(mk, ring, stoneD, PathW, .13f, ctx);
            // the shell: a quarter-sphere of royal blue with gold ribs, opening toward the tiers
            var sc = c - D * 12f; const float R = 10f; var shellIn = new Material(WorldMaterials.Glow(Royal, .45f)) { name = "Scores shell" }; life.shell = shellIn;
            Vector3 SP(float phi, float th) => sc + (Mathf.Cos(th) * (Mathf.Cos(phi) * -D + Mathf.Sin(phi) * S)) * R + up * (Mathf.Sin(th) * R);
            const int NP = 12, NT = 6;
            for (var i = 0; i < NP; i++) for (var j = 0; j < NT; j++)
            {
                float p0 = -Mathf.PI / 2f + Mathf.PI * i / NP, p1 = -Mathf.PI / 2f + Mathf.PI * (i + 1) / NP, t0 = Mathf.PI / 2f * j / NT, t1 = Mathf.PI / 2f * (j + 1) / NT;
                mk.Quad(shellIn, SP(p0, t0), SP(p1, t0), SP(p1, t1), SP(p0, t1), sc - D * R * 3f); mk.Quad(navy, SP(p0, t0) - D * .02f, SP(p1, t0) - D * .02f, SP(p1, t1) - D * .02f, SP(p0, t1) - D * .02f, sc);
            }
            for (var i = 0; i <= NP; i += 2) for (var j = 0; j < NT; j++) { float ph = -Mathf.PI / 2f + Mathf.PI * i / NP, t0 = Mathf.PI / 2f * j / NT, t1 = Mathf.PI / 2f * (j + 1) / NT; var a = SP(ph, t0); var b = SP(ph, t1); mk.Prism(goldM, a, (b - a).normalized, .1f, (b - a).magnitude, 5); }
            for (var i = 0; i < NP; i++) { float p0 = -Mathf.PI / 2f + Mathf.PI * i / NP, p1 = -Mathf.PI / 2f + Mathf.PI * (i + 1) / NP; var a = SP(p0, 0); var b = SP(p1, 0); solid.Box(stone, (a + b) / 2f + up * 2f, new Vector3((b - a).magnitude + .2f, 4f, .5f), Quaternion.LookRotation(Vector3.Cross(up, b - a).normalized)); }
            for (var k = 0; k < 10; k++) { var a = Mathf.PI * (k + .5f) / 10f - Mathf.PI / 2f; halos.Add(sc + (Mathf.Cos(a) * -D + Mathf.Sin(a) * S) * (R - .6f) + up * (R * .6f), 1.6f, Gold, .25f); mk.Blob(lampM, SP(a, .7f) + (sc - SP(a, .7f)).normalized * .2f, Vector3.one * .14f, rng, 0f); }
            ArtistBits.PointLight(ctx.Root, sc + D * 3f + up * 6f, new Color(1f, .84f, .56f), 26f, 2.2f);
            // the stage: a raised half-round in front of the shell
            mk.Prism(stoneD, sc - up * .2f, up, 8.6f, .7f, 24); solid.Prism(stoneD, sc - up * .2f, up, 8.6f, .7f, 12);
            // the orchestra: three arcs of chairs and music stands facing the podium
            for (var row = 0; row < 3; row++) for (var k = 0; k <= 6 + row * 2; k++)
            {
                var a = Mathf.Lerp(-70f, 70f, k / (float)(6 + row * 2)); var dir = Quaternion.Euler(0, a, 0) * D; var r = 3f + row * 1.8f; var cp = sc + dir * r + up * .5f; var face = -dir;
                mk.Box(dark, cp + up * .45f, new Vector3(.4f, .05f, .4f), Quaternion.LookRotation(face)); mk.Box(dark, cp + up * .7f - face * .2f, new Vector3(.4f, .5f, .04f), Quaternion.LookRotation(face)); foreach (var e in new[] { -.16f, .16f }) mk.Prism(iron, cp + Vector3.Cross(up, face) * e, up, .02f, .45f, 4);
                Stand(mk, cp + face * -.7f, face); BoxCol(rootT, cp + up * .7f - face * .3f, new Vector3(.55f, 1.4f, 1.3f), Quaternion.LookRotation(face));
                if (k % 3 == 1) Interact.Seat(tlife.transform, cp + up * .5f, -face, "Take a seat in the orchestra").seatHeight = .5f;
            }
            // the conductor's podium, at the front of the stage
            var pod = sc + D * 9.4f; mk.Prism(velvet, pod, up, .7f, .5f, 8); mk.Prism(goldM, pod + up * .5f, up, .72f, .04f, 8); BoxCol(ctx.Root, pod + up * .25f, new Vector3(1.4f, .5f, 1.4f), Quaternion.LookRotation(D)); mk.Prism(iron, pod + up * .5f - D * .4f, up, .03f, 1f, 4); mk.Box(standM, pod + up * 1.6f - D * .4f, new Vector3(.6f, .02f, .4f), Quaternion.LookRotation(-D) * Quaternion.Euler(20f, 0, 0));
            var baton = new MeshKit(); baton.Prism(white, Vector3.zero, Vector3.forward, .012f, .45f, 4); baton.Blob(dark, Vector3.zero, Vector3.one * .03f, rng, 0f); Thing(baton, "Baton", "the conductor's baton", pod + up * 1.65f - D * .4f, Quaternion.LookRotation(S), new Vector3(.1f, .1f, .5f), .1f, true);
            string[] conduct = { "You raise your arms. Every light on every music stand comes up gold, and somewhere a timpani rolls.", "A sweep of your hand and the whole shell glows. The orchestra waits for the downbeat.", "Bravo! Somebody in the back row is crying. It's fine. It's that kind of music." }; var cn = 0;
            Spot("The conductor's podium", pod + up * 1.3f, "Conduct the orchestra", new Vector3(1.4f, 1.4f, 1.4f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, pod + up * 2.6f, conduct[cn++ % conduct.Length], 5f); };
            // the tiers: a horseshoe of stone steps rising round the orchestra floor, an aisle down the middle toward the Star Road
            for (var r = 0; r < 6; r++)
            {
                var rad = 14.5f + r * 1.6f; var h = .45f * (r + 1);
                for (var a = -108f; a <= 108.1f; a += 8f)
                {
                    if (Mathf.Abs(a) < 16f) continue;
                    var dir = Quaternion.Euler(0, a, 0) * D; var p = c + dir * rad; var g = p; g.y = c.y; var rot = Quaternion.LookRotation(dir); var w = 2f * rad * Mathf.Sin(4f * Mathf.Deg2Rad) + .08f;
                    mk.Box(r % 2 == 0 ? stone : stoneD, g + up * (h / 2f - .3f), new Vector3(w, h + .6f, 1.6f), rot); solid.Box(stone, g + up * (h / 2f - .3f), new Vector3(w, h + .6f, 1.6f), rot);
                    if (r % 2 == 1 && Mathf.RoundToInt(a + 108f) % 24 == 0) Interact.Bench(tlife.transform, g + up * (h + .05f) - dir * .4f - Vector3.Cross(up, dir) * .6f, g + up * (h + .05f) - dir * .4f + Vector3.Cross(up, dir) * .6f, -dir);
                }
            }
            // braziers at the ends of the tiers
            foreach (var e in new[] { -1f, 1f }) { var bz = c + Quaternion.Euler(0, e * 116f, 0) * D * 16f; bz.y = c.y; mk.Prism(iron, bz, up, .3f, 1f, 8, .6f); mk.Prism(iron, bz + up * 1f, up, .5f, .3f, 10, 1.3f); mk.Cone(GlowM(Ember, 2f), bz + up * 1.2f, .4f, .8f, 7); life.fires.Add(bz + up * 1.6f); ArtistBits.PointLight(ctx.Root, bz + up * 2f, new Color(1f, .6f, .3f), 14f, 1.8f); ArtistBits.PostCollider(ctx.Root, bz, 1.3f, .5f); }
            Folk(sc + D * 6f + S * 4f + up * .5f, -S, Resident.Pose.Stand, new[] { "First violin. I've been tuning since Tuesday.", "Step up to the podium. We'll follow you. Probably.", "Tonight's programme: everything." }, Local());
            Folk(c + D * 8f - S * 5f + up * .14f, (-D + S * .4f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Isle of Scores! Every film you ever loved, under the stars.", "The Star Road runs out of the aisle. Follow it all the way.", "Sit on the steps. The best seats are all the seats." });
            Folk(c + D * 2f + S * 8f + up * .14f, -S, Resident.Pose.Sit, null, Local());
            Mark(c, 23f); Mark(sc, 11f);
            Shot(ctx, "the amphitheatre", c + D * 20f + S * 4f + up * 4f, sc + up * 4f);
            Shot(ctx, "the shell and the orchestra", c + D * 4f + up * 1.8f, sc + up * 3f);
        }

        // ---------------------------------------------------------------- the Star Road
        void StarRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 12.5f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, Paint(new Color(.14f, .16f, .26f)), RoadW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            var rng = new System.Random(9); for (var i = 0; i < pts.Count; i++) for (var k = 0; k < 2; k++) { var sp = pts[i] + S * ((float)rng.NextDouble() - .5f) * (RoadW - 1f) + D * (float)rng.NextDouble() * 2f; sp.y = ctx.Ground(sp.x, sp.z) + .16f; var sz = .06f + (float)rng.NextDouble() * .1f; mk.Box(starM, sp, new Vector3(sz, .02f, sz), Quaternion.Euler(0, (float)rng.NextDouble() * 90f, 0)); }
            var n = 0; for (var d = 26f; d <= streetL - 2f; d += 14f, n++) foreach (var e in new[] { -1f, 1f }) { var g = G(ctx, d, e * (RoadW / 2f + .8f)); mk.Prism(iron, g - up * .2f, up, .08f, 5.4f, 6, .6f); mk.Box(goldM, g + up * 5.3f, new Vector3(.4f, .06f, .4f), Quaternion.LookRotation(D)); for (var k = 0; k < 5; k++) { var a0 = Quaternion.Euler(0, 0, k * 72f) * Vector3.up; mk.Box(starM, g + up * 5.9f + Quaternion.LookRotation(S * e) * a0 * .26f, new Vector3(.12f, .4f, .04f), Quaternion.LookRotation(S * e) * Quaternion.Euler(0, 0, k * 72f)); } halos.Add(g + up * 5.9f, 1.8f, new Color(.9f, .9f, 1f), .4f); ArtistBits.PostCollider(ctx.Root, g, 5.4f, .1f); if ((n + (e > 0 ? 0 : 1)) % 2 == 0) ArtistBits.PointLight(ctx.Root, g + up * 5f, new Color(.86f, .88f, 1f), 14f, 1.4f); }
            // a sign at the start of the road
            var sg = G(ctx, 15f, RoadW / 2f + 2f); var sr = Quaternion.LookRotation(-D); mk.Prism(iron, sg, up, .05f, 2.4f, 5); mk.Box(navy, sg + up * 2.2f, new Vector3(1.8f, .5f, .06f), sr); Sign(sg + up * 2.2f - D * .04f, -D, "THE STAR ROAD", .2f, Gold); ArtistBits.PostCollider(ctx.Root, sg, 2.4f, .08f);
            Folk(G(ctx, 30f, 1.6f), D, Resident.Pose.Walk, new[] { "I walk it every night. Something different is out there every night.", "Mind the stars. They're warm." }, Local(), new[] { G(ctx, 30f, 1.6f), G(ctx, streetL - 6f, 1.6f), G(ctx, streetL - 6f, -1.6f), G(ctx, 30f, -1.6f) }).speed = 1.1f;
            Shot(ctx, "along the star road", G(ctx, 16f, -1.5f) + up * 2f, G(ctx, streetL + 20f, 0) + up * 5f);
        }
    }

    /// The Isle of Scores' holder for things you can use and carry.
    public class ScoresTownLife : MonoBehaviour { }
}
