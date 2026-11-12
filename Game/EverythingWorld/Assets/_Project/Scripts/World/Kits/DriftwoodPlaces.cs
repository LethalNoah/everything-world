using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Driftwood Shore laid out on purpose (the build-land standard). The lagoon, the promenade and every artist
    /// stay where they were; five more boardwalks (built by the land's own boardwalk maker, so they match, with its
    /// lanterns) run out to the empty north-east headland, the east heath, the south-west shore and the south. At
    /// their ends, with the people of the shore: the Lookout (a coastguard hut, a telescope, signal flags); the
    /// Smokehouse (tarred net huts, fish racks, a smoker glowing); the Boatyard (a hull in frame on a slip, a dinghy
    /// to sit in, a bell); the Beachcomber's (a shack hung with floats, sea glass sorted by colour to take, driftwood
    /// creatures); and the Sauna (a little hot hut and a steaming barrel tub to sit in, out of the wind).
    public partial class DriftwoodKit
    {
        static readonly Vector2 LookoutAt = new Vector2(8810, 7446), SmokeAt = new Vector2(8952, 7100), YardAt = new Vector2(8902, 6902), CombAt = new Vector2(8168, 6718), SaunaAt = new Vector2(8468, 6658);
        static readonly Vector2[] NorthEastWalk = { new Vector2(8760, 7262), new Vector2(8788, 7345), new Vector2(8806, 7422) };
        static readonly Vector2[] EastWalk = { new Vector2(8790, 7020), new Vector2(8832, 7042), new Vector2(8895, 7078), new Vector2(8934, 7094) };
        static readonly Vector2[] YardWalk = { new Vector2(8832, 7042), new Vector2(8868, 6972), new Vector2(8894, 6922) };
        static readonly Vector2[] SouthWestWalk = { new Vector2(8342, 6880), new Vector2(8282, 6822), new Vector2(8224, 6764), new Vector2(8188, 6732) };
        static readonly Vector2[] SouthWalk = { new Vector2(8542, 6760), new Vector2(8502, 6702), new Vector2(8480, 6676) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (LookoutAt, 9f), (SmokeAt, 12f), (YardAt, 13f), (CombAt, 11f), (SaunaAt, 9f) };

        DriftwoodPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Oilskins = { new Color(.85f, .7f, .3f), new Color(.35f, .42f, .5f), new Color(.75f, .72f, .68f), new Color(.55f, .35f, .32f), new Color(.3f, .4f, .38f), new Color(.2f, .25f, .35f) };
        static readonly Color[] Sea = { new Color(.25f, .28f, .34f), new Color(.4f, .38f, .34f), new Color(.2f, .25f, .35f) };
        static readonly Residents.Hat[] WoolHats = { Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Bucket };
        static readonly string[] ShoreTalk =
        {
            "Blew itself out about four this morning. It always looks sorry afterwards.", "You'll find something on the tide line. Everybody does. It isn't always what they lost.", "The light's trying. Give it a song or two.",
            "Cold hands, warm flask. That's the whole trick of this coast.", "Storm takes things and storm brings things. I've stopped keeping a tally.",
        };

        Residents.Look Shorer(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Oilskins, Sea, WoolHats, Oilskins); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Shorer(), pose, prng, lines ?? ShoreTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

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

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeShore(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) PlaceLevel(data, index, at, r, r + 12f);
        }

        /// The new boardwalks, added to the land's own list before it builds them (so they get its deck, posts and lanterns).
        void ShoreWalks(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1971);
            plife = new GameObject("Driftwood places life").AddComponent<DriftwoodPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            foreach (var way in new[] { NorthEastWalk, EastWalk, YardWalk, SouthWestWalk, SouthWalk })
            {
                Vector2 near = way[0]; var bd = 220f * 220f; foreach (var (p, _) in walkPoints) { var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
                var full = new List<Vector2>(); var dir0 = (way[0] - near); if (dir0.magnitude > 3f) full.Add(near + dir0.normalized * (WalkW / 2f + .3f)); full.AddRange(way);
                var pts = PlaceTools.Smooth(ctx, full, false, 3f);
                for (var i = 0; i < pts.Count; i++) foreach (var a in ctx.Artists) { var r = Foot(a) + 6f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                if (pts.Count >= 2) AddWalk(pts, 0);
            }
        }

        void Plank(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.2f, float size = .15f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, -3f);
            mk.Box(driftDark, g + up * .8f, new Vector3(.12f, 1.6f, .12f), Quaternion.identity); mk.Box(drift, g + up * 1.5f, new Vector3(w, .5f, .07f), rot); Col(g + up * .9f, new Vector3(w, 1.8f, .14f), rot);
            ArtistBits.Label(rootT, text, g + up * 1.5f + face * .045f, face, size, new Color(.2f, .2f, .22f), TextAnchor.MiddleCenter);
        }

        void DriftBench(MeshKit mk, MeshKit solid, Vector3 at, Vector3 look)
        {
            at = G(at); look = Vector3.ProjectOnPlane(look, Vector3.up).normalized; var rot = Quaternion.LookRotation(look); var side = Vector3.Cross(Vector3.up, look);
            mk.Box(drift, at + Vector3.up * .46f, new Vector3(1.8f, .08f, .45f), rot); mk.Box(drift, at + Vector3.up * .8f - look * .22f, new Vector3(1.8f, .35f, .06f), rot * Quaternion.Euler(-10f, 0, 0)); foreach (var s in new[] { -1f, 1f }) mk.Box(driftDark, at + side * s * .75f + Vector3.up * .23f, new Vector3(.1f, .46f, .42f), rot);
            solid.Box(drift, at + Vector3.up * .5f, new Vector3(1.8f, 1f, .5f), rot); Interact.Bench(plife.transform, at + Vector3.up * .52f - side * .5f, at + Vector3.up * .52f + side * .5f, look);
        }

        Carryable Thing(MeshKit m, string name, string thing, Vector3 at, Quaternion rot, float mass, Vector3 size, float lowest)
        {
            var go = m.BuildSingle(name, plife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, thing, mass, lowest); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); } return c;
        }

        Vector3 NearestWalk(Vector3 to) { var best = to; var bd = float.MaxValue; foreach (var (p, _) in walkPoints) { var d = (p.x - to.x) * (p.x - to.x) + (p.z - to.z) * (p.z - to.z); if (d < bd) { bd = d; best = p; } } return best; }

        void Lantern(MeshKit mk, Vector3 at, Vector3 arm) { var lamp = ArtistBits.LanternPost(mk, rootT, G(at), arm, driftDark, iron, Ember, 2.2f); halos.Add(lamp.transform.position, 1.3f, Ember, .9f); }

        void ShorePlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Lookout(ctx, mk, solid); Smokehouse(ctx, mk, solid); Boatyard(ctx, mk, solid); Beachcomber(ctx, mk, solid); Sauna(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Lookout
        void Lookout(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(LookoutAt.x, LookoutAt.y); var sea = SeaFrom(ctx.Data, g, out _, 300f); var f = new PlaceFrame(g, FaceTo(g, NearestWalk(g))); var white = WorldMaterials.Solid(Pearl);
            // a small white coastguard hut on a stone base, a window to the sea lit warm, a flag mast with signal flags
            var hc = G(g - f.Fwd * 2.5f); mk.Box(stone, hc + up * .1f, new Vector3(4.6f, .8f, 4f), f.r); mk.Box(white, hc + up * 1.7f, new Vector3(4f, 2.6f, 3.4f), f.r); solid.Box(white, hc + up * 1.3f, new Vector3(4.2f, 3.4f, 3.6f), f.r); mk.GableRoof(WorldMaterials.Solid(new Color(.3f, .34f, .4f)), hc + up * 3f, new Vector3(4.4f, 0, 3.8f), 1.1f, f.r);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .4f, .5f)), hc + up * 1.5f + f.Fwd * 1.72f - f.Right * 1f, new Vector3(.9f, 2f, .04f), f.r); mk.Box(WorldMaterials.Glow(Bulb, 1.5f), hc + up * 1.9f + f.Fwd * 1.72f + f.Right * .8f, new Vector3(1.3f, .9f, .04f), f.r); halos.Add(hc + up * 1.9f + f.Fwd * 2.1f + f.Right * .8f, 1.6f, Bulb, .5f); ArtistBits.PointLight(ctx.Root, hc + up * 2f + f.Fwd * 3f, Bulb, 10f, 1.4f);
            var mp = G(g + f.Right * 4.5f - f.Fwd * 1f); mk.Prism(white, mp, up, .09f, 8f, 6); ArtistBits.PostCollider(ctx.Root, mp, 8f, .1f); mk.Box(white, mp + up * 6.2f, new Vector3(2.6f, .07f, .07f), f.r);
            Color[] flags = { new Color(.8f, .2f, .2f), new Color(.95f, .85f, .3f), new Color(.25f, .4f, .75f), new Color(.95f, .95f, .95f) }; for (var k = 0; k < 4; k++) soft.Box(WorldMaterials.Solid(flags[k]), mp + up * (7.4f - k * .75f) - sea * .45f, new Vector3(.03f, .55f, .8f), Quaternion.LookRotation(sea));
            // the telescope on the seaward edge, a bench, a logbook to read
            var tp = G(g + sea * 4.5f); mk.Prism(iron, tp, up, .06f, 1.3f, 6); mk.Prism(WorldMaterials.Solid(new Color(.8f, .65f, .3f), .8f), tp + up * 1.35f - sea * .5f, (sea + up * .05f).normalized, .08f, 1f, 8, .75f); ArtistBits.PostCollider(ctx.Root, tp, 1.4f, .1f);
            var tgo = new GameObject("Lookout telescope"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = tp + up * 1.3f - sea * .4f; string[] see = { "A grey sea, still heaving. One gull standing on nothing.", "Far out, a sail, or the idea of one.", "The beacon at the end of the jetty, turning, turning.", "For a second, a spout. Then just water again." }; var sn = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Look through the telescope", new Vector3(.8f, .8f, .8f)).onUse = _ => Say(plife.transform, tp + up * 2.3f, see[sn++ % see.Length], 5f);
            DriftBench(mk, solid, g + sea * 2f - Vector3.Cross(up, sea) * 2.6f, sea);
            var lb = G(g + f.Fwd * .2f + f.Right * 1.9f); mk.Box(driftDark, lb + up * .5f, new Vector3(.7f, 1f, .5f), f.r); mk.Box(WorldMaterials.Solid(new Color(.86f, .84f, .78f)), lb + up * 1.03f, new Vector3(.55f, .04f, .4f), f.r * Quaternion.Euler(-12f, 0, 0)); Col(lb + up * .5f, new Vector3(.7f, 1f, .5f), f.r);
            var lgo = new GameObject("Logbook"); lgo.transform.SetParent(plife.transform, false); lgo.transform.position = lb + up * 1.1f; string[] log = { "\"04:10. Wind dropping. Nothing lost that I can see.\"", "\"06:30. One rowing boat adrift, pink lantern still lit. Recovered.\"", "\"07:15. Rainbow reported, seaward. Could not confirm; was looking at it.\"" }; var ln = 0;
            Interact.Toy(lgo, Toy.Kind.Custom, "Read the log", new Vector3(.8f, .6f, .6f)).onUse = _ => Say(plife.transform, lb + up * 2.1f, log[ln++ % log.Length], 6f);
            Folk(G(g + sea * 3.2f + Vector3.Cross(up, sea) * 1.6f), sea, Resident.Pose.Stand, new[] { "I count them out and I count them back. Last night I mostly counted waves.", "Have a look through the glass. You'll see nothing. It's very restful.", "Flags say \"all's well\". I'd not have hung them otherwise." }, Shorer(Residents.Tool.Mug));
            Plank(mk, f.P(3.6f, 0, 5.5f), f.Fwd, "THE LOOKOUT", 2f);
            Shot(ctx, "the lookout", f.P(-5f, 2.6f, 9f), hc + up * 2f);
        }

        // ---------------------------------------------------------------- the Smokehouse
        void Smokehouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SmokeAt.x, SmokeAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestWalk(g))); var tar = WorldMaterials.Solid(new Color(.14f, .13f, .14f));
            // three tall black net huts in a row, narrow and steep-roofed; the middle one is the smoker, glowing at its door
            for (var k = 0; k < 3; k++)
            {
                var c = G(f.P(-5f + k * 5f, 0, -4f)); mk.Box(tar, c + up * 2.6f, new Vector3(2.6f, 5.2f, 3f), f.r); solid.Box(tar, c + up * 2.6f, new Vector3(2.6f, 5.2f, 3f), f.r); mk.GableRoof(tar, c + up * 5.2f, new Vector3(2.9f, 0, 3.3f), 1.5f, f.r);
                if (k == 1) { mk.Box(WorldMaterials.Glow(Ember, 2f), c + up * 1f + f.Fwd * 1.52f, new Vector3(1f, 1.8f, .04f), f.r); halos.Add(c + up * 1.2f + f.Fwd * 2f, 2.2f, Ember, .7f); ArtistBits.PointLight(ctx.Root, c + up * 1.4f + f.Fwd * 2.6f, Ember, 13f, 1.8f); }
                else mk.Box(driftDark, c + up * 1f + f.Fwd * 1.52f, new Vector3(1f, 1.9f, .04f), f.r);
            }
            // fish drying on racks, a table, crates to sit on, a barrel
            for (var k = 0; k < 2; k++) { var rp = G(f.P(-6f + k * 9.5f, 0, 2.5f)); foreach (var e in new[] { -1.3f, 1.3f }) { mk.Box(driftDark, rp + f.Fwd * e + up * .9f, new Vector3(.1f, 1.8f, .1f), f.r); ArtistBits.PostCollider(ctx.Root, rp + f.Fwd * e, 1.8f, .1f); } mk.Box(driftDark, rp + up * 1.75f, new Vector3(.08f, .08f, 2.8f), f.r); for (var s = 0; s < 6; s++) soft.Box(WorldMaterials.Solid(new Color(.75f, .6f, .45f)), rp + up * 1.4f + f.Fwd * (-1.1f + s * .44f), new Vector3(.06f, .6f, .2f), f.r); }
            var tb = G(f.P(0, 0, 1.5f)); mk.Box(drift, tb + up * .8f, new Vector3(2.4f, .08f, .9f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(driftDark, tb + up * .4f + f.Right * e, new Vector3(.1f, .8f, .8f), f.r); Col(tb + up * .42f, new Vector3(2.4f, .85f, .9f), f.r);
            for (var k = 0; k < 3; k++) { var fm = new MeshKit(); var silver = WorldMaterials.Solid(k == 1 ? new Color(.8f, .6f, .4f) : new Color(.7f, .74f, .78f), .7f); fm.Blob(silver, Vector3.zero, new Vector3(.22f, .05f, .08f), prng, 0f); fm.Cone(silver, new Vector3(-.2f, 0, 0), .08f, .1f, 4); Thing(fm, "Fish", k == 1 ? "a smoked kipper" : "a silver herring", tb + up * .9f + f.Right * (-.7f + k * .7f), f.r * Quaternion.Euler(0, k * 30f, 0), .4f, new Vector3(.5f, .1f, .2f), g.y - 10f); }
            for (var k = 0; k < 3; k++) { var cp = G(f.P(-2.2f + k * 2.2f, 0, 3.4f)); mk.Box(driftDark, cp + up * .25f, new Vector3(.7f, .5f, .6f), f.r * Quaternion.Euler(0, k * 13f, 0)); Col(cp + up * .25f, new Vector3(.7f, .5f, .6f), f.r); if (k != 1) Interact.Seat(plife.transform, cp + up * .55f, -f.Fwd, "Sit on the crate"); }
            Folk(G(f.P(0, 0, 3.4f)) + up * .52f, -f.Fwd, Resident.Pose.Sit, new[] { "Oak chips. Twelve hours. You can't hurry a kipper and you shouldn't want to.", "Nets want mending after a night like that. So do I.", "Take a fish. It's that or the gulls take it." }, Shorer(Residents.Tool.Mug), null, .52f);
            // a net hung between two poles, someone mending it
            var n0 = G(f.P(6f, 0, 5f)); var n1 = G(f.P(9.5f, 0, 1f)); foreach (var p in new[] { n0, n1 }) { mk.Box(driftDark, p + up * 1.3f, new Vector3(.1f, 2.6f, .1f), f.r); ArtistBits.PostCollider(ctx.Root, p, 2.6f, .1f); } var nd = (n1 - n0); nd.y = 0; var nq = Quaternion.LookRotation(nd.normalized);
            for (var s = 0; s < 7; s++) soft.Box(rope, Vector3.Lerp(n0, n1, s / 6f) + up * 1.4f, new Vector3(.02f, 2f, .02f), nq); for (var s = 0; s < 4; s++) soft.Box(rope, (n0 + n1) / 2f + up * (.6f + s * .55f), new Vector3(.02f, .02f, nd.magnitude), nq);
            var mid = (n0 + n1) / 2f; var back = Vector3.Cross(up, nd.normalized); Folk(G(mid + back * .9f), -back, Resident.Pose.Work, new[] { "One knot, then the next knot. That's all a net is. That's all most things are.", "The storm put a hole in it you could sail through." }, Shorer(Residents.Tool.Rod)).workRate = .9f;
            Lantern(mk, f.P(-8.5f, 0, 5f), f.Right);
            Plank(mk, f.P(4.5f, 0, 8.5f), f.Fwd, "SMOKEHOUSE\nkippers when ready", 2.4f, .13f);
            Shot(ctx, "the smokehouse", f.P(-7f, 3f, 12f), g + up * 2f - f.Fwd * 2f);
        }

        // ---------------------------------------------------------------- the Boatyard
        void Boatyard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(YardAt.x, YardAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestWalk(g)));
            // a hull in frame on a slip: keel, stem and stern posts, pale ribs, half the planking on
            const float L = 11f; var kc = g - f.Fwd * 2f; foreach (var e in new[] { -3.5f, 0f, 3.5f }) { mk.Box(driftDark, kc + f.Right * e + up * .35f, new Vector3(.4f, .7f, 3.4f), f.r); Col(kc + f.Right * e + up * .35f, new Vector3(.4f, .7f, 3.4f), f.r); }
            mk.Box(driftDark, kc + up * .85f, new Vector3(L, .3f, .3f), f.r); mk.Prism(driftDark, kc + f.Right * (L / 2f) + up * .8f, (f.Right * .45f + up).normalized, .16f, 2.6f, 5); mk.Prism(driftDark, kc - f.Right * (L / 2f) + up * .8f, (-f.Right * .2f + up).normalized, .16f, 2.1f, 5);
            for (var k = 0; k < 11; k++)
            {
                var t = k / 10f; var x = -L / 2f + .5f + t * (L - 1f); var w = Mathf.Sin(Mathf.Lerp(.25f, 1f, 1f - Mathf.Abs(t - .5f) * 2f) * Mathf.PI * .5f) * 1.7f; var o = kc + f.Right * x + up * .95f;
                foreach (var e in new[] { -1f, 1f }) { var a = o; var b = o + f.Fwd * e * w * .75f + up * .7f; var c = o + f.Fwd * e * w + up * 2f; mk.Prism(drift, a, (b - a).normalized, .08f, Vector3.Distance(a, b), 5); mk.Prism(drift, b, (c - b).normalized, .07f, Vector3.Distance(b, c), 5); if (k < 6 && e < 0) { var p0 = (b + c) / 2f; mk.Box(planks, p0, new Vector3(1.05f, 1.45f, .06f), f.r * Quaternion.Euler(e * -28f, 0, 0)); } }
            }
            Col(kc + up * 1.6f, new Vector3(L, 2.4f, 3.4f), f.r);
            // a workbench with tools, wood shavings, a steaming kettle; the boatbuilder
            var wb = G(f.P(-6f, 0, 3.5f)); mk.Box(planks, wb + up * .9f, new Vector3(2.6f, .1f, .9f), f.r); foreach (var e in new[] { -1.1f, 1.1f }) mk.Box(driftDark, wb + up * .45f + f.Right * e, new Vector3(.12f, .9f, .8f), f.r); Col(wb + up * .48f, new Vector3(2.6f, .95f, .9f), f.r);
            for (var k = 0; k < 3; k++) { var tm = new MeshKit(); string thing; if (k == 0) { tm.Box(driftDark, new Vector3(0, .03f, 0), new Vector3(.06f, .06f, .34f), Quaternion.identity); tm.Box(iron, new Vector3(0, .05f, .2f), new Vector3(.2f, .1f, .1f), Quaternion.identity); thing = "a mallet"; } else if (k == 1) { tm.Box(planks, new Vector3(0, .05f, 0), new Vector3(.12f, .1f, .34f), Quaternion.identity); tm.Box(iron, new Vector3(0, .1f, 0), new Vector3(.04f, .08f, .06f), Quaternion.identity); thing = "a wood plane"; } else { tm.Blob(WorldMaterials.Solid(new Color(.9f, .82f, .65f)), new Vector3(0, .05f, 0), new Vector3(.14f, .05f, .14f), prng, .5f); thing = "a curl of wood shavings"; } Thing(tm, "Yard tool", thing, wb + up * .96f + f.Right * (-.8f + k * .8f), f.r, k == 2 ? .1f : .9f, new Vector3(.3f, .14f, .4f), g.y - 10f); }
            for (var k = 0; k < 12; k++) soft.Blob(WorldMaterials.Solid(new Color(.9f, .82f, .65f)), G(g + f.Fwd * (.6f + (k % 4) * .5f) + f.Right * (-3f + k * .55f)) + up * .05f, new Vector3(.2f, .04f, .14f), prng, .5f);
            Folk(G(f.P(-2f, 0, 1.2f)), -f.Fwd, Resident.Pose.Work, new[] { "She'll be a year yet. They always are. I've been saying a year for three.", "Larch on oak. She'll outlast me and the next fellow.", "Ring the bell when you launch one. I ring it Fridays anyway, to keep its hand in." }, Shorer(Residents.Tool.Spatula, true)).workRate = 1f;
            // the finished dinghy on the grass: sit in it; and the launching bell
            var dp = G(f.P(5.5f, 0, 4f)); var dq = Quaternion.LookRotation(Quaternion.Euler(0, 25f, 0) * f.Right); var hull = WorldMaterials.Solid(new Color(.6f, .68f, .76f)); mk.Box(hull, dp + up * .35f, new Vector3(1.4f, .5f, 3.4f), dq); mk.Prism(hull, dp + up * .35f + dq * Vector3.forward * 1.7f, dq * Vector3.forward, .5f, .9f, 4, .1f, 45f); mk.Box(planks, dp + up * .5f, new Vector3(1.3f, .06f, .3f), dq); mk.Box(planks, dp + up * .5f - dq * Vector3.forward * 1f, new Vector3(1.3f, .06f, .3f), dq); solid.Box(hull, dp + up * .25f, new Vector3(1.4f, .5f, 3.6f), dq);
            Interact.Seat(plife.transform, dp + up * .6f, dq * Vector3.forward, "Sit in the dinghy"); Interact.Seat(plife.transform, dp + up * .6f - dq * Vector3.forward * 1f, dq * Vector3.forward, "Sit in the dinghy");
            var bp = G(f.P(8f, 0, -1f)); mk.Box(driftDark, bp + up * 1.3f, new Vector3(.14f, 2.6f, .14f), f.r); mk.Box(driftDark, bp + up * 2.5f + f.Right * .35f, new Vector3(.8f, .1f, .1f), f.r); ArtistBits.PostCollider(ctx.Root, bp, 2.6f, .1f);
            var bell = new MeshKit(); bell.Prism(WorldMaterials.Solid(new Color(.8f, .65f, .3f), .85f), new Vector3(0, -.45f, 0), Vector3.up, .22f, .36f, 10, .5f); var bgo = bell.BuildSingle("Yard bell", plife.transform, false); bgo.transform.SetPositionAndRotation(bp + up * 2.45f + f.Right * .6f, f.r); var bs = bgo.AddComponent<Sway>(); bs.degrees = 32f; bs.speed = 7f; bs.decay = .9f; bs.seconds = 5f;
            var bt = Interact.Toy(bgo, Toy.Kind.Custom, "Ring the launching bell", new Vector3(.8f, 1.2f, .8f)); bgo.GetComponent<BoxCollider>().center = new Vector3(0, -.5f, 0); bt.onUse = _ => { bs.Push(); Say(plife.transform, bp + up * 3.3f, "It carries a long way over the heath.", 3.5f); };
            Lantern(mk, f.P(-8f, 0, 6f), f.Right); ArtistBits.PointLight(ctx.Root, kc + up * 3.4f + f.Fwd * 2f, Bulb, 16f, 1.2f);
            Plank(mk, f.P(4f, 0, 9f), f.Fwd, "BOATYARD", 1.8f);
            Shot(ctx, "the boatyard", f.P(-8f, 3.2f, 12f), kc + up * 1.6f);
        }

        // ---------------------------------------------------------------- the Beachcomber's
        void Beachcomber(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CombAt.x, CombAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestWalk(g)));
            // a shack of driftwood and tin, hung all over with floats and sea glass
            var sc = G(g - f.Fwd * 3.5f); mk.Box(drift, sc + up * 1.3f, new Vector3(4.4f, 2.6f, 3f), f.r); solid.Box(drift, sc + up * 1.3f, new Vector3(4.4f, 2.6f, 3f), f.r); mk.Box(WorldMaterials.Solid(new Color(.45f, .48f, .52f), .5f), sc + up * 2.75f + f.Fwd * .3f, new Vector3(5f, .1f, 4f), f.r * Quaternion.Euler(8f, 0, 0));
            mk.Box(driftDark, sc + up * 1.05f + f.Fwd * 1.52f - f.Right * 1.1f, new Vector3(.9f, 2f, .04f), f.r); mk.Box(WorldMaterials.Glow(Bulb, 1.4f), sc + up * 1.6f + f.Fwd * 1.52f + f.Right * .9f, new Vector3(1.2f, .9f, .04f), f.r);
            for (var k = 0; k < 9; k++) { var c = SeaGlass[k % SeaGlass.Length]; var p = sc + up * (2.3f - (k % 3) * .35f) + f.Fwd * 1.75f + f.Right * (-2f + k * .5f); soft.Box(rope, p + up * .25f, new Vector3(.015f, .5f, .015f), f.r); soft.Blob(WorldMaterials.Glow(c, 1.2f), p, Vector3.one * (.11f + (k % 2) * .05f), prng, 0f); if (k % 3 == 0) halos.Add(p, .7f, c, .5f); }
            ArtistBits.PointLight(ctx.Root, sc + up * 2f + f.Fwd * 3f, new Color(.7f, 1f, .9f), 11f, 1.3f);
            // the sorting table: sea glass in rows by colour, every piece yours to take
            var tb = G(f.P(2.2f, 0, .5f)); mk.Box(drift, tb + up * .8f, new Vector3(2.6f, .08f, 1f), f.r); foreach (var e in new[] { -1.1f, 1.1f }) mk.Box(driftDark, tb + up * .4f + f.Right * e, new Vector3(.1f, .8f, .9f), f.r); Col(tb + up * .42f, new Vector3(2.6f, .85f, 1f), f.r);
            string[] names = { "aqua", "green", "amber", "frosted white", "cobalt" };
            for (var k = 0; k < 10; k++) { var gm = new MeshKit(); var ci = k % 5; gm.Blob(WorldMaterials.Glow(SeaGlass[ci], .9f), new Vector3(0, .03f, 0), new Vector3(.07f, .03f, .05f), prng, .3f); Thing(gm, "Sea glass", "a piece of " + names[ci] + " sea glass", tb + up * .85f + f.Right * (-1f + ci * .5f) + f.Fwd * (-.2f + (k / 5) * .4f), Quaternion.Euler(0, k * 40f, 0), .1f, new Vector3(.16f, .08f, .12f), g.y - 10f); }
            halos.Add(tb + up * 1f, 1.6f, new Color(.6f, 1f, .9f), .35f);
            // driftwood creatures: a horse, a heron, a seated figure looking out to sea
            var hp = G(f.P(-4.5f, 0, 2.5f)); var hq = Quaternion.LookRotation(Quaternion.Euler(0, 30f, 0) * f.Right); var hf = hq * Vector3.forward; var hs = hq * Vector3.right;
            foreach (var x in new[] { -.25f, .25f }) foreach (var z in new[] { -.8f, .8f }) mk.Prism(drift, hp + hs * x + hf * z, (up + hf * (z > 0 ? .1f : -.1f)).normalized, .07f, 1.3f, 5); mk.Prism(drift, hp + up * 1.35f - hf * 1f, hf, .28f, 2f, 6); mk.Prism(drift, hp + up * 1.45f + hf * .9f, (hf + up * 1.2f).normalized, .16f, 1.1f, 5); mk.Prism(drift, hp + up * 2.3f + hf * 1.55f, (hf - up * .5f).normalized, .13f, .7f, 5); for (var k = 0; k < 5; k++) soft.Prism(driftDark, hp + up * 1.5f - hf * 1f, (-hf + up * (.2f - k * .25f) + hs * (k - 2) * .12f).normalized, .02f, .9f, 3);
            Col(hp + up * 1.1f, new Vector3(.8f, 2.2f, 2.6f), hq);
            var he = G(f.P(-1.5f, 0, 5f)); mk.Prism(drift, he, up, .03f, 1.1f, 4); mk.Prism(drift, he + f.Right * .12f, up, .03f, 1.1f, 4); mk.Blob(drift, he + up * 1.3f + f.Right * .06f, new Vector3(.2f, .3f, .32f), prng, .1f); mk.Prism(drift, he + up * 1.45f + f.Fwd * .2f, (up + f.Fwd * .3f).normalized, .04f, .7f, 4); mk.Prism(driftDark, he + up * 2.1f + f.Fwd * .4f, f.Fwd, .03f, .45f, 4); ArtistBits.PostCollider(ctx.Root, he, 2.1f, .2f);
            Folk(G(f.P(.8f, 0, 1.6f)), f.Fwd, Resident.Pose.Stand, new[] { "Forty years of walking this tide line. I've found everything but what I was looking for, and I like it better.", "Green's bottles. Amber's medicine. Blue's rare; blue's poison bottles and love letters.", "Take a piece. One. It's better if you only take one." }, Shorer(Residents.Tool.None, false));
            DriftBench(mk, solid, f.P(5.5f, 0, 4.5f), SeaFrom(ctx.Data, g, out _, 300f)); Lantern(mk, f.P(-6.5f, 0, -1f), f.Right);
            Plank(mk, f.P(5f, 0, 8f), f.Fwd, "THE BEACHCOMBER'S\nfound, not for sale", 2.6f, .13f);
            Shot(ctx, "the beachcomber's", f.P(-6f, 2.6f, 10f), g + up * 1.4f - f.Fwd * 1f);
        }

        // ---------------------------------------------------------------- the Sauna
        void Sauna(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SaunaAt.x, SaunaAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestWalk(g))); var cedar = WorldMaterials.Solid(new Color(.55f, .42f, .32f));
            // a small cedar hut with a stovepipe, a deck in front, a warm slit window and light under the door
            var hc = G(g - f.Fwd * 3f); mk.Box(planks, g + up * .12f - f.Fwd * .5f, new Vector3(8f, .24f, 8f), f.r); solid.Box(planks, g + up * .02f - f.Fwd * .5f, new Vector3(8f, .44f, 8f), f.r);
            mk.Box(cedar, hc + up * 1.6f, new Vector3(3.6f, 2.7f, 2.8f), f.r); solid.Box(cedar, hc + up * 1.6f, new Vector3(3.6f, 2.7f, 2.8f), f.r); mk.GableRoof(WorldMaterials.Solid(new Color(.25f, .25f, .28f)), hc + up * 2.95f, new Vector3(4f, 0, 3.2f), 1f, f.r); mk.Prism(iron, hc + up * 3.2f + f.Right * 1f, up, .1f, 1.5f, 6);
            mk.Box(driftDark, hc + up * 1.3f + f.Fwd * 1.42f - f.Right * .7f, new Vector3(.8f, 1.9f, .04f), f.r); mk.Box(WorldMaterials.Glow(Ember, 2.2f), hc + up * .36f + f.Fwd * 1.43f - f.Right * .7f, new Vector3(.8f, .05f, .03f), f.r); mk.Box(WorldMaterials.Glow(Ember, 1.6f), hc + up * 1.9f + f.Fwd * 1.42f + f.Right * .8f, new Vector3(1.1f, .35f, .04f), f.r); halos.Add(hc + up * 1.6f + f.Fwd * 1.9f, 1.8f, Ember, .6f); ArtistBits.PointLight(ctx.Root, hc + up * 1.6f + f.Fwd * 2.4f, Ember, 10f, 1.6f);
            // the barrel tub: staves and hoops, steaming; sit in it
            var tc = G(f.P(2f, 0, 1f)) + up * .24f; mk.Prism(cedar, tc, up, 1.3f, 1.1f, 14); mk.Prism(iron, tc + up * .25f, up, 1.33f, .06f, 14); mk.Prism(iron, tc + up * .85f, up, 1.33f, .06f, 14); var hot = new Material(WorldMaterials.Glow(new Color(.55f, .75f, .8f), .35f)) { name = "Hot tub water" }; mk.Prism(hot, tc + up * 1f, up, 1.18f, .04f, 14);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; Col(tc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.25f + up * .55f, new Vector3(.2f, 1.1f, .85f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            mk.Box(planks, tc + f.Fwd * 1.7f + up * .15f, new Vector3(1f, .3f, .5f), f.r); mk.Box(planks, tc + f.Fwd * 1.45f + up * .5f, new Vector3(1f, .3f, .4f), f.r); solid.Box(planks, tc + f.Fwd * 1.7f + up * .15f, new Vector3(1f, .3f, .5f), f.r); solid.Box(planks, tc + f.Fwd * 1.45f + up * .5f, new Vector3(1f, .3f, .4f), f.r);
            foreach (var e in new[] { -1f, 1f }) Interact.Seat(plife.transform, tc + f.Right * e * .6f + up * .62f, -f.Right * e, "Sit in the hot tub");
            for (var k = 0; k < 8; k++) { var sm = new MeshKit(); sm.Blob(WorldMaterials.Glow(new Color(.9f, .92f, .95f), .25f), Vector3.zero, Vector3.one * (.22f + (k % 3) * .08f), prng, .1f); var go = sm.BuildSingle("Soft steam", plife.transform, false); var p = tc + up * 1.1f + new Vector3(Mathf.Cos(k * 2.1f), 0, Mathf.Sin(k * 2.1f)) * .6f; go.transform.position = p; plife.steam.Add((go.transform, p, k * .61f)); }
            // a bench along the hut wall, towels on pegs, a bucket and ladle, a stack of logs
            DriftBench(mk, solid, f.P(-2.2f, 0, -1.2f), f.Fwd);
            for (var k = 0; k < 3; k++) soft.Box(WorldMaterials.Solid(new[] { Rose, Pearl, new Color(.6f, .68f, .76f) }[k]), g + f.r * new Vector3(-3.6f, 1.2f, -2.4f + k * .7f), new Vector3(.08f, .9f, .5f), f.r);
            mk.Box(driftDark, g + f.r * new Vector3(-3.7f, 1.7f, -1.7f), new Vector3(.08f, .08f, 2.2f), f.r); foreach (var e in new[] { -2.7f, -.7f }) { mk.Box(driftDark, g + f.r * new Vector3(-3.7f, .95f, e), new Vector3(.1f, 1.7f, .1f), f.r); ArtistBits.PostCollider(ctx.Root, g + f.r * new Vector3(-3.7f, .24f, e), 1.7f, .1f); }
            for (var k = 0; k < 6; k++) mk.Prism(drift, g + f.r * new Vector3(3.2f, .4f + (k / 3) * .3f, -3f + (k % 3) * .32f) - f.Right * .5f, f.Right, .15f, 1f, 6); Col(g + f.r * new Vector3(3.2f, .55f, -2.7f), new Vector3(1f, .7f, 1f), f.r);
            var bk = new MeshKit(); bk.Prism(cedar, Vector3.zero, Vector3.up, .16f, .24f, 8, 1.15f); bk.Box(driftDark, new Vector3(.1f, .3f, 0), new Vector3(.03f, .4f, .03f), Quaternion.Euler(0, 0, -20f)); Thing(bk, "Sauna bucket", "the bucket and ladle", g + f.r * new Vector3(.2f, .26f, -1.2f), f.r, .8f, new Vector3(.4f, .4f, .4f), g.y - 10f);
            var robe = Shorer(Residents.Tool.Mug); robe.top = Pearl; robe.bottom = Pearl; robe.hat = Residents.Hat.Beanie;
            Folk(G(f.P(-2.2f, 0, -1.2f)) + up * .52f + f.Right * .45f, f.Fwd, Resident.Pose.Sit, new[] { "In the sea, then in here, then in the sea. I can't recommend the first part. I do it anyway.", "Ten minutes in the tub and the storm's somebody else's problem.", "Throw a ladle on the stones if you go in. Just the one." }, robe, null, .52f);
            Plank(mk, f.P(4.2f, 0, 6f), f.Fwd, "SAUNA\nsea first, then here", 2.2f, .13f);
            Shot(ctx, "the sauna", f.P(-5f, 2.6f, 9f), g + up * 1.4f - f.Fwd * 1f);
        }
    }

    /// The Driftwood places' moving part: steam rising off the hot tub.
    public class DriftwoodPlacesLife : MonoBehaviour
    {
        public List<(Transform t, Vector3 at, float phase)> steam = new List<(Transform, Vector3, float)>();
        void Update()
        {
            var t = Time.time;
            foreach (var (tr, at, ph) in steam) { var u = Mathf.Repeat(t * .16f + ph, 1f); tr.position = at + Vector3.up * (u * 2.6f) + new Vector3(Mathf.Sin(t * .6f + ph * 4f), 0, Mathf.Cos(t * .5f + ph * 3f)) * (.15f + u * .5f) + Vector3.right * u * .8f; tr.localScale = Vector3.one * (Mathf.Sin(u * Mathf.PI) * (1f + u)); }
        }
    }
}
