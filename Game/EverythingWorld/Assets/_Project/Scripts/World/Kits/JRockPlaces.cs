using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The J-Rock Isles laid out on purpose (the build-land standard). The city, the school, the shrine and the train
    /// stay where they were; two more lantern-strung roads leave the city for the empty hills north and south. Along
    /// them, with the people of the Isles: the Konbini (a convenience store to walk into, cold-lit, rice balls and
    /// canned coffee) and the Batting Centre (a machine that pitches, bats to carry) in the north; the Summer
    /// Festival (two rows of stalls round a drum tower: a taiko to beat, goldfish, masks, fireworks to set off) and
    /// Cat Hill (a little shrine to a lucky cat and a dozen cats to stroke) in the south. The opening sequence's
    /// B-side: the episode where nothing happens and everyone goes to the festival.
    public partial class JRockKit
    {
        static readonly Vector2 KonbiniAt = new Vector2(7498, 7562), BattingAt = new Vector2(7618, 7570), FestAt = new Vector2(7562, 7050), CatAt = new Vector2(7684, 6972);
        static readonly Vector2[] NorthRoad = { new Vector2(7522, 7518), new Vector2(7512, 7540) };
        static readonly Vector2[] BattingRoad = { new Vector2(7512, 7540), new Vector2(7556, 7558), new Vector2(7598, 7566) };
        static readonly Vector2[] SouthRoad = { new Vector2(7550, 7205), new Vector2(7556, 7132), new Vector2(7560, 7084) };
        static readonly Vector2[] CatRoad = { new Vector2(7578, 7040), new Vector2(7624, 7010), new Vector2(7666, 6982) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (KonbiniAt, 9f), (BattingAt, 12f), (FestAt, 17f), (CatAt, 10f) };

        JRockPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; Material laneM; readonly List<Vector3> laneP = new List<Vector3>();

        static readonly Color[] Yukata = { new Color(.25f, .3f, .55f), new Color(.85f, .35f, .45f), new Color(.92f, .9f, .85f), new Color(.3f, .5f, .45f), new Color(.6f, .4f, .7f), new Color(.2f, .2f, .26f) };
        static readonly Color[] Obi = { new Color(.85f, .2f, .2f), new Color(.95f, .8f, .3f), new Color(.2f, .2f, .26f), new Color(.92f, .9f, .85f) };
        static readonly Color[] HairC = { new Color(.1f, .08f, .08f), new Color(.2f, .14f, .1f), new Color(1f, .5f, .7f), new Color(.4f, .7f, 1f), new Color(.9f, .9f, .95f), new Color(.1f, .08f, .08f) };
        static readonly string[] IslesTalk =
        {
            "It's always the last week of summer here. The cicadas never get the note about school.", "Run to the station! We're not late. It's just what you do in an opening.", "The train comes when the chorus does. Check if you don't believe me.",
            "Someone's about to transfer into our class. Someone always is.", "If the wind picks up and the petals go sideways, something important is being said nearby.",
        };

        Residents.Look Student(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Yukata, Obi, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Straw, Residents.Hat.Cap }, Obi); l.hair = HairC[prng.Next(HairC.Length)]; l.longHair = prng.Next(2) == 0; l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Student(), pose, prng, lines ?? IslesTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeIsles(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
        }

        /// A vertical neon board on a pole, like the city's. Signs on the +x side of a place's road; the photo spot on the −x side.
        void NeonBoard(MeshKit mk, Vector3 at, Vector3 face, string text, Color c, float size = .22f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            mk.Prism(metal, g - up * .2f, up, .08f, 3.6f, 6); mk.Box(dark, g + up * 2.4f, new Vector3(2.8f, 1.1f, .14f), rot); mk.Box(GlowM(c, 2.4f), g + up * 2.98f, new Vector3(2.8f, .06f, .16f), rot); mk.Box(GlowM(c, 2.4f), g + up * 1.82f, new Vector3(2.8f, .06f, .16f), rot); Col(g + up * 1.8f, new Vector3(2.8f, 3.6f, .2f), rot); halos.Add(g + up * 2.4f + face * .4f, 1.6f, c, .45f);
            ArtistBits.Label(rootT, text, g + up * 2.4f + face * .08f, face, size, c, TextAnchor.MiddleCenter);
        }

        void ParkBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .45f, new Vector3(len, .08f, .5f), rot); mk.Box(wood, g + up * .8f - face * .24f, new Vector3(len, .45f, .06f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(metal, g + up * .3f + side * e * (len / 2f - .15f) - face * .1f, new Vector3(.06f, .9f, .5f), rot);
            Col(g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(plife.transform, g + up * .52f - side * (len / 2f - .4f), g + up * .52f + side * (len / 2f - .4f), face);
        }

        List<Vector3> LanternRoad(LandContext ctx, MeshKit mk, Vector2[] way, bool join)
        {
            var full = new List<Vector2>();
            if (join) { Vector2 near = way[0]; var bd = 220f * 220f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } } full.Add(near); }
            full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2f);
            for (var i = 0; i < pts.Count; i++) { foreach (var a in ctx.Artists) { var r = Foot(a) + 6f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } } }
            if (pts.Count < 2) return pts;
            Strip(mk, pts, laneM, PathW + .4f, .16f, ctx); foreach (var p in pts) Remember(p, 700);
            // power poles down one side with lanterns strung between them, the way the city's streets have
            var up = Vector3.up; Vector3? prev = null;
            for (var i = 3; i < pts.Count; i += 9) { var d = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (PathW / 2f + 1.1f); if (InNoGrass(lp.x, lp.z, 1f) || !ctx.IsLand(lp.x, lp.z, 2f)) { prev = null; continue; } lp = ctx.OnGround(lp.x, lp.z); mk.Prism(woodDark, lp - up * .2f, up, .11f, 5.2f, 6); mk.Box(woodDark, lp + up * 4.7f, new Vector3(1.2f, .08f, .08f), Quaternion.LookRotation(d)); ArtistBits.PostCollider(ctx.Root, lp, 5f, .13f); var top = lp + up * 4.3f; if (prev.HasValue) LanternString(mk, prev.Value, top, prng); prev = top; }
            laneP.AddRange(pts); return pts;
        }

        void Alley(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(laneP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.7f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, laneM, 2.2f, .15f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void IslesPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            laneP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2011); laneM = WorldMaterials.Solid(new Color(.42f, .4f, .42f));
            plife = new GameObject("J-Rock places life").AddComponent<JRockPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            LanternRoad(ctx, mk, NorthRoad, true); LanternRoad(ctx, mk, BattingRoad, false); LanternRoad(ctx, mk, SouthRoad, true); LanternRoad(ctx, mk, CatRoad, false);
            Konbini(ctx, mk, solid); BattingCentre(ctx, mk, solid); Festival(ctx, mk, solid); CatHill(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Konbini
        void Konbini(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(KonbiniAt.x, KonbiniAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); const float W = 10f, D = 7f, H = 3.2f, T = .3f, Door = HumanScale.DoorWidth + .6f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var white = WorldMaterials.Solid(new Color(.95f, .95f, .96f)); var cool = new Color(.85f, .95f, 1f); var stripeA = WorldMaterials.Solid(new Color(.2f, .6f, .45f)); var stripeB = WorldMaterials.Solid(new Color(.95f, .55f, .2f));
            mk.Box(concrete, g - up * .35f, new Vector3(W + 3f, 1.2f, D + 5f), f.r); solid.Box(concrete, g - up * .35f, new Vector3(W + 3f, 1.2f, D + 5f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(white, p, s, f.r); solid.Box(white, p, s, f.r); }
            mk.Box(white, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            // the whole front is glass, lit from inside, with the sliding doors in the middle
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) { Wall(fo.P(e * (Door / 2f + seg / 2f), .3f, D / 2f - T / 2f), new Vector3(seg, .6f, T)); Wall(fo.P(e * (W / 2f - .15f), H / 2f, D / 2f - T / 2f), new Vector3(.3f, H, T)); foreach (var q in new[] { -.33f, 0f, .33f }) mk.Box(metal, fo.P(e * (Door / 2f + seg / 2f) + q * (seg - .3f), 1.6f, D / 2f - T / 2f), new Vector3(.05f, 2f, .06f), f.r); mk.Box(WorldMaterials.Glow(cool, .5f), fo.P(e * (Door / 2f + seg / 2f), 2.45f, D / 2f - T / 2f), new Vector3(seg - .3f, .3f, .05f), f.r); solid.Box(white, fo.P(e * (Door / 2f + seg / 2f), 1.6f, D / 2f - T / 2f), new Vector3(seg - .3f, 2f, .1f), f.r); }
            Wall(fo.P(0, H - .3f, D / 2f - T / 2f), new Vector3(W, .6f, T)); mk.Box(white, fo.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, D + .4f), f.r); solid.Box(white, fo.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, D + .4f), f.r);
            mk.Box(stripeA, fo.P(0, H + .55f, D / 2f + .05f), new Vector3(W + .4f, .3f, .12f), f.r); mk.Box(stripeB, fo.P(0, H + .85f, D / 2f + .05f), new Vector3(W + .4f, .3f, .12f), f.r); mk.Box(white, fo.P(0, H + .25f, D / 2f + .05f), new Vector3(W + .4f, .3f, .12f), f.r); ArtistBits.Label(ctx.Root, "24h  ISLE MART", fo.P(0, H + .7f, D / 2f + .13f), f.Fwd, .34f, new Color(.98f, .98f, 1f), TextAnchor.MiddleCenter);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f)); halos.Add(fo.P(0, 1.6f, D / 2f + 1f), 5f, cool, .3f); ArtistBits.PointLight(ctx.Root, fo.P(0, 2f, D / 2f + 2.4f), cool, 14f, 1.6f);
            // inside: two aisles of shelves, a wall of drinks fridges, the counter with hot food, a magazine rack
            Color[] goods = { new Color(1f, .5f, .5f), new Color(.5f, .8f, 1f), new Color(1f, .9f, .5f), new Color(.6f, 1f, .6f), new Color(.95f, .7f, 1f), new Color(1f, .7f, .4f) };
            foreach (var x in new[] { -1.4f, 1.4f }) { mk.Box(metal, fo.P(x, .7f, -.3f), new Vector3(.6f, 1.4f, 3.4f), f.r); Col(fo.P(x, .7f, -.3f), new Vector3(.6f, 1.4f, 3.4f), f.r); for (var row = 0; row < 3; row++) for (var k = 0; k < 8; k++) foreach (var e in new[] { -1f, 1f }) mk.Box(WorldMaterials.Glow(goods[(k + row + (e > 0 ? 3 : 0)) % goods.Length], .25f), fo.P(x + e * .32f, .35f + row * .45f, -1.8f + k * .43f), new Vector3(.05f, .3f, .34f), f.r); }
            mk.Box(metal, fo.P(0, 1.2f, -D / 2f + T + .35f), new Vector3(W - 1f, 2.4f, .6f), f.r); Col(fo.P(0, 1.2f, -D / 2f + T + .35f), new Vector3(W - 1f, 2.4f, .6f), f.r); for (var k = 0; k < 6; k++) { mk.Box(GlowM(cool, 1.2f), fo.P(-3.6f + k * 1.45f, 1.3f, -D / 2f + T + .67f), new Vector3(1.3f, 2f, .03f), f.r); for (var s = 0; s < 12; s++) mk.Prism(WorldMaterials.Glow(goods[(k + s) % goods.Length], .4f), fo.P(-4.1f + k * 1.45f + (s % 4) * .32f, .5f + (s / 4) * .6f, -D / 2f + T + .72f), up, .08f, .3f, 6); }
            var ct = new PlaceFrame(fo.P(-W / 2f + T + 1.5f, 0, 1.6f), f.Right); mk.Box(white, ct.P(0, .5f, 0), new Vector3(2.6f, 1f, .8f), ct.r); mk.Box(stripeA, ct.P(0, 1.02f, 0), new Vector3(2.7f, .05f, .9f), ct.r); Col(ct.P(0, .55f, 0), new Vector3(2.7f, 1.1f, .9f), ct.r); mk.Box(GlowM(new Color(1f, .75f, .4f), 1f), ct.P(.8f, 1.3f, 0), new Vector3(.8f, .5f, .6f), ct.r);
            var clerk = Student(Residents.Tool.None, true); clerk.top = new Color(.2f, .6f, .45f); Folk(ct.P(0, 0, -.8f), ct.Fwd, Resident.Pose.Stand, new[] { "Irasshaimase! ...Sorry. It's automatic. I say it to the door when the wind opens it.", "The rice balls are on the left. The good rice balls are behind those.", "You're the third hero through tonight. They always buy one canned coffee and look at the moon." }, clerk);
            string[] things = { "a salmon rice ball", "a tuna-mayo rice ball", "a can of hot coffee", "a melon bread", "a bottle of ramune" };
            for (var k = 0; k < 5; k++)
            {
                var m = new MeshKit(); var size = new Vector3(.16f, .14f, .12f);
                if (k < 2) { m.Prism(WorldMaterials.Solid(new Color(.97f, .97f, .95f)), new Vector3(0, .01f, -.03f), Vector3.forward, .09f, .06f, 3); m.Box(dark, new Vector3(0, .03f, 0), new Vector3(.1f, .06f, .065f), Quaternion.identity); }
                else if (k == 2) { m.Prism(WorldMaterials.Solid(new Color(.45f, .25f, .15f), .7f), Vector3.zero, Vector3.up, .035f, .12f, 8); size = new Vector3(.09f, .13f, .09f); }
                else if (k == 3) { m.Blob(WorldMaterials.Solid(new Color(.9f, .85f, .5f)), new Vector3(0, .05f, 0), new Vector3(.09f, .05f, .09f), prng, .08f); }
                else { m.Prism(WorldMaterials.Glow(new Color(.6f, .9f, 1f), .3f), Vector3.zero, Vector3.up, .035f, .2f, 8, .6f); size = new Vector3(.09f, .22f, .09f); }
                var go = m.BuildSingle("Konbini thing", plife.transform, false); go.transform.position = ct.P(-1f + k * .38f, 1.06f, .1f); var c = Interact.Carry(go, things[k], .2f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.8f, 0), cool, 13f, 1.7f, true);
            // outside: a bin, a bench, a bicycle, two students eating ice lollies under the light
            ParkBench(mk, f.P(3.4f, 0, D / 2f + 1.6f), f.Fwd); Vending(ctx, mk, G(f.P(-W / 2f - .9f, 0, D / 2f + .4f)) + up * .05f, f.Fwd, 3);
            Folk(G(f.P(3.4f, 0, D / 2f + 1.6f)) + up * .52f - f.Right * .45f, f.Fwd, Resident.Pose.Sit, new[] { "After cram school we always end up here. Even now there's no cram school.", "If you get a winning stick you get another ice. I've had eleven. It's becoming a problem.", "This is the part of the episode where we talk about the future. Pass." }, Student(), null, .52f);
            Alley(ctx, mk, f.P(0, 0, D / 2f + 2f)); Ring(g, 8f);
            Shot(ctx, "the konbini", f.P(-6f, 2.4f, D / 2f + 11f), fo.P(0, 2f, 0));
            Shot(ctx, "inside the konbini", fo.P(3.6f, 1.6f, D / 2f - 1.1f), fo.P(-1.5f, 1.1f, -D / 2f + 1.5f));
        }

        // ---------------------------------------------------------------- the Batting Centre
        void BattingCentre(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BattingAt.x, BattingAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); const float L = 20f, W = 9f, H = 5f;
            mk.Box(WorldMaterials.Solid(new Color(.3f, .45f, .35f)), g - up * .35f, new Vector3(W + 3f, 1f, L + 6f), f.r); solid.Box(concrete, g - up * .35f, new Vector3(W + 3f, 1f, L + 6f), f.r); var fo = new PlaceFrame(g + up * .15f, f.Fwd);
            // a net cage: green netting on a steel frame, open at the batter's end; home plate; a pitching machine at the far end
            foreach (var e in new[] { -1f, 1f }) { for (var k = 0; k <= 4; k++) { var p = fo.P(e * W / 2f, 0, -L / 2f + k * L / 4f); mk.Prism(metal, p, up, .07f, H, 6); ArtistBits.PostCollider(ctx.Root, p, H, .09f); } mk.Box(metal, fo.P(e * W / 2f, H, 0), new Vector3(.08f, .08f, L), f.r); Col(fo.P(e * W / 2f, H / 2f, 0), new Vector3(.12f, H, L), f.r); }
            mk.Box(metal, fo.P(0, H, -L / 2f), new Vector3(W, .08f, .08f), f.r); Col(fo.P(0, H / 2f, -L / 2f), new Vector3(W, H, .12f), f.r); var net = WorldMaterials.Solid(new Color(.2f, .4f, .3f)); for (var k = 0; k < 24; k++) { var z = -L / 2f + (k + .5f) * L / 24f; foreach (var e in new[] { -1f, 1f }) mk.Box(net, fo.P(e * W / 2f, H / 2f, z), new Vector3(.02f, H, .03f), f.r * Quaternion.Euler(k % 2 == 0 ? 25f : -25f, 0, 0)); mk.Box(net, fo.P(0, H, z), new Vector3(W, .02f, .03f), f.r); }
            mk.Prism(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), fo.P(0, .02f, L / 2f - 2.4f), up, .35f, .02f, 5); foreach (var e in new[] { -1f, 1f }) mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), fo.P(e * 1f, .02f, L / 2f - 2.4f), new Vector3(.08f, .01f, 1.8f), f.r);
            var mc = fo.P(0, 0, -L / 2f + 2f); mk.Box(WorldMaterials.Solid(new Color(.2f, .3f, .6f), .6f), mc + up * .7f, new Vector3(1f, 1.4f, 1.2f), f.r); mk.Prism(metal, mc + up * 1.3f + f.Fwd * .2f, (f.Fwd + up * .25f).normalized, .1f, .9f, 8); foreach (var e in new[] { -.35f, .35f }) mk.Prism(dark, mc + up * 1.3f + f.Right * e - f.Right * .05f * Mathf.Sign(e), f.Right * Mathf.Sign(e), .3f, .1f, 12); Col(mc + up * .8f, new Vector3(1f, 1.6f, 1.2f), f.r);
            var lamp = new Material(WorldMaterials.Glow(new Color(.4f, 1f, .5f), .6f)) { name = "Pitch ready light" }; mk.Blob(lamp, mc + up * 1.7f, Vector3.one * .1f, prng, 0f); plife.pitchM = lamp; plife.launchFrom = mc + up * 1.45f + f.Fwd * 1.2f; plife.launchDir = (f.Fwd + up * .12f).normalized;
            // the button on a post by the plate; balls in a bucket (they are what gets pitched), bats in a barrel
            var bp = fo.P(-2.4f, 0, L / 2f - 1.6f); mk.Prism(metal, bp, up, .06f, 1.1f, 6); var bm = new MeshKit(); bm.Prism(GlowM(new Color(1f, .3f, .2f), 2.4f), Vector3.zero, Vector3.up, .12f, .06f, 10); var bgo = bm.BuildSingle("Pitch button", plife.transform, false); bgo.transform.position = bp + up * 1.1f; ArtistBits.PostCollider(ctx.Root, bp, 1.1f, .08f);
            string[] pitch = { "Thwock. Fastball, down the middle.", "A curve. It knows what it did.", "Thwock. High and inside. Rude.", "It hesitates... then lobs one, gently, as if it's sorry." }; var pn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Press for a pitch", new Vector3(.6f, .6f, .6f)).onUse = _ => { if (plife.Pitch()) Say(plife.transform, bp + up * 2.2f, pitch[pn++ % pitch.Length], 3.5f); else Say(plife.transform, bp + up * 2.2f, "The machine's empty. Somebody go and fetch the balls.", 3.5f); };
            var bucket = fo.P(2.6f, 0, L / 2f - 1.2f); mk.Prism(metal, bucket, up, .3f, .4f, 8, 1.15f); ArtistBits.PostCollider(ctx.Root, bucket, .4f, .32f);
            for (var k = 0; k < 8; k++) { var m = new MeshKit(); m.Blob(WorldMaterials.Solid(new Color(.96f, .96f, .92f)), Vector3.zero, Vector3.one * .06f, prng, 0f); m.Box(WorldMaterials.Solid(new Color(.8f, .15f, .15f)), Vector3.zero, new Vector3(.125f, .012f, .03f), Quaternion.Euler(0, 0, 30f)); var go = m.BuildSingle("Baseball", plife.transform, false); go.transform.position = bucket + up * (.46f + (k / 4) * .1f) + new Vector3(Mathf.Cos(k * 1.6f), 0, Mathf.Sin(k * 1.6f)) * .14f; var c = Interact.Carry(go, "a baseball", .2f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = Vector3.one * .13f; bc.center = Vector3.zero; } plife.balls.Add(c); }
            var barrel = fo.P(3.6f, 0, L / 2f + 1.2f); mk.Prism(wood, barrel, up, .35f, .8f, 10); ArtistBits.PostCollider(ctx.Root, barrel, .8f, .36f);
            for (var k = 0; k < 3; k++) { var m = new MeshKit(); var bw = WorldMaterials.Solid(k == 1 ? new Color(.75f, .76f, .8f) : new Color(.8f, .65f, .42f), k == 1 ? .8f : .3f); m.Prism(bw, Vector3.zero, Vector3.up, .025f, .35f, 6); m.Prism(bw, new Vector3(0, .35f, 0), Vector3.up, .025f, .55f, 6, 1.8f); var go = m.BuildSingle("Bat", plife.transform, false); go.transform.SetPositionAndRotation(barrel + up * .5f + new Vector3(Mathf.Cos(k * 2.1f), 0, Mathf.Sin(k * 2.1f)) * .15f, Quaternion.Euler(8f, k * 90f, 6f)); var c = Interact.Carry(go, k == 1 ? "a metal bat" : "a wooden bat", .9f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.1f, .92f, .1f); bc.center = new Vector3(0, .45f, 0); } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            // floodlights, a scoreboard of home-run targets on the back net, a bench, a regular
            foreach (var e in new[] { -1f, 1f }) { var lp = G(f.P(e * (W / 2f + 1.6f), 0, L / 2f + 1f)); mk.Prism(metal, lp, up, .1f, 7f, 6); mk.Box(GlowM(new Color(.95f, .97f, 1f), 2.6f), lp + up * 7f - f.Fwd * .3f, new Vector3(1.2f, .5f, .2f), f.r * Quaternion.Euler(-25f, 0, 0)); halos.Add(lp + up * 6.8f, 2.4f, new Color(.9f, .95f, 1f), .7f); ArtistBits.PostCollider(ctx.Root, lp, 7f, .12f); ArtistBits.PointLight(ctx.Root, lp + up * 6f - f.Fwd * 3f, new Color(.9f, .95f, 1f), 22f, 1.6f); }
            for (var k = 0; k < 3; k++) { var tp = fo.P(-2.8f + k * 2.8f, 3.2f, -L / 2f + .15f); mk.Prism(k == 1 ? GlowM(new Color(1f, .8f, .3f), 2f) : WorldMaterials.Solid(new Color(.9f, .9f, .9f)), tp, f.Fwd, .6f, .06f, 14); ArtistBits.Label(ctx.Root, k == 1 ? "HOME\nRUN" : "HIT", tp + f.Fwd * .08f, f.Fwd, .16f, new Color(.15f, .15f, .2f), TextAnchor.MiddleCenter); }
            ParkBench(mk, f.P(-3.4f, 0, L / 2f + 2.4f), -f.Fwd);
            Folk(G(f.P(-3.4f, 0, L / 2f + 2.4f)) + up * .52f + f.Right * .45f, -f.Fwd, Resident.Pose.Sit, new[] { "One hundred swings a day. It's in the training arc. I'm on day four thousand.", "You can't hit them, exactly. But you can pick one up and throw it back, which is the same feeling.", "The home-run target's never been hit. The episode it gets hit, the series ends." }, Student(), null, .52f);
            NeonBoard(mk, f.P(6.4f, 0, L / 2f + 5f), f.Fwd, "BATTING CENTRE", Lime, .2f);
            Alley(ctx, mk, f.P(0, 0, L / 2f + 2.5f)); Ring(g, 12f);
            Shot(ctx, "the batting centre", f.P(-8f, 3.6f, L / 2f + 10f), fo.P(0, 2f, 0));
        }

        // ---------------------------------------------------------------- the Summer Festival
        void Festival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(FestAt.x, FestAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); var fo = new PlaceFrame(g + up * .12f, f.Fwd);
            mk.Prism(WorldMaterials.Solid(new Color(.5f, .45f, .4f)), g - up * .05f, up, 16f, .16f, 20);
            // the yagura: a red and white drum tower in the middle, a taiko on top, lantern strings running out to four poles
            const float YH = 3.2f; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { mk.Box(woodDark, fo.P(x * 1.6f, YH / 2f, z * 1.6f), new Vector3(.22f, YH, .22f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(x * 1.6f, 0, z * 1.6f), YH, .15f); }
            mk.Box(wood, fo.P(0, YH, 0), new Vector3(4f, .2f, 4f), f.r); solid.Box(wood, fo.P(0, YH, 0), new Vector3(4f, .2f, 4f), f.r); for (var k = 0; k < 4; k++) { var d = f.r * (Quaternion.Euler(0, k * 90f, 0) * Vector3.forward); var sd = Vector3.Cross(up, d); for (var s = 0; s < 4; s++) mk.Box(s % 2 == 0 ? red : WorldMaterials.Solid(new Color(.95f, .95f, .95f)), fo.o + d * 2.02f + sd * (-1.5f + s) + up * (YH - .55f), new Vector3(1f, .9f, .04f), Quaternion.LookRotation(d)); mk.Box(woodDark, fo.o + d * 1.9f + up * (YH + 1f), new Vector3(3.8f, .1f, .1f), Quaternion.LookRotation(d) * Quaternion.Euler(0, 90f, 0)); foreach (var e in new[] { -1.9f, 1.9f }) mk.Box(woodDark, fo.o + d * 1.9f + sd * e + up * (YH + .5f), new Vector3(.1f, 1f, .1f), f.r); }
            // steps up one side
            for (var k = 0; k < 9; k++) { var sp = fo.P(-1f, YH * (k + .5f) / 9f - .1f, 2.2f + (8 - k) * .36f); mk.Box(wood, sp, new Vector3(1.2f, .2f, .38f), f.r); solid.Box(wood, sp - up * (YH * (k + .5f) / 18f), new Vector3(1.2f, YH * (k + 1) / 9f, .38f), f.r); }
            var drum = fo.P(.5f, YH + .1f, 0); mk.Box(woodDark, drum + up * .45f, new Vector3(.9f, .9f, .9f), f.r * Quaternion.Euler(0, 45f, 0)); var dm = new MeshKit(); dm.Prism(WorldMaterials.Solid(new Color(.5f, .2f, .12f)), new Vector3(0, 0, -.5f), Vector3.forward, .62f, 1f, 14); var skin = new Material(WorldMaterials.Glow(new Color(.96f, .93f, .85f), .2f)) { name = "Taiko skin" }; dm.Prism(skin, new Vector3(0, 0, .5f), Vector3.forward, .58f, .02f, 14); dm.Prism(skin, new Vector3(0, 0, -.52f), Vector3.forward, .58f, .02f, 14);
            var dgo = dm.BuildSingle("Taiko", plife.transform, false); dgo.transform.SetPositionAndRotation(drum + up * 1.3f, f.r * Quaternion.Euler(-20f, 90f, 0)); Col(drum + up * .9f, new Vector3(1.2f, 1.8f, 1.2f), f.r); plife.taikoM = skin; plife.taikoLight = ArtistBits.PointLight(ctx.Root, drum + up * 2.4f, new Color(1f, .5f, .3f), 22f, 0f);
            string[] boom = { "DON!", "DON! DON!", "DON-doko-DON!", "DON! ...Everyone at the stalls looks up and claps twice." }; var bn = 0; var tt = Interact.Toy(dgo, Toy.Kind.Custom, "Beat the taiko", new Vector3(1.6f, 1.6f, 1.6f)); tt.onUse = _ => { plife.taiko = Time.time; Say(plife.transform, drum + up * 3.2f, boom[bn++ % boom.Length], 2.6f); };
            var poles = new List<Vector3>(); for (var k = 0; k < 6; k++) { var p = G(g + Quaternion.Euler(0, k * 60f + 30f, 0) * f.Fwd * 14.5f); mk.Prism(woodDark, p - up * .2f, up, .1f, 5f, 6); ArtistBits.PostCollider(ctx.Root, p, 5f, .12f); poles.Add(p + up * 4.6f); LanternString(mk, fo.P(0, YH + 2f, 0), p + up * 4.6f, prng); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, YH + 2.6f, 0), new Color(1f, .5f, .3f), 30f, 2f);
            // six stalls in a ring facing the tower: striped roofs, a counter, a keeper each, something to do or take
            string[] names = { "GOLDFISH", "YAKISOBA", "MASKS", "SHAVED ICE", "RING TOSS", "TAKOYAKI" }; Color[] roofs = { new Color(.85f, .2f, .2f), new Color(.95f, .7f, .2f), new Color(.3f, .5f, .8f), new Color(.4f, .8f, .9f), new Color(.6f, .35f, .8f), new Color(.9f, .4f, .3f) };
            string[][] cries =
            {
                new[] { "Scoop a goldfish! The paper breaks every time. That's the lesson. That's summer.", "This one's called Chorus. He comes back round." },
                new[] { "Yakisoba! Hot! I've been flipping the same noodles since the first verse.", "Extra ginger for the hero. You look like a hero. Or a rival. Rivals get extra ginger too." },
                new[] { "Fox, cat, or mysterious transfer student. The masks, I mean.", "Wear it on the side of your head. Nobody wears them on their face. It's a rule." },
                new[] { "Blue Hawaii or strawberry? Both turn your tongue a colour your mother won't like.", "It's just ice. It's the best thing you'll eat all year. Both are true." },
                new[] { "Three rings! Land one on the bottle and win... the bottle.", "Nobody's won since the pilot episode." },
                new[] { "Takoyaki! Careful, the middle is the temperature of the sun.", "Six to a boat. Share them with someone on the hill and say nothing. That's the scene." },
            };
            for (var k = 0; k < 6; k++)
            {
                if (k == 0) { }   // (the way in is between stalls 5 and 0, so nothing is skipped)
                var a = k * 60f + 60f; var d = Quaternion.Euler(0, a, 0) * f.Fwd; var sc = G(g + d * 10.5f); var sf = new PlaceFrame(sc, -d); var roof = WorldMaterials.Solid(roofs[k], .3f); var cream = WorldMaterials.Solid(new Color(.95f, .93f, .88f), .3f);
                foreach (var (x, z) in new[] { (-1.6f, -1f), (1.6f, -1f), (1.6f, 1f), (-1.6f, 1f) }) { mk.Box(woodDark, sf.P(x, 1.2f, z), new Vector3(.1f, 2.4f, .1f), sf.r); ArtistBits.PostCollider(ctx.Root, sf.P(x, 0, z), 2.4f, .08f); }
                for (var s = 0; s < 6; s++) mk.Box(s % 2 == 0 ? roof : cream, sf.P(-1.5f + s * .6f, 2.55f, .1f), new Vector3(.6f, .06f, 2.8f), sf.r * Quaternion.Euler(-10f, 0, 0)); mk.Box(roof, sf.P(0, 2.2f, 1.2f), new Vector3(3.4f, .5f, .05f), sf.r); ArtistBits.Label(ctx.Root, names[k], sf.P(0, 2.2f, 1.24f), sf.Fwd, .22f, new Color(.98f, .97f, .92f), TextAnchor.MiddleCenter);
                mk.Box(wood, sf.P(0, .95f, .7f), new Vector3(3.2f, .08f, .7f), sf.r); mk.Box(cream, sf.P(0, .47f, .98f), new Vector3(3.2f, .95f, .05f), sf.r); Col(sf.P(0, .5f, .7f), new Vector3(3.2f, 1f, .7f), sf.r); PaperLantern(mk, sf.P(-1.4f, 2f, 1.3f), prng); PaperLantern(mk, sf.P(1.4f, 2f, 1.3f), prng);
                Folk(sf.P(0, 0, -.2f), sf.Fwd, k % 2 == 0 ? Resident.Pose.Stand : Resident.Pose.Work, cries[k], Student(k == 1 || k == 5 ? Residents.Tool.Spatula : Residents.Tool.None, true)).workRate = .9f;
                // each stall's thing
                if (k == 0) { mk.Box(WorldMaterials.Glow(new Color(.3f, .6f, .9f), .4f), sf.P(0, 1.02f, .7f), new Vector3(2.4f, .06f, .5f), sf.r); for (var s = 0; s < 6; s++) mk.Blob(GlowM(s % 3 == 0 ? new Color(.95f, .95f, .95f) : new Color(1f, .45f, .15f), 1.6f), sf.P(-1f + s * .4f, 1.07f, .6f + (s % 2) * .2f), new Vector3(.07f, .03f, .04f), prng, 0f); }
                if (k == 2) for (var s = 0; s < 4; s++) { var m = new MeshKit(); var mc = s == 0 ? new Color(.95f, .95f, .95f) : s == 1 ? new Color(.95f, .6f, .2f) : s == 2 ? new Color(.2f, .2f, .26f) : new Color(1f, .6f, .75f); m.Blob(WorldMaterials.Solid(mc), Vector3.zero, new Vector3(.14f, .16f, .04f), prng, 0f); foreach (var e in new[] { -.08f, .08f }) { m.Cone(WorldMaterials.Solid(mc), new Vector3(e, .12f, 0), .05f, .1f, 4); m.Blob(GlowM(new Color(1f, .3f, .2f), 1.6f), new Vector3(e * .7f, .02f, .04f), new Vector3(.03f, .015f, .01f), prng, 0f); } var go = m.BuildSingle("Festival mask", plife.transform, false); go.transform.SetPositionAndRotation(sf.P(-1.1f + s * .72f, 1.16f, .7f), sf.r); var c = Interact.Carry(go, new[] { "a fox mask", "a cat mask", "a tengu mask", "a rabbit mask" }[s], .2f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.3f, .34f, .1f); bc.center = Vector3.zero; } }
                if (k == 3) for (var s = 0; s < 3; s++) { var m = new MeshKit(); m.Cone(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), new Vector3(0, .16f, 0), .08f, -.16f, 8); m.Blob(WorldMaterials.Glow(s == 0 ? new Color(.3f, .7f, 1f) : s == 1 ? new Color(1f, .4f, .5f) : new Color(.6f, 1f, .5f), .5f), new Vector3(0, .2f, 0), Vector3.one * .09f, prng, .1f); var go = m.BuildSingle("Shaved ice", plife.transform, false); go.transform.position = sf.P(-.7f + s * .7f, 1f, .7f); var c = Interact.Carry(go, "a cup of shaved ice", .2f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.18f, .3f, .18f); bc.center = new Vector3(0, .15f, 0); } }
                if (k == 4) { for (var s = 0; s < 5; s++) mk.Prism(WorldMaterials.Glow(new Color(.5f, .8f, .6f), .3f), sf.P(-.8f + s * .4f, 1f, .2f - (s % 2) * .3f - .6f), up, .05f, .26f, 6, .5f); for (var s = 0; s < 3; s++) { var m = new MeshKit(); for (var q = 0; q < 10; q++) { var an = q * Mathf.PI / 5f; m.Box(WorldMaterials.Solid(new[] { new Color(.85f, .2f, .2f), new Color(.95f, .8f, .2f), new Color(.2f, .4f, .8f) }[s]), new Vector3(Mathf.Cos(an) * .12f, 0, Mathf.Sin(an) * .12f), new Vector3(.03f, .03f, .09f), Quaternion.Euler(0, -q * 36f, 0)); } var go = m.BuildSingle("Toss ring", plife.transform, false); go.transform.position = sf.P(-.5f + s * .5f, 1.02f, .85f); var c = Interact.Carry(go, "a ring to toss", .15f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.3f, .04f, .3f); bc.center = Vector3.zero; } } }
                if (k == 1 || k == 5) { mk.Box(metal, sf.P(.4f, 1.03f, .6f), new Vector3(1.4f, .06f, .5f), sf.r); mk.Box(GlowM(new Color(1f, .5f, .2f), 1f), sf.P(.4f, .98f, .6f), new Vector3(1.2f, .02f, .4f), sf.r); for (var s = 0; s < 3; s++) { var m = new MeshKit(); m.Box(WorldMaterials.Solid(new Color(.9f, .85f, .7f)), new Vector3(0, .02f, 0), new Vector3(.3f, .04f, .14f), Quaternion.identity); for (var q = 0; q < (k == 5 ? 4 : 1); q++) m.Blob(WorldMaterials.Solid(k == 5 ? new Color(.75f, .5f, .3f) : new Color(.8f, .6f, .35f)), new Vector3(k == 5 ? -.1f + q * .07f : 0, .06f, 0), k == 5 ? Vector3.one * .035f : new Vector3(.12f, .03f, .05f), prng, .1f); var go = m.BuildSingle("Festival food", plife.transform, false); go.transform.SetPositionAndRotation(sf.P(-1.2f + s * .4f, 1f, .75f), sf.r); var c = Interact.Carry(go, k == 5 ? "a boat of takoyaki" : "a tray of yakisoba", .25f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.32f, .1f, .16f); bc.center = new Vector3(0, .04f, 0); } } }
            }
            // people in yukata walking the ring, two sitting on the tower steps; the firework mortars at the back
            var ring = new List<Vector3>(); for (var k = 0; k < 12; k++) { var p = g + Quaternion.Euler(0, k * 30f, 0) * f.Fwd * 6.6f; ring.Add(G(p) + up * .1f); }
            Folk(ring[0], FaceTo(ring[0], ring[1]), Resident.Pose.Walk, new[] { "Once round for the food, once round for the games, once round because it's nearly over.", "I lost my friends on purpose. You have to, to bump into the right person by the goldfish." }, Student(), ring.ToArray()).speed = 1f;
            var back = new List<Vector3>(ring); back.Reverse(); var r2 = new List<Vector3>(); for (var k = 0; k < back.Count; k++) r2.Add(back[(k + 5) % back.Count] + (back[(k + 5) % back.Count] - g).normalized * 1.1f); Folk(r2[0], FaceTo(r2[0], r2[1]), Resident.Pose.Walk, new[] { "The fireworks are at nine. It's been ten to nine all night.", "My geta are killing me. Worth it." }, Student(), r2.ToArray()).speed = .9f;
            var fw = G(g - f.Fwd * 13.5f); for (var k = 0; k < 3; k++) { mk.Prism(dark, fw + f.Right * (-1f + k) , up, .14f, .7f, 8); } Col(fw + up * .35f, new Vector3(2.6f, .7f, .5f), f.r); var fgo = new GameObject("Firework fuse"); fgo.transform.SetParent(plife.transform, false); fgo.transform.position = fw + up * .8f + f.Fwd * .4f; plife.fireAt = fw + up * 26f; for (var k = 0; k < 3; k++) plife.bursts.Add(ArtistBits.PointLight(ctx.Root, fw + up * (24f + k * 4f) + f.Right * (-8f + k * 8f), NeonC[k], 90f, 0f));
            string[] hanabi = { "Ssss... THOOM. Red, then gold, all the way down to the sea.", "THOOM. A chrysanthemum the size of the hill. Somebody near you says \"tamaya!\"", "Three at once. For a second the whole festival is daylight." }; var hn = 0;
            Interact.Toy(fgo, Toy.Kind.Custom, "Light the fireworks", new Vector3(2.6f, 1.2f, 1f)).onUse = _ => { plife.fired = Time.time; Say(plife.transform, fw + up * 2.6f, hanabi[hn++ % hanabi.Length], 5f); };
            ParkBench(mk, g - f.Fwd * 11f + f.Right * 5f, f.Fwd); ParkBench(mk, g - f.Fwd * 11f - f.Right * 5f, f.Fwd);
            NeonBoard(mk, f.P(5.5f, 0, 18.5f), f.Fwd, "SUMMER FESTIVAL", Amber, .2f);
            Alley(ctx, mk, f.P(0, 0, 15f)); Ring(g, 16f);
            Shot(ctx, "the summer festival", f.P(-10f, 6f, 20f), fo.P(0, 2.4f, 0));
        }

        // ---------------------------------------------------------------- Cat Hill
        void CatHill(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CatAt.x, CatAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g)));
            mk.Prism(concrete, g - up * .3f, up, 9f, .4f, 14); solid.Prism(concrete, g - up * .3f, up, 9f, .3f, 14);
            // a small shrine at the back under a torii: a giant lucky cat whose paw waves, an offering box, a bell rope
            Torii(ctx, mk, G(g + f.Fwd * 4.5f), f.Fwd, .8f); var sh = g - f.Fwd * 4.5f + up * .1f; mk.Box(concrete, sh + up * .4f, new Vector3(3.4f, .8f, 2.4f), f.r); solid.Box(concrete, sh + up * .4f, new Vector3(3.4f, .8f, 2.4f), f.r);
            var white = WorldMaterials.Solid(new Color(.97f, .96f, .94f)); var cat = sh + up * .8f; mk.Blob(white, cat + up * .9f, new Vector3(.8f, .95f, .7f), prng, .03f); mk.Blob(white, cat + up * 2.1f, new Vector3(.65f, .55f, .6f), prng, .03f); foreach (var e in new[] { -1f, 1f }) { mk.Cone(white, cat + up * 2.5f + f.Right * e * .38f, .22f, .36f, 4, 45f); mk.Cone(red, cat + up * 2.52f + f.Right * e * .38f + f.Fwd * .05f, .12f, .24f, 4, 45f); mk.Blob(dark, cat + up * 2.15f + f.Right * e * .25f + f.Fwd * .56f, new Vector3(.08f, .04f, .03f), prng, 0f); }
            mk.Box(red, cat + up * 1.62f, new Vector3(1.2f, .14f, 1.1f), f.r); mk.Blob(GlowM(new Color(1f, .8f, .3f), 2.4f), cat + up * 1.5f + f.Fwd * .62f, Vector3.one * .14f, prng, 0f); mk.Blob(GlowM(new Color(1f, .8f, .3f), 1.2f), cat + up * .7f + f.Fwd * .66f, new Vector3(.3f, .42f, .08f), prng, 0f); halos.Add(cat + up * 1.5f + f.Fwd * 1f, 1.6f, new Color(1f, .8f, .3f), .5f);
            var paw = new MeshKit(); paw.Blob(white, new Vector3(0, .4f, 0), new Vector3(.2f, .45f, .2f), prng, .03f); paw.Blob(WorldMaterials.Solid(new Color(1f, .7f, .78f)), new Vector3(0, .78f, .16f), new Vector3(.1f, .08f, .04f), prng, 0f); var pgo = paw.BuildSingle("Lucky paw", plife.transform, false); pgo.transform.SetPositionAndRotation(cat + up * 1.5f - f.Right * .8f, f.r); plife.paw = pgo.transform; plife.pawRot = f.r;
            var box = sh + f.Fwd * 1.9f - up * .1f; mk.Box(woodDark, box + up * .4f, new Vector3(1.2f, .6f, .6f), f.r); for (var k = 0; k < 5; k++) mk.Box(wood, box + up * .72f + f.Right * (-.44f + k * .22f), new Vector3(.06f, .04f, .56f), f.r); Col(box + up * .4f, new Vector3(1.2f, .7f, .6f), f.r);
            var bgo = new GameObject("Shrine bell rope"); bgo.transform.SetParent(plife.transform, false); bgo.transform.position = box + up * 1.4f + f.Fwd * .5f; mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .2f)), box + up * 1.9f + f.Fwd * .2f, new Vector3(.08f, 2f, .08f), f.r); mk.Blob(GlowM(new Color(1f, .8f, .3f), 1.6f), box + up * 3f + f.Fwd * .2f, Vector3.one * .2f, prng, 0f);
            string[] luck = { "Jangle. The big cat waves. \"GREAT LUCK: someone you haven't met yet is already humming your song.\"", "Jangle. \"GOOD LUCK: you will find a coin in an old jacket. It will be the wrong currency.\"", "Jangle. \"MIDDLING LUCK: the train will be exactly on time. So will you. Nothing will come of it.\"", "Jangle. \"SMALL LUCK: a cat will choose your lap. You will not be able to move for an hour.\"" }; var ln = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Ring the bell and draw a fortune", new Vector3(1f, 1.6f, 1f)).onUse = _ => { plife.rang = Time.time; Say(plife.transform, box + up * 3.4f, luck[ln++ % luck.Length], 6.5f); };
            // a dozen cats about the place, each a little different, each to stroke; fish on a mat to carry
            Color[] furs = { new Color(.95f, .6f, .25f), new Color(.15f, .14f, .14f), new Color(.95f, .94f, .9f), new Color(.6f, .6f, .62f), new Color(.75f, .5f, .3f), new Color(.3f, .25f, .22f) };
            string[] moods = { "It purrs like a tiny amplifier.", "It looks at you, then at the sea, then goes back to sleep.", "It rolls over. This is a trap. You fall for it.", "It headbutts your hand, precisely, twice.", "It was not asleep. It was composing.", "A slow blink. That means the song is good." };
            for (var k = 0; k < 12; k++)
            {
                var a = k * 31f + 10f; var r = 3f + (k % 4) * 1.5f; var cp = G(g + Quaternion.Euler(0, a, 0) * f.Fwd * r) + up * .12f; if (Vector3.Distance(cp, sh) < 2.6f) cp += f.Right * 3f; var fur = WorldMaterials.Solid(furs[k % furs.Length]); var cm = new MeshKit(); var lying = k % 3 == 0;
                cm.Blob(fur, new Vector3(0, lying ? .1f : .16f, 0), lying ? new Vector3(.2f, .1f, .32f) : new Vector3(.17f, .17f, .26f), prng, .04f); cm.Blob(fur, new Vector3(0, lying ? .14f : .3f, lying ? .34f : .24f), Vector3.one * .11f, prng, 0f); foreach (var e in new[] { -.06f, .06f }) cm.Cone(fur, new Vector3(e, lying ? .22f : .38f, lying ? .34f : .24f), .04f, .08f, 4); cm.Blob(fur, new Vector3(.12f, .06f, -.3f), new Vector3(.04f, .04f, .16f), prng, 0f); if (k % 4 == 1) cm.Blob(WorldMaterials.Solid(new Color(.95f, .94f, .9f)), new Vector3(0, .1f, .12f), new Vector3(.1f, .1f, .1f), prng, 0f);
                var go = cm.BuildSingle("Cat", plife.transform, false); go.transform.SetPositionAndRotation(cp, Quaternion.Euler(0, k * 77f, 0)); var kk = k; var n2 = 0; Interact.Toy(go, Toy.Kind.Custom, "Stroke the cat", new Vector3(.6f, .5f, .8f)).onUse = _ => Say(plife.transform, cp + up * 1.1f, moods[(kk + n2++) % moods.Length], 4f);
            }
            var mat = G(g + f.Right * 5.5f + f.Fwd * 1f); mk.Box(WorldMaterials.Solid(new Color(.7f, .62f, .4f)), mat + up * .14f, new Vector3(1.4f, .03f, .9f), f.r); for (var k = 0; k < 3; k++) { var m = new MeshKit(); var silver = WorldMaterials.Solid(new Color(.7f, .76f, .82f), .7f); m.Blob(silver, Vector3.zero, new Vector3(.18f, .04f, .07f), prng, 0f); m.Cone(silver, new Vector3(-.17f, 0, 0), .07f, .08f, 4); var go = m.BuildSingle("Fish", plife.transform, false); go.transform.SetPositionAndRotation(mat + up * .2f + f.Right * (-.4f + k * .4f), f.r * Quaternion.Euler(0, 20f * k, 0)); var c = Interact.Carry(go, "a fish (for the cats, obviously)", .2f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.4f, .08f, .16f); bc.center = Vector3.zero; } }
            StoneLantern(ctx, mk, G(g + f.Right * 3.6f - f.Fwd * 4.5f)); StoneLantern(ctx, mk, G(g - f.Right * 3.6f - f.Fwd * 4.5f)); Cherry(ctx, mk, G(g - f.Right * 7f + f.Fwd * 1f), 5f); ParkBench(mk, g - f.Right * 5f + f.Fwd * 2.4f, f.Right);
            Folk(G(g - f.Right * 5f + f.Fwd * 2.4f) + up * .52f - f.Fwd * .45f, f.Right, Resident.Pose.Sit, new[] { "I came up to feed one cat. That was the first one. Don't count them, more turn up.", "They only sit on you if you've got a tune in your head. It's how I know I'm working.", "Ring the bell. The big one waves. It's the only one here that'll do what you ask." }, Student(), null, .52f);
            ArtistBits.PointLight(ctx.Root, g + up * 3.4f, new Color(1f, .8f, .55f), 18f, 1.6f);
            NeonBoard(mk, f.P(5.8f, 0, 10.5f), f.Fwd, "CAT HILL", Pink, .24f);
            Alley(ctx, mk, f.P(0, 0, 7f)); Ring(g, 9f);
            Shot(ctx, "cat hill", f.P(-6f, 3f, 11f), cat + up * 1.2f);
        }
    }

    /// The J-Rock places' moving parts: the pitching machine, the taiko's flash, the fireworks, the lucky cat's paw.
    public class JRockPlacesLife : MonoBehaviour
    {
        public List<Carryable> balls = new List<Carryable>(); public Vector3 launchFrom, launchDir, fireAt; public Material pitchM, taikoM; public Light taikoLight; public List<Light> bursts = new List<Light>(); public float taiko = -99f, fired = -99f, rang = -99f; public Transform paw; public Quaternion pawRot; int next;
        static readonly Color Ready = new Color(.4f, 1f, .5f), Skin = new Color(.96f, .93f, .85f);

        /// Pitches the next ball nobody is holding. False if there are none.
        public bool Pitch()
        {
            for (var k = 0; k < balls.Count; k++)
            {
                var b = balls[(next + k) % balls.Count]; if (b == null || b.Held) continue; next = (next + k + 1) % balls.Count;
                var rb = b.GetComponent<Rigidbody>(); b.transform.position = launchFrom; if (rb != null) { rb.isKinematic = false; rb.linearVelocity = launchDir * 16f; rb.angularVelocity = Vector3.right * 20f; }
                return true;
            }
            return false;
        }

        void Update()
        {
            var t = Time.time;
            if (pitchM != null) pitchM.SetColor("_EmissionColor", Ready * (.6f + .6f * Mathf.Sin(t * 3f)));
            var a = t - taiko; var hit = a < 1.2f ? Mathf.Exp(-a * 5f) : 0f; if (taikoM != null) taikoM.SetColor("_EmissionColor", Skin * (.2f + hit * 4f)); if (taikoLight != null) taikoLight.intensity = hit * 5f;
            var fa = t - fired; for (var i = 0; i < bursts.Count; i++) { var u = fa - .8f - i * .9f; bursts[i].intensity = u > 0f && u < 3f ? 9f * Mathf.Exp(-u * 1.3f) * (.8f + .2f * Mathf.Sin(u * 40f)) : 0f; }
            if (paw != null) { var ra = t - rang; var wave = ra < 5f ? Mathf.Sin(ra * 7f) * 30f * (1f - ra / 5f) : Mathf.Sin(t * 1.5f) * 6f; paw.rotation = pawRot * Quaternion.Euler(wave - 20f, 0, 0); }
        }
    }
}
