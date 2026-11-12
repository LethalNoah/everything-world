using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Iron Range laid out on purpose (the build-land standard). The forge, the molten river and every artist stay
    /// where they were; four more gravel roads with braziers reach the empty parts: north to the miners' camp and the
    /// signal fire, east round the back of the Forge Peak and down the coast to the spit, south to the roadhouse, west
    /// along the high arm. Along them, with the people of the range: Hammertown (a smithy with an anvil to strike, a
    /// grindstone, a "test your metal" striker, a campfire); the Signal Fire on the north point; the Ore Dock (a crane
    /// whose hook swings, ingots to carry off); the Dragon's Bones on the spit (a pale skeleton as long as a ship, a
    /// hoard, an old knight on watch); the Rusty Nail (a roadhouse to walk into, bar stools, a jukebox, bikes out front);
    /// the Wall of Amps (a stage, a dial that goes to eleven, a crowd); and the Doom Bell at the end of the west road.
    public partial class IronKit
    {
        static readonly Vector2 CampAt = new Vector2(7850, 4050), SignalAt = new Vector2(7880, 4114), DockAt = new Vector2(8104, 3700), BonesAt = new Vector2(8205, 3441), NailAt = new Vector2(7705, 3458), AmpsAt = new Vector2(7440, 3705), BellAt = new Vector2(7328, 3757);
        static readonly Vector2[] NorthWay = { new Vector2(7765, 3950), new Vector2(7800, 3990), new Vector2(7835, 4030), new Vector2(7862, 4075), new Vector2(7878, 4104) };
        static readonly Vector2[] EastWay = { new Vector2(7950, 3835), new Vector2(8010, 3815), new Vector2(8060, 3780), new Vector2(8090, 3715), new Vector2(8092, 3640), new Vector2(8085, 3560), new Vector2(8085, 3490), new Vector2(8140, 3450), new Vector2(8185, 3441) };
        static readonly Vector2[] SouthWay = { new Vector2(7722, 3530), new Vector2(7712, 3495), new Vector2(7706, 3472) };
        static readonly Vector2[] WestWay = { new Vector2(7510, 3712), new Vector2(7462, 3712), new Vector2(7410, 3728), new Vector2(7365, 3748), new Vector2(7340, 3755) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (CampAt, 15f), (SignalAt, 6f), (DockAt, 13f), (BonesAt, 14f), (NailAt, 11f), (AmpsAt, 14f), (BellAt, 7f) };
        static readonly Color Bone = new Color(.86f, .83f, .72f);

        IronPlacesLife plife; System.Random prng; Transform rootT; readonly List<Vector3> roadPts = new List<Vector3>();
        Material boneM, boardM;

        static readonly Color[] Tops = { new Color(.12f, .12f, .13f), new Color(.2f, .2f, .22f), new Color(.45f, .12f, .1f), new Color(.25f, .28f, .35f), new Color(.5f, .45f, .4f), new Color(.15f, .2f, .3f) };
        static readonly Color[] Legs = { new Color(.1f, .1f, .12f), new Color(.2f, .24f, .34f), new Color(.3f, .25f, .2f) };
        static readonly Residents.Hat[] Lids = { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Beanie, Residents.Hat.None };
        static readonly Color[] LidColors = { new Color(.9f, .75f, .2f), new Color(.15f, .15f, .16f), new Color(.6f, .15f, .1f) };
        static readonly string[] RangeTalk =
        {
            "Storm's coming. Storm's always coming. Good.", "You get used to the thunder. Then you miss it when it stops.", "Everything here is louder than it needs to be. That's the point.",
            "Mind the cracks. The orange ones are warm; the white ones are hot.", "The Forge was here before the mountain. Ask anyone.",
        };

        Residents.Look Metalhead(Residents.Tool tool = Residents.Tool.None, bool apron = false, bool hair = true) { var l = Residents.Random(prng, Tops, Legs, Lids, LidColors); l.tool = tool; l.apron = apron; if (hair && l.hat == Residents.Hat.None) l.longHair = true; return l; }
        Residents.Look Miner(Residents.Tool tool = Residents.Tool.None) { var l = Residents.Random(prng, Tops, Legs, Lids, LidColors); l.hat = Residents.Hat.Cap; l.hatColor = new Color(.9f, .75f, .2f); l.tool = tool; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Metalhead(), pose, prng, lines ?? RangeTalk, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        LandContext ctx0;

        /// A riveted iron board on two posts. Signs go on the +x side of a place's lane; the photo spot on the −x side.
        void IronSign(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.6f, float size = .17f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + rot * Vector3.right * e * (w / 2f - .1f) + up * 1f, new Vector3(.1f, 2f, .1f), rot);
            mk.Box(boardM, g + up * 1.7f, new Vector3(w, .75f, .08f), rot); Col(g + up * 1.05f, new Vector3(w, 2.1f, .15f), rot);
            foreach (var (x, y) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(rust, g + up * (1.7f + y * .3f) + rot * Vector3.right * x * (w / 2f - .12f) + face * .045f, Vector3.one * .06f, rot);
            ArtistBits.Label(rootT, text, g + up * 1.72f + face * .05f, face, size, Molten, TextAnchor.MiddleCenter);
        }

        /// An iron slab on two stone blocks: somewhere to sit.
        void IronBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(iron, g + up * .45f, new Vector3(len, .08f, .45f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(rock, g + up * .2f + side * e * (len / 2f - .25f), new Vector3(.35f, .45f, .4f), rot);
            Col(g + up * .3f, new Vector3(len, .6f, .45f), rot); Interact.Bench(plife.transform, g + up * .5f - side * (len / 2f - .4f), g + up * .5f + side * (len / 2f - .4f), face);
        }

        void PlaceBend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                foreach (var c in noGrass) { if (c.y < 4f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
            }
        }

        List<Vector3> GravelRoad(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 160f * 160f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = Smooth(ctx, full, false, 2f); PlaceBend(ctx, pts); pts.RemoveAll(p => InRiver(p.x, p.z, 2f)); if (pts.Count < 2) return pts;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.4f, .36f, .33f)), 3f, .24f, ctx, true);
            var up = Vector3.up; var n = 0;
            for (var i = 6; i < pts.Count; i += 13) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 2.3f : -2.3f); if (ctx.IsLand(lp.x, lp.z, 2f) && !InRiver(lp.x, lp.z, 1f)) Brazier(ctx, mk, ctx.OnGround(lp.x, lp.z)); }
            foreach (var p in pts) Remember(p, 700);
            roadPts.AddRange(pts);
            // a bench half-way, set back from the gravel, facing the road
            if (pts.Count > 30) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = pts[i] + sd * 3.4f; if (ctx.IsLand(bp.x, bp.z, 2f)) { IronBench(mk, bp, -sd); Ring(bp, 2f); } }
            return pts;
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.6f; var b = new Vector2(from.x, from.z) + d * (len - stopShort);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.4f, .36f, .33f)), 2.2f, .27f, ctx, true); foreach (var p in pts) Remember(p, 900);
        }

        void RangePlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1970);
            boneM = WorldMaterials.Solid(Bone); boardM = WorldMaterials.Solid(new Color(.1f, .1f, .11f), .5f);
            plife = new GameObject("Iron places life").AddComponent<IronPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            var north = GravelRoad(ctx, mk, NorthWay); GravelRoad(ctx, mk, EastWay); GravelRoad(ctx, mk, SouthWay); GravelRoad(ctx, mk, WestWay);
            Hammertown(ctx, mk, solid); SignalFire(ctx, mk, solid); OreDock(ctx, mk, solid); DragonBones(ctx, mk, solid); RustyNail(ctx, mk, solid); WallOfAmps(ctx, mk, solid); DoomBell(ctx, mk, solid);
            // a miner walking the north road with a lamp on his hat
            if (north.Count > 20) { var route = new List<Vector3>(); for (var i = 4; i < north.Count - 4; i += 6) { var d = north[i + 1] - north[i - 1]; d.y = 0; d.Normalize(); var p = north[i] + Vector3.Cross(Vector3.up, d) * 1f; route.Add(ctx.OnGround(p.x, p.z)); } for (var i = route.Count - 2; i > 0; i--) route.Add(route[i]); if (route.Count > 3) Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, new[] { "Shift change. Up the hill, down the hill. It's a living.", "Camp's north. Anvil's free if you want a go.", "Found a seam of something green last week. Left it alone." }, Miner(Residents.Tool.Rake), route.ToArray()); }
        }

        // ---------------------------------------------------------------- Hammertown, the miners' camp
        void Hammertown(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CampAt.x, CampAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            // the smithy: three walls and a tin roof, a forge glowing at the back
            var s = new PlaceFrame(G(f.P(-6f, 0, -5f)), f.Fwd);
            mk.Box(timber, s.P(0, 1.6f, -2.4f), new Vector3(7f, 3.2f, .2f), f.r); Col(s.P(0, 1.6f, -2.4f), new Vector3(7f, 3.2f, .25f), f.r); foreach (var e in new[] { -3.4f, 3.4f }) { mk.Box(timber, s.P(e, 1.6f, -.8f), new Vector3(.2f, 3.2f, 3.2f), f.r); Col(s.P(e, 1.6f, -.8f), new Vector3(.25f, 3.2f, 3.2f), f.r); }
            mk.Box(rust, s.P(0, 3.4f, -.3f), new Vector3(7.8f, .1f, 5.2f), f.r * Quaternion.Euler(8f, 0, 0)); foreach (var e in new[] { -3.4f, 3.4f }) { mk.Box(iron, s.P(e, 1.5f, 1.9f), new Vector3(.16f, 3f, .16f), f.r); ArtistBits.PostCollider(ctx.Root, s.P(e, 0, 1.9f), 3f, .1f); }
            mk.Box(rock, s.P(-1.8f, .6f, -1.6f), new Vector3(1.8f, 1.2f, 1.3f), f.r); Col(s.P(-1.8f, .6f, -1.6f), new Vector3(1.8f, 1.2f, 1.3f), f.r); mk.Box(GlowM(Molten, 3f), s.P(-1.8f, 1.22f, -1.6f), new Vector3(1.3f, .06f, .9f), f.r); halos.Add(s.P(-1.8f, 1.6f, -1.6f), 1.8f, Molten, .8f); life.fires.Add(s.P(-1.8f, 1.3f, -1.6f));
            mk.Box(rock, s.P(-1.8f, 2.5f, -2f), new Vector3(1.2f, 2.6f, .6f), f.r);
            ArtistBits.PointLight(ctx.Root, s.P(-1f, 2.2f, -.4f), Molten, 14f, 1.8f);
            // the anvil: strike it
            var av = new MeshKit(); av.Box(rock, new Vector3(0, .3f, 0), new Vector3(.7f, .6f, .6f), Quaternion.identity); av.Box(iron, new Vector3(0, .72f, 0), new Vector3(.5f, .26f, .4f), Quaternion.identity); av.Box(iron, new Vector3(0, .95f, .08f), new Vector3(.42f, .2f, 1.1f), Quaternion.identity); av.Prism(iron, new Vector3(0, .95f, .6f), Vector3.forward, .1f, .4f, 6, .2f);
            var ago = av.BuildSingle("Anvil", plife.transform, false); ago.transform.SetPositionAndRotation(s.P(.8f, 0, .2f), f.r * Quaternion.Euler(0, 90f, 0)); Col(s.P(.8f, .5f, .2f), new Vector3(1.1f, 1f, .7f), f.r);
            var al = ArtistBits.PointLight(ctx.Root, s.P(.8f, 1.5f, .2f), new Color(1f, .8f, .4f), 9f, 0f); string[] clang = { "CLANG!", "CLANG! The whole camp looks round.", "CLANG! Sparks jump off the face of it." }; var cn = 0;
            var at = Interact.Toy(ago, Toy.Kind.Custom, "Strike the anvil", new Vector3(1.2f, 1.4f, 1.4f)); ago.GetComponent<BoxCollider>().center = new Vector3(0, .7f, 0); at.onUse = _ => { plife.Flash(al, 5f); Say(plife.transform, ago.transform.position + up * 2.2f, clang[cn++ % clang.Length], 2.5f); };
            Folk(s.P(2f, 0, .1f), -f.Right, Resident.Pose.Work, new[] { "Hit it like you mean it. It can take it. It's an anvil.", "I make pickaxes. And one sword a year, for the look of the thing.", "Hot iron and cold rain. That's the whole job." }, Metalhead(Residents.Tool.Spatula, true)).workRate = 1.4f;
            // the grindstone
            var gp = G(f.P(-1.5f, 0, -2.5f)); foreach (var e in new[] { -.3f, .3f }) mk.Box(timber, gp + f.Right * e + up * .5f, new Vector3(.1f, 1f, .5f), f.r); Col(gp + up * .5f, new Vector3(.8f, 1f, .6f), f.r);
            var wm = new MeshKit(); wm.Prism(rock, new Vector3(-.09f, 0, 0), Vector3.right, .5f, .18f, 14); wm.Box(iron, new Vector3(.25f, .2f, 0), new Vector3(.3f, .05f, .05f), Quaternion.identity);
            var wgo = wm.BuildSingle("Grindstone", plife.transform, false); wgo.transform.SetPositionAndRotation(gp + up * 1f, f.r); var wt = Interact.Toy(wgo, Toy.Kind.Spin, "Spin the grindstone", new Vector3(.8f, 1.2f, 1.2f)); wt.axis = Vector3.right; wt.amount = 1440f; wt.seconds = 4f;
            // TEST YOUR METAL: a striker with a bell on top
            var tp = G(f.P(5.5f, 0, -4.5f)); const float TH = 6f; mk.Box(boardM, tp + up * (TH / 2f), new Vector3(.7f, TH, .12f), f.r); mk.Box(rust, tp + up * .15f + f.Fwd * .6f, new Vector3(1.2f, .3f, 1.6f), f.r); Col(tp + up * (TH / 2f), new Vector3(.7f, TH, .3f), f.r); Col(tp + up * .15f + f.Fwd * .6f, new Vector3(1.2f, .3f, 1.6f), f.r);
            for (var k = 1; k <= 11; k++) mk.Box(GlowM(k == 11 ? Molten : new Color(.9f, .85f, .7f), k == 11 ? 2.6f : .8f), tp + up * (k * .48f + .3f) + f.Fwd * .07f, new Vector3(k == 11 ? .6f : .3f, .04f, .02f), f.r);
            mk.Prism(GlowM(new Color(1f, .8f, .3f), 1.2f), tp + up * (TH + .05f), up, .32f, .3f, 10, .5f); ArtistBits.Label(ctx.Root, "TEST YOUR METAL", tp + up * 1.2f + f.Fwd * 1.45f - up * .9f, f.Fwd, .12f, Molten, TextAnchor.MiddleCenter);
            var pk = new MeshKit(); pk.Box(GlowM(Molten, 2.4f), Vector3.zero, new Vector3(.34f, .14f, .1f), Quaternion.identity); var pgo = pk.BuildSingle("Striker puck", plife.transform, false); pgo.transform.SetPositionAndRotation(tp + up * .5f + f.Fwd * .13f, f.r);
            var bl = ArtistBits.PointLight(ctx.Root, tp + up * (TH + .4f) + f.Fwd * .5f, new Color(1f, .8f, .3f), 12f, 0f); plife.puck = pgo.transform; plife.puckBase = pgo.transform.position; plife.puckTop = TH - .7f; plife.bellLight = bl;
            var hgo = new GameObject("Striker pad"); hgo.transform.SetParent(plife.transform, false); hgo.transform.position = tp + up * .6f + f.Fwd * .9f; string[] grade = { "WEAKLING", "ROADIE", "HEADBANGER", "ELEVEN!" };
            Interact.Toy(hgo, Toy.Kind.Custom, "Swing the hammer", new Vector3(1.2f, 1.2f, 1.2f)).onUse = _ => { var power = plife.Strike(); Say(plife.transform, tp + up * 2.4f + f.Fwd * 1f, grade[Mathf.Min(3, (int)(power * 4f))], 3f); };
            // the campfire, logs to sit on, two tents
            var fp = G(f.P(2f, 0, 2f)); mk.Prism(rock, fp, up, .9f, .25f, 9); mk.Prism(GlowM(Molten, 3f), fp + up * .26f, up, .6f, .05f, 8); halos.Add(fp + up * .8f, 1.8f, Molten, .9f); life.fires.Add(fp + up * .4f); ArtistBits.PostCollider(ctx.Root, fp, .4f, .9f); ArtistBits.PointLight(ctx.Root, fp + up * 1.4f, Molten, 13f, 1.7f);
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 100f + 130f, 0) * f.Fwd; var lp = G(fp + d * 2.4f); var rot = Quaternion.LookRotation(Vector3.Cross(up, d)); mk.Prism(timber, lp + up * .25f - rot * Vector3.forward * .9f, rot * Vector3.forward, .25f, 1.8f, 7); Col(lp + up * .25f, new Vector3(.5f, .5f, 1.8f), rot); Interact.Seat(plife.transform, lp + up * .5f, -d, "Sit by the fire"); if (k == 0) Folk(lp + up * .5f, -d, Resident.Pose.Sit, new[] { "Twelve hours down the hole and the first thing I do is sit by a fire. Go figure.", "The striker's rigged. Hit it anyway.", "You hear the lullaby some nights, off south. Nobody goes looking." }, Miner(Residents.Tool.Mug), null, .5f); }
            foreach (var e in new[] { 0f, 1f }) { var tpos = G(f.P(7f + e * 4.5f, 0, 3f - e * 3f)); var tr = Quaternion.LookRotation(Quaternion.Euler(0, 25f - e * 50f, 0) * f.Fwd); mk.GableRoof(WorldMaterials.Solid(new Color(.42f, .38f, .3f)), tpos + up * .05f, new Vector3(3f, 0, 3.6f), 1.9f, tr); Col(tpos + up * .9f, new Vector3(3f, 1.8f, 3.6f), tr); mk.Box(dark, tpos + up * .7f + tr * Vector3.forward * 1.81f, new Vector3(.9f, 1.3f, .02f), tr); }
            // ore to carry off
            for (var k = 0; k < 4; k++) { var om = new MeshKit(); om.Blob(k % 2 == 0 ? darkRock : rust, Vector3.zero, new Vector3(.2f, .16f, .18f), ctx.Rng, .3f); om.Box(GlowM(Molten, 1.6f), new Vector3(0, .1f, .05f), Vector3.one * .07f, Quaternion.Euler(20f, 30f, 10f)); var go = om.BuildSingle("Ore lump", plife.transform, false); go.transform.position = G(f.P(-2.5f + k * .5f, 0, 1.5f + (k % 2) * .4f)) + up * .2f; Interact.Carry(go, "a lump of ore", 1.4f, g.y - 8f); }
            IronSign(mk, f.P(5f, 0, 8f), f.Fwd, "HAMMERTOWN\npop. loud", 2.6f);
            Lane(ctx, mk, f.P(0, 0, 4f)); Ring(g, 13f);
            Shot(ctx, "hammertown", f.P(-9f, 3f, 11f), g + up * 1.4f);
        }

        // ---------------------------------------------------------------- the Signal Fire on the north point
        void SignalFire(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SignalAt.x, SignalAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            var c = G(f.P(0, 0, -2.5f)); mk.Prism(rock, c - up * .3f, up, 1.5f, 2.5f, 8, .7f); solid.Prism(rock, c - up * .3f, up, 1.5f, 2.5f, 8, .7f); mk.Prism(iron, c + up * 2.2f, up, .9f, .5f, 8, 1.3f);
            var fm = new Material(WorldMaterials.Glow(Molten, .15f)) { name = "Signal fire" }; mk.Cone(fm, c + up * 2.7f, .8f, 1.8f, 7); var sl = ArtistBits.PointLight(ctx.Root, c + up * 4f, Molten, 40f, 0f); plife.signalM = fm; plife.signalLight = sl;
            var tgo = new GameObject("Signal fire"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = c + up * 1.4f + f.Fwd * 1.4f; Interact.Toy(tgo, Toy.Kind.Custom, "Light the signal fire", new Vector3(1.6f, 2f, 1.2f)).onUse = _ => { plife.signalOn = !plife.signalOn; Say(plife.transform, c + up * 4.6f, plife.signalOn ? "The fire catches. They'll see that from the Kingdom." : "You smother it. The point goes dark.", 4f); };
            IronBench(mk, f.P(3.2f, 0, -1f), FaceTo(f.P(3.2f, 0, -1f), g - f.Fwd * 30f));
            Ring(g, 5f);
            Shot(ctx, "the signal fire", f.P(-4f, 2.2f, 8f), c + up * 2.4f);
        }

        // ---------------------------------------------------------------- the Ore Dock
        void OreDock(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(DockAt.x, DockAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            mk.Box(rock, g - up * .35f, new Vector3(20f, 1f, 16f), f.r); solid.Box(rock, g - up * .35f, new Vector3(20f, 1f, 16f), f.r); var fo = new PlaceFrame(g + up * .15f, f.Fwd);
            // the crane: a riveted tower, a jib out over the yard, a hook on a chain to push
            var cb = fo.P(-5f, 0, -3f); const float CH = 8f; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(iron, cb + f.r * new Vector3(x * .8f, CH / 2f, z * .8f), new Vector3(.16f, CH, .16f), f.r);
            for (var k = 0; k < 4; k++) { var y = 1f + k * 2f; mk.Box(rust, cb + up * y + f.Fwd * .8f, new Vector3(1.9f, .1f, .1f), f.r * Quaternion.Euler(0, 0, k % 2 == 0 ? 35f : -35f)); mk.Box(rust, cb + up * y - f.Fwd * .8f, new Vector3(1.9f, .1f, .1f), f.r * Quaternion.Euler(0, 0, k % 2 == 0 ? -35f : 35f)); }
            Col(cb + up * (CH / 2f), new Vector3(1.8f, CH, 1.8f), f.r); mk.Box(rust, cb + up * (CH + .3f), new Vector3(2.2f, .6f, 2.2f), f.r); mk.Box(iron, cb + up * (CH + .3f) + f.Right * 3f, new Vector3(9f, .3f, .4f), f.r); mk.Box(rock, cb + up * (CH + .1f) - f.Right * 2f, new Vector3(1.4f, 1f, 1.2f), f.r);
            var tip = cb + up * (CH + .15f) + f.Right * 7f; var hm = new MeshKit(); for (var k = 0; k < 9; k++) hm.Box(chain, new Vector3(0, -.3f - k * .5f, 0), new Vector3(.07f, .4f, .07f), Quaternion.Euler(0, k * 90f, 0)); hm.Box(iron, new Vector3(0, -4.9f, 0), new Vector3(.4f, .4f, .3f), Quaternion.identity); hm.Box(iron, new Vector3(.2f, -5.4f, 0), new Vector3(.1f, .6f, .1f), Quaternion.identity); hm.Box(iron, new Vector3(0, -5.7f, 0), new Vector3(.5f, .1f, .1f), Quaternion.identity);
            var hgo = hm.BuildSingle("Crane hook", plife.transform, false); hgo.transform.SetPositionAndRotation(tip, f.r); var hs = hgo.AddComponent<Sway>(); hs.axis = Vector3.forward; hs.degrees = 22f; hs.speed = 2.2f; hs.decay = .35f; hs.seconds = 12f;
            var ht = Interact.Toy(hgo, Toy.Kind.Custom, "Push the hook", new Vector3(1f, 2f, 1f)); hgo.GetComponent<BoxCollider>().center = new Vector3(0, -5.2f, 0); ht.onUse = _ => hs.Push();
            // ore heaps, carts on a short track, crates, ingots to carry
            foreach (var (x, z, r) in new[] { (5f, -4f, 2.6f), (7.5f, -1.5f, 1.9f), (3f, -6f, 1.6f) }) { var p = fo.P(x, -.1f, z); mk.Cone(darkRock, p, r, r * .8f, 7, x * 20f); solid.Cone(darkRock, p, r, r * .8f, 7, x * 20f); for (var k = 0; k < 4; k++) { var a = k * 1.7f + x; mk.Box(GlowM(Molten, 1.8f), p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r * .5f + up * r * .42f, Vector3.one * .12f, Quaternion.Euler(30f, a * 50f, 20f)); } }
            foreach (var e in new[] { -.5f, .5f }) mk.Box(iron, fo.P(-1f, .04f, 4f + e), new Vector3(12f, .08f, .08f), f.r);
            for (var k = 0; k < 2; k++) { var p = fo.P(-4f + k * 3.2f, 0, 4f); mk.Box(rust, p + up * .75f, new Vector3(1.8f, .9f, 1.1f), f.r); mk.Box(darkRock, p + up * 1.25f, new Vector3(1.5f, .25f, .9f), f.r); foreach (var x in new[] { -.6f, .6f }) foreach (var z in new[] { -.5f, .5f }) mk.Prism(iron, p + f.r * new Vector3(x, .25f, z - .05f * Mathf.Sign(z)), f.Fwd * Mathf.Sign(z), .25f, .08f, 8); Col(p + up * .7f, new Vector3(1.8f, 1.4f, 1.2f), f.r); }
            for (var k = 0; k < 5; k++) { var im = new MeshKit(); im.Box(k == 0 ? WorldMaterials.Solid(new Color(.85f, .7f, .25f), .8f) : iron, Vector3.zero, new Vector3(.36f, .12f, .16f), Quaternion.identity); var go = im.BuildSingle("Ingot", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(1.5f + (k % 3) * .42f, .1f + (k / 3) * .13f, 1.2f), f.r); Interact.Carry(go, k == 0 ? "a gold ingot (don't tell)" : "an iron ingot", 2f, g.y - 10f); }
            mk.Box(timber, fo.P(2f, .03f, 1.2f), new Vector3(1.8f, .06f, 1f), f.r);
            Folk(fo.P(3.6f, 0, 2.2f), f.Fwd, Resident.Pose.Work, new[] { "Ship's due Thursday. Ship's been due Thursday since spring.", "Take an ingot. Take two. Nobody's counting but me, and I can't count.", "Give the hook a shove. Go on. Everyone does." }, Miner(Residents.Tool.Broom)).workRate = .7f;
            Folk(fo.P(-7.5f, 0, 5.5f), f.Fwd, Resident.Pose.Lean, new[] { "Sea's the only quiet thing in the range. I come here to hear nothing.", "Iron goes out, records come in. Fair trade." }, Metalhead());
            IronBench(mk, fo.P(7f, 0, 5.5f), -f.Fwd, 2.2f); Brazier(ctx, mk, G(fo.P(9f, 0, 7f)));
            ArtistBits.PointLight(ctx.Root, fo.P(0, 4f, 0), Molten, 20f, 1.5f);
            IronSign(mk, f.P(6f, 0, 11f), f.Fwd, "ORE DOCK", 2.2f);
            Lane(ctx, mk, f.P(0, 0, 8f)); Ring(g, 12f);
            Shot(ctx, "the ore dock", f.P(-10f, 3.5f, 13f), fo.P(0, 3f, -1f));
        }

        // ---------------------------------------------------------------- the Dragon's Bones on the spit
        void DragonBones(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BonesAt.x, BonesAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            // the spine curls round the spit: tail by the road, skull at the far end, turned back toward you
            var spine = new List<Vector3>(); for (var k = 0; k <= 22; k++) { var t = k / 22f; var a = Mathf.Lerp(-150f, 100f, t) * Mathf.Deg2Rad; var r = 9f; var p = f.P(Mathf.Sin(a) * r, 0, -1f + Mathf.Cos(a) * r * .8f); var gp = G(p); var arch = Mathf.Sin(Mathf.Clamp01((t - .2f) / .65f) * Mathf.PI); spine.Add(gp + up * (.35f + arch * 2.6f)); }
            for (var k = 0; k < spine.Count - 1; k++)
            {
                var t = k / 22f; var p = spine[k]; var d = (spine[k + 1] - p).normalized; var rot = Quaternion.LookRotation(d); var size = Mathf.Lerp(.25f, .7f, Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI * .55f));
                mk.Box(boneM, p, new Vector3(size, size, Vector3.Distance(p, spine[k + 1]) * .8f), rot); mk.Box(boneM, p + up * size * .8f, new Vector3(.1f, size * .9f, .25f), rot);
                var side = Vector3.Cross(up, d).normalized; var gy = ctx.Ground(p.x, p.z);
                if (t > .28f && t < .8f)
                {   // ribs: out and down to the ground, an arch you can walk under
                    var w = 2.6f * Mathf.Sin((t - .28f) / .52f * Mathf.PI) + 1.2f;
                    foreach (var e in new[] { -1f, 1f }) { var a = p + side * e * size * .4f; var b = p + side * e * w + up * .3f; var c = new Vector3(b.x + side.x * e * .3f, Mathf.Max(gy, ctx.Ground(b.x, b.z)) - .2f, b.z + side.z * e * .3f); mk.Prism(boneM, a, (b - a).normalized, .13f, Vector3.Distance(a, b), 5, .8f); mk.Prism(boneM, b, (c - b).normalized, .11f, Vector3.Distance(b, c), 5, .4f); ArtistBits.PostCollider(ctx.Root, new Vector3(c.x, gy, c.z), Mathf.Max(.5f, b.y - gy), .14f); }
                    Col(p, new Vector3(size, size * 1.6f, 1.3f), rot);
                }
                else Col(new Vector3(p.x, (p.y + gy) / 2f, p.z), new Vector3(size, p.y - gy + size, 1.2f), Quaternion.LookRotation(new Vector3(d.x, 0, d.z)));
                if (k == 3) Interact.Seat(plife.transform, p + up * (size / 2f + .05f), side, "Sit on the tail");
            }
            // the skull
            var sp = spine[spine.Count - 1]; var sd = (spine[spine.Count - 1] - spine[spine.Count - 3]); sd.y = 0; sd.Normalize(); var sr = Quaternion.LookRotation(sd); var hs = G(sp + sd * 1.6f) + up * 1f;
            mk.Box(boneM, hs, new Vector3(1.7f, 1.3f, 2.2f), sr); mk.Box(boneM, hs + sd * 2.2f - up * .15f, new Vector3(1.2f, .8f, 2.4f), sr); mk.Box(boneM, hs + sd * 1.9f - up * .85f, new Vector3(1f, .22f, 2.6f), sr * Quaternion.Euler(14f, 0, 0));
            Col(hs + sd * 1f - up * .2f, new Vector3(1.7f, 1.8f, 4.8f), sr);
            foreach (var e in new[] { -1f, 1f }) { var sx = sr * Vector3.right * e; mk.Prism(boneM, hs + sx * .7f + up * .5f - sd * .6f, (up * .6f - sd + sx * .35f).normalized, .22f, 2.4f, 6, .1f); mk.Blob(GlowM(Molten, 3f), hs + sx * .86f + up * .2f + sd * .5f, Vector3.one * .2f, ctx.Rng, 0f); halos.Add(hs + sx * 1f + up * .2f + sd * .5f, .9f, Molten, .8f); for (var k = 0; k < 4; k++) mk.Cone(boneM, hs + sd * (1.4f + k * .5f) + sx * .5f - up * .55f, .09f, -.35f, 4); }
            ArtistBits.PointLight(ctx.Root, hs + sd * 1f + up * 1.5f, Molten, 12f, 1.4f);
            // the hoard inside the ribs: a heap of gold, goblets to carry off, a sword stood in it
            var hp = G(f.P(0, 0, -1f)); var gold = WorldMaterials.Glow(new Color(1f, .78f, .2f), .5f); mk.Cone(gold, hp - up * .1f, 2.2f, 1.1f, 9); solid.Cone(gold, hp - up * .1f, 2f, .8f, 9); halos.Add(hp + up * 1f, 2.2f, new Color(1f, .8f, .3f), .5f);
            mk.Box(iron, hp + up * 1.7f, new Vector3(.12f, 1.8f, .03f), Quaternion.Euler(0, 30f, 8f)); mk.Box(rust, hp + up * 2.45f, new Vector3(.5f, .08f, .08f), Quaternion.Euler(0, 30f, 8f));
            for (var k = 0; k < 4; k++) { var cm = new MeshKit(); var gm = WorldMaterials.Solid(new Color(.9f, .72f, .22f), .8f); cm.Prism(gm, Vector3.zero, Vector3.up, .09f, .03f, 8); cm.Prism(gm, new Vector3(0, .03f, 0), Vector3.up, .025f, .12f, 6); cm.Prism(gm, new Vector3(0, .15f, 0), Vector3.up, .06f, .14f, 8, 1.5f); var go = cm.BuildSingle("Goblet", plife.transform, false); var a = k * 1.6f + .4f; go.transform.position = G(hp + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.7f) + up * .05f; Interact.Carry(go, "a dented goblet", .6f, g.y - 8f); }
            ArtistBits.PointLight(ctx.Root, hp + up * 2.4f, new Color(1f, .8f, .35f), 14f, 1.5f);
            // the old knight on watch
            Folk(G(f.P(4.5f, 0, 7.5f)), f.Fwd, Resident.Pose.Stand, new[] { "I was sent to slay it. Got here four hundred years late.", "The gold's cursed. Mildly. You'll lose a sock, that sort of thing.", "It sang, they say. Low. You could feel it in your teeth. I'd have liked that." },
                new Residents.Look { skin = new Color(.85f, .68f, .55f), hair = new Color(.8f, .8f, .8f), top = new Color(.55f, .57f, .6f), bottom = new Color(.4f, .42f, .45f), hat = Residents.Hat.Bucket, hatColor = new Color(.6f, .62f, .65f), tool = Residents.Tool.Rod });
            IronSign(mk, f.P(7f, 0, 10.5f), f.Fwd, "HERE LIES\nA DRAGON", 2.4f);
            Lane(ctx, mk, f.P(0, 0, 9.5f)); Ring(g, 12f);
            Shot(ctx, "the dragon's bones", f.P(-9f, 4f, 13f), g + up * 1.6f);
        }

        // ---------------------------------------------------------------- the Rusty Nail, a roadhouse
        void RustyNail(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(NailAt.x, NailAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 11f, D = 8f, H = 3.4f, T = .3f, Door = HumanScale.DoorWidth + .3f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var wallM = WorldMaterials.Solid(new Color(.22f, .17f, .14f));
            mk.Box(rock, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(wallM, p, s, f.r); solid.Box(wallM, p, s, f.r); }
            mk.Box(timber, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(timber, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) { Wall(fo.P(e * (Door / 2f + seg / 2f), .5f, D / 2f - T / 2f), new Vector3(seg, 1f, T)); Wall(fo.P(e * (Door / 2f + seg / 2f), H - .45f, D / 2f - T / 2f), new Vector3(seg, .9f, T)); Wall(fo.P(e * (Door / 2f + .25f), H / 2f, D / 2f - T / 2f), new Vector3(.5f, H, T)); Wall(fo.P(e * (W / 2f - .4f), H / 2f, D / 2f - T / 2f), new Vector3(.8f, H, T)); mk.Box(GlowM(new Color(1f, .6f, .2f), 1.3f), fo.P(e * (Door / 2f + seg / 2f + .05f), 1.75f, D / 2f - T / 2f), new Vector3(seg - 1.3f, 1.5f, .06f), f.r); solid.Box(wallM, fo.P(e * (Door / 2f + seg / 2f + .05f), 1.75f, D / 2f - T / 2f), new Vector3(seg - 1.3f, 1.5f, .1f), f.r); }
            Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(rust, fo.P(0, H + .08f, 0), new Vector3(W + .8f, .16f, D + .8f), f.r); solid.Box(rust, fo.P(0, H + .08f, 0), new Vector3(W + .8f, .16f, D + .8f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            // the sign on the roof: a great bent nail, the name in hot letters
            var red = new Color(1f, .25f, .1f); mk.Box(boardM, fo.P(0, H + .9f, D / 2f - .3f), new Vector3(6f, 1.3f, .15f), f.r); ArtistBits.Label(ctx.Root, "THE RUSTY NAIL", fo.P(0, H + .9f, D / 2f - .2f), f.Fwd, .42f, red, TextAnchor.MiddleCenter); halos.Add(fo.P(0, H + .9f, D / 2f + .3f), 2.4f, red, .5f);
            mk.Prism(rust, fo.P(4f, H + .1f, D / 2f - .3f), (up + f.Right * .25f).normalized, .12f, 2.4f, 6, .3f); mk.Prism(rust, fo.P(4.6f, H + 2.4f, D / 2f - .3f), (up + f.Right * .25f).normalized, .35f, .1f, 8);
            // inside: the bar along the back, stools, a jukebox, a table
            mk.Box(timber, fo.P(-1f, 1.05f, -D / 2f + 1.7f), new Vector3(6.4f, .1f, .8f), f.r); mk.Box(wallM, fo.P(-1f, .5f, -D / 2f + 1.7f), new Vector3(6.2f, 1f, .6f), f.r); Col(fo.P(-1f, .55f, -D / 2f + 1.7f), new Vector3(6.4f, 1.1f, .8f), f.r);
            for (var k = 0; k < 6; k++) mk.Prism(GlowM(k % 2 == 0 ? new Color(.3f, .9f, .4f) : new Color(1f, .7f, .2f), 1.2f), fo.P(-3.5f + k * 1f, 1.6f, -D / 2f + T + .2f), up, .07f, .32f, 6, .5f); mk.Box(timber, fo.P(-1f, 1.55f, -D / 2f + T + .2f), new Vector3(6f, .06f, .3f), f.r);
            for (var k = 0; k < 4; k++) { var sp = fo.P(-3.4f + k * 1.6f, 0, -D / 2f + 2.7f); mk.Prism(iron, sp, up, .06f, .7f, 6); mk.Prism(new Material(WorldMaterials.Solid(new Color(.5f, .1f, .08f))), sp + up * .7f, up, .24f, .08f, 10); ArtistBits.PostCollider(ctx.Root, sp, .75f, .2f); if (k != 1) Interact.Seat(plife.transform, sp + up * .8f, -f.Fwd, "Sit at the bar"); }
            Folk(fo.P(-3.4f + 1.6f, .78f, -D / 2f + 2.7f), -f.Fwd, Resident.Pose.Sit, new[] { "Rode in for one drink in '82. Bike's still out front.", "Jukebox only has the one kind of song. It's the right kind.", "Don't arm-wrestle Big Ann. Don't." }, Metalhead(Residents.Tool.Mug), null, .78f);
            Folk(fo.P(-1f, 0, -D / 2f + .9f), f.Fwd, Resident.Pose.Work, new[] { "What'll it be? We've got cold, and we've got colder.", "No fighting. Well. No losing.", "Storm glass is on the house. You just hold a glass out the window." }, Metalhead(Residents.Tool.Mug, true)).workRate = .5f;
            var jm = new MeshKit(); var jg = new Material(WorldMaterials.Glow(red, .5f)) { name = "Jukebox glow" }; jm.Box(dark, new Vector3(0, .75f, 0), new Vector3(.9f, 1.5f, .6f), Quaternion.identity); jm.Box(jg, new Vector3(0, 1.15f, .31f), new Vector3(.7f, .5f, .02f), Quaternion.identity); jm.Box(jg, new Vector3(0, .45f, .31f), new Vector3(.6f, .5f, .02f), Quaternion.identity); jm.Prism(jg, new Vector3(-.45f, 1.5f, 0), Vector3.right, .3f, .9f, 8);
            var jgo = jm.BuildSingle("Jukebox", plife.transform, false); jgo.transform.SetPositionAndRotation(fo.P(W / 2f - T - .5f, 0, -1f), Quaternion.LookRotation(-f.Right)); Col(fo.P(W / 2f - T - .5f, .9f, -1f), new Vector3(.6f, 1.8f, .9f), f.r);
            var jl = ArtistBits.PointLight(ctx.Root, fo.P(W / 2f - 1.6f, 1.4f, -1f), red, 7f, .6f, true); plife.jukeM = jg; plife.jukeLight = jl; string[] juke = { "It thinks about it. Then: something with two guitars.", "A drum fill rolls round the room.", "It plays the same one again. Nobody minds." }; var jn = 0;
            var jt = Interact.Toy(jgo, Toy.Kind.Custom, "Kick the jukebox", new Vector3(1.1f, 1.8f, 1f)); jgo.GetComponent<BoxCollider>().center = new Vector3(0, .9f, .2f); jt.onUse = _ => { plife.jukeHit = Time.time; Say(plife.transform, jgo.transform.position + up * 2.2f, juke[jn++ % juke.Length], 4f); };
            var tb = fo.P(2.6f, 0, 1.6f); mk.Prism(timber, tb, up, .1f, .75f, 6); mk.Prism(timber, tb + up * .75f, up, .7f, .07f, 10); ArtistBits.PostCollider(ctx.Root, tb, .8f, .5f);
            foreach (var e in new[] { -1f, 1f }) { var cp = tb + f.Right * e * 1.15f; mk.Box(timber, cp + up * .45f, new Vector3(.45f, .06f, .45f), f.r); mk.Box(timber, cp + up * .22f, new Vector3(.4f, .45f, .4f), f.r); Col(cp + up * .25f, new Vector3(.45f, .5f, .45f), f.r); Interact.Seat(plife.transform, cp + up * .5f, -f.Right * e, "Sit at the table"); }
            Folk(tb + f.Right * 1.15f + up * .5f, -f.Right, Resident.Pose.Sit, new[] { "I'm Big Ann. Sit down. Elbow on the table.", "Undefeated since the anvil fell on my other arm.", "You've got a good grip. For a tourist." }, new Residents.Look { skin = new Color(.8f, .6f, .48f), hair = new Color(.75f, .2f, .15f), top = new Color(.1f, .1f, .11f), bottom = new Color(.2f, .22f, .3f), longHair = true, height = 1.12f }, null, .5f);
            ArtistBits.PointLight(ctx.Root, fo.P(-1f, 2.7f, 0), new Color(1f, .6f, .25f), 13f, 2.8f, true);
            // bikes out front
            for (var k = 0; k < 3; k++)
            {
                var bp = G(f.P(-6.5f + k * 1.7f, 0, D / 2f + 2.6f)); var br = Quaternion.LookRotation(Quaternion.Euler(0, 20f, 0) * f.Fwd) * Quaternion.Euler(0, 0, -8f); var bf = br * Vector3.forward; var bs = br * Vector3.right;
                foreach (var e in new[] { -.75f, .75f }) mk.Prism(dark, bp + up * .34f + bf * e - bs * .07f, bs, .34f, .14f, 10); mk.Box(k == 1 ? rust : iron, bp + up * .6f, new Vector3(.3f, .35f, 1f), br); mk.Box(WorldMaterials.Solid(k == 0 ? new Color(.6f, .1f, .08f) : new Color(.08f, .08f, .09f), .7f), bp + up * .88f + bf * .2f, new Vector3(.32f, .22f, .5f), br); mk.Box(dark, bp + up * .82f - bf * .35f, new Vector3(.3f, .1f, .5f), br);
                mk.Box(chain, bp + up * .8f + bf * .85f, new Vector3(.05f, .9f, .05f), br * Quaternion.Euler(-25f, 0, 0)); mk.Box(chain, bp + up * 1.2f + bf * .65f, new Vector3(.7f, .04f, .04f), br); Col(bp + up * .6f, new Vector3(.5f, 1.2f, 2.1f), br); Interact.Seat(plife.transform, bp + up * .9f - bf * .35f, bf, "Sit on the bike");
            }
            IronBench(mk, f.P(4.2f, 0, D / 2f + 1.2f), f.Fwd, 2f); Brazier(ctx, mk, G(f.P(-Door / 2f - 1.4f, 0, D / 2f + 1.2f)));
            Lane(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 10f);
            Shot(ctx, "the rusty nail", f.P(-8f, 2.4f, D / 2f + 12f), fo.P(0, 2.4f, 0));
            Shot(ctx, "inside the rusty nail", fo.P(3.5f, 1.6f, D / 2f - 1f), fo.P(-2f, 1.2f, -D / 2f + 1.5f));
        }

        // ---------------------------------------------------------------- the Wall of Amps
        void WallOfAmps(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(AmpsAt.x, AmpsAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float SW = 14f, SD = 6f, SH = .9f;
            var st = f.P(0, 0, -6f); mk.Box(boardM, st + up * (SH / 2f - .3f), new Vector3(SW, SH + .6f, SD), f.r); solid.Box(boardM, st + up * (SH / 2f - .3f), new Vector3(SW, SH + .6f, SD), f.r); var so = new PlaceFrame(st + up * SH, f.Fwd);
            for (var k = 0; k < 3; k++) { var sp = st + f.Fwd * (SD / 2f + .2f + k * .32f) + up * (SH - .3f - k * .3f - .15f); mk.Box(iron, sp, new Vector3(2.4f, .3f, .34f), f.r); solid.Box(iron, sp, new Vector3(2.4f, .3f, .34f), f.r); }
            // the wall: five stacks, three high, grilles that glow with the dial
            var grille = new Material(WorldMaterials.Glow(Molten, .3f)) { name = "Amp grilles" }; plife.ampM = grille;
            for (var x = 0; x < 6; x++) for (var y = 0; y < 3; y++) { var p = so.P(-5.5f + x * 2.2f, .75f + y * 1.5f, -SD / 2f + .8f); mk.Box(dark, p, new Vector3(2f, 1.45f, 1f), f.r); mk.Box(grille, p + f.Fwd * .51f - up * .1f, new Vector3(1.7f, 1.05f, .02f), f.r); mk.Box(chain, p + f.Fwd * .51f + up * .58f, new Vector3(1.7f, .12f, .02f), f.r); }
            Col(so.P(0, 2.25f, -SD / 2f + .8f), new Vector3(13.2f, 4.5f, 1f), f.r);
            plife.ampLight = ArtistBits.PointLight(ctx.Root, so.P(0, 2.4f, 1.5f), Molten, 26f, 1.2f);
            // the dial: it goes to eleven
            var dp = so.P(5.5f, 0, 1.6f); mk.Box(dark, dp + up * .55f, new Vector3(.6f, 1.1f, .5f), f.r); Col(dp + up * .55f, new Vector3(.6f, 1.1f, .5f), f.r);
            var dm = new MeshKit(); dm.Prism(chain, new Vector3(0, 0, 0), Vector3.forward, .2f, .1f, 12); dm.Box(GlowM(Molten, 3f), new Vector3(0, .12f, .11f), new Vector3(.04f, .16f, .02f), Quaternion.identity); var dgo = dm.BuildSingle("Volume dial", plife.transform, false); dgo.transform.SetPositionAndRotation(dp + up * .8f + f.Fwd * .26f, f.r * Quaternion.Euler(0, 0, 120f)); plife.dial = dgo.transform; plife.dialRot = f.r;
            ArtistBits.Label(ctx.Root, "VOLUME", dp + up * .4f + f.Fwd * .26f, f.Fwd, .09f, Ink, TextAnchor.MiddleCenter);
            var dt = Interact.Toy(dgo, Toy.Kind.Custom, "Turn it up", new Vector3(.9f, .9f, .7f)); dt.onUse = _ => { plife.volume = plife.volume >= 11 ? 1 : Mathf.Min(11, plife.volume + 2); Say(plife.transform, dp + up * 2f, plife.volume >= 11 ? "ELEVEN." : plife.volume + ".", 2f); };
            // a mic stand, a drum riser, a guitar left leaning
            var mp = so.P(0, 0, 1.8f); mk.Prism(chain, mp, up, .03f, 1.5f, 5); mk.Prism(chain, mp, up, .22f, .04f, 8); mk.Blob(dark, mp + up * 1.55f, Vector3.one * .07f, ctx.Rng, 0f); ArtistBits.PostCollider(ctx.Root, mp, 1.5f, .08f);
            var dr = so.P(-2.5f, 0, .2f); mk.Prism(dark, dr + up * .0f, up, .6f, .5f, 12); mk.Prism(WorldMaterials.Solid(Bone), dr + up * .5f, up, .58f, .03f, 12); foreach (var e in new[] { -1f, 1f }) { mk.Prism(dark, dr + f.Right * e * .9f + up * .5f, up, .3f, .3f, 10); mk.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .8f), dr + f.Right * e * 1.3f + up * 1.2f - f.Fwd * .3f, up, .35f, .02f, 10); mk.Prism(chain, dr + f.Right * e * 1.3f - f.Fwd * .3f, up, .02f, 1.2f, 4); }
            Col(dr + up * .5f, new Vector3(3f, 1f, 1.4f), f.r);
            // the crowd: two rows, heads going
            string[] crowd = { "WHAT?", "I SAID IT'S GOOD!", "They haven't come on yet. Four years now. Worth the wait.", "Turn it up! No, the dial, by the stage. UP!", "My ears stopped ringing once. Worst day of my life." };
            for (var k = 0; k < 9; k++) { var row = k / 5; var p = G(f.P(-4.4f + (k % 5) * 2.2f + row * 1.1f, 0, 1.2f + row * 2.2f)); var r = Folk(p, FaceTo(p, st), Resident.Pose.Wave, crowd, Metalhead()); r.workRate = 2.2f; }
            IronBench(mk, f.P(-8.5f, 0, 6.5f), FaceTo(f.P(-8.5f, 0, 6.5f), st), 2.2f); IronBench(mk, f.P(8.5f, 0, 6.5f), FaceTo(f.P(8.5f, 0, 6.5f), st), 2.2f);
            foreach (var e in new[] { -1f, 1f }) Brazier(ctx, mk, G(f.P(e * 8.5f, 0, -2f)));
            IronSign(mk, f.P(7f, 0, 11f), f.Fwd, "THE WALL OF AMPS\nit goes to eleven", 3.2f, .15f);
            Lane(ctx, mk, f.P(0, 0, 9f)); Ring(g, 12f);
            Shot(ctx, "the wall of amps", f.P(-7f, 3.2f, 13f), so.P(0, 2f, 0));
        }

        // ---------------------------------------------------------------- the Doom Bell
        void DoomBell(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BellAt.x, BellAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float BH = 6f;
            foreach (var e in new[] { -1f, 1f }) { foreach (var z in new[] { -1f, 1f }) { var foot = G(f.P(e * 2.2f, 0, z * 1.4f)); var top = g + f.Right * e * 2f + up * BH; mk.Prism(timber, foot - up * .3f, (top - foot).normalized, .2f, Vector3.Distance(foot, top) + .3f, 5); } ArtistBits.PostCollider(ctx.Root, G(f.P(e * 2.1f, 0, 0)), BH, .45f); }
            mk.Box(timber, g + up * BH, new Vector3(5f, .4f, .4f), f.r);
            var bm = new MeshKit(); bm.Box(chain, new Vector3(0, -.3f, 0), new Vector3(.1f, .6f, .1f), Quaternion.identity); bm.Prism(iron, new Vector3(0, -2.9f, 0), Vector3.up, 1.35f, 2.3f, 12, .45f); bm.Prism(rust, new Vector3(0, -2.95f, 0), Vector3.up, 1.45f, .2f, 12); bm.Prism(dark, new Vector3(0, -3.3f, 0), Vector3.up, .22f, .5f, 6);
            var bgo = bm.BuildSingle("Doom bell", plife.transform, false); bgo.transform.SetPositionAndRotation(g + up * (BH - .2f), f.r); var bs = bgo.AddComponent<Sway>(); bs.axis = Vector3.right; bs.degrees = 26f; bs.speed = 1.6f; bs.decay = .3f; bs.seconds = 14f;
            var blt = ArtistBits.PointLight(ctx.Root, g + up * 2.2f, new Color(1f, .3f, .15f), 30f, 0f); string[] toll = { "DOOOOM. It rolls out over the whole range.", "The ground hums. Somewhere a crow gives up and leaves.", "Rain starts, or seems to." }; var tn = 0;
            var bt = Interact.Toy(bgo, Toy.Kind.Custom, "Toll the bell", new Vector3(2.8f, 2.6f, 2.8f)); bgo.GetComponent<BoxCollider>().center = new Vector3(0, -2f, 0); bt.onUse = _ => { bs.Push(); plife.Flash(blt, 4f, .5f); Say(plife.transform, g + up * 4.4f + f.Fwd * 2f, toll[tn++ % toll.Length], 5f); };
            IronBench(mk, f.P(-4.5f, 0, 3f), FaceTo(f.P(-4.5f, 0, 3f), g), 2f); Brazier(ctx, mk, G(f.P(4.2f, 0, 3.2f)));
            Folk(G(f.P(-2.8f, 0, 4.6f)), FaceTo(f.P(-2.8f, 0, 4.6f), g), Resident.Pose.Stand, new[] { "It tolls itself when it rains. It always rains.", "Three notes, that's all doom needs. This has the one. It's enough.", "Ring it. Everyone east of here will stop and look up. I love that." },
                new Residents.Look { skin = new Color(.9f, .85f, .8f), hair = new Color(.1f, .1f, .1f), top = new Color(.08f, .07f, .09f), bottom = new Color(.08f, .07f, .09f), longHair = true, height = 1.05f });
            IronSign(mk, f.P(5f, 0, 7f), f.Fwd, "THE DOOM BELL", 2.4f);
            Ring(g, 6f);
            Shot(ctx, "the doom bell", f.P(-6f, 2.4f, 9f), g + up * 3.4f);
        }
    }

    /// The Iron places' moving parts: lights that flash and fade, the striker's puck, the amps, the signal fire, the jukebox.
    public class IronPlacesLife : MonoBehaviour
    {
        readonly List<(Light l, float peak, float t0, float rate)> flashes = new List<(Light, float, float, float)>();
        public Transform puck; public Vector3 puckBase; public float puckTop; public Light bellLight; float struck = -99f, power;
        public Material ampM, signalM, jukeM; public Light ampLight, signalLight, jukeLight; public int volume = 3; public bool signalOn; public float jukeHit = -99f;
        public Transform dial; public Quaternion dialRot; float shown = 3f, sig;
        static readonly Color Molten = new Color(1f, .45f, .08f), Red = new Color(1f, .25f, .1f);

        public void Flash(Light l, float peak, float rate = 3f) { if (l == null) return; flashes.RemoveAll(f => f.l == l); flashes.Add((l, peak, Time.time, rate)); }
        public float Strike() { struck = Time.time; power = Mathf.Clamp01(.35f + Mathf.PerlinNoise(Time.time * 7.3f, 1.7f) * .9f); if (Random.value < .2f) power = 1f; return power; }

        void Update()
        {
            for (var i = flashes.Count - 1; i >= 0; i--) { var f = flashes[i]; var v = f.peak * Mathf.Exp(-(Time.time - f.t0) * f.rate); f.l.intensity = v; if (v < .02f) { f.l.intensity = 0f; flashes.RemoveAt(i); } }
            if (puck != null)
            {
                var age = Time.time - struck; var h = 0f; const float up = .45f;
                if (age < up) h = power * Mathf.Sin(age / up * Mathf.PI * .5f); else if (age < up + 1.2f) h = power * (1f - Mathf.Pow((age - up) / 1.2f, 2f));
                puck.position = puckBase + Vector3.up * h * puckTop;
                if (bellLight != null && power >= .99f && age >= up && age < up + .1f) Flash(bellLight, 6f, 2f);
            }
            shown = Mathf.MoveTowards(shown, volume, Time.deltaTime * 8f);
            if (ampM != null) { var beat = .75f + .25f * Mathf.Sin(Time.time * (4f + shown)); var v = shown / 11f; ampM.SetColor("_EmissionColor", Molten * (.2f + v * v * 5f * beat)); if (ampLight != null) ampLight.intensity = .6f + v * v * 4f * beat; }
            if (dial != null) dial.rotation = dialRot * Quaternion.Euler(0, 0, 135f - shown / 11f * 270f);
            sig = Mathf.MoveTowards(sig, signalOn ? 1f : 0f, Time.deltaTime * .8f);
            if (signalM != null) { var fl = .8f + .2f * Mathf.PerlinNoise(Time.time * 6f, 0f); signalM.SetColor("_EmissionColor", Molten * (.15f + sig * 5f * fl)); if (signalLight != null) signalLight.intensity = sig * 4f * fl; }
            if (jukeM != null) { var a = Time.time - jukeHit; var k = a < 6f ? 1f + 3f * Mathf.Abs(Mathf.Sin(a * 6f)) * (1f - a / 6f) : 1f; jukeM.SetColor("_EmissionColor", Red * .5f * k); if (jukeLight != null) jukeLight.intensity = .6f * k; }
        }
    }
}
