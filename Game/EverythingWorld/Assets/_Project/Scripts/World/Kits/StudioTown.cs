using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Studio Lot Island's places (part of StudioKit): the Front Gate (a grand arch with the studio's name, a water tower,
    /// a guard booth, a clapperboard to snap, a row of director's chairs, searchlights sweeping the sky); Backlot Avenue
    /// (sound stages down both sides with their numbers painted on, red ON AIR lights, big sliding doors half open); and
    /// the Tank (a pool of sea with a painted sky backdrop and a pirate ship to board, its sails up).
    public partial class StudioKit
    {
        StudioTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.2f, .2f, .22f), new Color(.86f, .84f, .8f), new Color(.6f, .2f, .2f), new Color(.3f, .4f, .6f), new Color(.9f, .8f, .5f), new Color(.5f, .5f, .5f), new Color(.7f, .5f, .7f) };
        static readonly Color[] Bottoms = { new Color(.2f, .22f, .3f), new Color(.14f, .14f, .16f), new Color(.5f, .44f, .36f), new Color(.86f, .84f, .8f) };
        static readonly string[] TownTalk =
        {
            "Quiet on set! Kidding. It's never quiet on set.", "Every stage is a different picture. Walk in, you're in the movie.", "The Tank's at the end of the Avenue. They're shooting pirates today. They're always shooting pirates.",
            "I'm an extra. I've been an extra in everything. You've seen my elbow in a hundred films.", "That's a wrap! No it isn't. It never is.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1939);
            tlife = new GameObject("Studio town life").AddComponent<StudioTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Bucket }, new[] { Gold, Red, Navy, Cream }); l.tool = tool; l.apron = apron; return l; }
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

        // ---------------------------------------------------------------- the Front Gate
        void FrontGate(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(asphalt, c - up * .28f, up, 20f, .42f, 32); mk.Prism(concrete, c + up * .14f, up, 7f, .02f, 24); for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1);
            // a star in the middle of the forecourt
            for (var k = 0; k < 5; k++) { var a0 = Quaternion.Euler(0, k * 72f, 0) * D; var a1 = Quaternion.Euler(0, k * 72f + 36f, 0) * D; var a2 = Quaternion.Euler(0, k * 72f - 36f, 0) * D; mk.Tri(GlowM(Gold, .8f), c + up * .17f + a0 * 3.6f, c + up * .17f + a1 * 1.4f, c + up * .17f + a2 * 1.4f, c - up); mk.Tri(GlowM(Gold, .8f), c + up * .17f, c + up * .17f + a1 * 1.4f, c + up * .17f + a2 * 1.4f, c - up); }
            // the grand arch over the way onto the lot
            var ga = c + D * 18f; foreach (var e in new[] { -5f, 5f }) { mk.Box(Paint(Cream), ga + S * e + up * 4f, new Vector3(1.6f, 8f, 1.6f), Quaternion.LookRotation(D)); solid.Box(Paint(Cream), ga + S * e + up * 4f, new Vector3(1.6f, 8f, 1.6f), Quaternion.LookRotation(D)); mk.Box(Paint(Gold), ga + S * e + up * 8.2f, new Vector3(2f, .4f, 2f), Quaternion.LookRotation(D)); }
            for (var k = 0; k <= 12; k++) { var a = Mathf.PI * k / 12f; mk.Box(Paint(Cream), ga + up * (8f + Mathf.Sin(a) * 2.6f) + S * Mathf.Cos(a) * 5f, new Vector3(1f, 1f, 1.4f), Quaternion.LookRotation(D) * Quaternion.Euler(0, 0, -Mathf.Cos(a) * 60f)); }
            mk.Box(Paint(Navy), ga + up * 9.2f - D * .72f, new Vector3(7f, 1.4f, .1f), Quaternion.LookRotation(D)); Sign(ga + up * 9.2f - D * .78f, -D, "STUDIO LOT", .8f, Gold); for (var k = 0; k < 14; k++) mk.Blob(lampM, ga + up * 9.2f - D * .8f + S * (-3.3f + k * .5f) + up * .62f, Vector3.one * .07f, rng, 0f);
            // the water tower with the studio's star on it
            var wt = c - D * 6f - S * 13f; const float H = 14f; foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var foot = wt + new Vector3(x, 0, zz) * 2f; mk.Prism(scaffold, foot - up * .2f, (wt + up * H + new Vector3(x, 0, zz) * 1.6f - foot).normalized, .12f, H + .3f, 6); ArtistBits.PostCollider(ctx.Root, foot, 4f, .18f); }
            mk.Prism(Paint(new Color(.86f, .86f, .84f)), wt + up * H, up, 3f, 4f, 16); mk.Cone(Paint(new Color(.86f, .86f, .84f)), wt + up * (H + 4f), 3.1f, 1.4f, 16); var tf = (c - wt); tf.y = 0; tf.Normalize(); Sign(wt + up * (H + 2f) + tf * 3.05f, tf, "★", 2f, Gold);
            // the guard booth with its barrier
            var gb = new PlaceFrame(ga + S * 7.6f - D * 1.6f, -D); mk.Box(white, gb.P(0, 1.3f, 0), new Vector3(2f, 2.6f, 2f), gb.r); solid.Box(white, gb.P(0, 1.3f, 0), new Vector3(2f, 2.6f, 2f), gb.r); mk.Box(windowM, gb.P(0, 1.6f, 1.01f), new Vector3(1.6f, 1f, .02f), gb.r); mk.Box(Paint(Red), gb.P(0, 2.7f, 0), new Vector3(2.4f, .2f, 2.4f), gb.r); mk.Box(Paint(Red), ga + S * 3f + up * 1f, new Vector3(5f, .14f, .14f), Quaternion.LookRotation(D) * Quaternion.Euler(0, 90f, 0));
            Folk(gb.P(0, 0, 1.6f), gb.Fwd, Resident.Pose.Stand, new[] { "Name? Doesn't matter. Everybody's on the list today.", "Stay off Stage 4. Something with lions. I'm not asking.", "Tour's free. Keep your hands inside the movie at all times." }, Local(Residents.Tool.Mug));
            // the clapperboard on a stand: snap it
            var cb = c + D * 6f - S * 4f + up * .14f; mk.Prism(iron, cb, up, .03f, 1.2f, 4); mk.Box(dark, cb + up * 1.4f, new Vector3(.9f, .7f, .06f), Quaternion.LookRotation(-D)); for (var k = 0; k < 5; k++) mk.Box(k % 2 == 0 ? white : dark, cb + up * 1.86f + S * (-.36f + k * .18f), new Vector3(.18f, .14f, .07f), Quaternion.LookRotation(-D) * Quaternion.Euler(0, 0, 10f)); ArtistBits.Label(rootT, "SCENE 1  TAKE 1", cb + up * 1.4f - D * .04f, -D, .08f, Cream, TextAnchor.MiddleCenter); ArtistBits.PostCollider(ctx.Root, cb, 1.2f, .05f);
            string[] snap = { "CLACK. \"Scene one, take one!\" Somebody yells \"Action!\" and every extra on the lot starts walking with purpose.", "CLACK. \"Take forty-seven.\" A sigh goes round the whole studio.", "CLACK. \"That's a wrap!\" Applause. Nobody stops working." }; var sn = 0;
            Spot("The clapperboard", cb + up * 1.5f, "Snap the clapperboard", new Vector3(1f, .8f, .6f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, cb + up * 2.8f, snap[sn++ % snap.Length], 4.5f); };
            // a row of director's chairs, searchlights at the corners, lamps round the edge
            var chairNames = new[] { "DIRECTOR", "STAR", "STAR TOO", "YOU" }; for (var k = 0; k < 4; k++) DirectorsChair(mk, c - D * 8f + S * (4f + k * .9f) + up * .14f, D, chairNames[k]);
            for (var k = 0; k < 2; k++) { var sp = c + Quaternion.Euler(0, k * 180f + 135f, 0) * D * 17f; mk.Box(Paint(Navy), sp + up * .5f, new Vector3(1.6f, 1f, 1.6f), Quaternion.identity); mk.Prism(scaffold, sp + up * 1f, up, .4f, .6f, 10); var head = new MeshKit(); head.Prism(dark, Vector3.zero, Vector3.forward, .7f, 1.4f, 12); head.Prism(lampM, Vector3.forward * 1.4f, Vector3.forward, .66f, .02f, 12); var h = head.BuildSingle("Searchlight", ctx.Root, false); h.transform.position = sp + up * 2.2f; var lt = new GameObject("Searchlight beam"); lt.transform.SetParent(h.transform, false); lt.transform.localPosition = Vector3.forward * 1.6f; var l = lt.AddComponent<Light>(); l.type = LightType.Spot; l.range = 300f; l.spotAngle = 6f; l.intensity = 8f; l.color = new Color(1f, .96f, .86f); life.searchlights.Add(h.transform); BoxCol(ctx.Root, sp + up * 1f, new Vector3(1.6f, 2f, 1.6f), Quaternion.identity); }
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * D; if (Vector3.Angle(d, D) < 30f) continue; Lamp(ctx, mk, c + d * 18f + up * .14f, k % 2 == 0); }
            Folk(c + D * 5f + S * 2f + up * .14f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the lot! Every stage is a different film. Walk in, look around.", "Backlot Avenue runs right down to the Tank. There's a pirate ship. A real fake one.", "Snap the clapperboard if you want to start something." });
            Mark(c, 21f); Mark(wt, 4f);
            Shot(ctx, "the front gate", c - D * 4f + S * 12f + up * 2.6f, ga + up * 6f);
        }

        // ---------------------------------------------------------------- Backlot Avenue
        void BacklotAvenue(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, asphalt, StreetW, .13f, ctx); foreach (var e in new[] { -1f, 1f }) { var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(p + S * e * (StreetW / 2f - .3f)); Strip(mk, edge, Paint(Gold), .12f, .15f, ctx); } foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 24f; d <= streetL - 4f; d += 12f, n++) foreach (var e in new[] { -1f, 1f }) Lamp(ctx, mk, G(ctx, d + (e > 0 ? 0f : 6f), e * (StreetW / 2f + .6f)), (n + (e > 0 ? 0 : 1)) % 2 == 0);
            // a studio cart driving slowly up and down
            var cm = new MeshKit(); cm.Box(white, new Vector3(0, .5f, 0), new Vector3(1.2f, .4f, 2.2f), Quaternion.identity); cm.Box(white, new Vector3(0, 1.6f, -.1f), new Vector3(1.2f, .06f, 2f), Quaternion.identity); foreach (var e in new[] { -.5f, .5f }) foreach (var q in new[] { -.9f, .7f }) cm.Prism(dark, new Vector3(e - Mathf.Sign(e) * .05f, .26f, q), Vector3.right * Mathf.Sign(e), .24f, .1f, 10); foreach (var e in new[] { -.55f, .55f }) foreach (var q in new[] { -.9f, .8f }) cm.Prism(iron, new Vector3(e, .7f, q), Vector3.up, .02f, .9f, 4); cm.Box(Paint(Red), new Vector3(0, .9f, .1f), new Vector3(1f, .1f, .6f), Quaternion.identity);
            var cart = cm.BuildSingle("Studio cart", ctx.Root, false); life.cart = cart.transform; life.cartA = G(ctx, 22f, 2.4f); life.cartB = G(ctx, streetL - 4f, 2.4f); life.cartSide = S;
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 15f) route.Add(G(ctx, d, -2.4f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 15f) route.Add(G(ctx, d, -3f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "I carry this clipboard so nobody asks what I do.", "Running late for Stage 3. I'm always running late for Stage 3." }, Local(Residents.Tool.None), route.ToArray()).speed = 1.3f;
            Shot(ctx, "down backlot avenue", G(ctx, 20f, -1f) + up * 2.4f, G(ctx, streetL + 30f, 0) + up * 6f);
        }

        /// The sound stages down both sides of Backlot Avenue, built after the lanes so nothing stands in one.
        void SoundStages(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0;
            foreach (var e in new[] { 1f, -1f })
            for (var d = 30f + (e > 0 ? 0f : 6f); d <= streetL - 8f; d += 22f)
            {
                var p = Wp(d, e * (StreetW / 2f + 10f)); if (!ctx.IsLand(p.x, p.z, 10f) || InFootprint(ctx, p.x, p.z, 10f) || LaneNear(p.x, p.z, 10f)) continue;
                Stage(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e), i + 1); i++; built++;
            }
            Debug.Log($"Studio Lot Island: {built} sound stages on Backlot Avenue");
        }

        /// A sound stage: a great barrel-roofed hangar, its number painted huge, a sliding door half open with light inside, ON AIR over the door.
        void Stage(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int n)
        {
            var up = Vector3.up; float W = 16f, Dp = 14f, H = 9f; var wall = Paint(Stages[n % Stages.Length]);
            mk.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); for (var k = 0; k <= 10; k++) { var a = Mathf.PI * k / 10f; mk.Box(wall, f.P(Mathf.Cos(a) * W / 2f * .98f, H + Mathf.Sin(a) * 2.4f, 0), new Vector3(W / 10f * 1.6f, .3f, Dp), f.r * Quaternion.Euler(0, 0, -Mathf.Cos(a) * 30f)); }
            mk.Box(Paint(Stages[(n + 2) % Stages.Length]), f.P(0, H * .6f, Dp / 2f + .03f), new Vector3(5f, 5f, .04f), f.r); ArtistBits.Label(rootT, n.ToString(), f.P(0, H * .6f, Dp / 2f + .06f), f.Fwd, 3.4f, Navy, TextAnchor.MiddleCenter);
            mk.Box(dark, f.P(-W * .25f, 2.4f, Dp / 2f + .02f), new Vector3(5f, 4.8f, .04f), f.r); mk.Box(windowM, f.P(-W * .25f - 1.4f, 2.4f, Dp / 2f + .04f), new Vector3(1.6f, 4.6f, .04f), f.r); mk.Box(scaffold, f.P(-W * .25f + 1f, 2.4f, Dp / 2f + .14f), new Vector3(3.4f, 4.8f, .1f), f.r); halos.Add(f.P(-W * .25f - 1.4f, 2.4f, Dp / 2f + .8f), 2.4f, new Color(1f, .8f, .5f), .26f);
            mk.Box(dark, f.P(-W * .25f, 5.3f, Dp / 2f + .1f), new Vector3(1.6f, .5f, .2f), f.r); mk.Box(onAirM, f.P(-W * .25f, 5.3f, Dp / 2f + .21f), new Vector3(1.4f, .36f, .02f), f.r); ArtistBits.Label(rootT, "ON AIR", f.P(-W * .25f, 5.3f, Dp / 2f + .23f), f.Fwd, .14f, Cream, TextAnchor.MiddleCenter);
            mk.Box(woodM, f.P(W * .3f, 1.1f, Dp / 2f + .03f), new Vector3(1f, 2.2f, .04f), f.r); mk.Box(lampM, f.P(W * .3f, 2.5f, Dp / 2f + .1f), new Vector3(.3f, .2f, .1f), f.r);
            if (n % 2 == 0) Folk(f.P(W * .3f + 1f, 0, Dp / 2f + .8f), f.Fwd, Resident.Pose.Lean, new[] { "On a break. They said ten minutes. That was yesterday.", "Inside's a whole jungle. Plastic. Very convincing.", "If the red light's on, whisper. It's always on." }, Local(Residents.Tool.Mug));
            else FilmLight(ctx, mk, f.P(W * .3f + 2f, 0, Dp / 2f + 2f), -f.Fwd + f.Right * .3f, false);
            Mark(f.o, 11f);
        }

        // ---------------------------------------------------------------- the Tank
        void TheTank(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var tc = G(ctx, streetL + 22f, 0); var rot = Quaternion.LookRotation(D); var rng = new System.Random(88);
            mk.Prism(concrete, tc - up * .28f, up, 24f, .42f, 32); for (var k = 0; k < 12; k++) Remember(tc + Quaternion.Euler(0, k * 30f, 0) * D * 22f, 1); Remember(tc - D * 10f, 1);
            // the tank: a wide pool of glowing sea behind a low wall
            mk.Prism(concrete, tc + up * .14f, up, 15f, .9f, 28); mk.Prism(seaM, tc + up * 1.06f, up, 14.2f, .02f, 28); solid.Prism(concrete, tc - up * .2f, up, 15f, 1.3f, 16); for (var k = 0; k < 28; k++) { var d = Quaternion.Euler(0, k * 12.86f, 0) * D; Remember(tc + d * 16f, 1); }
            // the painted sky backdrop, a great curved wall
            for (var k = -6; k <= 6; k++) { var d = Quaternion.Euler(0, k * 10f, 0) * D; var bp = tc + d * 17f; var bc = Color.Lerp(new Color(.4f, .6f, .9f), new Color(1f, .7f, .5f), (k + 6) / 12f); var bh = 12f + (k & 1) * .3f; mk.Box(Paint(bc), bp + up * bh / 2f, new Vector3(3.2f, bh, .3f), Quaternion.LookRotation(-d)); solid.Box(Paint(bc), bp + up * 6f, new Vector3(3.2f, 12f, .3f), Quaternion.LookRotation(-d)); mk.Box(Paint(Color.Lerp(bc, Color.white, .5f)), bp + up * (7f + (k % 3) * 1.2f) - d * .17f, new Vector3(2.4f, .9f, .02f), Quaternion.LookRotation(-d)); }
            // the pirate ship floating in the middle, sails up, a plank down to the edge
            var sp = tc + up * .9f; var shipFwd = S; var sr = Quaternion.LookRotation(shipFwd); var hull = Paint(new Color(.4f, .26f, .18f));
            mk.Box(hull, sp + up * .8f, new Vector3(4.6f, 2f, 14f), sr); solid.Box(hull, sp + up * .8f, new Vector3(4.6f, 2f, 14f), sr); mk.Box(woodM, sp + up * 1.84f, new Vector3(4.2f, .1f, 13.6f), sr); for (var k = 0; k < 2; k++) mk.Prism(hull, sp + up * .8f + shipFwd * (k == 0 ? 7f : -7f), (shipFwd * (k == 0 ? 1f : -1f) + up * .4f).normalized, 1.6f, 2.6f, 4, .2f);
            mk.Box(hull, sp + up * 2.8f - shipFwd * 5.6f, new Vector3(4.4f, 2f, 3f), sr); solid.Box(hull, sp + up * 2.8f - shipFwd * 5.6f, new Vector3(4.4f, 2f, 3f), sr); mk.Box(windowM, sp + up * 2.9f - shipFwd * 7.11f, new Vector3(3f, .6f, .02f), sr);
            foreach (var (z0, h) in new[] { (3.4f, 11f), (-1.4f, 13f) }) { var mp = sp + up * 1.8f + shipFwd * z0; mk.Prism(woodM, mp, up, .16f, h, 6); ArtistBits.PostCollider(ctx.Root, mp, h, .18f); for (var k = 0; k < 3; k++) { var sy = 2.4f + k * 3f; mk.Box(woodM, mp + up * (sy + 2.6f), new Vector3(5f - k * .8f, .12f, .12f), sr * Quaternion.Euler(0, 90f, 0)); mk.Box(canvas, mp + up * (sy + 1.3f) + shipFwd * .2f, new Vector3(4.6f - k * .8f, 2.4f, .06f), sr * Quaternion.Euler(0, 90f, 0) * Quaternion.Euler(0, 0, 0)); } }
            var flag = sp + up * 1.8f + shipFwd * -1.4f + up * 13.2f; mk.Box(dark, flag + Vector3.Cross(up, shipFwd) * .5f, new Vector3(.02f, .7f, 1f), sr * Quaternion.Euler(0, 90f, 0)); mk.Blob(white, flag + Vector3.Cross(up, shipFwd) * .5f, new Vector3(.1f, .12f, .1f), rng, 0f);
            Interact.Seat(tlife.transform, sp + up * 1.9f + shipFwd * 4.6f, shipFwd, "Take the ship's wheel").seatHeight = .1f; mk.Prism(woodM, sp + up * 2.6f + shipFwd * 5.2f, shipFwd, .5f, .08f, 10); for (var k = 0; k < 8; k++) mk.Box(woodM, sp + up * 2.6f + shipFwd * 5.24f + Quaternion.AngleAxis(k * 45f, shipFwd) * up * .44f, new Vector3(.05f, .4f, .05f), Quaternion.AngleAxis(k * 45f, shipFwd) * sr);
            var pl0 = tc - D * 15.6f + up * 1f; var pl1 = sp + up * 1.9f - D * 2.2f; var pm = (pl0 + pl1) / 2f; mk.Box(woodM, pm, new Vector3(1.2f, .12f, Vector3.Distance(pl0, pl1) + .2f), Quaternion.LookRotation((pl1 - pl0).normalized)); solid.Box(woodM, pm - up * .1f, new Vector3(1.2f, .3f, Vector3.Distance(pl0, pl1) + .2f), Quaternion.LookRotation((pl1 - pl0).normalized)); Remember(pl0, 1);
            Folk(sp + up * 1.9f - shipFwd * 3f, -shipFwd, Resident.Pose.Wave, new[] { "Arr! Sorry. Contractually obliged.", "The sea's two metres deep and the storm's a fan. Don't tell anyone.", "Climb up to the wheel. Everyone steers. Nobody goes anywhere. It's perfect." }, Local());
            FilmLight(ctx, mk, tc - D * 18f + S * 6f, D - S * .3f, true); FilmLight(ctx, mk, tc - D * 18f - S * 6f, D + S * .3f, true); DirectorsChair(mk, tc - D * 19f + up * .14f, D, "DIRECTOR");
            life.tank = tc + up * 1f; Mark(tc, 24f);
            Shot(ctx, "the tank and the pirate ship", tc - D * 22f + S * 4f + up * 3f, sp + up * 5f);
        }
    }

    /// Studio Lot Island's holder for things you can use and carry.
    public class StudioTownLife : MonoBehaviour { }
}
