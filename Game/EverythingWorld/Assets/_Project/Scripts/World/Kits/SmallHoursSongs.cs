using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Small Hours' deep pass (part of SmallHoursKit): songs as little places in the sleeping neighbourhood,
    /// each down its own wet lane with a sign, glowing while its song plays; and by every one-song artist's door,
    /// a piece from their song.
    /// - Steph.fm: a night bus stop with a greyhound waiting ("Greyhound"), a patch of old TV aerials with
    ///   blinking tips ("Antenna Patch"), a tall mirror standing in a garden ("a second glance").
    /// - Bell Thompson: a café table with a record turning under a blue neon ("Always Like This"), a trophy on a
    ///   pedestal under a spotlight inside velvet ropes ("Most Prized").
    /// - Noah Gallagher, round his place: a clearing of snow with snow hanging in the air ("Whiteout"), a corridor
    ///   of torch-lit stone posts ("The Gauntlet"), a lectern with a glowing open book ("Doctrine Factorum").
    public partial class SmallHoursKit
    {
        readonly List<Vector3> placeSpots = new List<Vector3>();
        static readonly Color JazzBlue = new Color(.3f, .55f, 1f), Frost = new Color(.75f, .88f, 1f);

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

        /// Inside a radio station's ring (its gold curtain): places and their views stay outside them.
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
                var r = wide ? ctx.Range(30f, 260f) : ctx.Range(nearR + clear + 4f, nearR + clear + 50f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(px, pz, clear + 30f) || !ctx.IsFree(px, pz, clear) || NearPath(px, pz, clear + 1f) || InNoah(new Vector3(px, 0, pz), clear) || InRing(ctx, new Vector3(px, 0, pz), clear + 2f)) continue;
                if (!Gentle(ctx, px, pz, clear + 2f, tries > 900 ? clear * .45f : clear * .25f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 16f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); ctx.AddKeepout(p.x, p.z, clear + 2f); return p;
            }
            return Vector3.positiveInfinity;
        }

        /// A wet lane from the nearest lane to the place, a sign on a post with a little lamp, the tour's mark.
        Vector3 PlaceWay(LandContext ctx, MeshKit mk, Vector3 c, float clear, string sign, string mark, Color col)
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
                if (pts.Count > 1) { Ribbon(ctx, mk, lane, pts, 1.8f, .045f); foreach (var p in pts) Remember(p); }
            }
            var side = Vector3.Cross(Vector3.up, face); var sp = ctx.OnGround(c.x + face.x * stop + side.x * 2.2f, c.z + face.z * stop + side.z * 2.2f); var rot = Quaternion.LookRotation(face);
            mk.Box(darkWood, sp + Vector3.up * .7f, new Vector3(.12f, 1.4f, .12f), rot); mk.Box(board, sp + Vector3.up * 1.5f, new Vector3(2.6f, .5f, .06f), rot);
            BoxCol(ctx.Root, sp + Vector3.up * 1.5f, new Vector3(2.6f, .5f, .12f), rot);   // the board at head height
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.5f + face * .04f, face, .15f, Ink, TextAnchor.MiddleCenter);
            mk.Blob(WorldMaterials.Glow(col, 2.4f), sp + Vector3.up * 1.95f, Vector3.one * .1f, ctx.Rng, 0f);
            ArtistBits.PostCollider(ctx.Root, sp, 1.6f, .15f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            // the camera inside the land (near a border, the land's edge curtain of light hid the place)
            var cam = c + face * (clear + 6f) - side * 5f;
            foreach (var (fo, so) in new[] { (clear + 6f, -5f), (clear + 6f, 5f), (clear + 3f, -4f), (clear + 3f, 4f), (-(clear + 5f), 3f), (clear + 1f, 0f) })
            { var q = c + face * fo + side * so; if (ctx.IsLand(q.x, q.z, 8f) && !InRing(ctx, q, 2f)) { cam = q; break; } }
            cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.2f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation((c + Vector3.up * 1.4f - cam - Vector3.up * .4f).normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            placeSpots.Clear(); foreach (var a in ctx.Artists) placeSpots.Add(ctx.OnGround(a.x, a.z));
            var placed = 0;
            void Place(string artist, float clear, string sign, string mark, Color col, System.Action<Vector3, Vector3> build)
            {
                var a = ctx.Artists.Find(x => x.name == artist);
                var near = a != null ? ctx.OnGround(a.x, a.z) : center; var nearR = a == null ? 0f : a.name == NoahName ? noahR : Mathf.Max(8f, a.r);
                if (a != null && a.name == NoahName) near = noah;
                var c = PlaceSpot(ctx, near, nearR, clear); if (float.IsInfinity(c.x)) { Debug.Log("The Small Hours: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clear, sign, mark, col)); placed++;
            }
            Place("Steph.fm", 6f, "GREYHOUND", "the night bus stop", Sodium, (c, f) => BusStop(ctx, mk, solid, c, f, "2myb67tEL55qGwSpNu4Mrp"));
            Place("Steph.fm", 6f, "ANTENNA PATCH", "the antenna patch", new Color(1f, .2f, .15f), (c, f) => AntennaPatch(ctx, mk, solid, c, f, "1GO23GHiO6BVqNgh2THlKn"));
            Place("Steph.fm", 5f, "A SECOND GLANCE", "the garden mirror", Frost, (c, f) => GardenMirror(ctx, mk, solid, c, f, "2SRCYceHYyaRKXPgA8G6k0"));
            Place("Bell Thompson", 5f, "ALWAYS LIKE THIS", "the café table", JazzBlue, (c, f) => CafeTable(ctx, mk, solid, c, f, "6RBg9koGBHYWRjkbITDie4"));
            Place("Bell Thompson", 5f, "MOST PRIZED", "the trophy", new Color(1f, .8f, .3f), (c, f) => Trophy(ctx, mk, solid, c, f, "15Ku4wayJ8nruoPuutBTQQ"));
            // (Noah Gallagher's songs have their pieces in his exhibit: SmallHoursExhibit)
            Debug.Log($"The Small Hours: {placed} song places");
        }

        // ---------------------------------------------------------------- Steph.fm
        void BusStop(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var glass = WorldMaterials.Solid(new Color(.5f, .58f, .66f), .9f); var roof = WorldMaterials.Solid(new Color(.2f, .21f, .25f));
            mk.Box(stone, c + up * .05f, new Vector3(5f, .1f, 2.6f), rot);
            mk.Box(glass, c - face * 1.1f + up * 1.3f, new Vector3(4f, 2.4f, .06f), rot); solid.Box(glass, c - face * 1.1f + up * 1.3f, new Vector3(4f, 2.4f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(iron, c + side * s * 2f - face * 1.1f + up * 1.3f, new Vector3(.08f, 2.6f, .08f), rot); mk.Box(glass, c + side * s * 2f - face * .5f + up * 1.3f, new Vector3(.06f, 2.4f, 1.1f), rot); solid.Box(glass, c + side * s * 2f - face * .5f + up * 1.3f, new Vector3(.15f, 2.4f, 1.1f), rot); }
            mk.Box(roof, c + up * 2.66f - face * .4f, new Vector3(4.4f, .12f, 1.8f), rot);
            mk.Box(SongGlow(id, Sodium, 2.4f), c + up * 2.58f + face * .45f, new Vector3(4.2f, .06f, .1f), rot);
            mk.Box(darkWood, c - face * .7f + up * .48f, new Vector3(3f, .08f, .45f), rot); solid.Box(darkWood, c - face * .7f + up * .3f, new Vector3(3f, .6f, .5f), rot);
            mk.Box(SongGlow(id, new Color(1f, .85f, .5f), 2f), c + side * 1.4f - face * 1.06f + up * 1.5f, new Vector3(.8f, 1f, .04f), rot);   // the lit timetable
            var pole = c + side * 2.9f + face * .9f; mk.Prism(iron, pole, up, .05f, 3f, 6); mk.Prism(SongGlow(id, Sodium, 1.6f), pole + up * 2.7f + face * .02f, face, .32f, .04f, 14);
            ArtistBits.PostCollider(ctx.Root, pole, 3f, .1f);
            ArtistBits.Label(ctx.Root, "GREYHOUND", c + side * 1.4f - face * 1.02f + up * 2.12f, face, .12f, new Color(.15f, .12f, .1f), TextAnchor.MiddleCenter);
            // a greyhound sitting by the bench, waiting
            var dog = WorldMaterials.Solid(new Color(.55f, .55f, .58f)); var g = c - side * 1.9f + face * .2f; var dr = Quaternion.LookRotation(side); ArtistBits.PostCollider(ctx.Root, g + side * .15f, 1f, .3f);
            mk.Box(dog, g + up * .45f, new Vector3(.22f, .3f, .55f), dr * Quaternion.Euler(-35f, 0, 0));
            mk.Box(dog, g + up * .75f + side * .2f, new Vector3(.12f, .32f, .12f), dr * Quaternion.Euler(-20f, 0, 0));
            mk.Box(dog, g + up * .92f + side * .32f, new Vector3(.12f, .12f, .3f), dr);
            foreach (var s in new[] { -1f, 1f }) mk.Box(dog, g + up * .2f + side * .18f + face * s * .07f, new Vector3(.05f, .4f, .05f), dr);
            Lit(ctx, c + up * 2.2f + face * .5f, Sodium);
        }

        void AntennaPatch(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var tip = SongGlow(id, new Color(1f, .15f, .1f), 3f); var cable = WorldMaterials.Solid(new Color(.12f, .12f, .14f));
            var tops = new List<Vector3>();
            for (var k = 0; k < 9; k++)
            {
                var a = k * 2.4f; var r = 1f + (k % 3) * 1.5f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r); var h = 2.6f + (k % 4) * .9f;
                mk.Prism(iron, p - up * .2f, up, .05f, h + .2f, 6); ArtistBits.PostCollider(ctx.Root, p, h, .1f);
                var yaw = Quaternion.Euler(0, k * 37f, 0);
                mk.Box(iron, p + up * (h - .3f), yaw * new Vector3(.03f, .03f, 1.6f), yaw);
                for (var e = 0; e < 5; e++) mk.Box(iron, p + up * (h - .3f) + yaw * Vector3.forward * (-.6f + e * .3f), new Vector3(.03f, .03f, .9f - e * .12f), yaw * Quaternion.Euler(0, 90f, 0));
                mk.Blob(tip, p + up * (h + .05f), Vector3.one * .08f, ctx.Rng, 0f);
                tops.Add(p + up * (h - .4f));
            }
            for (var k = 0; k + 1 < tops.Count; k++) { var a = tops[k]; var b = tops[k + 1]; var mid = (a + b) / 2f - up * .4f; mk.Box(cable, (a + mid) / 2f, new Vector3(.02f, .02f, Vector3.Distance(a, mid)), Quaternion.LookRotation(mid - a)); mk.Box(cable, (mid + b) / 2f, new Vector3(.02f, .02f, Vector3.Distance(mid, b)), Quaternion.LookRotation(b - mid)); }
            Lit(ctx, c + up * 3.4f, new Color(1f, .3f, .2f), 3f);
        }

        void GardenMirror(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 25f, 0); var fwd = rot * Vector3.forward;
            var frame = WorldMaterials.Solid(new Color(.55f, .45f, .3f));
            mk.Box(frame, c + up * 1.25f, new Vector3(1.3f, 2.5f, .12f), rot); solid.Box(frame, c + up * 1.25f, new Vector3(1.3f, 2.5f, .3f), rot);
            mk.Box(SongGlow(id, Frost, 1.2f), c + up * 1.25f + fwd * .07f, new Vector3(1.04f, 2.2f, .03f), rot);
            mk.Box(frame, c + up * .15f - fwd * .4f, new Vector3(.1f, .3f, .9f), rot);
            mk.Box(darkWood, c + up * .45f + fwd * 1.6f, new Vector3(.45f, .06f, .45f), rot); ArtistBits.PostCollider(ctx.Root, c + fwd * 1.6f, .5f, .3f);   // a stool in front, as if someone just stood up
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) mk.Box(darkWood, c + up * .22f + fwd * (1.6f + t * .18f) + rot * Vector3.right * s * .18f, new Vector3(.04f, .44f, .04f), rot);
            Lit(ctx, c + up * 2.4f + fwd * 1.5f, Frost);
        }

        // ---------------------------------------------------------------- Bell Thompson
        void CafeTable(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            mk.Prism(iron, c, up, .05f, .75f, 6); mk.Prism(darkWood, c + up * .75f, up, .55f, .05f, 14); solid.Prism(darkWood, c, up, .55f, .8f, 10);
            foreach (var s in new[] { -1f, 1f })
            {
                var ch = c + side * s * 1f; var cr = Quaternion.LookRotation(-side * s); ArtistBits.PostCollider(ctx.Root, ch, .9f, .3f);
                mk.Box(iron, ch + up * .45f, new Vector3(.45f, .05f, .45f), cr); mk.Box(iron, ch + up * .75f - (-side * s) * .2f, new Vector3(.45f, .6f, .04f), cr);
                foreach (var e in new[] { -1f, 1f }) foreach (var g in new[] { -1f, 1f }) mk.Box(iron, ch + up * .22f + cr * new Vector3(e * .18f, 0, g * .18f), new Vector3(.03f, .44f, .03f), cr);
            }
            mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .34f)), c + up * .84f, new Vector3(.5f, .08f, .4f), rot);
            mk.Prism(SongGlow(id, JazzBlue, 1.6f), c + up * .885f, up, .17f, .01f, 16);
            // the blue neon over it on two posts
            var sign = c - face * 1.4f;
            foreach (var s in new[] { -1f, 1f }) { mk.Prism(iron, sign + side * s * 1.3f, up, .05f, 2.8f, 6); ArtistBits.PostCollider(ctx.Root, sign + side * s * 1.3f, 2.8f, .1f); }
            mk.Box(darkWood, sign + up * 2.5f, new Vector3(2.8f, .7f, .08f), rot);
            mk.Box(SongGlow(id, JazzBlue, 3f), sign + up * 2.5f + face * .05f, new Vector3(2.4f, .05f, .02f), rot);
            ArtistBits.Label(ctx.Root, "always like this", sign + up * 2.62f + face * .06f, face, .2f, new Color(.6f, .8f, 1f), TextAnchor.MiddleCenter);
            Lit(ctx, c + up * 2f, JazzBlue);
        }

        void Trophy(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var velvet = WorldMaterials.Solid(new Color(.55f, .1f, .16f)); var brass = WorldMaterials.Solid(new Color(.8f, .65f, .3f), .7f);
            mk.Box(stone, c + up * .5f, new Vector3(.9f, 1f, .9f), Quaternion.LookRotation(face)); solid.Box(stone, c + up * .5f, new Vector3(.9f, 1f, .9f), Quaternion.LookRotation(face));
            var gold = SongGlow(id, new Color(1f, .78f, .3f), 2f);
            mk.Prism(gold, c + up * 1f, up, .18f, .12f, 10); mk.Prism(gold, c + up * 1.12f, up, .05f, .3f, 8);
            mk.Prism(gold, c + up * 1.42f, up, .12f, .45f, 12, 2.2f);
            foreach (var s in new[] { -1f, 1f }) mk.Box(gold, c + up * 1.68f + Vector3.Cross(up, face) * s * .3f, new Vector3(.06f, .22f, .06f), Quaternion.identity);
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI / 3f + .5f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 2.2f, c.z + Mathf.Sin(a) * 2.2f);
                mk.Prism(brass, p, up, .05f, .9f, 8); mk.Blob(brass, p + up * .95f, Vector3.one * .08f, ctx.Rng, 0f); ArtistBits.PostCollider(ctx.Root, p, .9f, .08f);
                var a2 = (k + 1) * Mathf.PI / 3f + .5f; var q = ctx.OnGround(c.x + Mathf.Cos(a2) * 2.2f, c.z + Mathf.Sin(a2) * 2.2f);
                if (k == 0) continue;   // a gap to walk in
                var mid = (p + q) / 2f + up * .6f; mk.Box(velvet, mid, new Vector3(.06f, .06f, Vector3.Distance(p, q)), Quaternion.LookRotation(q - p));
                solid.Box(velvet, mid - up * .2f, new Vector3(.1f, .8f, Vector3.Distance(p, q)), Quaternion.LookRotation(q - p));   // the rope keeps you out
            }
            // the spotlight cone from above
            var lamp = c + up * 4.2f; mk.Prism(iron, lamp, up, .2f, .3f, 8);
            ArtistBits.PointLight(ctx.Root, c + up * 2.4f, new Color(1f, .85f, .55f), 8f, 2.4f);
            halos.Add(c + up * 1.5f, 1.4f, new Color(1f, .8f, .4f), .6f);
        }

        // ---------------------------------------------------------------- Noah Gallagher
        void Whiteout(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var snow = WorldMaterials.Solid(new Color(.92f, .94f, .98f));
            for (var k = 0; k < 14; k++) { var a = k * 2.2f; var r = (k % 4) * 1.6f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r); mk.Blob(snow, p, new Vector3(1.6f, .25f + (k % 3) * .1f, 1.3f), ctx.Rng, .2f); }
            var post = c + face * .5f; mk.Prism(iron, post, up, .07f, 3.2f, 6); ArtistBits.PostCollider(ctx.Root, post, 3.2f, .12f);
            mk.Blob(SongGlow(id, Frost, 3f), post + up * 3.35f, Vector3.one * .22f, ctx.Rng, 0f);
            var flake = SongGlow(id, Color.white, 1.6f);
            for (var k = 0; k < 70; k++) mk.Blob(flake, c + new Vector3(ctx.Range(-6f, 6f), ctx.Range(.4f, 6f), ctx.Range(-6f, 6f)), Vector3.one * ctx.Range(.035f, .07f), ctx.Rng, 0f);
            Lit(ctx, post + up * 3f, Frost, 3.4f);
        }

        void Gauntlet(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var flame = SongGlow(id, Moon, 2.6f);
            for (var k = 0; k < 5; k++) foreach (var s in new[] { -1f, 1f })
            {
                var p = ctx.OnGround((c + face * (4f - k * 2f) + side * s * 1.8f).x, (c + face * (4f - k * 2f) + side * s * 1.8f).z);
                mk.Box(stone, p + up * .9f, new Vector3(.6f, 1.8f + k * .25f, .6f), Quaternion.LookRotation(face)); solid.Box(stone, p + up * .9f, new Vector3(.6f, 1.8f + k * .25f, .6f), Quaternion.LookRotation(face));
                var top = p + up * (1.8f + k * .125f);   // the post's top
                mk.Prism(iron, top, up, .14f, .12f, 8, 1.4f);
                mk.Blob(flame, top + up * .3f, new Vector3(.12f, .26f, .12f), ctx.Rng, .1f);
                if (k % 2 == 0) halos.Add(top + up * .5f, 1f, Moon, .6f);
            }
            var end = c - face * 5f; end = ctx.OnGround(end.x, end.z);
            mk.Box(SongGlow(id, Orchid, 2f), end + up * .03f, new Vector3(1.6f, .04f, 1.6f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 45f, 0));
            Lit(ctx, c + up * 3f, Moon, 3f);
        }

        void Lectern(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Box(stone, c + up * .55f, new Vector3(.5f, 1.1f, .4f), rot); solid.Box(stone, c + up * .6f, new Vector3(.7f, 1.2f, .6f), rot);
            mk.Box(darkWood, c + up * 1.2f, new Vector3(.9f, .08f, .7f), rot * Quaternion.Euler(-20f, 0, 0));
            var page = SongGlow(id, Teal, 2f);
            foreach (var s in new[] { -1f, 1f }) mk.Box(page, c + up * 1.27f + Vector3.Cross(up, face) * s * .19f + face * .02f, new Vector3(.36f, .03f, .5f), rot * Quaternion.Euler(-20f, 0, s * -6f));
            for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.6f; mk.Blob(SongGlow(id, k % 2 == 0 ? Orchid : Lime, 2.2f), p + up * (1.8f + Mathf.Sin(k * 1.3f) * .4f), Vector3.one * .12f, ctx.Rng, 0f); }
            Lit(ctx, c + up * 1.9f + face * .8f, Teal);
        }

        // ---------------------------------------------------------------- by each one-song artist's door
        static readonly Dictionary<string, string> PieceSong = new Dictionary<string, string>
        {
            { "Boowah", "23JWODlgLBsOsPYXFhcMoX" }, { "Carmen Silver", "4jN8Kp4pOkZH9zWCYShMEB" }, { "Fionavair", "00MVhRZgrfH1ZXZ5CRZwPn" },
            { "Hailey Picardi", "1yqQANXMu1CgGTjHQewP6G" }, { "John Luther", "72AEEEFQTX7XhlG8ErcgXK" }, { "Josie on the Rocks", "302l8jYtUfShewSscMur7U" },
            { "Sōra", "4MZbreFPwOwkOtYVVSHxdr" }, { "runfromobm", "1fnhrSxgOtZQiPRAVnumnp" },
        };

        void HomeSongPiece(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, IslandWorldData.Artist a, Vector3 home, Vector3 front, Vector3 side)
        {
            if (!PieceSong.TryGetValue(a.name, out var id)) return;
            var up = Vector3.up; var rot = Quaternion.LookRotation(front);
            var p = home + front * 6.2f - side * 3.2f; p = ctx.OnGround(p.x, p.z);
            switch (a.name)
            {
                case "Boowah":   // "Nutshell": half a walnut shell with a light inside
                    mk.Blob(WorldMaterials.Solid(new Color(.42f, .3f, .2f)), p + up * .35f, new Vector3(.8f, .5f, .7f), ctx.Rng, .25f);
                    mk.Blob(SongGlow(id, WindowGlow, 2.4f), p + up * .75f, new Vector3(.45f, .2f, .4f), ctx.Rng, .1f);
                    ArtistBits.PostCollider(root, p, .9f, .7f); Lit(ctx, p + up * 1.2f, WindowGlow, 1.6f); break;
                case "Carmen Silver":   // "Suspended Particles": motes hanging over the garden
                    var mote = SongGlow(id, new Color(1f, .9f, .7f), 2.6f);
                    for (var k = 0; k < 40; k++) mk.Blob(mote, p + new Vector3(ctx.Range(-1.4f, 1.4f), ctx.Range(.6f, 2.6f), ctx.Range(-1.4f, 1.4f)), Vector3.one * ctx.Range(.025f, .05f), ctx.Rng, 0f);
                    Lit(ctx, p + up * 1.6f, new Color(1f, .85f, .6f), 1.8f); break;
                case "Fionavair":   // "Familyguy": an old TV on the step, its screen glowing
                    mk.Box(WorldMaterials.Solid(new Color(.25f, .22f, .2f)), p + up * .35f, new Vector3(.8f, .7f, .7f), rot); solid.Box(darkWood, p + up * .35f, new Vector3(.8f, .7f, .7f), rot);
                    mk.Box(SongGlow(id, new Color(.55f, .7f, 1f), 2.2f), p + up * .38f + front * .36f, new Vector3(.6f, .48f, .03f), rot);
                    mk.Box(iron, p + up * .8f, new Vector3(.02f, .3f, .02f), rot * Quaternion.Euler(0, 0, 30f)); mk.Box(iron, p + up * .8f, new Vector3(.02f, .3f, .02f), rot * Quaternion.Euler(0, 0, -30f));
                    Lit(ctx, p + up * .9f + front * .8f, new Color(.55f, .7f, 1f), 1.4f); break;
                case "Hailey Picardi":   // "chaotic": a glowing scribble on a post
                    mk.Prism(darkWood, p, up, .05f, 1.2f, 6); ArtistBits.PostCollider(root, p, 1.2f, .1f);
                    var ink = SongGlow(id, new Color(1f, .4f, .75f), 2.4f); var q = p + up * 1.8f;
                    for (var k = 0; k < 16; k++) { var n = q + new Vector3(Mathf.Sin(k * 2.7f) * .5f, Mathf.Cos(k * 1.9f) * .4f, Mathf.Sin(k * 1.3f) * .3f); mk.Box(ink, (q + n) / 2f, new Vector3(.04f, .04f, Vector3.Distance(q, n) + .04f), Quaternion.LookRotation(n - q + Vector3.one * .001f)); q = n; }
                    Lit(ctx, p + up * 1.8f + front * .6f, new Color(1f, .4f, .75f), 1.6f); break;
                case "John Luther":   // "Inhale": a big pot, its leaves glowing green
                    mk.Prism(WorldMaterials.Solid(new Color(.55f, .32f, .22f)), p, up, .45f, .6f, 10, 1.25f); solid.Prism(darkWood, p, up, .55f, .6f, 8);
                    var lf = SongGlow(id, new Color(.45f, 1f, .5f), 1.4f);
                    for (var k = 0; k < 9; k++) { var a2 = k * .7f; var d = new Vector3(Mathf.Cos(a2), 1.4f + (k % 3) * .4f, Mathf.Sin(a2)).normalized; mk.Box(lf, p + up * .6f + d * .7f, new Vector3(.25f, .03f, .7f), Quaternion.LookRotation(d) * Quaternion.Euler(90f, 0, 0)); }
                    Lit(ctx, p + up * 1.6f, new Color(.45f, 1f, .5f), 1.6f); break;
                case "Josie on the Rocks":   // "Not You": a garden gate with a lit sign hung on it
                    foreach (var s in new[] { -1f, 1f }) { mk.Box(stone, p + side * s * .8f + up * .55f, new Vector3(.3f, 1.1f, .3f), rot); solid.Box(stone, p + side * s * .8f + up * .55f, new Vector3(.3f, 1.1f, .3f), rot); }
                    mk.Box(darkWood, p + up * .55f, new Vector3(1.25f, .9f, .05f), rot);
                    mk.Box(SongGlow(id, new Color(1f, .45f, .35f), 2f), p + up * .7f + front * .04f, new Vector3(.8f, .28f, .03f), rot);
                    ArtistBits.Label(ctx.Root, "NOT YOU", p + up * .7f + front * .06f, front, .12f, new Color(.2f, .05f, .05f), TextAnchor.MiddleCenter);
                    Lit(ctx, p + up * 1.2f + front * .6f, new Color(1f, .5f, .4f), 1.4f); break;
                case "Sōra":   // "Streetlight": a puddle under the street lamp, holding its light
                    var pd = home + front * 6.8f - side * 2.2f; pd = ctx.OnGround(pd.x, pd.z);
                    mk.Prism(SongGlow(id, Sodium, 1.4f), pd + up * .03f, up, 1.1f, .015f, 12, 1f, 20f);
                    halos.Add(pd + up * .2f, 1.4f, Sodium, .45f); break;
                case "runfromobm":   // "Grilled Cheese Sandwich": a toastie on a plate on a garden table
                    mk.Prism(darkWood, p, up, .05f, .7f, 6); mk.Box(darkWood, p + up * .72f, new Vector3(.9f, .05f, .9f), rot); solid.Box(darkWood, p + up * .37f, new Vector3(.9f, .75f, .9f), rot);
                    mk.Prism(WorldMaterials.Solid(new Color(.95f, .95f, .92f)), p + up * .75f, up, .3f, .02f, 14);
                    var bread = WorldMaterials.Solid(new Color(.78f, .55f, .28f));
                    mk.Box(bread, p + up * .8f, new Vector3(.36f, .05f, .36f), rot * Quaternion.Euler(0, 45f, 0)); mk.Box(bread, p + up * .9f, new Vector3(.36f, .05f, .36f), rot * Quaternion.Euler(0, 45f, 0));
                    mk.Box(SongGlow(id, new Color(1f, .78f, .2f), 2.4f), p + up * .85f, new Vector3(.4f, .04f, .4f), rot * Quaternion.Euler(0, 45f, 0));
                    Lit(ctx, p + up * 1.4f, new Color(1f, .8f, .4f), 1.4f); break;
            }
        }
    }
}
