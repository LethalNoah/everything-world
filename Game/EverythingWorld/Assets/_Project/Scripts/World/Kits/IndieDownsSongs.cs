using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Downs' deep pass (part of IndieDownsKit): songs as places and pieces, each glowing while its song plays.
    /// - Florence Road, places for all five songs, each on its own chalk path with a sign: rabbits by a pond, one
    ///   swimming ("Rabbits Can Swim"); a weather mast flying storm flags under a little rain cloud ("Storm Warnings");
    ///   an empty swing under a lone lamp ("Miss"); a front door standing alone on the grass with its number lit
    ///   ("7563"); sheets on a washing line ("Hanging Out To Dry").
    /// - Death Cab for Cutie: a little stone tower ("I Built You A Tower"), a trap door glowing in the turf ("Trap
    ///   Door"), a lantern globe full of stars ("Full of Stars"). Rainbow Kitten Surprise: a little rocket ("Mission to
    ///   Mars"), a hedge with eyes peeping out ("Hide").
    /// - Every smaller artist a piece from their song by their area (see SmallSongPiece).
    public partial class IndieDownsKit
    {
        DownsLife downsLife;
        readonly List<(string song, Material m, Color c)> pendingGlows = new List<(string, Material, Color)>();
        readonly List<Vector3> placeSpots = new List<Vector3>();

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(WorldMaterials.Glow(c, s * .65f)) { name = "Song glow" };
            if (downsLife != null) downsLife.songGlows.Add((song, m, c * (s * .65f))); else pendingGlows.Add((song, m, c * (s * .65f)));
            return m;
        }

        /// The song each smaller artist's piece answers to (their first song on the playlist).
        static readonly Dictionary<string, string> PieceSong = new Dictionary<string, string> { { "Cusk", "4UMjHRPZAi5LgR8xhYShqc" }, { "Dummy", "7oEDqMCCe6BJrCYDVi8kYQ" }, { "Interpol", "1zNeVf6Qk402L4Urb7CcG8" }, { "Milky Chance", "6vECYJHxYmm3Ydt3fF01pE" }, { "Pavement", "3qRnjMR3Cb61sfFuLw43oZ" }, { "12 RODS", "28VtGohHzjEeWTWt0wqjgV" }, { "Arcade Fire", "5iItYl3Q6wCnKVfpK1uNVf" }, { "Broken Social Scene", "03gKdAQAswBo7v4p8F1An4" }, { "Cold War Kids", "3IXRdLsQib8m0hBbQpe7T8" }, { "Current Joys", "0tYDtbOBF1v8qM1ZPyPlv3" }, { "Elio Mei", "0uEeF2vQCTu4ucShvarTIH" }, { "Francis of Delirium", "5f1MV72uMrrUVQcsohJOKj" }, { "Franz Ferdinand", "20I8RduZC2PWMWTDCZuuAN" }, { "Gigi Perez", "2262bWmqomIaJXwCRHr13j" }, { "Grizzly Bear", "04uR2cdkgiaLIzebPcs25m" }, { "Guster", "14j4diRlUSWvbq5t0rgR6b" }, { "Inner Wave", "69Tf2qK9sYOi1ncnXrfIUV" }, { "Korea Girl", "2WGF3TCRMp8ip5gnAvtUod" }, { "Mac DeMarco", "6942romFscvCKkkrTwkAvD" }, { "Manchester Orchestra", "2i8fcnTT5iItxMNwa8mfr3" }, { "Modest Mouse", "5piZxrb8wpeYFr9gVcwfZl" }, { "Rio Romeo", "0EF1EE8zusg3Y869e56JFd" }, { "Sam Fender", "5rF6YUIlgiat22OT1lWspJ" }, { "The 1975", "6WmIyn2fx1PKQ0XDpYj4VR" }, { "The Drums", "3GbhZOGi7PIDNhtTlPHPk2" }, { "The Folk Implosion", "3KtjOIeBcB9teZrtjJuFQy" }, { "Veronica Everheart", "1DbndbCRrMLxCwRWzUHZP8" }, { "Wolf Alice", "2Q7EmuVCLEY72iw7klTdPA" }, { "Yellow Days", "7l5j3FapCyr6HxUgoAynM2" } };
        string Song(IslandWorldData.Artist a, int i) => PieceSong.TryGetValue(a.name, out var id) ? id : null;

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
                var wide = tries > 500; var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(26f, wide ? 260f : 110f);
                var o = wide ? ctx.Plaza : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(px, pz, clear + 6f) || !Open(ctx, px, pz, clear)) continue;
                if (!Gentle(ctx, px, pz, clear + 3f, tries > 700 ? clear * .45f : clear * .25f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 18f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); noGrass.Add(new Vector3(p.x, clear * .85f, p.z)); ctx.AddKeepout(p.x, p.z, clear); return p;
            }
            return Vector3.positiveInfinity;
        }

        Vector3 PlaceWay(LandContext ctx, MeshKit mk, MeshKit pictures, Vector3 c, float clear, string sign, string mark)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var cell in pathCells.Values) foreach (var p in cell) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(ctx.Plaza.x - c.x, 0, ctx.Plaza.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var stop = clear - 1f;
            if (best < float.MaxValue && best > stop + 3f)
            {
                var pts = new List<Vector3>(); var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - stop;
                for (var d = 1f; ; d += 1.5f) { var dd = Mathf.Min(d, len); var q = from + dir * dd; if (!ctx.IsLand(q.x, q.z, 0f)) break; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count > 1) Ribbon(ctx, mk, chalk, pts, 1.6f, .05f);
                foreach (var p in pts) { var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f)); if (!pathCells.TryGetValue(key, out var cell)) pathCells[key] = cell = new List<Vector3>(); cell.Add(p); }   // so the fields and grass keep off it
            }
            var side = Vector3.Cross(Vector3.up, face); var sp = ctx.OnGround(c.x + face.x * stop + side.x * 2.2f, c.z + face.z * stop + side.z * 2.2f); var rot = Quaternion.LookRotation(face);
            mk.Box(darkWood, sp + Vector3.up * .6f, new Vector3(.12f, 1.2f, .12f), rot); mk.Box(cream, sp + Vector3.up * 1.35f, new Vector3(2.4f, .5f, .06f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.35f + face * .04f, face, .16f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PostCollider(ctx.Root, sp, 1.6f, .15f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 6f) - side * 6f; cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.2f; var look = c + Vector3.up * 1.8f - cam - Vector3.up * 1.6f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit pictures)
        {
            placeSpots.Clear(); foreach (var a in ctx.Artists) placeSpots.Add(ctx.OnGround(a.x, a.z));
            Vector3 Near(string n) { var a = ctx.Artists.Find(x => x.name == n); return a != null ? ctx.OnGround(a.x, a.z) : ctx.Plaza; }
            string Id(string artist, int i) { var a = ctx.Artists.Find(x => x.name == artist); return a != null ? Song(a, i) : null; }
            var placed = 0;
            void Place(Vector3 near, float clear, string sign, string mark, System.Action<Vector3, Vector3> build)
            {
                var c = PlaceSpot(ctx, near, clear); if (float.IsInfinity(c.x)) { Debug.Log("The Indie Downs: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, pictures, c, clear, sign, mark)); placed++;
            }
            var fr = Near("Florence Road"); var dc = Near("Death Cab for Cutie"); var rks = Near("Rainbow Kitten Surprise");
            Place(fr, 7f, "RABBITS CAN SWIM", "the rabbit pond", (c, f) => RabbitPond(ctx, mk, solid, c, f, "2TskEBtlcUyqqnVR2acmlt"));
            Place(fr, 7f, "STORM WARNINGS", "the storm mast", (c, f) => StormMast(ctx, mk, solid, c, f, "6BsVqDvy4QQY8C9FyCWaMK"));
            Place(fr, 6f, "MISS", "the empty swing", (c, f) => EmptySwing(ctx, mk, solid, c, f, "5MnePlSJRO9C1oTOnqZQ92"));
            Place(fr, 6f, "7563", "the lone door", (c, f) => LoneDoor(ctx, mk, solid, c, f, "2vJPGLhq3pub8fG9wAFN0Y"));
            Place(fr, 7f, "HANGING OUT TO DRY", "the washing line", (c, f) => WashingLine(ctx, mk, solid, c, f, "1GZ5KqHOdkU0cZoz1JfQo8"));
            Place(dc, 7f, "I BUILT YOU A TOWER", "the little tower", (c, f) => LittleTower(ctx, mk, solid, c, f, "1vUnfHvpynWRlkUXNOycmR"));
            Place(dc, 6f, "TRAP DOOR", "the trap door", (c, f) => TrapDoor(ctx, mk, solid, c, f, "5gON9YZynswoz8VSLufgKh"));
            Place(dc, 6f, "FULL OF STARS", "the star globe", (c, f) => StarGlobe(ctx, mk, solid, c, f, "4x5AM5TMAVC9gFcVV14lWW"));
            Place(rks, 7f, "MISSION TO MARS", "the little rocket", (c, f) => LittleRocket(ctx, mk, solid, c, f, "1TDdYSYLIXMJ4EiKO6ZCYk"));
            Place(rks, 6f, "HIDE", "the peeping hedge", (c, f) => PeepingHedge(ctx, mk, solid, c, f, "0hJS3EHWC4uL9sJ52ErpoN"));
            Debug.Log($"The Indie Downs: {placed} song places");
        }

        void RabbitPond(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var water = SongGlow(id, new Color(.35f, .6f, .9f), .7f);
            for (var k = 0; k < 18; k++) { float a0 = k * Mathf.PI / 9f, a1 = (k + 1) * Mathf.PI / 9f; mk.Tri(water, c + up * .05f, c + up * .05f + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 3f, c + up * .05f + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 3f, c - up); }
            var fur = WorldMaterials.Solid(new Color(.7f, .62f, .52f)); var white = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
            void Rabbit(Vector3 p, Vector3 d, bool swim)
            {
                var r = Quaternion.LookRotation(d); mk.Blob(fur, p + up * (swim ? .05f : .2f), new Vector3(.2f, .18f, .28f), ctx.Rng, 0f); mk.Blob(fur, p + up * (swim ? .2f : .38f) + d * .22f, Vector3.one * .12f, ctx.Rng, 0f);
                foreach (var s in new[] { -.05f, .05f }) mk.Box(fur, p + up * (swim ? .38f : .56f) + d * .2f + Vector3.Cross(up, d) * s, new Vector3(.04f, .22f, .06f), r);
                if (!swim) mk.Blob(white, p + up * .22f - d * .28f, Vector3.one * .06f, ctx.Rng, 0f);
            }
            Rabbit(c + up * .05f, face, true);
            for (var k = 0; k < 3; k++) { var a = 1.2f + k * .7f; Rabbit(ctx.OnGround(c.x + Mathf.Cos(a) * 3.8f, c.z + Mathf.Sin(a) * 3.8f), new Vector3(-Mathf.Cos(a), 0, -Mathf.Sin(a)), false); }
            for (var k = 0; k < 10; k++) { var a = k * .63f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 3.3f, c.z + Mathf.Sin(a) * 3.3f); mk.Box(WorldMaterials.Solid(new Color(.4f, .55f, .3f)), p + up * .4f, new Vector3(.05f, .8f, .05f), Quaternion.Euler(ctx.Range(-10f, 10f), 0, ctx.Range(-10f, 10f))); }
        }

        void StormMast(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; mk.Prism(white(), c, up, .1f, 7f, 6); ArtistBits.PostCollider(ctx.Root, c, 7f, .15f);
            mk.Box(white(), c + up * 6.2f, new Vector3(2.4f, .08f, .08f), Quaternion.LookRotation(face));
            var flag = SongGlow(id, new Color(1f, .3f, .15f), 1.6f); var side = Vector3.Cross(up, face);
            foreach (var s in new[] { -1f, 1f }) { var p = c + up * 5.6f + side * s * 1f; mk.Tri(flag, p + up * .5f, p - up * .5f, p + side * s * .9f, p + face); mk.Tri(flag, p - up * .5f, p + up * .5f, p + side * s * .9f, p - face); mk.Box(dark(), p, new Vector3(.25f, .25f, .02f), Quaternion.LookRotation(face)); }
            var cloud = WorldMaterials.Solid(new Color(.45f, .47f, .52f)); var cc = c + up * 9.5f + face * 2f;
            for (var k = 0; k < 5; k++) mk.Blob(cloud, cc + side * ((k - 2f) * .9f) + up * Mathf.Sin(k * 1.3f) * .3f, new Vector3(1.1f, .7f, .9f), ctx.Rng, .2f);
            for (var k = 0; k < 14; k++) mk.Box(SongGlow(id, new Color(.55f, .75f, 1f), 1.2f), cc - up * (1.2f + (k % 5) * 1.2f) + side * ((k * 1.37f) % 3.6f - 1.8f) + face * ((k * .71f) % 1.4f - .7f), new Vector3(.03f, .4f, .03f), Quaternion.identity);
        }
        Material white() => cream; Material dark() => darkWood;

        void EmptySwing(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            foreach (var s in new[] { -1.2f, 1.2f }) { mk.Box(darkWood, c + side * s + up * 1.4f, new Vector3(.12f, 2.9f, .12f), rot * Quaternion.Euler(0, 0, s > 0 ? -6f : 6f)); ArtistBits.PostCollider(ctx.Root, c + side * s, 2.8f, .12f); }
            mk.Box(darkWood, c + up * 2.8f, new Vector3(2.8f, .12f, .12f), rot);
            foreach (var s in new[] { -.4f, .4f }) mk.Box(iron, c + side * s + up * 1.75f, new Vector3(.02f, 2.1f, .02f), rot);
            mk.Box(wood, c + up * .7f, new Vector3(1f, .06f, .35f), rot);
            var lamp = c + face * 2f + side * 2f; lamp = ctx.OnGround(lamp.x, lamp.z); mk.Prism(iron, lamp, up, .06f, 3.2f, 6); mk.Box(SongGlow(id, Lamp, 2.4f), lamp + up * 3.3f, new Vector3(.3f, .4f, .3f), rot); halos.Add(lamp + up * 3.3f, 1.4f, Lamp, .7f); ArtistBits.PostCollider(ctx.Root, lamp, 3f, .1f);
        }

        void LoneDoor(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(brick, c + up * 1.4f, new Vector3(2.4f, 2.8f, .3f), rot); solid.Box(brick, c + up * 1.4f, new Vector3(2.4f, 2.8f, .3f), rot);
            mk.Box(WorldMaterials.Solid(Doors[0]), c + up * 1.05f + face * .16f, new Vector3(.95f, 2f, .04f), rot); mk.Blob(iron, c + up * 1.05f + face * .2f + side * .35f, Vector3.one * .04f, ctx.Rng, 0f);
            var num = SongGlow(id, Lamp, 2f); mk.Box(num, c + up * 2.3f + face * .17f, new Vector3(.8f, .28f, .02f), rot);
            ArtistBits.Label(ctx.Root, "7563", c + up * 2.3f + face * .19f, face, .2f, Ink, TextAnchor.MiddleCenter);
            mk.Box(stone, c + up * .05f + face * .6f, new Vector3(1.2f, .1f, .6f), rot);
        }

        void WashingLine(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var a = ctx.OnGround(c.x - side.x * 4f, c.z - side.z * 4f); var b = ctx.OnGround(c.x + side.x * 4f, c.z + side.z * 4f);
            foreach (var p in new[] { a, b }) { mk.Box(darkWood, p + up * 1.2f, new Vector3(.1f, 2.4f, .1f), rot); ArtistBits.PostCollider(ctx.Root, p, 2.4f, .1f); }
            mk.Box(cream, (a + b) / 2f + up * 2.2f, new Vector3(Vector3.Distance(a, b), .02f, .02f), Quaternion.LookRotation(Vector3.Cross(up, face)) * Quaternion.Euler(0, 90f, 0));
            Color[] sheets = { new Color(.95f, .94f, .9f), Pastels[2], Pastels[1], Pastels[3] };
            var glow = SongGlow(id, new Color(1f, .9f, .7f), .8f);
            for (var k = 0; k < 4; k++) { var p = Vector3.Lerp(a, b, .15f + k * .23f) + up * 1.6f; mk.Box(k == 1 ? glow : WorldMaterials.Solid(sheets[k]), p, new Vector3(1.4f, 1.2f, .03f), rot * Quaternion.Euler(0, ctx.Range(-8f, 8f), 0)); }
        }

        void LittleTower(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; mk.Prism(stone, c - up * .2f, up, 1.4f, 5f, 8, .85f); solid.Prism(stone, c - up * .2f, up, 1.4f, 5f, 8);
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; mk.Box(stone, c + up * 5f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.05f, new Vector3(.45f, .5f, .35f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            var win = SongGlow(id, Lamp, 1.8f); mk.Box(win, c + up * 3.6f + face * 1.25f, new Vector3(.4f, .7f, .04f), Quaternion.LookRotation(face)); mk.Box(darkWood, c + up * .9f + face * 1.35f, new Vector3(.8f, 1.6f, .05f), Quaternion.LookRotation(face));
        }

        void TrapDoor(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Box(darkWood, c + up * .05f, new Vector3(2.2f, .1f, 2.2f), rot); mk.Box(SongGlow(id, new Color(1f, .75f, .35f), 1.6f), c + up * .11f, new Vector3(1.8f, .02f, 1.8f), rot);
            mk.Box(wood, c + up * .9f - face * 1f, new Vector3(1.8f, 1.8f, .08f), rot * Quaternion.Euler(-15f, 0, 0)); mk.Blob(iron, c + up * 1.3f - face * .9f, Vector3.one * .06f, ctx.Rng, 0f);
            halos.Add(c + up * .6f, 2f, new Color(1f, .75f, .35f), .5f); solid.Box(wood, c + up * .9f - face * 1f, new Vector3(1.8f, 1.8f, .2f), rot);
        }

        void StarGlobe(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; mk.Prism(iron, c, up, .08f, 2.4f, 6); ArtistBits.PostCollider(ctx.Root, c, 2.4f, .15f);
            var g = c + up * 3.2f; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; var p0 = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .8f; mk.Box(iron, p0, new Vector3(.03f, 1.6f, .03f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, Mathf.Sin(a) * 0f)); }
            var star = SongGlow(id, new Color(.9f, .92f, 1f), 3f);
            for (var k = 0; k < 24; k++) mk.Blob(star, g + new Vector3(ctx.Range(-.6f, .6f), ctx.Range(-.6f, .6f), ctx.Range(-.6f, .6f)), Vector3.one * .05f, ctx.Rng, 0f);
            halos.Add(g, 1.8f, new Color(.85f, .9f, 1f), .4f);
        }

        void LittleRocket(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var redM = WorldMaterials.Solid(new Color(.85f, .25f, .2f)); mk.Prism(cream, c + up * .6f, up, .55f, 3.2f, 12); mk.Cone(redM, c + up * 3.8f, .55f, 1.2f, 12);
            foreach (var k in new[] { 0, 1, 2 }) { var d = Quaternion.Euler(0, k * 120f, 0) * Vector3.forward; mk.Tri(redM, c + d * .55f + up * .6f, c + d * .55f + up * 1.8f, c + d * 1.1f, c + Vector3.Cross(d, up) * 3f); mk.Tri(redM, c + d * .55f + up * .6f, c + d * 1.1f, c + d * .55f + up * 1.8f, -Vector3.Cross(d, up) * 3f + c); }
            mk.Prism(SongGlow(id, new Color(.5f, .8f, 1f), 2f), c + up * 2.8f + face * .53f, face, .18f, .04f, 10); mk.Prism(SongGlow(id, new Color(1f, .6f, .2f), 2.4f), c + up * .5f, Vector3.down, .4f, .3f, 10, .3f);
            ArtistBits.PostCollider(ctx.Root, c, 4f, .7f);
        }

        void PeepingHedge(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face);
            for (var k = 0; k < 5; k++) soft.Blob(hedge, c + side * ((k - 2f) * 1.2f) + up * 1f, new Vector3(1f, 1f, .9f), ctx.Rng, .3f);
            solid.Box(hedge, c + up * .9f, new Vector3(6.4f, 1.8f, 1.6f), Quaternion.LookRotation(face));
            var eye = SongGlow(id, new Color(1f, .9f, .4f), 2.4f);
            foreach (var x in new[] { -1.6f, .9f }) foreach (var s in new[] { -.1f, .1f }) mk.Blob(eye, c + side * (x + s) + up * 1.3f + face * .95f, new Vector3(.06f, .04f, .03f), ctx.Rng, 0f);
        }

        /// A piece from the artist's own song by each smaller area, glowing while it plays.
        void SmallSongPiece(Area z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.front); var id = Song(z.a, 0); if (id == null) return;
            var p = z.a.tier == "shrine" ? z.At(-1.4f, -3.4f) : z.At(-1.8f, -5.2f); var side = z.side; var f = z.front;
            Material G(Color c, float s = 1.8f) => SongGlow(id, c, s);
            void Post(float h) { z.mk.Prism(darkWood, p, up, .06f, h, 6); ArtistBits.PostCollider(z.root, p, h, .1f); }
            switch (z.a.name)
            {
                case "Cusk": z.mk.Box(darkWood, p + up * .6f, new Vector3(1.4f, 1.2f, .5f), rot); z.mk.Box(cream, p + up * .85f + f * .27f, new Vector3(1.2f, .05f, .2f), rot); for (var k = 0; k < 5; k++) z.mk.Blob(G(new Color(.3f, .55f, 1f), 2.2f), p + up * .92f + f * .27f + side * ((k - 2f) * .22f), Vector3.one * .05f, ctx.Rng, 0f); z.solid.Box(darkWood, p + up * .6f, new Vector3(1.4f, 1.2f, .6f), rot); break;
                case "Dummy": for (var k = 0; k < 6; k++) z.mk.Box(G(Color.HSVToRGB(k / 6f, .8f, 1f)), p + side * ((k - 2.5f) * .3f) + up * (.6f + k * .15f), new Vector3(.22f, 1.2f + k * .3f, .1f), rot); BoxCol(z.root, p + up * 1.4f, new Vector3(1.9f, 2.8f, .2f), rot); break;
                case "Interpol": Post(1f); z.mk.Box(darkWood, p + up * 1f, new Vector3(1.2f, .06f, .4f), rot); for (var k = 0; k < 3; k++) z.mk.Prism(G(new Color(.4f, 1f, .6f)), p + up * 1.03f + side * ((k - 1f) * .4f), up, .12f, .3f + k * .1f, 8, .4f); break;
                case "Milky Chance": for (var i = 0; i < 2; i++) for (var j = 0; j < 2; j++) z.mk.Box((i + j) % 2 == 0 ? G(new Color(1f, .8f, .3f)) : cream, p + side * (i - .5f) + f * (j - .5f) + up * .04f, new Vector3(.98f, .06f, .98f), rot); break;
                case "12 RODS": foreach (var s in new[] { -1.2f, 1.2f }) z.mk.Prism(darkWood, p + side * s, up, .05f, 1.4f, 6); z.mk.Box(G(new Color(1f, .3f, .3f)), p + up * 1.2f, new Vector3(2.4f, .06f, .02f), rot); BoxCol(z.root, p + up * .7f, new Vector3(2.5f, 1.4f, .15f), rot); break;
                case "Arcade Fire": for (var k = 0; k < 3; k++) { var h = p + side * ((k - 1f) * 1.1f); z.mk.Box(cream, h + up * .4f, new Vector3(.8f, .8f, .7f), rot); z.mk.Cone(slate, h + up * .8f, .6f, .4f, 4, 45f); z.mk.Box(G(Lamp, 2f), h + up * .45f + f * .36f, new Vector3(.25f, .25f, .02f), rot); } BoxCol(z.root, p + up * .5f, new Vector3(3.1f, 1f, .75f), rot); break;
                case "Broken Social Scene": for (var k = 0; k < 7; k++) z.mk.Box(G(new Color(.3f, .6f, 1f), 1.2f), p + side * ((k - 3f) * .35f) + up * (.4f + Mathf.Sin(k * .9f) * .3f), new Vector3(.3f, .12f, .3f), rot); break;
                case "Cold War Kids": Post(1.8f); z.mk.Box(darkWood, p + up * 1.75f + f * .2f, new Vector3(.06f, .06f, .4f), rot); z.mk.Box(G(new Color(.6f, .8f, 1f), 1f), p + up * 1.2f + f * .4f, new Vector3(.6f, .9f, .05f), rot); break;
                case "Current Joys": for (var k = 0; k < 16; k++) { float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f; z.mk.Tri(dark(), p + up * .03f, p + up * .03f + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * .9f, p + up * .03f + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * .9f, p - up); } z.mk.Prism(G(new Color(.8f, .5f, 1f), 1.6f), p + up * .02f, up, 1.05f, .02f, 16); break;
                case "Elio Mei": z.mk.Box(cream, p + up * .7f, new Vector3(.7f, 1.4f, .7f), rot); z.mk.Box(cream, p + up * 1.55f, new Vector3(.7f, .3f, .4f), rot * Quaternion.Euler(0, 0, 0)); z.mk.Box(G(new Color(.4f, .6f, 1f), 1.4f), p + up * .7f + f * .36f, new Vector3(.5f, .5f, .02f), rot); z.solid.Box(cream, p + up * .8f, new Vector3(.8f, 1.6f, .8f), rot); break;
                case "Francis of Delirium": for (var k = 0; k < 3; k++) z.mk.Box(G(new Color(1f, .95f, .85f), 1.2f), p + side * ((k - 1f) * .9f) + up * .03f + f * 1f, new Vector3(.06f, .02f, 4f), rot); break;
                case "Franz Ferdinand": Post(1.6f); var t = p + up * 2f; foreach (var r in new[] { .55f, .35f, .15f }) z.mk.Prism(r == .35f ? cream : G(new Color(1f, .25f, .2f)), t - f * (.02f * r), f, r, .03f, 16); BoxCol(z.root, t, new Vector3(1.1f, 1.1f, .12f), rot); break;
                case "Gigi Perez": z.mk.Box(wood, p + up * .3f, new Vector3(.9f, .4f, 2.2f), rot); z.mk.Prism(darkWood, p + up * .5f, up, .04f, 1.4f, 6); z.mk.Prism(G(Lamp, 2.4f), p + up * 1.9f, up, .12f, .25f, 8); z.solid.Box(wood, p + up * .3f, new Vector3(.9f, .6f, 2.2f), rot); ArtistBits.PostCollider(z.root, p, 2.1f, .15f); break;
                case "Grizzly Bear": Post(1.2f); z.mk.Box(cream, p + up * 1.5f, new Vector3(1.2f, .9f, .05f), rot); for (var k = 0; k < 14; k++) z.mk.Blob(G(new Color(1f, .7f, .3f), 2f), p + up * (1.75f - (k / 7) * .3f) + side * ((k % 7 - 3f) * .14f) + f * .04f, Vector3.one * .035f, ctx.Rng, 0f); BoxCol(z.root, p + up * 1.5f, new Vector3(1.2f, .9f, .15f), rot); break;
                case "Guster": Post(1.8f); z.mk.Blob(iron, p + up * 2.2f, Vector3.one * .3f, ctx.Rng, 0f); for (var k = 0; k < 4; k++) z.mk.Box(G(new Color(.7f, .9f, 1f), 1.6f), p + up * 2.2f + Quaternion.Euler(0, k * 90f + 45f, 30f) * Vector3.forward * .55f, new Vector3(.02f, .02f, .7f), Quaternion.Euler(0, k * 90f + 45f, 30f)); break;
                case "Inner Wave": Post(1.6f); z.mk.Box(darkWood, p + up * 1.7f, new Vector3(1.2f, .45f, .05f), rot); z.mk.Box(G(new Color(.6f, .4f, 1f), 1.6f), p + up * 1.7f + f * .03f, new Vector3(1.1f, .35f, .02f), rot); ArtistBits.Label(z.labels, "1 4 2", p + up * 1.7f + f * .05f, f, .2f, Ink, TextAnchor.MiddleCenter); break;
                case "Korea Girl": foreach (var s in new[] { -1f, 1f }) { var cp = p + side * s * .8f; z.mk.Box(wood, cp + up * .45f, new Vector3(.45f, .06f, .45f), Quaternion.LookRotation(-side * s)); z.mk.Box(wood, cp + up * .75f + side * s * .2f, new Vector3(.45f, .6f, .06f), Quaternion.LookRotation(side)); } z.mk.Prism(G(Lamp, 2.4f), p + up * .02f, up, .12f, .3f, 8); foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, p + side * s * .8f, .8f, .3f); break;
                case "Mac DeMarco": z.mk.Prism(cream, p, up, .6f, .35f, 14, 1.3f); for (var k = 0; k < 8; k++) z.mk.Blob(G(new Color(.5f, .9f, .4f), 1.2f), p + up * .4f + new Vector3(ctx.Range(-.4f, .4f), 0, ctx.Range(-.4f, .4f)), new Vector3(.18f, .08f, .18f), ctx.Rng, .3f); break;
                case "Manchester Orchestra": var a2 = p - side * 1.5f; var b2 = p + side * 1.5f; foreach (var q in new[] { a2, b2 }) { z.mk.Prism(darkWood, q, up, .05f, 1.4f, 6); z.mk.Prism(iron, q + up * 1.4f, side, .08f, .15f, 8); } z.mk.Box(G(new Color(1f, .85f, .5f), 1.6f), p + up * 1.45f, new Vector3(3f, .015f, .015f), Quaternion.LookRotation(side)); BoxCol(z.root, p + up * .75f, new Vector3(3.1f, 1.5f, .15f), rot); break;
                case "Modest Mouse": z.mk.Blob(stone, p + up * .6f, new Vector3(.9f, .7f, .9f), ctx.Rng, .3f); z.mk.Cone(stone, p + up * 1.2f + f * .3f, .25f, .5f, 5); foreach (var s in new[] { -.25f, .25f }) z.mk.Blob(G(new Color(1f, .5f, .1f), 2.6f), p + up * .9f + f * .75f + side * s, Vector3.one * .07f, ctx.Rng, 0f); z.solid.Box(stone, p + up * .6f, new Vector3(1.6f, 1.2f, 1.6f), rot); break;
                case "Rio Romeo": z.mk.Box(darkWood, p + up * .6f, new Vector3(1f, 1.2f, .6f), rot); z.mk.Box(G(new Color(1f, .9f, .7f), 1.4f), p + up * 1.3f + f * .31f, new Vector3(.9f, .3f, .02f), rot); ArtistBits.Label(z.labels, "NEWS", p + up * 1.3f + f * .33f, f, .16f, Ink, TextAnchor.MiddleCenter); z.solid.Box(darkWood, p + up * .7f, new Vector3(1f, 1.4f, .6f), rot); break;
                case "Sam Fender": z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .25f, .2f)), p, up, .4f, 1f, 10, .7f); z.mk.Prism(G(new Color(1f, .85f, .4f), 2.4f), p + up * 1f, up, .15f, .25f, 8); ArtistBits.Label(z.labels, "17", p + up * .6f + f * .42f, f, .2f, Cream, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, p, 1.3f, .4f); break;
                case "The 1975": Post(.3f); var fm = G(new Color(1f, .45f, .75f), 2.2f); var cc = p + up * 1.6f; foreach (var (x, y, w, h) in new[] { (0f, .7f, 1.6f, .08f), (0f, -.7f, 1.6f, .08f), (-.8f, 0f, .08f, 1.48f), (.8f, 0f, .08f, 1.48f) }) z.mk.Box(fm, cc + side * x + up * y, new Vector3(w, h, .05f), rot); z.mk.Box(darkWood, p + up * .6f, new Vector3(.08f, 1.2f, .08f), rot); break;
                case "The Drums": Post(1.4f); var sc = p + up * 2f; for (var k = 0; k < 10; k++) { var a = (90f + k * 36f) * Mathf.Deg2Rad; var a1 = (90f + (k + 1) * 36f) * Mathf.Deg2Rad; var r0 = k % 2 == 0 ? .6f : .25f; var r1 = k % 2 == 0 ? .25f : .6f; var q0 = sc + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r0; var q1 = sc + (side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * r1; z.mk.Tri(dark(), sc, q0, q1, sc - f); z.mk.Tri(dark(), sc, q1, q0, sc + f); z.mk.Box(G(new Color(.8f, .85f, 1f), 2.2f), (q0 + q1) / 2f + f * .03f, new Vector3(.04f, .04f, Vector3.Distance(q0, q1)), Quaternion.LookRotation(q1 - q0, f)); } BoxCol(z.root, sc, new Vector3(1.3f, 1.3f, .15f), rot); break;
                case "The Folk Implosion": Post(1.5f); z.mk.Blob(cream, p + up * 1.9f, new Vector3(.6f, .4f, .1f), ctx.Rng, 0f); for (var k = 0; k < 3; k++) z.mk.Blob(G(new Color(1f, .8f, .4f), 2.2f), p + up * 1.9f + side * ((k - 1f) * .25f) + f * .1f, Vector3.one * .05f, ctx.Rng, 0f); BoxCol(z.root, p + up * 1.9f, new Vector3(1.2f, .8f, .25f), rot); break;
                case "Veronica Everheart": foreach (var s in new[] { -.3f, .3f }) z.mk.Box(darkWood, p + side * s + up * 1.5f, new Vector3(.06f, 3f, .06f), rot * Quaternion.Euler(-8f, 0, 0)); for (var k = 0; k < 6; k++) z.mk.Box(G(new Color(.9f, .95f, 1f), 1.4f), p + up * (.3f + k * .5f) - f * k * .07f, new Vector3(.6f, .04f, .04f), rot); BoxCol(z.root, p + up * 1.5f - f * .2f, new Vector3(.8f, 3f, .5f), rot); break;
                case "Wolf Alice": var snow = WorldMaterials.Solid(new Color(.95f, .97f, 1f)); z.mk.Prism(snow, p - up * .1f, up, 1.4f, .15f, 12); z.mk.Cone(WorldMaterials.Solid(new Color(.3f, .45f, .35f)), p, .8f, 2f, 8); z.mk.Cone(snow, p + up * 1.2f, .5f, .8f, 8); z.mk.Blob(G(new Color(1f, .9f, .5f), 2.6f), p + up * 2.1f, Vector3.one * .12f, ctx.Rng, 0f); ArtistBits.PostCollider(z.root, p, 2f, .6f); break;
                case "Yellow Days": Post(2.2f); var cl = p + up * 2.6f; foreach (var s in new[] { -.6f, .6f }) z.mk.Blob(cream, cl + side * s, new Vector3(.6f, .35f, .4f), ctx.Rng, .2f); z.mk.Box(G(new Color(1f, .85f, .3f), 2f), cl - up * 1f, new Vector3(.25f, 2f, .05f), rot); break;
                case "Pavement": var sl = z.At(-1f, -5f); z.mk.Box(iron, sl + up * 1f, new Vector3(.6f, .05f, 3f), rot * Quaternion.Euler(-30f, 0, 0)); z.mk.Prism(iron, sl - f * 1.3f, up, .04f, 1.8f, 6); for (var k = 0; k < 6; k++) z.mk.Blob(G(new Color(1f, .75f, .3f), 1.4f), sl + f * (1.6f + k * .2f) + side * ctx.Range(-.6f, .6f) + up * .08f, Vector3.one * .1f, ctx.Rng, .4f); ArtistBits.PostCollider(z.root, sl, 1.8f, .5f); break;
            }
        }
    }
}
