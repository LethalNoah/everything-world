using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Fens' deep pass (part of FensKit): dreamy, floaty song places out in the open water near their artists'
    /// islands, reached by wading along a line of floating candles, each glowing (and moving a little more) while
    /// its song plays.
    /// - Deafheaven: a little house adrift on a raft of light ("Dream House"), a ring of glass pillars in every
    ///   colour ("Great Mass of Color"), a stair climbing out of the water to a glowing door in mid-air ("Vertigo").
    /// - Mazzy Star: a broken stone arch with a lantern looking down at its own reflection ("Look On Down From The
    ///   Bridge"), a frosted pier with snow hanging in the air ("Quiet, The Winter Harbor"), a thicket of glowing
    ///   roses with petals circling on the water ("Roseblood").
    /// - Slowdive: a great golden ring standing in the water ("When the Sun Hits"), a stone chair with paper
    ///   lanterns circling round it ("Alison").
    /// - my bloody valentine: a crescent moon rocking on the water with a pillow and a blanket ("When You Sleep"),
    ///   three terraced pools of pink light ("Only Shallow").
    public partial class FensKit
    {
        readonly List<Vector3> placeSpots = new List<Vector3>();

        Material SongGlow(string song, Color c, float s = 2f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" };
            life.songGlows.Add((song, m, c * (s * .7f)));
            return m;
        }

        /// A moving piece: geometry is built round its own origin, then the life bobs, rocks or turns it.
        (Transform t, MeshKit mk) Mover(LandContext ctx, string name, Vector3 at)
        {
            var go = new GameObject(name).transform; go.SetParent(ctx.Root, false); go.position = at;
            return (go, new MeshKit());
        }

        void Lit(LandContext ctx, Vector3 at, Color col, float r = 3f)
        {
            ArtistBits.PointLight(ctx.Root, at, col, 14f, 2f);
            halos.Add(at, r, col, .45f);
        }

        /// Open water round a spot: every point of a ring is under water, off the islands and the paths.
        bool OpenWater(LandContext ctx, float x, float z, float clear)
        {
            if (!InMarsh(x, z, clear + 4f) || NearPath(x, z, clear + 2f) || InNoGrass(x, z, clear)) return false;
            if (Vector2.Distance(new Vector2(x, z), heart) < CathR + clear + 8f) return false;
            for (var k = 0; k < 12; k++) foreach (var f in new[] { .5f, 1f })
            {
                var a = k * Mathf.PI / 6f; float px = x + Mathf.Cos(a) * clear * f, pz = z + Mathf.Sin(a) * clear * f;
                if (!Wet(ctx, px, pz) || OnIsland(px, pz, -3f)) return false;
            }
            return Wet(ctx, x, z);
        }

        Vector3 PlaceSpot(LandContext ctx, Vector3 near, float nearR, float clear)
        {
            for (var tries = 0; tries < 1400; tries++)
            {
                var wide = tries > 800; var a = ctx.Range(0f, Mathf.PI * 2f);
                var r = wide ? ctx.Range(CathR + 10f, 320f) : ctx.Range(nearR + clear + 6f, nearR + clear + 50f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                if (!OpenWater(ctx, px, pz, clear)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < s.y + clear + 8f) { ok = false; break; }
                if (!ok) continue;
                placeSpots.Add(new Vector3(px, clear, pz)); noGrass.Add(new Vector3(px, clear + 1f, pz));
                return new Vector3(px, waterY - FloorDrop, pz);
            }
            return Vector3.positiveInfinity;
        }

        /// The way to a place: a line of floating candles on lily pads from the nearest path, a sign on a post
        /// in the water, and the photo tour's mark. Returns the way the place faces (toward the path).
        Vector3 PlaceWay(LandContext ctx, MeshKit mk, Vector3 c, float clear, string sign, string mark, Color col)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var (p, _) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(center.x - c.x, 0, center.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var side = Vector3.Cross(Vector3.up, face);
            var pad = WorldMaterials.Solid(new Color(.36f, .5f, .4f)); var candle = GlowM(new Color(1f, .82f, .6f), 2.6f);
            if (best < float.MaxValue)
            {
                var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - clear - 1f;
                for (var d = 3f; d < len; d += 4.5f)
                {
                    var q = from + dir * d + side * Mathf.Sin(d * .25f) * .8f;
                    if (!Wet(ctx, q.x, q.z)) continue;
                    var w = new Vector3(q.x, waterY - WaterDrop + .012f, q.z);
                    soft.Prism(pad, w, Vector3.up, .5f, .02f, 9, 1f, d * 37f);       // floating: you wade through them
                    soft.Prism(WorldMaterials.Solid(new Color(.95f, .92f, .86f)), w + Vector3.up * .02f, Vector3.up, .06f, .16f, 6);
                    soft.Blob(candle, w + Vector3.up * .24f, new Vector3(.05f, .08f, .05f), ctx.Rng, 0f);
                    halos.Add(w + Vector3.up * .3f, .5f, new Color(1f, .82f, .6f), .6f);
                }
            }
            var sp = new Vector3(c.x, 0, c.z) + face * (clear + 1f) + side * 2.4f; sp.y = ctx.Ground(sp.x, sp.z); var rot = Quaternion.LookRotation(face);
            mk.Prism(iron, sp - Vector3.up * .1f, Vector3.up, .07f, 2.2f, 6);
            mk.Box(stoneDark, sp + Vector3.up * 1.85f, new Vector3(2.4f, .5f, .06f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.85f + face * .04f, face, .16f, Ink, TextAnchor.MiddleCenter);
            mk.Blob(GlowM(col, 2.4f), sp + Vector3.up * 2.35f, Vector3.one * .12f, ctx.Rng, 0f);
            ArtistBits.PostCollider(ctx.Root, sp, 2.2f, .12f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = new Vector3(c.x, 0, c.z) + face * (clear + 7f) - side * 5f; cam.y = Mathf.Max(ctx.Ground(cam.x, cam.z), waterY) + 1.2f;
            var look = new Vector3(c.x, waterY + 2.2f, c.z) - (cam + Vector3.up * .4f);
            m.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            placeSpots.Clear();
            var placed = 0;
            void Place(string artist, float clear, string sign, string mark, Color col, System.Action<Vector3, Vector3> build)
            {
                var a = ctx.Artists.Find(x => x.name == artist);
                var near = a != null ? new Vector3(a.x, 0, a.z) : center; var nearR = a != null ? IslandR(a) : CathR;
                var c = PlaceSpot(ctx, near, nearR, clear);
                if (float.IsInfinity(c.x)) { Debug.Log("The Shoegaze Fens: no room in the water for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clear, sign, mark, col)); placed++;
                Debug.Log($"The Shoegaze Fens: {mark} at x {c.x:0}, z {c.z:0}");
            }
            Place("Deafheaven", 6f, "DREAM HOUSE", "the house adrift", Gold, (c, f) => DriftHouse(ctx, solid, c, f, "6l3A2EWZSOA8LTMmzRCyD3"));
            Place("Deafheaven", 7f, "GREAT MASS OF COLOR", "the ring of colour", Violet, (c, f) => ColourRing(ctx, mk, solid, c, f, "7qlNaXjqDPJY8VuiY2Q6Gw"));
            Place("Deafheaven", 6f, "VERTIGO", "the stair into the air", Lilac, (c, f) => AirStair(ctx, mk, solid, c, f, "1Oe2Um6K7AsiDjKOBKKbZP"));
            Place("Mazzy Star", 7f, "LOOK ON DOWN FROM THE BRIDGE", "the broken arch", Gold, (c, f) => BrokenArch(ctx, mk, solid, c, f, "010ALbbh5KlQ4fKWgSdhRd"));
            Place("Mazzy Star", 6f, "QUIET, THE WINTER HARBOR", "the winter pier", new Color(.6f, .8f, 1f), (c, f) => WinterPier(ctx, mk, solid, c, f, "4V79NKNUFkjzHWcPLtdlNM"));
            Place("Mazzy Star", 5f, "ROSEBLOOD", "the rose thicket", new Color(1f, .25f, .35f), (c, f) => RoseThicket(ctx, mk, solid, c, f, "2qV5Ce0sMlFsrseRO5CTMU"));
            Place("Slowdive", 6f, "WHEN THE SUN HITS", "the golden ring", Gold, (c, f) => GoldenRing(ctx, mk, solid, c, f, SunSong));
            Place("Slowdive", 6f, "ALISON", "the lantern chair", HotPink, (c, f) => LanternChair(ctx, mk, solid, c, f, "33HRECrmuelZxOpid6XTNX"));
            Place("my bloody valentine", 6f, "WHEN YOU SLEEP", "the moon cradle", Lilac, (c, f) => MoonCradle(ctx, solid, c, f, "3HfEgAaf0koxBpBB8NvGda"));
            Place("my bloody valentine", 6f, "ONLY SHALLOW", "the shallow pools", HotPink, (c, f) => ShallowPools(ctx, mk, solid, c, f, "2pQ4A6w5HSurB5WiaLFhcF"));
            Debug.Log($"The Shoegaze Fens: {placed} song places");
        }

        float Surface => waterY - WaterDrop;

        // ---------------------------------------------------------------- Deafheaven
        void DriftHouse(LandContext ctx, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var at = new Vector3(c.x, Surface, c.z);
            var (t, mk) = Mover(ctx, "Dream House adrift", at);
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var raft = SongGlow(id, new Color(1f, .62f, .5f), 1.6f); var warm = SongGlow(id, Gold, 2.6f);
            mk.Prism(raft, -up * .1f, up, 3.4f, .22f, 14);
            mk.Box(black, up * 1.45f, new Vector3(3.2f, 2.7f, 3.2f), rot);
            mk.GableRoof(stoneDark, up * 2.8f, new Vector3(3.8f, 0, 3.8f), 1.7f, Quaternion.LookRotation(side));
            mk.Box(warm, face * 1.62f + up * 1.05f, new Vector3(.9f, 1.9f, .06f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Box(warm, face * 1.62f + side * s * 1.05f + up * 1.7f, new Vector3(.6f, .6f, .06f), rot);
                mk.Box(warm, side * s * 1.62f + up * 1.7f, new Vector3(.06f, .7f, 1f), rot);
            }
            mk.Box(warm, -face * 1.62f + up * 1.7f, new Vector3(1f, .7f, .06f), rot);
            mk.Prism(black, side * .8f + up * 3.2f, up, .22f, 1.1f, 6);
            mk.Build("House", t, false);
            life.movers.Add((t, at, .07f, .5f, 2.2f, 0f, id));
            solid.Box(black, at + up * 1.3f, new Vector3(3.4f, 3f, 3.4f), rot);
            Lit(ctx, at + face * 3f + up * 2f, Gold, 3.4f);
        }

        void ColourRing(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up;
            for (var k = 0; k < 12; k++)
            {
                var a = k * Mathf.PI * 2f / 12f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5f;
                var col = Color.HSVToRGB(k / 12f, .9f, 1f); var h = 3f + 4f * (.5f + .5f * Mathf.Sin(k * 1.9f));
                mk.Prism(SongGlow(id, col, .75f), p, up, .42f, h + FloorDrop, 6, .6f, k * 13f);   // kept low so each stays its own colour under the bloom
                ArtistBits.PostCollider(ctx.Root, p, h + FloorDrop, .45f);
            }
            mk.Prism(SongGlow(id, Lilac, 1.2f), new Vector3(c.x, Surface + .01f, c.z), up, 2.2f, .02f, 16);
            Lit(ctx, new Vector3(c.x, Surface + 3f, c.z), Violet, 4f);
        }

        void AirStair(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var dir = -face; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(dir);
            var foot = c + face * 4f;
            const int n = 18; const float rise = .28f, run = .5f;
            for (var k = 0; k < n; k++)
            {
                var top = waterY - FloorDrop + (k + 1) * rise; var p = foot + dir * (k * run);     // from the fen floor, so you can climb it out of the water
                var h = top - (waterY - FloorDrop) + .1f; var mid = new Vector3(p.x, top - h / 2f, p.z);
                if (k < 4) { mk.Box(stoneDark, mid, new Vector3(1.6f, h, run + .02f), rot); solid.Box(stoneDark, mid, new Vector3(1.6f, h, run), rot); }
                else
                {
                    mk.Box(stoneM, new Vector3(p.x, top - .12f, p.z), new Vector3(1.6f, .24f, run + .02f), rot);
                    solid.Box(stoneM, new Vector3(p.x, top - .12f, p.z), new Vector3(1.6f, .24f, run), rot);
                    if (k % 4 == 1) halos.Add(new Vector3(p.x, top - .3f, p.z), .8f, Lilac, .3f);
                }
            }
            var land = foot + dir * (n * run + .3f); var ly = waterY - FloorDrop + n * rise;
            mk.Box(stoneM, new Vector3(land.x, ly - .12f, land.z), new Vector3(2.2f, .24f, 1.4f), rot); solid.Box(stoneM, new Vector3(land.x, ly - .12f, land.z), new Vector3(2.2f, .24f, 1.4f), rot);
            var door = new Vector3(land.x, ly, land.z) + dir * .55f;
            foreach (var s in new[] { -1f, 1f }) mk.Box(black, door + side * s * .62f + up * 1.15f, new Vector3(.14f, 2.3f, .14f), rot);
            mk.Box(black, door + up * 2.36f, new Vector3(1.38f, .14f, .14f), rot);
            mk.Box(SongGlow(id, new Color(.9f, .8f, 1f), 3f), door + up * 1.12f, new Vector3(1.08f, 2.22f, .04f), rot);
            solid.Box(black, door + up * 1.2f, new Vector3(1.4f, 2.4f, .2f), rot);
            Lit(ctx, door - dir * 1.4f + up * 1.4f, Lilac, 3f);
        }

        // ---------------------------------------------------------------- Mazzy Star
        void BrokenArch(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var baseY = waterY - FloorDrop; const float R = 4.6f; const int n = 13;
            var b = new Vector3(c.x, baseY + 1.2f, c.z);
            for (var k = 0; k < n; k++)
            {
                if (k >= 10) continue;                       // the far end has fallen
                float a0 = k * Mathf.PI / n, a1 = (k + 1) * Mathf.PI / n, am = (a0 + a1) / 2f;
                var p = b + side * (-Mathf.Cos(am) * R) + up * (Mathf.Sin(am) * R);
                var tang = (side * Mathf.Sin(am) + up * Mathf.Cos(am)).normalized;
                var radial = (-side * Mathf.Cos(am) + up * Mathf.Sin(am)).normalized;
                mk.Box(stoneM, p, new Vector3(1.6f, .9f, R * Mathf.PI / n + .04f), Quaternion.LookRotation(tang, radial));
            }
            foreach (var s in new[] { -1f, 1f })
            {
                var pier = b + side * s * R - up * .6f;
                mk.Box(stoneDark, pier, new Vector3(1.4f, 1.3f, 1.8f), rot); solid.Box(stoneDark, pier, new Vector3(1.4f, 1.3f, 1.8f), rot);
            }
            ArtistBits.PostCollider(ctx.Root, b - side * R, 4f, .9f);
            for (var k = 0; k < 3; k++) mk.Box(stoneM, new Vector3(c.x, Surface - .2f, c.z) + side * (R - .4f + k * .9f) + face * (k - 1f) * .8f, new Vector3(1.4f, .7f, .9f), rot * Quaternion.Euler(k * 11f, k * 30f, k * 7f));
            var top = b + up * R;
            mk.Box(iron, top - up * .7f, new Vector3(.03f, 1f, .03f), rot);
            var lamp = SongGlow(id, Gold, 2.8f);
            mk.Blob(lamp, top - up * 1.35f, new Vector3(.24f, .32f, .24f), ctx.Rng, 0f);
            mk.Prism(SongGlow(id, new Color(1f, .7f, .4f), 1f), new Vector3(top.x, Surface + .012f, top.z), up, .9f, .02f, 14);
            Lit(ctx, top - up * 1.6f, Gold, 2.6f);
        }

        void WinterPier(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var frostWood = WorldMaterials.Solid(new Color(.8f, .85f, .92f)); var snow = WorldMaterials.Solid(new Color(.96f, .97f, 1f));
            var deckY = Surface + .45f; var a = c + face * 4f; var b = c - face * 3f; var mid = (a + b) / 2f;
            mk.Box(frostWood, new Vector3(mid.x, deckY - .07f, mid.z), new Vector3(2.2f, .14f, 7.2f), rot);
            solid.Box(frostWood, new Vector3(mid.x, deckY - .3f, mid.z), new Vector3(2.2f, .6f, 7.2f), rot);
            for (var k = 0; k < 4; k++) foreach (var s in new[] { -1f, 1f })
            {
                var p = a - face * (k * 2.3f) + side * s * 1.2f; p.y = waterY - FloorDrop;
                mk.Prism(wood, p, up, .13f, deckY - p.y + .5f, 6); mk.Prism(snow, p + up * (deckY - p.y + .5f), up, .15f, .08f, 6, .6f); ArtistBits.PostCollider(ctx.Root, p, deckY - p.y + .5f, .15f);
            }
            // two steps up out of the water at the near end
            foreach (var (o, top) in new[] { (1.25f, .3f), (.75f, .6f) })
            {
                var st = a + face * o; var sh = top + .05f; var sc = new Vector3(st.x, waterY - FloorDrop + top - sh / 2f, st.z);
                mk.Box(frostWood, sc, new Vector3(2.2f, sh, .5f), rot); solid.Box(frostWood, sc, new Vector3(2.2f, sh, .5f), rot);
            }
            var lampAt = b + side * .8f; lampAt.y = deckY;
            mk.Prism(iron, lampAt, up, .06f, 2.6f, 6); mk.Blob(SongGlow(id, new Color(.6f, .82f, 1f), 3f), lampAt + up * 2.75f, Vector3.one * .2f, ctx.Rng, 0f);
            ArtistBits.PostCollider(ctx.Root, lampAt, 2.6f, .1f);
            var ice = SongGlow(id, new Color(.75f, .88f, 1f), .5f);
            for (var k = 0; k < 18; k++) { var an = k * Mathf.PI * 2f / 18f; var r = 5.2f + Mathf.Sin(k * 2.3f) * .8f; soft.Box(ice, new Vector3(c.x, Surface + .01f + (k % 3) * .004f, c.z) + new Vector3(Mathf.Cos(an), 0, Mathf.Sin(an)) * r, new Vector3(1.3f, .02f, .8f), Quaternion.Euler(0, -an * Mathf.Rad2Deg + k * 17f, 0)); }
            // snow hanging in the air over the pier, turning slowly
            var (t, fm) = Mover(ctx, "Winter snow", new Vector3(c.x, deckY, c.z));
            var flake = SongGlow(id, new Color(.9f, .95f, 1f), 1.6f);
            for (var k = 0; k < 60; k++) fm.Blob(flake, new Vector3(ctx.Range(-5f, 5f), ctx.Range(.6f, 6f), ctx.Range(-5f, 5f)), Vector3.one * ctx.Range(.04f, .08f), ctx.Rng, 0f);
            fm.Build("Soft snow", t, false);   // (walked through: the land audit skips "Soft")
            life.movers.Add((t, t.position, .25f, .3f, 0f, 6f, id));
            Lit(ctx, lampAt + up * 2.4f, new Color(.6f, .8f, 1f), 3.2f);
        }

        void RoseThicket(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var stem = WorldMaterials.Solid(new Color(.16f, .2f, .16f)); var rose = SongGlow(id, new Color(1f, .16f, .28f), 2.4f);
            var leaf = WorldMaterials.Solid(new Color(.24f, .36f, .26f));
            for (var k = 0; k < 16; k++)
            {
                var a = k * 2.4f; var r = .4f + (k % 5) * .55f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                var lean = (new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .35f + up).normalized; var h = 2.2f + (k % 4) * .7f;
                mk.Prism(stem, p, lean, .07f, h, 5, .6f);
                soft.Blob(leaf, p + lean * h * .6f, new Vector3(.35f, .2f, .35f), ctx.Rng, .2f);
                mk.Blob(rose, p + lean * h + up * .1f, new Vector3(.3f, .24f, .3f), ctx.Rng, .15f);
            }
            ArtistBits.PostCollider(ctx.Root, c, 3f, 2.9f);
            halos.Add(c + up * 2.6f, 3.4f, new Color(1f, .2f, .35f), .45f);
            // petals circling slowly on the water round the thicket
            var (t, pm) = Mover(ctx, "Rose petals", new Vector3(c.x, Surface + .01f, c.z));
            var petal = SongGlow(id, new Color(1f, .3f, .4f), 1.2f);
            for (var k = 0; k < 40; k++) { var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(3.2f, 5.5f); pm.Box(petal, new Vector3(Mathf.Cos(a) * r, k * .0006f, Mathf.Sin(a) * r), new Vector3(.18f, .01f, .12f), Quaternion.Euler(0, ctx.Range(0, 360f), 0)); }
            pm.Build("Soft petals", t, false);
            life.movers.Add((t, t.position, 0f, 0f, 0f, 5f, id));
            Lit(ctx, c + up * 3.2f + face * 1.5f, new Color(1f, .3f, .4f), 2f);
        }

        // ---------------------------------------------------------------- Slowdive
        void GoldenRing(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); const float R = 5f; var mid = new Vector3(c.x, Surface + R - .6f, c.z);
            var gold = SongGlow(id, Gold, 2.2f);
            for (var k = 0; k < 28; k++)
            {
                var a = k * Mathf.PI * 2f / 28f; var p = mid + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * R;
                if (p.y < Surface - .3f) continue;
                var tang = (-side * Mathf.Sin(a) + up * Mathf.Cos(a)).normalized;
                mk.Box(gold, p, new Vector3(.5f, .5f, R * Mathf.PI * 2f / 28f + .05f), Quaternion.LookRotation(tang, face));
            }
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(ctx.Root, new Vector3(c.x, 0, c.z) + side * s * (R - .3f) + up * (waterY - FloorDrop), 2f, .5f);
            solid.Box(gold, new Vector3(c.x, waterY - FloorDrop + .5f, c.z), new Vector3(.6f, 1f, R * 1.8f), Quaternion.LookRotation(side));   // its low arc in the water
            mk.Prism(SongGlow(id, new Color(1f, .7f, .35f), .9f), new Vector3(c.x, Surface + .012f, c.z) + face * 1.5f, up, 3.2f, .02f, 18);
            Lit(ctx, mid, Gold, 4.2f);
        }

        void LanternChair(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            var plinthTop = Surface + .4f; var floor = waterY - FloorDrop;
            mk.Prism(stoneDark, new Vector3(c.x, floor, c.z), up, 2.4f, plinthTop - floor, 12);
            solid.Prism(stoneDark, new Vector3(c.x, floor, c.z), up, 2.4f, plinthTop - floor, 12);
            var step = new Vector3(c.x, floor + .21f, c.z) + face * 2.65f;
            mk.Box(stoneDark, step, new Vector3(1.4f, .42f, .6f), rot); solid.Box(stoneDark, step, new Vector3(1.4f, .42f, .6f), rot);
            var seat = new Vector3(c.x, plinthTop, c.z);
            mk.Box(stoneM, seat + up * .25f, new Vector3(1.1f, .5f, 1f), rot);
            mk.Box(stoneM, seat + up * 1.1f - face * .42f, new Vector3(1.1f, 1.7f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(stoneM, seat + up * .7f + Vector3.Cross(up, face) * s * .5f, new Vector3(.14f, .4f, .9f), rot);
            solid.Box(stoneM, seat + up * .9f, new Vector3(1.1f, 1.8f, 1f), rot);
            // paper lanterns circling slowly round the chair
            var (t, lm) = Mover(ctx, "Alison's lanterns", seat + up * 2.4f);
            var paper = SongGlow(id, new Color(1f, .55f, .7f), 2.2f);
            for (var k = 0; k < 9; k++)
            {
                var a = k * Mathf.PI * 2f / 9f; var p = new Vector3(Mathf.Cos(a), Mathf.Sin(k * 1.7f) * .35f, Mathf.Sin(a)) * 3.2f;
                lm.Prism(paper, p - up * .25f, up, .2f, .5f, 8, .75f);
                lm.Box(iron, p + up * .3f, new Vector3(.02f, .12f, .02f), Quaternion.identity);
            }
            lm.Build("Lanterns", t, false);
            life.movers.Add((t, t.position, .15f, .4f, 0f, 9f, id));
            Lit(ctx, seat + up * 2.6f, HotPink, 3.6f);
        }

        // ---------------------------------------------------------------- my bloody valentine
        void MoonCradle(LandContext ctx, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var at = new Vector3(c.x, Surface + .2f, c.z);
            var (t, mk) = Mover(ctx, "Moon cradle", at);
            var moon = SongGlow(id, new Color(.72f, .62f, 1f), 1f);
            const float R = 3.6f; const int n = 24;
            for (var k = 0; k < n; k++)
            {
                float a0 = Mathf.PI + k * Mathf.PI / n, a1 = Mathf.PI + (k + 1) * Mathf.PI / n, am = (a0 + a1) / 2f;
                var th = .3f + .7f * Mathf.Sin((k + .5f) * Mathf.PI / n);
                var p = (side * Mathf.Cos(am) + up * (Mathf.Sin(am) + 1f)) * R;
                var tang = (-side * Mathf.Sin(am) + up * Mathf.Cos(am)).normalized; var radial = (side * Mathf.Cos(am) + up * Mathf.Sin(am)).normalized;
                mk.Box(moon, p - radial * th * .3f, new Vector3(1.6f * th + .4f, .9f * th, R * Mathf.PI / n + .06f), Quaternion.LookRotation(tang, -radial));
            }
            mk.Box(cloth, up * .55f + side * -.9f, new Vector3(1.2f, .45f, 1.6f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, -12f));
            mk.Box(WorldMaterials.Solid(new Color(.55f, .45f, .7f)), up * .5f + side * .7f, new Vector3(2.4f, .2f, 2f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, 6f));
            mk.Build("Moon", t, false);
            life.movers.Add((t, at, .05f, .35f, 5f, 0f, id));
            solid.Box(cloth, at + up * .4f, new Vector3(2.2f, 1f, 7.6f), Quaternion.LookRotation(side));
            Lit(ctx, at + up * 3f + face * 1f, Lilac, 3.6f);
        }

        void ShallowPools(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var floor = waterY - FloorDrop;
            float[] rs = { 4.2f, 3f, 1.8f }; float[] tops = { Surface + .3f, Surface + .75f, Surface + 1.2f };
            for (var k = 0; k < 3; k++)
            {
                var cc = new Vector3(c.x, floor, c.z) - face * k * .5f;
                mk.Prism(stoneM, cc, up, rs[k], tops[k] - floor, 16, 1f, k * 7f);
                solid.Prism(stoneM, cc, up, rs[k], tops[k] - floor - .1f, 16);
                mk.Prism(SongGlow(id, Color.Lerp(HotPink, Lilac, k * .4f), 1.6f), new Vector3(cc.x, tops[k] + .006f, cc.z), up, rs[k] - .35f, .02f, 16, 1f, k * 7f);
                halos.Add(new Vector3(cc.x, tops[k] + .6f, cc.z), rs[k] * .8f, HotPink, .3f);
            }
            Lit(ctx, new Vector3(c.x, Surface + 3f, c.z), HotPink, 3f);
        }
    }
}
