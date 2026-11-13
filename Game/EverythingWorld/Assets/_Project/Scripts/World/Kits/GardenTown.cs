using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Sample Gardens' places (part of GardenKit): the brick garden where you arrive (a round of herringbone brick with
    /// a turntable on a tree stump, crates of records and radios in the beds) in front of the Greenhouse Studio (a white
    /// iron greenhouse open on the sides, glass roof, ferns and a mixing desk, turntables and speakers among the pots, a
    /// stool to sit at), and the Crate Walk (a brick path under arches of climbing roses, crates and pots either side).
    public partial class GardenKit
    {
        GardenTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.5f, .6f, .4f), new Color(.86f, .8f, .66f), Terracotta, new Color(.4f, .5f, .6f), Gold };
        static readonly Color[] Bottoms = { new Color(.3f, .3f, .36f), new Color(.5f, .4f, .3f), new Color(.24f, .26f, .2f) };
        static readonly string[] TownTalk =
        {
            "Every sound in this garden came from somewhere else first.", "Dig in any bed and you'll find a record.", "Drop the needle on the stump. See what grows.",
            "The radios in the flower beds all play different stations. Somehow it works.", "Follow the roses down to the marble machine.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2000);
            tlife = new GameObject("Garden town life").AddComponent<GardenTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None }, new[] { Gold, Rose, Leaf }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the brick garden and the Greenhouse Studio
        void Greenhouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(4);
            mk.Prism(brickD, c - up * .3f, up, 12.4f, .34f, 32); mk.Prism(brick, c + up * .04f, up, 12f, .02f, 32); for (var k = 0; k < 24; k++) { var d = Quaternion.Euler(0, k * 15f, 0) * D; mk.Box(brickD, c + up * .055f + d * 6f, new Vector3(.06f, .01f, 12f), Quaternion.LookRotation(Quaternion.Euler(0, 90f, 0) * d)); }
            for (var k = -3; k <= 3; k++) Remember(c + Quaternion.Euler(0, k * 35f, 0) * D * 11f, 0);
            // the greenhouse
            var g = new PlaceFrame(c - D * 17f, D); const float W = 14f, L = 9f, H = 3.6f;
            mk.Box(brick, g.P(0, .05f, 0), new Vector3(W, .3f, L), g.r);
            for (var x = -W / 2f; x <= W / 2f + .01f; x += 1.75f) foreach (var z in new[] { -L / 2f, L / 2f }) { if (z > 0 && Mathf.Abs(x) < 1.2f) continue; var pp = g.P(x, 0, z); mk.Box(iron, pp + up * H / 2f, new Vector3(.1f, H, .1f), g.r); ArtistBits.PostCollider(ctx.Root, pp, H, .1f); }
            for (var z = -L / 2f + 1.5f; z < L / 2f - .1f; z += 1.5f) foreach (var x in new[] { -W / 2f, W / 2f }) { var pp = g.P(x, 0, z); mk.Box(iron, pp + up * H / 2f, new Vector3(.1f, H, .1f), g.r); ArtistBits.PostCollider(ctx.Root, pp, H, .1f); }
            foreach (var z in new[] { -L / 2f, L / 2f }) mk.Box(iron, g.P(0, H, z), new Vector3(W, .12f, .12f), g.r); foreach (var x in new[] { -W / 2f, W / 2f }) mk.Box(iron, g.P(x, H, 0), new Vector3(.12f, .12f, L), g.r);
            foreach (var e in new[] { -1f, 1f }) { var a = Mathf.Atan2(2f, L / 2f) * Mathf.Rad2Deg; mk.Box(glassM, g.P(0, H + 1f, e * L / 4f), new Vector3(W, .04f, Mathf.Sqrt(4f + L * L / 16f)), g.r * Quaternion.Euler(e * a, 0, 0)); for (var x = -W / 2f; x <= W / 2f + .01f; x += 1.75f) mk.Box(iron, g.P(x, H + 1f, e * L / 4f), new Vector3(.06f, .06f, Mathf.Sqrt(4f + L * L / 16f)), g.r * Quaternion.Euler(e * a, 0, 0)); } mk.Box(iron, g.P(0, H + 2f, 0), new Vector3(W, .12f, .12f), g.r);
            foreach (var x in new[] { -W / 2f, W / 2f }) for (var k = 0; k < 4; k++) mk.Tri(glassM, g.P(x, H, -L / 2f + k * L / 4f), g.P(x, H, -L / 2f + (k + 1) * L / 4f), g.P(x, H + 2f - Mathf.Abs(-L / 2f + (k + .5f) * L / 4f) * 4f / L, -L / 2f + (k + .5f) * L / 4f), g.P(0, H, 0));
            halos.Add(g.P(0, 2f, 0), 7f, Gold, .2f); ArtistBits.PointLight(ctx.Root, g.P(0, 3f, 0), Gold, 16f, 1.8f, true);
            // inside: ferns in pots, a mixing desk, turntables and speakers among the plants, a stool
            for (var k = 0; k < 10; k++) { var pp = g.P((k % 5 - 2f) * 2.6f + (k / 5) * 1.3f, .2f, (k / 5 == 0 ? -1f : 1f) * 3.2f); mk.Prism(Paint(Terracotta), pp, up, .4f, .6f, 8, 1.3f); for (var j = 0; j < 6; j++) mk.Tri(leafM, pp + up * .5f, pp + up * (1.4f + (j % 2) * .3f) + Quaternion.Euler(0, j * 60f + k * 13f, 0) * D * .9f, pp + up * (1f + (j % 2) * .3f) + Quaternion.Euler(0, j * 60f + k * 13f + 20f, 0) * D * .9f, pp - up); ArtistBits.PostCollider(ctx.Root, pp, 1f, .5f); }
            var dk = g.P(0, .2f, -1.2f); mk.Box(woodD, dk + up * .45f, new Vector3(3.6f, .9f, 1f), g.r); mk.Box(Paint(new Color(.3f, .3f, .32f)), dk + up * .92f, new Vector3(3.4f, .04f, .9f), g.r); for (var k = 0; k < 16; k++) mk.Box(k % 4 == 0 ? dialM : Paint(new Color(.8f, .78f, .7f)), dk + up * .96f + g.Right * (-1.5f + (k % 8) * .42f) + g.Fwd * ((k / 8) * .3f - .15f), new Vector3(.06f, .06f, .14f), g.r); BoxCol(ctx.Root, dk + up * .45f, new Vector3(3.6f, .9f, 1f), g.r);
            foreach (var e in new[] { -1f, 1f }) { var tt = g.P(e * 3.4f, .2f, -1.2f); mk.Box(woodD, tt + up * .4f, new Vector3(1f, .8f, .8f), g.r); mk.Prism(vinylM, tt + up * .82f, up, .32f, .02f, 18); mk.Prism(Paint(new Color(.9f, .6f, .3f)), tt + up * .84f, up, .1f, .01f, 10); BoxCol(ctx.Root, tt + up * .4f, new Vector3(1f, .8f, .8f), g.r); var sp = g.P(e * 5.6f, .2f, -2.6f); mk.Box(woodD, sp + up * .7f, new Vector3(.8f, 1.4f, .7f), g.r); mk.Prism(dark, sp + up * .9f + g.Fwd * .36f, g.Fwd, .26f, .02f, 14); BoxCol(ctx.Root, sp + up * .7f, new Vector3(.8f, 1.4f, .7f), g.r); }
            var st = g.P(0, .2f, -.1f); mk.Prism(wood, st, up, .2f, .5f, 10); Interact.Seat(tlife.transform, st + up * .52f, -g.Fwd, "Sit at the mixing desk").seatHeight = .5f;
            // the turntable on a tree stump
            var tt2 = c + D * 3f + S * 3f; mk.Prism(trunkM, tt2, up, .6f, .8f, 10); mk.Prism(Paint(new Color(.66f, .5f, .36f)), tt2 + up * .8f, up, .58f, .02f, 10); mk.Box(woodD, tt2 + up * .88f, new Vector3(.8f, .12f, .6f), Quaternion.LookRotation(D)); var rec = new MeshKit(); rec.Prism(vinylM, Vector3.zero, Vector3.up, .3f, .02f, 20); rec.Prism(GlowM(Rose, 1f), Vector3.up * .021f, Vector3.up, .1f, .005f, 12); var rg = rec.BuildSingle("Record", ctx.Root, false); rg.transform.position = tt2 + up * .95f; life.records.Add(rg.transform); ArtistBits.PostCollider(ctx.Root, tt2, 1f, .6f);
            string[] needle = { "You drop the needle. A crackle, a loop of strings from somewhere, and every radio in the garden turns up a notch.", "A drum break you've heard a thousand times. The roses nod along.", "The record skips, then skips again, and that's the song now." }; var nn = 0;
            Spot("The turntable on the stump", tt2 + up * 1f, "Drop the needle", new Vector3(1f, .6f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, tt2 + up * 2.2f, needle[nn++ % needle.Length], 5f); };
            for (var k = 0; k < 6; k++) { var a = Quaternion.Euler(0, k * 60f + 20f, 0) * D; var bp = c + a * 9f; if (Vector3.Angle(a, D) < 25f || Vector3.Angle(a, -D) < 25f) continue; mk.Prism(brickD, bp - up * .2f, up, 1.6f, .4f, 10); mk.Prism(soil, bp + up * .2f, up, 1.4f, .02f, 10); Flowers(mk, bp + up * .2f, 1.2f, rng); Radio(mk, bp + up * .2f, -a); BoxCol(ctx.Root, bp + up * .45f, new Vector3(3f, 1.1f, 3f), Quaternion.identity); }
            for (var k = 0; k < 5; k++) Crate(ctx.Root, mk, c - D * 4f + S * (k - 2f) * .5f + S * 5f, D, rng);
            foreach (var e in new[] { -1f, 1f }) Lamp(ctx, mk, c + S * e * 10f + D * 2f, true);
            Folk(c + D * 5f - S * 3f, (-D + S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Gardens. Everything here was sampled from something.", "The greenhouse is the studio. The plants are the band.", "Drop the needle on the stump. Go on." });
            Folk(g.P(1.4f, .2f, -2.2f), g.Fwd, Resident.Pose.Stand, new[] { "This fern is on three records.", "Don't water the turntables. We've had words about it." }, Local());
            Folk(tt2 + S * 1f, -S, Resident.Pose.Stand, null, Local(Residents.Tool.Mug));
            Mark(c, 12.5f); Mark(g.o, 8f);
            Shot(ctx, "the greenhouse studio", c + D * 4f + S * 6f + up * 2.4f, g.P(0, 2.4f, 0));
        }

        // ---------------------------------------------------------------- the Crate Walk
        void CrateWalk(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 12.5f; d <= streetL + 4.01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return; var rng = new System.Random(9);
            Strip(mk, pts, brick, WalkW, .1f, ctx); foreach (var e in new[] { -1f, 1f }) { var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(p + S * e * (WalkW / 2f - .1f)); Strip(mk, edge, brickD, .2f, .12f, ctx); } foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 18f; d <= streetL; d += 8f, n++)
            {
                var a0 = G(ctx, d, -(WalkW / 2f + .2f)); var a1 = G(ctx, d, WalkW / 2f + .2f); foreach (var ap in new[] { a0, a1 }) { mk.Prism(iron, ap, up, .05f, 2.6f, 5); ArtistBits.PostCollider(ctx.Root, ap, 2.6f, .08f); }
                for (var k = 0; k <= 10; k++) { var a = Mathf.PI * k / 10f; var p = (a0 + a1) / 2f + S * Mathf.Cos(a) * (WalkW / 2f + .2f) + up * (2.6f + Mathf.Sin(a) * 1.2f); mk.Box(iron, p, new Vector3(.08f, .08f, .4f), Quaternion.LookRotation(S) * Quaternion.Euler(-a * Mathf.Rad2Deg + 90f, 0, 0)); mk.Blob(leafD, p + D * .1f, new Vector3(.3f, .24f, .3f), rng, .3f); if (k % 2 == 0) mk.Blob(Paint(Rose), p + D * .2f + up * .1f, Vector3.one * .1f, rng, 0f); }
                foreach (var e in new[] { -1f, 1f }) { var side = G(ctx, d + 4f, e * (WalkW / 2f + 1.2f)); if (n % 2 == 0) { for (var k = 0; k < 4; k++) Crate(ctx.Root, mk, side + D * (k % 2 - .5f) * .44f + up * (k / 2) * .42f, -S * e, rng); } else { mk.Prism(Paint(Terracotta), side, up, .4f, .5f, 8, 1.3f); mk.Blob(leafM, side + up * .9f, new Vector3(.6f, .6f, .6f), rng, .3f); ArtistBits.PostCollider(ctx.Root, side, 1f, .5f); } }
                if (n % 2 == 1) Lamp(ctx, mk, G(ctx, d + 2f, (n % 4 == 1 ? 1f : -1f) * (WalkW / 2f + 2.4f)), n % 4 == 1);
            }
            Folk(G(ctx, 20f, 1f), D, Resident.Pose.Walk, new[] { "I come down here for the roses. And the crates. Mostly the crates.", "Hear that? The marble machine's going." }, Local(), new[] { G(ctx, 20f, 1f), G(ctx, streetL, 1f), G(ctx, streetL, -1f), G(ctx, 20f, -1f) }).speed = .9f;
            Shot(ctx, "along the crate walk", G(ctx, 13f, -.6f) + up * 1.8f, G(ctx, streetL + 14f, 0) + up * 3f);
        }
    }

    /// The Sample Gardens' holder for things you can use and carry.
    public class GardenTownLife : MonoBehaviour { }
}
