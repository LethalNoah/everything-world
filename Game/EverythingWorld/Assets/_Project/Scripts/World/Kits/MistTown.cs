using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Ambient Mists' places (part of MistKit): the Meadow where you arrive (a lone tree with a rope swing, a picnic
    /// blanket, an old television showing colour bars, a radio), the Pylon Walk (a dirt track following a line of pylons
    /// and their sagging wires) and the Lake (still water, a lone tree on a tiny island, an old radio tower with a slow red
    /// light behind it, a jetty and a rowing boat).
    public partial class MistKit
    {
        MistTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.6f, .5f, .4f), new Color(.5f, .6f, .6f), new Color(.7f, .6f, .5f), new Color(.4f, .44f, .4f), new Color(.8f, .7f, .6f) };
        static readonly Color[] Bottoms = { new Color(.3f, .34f, .4f), new Color(.46f, .4f, .34f), new Color(.3f, .3f, .3f) };
        static readonly string[] TownTalk =
        {
            "It's always about six in the morning here. Nobody minds.", "Follow the wires to the lake. The tower's been humming since before I was born.", "Do you remember this? I feel like I remember this.",
            "The sheep don't do much. Neither do I. It's lovely.", "There's a tape somewhere of all of us, right here. I can't find it.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1998);
            tlife = new GameObject("Mist town life").AddComponent<MistTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Bucket }, new[] { Orange, Teal, Haze }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Meadow
        void Meadow(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(11);
            mk.Prism(Paint(new Color(.56f, .62f, .46f)), c - up * .3f, up, 10f, .34f, 30); for (var k = 0; k < 10; k++) Remember(c + Quaternion.Euler(0, k * 36f, 0) * D * 9f, 0);
            // the lone tree with a rope swing
            var tr = c - D * 6f; mk.Prism(trunkM, tr, up, .6f, 5f, 8, .7f); ArtistBits.PostCollider(ctx.Root, tr, 4f, .7f); var br = tr + up * 4.6f; mk.Prism(trunkM, br, (S + up * .2f).normalized, .26f, 4.4f, 6, .5f); for (var k = 0; k < 7; k++) mk.Blob(leafM, tr + up * (6f + (k % 2) * 1.4f) + Quaternion.Euler(0, k * 51f, 0) * D * (k == 0 ? 0f : 3f), new Vector3(3f, 2f, 3f), rng, .3f);
            var hang = br + (S + up * .2f).normalized * 3f; var sw = new MeshKit(); foreach (var e in new[] { -.3f, .3f }) sw.Prism(Paint(new Color(.7f, .62f, .5f)), new Vector3(0, 0, e), Vector3.down, .02f, 3.6f, 4); sw.Box(wood, new Vector3(0, -3.6f, 0), new Vector3(.4f, .06f, .8f), Quaternion.identity);
            var swing = sw.BuildSingle("Rope swing", ctx.Root, false); swing.transform.SetPositionAndRotation(hang, Quaternion.LookRotation(S)); life.swing = swing.transform; life.swingBase = Quaternion.LookRotation(S);
            Interact.Seat(tlife.transform, hang - up * 3.55f, D, "Sit on the swing").seatHeight = .55f;
            // the picnic blanket, an old TV and a radio
            var bl = c + S * 4f + D * 1f; for (var i = 0; i < 4; i++) for (var j = 0; j < 4; j++) mk.Box(Paint((i + j) % 2 == 0 ? new Color(.8f, .5f, .4f) : new Color(.86f, .8f, .7f)), bl + up * .03f + S * (i - 1.5f) * .55f + D * (j - 1.5f) * .55f, new Vector3(.55f, .02f, .55f), Quaternion.LookRotation(D));
            var tv = bl + S * 1.8f; OldTV(ctx.Root, mk, tv, -S, 1.1f);
            var rd = bl - D * .7f + up * .03f; mk.Box(Paint(new Color(.5f, .4f, .32f)), rd + up * .14f, new Vector3(.5f, .28f, .16f), Quaternion.LookRotation(D)); mk.Prism(dark, rd + up * .14f + D * .081f - S * .1f, D, .08f, .01f, 12); mk.Box(lampM, rd + up * .2f + D * .081f + S * .12f, new Vector3(.14f, .04f, .01f), Quaternion.LookRotation(D));
            string[] dial = { "You turn the dial. The colour bars wobble and, just for a second, there's a picture: a field, people waving, this field.", "Static, then an old jingle you half remember, then static again.", "The picture rolls. Somebody laughs on the tape. You think it might be you." }; var dn = 0;
            Spot("The old television", tv + up * .6f, "Turn the dial", new Vector3(1f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, tv + up * 1.6f, dial[dn++ % dial.Length], 5f); };
            foreach (var e in new[] { -1f, 1f }) Lamp(ctx, mk, c + S * e * 9.6f + D * 3f, true);
            Folk(c + D * 3f - S * 3f, (-D + S * .3f).normalized, Resident.Pose.Wave, new[] { "Morning. It's always morning here. Have a go on the swing.", "Follow the wires down to the lake. Take your time. There's nothing but time.", "The telly still works, sort of. Turn the dial." });
            Folk(bl + D * .4f - S * .3f, S, Resident.Pose.Sit, null, Local(), null, .05f);
            Mark(c, 10.5f); Mark(tr, 3f);
            Shot(ctx, "the meadow", c + D * 10f + S * 5f + up * 2.2f, tr + up * 3f);
        }

        // ---------------------------------------------------------------- the Pylon Walk
        void PylonWalk(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 9.5f; d <= streetL + 6.01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, dirtM, WalkW, .1f, ctx); foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 20f; d <= streetL; d += 18f, n++) Lamp(ctx, mk, G(ctx, d, (n % 2 == 0 ? 1f : -1f) * (WalkW / 2f + .8f)), n % 2 == 0);
            // the pylons and their wires, marching from behind the meadow to beyond the lake
            var tops = new List<Vector3>(); for (var d = -30f; d <= streetL + 60f; d += 38f) { var p = Wp(d, 13f); if (!ctx.IsLand(p.x, p.z, 4f) || InFootprint(ctx, p.x, p.z, 4f)) continue; var g = ctx.OnGround(p.x, p.z); Pylon(ctx, mk, g, D); tops.Add(g); Mark(g, 3f); }
            for (var i = 0; i + 1 < tops.Count; i++) foreach (var y in new[] { 22f * .72f, 22f * .9f }) foreach (var e in new[] { -4.2f, 4.2f }) { var a = tops[i] + up * (y - .8f) + S * e; var b = tops[i + 1] + up * (y - .8f) + S * e; for (var k = 0; k < 10; k++) { float t0 = k / 10f, t1 = (k + 1) / 10f; var p0 = Vector3.Lerp(a, b, t0) - up * Mathf.Sin(t0 * Mathf.PI) * 2.4f; var p1 = Vector3.Lerp(a, b, t1) - up * Mathf.Sin(t1 * Mathf.PI) * 2.4f; mk.Box(dark, (p0 + p1) / 2f, new Vector3(.04f, .04f, (p1 - p0).magnitude), Quaternion.LookRotation(p1 - p0)); } }
            Folk(G(ctx, 22f, 1f), D, Resident.Pose.Walk, new[] { "Listen. The wires hum in the fog.", "I walk down to the lake every morning. It's the same morning every time." }, Local(), new[] { G(ctx, 22f, 1f), G(ctx, streetL, 1f), G(ctx, streetL, -1f), G(ctx, 22f, -1f) }).speed = .8f;
            Shot(ctx, "along the pylon walk", G(ctx, 10f, -1f) + up * 2f, G(ctx, streetL + 24f, 0) + up * 6f);
        }

        // ---------------------------------------------------------------- the Lake
        void TheLake(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var lc = G(ctx, streetL + 24f, 0); var rng = new System.Random(13); const float R = 17f;
            mk.Prism(dirtM, lc - up * .5f, up, R + 1.2f, .56f, 36); mk.Prism(waterM, lc + up * .12f, up, R, .02f, 36); solid.Prism(dirtM, lc - up * .76f, up, R, .9f, 18);
            for (var k = 0; k < 24; k++) { var d = Quaternion.Euler(0, k * 15f, 0) * D; mk.Blob(Paint(new Color(.5f, .5f, .46f)), lc + d * (R + .6f) + up * .1f, new Vector3(.8f, .4f, .6f), rng, .3f); if (k % 2 == 0) Remember(lc + d * (R + 2.4f), 1); }
            for (var k = 0; k < 10; k++) { var rp = lc + Quaternion.Euler(0, k * 36f + 10f, 0) * D * (R - 1.4f); for (var j = 0; j < 4; j++) mk.Prism(Paint(new Color(.5f, .56f, .4f)), rp + new Vector3(j * .2f, 0, j * .14f), (Vector3.up + new Vector3(j - 1.5f, 0, .3f) * .1f).normalized, .03f, 1.4f + j * .2f, 4, .3f); }
            life.lake = lc + up * .14f;
            // the island and its lone tree
            var il = lc + S * 4f + D * 2f; mk.Prism(Paint(new Color(.5f, .56f, .42f)), il - up * .3f, up, 3.2f, .7f, 16); mk.Prism(trunkM, il + up * .3f, (up + S * .1f).normalized, .3f, 4f, 7, .6f); for (var k = 0; k < 5; k++) mk.Blob(leafM, il + up * 4.8f + Quaternion.Euler(0, k * 72f, 0) * D * (k == 0 ? 0f : 1.6f), new Vector3(2f, 1.4f, 2f), rng, .3f);
            // the old radio tower behind it, a slow red light on top
            var rt = lc + D * (R + 8f) - S * 4f; rt = ctx.OnGround(rt.x, rt.z); const float H = 34f; foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var foot = rt + new Vector3(a, 0, b) * 1.6f; var top = rt + new Vector3(a, 0, b) * .3f + up * H; mk.Prism(iron, foot - up * .3f, (top - foot).normalized, .08f, (top - foot).magnitude + .3f, 4); ArtistBits.PostCollider(ctx.Root, foot, 3f, .2f); } for (var k = 1; k < 12; k++) { var y = k * H / 12f; var w = Mathf.Lerp(1.6f, .3f, y / H); mk.Box(iron, rt + up * y, new Vector3(w * 2f, .05f, .05f), Quaternion.identity); mk.Box(iron, rt + up * y, new Vector3(.05f, .05f, w * 2f), Quaternion.identity); }
            var red = new Material(WorldMaterials.Glow(new Color(1f, .2f, .14f), 2f)) { name = "Mist tower light" }; life.towerLight = red; mk.Blob(red, rt + up * (H + .3f), Vector3.one * .3f, rng, 0f); halos.Add(rt + up * (H + .3f), 2.4f, new Color(1f, .3f, .2f), .4f); Mark(rt, 3f);
            // a jetty and a rowing boat
            var j0 = lc - D * (R + 1.5f); var jr = Quaternion.LookRotation(D); for (var k = 0; k < 8; k++) { var jp = j0 + D * (k * 1f) + up * .4f; mk.Box(wood, jp, new Vector3(1.6f, .1f, .9f), jr); solid.Box(wood, jp - up * .1f, new Vector3(1.6f, .3f, .9f), jr); if (k % 2 == 0) foreach (var e in new[] { -.8f, .8f }) mk.Prism(wood, jp + S * e - up * 1.2f, up, .08f, 1.6f, 6); }
            Remember(j0 - D * 1f, 1);
            var bt = j0 + D * 7.4f + S * 1.8f + up * .2f; var bm = new MeshKit(); bm.Box(Paint(new Color(.56f, .4f, .3f)), Vector3.zero, new Vector3(1.2f, .4f, 3f), Quaternion.identity); bm.Prism(Paint(new Color(.56f, .4f, .3f)), new Vector3(0, 0, 1.5f), Vector3.forward, .6f, .6f, 4, .1f, 45f); bm.Box(wood, new Vector3(0, .12f, 0), new Vector3(1.1f, .06f, .3f), Quaternion.identity); foreach (var e in new[] { -1f, 1f }) bm.Box(wood, new Vector3(e * .9f, .2f, .2f), new Vector3(1.2f, .04f, .1f), Quaternion.Euler(0, e * 20f, -e * 15f)); var bg = bm.BuildSingle("Rowing boat", ctx.Root, false); bg.transform.SetPositionAndRotation(bt, jr); life.boat = bg.transform; life.boatHome = bt;
            Interact.Seat(tlife.transform, bt + up * .2f, S, "Sit in the rowing boat").seatHeight = .2f;
            Folk(j0 + D * 5f - S * .3f + up * .45f, D, Resident.Pose.Sit, null, Local(), null, .45f);
            Folk(lc - D * (R + 3f) + S * 6f, D, Resident.Pose.Stand, new[] { "The tower's been off the air for years. The light still blinks.", "The water's so still you can see the morning in it twice." }, Local());
            Mark(lc, R + 2f);
            Shot(ctx, "the lake and the radio tower", lc - D * (R + 4f) + S * 8f + up * 2.2f, il + up * 4f);
        }
    }

    /// The Ambient Mists' holder for things you can use and carry.
    public class MistTownLife : MonoBehaviour { }
}
