using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The legends' worlds (part of ClassicKit): the four biggest bands each get places out in the Kingdom for their
    /// songs, reached by their own trails, each with a stone sign and a glow that wakes while its song plays
    /// (evoking the songs, never copying a cover):
    /// - The Beatles: a barber's shop with a striped pole on a little lane ("Penny Lane"), a giant egg sitting on a
    ///   wall ("I Am The Walrus"), a market barrow piled with fruit under a striped awning ("Ob-La-Di, Ob-La-Da"), a
    ///   saloon front with swinging doors ("Rocky Raccoon"), a ring of music stands round a conductor's podium
    ///   ("A Day In The Life").
    /// - Led Zeppelin: a Viking longship on the grass, shields along its side ("Immigrant Song"), a desert caravan's
    ///   tent with lanterns and camels on a patch of sand ("Kashmir"), a misty little mountain with a round green door
    ///   in its foot ("Misty Mountain Hop", "Ramble On"), an earth levee with water pouring through a break ("When the
    ///   Levee Breaks").
    /// - David Bowie: a small red planet turning over a plinth ("Life on Mars?"), a radio dish pointed at a waiting
    ///   star ("Starman"), a glittering stage with a lightning-bolt floor ("Moonage Daydream"), a crystal ball floating
    ///   over a ballroom floor ("As The World Falls Down").
    /// - Jimi Hendrix: a stone watchtower with a lantern at the top ("All Along the Watchtower"), sandcastles on a little
    ///   beach ("Castles Made of Sand"), wind chimes hung in a tree ("The Wind Cries Mary"), a rock split in two with
    ///   light in the crack ("Voodoo Child").
    /// And the Kingdom itself: a castle on a hill (towers, battlements, a gatehouse, banners) and a village pub.
    public partial class ClassicKit
    {
        public const string PennyLane = "1h04XMpzGzmAudoI6VHBgA", Walrus = "6Pq9MmkDQYZiiCDpxnvrf6", ObLaDi = "1gFNm7cXfG1vSMcxPpSxec", Rocky = "1ITQbrueGLl581a25XXm9c",
            DayInTheLife = "0hKRSZhUGEhKU6aNSPBACZ", Immigrant = "78lgmZwycJ3nzsdgmPPGNx", Kashmir = "6Vjk8MNXpQpi0F4BefdTyq", MistyMountain = "3frf3zTgzt3YIx9ylpUoqU",
            RambleOn = "3MODES4TNtygekLl146Dxd", Levee = "05f8Hg3RSfiPSCBQOtxl3i", LifeOnMars = "3ZE3wv8V3w2T2f7nOCjV0N", Starman = "0pQskrTITgmCMyr85tb9qq",
            Moonage = "6mib3N4E8PZHAGQ3xy7bho", WorldFallsDown = "2elAyF0DLb10MWj76pU6pP", Watchtower = "2aoo2jlRnM3A0NyLQqMN2f", CastlesSand = "3qebSLgUwaEqbwCM5sj5Kh",
            WindCriesMary = "5H6Jp0syB5yEPk7SWYdlmk", VoodooChild = "2AxCeJ6PSsBYiTckM0HLY7";

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
                var wide = tries > 500; var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(Foot2(near) + clear + 6f, wide ? 280f : 120f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(px, pz, clear + 8f) || !Open(ctx, px, pz, clear)) continue;
                if (!Gentle(ctx, px, pz, clear + 4f, tries > 700 ? clear * .4f : clear * .2f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 22f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); noGrass.Add(new Vector3(p.x, clear * .85f, p.z)); return p;
            }
            return Vector3.positiveInfinity;
        }
        float Foot2(Vector3 near) => 20f;

        Vector3 PlaceWay(LandContext ctx, MeshKit mk, Vector3 c, float clear, string sign, string mark)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(center.x - c.x, 0, center.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var stop = clear - 1f;
            if (best < float.MaxValue && best > stop + 3f)
            {
                var pts = new List<Vector3>(); var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - stop;
                for (var d = RoadW / 2f + .1f; ; d += 1.5f) { var dd = Mathf.Min(d, len); var q = from + dir * dd; if (!ctx.IsLand(q.x, q.z, 0f)) break; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count > 1) { Strip(mk, pts, WorldMaterials.Solid(new Color(.66f, .6f, .48f)), 2.4f, .12f); foreach (var p in pts) Remember(p, 350); }
            }
            var side = Vector3.Cross(Vector3.up, face); var sp = ctx.OnGround(c.x + face.x * stop + side.x * 2.4f, c.z + face.z * stop + side.z * 2.4f); var rot = Quaternion.LookRotation(face);
            mk.Box(stoneM, sp + Vector3.up * .6f, new Vector3(.25f, 1.2f, .25f), rot); mk.Box(stoneM, sp + Vector3.up * 1.45f, new Vector3(2.8f, .6f, .14f), rot);
            mk.Box(GlowM(new Color(1f, .85f, .5f), 1.6f), sp + Vector3.up * 1.12f + face * .08f, new Vector3(2.6f, .04f, .02f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.45f + face * .08f, face, .18f, new Color(.25f, .2f, .15f), TextAnchor.MiddleCenter);
            ArtistBits.PostCollider(ctx.Root, sp, 1.8f, .2f);
            Lamp(ctx, mk, ctx.Root, ctx.OnGround(c.x + face.x * stop - side.x * 2.4f, c.z + face.z * stop - side.z * 2.4f));
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 6f) - side * 6f; cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.2f; var look = c + Vector3.up * 2.2f - cam - Vector3.up * 1.6f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            return face;
        }

        void Legends(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            placeSpots.Clear(); foreach (var a in ctx.Artists) placeSpots.Add(ctx.OnGround(a.x, a.z));
            Vector3 Near(string n) { var a = ctx.Artists.Find(x => x.name == n); return a != null ? ctx.OnGround(a.x, a.z) : center; }
            var placed = 0;
            void Place(Vector3 near, float clear, string sign, string mark, System.Action<Vector3, Vector3> build)
            {
                var c = PlaceSpot(ctx, near, clear); if (float.IsInfinity(c.x)) { Debug.Log("Kingdom of Classic Rock: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clear, sign, mark)); placed++;
            }
            var beatles = Near("The Beatles"); var zep = Near("Led Zeppelin"); var bowie = Near("David Bowie"); var jimi = Near("Jimi Hendrix");
            Place(beatles, 9f, "PENNY LANE", "Penny Lane", (c, f) => PennyLaneShop(ctx, mk, solid, c, f));
            Place(beatles, 7f, "I AM THE WALRUS", "the egg on the wall", (c, f) => EggWall(ctx, mk, solid, c, f));
            Place(beatles, 7f, "OB-LA-DI, OB-LA-DA", "the market barrow", (c, f) => Barrow(ctx, mk, solid, c, f));
            Place(beatles, 8f, "ROCKY RACCOON", "the saloon", (c, f) => Saloon(ctx, mk, solid, c, f));
            Place(beatles, 9f, "A DAY IN THE LIFE", "the orchestra", (c, f) => Orchestra(ctx, mk, solid, c, f));
            Place(zep, 11f, "IMMIGRANT SONG", "the longship", (c, f) => Longship(ctx, mk, solid, c, f));
            Place(zep, 10f, "KASHMIR", "the caravan", (c, f) => Caravan(ctx, mk, solid, c, f));
            Place(zep, 10f, "MISTY MOUNTAIN HOP", "the misty mountain", (c, f) => MistyHill(ctx, mk, solid, c, f));
            Place(zep, 10f, "WHEN THE LEVEE BREAKS", "the levee", (c, f) => LeveeWall(ctx, mk, solid, c, f));
            Place(bowie, 8f, "LIFE ON MARS?", "the red planet", (c, f) => RedPlanet(ctx, mk, solid, c, f));
            Place(bowie, 8f, "STARMAN", "the radio dish", (c, f) => Dish(ctx, mk, solid, c, f));
            Place(bowie, 9f, "MOONAGE DAYDREAM", "the glitter stage", (c, f) => GlitterStage(ctx, mk, solid, c, f));
            Place(bowie, 8f, "AS THE WORLD FALLS DOWN", "the crystal ball", (c, f) => Ballroom(ctx, mk, solid, c, f));
            Place(jimi, 8f, "ALL ALONG THE WATCHTOWER", "the watchtower", (c, f) => WatchtowerPlace(ctx, mk, solid, c, f));
            Place(jimi, 8f, "CASTLES MADE OF SAND", "the sandcastles", (c, f) => Sandcastles(ctx, mk, solid, c, f));
            Place(jimi, 7f, "THE WIND CRIES MARY", "the wind chimes", (c, f) => Chimes(ctx, mk, solid, c, f));
            Place(jimi, 8f, "VOODOO CHILD", "the split rock", (c, f) => SplitRock(ctx, mk, solid, c, f));
            Debug.Log($"Kingdom of Classic Rock: {placed} legend places");
        }

        // ---------------------------------------------------------------- The Beatles
        void PennyLaneShop(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var brick = WorldMaterials.Solid(new Color(.6f, .35f, .28f));
            var b = c - face * 1.5f; mk.Box(brick, b + up * 1.8f, new Vector3(6f, 3.6f, 4f), rot); solid.Box(brick, b + up * 1.8f, new Vector3(6f, 3.6f, 4f), rot);
            mk.Box(dark, b + up * 3.75f, new Vector3(6.3f, .3f, 4.3f), rot); var fr = b + face * 2.01f;
            mk.Box(GlowM(new Color(1f, .88f, .6f), 1.5f), fr + up * 1.6f + side * 1.2f, new Vector3(2.6f, 1.8f, .03f), rot); mk.Box(dark, fr + up * 1.1f - side * 1.6f, new Vector3(1f, 2.2f, .03f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .3f, .2f)), fr + up * 3.1f, new Vector3(5f, .6f, .05f), rot);
            ArtistBits.Label(ctx.Root, "BARBER", fr + up * 3.1f + face * .04f, face, .3f, new Color(1f, .9f, .6f), TextAnchor.MiddleCenter);
            var pole = fr + face * .3f - side * 2.6f + up * 2f; var stripe = SongGlow(PennyLane, new Color(1f, .2f, .2f), 1.6f);
            mk.Prism(white, pole - up * .6f, up, .14f, 1.2f, 10); for (var k = 0; k < 4; k++) mk.Box(stripe, pole + up * (-.45f + k * .3f), new Vector3(.3f, .08f, .3f), rot * Quaternion.Euler(0, 0, 30f));
            mk.Blob(metal, pole + up * .7f, Vector3.one * .16f, ctx.Rng, 0f);
            var bench = c + face * 2.5f + side * 2f; bench = ctx.OnGround(bench.x, bench.z); mk.Box(wood, bench + up * .45f, new Vector3(1.8f, .08f, .45f), rot); solid.Box(wood, bench + up * .4f, new Vector3(1.8f, .8f, .5f), rot);
        }

        void EggWall(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Box(wallM, c + up * 1f, new Vector3(6f, 2f, 1.2f), rot); solid.Box(wallM, c + up * 1f, new Vector3(6f, 2f, 1.2f), rot);
            for (var k = 0; k < 6; k++) mk.Box(stoneM, c + up * 2.05f + Vector3.Cross(up, face) * (k - 2.5f), new Vector3(.9f, .2f, 1.3f), rot);
            var shell = WorldMaterials.Solid(new Color(.72f, .84f, .96f)); var egg = c + up * 3.6f; mk.Blob(shell, egg, new Vector3(1.1f, 1.45f, 1.1f), ctx.Rng, 0f);
            var crack = SongGlow(Walrus, new Color(1f, .85f, .3f), 2f);
            for (var k = 0; k < 6; k++) { var p = egg + face * 1.05f + Vector3.Cross(up, face) * ((k - 2.5f) * .3f) + up * ((k % 2) * .25f + .1f); mk.Box(crack, p, new Vector3(.32f, .05f, .03f), rot * Quaternion.Euler(0, 0, k % 2 == 0 ? 35f : -35f)); }
            halos.Add(egg + face, 2.2f, new Color(1f, .85f, .3f), .3f);
        }

        void Barrow(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, c + up * .9f, new Vector3(3f, .2f, 1.6f), rot); foreach (var s in new[] { -1f, 1f }) mk.Prism(dark, c + side * s * 1.3f + up * .45f, side * s, .45f, .12f, 12); solid.Box(wood, c + up * .7f, new Vector3(3f, 1.4f, 1.6f), rot);
            Color[] fruit = { new Color(1f, .5f, .1f), new Color(.9f, .15f, .15f), new Color(1f, .9f, .2f), new Color(.4f, .8f, .2f) };
            for (var k = 0; k < 24; k++) mk.Blob(WorldMaterials.Solid(fruit[k % 4], .6f), c + up * (1.1f + (k / 8) * .18f) + side * ((k % 8) * .35f - 1.2f) + face * (((k * 3) % 4) * .3f - .45f), Vector3.one * .16f, ctx.Rng, 0f);
            var aw = SongGlow(ObLaDi, new Color(1f, .5f, .7f), 1f);
            foreach (var s in new[] { -1.4f, 1.4f }) foreach (var f in new[] { -.7f, .7f }) mk.Prism(wood, c + side * s + face * f + up * .9f, up, .05f, 1.6f, 6);
            for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? aw : white, c + up * 2.6f + side * ((k - 2.5f) * .5f), new Vector3(.5f, .06f, 1.8f), rot * Quaternion.Euler(-10f, 0, 0));
        }

        void Saloon(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var b = c - face * 1.5f;
            mk.Box(wood, b + up * 2f, new Vector3(7f, 4f, 4f), rot); solid.Box(wood, b + up * 2f, new Vector3(7f, 4f, 4f), rot);
            mk.Box(wood, b + face * 2.05f + up * 5f, new Vector3(7f, 2f, .2f), rot);
            ArtistBits.Label(ctx.Root, "SALOON", b + face * 2.17f + up * 5f, face, .6f, new Color(.25f, .15f, .08f), TextAnchor.MiddleCenter);
            var glow = SongGlow(Rocky, new Color(1f, .75f, .35f), 1.6f); var fr = b + face * 2.02f;
            mk.Box(glow, fr + up * 1.3f, new Vector3(1.6f, 2.4f, .03f), rot); foreach (var s in new[] { -.4f, .4f }) mk.Box(wood, fr + face * .1f + up * 1.3f + side * s, new Vector3(.75f, 1f, .05f), rot * Quaternion.Euler(0, s > 0 ? 25f : -25f, 0));
            foreach (var s in new[] { -2.4f, 2.4f }) mk.Box(glow, fr + up * 2.2f + side * s, new Vector3(1.2f, 1f, .03f), rot);
            mk.Box(wood, b + face * 3f + up * .15f, new Vector3(7f, .3f, 2f), rot); solid.Box(wood, b + face * 3f + up * .15f, new Vector3(7f, .3f, 2f), rot);
            foreach (var s in new[] { -3.2f, 3.2f }) { mk.Box(wood, b + face * 3.8f + side * s + up * 1.8f, new Vector3(.15f, 3.3f, .15f), rot); ArtistBits.PostCollider(ctx.Root, b + face * 3.8f + side * s, 3.3f, .12f); }
            mk.Box(wood, b + face * 3f + up * 3.5f, new Vector3(7.2f, .1f, 2.2f), rot * Quaternion.Euler(8f, 0, 0));
        }

        void Orchestra(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var lampM = SongGlow(DayInTheLife, new Color(1f, .9f, .65f), 2f);
            mk.Prism(wood, c - up * .2f, up, 1f, .6f, 10); mk.Box(dark, c + up * .9f, new Vector3(.6f, 1f, .1f), Quaternion.LookRotation(face)); solid.Prism(wood, c - up * .2f, up, 1f, 1.2f, 10);
            for (var k = 0; k < 14; k++)
            {
                var a = Mathf.Atan2(face.z, face.x) + Mathf.PI + (k - 6.5f) * .2f; var r = 4f + (k % 2) * 1.6f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r); var to = (c - p); to.y = 0; var rot = Quaternion.LookRotation(to.normalized);
                mk.Prism(dark, p, up, .03f, 1.1f, 6); mk.Box(dark, p + up * 1.15f + to.normalized * .05f, new Vector3(.5f, .35f, .03f), rot * Quaternion.Euler(-25f, 0, 0));
                mk.Blob(lampM, p + up * 1.4f, new Vector3(.06f, .05f, .06f), ctx.Rng, 0f);
            }
            halos.Add(c + up * 1.5f, 5f, new Color(1f, .9f, .65f), .2f);
        }

        // ---------------------------------------------------------------- Led Zeppelin
        void Longship(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var hull = WorldMaterials.Solid(new Color(.42f, .3f, .2f)); var dir = side; var rot = Quaternion.LookRotation(dir);
            for (var k = 0; k < 10; k++) { var t = (k - 4.5f) / 4.5f; var w = 1.6f * (1f - t * t * .7f); mk.Box(hull, c + dir * (t * 6f) + up * (.9f + t * t * .6f), new Vector3(w * 2f, .9f, 1.3f), rot); }
            foreach (var e in new[] { -1f, 1f }) { var p = c + dir * e * 6.6f + up * 1.8f; mk.Prism(hull, p, (up + dir * e * .4f).normalized, .25f, 2.6f, 6, .4f); }
            solid.Box(hull, c + up * 1f, new Vector3(3f, 2f, 13f), rot);
            mk.Prism(wood, c + up * 1.3f, up, .15f, 7f, 6); var sail = SongGlow(Immigrant, new Color(.9f, .2f, .15f), .7f);
            for (var k = 0; k < 5; k++) mk.Box(k % 2 == 0 ? sail : white, c + up * 5.5f + dir * ((k - 2f) * .8f), new Vector3(.08f, 3.4f, .8f), rot);
            var shieldC = new[] { new Color(.8f, .7f, .2f), new Color(.2f, .35f, .7f), new Color(.75f, .15f, .15f) };
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 6; k++) mk.Prism(WorldMaterials.Solid(shieldC[k % 3]), c + side * s * 1.45f + dir * ((k - 2.5f) * 1.8f) + up * 1.3f, side * s, .4f, .06f, 12);
        }

        void Caravan(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var sandM = WorldMaterials.Solid(new Color(.9f, .78f, .5f));
            for (var k = 0; k < 4; k++) mk.Blob(sandM, c + new Vector3(Mathf.Cos(k * 1.7f), -.2f, Mathf.Sin(k * 1.7f)) * 3.5f, new Vector3(3.4f, .7f, 2.6f), ctx.Rng, .1f);
            var tent = WorldMaterials.Solid(new Color(.75f, .45f, .3f)); mk.Cone(tent, c + up * .1f, 2.6f, 3.2f, 8); solid.Prism(tent, c, up, 2.4f, 2.2f, 8);
            var lan = SongGlow(Kashmir, new Color(1f, .6f, .2f), 2.4f);
            for (var k = 0; k < 5; k++) { var a = k * 1.25f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 4.2f, c.z + Mathf.Sin(a) * 4.2f); mk.Prism(wood, p, up, .05f, 1.8f, 6); mk.Prism(lan, p + up * 1.8f, up, .16f, .3f, 8); halos.Add(p + up * 1.95f, .9f, new Color(1f, .6f, .2f), .7f); }
            var camel = WorldMaterials.Solid(new Color(.7f, .55f, .35f)); var side = Vector3.Cross(up, face);
            foreach (var s in new[] { -5f, 5.5f })
            {
                var p = ctx.OnGround(c.x + side.x * s - face.x * 2f, c.z + side.z * s - face.z * 2f); var d = face; var r = Quaternion.LookRotation(d);
                mk.Box(camel, p + up * 1.6f, new Vector3(.7f, .7f, 1.6f), r); mk.Blob(camel, p + up * 2.1f, new Vector3(.35f, .35f, .4f), ctx.Rng, 0f);
                mk.Box(camel, p + up * 2f + d * 1f, new Vector3(.25f, 1f, .25f), r * Quaternion.Euler(-30f, 0, 0)); mk.Box(camel, p + up * 2.5f + d * 1.4f, new Vector3(.25f, .25f, .5f), r);
                foreach (var a in new[] { -.25f, .25f }) foreach (var b in new[] { -.6f, .6f }) mk.Box(camel, p + Vector3.Cross(up, d) * a + d * b + up * .65f, new Vector3(.12f, 1.3f, .12f), r);
                solid.Box(camel, p + up * 1.3f, new Vector3(.8f, 2.6f, 2.2f), r);
            }
        }

        void MistyHill(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var hill = WorldMaterials.Solid(new Color(.38f, .5f, .32f));
            mk.Blob(hill, c - face * 2f + up * 1.5f, new Vector3(5f, 4f, 4.5f), ctx.Rng, .2f); mk.Cone(stoneM, c - face * 2.5f + up * 4.4f, 1.6f, 2.6f, 7);
            solid.Box(hill, c - face * 2f + up * 2f, new Vector3(8f, 4f, 7f), Quaternion.LookRotation(face));
            var door = c + face * 1.9f + up * 1.2f; var green = WorldMaterials.Solid(new Color(.15f, .45f, .25f));
            mk.Prism(wood, door - face * .05f, face, 1.25f, .12f, 18); mk.Prism(green, door, face, 1.1f, .08f, 18); mk.Blob(SongGlow(RambleOn, new Color(1f, .85f, .4f), 2.6f), door + face * .1f, Vector3.one * .1f, ctx.Rng, 0f);
            var win = SongGlow(MistyMountain, new Color(1f, .85f, .5f), 1.4f); mk.Prism(win, door + Vector3.Cross(up, face) * 2f + up * .3f + face * -.1f, face, .4f, .05f, 12);
            life.mists.Add(c - face * 2f + up * 3f);
        }

        void LeveeWall(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var earth = WorldMaterials.Solid(new Color(.45f, .38f, .28f)); var rot = Quaternion.LookRotation(face);
            foreach (var s in new[] { -1f, 1f }) { var p = c - face * 1f + side * s * 4f; mk.Box(earth, p + up * 1.2f, new Vector3(6f, 2.4f, 2.4f), rot * Quaternion.Euler(0, 0, s * 4f)); solid.Box(earth, p + up * 1.2f, new Vector3(6f, 2.4f, 2.4f), rot); }
            var water = SongGlow(Levee, new Color(.3f, .6f, 1f), .9f);
            mk.Box(water, c - face * 4f + up * 1.9f, new Vector3(16f, .08f, 4f), rot);
            for (var k = 0; k < 8; k++) mk.Box(water, c - face * (.9f - k * .5f) + up * Mathf.Max(.06f, 1.9f - k * .32f), new Vector3(1.6f, .08f, .6f), rot * Quaternion.Euler(30f, 0, 0));
            mk.Box(water, c + face * 3.2f + up * .06f, new Vector3(4f, .04f, 3f), rot);
            for (var k = 0; k < 6; k++) mk.Box(earth, c - face * (2.6f + k * .2f) + side * ((k - 2.5f) * 2.8f) + up * .9f, new Vector3(2.5f, 1.8f, .3f), rot);
        }

        // ---------------------------------------------------------------- David Bowie
        void RedPlanet(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; mk.Prism(stoneM, c - up * .2f, up, 1f, 1.4f, 10); solid.Prism(stoneM, c - up * .2f, up, 1f, 4f, 10);
            var pk = new MeshKit(); pk.Blob(SongGlow(LifeOnMars, new Color(.9f, .3f, .15f), .8f), Vector3.zero, Vector3.one * 1.4f, ctx.Rng, .15f);
            for (var k = 0; k < 5; k++) pk.Blob(WorldMaterials.Solid(new Color(.55f, .2f, .12f)), Quaternion.Euler(k * 40f, k * 70f, 0) * Vector3.forward * 1.35f, new Vector3(.35f, .35f, .1f), ctx.Rng, 0f);
            var t = pk.BuildSingle("Red planet", life.transform, false).transform; t.position = c + up * 3f; life.spinners.Add(t);
            halos.Add(c + up * 3f, 2.6f, new Color(.9f, .35f, .2f), .3f);
            var rv = c + face * 2f; rv = ctx.OnGround(rv.x, rv.z); mk.Box(metal, rv + up * .4f, new Vector3(.8f, .3f, 1.2f), Quaternion.LookRotation(face)); foreach (var s in new[] { -.45f, .45f }) foreach (var f in new[] { -.4f, .4f }) mk.Prism(dark, rv + Vector3.Cross(up, face) * s + face * f + up * .2f, Vector3.Cross(up, face) * Mathf.Sign(s), .2f, .1f, 8);
        }

        void Dish(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; mk.Prism(metal, c, up, .3f, 3f, 8); ArtistBits.PostCollider(ctx.Root, c, 3f, .4f);
            var aim = (up * 1f + face * .5f).normalized; var dc = c + up * 3.2f;
            mk.Prism(white, dc - aim * .3f, aim, 2.4f, .5f, 20, .3f); mk.Prism(metal, dc, aim, .06f, 1.6f, 6);
            var star = dc + aim * 16f; mk.Blob(SongGlow(Starman, new Color(.85f, .9f, 1f), 3f), star, Vector3.one * .5f, ctx.Rng, 0f); halos.Add(star, 3f, new Color(.85f, .9f, 1f), .6f);
        }

        void GlitterStage(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(dark, c + up * .4f, new Vector3(7f, .8f, 4.5f), rot); solid.Box(dark, c + up * .4f, new Vector3(7f, .8f, 4.5f), rot);
            var bolt = SongGlow(Moonage, new Color(.3f, .6f, 1f), 2f); var red = SongGlow(Moonage, new Color(1f, .2f, .2f), 2f);
            Vector3[] pts = { new Vector3(-.4f, 0, 2f), new Vector3(.8f, 0, .3f), new Vector3(-.2f, 0, .3f), new Vector3(.5f, 0, -2f) };
            for (var k = 0; k + 1 < pts.Length; k++) { var a = c + up * .82f + side * pts[k].x * 2f + face * pts[k].z; var b = c + up * .82f + side * pts[k + 1].x * 2f + face * pts[k + 1].z; mk.Box(k % 2 == 0 ? red : bolt, (a + b) / 2f, new Vector3(.6f, .02f, Vector3.Distance(a, b) + .3f), Quaternion.LookRotation(b - a)); }
            for (var k = 0; k < 30; k++) mk.Blob(GlowM(new Color(1f, .9f, .95f), 2.6f), c + up * (2f + (k % 5) * .7f) - face * 2.2f + side * ((k * 1.37f) % 6.4f - 3.2f), Vector3.one * .05f, ctx.Rng, 0f);
            mk.Prism(metal, c + up * .8f + face * 1f, up, .03f, 1.5f, 6);
            halos.Add(c + up * 2f, 4f, new Color(.6f, .6f, 1f), .3f);
        }

        void Ballroom(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            for (var i = 0; i < 6; i++) for (var j = 0; j < 6; j++) mk.Box((i + j) % 2 == 0 ? white : dark, c + Vector3.Cross(up, face) * ((i - 2.5f) * 1.1f) + face * ((j - 2.5f) * 1.1f) + up * .05f, new Vector3(1.08f, .1f, 1.08f), rot);
            foreach (var k in new[] { 0, 1, 2, 3 }) { var a = k * Mathf.PI / 2f + .78f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 4.4f, c.z + Mathf.Sin(a) * 4.4f); mk.Prism(white, p, up, .25f, 3.4f, 10); ArtistBits.PostCollider(ctx.Root, p, 3.4f, .3f); mk.Blob(GlowM(new Color(1f, .9f, .7f), 2.2f), p + up * 3.6f, Vector3.one * .25f, ctx.Rng, 0f); }
            var bk = new MeshKit(); bk.Blob(SongGlow(WorldFallsDown, new Color(.75f, .85f, 1f), 1.4f), Vector3.zero, Vector3.one * .7f, ctx.Rng, 0f);
            var t = bk.BuildSingle("Crystal ball", life.transform, false).transform; t.position = c + up * 2.4f; life.floaters.Add((t, c + up * 2.4f));
            halos.Add(c + up * 2.4f, 2.4f, new Color(.75f, .85f, 1f), .5f);
        }

        // ---------------------------------------------------------------- Jimi Hendrix
        void WatchtowerPlace(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; mk.Prism(stoneM, c - up * .3f, up, 2.2f, 10f, 8, .85f); solid.Prism(stoneM, c - up * .3f, up, 2.2f, 10f, 8);
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; mk.Box(stoneM, c + up * 10.1f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.7f, new Vector3(.7f, .8f, .5f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            mk.Box(dark, c + up * 1.2f + face * 1.95f, new Vector3(1f, 2.2f, .3f), Quaternion.LookRotation(face));
            var lan = SongGlow(Watchtower, new Color(1f, .75f, .35f), 2.6f); mk.Prism(lan, c + up * 10.3f, up, .25f, .6f, 8); halos.Add(c + up * 10.6f, 2f, new Color(1f, .75f, .35f), .7f);
            ArtistBits.PointLight(ctx.Root, c + up * 11f, new Color(1f, .75f, .4f), 18f, 1.2f);
        }

        void Sandcastles(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var sandM = WorldMaterials.Solid(new Color(.92f, .82f, .58f)); mk.Prism(sandM, c - up * .25f, up, 5f, .35f, 16);
            foreach (var (x, z, s) in new[] { (0f, 0f, 1.3f), (-2f, 1f, .8f), (1.8f, -1.2f, .9f) })
            {
                var p = c + Vector3.Cross(up, face) * x + face * z; mk.Box(sandM, p + up * .5f * s, new Vector3(1.6f, 1f, 1.6f) * s, Quaternion.identity);
                foreach (var d in new[] { new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1) }) { mk.Prism(sandM, p + d * .8f * s, up, .35f * s, 1.5f * s, 8); mk.Cone(sandM, p + d * .8f * s + up * 1.5f * s, .4f * s, .5f * s, 8); }
            }
            var sea = SongGlow(CastlesSand, new Color(.3f, .75f, 1f), .9f);
            for (var k = 0; k < 3; k++) mk.Box(sea, c + face * (3.6f + k * .6f) + up * .12f, new Vector3(9f - k * 2f, .03f, .4f), Quaternion.LookRotation(face));
            solid.Box(sandM, c + up * .7f, new Vector3(5f, 1.4f, 4f), Quaternion.LookRotation(face));
        }

        void Chimes(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            Oak(ctx, mk, c, 1.3f);
            var glow = SongGlow(WindCriesMary, new Color(.7f, .85f, 1f), 1.8f);
            for (var k = 0; k < 4; k++)
            {
                var hang = c + Vector3.up * 5f + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * 2.6f; var ck = new MeshKit();
                ck.Prism(metal, Vector3.zero, Vector3.down, .03f, .6f, 6); ck.Prism(wood, Vector3.down * .6f, Vector3.down, .25f, .05f, 10);
                for (var r = 0; r < 5; r++) { var a = r * Mathf.PI * 2f / 5f; ck.Prism(r % 2 == 0 ? glow : metal, new Vector3(Mathf.Cos(a) * .2f, -.7f, Mathf.Sin(a) * .2f), Vector3.down, .03f, .5f + r * .1f, 6); }
                var t = ck.BuildSingle("Wind chime", life.transform, false).transform; t.position = hang; life.swingers.Add((t, k * 1.3f));
            }
            halos.Add(c + Vector3.up * 4.4f, 3.4f, new Color(.7f, .85f, 1f), .2f);
        }

        void SplitRock(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rock = WorldMaterials.Solid(new Color(.5f, .48f, .45f));
            foreach (var s in new[] { -1f, 1f }) { var p = c + side * s * 1.4f; mk.Blob(rock, p + up * 1.8f, new Vector3(1.6f, 2.2f, 2f), ctx.Rng, .25f); solid.Box(rock, p + up * 1.8f, new Vector3(2.2f, 3.6f, 3f), Quaternion.LookRotation(face)); }
            var crack = SongGlow(VoodooChild, new Color(.75f, .4f, 1f), 2.4f); mk.Box(crack, c + up * 1.8f, new Vector3(.12f, 3.6f, 2.6f), Quaternion.LookRotation(face));
            halos.Add(c + up * 2f, 3f, new Color(.75f, .4f, 1f), .5f);
        }

        // ---------------------------------------------------------------- the Kingdom itself
        /// A castle on the highest open hill: a curtain wall round a keep, four round towers with conical roofs and
        /// banners, battlements, a gatehouse facing the village, lit windows.
        void Castle(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var best = Vector3.zero; var bestH = float.MinValue;
            for (var x = ctx.MinX; x < ctx.MaxX; x += 10f)
                for (var z = ctx.MinZ; z < ctx.MaxZ; z += 10f)
                {
                    var d = Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)); if (d < 110f || d > 340f) continue;
                    if (!ctx.IsLand(x, z, 30f) || !Open(ctx, x, z, 22f) || !Gentle(ctx, x, z, 18f, 5f)) continue;
                    var h = ctx.Ground(x, z); if (h > bestH) { bestH = h; best = new Vector3(x, h, z); }
                }
            if (bestH == float.MinValue) { Debug.Log("Kingdom of Classic Rock: no room for the castle"); return; }
            var c = best; var face = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var stone = WorldMaterials.Solid(new Color(.74f, .7f, .62f)); var roof = WorldMaterials.Solid(new Color(.25f, .3f, .55f)); const float R = 13f, H = 7f;
            // the curtain wall, with a gap for the gatehouse
            for (var k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, k * 90f, 0) * face; var wc = c + d * R; var wr = Quaternion.LookRotation(d); var ws = Vector3.Cross(up, d);
                if (k == 0) { foreach (var s in new[] { -1f, 1f }) { var p = wc + ws * s * 8f; mk.Box(stone, p + up * H / 2f, new Vector3(10f, H, 1.6f), wr); solid.Box(stone, p + up * H / 2f, new Vector3(10f, H, 1.6f), wr); } }
                else { mk.Box(stone, wc + up * H / 2f, new Vector3(26f, H, 1.6f), wr); solid.Box(stone, wc + up * H / 2f, new Vector3(26f, H, 1.6f), wr); }
                for (var m = 0; m < 12; m++) { var x = -12f + m * 2.2f; if (k == 0 && Mathf.Abs(x) < 3f) continue; mk.Box(stone, wc + ws * x + up * (H + .45f), new Vector3(1.1f, .9f, 1.6f), wr); }
            }
            foreach (var (sx, sz) in new[] { (1f, 1f), (-1f, 1f), (1f, -1f), (-1f, -1f) })
            {
                var t = c + face * sz * R + side * sx * R; mk.Prism(stone, t - up * .3f, up, 2.6f, H + 4.3f, 12); solid.Prism(stone, t - up * .3f, up, 2.6f, H + 4.3f, 12);
                mk.Cone(roof, t + up * (H + 4f), 3.1f, 4.2f, 12); mk.Prism(dark, t + up * (H + 8.2f), up, .05f, 2.2f, 6);
                mk.Box(WorldMaterials.Solid(sx * sz > 0 ? new Color(.75f, .12f, .15f) : new Color(.2f, .3f, .7f)), t + up * (H + 9.8f) + side * .6f, new Vector3(1.2f, .7f, .03f), rot);
                for (var w = 0; w < 2; w++) mk.Box(GlowM(new Color(1f, .82f, .5f), 1.6f), t + up * (3.5f + w * 3.5f) + face * 2.62f, new Vector3(.5f, .9f, .04f), rot);
            }
            // the gatehouse: two towers flanking an arch with a portcullis, the castle's name over it
            var g = c + face * R;
            foreach (var s in new[] { -1f, 1f }) { var t = g + side * s * 3.4f; mk.Box(stone, t + up * (H + 1.5f) / 2f, new Vector3(2.4f, H + 1.5f, 3f), rot); solid.Box(stone, t + up * (H + 1.5f) / 2f, new Vector3(2.4f, H + 1.5f, 3f), rot); }
            mk.Box(stone, g + up * (H - .8f), new Vector3(4.4f, 2.4f, 3f), rot);
            for (var k = 0; k < 5; k++) mk.Box(dark, g + up * (H - 2.3f) + side * ((k - 2f) * .8f) + face * 1.3f, new Vector3(.08f, .9f, .08f), rot);
            ArtistBits.Label(ctx.Root, "THE CASTLE OF CLASSIC ROCK", g + up * (H + .1f) + face * 1.55f, face, .28f, new Color(.3f, .25f, .2f), TextAnchor.MiddleCenter);
            // the keep
            var keep = c - face * 2f; mk.Box(stone, keep + up * 6f, new Vector3(9f, 12f, 9f), rot); solid.Box(stone, keep + up * 6f, new Vector3(9f, 12f, 9f), rot);
            for (var m = 0; m < 4; m++) foreach (var s in new[] { -1f, 1f }) { mk.Box(stone, keep + up * 12.45f + side * ((m - 1.5f) * 2.2f) + face * s * 4.2f, new Vector3(1.1f, .9f, .6f), rot); mk.Box(stone, keep + up * 12.45f + face * ((m - 1.5f) * 2.2f) + side * s * 4.2f, new Vector3(.6f, .9f, 1.1f), rot); }
            for (var w = 0; w < 3; w++) mk.Box(GlowM(new Color(1f, .82f, .5f), 1.8f), keep + up * (4f + w * 3f) + face * 4.52f + side * ((w - 1f) * 2.4f), new Vector3(.8f, 1.4f, .04f), rot);
            mk.Prism(dark, keep + up * 12.9f, up, .08f, 5f, 6); mk.Box(WorldMaterials.Solid(new Color(.85f, .7f, .2f)), keep + up * 17.2f + side * 1.1f, new Vector3(2.2f, 1.3f, .04f), rot);
            halos.Add(keep + up * 8f + face * 6f, 8f, new Color(1f, .85f, .55f), .2f);
            ArtistBits.PointLight(ctx.Root, g + face * 4f + up * 5f, new Color(1f, .85f, .6f), 20f, 1.4f);
            noGrass.Add(new Vector3(c.x, R + 4f, c.z));
            // a road from the nearest walk up to the gate
            Vector3 join = default; var bd = float.MaxValue; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var dd = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(g.x, g.z)); if (dd < bd) { bd = dd; join = p; } }
            if (bd < 200f) { var pts = new List<Vector3>(); var dir = new Vector3(g.x - join.x, 0, g.z - join.z).normalized; for (var d = RoadW / 2f; d < bd - 1f; d += 2f) { var q = join + dir * d; pts.Add(ctx.OnGround(q.x, q.z)); } if (pts.Count > 1) { Strip(mk, pts, WorldMaterials.Solid(new Color(.66f, .6f, .48f)), 3f, .12f); foreach (var p in pts) Remember(p, 351); } }
            var mark = new GameObject("Shot: the castle").transform; mark.SetParent(ctx.Root, false);
            var cam = g + face * 34f + side * 10f; cam = ctx.OnGround(cam.x, cam.z) + up * 1.5f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((c + up * 6f - cam - up * 1.6f).normalized));
        }

        /// A village pub on the green: whitewashed walls, a thatched roof, a hanging sign, lit windows, benches outside.
        void Pub(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c0 = ctx.OnGround(center.x, center.z);
            Vector3 at = Vector3.positiveInfinity;
            for (var k = 0; k < 16 && float.IsInfinity(at.x); k++) { var a = k * Mathf.PI / 8f; var p = new Vector3(c0.x + Mathf.Cos(a) * 30f, 0, c0.z + Mathf.Sin(a) * 30f); if (Open(ctx, p.x, p.z, 7f) && Gentle(ctx, p.x, p.z, 8f, 1.5f)) at = ctx.OnGround(p.x, p.z); }
            if (float.IsInfinity(at.x)) return;
            var face = new Vector3(c0.x - at.x, 0, c0.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var wash = WorldMaterials.Solid(new Color(.95f, .93f, .86f)); var thatch = WorldMaterials.Solid(new Color(.75f, .62f, .38f)); var timber = WorldMaterials.Solid(new Color(.2f, .15f, .12f));
            mk.Box(wash, at + up * 1.8f, new Vector3(10f, 3.6f, 6f), rot); solid.Box(wash, at + up * 1.8f, new Vector3(10f, 3.6f, 6f), rot);
            mk.GableRoof(thatch, at + up * 3.63f, new Vector3(6.8f, 0, 10.8f), 2.8f, Quaternion.LookRotation(side));
            var fr = at + face * 3.02f; for (var k = -2; k <= 2; k++) mk.Box(timber, fr + side * (k * 2.4f) + up * 1.8f, new Vector3(.2f, 3.6f, .04f), rot);
            foreach (var s in new[] { -3.6f, -1.2f, 3.6f }) mk.Box(GlowM(new Color(1f, .8f, .45f), 1.7f), fr + side * s + up * 1.9f, new Vector3(1.2f, 1f, .03f), rot);
            mk.Box(timber, fr + side * 1.2f + up * 1.1f, new Vector3(1.1f, 2.2f, .03f), rot);
            var sp = fr + face * .9f + side * 4.4f; mk.Box(timber, sp + up * 3.2f + face * -.4f, new Vector3(.12f, .12f, 1f), rot); mk.Box(WorldMaterials.Solid(new Color(.7f, .12f, .12f)), sp + up * 2.6f, new Vector3(.06f, 1f, .8f), Quaternion.LookRotation(side));
            ArtistBits.Label(ctx.Root, "THE ROYAL ROCK", fr + up * 3.3f + face * .05f, face, .3f, new Color(.25f, .15f, .1f), TextAnchor.MiddleCenter);
            foreach (var s in new[] { -2.5f, 2.5f }) { var b = fr + face * 2.4f + side * s; b = ctx.OnGround(b.x, b.z); mk.Box(wood, b + up * .75f, new Vector3(1.8f, .08f, .9f), rot); mk.Box(wood, b + up * .45f + face * .7f, new Vector3(1.8f, .06f, .3f), rot); mk.Box(wood, b + up * .45f - face * .7f, new Vector3(1.8f, .06f, .3f), rot); solid.Box(wood, b + up * .4f, new Vector3(1.9f, .8f, 1.9f), rot); }
            ArtistBits.PointLight(ctx.Root, fr + face * 3f + up * 2.5f, new Color(1f, .8f, .5f), 12f, 1.2f);
            noGrass.Add(new Vector3(at.x, 8f, at.z));
        }
    }
}
