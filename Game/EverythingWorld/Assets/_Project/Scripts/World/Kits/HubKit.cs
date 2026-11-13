using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Everything Hub (WORLD_GUIDE.md): a grand crossroads and a trophy hall for the whole playlist.
    /// Centred on its radio mast (the fast-travel tower), raised on a dais carved with the playlist's numbers
    /// and the names of the people who built it. Round it, curved stone stands face the mast; past them a
    /// formal park of planted trees and stone statues of music notation. Round everything, a great ring of
    /// every album cover, in the order the songs were added, turns slowly. A jukebox shows what's on and
    /// where it comes from; a signpost and a map board point out to every land; a single bench sits at the
    /// water's edge for anyone who needs it. HubLife adds the clouds, gulls, song-coloured sunset and fireworks.
    public partial class HubKit : LandKit
    {
        public override string Land => "The Everything Hub";

        // the plan, outward from the mast (metres)
        const float DaisR = 12f, FeatureR = 21f, StandsInner = 30f, LampR = 46f, PlazaR = 50f;
        const float TreeRowA = 58f, TreeRowB = 68f, StatueR = 78f, TreeRowC = 90f, RingR = 104f, LevelR = 114f, BlendR = 134f;

        static readonly Color Paving = new Color(.78f, .74f, .66f), Grass = new Color(.5f, .6f, .38f), GrassB = new Color(.56f, .64f, .41f);
        static readonly Color Warm = new Color(1f, .78f, .45f), Ink = new Color(.95f, .9f, .78f);
        /// Each person's own frame colour, chosen by them (by the display name shown on plaques).
        static readonly Dictionary<string, Color> PersonColor = new Dictionary<string, Color>
        {
            { "npgallagher", new Color(.05f, .45f, 1f) },     // electric blue
            { "Pyrosolstice", new Color(.13f, .55f, .2f) },   // forest green
            { "Hot Sause", new Color(.98f, .3f, .4f) },       // watermelon red
        };
        /// Colours for anyone new, until they pick their own.
        static readonly Color[] SpareColor = { new Color(.75f, .45f, 1f), new Color(1f, .55f, .15f), new Color(.2f, .9f, .85f), new Color(.85f, .85f, .85f) };
        static Color ColorFor(string[] adders, int index)
        {
            if (index < 0 || adders == null || index >= adders.Length) return new Color(.7f, .7f, .7f);
            if (PersonColor.TryGetValue(adders[index], out var c)) return c;
            var spare = 0;
            for (var i = 0; i < index; i++) if (!PersonColor.ContainsKey(adders[i])) spare++;
            return SpareColor[spare % SpareColor.Length];
        }
        /// The now-playing marker: a bright, pulsing electric yellow no one's frames use.
        static readonly Color NowPlaying = new Color(1f, .92f, .05f);

        Material stone, stoneDark, marble, bronze, wood, iron, board;
        Vector3 center;      // the mast, on the ground
        float groundY;
        readonly List<Vector3> openings = new List<Vector3>();
        readonly List<Vector3> statueSpots = new List<Vector3>();   // directions kept clear: causeways and the quiet bench

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            color = slope > 6f ? new Color(.6f, .58f, .5f) : patch > .55f ? GrassB : Grass;
            return true;
        }

        public override void ShapeGround(IslandWorldData data, int index)
        {
            // level ground for the plaza, the park and under the ring, easing back into the island's own shape
            var t = MastOf(data);
            if (t == null) return;
            var c = new Vector2(t.x, t.z);
            var level = Mathf.Max(2f, data.HeightAt(c.x, c.y));
            float step = data.Step;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * step;
                if (Mathf.Abs(z - c.y) > BlendR) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * step;
                    if (Mathf.Abs(x - c.x) > BlendR) continue;
                    var i = r * data.Nx + col;
                    if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < LevelR) data.heights[i] = level;
                    else if (d < BlendR) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - LevelR) / (BlendR - LevelR)));
                }
            }
        }

        static IslandWorldData.Tower MastOf(IslandWorldData data)
        {
            foreach (var t in data.layout.towers) if (t.sceneId == 1000) return t;
            return null;
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist artist) { }   // no artists live here

        public override void Dress(LandContext ctx)
        {
            stone = WorldMaterials.Solid(new Color(.82f, .78f, .7f));
            stoneDark = WorldMaterials.Solid(new Color(.55f, .52f, .48f));
            marble = WorldMaterials.Solid(new Color(.93f, .91f, .87f), .2f);
            bronze = WorldMaterials.Solid(new Color(.72f, .52f, .26f), .35f);
            wood = WorldMaterials.Solid(new Color(.45f, .32f, .22f));
            iron = WorldMaterials.Solid(new Color(.14f, .14f, .15f));
            board = WorldMaterials.Solid(new Color(.12f, .1f, .1f));
            var mast = MastOf(ctx.Data);
            if (mast == null) { Debug.LogWarning("The Everything Hub: no mast found"); return; }
            center = new Vector3(mast.x, 0, mast.z);
            groundY = ctx.Ground(center.x, center.z);
            center.y = groundY;
            openings.Clear(); statueSpots.Clear();
            foreach (var b in ctx.Data.layout.bridges)
            {
                if (b.a == 1000) openings.Add(new Vector3(b.ax - center.x, 0, b.az - center.z).normalized);
                if (b.b == 1000) openings.Add(new Vector3(b.bx - center.x, 0, b.bz - center.z).normalized);
            }
            var quietDir = QuietDirection();
            openings.Add(quietDir);

            var mk = new MeshKit(); var solid = new MeshKit(); var halos = new GlowHalos();
            var labels = new GameObject("Hub words").transform; labels.SetParent(ctx.Root, false);
            life = new GameObject("Hub life").AddComponent<HubLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.ringRadius = RingR;
            people = new GameObject("Hub people").transform; people.SetParent(life.transform, false); prng = new System.Random(1856);
            Plaza(ctx, mk, halos);
            Dais(ctx, mk, solid, labels, halos);
            Stands(ctx, mk, solid, halos);
            Paths(ctx, mk);
            Jukebox(ctx, mk, solid, labels, halos);
            Signpost(ctx, mk, labels);
            MapBoard(ctx, mk, solid, labels, halos);
            Park(ctx, mk, solid, labels);
            QuietBench(ctx, mk, solid, quietDir, halos);
            Board(ctx, mk, solid, labels, halos);
            Flags(ctx, mk, labels, halos);
            Lights(ctx, mk, halos);
            People(ctx, mk, solid);
            mk.Build("Hub", ctx.Root, false);
            solid.BuildCollider("Hub walls", ctx.Root);
            CoverRing(ctx);
            GroundCover(ctx);
            halos.Build("Hub glow", ctx.Root);
        }

        // ---------------------------------------------------------------- ground cover
        /// A short, kept lawn through the park, golden-green, with the odd daisy; past the ring of covers the
        /// grass is left to grow into a wild golden meadow with buttercups, down to the shore. Off the paths.
        void GroundCover(LandContext ctx)
        {
            var lawnMat = WindMesh.Material(.16f, .8f, Grass);
            var meadowMat = WindMesh.Material(.42f, 1f, Grass);
            var lawn = new Dictionary<(int, int), WindMesh>(); var meadow = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(1717);
            const float step = 1.5f, reach = 260f;
            var count = 0;
            for (var x = center.x - reach; x < center.x + reach; x += step)
            for (var z = center.z - reach; z < center.z + reach; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                var off = new Vector3(px - center.x, 0, pz - center.z); var r = off.magnitude;
                if (r < PlazaR + 1f || r > reach) continue;
                var wild = r > RingR - 2f;
                if (wild && rng.NextDouble() < .45) continue;                     // the meadow is a little sparser
                if (!ctx.IsLand(px, pz, 2f)) continue;
                if (NearOpening(off / r, ClearAngle(r, 3.6f))) continue;          // keep the paths clear
                var nearStatue = false;
                if (Mathf.Abs(r - StatueR) < 3.5f) foreach (var s in statueSpots) if ((new Vector2(s.x, s.z) - new Vector2(px, pz)).sqrMagnitude < 9f) { nearStatue = true; break; }
                if (nearStatue) continue;
                var g = ctx.Ground(px, pz);
                if (wild && Mathf.PerlinNoise(px * .02f + 3f, pz * .02f + 8f) < .32f) continue;
                var key = (Mathf.FloorToInt(px / 60f), Mathf.FloorToInt(pz / 60f));
                var set = wild ? meadow : lawn;
                if (!set.TryGetValue(key, out var wm)) set[key] = wm = new WindMesh();
                var at = new Vector3(px, g - .03f, pz);
                if (wild)
                {
                    var col = Color.Lerp(new Color(.6f, .62f, .36f), new Color(.82f, .72f, .42f), (float)rng.NextDouble());
                    wm.Tuft(at, .55f + (float)rng.NextDouble() * .55f, col, rng);
                    if (rng.NextDouble() < .05) wm.Flower(at + new Vector3(.25f, 0, .2f), .5f, rng.NextDouble() < .6 ? new Color(.98f, .85f, .2f) : new Color(.97f, .96f, .9f), rng);
                }
                else
                {
                    var col = Color.Lerp(new Color(.5f, .6f, .33f), new Color(.66f, .66f, .38f), (float)rng.NextDouble());
                    wm.Tuft(at, .25f + (float)rng.NextDouble() * .2f, col, Color.Lerp(col, new Color(.86f, .82f, .55f), .45f), rng, 3, .07f);
                    if (rng.NextDouble() < .025) wm.Flower(at + new Vector3(.2f, 0, .15f), .22f, rng.NextDouble() < .7 ? new Color(.97f, .96f, .9f) : new Color(.98f, .85f, .2f), rng);
                }
                count++;
            }
            foreach (var kv in lawn) kv.Value.Build("Lawn", ctx.Root, lawnMat);
            foreach (var kv in meadow) kv.Value.Build("Meadow", ctx.Root, meadowMat);
            Debug.Log($"The Everything Hub: {count} tufts of lawn and meadow");
        }

        // ---------------------------------------------------------------- the plaza and the dais
        void Plaza(LandContext ctx, MeshKit mk, GlowHalos halos)
        {
            var paving = WorldMaterials.Solid(Paving);
            var inlay = WorldMaterials.Solid(new Color(.66f, .6f, .5f));
            const int spokes = 48;
            for (var ring = 0; ring < 5; ring++)
            for (var s = 0; s < spokes; s++)
            {
                float r0 = ring * PlazaR / 5f, r1 = (ring + 1) * PlazaR / 5f;
                float a0 = s * Mathf.PI * 2f / spokes, a1 = (s + 1) * Mathf.PI * 2f / spokes;
                Vector3 P(float r, float a) => new Vector3(center.x + Mathf.Cos(a) * r, groundY + .04f, center.z + Mathf.Sin(a) * r);
                var m = (ring % 2 == 0) == (s % 2 == 0) ? paving : inlay;     // a faint chequer, like laid stone
                if (ring == 0) mk.Tri(m, P(0, 0), P(r1, a0), P(r1, a1), center - Vector3.up * 5f);
                else mk.Quad(m, P(r0, a0), P(r0, a1), P(r1, a1), P(r1, a0), center - Vector3.up * 5f);
            }
            // lanterns on tall posts round the plaza
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI * 2f / 16f + .1f;
                var p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * LampR;
                if (NearOpening(p - center, 8f)) continue;
                p.y = ctx.Ground(p.x, p.z);
                mk.Prism(iron, p, Vector3.up, .12f, 4.2f, 6);
                ArtistBits.PostCollider(ctx.Root, p, 4.2f);
                var lamp = p + Vector3.up * 4.4f;
                mk.Blob(WorldMaterials.Glow(Warm, 2f), lamp, new Vector3(.35f, .45f, .35f), ctx.Rng, .05f);
                halos.Add(lamp, 2f, Warm);
                ArtistBits.PointLight(ctx.Root, lamp, Warm, 14f, 1.4f);
            }
        }

        void Dais(LandContext ctx, MeshKit mk, MeshKit solid, Transform labels, GlowHalos halos)
        {
            const float h = 1.5f;
            mk.Prism(stone, center - Vector3.up * .5f, Vector3.up, DaisR, h + .5f, 32);
            solid.Prism(stone, center - Vector3.up * .5f, Vector3.up, DaisR, h + .5f, 32);
            mk.Prism(stoneDark, center + Vector3.up * (h - .05f), Vector3.up, DaisR - .8f, .1f, 32);
            // four ramps up onto the dais
            for (var k = 0; k < 4; k++)
            {
                var dir = Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward;
                var near = center + dir * (DaisR - .3f) + Vector3.up * h;
                var far = center + dir * (DaisR + 5f); far.y = groundY;
                var rot = Quaternion.LookRotation(near - far);
                var size = new Vector3(3.4f, .25f, Vector3.Distance(near, far) + .4f);
                mk.Box(stone, (near + far) / 2f, size, rot);
                solid.Box(stone, (near + far) / 2f, size, rot);
            }
            // a bronze ring set into the dais, glowing softly
            for (var k = 0; k < 64; k++)
            {
                var a = k * Mathf.PI * 2f / 64f;
                var p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (DaisR - .4f) + Vector3.up * (h + .02f);
                mk.Box(WorldMaterials.Glow(Warm, .8f), p, new Vector3(.5f, .05f, .5f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
            }
            // three standing tablets: the playlist in numbers, who built it (in their frame colours), and the ring
            var data = ctx.Data.layout;
            var adders = data.hubAdders ?? new string[0];
            for (var k = 0; k < 3; k++)
            {
                var dir = Quaternion.Euler(0, k * 120f, 0) * Vector3.forward;
                var at = center + dir * (DaisR + 3.5f); at.y = groundY;
                var face = dir;
                var rot = Quaternion.LookRotation(face);
                mk.Box(stoneDark, at + Vector3.up * 1.6f, new Vector3(3.6f, 3.2f, .5f), rot);
                mk.Box(stone, at + Vector3.up * 3.35f, new Vector3(3.9f, .3f, .7f), rot);
                solid.Box(stone, at + Vector3.up * 1.6f, new Vector3(3.6f, 3.2f, .6f), rot);
                var front = at + face * .27f;
                if (k == 0)
                {
                    ArtistBits.Label(labels, "THE EVERYTHING PLAYLIST", front + Vector3.up * 2.85f, face, .26f, Ink);
                    ArtistBits.Label(labels, $"{data.hubSongs:N0} songs\n{data.hubArtists:N0} artists\nsince {data.hubSince}", front + Vector3.up * 2.35f, face, .22f, Ink);
                }
                else if (k == 1)
                {
                    ArtistBits.Label(labels, "BUILT BY", front + Vector3.up * 2.85f, face, .26f, Ink);
                    for (var i = 0; i < adders.Length && i < 6; i++)
                        ArtistBits.Label(labels, adders[i], front + Vector3.up * (2.35f - i * .34f), face, .24f, ColorFor(adders, i));
                }
                else
                {
                    ArtistBits.Label(labels, "EVERY SONG, IN ORDER", front + Vector3.up * 2.85f, face, .26f, Ink);
                    ArtistBits.Label(labels, "the ring round the hub holds\nevery cover, first to newest.\nthe frame colour shows\nwho added it.", front + Vector3.up * 2.35f, face, .17f, Ink);
                }
                halos.Add(at + face * 2f + Vector3.up * 3.6f, 1.6f, Warm, .6f);
                ArtistBits.PointLight(ctx.Root, at + face * 2.5f + Vector3.up * 3.5f, Warm, 7f, 1.2f);
            }
        }

        // ---------------------------------------------------------------- the stands
        void Stands(LandContext ctx, MeshKit mk, MeshKit solid, GlowHalos halos)
        {
            const float tierDepth = 1.6f, tierRise = .32f;   // tiers you can step up and sit on
            const int tiers = 7, segments = 72;
            var stoneB = WorldMaterials.Solid(new Color(.76f, .72f, .64f));
            for (var s = 0; s < segments; s++)
            {
                float a0 = s * Mathf.PI * 2f / segments, a1 = (s + 1) * Mathf.PI * 2f / segments, am = (a0 + a1) / 2f;
                var dir = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am));
                if (NearOpening(dir, 10f)) continue;       // gaps where the paths and the quiet bench lie
                var rot = Quaternion.LookRotation(-dir);
                for (var t = 0; t < tiers; t++)
                {
                    var rr = StandsInner + (t + .5f) * tierDepth;
                    var h = (t + 1) * tierRise;
                    var c = center + dir * rr; c.y = groundY + h / 2f;
                    var size = new Vector3(rr * (a1 - a0) + .05f, h, tierDepth);
                    mk.Box(t % 2 == 0 ? stone : stoneB, c, size, rot);
                    solid.Box(stone, c, size, rot);
                }
                if (s % 6 == 0)                               // little lamps along the top tier
                {
                    var lamp = center + dir * (StandsInner + tiers * tierDepth - .4f) + Vector3.up * (tiers * tierRise + .7f);
                    mk.Box(WorldMaterials.Glow(Warm, 1.6f), lamp, new Vector3(.25f, .35f, .25f), rot);
                    halos.Add(lamp, 1.2f, Warm, .8f);
                }
            }
        }

        bool NearOpening(Vector3 dir, float degrees)
        {
            dir.y = 0; dir.Normalize();
            foreach (var o in openings) if (Vector3.Angle(dir, o) < degrees) return true;
            return false;
        }

        /// Degrees either side of a path to keep clear at this distance from the mast.
        static float ClearAngle(float r, float halfWidth) => Mathf.Atan2(halfWidth, r) * Mathf.Rad2Deg;

        /// The direction for the quiet bench: as far as possible from the causeways and the mast's door.
        Vector3 QuietDirection()
        {
            var best = Vector3.forward; var bestGap = -1f;
            for (var k = 0; k < 36; k++)
            {
                var d = Quaternion.Euler(0, k * 10f, 0) * Vector3.forward;
                var gap = 180f;
                foreach (var o in openings) gap = Mathf.Min(gap, Vector3.Angle(d, o));
                gap = Mathf.Min(gap, Vector3.Angle(d, Vector3.back) + 20f);   // the tower's door faces south
                if (gap > bestGap) { bestGap = gap; best = d; }
            }
            return best;
        }

        void Paths(LandContext ctx, MeshKit mk)
        {
            var paving = WorldMaterials.Solid(new Color(.74f, .7f, .62f));
            foreach (var o in openings)
            {
                var pts = new List<Vector3>();
                for (var d = PlazaR; d < 400f; d += 4f)
                {
                    var p = center + o * d;
                    if (!ctx.IsLand(p.x, p.z, 0f)) break;
                    pts.Add(ctx.OnGround(p.x, p.z));
                }
                for (var i = 0; i + 1 < pts.Count; i++)
                {
                    var side = Vector3.Cross(Vector3.up, o) * 2.4f;
                    Vector3 a = pts[i] - side, b = pts[i] + side, c = pts[i + 1] + side, e = pts[i + 1] - side;
                    a.y = ctx.Ground(a.x, a.z) + .08f; b.y = ctx.Ground(b.x, b.z) + .08f; c.y = ctx.Ground(c.x, c.z) + .08f; e.y = ctx.Ground(e.x, e.z) + .08f;
                    mk.Quad(paving, a, b, c, e, (a + c) / 2f - Vector3.up);
                }
            }
        }

        // ---------------------------------------------------------------- the jukebox
        /// A classic arched jukebox: a wooden cabinet with a rounded top, rainbow light tubes round the arch,
        /// a window showing the cover and the song, a speaker grille. Beside it, a sign on a post turns to point
        /// at the land the song comes from, with that land's name written on the arrow.
        void Jukebox(LandContext ctx, MeshKit mk, MeshKit solid, Transform labels, GlowHalos halos)
        {
            var dir = Quaternion.Euler(0, 60f, 0) * Vector3.forward;
            var at = center + dir * FeatureR; at.y = groundY;
            var face = -dir;                                         // faces the mast
            var rot = Quaternion.LookRotation(face);
            var right = Vector3.Cross(face, Vector3.up).normalized;  // the viewer's right
            var cabinet = WorldMaterials.Solid(new Color(.42f, .2f, .12f), .45f);
            const float W = 3.6f, D = 1.4f, bodyH = 3f, archR = 1.8f;
            var up = Vector3.up;
            mk.Box(cabinet, at + up * bodyH / 2f, new Vector3(W, bodyH, D), rot);
            mk.Prism(cabinet, at + up * bodyH - face * D / 2f, face, archR, D, 24);
            solid.Box(cabinet, at + up * (bodyH + archR) / 2f, new Vector3(W, bodyH + archR, D), rot);
            mk.Box(bronze, at + up * .08f, new Vector3(W + .2f, .16f, D + .2f), rot);   // plinth
            // rainbow tubes round the arch and down both sides
            Color[] tube = { new Color(1f, .25f, .2f), new Color(1f, .55f, .15f), new Color(1f, .85f, .2f), new Color(.35f, .95f, .35f), new Color(.25f, .65f, 1f), new Color(.6f, .35f, 1f), new Color(1f, .35f, .75f) };
            var front = face * (D / 2f + .06f);
            Vector3 ArchPoint(float a) => at + up * bodyH + right * Mathf.Cos(a) * (archR - .15f) + up * Mathf.Sin(a) * (archR - .15f) + front;
            const int arcSegs = 21;
            for (var k = 0; k < arcSegs; k++)
            {
                Vector3 p0 = ArchPoint(Mathf.PI * k / arcSegs), p1 = ArchPoint(Mathf.PI * (k + 1) / arcSegs);
                var seg = p1 - p0;
                mk.Box(WorldMaterials.Glow(tube[k * tube.Length / arcSegs], 2.2f), (p0 + p1) / 2f, new Vector3(.18f, seg.magnitude + .05f, .12f), Quaternion.LookRotation(face, seg.normalized));
            }
            foreach (var s in new[] { -1f, 1f })
            {
                var x = right * s * (archR - .15f);
                var col = s < 0 ? tube[tube.Length - 1] : tube[0];
                mk.Box(WorldMaterials.Glow(col, 2.2f), at + x + up * (bodyH + .25f) / 2f + front, new Vector3(.18f, bodyH - .25f, .12f), rot);
            }
            // the window: dark glass with the cover and the words
            mk.Box(board, at + up * 2.45f + face * (D / 2f + .01f), new Vector3(2.7f, 2.9f, .04f), rot);
            // the speaker grille: bronze slats
            for (var k = 0; k < 4; k++) mk.Box(bronze, at + up * (.4f + k * .16f) + face * (D / 2f + .02f), new Vector3(2.4f, .06f, .04f), rot);
            halos.Add(at + face * 1.5f + up * 3.2f, 3.2f, new Color(1f, .6f, .5f), .7f);
            ArtistBits.PointLight(ctx.Root, at + face * 2.5f + up * 2.8f, Warm, 10f, 1.4f);

            var root = new GameObject("Jukebox").transform; root.SetParent(ctx.Root, false);
            var box = root.gameObject.AddComponent<Jukebox>(); jukebox = box;
            var coverGo = new GameObject("Cover");
            coverGo.transform.SetParent(root, false);
            var mesh = new Mesh { name = "Jukebox cover" };
            const float half = .8f;
            var c = at + up * 2.95f + face * (D / 2f + .04f);
            mesh.SetVertices(new List<Vector3> { c - right * half - up * half, c + right * half - up * half, c + right * half + up * half, c - right * half + up * half });
            mesh.SetUVs(0, new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero });
            mesh.SetTriangles(Vector3.Dot(Vector3.Cross(right, up), face) > 0 ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateNormals();
            coverGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            coverGo.AddComponent<MeshRenderer>().sharedMaterial = WorldMaterials.Picture(ctx.CoverAtlas);
            box.cover = coverGo.GetComponent<MeshFilter>();
            box.coverRect = ctx.CoverRect;
            var glass = at + face * (D / 2f + .06f);
            ArtistBits.Label(labels, "NOW PLAYING", glass + up * (bodyH + .95f), face, .2f, Warm);
            box.title = ArtistBits.Label(labels, "", glass + up * 2.0f, face, .2f, Ink);
            box.detail = ArtistBits.Label(labels, "", glass + up * 1.68f, face, .15f, Ink);

            // the direction sign beside it
            var post = at - right * 3.6f + face * .4f; post.y = ctx.Ground(post.x, post.z);
            mk.Prism(wood, post, up, .16f, 5.2f, 8);
            mk.Blob(bronze, post + up * 5.3f, new Vector3(.25f, .25f, .25f), ctx.Rng, 0f);
            ArtistBits.PostCollider(ctx.Root, post, 5.2f);
            var pointer = new GameObject("Points to the song's land").transform;
            pointer.SetParent(root, false);
            pointer.SetPositionAndRotation(post + up * 4.7f, Quaternion.identity);
            var pmk = new MeshKit();
            var arrowWood = WorldMaterials.Solid(new Color(.86f, .74f, .5f));
            const float len = 3.8f, hh = .45f, th = .07f, tip = 1f, startZ = -.4f;
            // shaft: a board along +z; head: a flat triangle at the end (both built round the pointer's origin)
            pmk.Box(arrowWood, new Vector3(0, 0, startZ + len / 2f), new Vector3(th * 2f, hh * 2f, len), Quaternion.identity);
            Vector3 tp = new Vector3(0, 0, startZ + len + tip), tu = new Vector3(0, hh * 1.9f, startZ + len), td = new Vector3(0, -hh * 1.9f, startZ + len);
            var inside = new Vector3(0, 0, startZ + len + .3f);
            var xo = new Vector3(th, 0, 0);
            pmk.Tri(arrowWood, tp + xo, tu + xo, td + xo, inside);
            pmk.Tri(arrowWood, tp - xo, tu - xo, td - xo, inside);
            pmk.Quad(arrowWood, tp + xo, tu + xo, tu - xo, tp - xo, inside);
            pmk.Quad(arrowWood, tp + xo, td + xo, td - xo, tp - xo, inside);
            pmk.Quad(arrowWood, tu + xo, td + xo, td - xo, tu - xo, inside);
            var arrow = pmk.Build("Arrow", pointer, false);
            arrow.isStatic = false;
            arrow.transform.localPosition = Vector3.zero; arrow.transform.localRotation = Quaternion.identity;
            var words = new List<TextMesh>();
            foreach (var s in new[] { 1f, -1f })
            {
                var side = Vector3.right * s;
                var t = ArtistBits.Label(pointer, "", pointer.position + side * (th + .02f) + Vector3.forward * (startZ + len / 2f) + Vector3.up * .3f, side, .26f, new Color(.18f, .12f, .08f));
                words.Add(t);
            }
            box.sign = words.ToArray();
            box.pointer = pointer;
            foreach (var s in ctx.Data.layout.hubTimeline ?? new IslandWorldData.TimelineSong[0]) if (s.c >= 0) box.coverOf[s.id] = s.c;
            var islandName = new Dictionary<int, string>();
            foreach (var isl in ctx.Data.layout.islands) islandName[isl.sceneId] = isl.name;
            foreach (var a in ctx.Data.layout.artists)
                if (islandName.TryGetValue(a.sceneId, out var land)) box.homeOf[a.name] = (land, new Vector3(a.x, 0, a.z));
        }

        // ---------------------------------------------------------------- signpost and map board
        void Signpost(LandContext ctx, MeshKit mk, Transform labels)
        {
            var dir = Quaternion.Euler(0, 180f, 0) * Vector3.forward;
            var at = center + dir * FeatureR; at.y = groundY;
            mk.Prism(wood, at, Vector3.up, .22f, 7.5f, 8);
            ArtistBits.PostCollider(ctx.Root, at, 7.5f, .3f);
            var k = 0;
            foreach (var t in ctx.Data.layout.towers)
            {
                if (t.sceneId == 1000) continue;
                var to = new Vector3(t.x - at.x, 0, t.z - at.z);
                var km = to.magnitude / 1000f;
                var d = to.normalized;
                var y = 6.8f - k * .62f;
                var rot = Quaternion.LookRotation(d);
                var mid = at + Vector3.up * y + d * 1.4f;
                mk.Box(wood, mid, new Vector3(.08f, .45f, 2.6f), rot);
                var side = Vector3.Cross(Vector3.up, d);
                ArtistBits.Label(labels, $"{t.realm}  {km:0.0} km", mid + side * .06f + Vector3.up * .12f, side, .13f, Ink, TextAnchor.UpperCenter);
                ArtistBits.Label(labels, $"{t.realm}  {km:0.0} km", mid - side * .06f + Vector3.up * .12f, -side, .13f, Ink, TextAnchor.UpperCenter);
                k++;
            }
        }

        /// The world map on a big standing board, tilted back a little, facing the mast, north up.
        void MapBoard(LandContext ctx, MeshKit mk, MeshKit solid, Transform labels, GlowHalos halos)
        {
            var dir = Quaternion.Euler(0, 300f, 0) * Vector3.forward;
            var at = center + dir * FeatureR; at.y = groundY;
            var lay = ctx.Data.layout;
            const float w = 9.5f, bottom = .9f, tilt = 12f * Mathf.Deg2Rad;
            var h = w * lay.depth / lay.width;
            var face = -dir;
            var right = Vector3.Cross(face, Vector3.up).normalized;
            var up = Vector3.up * Mathf.Cos(tilt) - face * Mathf.Sin(tilt);      // the top leans away from you
            var normal = face * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt);
            var c = at + Vector3.up * bottom + up * (h / 2f);
            var rot = Quaternion.LookRotation(normal, up);
            // the frame and two legs
            mk.Box(wood, c - normal * .12f, new Vector3(w + .5f, h + .5f, .18f), rot);
            solid.Box(wood, c - normal * .12f, new Vector3(w + .5f, h + .5f, .3f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                // legs lean back with the board, just behind its frame, from the ground to near the top
                var leg = at + right * s * (w / 2f - .4f) - up * (bottom / Mathf.Cos(tilt)) + Vector3.up * bottom - normal * .42f;
                leg.y = groundY;
                mk.Prism(wood, leg, up, .14f, bottom / Mathf.Cos(tilt) + h * .92f, 6);
            }
            var top = new MeshKit();
            top.Picture(WorldMaterials.Picture(PaintMap(ctx.Data)), c, right * w / 2f, up * h / 2f, normal, new Rect(0, 0, 1, 1));
            top.Build("Map", ctx.Root, false);
            // names: every land small and dark, every realm big and red-brown over its lands
            Vector3 OnMap(float x, float z, float lift) => c + right * ((x / lay.width - .5f) * w) + up * ((z / lay.depth - .5f) * h) + normal * lift;
            var realms = new Dictionary<string, (Vector2 sum, int n)>();
            foreach (var isl in lay.islands)
            {
                if (isl.sceneId == 1000) continue;
                ArtistBits.Label(labels, MapName(isl.name), OnMap(isl.labelX, isl.labelZ, .03f) + up * .04f, normal, .075f, new Color(.16f, .12f, .09f), TextAnchor.MiddleCenter);
                if (string.IsNullOrEmpty(isl.realm)) continue;
                realms.TryGetValue(isl.realm, out var acc);
                realms[isl.realm] = (acc.sum + new Vector2(isl.labelX, isl.labelZ), acc.n + 1);
            }
            foreach (var kv in realms)
            {
                var m = kv.Value.sum / kv.Value.n;
                ArtistBits.Label(labels, string.Join(" ", kv.Key.ToUpperInvariant().ToCharArray()), OnMap(m.x, m.y, .045f) + up * .32f, normal, .17f, new Color(.55f, .12f, .08f, .85f), TextAnchor.MiddleCenter);
            }
            ArtistBits.Label(labels, "THE EVERYTHING WORLD", c + up * (h / 2f + .55f) + normal * .05f, normal, .32f, Ink);
            // you are here
            var here = c + right * ((center.x / lay.width - .5f) * w) + up * ((center.z / lay.depth - .5f) * h) + normal * .04f;
            var red = new Color(1f, .25f, .2f);
            mk.Prism(WorldMaterials.Glow(red, 3f), here - normal * .03f, normal, .16f, .08f, 12);
            halos.Add(here + normal * .1f, .35f, red, 1.4f);
            ArtistBits.Label(labels, "you are here", here + up * .5f + normal * .02f, normal, .14f, red);
            ArtistBits.PointLight(ctx.Root, c + normal * 3f + Vector3.up * 1.5f, Warm, 8f, 1f);
        }

        /// Land names on the map drop "The" and break long names over two lines.
        static string MapName(string name)
        {
            if (name.StartsWith("The ")) name = name.Substring(4);
            if (name.Length <= 16) return name;
            var mid = name.Length / 2; var cut = -1;
            for (var d = 0; d < mid; d++)
            {
                if (mid + d < name.Length && name[mid + d] == ' ') { cut = mid + d; break; }
                if (name[mid - d] == ' ') { cut = mid - d; break; }
            }
            return cut < 0 ? name : name.Substring(0, cut) + "\n" + name.Substring(cut + 1);
        }

        /// A clean painted map from the world itself: each land in its own colour with a little hill shading,
        /// dark lines where one land meets another, a darker coastline and soft blue sea. North is up.
        static Texture2D PaintMap(IslandWorldData data)
        {
            const int k = 2;                                   // one map pixel per 2 x 2 world cells
            int nx = data.Nx, nz = data.Nz, w = nx / k, h = nz / k;
            var px = new Color32[w * h];
            var isl = data.layout.islands;
            var colors = new Color[isl.Length];
            for (var i = 0; i < isl.Length; i++)
            {
                var t = isl[i].tintA;
                var c = t != null && t.Length >= 3 ? new Color(t[0] / 255f, t[1] / 255f, t[2] / 255f) : new Color(.7f, .65f, .5f);
                colors[i] = Color.Lerp(c, new Color(.93f, .87f, .72f), .35f);   // softened toward parchment
            }
            var sea = new Color(.6f, .74f, .8f); var deep = new Color(.48f, .62f, .72f);
            var coast = new Color(.28f, .2f, .14f); var border = new Color(.3f, .22f, .16f);
            byte Id(int col, int row) => data.islandIds[Mathf.Clamp(row, 0, nz - 1) * nx + Mathf.Clamp(col, 0, nx - 1)];
            float H(int col, int row) => data.heights[Mathf.Clamp(row, 0, nz - 1) * nx + Mathf.Clamp(col, 0, nx - 1)];
            for (var y = 0; y < h; y++)
            {
                var row = nz - 1 - y * k;                       // texture rows go up from the south; grid rows go down from the north
                for (var x = 0; x < w; x++)
                {
                    var col = x * k;
                    var id = Id(col, row);
                    Color c;
                    if (id == 255)
                    {
                        // sea, a touch lighter in the shallows near a coast
                        var nearLand = Id(col + 3, row) != 255 || Id(col - 3, row) != 255 || Id(col, row + 3) != 255 || Id(col, row - 3) != 255;
                        c = nearLand ? Color.Lerp(sea, Color.white, .15f) : Color.Lerp(sea, deep, Mathf.PerlinNoise(x * .02f, y * .02f));
                    }
                    else
                    {
                        c = id < colors.Length ? colors[id] : Color.grey;
                        var shade = 1f + Mathf.Clamp((H(col - k, row - k) - H(col + k, row + k)) * .03f, -.18f, .18f);   // light from the north-west
                        c *= shade;
                        byte r1 = Id(col + k, row), r2 = Id(col, row - k), r3 = Id(col - k, row), r4 = Id(col, row + k);
                        if (r1 == 255 || r2 == 255 || r3 == 255 || r4 == 255) c = coast;
                        else if (r1 != id || r2 != id) c = Color.Lerp(c, border, .75f);
                    }
                    c.a = 1f;
                    px[y * w + x] = c;
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Hub map", wrapMode = TextureWrapMode.Clamp, anisoLevel = 4 };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        // ---------------------------------------------------------------- the park
        /// A formal park past the stands: rows of planted trees (round, clipped cones, round again) with an
        /// avenue of stone statues of music notation between them.
        void Park(LandContext ctx, MeshKit mk, MeshKit solid, Transform labels)
        {
            var leafA = WorldMaterials.Solid(new Color(.33f, .5f, .28f));
            var leafB = WorldMaterials.Solid(new Color(.4f, .56f, .3f));
            var yew = WorldMaterials.Solid(new Color(.2f, .38f, .27f));
            foreach (var (r, cone) in new[] { (TreeRowA, false), (TreeRowB, true), (TreeRowC, false) })
            {
                var n = Mathf.RoundToInt(2f * Mathf.PI * r / 9f);
                for (var k = 0; k < n; k++)
                {
                    var a = (k + (cone ? .5f : 0f)) * Mathf.PI * 2f / n;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    if (NearOpening(dir, ClearAngle(r, 7f))) continue;
                    var p = center + dir * r;
                    if (!ctx.IsLand(p.x, p.z, 4f)) continue;
                    p.y = ctx.Ground(p.x, p.z);
                    if (cone)
                    {
                        mk.Prism(wood, p, Vector3.up, .2f, 1.2f, 6);
                        mk.Cone(yew, p + Vector3.up * 1f, 1.5f, 5.5f, 8);
                        ArtistBits.PostCollider(ctx.Root, p, 4f, 1.25f);     // the low, wide foliage
                    }
                    else
                    {
                        mk.Prism(wood, p, Vector3.up, .22f, 2.8f, 6, .8f);
                        mk.Blob(k % 2 == 0 ? leafA : leafB, p + Vector3.up * 4.3f, new Vector3(2.1f, 2f, 2.1f), ctx.Rng, .07f);
                    }
                    ArtistBits.PostCollider(ctx.Root, p, 3f, .3f);
                }
            }
            // the statues, facing the mast
            var glyphs = Glyphs();
            var slot = 0;
            const int slots = 16;
            for (var k = 0; k < slots; k++)
            {
                var a = (k + .5f) * Mathf.PI * 2f / slots;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                if (NearOpening(dir, ClearAngle(StatueR, 9f))) continue;
                var p = center + dir * StatueR;
                if (!ctx.IsLand(p.x, p.z, 4f)) continue;
                p.y = ctx.Ground(p.x, p.z);
                Statue(mk, solid, labels, glyphs[slot++ % glyphs.Count], p, -dir);
                statueSpots.Add(p);
            }
        }

        class Glyph
        {
            public string name;
            public List<Vector2[]> strokes = new List<Vector2[]>();
            public List<float> widths = new List<float>();
            public List<Vector3> discs = new List<Vector3>();   // x, y, radius
            public List<Vector3> rings = new List<Vector3>();   // x, y, radius
            public float ringWidth = .2f;
            public Glyph Stroke(float width, bool smooth, params Vector2[] pts) { strokes.Add(smooth ? Smooth(pts) : pts); widths.Add(width); return this; }
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);

        /// Music notation drawn in a unit space about 3 tall, sitting on y = 0.
        static List<Glyph> Glyphs()
        {
            var list = new List<Glyph>();
            // treble clef: a spiral round the G line, up into a loop, then straight down to a curl and a dot
            var treble = new List<Vector2>();
            for (var k = 0; k <= 30; k++)
            {
                var t = k / 30f; var ang = -2.5f * Mathf.PI * (1f - t); var rr = .08f + .47f * t;
                treble.Add(V(Mathf.Cos(ang) * rr, 1.6f + Mathf.Sin(ang) * rr));
            }
            treble.AddRange(new[] { V(.52f, 2.05f), V(.35f, 2.5f), V(.1f, 2.9f), V(-.08f, 3.25f), V(-.06f, 3.58f), V(.1f, 3.68f), V(.2f, 3.45f), V(.12f, 3.05f), V(.04f, 2.5f), V(.02f, 1.6f), V(.02f, .7f), V(0f, .3f), V(-.15f, .12f), V(-.33f, .2f), V(-.34f, .35f) });
            var g = new Glyph { name = "Treble clef" }; g.strokes.Add(treble.ToArray()); g.widths.Add(.2f); g.discs.Add(new Vector3(-.22f, .32f, .14f)); list.Add(g);

            g = new Glyph { name = "Bass clef" }.Stroke(.22f, true, V(0, 2.6f), V(.15f, 2.85f), V(.45f, 2.95f), V(.75f, 2.8f), V(.9f, 2.5f), V(.85f, 2.1f), V(.6f, 1.7f), V(.25f, 1.4f), V(-.1f, 1.2f));
            g.discs.Add(new Vector3(0, 2.55f, .2f)); g.discs.Add(new Vector3(1.15f, 2.65f, .11f)); g.discs.Add(new Vector3(1.15f, 2.2f, .11f)); list.Add(g);

            g = new Glyph { name = "Quarter note" }.Stroke(.16f, false, V(.38f, .5f), V(.38f, 3f)); g.discs.Add(new Vector3(0, .45f, .42f)); list.Add(g);

            g = new Glyph { name = "Eighth note" }.Stroke(.16f, false, V(.38f, .5f), V(.38f, 3f)).Stroke(.2f, true, V(.38f, 3f), V(.5f, 2.75f), V(.8f, 2.45f), V(.92f, 2.05f), V(.78f, 1.65f));
            g.discs.Add(new Vector3(0, .45f, .42f)); list.Add(g);

            g = new Glyph { name = "Beamed eighth notes" }.Stroke(.16f, false, V(-.32f, .5f), V(-.32f, 2.9f)).Stroke(.16f, false, V(1.08f, .8f), V(1.08f, 3.2f)).Stroke(.34f, false, V(-.32f, 2.82f), V(1.08f, 3.12f));
            g.discs.Add(new Vector3(-.7f, .45f, .42f)); g.discs.Add(new Vector3(.7f, .75f, .42f)); list.Add(g);

            g = new Glyph { name = "Half note" }.Stroke(.16f, false, V(.42f, .55f), V(.42f, 3f)); g.rings.Add(new Vector3(0, .5f, .4f)); list.Add(g);

            g = new Glyph { name = "Whole note", ringWidth = .3f }; g.rings.Add(new Vector3(0, .75f, .62f)); list.Add(g);

            g = new Glyph { name = "Sharp" }.Stroke(.16f, false, V(-.25f, .1f), V(-.25f, 2.5f)).Stroke(.16f, false, V(.25f, .25f), V(.25f, 2.65f))
                .Stroke(.3f, false, V(-.6f, .85f), V(.6f, 1.1f)).Stroke(.3f, false, V(-.6f, 1.65f), V(.6f, 1.9f)); list.Add(g);

            g = new Glyph { name = "Flat" }.Stroke(.16f, false, V(0, .1f), V(0, 2.8f)).Stroke(.2f, true, V(0, .1f), V(.4f, .4f), V(.55f, .8f), V(.4f, 1.1f), V(.1f, 1.05f), V(0, .9f)); list.Add(g);

            var arc = new List<Vector2>();
            for (var k = 0; k <= 16; k++) { var ang = Mathf.PI * k / 16f; arc.Add(V(Mathf.Cos(ang) * .95f, .3f + Mathf.Sin(ang) * .95f)); }
            g = new Glyph { name = "Fermata" }; g.strokes.Add(arc.ToArray()); g.widths.Add(.26f); g.discs.Add(new Vector3(0, .55f, .17f)); list.Add(g);

            g = new Glyph { name = "Quarter rest" }.Stroke(.26f, false, V(.05f, 2.6f), V(.4f, 2.1f), V(.05f, 1.6f), V(.42f, 1.1f), V(.1f, .85f), V(.3f, .35f)); list.Add(g);
            return list;
        }

        static Vector2[] Smooth(Vector2[] p, int div = 4)
        {
            if (p.Length < 3) return p;
            var o = new List<Vector2>();
            for (var i = 0; i + 1 < p.Length; i++)
            {
                Vector2 p0 = p[Mathf.Max(0, i - 1)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(p.Length - 1, i + 2)];
                for (var s = 0; s < div; s++)
                {
                    var t = s / (float)div; float t2 = t * t, t3 = t2 * t;
                    o.Add(.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            o.Add(p[p.Length - 1]);
            return o.ToArray();
        }

        void Statue(MeshKit mk, MeshKit solid, Transform labels, Glyph g, Vector3 ground, Vector3 face)
        {
            const float scale = 1.5f, depth = .5f, plinthH = 1.2f;
            var rot = Quaternion.LookRotation(face);
            var right = Vector3.Cross(face, Vector3.up).normalized;
            mk.Box(stoneDark, ground + Vector3.up * plinthH / 2f, new Vector3(2.6f, plinthH, 1.6f), rot);
            mk.Box(stone, ground + Vector3.up * (plinthH + .1f), new Vector3(2.9f, .2f, 1.9f), rot);
            solid.Box(stone, ground + Vector3.up * plinthH / 2f, new Vector3(2.9f, plinthH + .2f, 1.9f), rot);
            solid.Box(stone, ground + Vector3.up * (plinthH + 2.6f), new Vector3(2.4f, 4.8f, .6f), rot);   // the glyph itself
            var o = ground + Vector3.up * (plinthH + .2f);
            Vector3 W(Vector2 p) => o + right * p.x * scale + Vector3.up * p.y * scale;
            void Line(Vector2[] pts, float width)
            {
                for (var i = 0; i + 1 < pts.Length; i++)
                {
                    Vector3 a = W(pts[i]), b = W(pts[i + 1]);
                    var d = b - a; var len = d.magnitude;
                    if (len < 1e-4f) continue;
                    mk.Box(marble, (a + b) / 2f, new Vector3(width, len + width * .7f, depth), Quaternion.LookRotation(face, d / len));
                }
            }
            for (var s = 0; s < g.strokes.Count; s++) Line(g.strokes[s], g.widths[s] * scale);
            foreach (var d in g.discs) mk.Prism(marble, W(new Vector2(d.x, d.y)) - face * depth * .6f, face, d.z * scale, depth * 1.2f, 12);
            foreach (var r in g.rings)
            {
                var circle = new Vector2[25];
                for (var k = 0; k < 25; k++) { var ang = k * Mathf.PI * 2f / 24f; circle[k] = new Vector2(r.x + Mathf.Cos(ang) * r.z, r.y + Mathf.Sin(ang) * r.z * .8f); }
                Line(circle, g.ringWidth * scale);
            }
            ArtistBits.Label(labels, g.name, ground + Vector3.up * (plinthH * .62f) + face * .82f, face, .2f, new Color(.2f, .18f, .16f));
        }

        // ---------------------------------------------------------------- the quiet bench
        void QuietBench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 dir, GlowHalos halos)
        {
            // walk out to the shore and come back a little
            var p = center;
            for (var d = 60f; d < 600f; d += 2f)
            {
                var q = center + dir * d;
                if (!ctx.IsLand(q.x, q.z, 0f)) break;
                p = q;
            }
            p -= dir * 6f; p.y = ctx.Ground(p.x, p.z);
            var rot = Quaternion.LookRotation(dir);
            mk.Box(wood, p + Vector3.up * .45f, new Vector3(1.9f, .08f, .45f), rot);
            mk.Box(wood, p + Vector3.up * .8f - dir * .22f, new Vector3(1.9f, .35f, .06f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(iron, p + rot * new Vector3(s * .8f, .22f, 0), new Vector3(.08f, .45f, .42f), rot);
            solid.Box(wood, p + Vector3.up * .45f, new Vector3(1.9f, .9f, .5f), rot);
            var side = Vector3.Cross(Vector3.up, dir);
            var post = p + side * 1.6f - dir * .3f; post.y = ctx.Ground(post.x, post.z);
            ArtistBits.LanternPost(mk, ctx.Root, post, -side, wood, iron, Warm);
            halos.Add(post + Vector3.up * 2.15f - side * .65f, 1.2f, Warm, .8f);
            ArtistBits.PointLight(ctx.Root, post + Vector3.up * 2f, Warm, 7f, .9f);
        }

        // ---------------------------------------------------------------- the ring of covers
        /// Every cover, in the order the songs were added, spiralling up round the whole park. The lowest turn
        /// starts just over head height, so you can walk up under the first songs and look at them closely.
        void CoverRing(LandContext ctx)
        {
            var songs = ctx.Data.layout.hubTimeline;
            if (songs == null || songs.Length == 0) return;
            const float size = 3.2f, border = .32f, gap = 4.05f, firstBottom = 3.8f, turnRise = 4.4f;
            var firstHeight = firstBottom + size / 2f + border;
            var perTurn = Mathf.FloorToInt(2f * Mathf.PI * RingR / gap);
            var ringGo = new GameObject("The ring of every cover");
            ringGo.transform.SetParent(ctx.Root, false);
            ringGo.transform.position = new Vector3(center.x, groundY, center.z);
            var ring = ringGo.AddComponent<CoverRing>();
            var pictures = new MeshKit(); var frames = new MeshKit();
            var picture = WorldMaterials.Picture(ctx.CoverAtlas);
            var blank = WorldMaterials.Solid(new Color(.15f, .14f, .16f));
            var adders = ctx.Data.layout.hubAdders ?? new string[0];
            var frameMats = new Material[adders.Length];
            for (var i = 0; i < frameMats.Length; i++) frameMats[i] = WorldMaterials.Glow(ColorFor(adders, i), 1.3f);
            var noAdder = WorldMaterials.Glow(new Color(.7f, .7f, .7f), .8f);
            var words = new GameObject("Years").transform; words.SetParent(ringGo.transform, false);
            var lastYear = 0;
            for (var i = 0; i < songs.Length; i++)
            {
                var s = songs[i];
                var a = i * Mathf.PI * 2f / perTurn;
                var y = firstHeight + i * turnRise / perTurn;
                var outward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = outward * RingR + Vector3.up * y;               // local to the ring
                var frameMat = s.a >= 0 && s.a < frameMats.Length ? frameMats[s.a] : noAdder;
                // facing in (seen from the park) and out (seen across the water)
                foreach (var face in new[] { -outward, outward })
                {
                    var r2 = Vector3.Cross(face, Vector3.up);       // the viewer's right, so covers aren't mirrored
                    frames.Picture(frameMat, p - face * .03f, r2 * (size / 2f + border), Vector3.up * (size / 2f + border), face, new Rect(0, 0, 1, 1));
                    if (s.c >= 0) pictures.Picture(picture, p + face * .02f, r2 * size / 2f, Vector3.up * size / 2f, face, ctx.CoverRect(s.c));
                    else pictures.Picture(blank, p + face * .02f, r2 * size / 2f, Vector3.up * size / 2f, face, new Rect(0, 0, 1, 1));
                }
                ring.slotOf[s.id] = p - outward * .15f;
                ring.facingOf[s.id] = -outward;
                if (s.y != lastYear)
                {
                    lastYear = s.y;
                    var top = ringGo.transform.position + p + Vector3.up * (size / 2f + border + 1.5f);
                    ArtistBits.Label(words, s.y.ToString(), top - outward * .3f, -outward, 1.3f, Warm);
                    ArtistBits.Label(words, s.y.ToString(), top + outward * .3f, outward, 1.3f, Warm);
                }
            }
            frames.BuildSingle("Frames", ringGo.transform, false).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pictures.BuildSingle("Covers", ringGo.transform, false).GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (Transform child in ringGo.transform) child.gameObject.isStatic = false;
            // the light on the song playing now: a bright frame, and a beam of light down to the ground under it
            var hl = new GameObject("Now playing").transform;
            hl.SetParent(ringGo.transform, false);
            var hk = new MeshKit();
            var glow = new Material(WorldMaterials.Glow(NowPlaying, 3f)) { name = "Now playing" };   // its own copy, so it can pulse
            float edge = size / 2f + border + .25f, bar = .5f;
            hk.Box(glow, new Vector3(0, edge, 0), new Vector3(2f * edge + bar, bar, .08f), Quaternion.identity);
            hk.Box(glow, new Vector3(0, -edge, 0), new Vector3(2f * edge + bar, bar, .08f), Quaternion.identity);
            hk.Box(glow, new Vector3(edge, 0, 0), new Vector3(bar, 2f * edge, .08f), Quaternion.identity);
            hk.Box(glow, new Vector3(-edge, 0, 0), new Vector3(bar, 2f * edge, .08f), Quaternion.identity);
            hk.Build("Glow", hl, false).isStatic = false;
            var beam = new GameObject("Beam").transform;
            beam.SetParent(hl, false);
            beam.localPosition = new Vector3(0, -edge, 0);
            var bk = new MeshKit();
            bk.Box(glow, new Vector3(0, -.5f, 0), new Vector3(.22f, 1f, .22f), Quaternion.identity);
            var beamGo = bk.Build("Beam", beam, false);
            beamGo.isStatic = false;
            beamGo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.beam = beam; ring.beamDrop = edge;
            ring.highlight = hl;
            ring.highlightMaterial = glow; ring.highlightColor = NowPlaying;
            ring.highlightLight = ArtistBits.PointLight(hl, hl.position, NowPlaying, 14f, 3f);
            ring.highlightLight.transform.localPosition = Vector3.forward * 2f;
            hl.gameObject.SetActive(false);
            Debug.Log($"The Everything Hub: ring of {songs.Length} covers in {songs.Length / (float)perTurn:0.0} turns, top at {firstHeight + songs.Length * turnRise / perTurn:0} m");
        }
    }
}
