using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Driftwood Shore's deep pass (part of DriftwoodKit): songs as small, calm places out on the dunes and the
    /// heath, each reached by a line of stepping planks to a driftwood sign, the warmth in their small lights, each
    /// glowing while its song plays. (Songs already in the artists' own areas aren't repeated.)
    /// - Jack Johnson: a tall lookout chair with binoculars facing the sea ("If I Had Eyes"), a striped tide post in
    ///   a pool with a glowing waterline ("High Tide Or Low Tide"), a ring of candle jars all lit together on a log
    ///   circle ("All At Once"), two benches back to back facing opposite ways ("They Do, They Don't"), a glowing
    ///   bar on two posts framing the horizon ("The Horizon Has Been Defeated").
    /// - Agnes Obel: an ember brazier with a woodpile ("Fuel to Fire"), twin cairns mirrored side by side
    ///   ("Familiar"), a little stream of pale light with stepping stones ("Riverside"), a birch dropping amber
    ///   leaves on a bench ("September Song"), a ring of tall poles hung with chimes ("Tokka").
    /// - Angus & Julia Stone: a letter in a bottle on a rock beside a lantern ("Apologize").
    public partial class DriftwoodKit
    {
        readonly List<Vector3> placeSpots = new List<Vector3>();

        Material SongGlow(string song, Color c, float s = 2f)
        {
            var m = new Material(WorldMaterials.Glow(c, s)) { name = "Song glow" };
            life.songGlows.Add((song, m, c * s));
            return m;
        }

        void Lit(LandContext ctx, Vector3 at, Color col, float r = 2.6f)
        {
            ArtistBits.PointLight(ctx.Root, at, col, 12f, 1.8f);
            halos.Add(at, r, col, .5f);
        }

        bool Gentle(LandContext ctx, float x, float z, float r, float drop)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var k = 0; k < 12; k++) foreach (var f in new[] { .5f, 1f }) { var a = k * Mathf.PI / 6f; var h = ctx.Ground(x + Mathf.Cos(a) * r * f, z + Mathf.Sin(a) * r * f); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo < drop;
        }

        bool InRing(LandContext ctx, Vector3 q, float pad)
        {
            foreach (var a in ctx.Artists) if ((a.stationId != 0 || a.station) && new Vector2(q.x - a.x, q.z - a.z).magnitude < a.r + pad) return true;
            return false;
        }

        Vector3 PlaceSpot(LandContext ctx, Vector3 near, float nearR, float clear)
        {
            for (var tries = 0; tries < 1200; tries++)
            {
                var wide = tries > 700; var a = ctx.Range(0f, Mathf.PI * 2f);
                var r = wide ? ctx.Range(RingR + 20f, 300f) : ctx.Range(nearR + clear + 4f, nearR + clear + 55f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                var q = new Vector3(px, 0, pz);
                if (!ctx.IsLand(px, pz, clear + 25f) || !ctx.IsFree(px, pz, clear) || !Open(ctx, px, pz, clear) || InRing(ctx, q, clear + 2f)) continue;
                if (!Gentle(ctx, px, pz, clear + 2f, tries > 900 ? clear * .45f : clear * .25f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 16f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); ctx.AddKeepout(p.x, p.z, clear + 2f); noGrass.Add(new Vector3(p.x, clear, p.z)); return p;
            }
            return Vector3.positiveInfinity;
        }

        /// Stepping planks from the nearest boardwalk, a driftwood sign, the tour's mark (kept inside the land and
        /// outside the station rings). Returns the way the place faces (toward the boardwalk).
        Vector3 PlaceWay(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, float clear, string sign, string mark, Color col)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var (p, _) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(center.x - c.x, 0, center.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var side = Vector3.Cross(Vector3.up, face);
            if (best < float.MaxValue)
            {
                var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - clear;
                var k = 0;
                for (var d = WalkW / 2f + 1.2f; d < len; d += 1.3f, k++)
                {
                    var q = from + dir * d; if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    if (InFootprint(ctx, q.x, q.z, 1f)) continue;   // not across an artist's own scene
                    var g = ctx.OnGround(q.x, q.z);
                    mk.Box(planks, g + Vector3.up * .05f, new Vector3(1.5f, .1f, .55f), Quaternion.LookRotation(dir) * Quaternion.Euler(0, k % 2 == 0 ? 4f : -5f, 0));
                    Remember(g);
                }
            }
            var sp = ctx.OnGround(c.x + face.x * (clear - 1f) + side.x * 2.2f, c.z + face.z * (clear - 1f) + side.z * 2.2f); var rot = Quaternion.LookRotation(face);
            mk.Prism(drift, sp - Vector3.up * .2f, Vector3.up, .09f, 1.9f, 6, .8f);
            mk.Box(driftDark, sp + Vector3.up * 1.45f, new Vector3(2.5f, .45f, .07f), rot * Quaternion.Euler(0, 0, 3f));
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.45f + face * .045f, face, .14f, Ink, TextAnchor.MiddleCenter);
            mk.Blob(WorldMaterials.Glow(col, 2.2f), sp + Vector3.up * 1.85f, Vector3.one * .09f, ctx.Rng, 0f);
            ArtistBits.PostCollider(ctx.Root, sp, 1.7f, .14f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 6f) - side * 5f;
            foreach (var (fo, so) in new[] { (clear + 6f, -5f), (clear + 6f, 5f), (clear + 3f, -4f), (clear + 3f, 4f), (-(clear + 5f), 3f), (clear + 1f, 0f) })
            { var q = c + face * fo + side * so; if (ctx.IsLand(q.x, q.z, 8f) && !InRing(ctx, q, 2f)) { cam = q; break; } }
            cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.2f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation((c + Vector3.up * 1.2f - cam).normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            placeSpots.Clear(); foreach (var a in ctx.Artists) placeSpots.Add(ctx.OnGround(a.x, a.z));
            var placed = 0;
            void Place(string artist, float clear, string sign, string mark, Color col, System.Action<Vector3, Vector3> build)
            {
                var a = ctx.Artists.Find(x => x.name == artist);
                var near = a != null ? ctx.OnGround(a.x, a.z) : center; var nearR = a == null ? 0f : Mathf.Max(Foot(a), a.r);
                var c = PlaceSpot(ctx, near, nearR, clear); if (float.IsInfinity(c.x)) { Debug.Log("The Driftwood Shore: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, solid, c, clear, sign, mark, col)); placed++;
            }
            var amber = new Color(1f, .7f, .35f); var pale = new Color(.75f, .88f, 1f);
            Place("Jack Johnson", 5f, "IF I HAD EYES", "the lookout chair", Bulb, (c, f) => LookoutChair(ctx, mk, solid, c, f, "3FnkZFv7kbxwlgaLgbqeDc"));
            Place("Jack Johnson", 5f, "HIGH TIDE OR LOW TIDE", "the tide post", pale, (c, f) => TidePost(ctx, mk, solid, c, f, "3lGqz047x1WtLJjMV0tdA4"));
            Place("Jack Johnson", 5f, "ALL AT ONCE", "the candle jars", Bulb, (c, f) => CandleJars(ctx, mk, solid, c, f, "7p3eluZv7dxIArpFUTBkia"));
            Place("Jack Johnson", 5f, "THEY DO, THEY DON'T", "the two benches", Rose, (c, f) => BackToBack(ctx, mk, solid, c, f, "6xzzm22bl8aHP3cdPPLIrO"));
            Place("Jack Johnson", 6f, "THE HORIZON HAS BEEN DEFEATED", "the horizon frame", Rose, (c, f) => HorizonFrame(ctx, mk, solid, c, f, "3Fb9ZbU7aIeMVKZ6VJGbXq"));
            Place("Agnes Obel", 5f, "FUEL TO FIRE", "the brazier", Ember, (c, f) => Brazier(ctx, mk, solid, c, f, "3fHDbOrSf2IQhLeKaShLS4"));
            Place("Agnes Obel", 5f, "FAMILIAR", "the twin cairns", pale, (c, f) => TwinCairns(ctx, mk, solid, c, f, "2EWnKuspetOzgfBtmaNZvJ"));
            Place("Agnes Obel", 6f, "RIVERSIDE", "the little stream", pale, (c, f) => Stream(ctx, mk, solid, c, f, "4vJ2nNBA6sxAUN5kLGPq3Q"));
            Place("Agnes Obel", 5f, "SEPTEMBER SONG", "the birch bench", amber, (c, f) => BirchBench(ctx, mk, solid, c, f, "2EuqgpA1cTC95AQUgCcZHk"));
            Place("Agnes Obel", 5f, "TOKKA", "the chime poles", Ember, (c, f) => ChimePoles(ctx, mk, solid, c, f, "22bk8cz4FrIpxR166zmXFk"));
            Place("Angus & Julia Stone", 4f, "APOLOGIZE", "the letter in a bottle", SeaGlass[1], (c, f) => LetterBottle(ctx, mk, solid, c, f, "0ygCnKG5ksq9ndlxXAryGl"));
            Debug.Log($"The Driftwood Shore: {placed} song places");
        }

        // ---------------------------------------------------------------- Jack Johnson
        void LookoutChair(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var look = seaDir; var rot = Quaternion.LookRotation(look); var side = Vector3.Cross(up, look);
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f })
            {
                var leg = c + side * s * .55f + look * t * .5f; mk.Prism(drift, leg - up * .2f, (up * 2.4f - side * s * .15f - look * t * .12f).normalized, .06f, 2.4f, 6);
            }
            mk.Box(planks, c + up * 2.1f, new Vector3(1.2f, .08f, 1f), rot);
            mk.Box(planks, c + up * 2.7f - look * .5f, new Vector3(1.2f, 1.1f, .08f), rot);
            for (var k = 0; k < 4; k++) mk.Box(driftDark, c + up * (.4f + k * .45f) - look * .5f, new Vector3(1f, .06f, .06f), rot);
            solid.Box(planks, c + up * 1.2f, new Vector3(1.3f, 2.4f, 1.2f), rot);
            var bino = c + up * 2.22f + look * .1f;
            foreach (var s in new[] { -1f, 1f }) mk.Prism(iron, bino + side * s * .07f, look, .05f, .22f, 8);
            mk.Prism(SongGlow(id, Bulb, 2.4f), bino + side * .07f + look * .225f, look, .04f, .01f, 8); mk.Prism(SongGlow(id, Bulb, 2.4f), bino - side * .07f + look * .225f, look, .04f, .01f, 8);
            var lamp = c + side * 1.2f; mk.Prism(drift, lamp, up, .05f, 1.4f, 6); mk.Box(SongGlow(id, Bulb, 2.2f), lamp + up * 1.55f, new Vector3(.22f, .3f, .22f), Quaternion.identity);
            ArtistBits.PostCollider(ctx.Root, lamp, 1.4f, .1f);
            Lit(ctx, lamp + up * 1.6f + look * .4f, Bulb);
        }

        void TidePost(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var pool = WorldMaterials.Solid(new Color(.3f, .4f, .45f), .95f);
            mk.Prism(pool, c + up * .02f, up, 2.6f, .03f, 14);
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f + .2f; mk.Blob(stone, ctx.OnGround(c.x + Mathf.Cos(a) * 2.8f, c.z + Mathf.Sin(a) * 2.8f), new Vector3(.5f, .3f, .45f), ctx.Rng, .25f); }
            var white = WorldMaterials.Solid(new Color(.9f, .9f, .88f)); var red = WorldMaterials.Solid(new Color(.7f, .25f, .22f));
            for (var k = 0; k < 8; k++) mk.Box(k % 2 == 0 ? white : red, c + up * (.2f + k * .4f), new Vector3(.24f, .4f, .24f), Quaternion.LookRotation(face));
            ArtistBits.PostCollider(ctx.Root, c, 3.3f, .2f);
            mk.Box(SongGlow(id, new Color(.6f, .85f, 1f), 2.6f), c + up * 1.3f, new Vector3(.36f, .06f, .36f), Quaternion.LookRotation(face));
            mk.Prism(SongGlow(id, new Color(.55f, .8f, 1f), .9f), c + up * .055f, up, 2.4f, .01f, 14, 1f, 13f);
            for (var k = 0; k < 6; k++) ArtistBits.Label(ctx.Root, (k + 1).ToString(), c + up * (.4f + k * .45f) + face * .135f, face, .12f, Ink, TextAnchor.MiddleCenter);
            Lit(ctx, c + up * 2f + face * 1f, new Color(.6f, .85f, 1f));
        }

        void CandleJars(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var jar = WorldMaterials.Solid(new Color(.7f, .8f, .78f), .9f); var flame = SongGlow(id, Bulb, 2.8f);
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI / 3f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 2.4f, c.z + Mathf.Sin(a) * 2.4f); var d = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                mk.Prism(drift, p + up * .25f - d * .9f, d, .25f, 1.8f, 7);
                ArtistBits.PostCollider(ctx.Root, p, .5f, .45f);
                for (var j = -1; j <= 1; j++) { var jp = p + d * (j * .55f) + up * .5f; mk.Prism(jar, jp, up, .1f, .22f, 8); mk.Blob(flame, jp + up * .16f, new Vector3(.04f, .07f, .04f), ctx.Rng, 0f); }
            }
            halos.Add(c + up * 1.4f, 3f, Bulb, .4f);
            ArtistBits.PointLight(ctx.Root, c + up * 1.6f, Bulb, 10f, 1.6f);
        }

        void BackToBack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face);
            foreach (var s in new[] { -1f, 1f })
            {
                var b = c + face * s * .45f; var rot = Quaternion.LookRotation(face * s);
                mk.Box(planks, b + up * .45f, new Vector3(1.8f, .07f, .45f), rot);
                mk.Box(planks, b + up * .8f - face * s * .22f, new Vector3(1.8f, .45f, .06f), rot);
                foreach (var e in new[] { -1f, 1f }) mk.Box(driftDark, b + side * e * .8f + up * .22f, new Vector3(.08f, .45f, .4f), rot);
            }
            solid.Box(planks, c + up * .5f, new Vector3(1.9f, 1f, 1.5f), Quaternion.LookRotation(face));
            var lamp = c + side * 1.3f; mk.Prism(iron, lamp, up, .05f, 2.2f, 6); mk.Box(SongGlow(id, Rose, 2.4f), lamp + up * 2.3f, new Vector3(.22f, .3f, .22f), Quaternion.identity);
            ArtistBits.PostCollider(ctx.Root, lamp, 2.2f, .1f);
            Lit(ctx, lamp + up * 2.2f + face * .5f, Rose);
        }

        void HorizonFrame(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var look = seaDir; var side = Vector3.Cross(up, look); var rot = Quaternion.LookRotation(look);
            foreach (var s in new[] { -1f, 1f }) { var p = ctx.OnGround((c + side * s * 2.4f).x, (c + side * s * 2.4f).z); mk.Prism(drift, p - up * .2f, up, .12f, 2.1f, 6, .85f); ArtistBits.PostCollider(ctx.Root, p, 2f, .15f); }
            mk.Box(SongGlow(id, new Color(1f, .6f, .62f), 3.2f), c + up * 1.65f, new Vector3(4.6f, .11f, .11f), rot);
            mk.Box(driftDark, c + up * 1.9f, new Vector3(5.2f, .12f, .14f), rot);
            var bench = c - look * 2.2f; mk.Box(planks, bench + up * .42f, new Vector3(2.2f, .08f, .5f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(driftDark, bench + side * s * .9f + up * .2f, new Vector3(.12f, .4f, .4f), rot);
            solid.Box(planks, bench + up * .25f, new Vector3(2.2f, .5f, .5f), rot);
            Lit(ctx, c + up * 2.2f - look * 1f, Rose);
        }

        // ---------------------------------------------------------------- Agnes Obel
        void Brazier(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up;
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Blob(stone, ctx.OnGround(c.x + Mathf.Cos(a) * 1.5f, c.z + Mathf.Sin(a) * 1.5f), new Vector3(.35f, .25f, .3f), ctx.Rng, .2f); }
            foreach (var t in new[] { 0f, 120f, 240f }) { var d = Quaternion.Euler(0, t, 0) * Vector3.forward; mk.Prism(iron, c + d * .45f, (up * 1f - d * .25f).normalized, .04f, 1f, 5); }
            mk.Prism(iron, c + up * .85f, up, .5f, .25f, 10, 1.3f);
            mk.Blob(SongGlow(id, Ember, 3f), c + up * 1.12f, new Vector3(.45f, .2f, .45f), ctx.Rng, .3f);
            mk.Blob(SongGlow(id, new Color(1f, .45f, .15f), 2.6f), c + up * 1.35f, new Vector3(.2f, .35f, .2f), ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, c, 1.2f, .7f);
            var pile = c + face * 1.8f + Vector3.Cross(up, face) * 1.4f;
            for (var k = 0; k < 7; k++) { var p = pile + up * (.15f + (k / 3) * .25f) + Vector3.Cross(up, face) * ((k % 3) - 1f) * .26f; mk.Prism(driftDark, p - face * .6f, face, .12f, 1.2f, 6); }
            solid.Box(driftDark, pile + up * .35f, new Vector3(.9f, .7f, 1.2f), Quaternion.LookRotation(face));
            Lit(ctx, c + up * 1.8f, Ember);
        }

        void TwinCairns(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face);
            foreach (var s in new[] { -1f, 1f })
            {
                var b = ctx.OnGround((c + side * s * 1.2f).x, (c + side * s * 1.2f).z); var y = 0f;
                for (var k = 0; k < 6; k++) { var r = .55f - k * .07f; var h = .32f - k * .025f; mk.Blob(k % 2 == 0 ? stone : driftDark, b + up * (y + h / 2f) + side * s * Mathf.Sin(k * 1.3f) * .04f, new Vector3(r, h / 2f + .04f, r * .9f), new System.Random(k * 7 + 3), .15f); y += h; }
                mk.Blob(SongGlow(id, new Color(.7f, .88f, 1f), 2.6f), b + up * (y + .1f), Vector3.one * .12f, ctx.Rng, 0f);
                ArtistBits.PostCollider(ctx.Root, b, y, .5f);
            }
            Lit(ctx, c + up * 2.4f + face * .6f, new Color(.7f, .88f, 1f));
        }

        void Stream(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var water = SongGlow(id, new Color(.6f, .8f, .95f), .8f);
            var prev = Vector3.zero;
            for (var k = 0; k <= 10; k++)
            {
                var t = k / 10f - .5f; var p = c + side * (t * 11f) + face * Mathf.Sin(t * 5f) * 1.2f; p = ctx.OnGround(p.x, p.z) + up * .03f;
                if (k > 0) mk.Box(water, (prev + p) / 2f, new Vector3(1.1f, .02f, Vector3.Distance(prev, p) + .3f), Quaternion.LookRotation(p - prev));
                if (k % 2 == 1) { soft.Blob(seaweed, p + side * .2f + face * .9f + up * .3f, new Vector3(.25f, .5f, .25f), ctx.Rng, .3f); }
                prev = p;
            }
            foreach (var s in new[] { -.6f, 0f, .6f }) mk.Blob(stone, ctx.OnGround((c + face * s).x, (c + face * s).z) + up * .08f, new Vector3(.32f, .14f, .28f), ctx.Rng, .1f);
            Lit(ctx, c + up * 1.4f + face * 1.5f, new Color(.6f, .8f, .95f), 3f);
        }

        void BirchBench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var birch = WorldMaterials.Solid(new Color(.86f, .84f, .8f)); var leaf = SongGlow(id, new Color(1f, .68f, .25f), 1.4f);
            var trunk = c - face * 1.2f; mk.Prism(birch, trunk - up * .2f, up, .18f, 5.5f, 7, .6f); ArtistBits.PostCollider(ctx.Root, trunk, 5f, .25f);
            foreach (var (a, h, l) in new[] { (0f, 3.2f, 1.8f), (110f, 3.8f, 1.5f), (220f, 4.3f, 1.4f), (300f, 2.8f, 1.3f) }) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; mk.Prism(birch, trunk + up * h, (d + up * .7f).normalized, .07f, l, 5, .5f); }
            foreach (var (o, r) in new[] { (new Vector3(0, 5.3f, 0), 1.3f), (new Vector3(1f, 4.6f, .4f), 1f), (new Vector3(-.9f, 4.8f, -.3f), 1.05f), (new Vector3(.2f, 4.4f, -1f), .9f), (new Vector3(-.3f, 4.3f, 1f), .85f) })
                soft.Blob(WorldMaterials.Solid(Color.Lerp(new Color(.82f, .6f, .32f), new Color(.9f, .75f, .45f), r - .85f)), trunk + o, new Vector3(r, r * .75f, r), ctx.Rng, .25f);
            for (var k = 0; k < 40; k++) { var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(.2f, 3.2f); var p = ctx.OnGround(trunk.x + Mathf.Cos(a) * r, trunk.z + Mathf.Sin(a) * r); var y = k % 3 == 0 ? ctx.Range(.6f, 4f) : .02f; mk.Box(leaf, p + up * y, new Vector3(.12f, .01f, .09f), Quaternion.Euler(k % 3 == 0 ? 40f : 0f, ctx.Range(0, 360f), 0)); }
            var rot = Quaternion.LookRotation(face); mk.Box(planks, c + up * .45f, new Vector3(1.8f, .08f, .45f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(driftDark, c + Vector3.Cross(up, face) * s * .75f + up * .22f, new Vector3(.08f, .45f, .4f), rot);
            solid.Box(planks, c + up * .25f, new Vector3(1.8f, .5f, .45f), rot);
            Lit(ctx, c + up * 2.4f + face * 1f, new Color(1f, .7f, .35f));
        }

        void ChimePoles(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var brass = SongGlow(id, Ember, 1.6f);
            for (var k = 0; k < 7; k++)
            {
                var a = k * Mathf.PI * 2f / 7f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 2.6f, c.z + Mathf.Sin(a) * 2.6f); var h = 3.2f + (k % 3) * .5f;
                mk.Prism(driftDark, p - up * .2f, up, .07f, h + .2f, 6); ArtistBits.PostCollider(ctx.Root, p, h, .1f);
                var arm = (new Vector3(c.x, 0, c.z) - new Vector3(p.x, 0, p.z)).normalized;
                mk.Box(drift, p + up * (h - .1f) + arm * .35f, new Vector3(.05f, .05f, .7f), Quaternion.LookRotation(arm));
                for (var j = 0; j < 4; j++) { var q = p + up * (h - .1f) + arm * (.15f + j * .17f); mk.Box(rope, q - up * .2f, new Vector3(.01f, .4f, .01f), Quaternion.identity); mk.Prism(brass, q - up * (.75f + j * .1f), up, .03f, .35f + j * .1f, 6); }
            }
            Lit(ctx, c + up * 2.6f, Ember, 3f);
        }

        // ---------------------------------------------------------------- Angus & Julia Stone
        void LetterBottle(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face);
            mk.Blob(stone, c + up * .35f, new Vector3(1f, .5f, .8f), ctx.Rng, .2f); solid.Box(stone, c + up * .35f, new Vector3(1.6f, .7f, 1.3f), Quaternion.LookRotation(face));
            var b = c + up * .88f; var along = (side + face * .3f).normalized;
            mk.Prism(SongGlow(id, new Color(.45f, .85f, .55f), 1.2f), b - along * .2f, along, .09f, .3f, 8);
            mk.Prism(SongGlow(id, new Color(.45f, .85f, .55f), 1.2f), b + along * .1f, along, .035f, .14f, 6);
            mk.Box(SongGlow(id, new Color(1f, .92f, .75f), 1.8f), b - along * .05f, new Vector3(.06f, .06f, .22f), Quaternion.LookRotation(along));
            var lamp = c + side * -1.4f + face * .3f; lamp = ctx.OnGround(lamp.x, lamp.z);
            mk.Prism(driftDark, lamp, up, .05f, 1.1f, 6); mk.Box(SongGlow(id, Bulb, 2.2f), lamp + up * 1.25f, new Vector3(.2f, .28f, .2f), Quaternion.identity);
            ArtistBits.PostCollider(ctx.Root, lamp, 1.1f, .1f);
            Lit(ctx, b + up * .6f + face * .6f, Bulb, 1.8f);
        }
    }
}
