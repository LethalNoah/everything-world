using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Power Metal Peaks laid out on purpose (the build-land standard). The castle, the cove and every courtyard
    /// stay where they were; two more torch-lit flagstone roads climb out to the empty north and down the long south
    /// spur. Along them, with the people of the Peaks: the Mead Hall (a great timber hall to walk into: long tables,
    /// a hearth, horns to raise) and the Tournament Field (a quintain to strike, an archery butt, a stand of banners)
    /// in the north; the Wizard's Tower (a crystal ball that answers, floating books, a familiar) and the Neko Metal
    /// Café (pink neon, parfaits, a capsule machine) in the south, the fierce half and the kawaii half of the land.
    public partial class PowerKit
    {
        static readonly Vector2 HallAt = new Vector2(7106, 6040), TourneyAt = new Vector2(7190, 6092), TowerAt = new Vector2(6866, 5440), CafeAt = new Vector2(6846, 5288);
        static readonly Vector2[] NorthRoad = { new Vector2(7010, 5882), new Vector2(7046, 5946), new Vector2(7078, 6002), new Vector2(7132, 6064), new Vector2(7166, 6082) };
        static readonly Vector2[] SouthRoad = { new Vector2(6960, 5600), new Vector2(6932, 5522), new Vector2(6902, 5452), new Vector2(6882, 5362), new Vector2(6864, 5310) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (HallAt, 14f), (TourneyAt, 13f), (TowerAt, 9f), (CafeAt, 10f) };

        Material flagM;
        PowerPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> flagP = new List<Vector3>();

        static readonly Color[] Tunics = { new Color(.5f, .14f, .12f), new Color(.2f, .25f, .45f), new Color(.35f, .3f, .22f), new Color(.45f, .2f, .7f), new Color(.6f, .6f, .65f), new Color(.2f, .35f, .25f) };
        static readonly Color[] Hose = { new Color(.22f, .2f, .2f), new Color(.35f, .28f, .2f), new Color(.2f, .22f, .3f) };
        static readonly string[] PeakTalk =
        {
            "HAIL! ...Sorry. It's the mountain air. Everything comes out as a battle cry.", "There's a dragon on the castle. It's a nice dragon. It has to be, it's been there for years.", "Every song here is about a sword. Even the love songs. Especially the love songs.",
            "Faster! Higher! ...That's the whole philosophy, actually.", "Go down the south road and it all turns pink. Nobody knows why. We like it.",
        };

        Residents.Look Warrior(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tunics, Hose, new[] { Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Beanie }, new[] { new Color(.6f, .6f, .65f), new Color(.5f, .14f, .12f) }); l.tool = tool; l.apron = apron; l.longHair = prng.Next(3) != 0; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Warrior(), pose, prng, lines ?? PeakTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapePeaks(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
        }

        /// A shield on a post with the name under it. Signs on the +x side of a place's road; the photo spot on the −x side.
        void ShieldSign(MeshKit mk, Vector3 at, Vector3 face, string text, Material cloth_, float size = .2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            mk.Prism(woodDark, g - up * .2f, up, .1f, 3f, 6); mk.Box(woodDark, g + up * 1.5f, new Vector3(3f, .8f, .1f), rot); mk.Prism(cloth_, g + up * 2.5f - face * .0f, face, .6f, .1f, 6); mk.Prism(goldM, g + up * 2.5f + face * .1f, face, .2f, .04f, 6); Col(g + up * 1.5f, new Vector3(3f, 3f, .2f), rot);
            ArtistBits.Label(rootT, text, g + up * 1.5f + face * .06f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        void OakBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2.4f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .45f, new Vector3(len, .1f, .5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(woodDark, g + up * .22f + side * e * (len / 2f - .25f), new Vector3(.14f, .45f, .45f), rot);
            Col(g + up * .3f, new Vector3(len, .6f, .5f), rot); Interact.Bench(plife.transform, g + up * .54f - side * (len / 2f - .45f), g + up * .54f + side * (len / 2f - .45f), face);
        }

        List<Vector3> FlagRoad(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 200f * 200f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2f);
            for (var i = 0; i < pts.Count; i++) { foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } } foreach (var c in noGrass) { if (c.y < 8f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } } }
            if (pts.Count < 2) return pts;
            Strip(mk, pts, flagM, PathW, .2f, ctx); foreach (var p in pts) Remember(p, 700);
            var up = Vector3.up; var n = 0;
            for (var i = 6; i < pts.Count; i += 13) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d) * (n % 2 == 0 ? 1f : -1f); var lp = pts[i] + sd * (PathW / 2f + .9f); if (ctx.IsLand(lp.x, lp.z, 2f) && !InNoGrass(lp.x, lp.z, 1f)) { if (n % 4 == 3) Banner(ctx, mk, ctx.OnGround(lp.x, lp.z), -sd, n % 8 == 3 ? bannerRed : bannerPurple); else Torch(ctx, mk, ctx.OnGround(lp.x, lp.z)); } n++; }
            flagP.AddRange(pts);
            if (pts.Count > 40) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = pts[i] - sd * 3.2f; if (ctx.IsLand(bp.x, bp.z, 2f)) { OakBench(mk, bp, sd); Ring(bp, 2f); } }
            return pts;
        }

        void Flagstones(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(flagP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.6f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, flagM, 2.2f, .18f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void PeakPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            flagP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1999); flagM = WorldMaterials.Solid(new Color(.42f, .37f, .34f));
            plife = new GameObject("Power places life").AddComponent<PowerPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            FlagRoad(ctx, mk, NorthRoad); FlagRoad(ctx, mk, SouthRoad);
            MeadHall(ctx, mk, solid); Tournament(ctx, mk, solid); WizardTower(ctx, mk, solid); NekoCafe(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Mead Hall
        void MeadHall(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(HallAt.x, HallAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(flagP, g))); const float W = 11f, D = 18f, H = 3.6f, T = .4f, Door = 2.4f;
            var fo = new PlaceFrame(g + up * .3f, f.Fwd);
            mk.Box(wallStone, g - up * .35f, new Vector3(W + .8f, 1.3f, D + .8f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(woodDark, p, s, f.r); solid.Box(woodDark, p, s, f.r); }
            mk.Box(wood, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(wood, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, H - .3f, D / 2f - T / 2f), new Vector3(Door, .6f, T));
            // a steep roof open to the rafters inside, crossed dragon heads on the gable
            mk.GableRoof(wood, fo.P(0, H, 0), new Vector3(W + 1.2f, 0, D + 1.2f), 5f, f.r); solid.Box(wood, fo.P(0, H + 2.2f, 0), new Vector3(W * .5f, .4f, D), f.r); foreach (var e in new[] { -1f, 1f }) { solid.Box(wood, fo.P(e * W * .3f, H + 1f, 0), new Vector3(W * .45f, .3f, D), f.r * Quaternion.Euler(0, 0, -e * 42f)); mk.Prism(goldM, fo.P(e * .4f, H + 5f, D / 2f + .5f), (up + f.Right * e * .8f + f.Fwd * .3f).normalized, .16f, 1.6f, 5, .2f); }
            for (var k = 0; k < 4; k++) mk.Box(woodDark, fo.P(0, H + .1f, -D / 2f + 2.5f + k * 4.3f), new Vector3(W - T, .3f, .3f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .9f, H, D - .9f));
            foreach (var e in new[] { -1f, 1f }) { Banner(ctx, mk, G(f.P(e * (W / 2f + 1.6f), 0, D / 2f + 1.5f)), f.Fwd, e < 0 ? bannerRed : bannerPurple); Brazier(ctx, mk, G(f.P(e * 2.6f, 0, D / 2f + 2.4f))); }
            // the long hearth down the middle, two long tables with benches, the high seat at the far end
            mk.Box(wallDark, fo.P(0, .15f, -1f), new Vector3(1.2f, .3f, 7f), f.r); mk.Box(fireM, fo.P(0, .34f, -1f), new Vector3(.8f, .1f, 6.4f), f.r); Col(fo.P(0, .2f, -1f), new Vector3(1.2f, .4f, 7f), f.r); for (var k = 0; k < 3; k++) { halos.Add(fo.P(0, 1f, -3.5f + k * 2.5f), 1.8f, Fire, .7f); life.torches.Add(fo.P(0, .6f, -3.5f + k * 2.5f)); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2f, -1f), Fire, 16f, 3f, true); ArtistBits.PointLight(ctx.Root, fo.P(0, 2.4f, 5f), Fire, 12f, 2f, true);
            string[] feast = { "SKÅL! To the dragon! To the other dragon! To dragons generally!", "Eat! There's a whole boar. There's always a whole boar. Nobody's ever seen a half one.", "I slew a troll once. Well. I out-sang one. Same thing up here.", "Raise a horn with us! It's rude not to and we are VERY polite." };
            foreach (var e in new[] { -1f, 1f })
            {
                var tx = e * 2.9f; mk.Box(wood, fo.P(tx, .8f, -1f), new Vector3(1f, .1f, 8f), f.r); foreach (var z in new[] { -4.5f, -1f, 2.5f }) mk.Box(woodDark, fo.P(tx, .4f, z), new Vector3(.8f, .8f, .16f), f.r); Col(fo.P(tx, .42f, -1f), new Vector3(1f, .85f, 8f), f.r);
                foreach (var s in new[] { -1f, 1f }) { var bx = tx + s * .95f; mk.Box(wood, fo.P(bx, .45f, -1f), new Vector3(.4f, .08f, 8f), f.r); Col(fo.P(bx, .25f, -1f), new Vector3(.4f, .5f, 8f), f.r); for (var k = 0; k < 4; k++) { var sp = fo.P(bx, .5f, -4f + k * 2f); var taken = (k + (e > 0 ? 1 : 0) + (s > 0 ? 2 : 0)) % 4 == 0; if (taken) Folk(sp, f.Right * -s, Resident.Pose.Sit, feast, Warrior(Residents.Tool.Mug), null, .5f); else Interact.Seat(plife.transform, sp + up * .04f, f.Right * -s, "Sit at the long table"); } }
                for (var k = 0; k < 3; k++) { var hm = new MeshKit(); var horn = WorldMaterials.Solid(new Color(.85f, .8f, .65f)); hm.Prism(horn, Vector3.zero, new Vector3(.3f, 1f, 0).normalized, .06f, .28f, 6, .25f); hm.Prism(goldM, new Vector3(.08f, .26f, 0), new Vector3(.3f, 1f, 0).normalized, .065f, .03f, 6); var go = hm.BuildSingle("Drinking horn", plife.transform, false); go.transform.position = fo.P(tx, .86f, -3.4f + k * 2.4f); var c = Interact.Carry(go, "a horn of mead", .3f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.2f, .3f, .14f); bc.center = new Vector3(.04f, .14f, 0); } }
                mk.Blob(WorldMaterials.Solid(new Color(.55f, .32f, .2f)), fo.P(tx, .98f, 1.5f), new Vector3(.3f, .16f, .5f), prng, .1f); mk.Prism(WorldMaterials.Solid(new Color(.8f, .7f, .45f)), fo.P(tx, .86f, -1f), up, .3f, .1f, 10);
            }
            var hs = fo.P(0, 0, -D / 2f + T + 1f); mk.Box(wallDark, hs + up * .2f, new Vector3(4f, .4f, 2f), f.r); solid.Box(wallDark, hs + up * .2f, new Vector3(4f, .4f, 2f), f.r); mk.Box(woodDark, hs + up * .85f, new Vector3(1.2f, .14f, 1f), f.r); mk.Box(woodDark, hs + up * 1.9f - f.Fwd * .45f, new Vector3(1.2f, 2.2f, .14f), f.r); foreach (var e in new[] { -.6f, .6f }) mk.Prism(goldM, hs + up * 3f - f.Fwd * .45f + f.Right * e, up, .08f, .4f, 5, .2f); Col(hs + up * .7f, new Vector3(1.2f, .7f, 1f), f.r); Interact.Seat(plife.transform, hs + up * .94f, f.Fwd, "Sit in the high seat");
            // the horn to sound, the bard by the door
            var hp = fo.P(-W / 2f + T + .5f, 0, D / 2f - 2f); mk.Prism(woodDark, hp, up, .08f, 1.5f, 6); var bh = new MeshKit(); bh.Prism(WorldMaterials.Solid(new Color(.85f, .8f, .65f)), Vector3.zero, new Vector3(1f, .35f, 0).normalized, .18f, 1.4f, 8, .15f); bh.Prism(goldM, new Vector3(-.02f, 0, 0), new Vector3(1f, .35f, 0).normalized, .2f, .1f, 8); var bgo = bh.BuildSingle("Great horn", plife.transform, false); bgo.transform.SetPositionAndRotation(hp + up * 1.5f, f.r); ArtistBits.PostCollider(ctx.Root, hp, 1.5f, .1f);
            Interact.Toy(bgo, Toy.Kind.Custom, "Sound the horn", new Vector3(1.6f, .8f, .8f)).onUse = _ => { plife.horn = Time.time; Say(plife.transform, hp + up * 2.8f, "BWWAAAAAAAH. Every horn in the hall goes up. \"SKÅL!\"", 5f); };
            plife.hallLight = ArtistBits.PointLight(ctx.Root, fo.P(0, 3f, 2f), Gold, 18f, 0f, true);
            Folk(fo.P(W / 2f - T - 1f, 0, D / 2f - 2.2f), -f.Right, Resident.Pose.Work, new[] { "I know one hundred and forty verses of the Saga of the Sword. I'm on verse nine. Sit down.", "Every verse ends in a key change. That's how you know it's a new verse.", "The chorus is just \"FIRE\" eight times. Join in when you're ready." }, Warrior(Residents.Tool.Rod)).workRate = 1.1f;
            Flagstones(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 13f);
            Shot(ctx, "the mead hall", f.P(-9f, 3.4f, D / 2f + 14f), fo.P(0, 4f, 0));
            Shot(ctx, "inside the mead hall", fo.P(1.4f, 1.7f, D / 2f - 1.2f), fo.P(0, 1.2f, -D / 2f + 1.5f));
        }

        // ---------------------------------------------------------------- the Tournament Field
        void Tournament(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TourneyAt.x, TourneyAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(flagP, g)));
            // the lists: a long tilt rail down the middle of a sand strip, banners at both ends
            var sand = WorldMaterials.Solid(new Color(.62f, .52f, .38f)); mk.Box(sand, g + up * .04f, new Vector3(22f, .12f, 9f), f.r); solid.Box(sand, g - up * .1f, new Vector3(22f, .3f, 9f), f.r);
            mk.Box(wood, g + up * 1f, new Vector3(18f, .12f, .12f), f.r); for (var k = 0; k < 7; k++) { mk.Box(woodDark, g + up * .5f + f.Right * (-9f + k * 3f), new Vector3(.14f, 1f, .14f), f.r); mk.Box(k % 2 == 0 ? bannerRed : bannerPurple, g + up * .65f + f.Right * (-7.5f + k * 3f), new Vector3(2.6f, .55f, .04f), f.r); } Col(g + up * .55f, new Vector3(18f, 1.1f, .2f), f.r);
            foreach (var e in new[] { -1f, 1f }) { Banner(ctx, mk, G(g + f.Right * e * 11.5f - f.Fwd * 3f), -f.Right * e, e < 0 ? bannerRed : bannerPurple); Banner(ctx, mk, G(g + f.Right * e * 11.5f + f.Fwd * 3f), -f.Right * e, e < 0 ? bannerPurple : bannerRed); }
            // the quintain: a post with a pivoting arm, a shield on one end and a sandbag on the other. Strike it and it whirls.
            var qp = g + f.Right * 5f + f.Fwd * 2.4f + up * .1f; mk.Prism(woodDark, qp, up, .14f, 2.4f, 6); ArtistBits.PostCollider(ctx.Root, qp, 2.4f, .16f); var qm = new MeshKit(); qm.Box(wood, Vector3.zero, new Vector3(3.2f, .12f, .12f), Quaternion.identity); qm.Prism(bannerRed, new Vector3(-1.6f, -.1f, -.06f), Vector3.forward, .45f, .1f, 6); qm.Prism(goldM, new Vector3(-1.6f, -.1f, .05f), Vector3.forward, .16f, .04f, 6); qm.Box(iron, new Vector3(1.6f, -.4f, 0), new Vector3(.03f, .7f, .03f), Quaternion.identity); qm.Blob(WorldMaterials.Solid(new Color(.5f, .42f, .3f)), new Vector3(1.6f, -.95f, 0), new Vector3(.24f, .32f, .24f), prng, .1f);
            var qgo = qm.BuildSingle("Quintain", plife.transform, false); qgo.transform.SetPositionAndRotation(qp + up * 2.4f, f.r); var qt = Interact.Toy(qgo, Toy.Kind.Spin, "Strike the quintain", new Vector3(3.4f, 1.6f, 1.2f)); qgo.GetComponent<BoxCollider>().center = new Vector3(0, -.5f, 0); qt.axis = Vector3.up; qt.amount = 1260f; qt.seconds = 5f;
            // an archery butt with a painted target and arrows already in it; a rack of wooden swords to carry about
            var ab = G(g - f.Right * 7f - f.Fwd * 6f); var aq = Quaternion.LookRotation(FaceTo(ab, g)); mk.Prism(WorldMaterials.Solid(new Color(.8f, .7f, .45f)), ab + up * 1.2f - aq * Vector3.forward * .2f, aq * Vector3.forward, 1f, .4f, 14); mk.Prism(bannerRed, ab + up * 1.2f + aq * Vector3.forward * .21f, aq * Vector3.forward, .7f, .01f, 14); mk.Prism(WorldMaterials.Solid(new Color(.95f, .95f, .9f)), ab + up * 1.2f + aq * Vector3.forward * .22f, aq * Vector3.forward, .45f, .01f, 12); mk.Prism(goldM, ab + up * 1.2f + aq * Vector3.forward * .23f, aq * Vector3.forward, .18f, .01f, 10); foreach (var e in new[] { -.6f, .6f }) mk.Prism(woodDark, ab + aq * Vector3.right * e - aq * Vector3.forward * .5f, (up * 1.6f + aq * Vector3.forward * .5f).normalized, .06f, 2f, 5); Col(ab + up * 1.1f, new Vector3(2f, 2.2f, .8f), aq);
            for (var k = 0; k < 3; k++) mk.Prism(wood, ab + up * (1.1f + k * .2f) + aq * Vector3.right * (-.3f + k * .28f) + aq * Vector3.forward * .22f, (aq * Vector3.forward + up * .1f).normalized, .015f, .7f, 4);
            var rack = G(g - f.Right * 3f + f.Fwd * 6.2f); mk.Box(woodDark, rack + up * .9f, new Vector3(2.4f, .08f, .3f), f.r); foreach (var e in new[] { -1.1f, 1.1f }) mk.Box(woodDark, rack + up * .45f + f.Right * e, new Vector3(.1f, .9f, .3f), f.r); Col(rack + up * .5f, new Vector3(2.4f, 1f, .4f), f.r);
            for (var k = 0; k < 4; k++) { var sm = new MeshKit(); sm.Box(wood, new Vector3(0, .5f, 0), new Vector3(.07f, .9f, .03f), Quaternion.identity); sm.Box(woodDark, new Vector3(0, .08f, 0), new Vector3(.28f, .05f, .05f), Quaternion.identity); sm.Box(woodDark, new Vector3(0, -.05f, 0), new Vector3(.05f, .22f, .05f), Quaternion.identity); var go = sm.BuildSingle("Wooden sword", plife.transform, false); go.transform.SetPositionAndRotation(rack + up * 1.1f + f.Right * (-.75f + k * .5f), f.r * Quaternion.Euler(0, 0, 180f)); var c = Interact.Carry(go, "a wooden practice sword", .6f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.3f, 1.2f, .08f); bc.center = new Vector3(0, .4f, 0); } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            // the stand: three tiers to sit on under an awning, two spectators; the herald
            var sc = G(g - f.Fwd * 7.5f + f.Right * 2f); for (var k = 0; k < 3; k++) { var tp = sc - f.Fwd * (k * .9f) + up * (.3f + k * .45f); mk.Box(wood, tp, new Vector3(9f, .5f + k * .9f, .9f), f.r); solid.Box(wood, tp, new Vector3(9f, .5f + k * .9f, .9f), f.r); Interact.Bench(plife.transform, tp + up * (.3f + k * .45f) - f.Right * 3f, tp + up * (.3f + k * .45f) + f.Right * 1f, f.Fwd); }
            foreach (var e in new[] { -4.4f, 4.4f }) { mk.Box(woodDark, sc - f.Fwd * 2.4f + f.Right * e + up * 2.2f, new Vector3(.14f, 4.4f, .14f), f.r); mk.Box(woodDark, sc + f.Fwd * .6f + f.Right * e + up * 1.8f, new Vector3(.14f, 3.6f, .14f), f.r); } for (var s = 0; s < 6; s++) mk.Box(s % 2 == 0 ? bannerRed : cloth, sc - f.Fwd * .9f + f.Right * (-3.75f + s * 1.5f) + up * 4f, new Vector3(1.5f, .05f, 3.4f), f.r * Quaternion.Euler(-12f, 0, 0));
            Folk(sc - f.Fwd * .9f + up * 1.05f + f.Right * 2.6f, f.Fwd, Resident.Pose.Sit, new[] { "My money's on the red knight. There's only a red knight. I like my odds.", "Hit the quintain and duck. Everyone forgets the second part." }, Warrior(Residents.Tool.Mug), null, .5f);
            Folk(G(g + f.Right * 9f + f.Fwd * 5.5f), -f.Right, Resident.Pose.Wave, new[] { "HEAR YE! The lists are OPEN! They have always been open! Nobody has ever closed them!", "Take a sword! They're wood! Mostly!", "A champion approaches! ...Is it you? It might be you." }, Warrior());
            ShieldSign(mk, f.P(8f, 0, 9f), f.Fwd, "TOURNAMENT FIELD", bannerRed, .18f);
            Flagstones(ctx, mk, f.P(0, 0, 6.5f)); Ring(g, 13f);
            Shot(ctx, "the tournament field", f.P(-11f, 5f, 13f), g + up * 1.2f);
        }

        // ---------------------------------------------------------------- the Wizard's Tower
        void WizardTower(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TowerAt.x, TowerAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(flagP, g))); const float R = 4.4f, H = 4f, Door = HumanScale.DoorWidth + .3f; const int N = 10; var violet = new Color(.65f, .4f, 1f);
            mk.Prism(wallDark, g - up * .5f, up, R + .8f, .8f, N); solid.Prism(wallDark, g - up * .5f, up, R + .8f, .6f, N); var fo = g + up * .28f; mk.Prism(woodDark, fo - up * .03f, up, R - .2f, .06f, N);
            for (var k = 0; k < N; k++)
            {
                var a0 = (k - .5f) * Mathf.PI * 2f / N; var a1 = (k + .5f) * Mathf.PI * 2f / N; var d0 = f.r * new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)); var d1 = f.r * new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)); var p0 = fo + d0 * R; var p1 = fo + d1 * R; var mid = (p0 + p1) / 2f; var q = Quaternion.LookRotation(p1 - p0); var len = Vector3.Distance(p0, p1) + .05f;
                if (k == 0) { var sd = (p1 - p0).normalized; var side = (len - Door) / 2f; foreach (var e in new[] { -1f, 1f }) { var c = mid + sd * e * (Door / 2f + side / 2f); mk.Box(wallStone, c + up * (H / 2f), new Vector3(.4f, H, side), q); solid.Box(wallStone, c + up * (H / 2f), new Vector3(.4f, H, side), q); } mk.Box(wallStone, mid + up * (H - .45f), new Vector3(.4f, .9f, Door), q); solid.Box(wallStone, mid + up * (H - .45f), new Vector3(.4f, .9f, Door), q); }
                else { mk.Box(wallStone, mid + up * (H / 2f), new Vector3(.4f, H, len), q); solid.Box(wallStone, mid + up * (H / 2f), new Vector3(.4f, H, len), q); if (k % 3 == 1) mk.Box(GlowM(violet, 1.6f), mid + up * 2.2f + (mid - fo).normalized * .21f, new Vector3(.03f, 1.2f, .45f), q); }
                // the tower goes on up above the room: a second, narrower stage and a tall crooked hat of a roof
                mk.Box(wallDark, mid + up * (H + 3f) - (mid - fo).normalized * .5f, new Vector3(.4f, 6f, len * .9f), q);
            }
            mk.Prism(woodDark, fo + up * H, up, R + .5f, .3f, N); solid.Prism(woodDark, fo + up * H, up, R + .3f, .3f, N); mk.Prism(wallDark, fo + up * (H + 6f), up, R + .2f, .4f, N); mk.Cone(WorldMaterials.Solid(new Color(.3f, .16f, .5f)), fo + up * (H + 6.4f) + f.Right * .0f, R + .9f, 6f, N); mk.Prism(WorldMaterials.Solid(new Color(.3f, .16f, .5f)), fo + up * (H + 11.6f), (up + f.Right * .5f).normalized, .5f, 2f, 6, .1f); var orb = fo + up * (H + 13.4f) + f.Right * 1f; mk.Blob(GlowM(violet, 3f), orb, Vector3.one * .35f, prng, 0f); halos.Add(orb, 2.6f, violet, .6f);
            IndoorSpaces.Add(fo + up * (H / 2f), f.r, new Vector3(R * 1.45f, H, R * 1.45f));
            // inside: the crystal ball on a claw stand, shelves of books, three books floating, a cauldron, the wizard, the cat
            var cb = fo; mk.Prism(goldM, cb, up, .3f, .9f, 5, .5f); var ballM = new Material(WorldMaterials.Glow(violet, 1.2f)) { name = "Crystal ball" }; mk.Blob(ballM, cb + up * 1.2f, Vector3.one * .38f, prng, 0f); var cbc = new GameObject("Crystal ball stand"); cbc.transform.SetParent(rootT, false); cbc.transform.position = cb + up * .7f; var cbcc = cbc.AddComponent<CapsuleCollider>(); cbcc.radius = .4f; cbcc.height = 1.6f; plife.ballM = ballM; plife.ballLight = ArtistBits.PointLight(ctx.Root, cb + up * 1.6f, violet, 10f, 2f, true);
            var cgo = new GameObject("Crystal ball"); cgo.transform.SetParent(plife.transform, false); cgo.transform.position = cb + up * 1.2f + f.Fwd * .5f;
            string[] fortunes = { "It clouds... it clears... \"A GUITAR SOLO IS IN YOUR FUTURE.\" It is in everyone's future, here.", "\"YOU WILL MEET A TALL DARK STRANGER.\" It shows you the dragon. The dragon waves.", "\"BEWARE THE KEY CHANGE.\" The ball goes up a semitone as it says it.", "\"THE ANSWER IS YES.\" You hadn't asked anything. It seems pleased with itself.", "\"YOUR QUEST LIES SOUTH.\" It shows you a parfait." }; var fn = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Gaze into the crystal ball", new Vector3(1f, 1f, 1f)).onUse = _ => { plife.gazed = Time.time; Say(plife.transform, cb + up * 2.6f, fortunes[fn++ % fortunes.Length], 6.5f); };
            for (var k = 0; k < 3; k++) { var a = (k * 2 + 3.5f) * Mathf.PI * 2f / N; var d = f.r * new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); var sp = fo + d * (R - .55f); var sq = Quaternion.LookRotation(-d); mk.Box(woodDark, sp + up * 1.4f, new Vector3(1.8f, 2.8f, .4f), sq); Col(sp + up * 1.4f, new Vector3(1.8f, 2.8f, .5f), sq); for (var row = 0; row < 3; row++) for (var b = 0; b < 6; b++) mk.Box(WorldMaterials.Solid(Tunics[(b + row + k) % Tunics.Length]), sp + up * (.6f + row * .85f) + sq * new Vector3(-.7f + b * .28f, 0, .14f), new Vector3(.2f, .55f + (b % 3) * .06f, .2f), sq); }
            for (var k = 0; k < 3; k++) { var bm = new MeshKit(); bm.Box(WorldMaterials.Solid(Tunics[(k * 2) % Tunics.Length]), Vector3.zero, new Vector3(.5f, .08f, .36f), Quaternion.identity); bm.Box(WorldMaterials.Solid(new Color(.92f, .9f, .82f)), new Vector3(0, 0, .01f), new Vector3(.46f, .06f, .36f), Quaternion.identity); var go = bm.BuildSingle("Soft floating book", plife.transform, false); var p = fo + up * (2.2f + k * .4f) + Quaternion.Euler(0, k * 120f, 0) * f.Fwd * 2f; go.transform.position = p; plife.books.Add((go.transform, p, k * 2.1f)); }
            var cl = fo + f.Right * 2.4f + f.Fwd * 1.2f; mk.Prism(iron, cl, up, .6f, .7f, 10, 1.15f); var brew = new Material(WorldMaterials.Glow(new Color(.4f, 1f, .5f), 1.4f)) { name = "Cauldron brew" }; mk.Prism(brew, cl + up * .68f, up, .58f, .04f, 10); var clc = new GameObject("Cauldron"); clc.transform.SetParent(rootT, false); clc.transform.position = cl + up * .35f; clc.AddComponent<SphereCollider>().radius = .65f; halos.Add(cl + up * 1f, 1.2f, new Color(.4f, 1f, .5f), .5f);
            var wiz = new Residents.Look { skin = new Color(.85f, .7f, .58f), hair = new Color(.9f, .9f, .9f), top = new Color(.3f, .16f, .5f), bottom = new Color(.3f, .16f, .5f), longHair = true, hat = Residents.Hat.Bucket, hatColor = new Color(.3f, .16f, .5f), tool = Residents.Tool.Rod, height = 1.05f };
            Folk(fo - f.Right * 2.2f - f.Fwd * .6f, f.Right, Resident.Pose.Stand, new[] { "Ask the ball anything. It only knows about guitar solos, but it knows EVERYTHING about guitar solos.", "I enchanted the books to fly. I cannot now get them down. Do not tell the hall.", "Fireball? No. I do a very good dry-ice effect, though. For the stage." }, wiz);
            var cat = new MeshKit(); var fur = WorldMaterials.Solid(new Color(.08f, .08f, .1f)); cat.Blob(fur, new Vector3(0, .16f, 0), new Vector3(.3f, .17f, .2f), prng, .05f); cat.Blob(fur, new Vector3(.26f, .26f, 0), Vector3.one * .12f, prng, 0f); foreach (var e in new[] { -.06f, .06f }) { cat.Cone(fur, new Vector3(.27f, .34f, e), .04f, .09f, 4); cat.Blob(GlowM(new Color(.5f, 1f, .5f), 2.4f), new Vector3(.36f, .28f, e), Vector3.one * .02f, prng, 0f); }
            var catgo = cat.BuildSingle("Familiar", plife.transform, false); catgo.transform.SetPositionAndRotation(fo + f.Fwd * 2.6f - f.Right * 1.4f, f.r * Quaternion.Euler(0, 140f, 0)); string[] purr = { "The familiar regards you as it would a mouse with a lute.", "It purrs in drop D.", "It allows one stroke. One." }; var pn = 0; Interact.Toy(catgo, Toy.Kind.Custom, "Stroke the familiar", new Vector3(.8f, .5f, .6f)).onUse = _ => Say(plife.transform, catgo.transform.position + up * 1.2f, purr[pn++ % purr.Length], 4f);
            var st = fo - f.Fwd * 2.4f + f.Right * .8f; mk.Prism(woodDark, st, up, .26f, .5f, 8); var stc = new GameObject("Stool"); stc.transform.SetParent(rootT, false); stc.transform.position = st + up * .25f; stc.AddComponent<BoxCollider>().size = new Vector3(.5f, .5f, .5f); Interact.Seat(plife.transform, st + up * .56f, f.Fwd, "Sit on the stool");
            Torch(ctx, mk, G(g + f.Fwd * (R + 1.6f) + f.Right * 2.2f)); OakBench(mk, g + f.Fwd * (R + 2.2f) - f.Right * 3.2f, f.Fwd, 2f);
            ShieldSign(mk, g + f.Fwd * (R + 6f) + f.Right * 5f, f.Fwd, "THE WIZARD\n(is in)", bannerPurple, .18f);
            Flagstones(ctx, mk, g + f.Fwd * (R + 1.2f)); Ring(g, 8f);
            Shot(ctx, "the wizard's tower", g + f.Fwd * (R + 14f) - f.Right * 7f + up * 4f, fo + up * 6f);
            Shot(ctx, "inside the wizard's tower", fo + f.Fwd * (R - 1.4f) + up * 1.6f, fo + up * 1.4f - f.Fwd * 2f);
        }

        // ---------------------------------------------------------------- the Neko Metal Café
        void NekoCafe(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CafeAt.x, CafeAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(flagP, g))); const float W = 10f, D = 7f, H = 3.3f, T = .3f, Door = HumanScale.DoorWidth + .3f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var pinkWall = WorldMaterials.Solid(new Color(1f, .7f, .82f)); var white = WorldMaterials.Solid(new Color(.98f, .96f, .97f)); var mint = WorldMaterials.Solid(new Color(.65f, .92f, .85f));
            mk.Box(wallStone, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(pinkWall, p, s, f.r); solid.Box(pinkWall, p, s, f.r); }
            mk.Box(white, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(white, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r); for (var x = 0; x < 6; x++) for (var z = 0; z < 4; z++) if ((x + z) % 2 == 0) mk.Box(mint, fo.P(-W / 2f + T + .8f + x * 1.55f, .025f, -D / 2f + T + .8f + z * 1.55f), new Vector3(1.5f, .01f, 1.5f), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) { Wall(fo.P(e * (Door / 2f + seg / 2f), .45f, D / 2f - T / 2f), new Vector3(seg, .9f, T)); Wall(fo.P(e * (Door / 2f + seg / 2f), H - .35f, D / 2f - T / 2f), new Vector3(seg, .7f, T)); Wall(fo.P(e * (Door / 2f + .2f), H / 2f, D / 2f - T / 2f), new Vector3(.4f, H, T)); Wall(fo.P(e * (W / 2f - .3f), H / 2f, D / 2f - T / 2f), new Vector3(.6f, H, T)); solid.Box(pinkWall, fo.P(e * (Door / 2f + seg / 2f), 1.75f, D / 2f - T / 2f), new Vector3(seg, 2f, .1f), f.r); }
            Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(white, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r); solid.Box(white, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            // the sign: neon name, and a giant cat's head on the roof with studded collar and glowing eyes
            ArtistBits.Label(ctx.Root, "NEKO METAL CAFÉ", fo.P(0, H + .7f, D / 2f + .14f), f.Fwd, .42f, Pink, TextAnchor.MiddleCenter); mk.Box(WorldMaterials.Solid(new Color(.1f, .08f, .12f)), fo.P(0, H + .7f, D / 2f), new Vector3(6.4f, 1f, .16f), f.r); halos.Add(fo.P(0, H + .7f, D / 2f + .6f), 3f, Pink, .5f);
            var hd = fo.P(0, H + 2.4f, -.5f); mk.Blob(white, hd, new Vector3(1.6f, 1.3f, 1.3f), prng, .03f); foreach (var e in new[] { -1f, 1f }) { mk.Cone(white, hd + up * .9f + f.Right * e * .95f, .5f, .9f, 4, 45f); mk.Cone(neonPink, hd + up * .95f + f.Right * e * .95f + f.Fwd * .12f, .28f, .6f, 4, 45f); mk.Blob(neonCyan, hd + up * .15f + f.Right * e * .6f + f.Fwd * 1.2f, new Vector3(.2f, .26f, .08f), prng, 0f); for (var s = 0; s < 3; s++) mk.Box(WorldMaterials.Solid(new Color(.1f, .08f, .12f)), hd - up * .2f + f.Right * e * (1.3f + s * .0f) + f.Fwd * 1f + up * (s - 1) * .16f, new Vector3(.9f, .02f, .02f), f.r * Quaternion.Euler(0, 0, e * (s - 1) * 12f)); }
            mk.Blob(neonPink, hd - up * .15f + f.Fwd * 1.3f, new Vector3(.12f, .09f, .06f), prng, 0f); mk.Box(WorldMaterials.Solid(new Color(.1f, .08f, .12f)), hd - up * 1.15f, new Vector3(2.6f, .25f, 2.2f), f.r); for (var s = 0; s < 6; s++) mk.Cone(WorldMaterials.Solid(new Color(.8f, .8f, .85f), .9f), hd - up * 1.15f + f.Fwd * 1.12f + f.Right * (-1f + s * .4f), .08f, .2f, 4); halos.Add(hd + f.Fwd * 1.6f, 2f, NeonCyan, .4f);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(neonPink, fo.P(e * (W / 2f), H / 2f, D / 2f + .03f), new Vector3(.08f, H, .06f), f.r); mk.Box(neonCyan, fo.P(e * (Door / 2f + .1f), 1.3f, D / 2f + .03f), new Vector3(.06f, 2.6f, .06f), f.r); }
            // inside: the counter with parfaits to carry, three little round tables with heart-backed chairs, the capsule machine, a tiny stage with a pink flying V
            mk.Box(white, fo.P(-1.5f, .5f, -D / 2f + 1.6f), new Vector3(4.6f, 1f, .7f), f.r); mk.Box(neonPink, fo.P(-1.5f, 1.02f, -D / 2f + 1.6f), new Vector3(4.7f, .05f, .8f), f.r); Col(fo.P(-1.5f, .55f, -D / 2f + 1.6f), new Vector3(4.7f, 1.1f, .8f), f.r);
            for (var k = 0; k < 4; k++) { var pm = new MeshKit(); pm.Prism(WorldMaterials.Glow(new Color(.9f, .95f, 1f), .2f), Vector3.zero, Vector3.up, .06f, .2f, 8, 1.5f); pm.Blob(WorldMaterials.Solid(k % 2 == 0 ? new Color(1f, .7f, .8f) : new Color(.98f, .95f, .85f)), new Vector3(0, .24f, 0), Vector3.one * .09f, prng, .1f); pm.Blob(WorldMaterials.Solid(new Color(.85f, .15f, .2f)), new Vector3(.02f, .34f, 0), Vector3.one * .03f, prng, 0f); var go = pm.BuildSingle("Parfait", plife.transform, false); go.transform.position = fo.P(-3.2f + k * .8f, 1.06f, -D / 2f + 1.6f); var c = Interact.Carry(go, k % 2 == 0 ? "a strawberry parfait (with a skull wafer)" : "a vanilla parfait (extremely metal)", .3f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.18f, .38f, .18f); bc.center = new Vector3(0, .18f, 0); } }
            var maid = new Residents.Look { skin = new Color(.95f, .8f, .7f), hair = new Color(1f, .55f, .8f), top = new Color(.1f, .08f, .12f), bottom = new Color(.1f, .08f, .12f), longHair = true, apron = true, tool = Residents.Tool.Mug }; Folk(fo.P(-1.5f, 0, -D / 2f + .85f), f.Fwd, Resident.Pose.Work, new[] { "Welcome home, brave adventurer! Table for one, or for your whole war band?", "Today's special is the Doom Parfait. It's strawberry. The doom is in the name.", "We do a little dance when the food comes. You have to do the horns back. Rules." }, maid).workRate = .7f;
            for (var t = 0; t < 3; t++)
            {
                var tc = fo.P(-2.6f + t * 2.6f, 0, 1.2f + (t % 2) * .5f); mk.Prism(white, tc, up, .06f, .72f, 6); mk.Prism(t == 1 ? mint : pinkWall, tc + up * .72f, up, .5f, .05f, 10); ArtistBits.PostCollider(ctx.Root, tc, .78f, .4f);
                foreach (var e in new[] { -1f, 1f }) { var cp = tc + f.Right * e * .95f; var cq = Quaternion.LookRotation(-f.Right * e); mk.Box(white, cp + up * .45f, new Vector3(.42f, .05f, .42f), cq); mk.Box(white, cp + up * .22f, new Vector3(.36f, .45f, .36f), cq); mk.Blob(neonPink, cp + up * .9f + f.Right * e * .2f, new Vector3(.04f, .2f, .2f), prng, 0f); Col(cp + up * .45f, new Vector3(.45f, .9f, .45f), cq); var taken = t == 0 && e < 0 || t == 2 && e > 0; if (taken) { var cust = Warrior(Residents.Tool.Mug); Folk(cp + up * .5f, -f.Right * e, Resident.Pose.Sit, t == 0 ? new[] { "I came in full armour. They gave me a bib with a kitten on it. I've never felt so seen.", "I have slain dragons. I am afraid of how much I like this parfait." } : new[] { "The capsule machine gave me a tiny battle-axe. Best day of the campaign.", "Don't tell the hall I'm here. Half the hall's here." }, cust, null, .5f); } else Interact.Seat(plife.transform, cp + up * .52f, -f.Right * e, "Sit at the table"); }
            }
            var gm = fo.P(W / 2f - T - .5f, 0, -1.5f); mk.Box(white, gm + up * .5f, new Vector3(.7f, 1f, .7f), f.r); var capsM = new Material(WorldMaterials.Glow(new Color(.9f, .95f, 1f), .5f)) { name = "Capsule dome" }; mk.Blob(capsM, gm + up * 1.35f, new Vector3(.42f, .42f, .42f), prng, 0f); for (var k = 0; k < 7; k++) mk.Blob(GlowM(new[] { Pink, NeonCyan, Gold, new Color(.6f, 1f, .5f) }[k % 4], 1.6f), gm + up * (1.2f + (k % 3) * .12f) + new Vector3(Mathf.Cos(k * 1.9f), 0, Mathf.Sin(k * 1.9f)) * .22f, Vector3.one * .08f, prng, 0f); Col(gm + up * .9f, new Vector3(.8f, 1.8f, .8f), f.r); plife.capsM = capsM;
            var ggo = new GameObject("Capsule machine"); ggo.transform.SetParent(plife.transform, false); ggo.transform.position = gm + up * 1f - f.Right * .5f; string[] caps = { "Clunk. A capsule: a tiny plastic battle-axe.", "Clunk. A capsule: a kitten in a horned helmet.", "Clunk. A capsule: a dragon keyring. Its eyes light up. You are delighted.", "Clunk. A capsule: another capsule. Inside that: a smaller capsule.", "Clunk. A capsule: a sticker that says KAWAII OR DIE." }; var gn = 0;
            Interact.Toy(ggo, Toy.Kind.Custom, "Turn the capsule machine", new Vector3(.9f, 1.4f, .9f)).onUse = _ => { plife.capsule = Time.time; Say(plife.transform, gm + up * 2.4f, caps[gn++ % caps.Length], 5f); };
            var stg = fo.P(3f, 0, -D / 2f + T + .9f); mk.Box(WorldMaterials.Solid(new Color(.1f, .08f, .12f)), stg + up * .15f, new Vector3(2.6f, .3f, 1.6f), f.r); solid.Box(white, stg + up * .15f, new Vector3(2.6f, .3f, 1.6f), f.r); mk.Box(neonPink, stg + up * .9f, new Vector3(.12f, .6f, .05f), f.r * Quaternion.Euler(0, 0, 35f)); mk.Box(neonPink, stg + up * .9f, new Vector3(.12f, .6f, .05f), f.r * Quaternion.Euler(0, 0, -35f)); mk.Box(white, stg + up * 1.45f, new Vector3(.06f, .8f, .04f), f.r); mk.Prism(wallDark, stg + up * .3f + f.Right * .9f, up, .025f, 1.4f, 5); mk.Blob(neonPink, stg + up * 1.75f + f.Right * .9f, Vector3.one * .07f, prng, 0f);
            for (var k = 0; k < 9; k++) { var p = fo.P(-W / 2f + .8f + k * 1.05f, H - .35f - Mathf.Sin(k / 8f * Mathf.PI) * .25f, D / 2f - T - .1f); mk.Blob(k % 2 == 0 ? neonPink : neonCyan, p, Vector3.one * .07f, prng, 0f); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.7f, 0), new Color(1f, .75f, .88f), 13f, 2.6f, true);
            StoneLantern(ctx, mk, G(f.P(-Door / 2f - 2.6f, 0, D / 2f + 2.2f))); Cherry(ctx, mk, G(f.P(W / 2f + 3.5f, 0, D / 2f + 2.5f))); OakBench(mk, f.P(3.6f, 0, D / 2f + 1.4f), f.Fwd, 2f);
            Flagstones(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 9f);
            Shot(ctx, "the neko metal cafe", f.P(-7f, 2.8f, D / 2f + 12f), fo.P(0, 3.2f, 0));
            Shot(ctx, "inside the neko metal cafe", fo.P(-3.6f, 1.6f, D / 2f - 1.1f), fo.P(1.6f, 1.1f, -D / 2f + 1.2f));
        }
    }

    /// The Power places' moving parts: the hall's cheer when the horn sounds, the crystal ball, the floating books, the capsule machine.
    public class PowerPlacesLife : MonoBehaviour
    {
        public float horn = -99f, gazed = -99f, capsule = -99f; public Light hallLight, ballLight; public Material ballM, capsM; public List<(Transform t, Vector3 at, float phase)> books = new List<(Transform, Vector3, float)>();
        static readonly Color Violet = new Color(.65f, .4f, 1f), Pale = new Color(.9f, .95f, 1f);

        void Update()
        {
            var t = Time.time; var a = t - horn; if (hallLight != null) hallLight.intensity = a < 5f ? 4f * (1f - a / 5f) * (.7f + .3f * Mathf.Sin(a * 14f)) : 0f;
            var gz = t - gazed; var swirl = gz < 6.5f ? 1f - gz / 6.5f : 0f; if (ballM != null) ballM.SetColor("_EmissionColor", Color.Lerp(Violet, Pale, swirl * (.5f + .5f * Mathf.Sin(t * 9f))) * (1.2f + swirl * 3f)); if (ballLight != null) ballLight.intensity = 2f + swirl * 3f;
            foreach (var (tr, at, ph) in books) { tr.position = at + Vector3.up * Mathf.Sin(t * .8f + ph) * .25f; tr.rotation = Quaternion.Euler(Mathf.Sin(t * .6f + ph) * 12f, t * 20f + ph * 60f, Mathf.Cos(t * .5f + ph) * 10f); }
            var c = t - capsule; if (capsM != null) capsM.SetColor("_EmissionColor", Pale * (.5f + (c < 2f ? 3f * Mathf.Abs(Mathf.Sin(c * 12f)) * (1f - c / 2f) : 0f)));
        }
    }
}
