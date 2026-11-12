using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Hair Metal Strip (WORLD_GUIDE.md): a neon boulevard across a dry desert at electric violet dusk, the sun
    /// just gone and the horizon still burning orange. Loud and silly in the best way. You arrive under a neon arch
    /// beside a giant hairspray can puffing glitter; the Strip runs away between palms and chrome lamps, a venue
    /// for every artist down both sides, each under its own neon sign. At the far end stands the Club, built as a
    /// giant amp head with pyro jets on its roof. Round the Strip, desert mesas and more giant hairspray cans stand
    /// like hoodoos. Motorbikes cruise the Strip. Files: HairKit (layout, the Strip, the Club, the desert),
    /// HairArtists, HairLife.
    public partial class HairKit : LandKit
    {
        public override string Land => "The Hair Metal Strip";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a)
        {
            if (!hasStreet) return base.ArtistFront(data, a);
            var sideV = Vector3.Cross(Vector3.up, streetDir);
            return Vector3.Dot(new Vector3(a.x, 0, a.z) - streetA, sideV) >= 0f ? -sideV : sideV;
        }

        public const string Kickstart = "6imjJij6aCM0IbbODUzJQn", NovemberRain = "3YRCqOhFifThpSRFJ1VWFM", FinalCountdown = "3MrRksHupTVEQ7YbA0FsZK", Prayer = "2tyW1uBUnYMKAFEfKDKi9B";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (Kickstart, "Kickstart My Heart", "Mötley Crüe"), (NovemberRain, "November Rain", "Guns N' Roses"),
            (FinalCountdown, "The Final Countdown", "Europe"), (Prayer, "Livin' On A Prayer", "Bon Jovi"),
        };

        // hot pink and leopard gold neon against violet; chrome and black
        static readonly Color Pink = new Color(1f, .25f, .65f), Gold = new Color(1f, .78f, .25f), Cyan = new Color(.3f, .9f, 1f), Violet = new Color(.65f, .35f, 1f), Ink = new Color(1f, .95f, .9f);
        static readonly Color[] Neon = { Pink, Gold, Cyan, Violet, new Color(1f, .35f, .2f) };
        const float StreetHalf = 7f, Walk = 3f, Setback = 21f;

        Vector3 streetA, streetDir = Vector3.forward; float streetLen = 300f; bool hasStreet; float[] profile = new float[0];
        Material asphalt, pave, chrome, black, sand, rock, leopard, palmLeaf, palmBark;
        GlowHalos halos; Vector3 center; HairLife life; MeshKit soft;
        readonly List<Vector3> noGrass = new List<Vector3>();

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Solid(new Color(.14f, .12f, .16f), .7f); pave = WorldMaterials.Solid(new Color(.42f, .38f, .42f));
            chrome = WorldMaterials.Solid(new Color(.82f, .84f, .88f), .95f); black = WorldMaterials.Solid(new Color(.07f, .06f, .08f), .6f);
            sand = WorldMaterials.Solid(new Color(.78f, .58f, .42f)); rock = WorldMaterials.Solid(new Color(.62f, .38f, .3f));
            leopard = WorldMaterials.Solid(new Color(.85f, .62f, .28f)); palmLeaf = WorldMaterials.Solid(new Color(.2f, .38f, .26f)); palmBark = WorldMaterials.Solid(new Color(.45f, .34f, .26f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 8f) color = Color.Lerp(new Color(.56f, .34f, .28f), new Color(.66f, .42f, .32f), patch);     // red rock
            else if (height < 2.5f) color = new Color(.82f, .7f, .55f);
            else color = Color.Lerp(new Color(.72f, .54f, .4f), new Color(.6f, .48f, .38f), Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        float StreetY(float t)
        {
            if (profile.Length == 0) return 0f;
            var f = Mathf.Clamp(t / 8f, 0, profile.Length - 1.001f); var i = Mathf.FloorToInt(f);
            return Mathf.Lerp(profile[i], profile[Mathf.Min(i + 1, profile.Length - 1)], f - i);
        }

        Vector3 OnStreet(float t, float across) { var side = Vector3.Cross(Vector3.up, streetDir); var p = streetA + streetDir * t + side * across; p.y = StreetY(t); return p; }

        /// The Strip: from where you arrive, the longest straight run well inside the land, levelled into a smooth
        /// boulevard; the venues down both sides, the radio-station artists in the last pairs before the Club.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index];
            var heart = new Vector2(isl.labelX, isl.labelZ);
            int Id(float x, float z)
            {
                var cc = Mathf.RoundToInt(x / data.Step); var rr = Mathf.RoundToInt((data.layout.depth - z) / data.Step);
                if (cc < 0 || rr < 0 || cc >= data.Nx || rr >= data.Nz) return 255;
                var i = rr * data.Nx + cc;
                return data.heights[i] > 1.6f ? data.islandIds[i] : 254;
            }
            bool Clear(Vector2 p, float r)
            {
                for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (Id(p.x + Mathf.Cos(a) * r, p.y + Mathf.Sin(a) * r) != index) return false; }
                return Id(p.x, p.y) == index;
            }
            var bestLen = 0f; var bestDir = Vector2.up; var bestStart = heart;
            for (var k = 0; k < 36; k++)
            {
                var a = k * Mathf.PI * 2f / 36f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                // the Strip may start a little behind where you arrive, so it can run longer
                for (var back = 0f; back <= 60f; back += 30f)
                {
                    var st = heart - d * back; if (!Clear(st, 50f)) continue;
                    var len = 0f;
                    for (var s = 10f; s <= 460f; s += 10f) { if (!Clear(st + d * s, 50f)) break; len = s; }
                    if (len > bestLen) { bestLen = len; bestDir = d; bestStart = st; }
                }
            }
            if (bestLen < 150f) { Debug.LogWarning("The Hair Metal Strip: no room for the Strip"); return; }
            hasStreet = true;
            streetDir = new Vector3(bestDir.x, 0, bestDir.y); streetA = new Vector3(bestStart.x, 0, bestStart.y); streetLen = bestLen;
            var n = Mathf.CeilToInt(streetLen / 8f) + 1;
            var raw = new float[n];
            for (var i = 0; i < n; i++) { var p = bestStart + bestDir * (i * 8f); raw[i] = data.HeightAt(p.x, p.y); }
            profile = new float[n];
            for (var i = 0; i < n; i++) { float sum = 0f; var c = 0; for (var j = -6; j <= 6; j++) { sum += raw[Mathf.Clamp(i + j, 0, n - 1)]; c++; } profile[i] = sum / c; }
            for (var i = 1; i < n; i++) profile[i] = Mathf.Clamp(profile[i], profile[i - 1] - .55f, profile[i - 1] + .55f);
            var across = new Vector2(-bestDir.y, bestDir.x);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var x = col * data.Step; var rel = new Vector2(x, z) - bestStart;
                    var t = Vector2.Dot(rel, bestDir); var off = Mathf.Abs(Vector2.Dot(rel, across));
                    if (t < -50f || t > streetLen + 60f) continue;
                    var tc = Mathf.Clamp(t, 0f, streetLen);
                    var endFall = t < 0f ? -t : t > streetLen ? t - streetLen : 0f;
                    var d = Mathf.Max(off, endFall * .8f);
                    if (d > 64f) continue;
                    var y = StreetY(tc);
                    data.heights[i] = d < 38f ? y : Mathf.Lerp(y, data.heights[i], Mathf.SmoothStep(0f, 1f, (d - 38f) / 26f));
                }
            }
            var arts = new List<IslandWorldData.Artist>();
            foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) arts.Add(a);
            arts.Sort((p, q) => Rank(q).CompareTo(Rank(p)) != 0 ? Rank(q).CompareTo(Rank(p)) : string.CompareOrdinal(p.name, q.name));
            var pairs = Mathf.CeilToInt(arts.Count / 2f);
            var first = 50f; var last = streetLen - 75f; var step = pairs > 1 ? (last - first) / (pairs - 1) : 0f;
            for (var i = 0; i < arts.Count; i++)
            {
                var pair = pairs - 1 - i / 2; var side = i % 2 == 0 ? 1 : -1;
                var t = first + pair * step + (side > 0 ? 0f : step * .4f);
                var p = bestStart + bestDir * t + across * side * Setback;
                arts[i].x = p.x; arts[i].z = p.y;
                arts[i].r = Mathf.Min(arts[i].r, 18f);
            }
        }

        static int Rank(IslandWorldData.Artist a) => a.name == "Guns N' Roses" ? 100 : (a.songList?.Length ?? 1) * 10 + (HasStation(a) ? 5 : 0);

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos(); noGrass.Clear(); soft = new MeshKit();
            center = ctx.Plaza;
            life = new GameObject("Hair Metal life").AddComponent<HairLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            if (hasStreet) { Strip(ctx, mk, solid); Arrival(ctx, mk, solid); Club(ctx, mk, solid); }
            Desert(ctx, mk, solid);
            DesertMore(ctx, mk, solid);
            mk.Build("The Strip", ctx.Root, false);
            soft.Build("Soft strip", ctx.Root, false);
            solid.BuildCollider("Strip walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Strip glow", ctx.Root); life.Flatten(); }

        bool OnStrip(float x, float z, float pad)
        {
            if (!hasStreet) return false;
            var rel = new Vector3(x - streetA.x, 0, z - streetA.z);
            var t = Vector3.Dot(rel, streetDir); var off = Mathf.Abs(Vector3.Dot(rel, Vector3.Cross(Vector3.up, streetDir)));
            return t > -40f - pad && t < streetLen + 55f + pad && off < 38f + pad;
        }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        // ---------------------------------------------------------------- the Strip
        /// The boulevard: dark asphalt with a pink centre line, pavements and kerbs, palms ringed with neon, chrome
        /// lamps with pink tubes, motorbikes parked at the kerb.
        void Strip(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var side = Vector3.Cross(Vector3.up, streetDir); var up = Vector3.up;
            var curb = WorldMaterials.Solid(new Color(.6f, .58f, .6f));
            const float seg = 4f;
            for (var t = -20f; t < streetLen + 30f; t += seg)
            {
                Vector3 S(float tt, float a) { var p = OnStreet(Mathf.Clamp(tt, 0f, streetLen), a); p += streetDir * (tt - Mathf.Clamp(tt, 0f, streetLen)); return p; }
                var a0 = S(t, -StreetHalf); var a1 = S(t + seg, -StreetHalf); var b0 = S(t, StreetHalf); var b1 = S(t + seg, StreetHalf);
                mk.Quad(asphalt, a0 + up * .06f, b0 + up * .06f, b1 + up * .06f, a1 + up * .06f, (a0 + b1) / 2f - up);
                if (Mathf.Repeat(t, 8f) < .1f) foreach (var s in new[] { -.25f, .25f }) mk.Box(GlowM(Pink, 1.2f), S(t + 1.5f, s) + up * .075f, new Vector3(.12f, .02f, 3f), Quaternion.LookRotation(streetDir));
                foreach (var s in new[] { -1f, 1f })
                {
                    var c0 = S(t, s * (StreetHalf + Walk / 2f)); var c1 = S(t + seg, s * (StreetHalf + Walk / 2f));
                    mk.Box(pave, (c0 + c1) / 2f + up * .13f, new Vector3(Walk, .22f, Vector3.Distance(c0, c1) + .05f), Quaternion.LookRotation(c1 - c0));
                    mk.Box(curb, S(t + seg / 2f, s * (StreetHalf + .1f)) + up * .17f, new Vector3(.25f, .27f, seg + .02f), Quaternion.LookRotation(c1 - c0));
                }
            }
            // palms ringed with neon and chrome lamps, alternating, both sides
            for (var t = 8f; t < streetLen; t += 16f)
                foreach (var s in new[] { -1f, 1f })
                {
                    var p = OnStreet(t + (s > 0 ? 0f : 8f), s * (StreetHalf + Walk + 1.2f));
                    if (Mathf.Repeat(t, 32f) < 16f) Palm(ctx, mk, p, ctx.Range(9f, 12f), Neon[Mathf.Abs((int)(t / 16f)) % 3]);
                    else if (!InFrontOfVenue(ctx, t + (s > 0 ? 0f : 8f), s))   // no lamp post standing across a venue's front
                    {
                        var lp = OnStreet(t + (s > 0 ? 0f : 8f), s * (StreetHalf + .6f));
                        mk.Prism(chrome, lp, up, .12f, 7f, 8);
                        mk.Box(GlowM(Pink, 2.2f), lp + up * 4.8f, new Vector3(.12f, 3.6f, .12f), Quaternion.identity);
                        mk.Box(chrome, lp + up * 7f - side * s * .8f, new Vector3(.1f, .1f, 1.7f), Quaternion.LookRotation(side));
                        var head = lp + up * 6.9f - side * s * 1.6f;
                        mk.Box(GlowM(new Color(1f, .85f, .7f), 2.4f), head, new Vector3(.6f, .14f, .35f), Quaternion.LookRotation(streetDir));
                        halos.Add(head - up * .4f, 2f, new Color(1f, .6f, .7f), .8f);
                        ArtistBits.PointLight(ctx.Root, head - up * .6f, new Color(1f, .7f, .8f), 18f, 2f);
                        ArtistBits.PostCollider(ctx.Root, lp, 7f, .15f);
                    }
                }
            // motorbikes parked at the kerb in rows
            for (var t = 20f; t < streetLen - 20f; t += 37f)
                foreach (var s in new[] { -1f, 1f })
                    for (var k = 0; k < 3; k++)
                    {
                        var p = OnStreet(t + k * 1.3f + (s > 0 ? 0f : 18f), s * (StreetHalf - 1.2f));
                        Bike(mk, solid, p, Quaternion.Euler(0, s * 60f, 0) * streetDir, k % 2 == 0 ? new Color(.08f, .08f, .1f) : Neon[(k + (int)t) % Neon.Length]);
                    }
            life.streetA = streetA; life.streetDir = streetDir; life.streetLen = streetLen; life.profile = profile;
            noGrass.Add(new Vector3(streetA.x, 0, streetA.z));
        }

        /// Whether a spot at distance t along the Strip, on side s, is in front of a venue (within its width).
        bool InFrontOfVenue(LandContext ctx, float t, float s)
        {
            var sideV = Vector3.Cross(Vector3.up, streetDir);
            foreach (var a in ctx.Artists)
            {
                var rel = new Vector3(a.x - streetA.x, 0, a.z - streetA.z);
                if (Vector3.Dot(rel, sideV) * s <= 0f) continue;
                var half = a.name == "Guns N' Roses" ? 11f : HasStation(a) ? 9f : 6.5f;
                if (Mathf.Abs(Vector3.Dot(rel, streetDir) - t) < half + 1.5f) return true;
            }
            return false;
        }

        void Palm(LandContext ctx, MeshKit mk, Vector3 at, float h, Color ring)
        {
            var up = Vector3.up; var lean = (Vector3.up + new Vector3(ctx.Range(-.12f, .12f), 0, ctx.Range(-.12f, .12f))).normalized;
            mk.Prism(palmBark, at - up * .3f, lean, .32f, h, 7, .7f);
            var top = at + lean * h;
            for (var k = 0; k < 4; k++) mk.Prism(GlowM(ring, 2.2f), at + lean * (2f + k * 1.6f), lean, .36f - k * .02f, .12f, 10);
            for (var k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0, k * 45f + ctx.Range(-10f, 10f), 0) * Vector3.forward; var w = Vector3.Cross(up, d) * .5f;
                var mid = top + d * 2f + up * .4f; var tip = top + d * 4f - up * 1.2f;
                soft.Tri(palmLeaf, top, mid + w, mid - w, top - up * 3f); soft.Tri(palmLeaf, top, mid - w, mid + w, top + up * 3f);
                soft.Tri(palmLeaf, mid + w, tip, mid - w, mid - up * 3f); soft.Tri(palmLeaf, mid - w, tip, mid + w, mid + up * 3f);
            }
            ArtistBits.PostCollider(ctx.Root, at, h, .35f);
        }

        void Bike(MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir, Color paint)
        {
            var up = Vector3.up; dir.y = 0; dir.Normalize(); var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var lean = Quaternion.AngleAxis(12f, dir); var body = WorldMaterials.Solid(paint, .9f);
            foreach (var z in new[] { -.8f, .85f }) { mk.Prism(black, at + dir * z + up * .36f - side * .07f, side, .36f, .14f, 12); mk.Prism(chrome, at + dir * z + up * .36f + side * .075f, side, .15f, .01f, 8); }
            mk.Box(body, at + lean * (up * .8f) + dir * .2f, new Vector3(.32f, .28f, .62f), rot);
            mk.Box(black, at + lean * (up * .82f) - dir * .38f, new Vector3(.3f, .1f, .65f), rot);
            mk.Box(chrome, at + lean * (up * .55f), new Vector3(.26f, .3f, .42f), rot);
            mk.Box(chrome, at + lean * (up * 1.15f) + dir * .7f, new Vector3(.95f, .05f, .05f), rot);
            mk.Box(chrome, at + lean * (up * .75f) + dir * .8f, new Vector3(.05f, .75f, .05f), rot * Quaternion.Euler(28f, 0, 0));
            mk.Box(chrome, at + lean * (up * .45f) - dir * .6f + side * .2f, new Vector3(.08f, .08f, .9f), rot);       // pipes
            solid.Box(black, at + up * .65f, new Vector3(1.1f, 1.3f, 2.1f), rot);   // wide enough for the leaning handlebars
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        // ---------------------------------------------------------------- where you arrive
        /// A neon arch over the start of the Strip, "THE STRIP" in pink and gold, and beside it a giant hairspray can
        /// puffing a cloud of glitter (HairLife).
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, streetDir); var rot = Quaternion.LookRotation(streetDir);
            var a = OnStreet(4f, 0f);
            foreach (var s in new[] { -1f, 1f })
            {
                var leg = a + side * s * (StreetHalf + Walk + .6f);
                mk.Box(chrome, leg + up * 6f, new Vector3(.8f, 12f, .8f), rot); solid.Box(chrome, leg + up * 6f, new Vector3(.8f, 12f, .8f), rot);
                for (var k = 0; k < 10; k++) mk.Box(GlowM(k % 2 == 0 ? Pink : Gold, 2.4f), leg + up * (1f + k * 1.1f) - streetDir * .41f, new Vector3(.5f, .4f, .02f), rot);
            }
            var arch = a + up * 12.6f;
            mk.Box(black, arch, new Vector3((StreetHalf + Walk + .6f) * 2f + 1f, 2.6f, .8f), rot);
            mk.Box(GlowM(Pink, 2.2f), arch - streetDir * .41f + up * 1.2f, new Vector3((StreetHalf + Walk) * 2f + 1f, .12f, .02f), rot);
            mk.Box(GlowM(Gold, 2.2f), arch - streetDir * .41f - up * 1.2f, new Vector3((StreetHalf + Walk) * 2f + 1f, .12f, .02f), rot);
            ArtistBits.Label(ctx.Root, "THE STRIP", arch - streetDir * .45f, -streetDir, .9f, Pink, TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "THE HAIR METAL STRIP", arch + streetDir * .45f, streetDir, .5f, Gold, TextAnchor.MiddleCenter);
            halos.Add(arch - streetDir * 1.5f, 6f, Pink, .5f);
            // the giant hairspray can
            var can = OnStreet(-8f, StreetHalf + Walk + 12f);
            life.cans.Add(HairsprayCan(ctx, mk, solid, can, 1.8f, Pink));
        }

        /// A giant hairspray can: a tall cylinder in bright paint with a chrome shoulder, a cap and a nozzle; the
        /// nozzle is where HairLife puffs glitter from. Returns the nozzle.
        Vector3 HairsprayCan(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, float s, Color paint)
        {
            var up = Vector3.up; var body = WorldMaterials.Solid(paint, .85f);
            mk.Prism(body, g - up * .5f, up, 2f * s, 8f * s, 20);
            mk.Prism(chrome, g + up * (7.5f * s), up, 2f * s, 1.2f * s, 20, .55f);
            mk.Prism(chrome, g + up * (8.7f * s), up, 1.1f * s, 1f * s, 14);
            mk.Box(black, g + up * (9.3f * s) + Vector3.forward * 1f * s, new Vector3(.4f, .4f, .8f) * s, Quaternion.identity);
            mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), g + up * (4f * s) + Vector3.forward * 2.01f * s, new Vector3(2.4f, 2.6f, .02f) * s, Quaternion.identity);   // label
            mk.Box(GlowM(Gold, 1.8f), g + up * (4.9f * s) + Vector3.forward * 2.03f * s, new Vector3(2f, .3f, .02f) * s, Quaternion.identity);
            solid.Prism(body, g - up * .5f, up, 2f * s, 9.5f * s, 12);
            noGrass.Add(new Vector3(g.x, 2.5f * s, g.z));
            return g + up * (9.3f * s) + Vector3.forward * 1.5f * s;
        }

        // ---------------------------------------------------------------- the Club
        /// The Club at the end of the Strip, built as a giant amp head: a black box with gold piping, a woven
        /// grille across its face, a row of chrome knobs, a carrying handle on top, a glowing name plate, and pyro
        /// jets along the roof (HairLife fires them). Its front is open as a stage you can walk onto.
        void Club(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, streetDir);
            var c = OnStreet(streetLen + 26f, 0f); c.y = StreetY(streetLen);
            var face = -streetDir; var rot = Quaternion.LookRotation(face);
            const float W = 34f, D = 18f, H = 18f;
            var goldM = WorldMaterials.Solid(Gold, .8f); var grille = WorldMaterials.Solid(new Color(.24f, .2f, .18f));
            // the shell: back, sides, roof, and the face above the stage opening
            mk.Box(black, c + up * H / 2f - face * (D / 2f - .4f), new Vector3(W, H, .8f), rot); solid.Box(black, c + up * H / 2f - face * (D / 2f - .4f), new Vector3(W, H, .8f), rot);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(black, c + up * H / 2f + side * s * (W / 2f - .4f), new Vector3(.8f, H, D), rot); solid.Box(black, c + up * H / 2f + side * s * (W / 2f - .4f), new Vector3(.8f, H, D), rot); }
            mk.Box(black, c + up * (H - .4f), new Vector3(W, .8f, D), rot);
            var faceY = 9f;
            mk.Box(black, c + up * (faceY + (H - faceY) / 2f) + face * (D / 2f - .4f), new Vector3(W, H - faceY, .8f), rot);
            mk.Box(grille, c + up * (faceY + (H - faceY) / 2f) + face * (D / 2f + .02f), new Vector3(W - 3f, H - faceY - 2.4f, .05f), rot);
            for (var k = 0; k < 12; k++) mk.Box(WorldMaterials.Solid(new Color(.3f, .26f, .22f)), c + up * (faceY + 1.4f + k * .55f) + face * (D / 2f + .05f), new Vector3(W - 3f, .06f, .03f), rot);
            // gold piping round the face, the name plate, the knobs along the top edge
            foreach (var y in new[] { faceY + .3f, H - .3f }) mk.Box(goldM, c + up * y + face * (D / 2f + .06f), new Vector3(W + .1f, .25f, .1f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(goldM, c + up * (H / 2f) + side * s * (W / 2f) + face * (D / 2f + .06f), new Vector3(.25f, H, .1f), rot);
            mk.Box(GlowM(Gold, 2.6f), c + up * (faceY + (H - faceY) / 2f + .4f) + face * (D / 2f + .1f), new Vector3(10f, 1.8f, .06f), rot);
            ArtistBits.Label(ctx.Root, "THE CLUB", c + up * (faceY + (H - faceY) / 2f + .4f) + face * (D / 2f + .16f), face, .9f, new Color(.15f, .05f, .05f), TextAnchor.MiddleCenter);
            for (var k = 0; k < 10; k++)
            {
                var kp = c + up * (H - 1.4f) + face * (D / 2f + .2f) + side * ((k - 4.5f) * 2.6f);
                mk.Prism(chrome, kp, face, .55f, .5f, 14); mk.Box(black, kp + face * .51f + up * .25f, new Vector3(.08f, .5f, .02f), rot);
            }
            // the handle on top
            foreach (var s in new[] { -1f, 1f }) mk.Box(black, c + up * (H + 1.2f) + side * s * 6f, new Vector3(1f, 2.4f, 1.6f), rot);
            mk.Box(black, c + up * (H + 2.6f), new Vector3(13f, 1f, 1.6f), rot);
            // inside: a big stage, amp stacks, a drum riser, a lighting rig, a glitter ball
            mk.Box(black, c + up * .8f - face * 3f, new Vector3(W - 4f, 1.6f, 10f), rot); solid.Box(black, c + up * .8f - face * 3f, new Vector3(W - 4f, 1.6f, 10f), rot);
            for (var k = 0; k < 6; k++) foreach (var s in new[] { -1f, 1f })
            {
                var amp = c + up * (1.6f + .9f + (k / 2) * 1.8f) - face * 7f + side * s * (9f + (k % 2) * 2f);
                mk.Box(black, amp, new Vector3(1.9f, 1.75f, 1f), rot); mk.Box(grille, amp + face * .51f, new Vector3(1.7f, 1.5f, .02f), rot);
            }
            mk.Box(WorldMaterials.Solid(new Color(.8f, .1f, .3f)), c + up * 2.4f - face * 6.5f, new Vector3(6f, 1.6f, 4f), rot);
            mk.Blob(chrome, c + up * 12f, Vector3.one * 1.4f, ctx.Rng, 0f);
            life.glitterBall = c + up * 12f;
            for (var k = 0; k < 8; k++) mk.Prism(GlowM(Neon[k % Neon.Length], 2.4f), c + up * (H - 1.6f) - face * 2f + side * ((k - 3.5f) * 3.6f), (face - up).normalized, .35f, .5f, 10);
            ArtistBits.PointLight(ctx.Root, c + up * 8f + face * 2f, Pink, 30f, 2.2f, true);
            IndoorSpaces.Add(c + up * H / 2f, rot, new Vector3(W, H, D));
            // pyro jets along the roof
            for (var k = 0; k < 6; k++)
            {
                var j = c + up * H + side * ((k - 2.5f) * 5.5f) + face * (D / 2f - 1.5f);
                mk.Prism(chrome, j, up, .4f, .8f, 10, .7f); life.pyro.Add(j + up * .8f);
            }
            life.club = c;
            noGrass.Add(new Vector3(c.x, 24f, c.z));
        }

        // ---------------------------------------------------------------- the desert round the Strip
        /// Mesas, saguaros, giant hairspray cans standing like hoodoos (puffing glitter now and then), billboards,
        /// a crashed tour bus, chrome guitar sculptures, leopard-print awnings over little stalls.
        void Desert(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int mesas = 0, cacti = 0, cans = 0, boards = 0, guitars = 0;
            const float cell = 24f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!ctx.IsLand(px, pz, 6f) || OnStrip(px, pz, 4f) || InNoGrass(px, pz, 4f) || !ctx.IsFree(px, pz, 8f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .1f && mesas < 14) { Mesa(ctx, mk, solid, p); mesas++; }
                else if (roll < .22f && cans < 11) { life.cans.Add(HairsprayCan(ctx, mk, solid, p, ctx.Range(.9f, 1.4f), Neon[cans % Neon.Length])); cans++; }
                else if (roll < .55f) { for (var k = 0; k < ctx.Rng.Next(1, 4); k++) Saguaro(ctx, mk, ctx.OnGround(px + ctx.Range(-6f, 6f), pz + ctx.Range(-6f, 6f))); cacti++; }
                else if (roll < .62f && boards < 9) { Billboard(ctx, mk, solid, p, boards); boards++; }
                else if (roll < .68f && guitars < 7) { ChromeGuitar(ctx, mk, solid, p); guitars++; }
                else continue;
                noGrass.Add(new Vector3(px, 6f, pz));
            }
            Debug.Log($"The Hair Metal Strip: {mesas} mesas, {cans} hairspray cans, {cacti} cactus groups, {boards} billboards, {guitars} chrome guitars");
        }

        void Mesa(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g)
        {
            var up = Vector3.up; var r = ctx.Range(6f, 11f); var h = ctx.Range(7f, 14f);
            mk.Prism(rock, g - up * 1f, up, r, h * .55f, 9, .85f, ctx.Range(0, 40f));
            mk.Prism(WorldMaterials.Solid(new Color(.7f, .45f, .34f)), g + up * (h * .55f - 1.05f), up, r * .85f, h * .45f, 9, .9f, ctx.Range(0, 40f));
            solid.Prism(rock, g - up * 1f, up, r * .9f, h, 8);
        }

        void Saguaro(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var green = WorldMaterials.Solid(new Color(.3f, .48f, .3f)); var h = ctx.Range(3f, 6f);
            mk.Prism(green, g - up * .2f, up, .35f, h, 8, .9f);
            mk.Blob(green, g + up * h, Vector3.one * .33f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f })
            {
                if (ctx.Chance(.3f)) continue;
                var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.right * s; var y = h * ctx.Range(.35f, .6f);
                mk.Box(green, g + up * y + d * .5f, new Vector3(.55f, .45f, .5f), Quaternion.LookRotation(d));
                mk.Prism(green, g + up * y + d * .8f, up, .26f, h * .35f, 8, .9f);
                BoxCol(ctx.Root, g + up * (y + h * .175f) + d * .7f, new Vector3(.6f, h * .35f + .5f, .8f), Quaternion.LookRotation(d));   // the arm
            }
            ArtistBits.PostCollider(ctx.Root, g, h, .35f);
        }

        void Billboard(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, int k)
        {
            var up = Vector3.up; var face = new Vector3(streetA.x - g.x, 0, streetA.z - g.z).normalized; var rot = Quaternion.LookRotation(face);
            foreach (var s in new[] { -3f, 3f }) { mk.Box(chrome, g + up * 4f + Vector3.Cross(up, face) * s, new Vector3(.4f, 8f, .4f), rot); ArtistBits.PostCollider(ctx.Root, g + Vector3.Cross(up, face) * s, 8f, .3f); }
            mk.Box(chrome, g + up * 9.5f, new Vector3(10f, 4.4f, .4f), rot);
            mk.Box(leopard, g + up * 9.5f + face * .21f, new Vector3(9.4f, 3.8f, .02f), rot);
            for (var i = 0; i < 14; i++) mk.Blob(black, g + up * (8f + (i % 4) * .9f) + Vector3.Cross(up, face) * ((i * 1.37f) % 8.6f - 4.3f) + face * .23f, new Vector3(.22f, .16f, .02f), ctx.Rng, .4f);
            string[] words = { "TEASE IT UP", "LOUDER", "ALL NIGHT", "SPANDEX SALE", "BIGGER HAIR", "ROCK ON" };
            mk.Box(GlowM(Neon[k % Neon.Length], 2.4f), g + up * 11.95f + face * .21f, new Vector3(10f, .15f, .04f), rot);
            ArtistBits.Label(ctx.Root, words[k % words.Length], g + up * 9.5f + face * .26f, face, .6f, new Color(.1f, .05f, .08f), TextAnchor.MiddleCenter);
            halos.Add(g + up * 12f + face * 1f, 3f, Neon[k % Neon.Length], .5f);
        }

        void ChromeGuitar(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g)
        {
            var up = Vector3.up; var face = new Vector3(streetA.x - g.x, 0, streetA.z - g.z).normalized; var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, ctx.Range(-25f, 25f));
            mk.Box(black, g + up * .5f, new Vector3(3f, 1f, 3f), Quaternion.LookRotation(face));
            // a flying-V in chrome, standing on its point
            var u = rot * Vector3.up; var r = rot * Vector3.right;
            mk.Tri(chrome, g + up * 1f, g + up * 1f + u * 5f + r * 2.2f, g + up * 1f + u * 3.4f, g + up * 1f + face * 3f);
            mk.Tri(chrome, g + up * 1f, g + up * 1f + u * 3.4f, g + up * 1f + u * 5f - r * 2.2f, g + up * 1f + face * 3f);
            mk.Tri(chrome, g + up * 1f, g + up * 1f + u * 3.4f, g + up * 1f + u * 5f + r * 2.2f, g + up * 1f - face * 3f);
            mk.Tri(chrome, g + up * 1f, g + up * 1f + u * 5f - r * 2.2f, g + up * 1f + u * 3.4f, g + up * 1f - face * 3f);
            mk.Box(black, g + up * 1f + u * 6f, new Vector3(.3f, 5.5f, .2f), rot);
            mk.Box(GlowM(Pink, 2.4f), g + up * 1f + u * 8.9f, new Vector3(.6f, .9f, .25f), rot);
            solid.Box(black, g + up * 3f, new Vector3(3f, 6f, 1f), Quaternion.LookRotation(face));
            solid.Box(black, g + up * 1.2f, new Vector3(3f, 2.4f, 5.6f), Quaternion.LookRotation(face));   // the plinth and the V's lower wings
            halos.Add(g + up * 4f + face, 2f, Pink, .5f);
        }

        /// Dry desert grass in tufts, sparse, with little yellow flowers.
        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.2f, .8f, new Color(.7f, .58f, .38f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1987);
            const float step = 3f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 2f) || OnStrip(px, pz, 0f) || InNoGrass(px, pz, 0f) || !ctx.IsFree(px, pz, .5f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 2f, pz * .03f + 9f) < .45f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.7f, .58f, .36f), new Color(.55f, .5f, .32f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .3f + (float)rng.NextDouble() * .35f, col, Color.Lerp(col, new Color(.95f, .85f, .55f), .4f), rng, 3, .07f);
                if (rng.NextDouble() < .05) wm.Flower(at + new Vector3(.1f, 0, .1f), .3f, Gold, rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Desert grass", ctx.Root, mat);
        }
    }
}
