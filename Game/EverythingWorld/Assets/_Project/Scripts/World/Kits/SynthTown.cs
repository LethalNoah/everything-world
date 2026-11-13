using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Synthwave Coast's places (part of SynthKit): the Neon Diner (a chrome diner with a pink sign, booths to sit
    /// in, milkshakes to carry, an arcade cabinet, chrome cars in the lot); the Coast Road (a dark road with glowing
    /// edge lines, tube lamps and neon palms, running straight at the sun); Sunset Point (a lookout on the cliff, a
    /// convertible to sit in, a neon sign, the sun sitting on the sea); and the sun itself, a giant striped disc.
    public partial class SynthKit
    {
        SynthTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(1f, .3f, .7f), new Color(.2f, .8f, 1f), new Color(.14f, .12f, .2f), new Color(.96f, .92f, 1f), new Color(.6f, .3f, .9f), new Color(1f, .6f, .2f), new Color(.3f, .3f, .4f) };
        static readonly Color[] Bottoms = { new Color(.12f, .12f, .2f), new Color(.3f, .3f, .5f), new Color(.9f, .9f, .96f), new Color(.2f, .14f, .3f) };
        static readonly string[] TownTalk =
        {
            "Night's warm. Windows down. Where are we going? Doesn't matter.", "Follow the road to the sun. It never sets, it just waits for you.", "The diner's open all night. All night is the only time there is here.",
            "Somebody's got a tape playing in every car. Listen. They're all in the same key.", "Neon looks best when you're a little bit sad. That's a fact.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1984);
            tlife = new GameObject("Synthwave town life").AddComponent<SynthTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket }, new[] { Magenta, Cyan, Chrome, SunGold }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Neon Diner
        void NeonDiner(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(asphalt, c - up * .28f, up, 20f, .42f, 32); for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            GridPad(ctx, mk, c + up * .13f, Quaternion.LookRotation(D), 22f, 2.2f); noGrass.RemoveAt(noGrass.Count - 1);
            // the diner: a long chrome box with a band of windows, a pink sign on a pole, a door
            var f = new PlaceFrame(c - D * 11f - S * 3f + up * .14f, D); const float W = 14f, Dp = 6f, H = 3.4f;
            void Wall(Vector3 at, Vector3 size) { mk.Box(chrome, at, size, f.r); solid.Box(chrome, at, size, f.r); }
            Wall(f.P(0, H / 2f, -Dp / 2f + .15f), new Vector3(W, H, .3f)); foreach (var e in new[] { -1f, 1f }) Wall(f.P(e * (W / 2f - .15f), H / 2f, 0), new Vector3(.3f, H, Dp - .6f)); Wall(f.P(-1.6f, .5f, Dp / 2f - .15f), new Vector3(W - 3.2f, 1f, .3f)); Wall(f.P(-1.6f, H - .4f, Dp / 2f - .15f), new Vector3(W - 3.2f, .8f, .3f)); Wall(f.P(W / 2f - .4f, H / 2f, Dp / 2f - .15f), new Vector3(.8f, H, .3f)); Wall(f.P(W / 2f - 1.8f, H - .4f, Dp / 2f - .15f), new Vector3(2f, .8f, .3f));
            mk.Box(windowM, f.P(-1.6f, 1.8f, Dp / 2f - .15f), new Vector3(W - 3.2f, 1.6f, .1f), f.r); mk.Box(pinkN, f.P(0, H - .02f, Dp / 2f + .02f), new Vector3(W, .08f, .04f), f.r); mk.Box(cyanN, f.P(0, .98f, Dp / 2f + .02f), new Vector3(W, .06f, .04f), f.r);
            mk.Box(chrome, f.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, Dp + .4f), f.r); solid.Box(chrome, f.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, Dp + .4f), f.r); mk.Box(asphalt, f.P(0, .02f, 0), new Vector3(W - .6f, .04f, Dp - .6f), f.r);
            IndoorSpaces.Add(f.P(0, H / 2f, 0), f.r, new Vector3(W, H, Dp));
            var sp = f.P(-W / 2f - 1.6f, 0, Dp / 2f + 1f); mk.Prism(chrome, sp - up * .2f, up, .14f, 7f, 8); ArtistBits.PostCollider(ctx.Root, sp, 6.6f, .16f); mk.Box(dark, sp + up * 7.4f, new Vector3(.3f, 2f, 5.6f), f.r * Quaternion.Euler(0, 90f, 0)); foreach (var e in new[] { -1f, 1f }) Sign(sp + up * 7.5f + f.Right * e * .17f, f.Right * e, "NEON DINER", .5f, Magenta); mk.Box(pinkN, sp + up * 6.4f, new Vector3(.32f, .06f, 5.6f), f.r * Quaternion.Euler(0, 90f, 0)); mk.Box(cyanN, sp + up * 8.4f, new Vector3(.32f, .06f, 5.6f), f.r * Quaternion.Euler(0, 90f, 0)); halos.Add(sp + up * 7.4f, 3.4f, Magenta, .3f);
            // inside: a counter, stools, booths along the window, a milkshake machine, a jukebox
            mk.Box(chrome, f.P(0, .55f, -1.2f), new Vector3(8f, 1.1f, .6f), f.r); mk.Box(pinkN, f.P(0, 1.12f, -1.2f), new Vector3(8.1f, .04f, .7f), f.r); Col(f.P(0, .55f, -1.2f), new Vector3(8f, 1.1f, .6f), f.r);
            for (var k = 0; k < 6; k++) { var stp = f.P(-3.4f + k * 1.36f, 0, -.4f); mk.Prism(chrome, stp, up, .03f, .72f, 4); mk.Prism(Paint(Magenta), stp + up * .72f, up, .2f, .08f, 10); Interact.Seat(tlife.transform, stp + up * .8f, -f.Fwd, "Sit at the counter").seatHeight = .76f; }
            for (var k = 0; k < 3; k++) { var bp = f.P(-5f + k * 3f, 0, 1.8f); mk.Box(chrome, bp + up * .74f, new Vector3(1.2f, .05f, .7f), f.r); mk.Prism(chrome, bp, up, .05f, .74f, 4); foreach (var e in new[] { -1f, 1f }) { mk.Box(Paint(new Color(.6f, .1f, .3f)), bp + up * .3f + f.Right * e * 1f, new Vector3(.5f, .5f, 1f), f.r); mk.Box(Paint(new Color(.6f, .1f, .3f)), bp + up * .8f + f.Right * e * 1.24f, new Vector3(.12f, .8f, 1f), f.r); Interact.Seat(tlife.transform, bp + up * .6f + f.Right * e * 1f, -f.Right * e, "Sit in the booth").seatHeight = .5f; } Col(bp + up * .4f, new Vector3(2.6f, .8f, 1f), f.r); }
            for (var k = 0; k < 3; k++) { var sm = new MeshKit(); sm.Prism(WorldMaterials.Solid(new Color(.86f, .9f, .96f), .9f), Vector3.zero, Vector3.up, .06f, .22f, 8, 1.4f); sm.Blob(Paint(new[] { new Color(1f, .7f, .8f), new Color(.6f, .4f, .3f), new Color(1f, .96f, .9f) }[k]), new Vector3(0, .24f, 0), new Vector3(.08f, .06f, .08f), rng, .1f); sm.Prism(pinkN, new Vector3(.02f, .24f, 0), (Vector3.up + Vector3.right * .2f).normalized, .008f, .18f, 3); Thing(sm, "Milkshake", new[] { "a strawberry milkshake", "a chocolate milkshake", "a vanilla milkshake" }[k], f.P(-2f + k * .8f, 1.14f, -1.2f), Quaternion.identity, new Vector3(.14f, .34f, .14f), .2f, false); }
            Folk(f.P(.6f, 0, -2f), f.Fwd, Resident.Pose.Stand, new[] { "What'll it be, hon? Shakes are on the counter. Fries are in the future.", "We've been open since 1986. It's still 1986.", "Sit in a booth, look out the window. That's the special." }, Local(Residents.Tool.None, true));
            var cab = f.P(W / 2f - 1f, 0, -1.8f); mk.Box(dark, cab + up * .9f, new Vector3(.8f, 1.8f, .8f), f.r); mk.Box(cyanN, cab + up * 1.3f + f.Fwd * .41f, new Vector3(.6f, .5f, .02f), f.r); mk.Box(pinkN, cab + up * 1.8f + f.Fwd * .41f, new Vector3(.7f, .16f, .02f), f.r); Col(cab + up * .9f, new Vector3(.8f, 1.8f, .8f), f.r);
            string[] play = { "INSERT COIN. You do. A car, a road, a sunset, 3:00 left on the clock. Go.", "You take the bend too fast and the cabinet makes a noise like it believes in you.", "New high score. You enter three letters. They spell nothing. They're perfect." }; var pn = 0;
            Spot("The arcade cabinet", cab + up * 1.2f + f.Fwd * .7f, "Play the arcade game", new Vector3(.8f, 1f, .6f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, cab + up * 2.4f + f.Fwd * .6f, play[pn++ % play.Length], 4.5f); };
            ArtistBits.PointLight(ctx.Root, f.P(0, 2.6f, 0), new Color(1f, .7f, .9f), 12f, 2.4f, true); ArtistBits.PointLight(ctx.Root, f.P(0, 2f, Dp / 2f + 3f), new Color(1f, .5f, .8f), 16f, 2.4f);
            // the lot: chrome cars nose-in under the window, palms and lamps round the edge
            var cols = new[] { Magenta, Cyan, Chrome, SunGold, Violet };
            for (var k = 0; k < 4; k++) SportsCar(ctx.Root, mk, f.P(-5.4f + k * 3f, 0, Dp / 2f + 4.6f) + up * .02f, f.r * Quaternion.Euler(0, 180f, 0), cols[k], k % 2 == 0);
            var rngP = new System.Random(4); for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * D; if (Vector3.Angle(d, D) < 30f) continue; if (k % 2 == 0) NeonPalm(ctx, mk, c + d * 18.6f, 1.2f, rngP, k); else TubeLamp(ctx, mk, c + d * 18.6f + up * .14f, -d, true); }
            Folk(c + D * 5f + S * 2f + up * .14f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Hey! Nice night for it. Every night's a nice night for it.", "The road goes straight to the sun. Can't miss it. Can't miss the sun.", "Grab a shake, sit in a car, put a tape in. That's the whole night." });
            Mark(c, 21f); Mark(f.o, 9f);
            Shot(ctx, "the neon diner", c + D * 14f + S * 9f + up * 2.4f, f.P(0, 2f, 0));
            Shot(ctx, "inside the diner", f.P(4f, 1.6f, 1.6f), f.P(-4f, 1.2f, -1.6f));
        }

        // ---------------------------------------------------------------- the Coast Road
        void CoastRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, asphalt, RoadW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            foreach (var e in new[] { -1f, 1f }) { var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(p + S * e * (RoadW / 2f - .3f)); Strip(mk, edge, pinkN, .14f, .15f, ctx); }
            for (var i = 0; i + 1 < pts.Count; i += 3) Strip(mk, new List<Vector3> { pts[i], pts[i + 1] }, cyanN, .2f, .15f, ctx);
            var rng = new System.Random(6); var n = 0;
            for (var d = 26f; d <= streetL - 4f; d += 12f, n++) foreach (var e in new[] { -1f, 1f }) { var g = G(ctx, d + (e > 0 ? 0f : 6f), e * (RoadW / 2f + 1.6f)); if (n % 2 == 0) NeonPalm(ctx, mk, g, 1.3f, rng, n); else TubeLamp(ctx, mk, g, -S * e, e > 0); }
            // a car cruising up and down the road, slowly
            var tmp = new GameObject("tmp").transform; var cm = new MeshKit(); SportsCar(tmp, cm, Vector3.zero, Quaternion.identity, Magenta, false); Object.Destroy(tmp.gameObject); var car = cm.BuildSingle("Cruising car", ctx.Root, false);
            life.car = car.transform; life.roadA = G(ctx, 22f, 2.2f); life.roadB = G(ctx, streetL - 4f, 2.2f); life.roadSide = S;
            Shot(ctx, "down the coast road", G(ctx, 21f, -1.4f) + up * 2f, G(ctx, streetL + 300f, 0) + up * 40f);
        }

        // ---------------------------------------------------------------- Sunset Point
        void SunsetPoint(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var sc = G(ctx, streetL + 16f, 0); var rot = Quaternion.LookRotation(D); var rng = new System.Random(88);
            mk.Prism(asphalt, sc - up * .28f, up, 15f, .42f, 28); for (var k = 0; k < 12; k++) Remember(sc + Quaternion.Euler(0, k * 30f, 0) * D * 14f, 1); Remember(sc - D * 6f, 1);
            GridPad(ctx, mk, sc + up * .13f, rot, 16f, 2f); noGrass.RemoveAt(noGrass.Count - 1);
            // a chrome rail along the seaward edge, a neon sign, a convertible facing the sun
            for (var k = -6; k <= 6; k++) { var rp = sc + D * 13f + S * k * 1.2f; mk.Prism(chrome, rp, up, .04f, 1.1f, 4); ArtistBits.PostCollider(ctx.Root, rp, 1.1f, .06f); } mk.Box(chrome, sc + D * 13f + up * 1.1f, new Vector3(15f, .06f, .06f), Quaternion.LookRotation(S)); mk.Box(pinkN, sc + D * 13f + up * 1.04f, new Vector3(15f, .03f, .03f), Quaternion.LookRotation(S)); Col(sc + D * 13f + up * .6f, new Vector3(15f, 1.2f, .3f), Quaternion.LookRotation(S));
            var sg = sc - S * 9f + D * 4f; foreach (var e in new[] { -2f, 2f }) { mk.Prism(chrome, sg + D * e - up * .2f, up, .1f, 5f, 6); ArtistBits.PostCollider(ctx.Root, sg + D * e, 4.8f, .12f); } mk.Box(dark, sg + up * 5.6f, new Vector3(.3f, 2.4f, 6f), rot); Sign(sg + up * 5.8f + S * .17f, S, "SUNSET POINT", .6f, SunGold); mk.Box(pinkN, sg + up * 4.6f + S * .17f, new Vector3(.02f, .06f, 5.6f), rot); halos.Add(sg + up * 5.6f, 3f, SunGold, .3f);
            var cv = sc + D * 6f + S * 2.6f + up * .14f; SportsCar(ctx.Root, mk, cv, rot, Cyan, true);
            var cv2 = sc + D * 6f - S * 2.6f + up * .14f; SportsCar(ctx.Root, mk, cv2, rot * Quaternion.Euler(0, -8f, 0), Chrome, true);
            Folk(sc + D * 10f + S * .4f + up * .14f, D, Resident.Pose.Lean, new[] { "Shh. Just look at it. It doesn't set. It just sits there for us.", "I drive out here every night and the tape always ends right as I park.", "Feels like the end of a movie. The good kind." }, Local());
            Folk(sc + D * 9f - S * 1.4f + up * .14f, D, Resident.Pose.Stand, new[] { "You can hear the synths from the sea. Swear to god.", "One more song, then we drive back. We always say that." }, Local());
            ArtistBits.PointLight(ctx.Root, sc + up * 4f, new Color(1f, .6f, .6f), 20f, 2.2f); Mark(sc, 15f);
            Shot(ctx, "sunset point", sc - D * 2f + up * 1.8f, sc + D * 400f + up * 40f);
        }

        // ---------------------------------------------------------------- the sun
        /// A giant striped sun sitting on the horizon at the end of the road, out over the sea.
        void TheSun(LandContext ctx)
        {
            var up = Vector3.up; var at = center + D * (streetL + 520f); at.y = waterY + 24f; var face = -D; var side = S; const float R = 90f; var mk = new MeshKit();
            var bands = 26; for (var k = 0; k < bands; k++)
            {
                var y0 = -R + k * 2f * R / bands; var y1 = y0 + 2f * R / bands; var gapFrac = k < bands / 2 ? Mathf.Lerp(.5f, 0f, (float)k / (bands / 2)) : 0f; y0 += (y1 - y0) * gapFrac;
                if (y1 < -R * .1f && k % 2 == 1) continue;
                float W(float y) => Mathf.Sqrt(Mathf.Max(0f, R * R - y * y));
                var col = Color.Lerp(new Color(1f, .3f, .6f), SunGold, (float)k / bands); var m = new Material(GlowM(col, 1.8f)) { name = "Synthwave sun band" }; life.sunBands.Add((m, col * 1.17f));
                mk.Quad(m, at + up * y0 - side * W(y0), at + up * y0 + side * W(y0), at + up * y1 + side * W(y1), at + up * y1 - side * W(y1), at - face);
            }
            mk.Build("The striped sun", ctx.Root, false); life.sunAt = at;
        }
    }

    /// The Synthwave Coast's holder for things you can use and carry.
    public class SynthTownLife : MonoBehaviour { }
}
