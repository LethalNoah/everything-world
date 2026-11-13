using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Liquid Rapids' places (part of RapidsKit): the Falls where you arrive (a warm stone terrace beside a rock face
    /// with a waterfall pouring into a glowing pool, stepping stones, a rope swing over the water, spray in the air), the
    /// River Walk (a path beside a long river of rapids and rocks, crossed by wooden footbridges) and the Weir (the river
    /// dropping over stone steps beside a mill with a turning water wheel).
    public partial class RapidsKit
    {
        RapidsTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.9f, .86f, .8f), new Color(.3f, .5f, .7f), new Color(.8f, .5f, .3f), new Color(.4f, .6f, .4f), Gold };
        static readonly Color[] Bottoms = { new Color(.3f, .34f, .44f), new Color(.6f, .54f, .44f), new Color(.2f, .2f, .22f) };
        static readonly string[] TownTalk =
        {
            "Listen to it. The water's keeping time.", "Follow the river down to the weir. The mill wheel never stops.", "Best evening of the summer. Every evening is.",
            "Jump in! Kidding. Paddle, maybe.", "It sounds like rain and drums at the same time.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2004);
            tlife = new GameObject("Rapids town life").AddComponent<RapidsTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None }, new[] { Gold, Water, Leaf }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Falls
        void Falls(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(4);
            mk.Prism(stoneD, c - up * .3f, up, 11f, .34f, 28); mk.Prism(stone, c + up * .04f, up, 10.6f, .02f, 28); for (var k = 0; k < 10; k++) Remember(c + Quaternion.Euler(0, k * 36f, 0) * D * 10f, 0);
            // the rock face and the waterfall
            var rf = c - D * 19f; for (var k = 0; k < 14; k++) mk.Blob(k % 2 == 0 ? stone : stoneD, rf + S * (k % 7 - 3f) * 3f + up * (2f + (k / 7) * 5f) - D * (k / 7) * 1f, new Vector3(2.4f, 3f, 2f), rng, .25f); solid.Box(stoneD, rf + up * 5f - D * 1f, new Vector3(22f, 10f, 4f), Quaternion.LookRotation(D));
            var pool = c - D * 13f; mk.Prism(stoneD, pool - up * .3f, up, 6.4f, .5f, 22); mk.Prism(waterM, pool + up * .21f, up, 6f, .02f, 22); BoxCol(ctx.Root, pool, new Vector3(12f, .5f, 12f), Quaternion.LookRotation(D));
            var top = rf + up * 11f + D * 1.4f; mk.Box(waterM, (top + pool + D * -4.4f) / 2f + up * .2f, new Vector3(4f, top.y - pool.y, .2f), Quaternion.LookRotation(D)); mk.Box(foamM, (top + pool + D * -4.4f) / 2f + up * .2f + D * .12f, new Vector3(2.4f, top.y - pool.y, .04f), Quaternion.LookRotation(D)); mk.Prism(foamM, pool - D * 4.2f + up * .23f, up, 2.6f, .02f, 16); life.sprays.Add(pool - D * 4f + up * .4f); life.sprays.Add(pool - D * 3f + up * .4f); halos.Add(pool - D * 3f + up * 2f, 5f, Water, .3f);
            ArtistBits.PointLight(ctx.Root, pool + up * 3f, new Color(.6f, .8f, 1f), 16f, 1.6f);
            // stepping stones over the stream to the river, a rope swing from an old tree over the pool
            for (var k = 0; k < 6; k++) { var sp = pool + S * (6f + k * 1.4f) + D * (k * 1.2f); mk.Prism(stone, ctx.OnGround(sp.x, sp.z) - up * .1f, up, .6f, .4f, 8); }
            var tr = pool + S * -7f; mk.Prism(trunkM, tr, (up + S * .3f).normalized, .5f, 6f, 8, .6f); mk.Prism(trunkM, tr + (up + S * .3f).normalized * 5.5f, (S + D * -.2f).normalized, .2f, 5f, 6, .5f); for (var k = 0; k < 6; k++) mk.Blob(willowM, tr + up * 6.4f + Quaternion.Euler(0, k * 60f, 0) * D * 2f, new Vector3(2f, 1.2f, 2f), rng, .3f); ArtistBits.PostCollider(ctx.Root, tr, 4f, .6f);
            var hang = tr + (up + S * .3f).normalized * 5.5f + (S + D * -.2f).normalized * 4f; var sw = new MeshKit(); sw.Prism(Paint(new Color(.7f, .62f, .48f)), Vector3.zero, Vector3.down, .03f, 4f, 4); sw.Prism(woodD, Vector3.down * 4f, Vector3.up, .25f, .1f, 10); var swing = sw.BuildSingle("Rope swing", ctx.Root, false); swing.transform.position = hang; life.swing = swing.transform; life.swingHome = hang;
            Interact.Seat(tlife.transform, hang - up * 3.9f, D, "Swing out over the water").seatHeight = .1f;
            string[] splash = { "You swing out over the pool, let go, and land with a splash. The whole bank cheers.", "Too short! Your feet skim the water. Go again.", "Perfect drop. The spray catches the sunset." }; var sn = 0;
            Spot("The rope swing", hang - up * 3f, "Swing and jump", new Vector3(1f, 1.4f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, hang - up * 1.6f, splash[sn++ % splash.Length], 4.5f); };
            for (var k = 0; k < 3; k++) Bench(ctx.Root, mk, c - D * 5f + S * (k - 1f) * 3.4f, -D);
            foreach (var e in new[] { -1f, 1f }) Lamp(ctx, mk, c + S * e * 10f, true);
            Folk(c + D * 4f - S * 3f, (-D + S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Rapids. Sit by the falls, or follow the river down.", "There's a rope swing over the pool. Everybody has a go.", "The water never stops. Neither does the music." });
            Folk(pool + S * 4f + D * 4f, -D, Resident.Pose.Sit, null, Local(), null, .05f);
            Mark(c, 11f); Mark(pool, 7f);
            Shot(ctx, "the falls", c + D * 6f + S * 6f + up * 2.2f, rf + up * 6f);
        }

        // ---------------------------------------------------------------- the River Walk and the Weir
        void RiverWalk(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(8);
            var river = new List<Vector3>(); for (var d = 12f; d <= streetL + 12.01f; d += 2f) { var p = G(ctx, d, 7.4f + Mathf.Sin(d * .06f) * 1.6f); river.Add(p); Mark(p, 4.2f); }
            Strip(mk, river, stoneD, 7f, .03f, ctx); Strip(mk, river, waterM, 5.6f, .12f, ctx); for (var i = 0; i < river.Count; i += 2) { mk.Blob(i % 4 == 0 ? stone : stoneD, river[i] + S * ((i % 3) - 1f) * 1.6f + up * .2f, new Vector3(.6f, .3f, .5f), rng, .3f); if (i % 3 == 0) mk.Box(foamM, river[i] + up * .08f + S * Mathf.Sin(i) * 1.2f, new Vector3(1.2f, .01f, .3f), Quaternion.LookRotation(D) * Quaternion.Euler(0, i * 17f, 0)); }
            life.river.AddRange(river);
            var walk = new List<Vector3>(); for (var d = 10.5f; d <= streetL + 6.01f; d += 2f) walk.Add(G(ctx, d, 0)); Strip(mk, walk, path, WalkW, .1f, ctx); foreach (var p in walk) Remember(p, 1);
            // footbridges over the river, arched, with rails following the arch
            var n = 0; for (var d = 24f; d <= streetL; d += 24f, n++)
            {
                var mid = G(ctx, d, 7.4f + Mathf.Sin(d * .06f) * 1.6f); var a = mid - S * 4.4f; var b = mid + S * 4.4f; a.y = ctx.Ground(a.x, a.z); b.y = ctx.Ground(b.x, b.z); var prev = Vector3.zero;
                for (var k = 0; k <= 10; k++)
                {
                    var t = k / 10f; var bp = Vector3.Lerp(a, b, t) + up * (Mathf.Sin(t * Mathf.PI) * .9f + .15f);
                    mk.Box(wood, bp, new Vector3(.82f, .1f, 1.6f), Quaternion.LookRotation(D)); solid.Box(wood, bp - up * .2f, new Vector3(.82f, .4f, 1.6f), Quaternion.LookRotation(D));
                    if (k > 0) foreach (var e in new[] { -.8f, .8f }) { var ra = prev + D * e + up * .95f; var rb = bp + D * e + up * .95f; mk.Box(woodD, (ra + rb) / 2f, new Vector3(.06f, .06f, (rb - ra).magnitude), Quaternion.LookRotation(rb - ra)); BoxCol(ctx.Root, (ra + rb) / 2f - up * .4f, new Vector3(.12f, .9f, (rb - ra).magnitude), Quaternion.LookRotation(rb - ra)); }
                    if (k % 2 == 0) foreach (var e in new[] { -.8f, .8f }) mk.Prism(woodD, bp + D * e, up, .04f, .95f, 4);
                    prev = bp;
                }
                Remember(a - S * .5f, 1); Remember(b + S * .5f, 1); Lamp(ctx, mk, a - S * 1.4f + D * 1.6f, n % 2 == 0);
            }
            for (var d = 18f; d <= streetL; d += 16f) Lamp(ctx, mk, G(ctx, d, -(WalkW / 2f + 1f)), ((int)d / 16) % 2 == 0);
            // the Weir and the mill
            var wc = G(ctx, streetL + 10f, 7.4f + Mathf.Sin((streetL + 10f) * .06f) * 1.6f); for (var k = 0; k < 3; k++) { mk.Box(stone, wc + D * k * 1.2f + up * (.4f - k * .25f), new Vector3(7f, .8f - k * .25f, 1.2f), Quaternion.LookRotation(D)); mk.Box(foamM, wc + D * (k * 1.2f + .62f) + up * (.4f - k * .25f), new Vector3(5.6f, .7f - k * .2f, .02f), Quaternion.LookRotation(D)); } solid.Box(stone, wc + D * 1.2f + up * .2f, new Vector3(7f, .6f, 3.6f), Quaternion.LookRotation(D)); life.sprays.Add(wc + D * 3f + up * .2f);
            var wl = Paint(new Color(.66f, .6f, .5f)); var mo0 = wc + S * 7f + D * 2f; var mill = new PlaceFrame(ctx.OnGround(mo0.x, mo0.z), -S); mk.Box(wl, mill.P(0, 2f, 0), new Vector3(5f, 4f, 4f), mill.r); solid.Box(wl, mill.P(0, 2f, 0), new Vector3(5f, 4f, 4f), mill.r); mk.Prism(Paint(new Color(.5f, .3f, .24f)), mill.P(0, 4f, 0), up, 3.6f, 1.6f, 4, .05f, 45f); mk.Box(GlowM(Gold, 1f), mill.P(1.2f, 2.4f, 2.01f), new Vector3(.8f, .7f, .02f), mill.r); mk.Box(dark, mill.P(-1f, 1.1f, 2.01f), new Vector3(1f, 2.2f, .02f), mill.r); halos.Add(mill.P(1.2f, 2.4f, 2.5f), 1.4f, Gold, .4f);
            var wm = new MeshKit(); for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; wm.Box(woodD, new Vector3(0, Mathf.Sin(a), Mathf.Cos(a)) * 1.8f, new Vector3(1f, .1f, .8f), Quaternion.Euler(-a * Mathf.Rad2Deg, 0, 0)); wm.Box(wood, new Vector3(0, Mathf.Sin(a), Mathf.Cos(a)) * .9f, new Vector3(.1f, .1f, 1.8f), Quaternion.Euler(-a * Mathf.Rad2Deg + 90f, 0, 0)); } wm.Prism(woodD, Vector3.left * .6f, Vector3.right, .3f, 1.2f, 10);
            var wheel = wm.BuildSingle("Water wheel", ctx.Root, false); wheel.transform.SetPositionAndRotation(mill.P(0, 2f, 2.6f) + mill.Fwd * .1f, Quaternion.LookRotation(D)); life.wheel = wheel.transform; BoxCol(ctx.Root, mill.P(0, 2f, 2.6f), new Vector3(3.8f, 3.8f, 1.2f), mill.r); life.wheelRot = Quaternion.LookRotation(D); life.sprays.Add(mill.P(0, .4f, 2.8f));
            Folk(mill.P(2.8f, 0, 3f), -mill.Right, Resident.Pose.Stand, new[] { "The wheel's turned every day for a hundred years.", "Sit on the weir and dangle your feet. Everyone does." }, Local());
            Folk(G(ctx, 22f, -1.2f), D, Resident.Pose.Walk, new[] { "Evening walk. Best thing in the world.", "Count the bridges. I always lose count." }, Local(), new[] { G(ctx, 22f, -1.2f), G(ctx, streetL, -1.2f), G(ctx, streetL, -.2f), G(ctx, 22f, -.2f) }).speed = .9f;
            Mark(wc, 5f); Mark(mill.o, 4f);
            Shot(ctx, "along the river walk", G(ctx, 11f, -1.4f) + up * 2f, G(ctx, streetL, 7f) + up * 2f);
            Shot(ctx, "the weir and the mill", wc - S * 2f + D * 10f + up * 2.4f, mill.P(0, 2f, 2f));
        }
    }

    /// The Liquid Rapids' holder for things you can use and carry.
    public class RapidsTownLife : MonoBehaviour { }
}
