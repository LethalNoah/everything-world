using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Hair Metal Strip laid out on purpose (the build-land standard). The Strip and every venue stay where they
    /// were; three more neon-lit roads leave it for the empty desert: the Loop (north off the west end, along the high
    /// ground and back down to the Club: somewhere to cruise), the Desert Highway west into the mesas, and the Canyon
    /// Road south down the tail. Along them, with the people of the Strip: the Motel (a pink court round a pool, a TV
    /// to throw in it); Tease (a salon with dryer chairs and a wall of hairspray); the Chapel of Love (a neon heart, a
    /// bell, a bouquet to throw); the Video Shoot (a wind machine, a camera crane, a director's chair, a band miming on
    /// a mesa); the Shred Shed (pointy guitars to take off the wall and smash); and Makeout Point (giant white letters
    /// on the ridge, a convertible, the whole coast below).
    public partial class HairKit
    {
        static readonly Vector2 MotelAt = new Vector2(7708, 3300), SalonAt = new Vector2(7905, 3316), ChapelAt = new Vector2(7548, 3266), ShootAt = new Vector2(7422, 3320), ShedAt = new Vector2(7748, 3082), PointAt = new Vector2(7664, 2872);
        static readonly Vector2[] LoopWay = { new Vector2(7676, 3222), new Vector2(7682, 3262), new Vector2(7740, 3296), new Vector2(7820, 3306), new Vector2(7905, 3296), new Vector2(8000, 3306), new Vector2(8075, 3292), new Vector2(8135, 3262), new Vector2(8172, 3236) };
        static readonly Vector2[] WestWay = { new Vector2(7662, 3200), new Vector2(7610, 3226), new Vector2(7560, 3246), new Vector2(7495, 3284), new Vector2(7445, 3306) };
        static readonly Vector2[] SouthWay = { new Vector2(7716, 3152), new Vector2(7730, 3105), new Vector2(7722, 3040), new Vector2(7700, 2985), new Vector2(7675, 2925), new Vector2(7666, 2892) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (MotelAt, 16f), (SalonAt, 10f), (ChapelAt, 10f), (ShootAt, 15f), (ShedAt, 10f), (PointAt, 12f) };

        HairPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> roadPts = new List<Vector3>();

        static readonly Color[] Spandex = { new Color(1f, .25f, .65f), new Color(.1f, .1f, .12f), new Color(.3f, .9f, 1f), new Color(1f, .78f, .25f), new Color(.65f, .35f, 1f), new Color(.95f, .95f, .95f), new Color(.85f, .15f, .15f) };
        static readonly Color[] Tights = { new Color(.1f, .1f, .12f), new Color(.85f, .62f, .28f), new Color(1f, .25f, .65f), new Color(.2f, .3f, .6f), new Color(.95f, .95f, .95f) };
        static readonly Color[] BigHair = { new Color(.98f, .9f, .5f), new Color(.1f, .08f, .08f), new Color(.85f, .3f, .15f), new Color(.95f, .95f, .9f), new Color(1f, .4f, .7f) };
        static readonly string[] StripTalk =
        {
            "Nothing but a good time. That's the zoning law.", "The higher the hair, the closer to the encore.", "We go on at nine. We've been going on at nine since '87.",
            "Is my eyeliner straight? Doesn't matter. Is it LOUD?", "You gotta cruise the Loop at least once. It's how you say hello.",
        };

        Residents.Look Rocker(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Spandex, Tights, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Cowboy, Residents.Hat.None }, Spandex); l.hair = BigHair[prng.Next(BigHair.Length)]; l.longHair = true; l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Rocker(), pose, prng, lines ?? StripTalk, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// Flattens a site (ShapeGround).
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

        /// Neon letters on a black board on chrome posts. Signs go on the +x side of a lane; the photo spot on the −x side.
        void NeonSign(MeshKit mk, Vector3 at, Vector3 face, string text, Color c, float w = 3f, float size = .2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(chrome, g + rot * Vector3.right * e * (w / 2f - .1f) + up * 1.1f, new Vector3(.1f, 2.2f, .1f), rot);
            mk.Box(black, g + up * 1.9f, new Vector3(w, .9f, .1f), rot); Col(g + up * 1.2f, new Vector3(w, 2.4f, .15f), rot);
            mk.Box(GlowM(c, 2.4f), g + up * 2.38f, new Vector3(w, .05f, .12f), rot); mk.Box(GlowM(c, 2.4f), g + up * 1.42f, new Vector3(w, .05f, .12f), rot); halos.Add(g + up * 1.9f + face * .3f, 1.5f, c, .45f);
            ArtistBits.Label(rootT, text, g + up * 1.9f + face * .06f, face, size, c, TextAnchor.MiddleCenter);
        }

        void ChromeLamp(MeshKit mk, Vector3 g, Color c)
        {
            var up = Vector3.up; mk.Prism(chrome, g - up * .2f, up, .08f, 4.2f, 6); mk.Box(GlowM(c, 2.6f), g + up * 4.1f, new Vector3(.9f, .1f, .1f), Quaternion.identity); mk.Box(GlowM(c, 2.6f), g + up * 4.1f, new Vector3(.1f, .1f, .9f), Quaternion.identity);
            halos.Add(g + up * 4.1f, 1.5f, c, .7f); ArtistBits.PostCollider(rootT, g, 4.2f, .1f);
        }

        void LeopardBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(leopard, g + up * .45f, new Vector3(len, .1f, .5f), rot); mk.Box(leopard, g + up * .8f - face * .25f, new Vector3(len, .5f, .08f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(chrome, g + up * .22f + side * e * (len / 2f - .15f), new Vector3(.06f, .45f, .45f), rot);
            Col(g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(plife.transform, g + up * .5f - side * (len / 2f - .4f), g + up * .5f + side * (len / 2f - .4f), face);
        }

        void RoadStrip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]); var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t); var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] + Vector3.up * lift - side * (w / 2f * miter); R[i] = pts[i] + Vector3.up * lift + side * (w / 2f * miter);
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        List<Vector3> NeonRoad(LandContext ctx, MeshKit mk, Vector2[] way, Color c)
        {
            var pts = Smooth(ctx, way, false, 2.5f);
            for (var i = 0; i < pts.Count; i++) foreach (var ring in noGrass) { if (ring.y < 9f) continue; var r = ring.y + 3f; var v = new Vector2(pts[i].x - ring.x, pts[i].z - ring.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(ring.x + v.x, ring.z + v.y); } }
            if (pts.Count < 2) return pts;
            RoadStrip(mk, pts, asphalt, 5f, .07f); var up = Vector3.up; var lineM = GlowM(c, 1.6f);
            for (var i = 1; i < pts.Count; i += 2) { var a = pts[i - 1]; var b = pts[i]; mk.Box(lineM, (a + b) / 2f + up * .085f, new Vector3(.16f, .02f, Vector3.Distance(a, b) * .6f), Quaternion.LookRotation(b - a)); }
            var n = 0;
            for (var i = 4; i < pts.Count; i += 10)
            {
                var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d) * (n % 2 == 0 ? 1f : -1f); var lp = pts[i] + sd * 3.4f;
                if (ctx.IsLand(lp.x, lp.z, 2f) && !OnStrip(lp.x, lp.z, 0f)) { if (n % 3 == 1) Palm(ctx, mk, ctx.OnGround(lp.x, lp.z), ctx.Range(7f, 9f), Neon[n % Neon.Length]); else ChromeLamp(mk, ctx.OnGround(lp.x, lp.z), n % 2 == 0 ? c : Gold); }
                n++;
            }
            for (var i = 0; i < pts.Count; i += 2) noGrass.Add(new Vector3(pts[i].x, 4.5f, pts[i].z));   // keeps the desert's scatter off the road
            roadPts.AddRange(pts);
            if (pts.Count > 30) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = pts[i] - sd * 4.2f; if (ctx.IsLand(bp.x, bp.z, 2f) && !OnStrip(bp.x, bp.z, 0f)) LeopardBench(mk, bp, sd); }
            return pts;
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 2.6f; var b = new Vector2(from.x, from.z) + d * (len - stopShort);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            RoadStrip(mk, pts, pave, 2.6f, .065f); foreach (var p in pts) noGrass.Add(new Vector3(p.x, 3f, p.z));
        }

        void StripPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1987);
            plife = new GameObject("Strip places life").AddComponent<HairPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            var loop = NeonRoad(ctx, mk, LoopWay, Pink); NeonRoad(ctx, mk, WestWay, Cyan); NeonRoad(ctx, mk, SouthWay, Violet);
            Motel(ctx, mk, solid); Salon(ctx, mk, solid); Chapel(ctx, mk, solid); VideoShoot(ctx, mk, solid); ShredShed(ctx, mk, solid); MakeoutPoint(ctx, mk, solid);
            // two people walking the Loop, as you do
            if (loop.Count > 20) { var route = new List<Vector3>(); for (var i = 3; i < loop.Count - 3; i += 5) { var d = loop[i + 1] - loop[i - 1]; d.y = 0; d.Normalize(); var p = loop[i] + Vector3.Cross(Vector3.up, d) * 3.2f; route.Add(ctx.OnGround(p.x, p.z)); } for (var i = route.Count - 2; i > 0; i--) route.Add(route[i]); if (route.Count > 3) { Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, new[] { "Walking the Loop. The car's in the pool. Long story.", "You seen a drummer? Ours exploded. It happens.", "Tease is up ahead if your hair's gone flat. No shame in it." }, Rocker(), route.ToArray()); } }
        }

        // ---------------------------------------------------------------- the Motel
        void Motel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(MotelAt.x, MotelAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); var pinkWall = WorldMaterials.Solid(new Color(1f, .55f, .7f)); var teal = WorldMaterials.Solid(new Color(.2f, .7f, .7f));
            mk.Box(pave, g - up * .4f, new Vector3(30f, 1f, 24f), f.r); solid.Box(pave, g - up * .4f, new Vector3(30f, 1f, 24f), f.r); var fo = new PlaceFrame(g + up * .1f, f.Fwd);
            // a U of rooms round the court: back wing and two side wings, a walkway roof on chrome posts, doors and lit windows
            void Wing(Vector3 c, Vector3 faceDir, float len, int rooms)
            {
                var wr = Quaternion.LookRotation(faceDir); var ws = Vector3.Cross(up, faceDir);
                mk.Box(pinkWall, c + up * 1.6f, new Vector3(len, 3.2f, 4f), wr); solid.Box(pinkWall, c + up * 1.6f, new Vector3(len, 3.2f, 4f), wr); mk.Box(teal, c + up * 3.3f + faceDir * .9f, new Vector3(len + .4f, .2f, 6.2f), wr);
                for (var k = 0; k < rooms; k++)
                {
                    var x = -len / 2f + (k + .5f) * len / rooms; var p = c + ws * x + faceDir * 2.02f;
                    mk.Box(teal, p + up * 1.05f - ws * .9f, new Vector3(.95f, 2.1f, .05f), wr); mk.Box(GlowM(k % 3 == 1 ? Cyan : Gold, 1.3f), p + up * 1.6f + ws * .7f, new Vector3(1.3f, 1.1f, .05f), wr); mk.Box(GlowM(Pink, 2f), p + up * 2.5f - ws * .9f, new Vector3(.3f, .2f, .05f), wr);
                    var post = c + ws * (x - len / rooms / 2f) + faceDir * 3.8f; mk.Prism(chrome, post, up, .06f, 3.2f, 6); ArtistBits.PostCollider(rootT, post, 3.2f, .08f);
                }
            }
            Wing(fo.P(0, 0, -9f), f.Fwd, 26f, 6); Wing(fo.P(-12f, 0, 0), f.Right, 12f, 3); Wing(fo.P(12f, 0, 0), -f.Right, 12f, 3);
            // the pool in the court (raised rim, glowing), loungers
            var poolM = new Material(WorldMaterials.Glow(new Color(.25f, .85f, .95f), .8f)) { name = "Motel pool" }; var pc = fo.P(0, 0, 0);
            mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), pc + up * .2f, new Vector3(11f, .5f, 6f), f.r); mk.Box(poolM, pc + up * .46f, new Vector3(10.2f, .02f, 5.2f), f.r);
            foreach (var (x, z, w, d) in new[] { (0f, -2.8f, 11f, .4f), (0f, 2.8f, 11f, .4f), (-5.3f, 0f, .4f, 6f), (5.3f, 0f, .4f, 6f) }) Col(pc + f.r * new Vector3(x, .25f, z), new Vector3(w, .5f, d), f.r);
            var poolFloor = new GameObject("Motel pool floor"); poolFloor.transform.SetParent(rootT, false); poolFloor.transform.SetPositionAndRotation(pc + up * .2f, f.r); poolFloor.AddComponent<BoxCollider>().size = new Vector3(10.2f, .4f, 5.2f);
            halos.Add(pc + up * 1f, 4.5f, new Color(.3f, .9f, 1f), .35f); ArtistBits.PointLight(ctx.Root, pc + up * 1.6f, new Color(.4f, .9f, 1f), 18f, 1.8f);
            for (var k = 0; k < 4; k++) { var p = fo.P(-4.5f + k * 3f, 0, 4.6f); mk.Box(chrome, p + up * .3f, new Vector3(.7f, .06f, 1.7f), f.r); mk.Box(WorldMaterials.Solid(Neon[k % Neon.Length]), p + up * .35f, new Vector3(.62f, .05f, 1.6f), f.r); mk.Box(chrome, p + up * .6f + f.Fwd * .95f, new Vector3(.7f, .06f, .7f), f.r * Quaternion.Euler(55f, 0, 0)); Col(p + up * .3f, new Vector3(.7f, .6f, 1.9f), f.r); if (k != 1) Interact.Seat(plife.transform, p + up * .42f, -f.Fwd, "Lie by the pool"); }
            Folk(fo.P(-1.5f, .42f, 4.6f), -f.Fwd, Resident.Pose.Sit, new[] { "Room service! ...It's just me shouting. There's no room service.", "The TV was like that when we checked in.", "Throw it in. Everybody does. They bring a new one every morning." }, Rocker(Residents.Tool.Mug), null, .42f);
            // the TV: carry it, throw it in the pool; and a couple of ice buckets
            var tv = new MeshKit(); tv.Box(WorldMaterials.Solid(new Color(.35f, .25f, .2f)), Vector3.zero, new Vector3(.7f, .55f, .5f), Quaternion.identity); tv.Box(GlowM(new Color(.7f, .85f, 1f), 1.4f), new Vector3(-.06f, .02f, .26f), new Vector3(.46f, .4f, .02f), Quaternion.identity); tv.Box(chrome, new Vector3(.1f, .5f, 0), new Vector3(.02f, .5f, .02f), Quaternion.Euler(0, 0, -25f)); tv.Box(chrome, new Vector3(-.1f, .5f, 0), new Vector3(.02f, .5f, .02f), Quaternion.Euler(0, 0, 25f));
            var tgo = tv.BuildSingle("Motel TV", plife.transform, false); tgo.transform.SetPositionAndRotation(fo.P(4f, .4f, 5.8f), f.r * Quaternion.Euler(0, 200f, 0)); var tc = Interact.Carry(tgo, "the television", 3f, g.y - 10f); tc.GetComponent<BoxCollider>().size = new Vector3(.7f, .6f, .5f);
            for (var k = 0; k < 2; k++) { var bm = new MeshKit(); bm.Prism(chrome, Vector3.zero, Vector3.up, .14f, .24f, 8, 1.3f); bm.Blob(WorldMaterials.Glow(new Color(.8f, .95f, 1f), .3f), new Vector3(0, .26f, 0), Vector3.one * .14f, ctx.Rng, .3f); var go = bm.BuildSingle("Ice bucket", plife.transform, false); go.transform.position = fo.P(-6.5f + k * .6f, .12f, 5.5f); Interact.Carry(go, "an ice bucket", .8f, g.y - 10f); }
            // the sign on a tall pole at the road
            var sp = G(f.P(12f, 0, 15f)); mk.Prism(chrome, sp, up, .15f, 8f, 8); ArtistBits.PostCollider(rootT, sp, 8f, .2f); mk.Box(black, sp + up * 7f, new Vector3(4.6f, 2f, .3f), f.r); mk.Box(black, sp + up * 5.4f, new Vector3(3.4f, .8f, .3f), f.r);
            ArtistBits.Label(ctx.Root, "THE ROXY-ISH\nMOTEL", sp + up * 7f + f.Fwd * .17f, f.Fwd, .45f, Pink, TextAnchor.MiddleCenter); ArtistBits.Label(ctx.Root, "POOL · COLOR TV (WET)", sp + up * 5.4f + f.Fwd * .17f, f.Fwd, .2f, Cyan, TextAnchor.MiddleCenter); halos.Add(sp + up * 7f + f.Fwd * .5f, 3f, Pink, .5f);
            Lane(ctx, mk, f.P(0, 0, 11f)); Ring(g, 17f);
            Shot(ctx, "the motel", f.P(-9f, 5f, 22f), g + up * 1.2f);
        }

        // ---------------------------------------------------------------- Tease, the salon
        void Salon(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SalonAt.x, SalonAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 10f, D = 7f, H = 3.4f, T = .3f, Door = HumanScale.DoorWidth + .3f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var wallM = WorldMaterials.Solid(new Color(.95f, .45f, .7f)); var floorM = WorldMaterials.Solid(new Color(.9f, .9f, .92f));
            mk.Box(pave, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(wallM, p, s, f.r); solid.Box(wallM, p, s, f.r); }
            mk.Box(floorM, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(floorM, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            for (var x = 0; x < 6; x++) for (var z = 0; z < 4; z++) if ((x + z) % 2 == 0) mk.Box(black, fo.P(-W / 2f + T + .8f + x * 1.55f, .025f, -D / 2f + T + .8f + z * 1.55f), new Vector3(1.5f, .01f, 1.5f), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) { Wall(fo.P(e * (Door / 2f + seg / 2f), .4f, D / 2f - T / 2f), new Vector3(seg, .8f, T)); Wall(fo.P(e * (Door / 2f + seg / 2f), H - .35f, D / 2f - T / 2f), new Vector3(seg, .7f, T)); Wall(fo.P(e * (W / 2f - .3f), H / 2f, D / 2f - T / 2f), new Vector3(.6f, H, T)); Wall(fo.P(e * (Door / 2f + .2f), H / 2f, D / 2f - T / 2f), new Vector3(.4f, H, T)); solid.Box(wallM, fo.P(e * (Door / 2f + seg / 2f), 1.75f, D / 2f - T / 2f), new Vector3(seg, 2f, .1f), f.r); }
            Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(black, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r); solid.Box(black, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r); for (var s = 0; s < 10; s++) mk.Box(s % 2 == 0 ? black : floorM, fo.P(-W / 2f + .5f + s * 1f, H - .25f, D / 2f + .8f), new Vector3(1f, .08f, 1.6f), f.r * Quaternion.Euler(14f, 0, 0));
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            ArtistBits.Label(ctx.Root, "TEASE", fo.P(0, H + 1.1f, D / 2f + .12f), f.Fwd, .8f, Pink, TextAnchor.MiddleCenter); mk.Box(black, fo.P(0, H + 1.1f, D / 2f), new Vector3(5f, 1.6f, .14f), f.r); halos.Add(fo.P(0, H + 1.1f, D / 2f + .5f), 2.8f, Pink, .5f);
            // a giant hairspray can on the roof corner, puffing
            life.cans.Add(HairsprayCan(ctx, mk, solid, fo.P(-W / 2f + 1.2f, H + .2f, -1f), .5f, Gold));
            // three dryer chairs along the back: sit under one and the hood glows
            for (var k = 0; k < 3; k++)
            {
                var c = fo.P(-3f + k * 2.2f, 0, -D / 2f + T + .7f); mk.Box(WorldMaterials.Solid(Neon[k]), c + up * .45f, new Vector3(.7f, .12f, .7f), f.r); mk.Box(WorldMaterials.Solid(Neon[k]), c + up * .95f - f.Fwd * .32f, new Vector3(.7f, .9f, .1f), f.r); mk.Prism(chrome, c, up, .08f, .4f, 6); Col(c + up * .5f, new Vector3(.7f, 1f, .7f), f.r);
                mk.Prism(chrome, c + up * 1.45f - f.Fwd * .32f, up, .04f, .5f, 5); var hm = new Material(WorldMaterials.Glow(Neon[k], .2f)) { name = "Dryer hood" }; var hk = new MeshKit(); hk.Prism(chrome, Vector3.zero, Vector3.up, .34f, .4f, 10, .7f); hk.Prism(hm, new Vector3(0, -.02f, 0), Vector3.up, .3f, .03f, 10);
                var hgo = hk.BuildSingle("Dryer hood", plife.transform, false); hgo.transform.position = c + up * 1.72f; plife.hoods.Add((hm, Neon[k]));
                var seat = Interact.Seat(plife.transform, c + up * .52f, f.Fwd, "Sit under the dryer"); plife.dryerSeats.Add(seat.transform.position);
            }
            // the styling station: a mirror ringed with bulbs, a shelf of hairspray to carry off
            var ms = fo.P(W / 2f - T - .05f, 0, -.6f); mk.Box(GlowM(new Color(.8f, .9f, 1f), .5f), ms + up * 1.6f, new Vector3(.04f, 1.2f, 1.6f), f.r); for (var k = 0; k < 6; k++) { mk.Blob(GlowM(Gold, 2.6f), ms + up * 2.3f - f.Right * .06f + f.Fwd * (-.75f + k * .3f), Vector3.one * .06f, ctx.Rng, 0f); mk.Blob(GlowM(Gold, 2.6f), ms + up * .9f - f.Right * .06f + f.Fwd * (-.75f + k * .3f), Vector3.one * .06f, ctx.Rng, 0f); }
            mk.Box(chrome, ms + up * .8f - f.Right * .3f, new Vector3(.55f, .06f, 1.8f), f.r); Col(ms + up * .45f - f.Right * .3f, new Vector3(.55f, .9f, 1.8f), f.r);
            for (var k = 0; k < 4; k++) { var cm = new MeshKit(); cm.Prism(WorldMaterials.Solid(Neon[(k + 1) % Neon.Length], .6f), Vector3.zero, Vector3.up, .06f, .26f, 8); cm.Prism(chrome, new Vector3(0, .26f, 0), Vector3.up, .035f, .06f, 6); var go = cm.BuildSingle("Hairspray", plife.transform, false); go.transform.position = ms + up * .84f - f.Right * .3f + f.Fwd * (-.6f + k * .4f); Interact.Carry(go, "a can of hairspray (extra hold)", .4f, g.y - 10f); }
            var sc = fo.P(W / 2f - T - 1.5f, 0, -.6f); mk.Prism(chrome, sc, up, .08f, .45f, 6); mk.Box(black, sc + up * .5f, new Vector3(.6f, .1f, .6f), f.r); mk.Box(black, sc + up * .9f - f.Right * .3f, new Vector3(.08f, .7f, .6f), f.r); Col(sc + up * .5f, new Vector3(.6f, 1f, .6f), f.r); Interact.Seat(plife.transform, sc + up * .56f, f.Right, "Sit in the stylist's chair");
            Folk(sc - f.Fwd * 1.1f - f.Right * .2f, FaceTo(sc - f.Fwd * 1.1f, sc), Resident.Pose.Work, new[] { "Sit. I can get another four inches out of that, easy.", "It's not big enough until it needs its own lane.", "Two cans a head. Three for a ballad." }, Rocker(Residents.Tool.Hose, true)).workRate = 1.2f;
            Folk(fo.P(-3f + 2.2f, .52f, -D / 2f + T + .7f), f.Fwd, Resident.Pose.Sit, new[] { "Forty minutes under here. Worth it. You can't rush structure.", "WHAT? I'M UNDER THE DRYER." }, Rocker(), null, .52f);
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.7f, 0), new Color(1f, .7f, .85f), 12f, 2f, true);
            LeopardBench(mk, f.P(-3.8f, 0, D / 2f + 2.4f), f.Fwd);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 9f);
            Shot(ctx, "tease, the salon", f.P(-7f, 2.6f, D / 2f + 11f), fo.P(0, 2.6f, 0));
            Shot(ctx, "inside tease", fo.P(-3.6f, 1.6f, D / 2f - 1.1f), fo.P(1.5f, 1.2f, -D / 2f + 1f));
        }

        // ---------------------------------------------------------------- the Chapel of Love
        void Chapel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ChapelAt.x, ChapelAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); var whiteM = WorldMaterials.Solid(new Color(.96f, .95f, .97f));
            mk.Box(pave, g - up * .4f, new Vector3(13f, 1f, 16f), f.r); solid.Box(pave, g - up * .4f, new Vector3(13f, 1f, 16f), f.r); var fo = new PlaceFrame(g + up * .1f, f.Fwd);
            // an open-fronted little chapel: back and side walls, a steep roof, a steeple with a bell, a neon heart over the arch
            mk.Box(whiteM, fo.P(0, 1.8f, -5f), new Vector3(6f, 3.6f, .3f), f.r); solid.Box(whiteM, fo.P(0, 1.8f, -5f), new Vector3(6f, 3.6f, .3f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(whiteM, fo.P(e * 2.85f, 1.8f, -2.5f), new Vector3(.3f, 3.6f, 5f), f.r); solid.Box(whiteM, fo.P(e * 2.85f, 1.8f, -2.5f), new Vector3(.3f, 3.6f, 5f), f.r); }
            mk.GableRoof(WorldMaterials.Solid(new Color(1f, .45f, .7f)), fo.P(0, 3.6f, -2.5f), new Vector3(6.6f, 0, 5.6f), 2.6f, f.r); mk.Box(whiteM, fo.P(0, 6.6f, -.6f), new Vector3(1.1f, 1.8f, 1.1f), f.r); mk.Cone(WorldMaterials.Solid(new Color(1f, .45f, .7f)), fo.P(0, 7.5f, -.6f), .9f, 1.8f, 4, 45f);
            var heart = new MeshKit(); var hm = new Material(WorldMaterials.Glow(Pink, 1.6f)) { name = "Chapel heart" }; heart.Blob(hm, new Vector3(-.42f, .3f, 0), new Vector3(.55f, .55f, .12f), ctx.Rng, 0f); heart.Blob(hm, new Vector3(.42f, .3f, 0), new Vector3(.55f, .55f, .12f), ctx.Rng, 0f); heart.Cone(hm, new Vector3(0, .25f, 0), .82f, -1.1f, 4, 45f);
            var hgo = heart.BuildSingle("Soft chapel heart", plife.transform, false); hgo.transform.SetPositionAndRotation(fo.P(0, 4.9f, .25f), f.r); hgo.transform.localScale = new Vector3(1f, 1f, .25f); plife.heart = hgo.transform; plife.heartM = hm; plife.heartLight = ArtistBits.PointLight(ctx.Root, fo.P(0, 4.6f, 1.6f), Pink, 16f, 1.6f); halos.Add(fo.P(0, 4.9f, .6f), 2.2f, Pink, .5f);
            // the bell: ring it and the heart beats
            var bm = new MeshKit(); bm.Prism(WorldMaterials.Solid(new Color(.9f, .75f, .3f), .85f), new Vector3(0, -.5f, 0), Vector3.up, .32f, .5f, 10, .45f); var bgo = bm.BuildSingle("Chapel bell", plife.transform, false); bgo.transform.SetPositionAndRotation(fo.P(0, 6.9f, -.6f), f.r); var bs = bgo.AddComponent<Sway>(); bs.degrees = 30f; bs.speed = 6f; bs.decay = .8f; bs.seconds = 6f;
            var rope = G(fo.P(3.6f, 0, .6f)); mk.Prism(chrome, rope, up, .04f, 1.4f, 5); mk.Blob(GlowM(Gold, 2f), rope + up * 1.45f, Vector3.one * .1f, ctx.Rng, 0f); ArtistBits.PostCollider(rootT, rope, 1.4f, .06f);
            var rgo = new GameObject("Chapel bell pull"); rgo.transform.SetParent(plife.transform, false); rgo.transform.position = rope + up * 1.3f; Interact.Toy(rgo, Toy.Kind.Custom, "Ring the wedding bell", new Vector3(.7f, .9f, .7f)).onUse = _ => { bs.Push(); plife.rang = Time.time; Say(plife.transform, fo.P(0, 3.4f, 1.5f), "Ding! Somebody, somewhere on the Strip, just got married.", 4f); };
            // inside: an altar with a leopard cloth, two short pews, the officiant, a couple
            mk.Box(leopard, fo.P(0, .55f, -3.9f), new Vector3(1.6f, 1.1f, .7f), f.r); Col(fo.P(0, .55f, -3.9f), new Vector3(1.6f, 1.1f, .7f), f.r); foreach (var e in new[] { -.5f, .5f }) { mk.Prism(whiteM, fo.P(e, 1.1f, -3.9f), up, .03f, .4f, 5); mk.Blob(GlowM(Gold, 3f), fo.P(e, 1.56f, -3.9f), new Vector3(.04f, .08f, .04f), ctx.Rng, 0f); }
            foreach (var e in new[] { -1f, 1f }) { var p = fo.P(e * 1.6f, 0, -.9f); mk.Box(whiteM, p + up * .45f, new Vector3(1.7f, .08f, .45f), f.r); mk.Box(whiteM, p + up * .8f + f.Fwd * .22f, new Vector3(1.7f, .5f, .06f), f.r); Col(p + up * .5f, new Vector3(1.7f, 1f, .5f), f.r); Interact.Bench(plife.transform, p + up * .5f - f.Right * .45f, p + up * .5f + f.Right * .45f, -f.Fwd); }
            Folk(fo.P(0, 0, -3.1f), f.Fwd, Resident.Pose.Stand, new[] { "Do you take this power ballad, to have and to hold, till the fade-out?", "Rings are extra. Pyro is included.", "I've married this couple nine times. They like the bit with the bell." }, new Residents.Look { skin = new Color(.85f, .65f, .5f), hair = new Color(.1f, .08f, .08f), top = new Color(.85f, .62f, .28f), bottom = new Color(.1f, .1f, .12f), longHair = true, height = 1.05f });
            var bride = Rocker(); bride.top = new Color(.97f, .97f, .97f); bride.bottom = new Color(.97f, .97f, .97f); var groom = Rocker(); groom.top = new Color(.1f, .1f, .12f); groom.bottom = Tights[1];
            Folk(fo.P(-.45f, 0, -2f), -f.Fwd, Resident.Pose.Stand, new[] { "I do. Again!", "He wrote me a solo. It's eleven minutes." }, bride); Folk(fo.P(.45f, 0, -2f), -f.Fwd, Resident.Pose.Stand, new[] { "I do. I always do.", "We met in the pit. She was crowd-surfing. I caught her. Mostly." }, groom);
            // the bouquet to throw
            var bq = new MeshKit(); bq.Prism(WorldMaterials.Solid(new Color(.3f, .6f, .35f)), Vector3.zero, Vector3.up, .04f, .25f, 5); foreach (var (x, z, c) in new[] { (-.07f, 0f, Pink), (.07f, .03f, new Color(1f, .3f, .3f)), (0f, -.07f, new Color(1f, 1f, 1f)), (0f, .08f, Pink) }) bq.Blob(WorldMaterials.Solid(c), new Vector3(x, .3f, z), Vector3.one * .08f, ctx.Rng, .2f);
            var qgo = bq.BuildSingle("Bouquet", plife.transform, false); qgo.transform.position = fo.P(-1.6f, .52f, -.9f) + f.Right * -.2f + up * .05f; Interact.Carry(qgo, "the bouquet", .3f, g.y - 10f);
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.6f, -2.5f), new Color(1f, .8f, .85f), 10f, 1.6f);
            NeonSign(mk, f.P(5.5f, 0, 9f), f.Fwd, "CHAPEL OF LOVE\nopen 24 hrs · no refunds", Pink, 3.6f, .17f);
            Lane(ctx, mk, f.P(0, 0, 4f)); Ring(g, 9f);
            Shot(ctx, "the chapel of love", f.P(-6f, 2.6f, 12f), fo.P(0, 3f, -2f));
        }

        // ---------------------------------------------------------------- the Video Shoot
        void VideoShoot(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ShootAt.x, ShootAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            // the set: a low rock stage dressed as a mesa top, a band miming, a backdrop of a painted sunset on a scaffold
            var st = G(f.P(0, 0, -7f)); mk.Prism(rock, st - up * .6f, up, 6.5f, 1.5f, 9, .85f); solid.Prism(rock, st - up * .6f, up, 6.5f, 1.5f, 9, .85f); var so = st + up * .9f;
            for (var k = 0; k < 3; k++) { var sp = st + f.Fwd * (5.6f + k * .35f) + up * (.9f - .3f - k * .3f - .15f); mk.Box(rock, sp, new Vector3(2.6f, .3f, .4f), f.r); solid.Box(rock, sp, new Vector3(2.6f, .3f, .4f), f.r); }
            foreach (var e in new[] { -1f, 1f }) { var p = G(st - f.Fwd * 6.6f + f.Right * e * 6f); mk.Prism(chrome, p, up, .1f, 8f, 6); ArtistBits.PostCollider(rootT, p, 8f, .12f); }
            var bd = G(st - f.Fwd * 6.6f) + up * 5f; mk.Box(GlowM(new Color(1f, .5f, .3f), 1f), bd - up * 1.5f, new Vector3(12f, 3f, .1f), f.r); mk.Box(GlowM(new Color(.75f, .3f, .8f), 1f), bd + up * 1.5f, new Vector3(12f, 3f, .1f), f.r); mk.Prism(GlowM(Gold, 2.4f), bd - up * 1.2f + f.Fwd * .16f, f.Fwd, 1.6f, .05f, 16); Col(bd, new Vector3(12f, 6f, .2f), f.r);
            string[] mime = { "Is the wind on? My hair's just sitting there.", "Take nineteen. I point at the sky on 'forever'.", "We're not plugged in. Don't tell the camera." };
            Folk(so + f.Fwd * 2f, f.Fwd, Resident.Pose.Wave, mime, Rocker()); Folk(so + f.Fwd * .6f - f.Right * 2.6f, f.Fwd, Resident.Pose.Work, mime, Rocker(Residents.Tool.Rod)).workRate = 2f; Folk(so + f.Fwd * .6f + f.Right * 2.6f, f.Fwd, Resident.Pose.Work, mime, Rocker(Residents.Tool.Rod)).workRate = 1.7f;
            // the wind machine: start it and the fan spins, streamers fly
            var wp = G(f.P(-7.5f, 0, 1f)); var wdir = FaceTo(wp, st); var wr = Quaternion.LookRotation(wdir); mk.Box(black, wp + up * .25f, new Vector3(1.6f, .5f, 1.2f), wr); foreach (var e in new[] { -.7f, .7f }) mk.Box(chrome, wp + up * 1.2f + wr * Vector3.right * e, new Vector3(.08f, 1.6f, .08f), wr); Col(wp + up * 1f, new Vector3(1.8f, 2f, 1.2f), wr);
            mk.Prism(chrome, wp + up * 1.5f - wdir * .25f, wdir, 1.05f, .5f, 14);
            var fan = new MeshKit(); for (var k = 0; k < 5; k++) fan.Box(black, Quaternion.Euler(0, 0, k * 72f) * new Vector3(0, .5f, 0), new Vector3(.28f, .9f, .04f), Quaternion.Euler(0, 20f, k * 72f)); fan.Prism(chrome, new Vector3(0, 0, -.05f), Vector3.forward, .14f, .12f, 8);
            var fgo = fan.BuildSingle("Wind machine fan", plife.transform, false); fgo.transform.SetPositionAndRotation(wp + up * 1.5f + wdir * .28f, wr); plife.fan = fgo.transform; plife.fanRot = wr;
            var ft = Interact.Toy(fgo, Toy.Kind.Custom, "Switch the wind machine", new Vector3(2.2f, 2.2f, 1f)); ft.onUse = _ => { plife.wind = !plife.wind; Say(plife.transform, wp + up * 3.2f, plife.wind ? "WHOOSH. Every head of hair on the set goes sideways. Perfect." : "The hair settles. Somebody sighs.", 4f); };
            for (var k = 0; k < 6; k++) { var sm = new MeshKit(); sm.Box(WorldMaterials.Solid(Neon[k % Neon.Length]), new Vector3(0, 0, .6f), new Vector3(.08f, .02f, 1.2f), Quaternion.identity); var sgo = sm.BuildSingle("Soft streamer", plife.transform, false); var sp = wp + up * (1.1f + (k % 3) * .4f) + wdir * .6f + wr * Vector3.right * (-.5f + (k % 2) * 1f); sgo.transform.SetPositionAndRotation(sp, wr); plife.streamers.Add((sgo.transform, wr, k * 1.3f)); }
            // the camera crane and the director
            var cb = G(f.P(5.5f, 0, 3f)); mk.Box(black, cb + up * .3f, new Vector3(1.6f, .6f, 1.6f), f.r); mk.Prism(chrome, cb + up * .6f, up, .12f, 1.6f, 6); Col(cb + up * 1f, new Vector3(1.6f, 2f, 1.6f), f.r); var arm = (FaceTo(cb, st) + up * .45f).normalized; mk.Prism(chrome, cb + up * 2.2f - arm * 1.5f, arm, .08f, 6.5f, 6); mk.Box(black, cb + up * 2.2f - arm * 1.7f, new Vector3(.6f, .6f, .6f), f.r); mk.Box(black, cb + up * 2.2f + arm * 5.1f, new Vector3(.5f, .5f, .9f), Quaternion.LookRotation(FaceTo(cb, st))); mk.Blob(GlowM(new Color(1f, .2f, .15f), 3f), cb + up * 2.55f + arm * 5.1f, Vector3.one * .07f, ctx.Rng, 0f);
            var dc = G(f.P(2.4f, 0, 5f)); var dr = Quaternion.LookRotation(FaceTo(dc, st)); foreach (var (x, z) in new[] { (-.3f, -.25f), (.3f, -.25f), (.3f, .25f), (-.3f, .25f) }) mk.Box(chrome, dc + dr * new Vector3(x, .4f, z), new Vector3(.04f, .8f, .04f), dr); mk.Box(black, dc + up * .7f, new Vector3(.62f, .05f, .52f), dr); mk.Box(black, dc + up * 1.2f - dr * Vector3.forward * .27f, new Vector3(.62f, .35f, .04f), dr); Col(dc + up * .6f, new Vector3(.65f, 1.2f, .55f), dr);
            Interact.Seat(plife.transform, dc + up * .76f, FaceTo(dc, st), "Sit in the director's chair"); ArtistBits.Label(ctx.Root, "DIRECTOR", dc + up * 1.2f - dr * Vector3.forward * .3f, -(dr * Vector3.forward), .08f, Ink, TextAnchor.MiddleCenter);
            Folk(G(dc + dr * Vector3.right * 1.3f), FaceTo(dc, st), Resident.Pose.Stand, new[] { "More wind! More fog! More POINTING!", "It's a love song, so obviously we're on a cliff.", "Sit in my chair. Tell me if it reads as 'yearning'." }, new Residents.Look { skin = new Color(.8f, .6f, .48f), hair = new Color(.3f, .2f, .15f), top = new Color(.1f, .1f, .12f), bottom = new Color(.85f, .82f, .75f), hat = Residents.Hat.Cap, hatColor = new Color(.85f, .15f, .15f), tool = Residents.Tool.Mug });
            foreach (var e in new[] { -1f, 1f }) { var lp = G(f.P(e * 9f, 0, -3f)); mk.Prism(chrome, lp, up, .06f, 4f, 5); mk.Box(black, lp + up * 4.1f, new Vector3(.7f, .5f, .5f), Quaternion.LookRotation(FaceTo(lp, st))); mk.Box(GlowM(e < 0 ? Pink : Cyan, 3f), lp + up * 4.1f + FaceTo(lp, st) * .26f, new Vector3(.6f, .4f, .02f), Quaternion.LookRotation(FaceTo(lp, st))); ArtistBits.PostCollider(rootT, lp, 4f, .08f); ArtistBits.PointLight(ctx.Root, lp + up * 3.6f + FaceTo(lp, st) * 2f, e < 0 ? Pink : Cyan, 18f, 2f); }
            NeonSign(mk, f.P(8f, 0, 11f), f.Fwd, "QUIET ON SET\n(as if)", Gold, 3f, .18f);
            Lane(ctx, mk, f.P(0, 0, 8f)); Ring(g, 14f);
            Shot(ctx, "the video shoot", f.P(-10f, 3.6f, 13f), st + up * 2.4f);
        }

        // ---------------------------------------------------------------- the Shred Shed
        void ShredShed(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ShedAt.x, ShedAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            mk.Box(pave, g - up * .4f, new Vector3(14f, 1f, 12f), f.r); solid.Box(pave, g - up * .4f, new Vector3(14f, 1f, 12f), f.r); var fo = new PlaceFrame(g + up * .1f, f.Fwd);
            // an open-fronted shop: back wall hung with guitars, a striped roof on chrome posts
            mk.Box(black, fo.P(0, 1.8f, -4f), new Vector3(10f, 3.6f, .3f), f.r); solid.Box(black, fo.P(0, 1.8f, -4f), new Vector3(10f, 3.6f, .3f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(black, fo.P(e * 4.85f, 1.8f, -2.4f), new Vector3(.3f, 3.6f, 3.2f), f.r); solid.Box(black, fo.P(e * 4.85f, 1.8f, -2.4f), new Vector3(.3f, 3.6f, 3.2f), f.r); mk.Prism(chrome, fo.P(e * 4.85f, 0, .6f), up, .08f, 3.6f, 6); ArtistBits.PostCollider(rootT, fo.P(e * 4.85f, 0, .6f), 3.6f, .1f); }
            for (var s = 0; s < 10; s++) mk.Box(WorldMaterials.Solid(s % 2 == 0 ? new Color(1f, .25f, .65f) : new Color(.1f, .1f, .12f), .4f), fo.P(-4.5f + s * 1f, 3.75f, -1.6f), new Vector3(1f, .1f, 5.4f), f.r * Quaternion.Euler(6f, 0, 0));
            ArtistBits.Label(ctx.Root, "SHRED SHED", fo.P(0, 4.6f, .9f), f.Fwd, .6f, Cyan, TextAnchor.MiddleCenter); mk.Box(black, fo.P(0, 4.6f, .8f), new Vector3(6f, 1.2f, .14f), f.r); halos.Add(fo.P(0, 4.6f, 1.3f), 2.6f, Cyan, .5f);
            // guitars on the wall: take one down, carry it, throw it (the amp out front says SMASH HERE)
            Color[] gc = { Pink, Cyan, Gold, new Color(.95f, .95f, .95f), new Color(.85f, .15f, .15f), Violet };
            for (var k = 0; k < 6; k++)
            {
                var gm = new MeshKit(); var body = WorldMaterials.Solid(gc[k], .7f);
                if (k % 2 == 0) { gm.Box(body, new Vector3(0, 0, 0), new Vector3(.12f, .5f, .05f), Quaternion.Euler(0, 0, 35f)); gm.Box(body, new Vector3(0, 0, 0), new Vector3(.12f, .5f, .05f), Quaternion.Euler(0, 0, -35f)); } else { gm.Box(body, Vector3.zero, new Vector3(.34f, .42f, .05f), Quaternion.Euler(0, 0, 12f)); gm.Box(body, new Vector3(.14f, .22f, 0), new Vector3(.1f, .22f, .05f), Quaternion.Euler(0, 0, -20f)); }
                gm.Box(WorldMaterials.Solid(new Color(.3f, .2f, .15f)), new Vector3(0, .55f, 0), new Vector3(.06f, .75f, .03f), Quaternion.identity); gm.Box(body, new Vector3(0, .98f, 0), new Vector3(.1f, .16f, .03f), Quaternion.Euler(0, 0, 15f));
                var go = gm.BuildSingle("Guitar", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(-3.75f + k * 1.5f, 1.6f, -3.75f), f.r); var c = Interact.Carry(go, "a " + new[] { "pink flying V", "cyan superstrat", "gold flying V", "white superstrat", "red flying V", "violet superstrat" }[k], 1.5f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); bc.size = new Vector3(.4f, 1.3f, .1f); bc.center = new Vector3(0, .35f, 0);
                var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true;   // hangs on the wall until someone takes it
            }
            mk.Box(chrome, fo.P(0, 1.2f, -3.82f), new Vector3(9.4f, .04f, .06f), f.r);
            var amp = fo.P(3f, 0, 3.4f); mk.Box(black, amp + up * .9f, new Vector3(1.8f, 1.8f, .9f), f.r); mk.Box(leopard, amp + up * .8f + f.Fwd * .46f, new Vector3(1.5f, 1.3f, .02f), f.r); mk.Box(chrome, amp + up * 1.65f + f.Fwd * .46f, new Vector3(1.5f, .14f, .02f), f.r); Col(amp + up * .9f, new Vector3(1.8f, 1.8f, .9f), f.r); ArtistBits.Label(ctx.Root, "SMASH HERE", amp + up * 2.1f, f.Fwd, .2f, Gold, TextAnchor.MiddleCenter);
            // the counter and the owner, a stool
            mk.Box(leopard, fo.P(-3f, .5f, -1.4f), new Vector3(2.4f, 1f, .7f), f.r); mk.Box(chrome, fo.P(-3f, 1.03f, -1.4f), new Vector3(2.5f, .06f, .8f), f.r); Col(fo.P(-3f, .55f, -1.4f), new Vector3(2.5f, 1.1f, .8f), f.r);
            Folk(fo.P(-3f, 0, -2.4f), f.Fwd, Resident.Pose.Stand, new[] { "Take one down. Go on. They're happier flying.", "That one's been smashed nine times. Glue's the strongest part of it now.", "No Stairway. Shop rule. Different land entirely." }, Rocker(Residents.Tool.None, true));
            var st = fo.P(-1.2f, 0, -.4f); mk.Prism(chrome, st, up, .05f, .7f, 5); mk.Prism(black, st + up * .7f, up, .22f, .08f, 10); ArtistBits.PostCollider(rootT, st, .75f, .2f); Interact.Seat(plife.transform, st + up * .8f, f.Fwd, "Sit and try one out");
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.9f, -1f), new Color(.7f, .9f, 1f), 13f, 2f);
            LeopardBench(mk, f.P(-5.2f, 0, 4.6f), f.Fwd);
            Lane(ctx, mk, f.P(0, 0, 6f)); Ring(g, 9f);
            Shot(ctx, "the shred shed", f.P(-6f, 2.6f, 12f), fo.P(0, 2f, -2f));
        }

        // ---------------------------------------------------------------- Makeout Point
        void MakeoutPoint(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PointAt.x, PointAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            // giant white letters on the seaward edge, facing back up the road: you read them all the way down
            const string word = "BIG HAIR"; var whiteM = WorldMaterials.Glow(new Color(.97f, .96f, .98f), .35f); var x0 = -(word.Length - 1) * 2.2f / 2f;
            for (var k = 0; k < word.Length; k++)
            {
                if (word[k] == ' ') continue; var lp = G(f.P(-(x0 + k * 2.2f), 0, -8f)); var o = lp + up * .2f; var r = f.r * Quaternion.Euler(0, 180f, 0);   // turned to face up the road, so it reads the right way round
                void Bar(float x, float y, float w, float h, float tilt = 0f) => mk.Box(whiteM, o + r * new Vector3(x, y, 0), new Vector3(w, h, .3f), r * Quaternion.Euler(0, 0, tilt));
                switch (word[k])
                {
                    case 'B': Bar(-.6f, 2f, .4f, 4f); Bar(0, 3.8f, 1.2f, .4f); Bar(0, 2f, 1.2f, .4f); Bar(0, .2f, 1.2f, .4f); Bar(.6f, 2.9f, .4f, 1.5f); Bar(.6f, 1.1f, .4f, 1.5f); break;
                    case 'I': Bar(0, 2f, .45f, 4f); break;
                    case 'G': Bar(-.6f, 2f, .4f, 4f); Bar(0, 3.8f, 1.6f, .4f); Bar(0, .2f, 1.6f, .4f); Bar(.6f, 1f, .4f, 1.6f); Bar(.35f, 1.9f, .9f, .4f); break;
                    case 'H': Bar(-.6f, 2f, .4f, 4f); Bar(.6f, 2f, .4f, 4f); Bar(0, 2f, 1.2f, .4f); break;
                    case 'A': Bar(-.5f, 2f, .4f, 4.1f, -14f); Bar(.5f, 2f, .4f, 4.1f, 14f); Bar(0, 1.5f, 1f, .4f); break;
                    case 'R': Bar(-.6f, 2f, .4f, 4f); Bar(0, 3.8f, 1.2f, .4f); Bar(0, 2.1f, 1.2f, .4f); Bar(.6f, 2.95f, .4f, 1.5f); Bar(.3f, 1f, .4f, 2.2f, 22f); break;
                }
                foreach (var e in new[] { -.6f, .6f }) mk.Box(chrome, o + r * new Vector3(e * .5f, 1.6f, .9f), new Vector3(.08f, 3.6f, .08f), r * Quaternion.Euler(28f, 0, 0)); Col(o + up * 2f, new Vector3(1.8f, 4f, .4f), r);
            }
            ArtistBits.PointLight(ctx.Root, G(f.P(0, 0, -5f)) + up * 2f, new Color(1f, .9f, .95f), 26f, 2.2f); halos.Add(G(f.P(0, 0, -7.6f)) + up * 2.2f, 7f, new Color(1f, .85f, .95f), .25f);
            // the convertible, nose to the view, radio on; a bench; a coin telescope
            var cp = G(f.P(-4f, 0, -1f)); var cd = (-f.Fwd - f.Right * .3f).normalized; var cr = Quaternion.LookRotation(cd); var cs = Vector3.Cross(up, cd); var body = WorldMaterials.Solid(new Color(1f, .25f, .65f), .8f);
            mk.Box(body, cp + up * .65f, new Vector3(1.9f, .6f, 4.6f), cr); mk.Box(black, cp + up * .9f - cd * .4f, new Vector3(1.6f, .12f, 1.6f), cr); mk.Box(GlowM(new Color(.7f, .85f, 1f), .3f), cp + up * 1.15f + cd * .6f, new Vector3(1.7f, .5f, .05f), cr * Quaternion.Euler(-20f, 0, 0));
            foreach (var e in new[] { -1f, 1f }) { foreach (var z in new[] { -1.5f, 1.5f }) mk.Prism(black, cp + cs * e * .86f + cd * z + up * .38f, cs * e, .38f, .22f, 10); mk.Box(GlowM(new Color(1f, .95f, .8f), 2.4f), cp + up * .7f + cd * 2.31f + cs * e * .65f, new Vector3(.3f, .16f, .03f), cr); mk.Box(body, cp + up * .95f - cd * 2.1f + cs * e * .9f, new Vector3(.08f, .5f, .9f), cr); }
            solid.Box(body, cp + up * .45f, new Vector3(1.9f, .9f, 4.6f), cr); foreach (var e in new[] { -.42f, .42f }) Interact.Seat(plife.transform, cp + up * 1f - cd * .4f + cs * e, cd, "Sit in the convertible");
            var rgo = new GameObject("Car radio"); rgo.transform.SetParent(plife.transform, false); rgo.transform.position = cp + up * 1.05f + cd * .35f; string[] radio = { "The radio finds a power ballad. Of course it does.", "Static, then a guitar solo already in progress.", "\"...that was twenty minutes of commercial-free rock...\"" }; var rn = 0;
            Interact.Toy(rgo, Toy.Kind.Custom, "Turn the radio dial", new Vector3(.6f, .4f, .5f)).onUse = _ => Say(plife.transform, cp + up * 2.2f, radio[rn++ % radio.Length], 4.5f);
            LeopardBench(mk, f.P(3.5f, 0, -2f), -f.Fwd, 2.2f);
            var tp = G(f.P(6.5f, 0, -3.5f)); mk.Prism(chrome, tp, up, .06f, 1.3f, 6); mk.Prism(chrome, tp + up * 1.35f + f.Fwd * .4f, -f.Fwd, .1f, .8f, 8, .8f); ArtistBits.PostCollider(rootT, tp, 1.4f, .1f);
            var tgo = new GameObject("Coin telescope"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = tp + up * 1.3f; string[] see = { "The Strip, end to end, a line of neon in the dark.", "A motorbike's headlight crawling along the Loop.", "Out at sea, something big and pink. Probably an island. Probably." }; var sn = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Look through the telescope", new Vector3(.8f, .8f, .8f)).onUse = _ => Say(plife.transform, tp + up * 2.2f, see[sn++ % see.Length], 5f);
            var a1 = Rocker(); var a2 = Rocker(); var cpl = G(f.P(1f, 0, -4.2f)); Folk(cpl - f.Right * .35f, -f.Fwd, Resident.Pose.Stand, new[] { "You can see our whole lives from up here. It's mostly the Strip.", "We come up here to be quiet. It's the only quiet we've got." }, a1); Folk(cpl + f.Right * .35f, -f.Fwd, Resident.Pose.Stand, new[] { "He says the letters were his idea. They were not his idea.", "Sit in the car if you like. It's nobody's. It's everybody's." }, a2);
            ChromeLamp(mk, G(f.P(-8f, 0, 4f)), Violet);
            Ring(g, 11f);
            Shot(ctx, "makeout point", f.P(-3f, 3.2f, 12f), G(f.P(0, 0, -8f)) + up * 2.2f);
        }
    }

    /// The Strip places' moving parts: the salon's dryer hoods, the chapel heart, the wind machine and its streamers.
    public class HairPlacesLife : MonoBehaviour
    {
        public List<(Material m, Color c)> hoods = new List<(Material, Color)>(); public List<Vector3> dryerSeats = new List<Vector3>();
        public Transform heart, fan; public Material heartM; public Light heartLight; public float rang = -99f; public Quaternion fanRot; public bool wind;
        public List<(Transform t, Quaternion rot, float phase)> streamers = new List<(Transform, Quaternion, float)>();
        static readonly Color Pink = new Color(1f, .25f, .65f);
        Transform player; float spin, speed;

        void Update()
        {
            var t = Time.time;
            if (player == null) { var p = FindAnyObjectByType<EverythingWorld.Player.SimplePlayerController>(); if (p != null) player = p.transform; }
            for (var i = 0; i < hoods.Count; i++) { var on = player != null && i < dryerSeats.Count && (player.position - dryerSeats[i]).sqrMagnitude < 1.2f; hoods[i].m.SetColor("_EmissionColor", hoods[i].c * (on ? 3.5f + Mathf.Sin(t * 20f) * .4f : .2f)); }
            if (heart != null) { var age = t - rang; var beat = age < 8f ? 1f + .25f * Mathf.Abs(Mathf.Sin(age * 5f)) * (1f - age / 8f) : 1f + .04f * Mathf.Sin(t * 2f); heart.localScale = new Vector3(beat, beat, .25f); if (heartM != null) heartM.SetColor("_EmissionColor", Pink * (1.6f * beat * beat)); if (heartLight != null) heartLight.intensity = 1.6f * beat * beat; }
            speed = Mathf.MoveTowards(speed, wind ? 1f : 0f, Time.deltaTime * .6f); spin += speed * 900f * Time.deltaTime;
            if (fan != null) fan.rotation = fanRot * Quaternion.Euler(0, 0, spin);
            foreach (var (tr, rot, ph) in streamers) tr.rotation = rot * Quaternion.Euler(Mathf.Lerp(80f, Mathf.Sin(t * 9f + ph) * 12f - 4f, speed), Mathf.Sin(t * 7f + ph * 2f) * 10f * speed, 0);
        }
    }
}
