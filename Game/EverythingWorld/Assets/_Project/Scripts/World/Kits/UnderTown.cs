using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Trip-Hop Undercity's places (part of UnderKit): the Platform (an old subway station under a vaulted concrete
    /// ceiling on columns lit blue and red, white tiles with a blue stripe, the station's name in enamel, a train standing at
    /// the platform with its windows lit, benches, a clock, a ticket machine), the Flooded Tunnel (a walkway beside a
    /// channel of black water under arched concrete ribs and hanging lamps) and the Control Room (a spy-film room of
    /// consoles, screens and a glowing map wall, with a red button).
    public partial class UnderKit
    {
        UnderTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.08f, .08f, .1f), new Color(.3f, .3f, .34f), new Color(.5f, .1f, .1f), new Color(.2f, .26f, .4f), new Color(.6f, .56f, .5f) };
        static readonly Color[] Bottoms = { new Color(.08f, .08f, .1f), new Color(.16f, .16f, .2f), new Color(.3f, .26f, .24f) };
        static readonly string[] TownTalk =
        {
            "The last train left years ago. It's still here. Don't ask.", "Follow the tunnel to the control room. Mind the water. It's deeper than it looks.", "It's always three in the morning down here.",
            "Somebody's always playing a record somewhere in these tunnels.", "I came down for one song. That was a while ago.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1994);
            tlife = new GameObject("Under town life").AddComponent<UnderTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Cap }, new[] { Red, Cold, Amber }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Platform
        void Platform(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rot = Quaternion.LookRotation(D); const float D0 = -28f, D1 = 12f, H = 7f;
            // floor: the platform in tiles, the track bed of dark ballast with two rails
            mk.Box(concreteD, c + D * ((D0 + D1) / 2f) - up * .25f, new Vector3(20f, .5f, D1 - D0), rot); mk.Box(tileM, c + D * ((D0 + D1) / 2f) + S * 2f + up * .01f, new Vector3(10f, .02f, D1 - D0), rot); mk.Box(Paint(new Color(.9f, .76f, .2f)), c + D * ((D0 + D1) / 2f) - S * 2.9f + up * .02f, new Vector3(.3f, .02f, D1 - D0), rot); mk.Box(Paint(new Color(.16f, .14f, .14f)), c + D * ((D0 + D1) / 2f) - S * 6f + up * .01f, new Vector3(5.6f, .02f, D1 - D0), rot); foreach (var e in new[] { -6.7f, -5.3f }) mk.Box(iron, c + D * ((D0 + D1) / 2f) + S * e + up * .08f, new Vector3(.1f, .14f, D1 - D0), rot); for (var k = 0; k < 40; k++) mk.Box(Paint(new Color(.3f, .24f, .2f)), c + D * (D0 + k * 1f) - S * 6f + up * .03f, new Vector3(2.4f, .04f, .3f), rot);
            for (var k = -2; k <= 2; k++) { Remember(c + S * 8.8f + D * k * 6f, 0); Remember(c + D * 11f + S * k * 2f, 0); }
            // the vault and its columns
            for (var d = D0; d <= D1 + .01f; d += 4f) { for (var k = 0; k <= 12; k++) { var a = Mathf.PI * k / 12f; var p = c + D * d + S * Mathf.Cos(a) * 9.6f + up * (4.6f + Mathf.Sin(a) * 2.6f); mk.Box(concrete, p, new Vector3(2.6f, .5f, 4.04f), rot * Quaternion.Euler(0, 0, -Mathf.Cos(a) * 60f)); } if (((int)d) % 8 == 0) foreach (var e in new[] { -1f, 1f }) Column(ctx, mk, c + D * d + S * e * 9.2f, 4.6f, e > 0); }
            for (var d = D0 + 4f; d <= D1; d += 8f) { var lp = c + D * d + S * 2f + up * 5.4f; mk.Box(lampM, lp, new Vector3(.2f, .1f, 2.4f), rot); halos.Add(lp, 2f, Amber, .3f); ArtistBits.PointLight(ctx.Root, lp - up * .4f, new Color(1f, .8f, .6f), 14f, 1.6f, true); }
            // the back wall: tiles, a blue stripe, the station's name
            var bw = new PlaceFrame(c + D * D0, D); mk.Box(tileM, bw.P(0, H / 2f, 0), new Vector3(20f, H + 2f, .4f), bw.r); solid.Box(tileM, bw.P(0, H / 2f, 0), new Vector3(20f, H + 2f, .4f), bw.r); mk.Box(stripeM, bw.P(0, 2.4f, .21f), new Vector3(20f, .4f, .02f), bw.r); mk.Box(Paint(new Color(.8f, .1f, .1f)), bw.P(2f, 3.4f, .22f), new Vector3(6f, 1f, .02f), bw.r); mk.Box(stripeM, bw.P(2f, 3.4f, .23f), new Vector3(6.4f, .3f, .02f), bw.r); ArtistBits.Label(rootT, "UNDERCITY", bw.P(2f, 3.4f, .25f), bw.Fwd, .4f, Color.white, TextAnchor.MiddleCenter);
            IndoorSpaces.Add(c + D * ((D0 + D1) / 2f) + up * 3.5f, rot, new Vector3(18f, 7f, D1 - D0));
            // the train standing at the platform
            var tc = c + D * -8f - S * 6f; var tr = rot; mk.Box(Paint(new Color(.56f, .58f, .62f)), tc + up * 1.8f, new Vector3(2.8f, 3f, 30f), tr); solid.Box(concrete, tc + up * 1.8f, new Vector3(2.8f, 3f, 30f), tr); mk.Box(Paint(new Color(.6f, .1f, .1f)), tc + up * 1.1f, new Vector3(2.82f, .3f, 30.02f), tr); for (var k = 0; k < 14; k++) { var wp = tc + up * 2.1f + D * (-13f + k * 2f) + S * 1.41f; mk.Box(k % 4 == 1 ? coldM : windowM(), wp, new Vector3(.02f, .8f, 1.4f), tr); } for (var k = 0; k < 4; k++) mk.Box(dark, tc + up * 1.6f + D * (-11f + k * 7f) + S * 1.42f, new Vector3(.02f, 2.2f, 1.2f), tr); mk.Box(Paint(new Color(.4f, .42f, .46f)), tc + up * 3.4f, new Vector3(2.4f, .2f, 30f), tr); mk.Box(redM, tc + up * 2.6f + D * 15.01f, new Vector3(1.6f, .3f, .02f), tr); halos.Add(tc + up * 2.1f + S * 2f, 6f, Amber, .15f);
            // benches, a clock, a ticket machine
            for (var k = 0; k < 3; k++) Bench(ctx.Root, mk, c + D * (-18f + k * 9f) + S * 6.4f, -S);
            var ck = c + D * 4f + S * 2f + up * 4.6f; mk.Prism(iron, ck + up * .5f, up, .02f, .9f, 4); mk.Prism(white, ck - D * .1f, D, .5f, .2f, 18); mk.Box(dark, ck + D * .11f + up * .15f, new Vector3(.04f, .34f, .01f), rot); mk.Box(dark, ck + D * .11f + S * .1f, new Vector3(.24f, .04f, .01f), rot); halos.Add(ck, 1.2f, Color.white, .3f);
            var tm = c + S * 8f - D * 3f; mk.Box(Paint(new Color(.3f, .34f, .5f)), tm + up * .9f, new Vector3(.9f, 1.8f, .6f), Quaternion.LookRotation(-S)); mk.Box(screenM, tm + up * 1.3f - S * .31f, new Vector3(.6f, .4f, .02f), Quaternion.LookRotation(-S)); BoxCol(ctx.Root, tm + up * .9f, new Vector3(.9f, 1.8f, .6f), Quaternion.LookRotation(-S));
            string[] ticket = { "The machine prints a ticket. The destination just says SOMEWHERE ELSE.", "It takes your coin and gives you back a different one. Older.", "SINGLE TO THE CONTROL ROOM. VALID UNTIL DAWN. Dawn doesn't come down here." }; var tn = 0;
            Spot("The ticket machine", tm + up * 1.3f, "Buy a ticket", new Vector3(1f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, tm + up * 2.6f, ticket[tn++ % ticket.Length], 5f); };
            Folk(c + D * 3f + S * 3f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Undercity. Last train's gone. It's still here though.", "The tunnel goes on to the control room. Spy stuff. Big red button.", "Sit and wait for a train. Nobody minds." });
            Folk(c - D * 12f + S * 6f, -S, Resident.Pose.Sit, null, Local(), null, .52f);
            Folk(c - D * 4f + S * 4f, -S, Resident.Pose.Stand, new[] { "I'm waiting for someone. I've been waiting a while.", "Hear that? Somebody's playing a record in the tunnels." }, Local());
            Mark(c + D * -8f, 22f);
            Shot(ctx, "the platform", c + D * 10f + S * 5f + up * 2.2f, c - D * 20f + up * 3f);
        }

        // ---------------------------------------------------------------- the Flooded Tunnel
        void Tunnel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 12.5f; d <= streetL + 6.01f; d += 2f) pts.Add(G(ctx, d, -1f)); if (pts.Count < 2) return;
            Strip(mk, pts, concrete, WalkW, .1f, ctx); var e1 = new List<Vector3>(); foreach (var p in pts) e1.Add(p + S * (WalkW / 2f - .2f)); Strip(mk, e1, Paint(new Color(.8f, .7f, .2f)), .15f, .12f, ctx); foreach (var p in pts) Remember(p, 1);
            var ch = new List<Vector3>(); foreach (var p in pts) ch.Add(p + S * 4.6f); Strip(mk, ch, waterM, 4.2f, .02f, ctx);
            var n = 0; for (var d = 16f; d <= streetL + 4f; d += 6f, n++)
            {
                var a0 = G(ctx, d, -4f); var a1 = G(ctx, d, 7.4f); var mid = (a0 + a1) / 2f; var w = (a1 - a0).magnitude / 2f;
                foreach (var ap in new[] { a0, a1 }) { mk.Box(concrete, ap + up * 1.6f, new Vector3(.7f, 3.2f, .9f), Quaternion.LookRotation(D)); ArtistBits.PostCollider(ctx.Root, ap, 3.2f, .5f); }
                for (var k = 0; k <= 10; k++) { var a = Mathf.PI * k / 10f; mk.Box(concrete, mid + S * Mathf.Cos(a) * w + up * (3.2f + Mathf.Sin(a) * 2.4f), new Vector3(1.6f, .4f, .9f), Quaternion.LookRotation(D) * Quaternion.Euler(0, 0, -Mathf.Cos(a) * 60f)); }
                var hl = mid + up * 4.6f - S * 2f; mk.Prism(iron, hl, up, .01f, 1f, 3); mk.Blob(n % 3 == 0 ? redM : n % 3 == 1 ? lampM : coldM, hl, Vector3.one * .14f, new System.Random(n), 0f); halos.Add(hl, 1.6f, n % 3 == 0 ? Red : n % 3 == 1 ? Amber : Cold, .4f); if (n % 2 == 0) ArtistBits.PointLight(ctx.Root, hl - up * .3f, n % 3 == 0 ? Red : n % 3 == 1 ? Amber : Cold, 12f, 1.6f);
            }
            life.drips.Add(G(ctx, streetL * .5f, 4.6f)); life.drips.Add(G(ctx, streetL * .8f, 4.6f));
            Folk(G(ctx, 22f, -1.4f), D, Resident.Pose.Walk, new[] { "Echo! ...echo. Sorry.", "The water's moving. Water shouldn't move down here." }, Local(), new[] { G(ctx, 22f, -1.4f), G(ctx, streetL, -1.4f), G(ctx, streetL, -.4f), G(ctx, 22f, -.4f) }).speed = .9f;
            Shot(ctx, "along the flooded tunnel", G(ctx, 13f, -2f) + up * 1.8f, G(ctx, streetL + 14f, 0) + up * 3f);
        }

        // ---------------------------------------------------------------- the Control Room
        void ControlRoom(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rc = G(ctx, streetL + 14f, 0); var f = new PlaceFrame(rc, -D); const float W = 14f, L = 10f, H = 4.4f;
            mk.Box(concreteD, f.P(0, -.1f, 0), new Vector3(W, .3f, L), f.r);
            mk.Box(concrete, f.P(0, H / 2f, -L / 2f), new Vector3(W, H, .4f), f.r); solid.Box(concrete, f.P(0, H / 2f, -L / 2f), new Vector3(W, H, .4f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(concrete, f.P(e * W / 2f, H / 2f, 0), new Vector3(.4f, H, L - .4f), f.r); solid.Box(concrete, f.P(e * W / 2f, H / 2f, 0), new Vector3(.4f, H, L - .4f), f.r); mk.Box(concrete, f.P(e * (W / 2f - 2.4f), H / 2f, L / 2f), new Vector3(4.4f, H, .4f), f.r); solid.Box(concrete, f.P(e * (W / 2f - 2.4f), H / 2f, L / 2f), new Vector3(4.4f, H, .4f), f.r); }
            mk.Box(concreteD, f.P(0, H + .2f, 0), new Vector3(W + .4f, .4f, L + .4f), f.r); Remember(f.P(0, 0, L / 2f + 2f), 1);
            IndoorSpaces.Add(f.P(0, H / 2f, 0), f.r, new Vector3(W - .8f, H, L - .8f));
            // the map wall
            var map = new Material(WorldMaterials.Glow(new Color(.3f, .7f, 1f), 1.1f)) { name = "Under map" }; life.map = map; mk.Box(dark, f.P(0, 2.4f, -L / 2f + .22f), new Vector3(10f, 3f, .02f), f.r); var rng = new System.Random(7); for (var k = 0; k < 60; k++) { var x = (float)rng.NextDouble() * 9f - 4.5f; var y = (float)rng.NextDouble() * 2.4f + 1.2f; if (Mathf.PerlinNoise(x * .4f + 3f, y * .6f) < .45f) continue; mk.Box(map, f.P(x, y, -L / 2f + .24f), new Vector3(.3f, .2f, .01f), f.r); }
            for (var k = 0; k < 6; k++) mk.Blob(redM, f.P(-4f + k * 1.6f, 1.6f + (k * 37 % 5) * .4f, -L / 2f + .26f), Vector3.one * .06f, rng, 0f);
            // the console desk in an arc of screens, chairs, the red button
            for (var k = 0; k < 7; k++) { var a = (k - 3f) * 14f; var dp = f.P(Mathf.Sin(a * Mathf.Deg2Rad) * 4f, 0, -1f + (1f - Mathf.Cos(a * Mathf.Deg2Rad)) * 4f); var dr = f.r * Quaternion.Euler(0, a, 0); mk.Box(Paint(new Color(.3f, .32f, .36f)), dp + up * .45f, new Vector3(1.4f, .9f, .9f), dr); mk.Box(screenM, dp + up * 1.3f - dr * Vector3.forward * .3f, new Vector3(1.2f, .7f, .04f), dr * Quaternion.Euler(-12f, 0, 0)); for (var j = 0; j < 6; j++) mk.Box(j % 3 == 0 ? redM : coldM, dp + up * .92f + dr * new Vector3(-.5f + j * .2f, 0, .2f), new Vector3(.08f, .02f, .08f), dr); BoxCol(ctx.Root, dp + up * .5f, new Vector3(1.4f, 1f, .9f), dr); if (k % 2 == 1) Interact.Seat(tlife.transform, dp + up * .5f + dr * Vector3.forward * 1f, -(dr * Vector3.forward), "Sit at the console").seatHeight = .5f; }
            var rb = f.P(0, 0, 2.6f); mk.Prism(iron, rb, up, .2f, 1f, 8); mk.Prism(redM, rb + up * 1f, up, .16f, .1f, 12); BoxCol(ctx.Root, rb + up * .5f, new Vector3(.4f, 1f, .4f), f.r);
            string[] button = { "You press it. Every screen goes red, a siren winds up somewhere deep in the tunnels, then it all goes quiet. Nothing happens. Or something did.", "The map wall lights up from one end to the other. A voice says: 'Proceed.'", "Click. A record starts playing somewhere above you." }; var bn = 0;
            Spot("The red button", rb + up * 1.1f, "Press the red button", new Vector3(.6f, .6f, .6f)).onUse = _ => { life.alarm = Time.time; life.played = Time.time; Say(tlife.transform, rb + up * 2.2f, button[bn++ % button.Length], 5f); };
            ArtistBits.PointLight(ctx.Root, f.P(0, 3.6f, 0), new Color(.5f, .7f, 1f), 14f, 1.6f, true); ArtistBits.PointLight(ctx.Root, f.P(0, 3.6f, -3f), Red, 10f, 1.2f, true);
            Folk(f.P(4f, 0, -2f), f.Fwd, Resident.Pose.Stand, new[] { "Don't press the button. Okay, everybody presses the button.", "We watch the whole Undercity from here. Mostly we watch the kettle." }, Local(Residents.Tool.Mug));
            Mark(rc, 9f);
            Shot(ctx, "the control room", f.P(0, 1.8f, L / 2f + 3f), f.P(0, 2.2f, -L / 2f));
        }
    }

    /// The Trip-Hop Undercity's holder for things you can use and carry.
    public class UnderTownLife : MonoBehaviour { }
}
