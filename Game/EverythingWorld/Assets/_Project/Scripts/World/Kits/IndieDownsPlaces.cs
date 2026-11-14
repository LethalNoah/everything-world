using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Indie Downs laid out on purpose (the build-land standard), lightly: the two paths, the green, the quiet
    /// place and every artist stay exactly where they were. Four plain chalk lanes with warm lanterns (no one's
    /// colour) reach the empty west, north-west, south-east and north-east. Along them, with a few people of the
    /// Downs: the Beach Huts above the west shore (deckchairs, a windbreak, an ice cream van, pebbles); the Allotments
    /// (beds, sheds, a greenhouse, sunflowers, a watering can, pumpkins); the Car Boot Sale (open boots and trestle
    /// tables of half-remembered things to pick up); the Caravan Site (three old caravans, a washing line, swingball,
    /// a radio); and the Toposcope on the north-east rise, which tells you what lies each way.
    public partial class IndieDownsKit
    {
        static readonly Vector2 HutsAt = new Vector2(8088, 5640), PlotsAt = new Vector2(8313, 5668), BootAt = new Vector2(8300, 5928), VansAt = new Vector2(8848, 5212), TopoAt = new Vector2(8972, 5856);
        static readonly Vector2[] WestLane = { new Vector2(8440, 5604), new Vector2(8385, 5640), new Vector2(8340, 5642), new Vector2(8240, 5650), new Vector2(8160, 5645), new Vector2(8112, 5641) };
        static readonly Vector2[] NorthWestLane = { new Vector2(8385, 5640), new Vector2(8362, 5720), new Vector2(8340, 5800), new Vector2(8318, 5868), new Vector2(8304, 5904) };
        static readonly Vector2[] EastLane = { new Vector2(8566, 5296), new Vector2(8640, 5266), new Vector2(8740, 5236), new Vector2(8822, 5216) };
        static readonly Vector2[] NorthEastLane = { new Vector2(8900, 5896), new Vector2(8935, 5880), new Vector2(8958, 5864) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (HutsAt, 13f), (PlotsAt, 15f), (BootAt, 15f), (VansAt, 14f), (TopoAt, 6f) };

        DownsPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> laneP = new List<Vector3>();

        static readonly Color[] Knits = { new Color(.75f, .6f, .45f), new Color(.55f, .65f, .6f), new Color(.8f, .5f, .45f), new Color(.9f, .85f, .7f), new Color(.45f, .5f, .65f), new Color(.85f, .75f, .4f), new Color(.6f, .45f, .55f) };
        static readonly Color[] Cords = { new Color(.35f, .4f, .5f), new Color(.5f, .4f, .3f), new Color(.3f, .35f, .3f), new Color(.75f, .7f, .6f) };
        static readonly Residents.Hat[] SunHats = { Residents.Hat.None, Residents.Hat.Straw, Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Beanie };
        static readonly string[] DownsTalk =
        {
            "It was a summer like this one. Or it was this one. Hard to say.", "The light does that about now. Give it a minute; it'll do it again.", "There's a band on at the green later. There's always a band on later.",
            "You can smell rain coming. It won't come to much.", "Somebody lost a kite up that tree in... well. A while ago.",
        };

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Knits, Cords, SunHats, Knits); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Local(), pose, prng, lines ?? DownsTalk, route, seatH);
        void Clear(Vector3 at, float r) { noGrass.Add(new Vector3(at.x, r, at.z)); ctx0.AddKeepout(at.x, at.z, r); }
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// The places' life object is made first, so the Downs' own benches can be sat on too.
        void StartPlaces(LandContext ctx)
        {
            laneP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2004);
            plife = new GameObject("Downs places life").AddComponent<DownsPlacesLife>(); plife.transform.SetParent(ctx.Root, false);
        }

        static void PlaceLevel(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = data.HeightAt(c.x, c.y);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level; else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        /// A hand-painted board on a post. Signs go on the +x side of a place's lane; the photo spot on the −x side.
        void PaintedSign(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.2f, float size = .15f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            mk.Box(wood, g + up * .8f, new Vector3(.1f, 1.6f, .1f), rot); mk.Box(cream, g + up * 1.5f, new Vector3(w, .7f, .06f), rot); Col(g + up * .95f, new Vector3(w, 1.9f, .12f), rot);
            ArtistBits.Label(rootT, text, g + up * 1.5f + face * .04f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        void LaneLantern(MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; mk.Box(wood, g + up * .7f, new Vector3(.1f, 1.4f, .1f), Quaternion.identity); mk.Box(iron, g + up * 1.5f, new Vector3(.22f, .04f, .22f), Quaternion.identity); mk.Box(WorldMaterials.Glow(Lamp, 1.5f), g + up * 1.62f, new Vector3(.16f, .2f, .16f), Quaternion.identity); mk.Box(iron, g + up * 1.75f, new Vector3(.24f, .04f, .24f), Quaternion.identity);
            halos.Add(g + up * 1.62f, .9f, Lamp, .7f); ArtistBits.PostCollider(rootT, g, 1.7f, .1f);
        }

        void Deckchair(MeshKit mk, Vector3 at, Vector3 face, Color stripe, bool seat = true)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var a = WorldMaterials.Solid(stripe); var b = WorldMaterials.Solid(Cream);
            for (var s = 0; s < 4; s++) mk.Box(s % 2 == 0 ? a : b, g + up * (.32f + s * .14f) - face * (s * .17f - .2f), new Vector3(.6f, .03f, .24f), rot * Quaternion.Euler(-38f, 0, 0));
            foreach (var e in new[] { -.32f, .32f }) { mk.Box(wood, g + rot * new Vector3(e, .35f, -.05f), new Vector3(.04f, .04f, 1.15f), rot * Quaternion.Euler(-38f, 0, 0)); mk.Box(wood, g + rot * new Vector3(e, .25f, .1f), new Vector3(.04f, .04f, .9f), rot * Quaternion.Euler(25f, 0, 0)); }
            Col(g + up * .35f, new Vector3(.65f, .7f, 1f), rot); if (seat) Interact.Seat(plife.transform, g + up * .42f, face, "Sit in the deckchair");
        }

        void TrestleTable(MeshKit mk, Vector3 at, Quaternion rot, float len = 2.2f)
        {
            var up = Vector3.up; mk.Box(wood, at + up * .75f, new Vector3(len, .05f, .8f), rot); foreach (var e in new[] { -1f, 1f }) { mk.Box(darkWood, at + rot * new Vector3(e * (len / 2f - .25f), .37f, .2f), new Vector3(.05f, .78f, .05f), rot * Quaternion.Euler(14f, 0, 0)); mk.Box(darkWood, at + rot * new Vector3(e * (len / 2f - .25f), .37f, -.2f), new Vector3(.05f, .78f, .05f), rot * Quaternion.Euler(-14f, 0, 0)); }
            Col(at + up * .4f, new Vector3(len, .8f, .8f), rot);
        }

        Carryable Thing(MeshKit m, string name, string thing, Vector3 at, Quaternion rot, float mass, Vector3 size, float lowest)
        {
            var go = m.BuildSingle(name, plife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, thing, mass, lowest); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); } return c;
        }

        List<Vector3> ChalkLane(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            var pts = PlaceTools.Smooth(ctx, way, false, 2f);
            for (var i = 0; i < pts.Count; i++)
            {
                foreach (var a in ctx.Artists) { var r = a.r + 4f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                foreach (var c in noGrass) { if (c.y < 9f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
                if (hasQuiet) { var r = QuietBlend + 6f; var v = new Vector2(pts[i].x - quiet.x, pts[i].z - quiet.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(quiet.x + v.x, quiet.z + v.y); } }
            }
            if (pts.Count < 2) return pts;
            Ribbon(ctx, mk, chalk, pts, 2.4f, .055f);
            foreach (var p in pts) { var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f)); if (!pathCells.TryGetValue(key, out var cell)) pathCells[key] = cell = new List<Vector3>(); cell.Add(p); }
            var up = Vector3.up; var n = 0;
            for (var i = 8; i < pts.Count; i += 16) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 1.9f : -1.9f); if (ctx.IsLand(lp.x, lp.z, 2f)) LaneLantern(mk, ctx.OnGround(lp.x, lp.z)); }
            laneP.AddRange(pts);
            if (pts.Count > 40) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = ctx.OnGround(pts[i].x - sd.x * 2.8f, pts[i].z - sd.z * 2.8f); if (ctx.IsLand(bp.x, bp.z, 2f)) { Bench(mk, solidKit, bp, sd); Clear(bp, 2f); } }
            return pts;
        }

        void PlaceLane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(laneP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.3f; var b = new Vector2(from.x, from.z) + d * (len - stopShort);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Ribbon(ctx, mk, chalk, pts, 1.8f, .05f); foreach (var p in pts) { var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f)); if (!pathCells.TryGetValue(key, out var cell)) pathCells[key] = cell = new List<Vector3>(); cell.Add(p); }
        }

        MeshKit solidKit;

        void DownsPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            solidKit = solid;
            foreach (var (at, r) in PlaceRings) { noGrass.Add(new Vector3(at.x, r, at.y)); ctx.AddKeepout(at.x, at.y, r); }
            ChalkLane(ctx, mk, WestLane); ChalkLane(ctx, mk, NorthWestLane); ChalkLane(ctx, mk, EastLane); ChalkLane(ctx, mk, NorthEastLane);
            BeachHuts(ctx, mk, solid); Allotments(ctx, mk, solid); CarBoot(ctx, mk, solid); CaravanSite(ctx, mk, solid); Toposcope(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Beach Huts
        void BeachHuts(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(HutsAt.x, HutsAt.y); var sea = Vector3.left; var f = new PlaceFrame(g, sea);   // everything faces the sea, west
            // a row of five huts along the back, each a different faded colour, a little deck in front
            for (var k = 0; k < 5; k++)
            {
                var c = G(f.P(-8f + k * 4f, 0, -5f)); var wall = WorldMaterials.Solid(Pastels[k % Pastels.Length]); var trim = WorldMaterials.Solid(Cream);
                mk.Box(wall, c + up * 1.4f, new Vector3(2.8f, 2.4f, 3f), f.r); solid.Box(wall, c + up * 1.3f, new Vector3(2.8f, 2.8f, 3f), f.r); mk.GableRoof(k % 2 == 0 ? slate : terracotta, c + up * 2.6f, new Vector3(3.1f, 0, 3.3f), 1.1f, f.r);
                for (var s = 0; s < 5; s++) if (s % 2 == 0) mk.Box(trim, c + up * 1.4f + f.Fwd * 1.51f + f.Right * (-1.12f + s * .56f), new Vector3(.28f, 2.4f, .02f), f.r);
                mk.Box(WorldMaterials.Solid(Doors[k % Doors.Length]), c + up * 1.15f + f.Fwd * 1.53f, new Vector3(.8f, 1.9f, .03f), f.r); mk.Box(wood, c + up * .15f + f.Fwd * 2.2f, new Vector3(2.8f, .12f, 1.4f), f.r); solid.Box(wood, c + up * .1f + f.Fwd * 2.2f, new Vector3(2.8f, .2f, 1.4f), f.r);
                if (k % 2 == 1) { mk.Box(WorldMaterials.Glow(Lamp, 1.4f), c + up * 2.2f + f.Fwd * 1.56f, new Vector3(.16f, .2f, .1f), f.r); halos.Add(c + up * 2.2f + f.Fwd * 1.8f, .8f, Lamp, .6f); }
            }
            // deckchairs facing the sea, a windbreak, a bucket and spade
            Color[] st = { new Color(.8f, .3f, .28f), new Color(.3f, .45f, .7f), new Color(.85f, .7f, .3f), new Color(.35f, .6f, .45f) };
            for (var k = 0; k < 4; k++) Deckchair(mk, f.P(-5f + k * 2.6f, 0, 2.5f + (k % 2) * .6f), f.Fwd, st[k], k != 1);
            Folk(G(f.P(-5f + 2.6f, 0, 3.1f)) + up * .42f, f.Fwd, Resident.Pose.Sit, new[] { "Same hut every year. Number three. The blue was my mother's idea.", "I've read this page four times. It's a good page.", "The tide's a long way out. It'll think about coming back." }, Local(Residents.Tool.Mug), null, .42f);
            for (var k = 0; k < 4; k++) { var p = G(f.P(7.5f, 0, 1f + k * 1.2f)); mk.Box(wood, p + up * .7f, new Vector3(.06f, 1.4f, .06f), f.r); if (k < 3) { var q = G(f.P(7.5f, 0, 2.2f + k * 1.2f)); mk.Box(WorldMaterials.Solid(st[k]), (p + q) / 2f + up * .75f, new Vector3(.03f, 1f, 1.2f), Quaternion.LookRotation(q - p)); Col((p + q) / 2f + up * .7f, new Vector3(.1f, 1.3f, 1.2f), Quaternion.LookRotation(q - p)); } }
            // the ice cream van, parked clear of the end of the huts' decks
            var vp = G(f.P(12.5f, 0, -3f)); var vr = Quaternion.LookRotation(-f.Right); var vf = vr * Vector3.forward; mk.Box(cream, vp + up * 1.4f, new Vector3(2f, 2f, 4.2f), vr); mk.Box(WorldMaterials.Solid(new Color(.95f, .65f, .7f)), vp + up * .7f, new Vector3(2.04f, .6f, 4.24f), vr); solid.Box(cream, vp + up * 1.2f, new Vector3(2f, 2.4f, 4.2f), vr);
            mk.Box(glassDark, vp + up * 1.6f + vr * Vector3.right * 1.01f, new Vector3(.02f, .8f, 1.6f), vr); mk.Box(WorldMaterials.Glow(Lamp, 1.2f), vp + up * 1.6f + vr * Vector3.right * 1.02f - vf * .9f, new Vector3(.02f, .6f, .5f), vr); foreach (var e in new[] { -1f, 1f }) foreach (var z in new[] { -1.3f, 1.3f }) mk.Prism(iron, vp + vr * new Vector3(e * .9f, .35f, z), vr * Vector3.right * e, .35f, .2f, 10);
            var cone = new MeshKit(); cone.Cone(WorldMaterials.Solid(new Color(.85f, .65f, .4f)), new Vector3(0, .9f, 0), .32f, -.9f, 7); cone.Blob(WorldMaterials.Solid(new Color(.98f, .95f, .88f)), new Vector3(0, 1.1f, 0), Vector3.one * .38f, prng, .1f); cone.Box(WorldMaterials.Solid(new Color(.4f, .28f, .2f)), new Vector3(.15f, 1.45f, 0), new Vector3(.08f, .45f, .08f), Quaternion.Euler(0, 0, -25f));
            var cgo = cone.BuildSingle("Soft giant cone", plife.transform, false); cgo.transform.position = vp + up * 2.4f;
            ArtistBits.Label(ctx.Root, "ICES", vp + up * 2.15f + vr * Vector3.right * 1.03f, vr * Vector3.right, .22f, new Color(.7f, .25f, .3f), TextAnchor.MiddleCenter);
            Folk(G(vp + vr * Vector3.right * 2f), -(vr * Vector3.right), Resident.Pose.Stand, new[] { "A ninety-nine, please. They were ninety-nine once. I think.", "He only has vanilla. It's the right amount of choice." }, Local());
            // pebbles to pocket and skim
            for (var k = 0; k < 5; k++) { var pm = new MeshKit(); pm.Blob(WorldMaterials.Solid(k % 2 == 0 ? new Color(.78f, .76f, .71f) : new Color(.55f, .53f, .5f)), Vector3.zero, new Vector3(.09f, .04f, .07f), prng, .2f); Thing(pm, "Pebble", "a flat pebble", G(f.P(-1f + k * .5f, 0, 6f + (k % 2) * .4f)) + up * .06f, Quaternion.identity, .3f, new Vector3(.2f, .1f, .16f), g.y - 12f); }
            Bench(mk, solid, G(f.P(3f, 0, 7.5f)), f.Fwd);
            PaintedSign(mk, f.P(-11.5f, 0, -1f), -sea, "BEACH HUTS\nno. 1–5", 2f);
            PlaceLane(ctx, mk, f.P(0, 0, -1f)); Clear(g, 12f);
            Shot(ctx, "the beach huts", f.P(8f, 3f, 13f), G(f.P(0, 0, -4f)) + up * 1.4f);
        }

        // ---------------------------------------------------------------- the Allotments
        void Allotments(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PlotsAt.x, PlotsAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); var soil = WorldMaterials.Solid(new Color(.36f, .27f, .2f)); var green = WorldMaterials.Solid(new Color(.38f, .56f, .3f));
            // six beds in two rows, a grass walk between; each grows something different
            for (var k = 0; k < 6; k++)
            {
                var c = G(f.P(-8f + (k % 3) * 8f, 0, -5.5f + (k / 3) * 8f)); mk.Box(soil, c + up * .08f, new Vector3(6f, .2f, 5f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(wood, c + up * .12f + f.Fwd * e * 2.5f, new Vector3(6.1f, .26f, .08f), f.r); mk.Box(wood, c + up * .12f + f.Right * e * 3f, new Vector3(.08f, .26f, 5.1f), f.r); }
                for (var row = 0; row < 4; row++) for (var s = 0; s < 6; s++)
                {
                    var p = c + f.r * new Vector3(-2.4f + s * .95f, .2f, -1.8f + row * 1.2f);
                    switch (k)
                    {
                        case 0: soft.Blob(green, p + up * .15f, new Vector3(.3f, .22f, .3f), prng, .3f); break;                                                                                 // cabbages
                        case 1: if (s % 2 == 0) { mk.Box(wood, p + up * 1f, new Vector3(.04f, 2f, .04f), f.r); soft.Blob(green, p + up * 1.1f, new Vector3(.25f, .8f, .25f), prng, .3f); } break;   // beans
                        case 2: soft.Blob(WorldMaterials.Solid(new Color(.5f, .62f, .3f)), p + up * .2f, new Vector3(.12f, .3f, .12f), prng, .2f); break;                                        // leeks
                        case 3: if (row % 2 == 0 && s % 2 == 0) { mk.Prism(green, p, up, .04f, 2.1f, 5); mk.Prism(WorldMaterials.Solid(new Color(.95f, .78f, .2f)), p + up * 2.1f - f.Fwd * .05f, f.Fwd, .3f, .06f, 10); mk.Prism(WorldMaterials.Solid(new Color(.35f, .25f, .15f)), p + up * 2.1f + f.Fwd * .02f, f.Fwd, .13f, .02f, 8); } break;   // sunflowers
                        case 4: if ((row + s) % 3 == 0) soft.Blob(green, p + up * .12f, new Vector3(.5f, .15f, .5f), prng, .35f); break;                                                         // pumpkin vines
                        default: soft.Blob(WorldMaterials.Solid(Flowers[(row + s) % Flowers.Length]), p + up * .3f, new Vector3(.14f, .14f, .14f), prng, .2f); soft.Blob(green, p + up * .12f, new Vector3(.16f, .14f, .16f), prng, .2f); break;   // cut flowers
                    }
                }
            }
            // two sheds and a greenhouse along the back
            for (var k = 0; k < 2; k++) { var c = G(f.P(-9f + k * 7f, 0, -11f)); mk.Box(k == 0 ? WorldMaterials.Solid(Pastels[2]) : darkWood, c + up * 1.1f, new Vector3(2.4f, 2.2f, 2f), f.r); solid.Box(darkWood, c + up * 1.1f, new Vector3(2.4f, 2.2f, 2f), f.r); mk.GableRoof(slate, c + up * 2.2f, new Vector3(2.7f, 0, 2.3f), .7f, f.r); mk.Box(WorldMaterials.Solid(Doors[k + 1]), c + up * 1f + f.Fwd * 1.02f, new Vector3(.8f, 1.8f, .03f), f.r); }
            var gh = G(f.P(7f, 0, -11f)); var glass = WorldMaterials.Glow(new Color(.8f, .95f, .85f), .35f); mk.Box(glass, gh + up * 1.1f, new Vector3(4f, 2.2f, 2.6f), f.r); mk.GableRoof(glass, gh + up * 2.2f, new Vector3(4.1f, 0, 2.7f), .9f, f.r); solid.Box(glass, gh + up * 1.1f, new Vector3(4f, 2.2f, 2.6f), f.r); foreach (var e in new[] { -2f, -.67f, .67f, 2f }) mk.Box(cream, gh + up * 1.1f + f.Right * e + f.Fwd * 1.31f, new Vector3(.06f, 2.2f, .04f), f.r); halos.Add(gh + up * 1.4f, 2.4f, new Color(.8f, 1f, .85f), .3f);
            // a water butt, a wheelbarrow, a bench, a lantern on the shed
            var wb = G(f.P(-5.5f, 0, -10f)); mk.Prism(WorldMaterials.Solid(new Color(.25f, .4f, .32f)), wb, up, .45f, 1.1f, 10); ArtistBits.PostCollider(rootT, wb, 1.1f, .45f);
            var br = G(f.P(1f, 0, -1.5f)); var bq = Quaternion.LookRotation(Quaternion.Euler(0, 30f, 0) * f.Fwd); mk.Box(WorldMaterials.Solid(new Color(.3f, .5f, .4f)), br + up * .45f, new Vector3(.7f, .3f, 1f), bq); mk.Prism(iron, br + up * .2f + bq * new Vector3(-.06f, 0, .6f), bq * Vector3.right, .2f, .12f, 8); foreach (var e in new[] { -.3f, .3f }) mk.Box(wood, br + up * .5f + bq * new Vector3(e, 0, -.9f), new Vector3(.04f, .04f, 1f), bq); Col(br + up * .35f, new Vector3(.7f, .7f, 1.6f), bq);
            Bench(mk, solid, G(f.P(-1f, 0, -9.6f)), f.Fwd); LaneLantern(mk, G(f.P(-11.5f, 0, -9.5f)));
            // a watering can and pumpkins to carry
            var wc = new MeshKit(); var tin = WorldMaterials.Solid(new Color(.55f, .65f, .6f), .6f); wc.Prism(tin, Vector3.zero, Vector3.up, .14f, .28f, 8); wc.Prism(tin, new Vector3(.12f, .14f, 0), new Vector3(1f, .7f, 0).normalized, .03f, .35f, 5); wc.Box(tin, new Vector3(-.2f, .2f, 0), new Vector3(.03f, .2f, .04f), Quaternion.identity); wc.Box(tin, new Vector3(-.12f, .3f, 0), new Vector3(.16f, .03f, .04f), Quaternion.identity);
            Thing(wc, "Watering can", "the watering can", G(f.P(-4.3f, 0, -9.6f)) + up * .05f, f.r, .8f, new Vector3(.5f, .35f, .3f), g.y - 12f);
            for (var k = 0; k < 3; k++) { var pm = new MeshKit(); pm.Blob(WorldMaterials.Solid(new Color(.9f, .5f, .15f)), new Vector3(0, .18f, 0), new Vector3(.26f, .2f, .26f), prng, .12f); pm.Box(green, new Vector3(0, .4f, 0), new Vector3(.05f, .1f, .05f), Quaternion.identity); Thing(pm, "Pumpkin", "a pumpkin", G(f.P(-1.5f + k * 1.6f, 0, 2.2f + (k % 2) * 1.5f)) + up * .2f, Quaternion.identity, 1.5f, new Vector3(.5f, .45f, .5f), g.y - 12f); }
            Folk(G(f.P(-8f, 0, -1.6f)), f.Right, Resident.Pose.Work, new[] { "Slugs had the lot last year. This year I've had words with them.", "Take a pumpkin. I grew nine and I live alone.", "You want to dig when it's dry and plant when it's wet. Or the other way. It's worked so far." }, Local(Residents.Tool.Rake, true)).workRate = .7f;
            Folk(G(f.P(8f, 0, 6.8f)), -f.Fwd, Resident.Pose.Work, new[] { "They're cut flowers, but I never cut them.", "The sunflowers follow the sun. On a cloudy day they just look confused." }, Local(Residents.Tool.Hose)).workRate = .5f;
            PaintedSign(mk, f.P(6f, 0, 13f), f.Fwd, "ALLOTMENTS\nplots 1–6", 2f);
            PlaceLane(ctx, mk, f.P(0, 0, 10f)); Clear(g, 14f);
            Shot(ctx, "the allotments", f.P(-10f, 4f, 15f), g + up * .6f);
        }

        // ---------------------------------------------------------------- the Car Boot Sale
        void CarBoot(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BootAt.x, BootAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g)));
            Color[] paint = { new Color(.6f, .7f, .65f), new Color(.75f, .6f, .45f), new Color(.5f, .55f, .7f), new Color(.85f, .82f, .7f), new Color(.7f, .45f, .4f) };
            string[][] patter =
            {
                new[] { "Everything's a pound. Except that. That's two.", "It works. It worked. It will have worked.", "That was my nan's. She'd want you to have it for a pound." },
                new[] { "All the records are scratched in the same place. It's the good bit.", "No, I don't know what it's for either. Pick it up. Feels right, doesn't it?" },
                new[] { "I come every Sunday. It's always Sunday.", "Half of this I bought here last week." },
            };
            var n = 0;
            for (var k = 0; k < 5; k++)
            {
                // two rows facing each other across an aisle
                var row = k < 3 ? -1f : 1f; var cp = G(f.P(k < 3 ? -7f + k * 7f : -3.5f + (k - 3) * 7f, 0, row * 7.5f)); var dir = f.Fwd * row; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir); var body = WorldMaterials.Solid(paint[k], .5f);
                mk.Box(body, cp + up * .7f, new Vector3(1.7f, .7f, 3.8f), rot); mk.Box(body, cp + up * 1.35f + dir * .2f, new Vector3(1.6f, .6f, 1.9f), rot); mk.Box(glassDark, cp + up * 1.35f + dir * 1.16f, new Vector3(1.4f, .5f, .02f), rot); mk.Box(body, cp + up * 1.9f - dir * 1.5f, new Vector3(1.6f, .06f, 1.1f), rot * Quaternion.Euler(-25f, 0, 0));
                foreach (var e in new[] { -1f, 1f }) foreach (var z in new[] { -1.2f, 1.2f }) mk.Prism(iron, cp + side * e * .78f + dir * z + up * .33f, side * e, .33f, .2f, 10); solid.Box(body, cp + up * .9f, new Vector3(1.7f, 1.8f, 3.8f), rot);
                var tp = G(cp - dir * 3.2f); TrestleTable(mk, tp, rot);
                // things on the table: each one can be carried off
                for (var s = 0; s < 3; s++)
                {
                    var at = tp + up * .78f + rot * new Vector3(-.7f + s * .7f, 0, 0); var m = new MeshKit(); string name, thing; var size = new Vector3(.3f, .3f, .3f);
                    switch (n % 8)
                    {
                        case 0: m.Blob(WorldMaterials.Solid(new Color(.85f, .8f, .65f)), new Vector3(0, .11f, 0), new Vector3(.13f, .11f, .13f), prng, .05f); m.Prism(WorldMaterials.Solid(new Color(.85f, .8f, .65f)), new Vector3(.1f, .1f, 0), new Vector3(1f, .5f, 0).normalized, .025f, .15f, 5); m.Box(WorldMaterials.Solid(new Color(.85f, .8f, .65f)), new Vector3(-.15f, .12f, 0), new Vector3(.03f, .12f, .03f), Quaternion.identity); name = "Teapot"; thing = "a teapot with a chipped spout"; break;
                        case 1: m.Prism(iron, Vector3.zero, Vector3.up, .08f, .03f, 8); m.Prism(iron, Vector3.zero, Vector3.up, .015f, .3f, 5); m.Cone(WorldMaterials.Glow(Lamp, .6f), new Vector3(0, .26f, 0), .14f, .16f, 8); name = "Table lamp"; thing = "a little lamp that still works"; size = new Vector3(.28f, .44f, .28f); break;
                        case 2: m.Prism(darkWood, Vector3.zero, Vector3.up, .08f, .03f, 8); m.Prism(iron, Vector3.zero, Vector3.up, .012f, .14f, 5); m.Blob(WorldMaterials.Solid(new Color(.35f, .55f, .7f)), new Vector3(0, .26f, 0), Vector3.one * .13f, prng, 0f); m.Blob(WorldMaterials.Solid(new Color(.55f, .65f, .4f)), new Vector3(.04f, .28f, .05f), new Vector3(.09f, .08f, .09f), prng, .3f); name = "Globe"; thing = "a globe of somewhere else"; size = new Vector3(.28f, .42f, .28f); break;
                        case 3: m.Box(WorldMaterials.Solid(Pastels[n % Pastels.Length]), new Vector3(0, .01f, 0), new Vector3(.31f, .015f, .31f), Quaternion.identity); m.Prism(board, new Vector3(.03f, .02f, 0), Vector3.up, .13f, .008f, 14); name = "Record"; thing = "a record with no label"; size = new Vector3(.32f, .05f, .32f); break;
                        case 4: m.Box(WorldMaterials.Solid(new Color(.8f, .8f, .82f), .7f), new Vector3(0, .1f, 0), new Vector3(.28f, .2f, .16f), Quaternion.identity); m.Box(board, new Vector3(0, .2f, 0), new Vector3(.2f, .01f, .03f), Quaternion.identity); m.Box(WorldMaterials.Solid(new Color(.8f, .65f, .4f)), new Vector3(.05f, .22f, 0), new Vector3(.1f, .1f, .02f), Quaternion.identity); name = "Toaster"; thing = "a toaster (one side only)"; break;
                        case 5: m.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .8f), Vector3.zero, Vector3.up, .07f, .04f, 8); m.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .8f), new Vector3(0, .04f, 0), Vector3.up, .02f, .1f, 5); m.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .8f), new Vector3(0, .14f, 0), Vector3.up, .05f, .14f, 8, 1.6f); name = "Trophy"; thing = "a trophy for third place"; size = new Vector3(.2f, .3f, .2f); break;
                        case 6: m.Box(WorldMaterials.Solid(new Color(.6f, .3f, .28f)), new Vector3(0, .1f, 0), new Vector3(.2f, .2f, .04f), Quaternion.identity); m.Box(cream, new Vector3(0, .1f, .001f), new Vector3(.17f, .17f, .04f), Quaternion.identity); m.Box(WorldMaterials.Solid(new Color(.5f, .6f, .7f)), new Vector3(0, .12f, .002f), new Vector3(.13f, .09f, .04f), Quaternion.identity); name = "Framed photo"; thing = "a photo of someone's holiday"; size = new Vector3(.22f, .22f, .08f); break;
                        default: m.Box(WorldMaterials.Solid(new Color(.25f, .25f, .28f)), new Vector3(0, .05f, 0), new Vector3(.16f, .1f, .1f), Quaternion.identity); m.Box(WorldMaterials.Solid(Pastels[(n + 2) % Pastels.Length]), new Vector3(0, .05f, .051f), new Vector3(.12f, .06f, .005f), Quaternion.identity); name = "Mixtape"; thing = "a mixtape, side B unlabelled"; size = new Vector3(.18f, .12f, .12f); break;
                    }
                    Thing(m, name, thing, at, rot, .5f, size, g.y - 12f); n++;
                }
                if (k < 3) { var sp = G(cp - dir * 1.2f + side * 1.5f); Deckchair(mk, sp, -dir, paint[(k + 2) % 5], false); Folk(sp + up * .42f, -dir, Resident.Pose.Sit, patter[k], Local(k == 1 ? Residents.Tool.Mug : Residents.Tool.None), null, .42f); }
            }
            Folk(G(f.P(-2.5f, 0, .8f)), -f.Fwd, Resident.Pose.Stand, new[] { "I'm only looking. I've been only looking since ten.", "I had one of these. I think I had this one." }, Local()); Folk(G(f.P(4.5f, 0, -1f)), f.Fwd, Resident.Pose.Stand, new[] { "Is that a pound for the box or a pound each?", "You don't see these any more. You didn't see them then, to be fair." }, Local());
            // bunting across the aisle, a tea table
            var b0 = G(f.P(-12f, 0, 0)); var b1 = G(f.P(12f, 0, 0)); foreach (var p in new[] { b0, b1 }) { mk.Box(wood, p + up * 1.6f, new Vector3(.1f, 3.2f, .1f), f.r); ArtistBits.PostCollider(rootT, p, 3.2f, .1f); } Bunting(mk, b0 + up * 3.1f, b1 + up * 3.1f, Pastels, 0, .9f);
            var tt = G(f.P(10.5f, 0, -3f)); TrestleTable(mk, tt, Quaternion.LookRotation(-f.Right), 1.8f); mk.Prism(WorldMaterials.Solid(new Color(.75f, .75f, .78f), .7f), tt + up * .78f, up, .16f, .4f, 10); mk.Blob(WorldMaterials.Glow(Lamp, 1.2f), tt + up * 1.25f, Vector3.one * .05f, prng, 0f); ArtistBits.Label(ctx.Root, "TEAS 20p", tt + up * .5f - f.Right * .42f, -f.Right, .12f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, g + up * 3f, Lamp, 18f, .9f);
            PaintedSign(mk, f.P(7f, 0, 15.5f), f.Fwd, "CAR BOOT SALE\nSundays (it is always Sunday)", 3f, .13f);
            PlaceLane(ctx, mk, f.P(0, 0, 11f)); Clear(g, 14f);
            Shot(ctx, "the car boot sale", f.P(-11f, 4f, 16f), g + up * .8f);
        }

        // ---------------------------------------------------------------- the Caravan Site
        void CaravanSite(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(VansAt.x, VansAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g)));
            // three old caravans in an arc, doors to the middle: cream above, a faded colour band below, a lit window
            for (var k = 0; k < 3; k++)
            {
                var d = Quaternion.Euler(0, -55f + k * 55f, 0) * -f.Fwd; var cp = G(g + d * 8.5f); var rot = Quaternion.LookRotation(Vector3.Cross(up, d)); var inward = -d; var band = WorldMaterials.Solid(Pastels[(k * 2 + 1) % Pastels.Length]);
                mk.Box(cream, cp + up * 1.75f, new Vector3(2.1f, 1.5f, 4.6f), rot); mk.Box(band, cp + up * .95f, new Vector3(2.12f, .5f, 4.62f), rot); mk.Box(cream, cp + up * 2.55f, new Vector3(1.9f, .2f, 4.2f), rot); solid.Box(cream, cp + up * 1.5f, new Vector3(2.1f, 2.2f, 4.6f), rot);
                foreach (var e in new[] { -1f, 1f }) mk.Prism(iron, cp + rot * new Vector3(e * .95f, .38f, -.3f), rot * Vector3.right * e, .38f, .2f, 10); mk.Box(iron, cp + up * .55f + rot * Vector3.forward * 2.8f, new Vector3(.08f, .08f, 1.2f), rot); mk.Box(iron, cp + up * .28f + rot * Vector3.forward * 3.3f, new Vector3(.06f, .55f, .06f), rot);
                mk.Box(WorldMaterials.Glow(Lamp, 1.3f), cp + up * 1.9f + inward * 1.06f + rot * Vector3.forward * 1f, new Vector3(1.3f, .7f, .02f), Quaternion.LookRotation(inward)); mk.Box(WorldMaterials.Solid(Doors[k]), cp + up * 1.45f + inward * 1.07f - rot * Vector3.forward * 1f, new Vector3(.7f, 1.7f, .02f), Quaternion.LookRotation(inward)); mk.Box(wood, cp + up * .35f + inward * 1.4f - rot * Vector3.forward * 1f, new Vector3(.8f, .2f, .5f), Quaternion.LookRotation(inward)); Col(cp + up * .3f + inward * 1.4f - rot * Vector3.forward * 1f, new Vector3(.8f, .3f, .5f), Quaternion.LookRotation(inward));
                // a striped awning on two poles
                for (var s = 0; s < 6; s++) mk.Box(s % 2 == 0 ? band : cream, cp + up * 2.35f + inward * 2.1f + rot * Vector3.forward * (.2f + s * .4f), new Vector3(.4f, .04f, 2.2f), Quaternion.LookRotation(inward) * Quaternion.Euler(10f, 90f, 0) * Quaternion.Euler(0, -90f, 0));
                foreach (var e in new[] { .1f, 2.3f }) { var pp = G(cp + inward * 3.1f + rot * Vector3.forward * e); mk.Box(cream, pp + up * 1.05f, new Vector3(.05f, 2.1f, .05f), rot); ArtistBits.PostCollider(rootT, pp, 2.1f, .06f); }
                halos.Add(cp + up * 1.9f + inward * 1.4f, 1.2f, Lamp, .5f);
            }
            // deckchairs, a folding table with a radio, a washing line, swingball
            Color[] st = { new Color(.8f, .3f, .28f), new Color(.3f, .45f, .7f), new Color(.85f, .7f, .3f) };
            for (var k = 0; k < 3; k++) { var p = f.P(-2.4f + k * 2.4f, 0, -1.5f - (k == 1 ? .6f : 0)); Deckchair(mk, p, f.Fwd, st[k], k != 0); }
            Folk(G(f.P(-2.4f, 0, -1.5f)) + up * .42f, f.Fwd, Resident.Pose.Sit, new[] { "Two weeks every August. Same pitch. The view's not changed and neither have I.", "Tea's stewed. There's more stewing.", "Have a go at the swingball. Mind the washing." }, Local(Residents.Tool.Mug), null, .42f);
            var tb = G(f.P(3.2f, 0, 1.4f)); mk.Box(cream, tb + up * .65f, new Vector3(.9f, .04f, .6f), f.r); foreach (var e in new[] { -.35f, .35f }) mk.Box(iron, tb + up * .32f + f.Right * e, new Vector3(.03f, .65f, .5f), f.r); Col(tb + up * .35f, new Vector3(.9f, .7f, .6f), f.r);
            var rm = new MeshKit(); rm.Box(WorldMaterials.Solid(new Color(.7f, .4f, .3f)), new Vector3(0, .1f, 0), new Vector3(.32f, .2f, .1f), Quaternion.identity); rm.Box(cream, new Vector3(-.06f, .1f, .051f), new Vector3(.14f, .14f, .005f), Quaternion.identity); rm.Box(iron, new Vector3(.12f, .32f, 0), new Vector3(.01f, .3f, .01f), Quaternion.Euler(0, 0, -15f));
            var rgo = rm.BuildSingle("Caravan radio", plife.transform, false); rgo.transform.SetPositionAndRotation(tb + up * .67f, f.r); string[] radio = { "The shipping forecast, for sea areas you've never heard of.", "A song you know all the words to and none of the title.", "Someone reads the cricket scores from a match that ended years ago.", "Static, and under it a guitar, very far off." }; var rn = 0;
            Interact.Toy(rgo, Toy.Kind.Custom, "Turn the radio dial", new Vector3(.5f, .5f, .4f)).onUse = _ => Say(plife.transform, tb + up * 1.8f, radio[rn++ % radio.Length], 5f);
            var w0 = G(f.P(-9f, 0, 3.5f)); var w1 = G(f.P(-4f, 0, 6f)); foreach (var p in new[] { w0, w1 }) { mk.Box(wood, p + up * 1f, new Vector3(.08f, 2f, .08f), f.r); ArtistBits.PostCollider(rootT, p, 2f, .08f); } mk.Box(iron, (w0 + w1) / 2f + up * 1.95f, new Vector3(.015f, .015f, Vector3.Distance(w0, w1)), Quaternion.LookRotation(w1 - w0));
            var wm = new Material(WindMaterial(.5f, 1f)); for (var k = 0; k < 5; k++) { var p = Vector3.Lerp(w0, w1, (k + .5f) / 5f) + up * 1.55f; var wq = Quaternion.LookRotation(w1 - w0); mk.Box(WorldMaterials.Solid(k % 2 == 0 ? Cream : Pastels[k % Pastels.Length]), p, new Vector3(.03f, k % 2 == 0 ? .8f : .6f, .7f), wq); }
            // swingball: a ball on a string round a pole; hit it and it whirls
            var sp = G(f.P(6.5f, 0, 5f)); mk.Prism(iron, sp, up, .03f, 1.7f, 5); ArtistBits.PostCollider(rootT, sp, 1.7f, .05f); var sb = new MeshKit(); sb.Box(iron, new Vector3(.45f, 0, 0), new Vector3(.9f, .01f, .01f), Quaternion.identity); sb.Blob(WorldMaterials.Solid(new Color(.85f, .9f, .3f)), new Vector3(.9f, 0, 0), Vector3.one * .07f, prng, 0f);
            var sgo = sb.BuildSingle("Swingball", plife.transform, false); sgo.transform.position = sp + up * 1.6f; var stt = Interact.Toy(sgo, Toy.Kind.Spin, "Hit the swingball", new Vector3(1.9f, .6f, 1.9f)); stt.axis = Vector3.up; stt.amount = 1440f; stt.seconds = 4f;
            var kid = Local(); kid.height = .78f; Folk(G(sp + f.Right * 1.6f), -f.Right, Resident.Pose.Wave, new[] { "My go! It's been my go for ages!", "You have to hit it THAT way. No. THAT way." }, kid);
            ArtistBits.PointLight(ctx.Root, g + up * 2.6f, Lamp, 16f, 1f);
            PaintedSign(mk, f.P(7.5f, 0, 12f), f.Fwd, "CARAVAN SITE\nno vacancies, but come in", 2.8f, .13f);
            PlaceLane(ctx, mk, f.P(0, 0, 9f)); Clear(g, 13f);
            Shot(ctx, "the caravan site", f.P(-8f, 3.4f, 13f), g + up * 1f - f.Fwd * 3f);
        }

        // ---------------------------------------------------------------- the Toposcope
        void Toposcope(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TopoAt.x, TopoAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g)));
            mk.Prism(stone, g - up * .3f, up, 1.9f, .45f, 10); solid.Prism(stone, g - up * .3f, up, 1.9f, .45f, 10); mk.Prism(stone, g + up * .15f, up, .5f, .85f, 8, .85f); mk.Prism(WorldMaterials.Solid(new Color(.8f, .65f, .3f), .8f), g + up * 1f, up, .55f, .04f, 16); ArtistBits.PostCollider(rootT, g, 1f, .55f);
            for (var k = 0; k < 8; k++) mk.Box(board, g + up * 1.045f + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .3f, new Vector3(.015f, .005f, .4f), Quaternion.Euler(0, k * 45f, 0));
            var tgo = new GameObject("Toposcope"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = g + up * 1.1f;
            string[] ways = { "WEST: Lanternwood, white under its own snow.", "NORTH: the Bedroom Pop Meadows, soft and violet.", "EAST: the Small Hours, where it is always night.", "SOUTH-WEST: the causeway, and the Hub beyond it.", "UP: weather. It follows the music here." }; var wn = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Read the toposcope", new Vector3(1.2f, .6f, 1.2f)).onUse = _ => Say(plife.transform, g + up * 2f, ways[wn++ % ways.Length], 5f);
            // a white trig pillar, a bench, a flask someone left
            var tp = G(f.P(-2.8f, 0, -2f)); mk.Prism(cream, tp, up, .28f, 1.25f, 4, .7f, 45f); ArtistBits.PostCollider(rootT, tp, 1.25f, .28f);
            var bp = G(f.P(2.8f, 0, -1.2f)); var bf = FaceTo(bp, g + f.Fwd * 30f - f.Right * 10f); Bench(mk, solid, bp, bf);
            var fm = new MeshKit(); fm.Prism(WorldMaterials.Solid(new Color(.3f, .45f, .4f), .6f), Vector3.zero, Vector3.up, .045f, .24f, 8); fm.Prism(cream, new Vector3(0, .24f, 0), Vector3.up, .04f, .05f, 8); Thing(fm, "Flask", "somebody's flask (still warm)", bp + up * (HumanScale.SeatHeight + .05f) + Vector3.Cross(up, bf) * .7f, Quaternion.identity, .5f, new Vector3(.12f, .3f, .12f), g.y - 12f);
            LaneLantern(mk, G(f.P(-1.5f, 0, 3.4f)));
            Folk(G(f.P(1.2f, 0, 2.6f)), -f.Fwd, Resident.Pose.Stand, new[] { "On a clear day you can see four lands. On a hazy one you can see why you came.", "I walk up most evenings. Someone has to check it's all still there." }, Local());
            Clear(g, 5f);
            Shot(ctx, "the toposcope", f.P(-5f, 2.2f, 7f), g + up * 1f);
        }
    }

    /// The Downs places' holder (nothing here moves by itself: the seats, toys and people look after themselves).
    public class DownsPlacesLife : MonoBehaviour { }
}
