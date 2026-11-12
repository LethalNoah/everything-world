using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Garage Rock Row (WORLD_GUIDE.md): a gritty small-town main street late at night after rain. You arrive at
    /// one end of the Row: wet asphalt shining with neon, orange street lamps, a garage for every artist down both
    /// sides with the door rolled up and the band's gear set up inside, a pub, a diner, a gas station, a motel and
    /// back alleys, and at the far end a neon casino with chasing marquee bulbs. Arctic Monkeys have the biggest
    /// garage, nearest the casino. Round the town: hills with a water tower, billboards, a junkyard, power lines.
    /// Cars cruise the Row with their headlights on; "Sex on Fire" sets the burn barrels roaring, "505" turns the
    /// neon red, "When You Were Young" sends the casino wild. Files: GarageKit (the street, the town),
    /// GarageArtists, GarageLife.
    public partial class GarageKit : LandKit
    {
        public override string Land => "Garage Rock Row";
        /// The garages face the street.
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a)
        {
            if (!hasStreet) return base.ArtistFront(data, a);
            var sideV = Vector3.Cross(Vector3.up, streetDir);
            return Vector3.Dot(new Vector3(a.x, 0, a.z) - streetA, sideV) >= 0f ? -sideV : sideV;
        }

        public const string DoIWannaKnow = "5FVd6KXrgO9B3JPmC8OPst", FiveOhFive = "58ge6dfP91o9oXMzq3XkIS", SexOnFire = "0ntQJM78wzOLVeCUAW7Y45", WhenYouWereYoung = "70wYA8oYHoMzhRRkARoMhU";
        public override (string song, string label, string artist)[] SpecialSongs => new[] { (SexOnFire, "Sex on Fire", "Kings of Leon"), (FiveOhFive, "505", "Arctic Monkeys"), (WhenYouWereYoung, "When You Were Young", "The Killers") };

        // a bright neon night: yellow sodium light against a teal sky, neon mostly cyan, white, yellow and electric
        // blue (yellow against blue reads for everyone, including the colourblind; the Small Hours stays dark)
        static readonly Color Asphalt = new Color(.2f, .21f, .24f), Curb = new Color(.55f, .55f, .55f), Sodium = new Color(1f, .82f, .32f), Ink = new Color(.96f, .94f, .9f);
        static readonly Color[] NeonC = { new Color(.2f, .95f, 1f), new Color(.95f, .97f, 1f), new Color(1f, .88f, .25f), new Color(.35f, .55f, 1f), new Color(1f, .4f, .9f), new Color(1f, .62f, .15f) };
        static readonly Color[] Paint = { new Color(.78f, .7f, .55f), new Color(.6f, .66f, .72f), new Color(.45f, .62f, .75f), new Color(.85f, .78f, .5f), new Color(.7f, .72f, .75f), new Color(.9f, .86f, .78f) };

        const float StreetHalf = 6f, Walk = 3f, Setback = 22f;

        // the street, worked out in ShapeGround
        Vector3 streetA, streetDir = Vector3.forward; float streetLen = 300f; bool hasStreet;
        float[] profile = new float[0];

        Material asphalt, curb, dark, metal, brick, glass, wood;
        GlowHalos halos;
        Vector3 center;
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly Dictionary<string, (Vector3 front, int side)> slots = new Dictionary<string, (Vector3, int)>();
        GarageLife life;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Solid(Asphalt, .85f);
            curb = WorldMaterials.Solid(Curb);
            dark = WorldMaterials.Solid(new Color(.08f, .08f, .09f));
            metal = WorldMaterials.Solid(new Color(.5f, .52f, .55f), .5f);
            brick = WorldMaterials.Solid(new Color(.5f, .27f, .22f));
            glass = WorldMaterials.Glow(new Color(1f, .78f, .5f), 1.1f);
            wood = WorldMaterials.Solid(new Color(.4f, .3f, .24f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static readonly System.Random ColRng = new System.Random(0);    // for collider blobs: no lumps, and the land's own random stays as it was
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 7f) color = Color.Lerp(new Color(.35f, .33f, .32f), new Color(.42f, .4f, .38f), patch);
            else if (height < 2.5f) color = new Color(.4f, .38f, .34f);
            else color = Color.Lerp(new Color(.26f, .33f, .24f), new Color(.34f, .36f, .26f), Mathf.SmoothStep(.1f, .9f, patch));
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

        /// Lay out the Row: from where you arrive, the longest straight run that stays well inside the land; level
        /// a smooth road along it (with the lots either side), and put the garages down both sides, Arctic
        /// Monkeys and The Killers in the last pair before the casino.
        void ShapeRow(IslandWorldData data, int index)
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
            var bestLen = 0f; var bestDir = Vector2.up;
            for (var k = 0; k < 36; k++)
            {
                var a = k * Mathf.PI * 2f / 36f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var len = 0f;
                for (var s = 10f; s <= 440f; s += 10f) { if (!Clear(heart + d * s, 55f)) break; len = s; }
                if (len > bestLen) { bestLen = len; bestDir = d; }
            }
            if (bestLen < 150f) { Debug.LogWarning("Garage Rock Row: no room for the street"); return; }
            hasStreet = true;
            streetDir = new Vector3(bestDir.x, 0, bestDir.y); streetA = new Vector3(heart.x, 0, heart.y); streetLen = bestLen;
            // the road's height: the ground along it, smoothed and eased so it never gets steep
            var n = Mathf.CeilToInt(streetLen / 8f) + 1;
            var raw = new float[n];
            for (var i = 0; i < n; i++) { var p = heart + bestDir * (i * 8f); raw[i] = data.HeightAt(p.x, p.y); }
            profile = new float[n];
            for (var i = 0; i < n; i++)
            {
                float sum = 0f; var c = 0;
                for (var j = -6; j <= 6; j++) { var q = Mathf.Clamp(i + j, 0, n - 1); sum += raw[q]; c++; }
                profile[i] = sum / c;
            }
            for (var i = 1; i < n; i++) profile[i] = Mathf.Clamp(profile[i], profile[i - 1] - .55f, profile[i - 1] + .55f);   // never steeper than about 1 in 15
            var across = new Vector2(-bestDir.y, bestDir.x);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col;
                    if (data.islandIds[i] != index) continue;
                    var x = col * data.Step; var rel = new Vector2(x, z) - heart;
                    var t = Vector2.Dot(rel, bestDir); var off = Mathf.Abs(Vector2.Dot(rel, across));
                    if (t < -40f || t > streetLen + 45f) continue;
                    var tc = Mathf.Clamp(t, 0f, streetLen);
                    var endFall = t < 0f ? -t : t > streetLen ? t - streetLen : 0f;
                    var d = Mathf.Max(off, endFall);
                    if (d > 60f) continue;
                    var y = StreetY(tc);
                    data.heights[i] = d < 36f ? y : Mathf.Lerp(y, data.heights[i], Mathf.SmoothStep(0f, 1f, (d - 36f) / 24f));
                }
            }
            // the garages, in pairs down the street, the biggest names last (nearest the casino)
            var arts = new List<IslandWorldData.Artist>();
            foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) arts.Add(a);
            arts.Sort((p, q) => Rank(q).CompareTo(Rank(p)) != 0 ? Rank(q).CompareTo(Rank(p)) : string.CompareOrdinal(p.name, q.name));
            // arts[0] and [1] go last (nearest the casino), the rest fill back toward where you arrive
            var pairs = Mathf.CeilToInt(arts.Count / 2f);
            var first = 45f; var last = streetLen - 70f; var step = pairs > 1 ? (last - first) / (pairs - 1) : 0f;
            slots.Clear();
            for (var i = 0; i < arts.Count; i++)
            {
                var pair = pairs - 1 - i / 2; var side = i % 2 == 0 ? 1 : -1;
                var t = first + pair * step + (side > 0 ? 0f : step * .35f);
                var p = heart + bestDir * t + across * side * Setback;
                arts[i].x = p.x; arts[i].z = p.y;
                arts[i].r = Mathf.Min(arts[i].r, 19f);
            }
            SmoothSuburbs(data, index, heart, bestDir);
        }

        static int Rank(IslandWorldData.Artist a) => a.name == "Arctic Monkeys" ? 100 : a.name == "The Killers" ? 99 : a.songs * 10 + (HasStation(a) ? 5 : 0);

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Garage life").AddComponent<GarageLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            if (hasStreet)
            {
                Street(ctx, mk, solid);
                Town(ctx, mk, solid);
                Casino(ctx, mk, solid);
            }
            SongPlaces(ctx, mk, solid);
            Suburbs(ctx, mk, solid);
            RowPlaces(ctx, mk, solid);
            Outskirts(ctx, mk, solid);
            mk.Build("The Row", ctx.Root, false);
            solid.BuildCollider("The Row walls", ctx.Root);
        }

        public override void Finish(LandContext ctx)
        {
            GroundCover(ctx);
            halos.Build("Row glow", ctx.Root);
        }

        bool OnTown(float x, float z, float pad)
        {
            if (!hasStreet) return false;
            var rel = new Vector3(x - streetA.x, 0, z - streetA.z);
            var t = Vector3.Dot(rel, streetDir); var off = Mathf.Abs(Vector3.Dot(rel, Vector3.Cross(Vector3.up, streetDir)));
            return t > -30f - pad && t < streetLen + 40f + pad && off < 40f + pad;
        }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        // ---------------------------------------------------------------- the street
        /// The road (wet, with puddles and a dashed centre line), kerbs and pavements both sides, sodium lamps,
        /// and a crossing at the end where you arrive.
        void Street(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var side = Vector3.Cross(Vector3.up, streetDir);
            var pave = WorldMaterials.Solid(new Color(.33f, .32f, .33f));
            var line = WorldMaterials.Solid(new Color(.9f, .8f, .4f));
            var puddle = WorldMaterials.Solid(new Color(.1f, .1f, .14f), .98f);
            const float seg = 4f;
            for (var t = -20f; t < streetLen + 20f; t += seg)
            {
                Vector3 S(float tt, float a) { var p = OnStreet(Mathf.Clamp(tt, 0f, streetLen), a); p += streetDir * (tt - Mathf.Clamp(tt, 0f, streetLen)); return p; }
                var a0 = S(t, -StreetHalf); var a1 = S(t + seg, -StreetHalf); var b0 = S(t, StreetHalf); var b1 = S(t + seg, StreetHalf);
                mk.Quad(asphalt, a0 + Vector3.up * .06f, b0 + Vector3.up * .06f, b1 + Vector3.up * .06f, a1 + Vector3.up * .06f, (a0 + b1) / 2f - Vector3.up);
                if (Mathf.Repeat(t, 8f) < .1f) mk.Box(line, S(t + 1.5f, 0) + Vector3.up * .075f, new Vector3(.18f, .02f, 3f), Quaternion.LookRotation(streetDir));
                foreach (var s in new[] { -1f, 1f })
                {
                    var c0 = S(t, s * (StreetHalf + Walk / 2f)); var c1 = S(t + seg, s * (StreetHalf + Walk / 2f));
                    var mid = (c0 + c1) / 2f + Vector3.up * .12f;
                    mk.Box(pave, mid, new Vector3(Walk, .24f, Vector3.Distance(c0, c1) + .05f), Quaternion.LookRotation(c1 - c0));
                    mk.Box(curb, S(t + seg / 2f, s * (StreetHalf + .1f)) + Vector3.up * .13f, new Vector3(.25f, .26f, seg + .02f), Quaternion.LookRotation(c1 - c0));
                }
                // puddles here and there
                if (ctx.Chance(.35f)) mk.Prism(puddle, S(t + ctx.Range(0f, seg), ctx.Range(-StreetHalf + 1f, StreetHalf - 1f)) + Vector3.up * .07f, Vector3.up, ctx.Range(.6f, 1.8f), .01f, 10, 1f, ctx.Range(0, 40f));
            }
            // sodium street lamps both sides
            for (var t = 10f; t < streetLen; t += 26f)
                foreach (var s in new[] { -1f, 1f })
                {
                    var p = OnStreet(t + (s > 0 ? 0f : 13f), s * (StreetHalf + .6f));
                    mk.Prism(metal, p, Vector3.up, .1f, 7.5f, 6);
                    var head = p + Vector3.up * 7.4f - side * s * 1.4f;
                    mk.Box(metal, p + Vector3.up * 7.4f - side * s * .7f, new Vector3(.1f, .1f, 1.5f), Quaternion.LookRotation(side));
                    mk.Box(GlowM(Sodium, 2.6f), head - Vector3.up * .1f, new Vector3(.6f, .12f, .3f), Quaternion.LookRotation(streetDir));
                    halos.Add(head - Vector3.up * .5f, 2.2f, Sodium, .9f);
                    ArtistBits.PointLight(ctx.Root, head - Vector3.up * .6f, Sodium, 20f, 2.4f);                 // every lamp lights the street
                    ArtistBits.PostCollider(ctx.Root, p, 7f, .15f);
                }
            // a zebra crossing and a stop line where you arrive
            for (var k = 0; k < 7; k++) mk.Box(WorldMaterials.Solid(new Color(.85f, .85f, .82f)), OnStreet(6f, (k - 3f) * 1.6f) + Vector3.up * .075f, new Vector3(.8f, .02f, 3f), Quaternion.LookRotation(streetDir));
            life.streetA = streetA; life.streetDir = streetDir; life.streetLen = streetLen; life.profile = profile;
        }
    }
}
