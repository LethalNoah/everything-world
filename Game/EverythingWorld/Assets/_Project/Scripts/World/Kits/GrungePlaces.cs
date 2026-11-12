using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Grunge Rainwood laid out on purpose (the build-land standard). The crossroads, the mill, the river and every
    /// clearing stay where they were; four more logging roads with yellow lamps push out into the four empty corners
    /// of the forest (none of them crosses the river). At their ends, with the people of the woods: the House Show (a
    /// rented house with the band in the living room and everyone else on the porch); the Logging Camp (a clear-cut
    /// of pale stumps, a bunkhouse, a saw stuck in a log, axes to throw at a target); Last Chance Gas (a station gone
    /// to moss, one pump light still on, a pinball machine that works); and the Big Fella's clearing (somebody very
    /// large and very shy, sat by a fire with his headphones on).
    public partial class GrungeKit
    {
        static readonly Vector2 HouseAt = new Vector2(8966, 8386), CampAt = new Vector2(9150, 8070), GasAt = new Vector2(8386, 7884), BigAt = new Vector2(8404, 8450);
        static readonly Vector2[] NorthEastRoad = { new Vector2(8905, 8292), new Vector2(8930, 8330), new Vector2(8950, 8362) };
        static readonly Vector2[] EastRoad = { new Vector2(9005, 8008), new Vector2(9070, 8036), new Vector2(9120, 8058) };
        static readonly Vector2[] SouthWestRoad = { new Vector2(8522, 7932), new Vector2(8465, 7902), new Vector2(8412, 7890) };
        static readonly Vector2[] NorthWestRoad = { new Vector2(8562, 8390), new Vector2(8494, 8420), new Vector2(8432, 8440) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (HouseAt, 13f), (CampAt, 17f), (GasAt, 13f), (BigAt, 10f) };

        GrungePlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> logRoad = new List<Vector3>();

        static readonly Color[] Flannels = { new Color(.62f, .16f, .14f), new Color(.2f, .35f, .3f), new Color(.25f, .3f, .45f), new Color(.5f, .42f, .25f), new Color(.3f, .3f, .32f), new Color(.75f, .72f, .65f) };
        static readonly Color[] Ripped = { new Color(.3f, .38f, .5f), new Color(.2f, .22f, .26f), new Color(.38f, .33f, .26f) };
        static readonly Residents.Hat[] WetHats = { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Cap };
        static readonly string[] RainTalk =
        {
            "Still raining. It's not weather here, it's furniture.", "Whatever. No, I mean it nicely. Whatever.", "There's a show at the mill. There's always a show at the mill. I'll go when it's quieter. It's never quieter.",
            "You get used to wet socks. I'm told. I haven't.", "Everything good here got written in a room with a leak.",
        };

        Residents.Look Slacker(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Flannels, Ripped, WetHats, Flannels); l.tool = tool; l.apron = apron; if (l.hat != Residents.Hat.Cap) l.longHair = prng.Next(3) != 0; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Slacker(), pose, prng, lines ?? RainTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeRainwood(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
        }

        /// A routed plank sign on two posts under a little mossy roof. Signs on the +x side of a place's road; the photo spot on the −x side.
        void PlankSign(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.6f, float size = .17f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Prism(bark, g + rot * Vector3.right * e * (w / 2f - .1f) - up * .2f, up, .09f, 2.6f, 6); mk.Box(plank, g + up * 1.65f, new Vector3(w, .8f, .1f), rot); mk.Box(oldPlank, g + up * 2.35f, new Vector3(w + .5f, .08f, .7f), rot * Quaternion.Euler(10f, 0, 0)); mk.Blob(moss, g + up * 2.42f, new Vector3(w * .5f, .06f, .3f), prng, .2f);
            Col(g + up * 1.2f, new Vector3(w, 2.4f, .16f), rot); ArtistBits.Label(rootT, text, g + up * 1.65f + face * .06f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        void LogSeat(MeshKit mk, Vector3 at, Vector3 face, float len = 2.2f)
        {
            var up = Vector3.up; var g = G(at); var along = Vector3.Cross(up, face); mk.Prism(bark, g + up * .32f - along * (len / 2f), along, .32f, len, 8); mk.Blob(moss, g + up * .6f + along * .3f, new Vector3(.4f, .06f, .2f), prng, .2f);
            Col(g + up * .32f, new Vector3(len, .64f, .64f), Quaternion.LookRotation(face)); Interact.Bench(plife.transform, g + up * .68f - along * (len / 2f - .45f), g + up * .68f + along * (len / 2f - .45f), face);
        }

        List<Vector3> LoggingRoad(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 200f * 200f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2.5f);
            for (var i = 0; i < pts.Count; i++) foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
            pts.RemoveAll(p => InRiver(p.x, p.z, 2f)); if (pts.Count < 2) return pts;
            Strip(mk, pts, mud, RoadW, .06f, ctx); foreach (var p in pts) Remember(p, 700);
            var up = Vector3.up; var n = 0;
            for (var i = 5; i < pts.Count; i += 12) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d) * (n++ % 2 == 0 ? 1f : -1f); var lp = pts[i] + sd * (RoadW / 2f + 1f); if (ctx.IsLand(lp.x, lp.z, 2f) && !InRiver(lp.x, lp.z, 1f)) RoadLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -sd); if (n % 2 == 0) Puddle(mk, pts[i] - sd * .6f + up * .09f, .9f); }
            logRoad.AddRange(pts); return pts;
        }

        void Spur(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(logRoad, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.7f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, mud, 2.4f, .055f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void RainwoodPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            logRoad.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1991);
            plife = new GameObject("Rainwood places life").AddComponent<GrungePlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            LoggingRoad(ctx, mk, NorthEastRoad); LoggingRoad(ctx, mk, EastRoad); LoggingRoad(ctx, mk, SouthWestRoad); LoggingRoad(ctx, mk, NorthWestRoad);
            HouseShow(ctx, mk, solid); LoggingCamp(ctx, mk, solid); LastChanceGas(ctx, mk, solid); BigFella(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the House Show
        void HouseShow(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(HouseAt.x, HouseAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(logRoad, g))); const float W = 10f, D = 8f, H = 3.2f, T = .3f, Door = HumanScale.DoorWidth + .3f;
            var fo = new PlaceFrame(g + up * .5f, f.Fwd); var siding = WorldMaterials.Solid(new Color(.42f, .5f, .46f)); var floorM = WorldMaterials.Solid(new Color(.36f, .28f, .22f)); var warm = new Color(1f, .75f, .45f);
            mk.Box(concrete, g - up * .25f, new Vector3(W + .4f, 1.5f, D + .4f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(siding, p, s, f.r); solid.Box(siding, p, s, f.r); }
            mk.Box(floorM, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(floorM, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) { Wall(fo.P(e * (Door / 2f + seg / 2f), .5f, D / 2f - T / 2f), new Vector3(seg, 1f, T)); Wall(fo.P(e * (Door / 2f + seg / 2f), H - .4f, D / 2f - T / 2f), new Vector3(seg, .8f, T)); Wall(fo.P(e * (Door / 2f + .25f), H / 2f, D / 2f - T / 2f), new Vector3(.5f, H, T)); Wall(fo.P(e * (W / 2f - .4f), H / 2f, D / 2f - T / 2f), new Vector3(.8f, H, T)); mk.Box(GlowM(warm, 1.4f), fo.P(e * (Door / 2f + seg / 2f + .05f), 1.7f, D / 2f - T / 2f), new Vector3(seg - 1.3f, 1.4f, .05f), f.r); solid.Box(siding, fo.P(e * (Door / 2f + seg / 2f + .05f), 1.7f, D / 2f - T / 2f), new Vector3(seg - 1.3f, 1.4f, .1f), f.r); }
            Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(oldPlank, fo.P(0, H + .06f, 0), new Vector3(W - T, .12f, D - T), f.r); mk.GableRoof(tin, fo.P(0, H + .1f, 1f), new Vector3(W + .8f, 0, D + 3.2f), 2f, f.r); solid.Box(tin, fo.P(0, H + .5f, 0), new Vector3(W, .6f, D), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            // the porch: boards, posts, a sagging couch, a string of bulbs, three steps
            mk.Box(plank, fo.P(0, -.08f, D / 2f + 1.2f), new Vector3(W, .16f, 2.4f), f.r); solid.Box(plank, fo.P(0, -.2f, D / 2f + 1.2f), new Vector3(W, .4f, 2.4f), f.r); foreach (var e in new[] { -4.8f, -1.3f, 1.3f, 4.8f }) { mk.Box(plank, fo.P(e, 1.3f, D / 2f + 2.3f), new Vector3(.14f, 2.6f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(e, 0, D / 2f + 2.3f), 2.6f, .1f); }
            for (var k = 0; k < 2; k++) { var st = fo.P(0, -.2f - k * .2f, D / 2f + 2.6f + k * .35f); mk.Box(plank, st, new Vector3(2.4f, .2f, .4f), f.r); solid.Box(plank, st, new Vector3(2.4f, .2f, .4f), f.r); }
            for (var k = 0; k < 11; k++) { var p = fo.P(-4.6f + k * .92f, 2.45f - Mathf.Sin(k / 10f * Mathf.PI) * .25f, D / 2f + 2.3f); mk.Blob(GlowM(k % 3 == 0 ? new Color(1f, .4f, .3f) : warm, 2.4f), p, Vector3.one * .07f, prng, 0f); if (k % 4 == 1) halos.Add(p, .8f, warm, .6f); }
            var couch = fo.P(-3.2f, 0, D / 2f + .8f); var cm = WorldMaterials.Solid(new Color(.5f, .38f, .3f)); mk.Box(cm, couch + up * .32f, new Vector3(2.4f, .5f, .9f), f.r); mk.Box(cm, couch + up * .75f - f.Fwd * .38f, new Vector3(2.4f, .6f, .2f), f.r); foreach (var e in new[] { -1.15f, 1.15f }) mk.Box(cm, couch + up * .55f + f.Right * e, new Vector3(.2f, .5f, .9f), f.r); Col(couch + up * .45f, new Vector3(2.5f, .9f, .9f), f.r); Interact.Bench(plife.transform, couch + up * .6f - f.Right * .2f, couch + up * .6f + f.Right * .8f, f.Fwd);
            Folk(couch + up * .6f - f.Right * .8f, f.Fwd, Resident.Pose.Sit, new[] { "Too loud in there. I mean that as the highest praise. I'm still out here though.", "Five bucks at the door. There's no door guy. Leave it in the coffee can; nobody ever has.", "Second band's my roommate's. First band's also my roommate's." }, Slacker(Residents.Tool.Mug), null, .6f);
            Folk(fo.P(2.8f, 0, D / 2f + 1.5f), -f.Right, Resident.Pose.Lean, new[] { "Cops came once. They stayed for the set.", "Landlord thinks it's a book club." }, Slacker());
            // inside: the band against the back wall, amps, a drum kit, a crowd of six, a lamp with a flannel thrown over it
            mk.Box(dark, fo.P(-3.2f, .7f, -D / 2f + T + .5f), new Vector3(1.2f, 1.4f, .7f), f.r); mk.Box(dark, fo.P(3.2f, .9f, -D / 2f + T + .5f), new Vector3(1.4f, 1.8f, .7f), f.r); Col(fo.P(-3.2f, .7f, -D / 2f + T + .5f), new Vector3(1.2f, 1.4f, .7f), f.r); Col(fo.P(3.2f, .9f, -D / 2f + T + .5f), new Vector3(1.4f, 1.8f, .7f), f.r);
            var ampM = new Material(WorldMaterials.Glow(new Color(1f, .35f, .2f), .6f)) { name = "House show amp" }; foreach (var e in new[] { -3.2f, 3.2f }) mk.Blob(ampM, fo.P(e + .4f, e < 0 ? 1.2f : 1.6f, -D / 2f + T + .87f), Vector3.one * .05f, prng, 0f); plife.ampM = ampM;
            var dk = fo.P(0, 0, -D / 2f + T + 1f); mk.Prism(dark, dk, up, .5f, .45f, 12); foreach (var e in new[] { -1f, 1f }) { mk.Prism(dark, dk + f.Right * e * .8f + up * .45f, up, .26f, .28f, 10); mk.Prism(WorldMaterials.Solid(new Color(.8f, .65f, .3f), .8f), dk + f.Right * e * 1.1f + up * 1.15f + f.Fwd * .2f, up, .3f, .02f, 10); mk.Prism(metal, dk + f.Right * e * 1.1f + f.Fwd * .2f, up, .02f, 1.15f, 4); } Col(dk + up * .5f, new Vector3(2.6f, 1f, 1.2f), f.r);
            string[] band = { "(Can't hear you. Is playing. Nods anyway.)", "ONE MORE. IT'S THE SAME ONE AGAIN.", "(Breaks a string. Keeps going. It's better.)" };
            var dr = Slacker(Residents.Tool.Spatula); Folk(dk - f.Fwd * .15f + up * .0f, f.Fwd, Resident.Pose.Work, band, dr).workRate = 3f; var g1 = Slacker(Residents.Tool.Rod); Folk(fo.P(-1.9f, 0, -D / 2f + T + 1.6f), f.Fwd, Resident.Pose.Work, band, g1).workRate = 2.2f; var g2 = Slacker(Residents.Tool.Rod); Folk(fo.P(1.9f, 0, -D / 2f + T + 1.6f), f.Fwd, Resident.Pose.Work, band, g2).workRate = 1.9f;
            string[] crowd = { "WHAT?", "THEY'RE SO GOOD. I THINK. I CAN'T TELL. THEY'RE SO GOOD.", "(Nods in time, eyes shut, hair in face.)", "MY EARS ARE GOING TO RING TILL THURSDAY." };
            for (var k = 0; k < 6; k++) { var p = fo.P(-2.8f + (k % 3) * 2.6f + (k / 3) * .9f, 0, .2f + (k / 3) * 1.5f); Folk(p, -f.Fwd, Resident.Pose.Wave, crowd, Slacker()).workRate = 1.6f + k * .2f; }
            var lp = fo.P(W / 2f - T - .5f, 0, 2.4f); mk.Prism(metal, lp, up, .04f, 1.5f, 5); mk.Cone(GlowM(new Color(1f, .35f, .25f), 1.6f), lp + up * 1.4f, .3f, .35f, 8); ArtistBits.PostCollider(ctx.Root, lp, 1.5f, .06f);
            plife.showLight = ArtistBits.PointLight(ctx.Root, fo.P(0, 2.6f, -1f), new Color(1f, .5f, .35f), 12f, 2.4f, true); ArtistBits.PointLight(ctx.Root, fo.P(0, 2.2f, D / 2f + 1.6f), warm, 10f, 1.6f);
            // the coffee can for the door money, a van out front
            var can = new MeshKit(); can.Prism(rust, Vector3.zero, Vector3.up, .08f, .16f, 8); var cgo = can.BuildSingle("Coffee can", plife.transform, false); cgo.transform.position = fo.P(Door / 2f + .5f, .02f, D / 2f + .5f); var cc = Interact.Carry(cgo, "the door money (empty)", .3f, g.y - 10f); var cbc = cc.GetComponent<BoxCollider>(); if (cbc != null) { cbc.size = new Vector3(.18f, .18f, .18f); cbc.center = new Vector3(0, .08f, 0); }
            var vp = G(f.P(-7.5f, 0, D / 2f + 7f)); var vq = Quaternion.LookRotation(Quaternion.Euler(0, 20f, 0) * f.Right); var vm = WorldMaterials.Solid(new Color(.55f, .5f, .4f), .4f); mk.Box(vm, vp + up * 1.2f, new Vector3(2f, 1.8f, 4.6f), vq); mk.Box(dark, vp + up * 1.6f + vq * Vector3.forward * 2.31f, new Vector3(1.7f, .7f, .03f), vq); foreach (var e in new[] { -1f, 1f }) foreach (var z in new[] { -1.4f, 1.4f }) mk.Prism(dark, vp + vq * new Vector3(e * .9f, .38f, z), vq * Vector3.right * e, .38f, .22f, 10); solid.Box(vm, vp + up * 1.1f, new Vector3(2f, 2.2f, 4.6f), vq); ArtistBits.Label(ctx.Root, "wash me", vp + up * 1.4f + vq * Vector3.right * 1.02f, vq * Vector3.right, .16f, new Color(.35f, .32f, .28f), TextAnchor.MiddleCenter);
            PlankSign(mk, f.P(6.5f, 0, D / 2f + 8f), f.Fwd, "HOUSE SHOW TONIGHT\n(every night)", 3f, .15f);
            Spur(ctx, mk, f.P(0, 0, D / 2f + 3.6f)); Ring(g, 12f);
            Shot(ctx, "the house show", f.P(-8f, 2.6f, D / 2f + 13f), fo.P(0, 2f, 0));
            Shot(ctx, "inside the house show", fo.P(3.6f, 1.6f, D / 2f - 1f), fo.P(-1f, 1.2f, -D / 2f + 1.5f));
        }

        // ---------------------------------------------------------------- the Logging Camp
        void LoggingCamp(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CampAt.x, CampAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(logRoad, g))); var pale = WorldMaterials.Solid(new Color(.78f, .68f, .5f));
            // a clear-cut: pale fresh stumps all round (the one bright open place in the forest), sawdust on the ground
            var rng = new System.Random(88); mk.Prism(WorldMaterials.Solid(new Color(.55f, .47f, .34f)), g - up * .04f, up, 9f, .1f, 16);
            for (var k = 0; k < 26; k++) { var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = 8f + (float)rng.NextDouble() * 9f; var p = G(g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r); if (Vector3.Distance(p, Nearest(logRoad, p)) < 3f) continue; var sr = .35f + (float)rng.NextDouble() * .4f; var sh = .4f + (float)rng.NextDouble() * .5f; mk.Prism(bark, p - up * .3f, up, sr, sh + .3f, 8); mk.Prism(pale, p + up * sh, up, sr * .96f, .03f, 8); ArtistBits.PostCollider(ctx.Root, p, sh, sr); if (k % 5 == 0) Interact.Seat(plife.transform, p + up * (sh + .08f), FaceTo(p, g), "Sit on the stump"); }
            // the bunkhouse (a long low shed with a lit window and a stovepipe) and a cook fire under a tarp
            var bh = G(f.P(-9f, 0, -5f)); var bq = Quaternion.LookRotation(f.Right); mk.Box(oldPlank, bh + up * 1.4f, new Vector3(4f, 2.8f, 9f), bq); solid.Box(oldPlank, bh + up * 1.4f, new Vector3(4f, 2.8f, 9f), bq); mk.GableRoof(tin, bh + up * 2.8f, new Vector3(4.6f, 0, 9.6f), 1.2f, bq); mk.Box(GlowM(Lamp, 1.5f), bh + up * 1.6f + f.Right * 2.02f - f.Fwd * 2f, new Vector3(.04f, .9f, 1.4f), f.r); mk.Box(dark, bh + up * 1.05f + f.Right * 2.02f + f.Fwd * 2f, new Vector3(.04f, 2.1f, 1f), f.r); mk.Prism(rust, bh + up * 3.2f - f.Fwd * 3f, up, .12f, 1.6f, 6); halos.Add(bh + up * 1.6f + f.Right * 2.5f - f.Fwd * 2f, 1.4f, Lamp, .5f);
            var fp = G(f.P(-3f, 0, -6f)); mk.Prism(rock, fp, up, .8f, .25f, 9); mk.Prism(GlowM(new Color(1f, .5f, .15f), 3f), fp + up * .26f, up, .5f, .05f, 8); halos.Add(fp + up * .8f, 1.8f, new Color(1f, .55f, .2f), .9f); ArtistBits.PointLight(ctx.Root, fp + up * 1.4f, new Color(1f, .55f, .2f), 14f, 2f); ArtistBits.PostCollider(ctx.Root, fp, .35f, .8f);
            foreach (var (x, z) in new[] { (-2f, -2f), (2f, -2f), (2f, 2f), (-2f, 2f) }) { var p = G(fp + f.r * new Vector3(x, 0, z)); mk.Prism(bark, p, up, .07f, 2.6f + (z > 0 ? .5f : 0f), 5); ArtistBits.PostCollider(ctx.Root, p, 2.6f, .08f); } mk.Box(WorldMaterials.Solid(new Color(.25f, .35f, .5f)), fp + up * 2.9f, new Vector3(4.4f, .05f, 4.4f), f.r * Quaternion.Euler(-7f, 0, 0));
            LogSeat(mk, fp + f.Fwd * 2.4f, -f.Fwd); LogSeat(mk, fp - f.Right * 2.4f, f.Right, 1.8f);
            Folk(G(fp + f.Fwd * 2.4f) + up * .68f + f.Right * .5f, -f.Fwd, Resident.Pose.Sit, new[] { "We cut it, the mill saws it, somebody builds a stage out of it. Circle of life.", "Have a throw at the target. Lose an axe in the trees and you're fetching it.", "Rain's good for the saw. Rain's good for nothing else." }, Slacker(Residents.Tool.Mug), null, .68f);
            // a big log on blocks with a two-man saw stuck in it: pull it
            var lg = G(f.P(4f, 0, -6.5f)); var lq = Quaternion.LookRotation(f.Right); mk.Prism(bark, lg + up * 1f - f.Right * 3.5f, f.Right, .7f, 7f, 10); mk.Prism(pale, lg + up * 1f + f.Right * 3.5f, f.Right, .68f, .03f, 10); foreach (var e in new[] { -2.2f, 2.2f }) mk.Box(oldPlank, lg + f.Right * e + up * .2f, new Vector3(.5f, .4f, 1.8f), f.r); Col(lg + up * .9f, new Vector3(1.4f, 1.6f, 7f), lq);
            var sw = new MeshKit(); sw.Box(metal, new Vector3(0, 0, 0), new Vector3(.02f, .3f, 3f), Quaternion.identity); foreach (var e in new[] { -1.55f, 1.55f }) sw.Box(plank, new Vector3(0, .25f, e), new Vector3(.06f, .35f, .08f), Quaternion.identity);
            var sgo = sw.BuildSingle("Two-man saw", plife.transform, false); sgo.transform.SetPositionAndRotation(lg + up * 1.55f + f.Right * .6f, f.r); plife.saw = sgo.transform; plife.sawAt = sgo.transform.position; plife.sawDir = f.Fwd; var st = Interact.Toy(sgo, Toy.Kind.Custom, "Pull the saw", new Vector3(.6f, .9f, 3.2f)); st.onUse = _ => { plife.sawing = Time.time; Say(plife.transform, lg + up * 2.8f, "Zzzip. Zzzip. Sawdust on your boots.", 3.5f); };
            Folk(G(lg + f.Fwd * 2.1f + f.Right * .6f), -f.Fwd, Resident.Pose.Work, new[] { "It's a two-man saw. I've been one man at it since lunch. Grab the other end.", "This tree was older than the town. So's the saw." }, Slacker(Residents.Tool.None, true)).workRate = 1f;
            // the axe target: a round of wood on a tripod with rings painted on, axes in a block to throw
            var tg = G(f.P(8.5f, 0, 2f)); var tdir = FaceTo(tg, g); var tq = Quaternion.LookRotation(tdir); foreach (var a in new[] { -25f, 25f, 180f }) mk.Prism(bark, tg + Quaternion.Euler(0, a, 0) * -tdir * .9f, (up * 1.6f + Quaternion.Euler(0, a, 0) * tdir * .9f).normalized, .06f, 2f, 5);
            mk.Prism(pale, tg + up * 1.5f - tdir * .1f, tdir, .8f, .2f, 14); mk.Prism(GlowM(new Color(1f, .25f, .2f), 1.6f), tg + up * 1.5f + tdir * .1f, tdir, .5f, .01f, 14); mk.Prism(pale, tg + up * 1.5f + tdir * .11f, tdir, .32f, .01f, 14); mk.Prism(GlowM(new Color(1f, .25f, .2f), 2f), tg + up * 1.5f + tdir * .12f, tdir, .13f, .01f, 10); Col(tg + up * 1.3f, new Vector3(1.7f, 2.6f, .5f), tq);
            var blk = G(tg + tdir * 6f); mk.Prism(bark, blk, up, .45f, .6f, 9); mk.Prism(pale, blk + up * .6f, up, .43f, .02f, 9); ArtistBits.PostCollider(ctx.Root, blk, .6f, .45f); mk.Box(GlowM(Lamp, 1.6f), blk + up * .03f + tdir * 1.5f, new Vector3(1.6f, .02f, .08f), tq);
            for (var k = 0; k < 4; k++) { var am = new MeshKit(); am.Box(plank, new Vector3(0, .3f, 0), new Vector3(.04f, .6f, .05f), Quaternion.identity); am.Box(metal, new Vector3(0, .56f, .09f), new Vector3(.03f, .14f, .2f), Quaternion.identity); var go = am.BuildSingle("Axe", plife.transform, false); go.transform.SetPositionAndRotation(blk + up * .62f + new Vector3(Mathf.Cos(k * 1.6f), 0, Mathf.Sin(k * 1.6f)) * .22f, Quaternion.Euler(0, k * 90f, 12f)); var c = Interact.Carry(go, "a throwing axe", 1.2f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.1f, .66f, .28f); bc.center = new Vector3(0, .33f, .04f); } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            RoadLamp(ctx, mk, G(f.P(6f, 0, 9f)), -f.Right); ArtistBits.PointLight(ctx.Root, g + up * 5f, new Color(.8f, .85f, 1f), 26f, .9f);
            PlankSign(mk, f.P(7f, 0, 13f), f.Fwd, "LOGGING CAMP\nmind the axes", 2.6f);
            Spur(ctx, mk, f.P(0, 0, 8f)); Ring(g, 16f);
            Shot(ctx, "the logging camp", f.P(-9f, 5f, 16f), g + up * 1f);
        }

        // ---------------------------------------------------------------- Last Chance Gas
        void LastChanceGas(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(GasAt.x, GasAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(logRoad, g))); var paint = WorldMaterials.Solid(new Color(.72f, .7f, .62f)); var red = WorldMaterials.Solid(new Color(.6f, .2f, .16f));
            mk.Box(concrete, g - up * .38f, new Vector3(22f, 1f, 18f), f.r); solid.Box(concrete, g - up * .38f, new Vector3(22f, 1f, 18f), f.r); var fo = new PlaceFrame(g + up * .12f, f.Fwd);
            // the canopy over two old pumps, sagging, mossy; one strip light still on, flickering
            foreach (var e in new[] { -3.5f, 3.5f }) { mk.Box(paint, fo.P(e, 2.2f, 3f), new Vector3(.35f, 4.4f, .35f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(e, 0, 3f), 4.4f, .22f); }
            mk.Box(paint, fo.P(0, 4.5f, 3f), new Vector3(11f, .5f, 6f), f.r * Quaternion.Euler(0, 0, 2.5f)); mk.Box(red, fo.P(0, 4.5f, 6.02f), new Vector3(11f, .52f, .06f), f.r * Quaternion.Euler(0, 0, 2.5f)); for (var k = 0; k < 7; k++) soft.Blob(moss, fo.P(-4.6f + k * 1.5f, 4.8f, 1.5f + (k % 3) * 1.4f), new Vector3(1f, .12f, .8f), prng, .3f); for (var k = 0; k < 5; k++) soft.Box(moss, fo.P(-4.4f + k * 2.2f, 3.9f - (k % 2) * .3f, 6.05f), new Vector3(.2f, .8f + (k % 2) * .6f, .05f), f.r);
            var strip = new Material(WorldMaterials.Glow(new Color(.85f, .95f, 1f), 2f)) { name = "Flickering strip light" }; mk.Box(strip, fo.P(1.5f, 4.22f, 3f), new Vector3(3f, .06f, .2f), f.r); plife.stripM = strip; plife.stripLight = ArtistBits.PointLight(ctx.Root, fo.P(1.5f, 3.6f, 3f), new Color(.85f, .95f, 1f), 14f, 2f); mk.Box(dark, fo.P(-2.5f, 4.22f, 3f), new Vector3(3f, .06f, .2f), f.r);
            foreach (var e in new[] { -1.8f, 1.8f }) { var p = fo.P(e, 0, 3f); mk.Box(red, p + up * .9f, new Vector3(.7f, 1.8f, .5f), f.r); mk.Box(paint, p + up * 1.4f + f.Fwd * .26f, new Vector3(.5f, .5f, .02f), f.r); mk.Prism(paint, p + up * 1.8f, up, .3f, .5f, 10); mk.Box(dark, p + up * .9f + f.Right * .4f, new Vector3(.06f, 1.2f, .06f), f.r); Col(p + up * 1f, new Vector3(.8f, 2.2f, .6f), f.r); soft.Blob(moss, p + up * 2.3f, new Vector3(.3f, .08f, .3f), prng, .2f); }
            // the shop: boarded front, door hanging open, shelves of nothing, a pinball machine still lit in the corner
            const float W = 9f, D = 6f, H = 3.2f, T = .3f; var so = new PlaceFrame(fo.P(0, 0, -5f), f.Fwd);
            void Wall(Vector3 p, Vector3 s) { mk.Box(paint, p, s, f.r); solid.Box(paint, p, s, f.r); }
            mk.Box(oldPlank, so.P(0, .0f, 0), new Vector3(W - T, .06f, D - T), f.r); Wall(so.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(so.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            Wall(so.P(-2.75f, H / 2f, D / 2f - T / 2f), new Vector3(3.5f, H, T)); Wall(so.P(3.1f, H / 2f, D / 2f - T / 2f), new Vector3(2.8f, H, T)); Wall(so.P(.35f, H - .4f, D / 2f - T / 2f), new Vector3(2.7f, .8f, T)); for (var k = 0; k < 3; k++) mk.Box(oldPlank, so.P(-2.75f, .9f + k * .7f, D / 2f + .02f), new Vector3(2.6f, .3f, .05f), f.r * Quaternion.Euler(0, 0, (k - 1) * 6f));
            mk.Box(tin, so.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r); solid.Box(tin, so.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r); for (var k = 0; k < 5; k++) soft.Blob(moss, so.P(-3.5f + k * 1.8f, H + .3f, -1f + (k % 2) * 2f), new Vector3(1.2f, .12f, 1f), prng, .3f);
            IndoorSpaces.Add(so.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            ArtistBits.Label(ctx.Root, "LAST CHANCE GAS", fo.P(0, 4.5f, 6.07f), f.Fwd, .34f, new Color(.9f, .86f, .75f), TextAnchor.MiddleCenter);
            for (var k = 0; k < 2; k++) { mk.Box(metal, so.P(-3.2f + k * 2.2f, 1f, -D / 2f + T + .3f), new Vector3(1.8f, 2f, .5f), f.r); Col(so.P(-3.2f + k * 2.2f, 1f, -D / 2f + T + .3f), new Vector3(1.8f, 2f, .5f), f.r); }
            var pb = so.P(W / 2f - T - .6f, 0, -1.2f); var pq = Quaternion.LookRotation(-f.Right); mk.Box(dark, pb + up * .45f, new Vector3(.7f, .9f, 1.5f), f.r); var glassM = new Material(WorldMaterials.Glow(new Color(1f, .5f, .8f), 1.2f)) { name = "Pinball table" }; mk.Box(glassM, pb + up * .95f, new Vector3(.62f, .04f, 1.4f), f.r * Quaternion.Euler(-6f, 0, 0)); var backM = new Material(WorldMaterials.Glow(new Color(.4f, .9f, 1f), 1.2f)) { name = "Pinball backglass" }; mk.Box(dark, pb + up * 1.5f - f.Fwd * .7f, new Vector3(.7f, .9f, .14f), f.r); mk.Box(backM, pb + up * 1.5f - f.Fwd * .62f, new Vector3(.6f, .76f, .02f), f.r); Col(pb + up * .9f, new Vector3(.7f, 1.9f, 1.5f), f.r);
            plife.pinM = glassM; plife.pinBack = backM; plife.pinLight = ArtistBits.PointLight(ctx.Root, pb + up * 1.6f + f.Fwd * .4f - f.Right * .5f, new Color(1f, .5f, .8f), 8f, 1.2f, true);
            var pgo = new GameObject("Pinball machine"); pgo.transform.SetParent(plife.transform, false); pgo.transform.position = pb + up * 1f + f.Fwd * .7f; string[] ball = { "Ball one. It drains straight down the middle, like it's embarrassed.", "TILT. You didn't even touch it. It just wanted to say tilt.", "Multiball! Bells, lights, everything. For eleven seconds this is the best gas station on earth.", "Free game. The machine has decided it likes you." }; var bn = 0;
            Interact.Toy(pgo, Toy.Kind.Custom, "Play pinball", new Vector3(.9f, 1.2f, .9f)).onUse = _ => { plife.pinHit = Time.time; plife.multiball = bn % 4 == 2; Say(plife.transform, pb + up * 2.4f, ball[bn++ % ball.Length], 5f); };
            Folk(so.P(-.5f, 0, .8f), f.Fwd, Resident.Pose.Stand, new[] { "No gas since '94. I still open up. Somebody has to be here when the pinball pays out.", "Pump two's got a bird's nest in it. Pump one's got a different bird's nest in it.", "You want directions? Everything's down the road. Road's that way. Both ways, really." }, Slacker(Residents.Tool.Mug, true));
            // an ice chest of sodas, a bench against the wall, a truck that isn't going anywhere
            var ic = fo.P(3.4f, 0, -1.6f); mk.Box(paint, ic + up * .45f, new Vector3(1.4f, .9f, .7f), f.r); ArtistBits.Label(ctx.Root, "ICE", ic + up * .5f + f.Fwd * .36f, f.Fwd, .22f, new Color(.3f, .5f, .8f), TextAnchor.MiddleCenter); Col(ic + up * .45f, new Vector3(1.4f, .9f, .7f), f.r);
            for (var k = 0; k < 3; k++) { var cm = new MeshKit(); cm.Prism(WorldMaterials.Solid(k == 1 ? new Color(.3f, .6f, .3f) : new Color(.6f, .35f, .2f), .5f), Vector3.zero, Vector3.up, .035f, .2f, 8, .6f); var go = cm.BuildSingle("Soda bottle", plife.transform, false); go.transform.position = ic + up * .92f + f.Right * (-.3f + k * .3f); var c = Interact.Carry(go, "a bottle of soda (flat since '94)", .3f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.09f, .22f, .09f); bc.center = new Vector3(0, .1f, 0); } }
            var bnch = fo.P(-3.2f, 0, -1.6f); mk.Box(oldPlank, bnch + up * .45f, new Vector3(2.2f, .08f, .5f), f.r); foreach (var e in new[] { -.95f, .95f }) mk.Box(dark, bnch + up * .22f + f.Right * e, new Vector3(.08f, .45f, .45f), f.r); Col(bnch + up * .3f, new Vector3(2.2f, .6f, .5f), f.r); Interact.Bench(plife.transform, bnch + up * .52f - f.Right * .6f, bnch + up * .52f + f.Right * .6f, f.Fwd);
            OldCar(ctx, mk, solid, G(f.P(-8f, 0, 4f)));
            PlankSign(mk, f.P(9f, 0, 11f), f.Fwd, "LAST CHANCE GAS\n(no gas)", 2.8f);
            Spur(ctx, mk, f.P(0, 0, 9.5f)); Ring(g, 13f);
            Shot(ctx, "last chance gas", f.P(-10f, 3f, 14f), fo.P(0, 2.4f, 0));
            Shot(ctx, "inside last chance gas", so.P(-3f, 1.6f, D / 2f - 1f), pb + up * 1f);
        }

        // ---------------------------------------------------------------- the Big Fella
        void BigFella(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BigAt.x, BigAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(logRoad, g))); var fur = WorldMaterials.Solid(new Color(.3f, .2f, .14f)); var furB = WorldMaterials.Solid(new Color(.38f, .27f, .18f)); var skin = WorldMaterials.Solid(new Color(.2f, .14f, .11f));
            // giant footprints up the lane, pressed into the mud
            var from = Nearest(logRoad, g); for (var k = 0; k < 7; k++) { var t = (k + 1) / 9f; var p = Vector3.Lerp(from, g, t); var d = FaceTo(from, g); var sd = Vector3.Cross(up, d) * (k % 2 == 0 ? .7f : -.7f); var fp = G(p + sd); var q = Quaternion.LookRotation(d); mk.Box(wetMud, fp + up * .07f, new Vector3(.5f, .02f, 1.1f), q); for (var s = 0; s < 4; s++) mk.Box(wetMud, fp + up * .07f + q * new Vector3(-.2f + s * .14f, 0, .68f), new Vector3(.11f, .02f, .16f), q); }
            // a fire, and on a fallen trunk beside it, somebody enormous with his back half turned, headphones on
            var fire = G(g + f.Fwd * 1.2f); mk.Prism(rock, fire, up, .9f, .25f, 9); mk.Prism(GlowM(new Color(1f, .5f, .15f), 3f), fire + up * .26f, up, .6f, .05f, 8); halos.Add(fire + up * .9f, 2f, new Color(1f, .55f, .2f), .9f); ArtistBits.PointLight(ctx.Root, fire + up * 1.6f, new Color(1f, .55f, .2f), 16f, 2.2f); ArtistBits.PostCollider(ctx.Root, fire, .35f, .9f);
            var log = G(g - f.Fwd * 2f); mk.Prism(bark, log + up * .5f - f.Right * 3f, f.Right, .5f, 6f, 9); Col(log + up * .5f, new Vector3(6f, 1f, 1f), f.r); soft.Blob(moss, log + up * .95f + f.Right * 2f, new Vector3(.8f, .1f, .4f), prng, .2f);
            var b = log + up * 1f - f.Right * .6f; var bq = Quaternion.LookRotation((f.Fwd + f.Right * .35f).normalized); var bf = bq * Vector3.forward; var bs = bq * Vector3.right;
            mk.Blob(fur, b + up * 1.05f, new Vector3(.95f, 1.15f, .75f), prng, .12f); mk.Blob(furB, b + up * 2.25f + bf * .1f, new Vector3(.5f, .55f, .5f), prng, .1f); mk.Box(skin, b + up * 2.2f + bf * .45f, new Vector3(.5f, .4f, .12f), bq); foreach (var e in new[] { -.14f, .14f }) mk.Blob(GlowM(new Color(1f, .85f, .5f), 1.4f), b + up * 2.3f + bf * .52f + bs * e, Vector3.one * .04f, prng, 0f);
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(fur, b + up * 1.7f + bs * e * .85f, (-up + bf * .5f + bs * e * .1f).normalized, .26f, 1.5f, 6, .8f); mk.Prism(fur, b + up * .2f + bs * e * .45f + bf * .2f, (bf + up * -.1f).normalized, .3f, 1.3f, 6); mk.Prism(fur, b + up * .1f + bs * e * .45f + bf * 1.4f, -up, .26f, 1f, 6, .9f); mk.Box(skin, b - up * .95f + bs * e * .45f + bf * 1.7f, new Vector3(.4f, .16f, .9f), bq); }
            var phones = new Material(WorldMaterials.Glow(new Color(.4f, .9f, 1f), 1.4f)) { name = "Big Fella's headphones" }; mk.Box(dark, b + up * 2.82f + bf * .1f, new Vector3(1.1f, .08f, .12f), bq); foreach (var e in new[] { -.55f, .55f }) mk.Blob(phones, b + up * 2.35f + bf * .1f + bs * e, new Vector3(.12f, .22f, .22f), prng, 0f); plife.phonesM = phones; halos.Add(b + up * 2.4f, 1.2f, new Color(.4f, .9f, 1f), .4f);
            Col(b + up * 1.2f + bf * .3f, new Vector3(2f, 3.4f, 2.2f), bq);
            ArtistBits.PointLight(ctx.Root, b + up * 2.2f + bf * 2.4f, new Color(1f, .7f, .45f), 9f, 2.2f);   // so you can see him
            var bgo = new GameObject("The Big Fella"); bgo.transform.SetParent(plife.transform, false); bgo.transform.position = b + up * 1.4f + bf * 1.2f; string[] says = { "He lifts one earcup. From inside it, very faintly: an acoustic guitar. He puts it back.", "He slides along the log to make room. The log creaks about it.", "He holds out a marshmallow on a stick the size of a fence post.", "He nods, slow, in time with something. You find you are nodding too.", "A low rumble. It might be a hum. It is exactly in tune." }; var sn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Say hi to the Big Fella", new Vector3(2.4f, 3f, 2f)).onUse = _ => Say(plife.transform, b + up * 3.6f, says[sn++ % says.Length], 6f);
            Interact.Bench(plife.transform, log + up * 1.06f + f.Right * 1.4f, log + up * 1.06f + f.Right * 2.4f, f.Fwd);
            // his things: a tape player, a stack of tapes to carry, a very large mug, a blurry photo nailed to a tree
            var tp = G(g + f.Right * 2.6f - f.Fwd * .2f); mk.Box(dark, tp + up * .18f, new Vector3(.7f, .36f, .3f), f.r); foreach (var e in new[] { -.18f, .18f }) mk.Prism(metal, tp + up * .2f + f.Right * e + f.Fwd * .15f, f.Fwd, .1f, .02f, 8); Col(tp + up * .18f, new Vector3(.7f, .36f, .3f), f.r);
            for (var k = 0; k < 3; k++) { var tm = new MeshKit(); tm.Box(WorldMaterials.Solid(Flannels[k]), new Vector3(0, .03f, 0), new Vector3(.2f, .06f, .13f), Quaternion.identity); var go = tm.BuildSingle("Cassette", plife.transform, false); go.transform.SetPositionAndRotation(tp + f.Right * .7f + up * (.02f + k * .07f), f.r * Quaternion.Euler(0, k * 20f, 0)); var c = Interact.Carry(go, new[] { "a tape marked \"quiet ones\"", "a tape marked \"loud ones\"", "a tape marked \"do not tape over\"" }[k], .2f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.22f, .07f, .15f); bc.center = new Vector3(0, .03f, 0); } }
            mk.Prism(WorldMaterials.Solid(new Color(.75f, .72f, .65f)), log + up * 1f - f.Right * 2.4f, up, .26f, .45f, 10);
            var tree = G(g - f.Fwd * 6f + f.Right * 4f); mk.Prism(bark, tree - up * .3f, up, .5f, 9f, 8, .7f); ArtistBits.PostCollider(ctx.Root, tree, 6f, .5f); mk.Box(WorldMaterials.Solid(new Color(.8f, .78f, .7f)), tree + up * 1.7f + f.Fwd * .5f, new Vector3(.5f, .4f, .03f), f.r); mk.Blob(fur, tree + up * 1.7f + f.Fwd * .52f, new Vector3(.08f, .13f, .01f), prng, .4f); ArtistBits.Label(ctx.Root, "not me", tree + up * 1.42f + f.Fwd * .52f, f.Fwd, .05f, new Color(.2f, .2f, .2f), TextAnchor.MiddleCenter);
            LogSeat(mk, fire + f.Right * 2.8f + f.Fwd * .6f, -f.Right, 1.8f);
            Spur(ctx, mk, g + f.Fwd * 5f); Ring(g, 9f);
            Shot(ctx, "the big fella", f.P(-5f, 2.4f, 9f), b + up * 1.6f);
        }
    }

    /// The Rainwood places' moving parts: the house show's lights thumping, the saw, the gas station's flickering
    /// strip light and its pinball machine, the Big Fella's headphones.
    public class GrungePlacesLife : MonoBehaviour
    {
        public Material ampM, stripM, pinM, pinBack, phonesM; public Light showLight, stripLight, pinLight; public Transform saw; public Vector3 sawAt, sawDir; public float sawing = -99f, pinHit = -99f; public bool multiball;
        static readonly Color Amp = new Color(1f, .35f, .2f), Strip = new Color(.85f, .95f, 1f), Pink = new Color(1f, .5f, .8f), Cyan = new Color(.4f, .9f, 1f);

        void Update()
        {
            var t = Time.time;
            var beat = Mathf.Max(0f, Mathf.Sin(t * 9.4f)); if (ampM != null) ampM.SetColor("_EmissionColor", Amp * (.6f + beat * 2f)); if (showLight != null) showLight.intensity = 2f + beat * 1.2f;
            if (saw != null) { var age = t - sawing; saw.position = sawAt + sawDir * (age < 4f ? Mathf.Sin(age * 7f) * .8f * (1f - age / 4f) : 0f); }
            if (stripM != null) { var n = Mathf.PerlinNoise(t * 9f, 2f); var on = n > .32f ? 1f : .15f; if (Mathf.Repeat(t, 11f) < .5f) on = 0f; stripM.SetColor("_EmissionColor", Strip * (2f * on)); if (stripLight != null) stripLight.intensity = 2f * on; }
            if (pinM != null) { var age = t - pinHit; var live = age < (multiball ? 11f : 4f) ? 1f : 0f; var flash = live * Mathf.Abs(Mathf.Sin(t * (multiball ? 22f : 10f))); pinM.SetColor("_EmissionColor", Pink * (1.2f + flash * 3f)); if (pinBack != null) pinBack.SetColor("_EmissionColor", Cyan * (1.2f + live * Mathf.Abs(Mathf.Cos(t * 13f)) * 3f)); if (pinLight != null) pinLight.intensity = 1.2f + flash * 2f; }
            if (phonesM != null) phonesM.SetColor("_EmissionColor", Cyan * (1.2f + .5f * Mathf.Sin(t * 2.2f)));
        }
    }
}
