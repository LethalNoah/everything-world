using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Boom Bap Harbour's places (part of BoomKit): the top of the Block (a mural wall, a boombox on a crate to play, a
    /// hot-dog cart); the Block itself (asphalt, sidewalks, brownstones with stoops to sit on, street trees); the El
    /// (rusted green iron over the cross street, a silver train going back and forth); the block party under it (a DJ
    /// on a flatbed with turntables to scratch, speaker stacks, a hydrant spraying, bunting, an ice-cream truck,
    /// cardboard for breaking); and the harbour (a scrap of beach, a pier, shipping containers, a crane).
    public partial class BoomKit
    {
        BoomTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.95f, .95f, .92f), new Color(.85f, .2f, .16f), new Color(.2f, .3f, .7f), new Color(1f, .78f, .25f), new Color(.18f, .58f, .3f), new Color(.12f, .12f, .14f), new Color(.95f, .5f, .15f), new Color(.6f, .3f, .7f) };
        static readonly Color[] Bottoms = { new Color(.2f, .28f, .46f), new Color(.12f, .12f, .14f), new Color(.5f, .46f, .38f), new Color(.34f, .4f, .3f) };
        static readonly string[] TownTalk =
        {
            "Hot enough for you? Hydrant's open under the El. Go stand in it.", "Party's been going since noon. Noon which day, I couldn't tell you.", "Every stoop on this block has a story and somebody on it who'll tell you the wrong version.",
            "Train comes over every minute. You stop hearing it. Then you can't sleep anywhere else.", "Harbour's at the bottom. Ferry's the orange one. It goes to Shaolin.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1994);
            tlife = new GameObject("Boom Bap town life").AddComponent<BoomTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cap, Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Cap, Residents.Hat.None }, Tops); l.tool = tool; l.apron = apron; return l; }
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

        /// A milk crate to sit on.
        void Crate(MeshKit mk, Vector3 g, Vector3 face, int i, bool seat = true)
        {
            var rot = Quaternion.LookRotation(face); mk.Box(Paint(Tags[i % Tags.Length] * .8f), g + Vector3.up * .2f, new Vector3(.46f, .4f, .46f), rot); mk.Box(dark, g + Vector3.up * .2f, new Vector3(.48f, .05f, .48f), rot); Col(g + Vector3.up * .2f, new Vector3(.46f, .4f, .46f), rot);
            if (seat) Interact.Seat(tlife.transform, g + Vector3.up * .42f, face).seatHeight = .4f;
        }

        /// Two turntables and a mixer on a table. Returns the middle of the table top.
        Vector3 Decks(Transform parent, MeshKit mk, Vector3 g, Quaternion rot, Material lit = null)
        {
            var up = Vector3.up; var r = rot * Vector3.right; mk.Box(dark, g + up * .5f, new Vector3(2.2f, 1f, .7f), rot); BoxCol(parent, g + up * .5f, new Vector3(2.2f, 1f, .7f), rot);
            foreach (var e in new[] { -.7f, .7f }) { mk.Box(chain, g + up * 1.04f + r * e, new Vector3(.6f, .08f, .5f), rot); mk.Prism(dark, g + up * 1.09f + r * e, up, .22f, .012f, 16); mk.Prism(Paint(Tags[e < 0 ? 0 : 2]), g + up * 1.1f + r * e, up, .07f, .01f, 10); mk.Box(chain, g + up * 1.12f + r * (e + .22f) - rot * Vector3.forward * .1f, new Vector3(.02f, .02f, .3f), rot * Quaternion.Euler(0, 20f, 0)); }
            mk.Box(iron, g + up * 1.05f, new Vector3(.4f, .1f, .5f), rot); for (var k = 0; k < 3; k++) mk.Blob(lit ?? goldM, g + up * 1.11f + r * (-.12f + k * .12f), Vector3.one * .02f, prng, 0f);
            return g + up * 1.1f;
        }

        // ---------------------------------------------------------------- the top of the Block
        void TopOfTheBlock(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(asphalt, c - up * .24f, up, 19f, .4f, 28); mk.Prism(walk, c - up * .3f, up, 21f, .4f, 28); for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * D; mk.Box(Paint(Yellow), c + d * 12f + up * .165f, new Vector3(.2f, .01f, 2.4f), Quaternion.LookRotation(Vector3.Cross(up, d))); }
            // the mural wall behind you: the island's name, big
            var wl = c - D * 16.5f + up * .12f; var wr = Quaternion.LookRotation(D); mk.Box(Paint(Browns[1]), wl + up * 2.4f, new Vector3(16f, 4.8f, .5f), wr); solid.Box(concrete, wl + up * 2.4f, new Vector3(16f, 4.8f, .5f), wr); mk.Box(concrete, wl + up * 4.86f, new Vector3(16.4f, .2f, .7f), wr);
            Graffiti(mk, wl + up * 2.6f + D * .26f - S * 6f, D, 3.2f, 2.6f, 2); Graffiti(mk, wl + up * 2.6f + D * .26f + S * 6.1f, D, 3f, 2.4f, 5); Sign(wl + up * 3.1f + D * .3f, D, "BOOM BAP\nHARBOUR", .8f, Cream); Sign(wl + up * 1.2f + D * .3f, D, "the block is hot", .26f, Gold);
            // a boombox on a crate: press play
            var bx = c - D * 6f + S * 9f + up * .12f; Crate(mk, bx, D, 3, false); var bm = new MeshKit(); Boombox(bm, new Color(.75f, .76f, .78f), goldM); var bgo = bm.BuildSingle("The block's boombox", tlife.transform, false); bgo.transform.SetPositionAndRotation(bx + up * .42f, Quaternion.LookRotation((D - S * .4f).normalized));
            string[] play = { "Clunk. Hiss. Then a kick, a snare, a kick-kick snare. Every head on the block nods once.", "The tape's been dubbed so many times the hi-hats sound like rain. Perfect.", "Boom. Bap. Boom-boom, bap. A window opens three floors up to hear it better.", "You press play and a kid across the street starts rhyming over it without looking up." }; var pn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Press play", new Vector3(.9f, .7f, .5f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, bx + up * 2f, play[pn++ % play.Length], 5f); };
            Crate(mk, bx + S * 1.1f - D * .3f, D, 1); Crate(mk, bx - S * 1.2f + D * .2f, (D + S * .3f).normalized, 4);
            Folk(bx + S * 1.1f - D * .3f + up * .4f, D, Resident.Pose.Sit, new[] { "Top of the block. Best seat there is: you see everybody coming.", "Go down under the El, they shut the street. Then keep going till your feet are wet.", "That boombox is older than you. Treat it nice." }, null, null, .4f);
            // a hot-dog cart under an umbrella
            var hc = c + D * 4f - S * 12f + up * .12f; var hr = Quaternion.LookRotation((S + D * .2f).normalized); mk.Box(chain, hc + up * .7f, new Vector3(1.5f, .8f, .8f), hr); foreach (var e in new[] { -1f, 1f }) mk.Prism(dark, hc + up * .3f + hr * new Vector3(e * .8f, 0, 0), hr * Vector3.right * e, .3f, .06f, 12); mk.Prism(chain, hc + hr * new Vector3(0, 1.1f, 0), up, .025f, 1.3f, 4); mk.Cone(Paint(Yellow), hc + up * 2.2f, 1.1f, .4f, 8); mk.Cone(Paint(Sky), hc + up * 2.36f, .6f, .24f, 8, 22.5f); Col(hc + up * .6f, new Vector3(1.6f, 1.2f, .9f), hr);
            for (var k = 0; k < 3; k++) { var hm = new MeshKit(); hm.Box(Paint(new Color(.9f, .78f, .5f)), new Vector3(0, .03f, 0), new Vector3(.07f, .05f, .2f), Quaternion.identity); hm.Prism(Paint(new Color(.7f, .3f, .2f)), new Vector3(0, .06f, -.11f), Vector3.forward, .022f, .22f, 6); hm.Box(Paint(Yellow), new Vector3(0, .085f, 0), new Vector3(.012f, .008f, .18f), Quaternion.identity); Thing(hm, "Hot dog", "a hot dog with mustard", hc + up * 1.12f + hr * new Vector3(-.4f + k * .4f, 0, .1f), hr, new Vector3(.1f, .1f, .24f), .1f, false); }
            Folk(hc + hr * new Vector3(.2f, -.0f, -.9f), hr * Vector3.forward, Resident.Pose.Stand, new[] { "Dirty water dog! Best on the island. Only on the island.", "Mustard's free. Opinions are free. Dog's a dollar.", "I've fed every rapper on this block. Some of them twice a day." }, Local(Residents.Tool.Spatula, true));
            foreach (var (d, s) in new[] { (6f, 15f), (-10f, -13f), (12f, -14f) }) StreetLight(ctx, mk, c + D * d + S * s + up * .12f, -(D * d + S * s));
            Mark(c, 22f);
            Shot(ctx, "the top of the block", c + D * 12f + S * 8f + up * 2.6f, wl + up * 2.6f);
        }

        // ---------------------------------------------------------------- the Block
        void TheBlock(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 18f; d <= roadL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, asphalt, StreetW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            foreach (var e in new[] { -1f, 1f }) { var sw = new List<Vector3>(); for (var d = 18f; d <= roadL + .01f; d += 2f) sw.Add(G(ctx, d, e * (StreetW / 2f + 1.3f))); Strip(mk, sw, walk, 2.6f, .2f, ctx); }
            // a broken yellow line down the middle
            for (var d = 22f; d <= roadL - 4f; d += 6f) { if (Mathf.Abs(d - ElOff) < 12f) continue; var a = new List<Vector3> { G(ctx, d, 0), G(ctx, d + 3f, 0) }; Strip(mk, a, Paint(Yellow), .18f, .145f, ctx); }
            // street lights and trees along the sidewalks
            var rng = new System.Random(8); var n = 0; for (var d = 30f; d <= roadL - 8f; d += 17f, n++) { if (Mathf.Abs(d - ElOff) < 14f) continue; var e = n % 2 == 0 ? 1f : -1f; var g = G(ctx, d, e * (StreetW / 2f + .6f)); if (!InFootprint(ctx, g.x, g.z, .5f)) StreetLight(ctx, mk, g, -S * e); var t = G(ctx, d + 8f, -e * (StreetW / 2f + 1.6f)); if (!InFootprint(ctx, t.x, t.z, 1f) && Mathf.Abs(d + 8f - ElOff) > 12f) StreetTree(ctx, mk, t, 1.1f, rng); }
            // people out on the Block
            var route = new List<Vector3>(); for (var d = 24f; d <= roadL - 8f; d += 15f) route.Add(G(ctx, d, StreetW / 2f + 1.3f) + up * .2f); for (var d = roadL - 8f; d >= 24f; d -= 15f) route.Add(G(ctx, d, -(StreetW / 2f + 1.3f)) + up * .2f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Down one side, up the other. I know every crack in this sidewalk by name.", "You hear that? Three different radios, same station. That's a hit." }, null, route.ToArray()).speed = 1.2f;
            Shot(ctx, "down the block", G(ctx, 22f, 0) + up * 2.4f, G(ctx, Mathf.Min(roadL, 120f), 0) + up * 4f);
        }

        // ---------------------------------------------------------------- the brownstones
        void Brownstones(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var off = StreetW / 2f + 2.7f + 1.6f + HouseD / 2f;
            foreach (var e in new[] { 1f, -1f })
            for (var d = 26f; d <= roadL - 34f; d += HouseW + .05f, i++)
            {
                if (Mathf.Abs(d - ElOff) < 13f) continue;
                var p = Wp(d, e * off); if (!ctx.IsLand(p.x, p.z, 8f) || InFootprint(ctx, p.x, p.z, 6.2f) || LaneNear(p.x, p.z, 6.4f)) continue;
                Brownstone(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e), i); built++;
            }
            Debug.Log($"Boom Bap Harbour: {built} brownstones");
        }

        /// A brownstone: three floors over a basement, a stoop up to the door (sit on it), tall windows with stone
        /// lintels, a cornice, sometimes a fire escape, an air conditioner, somebody on the steps.
        void Brownstone(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i)
        {
            var up = Vector3.up; const float W = HouseW, Dp = HouseD, H = 11.4f; var wall = Paint(Browns[i % Browns.Length]); var trim = Paint(Browns[(i + 2) % Browns.Length] * 1.15f); var fz = Dp / 2f; var rng = new System.Random(i * 19 + 4);
            mk.Box(wall, f.P(0, (H - 3f) / 2f, 0), new Vector3(W - .1f, H + 3f, Dp), f.r); solid.Box(wall, f.P(0, (H - 3f) / 2f, 0), new Vector3(W, H + 3f, Dp), f.r); mk.Box(trim, f.P(0, H + .2f, fz + .1f), new Vector3(W, .5f, .5f), f.r); mk.Box(dark, f.P(0, H - .2f, fz + .06f), new Vector3(W - .2f, .16f, .2f), f.r);
            // the stoop: six steps up to the door, walls either side
            var dx = (i % 2 == 0 ? -1f : 1f) * 2.1f; const int steps = 6; for (var k = 0; k < steps; k++) { var sy = (k + 1) * .26f; var sz = fz + 2.6f - k * .42f; mk.Box(trim, f.P(dx, sy - .13f, sz), new Vector3(1.9f, .26f, .44f), f.r); solid.Box(trim, f.P(dx, sy / 2f, sz), new Vector3(1.9f, sy, .44f), f.r); }
            mk.Box(trim, f.P(dx, .78f, fz + .26f), new Vector3(1.9f, 1.56f, .56f), f.r); solid.Box(trim, f.P(dx, .78f, fz + .26f), new Vector3(1.9f, 1.56f, .56f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(wall, f.P(dx + e * 1.08f, .9f, fz + 1.4f), new Vector3(.26f, 1.9f, 2.9f), f.r); solid.Box(wall, f.P(dx + e * 1.08f, .9f, fz + 1.4f), new Vector3(.26f, 1.9f, 2.9f), f.r); mk.Box(trim, f.P(dx + e * 1.08f, 1.9f, fz + 1.4f), new Vector3(.36f, .12f, 3f), f.r); }
            mk.Box(dark, f.P(dx, 2.7f, fz + .03f), new Vector3(1.2f, 2.3f, .08f), f.r); mk.Box(trim, f.P(dx, 4f, fz + .06f), new Vector3(1.7f, .3f, .2f), f.r); mk.Box(windowM, f.P(dx, 3.6f, fz + .05f), new Vector3(.9f, .4f, .06f), f.r);
            Interact.Bench(tlife.transform, f.P(dx - .5f, .84f, fz + 1.86f), f.P(dx + .5f, .84f, fz + 1.86f), f.Fwd);
            // windows: basement, parlour, and two floors above
            mk.Box(glassM, f.P(-dx, .7f, fz + .03f), new Vector3(1.4f, .8f, .06f), f.r); for (var k = 0; k < 4; k++) mk.Prism(iron, f.P(-dx - .6f + k * .4f, .3f, fz + .08f), up, .02f, .8f, 3);
            for (var s = 0; s < 3; s++) for (var k = 0; k < (s == 0 ? 2 : 3); k++)
            {
                var wx = s == 0 ? -dx + (k - .5f) * 1.8f : -2.5f + k * 2.5f; if (s == 0 && Mathf.Abs(wx - dx) < 1.5f) continue; var wy = 2.9f + s * 3.1f; var wp = f.P(wx, wy, fz + .03f);
                mk.Box((i * 3 + s * 5 + k * 7) % 4 == 0 ? (Material)windowM : glassM, wp, new Vector3(1.1f, 2f, .06f), f.r); mk.Box(trim, wp + up * 1.12f + f.Fwd * .04f, new Vector3(1.4f, .22f, .16f), f.r); mk.Box(trim, wp - up * 1.06f + f.Fwd * .06f, new Vector3(1.3f, .12f, .22f), f.r); mk.Box(wall, wp + f.Fwd * .04f, new Vector3(1.1f, .06f, .04f), f.r);
                if ((i + s * 2 + k) % 5 == 0) mk.Box(chain, wp - up * .7f + f.Fwd * .26f, new Vector3(.8f, .5f, .5f), f.r);
            }
            if (i % 3 == 1) { for (var s = 1; s < 3; s++) { var y = 1.8f + s * 3.1f; mk.Box(iron, f.P(-.6f, y, fz + .6f), new Vector3(W - 3.4f, .05f, 1.1f), f.r); mk.Box(iron, f.P(-.6f, y + 1f, fz + 1.14f), new Vector3(W - 3.4f, .05f, .05f), f.r); for (var q = 0; q < 6; q++) mk.Prism(iron, f.P(-.6f - (W - 3.4f) / 2f + q * (W - 3.4f) / 5f, y, fz + 1.14f), up, .02f, 1f, 3); if (s > 1) mk.Box(iron, f.P(-1.6f, y - 1.5f, fz + .7f), new Vector3(.5f, 3.3f, .05f), f.r * Quaternion.Euler(0, 0, 18f)); } }
            if (i % 4 == 0) Folk(f.P(dx - .5f, 1.24f, fz + 1.86f), f.Fwd, Resident.Pose.Sit, new[] { "My stoop. My block. You're welcome on both.", "I've been sitting here since the first record came out. Still good.", "Kids don't know. This used to be all fields. No it didn't. It used to be exactly this.", "You want to know who's nice? Listen who the block gets quiet for." }, null, null, .4f);
            if (i % 6 == 3) { var tr = f.P(-dx - .2f, .0f, fz + 1.2f); mk.Prism(chain, tr, up, .3f, .9f, 10); mk.Prism(chain, tr + up * .9f, up, .32f, .06f, 10); ArtistBits.PostCollider(ctx.Root, tr, .9f, .32f); }
            Mark(f.o, 6.4f); Mark(f.P(dx, 0, fz + 1.6f), 2f);
        }

        // ---------------------------------------------------------------- the El
        void TheEl(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var baseG = G(ctx, ElOff, 0); var deckY = baseG.y + ElH; var along = Quaternion.LookRotation(S);
            Vector3 E(float s, float across, float y) { var p = Wp(ElOff + across, s); p.y = deckY + y; return p; }
            // the cross street under it
            foreach (var e in new[] { -1f, 1f }) { var cs = new List<Vector3>(); for (var s = StreetW / 2f; s <= elL + .01f; s += 2f) cs.Add(G(ctx, ElOff, e * s)); if (cs.Count > 1) { Strip(mk, cs, asphalt, StreetW, .13f, ctx); foreach (var p in cs) Remember(p, 2); } }
            // the deck: two long girders, cross ties, two tracks of rails, a walkway rail
            foreach (var a in new[] { -2.6f, 2.6f }) { mk.Box(steelEl, E(0, a, -.5f), new Vector3(.5f, 1f, elL * 2f), along); mk.Box(steelEl, E(0, a, .7f), new Vector3(.08f, .08f, elL * 2f), along); for (var s = -elL; s <= elL + .01f; s += 3f) mk.Prism(steelEl, E(s, a, 0), up, .03f, .7f, 4); }
            for (var s = -elL; s <= elL + .01f; s += 1.5f) mk.Box(woodM, E(s, 0, .06f), new Vector3(.3f, .14f, 5f), Quaternion.LookRotation(D));
            foreach (var a in new[] { -1.5f, -.7f, .7f, 1.5f }) mk.Box(chain, E(0, a, .2f), new Vector3(.1f, .14f, elL * 2f), along);
            // legs: iron bents either side of the cross street every 12 m, braced
            for (var s = -elL + 2f; s <= elL - 1.9f; s += 12f)
            {
                if (Mathf.Abs(s) < StreetW / 2f + 1f) continue;
                foreach (var a in new[] { -4.6f, 4.6f }) { var g = G(ctx, ElOff + a, s); var top = E(s, a * .56f, -1f); var dir = (top - g).normalized; mk.Prism(steelEl, g - up * .3f, dir, .2f, (top - g).magnitude + .5f, 6); mk.Prism(concrete, g - up * .2f, up, .5f, .6f, 8); ArtistBits.PostCollider(ctx.Root, g, 4f, .4f); }
                mk.Box(steelEl, E(s, 0, -1.1f), new Vector3(.4f, .5f, 6f), Quaternion.LookRotation(D)); foreach (var q in new[] { -1f, 1f }) mk.Box(steelEl, E(s, q * 1.4f, -2.2f), new Vector3(.12f, .12f, 3.4f), Quaternion.LookRotation(D) * Quaternion.Euler(q * 40f, 0, 0));
            }
            // a little station over the cross street's east side: a platform, a roof, a sign, a stair down
            var st = Mathf.Min(elL - 12f, 26f); mk.Box(woodM, E(st, 4.4f, .3f), new Vector3(3f, .16f, 14f), along); mk.Box(steelEl, E(st, 5.6f, 2.9f), new Vector3(3.6f, .12f, 14.6f), along * Quaternion.Euler(0, 0, 8f)); foreach (var q in new[] { -6f, 0f, 6f }) mk.Prism(steelEl, E(st + q, 5.6f, .3f), up, .08f, 2.7f, 5);
            mk.Box(white, E(st, 5.94f, 1.9f), new Vector3(.06f, .6f, 4f), along); Sign(E(st, 5.9f, 1.9f) - D * .0f + D * -.04f, -D, "HARBOUR LINE", .26f, Ink); for (var k = 0; k < 14; k++) mk.Box(steelEl, E(st + 8f + k * .5f, 4.4f, .2f - k * .6f), new Vector3(1.6f, .08f, .5f), along);
            // the train: three silver cars; BoomTownLife runs it up and down the line
            var train = new GameObject("The Harbour Line train").transform; train.SetParent(tlife.transform, false); var tm = new MeshKit(); var silver = WorldMaterials.Solid(new Color(.72f, .74f, .78f), .8f);
            for (var cIdx = 0; cIdx < 3; cIdx++)
            {
                var z0 = (cIdx - 1) * 15.4f; tm.Box(silver, new Vector3(0, 1.9f, z0), new Vector3(2.9f, 2.9f, 15f), Quaternion.identity); tm.Box(dark, new Vector3(0, .3f, z0), new Vector3(2.4f, .5f, 13f), Quaternion.identity); tm.Box(Paint(Sky * .8f), new Vector3(0, 1.1f, z0), new Vector3(2.94f, .2f, 15.02f), Quaternion.identity);
                foreach (var e in new[] { -1f, 1f }) { for (var k = 0; k < 6; k++) tm.Box(windowM, new Vector3(e * 1.46f, 2.3f, z0 - 5.6f + k * 2.24f), new Vector3(.03f, .9f, 1.5f), Quaternion.identity); Graffiti(tm, new Vector3(e * 1.47f, 1.5f, z0 + (e > 0 ? 1.5f : -2f)), Vector3.right * e, 5f, 1.2f, cIdx * 2 + (e > 0 ? 0 : 1)); }
            }
            tm.Blob(bulbM, new Vector3(-.8f, 1.2f, 23.1f), Vector3.one * .16f, prng, 0f); tm.Blob(bulbM, new Vector3(.8f, 1.2f, 23.1f), Vector3.one * .16f, prng, 0f);
            var tgo = tm.BuildSingle("Cars", train, false); tgo.transform.localPosition = Vector3.zero; train.rotation = along; tlife.train = train; tlife.trainA = E(-elL - 30f, -1.1f, .3f); tlife.trainB = E(elL + 30f, -1.1f, .3f); train.position = tlife.trainA; life.trainAt = train;
            Shot(ctx, "the el over the block party", G(ctx, ElOff - 26f, -7f) + up * 2.4f, E(4f, 0, -1f));
        }

        // ---------------------------------------------------------------- the block party
        void BlockParty(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = G(ctx, ElOff, 0) + up * .14f;
            // police-blue sawhorses shut the street above and below
            foreach (var d in new[] { -15f, 15f }) for (var k = 0; k < 2; k++) { var b = c + D * d + S * (-2f + k * 4f); b.y = ctx.Ground(b.x, b.z) + .14f; if (k == 0 && d < 0) continue; mk.Box(Paint(Sky * .7f), b + up * .9f, new Vector3(.12f, .2f, 3f), Quaternion.LookRotation(S)); foreach (var e in new[] { -1.2f, 1.2f }) { mk.Box(chain, b + up * .45f + S * e, new Vector3(.6f, .9f, .06f), Quaternion.LookRotation(S) * Quaternion.Euler(0, 0, 0)); } Col(b + up * .5f, new Vector3(.3f, 1f, 3f), Quaternion.LookRotation(S)); }
            // the DJ on a flatbed on the corner, speaker stacks either side
            var fb = c + D * 9f + S * 11f; fb.y = ctx.Ground(fb.x, fb.z) + .14f; var fr = Quaternion.LookRotation((-D - S).normalized); var ff = fr * Vector3.forward; var fs = fr * Vector3.right;
            mk.Box(woodM, fb + up * .9f, new Vector3(6f, .2f, 3f), fr); solid.Box(woodM, fb + up * .5f, new Vector3(6f, 1f, 3f), fr); foreach (var e in new[] { -2.2f, 2.2f }) foreach (var q in new[] { -1.2f, 1.2f }) mk.Prism(dark, fb + up * .45f + fs * e + ff * q, fs, .45f, .3f, 10); for (var k = 0; k < 3; k++) { mk.Box(woodM, fb + ff * (1.8f + k * .34f) + up * (.72f - k * .26f) + fs * 2f, new Vector3(1.4f, .26f, .36f), fr); solid.Box(woodM, fb + ff * (1.8f + k * .34f) + up * (.72f - k * .26f) + fs * 2f, new Vector3(1.4f, .26f, .36f), fr); }
            var dk = Decks(ctx.Root, mk, fb + up * 1f - ff * .3f, fr, goldM); Speakers(ctx.Root, mk, fb + up * 1f - fs * 2.3f - ff * .4f, fr, 2, goldM, new Color(.1f, .1f, .12f)); Speakers(ctx.Root, mk, fb + fs * 4.2f + ff * .4f, fr, 3, goldM, new Color(.1f, .1f, .12f)); Speakers(ctx.Root, mk, fb - fs * 4.2f + ff * .4f, fr, 3, goldM, new Color(.1f, .1f, .12f));
            string[] cut = { "Wikka-wikka-WHAT. The break comes back in and the street goes up.", "You catch the one, spin it back, catch it again. Two copies, one bar, forever.", "Zigga-zigga. A kid on cardboard hits a freeze exactly on your scratch.", "You let the record ride. The train goes over, perfectly in time." }; var cn = 0;
            Spot("The decks", dk + up * .2f, "Scratch", new Vector3(2.2f, .8f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, dk + up * 1.8f, cut[cn++ % cut.Length], 5f); };
            var dj = Local(); dj.hat = Residents.Hat.Cap; Folk(fb + up * 1f - ff * 1.1f, ff, Resident.Pose.Work, new[] { "Requests? No. ...Okay, one. No.", "Get on the decks if you want. Don't touch the red one. I'm joking. Touch them all.", "I carried those speakers up from the harbour myself. Twice. Second time they worked." }, dj).workRate = 1.6f;
            // bunting across the street, a hydrant spraying, cardboard for breaking
            for (var k = 0; k < 3; k++) { var a = c + D * (-10f + k * 10f) + S * 6.4f; var b = c + D * (-10f + k * 10f) - S * 6.4f; a.y = ctx.Ground(a.x, a.z); b.y = ctx.Ground(b.x, b.z); foreach (var g in new[] { a, b }) { mk.Prism(chain, g - up * .2f, up, .05f, 5f, 5); ArtistBits.PostCollider(ctx.Root, g, 4.6f, .07f); } var prev = a + up * 4.7f; for (var q = 1; q <= 14; q++) { var t = q / 14f; var p = Vector3.Lerp(a, b, t) + up * (4.7f - Mathf.Sin(t * Mathf.PI) * .7f); mk.Prism(dark, prev, (p - prev).normalized, .01f, Vector3.Distance(prev, p), 3); mk.Tri(Paint(Tags[(q + k) % Tags.Length]), p + D * .16f, p - D * .16f, p - up * .4f, p + S); mk.Tri(Paint(Tags[(q + k) % Tags.Length]), p - D * .16f, p + D * .16f, p - up * .4f, p - S); prev = p; } }
            var hy = c - D * 6f - S * 5.4f; hy.y = ctx.Ground(hy.x, hy.z) + .2f; mk.Prism(Paint(Red), hy, up, .14f, .7f, 8); mk.Prism(Paint(Red), hy + up * .7f, up, .1f, .14f, 8, .4f); mk.Prism(chain, hy + up * .45f - S * .16f, S, .07f, .32f, 6); ArtistBits.PostCollider(ctx.Root, hy, .8f, .16f); life.hydrant = hy + up * .45f + S * .2f; life.hydrantDir = (S + up * .7f + D * .2f).normalized;
            mk.Prism(WorldMaterials.Solid(new Color(.2f, .24f, .3f), .95f), hy + S * 4f - up * .05f, up, 2.6f, .012f, 12);
            var cb = c - D * 2f + S * 2f; mk.Box(Paint(new Color(.7f, .56f, .38f)), cb + up * .02f, new Vector3(2.6f, .02f, 2f), Quaternion.LookRotation((D + S * .3f).normalized)); Folk(cb + up * .04f, -D, Resident.Pose.Work, new[] { "(He's on his back, spinning. He'll talk when it stops. It isn't stopping.)", "Six-step, windmill, freeze. Hold my hat." }).workRate = 2.4f;
            Folk(c + D * 3f - S * 3f, (D + S).normalized, Resident.Pose.Wave, new[] { "Go! Go! Go! Go!", "This is the one! No, THIS is the one!" }); Folk(c - D * 5f + S * 4f, (D + S * .6f).normalized, Resident.Pose.Wave, new[] { "Whole block's out. Even Mrs. Alvarez. Especially Mrs. Alvarez.", "Stay till the lights come on. That's when it gets good." });
            // an ice-cream truck: take an ice pop
            var ic = c - D * 11f + S * 10f; ic.y = ctx.Ground(ic.x, ic.z) + .14f; var ir = Quaternion.LookRotation((D - S * .2f).normalized); var iff = ir * Vector3.forward; var isd = ir * Vector3.right; mk.Box(white, ic + up * 1.5f - iff * .5f, new Vector3(2f, 2.2f, 3.6f), ir); mk.Box(white, ic + up * 1.05f + iff * 1.9f, new Vector3(2f, 1.3f, 1.4f), ir); mk.Box(glassM, ic + up * 1.5f + iff * 2.3f, new Vector3(1.8f, .6f, .7f), ir * Quaternion.Euler(-30f, 0, 0)); mk.Box(Paint(new Color(1f, .6f, .75f)), ic + up * .6f - iff * .5f, new Vector3(2.04f, .3f, 3.64f), ir); mk.Box(dark, ic + up * 1.6f - iff * .5f - isd * 1.01f, new Vector3(.04f, .9f, 1.6f), ir); mk.Box(Paint(Yellow), ic + up * 2.3f - iff * .5f - isd * 1.3f, new Vector3(.8f, .05f, 1.8f), ir * Quaternion.Euler(0, 0, -14f));
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1.6f, 1.6f }) mk.Prism(dark, ic + up * .36f + isd * e * .9f + iff * q, isd * e, .36f, .2f, 10); mk.Cone(Paint(new Color(.85f, .68f, .4f)), ic + up * 3.1f - iff * .5f, .3f, -.5f, 8); mk.Blob(Paint(new Color(1f, .6f, .75f)), ic + up * 3.24f - iff * .5f, Vector3.one * .34f, prng, .1f); Col(ic + up * 1.3f, new Vector3(2.1f, 2.6f, 5f), ir);
            for (var k = 0; k < 3; k++) { var im = new MeshKit(); im.Box(Paint(new[] { Red, Sky, Yellow }[k]), new Vector3(0, .1f, 0), new Vector3(.06f, .16f, .03f), Quaternion.identity); im.Box(woodM, new Vector3(0, 0, 0), new Vector3(.015f, .1f, .008f), Quaternion.identity); Thing(im, "Ice pop", new[] { "a cherry ice pop", "a blue ice pop (flavour: blue)", "a lemon ice pop" }[k], ic + up * 1.2f - iff * (.9f - k * .4f) - isd * 1.2f, Quaternion.identity, new Vector3(.1f, .22f, .1f), .05f, false); }
            mk.Box(chain, ic + up * 1.14f - iff * .5f - isd * 1.2f, new Vector3(.4f, .05f, 1.6f), ir);
            Mark(c, 16f); Mark(fb, 5f); Mark(ic, 4f);
            Shot(ctx, "the block party", c - D * 13f - S * 4f + up * 2.2f, fb + up * 2f);
        }

        // ---------------------------------------------------------------- the harbour
        void Harbour(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(beach2.x, beach2.y); var rng = new System.Random(36); var along = Quaternion.LookRotation(D);
            mk.Prism(sandM, c - up * .26f, up, 19f, .34f, 26); for (var k = 0; k < 8; k++) Remember(c + Quaternion.Euler(0, k * 45f, 0) * D * 10f, 1); Remember(c, 1);
            // a pier straight out over the water, lamps on it, somebody fishing off the end
            var deckY = waterY + 1.8f; const float pl = 30f; var p0 = c + D * 8f; var pc = p0 + D * pl / 2f; pc.y = deckY; mk.Box(woodM, pc, new Vector3(4f, .2f, pl), along); solid.Box(woodM, pc - up * .4f, new Vector3(4f, 1f, pl), along);
            for (var d = 0f; d <= pl; d += 5f) foreach (var e in new[] { -1f, 1f }) { var p = p0 + D * d + S * e * 2f; p.y = waterY - 2f; mk.Prism(trunkM, p, up, .16f, 4.6f, 6); if (d % 10f < 1f) { var lp = p0 + D * d + S * e * 1.8f; lp.y = deckY + .1f; StreetLight(ctx, mk, lp, -S * e); } }
            for (var d = 0f; d <= pl; d += 2f) { var p = p0 + D * d; p.y = deckY + .12f; Remember(p, 1); }
            var fe = p0 + D * (pl - 1f) + S * .8f; fe.y = deckY + .1f; Crate(mk, fe, D, 2, false); Folk(fe + up * .4f, D, Resident.Pose.Sit, new[] { "Nothing's biting. Nothing ever bites. That's not why you come.", "You can see the whole city from here, when there's a city. Today there's just the idea of one.", "Ferry's the orange one. Goes to Shaolin. Comes back different." }, Local(Residents.Tool.Rod), null, .4f);
            // stacked shipping containers and a crane, off to one side
            var yd = c + S * 22f + D * 2f; for (var k = 0; k < 7; k++) { var col = k % 3; var row = k / 3; var p = yd + S * (col * 2.8f) + D * (row % 2 * 1.2f - 3f + (k == 6 ? 6f : 0f)); p.y = ctx.Ground(p.x, p.z); var lvl = k == 6 ? 0 : row; if (!ctx.IsLand(p.x, p.z, 2f)) continue; var body = Paint(Tags[(k * 2 + 1) % Tags.Length] * .75f); var cp = p + up * (1.3f + lvl * 2.62f); mk.Box(body, cp, new Vector3(2.5f, 2.6f, 6.1f), along); for (var q = 0; q < 9; q++) mk.Box(dark, cp + D * (-2.7f + q * .68f) + S * 1.26f, new Vector3(.03f, 2.4f, .1f), along); if (lvl == 0) Col(cp + up * (row > 0 ? 0f : 1.3f), new Vector3(2.5f, 5.2f, 6.1f), along); }
            Graffiti(mk, yd + S * -1.27f + up * 1.4f + D * -3f, -S, 4.6f, 1.8f, 9); Mark(yd + S * 2.8f, 8f);
            var cr = c - S * 20f + D * 6f; cr.y = ctx.Ground(cr.x, cr.z); if (ctx.IsLand(cr.x, cr.z, 2f)) { foreach (var e in new[] { -1.6f, 1.6f }) foreach (var q in new[] { -1.6f, 1.6f }) { mk.Prism(Paint(Yellow * .9f), cr + S * e + D * q - up * .2f, up, .16f, 14f, 5); ArtistBits.PostCollider(ctx.Root, cr + S * e + D * q, 6f, .2f); } for (var k = 1; k < 5; k++) mk.Box(Paint(Yellow * .9f), cr + up * k * 3f, new Vector3(3.4f, .14f, 3.4f), along); mk.Box(Paint(Yellow * .9f), cr + up * 14.4f + D * 6f, new Vector3(1f, 1f, 22f), along); mk.Box(dark, cr + up * 14.4f - D * 4f, new Vector3(2.4f, 2f, 3f), along); mk.Prism(dark, cr + up * 14f + D * 14f, -up, .03f, 6f, 3); mk.Box(Paint(Red * .9f), cr + up * 6.8f + D * 14f, new Vector3(2.5f, 2.6f, 6.1f), Quaternion.LookRotation(S)); Mark(cr, 4f); }
            // a beach of sorts: a lifeguard chair, a cooler with sodas, towels
            var lg = c - S * 6f + D * 2f + up * .08f; foreach (var e in new[] { -.6f, .6f }) { mk.Box(white, lg + up * 1.1f + S * e + D * .4f, new Vector3(.1f, 2.3f, .1f), Quaternion.Euler(0, 0, 0) * along * Quaternion.Euler(-12f, 0, 0)); mk.Box(white, lg + up * 1.1f + S * e - D * .4f, new Vector3(.1f, 2.3f, .1f), along * Quaternion.Euler(12f, 0, 0)); } mk.Box(white, lg + up * 2.1f, new Vector3(1.3f, .1f, .8f), along); mk.Box(Paint(Red), lg + up * 2.6f - D * .36f, new Vector3(1.3f, .9f, .08f), along); ArtistBits.PostCollider(ctx.Root, lg, 2f, .7f);
            var co = c + S * 7f + D * 3f + up * .08f; mk.Box(Paint(Sky), co + up * .22f, new Vector3(.8f, .44f, .5f), along); mk.Box(white, co + up * .46f, new Vector3(.84f, .06f, .54f), along); Col(co + up * .22f, new Vector3(.8f, .5f, .5f), along); for (var k = 0; k < 3; k++) { var sm = new MeshKit(); sm.Prism(Paint(Tags[(k * 2) % Tags.Length]), Vector3.zero, Vector3.up, .035f, .13f, 8); sm.Prism(chain, new Vector3(0, .13f, 0), Vector3.up, .03f, .01f, 8); Thing(sm, "Soda", "a cold can of soda", co + up * .5f + S * (-.24f + k * .24f), Quaternion.identity, new Vector3(.08f, .14f, .08f), .15f, false); }
            for (var k = 0; k < 3; k++) mk.Box(Paint(Tags[(k + 3) % Tags.Length]), c + S * (2f + k * 2.4f) - D * (2f - k) + up * .1f, new Vector3(1f, .02f, 1.9f), Quaternion.Euler(0, k * 25f, 0)); Interact.Seat(tlife.transform, c + S * 2f - D * 2f + up * .14f, D, "Sit on the towel").seatHeight = .12f;
            Mark(c, 20f);
            Shot(ctx, "the harbour", c - D * 14f + S * 6f + up * 2.6f, pc + up * 1f);
        }
    }

    /// The Harbour Line: the train runs the length of the El and back, stopping a moment at each end.
    public class BoomTownLife : MonoBehaviour
    {
        public Transform train; public Vector3 trainA, trainB;

        void Update()
        {
            if (train == null) return; var u = (float)(SharedChance.Now % 44.0) / 44f; var leg = u < .5f ? u * 2f : (1f - u) * 2f; var t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.08f, .92f, leg));
            train.position = Vector3.Lerp(trainA, trainB, t) + Vector3.up * Mathf.Sin(Time.time * 17f) * .01f;
        }
    }
}
