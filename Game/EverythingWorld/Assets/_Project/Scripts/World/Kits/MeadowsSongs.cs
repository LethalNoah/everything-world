using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Meadows' deep pass (part of MeadowsKit): songs as little dreamlike places and pieces, each glowing while
    /// its song plays.
    /// - Clairo: packed bags waiting by a door standing alone in the grass ("Bags"), a vanity mirror ringed with
    ///   bulbs ("Pretty Girl"), a chalk heart drawn on the path with a tape player in it ("Sofia").
    /// - beabadoobee: a giant steaming coffee mug ("Coffee"), a giant glue bottle with paper hearts stuck round it
    ///   ("Glue Song"), two swings hung facing away from each other ("Estranged").
    /// - Every one-song artist a piece from their song beside their daydream (see SmallSongPiece).
    public partial class MeadowsKit
    {
        MeadowsLife meadowsLife;
        readonly List<(string song, Material m, Color c)> pendingGlows = new List<(string, Material, Color)>();
        readonly List<Vector3> placeSpots = new List<Vector3>();

        static readonly Dictionary<string, string> PieceSong = new Dictionary<string, string>
        {
            { "Anna Sofia", "4RtLJdGrUDDf6o7EhQtcas" }, { "Balu Brigada", "7yT4NJt5rgmVoMJMGPULcj" }, { "Beach Bunny", "6GJdFTOm23lC5bqjYSMJTj" },
            { "Cavetown", "0vf2eBw2inhl8y61cYQMv2" }, { "Chaos Chaos", "3lOok0REf4j3790abX26PR" }, { "Dazy", "2MJRMs7ZWRikUtQS3Hs009" },
            { "Foster The People", "4VbDJMkAX3dWNBdn3KH6Wx" }, { "London Grammar", "0V62AlQ6Lqy6xPK1sZTLD5" }, { "Loving", "1aPucggplTw2KyCmFEhu9e" },
            { "Monsune", "21NbcXsuKVVdymWt8Kz57t" }, { "Pacific Soul", "5flM2z37ssfGq9jkMLwPeR" }, { "Peach Pit", "6OiRh4kttAs1YWglvTcYkB" },
            { "Royel Otis", "6EoD3qpTgNtwXNqamzLCn5" }, { "The Apples In Stereo", "5uAEx8Gbz7FIWhhorTfU9o" }, { "The Neighbourhood", "2QjOHCTQ1Jl3zawyYOpxh6" },
            { "The Postal Service", "4G21jWo0Cm6lOIUNnZ2mUI" }, { "fun.", "6t6oULCRS6hnI7rm0h5gwl" },
        };

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(WorldMaterials.Glow(c, s * .65f)) { name = "Song glow" };
            if (meadowsLife != null) meadowsLife.songGlows.Add((song, m, c * (s * .65f))); else pendingGlows.Add((song, m, c * (s * .65f)));
            return m;
        }

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
                var wide = tries > 500; var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(22f, wide ? 240f : 90f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(px, pz, clear + 6f) || !ctx.IsFree(px, pz, clear) || NearPath(px, pz, clear + 1f)) continue;
                if (!Gentle(ctx, px, pz, clear + 3f, tries > 700 ? clear * .45f : clear * .25f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 16f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); ctx.AddKeepout(p.x, p.z, clear); return p;
            }
            return Vector3.positiveInfinity;
        }

        Vector3 PlaceWay(LandContext ctx, MeshKit mk, Vector3 c, float clear, string sign, string mark)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var cell in pathCells.Values) foreach (var p in cell) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(center.x - c.x, 0, center.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var stop = clear - 1f;
            if (best < float.MaxValue && best > stop + 3f)
            {
                var pts = new List<Vector3>(); var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - stop;
                for (var d = 1f; ; d += 1.5f) { var dd = Mathf.Min(d, len); var q = from + dir * dd; if (!ctx.IsLand(q.x, q.z, 0f)) break; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count > 1) { Ribbon(ctx, mk, track, pts, 1.6f, .05f); foreach (var p in pts) Remember(p); }
            }
            var side = Vector3.Cross(Vector3.up, face); var sp = ctx.OnGround(c.x + face.x * stop + side.x * 2.2f, c.z + face.z * stop + side.z * 2.2f); var rot = Quaternion.LookRotation(face);
            mk.Box(darkWood, sp + Vector3.up * .6f, new Vector3(.12f, 1.2f, .12f), rot); mk.Box(fabric, sp + Vector3.up * 1.35f, new Vector3(2.4f, .5f, .06f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.35f + face * .04f, face, .16f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PostCollider(ctx.Root, sp, 1.6f, .15f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 6f) - side * 6f; cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.2f; var look = c + Vector3.up * 1.6f - cam - Vector3.up * 1.6f;
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
                var c = PlaceSpot(ctx, near, clear); if (float.IsInfinity(c.x)) { Debug.Log("The Bedroom Pop Meadows: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clear, sign, mark)); placed++;
            }
            var cl = Near("Clairo"); var bb = Near("beabadoobee");
            Place(cl, 7f, "BAGS", "the packed bags", (c, f) => PackedBags(ctx, mk, solid, c, f, "3ISKxnCGKc5B9zr9CTUB3v"));
            Place(cl, 7f, "PRETTY GIRL", "the vanity mirror", (c, f) => Vanity(ctx, mk, solid, c, f, "3ey2Lbv2YOjgUYKTAIopNS"));
            Place(cl, 6f, "SOFIA", "the chalk heart", (c, f) => ChalkHeart(ctx, mk, solid, c, f, "1zHUEIQ6yzaWBzhNCV1SGW"));
            Place(bb, 6f, "COFFEE", "the coffee mug", (c, f) => Mug(ctx, mk, solid, c, f, "429NtPmr12aypzFH3FkN9l"));
            Place(bb, 6f, "GLUE SONG", "the glue bottle", (c, f) => Glue(ctx, mk, solid, c, f, "3iBgrkexCzVuPy4O9vx7Mf"));
            Place(bb, 8f, "ESTRANGED", "the turned swings", (c, f) => TurnedSwings(ctx, mk, solid, c, f, "4M7lT4DNieS5JTs2dEKiBQ"));
            Debug.Log($"The Bedroom Pop Meadows: {placed} song places");
        }

        /// Each place is dream-sized (half again as big as life) and lit, so it reads from the path at dusk.
        void Lit(LandContext ctx, Vector3 at, Color col, float r = 3f)
        {
            ArtistBits.PointLight(ctx.Root, at, col, 12f, 1.8f);
            halos.Add(at, r, col, .45f);
        }

        void PackedBags(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            const float S = 1.6f; var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(fabric, c + (up * 1.2f - face * .2f) * S, new Vector3(1.4f, 2.4f, .15f) * S, rot); mk.Box(SongGlow(id, FairyWarm, 2f), c + (up * 1.05f - face * .1f) * S, new Vector3(.9f, 2f, .03f) * S, rot);
            mk.Box(WorldMaterials.Solid(Pastel[1]), c + (up * 1.05f + face * .3f + side * .5f) * S, new Vector3(.08f, 2f, .9f) * S, rot * Quaternion.Euler(0, -35f, 0)); solid.Box(fabric, c + (up * 1.2f - face * .2f) * S, new Vector3(1.4f, 2.4f, .3f) * S, rot);
            var bagC = new[] { Pastel[0], Pastel[3], Pastel[2] };
            for (var k = 0; k < 3; k++) { var p = c + (face * 1f + side * ((k - 1f) * .8f)) * S; mk.Box(WorldMaterials.Solid(bagC[k]), p + up * (.3f + k % 2 * .1f) * S, new Vector3(.6f, .6f + k % 2 * .2f, .3f) * S, rot * Quaternion.Euler(0, k * 12f, 0)); mk.Box(darkWood, p + up * (.7f + k % 2 * .2f) * S, new Vector3(.3f, .06f, .06f) * S, rot); }
            solid.Box(fabric, c + (face * 1f + up * .4f) * S, new Vector3(2.4f, .8f, .5f) * S, rot);
            Lit(ctx, c + (face * 1.2f + up * 1.6f) * S, FairyWarm);
        }

        void Vanity(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            const float S = 1.6f; var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(WorldMaterials.Solid(Pastel[0]), c + up * .4f * S, new Vector3(1.6f, .8f, .6f) * S, rot); solid.Box(fabric, c + up * .4f * S, new Vector3(1.6f, .8f, .6f) * S, rot);
            var mirror = c + (up * 1.6f - face * .2f) * S; mk.Box(WorldMaterials.Solid(new Color(.8f, .85f, .95f), .95f), mirror, new Vector3(1.1f, 1.3f, .04f) * S, rot);
            var bulb = SongGlow(id, new Color(1f, .9f, .7f), 2.6f);
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; mk.Blob(bulb, mirror + (face * .04f + side * Mathf.Cos(a) * .68f + up * Mathf.Sin(a) * .78f) * S, Vector3.one * .06f * S, ctx.Rng, 0f); }
            halos.Add(mirror + face * .4f * S, 1.6f * S, new Color(1f, .9f, .7f), .4f);
            mk.Box(darkWood, c + (up * .25f + face * .8f) * S, new Vector3(.5f, .5f, .5f) * S, rot);
            ArtistBits.PointLight(ctx.Root, mirror + face * 1.5f, new Color(1f, .9f, .7f), 12f, 1.8f);
        }

        void ChalkHeart(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var chalkM = SongGlow(id, new Color(1f, .55f, .75f), 1.8f);
            for (var k = 0; k < 48; k++) { float t0 = k / 48f * Mathf.PI * 2f, t1 = (k + 1) / 48f * Mathf.PI * 2f; Vector3 H(float t) { var p = c + side * (16f * Mathf.Pow(Mathf.Sin(t), 3)) * .26f + face * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2 * t) - 2f * Mathf.Cos(3 * t) - Mathf.Cos(4 * t)) * .26f; p.y = ctx.Ground(p.x, p.z) + .05f; return p; } var a = H(t0); var b = H(t1); mk.Box(chalkM, (a + b) / 2f, new Vector3(.32f, .04f, Vector3.Distance(a, b) + .08f), Quaternion.LookRotation(b - a)); }
            // the tape player in the middle, big enough to see, its two reels lit
            var rot = Quaternion.LookRotation(face);
            mk.Box(WorldMaterials.Solid(Pastel[2]), c + up * .35f, new Vector3(1.4f, .7f, .6f), rot); mk.Box(darkWood, c + up * .42f + face * .31f, new Vector3(.9f, .3f, .02f), rot);
            foreach (var s in new[] { -.22f, .22f }) mk.Prism(chalkM, c + up * .42f + face * .33f + side * s, face, .1f, .02f, 10);
            solid.Box(fabric, c + up * .35f, new Vector3(1.4f, .7f, .6f), rot);
            Lit(ctx, c + up * 2.2f, new Color(1f, .55f, .75f), 3.5f);
        }

        void Mug(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            const float S = 1.4f; var up = Vector3.up; var side = Vector3.Cross(up, face); var cup = WorldMaterials.Solid(Pastel[4]);
            mk.Prism(cup, c, up, 1.2f * S, 2f * S, 18); mk.Prism(SongGlow(id, new Color(.75f, .45f, .2f), 1f), c + up * 1.85f * S, up, 1.1f * S, .1f * S, 18);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 9f - Mathf.PI / 2f; var p = c + (side * 1.2f + up * 1f + side * Mathf.Cos(a) * .7f + up * Mathf.Sin(a) * .6f) * S; mk.Box(cup, p, new Vector3(.2f, .2f, .2f) * S, Quaternion.identity); }
            solid.Prism(cup, c, up, 1.2f * S, 2f * S, 10);
            var steam = SongGlow(id, new Color(1f, .95f, .9f), 1.4f); for (var k = 0; k < 3; k++) for (var j = 0; j < 4; j++) mk.Blob(steam, c + (up * (2.4f + j * .4f) + side * ((k - 1f) * .4f + Mathf.Sin(j + k) * .15f)) * S, Vector3.one * (.12f - j * .02f) * S * 1.5f, ctx.Rng, 0f);
            Lit(ctx, c + up * 3.6f * S + face * 1.5f, new Color(1f, .8f, .6f));
        }

        void Glue(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            const float S = 1.6f; var up = Vector3.up; var white = WorldMaterials.Solid(new Color(.96f, .95f, .92f)); var orange = WorldMaterials.Solid(new Color(1f, .6f, .3f));
            mk.Box(white, c + up * 1f * S, new Vector3(1f, 2f, .6f) * S, Quaternion.LookRotation(face)); mk.Cone(orange, c + up * 2f * S, .4f * S, .8f * S, 8); solid.Box(white, c + up * 1f * S, new Vector3(1f, 2f, .6f) * S, Quaternion.LookRotation(face));
            var heart = SongGlow(id, new Color(1f, .45f, .6f), 2.2f); var side = Vector3.Cross(up, face);
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; var p = c + (up * (.6f + (k % 3) * .5f) + (side * Mathf.Cos(a) + face * Mathf.Sin(a)) * .8f) * S; mk.Box(heart, p, new Vector3(.25f, .22f, .03f) * S * 1.3f, Quaternion.LookRotation((p - c - up * p.y + up * c.y).normalized)); }
            Lit(ctx, c + up * 2f * S + face * 1.6f, new Color(1f, .5f, .65f));
        }

        void TurnedSwings(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            const float S = 1.5f; var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            foreach (var s in new[] { -2f, 2f }) { mk.Box(darkWood, c + (side * s + up * 1.5f) * S, new Vector3(.14f, 3f, .14f) * S, rot); ArtistBits.PostCollider(ctx.Root, c + side * s * S, 3f * S, .12f * S); }
            mk.Box(darkWood, c + up * 3f * S, new Vector3(4.4f, .14f, .14f) * S, rot);
            var seat = SongGlow(id, Pastel[1], 1.6f);
            foreach (var s in new[] { -1f, 1f }) { var p = c + side * s * .9f * S; foreach (var e in new[] { -.3f, .3f }) mk.Box(rope, p + (side * e + up * 1.9f) * S, new Vector3(.02f, 2.2f, .02f) * S, rot); mk.Box(seat, p + up * .75f * S, new Vector3(.8f, .06f, .35f) * S, rot * Quaternion.Euler(0, s * 40f, 0)); halos.Add(p + up * 1.4f * S, 1f, Pastel[1], .35f); }
            ArtistBits.PointLight(ctx.Root, c + up * 2.5f * S + face * 2f, Pastel[1], 12f, 1.6f);
        }

        /// A piece from each one-song artist's song beside their daydream.
        void SmallSongPiece(Area z)
        {
            if (!PieceSong.TryGetValue(z.a.name, out var id)) return;
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.front); var p = z.At(-2.6f, 3.4f); var side = z.side; var f = z.front;
            Material G(Color c, float s = 1.8f) => SongGlow(id, c, s);
            switch (z.a.name)
            {
                case "Anna Sofia": z.mk.Prism(darkWood, p, up, .06f, 1f, 6); z.mk.Blob(G(new Color(.9f, .3f, .35f), 1.4f), p + up * 1.4f, Vector3.one * .4f, ctx.Rng, .1f); for (var k = 0; k < 6; k++) z.mk.Cone(darkWood, p + up * 1.4f + Quaternion.Euler(k * 50f, k * 70f, 0) * Vector3.forward * .4f, .05f, .2f, 4); break;
                case "Balu Brigada": z.mk.Box(WorldMaterials.Solid(new Color(.35f, .3f, .4f)), p + up * .3f, new Vector3(1.6f, .4f, .6f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.35f, .3f, .4f)), p + up * .75f - f * .25f, new Vector3(1.6f, .7f, .15f), rot); z.mk.Blob(G(FairyWarm, 2.4f), p + up * 1.5f, Vector3.one * .08f, ctx.Rng, 0f); z.solid.Box(fabric, p + up * .5f, new Vector3(1.6f, 1f, .7f), rot); break;
                case "Beach Bunny": z.mk.Prism(fabric, p, up, .35f, .9f, 10); for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f, 0) * f; z.mk.Cone(G(new Color(1f, .8f, .4f), 2.2f), p + up * 1f + d * .2f, .08f, .25f + (k == 0 ? .1f : 0f), 4); } z.mk.Prism(G(new Color(1f, .8f, .4f), 2.2f), p + up * .95f, up, .25f, .05f, 12); break;
                case "Cavetown": for (var k = 0; k < 4; k++) { var h = p + side * ((k - 1.5f) * .5f); z.mk.Box(WorldMaterials.Solid(new Color(.75f, .65f, .5f)), h + up * .3f, new Vector3(.4f, .6f, .4f), rot); z.mk.Cone(darkWood, h + up * .6f, .32f, .3f, 4, 45f); z.mk.Box(k == 2 ? G(new Color(1f, .25f, .2f), 2.2f) : board, h + up * .35f + f * .21f, new Vector3(.12f, .12f, .02f), rot); } break;
                case "Chaos Chaos": z.mk.Prism(darkWood, p, up, .05f, 1.2f, 6); for (var k = 0; k < 24; k++) { float t0 = k / 24f * Mathf.PI * 2f, t1 = (k + 1) / 24f * Mathf.PI * 2f; Vector3 H(float t) => p + up * 1.6f + (side * 16f * Mathf.Pow(Mathf.Sin(t), 3) + up * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2 * t) - 2f * Mathf.Cos(3 * t) - Mathf.Cos(4 * t))) * .025f; z.mk.Box(G(new Color(1f, .35f, .55f), 2.2f), (H(t0) + H(t1)) / 2f, new Vector3(.06f, .06f, Vector3.Distance(H(t0), H(t1)) + .02f), Quaternion.LookRotation(H(t1) - H(t0), f)); } break;
                case "Dazy": z.mk.Blob(WorldMaterials.Solid(new Color(.55f, .8f, .55f)), p + up * .5f, new Vector3(.5f, .55f, .45f), ctx.Rng, .35f); foreach (var s in new[] { -.15f, .15f }) z.mk.Blob(G(new Color(1f, .95f, .6f), 2.4f), p + up * .65f + f * .42f + side * s, Vector3.one * .07f, ctx.Rng, 0f); break;
                case "Foster The People": z.mk.Box(board, p + up * .5f, new Vector3(1f, .2f, .7f), rot); z.mk.Prism(darkWood, p, up, .06f, .4f, 6); for (var i = 0; i < 4; i++) for (var j = 0; j < 3; j++) z.mk.Box((i + j) % 2 == 0 ? G(new Color(.4f, .9f, 1f), 2f) : G(new Color(1f, .5f, .8f), 2f), p + up * .61f + side * ((i - 1.5f) * .2f) + f * ((j - 1f) * .18f), new Vector3(.15f, .03f, .13f), rot); break;
                case "London Grammar": z.mk.Prism(WorldMaterials.Solid(new Color(.8f, .85f, .95f), .9f), p, up, .25f, .6f, 10); z.mk.Prism(G(FairyWarm, 2.6f), p + up * .15f, up, .1f, .25f, 8); halos.Add(p + up * .3f, 1f, FairyWarm, .6f); break;
                case "Loving": for (var k = 0; k < 9; k++) z.mk.Box(G(new Color(.4f, .9f, .85f), 1.4f), p + side * ((k - 4f) * .3f) + up * (.4f + Mathf.Sin(k * .8f) * .25f), new Vector3(.3f, .08f, .15f), rot * Quaternion.Euler(0, 0, Mathf.Cos(k * .8f) * 30f)); break;
                case "Monsune": z.mk.Prism(darkWood, p, up, .05f, 1.2f, 6); z.mk.Blob(fabric, p + up * 1.6f, new Vector3(.6f, .4f, .1f), ctx.Rng, 0f); z.mk.Box(G(new Color(.8f, .6f, 1f), 2f), p + up * 1.6f + f * .1f, new Vector3(.6f, .05f, .02f), rot); break;
                case "Pacific Soul": for (var k = 0; k < 5; k++) { var d = Quaternion.AngleAxis(k * 72f, up) * f; z.mk.Blob(G(new Color(1f, .35f, .4f), 1.6f), p + up * .5f + d * .3f, new Vector3(.25f, .06f, .2f), ctx.Rng, 0f); } z.mk.Prism(G(new Color(1f, .85f, .3f), 2f), p + up * .5f, up, .06f, .3f, 6); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .5f, .3f)), p, up, .04f, .5f, 5); break;
                case "Peach Pit": var peach = WorldMaterials.Solid(new Color(1f, .7f, .5f)); z.mk.Blob(peach, p + up * .5f - side * .35f, new Vector3(.5f, .5f, .5f), ctx.Rng, 0f); z.mk.Blob(peach, p + up * .5f + side * .35f, new Vector3(.5f, .5f, .5f), ctx.Rng, 0f); z.mk.Blob(G(new Color(.9f, .45f, .25f), 1.6f), p + up * .55f + f * .2f, new Vector3(.18f, .25f, .14f), ctx.Rng, .2f); break;
                case "Royel Otis": for (var k = 0; k < 7; k++) { var cp = p + side * ((k - 3f) * .25f) + f * ((k % 2) * .2f); var h = .3f + (k % 3) * .15f; z.mk.Prism(fabric, cp, up, .05f, h, 6); z.mk.Blob(G(FairyWarm, 2.6f), cp + up * (h + .06f), new Vector3(.03f, .06f, .03f), ctx.Rng, 0f); } break;
                case "The Apples In Stereo": foreach (var s in new[] { -.4f, .4f }) { z.mk.Blob(WorldMaterials.Solid(new Color(.85f, .2f, .2f)), p + up * .35f + side * s, Vector3.one * .3f, ctx.Rng, 0f); z.mk.Box(darkWood, p + up * .7f + side * s, new Vector3(.04f, .15f, .04f), rot); } var bolt = G(new Color(1f, .9f, .3f), 2.4f); z.mk.Box(bolt, p + up * 1.2f, new Vector3(.12f, .5f, .04f), rot * Quaternion.Euler(0, 0, 20f)); z.mk.Box(bolt, p + up * .85f + side * .05f, new Vector3(.12f, .45f, .04f), rot * Quaternion.Euler(0, 0, -20f)); break;
                case "The Neighbourhood": z.mk.Prism(darkWood, p, up, .05f, 1.6f, 6); z.mk.Box(darkWood, p + up * 1.6f, new Vector3(.8f, .04f, .04f), rot); var knit = WorldMaterials.Solid(new Color(.55f, .6f, .7f)); z.mk.Box(knit, p + up * 1.2f, new Vector3(.7f, .7f, .1f), rot); foreach (var s in new[] { -.5f, .5f }) z.mk.Box(knit, p + up * 1.2f + side * s, new Vector3(.25f, .6f, .1f), rot * Quaternion.Euler(0, 0, s > 0 ? 20f : -20f)); for (var k = 0; k < 6; k++) z.mk.Blob(G(new Color(.9f, .95f, 1f), 2.4f), p + up * (1.8f + (k % 3) * .3f) + side * ((k - 2.5f) * .3f), Vector3.one * .05f, ctx.Rng, 0f); break;
                case "The Postal Service": z.mk.Prism(darkWood, p, up, .05f, 1f, 6); z.mk.Box(WorldMaterials.Solid(new Color(.4f, .5f, .75f)), p + up * 1.2f, new Vector3(.5f, .4f, .7f), rot); var plane = G(new Color(1f, .97f, .9f), 1.8f); var pp = p + up * 2.4f; z.mk.Tri(plane, pp + f * .4f, pp - f * .3f + side * .3f, pp - f * .3f - side * .3f, pp + up); z.mk.Tri(plane, pp + f * .4f, pp - f * .3f - side * .3f, pp - f * .3f + side * .3f, pp - up); break;
                case "fun.": z.mk.Prism(darkWood, p, up, .05f, 1.4f, 6); var moon = G(new Color(1f, .9f, .6f), 2f); for (var k = 0; k < 10; k++) { var a0 = (40f + k * 28f) * Mathf.Deg2Rad; var q = p + up * 1.9f + (side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * .4f; z.mk.Blob(moon, q, Vector3.one * .08f, ctx.Rng, 0f); } for (var k = 0; k < 4; k++) z.mk.Blob(moon, p + up * (2.2f + (k % 2) * .3f) + side * ((k - 1.5f) * .4f) + f * .2f, Vector3.one * .04f, ctx.Rng, 0f); break;
            }
        }
    }
}
