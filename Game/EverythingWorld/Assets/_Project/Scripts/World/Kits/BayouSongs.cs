using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bayou's deep pass (part of BayouKit): the big bands' songs as places out in the swamp, each reached by
    /// its own path with a hand-painted sign and a lantern, each glowing while its song plays; and a song piece on
    /// every smaller porch.
    /// - Allman Brothers Band: an old convertible parked on a stretch of open road, headlights on ("Jessica"); a
    ///   bench on a rise under a ring of blue lanterns ("Blue Sky"); the sunburst sign over their porch shines for
    ///   "Soulshine".
    /// - Lynyrd Skynyrd: a tall dead cypress with a white bird taking off from the top ("Free Bird"); a little white
    ///   farmhouse with a porch swing, a mailbox and a vegetable patch, its window lit ("Simple Man"); a porch light
    ///   and "HOME" over the gate ("Sweet Home Alabama").
    /// - The Marshall Tucker Band: a railroad crossing with lights that flash ("Can't You See"); a jukebox glowing
    ///   at the end of a dock ("Heard It in a Love Song"); an eagle's nest on a tall snag ("Fly Eagle Fly"); their
    ///   rainbow glows for "Searchin' for a Rainbow".
    /// - The smaller porches: Little Feat's moon for "Spanish Moon", the Outlaws' green tide pool ("Green Grass & High
    ///   Tides"), 38 Special's rope swing lit with lanterns ("Hold On Loosely"), a neon "BAD" sign ("Bad To The Bone"),
    ///   glowing footprints walking away ("Walk Away"), a red clay pot of fire ("Wondering Why").
    public partial class BayouKit
    {
        public const string Jessica = "7AN22wQYwiCG49qYosygFq", BlueSky = "1jjExXOVambZfZwnSgR6qR", Soulshine = "7a014uMHF3nlqzXCxV551C",
            SimpleMan = "1ju7EsSGvRybSNEsRvc7qY", SweetHome = "7e89621JPkKaeDSTQ3avtg", CantYouSee = "4egy2d8QiyXn5tzVQiptTb", LoveSong = "6PoHD6eS4Ux7Hx66Q3RYOb",
            FlyEagle = "6b65o12QbJQuT2V26VshSX", Rainbow = "3QGT3GLyCV16E3vUPkFyD4", SpanishMoon = "24eRthJ3VWBI7B9cuFBdMG", GreenGrass = "2sXtpY9mCTXnUTUSQqrXbX",
            HoldOn = "6i9dZZQWNB06HKsQYKTiPF", BadBone = "6s0NHplywwr1IjnQpUpWJk", WalkAwaySong = "65aeHyw8GdHFwmEvdSns21", WonderingWhy = "1HbzxLqpNVPdiBXvpC7Ovb";

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        readonly List<Vector3> placeSpots = new List<Vector3>();

        bool Gentle(LandContext ctx, float x, float z, float r, float drop)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var k = 0; k < 12; k++) foreach (var f in new[] { .5f, 1f }) { var a = k * Mathf.PI / 6f; var h = ctx.Ground(x + Mathf.Cos(a) * r * f, z + Mathf.Sin(a) * r * f); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo < drop;
        }

        Vector3 PlaceSpot(LandContext ctx, Vector3 near, float clear)
        {
            for (var tries = 0; tries < 900; tries++)
            {
                var wide = tries > 500; var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(30f, wide ? 280f : 130f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(px, pz, clear + 8f) || !Open(ctx, px, pz, clear)) continue;
                if (!Gentle(ctx, px, pz, clear + 4f, tries > 700 ? clear * .4f : clear * .2f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 22f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); noGrass.Add(new Vector3(p.x, clear * .85f, p.z)); return p;
            }
            return Vector3.positiveInfinity;
        }

        Vector3 PlaceWay(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, float clear, string sign, string mark)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(center.x - c.x, 0, center.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var stop = clear - 1f;
            if (best < float.MaxValue && best > stop + 3f)
            {
                var pts = new List<Vector3>(); var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - stop; var wet = false;
                for (var d = RoadW / 2f + .1f; ; d += 1.5f) { var dd = Mathf.Min(d, len); var q = from + dir * dd; if (!ctx.IsLand(q.x, q.z, 0f)) break; if (WaterAt(q.x, q.z, 1f, out _)) wet = true; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count > 1) { if (wet) Bridge(ctx, mk, solid, pts); else Strip(mk, pts, WorldMaterials.Solid(Mud * 1.2f), 2.4f, .06f); foreach (var p in pts) Remember(p, 350); }
            }
            var side = Vector3.Cross(Vector3.up, face); var sp = ctx.OnGround(c.x + face.x * stop + side.x * 2.4f, c.z + face.z * stop + side.z * 2.4f); var rot = Quaternion.LookRotation(face);
            mk.Box(greyWood, sp + Vector3.up * .7f, new Vector3(.2f, 1.4f, .2f), rot); mk.Box(plank, sp + Vector3.up * 1.5f, new Vector3(2.8f, .6f, .1f), rot * Quaternion.Euler(0, 0, 3f));
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.5f + face * .06f, face, .18f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PostCollider(ctx.Root, sp, 1.8f, .2f);
            Lamp(ctx, mk, ctx.Root, ctx.OnGround(c.x + face.x * stop - side.x * 2.4f, c.z + face.z * stop - side.z * 2.4f));
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 6f) - side * 6f; cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.2f; var look = c + Vector3.up * 2.2f - cam - Vector3.up * 1.6f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            placeSpots.Clear(); foreach (var a in ctx.Artists) placeSpots.Add(ctx.OnGround(a.x, a.z));
            Vector3 Near(string n) { var a = ctx.Artists.Find(x => x.name == n); return a != null ? ctx.OnGround(a.x, a.z) : center; }
            var placed = 0;
            void Place(Vector3 near, float clear, string sign, string mark, System.Action<Vector3, Vector3> build)
            {
                var c = PlaceSpot(ctx, near, clear); if (float.IsInfinity(c.x)) { Debug.Log("The Southern Rock Bayou: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, solid, c, clear, sign, mark)); placed++;
            }
            var allman = Near("Allman Brothers Band"); var sky = Near("Lynyrd Skynyrd"); var mt = Near("The Marshall Tucker Band");
            Place(allman, 9f, "JESSICA", "the open road", (c, f) => OpenRoad(ctx, mk, solid, c, f));
            Place(allman, 7f, "BLUE SKY", "the blue sky bench", (c, f) => BlueBench(ctx, mk, solid, c, f));
            Place(sky, 7f, "FREE BIRD", "the free bird", (c, f) => FreeBirdSnag(ctx, mk, solid, c, f));
            Place(sky, 9f, "SIMPLE MAN", "the farmhouse", (c, f) => Farmhouse(ctx, mk, solid, c, f));
            Place(mt, 8f, "CAN'T YOU SEE", "the crossing", (c, f) => Crossing(ctx, mk, solid, c, f));
            Place(mt, 7f, "HEARD IT IN A LOVE SONG", "the dock jukebox", (c, f) => DockJukebox(ctx, mk, solid, c, f));
            Place(mt, 7f, "FLY EAGLE FLY", "the eagle's nest", (c, f) => EagleNest(ctx, mk, solid, c, f));
            Debug.Log($"The Southern Rock Bayou: {placed} song places");
        }

        void OpenRoad(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var dir = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(dir); var asphalt = WorldMaterials.Solid(new Color(.22f, .22f, .24f));
            var pts = new List<Vector3>(); for (var k = -8; k <= 8; k++) { var p = c + dir * k * 1.2f; pts.Add(ctx.OnGround(p.x, p.z)); }
            Strip(mk, pts, asphalt, 5f, .07f); for (var k = -7; k <= 7; k += 2) mk.Box(WorldMaterials.Solid(new Color(.95f, .85f, .3f)), ctx.OnGround(c.x + dir.x * k * 1.2f, c.z + dir.z * k * 1.2f) + up * .09f, new Vector3(.12f, .02f, .9f), rot);
            var paint = WorldMaterials.Solid(new Color(.15f, .35f, .6f), .8f); var car = c + up * .1f; var side = face;
            mk.Box(paint, car + up * .6f, new Vector3(1.9f, .6f, 4.6f), rot); mk.Box(WorldMaterials.Solid(new Color(.9f, .88f, .8f)), car + up * .92f - dir * .4f, new Vector3(1.7f, .12f, 1.6f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), car + up * 1.15f + dir * .6f, new Vector3(1.7f, .45f, .05f), rot * Quaternion.Euler(-25f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) foreach (var f in new[] { -1.5f, 1.5f }) mk.Prism(dark, car + side * s * .95f + dir * f + up * .38f, side * s, .38f, .22f, 12);
            var head = SongGlow(Jessica, new Color(1f, .95f, .75f), 2.6f); foreach (var s in new[] { -.6f, .6f }) mk.Prism(head, car + up * .7f + dir * 2.31f + side * s, dir, .16f, .04f, 10);
            halos.Add(car + up * .7f + dir * 3f, 2f, new Color(1f, .95f, .75f), .45f); solid.Box(paint, car + up * .7f, new Vector3(2f, 1.4f, 4.7f), rot);
        }

        void BlueBench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(-face); var blue = SongGlow(BlueSky, new Color(.35f, .6f, 1f), 2f);
            mk.Box(plank, c + up * .45f, new Vector3(2f, .08f, .5f), rot); mk.Box(plank, c + up * .8f + face * .25f, new Vector3(2f, .5f, .06f), rot); solid.Box(plank, c + up * .5f, new Vector3(2.1f, 1f, .7f), rot);
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 4f, c.z + Mathf.Sin(a) * 4f); mk.Prism(greyWood, p, up, .06f, 2.2f, 6); mk.Prism(blue, p + up * 2.2f, up, .18f, .35f, 8); halos.Add(p + up * 2.4f, 1f, new Color(.35f, .6f, 1f), .7f); }
        }

        void FreeBirdSnag(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; mk.Prism(bark, c - up * .3f, up, .6f, 9f, 8, .3f); ArtistBits.PostCollider(ctx.Root, c, 9f, .6f);
            foreach (var k in new[] { 0, 1, 2 }) { var d = Quaternion.Euler(0, k * 120f + 20f, 0) * Vector3.forward; mk.Prism(bark, c + up * (5f + k), (d + up * .5f).normalized, .12f, 2.4f, 5, .3f); }
            var w = SongGlow(FreeBird, new Color(.95f, .97f, 1f), 1.8f); var b = c + up * 9.4f;
            mk.Blob(w, b, new Vector3(.25f, .2f, .45f), ctx.Rng, 0f); var sd = Vector3.Cross(up, face);
            foreach (var s in new[] { -1f, 1f }) { mk.Tri(w, b, b + sd * s * 1.4f + up * .8f, b + sd * s * 1f - face * .3f + up * .2f, b - up); mk.Tri(w, b, b + sd * s * 1f - face * .3f + up * .2f, b + sd * s * 1.4f + up * .8f, b + up); }
            halos.Add(b, 2.4f, new Color(.95f, .97f, 1f), .4f);
        }

        void Farmhouse(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var b = c - face * 1.5f;
            mk.Box(white, b + up * 1.7f, new Vector3(6f, 3.4f, 5f), rot); solid.Box(white, b + up * 1.7f, new Vector3(6f, 3.4f, 5f), rot);
            mk.GableRoof(tin, b + up * 3.43f, new Vector3(6.6f, 0, 5.6f), 1.8f, Quaternion.LookRotation(side));
            var fr = b + face * 2.52f; var win = SongGlow(SimpleMan, new Color(1f, .8f, .45f), 1.6f);
            foreach (var s in new[] { -1.7f, 1.7f }) mk.Box(win, fr + up * 1.8f + side * s, new Vector3(.9f, .9f, .03f), rot); mk.Box(dark, fr + up * 1.1f, new Vector3(1f, 2.1f, .03f), rot);
            mk.Box(plank, fr + face * 1.1f + up * .2f, new Vector3(6f, .4f, 2.2f), rot); solid.Box(plank, fr + face * 1.1f + up * .2f, new Vector3(6f, .4f, 2.2f), rot);
            foreach (var s in new[] { -2.8f, 2.8f }) { mk.Box(greyWood, fr + face * 2f + side * s + up * 1.6f, new Vector3(.15f, 2.4f, .15f), rot); ArtistBits.PostCollider(ctx.Root, fr + face * 2f + side * s, 2.8f, .12f); }
            mk.Box(tin, fr + face * 1.2f + up * 2.85f, new Vector3(6.4f, .08f, 2.6f), rot * Quaternion.Euler(10f, 0, 0));
            var sw = fr + face * 1.4f + side * 1.6f + up * .4f; mk.Box(plank, sw + up * .5f, new Vector3(1.6f, .08f, .5f), rot); foreach (var s in new[] { -.75f, .75f }) mk.Box(rope, sw + up * 1.5f + side * s, new Vector3(.03f, 2f, .03f), rot);
            var home = SongGlow(SweetHome, new Color(1f, .85f, .5f), 2.4f); mk.Blob(home, fr + face * 2f + up * 2.4f - side * 1.6f, Vector3.one * .12f, ctx.Rng, 0f); halos.Add(fr + face * 2f + up * 2.4f - side * 1.6f, 1.1f, new Color(1f, .85f, .5f), .7f);
            var mb = c + face * 4.5f - side * 3.4f; mb = ctx.OnGround(mb.x, mb.z); mk.Prism(greyWood, mb, up, .06f, 1.1f, 6); mk.Box(metal, mb + up * 1.2f, new Vector3(.3f, .3f, .5f), rot); ArtistBits.PostCollider(ctx.Root, mb, 1.2f, .1f);
            for (var r = 0; r < 3; r++) for (var k = 0; k < 6; k++) { var p = b - face * 4.5f + side * (k - 2.5f) * .8f + face * (r * .9f); p = ctx.OnGround(p.x, p.z); soft.Blob(leaf, p + up * .3f, new Vector3(.3f, .3f, .3f), ctx.Rng, .3f); }
            ArtistBits.Label(ctx.Root, "HOME", fr + face * 2.05f + up * 2.55f, face, .22f, new Color(.95f, .9f, .75f), TextAnchor.MiddleCenter);
        }

        void Crossing(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var dir = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(dir);
            for (var k = -7; k <= 7; k++) mk.Box(bark, ctx.OnGround(c.x + dir.x * k * 1.1f, c.z + dir.z * k * 1.1f) + up * .08f, new Vector3(2.6f, .16f, .3f), Quaternion.LookRotation(face));
            foreach (var s in new[] { -.75f, .75f }) mk.Box(metal, c + face * s + up * .24f, new Vector3(.1f, .12f, 16f), rot);
            var red = SongGlow(CantYouSee, new Color(1f, .15f, .1f), 2.6f); life.flashers.Add(red);
            foreach (var s in new[] { -1f, 1f })
            {
                var p = c + face * s * 2.6f + dir * s * 1.6f; p = ctx.OnGround(p.x, p.z); mk.Prism(white, p, up, .08f, 3.4f, 6); ArtistBits.PostCollider(ctx.Root, p, 3.4f, .12f);
                foreach (var a in new[] { 40f, -40f }) mk.Box(white, p + up * 3f, new Vector3(1.6f, .2f, .04f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, a));
                mk.Box(dark, p + up * 2.2f, new Vector3(1.2f, .4f, .2f), Quaternion.LookRotation(face));
                foreach (var e in new[] { -.4f, .4f }) mk.Prism(red, p + up * 2.2f + Vector3.Cross(up, face) * e + face * s * .11f, face * s, .14f, .04f, 10);
            }
        }

        void DockJukebox(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Box(plank, c + up * .3f, new Vector3(3f, .2f, 6f), rot); solid.Box(plank, c + up * .25f, new Vector3(3f, .3f, 6f), rot);
            foreach (var s in new[] { -1.3f, 1.3f }) foreach (var f in new[] { -2.6f, 0f, 2.6f }) mk.Prism(greyWood, c + Vector3.Cross(up, face) * s + face * f - up * .5f, up, .12f, 1f, 6);
            var jb = c - face * 2f + up * .4f; var box = WorldMaterials.Solid(new Color(.55f, .25f, .15f), .7f);
            mk.Box(box, jb + up * .9f, new Vector3(1.4f, 1.8f, .8f), rot); mk.Prism(box, jb + up * 1.8f, face, .7f, .8f, 14); solid.Box(box, jb + up * 1.2f, new Vector3(1.5f, 2.6f, .9f), rot);
            var g = SongGlow(LoveSong, new Color(1f, .4f, .65f), 2f); var g2 = SongGlow(LoveSong, new Color(1f, .8f, .35f), 2f);
            for (var k = 0; k < 9; k++) { var a = (k / 8f) * Mathf.PI; mk.Box(k % 2 == 0 ? g : g2, jb + up * 1.8f + face * .41f + (Vector3.Cross(up, face) * Mathf.Cos(a) + up * Mathf.Sin(a)) * .6f, new Vector3(.12f, .12f, .02f), rot); }
            halos.Add(jb + up * 1.6f + face * .6f, 1.8f, new Color(1f, .55f, .55f), .4f);
        }

        void EagleNest(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; mk.Prism(bark, c - up * .3f, up, .5f, 8f, 7, .35f); ArtistBits.PostCollider(ctx.Root, c, 8f, .5f);
            var nest = c + up * 7.8f; for (var k = 0; k < 14; k++) { var a = k * Mathf.PI / 7f; mk.Box(bark, nest + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .9f, new Vector3(.1f, .25f, 1.2f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            var brown = WorldMaterials.Solid(new Color(.35f, .25f, .15f)); var head = SongGlow(FlyEagle, new Color(1f, .97f, .9f), 1.4f); var sd = Vector3.Cross(up, face);
            mk.Blob(brown, nest + up * .5f, new Vector3(.35f, .45f, .3f), ctx.Rng, 0f); mk.Blob(head, nest + up * 1.05f + face * .1f, Vector3.one * .18f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) { mk.Tri(brown, nest + up * .6f, nest + up * 1.6f + sd * s * 1.8f, nest + up * .5f + sd * s * 1.1f - face * .3f, face); mk.Tri(brown, nest + up * .6f, nest + up * .5f + sd * s * 1.1f - face * .3f, nest + up * 1.6f + sd * s * 1.8f, -face); }
            halos.Add(nest + up * 1f, 1.6f, new Color(1f, .95f, .85f), .4f);
        }

        /// A song piece on each smaller porch.
        void SmallSongs(Spot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.front);
            switch (z.a.name)
            {
                case "The Outlaws": { var p = z.At(4f, 6f); var g = SongGlow(GreenGrass, new Color(.3f, 1f, .5f), 1f); for (var k = 0; k < 16; k++) { float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f; z.mk.Tri(g, p + up * .05f, p + up * .05f + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 1.6f, p + up * .05f + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 1.6f, p - up); } halos.Add(p + up * .4f, 2f, new Color(.3f, 1f, .5f), .3f); break; }
                case "38 Special": { var g = SongGlow(HoldOn, new Color(1f, .8f, .4f), 2.4f); for (var k = 0; k < 5; k++) { var p = z.At(3f, -4f + k * 2f) + up * 2.6f; z.mk.Blob(g, p, Vector3.one * .12f, ctx.Rng, 0f); halos.Add(p, .8f, new Color(1f, .8f, .4f), .6f); } break; }
                case "George Thorogood & The Destroyers": { var p = z.At(4f, 5f); z.mk.Prism(greyWood, p, up, .08f, 2.4f, 6); ArtistBits.PostCollider(z.root, p, 2.4f, .1f); var g = SongGlow(BadBone, new Color(1f, .2f, .15f), 2.4f); z.mk.Box(dark, p + up * 2.6f, new Vector3(1.6f, .7f, .1f), rot); z.mk.Box(g, p + up * 2.6f + z.front * .06f + up * .28f, new Vector3(1.4f, .06f, .02f), rot); z.mk.Box(g, p + up * 2.6f + z.front * .06f - up * .28f, new Vector3(1.4f, .06f, .02f), rot); ArtistBits.Label(z.labels, "BAD", p + up * 2.6f + z.front * .07f, z.front, .36f, new Color(1f, .3f, .2f), TextAnchor.MiddleCenter); break; }
                case "James Gang": { var g = SongGlow(WalkAwaySong, new Color(.6f, .85f, 1f), 1.6f); for (var k = 0; k < 8; k++) { var p = z.At(2f - k * .9f, 4f + (k % 2) * .4f); z.mk.Blob(g, p + up * .05f, new Vector3(.12f, .02f, .25f), ctx.Rng, 0f); } break; }
                case "The Red Clay Strays": { var p = z.At(4f, 5f); var clay = WorldMaterials.Solid(new Color(.65f, .3f, .2f)); z.mk.Prism(clay, p, up, .7f, .9f, 12, 1.2f); z.mk.Blob(SongGlow(WonderingWhy, new Color(1f, .5f, .15f), 2.6f), p + up * 1f, new Vector3(.5f, .4f, .5f), ctx.Rng, .2f); life.fires.Add(p + up * 1f); ArtistBits.PostCollider(z.root, p, 1f, .8f); break; }
            }
        }

        /// Inside a porch house: a braided rug, a sagging sofa against one wall, a little table with an oil lamp, three
        /// guitars hung on the back wall, a framed gold record, an old radio.
        void Interior(Spot z, Vector3 floor, float W, float D, Quaternion rot)
        {
            var up = Vector3.up; var ctx = z.ctx; var back = floor - z.front * (D / 2f - .25f);
            z.mk.Box(WorldMaterials.Solid(new Color(.55f, .25f, .2f)), floor + up * .015f, new Vector3(W * .5f, .03f, D * .45f), rot);
            var sofa = floor + z.side * (W / 2f - .8f); var srot = Quaternion.LookRotation(-z.side); var cloth = WorldMaterials.Solid(new Color(.35f, .45f, .4f));
            z.mk.Box(cloth, sofa + up * .3f, new Vector3(2.2f, .6f, .9f), srot); z.mk.Box(cloth, sofa + up * .75f + z.side * .35f, new Vector3(2.2f, .7f, .2f), srot); z.solid.Box(cloth, sofa + up * .4f, new Vector3(2.2f, .8f, .9f), srot);
            var tbl = floor - z.side * (W / 2f - 1f) - z.front * .5f; z.mk.Box(plank, tbl + up * .7f, new Vector3(.9f, .06f, .9f), rot); z.mk.Prism(plank, tbl, up, .06f, .7f, 6); z.solid.Box(plank, tbl + up * .4f, new Vector3(.9f, .8f, .9f), rot);
            z.mk.Prism(GlowM(new Color(1f, .75f, .4f), 2.4f), tbl + up * .76f, up, .1f, .25f, 8); halos.Add(tbl + up * .95f, .8f, new Color(1f, .75f, .4f), .7f);
            Color[] paints = { new Color(.7f, .45f, .2f), new Color(.8f, .2f, .15f), new Color(.15f, .12f, .1f) };
            for (var k = 0; k < 3; k++) { var g = back + z.front * .08f + z.side * ((k - 1f) * .9f) + up * 1.7f; z.mk.Blob(WorldMaterials.Solid(paints[k], .7f), g, new Vector3(.3f, .38f, .07f), ctx.Rng, 0f); z.mk.Box(dark, g + up * .62f, new Vector3(.05f, .75f, .04f), rot); }
            var rec = back + z.front * .05f + z.side * 2.2f + up * 2.2f; z.mk.Box(plank, rec, new Vector3(.8f, .8f, .04f), rot); z.mk.Prism(GlowM(new Color(1f, .8f, .35f), 1.4f), rec + z.front * .02f, z.front, .28f, .02f, 16);
            var radio = back + z.front * .4f - z.side * 1.8f; z.mk.Box(plank, radio + up * .5f, new Vector3(.7f, 1f, .45f), rot); z.mk.Prism(GlowM(new Color(1f, .8f, .45f), 1.6f), radio + up * .7f + z.front * .23f, z.front, .12f, .02f, 12);
        }
    }
}
