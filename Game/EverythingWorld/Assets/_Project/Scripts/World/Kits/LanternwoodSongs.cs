using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Lanternwood's deep pass (part of LanternwoodKit): songs as small, hushed places in the snowy woods, each down
    /// its own lantern-lit side path (two of them out on the frozen lake), each glowing and breathing while its song
    /// plays; and in every one-song artist's clearing, a piece from their song.
    /// - Phoebe Bridgers: a rowing boat frozen into the ice, tilted ("Motion Sickness"); a road ending in a snowbank
    ///   with a sign and a doorway of light beyond ("I Know The End"); a sheet ghost holding a lantern ("Haunted");
    ///   a bench on a little platform under a clock stopped near midnight ("I Can't Wait"); a red gate hung with
    ///   paper lanterns and a phone on a post ("Kyoto"); a bare tree still standing in a ring of stones with one
    ///   lantern ("Still Standing"); a pool of gold under the ice with a shovel beside it ("The Gold"); a camcorder
    ///   on a tripod pointed at an empty bench, its red light on ("On Video"); a viewing platform with coin
    ///   binoculars over the lake ("Panorama").
    /// - Big Thief: a ring of birches hung with red lanterns round a stone table ("Vampire Empire"); a beehive on a
    ///   post in a swarm of glowing motes ("Simulation Swarm"); an empty frame of frost glowing at its edges
    ///   ("Not"); a tangle of string lights caught in a bush ("Incomprehensible"); one steady candle on a stone in a
    ///   ring of snow ("Certainty"); two lanterns on two posts joined by a glowing thread ("Real Love").
    /// - Lizzy McAlpine: a kneeler before a candle shrine ("Pushing It Down and Praying"); glowing footprints
    ///   zigzagging through the snow ("Drunk, Running"); a table crowded with little things under string lights
    ///   ("A Little Bit of Everything").
    /// - Mitski: a kennel with a prize rosette ("I Bet on Losing Dogs"); a mailbox stuffed with glowing letters
    ///   ("Francis Forever"). Noah Kahan: a leaning pile of bare sticks with a few red leaves on the snow ("Stick
    ///   Season"); a signpost of arrows with a suitcase at its foot ("You're Gonna Go Far"). Daughter: a little
    ///   cabinet of glowing bottles ("Medicine"); a swing set half buried in snow ("Youth").
    public partial class LanternwoodKit
    {
        LanternwoodLife life;
        readonly List<Vector3> placeSpots = new List<Vector3>();
        static readonly Color Warm = new Color(1f, .7f, .38f), Frost = new Color(.7f, .85f, 1f), Ember = new Color(1f, .45f, .25f), Gold = new Color(1f, .8f, .35f);

        Material SongGlow(string song, Color c, float s = 1.8f)
        {
            var m = new Material(WorldMaterials.Glow(c, s)) { name = "Song glow" };
            life.songGlows.Add((song, m, c * s));
            return m;
        }

        void Lit(LandContext ctx, Vector3 at, Color col, float r = 2.4f)
        {
            ArtistBits.PointLight(ctx.Root, at, col, 11f, 1.6f);
            halos.Add(at, r, col, .55f);
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

        Vector3 PlaceSpot(LandContext ctx, Vector3 near, float nearR, float clearR, bool onIce)
        {
            for (var tries = 0; tries < 1600; tries++)
            {
                var wide = tries > 900; var a = ctx.Range(0f, Mathf.PI * 2f);
                float px, pz;
                if (onIce)
                {
                    if (!hasLake) return Vector3.positiveInfinity;
                    var r = LakeEdge(a) * ctx.Range(.2f, .72f); px = lakeC.x + Mathf.Cos(a) * r; pz = lakeC.z + Mathf.Sin(a) * r;
                    var q0 = new Vector3(px, 0, pz);
                    if (!OnLake(q0, -(clearR + 3f))) continue;
                    var pierOk = true; foreach (var ar in ctx.Artists) if (ar.name == lakeHall && Vector2.Distance(new Vector2(ar.x, ar.z), new Vector2(px, pz)) < PierMax + BoatD + clearR) pierOk = false;
                    if (!pierOk) continue;
                }
                else
                {
                    var r = wide ? ctx.Range(30f, 300f) : ctx.Range(nearR + clearR + 4f, nearR + clearR + 55f);
                    var o = wide ? ctx.Plaza : near; px = o.x + Mathf.Cos(a) * r; pz = o.z + Mathf.Sin(a) * r;
                    var q = new Vector3(px, 0, pz);
                    if (!ctx.IsLand(px, pz, clearR + 18f) || !ctx.IsFree(px, pz, clearR) || NearClear(px, pz) || OnLake(q, clearR + 4f) || InRing(ctx, q, clearR + 2f)) continue;
                    if (!Gentle(ctx, px, pz, clearR + 2f, tries > 1200 ? clearR * .5f : clearR * .3f)) continue;
                }
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < s.y + clearR + 10f) { ok = false; break; }
                if (!ok) continue;
                placeSpots.Add(new Vector3(px, clearR, pz));
                if (!onIce) { ctx.AddKeepout(px, pz, clearR + 2f); clear.Add(new Vector3(px, clearR + 2f, pz)); }
                return onIce ? new Vector3(px, lakeLevel + .03f, pz) : ctx.OnGround(px, pz);
            }
            return Vector3.positiveInfinity;
        }

        /// A side path (from the nearest path, with a lantern post on the way) to a carved sign, and the tour's mark.
        /// Returns the way the place faces (toward the path it hangs off).
        Vector3 PlaceWay(LandContext ctx, MeshKit mk, Vector3 c, float clearR, string sign, string mark, Color col, bool onIce)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var p in pathPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(ctx.Plaza.x - c.x, 0, ctx.Plaza.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var side = Vector3.Cross(Vector3.up, face);
            if (!onIce && best < float.MaxValue && best > clearR + 3f)
            {
                var pts = new List<Vector3>(); var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - clearR + 1f;
                for (var d = 1f; ; d += 2f) { var dd = Mathf.Min(d, len); var q = from + dir * dd; if (!ctx.IsLand(q.x, q.z, 0f) || OnLake(q, 1f)) break; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count > 1)
                {
                    Ribbon(ctx, mk, WorldMaterials.Solid(new Color(.72f, .74f, .8f)), pts, 1.4f, .07f);
                    foreach (var p in pts) clear.Add(new Vector3(p.x, 2.6f, p.z));
                    if (len > 10f)
                    {
                        var mid = pts[pts.Count / 2]; var post = mid + side * 1.3f; post.y = ctx.Ground(post.x, post.z);
                        mk.Prism(darkWood, post, Vector3.up, .06f, 2.4f, 5); ArtistBits.PostCollider(ctx.Root, post, 2.4f, .1f);
                        mk.Box(darkWood, post + Vector3.up * 2.32f - side * .25f, new Vector3(.5f, .05f, .05f), Quaternion.LookRotation(face));
                        var lamp = post + Vector3.up * 1.95f - side * .45f; mk.Box(WorldMaterials.Glow(LanternGlass, 1.4f), lamp, new Vector3(.16f, .22f, .16f), Quaternion.identity);
                        halos.Add(lamp, 1.2f, LanternGlass, .8f);
                    }
                }
            }
            var sp = new Vector3(c.x, 0, c.z) + face * (clearR - .6f) + side * 2.2f; sp.y = onIce ? lakeLevel + .03f : ctx.Ground(sp.x, sp.z); var rot = Quaternion.LookRotation(face);
            mk.Prism(darkWood, sp - Vector3.up * .2f, Vector3.up, .07f, 1.75f, 5);
            mk.Box(darkWood, sp + Vector3.up * 1.32f, new Vector3(2.3f, .4f, .07f), rot); mk.Box(snowTop, sp + Vector3.up * 1.545f, new Vector3(2.3f, .05f, .1f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.32f + face * .045f, face, .12f, Ink, TextAnchor.MiddleCenter);
            mk.Blob(WorldMaterials.Glow(col, 2f), sp + Vector3.up * 1.72f, Vector3.one * .08f, ctx.Rng, 0f);
            ArtistBits.PostCollider(ctx.Root, sp, 1.6f, .12f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clearR + 6f) - side * 4f;
            foreach (var (fo, so) in new[] { (clearR + 6f, -4f), (clearR + 6f, 4f), (clearR + 4f, -4f), (-(clearR + 5f), 3f), (clearR + 2f, 0f) })
            { var q = c + face * fo + side * so; if ((onIce || ctx.IsLand(q.x, q.z, 5f)) && !InRing(ctx, q, 1f)) { cam = q; break; } }
            cam.y = (onIce && OnLake(cam, -1f) ? lakeLevel + .03f : ctx.Ground(cam.x, cam.z)) + 1.3f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation((c + Vector3.up * 1.3f - cam).normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            placeSpots.Clear();
            var placed = 0;
            void Place(string artist, float clearR, string sign, string mark, Color col, System.Action<Vector3, Vector3> build, bool onIce = false)
            {
                var a = ctx.Artists.Find(x => x.name == artist);
                var near = a != null ? ctx.OnGround(a.x, a.z) : ctx.Plaza; var nearR = a == null ? 0f : a.r;
                var c = PlaceSpot(ctx, near, nearR, clearR, onIce); if (float.IsInfinity(c.x)) { Debug.Log("Lanternwood: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clearR, sign, mark, col, onIce)); placed++;
            }
            const string PB = "Phoebe Bridgers", BT = "Big Thief", LM = "Lizzy McAlpine";
            Place(PB, 5f, "MOTION SICKNESS", "the frozen boat", Frost, (c, f) => FrozenBoat(ctx, mk, solid, c, f, "5xo8RrjJ9CVNrtRg2S3B1R"), true);
            Place(PB, 6f, "I KNOW THE END", "the road's end", Gold, (c, f) => RoadsEnd(ctx, mk, solid, c, f, "0pJW1Xw3aY4Eh6k5iuBkfI"));
            Place(PB, 4f, "HAUNTED", "the sheet ghost", Frost, (c, f) => SheetGhost(ctx, mk, solid, c, f, "4b1QYjd7i5D1Gx2VjN21qI"));
            Place(PB, 5f, "I CAN'T WAIT", "the stopped clock", Warm, (c, f) => StoppedClock(ctx, mk, solid, c, f, "5s50vIGQHK8FG8LfSdHC5q"));
            Place(PB, 5f, "KYOTO", "the lantern gate", Ember, (c, f) => LanternGate(ctx, mk, solid, c, f, "4vjvx7Zxkb4AltGcZ0BBvI"));
            Place(PB, 5f, "STILL STANDING", "the standing tree", Warm, (c, f) => StandingTree(ctx, mk, solid, c, f, "09IT8imSuZBu1mjhNWHjuo"));
            Place(PB, 4f, "THE GOLD", "the gold under the ice", Gold, (c, f) => GoldIce(ctx, mk, solid, c, f, "7qcXUzPwoxSBFxjTbNrV0B"), true);
            Place(PB, 4f, "ON VIDEO", "the camcorder", Ember, (c, f) => Camcorder(ctx, mk, solid, c, f, "2Flb8QXjKZKq3rWgWDmqq5"));
            Place(PB, 5f, "PANORAMA", "the viewing platform", Frost, (c, f) => ViewingPlatform(ctx, mk, solid, c, f, "696AN0lEtK19M7S7eX7A8y"));
            Place(BT, 6f, "VAMPIRE EMPIRE", "the red lantern ring", Ember, (c, f) => RedLanternRing(ctx, mk, solid, c, f, "4SwSpxsOYtnEaRxWtYrnvZ"));
            Place(BT, 4f, "SIMULATION SWARM", "the swarm", Gold, (c, f) => Swarm(ctx, mk, solid, c, f, "2FwDApgXk91kXvqy2oB7dz"));
            Place(BT, 4f, "NOT", "the empty frame", Frost, (c, f) => EmptyFrame(ctx, mk, solid, c, f, "7lrhugjSbiImrNSLIFIq0c"));
            Place(BT, 4f, "INCOMPREHENSIBLE", "the tangled lights", Warm, (c, f) => TangledLights(ctx, mk, solid, c, f, "5GTRE7cnySeaYBanwM5FIt"));
            Place(BT, 4f, "CERTAINTY", "the one candle", Warm, (c, f) => OneCandle(ctx, mk, solid, c, f, "3i7XRi2UyvGTdJawJRkVhd"));
            Place(BT, 5f, "REAL LOVE", "the two lanterns", Ember, (c, f) => TwoLanterns(ctx, mk, solid, c, f, "6sNy4KFU2xAIrFZ5DUtDUp"));
            Place(LM, 4f, "PUSHING IT DOWN AND PRAYING", "the kneeler", Warm, (c, f) => Kneeler(ctx, mk, solid, c, f, "0Exki6SgSuYfWQDP0npFlF"));
            Place(LM, 6f, "DRUNK, RUNNING", "the footprints", Frost, (c, f) => Footprints(ctx, mk, solid, c, f, "0OQFriWHtnIzsRp3uImlGt"));
            Place(LM, 4f, "A LITTLE BIT OF EVERYTHING", "the crowded table", Warm, (c, f) => CrowdedTable(ctx, mk, solid, c, f, "66yvX4stoMKIiTcZeDJGpt"));
            Place("Mitski", 4f, "I BET ON LOSING DOGS", "the kennel", Ember, (c, f) => Kennel(ctx, mk, solid, c, f, "2Co0IjcLTSHMtodwD4gzfg"));
            Place("Mitski", 4f, "FRANCIS FOREVER", "the mailbox", Warm, (c, f) => Mailbox(ctx, mk, solid, c, f, "5411TEB6tlzvuF5A4oyldr"));
            Place("Noah Kahan", 4f, "STICK SEASON", "the stick pile", Ember, (c, f) => StickPile(ctx, mk, solid, c, f, "0GNVXNz7Jkicfk2mp5OyG5"));
            Place("Noah Kahan", 4f, "YOU'RE GONNA GO FAR", "the signpost", Warm, (c, f) => Signpost(ctx, mk, solid, c, f, "4nHJcUtNSUVjXRnjdP29Bk"));
            Place("Daughter", 4f, "MEDICINE", "the medicine cabinet", Frost, (c, f) => MedicineCabinet(ctx, mk, solid, c, f, "1ujiHGg5p9SVrNIV4jumQK"));
            Place("Daughter", 5f, "YOUTH", "the buried swings", Frost, (c, f) => BuriedSwings(ctx, mk, solid, c, f, "1vyrrrnb7182SL6iOMor3O"));
            Debug.Log($"Lanternwood: {placed} song places");
        }

        Vector3 G(LandContext ctx, Vector3 p) => ctx.OnGround(p.x, p.z);

        // ---------------------------------------------------------------- Phoebe Bridgers
        void FrozenBoat(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(Vector3.Cross(up, face)) * Quaternion.Euler(0, 0, 14f);
            var hull = WorldMaterials.Solid(new Color(.55f, .66f, .78f)); mk.Box(hull, c + up * .15f, new Vector3(1.3f, .5f, 3.6f), rot);   // its own pale blue, not the canoes' paint mk.Box(darkWood, c + up * .38f, new Vector3(1.1f, .06f, .3f), rot);
            mk.Box(planks, c + up * .42f + rot * Vector3.forward * .8f, new Vector3(1.1f, .06f, .3f), rot);
            mk.Prism(darkWood, c + up * .3f, (rot * new Vector3(.9f, .5f, .2f)).normalized, .04f, 2f, 5);
            mk.Prism(SongGlow(id, Frost, .7f), c + up * .005f, up, 3.2f, .02f, 18);
            mk.Prism(iron, c + up * .4f + rot * Vector3.forward * 1.7f, up, .03f, .5f, 5);
            mk.Box(SongGlow(id, Warm, 2.2f), c + up * .98f + rot * Vector3.forward * 1.7f, new Vector3(.16f, .22f, .16f), Quaternion.identity);
            solid.Box(hull, c + up * .3f, new Vector3(1.4f, .8f, 3.7f), rot);
            Lit(ctx, c + up * 1.4f + rot * Vector3.forward * 1.7f, Warm);
        }

        void RoadsEnd(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var dir = -face; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var tar = WorldMaterials.Solid(new Color(.64f, .66f, .72f));  var paint = WorldMaterials.Solid(new Color(.95f, .85f, .4f));   // an old snow-dusted grey road (dark read as a hole)
            var pts = new List<Vector3>(); for (var d = -5f; d <= 4f; d += 1.5f) pts.Add(G(ctx, c + dir * d));
            Ribbon(ctx, mk, tar, pts, 3.4f, .1f);
            for (var d = -4.5f; d <= 2.5f; d += 2.2f) mk.Box(paint, G(ctx, c + dir * d) + up * .13f, new Vector3(.12f, .02f, .9f), rot);
            mk.Blob(snowTop, G(ctx, c + dir * 5f) + up * .3f, new Vector3(2.4f, .8f, 1.4f), ctx.Rng, .2f);
            var sg = c + dir * 3.4f + side * 2.2f; sg = G(ctx, sg); mk.Prism(iron, sg, up, .05f, 2.2f, 6); ArtistBits.PostCollider(ctx.Root, sg, 2.2f, .1f);
            mk.Box(WorldMaterials.Solid(new Color(.95f, .8f, .2f)), sg + up * 2.2f, new Vector3(.75f, .75f, .04f), rot * Quaternion.Euler(0, 0, 45f));
            ArtistBits.Label(ctx.Root, "END", sg + up * 2.2f - dir * .03f, -dir, .16f, new Color(.1f, .08f, .05f), TextAnchor.MiddleCenter);
            var door = G(ctx, c + dir * 7f);
            foreach (var s in new[] { -1f, 1f }) mk.Box(darkWood, door + side * s * .6f + up * 1.15f, new Vector3(.12f, 2.3f, .12f), rot);
            mk.Box(darkWood, door + up * 2.35f, new Vector3(1.32f, .12f, .12f), rot);
            mk.Box(SongGlow(id, Gold, 2.6f), door + up * 1.12f, new Vector3(1.08f, 2.24f, .03f), rot);
            solid.Box(darkWood, door + up * 1.2f, new Vector3(1.4f, 2.4f, .2f), rot);
            Lit(ctx, door - dir * 1.2f + up * 1.4f, Gold, 3f);
        }

        void SheetGhost(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var sheet = WorldMaterials.Solid(new Color(.95f, .96f, .98f));
            mk.Prism(sheet, c, up, .6f, 1.2f, 10, .55f); mk.Blob(sheet, c + up * 1.35f, new Vector3(.36f, .38f, .36f), ctx.Rng, 0f);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Box(sheet, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .58f + up * .08f, new Vector3(.25f, .16f, .05f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            var side = Vector3.Cross(up, face);
            foreach (var s in new[] { -1f, 1f }) mk.Blob(iron, c + up * 1.4f + face * .33f + side * s * .12f, new Vector3(.06f, .08f, .03f), ctx.Rng, 0f);
            var hand = c + face * .55f + side * .35f + up * .8f; mk.Box(iron, hand + up * .2f, new Vector3(.02f, .3f, .02f), Quaternion.identity);
            mk.Box(SongGlow(id, Warm, 2.4f), hand - up * .05f, new Vector3(.16f, .22f, .16f), Quaternion.identity);
            ArtistBits.PostCollider(ctx.Root, c, 1.6f, .55f);
            Lit(ctx, hand + face * .5f, Warm, 1.8f);
        }

        void StoppedClock(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(planks, c + up * .2f, new Vector3(4f, .4f, 2.2f), rot); solid.Box(planks, c + up * .2f, new Vector3(4f, .4f, 2.2f), rot);
            mk.Box(snowTop, c + up * .42f, new Vector3(3.9f, .04f, 2.1f), rot);
            Bench(ctx, mk, solid, c + up * .4f, face, c.y + .44f);
            var pole = c - face * .8f + side * 1.5f; mk.Prism(iron, pole + up * .4f, up, .07f, 3f, 6); ArtistBits.PostCollider(ctx.Root, pole, 3.4f, .1f);
            var clock = pole + up * 3.5f; mk.Prism(iron, clock - face * .1f, face, .5f, .2f, 16);
            mk.Prism(SongGlow(id, new Color(1f, .92f, .75f), 1.8f), clock + face * .1f, face, .44f, .02f, 16);
            mk.Box(iron, clock + face * .13f + up * .17f, new Vector3(.04f, .34f, .01f), rot);
            mk.Box(iron, clock + face * .13f + up * .12f - side * .02f, new Vector3(.04f, .25f, .01f), rot * Quaternion.Euler(0, 0, 8f));
            Lit(ctx, clock + face * .8f, Warm, 1.8f);
        }

        void LanternGate(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var red = WorldMaterials.Solid(new Color(.62f, .14f, .12f));
            foreach (var s in new[] { -1f, 1f }) { var p = c + side * s * 1.6f; mk.Prism(red, p - up * .2f, up, .17f, 3.6f, 8); ArtistBits.PostCollider(ctx.Root, p, 3.4f, .2f); }
            mk.Box(red, c + up * 3.25f, new Vector3(4.2f, .25f, .3f), rot); mk.Box(red, c + up * 3.6f, new Vector3(4.8f, .22f, .36f), rot);
            mk.Box(snowTop, c + up * 3.74f, new Vector3(4.7f, .06f, .36f), rot);
            var paper = SongGlow(id, new Color(1f, .55f, .3f), 2.2f);
            foreach (var s in new[] { -1f, -.35f, .35f, 1f }) { var p = c + side * s * 1.4f + up * 2.75f; mk.Box(iron, p + up * .3f, new Vector3(.01f, .25f, .01f), rot); mk.Prism(paper, p - up * .2f, up, .17f, .38f, 8, .8f); }
            var phone = c + side * 2.8f + face * .6f; phone = G(ctx, phone); mk.Prism(darkWood, phone, up, .06f, 1.3f, 6); ArtistBits.PostCollider(ctx.Root, phone, 1.3f, .1f);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .55f, .4f)), phone + up * 1.5f, new Vector3(.4f, .55f, .25f), rot);
            mk.Box(SongGlow(id, new Color(.7f, 1f, .8f), 1.6f), phone + up * 1.6f + face * .13f, new Vector3(.22f, .14f, .02f), rot);
            mk.Box(iron, phone + up * 1.42f + face * .15f + side * .14f, new Vector3(.07f, .26f, .07f), rot);
            Lit(ctx, c + up * 2.6f + face * .8f, new Color(1f, .55f, .3f), 2.4f);
        }

        void StandingTree(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var dead = WorldMaterials.Solid(new Color(.3f, .27f, .26f));
            mk.Prism(dead, c - up * .3f, up, .3f, 5.5f, 7, .5f); ArtistBits.PostCollider(ctx.Root, c, 5f, .35f);
            foreach (var (a, h, l) in new[] { (20f, 2.8f, 2.2f), (140f, 3.6f, 1.8f), (250f, 4.2f, 1.6f), (320f, 2.2f, 1.4f) })
            { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; mk.Prism(dead, c + up * h, (d + up * .5f).normalized, .1f, l, 5, .4f); }
            for (var k = 0; k < 9; k++) { var a = k * Mathf.PI * 2f / 9f; mk.Blob(stone, G(ctx, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.2f) + up * .15f, new Vector3(.35f, .25f, .3f), ctx.Rng, .2f); }
            var br = c + up * 3f + Quaternion.Euler(0, 20f, 0) * Vector3.forward * 1.4f;
            mk.Box(iron, br - up * .25f, new Vector3(.015f, .5f, .015f), Quaternion.identity);
            mk.Box(SongGlow(id, Warm, 2.6f), br - up * .65f, new Vector3(.18f, .26f, .18f), Quaternion.identity);
            Lit(ctx, br - up * .6f + face * .4f, Warm, 1.8f);
        }

        void GoldIce(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up;
            mk.Prism(SongGlow(id, Gold, 1.6f), c + up * .004f, up, 2.6f, .02f, 16, 1f, 11f);
            for (var k = 0; k < 7; k++) { var a = k * .9f; mk.Prism(SongGlow(id, new Color(1f, .9f, .55f), 2.2f), c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (.6f + k * .25f) + up * .008f, up, .2f, .02f, 6); }
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f + .2f; mk.Blob(snowTop, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3f + up * .05f, new Vector3(.7f, .18f, .5f), ctx.Rng, .2f); }
            var sh = c + Vector3.Cross(up, face) * 1.8f + face * 1f;
            mk.Prism(darkWood, sh, (up + face * .2f).normalized, .04f, 1.3f, 5); mk.Box(iron, sh + up * .05f, new Vector3(.3f, .02f, .35f), Quaternion.LookRotation(face) * Quaternion.Euler(-60f, 0, 0));
            ArtistBits.PointLight(ctx.Root, c + up * 1.2f, Gold, 9f, 1.8f);
        }

        void Camcorder(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var look = -face; var rot = Quaternion.LookRotation(look);
            var cam = c + face * 1.2f;
            foreach (var a in new[] { 0f, 120f, 240f }) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; mk.Prism(iron, cam + d * .4f, (up - d * .3f).normalized, .025f, 1.45f, 5); }
            mk.Box(iron, cam + up * 1.5f, new Vector3(.2f, .2f, .42f), rot); mk.Prism(iron, cam + up * 1.5f + look * .2f, look, .07f, .12f, 10);
            mk.Blob(SongGlow(id, new Color(1f, .15f, .1f), 3f), cam + up * 1.63f - look * .1f + Vector3.Cross(up, look) * .07f, Vector3.one * .035f, ctx.Rng, 0f);
            mk.Box(SongGlow(id, new Color(.7f, .8f, 1f), 1.4f), cam + up * 1.52f - look * .22f, new Vector3(.16f, .12f, .01f), rot);
            ArtistBits.PostCollider(ctx.Root, cam, 1.6f, .35f);
            Bench(ctx, mk, solid, c - face * 2.2f, face);
            Lit(ctx, c - face * 2f + up * 1.6f, new Color(.8f, .85f, 1f), 1.8f);
        }

        void ViewingPlatform(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var look = hasLake ? new Vector3(lakeC.x - c.x, 0, lakeC.z - c.z).normalized : -face; var rot = Quaternion.LookRotation(look); var side = Vector3.Cross(up, look);
            mk.Box(planks, c + up * .25f, new Vector3(4.4f, .5f, 3f), rot); solid.Box(planks, c + up * .25f, new Vector3(4.4f, .5f, 3f), rot);
            mk.Box(snowTop, c + up * .52f, new Vector3(4.3f, .04f, 2.9f), rot);
            mk.Box(darkWood, c + look * 1.45f + up * 1.45f, new Vector3(4.4f, .1f, .1f), rot);
            foreach (var s in new[] { -2.1f, -.7f, .7f, 2.1f }) mk.Box(darkWood, c + look * 1.45f + side * s + up * 1f, new Vector3(.08f, 1f, .08f), rot);
            solid.Box(darkWood, c + look * 1.45f + up * 1f, new Vector3(4.4f, 1f, .15f), rot);
            var b = c + look * .9f + up * .5f; mk.Prism(iron, b, up, .06f, .9f, 6); mk.Box(SongGlow(id, Frost, 1.4f), b + up * 1.05f, new Vector3(.38f, .22f, .26f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Prism(iron, b + up * 1.05f + side * s * .09f + look * .13f, look, .055f, .14f, 8);
            ArtistBits.PostCollider(ctx.Root, b, 1.4f, .15f);
            Lit(ctx, b + up * 1.8f - look * .6f, Frost, 2f);
        }

        // ---------------------------------------------------------------- Big Thief
        void RedLanternRing(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var red = SongGlow(id, new Color(.95f, .12f, .1f), 1.3f);   // kept low so they stay red
            for (var k = 0; k < 7; k++)
            {
                var a = k * Mathf.PI * 2f / 7f; var p = G(ctx, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 4.6f);
                mk.Prism(bark, p - up * .3f, up, .17f, 6.5f, 7, .55f); ArtistBits.PostCollider(ctx.Root, p, 6f, .2f);
                for (var j = 0; j < 4; j++) mk.Box(barkMark, p + up * (1f + j * 1.3f), new Vector3(.3f, .06f, .06f), Quaternion.Euler(0, a * Mathf.Rad2Deg + j * 40f, 0));
                var l = p + up * 2.4f - new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .35f;
                mk.Box(iron, l + up * .2f, new Vector3(.015f, .3f, .015f), Quaternion.identity); mk.Prism(red, l - up * .15f, up, .15f, .3f, 8, .8f);
            }
            mk.Box(stone, c + up * .45f, new Vector3(1.8f, .9f, 1f), Quaternion.LookRotation(face)); solid.Box(stone, c + up * .45f, new Vector3(1.8f, .9f, 1f), Quaternion.LookRotation(face));
            mk.Box(snowTop, c + up * .92f, new Vector3(1.7f, .05f, .9f), Quaternion.LookRotation(face));
            Lit(ctx, c + up * 2.6f, new Color(1f, .3f, .2f), 3.4f);
        }

        void Swarm(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Prism(darkWood, c, up, .08f, 1f, 6); ArtistBits.PostCollider(ctx.Root, c, 1.8f, .4f);
            var hive = WorldMaterials.Solid(new Color(.85f, .78f, .55f));
            for (var k = 0; k < 3; k++) mk.Box(hive, c + up * (1.15f + k * .27f), new Vector3(.7f - k * .04f, .26f, .6f - k * .04f), rot);
            mk.Box(snowTop, c + up * 1.9f, new Vector3(.8f, .1f, .7f), rot);
            var mote = SongGlow(id, Gold, 2.8f);
            for (var k = 0; k < 60; k++) { var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(.5f, 2.4f); mk.Blob(mote, c + new Vector3(Mathf.Cos(a) * r, ctx.Range(.6f, 3.2f), Mathf.Sin(a) * r), Vector3.one * ctx.Range(.03f, .055f), ctx.Rng, 0f); }
            halos.Add(c + up * 2.2f, 2.4f, Gold, .4f); ArtistBits.PointLight(ctx.Root, c + up * 2.4f + face * .6f, Gold, 9f, 1.4f);
        }

        void EmptyFrame(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var edge = SongGlow(id, Frost, 2.4f);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(darkWood, c + side * s * .95f + up * 1.3f, new Vector3(.14f, 2.6f, .14f), rot); mk.Box(edge, c + side * s * .86f + up * 1.3f, new Vector3(.03f, 2.4f, .03f), rot); }
            mk.Box(darkWood, c + up * 2.65f, new Vector3(2.04f, .14f, .14f), rot); mk.Box(edge, c + up * 2.55f, new Vector3(1.7f, .03f, .03f), rot);
            mk.Box(darkWood, c + up * .05f, new Vector3(2.04f, .1f, .3f), rot); mk.Box(snowTop, c + up * 2.74f, new Vector3(2f, .05f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(ctx.Root, c + side * s * .95f, 2.6f, .1f);
            Lit(ctx, c + up * 1.4f + face * 1.4f, Frost, 1.8f);
        }

        void TangledLights(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var bush = WorldMaterials.Solid(new Color(.3f, .34f, .3f));
            mk.Blob(bush, c + up * .9f, new Vector3(1.6f, 1.1f, 1.4f), ctx.Rng, .3f); mk.Blob(snowTop, c + up * 1.7f, new Vector3(1.2f, .35f, 1f), ctx.Rng, .2f);
            ArtistBits.PostCollider(ctx.Root, c, 1.8f, 1.4f);
            var wire = WorldMaterials.Solid(new Color(.15f, .17f, .15f)); var bulb = SongGlow(id, Warm, 2.8f); var q = c + up * .6f + face * 1.4f;
            for (var k = 0; k < 40; k++)
            {
                var n = c + up * (.4f + Mathf.Abs(Mathf.Sin(k * 1.7f)) * 1.6f) + new Vector3(Mathf.Sin(k * 2.3f), 0, Mathf.Cos(k * 1.9f)) * (1.2f + Mathf.Sin(k) * .4f);
                mk.Box(wire, (q + n) / 2f, new Vector3(.015f, .015f, Vector3.Distance(q, n)), Quaternion.LookRotation(n - q + Vector3.one * .001f));
                mk.Blob(bulb, n, Vector3.one * .05f, ctx.Rng, 0f); q = n;
            }
            Lit(ctx, c + up * 1.4f + face * 1.6f, Warm, 2.4f);
        }

        void OneCandle(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up;
            mk.Prism(snowTop, c, up, 2.6f, .12f, 18);
            mk.Blob(stone, c + up * .3f, new Vector3(.55f, .3f, .5f), ctx.Rng, .15f); ArtistBits.PostCollider(ctx.Root, c, .7f, .5f);
            mk.Prism(candle, c + up * .55f, up, .07f, .32f, 8);
            mk.Blob(SongGlow(id, Warm, 3f), c + up * .95f, new Vector3(.05f, .1f, .05f), ctx.Rng, 0f);
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; mk.Blob(stone, G(ctx, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.6f) + up * .08f, new Vector3(.18f, .1f, .16f), ctx.Rng, .1f); }
            ArtistBits.PointLight(ctx.Root, c + up * 1.1f, Warm, 7f, 1.6f);
            halos.Add(c + up * 1f, .9f, Warm, .8f);
        }

        void TwoLanterns(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var glow = SongGlow(id, Ember, 2.6f);
            var tops = new List<Vector3>();
            foreach (var s in new[] { -1f, 1f })
            {
                var p = G(ctx, c + side * s * 2.4f); mk.Prism(darkWood, p, up, .07f, 2.4f, 5); ArtistBits.PostCollider(ctx.Root, p, 2.4f, .1f);
                var l = p + up * 2.1f - side * s * .45f; mk.Box(darkWood, p + up * 2.35f - side * s * .25f, new Vector3(.5f, .05f, .05f), Quaternion.LookRotation(face));
                mk.Box(glow, l, new Vector3(.18f, .26f, .18f), Quaternion.identity); tops.Add(l);
            }
            var thread = SongGlow(id, new Color(1f, .55f, .5f), 2.4f); var prev = tops[0];
            for (var k = 1; k <= 12; k++) { var t = k / 12f; var p = Vector3.Lerp(tops[0], tops[1], t) - up * Mathf.Sin(t * Mathf.PI) * .6f; mk.Box(thread, (prev + p) / 2f, new Vector3(.025f, .025f, Vector3.Distance(prev, p)), Quaternion.LookRotation(p - prev)); prev = p; }
            Lit(ctx, c + up * 1.8f + face * .8f, Ember, 2.4f);
        }

        // ---------------------------------------------------------------- Lizzy McAlpine
        void Kneeler(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(-face);
            var shrine = c - face * 1.2f; mk.Box(stone, shrine + up * .5f, new Vector3(1.2f, 1f, .5f), rot); solid.Box(stone, shrine + up * .5f, new Vector3(1.2f, 1f, .5f), rot);
            mk.Box(snowTop, shrine + up * 1.02f, new Vector3(1.1f, .04f, .45f), rot);
            for (var k = 0; k < 5; k++) { var p = shrine + Vector3.Cross(up, face) * (k - 2f) * .2f + up * 1.04f; mk.Prism(candle, p, up, .035f, .14f + (k % 3) * .06f, 6); mk.Blob(SongGlow(id, Warm, 3f), p + up * (.2f + (k % 3) * .06f), new Vector3(.025f, .05f, .025f), ctx.Rng, 0f); }
            mk.Box(darkWood, c + up * .15f, new Vector3(1f, .3f, .4f), rot); mk.Box(WorldMaterials.Solid(new Color(.5f, .2f, .22f)), c + up * .32f, new Vector3(.95f, .05f, .38f), rot);
            mk.Box(darkWood, c - face * .35f + up * .7f, new Vector3(1f, .08f, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(darkWood, c - face * .35f + Vector3.Cross(up, face) * s * .45f + up * .38f, new Vector3(.06f, .65f, .06f), rot);
            solid.Box(darkWood, c - face * .15f + up * .35f, new Vector3(1f, .7f, .7f), rot);
            Lit(ctx, shrine + up * 1.6f + face * .4f, Warm, 1.6f);
        }

        void Footprints(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var print = SongGlow(id, Frost, 1.6f);
            for (var k = 0; k < 22; k++)
            {
                var t = k / 21f; var p = c + face * ((t - .5f) * 10f) + side * Mathf.Sin(t * 9f) * 2.2f + side * (k % 2 == 0 ? .15f : -.15f);
                var dirv = face + side * Mathf.Cos(t * 9f) * .9f; mk.Box(print, G(ctx, p) + up * .02f, new Vector3(.12f, .01f, .26f), Quaternion.LookRotation(dirv) * Quaternion.Euler(0, k * 7 % 20 - 10, 0));
            }
            var lamp = G(ctx, c + face * 5.5f + side * 1.2f); mk.Prism(darkWood, lamp, up, .06f, 1.8f, 5); ArtistBits.PostCollider(ctx.Root, lamp, 1.8f, .1f);
            mk.Box(WorldMaterials.Glow(LanternGlass, 1.6f), lamp + up * 1.95f, new Vector3(.16f, .22f, .16f), Quaternion.identity);
            Lit(ctx, c + up * 1.6f, Frost, 3f);
        }

        void CrowdedTable(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(darkWood, c + up * .76f, new Vector3(2f, .07f, 1f), rot); foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) mk.Box(darkWood, c + side * s * .9f + face * t * .4f + up * .37f, new Vector3(.07f, .74f, .07f), rot);
            solid.Box(darkWood, c + up * .4f, new Vector3(2f, .8f, 1f), rot);
            Color[] cols = { new Color(.8f, .3f, .3f), new Color(.3f, .5f, .8f), new Color(.9f, .8f, .4f), new Color(.4f, .6f, .4f) };
            for (var k = 0; k < 12; k++)
            {
                var p = c + up * .8f + side * ((k % 6) - 2.5f) * .3f + face * ((k / 6) - .5f) * .45f;
                if (k % 4 == 0) mk.Prism(WorldMaterials.Solid(cols[k % 4]), p, up, .06f, .14f, 8);
                else if (k % 4 == 1) mk.Box(WorldMaterials.Solid(cols[(k + 1) % 4]), p + up * .04f, new Vector3(.22f, .08f, .3f), rot * Quaternion.Euler(0, k * 13f, 0));
                else if (k % 4 == 2) { mk.Prism(candle, p, up, .03f, .14f, 6); mk.Blob(SongGlow(id, Warm, 3f), p + up * .2f, new Vector3(.025f, .045f, .025f), ctx.Rng, 0f); }
                else mk.Prism(WorldMaterials.Solid(new Color(.75f, .85f, .9f), .8f), p, up, .05f, .2f, 8, .6f);
            }
            var wire = WorldMaterials.Solid(new Color(.15f, .15f, .15f)); var bulb = SongGlow(id, Warm, 2.4f);
            foreach (var s in new[] { -1f, 1f }) { var p = G(ctx, c + side * s * 1.6f); mk.Prism(darkWood, p, up, .05f, 2.4f, 5); ArtistBits.PostCollider(ctx.Root, p, 2.4f, .1f); }
            for (var k = 0; k <= 10; k++) { var t = k / 10f; var p = c + side * (t - .5f) * 3.2f + up * (2.35f - Mathf.Sin(t * Mathf.PI) * .4f); mk.Blob(bulb, p, Vector3.one * .05f, ctx.Rng, 0f); }
            mk.Box(wire, c + up * 2.25f, new Vector3(.01f, .01f, 3.2f), Quaternion.LookRotation(side));
            Lit(ctx, c + up * 1.9f + face * .8f, Warm, 2.4f);
        }

        // ---------------------------------------------------------------- Mitski, Noah Kahan, Daughter
        void Kennel(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(planks, c + up * .45f, new Vector3(1.2f, .9f, 1.4f), rot); mk.GableRoof(darkWood, c + up * .9f, new Vector3(1.6f, 0, 1.6f), .55f, Quaternion.LookRotation(side), snowTop);
            mk.Box(iron, c + face * .71f + up * .38f, new Vector3(.5f, .6f, .02f), rot); solid.Box(planks, c + up * .55f, new Vector3(1.3f, 1.1f, 1.5f), rot);
            var ro = c + face * .72f + up * 1.05f + side * .35f;
            mk.Prism(SongGlow(id, new Color(1f, .3f, .3f), 2f), ro, face, .14f, .02f, 12);
            foreach (var s in new[] { -1f, 1f }) mk.Box(SongGlow(id, new Color(1f, .3f, .3f), 2f), ro - up * .2f + side * s * .05f + face * .01f, new Vector3(.06f, .22f, .01f), rot * Quaternion.Euler(0, 0, s * 12f));
            mk.Prism(WorldMaterials.Solid(new Color(.6f, .6f, .65f)), c + face * 1.3f - side * .5f, up, .14f, .08f, 10);
            Lit(ctx, c + up * 1.6f + face * 1.2f, Ember, 1.6f);
        }

        void Mailbox(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Prism(darkWood, c, up, .06f, 1.05f, 5); ArtistBits.PostCollider(ctx.Root, c, 1.3f, .2f);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .4f, .55f)), c + up * 1.2f, new Vector3(.35f, .35f, .7f), rot); mk.Box(snowTop, c + up * 1.4f, new Vector3(.36f, .05f, .7f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.8f, .2f, .2f)), c + up * 1.35f + Vector3.Cross(up, face) * .19f - face * .15f, new Vector3(.02f, .25f, .06f), rot);
            var letter = SongGlow(id, new Color(1f, .92f, .75f), 2f);
            for (var k = 0; k < 4; k++) mk.Box(letter, c + up * (1.18f + k * .05f) + face * (.38f + k * .04f), new Vector3(.22f, .02f, .16f), rot * Quaternion.Euler(k * 8f, k * 15f - 20f, 0));
            for (var k = 0; k < 3; k++) mk.Box(letter, G(ctx, c + face * (.8f + k * .3f) + Vector3.Cross(up, face) * (k - 1f) * .3f) + up * .02f, new Vector3(.22f, .01f, .16f), rot * Quaternion.Euler(0, k * 30f, 0));
            Lit(ctx, c + up * 1.7f + face * .8f, Warm, 1.6f);
        }

        void StickPile(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up;
            for (var k = 0; k < 14; k++) { var a = k * Mathf.PI * 2f / 14f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); mk.Prism(darkWood, c + d * .9f, (up * 2.2f - d * .85f).normalized, .05f, 2.3f, 5); }
            ArtistBits.PostCollider(ctx.Root, c, 2f, 1f);
            var leaf = SongGlow(id, new Color(1f, .35f, .15f), 1.6f);
            for (var k = 0; k < 20; k++) { var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(1.2f, 3f); mk.Box(leaf, G(ctx, c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)) + up * .02f, new Vector3(.12f, .01f, .09f), Quaternion.Euler(0, ctx.Range(0, 360f), 0)); }
            Lit(ctx, c + up * 2.4f + face * .8f, Ember, 1.8f);
        }

        void Signpost(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; mk.Prism(darkWood, c - up * .2f, up, .08f, 2.8f, 6); ArtistBits.PostCollider(ctx.Root, c, 2.6f, .12f);
            string[] names = { "FAR", "FARTHER", "HOME", "AWAY" }; var arrow = SongGlow(id, Warm, 1.2f);
            for (var k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, k * 77f + 20f, 0) * Vector3.forward; var p = c + up * (1.4f + k * .32f) + d * .55f;
                mk.Box(planks, p, new Vector3(.08f, .22f, 1.1f), Quaternion.LookRotation(d)); mk.Box(arrow, p + Vector3.Cross(up, d) * .05f, new Vector3(.01f, .05f, .9f), Quaternion.LookRotation(d));
                ArtistBits.Label(ctx.Root, names[k], p + Vector3.Cross(up, d) * .055f, Vector3.Cross(up, d), .08f, Ink, TextAnchor.MiddleCenter);
            }
            var case_ = G(ctx, c + face * .8f + Vector3.Cross(up, face) * .4f); mk.Box(WorldMaterials.Solid(new Color(.45f, .3f, .22f)), case_ + up * .3f, new Vector3(.7f, .5f, .22f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 15f, 0));
            Lit(ctx, c + up * 2.4f + face * .8f, Warm, 1.8f);
        }

        void MedicineCabinet(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(WorldMaterials.Solid(new Color(.42f, .36f, .32f)), c + up * .9f, new Vector3(1.2f, 1.8f, .5f), rot); solid.Box(darkWood, c + up * .9f, new Vector3(1.2f, 1.8f, .5f), rot);
            mk.Box(snowTop, c + up * 1.83f, new Vector3(1.2f, .06f, .5f), rot);
            Color[] cols = { new Color(.4f, .7f, 1f), new Color(.5f, 1f, .7f), new Color(.85f, .7f, 1f) };
            for (var r = 0; r < 3; r++) { mk.Box(darkWood, c + up * (.5f + r * .45f) + face * .2f, new Vector3(1.05f, .03f, .12f), rot); for (var k = 0; k < 4; k++) mk.Prism(SongGlow(id, cols[(r + k) % 3], 1.1f), c + up * (.52f + r * .45f) + face * .22f + side * (k - 1.5f) * .24f, up, .06f, .18f + (k % 2) * .06f, 8, .7f); }
            Lit(ctx, c + up * 1.2f + face * 1f, Frost, 1.6f);
        }

        void BuriedSwings(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) mk.Prism(iron, G(ctx, c + side * s * 1.6f + face * t * .5f), (up * 2.6f - face * t * .5f).normalized, .05f, 2.65f, 6);
            mk.Box(iron, c + up * 2.6f, new Vector3(3.3f, .08f, .08f), Quaternion.LookRotation(side));
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(ctx.Root, c + side * s * 1.6f, 2.6f, .3f);
            var seat = SongGlow(id, Frost, 1.6f);
            foreach (var s in new[] { -.6f, .6f }) { foreach (var e in new[] { -.22f, .22f }) mk.Box(iron, c + side * (s + e) + up * 1.55f, new Vector3(.015f, 2f, .015f), rot); mk.Box(seat, c + side * s + up * .5f, new Vector3(.6f, .05f, .25f), rot); }
            mk.Blob(snowTop, c + up * .15f, new Vector3(2.4f, .45f, 1.6f), ctx.Rng, .15f);
            Lit(ctx, c + up * 2f + face * 1f, Frost, 2f);
        }

        // ---------------------------------------------------------------- one-song artists: a piece in their clearing
        static readonly Dictionary<string, string> PieceSong = new Dictionary<string, string>
        {
            { "Andrew Bird", "6HkIgqeABgeFyk0yuCnoU3" }, { "Andréa Daltro", "2EHoGfdxiSCYXsrt7SDMUq" }, { "Bright Eyes", "39Mzqwp2PBwMy61CVTuLDl" },
            { "Caamp", "2HQsCNZ50sGTMQFPJeiHMu" }, { "Cameron Winter", "2zf1izCOz2F22PF27uhxRF" }, { "Jessie Mazin", "7HFA3UBP3nUyjZtDtQ4ml7" },
            { "Lord Huron", "4FE9SgxcOoCNuGnaaU8TXz" }, { "Lucy Dacus", "1yYlpGuBiRRf33e1gY61bN" }, { "Neutral Milk Hotel", "1NiAMp0CzUdG6e97VYRB1a" },
            { "Of Monsters and Men", "2ihCaVdNZmnHZWt0fvAM7B" }, { "Sufjan Stevens", "1yupbrI7ROhigIHpQBevPh" }, { "The Lumineers", "3d8y0t70g7hw2FOWl9Z4Fm" },
            { "The Middle East", "13yuX1iqobZtQtq5lr8UoC" }, { "The Millennium", "3Bw3v2zZpA1Yo5faG2WYON" }, { "Vance Joy", "6liNJDyWJjyHAgDnAX95bw" },
            { "Will Backerman", "5PrMtXEFRE7MMRMnMTAwy4" }, { "boygenius", "74uoN2UfgdUAzwVeFaRUE2" },
        };

        /// By each one-song artist's post in its clearing, a piece from their song, glowing while it plays.
        void SongPiece(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, IslandWorldData.Artist a, Vector3 center, Vector3 front, Vector3 right)
        {
            if (!PieceSong.TryGetValue(a.name, out var id)) return;
            var up = Vector3.up; var p = G(ctx, center - right * 3.4f + front * .6f); var rot = Quaternion.LookRotation(front);
            switch (a.name)
            {
                case "Andrew Bird":   // "Orpheo Looks Back": a lyre on a stump, turned to look behind
                    mk.Prism(logs, p, up, .35f, .6f, 8); solid.Prism(logs, p, up, .35f, .6f, 8);
                    var lr = Quaternion.LookRotation(-front); var lyre = SongGlow(id, Gold, 1.6f);
                    foreach (var s in new[] { -1f, 1f }) mk.Box(lyre, p + up * 1f + right * s * .2f, new Vector3(.05f, .75f, .05f), lr * Quaternion.Euler(0, 0, s * -12f));
                    mk.Box(lyre, p + up * 1.36f, new Vector3(.5f, .05f, .05f), lr); for (var k = -2; k <= 2; k++) mk.Box(WorldMaterials.Solid(new Color(.9f, .9f, .85f)), p + up * 1f + right * k * .06f, new Vector3(.008f, .7f, .008f), lr);
                    Lit(ctx, p + up * 1.4f + front * .6f, Gold, 1.4f); break;
                case "Andréa Daltro":   // "Kiuá": a little bird singing on a branch, its song in glowing notes
                    mk.Prism(darkWood, p, (up + right * .2f).normalized, .05f, 1.6f, 5); ArtistBits.PostCollider(root, p, 1.6f, .1f);
                    var bird = p + up * 1.62f + right * .3f; mk.Blob(WorldMaterials.Solid(new Color(.75f, .45f, .3f)), bird, new Vector3(.12f, .1f, .16f), ctx.Rng, 0f); mk.Blob(WorldMaterials.Solid(new Color(.75f, .45f, .3f)), bird + up * .1f + front * .08f, Vector3.one * .07f, ctx.Rng, 0f);
                    for (var k = 0; k < 5; k++) mk.Blob(SongGlow(id, Gold, 2.6f), bird + front * (.3f + k * .22f) + up * (.2f + Mathf.Sin(k * 1.4f) * .15f), Vector3.one * .04f, ctx.Rng, 0f);
                    Lit(ctx, bird + front * .6f, Gold, 1.2f); break;
                case "Bright Eyes":   // "At the Bottom of Everything": a stone well with light at the bottom
                    for (var k = 0; k < 10; k++) { var a2 = k * Mathf.PI / 5f; mk.Box(stone, p + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * .7f + up * .4f, new Vector3(.45f, .8f, .25f), Quaternion.Euler(0, -a2 * Mathf.Rad2Deg + 90f, 0)); }
                    solid.Prism(stone, p, up, .9f, .8f, 10); mk.Prism(SongGlow(id, new Color(.6f, .8f, 1f), 2.2f), p + up * .2f, up, .5f, .02f, 12);
                    mk.Box(snowTop, p + up * .83f + right * .5f, new Vector3(.4f, .05f, .3f), rot); Lit(ctx, p + up * 1.2f, Frost, 1.2f); break;
                case "Caamp":   // "By and By": a banjo leaning on a chair
                    mk.Box(darkWood, p + up * .45f, new Vector3(.5f, .06f, .5f), rot); mk.Box(darkWood, p + up * .75f - front * .23f, new Vector3(.5f, .6f, .05f), rot);
                    foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) mk.Box(darkWood, p + up * .22f + right * s * .2f + front * t * .2f, new Vector3(.04f, .45f, .04f), rot);
                    solid.Box(darkWood, p + up * .45f, new Vector3(.55f, .9f, .55f), rot);
                    var bj = p + front * .4f + up * .55f; mk.Prism(SongGlow(id, new Color(1f, .9f, .7f), 1.6f), bj, front, .18f, .05f, 14); mk.Box(darkWood, bj + up * .45f - front * .08f, new Vector3(.05f, .8f, .04f), rot * Quaternion.Euler(-10f, 0, 0));
                    Lit(ctx, p + up * 1.2f + front * .8f, Warm, 1.2f); break;
                case "Cameron Winter":   // "Love Takes Miles": a milestone with a heart cut in it
                    mk.Box(stone, p + up * .5f, new Vector3(.6f, 1f, .3f), rot); mk.Prism(stone, p + up * 1f, right, .3f, .6f, 10); solid.Box(stone, p + up * .6f, new Vector3(.6f, 1.2f, .3f), rot);
                    var hc = SongGlow(id, new Color(1f, .35f, .4f), 2.2f); foreach (var s in new[] { -1f, 1f }) mk.Box(hc, p + up * .78f + right * s * .06f + front * .16f, new Vector3(.12f, .2f, .02f), rot * Quaternion.Euler(0, 0, s * 40f));
                    ArtistBits.Label(ctx.Root, "MILES", p + up * .42f + front * .16f, front, .08f, new Color(.2f, .2f, .22f), TextAnchor.MiddleCenter);
                    Lit(ctx, p + up * 1.2f + front * .6f, new Color(1f, .4f, .45f), 1.2f); break;
                case "Jessie Mazin":   // "the precipice": a lantern at the very edge of a stone ledge
                    mk.Box(stone, p + up * .25f, new Vector3(2f, .5f, 1.2f), rot); solid.Box(stone, p + up * .25f, new Vector3(2f, .5f, 1.2f), rot); mk.Box(snowTop, p + up * .52f, new Vector3(1.9f, .04f, 1.1f), rot);
                    mk.Box(iron, p + up * .6f + front * .5f + right * .8f, new Vector3(.2f, .05f, .2f), rot); mk.Box(SongGlow(id, Warm, 2.4f), p + up * .78f + front * .5f + right * .8f, new Vector3(.16f, .24f, .16f), rot);
                    Lit(ctx, p + up * 1.1f + front * .8f + right * .8f, Warm, 1.2f); break;
                case "Lord Huron":   // "When the Night is Over": a crescent moon lantern on a tall pole
                    mk.Prism(darkWood, p, up, .06f, 3f, 5); ArtistBits.PostCollider(root, p, 3f, .1f); var moon = SongGlow(id, new Color(.85f, .9f, 1f), 2.4f);
                    for (var k = 0; k <= 8; k++) { var a2 = -Mathf.PI / 2f + k * Mathf.PI / 8f; mk.Blob(moon, p + up * 3.4f + (up * Mathf.Sin(a2) + right * Mathf.Cos(a2)) * .32f, Vector3.one * (.05f + .05f * Mathf.Cos(a2)), ctx.Rng, 0f); }
                    Lit(ctx, p + up * 3.3f + front * .5f, Frost, 1.4f); break;
                case "Lucy Dacus":   // "Night Shift": a punch clock on a post with its card in
                    mk.Prism(darkWood, p, up, .06f, 1.2f, 5); ArtistBits.PostCollider(root, p, 1.5f, .15f); mk.Box(WorldMaterials.Solid(new Color(.45f, .5f, .45f)), p + up * 1.45f, new Vector3(.4f, .55f, .25f), rot);
                    mk.Prism(SongGlow(id, new Color(1f, .95f, .8f), 1.8f), p + up * 1.55f + front * .13f, front, .13f, .01f, 12); mk.Box(WorldMaterials.Solid(new Color(.95f, .9f, .75f)), p + up * 1.75f + front * .02f, new Vector3(.1f, .2f, .01f), rot);
                    Lit(ctx, p + up * 1.6f + front * .6f, Warm, 1.2f); break;
                case "Neutral Milk Hotel":   // "In the Aeroplane Over the Sea": a little biplane hung from a birch branch
                    mk.Prism(bark, p - up * .3f, up, .12f, 4.4f, 6, .6f); ArtistBits.PostCollider(root, p, 4f, .15f); mk.Prism(bark, p + up * 3.2f, (right + up * .2f).normalized, .05f, 1.4f, 5);
                    var pl = p + right * 1.1f + up * 2.4f; mk.Box(iron, pl + up * .45f, new Vector3(.01f, .8f, .01f), Quaternion.identity); var plane = SongGlow(id, new Color(1f, .85f, .5f), 1.6f);
                    mk.Box(plane, pl, new Vector3(.12f, .12f, .6f), rot); mk.Box(plane, pl + front * .08f, new Vector3(.8f, .03f, .16f), rot); mk.Box(plane, pl + front * .08f + up * .18f, new Vector3(.8f, .03f, .16f), rot);
                    Lit(ctx, pl + front * .6f, Warm, 1.2f); break;
                case "Of Monsters and Men":   // "Little Talks": two chairs turned to each other with a lantern between
                    foreach (var s in new[] { -1f, 1f }) { var ch = p + right * s * .6f; var cr = Quaternion.LookRotation(-right * s); mk.Box(darkWood, ch + up * .45f, new Vector3(.45f, .06f, .45f), cr); mk.Box(darkWood, ch + up * .75f + right * s * .2f, new Vector3(.45f, .6f, .05f), cr); solid.Box(darkWood, ch + up * .45f, new Vector3(.5f, .9f, .5f), cr); }
                    mk.Box(SongGlow(id, Warm, 2.2f), p + up * .15f, new Vector3(.18f, .26f, .18f), Quaternion.identity); Lit(ctx, p + up * .8f + front * .4f, Warm, 1.2f); break;
                case "Sufjan Stevens":   // "Chicago": a little road sign pointing the long way
                    mk.Prism(iron, p, up, .05f, 2f, 6); ArtistBits.PostCollider(root, p, 2f, .1f); mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .3f)), p + up * 1.9f + right * .3f, new Vector3(1.3f, .35f, .04f), rot);
                    ArtistBits.Label(ctx.Root, "CHICAGO", p + up * 1.9f + right * .3f + front * .03f, front, .1f, Ink, TextAnchor.MiddleCenter);
                    mk.Box(SongGlow(id, Gold, 2f), p + up * 1.68f + right * .3f + front * .03f, new Vector3(1.1f, .03f, .01f), rot); Lit(ctx, p + up * 2f + front * .6f, Gold, 1.2f); break;
                case "The Lumineers":   // "Ophelia": a flower crown on a rock, its petals glowing
                    mk.Blob(stone, p + up * .25f, new Vector3(.6f, .3f, .5f), ctx.Rng, .15f); ArtistBits.PostCollider(root, p, .6f, .5f);
                    for (var k = 0; k < 10; k++) { var a2 = k * Mathf.PI / 5f; mk.Blob(SongGlow(id, k % 2 == 0 ? new Color(1f, .7f, .8f) : new Color(1f, .9f, .6f), 1.6f), p + up * .58f + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * .22f, Vector3.one * .06f, ctx.Rng, 0f); }
                    Lit(ctx, p + up * 1f + front * .4f, new Color(1f, .75f, .8f), 1.2f); break;
                case "The Middle East":   // "Blood": a red ribbon tied round a birch
                    mk.Prism(bark, p - up * .3f, up, .16f, 4.8f, 7, .6f); ArtistBits.PostCollider(root, p, 4.4f, .2f);
                    var rib = SongGlow(id, new Color(.9f, .1f, .12f), 2f); mk.Prism(rib, p + up * 1.5f, up, .19f, .1f, 10);
                    mk.Box(rib, p + up * 1.4f + front * .22f + right * .05f, new Vector3(.06f, .5f, .02f), rot * Quaternion.Euler(0, 0, 10f)); Lit(ctx, p + up * 1.6f + front * .6f, Ember, 1.2f); break;
                case "The Millennium":   // "5 a.m.": a little clock lantern showing five
                    mk.Prism(darkWood, p, up, .05f, 1.4f, 5); ArtistBits.PostCollider(root, p, 1.6f, .12f); mk.Prism(iron, p + up * 1.6f - front * .05f, front, .25f, .1f, 14);
                    mk.Prism(SongGlow(id, new Color(1f, .85f, .7f), 1.8f), p + up * 1.6f + front * .05f, front, .22f, .01f, 14);
                    mk.Box(iron, p + up * 1.68f + front * .065f, new Vector3(.02f, .17f, .005f), rot); mk.Box(iron, p + up * 1.53f + front * .065f + right * .04f, new Vector3(.02f, .12f, .005f), rot * Quaternion.Euler(0, 0, -150f));
                    Lit(ctx, p + up * 1.6f + front * .6f, Warm, 1.2f); break;
                case "Vance Joy":   // "Fire and the Flood": a little fire bowl beside a frozen puddle
                    mk.Prism(iron, p, up, .4f, .35f, 10, 1.3f); solid.Prism(iron, p, up, .45f, .4f, 8); mk.Blob(SongGlow(id, Ember, 2.8f), p + up * .45f, new Vector3(.3f, .2f, .3f), ctx.Rng, .3f);
                    mk.Prism(ice, G(ctx, p + right * 1.3f) + up * .02f, up, .8f, .02f, 12, 1f, 15f); Lit(ctx, p + up * .9f + front * .4f, Ember, 1.4f); break;
                case "Will Backerman":   // "Pawn Shop Ring": a ring in an open box on a stump
                    mk.Prism(logs, p, up, .32f, .65f, 8); solid.Prism(logs, p, up, .32f, .65f, 8); mk.Box(WorldMaterials.Solid(new Color(.4f, .1f, .15f)), p + up * .72f, new Vector3(.18f, .12f, .18f), rot);
                    mk.Box(WorldMaterials.Solid(new Color(.4f, .1f, .15f)), p + up * .82f - front * .08f, new Vector3(.18f, .16f, .03f), rot * Quaternion.Euler(-20f, 0, 0));
                    var ring = SongGlow(id, Gold, 2.6f); for (var k = 0; k < 10; k++) { var a2 = k * Mathf.PI / 5f; mk.Blob(ring, p + up * (.88f + Mathf.Sin(a2) * .05f) + right * Mathf.Cos(a2) * .05f, Vector3.one * .018f, ctx.Rng, 0f); }
                    Lit(ctx, p + up * 1.1f + front * .4f, Gold, 1f); break;
                case "boygenius":   // "Me & My Dog": a dog curled up by a lantern on a blanket
                    mk.Box(WorldMaterials.Solid(new Color(.55f, .35f, .3f)), p + up * .02f, new Vector3(1.6f, .03f, 1.2f), rot);
                    var dog = WorldMaterials.Solid(new Color(.5f, .4f, .3f)); mk.Blob(dog, p + up * .25f, new Vector3(.45f, .22f, .3f), ctx.Rng, 0f); mk.Blob(dog, p + up * .28f + right * .35f + front * .1f, Vector3.one * .14f, ctx.Rng, 0f);
                    ArtistBits.PostCollider(root, p, .5f, .5f); mk.Box(SongGlow(id, Warm, 2.4f), p + up * .15f - right * .6f, new Vector3(.16f, .24f, .16f), Quaternion.identity);
                    Lit(ctx, p + up * .7f - right * .5f + front * .3f, Warm, 1.2f); break;
            }
        }
            // ---------------------------------------------------------------- inside the two halls
        /// Furnishes the boathouse (Phoebe Bridgers) or the treehouse (Big Thief) so they feel lived in: fairy lights
        /// under the roof, a rug, and each its own things. P(x, y, z) is the room's frame (floor at y 0, z toward the
        /// big opening or the door).
        void Furnish(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, System.Func<float, float, float, Vector3> P, Quaternion rot, float w, float d, float wallH, bool boathouse)
        {
            var bulb = WorldMaterials.Glow(new Color(1f, .78f, .45f), 2.4f); var wire = WorldMaterials.Solid(new Color(.12f, .1f, .1f));
            foreach (var z in new[] { -d / 4f, d / 4f })
                for (var k = 0; k <= 14; k++)
                {
                    var t = k / 14f; var p = P(-w / 2f + .3f + t * (w - .6f), wallH - .35f - Mathf.Sin(t * Mathf.PI) * .55f, z);
                    mk.Blob(bulb, p, Vector3.one * .06f, ctx.Rng, 0f);
                    if (k % 3 == 1) halos.Add(p, .5f, new Color(1f, .78f, .45f), .4f);
                }
            mk.Box(wire, P(0, wallH - .5f, -d / 4f), new Vector3(w - .6f, .01f, .01f), rot); mk.Box(wire, P(0, wallH - .5f, d / 4f), new Vector3(w - .6f, .01f, .01f), rot);
            var rug = WorldMaterials.Solid(boathouse ? new Color(.3f, .36f, .5f) : new Color(.55f, .3f, .2f));
            mk.Box(rug, P(0, .015f, .3f), new Vector3(w * .55f, .02f, d * .45f), rot);
            var up = Vector3.up; var right = rot * Vector3.right; var fwd = rot * Vector3.forward;
            if (boathouse)
            {
                // a rowing boat up on trestles along the right wall, oars crossed above it
                var boat = P(w / 2f - 1.6f, .9f, -.5f);
                mk.Blob(canoeRed, boat, new Vector3(.7f, .35f, 2.4f), ctx.Rng, .03f); mk.Box(darkWood, boat + up * .25f, new Vector3(1f, .05f, .25f), rot);
                foreach (var z in new[] { -1.4f, 1.4f }) foreach (var s in new[] { -1f, 1f }) mk.Box(darkWood, P(w / 2f - 1.6f + s * .45f, .3f, -.5f + z), new Vector3(.08f, .6f, .08f), rot * Quaternion.Euler(0, 0, s * 12f));
                solid.Box(darkWood, P(w / 2f - 1.6f, .6f, -.5f), new Vector3(1.5f, 1.2f, 5f), rot);
                foreach (var s in new[] { -1f, 1f }) mk.Box(planks, P(w / 2f - .25f, 2.9f, -.5f), new Vector3(.06f, .12f, 3f), rot * Quaternion.Euler(s * 25f, 0, 0));
                // an armchair facing the lake, a floor lamp, a record player on a crate, candles
                var chair = P(-1.8f, .03f, 2.2f);   // standing on the rug
                var velvet = WorldMaterials.Solid(new Color(.45f, .3f, .38f));
                mk.Box(velvet, chair + up * .3f, new Vector3(1.1f, .6f, 1f), rot); mk.Box(velvet, chair + up * .85f - fwd * .4f, new Vector3(1.1f, .9f, .25f), rot);
                foreach (var s in new[] { -1f, 1f }) mk.Box(velvet, chair + up * .65f + right * s * .5f, new Vector3(.2f, .35f, .9f), rot);
                solid.Box(velvet, chair + up * .5f, new Vector3(1.1f, 1f, 1f), rot);
                var lamp = chair + right * -1f; mk.Prism(iron, lamp, up, .03f, 1.6f, 6); mk.Prism(WorldMaterials.Glow(new Color(1f, .8f, .55f), 1.8f), lamp + up * 1.55f, up, .25f, .3f, 10, .6f);
                ArtistBits.PostCollider(root, lamp, 1.8f, .2f); halos.Add(lamp + up * 1.6f, 1.2f, new Color(1f, .78f, .5f), .6f);
                var crate = P(-w / 2f + 1f, 0, 2.6f); mk.Box(planks, crate + up * .3f, new Vector3(.7f, .6f, .6f), rot); solid.Box(planks, crate + up * .3f, new Vector3(.7f, .6f, .6f), rot);
                mk.Box(WorldMaterials.Solid(new Color(.3f, .25f, .22f)), crate + up * .66f, new Vector3(.55f, .1f, .45f), rot); mk.Prism(WorldMaterials.Solid(new Color(.06f, .06f, .07f)), crate + up * .72f, up, .19f, .01f, 16);
                var cand = P(1.6f, 0, -d / 2f + 1f); mk.Box(planks, cand + up * .25f, new Vector3(.6f, .5f, .6f), rot); solid.Box(planks, cand + up * .25f, new Vector3(.6f, .5f, .6f), rot);
                for (var k = 0; k < 4; k++) { var p = cand + up * .5f + right * ((k % 2) - .5f) * .25f + fwd * ((k / 2) - .5f) * .25f; mk.Prism(candle, p, up, .035f, .12f + k * .04f, 6); mk.Blob(WorldMaterials.Glow(new Color(1f, .75f, .4f), 3f), p + up * (.17f + k * .04f), new Vector3(.025f, .045f, .025f), ctx.Rng, 0f); }
                halos.Add(cand + up * .8f, .9f, new Color(1f, .75f, .4f), .6f);
            }
            else
            {
                // a little woodstove in the corner with its window glowing, its pipe up through the roof
                var stove = P(w / 2f - .9f, 0, -d / 2f + .9f);
                mk.Box(iron, stove + up * .45f, new Vector3(.7f, .9f, .6f), rot); mk.Prism(iron, stove + up * .9f, up, .1f, wallH + 1.5f, 8);
                mk.Box(WorldMaterials.Glow(new Color(1f, .5f, .2f), 2.6f), stove + up * .45f + fwd * .31f, new Vector3(.35f, .3f, .02f), rot);
                solid.Box(iron, stove + up * .45f, new Vector3(.8f, .9f, .7f), rot); halos.Add(stove + up * .5f + fwd * .8f, 1f, new Color(1f, .5f, .2f), .7f);
                ArtistBits.PointLight(root, stove + up * .6f + fwd * .8f, new Color(1f, .55f, .25f), 6f, 1.4f, true);
                // floor cushions round a low table with mugs and a candle
                var table = P(-.6f, 0, .8f); mk.Box(darkWood, table + up * .3f, new Vector3(1.2f, .07f, .8f), rot);
                foreach (var s in new[] { -1f, 1f }) mk.Box(darkWood, table + up * .14f + right * s * .5f, new Vector3(.07f, .28f, .7f), rot);
                solid.Box(darkWood, table + up * .17f, new Vector3(1.2f, .35f, .8f), rot);
                Color[] cush = { new Color(.7f, .45f, .3f), new Color(.45f, .5f, .35f), new Color(.6f, .5f, .3f), new Color(.5f, .35f, .45f) };
                for (var k = 0; k < 4; k++) { var a = k * Mathf.PI / 2f + .4f; mk.Blob(WorldMaterials.Solid(cush[k]), table + (right * Mathf.Cos(a) + fwd * Mathf.Sin(a)) * 1.2f + up * .14f, new Vector3(.4f, .14f, .4f), ctx.Rng, .1f); }
                foreach (var s in new[] { -1f, 1f }) mk.Prism(WorldMaterials.Solid(new Color(.85f, .82f, .75f)), table + up * .34f + right * s * .3f, up, .06f, .12f, 8);
                mk.Prism(candle, table + up * .34f, up, .04f, .14f, 6); mk.Blob(WorldMaterials.Glow(new Color(1f, .75f, .4f), 3f), table + up * .53f, new Vector3(.025f, .05f, .025f), ctx.Rng, 0f);
                halos.Add(table + up * .7f, .9f, new Color(1f, .75f, .4f), .6f);
                // a guitar leaning on the wall, and plants along the other side
                var gt = P(-w / 2f + .35f, 0, -1.2f); mk.Blob(WorldMaterials.Solid(new Color(.75f, .5f, .25f)), gt + up * .45f + right * .05f, new Vector3(.3f, .38f, .12f), ctx.Rng, 0f);
                mk.Box(darkWood, gt + up * 1.05f, new Vector3(.06f, .8f, .05f), rot * Quaternion.Euler(0, 0, -8f));
                for (var k = 0; k < 3; k++) { var pp = P(-w / 2f + .5f, 0, 1.2f + k * .7f); mk.Prism(WorldMaterials.Solid(new Color(.6f, .35f, .25f)), pp, up, .17f, .3f, 8, 1.2f); mk.Blob(leafPale, pp + up * .5f, new Vector3(.28f, .3f, .28f), ctx.Rng, .25f); }
            }
        }
    }
}
