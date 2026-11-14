using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Kingdom of Classic Rock laid out on purpose (the build-land standard). Everything that was here stays where
    /// it was (the village, the Hall, the castle, the legends' song places); two new coast roads close a great loop
    /// round the Kingdom: east from the south shore up to the castle gate, and west from the south shore round to the
    /// western road. Along them, places to stop, with the people of the Kingdom:
    /// the Pier (a promenade, deckchairs, an ice-cream kiosk, a helter-skelter, a puppet booth, a bandstand at the end
    /// over the sea); the Balloon (a tethered balloon that goes up over the whole Kingdom: hop in when it lands);
    /// the Maze (a hedge maze by the castle with a golden guitar at its heart); the Fete (bunting, a coconut shy to
    /// throw at, a tombola to spin, a cake stall, a maypole, hay bales to sit on); the Cricket (a match that has
    /// been going since Tuesday, a pavilion with a veranda, deckchairs on the boundary).
    /// Also: guards at the castle gate, villagers on the green, the publican at the Royal Rock.
    public partial class ClassicKit
    {
        static readonly Vector2 PierAt = new Vector2(7215, 2495), BalloonAt = new Vector2(7385, 2740), MazeAt = new Vector2(7378, 2828), FeteAt = new Vector2(6645, 2650), CricketAt = new Vector2(6745, 2520);
        static readonly Vector2[] CoastEast = { new Vector2(6965, 2425), new Vector2(7080, 2445), new Vector2(7190, 2530), new Vector2(7290, 2620), new Vector2(7335, 2710), new Vector2(7300, 2790), new Vector2(7262, 2832), new Vector2(7200, 2890) };
        static readonly Vector2[] CoastWest = { new Vector2(6900, 2425), new Vector2(6800, 2445), new Vector2(6700, 2475), new Vector2(6620, 2580), new Vector2(6580, 2700), new Vector2(6610, 2790), new Vector2(6680, 2815) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (PierAt, 14f), (BalloonAt, 13f), (MazeAt, 16f), (FeteAt, 20f), (CricketAt, 30f) };

        ClassicPlacesLife plife; System.Random prng; Transform rootT; Vector3 castleAt, castleFace; bool hasCastle;
        readonly List<Vector3> roadPts = new List<Vector3>();
        Material stripeR, stripeW, gilt;

        static readonly Color[] Tops = { new Color(.55f, .45f, .3f), new Color(.75f, .2f, .2f), new Color(.25f, .35f, .55f), new Color(.9f, .88f, .8f), new Color(.3f, .45f, .35f), new Color(.85f, .7f, .3f), new Color(.6f, .4f, .6f) };
        static readonly Color[] Legs = { new Color(.3f, .3f, .34f), new Color(.45f, .38f, .28f), new Color(.2f, .25f, .4f), new Color(.6f, .55f, .45f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Straw, Residents.Hat.Bucket };
        static readonly string[] Subjects =
        {
            "Lovely evening. It's always evening here. Golden, isn't it.", "You'll have heard this one. Everyone has. That's rather the point.", "Mind the sheep, they've right of way.",
            "The Hall's up the hill. Go quietly; they like the quiet.", "They don't write them like this any more. Well. They try.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Legs, Hats, Tops); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Subjects, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);

        void Deckchair(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, int k)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            for (var s = 0; s < 4; s++) mk.Box(s % 2 == 0 ? (k % 2 == 0 ? stripeR : WorldMaterials.Solid(Paint[1])) : stripeW, g + up * .36f + rot * new Vector3(-.24f + s * .16f, 0, .1f), new Vector3(.16f, .04f, .9f), rot * Quaternion.Euler(-12f, 0, 0));
            for (var s = 0; s < 4; s++) mk.Box(s % 2 == 0 ? (k % 2 == 0 ? stripeR : WorldMaterials.Solid(Paint[1])) : stripeW, g + up * .75f + rot * new Vector3(-.24f + s * .16f, 0, -.5f), new Vector3(.16f, .04f, .8f), rot * Quaternion.Euler(-62f, 0, 0));
            foreach (var e in new[] { -.34f, .34f }) mk.Box(wood, g + up * .3f + rot * new Vector3(e, 0, 0), new Vector3(.04f, .6f, 1.1f), rot);
            Col(g + up * .45f - face * .2f, new Vector3(.7f, .9f, 1f), rot); Interact.Seat(plife.transform, g + up * .42f + face * .1f, face, "Sit in the deckchair").seatHeight = .36f;
        }

        void Notice(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.6f, float size = .17f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(white, g + up * 1f + rot * Vector3.right * e * (w / 2f - .1f), new Vector3(.1f, 2f, .1f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .2f, .4f)), g + up * 1.6f, new Vector3(w, .8f, .08f), rot); Col(g + up * 1f, new Vector3(w, 2f, .15f), rot);
            ArtistBits.Label(ctx.Root, text, g + up * 1.62f + face * .05f, face, size, new Color(.98f, .85f, .45f), TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- the coast roads
        /// Pushes a road out of anything already standing (artists' ground, the castle, the song places, the pond).
        void Bend(LandContext ctx, List<Vector3> pts)
        {
            for (var pass = 0; pass < 2; pass++)
                for (var i = 0; i < pts.Count; i++)
                {
                    foreach (var a in ctx.Artists) Push(ctx, pts, i, new Vector2(a.x, a.z), Foot(a) + 9f);
                    foreach (var c in noGrass) if (c.y >= 4f) Push(ctx, pts, i, new Vector2(c.x, c.z), c.y + 5f);
                    if (hasPond) Push(ctx, pts, i, pondC, pondR + 8f);
                }
        }
        static void Push(LandContext ctx, List<Vector3> pts, int i, Vector2 c, float r)
        {
            var v = new Vector2(pts[i].x - c.x, pts[i].z - c.y); var d = v.magnitude; if (d >= r) return;
            v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.y + v.y);
        }

        void CoastRoad(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            // join the roads that are already there at both ends
            Vector2 Join(Vector2 near) { var best = near; var bd = 120f * 120f; foreach (var (p, _) in walkPoints) { var d = (p.x - near.x) * (p.x - near.x) + (p.z - near.y) * (p.z - near.y); if (d < bd) { bd = d; best = new Vector2(p.x, p.z); } } return best; }
            var full = new List<Vector2> { Join(way[0]) }; full.AddRange(way); full.Add(Join(way[way.Length - 1]));
            var pts = Smooth(ctx, full, false, 3f); Bend(ctx, pts); if (pts.Count < 2) return;
            Strip(mk, pts, asphalt, RoadW, .06f);
            var line = WorldMaterials.Solid(new Color(.95f, .94f, .9f)); var up = Vector3.up; var n = 0;
            for (var i = 1; i < pts.Count; i += 2) { var a = pts[i - 1]; var b = pts[i]; if ((b - a).sqrMagnitude < .01f) continue; mk.Box(line, (a + b) / 2f + up * .075f, new Vector3(.15f, .02f, Vector3.Distance(a, b) * .6f), Quaternion.LookRotation(b - a)); }
            for (var i = 6; i < pts.Count; i += 14)
            {
                var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d) * (n % 2 == 0 ? 1f : -1f); var lp = pts[i] + side * (RoadW / 2f + .8f);
                if (ctx.IsLand(lp.x, lp.z, 2f) && !InFootprint(ctx, lp.x, lp.z, 3f)) { var lg = ctx.OnGround(lp.x, lp.z); var crimson = n % 2 == 0; if (n % 3 == 2) roadside.Add((new[] { lg }, 700, .4f, () => Banner(ctx, mk, lg, crimson))); else roadside.Add((new[] { lg }, 700, .4f, () => Lamp(ctx, mk, ctx.Root, lg))); }
                n++;
            }
            foreach (var p in pts) Remember(p, 700);
            roadPts.AddRange(pts);
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + RoadW) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * (RoadW / 2f + .1f); var b = new Vector2(from.x, from.z) + d * (len - stopShort); var side = new Vector2(-d.y, d.x);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + side * Mathf.Min(7f, len * .1f), b }, false, 2f, 1f);
            if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.72f, .66f, .52f)), 2.6f, .05f); foreach (var p in pts) Remember(p, 900);
        }

        /// Called after everything that was already here is placed, and before the countryside is scattered (so the
        /// scatter keeps clear of the new roads and places).
        void KingdomPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            stripeR = WorldMaterials.Solid(new Color(.85f, .18f, .2f)); stripeW = WorldMaterials.Solid(new Color(.96f, .95f, .9f)); gilt = WorldMaterials.Solid(new Color(.95f, .76f, .3f), .85f);
            roadPts.Clear(); rootT = ctx.Root; prng = new System.Random(1966);
            plife = new GameObject("Kingdom places life").AddComponent<ClassicPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            CoastRoad(ctx, mk, CoastEast); CoastRoad(ctx, mk, CoastWest);
            Pier(ctx, mk, solid); Balloon(ctx, mk, solid); Maze(ctx, mk, solid); Fete(ctx, mk, solid); Cricket(ctx, mk, solid); People(ctx, mk, solid);
        }

        // the Pier
        void Pier(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, PierAt, 4f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 45f, g.z + d.z * 45f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.back; var f = new PlaceFrame(g, toSea.normalized); var deck = Mathf.Max(g.y, 2f) + .6f; const float L = 46f, W = 4f;
            Vector3 D(float x, float y, float z) { var p = f.P(x, 0, z); return new Vector3(p.x, deck + y, p.z); }
            // the promenade on the shore, the pier out over the water
            mk.Box(wood, D(0, -.06f, -5f), new Vector3(16f, .12f, 8f), f.r); solid.Box(wood, D(0, -.3f, -5f), new Vector3(16f, .6f, 8f), f.r); mk.Box(stoneM, D(0, -1.4f, -5f), new Vector3(15.6f, 2.6f, 7.6f), f.r);
            for (var z = 0f; z < L; z += 1f) { mk.Box(wood, D(0, -.06f, z - .5f), new Vector3(W, .12f, .92f), f.r); if (((int)z) % 6 == 0) foreach (var e in new[] { -1f, 1f }) { var p = D(e * (W / 2f - .2f), 0, z); mk.Box(dark, p - up * (deck / 2f + .8f), new Vector3(.25f, deck + 1.8f, .25f), f.r); } }
            solid.Box(wood, D(0, -.2f, L / 2f - 1f), new Vector3(W, .4f, L), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(white, D(e * (W / 2f - .05f), .95f, L / 2f - 1f), new Vector3(.08f, .08f, L), f.r); Col(D(e * (W / 2f - .05f), .5f, L / 2f - 1f), new Vector3(.12f, 1f, L), f.r); for (var z = 0f; z < L; z += 3f) mk.Box(white, D(e * (W / 2f - .05f), .47f, z - 1f), new Vector3(.06f, .95f, .06f), f.r); }
            for (var z = 6f; z < L - 8f; z += 12f) foreach (var e in new[] { -1f, 1f }) { var p = D(e * (W / 2f - .1f), 0, z); mk.Box(dark, p + up * 1.6f, new Vector3(.08f, 3.2f, .08f), f.r); mk.Blob(GlowM(new Color(1f, .9f, .65f), 2.2f), p + up * 3.3f, Vector3.one * .18f, ctx.Rng, 0f); halos.Add(p + up * 3.3f, 1.3f, new Color(1f, .9f, .65f), .7f); }
            ArtistBits.PointLight(ctx.Root, D(0, 3f, 12f), new Color(1f, .9f, .65f), 16f, 1.3f); ArtistBits.PointLight(ctx.Root, D(0, 3f, 30f), new Color(1f, .9f, .65f), 16f, 1.3f);
            // the bandstand at the end: eight posts, a pointed roof, a bench round it
            var b = D(0, 0, L + 3f); mk.Prism(wood, b - up * .3f, up, 5f, .3f, 8); solid.Prism(wood, b - up * .5f, up, 5f, .5f, 8); mk.Box(dark, b - up * (deck / 2f + .8f), new Vector3(.5f, deck + 1.8f, .5f), f.r);
            for (var k = 0; k < 8; k++) { var d = f.r * (Quaternion.Euler(0, k * 45f + 22.5f, 0) * Vector3.forward); mk.Box(white, b + d * 4.5f + up * 1.6f, new Vector3(.14f, 3.2f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, b + d * 4.5f, 3.2f, .1f); if (k != 4 && k != 3) { var bd = f.r * (Quaternion.Euler(0, k * 45f + 45f, 0) * Vector3.forward); var bc = b + bd * 4.1f; mk.Box(wood, bc + up * .45f, new Vector3(3f, .07f, .45f), Quaternion.LookRotation(-bd)); Col(bc + up * .5f + bd * .25f, new Vector3(3.2f, 1f, .2f), Quaternion.LookRotation(bd)); Interact.Bench(plife.transform, bc + up * .5f - Vector3.Cross(up, bd) * 1.1f, bc + up * .5f + Vector3.Cross(up, bd) * 1.1f, -bd); } }
            mk.Cone(WorldMaterials.Solid(new Color(.25f, .3f, .55f)), b + up * 3.2f, 5.6f, 2.4f, 8, 22.5f); mk.Blob(gilt, b + up * 5.7f, Vector3.one * .25f, ctx.Rng, 0f); ArtistBits.PointLight(ctx.Root, b + up * 2.8f, new Color(1f, .85f, .55f), 13f, 1.6f);
            // the helter-skelter on the promenade
            var hs = D(-5f, 0, -6f); for (var k = 0; k < 6; k++) mk.Prism(k % 2 == 0 ? stripeR : stripeW, hs + up * (k * 1.5f), up, 2.1f - k * .22f, 1.5f, 12, (2.1f - (k + 1) * .22f) / (2.1f - k * .22f)); mk.Cone(WorldMaterials.Solid(new Color(.25f, .3f, .55f)), hs + up * 9f, 1.3f, 1.6f, 12);
            for (var k = 0; k < 40; k++) { var a = k * 27f; var y = 7.6f - k * .19f; var r = 1.2f + (9f - y) * .16f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward; mk.Box(gilt, hs + d * (r + .45f) + up * y, new Vector3(.9f, .08f, .9f), Quaternion.LookRotation(d) * Quaternion.Euler(0, 0, 8f)); }
            ArtistBits.PostCollider(ctx.Root, hs, 9f, 3.1f);
            // the ice-cream kiosk and its keeper, the puppet booth, a telescope
            var ki = D(5f, 0, -6.5f); mk.Box(stripeW, ki + up * 1.3f, new Vector3(2.6f, 2.6f, 2.2f), f.r); solid.Box(stripeW, ki + up * 1.3f, new Vector3(2.6f, 2.6f, 2.2f), f.r); for (var k = 0; k < 5; k++) mk.Box(k % 2 == 0 ? stripeR : stripeW, ki + up * 2.75f + f.r * new Vector3(-1.04f + k * .52f, 0, .8f), new Vector3(.52f, .06f, 1.6f), f.r * Quaternion.Euler(12f, 0, 0));
            mk.Box(GlowM(new Color(1f, .9f, .65f), 1.5f), ki + up * 1.6f + f.Fwd * 1.11f, new Vector3(1.6f, .8f, .02f), f.r); ArtistBits.Label(ctx.Root, "ICES", ki + up * 2.3f + f.Fwd * 1.12f, f.Fwd, .3f, new Color(.8f, .15f, .2f), TextAnchor.MiddleCenter);
            Folk(ki + f.Fwd * 1.7f + f.Right * 1.6f, f.Fwd, Resident.Pose.Wave, new[] { "Ninety-nine? With a flake? It's the law.", "Mind the gulls. They've read the menu." }, Look(Residents.Tool.None, true));
            var pb = D(1.5f, 0, -8f); mk.Box(stripeR, pb + up * 1.3f, new Vector3(1.4f, 2.6f, 1f), f.r); for (var k = 0; k < 3; k++) mk.Box(stripeW, pb + up * 1.3f + f.r * new Vector3(-.46f + k * .46f, 0, .51f), new Vector3(.2f, 2.6f, .02f), f.r); mk.Box(dark, pb + up * 1.9f + f.Fwd * .52f, new Vector3(1f, .8f, .02f), f.r); Col(pb + up * 1.3f, new Vector3(1.4f, 2.6f, 1f), f.r);
            var pup = new MeshKit(); foreach (var e in new[] { -.25f, .25f }) { pup.Blob(WorldMaterials.Solid(e < 0 ? Paint[0] : Paint[2]), new Vector3(e, 0, 0), new Vector3(.12f, .18f, .1f), prng, 0f); pup.Blob(WorldMaterials.Solid(new Color(.95f, .75f, .6f)), new Vector3(e, .24f, 0), Vector3.one * .1f, prng, 0f); }
            var pgo = pup.BuildSingle("Puppets", plife.transform, false); pgo.transform.SetPositionAndRotation(pb + up * 1.75f + f.Fwd * .56f, f.r); var pt = Interact.Toy(pgo, Toy.Kind.Bob, "Watch the show", new Vector3(1.2f, 1f, .6f)); pt.amount = .18f; pt.seconds = .5f;
            var tp = D(-1.4f, 0, 8f); mk.Prism(metal, tp, up, .06f, 1.3f, 6); ArtistBits.PostCollider(ctx.Root, tp, 1.3f, .1f); var tel = new MeshKit(); tel.Prism(gilt, new Vector3(0, 0, -.5f), Vector3.forward, .09f, 1f, 8, .8f); var tgo = tel.BuildSingle("Telescope", plife.transform, false); tgo.transform.SetPositionAndRotation(tp + up * 1.4f, Quaternion.LookRotation(f.Fwd)); var tt = Interact.Toy(tgo, Toy.Kind.Spin, "Swing the telescope", new Vector3(.5f, .5f, 1.2f)); tt.amount = 360f; tt.seconds = 4f;
            // deckchairs along the front of the promenade, two taken
            for (var k = 0; k < 6; k++) { var c = D(-6.5f + k * 2.6f, 0, -2.2f); var gch = new Vector3(c.x, deck, c.z); var rot = f.r; for (var s = 0; s < 4; s++) mk.Box(s % 2 == 0 ? (k % 2 == 0 ? stripeR : WorldMaterials.Solid(Paint[1])) : stripeW, gch + up * .36f + rot * new Vector3(-.24f + s * .16f, 0, .1f), new Vector3(.16f, .04f, .9f), rot * Quaternion.Euler(-12f, 0, 0)); for (var s = 0; s < 4; s++) mk.Box(s % 2 == 0 ? (k % 2 == 0 ? stripeR : WorldMaterials.Solid(Paint[1])) : stripeW, gch + up * .75f + rot * new Vector3(-.24f + s * .16f, 0, -.5f), new Vector3(.16f, .04f, .8f), rot * Quaternion.Euler(-62f, 0, 0)); Col(gch + up * .45f - f.Fwd * .2f, new Vector3(.7f, .9f, 1f), rot);
                if (k == 1 || k == 4) Folk(gch + up * .42f + f.Fwd * .1f, f.Fwd, Resident.Pose.Sit, k == 1 ? new[] { "Wake me when the tide's in. Or out. Either.", "Best seat in the Kingdom, and it's fourpence." } : new[] { "We came for the day in 1963. Lovely day. Still going." }, null, null, .36f); else Interact.Seat(plife.transform, gch + up * .42f + f.Fwd * .1f, f.Fwd, "Sit in the deckchair").seatHeight = .36f; }
            Folk(D(1.6f, 0, 20f), -f.Right, Resident.Pose.Lean, new[] { "Band's on at eight. Has been for years. Good band.", "You can see the whole coast from the end. Go on." }, new Residents.Look { top = new Color(.12f, .12f, .15f), bottom = new Color(.2f, .25f, .4f), hair = new Color(.1f, .08f, .07f), skin = new Color(.9f, .72f, .6f) });
            Notice(ctx, mk, f.P(-9.5f, 0, -11f), -f.Fwd, "THE PIER\nbandstand, ices, sea", 3f);
            Lane(ctx, mk, f.P(0, 0, -10f)); Ring(g, 12f);
            Shot(ctx, "the pier", f.P(14f, deck - g.y + 3f, -17f), D(0, 2f, 14f));
            Shot(ctx, "the bandstand at the end of the pier", D(1f, 1.7f, L - 6f), b + up * 2f);
        }

        // the Balloon: goes up over the whole Kingdom and comes down again; hop in when it lands
        void Balloon(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BalloonAt.x, BalloonAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            mk.Prism(WorldMaterials.Solid(new Color(.6f, .72f, .4f)), g - up * .4f, up, 9f, .45f, 20);
            var bm = new MeshKit(); var basket = WorldMaterials.Solid(new Color(.6f, .42f, .25f));
            bm.Box(basket, new Vector3(0, .1f, 0), new Vector3(1.8f, .2f, 1.8f), Quaternion.identity); foreach (var (x, z, sx, sz) in new[] { (0f, .85f, 1.8f, .1f), (0f, -.85f, 1.8f, .1f), (.85f, 0f, .1f, 1.8f), (-.85f, 0f, .1f, .6f) }) bm.Box(basket, new Vector3(x, .65f, z), new Vector3(sx, 1.1f, sz), Quaternion.identity);
            bm.Box(basket, new Vector3(.45f, .5f, 0), new Vector3(.5f, .1f, 1.5f), Quaternion.identity);
            foreach (var (x, z) in new[] { (-.8f, -.8f), (.8f, -.8f), (.8f, .8f), (-.8f, .8f) }) bm.Box(dark, new Vector3(x * 1.6f, 3.4f, z * 1.6f), new Vector3(.04f, 5f, .04f), Quaternion.Euler(z * 14f, 0, -x * 14f));
            for (var k = 0; k < 8; k++) { var a = k * 45f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward; bm.Blob(WorldMaterials.Solid(k % 2 == 0 ? Paint[0] : Paint[2], .3f), new Vector3(0, 10.5f, 0) + d * 1.6f, new Vector3(3.4f, 5.2f, 3.4f), prng, .05f); }
            bm.Blob(GlowM(new Color(1f, .6f, .25f), 2.4f), new Vector3(0, 5.2f, 0), new Vector3(.3f, .6f, .3f), prng, .2f);
            var go = bm.BuildSingle("The balloon", plife.transform, false); go.transform.SetPositionAndRotation(g + up * .15f, f.r); plife.balloon = go.transform; plife.balloonHome = g + up * .15f;
            var seat = Interact.Seat(go.transform, go.transform.position, f.Fwd, "Go up in the balloon"); seat.transform.localPosition = new Vector3(.45f, .58f, 0); seat.transform.localRotation = Quaternion.Euler(0, -90f, 0); seat.facing = -f.Right; seat.GetComponent<BoxCollider>().size = new Vector3(1.8f, 1.6f, 1.8f); seat.GetComponent<BoxCollider>().center = new Vector3(-.4f, .3f, 0);
            var stake = f.P(3f, 0, 0); mk.Prism(wood, ctx.OnGround(stake.x, stake.z) - up * .2f, up, .1f, .7f, 6); plife.tetherFoot = ctx.OnGround(stake.x, stake.z) + up * .4f;
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(rope.GetComponent<Collider>()); rope.name = "Balloon tether"; rope.transform.SetParent(plife.transform, false); rope.GetComponent<Renderer>().sharedMaterial = dark; plife.tether = rope.transform;
            Folk(f.P(-3f, 0, 3f), f.Fwd, Resident.Pose.Wave, new[] { "She goes up, she comes down. Hop in when she's down.", "From the top you can see the Hall, the castle and Tuesday.", "Hold your hat. Not for the wind. It's just good manners." }, Look());
            var bn = f.P(-5f, 0, -3f); bn = ctx.OnGround(bn.x, bn.z); mk.Box(wood, bn + up * .45f, new Vector3(2f, .07f, .45f), f.r); Col(bn + up * .3f, new Vector3(2f, .6f, .45f), f.r); Interact.Bench(plife.transform, bn + up * .5f - f.Right * .7f, bn + up * .5f + f.Right * .7f, f.Fwd);
            Lamp(ctx, mk, ctx.Root, ctx.OnGround(f.P(5f, 0, 5f).x, f.P(5f, 0, 5f).z));
            Notice(ctx, mk, f.P(4f, 0, 9f), f.Fwd, "BALLOON ASCENTS\nup, then down", 3f);
            Lane(ctx, mk, f.P(0, 0, 9f)); Ring(g, 10f);
            Shot(ctx, "the balloon", f.P(-9f, 2f, 14f), g + up * 6f);
        }

        // the Maze: a hedge maze by the castle with a golden guitar at its heart
        void Maze(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(MazeAt.x, MazeAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const int N = 9; const float C = 2.6f, HH = 2.5f; var rng = new System.Random(1066);
            mk.Box(WorldMaterials.Solid(new Color(.7f, .66f, .52f)), g - up * .15f, new Vector3(N * C + 1f, .36f, N * C + 1f), f.r);
            // carve passages (depth first), then open a court in the middle and a way in at the front
            var east = new bool[N, N]; var north = new bool[N, N]; for (var x = 0; x < N; x++) for (var z = 0; z < N; z++) { east[x, z] = true; north[x, z] = true; }
            var seen = new bool[N, N]; var stack = new Stack<(int, int)>(); stack.Push((N / 2, 0)); seen[N / 2, 0] = true;
            while (stack.Count > 0)
            {
                var (x, z) = stack.Peek(); var opts = new List<(int, int)>(); foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { int nx = x + dx, nz = z + dz; if (nx >= 0 && nz >= 0 && nx < N && nz < N && !seen[nx, nz]) opts.Add((dx, dz)); }
                if (opts.Count == 0) { stack.Pop(); continue; }
                var (ox, oz) = opts[rng.Next(opts.Count)]; if (ox == 1) east[x, z] = false; else if (ox == -1) east[x - 1, z] = false; else if (oz == 1) north[x, z] = false; else north[x, z - 1] = false;
                seen[x + ox, z + oz] = true; stack.Push((x + ox, z + oz));
            }
            for (var x = 3; x <= 5; x++) for (var z = 3; z <= 5; z++) { if (x < 5) east[x, z] = false; if (z < 5) north[x, z] = false; }
            Vector3 P(float cx, float cz) => f.P((cx - N / 2f) * C, 0, (N / 2f - cz) * C);   // cell z = 0 is the front row (nearest the road)
            void Hedge(Vector3 a, Vector3 b) { var mid = (a + b) / 2f; var len = Vector3.Distance(a, b); var rot = Quaternion.LookRotation(b - a); mk.Box(hedge, mid + up * (HH / 2f), new Vector3(.6f, HH, len + .6f), rot); solid.Box(hedge, mid + up * (HH / 2f), new Vector3(.6f, HH, len + .6f), rot); }
            for (var x = 0; x < N; x++) for (var z = 0; z < N; z++)
            {
                if (east[x, z] && x < N - 1) Hedge(P(x + 1, z), P(x + 1, z + 1));
                if (north[x, z] && z < N - 1) Hedge(P(x, z + 1), P(x + 1, z + 1));
            }
            Hedge(P(0, 0), P(0, N)); Hedge(P(N, 0), P(N, N)); Hedge(P(0, N), P(N, N)); Hedge(P(0, 0), P(N / 2, 0)); Hedge(P(N / 2 + 1, 0), P(N, 0));
            // the heart: a golden guitar on a plinth, a bench, a lamp
            mk.Prism(stoneM, g - up * .1f, up, .9f, 1f, 8); solid.Prism(stoneM, g - up * .1f, up, .9f, 1f, 8); Guitar(mk, ctx.Rng, g + up * .9f, f.Fwd, new Color(.95f, .76f, .3f), 1.4f, 0f);
            var bn = P(4.5f, 5.6f); mk.Box(stoneM, bn + up * .25f, new Vector3(2f, .5f, .5f), f.r); Col(bn + up * .25f, new Vector3(2f, .5f, .5f), f.r); Interact.Bench(plife.transform, bn + up * .52f - f.Right * .7f, bn + up * .52f + f.Right * .7f, f.Fwd);
            ArtistBits.PointLight(ctx.Root, g + up * 3.5f, new Color(1f, .85f, .5f), 14f, 1.6f); halos.Add(g + up * 2.2f, 1.6f, new Color(1f, .85f, .5f), .5f);
            Folk(P(N / 2f + 1.4f, -1.2f), f.Fwd, Resident.Pose.Stand, new[] { "The way in's the way out. Eventually.", "A duke went in in 1971. We leave sandwiches.", "Keep the hedge on your left. Or was it the other left." }, Look(Residents.Tool.Rake));
            Notice(ctx, mk, P(N / 2f - 2.2f, -1.6f), f.Fwd, "THE ROYAL MAZE", 2.6f);
            Lane(ctx, mk, P(N / 2f + .5f, -.8f)); Ring(g, N * C * .75f);
            Shot(ctx, "the royal maze", f.P(6f, 9f, N * C / 2f + 12f), g + up * 1f);
        }

        // the Fete
        void Fete(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(FeteAt.x, FeteAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            // bunting on a ring of poles
            for (var k = 0; k < 10; k++)
            {
                var a0 = k * 36f; var p0 = g + Quaternion.Euler(0, a0, 0) * Vector3.forward * 15f; p0 = ctx.OnGround(p0.x, p0.z); mk.Box(white, p0 + up * 1.8f, new Vector3(.1f, 3.6f, .1f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, p0, 3.5f, .1f);
                var p1 = g + Quaternion.Euler(0, a0 + 36f, 0) * Vector3.forward * 15f; p1 = ctx.OnGround(p1.x, p1.z);
                for (var s = 1; s < 11; s++) { var t = s / 11f; var p = Vector3.Lerp(p0, p1, t) + up * (3.5f - Mathf.Sin(t * Mathf.PI) * .6f); mk.Tri(WorldMaterials.Solid(Paint[(s + k) % 4]), p + Vector3.Cross(up, p1 - p0).normalized * 0f - (p1 - p0).normalized * .15f, p + (p1 - p0).normalized * .15f, p - up * .35f, p + Vector3.Cross(up, p1 - p0)); mk.Tri(WorldMaterials.Solid(Paint[(s + k) % 4]), p + (p1 - p0).normalized * .15f, p - (p1 - p0).normalized * .15f, p - up * .35f, p - Vector3.Cross(up, p1 - p0)); }
            }
            // the coconut shy: five coconuts on posts in a striped stall, balls to throw
            var cs = f.P(-7f, 0, -4f); cs = ctx.OnGround(cs.x, cs.z); var cf = new PlaceFrame(cs, f.Right);
            for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? stripeR : stripeW, cf.P(-2.5f + k * 1f, 1.5f, -2f), new Vector3(1f, 3f, .08f), cf.r); Col(cf.P(0, 1.5f, -2f), new Vector3(6f, 3f, .2f), cf.r);
            foreach (var e in new[] { -3f, 3f }) { mk.Box(white, cf.P(e, 1.5f, -1f), new Vector3(.1f, 3f, 2f), cf.r); Col(cf.P(e, 1.5f, -1f), new Vector3(.15f, 3f, 2f), cf.r); }
            for (var k = 0; k < 5; k++)
            {
                var post = cf.P(-2f + k * 1f, 0, -1.2f); mk.Prism(wood, post, up, .06f, 1.45f, 6); mk.Prism(wood, post + up * 1.42f, up, .12f, .06f, 8); ArtistBits.PostCollider(ctx.Root, post, 1.4f, .06f);
                var cm = new MeshKit(); cm.Blob(WorldMaterials.Solid(new Color(.4f, .27f, .17f)), Vector3.zero, new Vector3(.14f, .17f, .14f), prng, .05f); var cgo = cm.BuildSingle("Coconut", plife.transform, false); cgo.transform.position = post + up * 1.66f;
                cgo.AddComponent<SphereCollider>().radius = .15f; var rb = cgo.AddComponent<Rigidbody>(); rb.mass = .5f; rb.angularDamping = 2f; var pu = cgo.AddComponent<Pushable>(); pu.home = cgo.transform.position; pu.lowest = post.y + 1f;
            }
            mk.Box(wood, cf.P(0, .45f, 3.5f), new Vector3(3f, .9f, .6f), cf.r); Col(cf.P(0, .45f, 3.5f), new Vector3(3f, .9f, .6f), cf.r);
            for (var k = 0; k < 6; k++) { var bm = new MeshKit(); bm.Blob(WorldMaterials.Solid(Paint[k % 3]), Vector3.zero, Vector3.one * .08f, prng, 0f); var bgo = bm.BuildSingle("Wooden ball", plife.transform, false); bgo.transform.position = cf.P(-1f + k * .4f, .98f, 3.5f); bgo.AddComponent<SphereCollider>().radius = .08f; var rb = bgo.AddComponent<Rigidbody>(); rb.mass = .35f; rb.isKinematic = true; var c = bgo.AddComponent<Carryable>(); c.thing = "a wooden ball"; c.home = bgo.transform.position; c.lowest = g.y - 6f; }
            ArtistBits.Label(ctx.Root, "COCONUT SHY\nthree balls a go", cf.P(0, 2.5f, -1.94f), cf.Fwd, .22f, new Color(.2f, .2f, .3f), TextAnchor.MiddleCenter);
            Folk(cf.P(2.4f, 0, 3.8f), -cf.Fwd, Resident.Pose.Wave, new[] { "Roll up! Knock one off and it's yours. They go back on by themselves; don't ask.", "Three balls a go! You've had nine. I'm not counting." }, Look());
            // the cake stall, the tombola, the maypole, hay bales
            var ck = f.P(7f, 0, -5f); ck = ctx.OnGround(ck.x, ck.z); mk.Box(stripeW, ck + up * .8f, new Vector3(3f, .08f, 1.1f), f.r); foreach (var e in new[] { -1.3f, 1.3f }) mk.Box(wood, ck + up * .4f + f.Right * e, new Vector3(.08f, .8f, 1f), f.r); Col(ck + up * .45f, new Vector3(3f, .9f, 1.1f), f.r);
            for (var k = 0; k < 4; k++) { mk.Prism(WorldMaterials.Solid(new[] { new Color(.95f, .85f, .7f), new Color(.5f, .3f, .2f), new Color(1f, .7f, .75f), new Color(.95f, .9f, .5f) }[k]), ck + up * .84f + f.Right * (-1.05f + k * .7f), up, .24f, .2f, 12); mk.Blob(WorldMaterials.Solid(Paint[0]), ck + up * 1.08f + f.Right * (-1.05f + k * .7f), Vector3.one * .05f, ctx.Rng, 0f); }
            Folk(ck - f.Fwd * 1f, f.Fwd, Resident.Pose.Stand, new[] { "Victoria sponge, lemon drizzle, or the one that went wrong. People like the one that went wrong.", "All proceeds to the bandstand roof. It's had a hard decade." }, Look(Residents.Tool.None, true));
            var tb = f.P(8f, 0, 4f); tb = ctx.OnGround(tb.x, tb.z); foreach (var e in new[] { -.6f, .6f }) mk.Box(wood, tb + up * .6f + f.Right * e, new Vector3(.08f, 1.2f, .08f), f.r); ArtistBits.PostCollider(ctx.Root, tb, 1.2f, .6f);
            var dr = new MeshKit(); dr.Prism(WorldMaterials.Solid(Paint[1]), new Vector3(-.5f, 0, 0), Vector3.right, .45f, 1f, 8); dr.Box(gilt, new Vector3(.6f, .2f, 0), new Vector3(.08f, .4f, .08f), Quaternion.identity);
            var dgo = dr.BuildSingle("Tombola", plife.transform, false); dgo.transform.SetPositionAndRotation(tb + up * 1.2f, f.r); var dt = Interact.Toy(dgo, Toy.Kind.Spin, "Spin the tombola", new Vector3(1.4f, 1f, 1f)); dt.axis = Vector3.right; dt.amount = 1080f; dt.seconds = 2.5f; var tn = 0; var prizes = new[] { "Ticket 47: a jar of chutney, 1974.", "Ticket 12: a bottle of bath salts. Again.", "Ticket 99: first prize! It's the chutney.", "Ticket 3: a tea towel of the castle." }; dt.onUse = _ => Say(dgo.transform, tb + up * 2.2f, prizes[tn++ % prizes.Length]);
            mk.Prism(white, g - up * .2f, up, .12f, 6.2f, 8); ArtistBits.PostCollider(ctx.Root, g, 6f, .15f); mk.Blob(gilt, g + up * 6.1f, Vector3.one * .2f, ctx.Rng, 0f);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; var a = g + up * 5.9f; var b = ctx.OnGround(g.x + d.x * 4.2f, g.z + d.z * 4.2f) + up * 1.1f; mk.Box(WorldMaterials.Solid(Paint[k % 4]), (a + b) / 2f, new Vector3(.08f, .02f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a)); }
            foreach (var e in new[] { 2, 5 }) { var d = Quaternion.Euler(0, e * 45f, 0) * Vector3.forward; Folk(ctx.OnGround(g.x + d.x * 4.4f, g.z + d.z * 4.4f), -d, Resident.Pose.Wave, new[] { "Round and round! I've forgotten which way.", "We do this every year. It's always this year." }, new Residents.Look { top = Paint[e % 3], bottom = new Color(.9f, .9f, .85f), height = 1.3f, skin = new Color(.9f, .72f, .6f), hair = new Color(.55f, .4f, .2f) }); }
            for (var k = 0; k < 5; k++) { var d = f.r * (Quaternion.Euler(0, 150f + k * 15f, 0) * Vector3.forward); var p = ctx.OnGround(g.x + d.x * 10f, g.z + d.z * 10f); mk.Box(WorldMaterials.Solid(new Color(.85f, .75f, .45f)), p + up * .3f, new Vector3(1.3f, .6f, .7f), Quaternion.LookRotation(d)); Col(p + up * .3f, new Vector3(1.3f, .6f, .7f), Quaternion.LookRotation(d)); Interact.Seat(plife.transform, p + up * .62f, -d, "Sit on the hay bale").seatHeight = .5f; }
            Lamp(ctx, mk, ctx.Root, ctx.OnGround(f.P(-11f, 0, 8f).x, f.P(-11f, 0, 8f).z)); Lamp(ctx, mk, ctx.Root, ctx.OnGround(f.P(11f, 0, 8f).x, f.P(11f, 0, 8f).z));
            Notice(ctx, mk, f.P(4f, 0, 17f), f.Fwd, "THE KINGDOM FETE\ntoday (every day)", 3f);
            Lane(ctx, mk, f.P(0, 0, 16f)); Ring(g, 16f);
            Shot(ctx, "the village fete", f.P(9f, 3f, 19f), g + up * 1.5f);
        }

        // the Cricket
        void Cricket(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CricketAt.x, CricketAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            mk.Prism(WorldMaterials.Solid(new Color(.5f, .72f, .38f)), g - up * .4f, up, 27f, .46f, 36);   // the outfield, mown
            mk.Box(WorldMaterials.Solid(new Color(.85f, .8f, .6f)), g + up * .075f, new Vector3(3f, .02f, 20f), f.r);
            foreach (var e in new[] { -1f, 1f }) { for (var s = -1; s <= 1; s++) mk.Box(white, f.P(s * .12f, .42f, e * 9.4f), new Vector3(.04f, .72f, .04f), f.r); mk.Box(white, f.P(0, .79f, e * 9.4f), new Vector3(.3f, .03f, .04f), f.r); Col(f.P(0, .4f, e * 9.4f), new Vector3(.32f, .8f, .1f), f.r); mk.Box(white, f.P(0, .085f, e * 8.2f), new Vector3(2.6f, .01f, .06f), f.r); }
            var whites = new Residents.Look { top = new Color(.96f, .95f, .9f), bottom = new Color(.96f, .95f, .9f), skin = new Color(.88f, .68f, .52f), hair = new Color(.3f, .2f, .12f), hat = Residents.Hat.Cap, hatColor = new Color(.2f, .3f, .5f) };
            Residents.Look W(int k0) { var k = Mathf.Abs(k0); return new Residents.Look { top = whites.top, bottom = whites.bottom, skin = new[] { new Color(.96f, .8f, .68f), new Color(.75f, .55f, .4f), new Color(.55f, .38f, .26f) }[k % 3], hair = whites.hair, hat = k % 2 == 0 ? Residents.Hat.Cap : Residents.Hat.None, hatColor = whites.hatColor, tool = k == 0 ? Residents.Tool.Broom : Residents.Tool.None }; }
            Folk(f.P(.5f, .08f, 8.3f), -f.Fwd, Resident.Pose.Stand, new[] { "I've been on ninety-nine since Tuesday.", "He bowls, I leave it. It's a long game. That's the charm." }, W(0));
            Folk(f.P(0, .08f, -12f), f.Fwd, Resident.Pose.Work, new[] { "Long run-up. Very long. I started it in May.", "Next one's the quick one. They're all the quick one." }, W(1)).workRate = 1.2f;
            Folk(f.P(0, .08f, 11f), -f.Fwd, Resident.Pose.Stand, new[] { "Haven't touched the ball all week. Lovely." }, W(2));
            Folk(f.P(-2f, .08f, -9.6f), f.Right, Resident.Pose.Stand, new[] { "Not out. I think. I was looking at a cloud." }, new Residents.Look { top = new Color(.96f, .95f, .9f), bottom = new Color(.2f, .2f, .22f), hat = Residents.Hat.Bucket, hatColor = new Color(.96f, .95f, .9f), skin = new Color(.9f, .72f, .6f), hair = new Color(.8f, .8f, .8f), apron = true });
            foreach (var (x, z) in new[] { (-14f, 4f), (12f, -8f), (16f, 9f), (-9f, -16f) }) Folk(ctx.OnGround(f.P(x, 0, z).x, f.P(x, 0, z).z), FaceTo(f.P(x, 0, z), g), Resident.Pose.Stand, new[] { "Deep square leg. Nobody's hit it here since the war.", "I'm fielding. It's mostly standing. I'm very good." }, W((int)x));
            var ball = new MeshKit(); ball.Blob(WorldMaterials.Solid(new Color(.7f, .1f, .1f), .6f), Vector3.zero, Vector3.one * .06f, prng, 0f); var bgo = ball.BuildSingle("Cricket ball", plife.transform, false); bgo.transform.position = f.P(1.4f, .2f, 2f); bgo.AddComponent<SphereCollider>().radius = .06f; var rb = bgo.AddComponent<Rigidbody>(); rb.mass = .3f; rb.linearDamping = .4f; var cb = bgo.AddComponent<Carryable>(); cb.thing = "the cricket ball"; cb.home = bgo.transform.position; cb.lowest = g.y - 6f;
            // the pavilion: white boards, a green roof, a veranda with a bench, a clock, the scoreboard beside it
            var pv = f.P(0, 0, 33f); pv = ctx.OnGround(pv.x, pv.z); var pf = new PlaceFrame(pv, -f.Fwd); var green = WorldMaterials.Solid(new Color(.2f, .4f, .3f));
            mk.Box(white, pf.P(0, 1.6f, -2f), new Vector3(11f, 3.2f, 4f), pf.r); solid.Box(white, pf.P(0, 1.6f, -2f), new Vector3(11f, 3.2f, 4f), pf.r); mk.GableRoof(green, pf.P(0, 3.2f, -.8f), new Vector3(6.8f, 0, 11.6f), 1.8f, Quaternion.LookRotation(pf.Right));
            mk.Box(wood, pf.P(0, .15f, 1.2f), new Vector3(11f, .3f, 2.4f), pf.r); solid.Box(wood, pf.P(0, .15f, 1.2f), new Vector3(11f, .3f, 2.4f), pf.r); foreach (var e in new[] { -5.3f, -1.8f, 1.8f, 5.3f }) { mk.Box(white, pf.P(e, 1.75f, 2.2f), new Vector3(.14f, 2.9f, .14f), pf.r); ArtistBits.PostCollider(ctx.Root, pf.P(e, 0, 2.2f), 3f, .1f); }
            foreach (var e in new[] { -3.4f, 3.4f }) { mk.Box(GlowM(new Color(1f, .85f, .55f), 1.4f), pf.P(e, 1.8f, .02f), new Vector3(1.6f, 1.1f, .04f), pf.r); mk.Box(wood, pf.P(e, .75f, .9f), new Vector3(2.4f, .07f, .45f), pf.r); Interact.Bench(plife.transform, pf.P(e - .9f, .8f, .9f), pf.P(e + .9f, .8f, .9f), pf.Fwd); }
            mk.Box(dark, pf.P(0, 1.3f, .02f), new Vector3(1.1f, 2.2f, .04f), pf.r); mk.Prism(white, pf.P(0, 4.2f, .9f), pf.Fwd, .5f, .06f, 14); mk.Box(dark, pf.P(0, 4.33f, .97f), new Vector3(.04f, .3f, .01f), pf.r); mk.Box(dark, pf.P(.1f, 4.2f, .97f), new Vector3(.22f, .04f, .01f), pf.r);
            Folk(pf.P(3.4f + .6f, .8f, .9f), pf.Fwd, Resident.Pose.Sit, new[] { "Tea's at four. It's been ten to four for a while now.", "I keep the score. The score keeps itself, really." }, Look(Residents.Tool.Mug), null, .5f);
            var sb = pf.P(9f, 0, 0); sb = ctx.OnGround(sb.x, sb.z); mk.Box(dark, sb + up * 2f, new Vector3(4.4f, 2.6f, .3f), pf.r); foreach (var e in new[] { -1.8f, 1.8f }) mk.Box(white, sb + up * .5f + pf.Right * e, new Vector3(.14f, 1.4f, .14f), pf.r); Col(sb + up * 1.6f, new Vector3(4.4f, 3.2f, .3f), pf.r);
            ArtistBits.Label(ctx.Root, "KINGDOM  256 FOR 3\nLAST MAN  99\nRAIN STOPPED PLAY (1971)", sb + up * 2f + pf.Fwd * .16f, pf.Fwd, .22f, Ink, TextAnchor.MiddleCenter);
            // deckchairs on the boundary
            for (var k = 0; k < 5; k++) { var d = f.r * (Quaternion.Euler(0, 55f + k * 12f, 0) * Vector3.forward); var p = g + d * 25f; Deckchair(ctx, mk, p, -d, k); }
            ArtistBits.PointLight(ctx.Root, pf.P(0, 2.6f, 2f), new Color(1f, .85f, .55f), 13f, 1.3f);
            Notice(ctx, mk, f.P(-9f, 0, -30f), -f.Fwd, "KINGDOM C.C.\nmatch in progress", 3f);
            Lane(ctx, mk, f.P(0, 0, -28f)); Ring(g, 28f); Ring(pv, 9f);
            Shot(ctx, "the cricket", f.P(-16f, 3f, -22f), g + up * 1f + f.Fwd * 8f);
        }

        // guards at the castle gate, villagers on the green, the publican
        void People(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up;
            if (hasCastle)
            {
                var side = Vector3.Cross(up, castleFace);
                foreach (var e in new[] { -1f, 1f }) { var p = castleAt + castleFace * 17.5f + side * e * 3.4f; Folk(ctx.OnGround(p.x, p.z), castleFace, Resident.Pose.Stand, new[] { "Halt! ...No, go on in. Everyone does.", "Who goes there? Rhetorical. It's always you lot.", "The King? Which one? We've several." }, new Residents.Look { top = new Color(.7f, .72f, .76f), bottom = new Color(.5f, .52f, .56f), hat = Residents.Hat.Bucket, hatColor = new Color(.75f, .77f, .8f), skin = new Color(.88f, .68f, .52f), hair = new Color(.2f, .15f, .1f), tool = Residents.Tool.Rod, height = 1.85f }); }
            }
            var c = ctx.OnGround(center.x, center.z);
            var route = new Vector3[10]; for (var k = 0; k < 10; k++) { var a = k * 36f * Mathf.Deg2Rad; route[k] = ctx.OnGround(c.x + Mathf.Cos(a) * 11f, c.z + Mathf.Sin(a) * 11f); }
            Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, null, null, route);
            var fl = ctx.OnGround(c.x - 11f, c.z - 6f); mk.Box(wood, fl + up * .7f, new Vector3(1.6f, .5f, .9f), Quaternion.identity); foreach (var e in new[] { -.6f, .6f }) mk.Prism(dark, fl + new Vector3(e, .3f, .5f), Vector3.forward, .3f, .06f, 10); Col(fl + up * .5f, new Vector3(1.6f, 1f, 1f), Quaternion.identity);
            for (var k = 0; k < 8; k++) soft.Blob(WorldMaterials.Solid(new[] { new Color(.9f, .2f, .3f), new Color(1f, .8f, .2f), new Color(.95f, .5f, .7f), new Color(.6f, .4f, .9f) }[k % 4]), fl + up * 1.05f + new Vector3(-.6f + (k % 4) * .4f, 0, -.2f + (k / 4) * .4f), Vector3.one * .16f, ctx.Rng, .2f);
            Folk(fl + new Vector3(1.4f, 0, .2f), FaceTo(fl, c), Resident.Pose.Stand, new[] { "Roses for the Hall? They're on me.", "Buttercups are free. Help yourself, they're everywhere." }, Look(Residents.Tool.None, true));
        }
    }

    /// The Kingdom places' moving parts: the balloon on its tether.
    public class ClassicPlacesLife : MonoBehaviour
    {
        public Transform balloon, tether; public Vector3 balloonHome, tetherFoot;
        void Update()
        {
            if (balloon == null) return;
            const float Cycle = 100f, Top = 38f; var c = (float)(System.DateTime.UtcNow.TimeOfDay.TotalSeconds % Cycle); float h;
            float Ease(float x) => x * x * (3f - 2f * x);
            if (c < 25f) h = 0f; else if (c < 45f) h = Ease((c - 25f) / 20f) * Top; else if (c < 80f) h = Top; else h = (1f - Ease((c - 80f) / 20f)) * Top;
            var sway = h / Top; balloon.position = balloonHome + Vector3.up * h + new Vector3(Mathf.Sin(Time.time * .3f), 0, Mathf.Cos(Time.time * .23f)) * 1.5f * sway;
            if (tether != null) { var a = tetherFoot; var b = balloon.position + Vector3.up * .2f; var d = b - a; tether.SetPositionAndRotation(a + d / 2f, Quaternion.LookRotation(d)); tether.localScale = new Vector3(.03f, .03f, d.magnitude); }
        }
    }
}
