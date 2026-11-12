using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Metalcore Caldera laid out on purpose (the build-land standard), lightly, because it already works: the arena,
    /// the volcanoes and every clearing stay where they were. Two more basalt paths with cyan studs and neon posts
    /// run out across the flat ground to the south-east and the east. Along them, with the people of the basin: the
    /// Rage Room (a chain-link pen of old televisions, plates and a bat-sized pipe: pick things up and throw them);
    /// the Wall of Things Unsaid (a long obsidian wall whose neon handwriting lights as you pass; add a line of your
    /// own); Black Ink (a tattoo and merch bunker with a chair to sit in); and Scream Point (a platform on the east
    /// rim: scream, and a ring of light rolls out over the whole caldera).
    public partial class CalderaKit
    {
        static readonly Vector2 RageAt = new Vector2(6878, 7052), WallAt = new Vector2(6908, 6966), InkAt = new Vector2(7100, 7106), ScreamAt = new Vector2(7194, 7100);
        static readonly Vector2[] SouthEastPath = { new Vector2(6902, 7112), new Vector2(6900, 7070), new Vector2(6904, 7020), new Vector2(6906, 6988) };
        static readonly Vector2[] EastPath = { new Vector2(7020, 7206), new Vector2(7052, 7160), new Vector2(7086, 7128), new Vector2(7140, 7108), new Vector2(7182, 7101) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (RageAt, 11f), (WallAt, 13f), (InkAt, 10f), (ScreamAt, 8f) };

        CalderaPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> basaltP = new List<Vector3>();

        static readonly Color[] Blacks = { new Color(.06f, .06f, .08f), new Color(.12f, .12f, .14f), new Color(.2f, .2f, .22f), new Color(.5f, .1f, .12f), new Color(.85f, .85f, .88f) };
        static readonly Color[] Dyed = { new Color(.2f, .92f, 1f), new Color(1f, .25f, .75f), new Color(.6f, .3f, 1f), new Color(.08f, .08f, .1f), new Color(.9f, .9f, .92f), new Color(.08f, .08f, .1f) };
        static readonly string[] BasinTalk =
        {
            "You feel that? Ground's counting in. Breakdown's coming.", "It's not anger. People get that wrong. It's what's left after you put the anger down.", "Ash in your hair is good luck here. Everyone's very lucky.",
            "I screamed at the Point yesterday. Slept like a baby. A loud baby.", "Clean vocals for the chorus, obviously. We're not animals.",
        };

        Residents.Look Kid(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Blacks, Blacks, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Cap }, Blacks); l.hair = Dyed[prng.Next(Dyed.Length)]; l.longHair = prng.Next(2) == 0; l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Kid(), pose, prng, lines ?? BasinTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeCaldera(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
        }

        /// An obsidian slab with neon words. Signs on the +x side of a place's path; the photo spot on the −x side.
        void NeonSlab(MeshKit mk, Vector3 at, Vector3 face, string text, Color c, float w = 3f, float size = .22f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, -3f);
            mk.Box(obsidian, g + up * 1f, new Vector3(w, 2.2f, .35f), rot); Col(g + up * 1f, new Vector3(w, 2.2f, .4f), rot); mk.Box(GlowM(c, 2.4f), g + up * .12f + face * .2f, new Vector3(w * .9f, .06f, .04f), rot); halos.Add(g + up * 1.2f + face * .4f, 1.6f, c, .45f);
            ArtistBits.Label(rootT, text, g + up * 1.2f + face * .19f, face, size, c, TextAnchor.MiddleCenter);
        }

        void BasaltBench(MeshKit mk, Vector3 at, Vector3 face, Color c, float len = 2.2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(obsidian, g + up * .25f, new Vector3(len, .5f, .6f), rot); mk.Box(GlowM(c, 2.2f), g + up * .04f + face * .31f, new Vector3(len, .05f, .03f), rot); Col(g + up * .25f, new Vector3(len, .5f, .6f), rot);
            Interact.Bench(plife.transform, g + up * .56f - side * (len / 2f - .45f), g + up * .56f + side * (len / 2f - .45f), face);
        }

        List<Vector3> BasaltPath(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 170f * 170f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2f);
            for (var i = 0; i < pts.Count; i++) { foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } } foreach (var c in noGrass) { if (c.y < 7f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } } }
            if (pts.Count < 2) return pts;
            Strip(mk, pts, basalt, PathW, .22f, ctx); var up = Vector3.up;
            for (var i = 2; i < pts.Count - 1; i += 3) { var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var side = Vector3.Cross(up, d); foreach (var s in new[] { -1f, 1f }) { var st = pts[i] + side * s * (PathW / 2f + .15f); st.y = ctx.Ground(st.x, st.z) + .08f; mk.Box(lavaCyan, st, new Vector3(.18f, .12f, .18f), Quaternion.identity); } }
            foreach (var p in pts) Remember(p, 700);
            var n = 0; for (var i = 8; i < pts.Count; i += 16) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 1f : -1f) * (PathW / 2f + 1f); if (!InNoGrass(lp.x, lp.z, 1f)) NeonPost(ctx, mk, ctx.OnGround(lp.x, lp.z)); }
            basaltP.AddRange(pts);
            if (pts.Count > 40) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = pts[i] - sd * 3.4f; if (ctx.IsLand(bp.x, bp.z, 2f)) { BasaltBench(mk, bp, sd, Violet); Ring(bp, 2f); } }
            return pts;
        }

        void Branch(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(basaltP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.7f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, basalt, 2.2f, .2f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void BasinPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            basaltP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2004);
            plife = new GameObject("Caldera places life").AddComponent<CalderaPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            BasaltPath(ctx, mk, SouthEastPath); BasaltPath(ctx, mk, EastPath);
            RageRoom(ctx, mk, solid); WallUnsaid(ctx, mk, solid); BlackInk(ctx, mk, solid); ScreamPoint(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Rage Room
        void RageRoom(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(RageAt.x, RageAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(basaltP, g))); const float S = 12f, H = 3.2f;
            mk.Box(basalt, g - up * .35f, new Vector3(S + 2f, 1f, S + 2f), f.r); solid.Box(basalt, g - up * .35f, new Vector3(S + 2f, 1f, S + 2f), f.r); var fo = new PlaceFrame(g + up * .15f, f.Fwd);
            // a chain-link pen with a gap for a gate; magenta strip lights along the top rail
            void Fence(Vector3 a, Vector3 b) { var mid = (a + b) / 2f; var d = b - a; var len = d.magnitude; var q = Quaternion.LookRotation(d.normalized); mk.Box(steel, mid + up * H, new Vector3(.06f, .06f, len), q); mk.Box(steel, mid + up * .1f, new Vector3(.06f, .06f, len), q); for (var s = 0; s <= Mathf.RoundToInt(len / 2f); s++) mk.Box(steel, Vector3.Lerp(a, b, s / Mathf.Max(1f, Mathf.Round(len / 2f))) + up * (H / 2f), new Vector3(.07f, H, .07f), q); for (var s = 0; s < Mathf.RoundToInt(len / .5f); s++) mk.Box(chain, Vector3.Lerp(a, b, (s + .5f) / Mathf.Round(len / .5f)) + up * (H / 2f), new Vector3(.015f, H - .2f, .015f), q * Quaternion.Euler(0, 0, s % 2 == 0 ? 30f : -30f)); mk.Box(GlowM(Magenta, 2.4f), mid + up * (H + .06f), new Vector3(.04f, .04f, len), q); Col(mid + up * (H / 2f), new Vector3(.15f, H, len), q); }
            Vector3 C(float x, float z) => fo.P(x * S / 2f, 0, z * S / 2f);
            Fence(C(-1, -1), C(1, -1)); Fence(C(1, -1), C(1, 1)); Fence(C(-1, 1), C(-1, -1)); Fence(C(1, 1), fo.P(1.4f, 0, S / 2f)); Fence(fo.P(-1.4f, 0, S / 2f), C(-1, 1));
            plife.rageLight = ArtistBits.PointLight(ctx.Root, fo.P(0, 3f, 0), Magenta, 20f, 1.8f); halos.Add(fo.P(0, 2.4f, 0), 5f, Magenta, .2f);
            // things to wreck: old tellies on crates, a stack of plates, bottles, a keyboard, a pipe; a concrete target wall sprayed with a ring
            mk.Box(ashDark, fo.P(0, 1.4f, -S / 2f + .5f), new Vector3(6f, 2.8f, .5f), f.r); Col(fo.P(0, 1.4f, -S / 2f + .5f), new Vector3(6f, 2.8f, .5f), f.r); mk.Prism(GlowM(Cyan, 2f), fo.P(0, 1.6f, -S / 2f + .76f), f.Fwd, .9f, .02f, 20); mk.Prism(ashDark, fo.P(0, 1.6f, -S / 2f + .77f), f.Fwd, .75f, .02f, 20); mk.Prism(GlowM(Magenta, 2.4f), fo.P(0, 1.6f, -S / 2f + .78f), f.Fwd, .2f, .02f, 12);
            foreach (var e in new[] { -3.2f, 3.2f }) { mk.Box(steel, fo.P(e, .4f, 1.5f), new Vector3(1.4f, .8f, .9f), f.r); Col(fo.P(e, .4f, 1.5f), new Vector3(1.4f, .8f, .9f), f.r); }
            var n = 0;
            Carryable Thing(MeshKit m, string name, string thing, Vector3 at, float mass, Vector3 size) { var go = m.BuildSingle(name, plife.transform, false); go.transform.SetPositionAndRotation(at, f.r * Quaternion.Euler(0, n++ * 37f, 0)); var c = Interact.Carry(go, thing, mass, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); } return c; }
            for (var k = 0; k < 3; k++) { var tv = new MeshKit(); tv.Box(dark, new Vector3(0, .25f, 0), new Vector3(.6f, .5f, .5f), Quaternion.identity); tv.Box(GlowM(k == 1 ? Violet : Cyan, 1.4f), new Vector3(-.05f, .27f, .255f), new Vector3(.4f, .34f, .01f), Quaternion.identity); Thing(tv, "Old television", "an old television", fo.P(-3.4f + k * .0f + (k == 2 ? 6.6f : k * .7f), .82f, 1.5f), 3f, new Vector3(.6f, .5f, .5f)); }
            for (var k = 0; k < 5; k++) { var pl = new MeshKit(); pl.Prism(WorldMaterials.Solid(new Color(.9f, .9f, .92f), .6f), Vector3.zero, Vector3.up, .14f, .02f, 12); Thing(pl, "Plate", "a plate", fo.P(3.5f, .82f + k * .03f, 1.4f), .3f, new Vector3(.28f, .03f, .28f)); }
            for (var k = 0; k < 4; k++) { var bt = new MeshKit(); bt.Prism(WorldMaterials.Glow(k % 2 == 0 ? new Color(.2f, .6f, .4f) : new Color(.5f, .3f, .15f), .2f), Vector3.zero, Vector3.up, .04f, .2f, 8, .5f); Thing(bt, "Bottle", "an empty bottle", fo.P(2.7f + k * .2f, .82f, 1.75f), .25f, new Vector3(.1f, .24f, .1f)); }
            var kb = new MeshKit(); kb.Box(WorldMaterials.Solid(new Color(.8f, .78f, .72f)), new Vector3(0, .02f, 0), new Vector3(.45f, .04f, .16f), Quaternion.identity); Thing(kb, "Keyboard", "an office keyboard (the spreadsheet kind)", fo.P(-3.4f, .82f, 1.85f), .4f, new Vector3(.45f, .06f, .16f));
            var pipe = new MeshKit(); pipe.Prism(steel, new Vector3(0, .03f, -.4f), Vector3.forward, .03f, .8f, 6); Thing(pipe, "Pipe", "a length of pipe", fo.P(-1f, .05f, 3f), 1.2f, new Vector3(.08f, .08f, .8f));
            // the keeper, in goggles, entirely calm
            Folk(fo.P(4.6f, 0, 4.2f), -f.Right, Resident.Pose.Stand, new[] { "Pick it up. Name it after something. Throw it at the wall. Don't tell me what you named it.", "We go through forty tellies a week. I don't ask where they come from. They don't ask where they're going.", "Feel better? Good. Sweep up. I'm joking, nobody sweeps up." }, Kid(Residents.Tool.Broom, true));
            BasaltBench(mk, f.P(-4f, 0, S / 2f + 2.4f), f.Fwd, Magenta);
            NeonSlab(mk, f.P(5f, 0, S / 2f + 4f), f.Fwd, "THE RAGE ROOM\nbreak something", Magenta, 3.4f, .2f);
            Branch(ctx, mk, f.P(0, 0, S / 2f + 1f)); Ring(g, 10f);
            Shot(ctx, "the rage room", f.P(-7f, 4f, S / 2f + 9f), fo.P(0, 1.2f, 0));
        }

        // ---------------------------------------------------------------- the Wall of Things Unsaid
        void WallUnsaid(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(WallAt.x, WallAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(basaltP, g))); const float L = 22f, H = 4.2f;
            // a long, slightly curved wall of obsidian slabs; on each, a line in neon handwriting, dim until you come near
            string[] unsaid =
            {
                "i should have called", "it wasn't your fault", "i'm still here", "i kept the hoodie", "thank you for staying", "i was scared, not angry",
                "i forgive you (mostly)", "i meant every word", "it got better", "you were right about the bridge", "sorry about the van",
            };
            Color[] cols = { Cyan, Magenta, Violet, new Color(.9f, .95f, 1f) };
            for (var k = 0; k < 11; k++)
            {
                var t = (k - 5) / 5f; var p = G(f.P(t * L / 2f, 0, -3f - t * t * 2.5f)); var face = FaceTo(p, g + f.Fwd * 14f); var q = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, (k * 37 % 7) - 3f); var h = H - (k * 53 % 9) * .12f;
                mk.Box(obsidian, p + up * (h / 2f - .3f), new Vector3(2.05f, h + .6f, .5f), q); Col(p + up * (h / 2f), new Vector3(2.05f, h, .5f), q);
                var c = cols[k % cols.Length]; var go = new GameObject("Unsaid " + k); go.transform.SetParent(plife.transform, false);
                var label = ArtistBits.Label(go.transform, unsaid[k], p + up * (1.5f + (k % 3) * .5f) + face * .27f, face, .2f, c, TextAnchor.MiddleCenter);
                var line = new Material(WorldMaterials.Glow(c, .2f)) { name = "Unsaid underline" }; mk.Box(line, p + up * (1.2f + (k % 3) * .5f) + face * .27f, new Vector3(1.5f, .03f, .02f), q); plife.unsaid.Add((p + face * 2f, line, c, label));
            }
            // the booth where you add your own: a stylus on a chain
            var bp = G(f.P(0, 0, 4f)); mk.Box(obsidian, bp + up * .55f, new Vector3(.9f, 1.1f, .7f), f.r); mk.Box(GlowM(Cyan, 1.4f), bp + up * 1.12f, new Vector3(.7f, .03f, .5f), f.r * Quaternion.Euler(-14f, 0, 0)); Col(bp + up * .55f, new Vector3(.9f, 1.1f, .7f), f.r);
            var sgo = new GameObject("Neon stylus"); sgo.transform.SetParent(plife.transform, false); sgo.transform.position = bp + up * 1.2f + f.Fwd * .3f;
            string[] mine = { "You write it. It glows for a second on the black, then joins the others.", "You write the thing. It's lighter on the wall than it was in your chest.", "You write a name, then rub it out, then write it again. It stays." }; var mn = 0;
            Interact.Toy(sgo, Toy.Kind.Custom, "Write on the wall", new Vector3(.9f, .7f, .8f)).onUse = _ => { plife.wrote = Time.time; Say(plife.transform, bp + up * 2.4f, mine[mn++ % mine.Length], 6f); };
            plife.wallLight = ArtistBits.PointLight(ctx.Root, G(f.P(0, 0, 0)) + up * 3f, Violet, 22f, 1.4f);
            BasaltBench(mk, f.P(-5f, 0, 5f), -f.Fwd, Cyan); BasaltBench(mk, f.P(5f, 0, 5f), -f.Fwd, Violet);
            Folk(G(f.P(-5f, 0, 5f)) + up * .56f + f.Right * .5f, -f.Fwd, Resident.Pose.Sit, new[] { "Mine's the third from the left. I'm not saying which line.", "Nobody reads them out loud. That's the only rule.", "You don't have to mean it for ever. You just have to mean it once, in neon." }, Kid(), null, .56f);
            Folk(G(f.P(3.2f, 0, .4f)), -f.Fwd, Resident.Pose.Stand, new[] { "\"sorry about the van\". That one's about me. I think. I hope.", "I come and stand in front of the one I can't write yet." }, Kid());
            NeonSlab(mk, f.P(8.5f, 0, 9f), f.Fwd, "THE WALL OF\nTHINGS UNSAID", Violet, 3.4f, .2f);
            Branch(ctx, mk, f.P(0, 0, 7f)); Ring(g, 12f);
            Shot(ctx, "the wall of things unsaid", f.P(-8f, 3f, 13f), g + up * 2f - f.Fwd * 2f);
        }

        // ---------------------------------------------------------------- Black Ink
        void BlackInk(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(InkAt.x, InkAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(basaltP, g))); const float W = 9f, D = 7f, H = 3.3f, T = .35f, Door = HumanScale.DoorWidth + .3f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd);
            mk.Box(basalt, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(obsidian, p, s, f.r); solid.Box(obsidian, p, s, f.r); }
            mk.Box(ashDark, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(ashDark, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(obsidian, fo.P(0, H + .1f, 0), new Vector3(W + .5f, .2f, D + .5f), f.r); solid.Box(obsidian, fo.P(0, H + .1f, 0), new Vector3(W + .5f, .2f, D + .5f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .8f, H, D - .8f));
            ArtistBits.Label(ctx.Root, "BLACK INK", fo.P(0, H + .9f, D / 2f + .1f), f.Fwd, .6f, Cyan, TextAnchor.MiddleCenter); mk.Box(dark, fo.P(0, H + .9f, D / 2f), new Vector3(5f, 1.2f, .14f), f.r); halos.Add(fo.P(0, H + .9f, D / 2f + .6f), 2.6f, Cyan, .5f); foreach (var e in new[] { -1f, 1f }) { mk.Box(GlowM(Magenta, 2.4f), fo.P(e * (Door / 2f + .08f), 1.4f, D / 2f + .02f), new Vector3(.06f, 2.8f, .05f), f.r); mk.Box(GlowM(Cyan, 2.4f), fo.P(e * (W / 2f), H / 2f, D / 2f + .02f), new Vector3(.06f, H, .05f), f.r); }
            // inside: the chair, a tray of inks, flash sheets glowing on the wall, a rail of band shirts, a counter
            var ch = fo.P(-1.8f, 0, -.6f); var cq = Quaternion.LookRotation(f.Right); mk.Box(dark, ch + up * .5f, new Vector3(.7f, .16f, 1.3f), cq); mk.Box(dark, ch + up * .95f - f.Right * .7f, new Vector3(.7f, .16f, .9f), cq * Quaternion.Euler(-55f, 0, 0)); mk.Prism(steel, ch, up, .14f, .45f, 8); Col(ch + up * .45f, new Vector3(1.4f, .9f, .8f), f.r); Interact.Seat(plife.transform, ch + up * .62f, f.Right, "Sit in the tattoo chair");
            var tray = ch - f.Fwd * 1.1f; mk.Prism(steel, tray, up, .04f, .9f, 5); mk.Box(steel, tray + up * .9f, new Vector3(.6f, .03f, .4f), f.r); for (var k = 0; k < 5; k++) mk.Prism(GlowM(new[] { Cyan, Magenta, Violet, new Color(.1f, .1f, .1f), new Color(1f, .3f, .2f) }[k], 1.6f), tray + up * .92f + f.Right * (-.22f + k * .11f), up, .03f, .08f, 6); ArtistBits.PostCollider(ctx.Root, tray, .9f, .25f);
            string[] flash = { "a dagger through a heart", "a moth", "a rose, very black", "a tiny volcano", "\"STAY\"", "a swallow", "a broken clock", "an anchor (ironic)" };
            for (var k = 0; k < 8; k++) { var p = fo.P(-3.4f + (k % 4) * 1f, 1.3f + (k / 4) * 1f, -D / 2f + T + .03f); mk.Box(WorldMaterials.Solid(new Color(.9f, .9f, .86f)), p, new Vector3(.8f, .8f, .02f), f.r); mk.Blob(GlowM(new[] { Cyan, Magenta, Violet }[k % 3], 1.6f), p + f.Fwd * .02f, new Vector3(.2f, .26f, .01f), prng, .5f); }
            var fgo = new GameObject("Flash wall"); fgo.transform.SetParent(plife.transform, false); fgo.transform.position = fo.P(-1.9f, 1.8f, -D / 2f + T + .6f); var fn = 0; Interact.Toy(fgo, Toy.Kind.Custom, "Pick a design", new Vector3(4.2f, 2.2f, 1f)).onUse = _ => Say(plife.transform, fo.P(-1.9f, 2.9f, -D / 2f + 1.4f), "You point at " + flash[fn++ % flash.Length] + ". \"Good choice. Everyone gets that one. That's why it's good.\"", 5.5f);
            var artist = Kid(Residents.Tool.Rod, true); artist.hair = Magenta; Folk(ch + f.Fwd * 1.1f, -f.Fwd, Resident.Pose.Work, new[] { "Sit down. It only hurts for the whole time.", "Lyrics on the ribs? Brave. Nobody's ever done that. Everybody's done that.", "I did the whole arena's knuckles. One letter each. It spells a very long word." }, artist).workRate = 1.4f;
            var rail = fo.P(2.6f, 0, -D / 2f + T + .6f); mk.Box(steel, rail + up * 1.7f, new Vector3(2.6f, .04f, .04f), f.r); foreach (var e in new[] { -1.3f, 1.3f }) mk.Box(steel, rail + up * .85f + f.Right * e, new Vector3(.04f, 1.7f, .04f), f.r); Col(rail + up * .9f, new Vector3(2.6f, 1.8f, .5f), f.r);
            for (var k = 0; k < 5; k++) { var sm = new MeshKit(); sm.Box(dark, new Vector3(0, -.35f, 0), new Vector3(.5f, .6f, .06f), Quaternion.identity); sm.Box(dark, new Vector3(0, -.12f, 0), new Vector3(.8f, .16f, .06f), Quaternion.identity); sm.Blob(GlowM(new[] { Cyan, Magenta, Violet, new Color(.9f, .9f, .9f), new Color(1f, .3f, .2f) }[k], 1.4f), new Vector3(0, -.35f, .035f), new Vector3(.14f, .16f, .005f), prng, .5f); var go = sm.BuildSingle("Band shirt", plife.transform, false); go.transform.SetPositionAndRotation(rail + up * 1.66f + f.Right * (-1f + k * .5f), f.r * Quaternion.Euler(0, 90f, 0)); var c = Interact.Carry(go, "a band shirt (black, obviously)", .2f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.8f, .7f, .1f); bc.center = new Vector3(0, -.32f, 0); } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            mk.Box(obsidian, fo.P(3f, .55f, 1.6f), new Vector3(1.8f, 1.1f, .7f), f.r); Col(fo.P(3f, .55f, 1.6f), new Vector3(1.8f, 1.1f, .7f), f.r); mk.Box(GlowM(Violet, 1.4f), fo.P(3f, 1.12f, 1.6f), new Vector3(1.6f, .03f, .5f), f.r);
            Folk(fo.P(3f, 0, .7f), f.Fwd, Resident.Pose.Stand, new[] { "Shirts come in black, and faded black, which is the old black.", "Medium's all gone. It's always all gone. Smalls and triple-XLs, that's a scene for you." }, Kid());
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.7f, 0), new Color(.8f, .85f, 1f), 12f, 2.4f, true);
            BasaltBench(mk, f.P(-3.6f, 0, D / 2f + 1.5f), f.Fwd, Cyan);
            Branch(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 9f);
            Shot(ctx, "black ink", f.P(-6f, 2.6f, D / 2f + 11f), fo.P(0, 2.4f, 0));
            Shot(ctx, "inside black ink", fo.P(3.2f, 1.6f, D / 2f - 1f), fo.P(-2f, 1.2f, -1.5f));
        }

        // ---------------------------------------------------------------- Scream Point
        void ScreamPoint(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ScreamAt.x, ScreamAt.y); var basin = FaceTo(g, new Vector3(center.x, 0, center.z)); var f = new PlaceFrame(g, basin);
            // a round obsidian platform ringed in cyan, a single mic on a stand at its lip, facing the whole caldera
            mk.Prism(obsidian, g - up * .6f, up, 5f, 1f, 12); solid.Prism(obsidian, g - up * .6f, up, 5f, 1f, 12); mk.Prism(GlowM(Cyan, 2.4f), g + up * .41f, up, 4.6f, .02f, 24); mk.Prism(obsidian, g + up * .42f, up, 4.4f, .02f, 24); halos.Add(g + up * .6f, 5f, Cyan, .25f);
            var mp = g + up * .44f + basin * 3.4f; mk.Prism(steel, mp, up, .03f, 1.5f, 5); mk.Prism(steel, mp, up, .25f, .04f, 8); mk.Blob(dark, mp + up * 1.56f, Vector3.one * .08f, prng, 0f);
            var ringM = new Material(WorldMaterials.Glow(Magenta, 0f)) { name = "Scream ring" }; var rk = new MeshKit(); for (var s = 0; s < 32; s++) { var a0 = s * Mathf.PI / 16f; var a1 = (s + 1) * Mathf.PI / 16f; var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)); rk.Box(ringM, (p0 + p1) / 2f, new Vector3(.03f, .03f, Vector3.Distance(p0, p1) + .01f), Quaternion.LookRotation(p1 - p0)); }
            var rgo = rk.BuildSingle("Soft scream ring", plife.transform, false); rgo.transform.position = mp + up * 1.5f; rgo.transform.localScale = Vector3.one * .1f; plife.screamRing = rgo.transform; plife.screamM = ringM; plife.screamLight = ArtistBits.PointLight(ctx.Root, mp + up * 2.2f, Magenta, 60f, 0f);
            var sgo = new GameObject("Scream mic"); sgo.transform.SetParent(plife.transform, false); sgo.transform.position = mp + up * 1.4f - basin * .4f; string[] scream = { "You let it go. It rolls out across the basin and every crack in the ground answers.", "Somewhere down in the arena, a hundred people scream it back.", "The echo comes home off the far rim, a little hoarse, a lot lighter." }; var sn = 0;
            Interact.Toy(sgo, Toy.Kind.Custom, "Scream", new Vector3(.9f, 1.2f, .9f)).onUse = _ => { plife.screamed = Time.time; Say(plife.transform, mp + up * 2.8f, scream[sn++ % scream.Length], 6f); };
            foreach (var e in new[] { -1f, 1f }) NeonPost(ctx, mk, G(g + f.Right * e * 6f - basin * 1f));
            BasaltBench(mk, g - basin * 6.5f + f.Right * 2f, basin, Magenta);
            Folk(G(g - basin * 6.5f + f.Right * 2f) + up * .56f - f.Right * .5f, basin, Resident.Pose.Sit, new[] { "I'm next. I've been next for an hour. I'm working up to it.", "First time's embarrassing. Second time's the best thing you've ever done.", "Give it everything. The basin can take it. That's what it's a basin for." }, Kid(), null, .56f);
            NeonSlab(mk, g - basin * 9f - f.Right * 4f, -basin, "SCREAM POINT", Magenta, 3f);
            Ring(g, 7f);
            Shot(ctx, "scream point", g - basin * 10f + f.Right * 5f + up * 3.4f, mp + up * 1.4f + basin * 6f);
        }
    }

    /// The Caldera places' moving parts: the wall's lines lighting as you pass, the scream's ring of light, the rage room's glow.
    public class CalderaPlacesLife : MonoBehaviour
    {
        public List<(Vector3 at, Material line, Color c, TextMesh label)> unsaid = new List<(Vector3, Material, Color, TextMesh)>(); public float wrote = -99f, screamed = -99f; public Light wallLight, screamLight, rageLight;
        public Transform screamRing; public Material screamM; Transform player; readonly List<float> lit = new List<float>();
        static readonly Color Magenta = new Color(1f, .25f, .75f);

        void Update()
        {
            var t = Time.time;
            if (player == null) { var p = FindAnyObjectByType<EverythingWorld.Player.SimplePlayerController>(); if (p != null) player = p.transform; }
            while (lit.Count < unsaid.Count) lit.Add(0f);
            var all = t - wrote < 8f;
            for (var i = 0; i < unsaid.Count; i++)
            {
                var (at, line, c, label) = unsaid[i]; var near = all || player != null && (player.position - at).sqrMagnitude < 5f * 5f;
                lit[i] = Mathf.MoveTowards(lit[i], near ? 1f : 0f, Time.deltaTime * 1.6f); line.SetColor("_EmissionColor", c * (.2f + lit[i] * 3f));
                if (label != null) { var col = Color.Lerp(c * .35f, c, lit[i]); col.a = 1f; label.color = col; }
            }
            if (wallLight != null) wallLight.intensity = 1.4f + (all ? 1.6f * (1f - (t - wrote) / 8f) : 0f);
            var a = t - screamed;
            if (screamRing != null) { var u = a < 5f ? a / 5f : 0f; screamRing.localScale = Vector3.one * (.1f + u * 160f); if (screamM != null) screamM.SetColor("_EmissionColor", Magenta * (a < 5f ? 5f * (1f - u) : 0f)); if (screamLight != null) screamLight.intensity = a < 5f ? 6f * (1f - u) : 0f; }
            if (rageLight != null) rageLight.intensity = 1.8f + .5f * Mathf.Sin(t * 3f);
        }
    }
}
