using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Glitterlands (WORLD_GUIDE.md): a glitter disco in the clouds at dusk. The ground is a soft land of pink and
    /// lilac cloud; light-up dance floors sit on it and float above it; mirror balls hang everywhere; a lilac sky with a
    /// pink horizon; glitter always drifting. You arrive under the Mirrorball, a disco ball as big as a house turning
    /// over a floor of light. The Glitter Mile runs from it to the Sky Club. Dancing on your own, dancing with everyone.
    /// Laid out on purpose (the build-land standard): GlitTown.cs has the places and the people.
    public partial class GlitKit : LandKit
    {
        public override string Land => "The Glitterlands";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Pink = new Color(1f, .56f, .8f), Lilac = new Color(.72f, .6f, 1f), Silver = new Color(.86f, .88f, .96f), Ink = new Color(.14f, .1f, .22f), Cream = new Color(.98f, .94f, .96f), Lime = new Color(.6f, 1f, .3f), Gold = new Color(1f, .82f, .4f), Aqua = new Color(.4f, .9f, 1f);
        static readonly Color[] Disco = { new Color(1f, .4f, .74f), new Color(.6f, .5f, 1f), new Color(.4f, .86f, 1f), new Color(1f, .84f, .4f), new Color(.98f, .7f, .9f), new Color(.5f, 1f, .7f) };
        const float PathW = 3f, MileW = 6f;

        Material cloudM, cloudPink, floorM, chrome, mirror, white, dark, iron, glassM, lampM, crystal;
        readonly Material[] tileM = new Material[4];
        GlowHalos halos; Vector3 center, D, S; GlitLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            cloudM = WorldMaterials.Glow(new Color(.9f, .84f, .96f), .35f); cloudPink = WorldMaterials.Glow(new Color(1f, .78f, .9f), .35f); floorM = WorldMaterials.Glow(new Color(.3f, .26f, .4f), .2f); chrome = WorldMaterials.Solid(new Color(.82f, .84f, .92f), .95f); white = WorldMaterials.Glow(Cream, .2f); dark = WorldMaterials.Solid(new Color(.1f, .08f, .14f)); iron = WorldMaterials.Solid(new Color(.3f, .3f, .38f), .7f); glassM = WorldMaterials.Solid(new Color(.7f, .8f, 1f), .9f);
            mirror = new Material(WorldMaterials.Glow(Silver, 1.1f)) { name = "Glitter mirrors" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .9f, .96f), 1.4f)) { name = "Glitter lamps" }; crystal = new Material(WorldMaterials.Glow(Lilac, .9f)) { name = "Glitter crystal" };
            for (var i = 0; i < 4; i++) tileM[i] = new Material(WorldMaterials.Glow(Disco[i], 1.1f)) { name = "Glitter tiles " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .2f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Carly Rae Jepsen" ? 14f : a.name == "Charli xcx" || a.name == "Magdalena Bay" ? 11f : 8.5f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // cloud: lilac and pink, brighter on the tops
            color = Color.Lerp(new Color(.54f, .5f, .7f), new Color(.74f, .64f, .8f), Mathf.SmoothStep(0f, 1f, patch)); if (slope > 26f) color *= .86f;
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float d, float s) => heart2 + D2 * d + S2 * s;
        Vector3 Wp(float d, float s) => center + D * d + S * s;
        Vector3 G(LandContext ctx, float d, float s) { var p = Wp(d, s); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 140f && Inside(heart2 + dir * (len + 10f), 50f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var club = P2(streetL + 16f, 0);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Carly Rae Jepsen", -6f, 48f, 28f); Put("Charli xcx", 6f, -46f, 24f); Put("Magdalena Bay", 70f, 40f, 24f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 12f, heart2.y + D2.y * d)); avoid.Add(new Vector3(club.x, 26f, club.y));
            ArtistPlacement.Spread(data, index, 54f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY); Level(data, index, club, 20f, 36f, Mathf.Max(data.HeightAt(club.x, club.y), waterY + 4f));
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend, float level)
        {
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Ease(0f, 1f, (d - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Glitter life").AddComponent<GlitLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.tiles = tileM; life.mirror = mirror; life.lamps = lampM; life.crystal = crystal;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Mirrorball(ctx, mk, solid);
            GlitterMile(ctx, mk, solid);
            SkyClub(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Glitterlands", ctx.Root, false);
            solid.BuildCollider("The Glitterlands walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Glitter glow", ctx.Root); }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f); var n = Mathf.CeilToInt(within / 8f);
            for (var i = -n; i <= n; i++)
            for (var j = -n; j <= n; j++)
                if (pathCells.TryGetValue((cx + i, cz + j), out var l))
                    foreach (var p in l) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        bool LaneNear(float x, float z, float within) { foreach (var (p, w) in walkPoints) { if (w < 10) continue; float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; } return false; }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool InFootprint(LandContext ctx, float x, float z, float pad)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            var n = pts.Count; var L = new Vector3[n]; var Rr = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] - side * (w / 2f * miter); Rr[i] = pts[i] + side * (w / 2f * miter);
                float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(Rr[i].x, Rr[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f);
                L[i].y = gl + bulge + lift; Rr[i].y = gr + bulge + lift;
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], Rr[i], Rr[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A bank of cloud: soft lumps, pink and lilac.
        void CloudBank(MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var n = 4 + rng.Next(4); for (var k = 0; k < n; k++) mk.Blob(k % 3 == 0 ? cloudPink : cloudM, g + up * (.4f + (float)rng.NextDouble() * .8f) * s + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * 3f * s, new Vector3(1.4f, .9f, 1.3f) * s * (.7f + (float)rng.NextDouble() * .6f), rng, .1f);
            noGrass.Add(new Vector3(g.x, 1.8f * s, g.z));
        }

        /// A mirror ball on a curved post, with a spotlight on it.
        void BallPost(LandContext ctx, MeshKit mk, Vector3 g, Vector3 toward, float s = 1f, bool light = false)
        {
            var up = Vector3.up; toward.y = 0; toward.Normalize(); mk.Prism(chrome, g - up * .2f, up, .07f, 4f * s, 6, .7f); mk.Prism(chrome, g + up * 3.8f * s, (toward + up * .4f).normalized, .05f, 1f * s, 4); var b = g + up * (3.8f * s - .2f) + toward * .9f * s; mk.Prism(iron, b + up * .4f * s, -up, .01f, .3f * s, 3); Ball(mk, b, .3f * s);
            halos.Add(b, 1.4f * s, Silver, .4f); ArtistBits.PostCollider(ctx.Root, g, 3.8f * s, .09f); if (light) ArtistBits.PointLight(ctx.Root, b - up * .8f, new Color(.96f, .86f, 1f), 13f, 2f);
        }

        /// A mirror ball: a sphere of little square mirrors.
        void Ball(MeshKit mk, Vector3 c, float r)
        {
            var bands = Mathf.Clamp(Mathf.RoundToInt(r * 8f), 5, 14);
            for (var i = 1; i < bands; i++) { var lat = Mathf.PI * i / bands - Mathf.PI / 2f; var ring = Mathf.Max(3, Mathf.RoundToInt(Mathf.Cos(lat) * bands * 2f)); for (var j = 0; j < ring; j++) { var lon = j * Mathf.PI * 2f / ring; var n = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon)); var sz = r * Mathf.PI / bands * .9f; mk.Box((i + j) % 5 == 0 ? lampM : mirror, c + n * r, new Vector3(sz, sz, r * .06f), Quaternion.LookRotation(n)); } }
            mk.Blob(dark, c, Vector3.one * r * .96f, new System.Random(1), 0f);
        }

        /// A light-up dance floor: a square of glowing tiles that pulse with the music.
        void DanceFloor(MeshKit mk, MeshKit solid, Vector3 g, Quaternion rot, int nx, int nz, float tile = 1.2f, float rise = .3f)
        {
            var up = Vector3.up; var w = nx * tile; var dp = nz * tile; mk.Box(floorM, g + up * (rise / 2f), new Vector3(w + .4f, rise, dp + .4f), rot); solid.Box(floorM, g + up * (rise / 2f - .2f), new Vector3(w + .4f, rise + .4f, dp + .4f), rot);
            for (var x = 0; x < nx; x++) for (var z = 0; z < nz; z++) mk.Box(tileM[(x * 3 + z * 7) % 4], g + up * (rise + .01f) + rot * new Vector3(-w / 2f + (x + .5f) * tile, 0, -dp / 2f + (z + .5f) * tile), new Vector3(tile * .92f, .02f, tile * .92f), rot);
        }

        /// A chair to sit on.
        void Chair(MeshKit mk, Vector3 g, Vector3 face, string verb = "Sit")
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(chrome, g + up * .45f, new Vector3(.46f, .05f, .46f), rot); mk.Box(Paint(Pink), g + up * .8f - face * .21f, new Vector3(.46f, .7f, .05f), rot); mk.Prism(chrome, g, Vector3.up, .04f, .44f, 5);
            Interact.Seat(tlife.transform, g + up * .5f, face, verb);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var n = 0;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort; if (end < PathW) return;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 16f) * Mathf.Min(2f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, floorM, PathW, .13f, ctx); for (var i = 1; i + 1 < pts.Count; i += 2) Strip(mk, new List<Vector3> { pts[i], pts[i + 1] }, tileM[(i / 2) % 4], PathW - .8f, .15f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .7f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    BallPost(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s, 1f, n % 3 == 0);
                }
                walk++;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- the cloudscape
        /// Cloud banks, little dance floors with a mirror ball over them, crystal arches, heart balloons, floating
        /// floors up in the air, velvet sofas.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int clouds = 0, floors = 0, arches = 0, balloons = 0, floaters = 0, sofas = 0; var rng = new System.Random(2007);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var up = Vector3.up;
                if (roll < .36f) { CloudBank(mk, p, .8f + s * 1.2f, rng); clouds++; }
                else if (roll < .46f) { if (!Open(ctx, px, pz, 6f) || Mathf.Abs(ctx.Ground(px + 3f, pz) - ctx.Ground(px - 3f, pz)) > 1.2f) continue; DanceFloor(mk, solid, p, Quaternion.LookRotation(f), 4, 4); BallPost(ctx, mk, p - f * 3f, f, 1.1f, floors % 2 == 0); noGrass.Add(new Vector3(p.x, 4f, p.z)); floors++; }
                else if (roll < .52f) { var side = Vector3.Cross(up, f); for (var k = 0; k <= 10; k++) { var a = Mathf.PI * k / 10f; mk.Box(crystal, p + up * Mathf.Sin(a) * 3.4f + side * Mathf.Cos(a) * 2.4f, new Vector3(.4f, .8f, .4f), Quaternion.LookRotation(f) * Quaternion.Euler(0, 0, -Mathf.Cos(a) * 60f + 45f)); } foreach (var e in new[] { -2.4f, 2.4f }) ArtistBits.PostCollider(ctx.Root, p + side * e, 1.4f, .3f); halos.Add(p + up * 3f, 2f, Lilac, .2f); arches++; }
                else if (roll < .58f) { for (var k = 0; k < 3; k++) { var bp = p + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .4f; var h = 2.6f + k * .5f; mk.Prism(white, bp, up, .006f, h, 3); Heart(mk, Paint(Disco[(balloons + k) % Disco.Length]), bp + up * (h + .4f), f, .45f); } balloons++; }
                else if (roll < .63f) { var fh = 6f + s * 6f; var fp = p + up * fh; DanceFloor(mk, solid, fp, Quaternion.LookRotation(f), 3, 3, 1.2f, .3f); CloudBank(mk, fp - up * 1.2f, 1.1f, rng); Ball(mk, fp + up * 3f, .5f); halos.Add(fp + up * 3f, 2f, Silver, .3f); floaters++; }
                else if (roll < .69f) { var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(up, f); var m = Paint(new Color(.6f, .3f, .7f)); mk.Box(m, p + up * .3f, new Vector3(2.2f, .4f, .9f), rot); mk.Box(m, p + up * .7f - f * .36f, new Vector3(2.2f, .6f, .2f), rot); BoxCol(ctx.Root, p + up * .45f, new Vector3(2.2f, .9f, .9f), rot); Interact.Bench(tlife.transform, p + up * .52f - side * .5f, p + up * .52f + side * .5f, f); BallPost(ctx, mk, p - f * 1.2f + side * 1.6f, f, .9f, sofas % 2 == 0); noGrass.Add(new Vector3(p.x, 2f, p.z)); sofas++; }
            }
            Debug.Log($"The Glitterlands: Glitter Mile {streetL:0} m, {clouds} cloud banks, {floors} dance floors, {floaters} floating floors, {arches} crystal arches, {balloons} heart balloons, {sofas} sofas");
        }

        /// A flat heart shape facing 'face'.
        void Heart(MeshKit mk, Material m, Vector3 c, Vector3 face, float s)
        {
            face.y = 0; face.Normalize(); var side = Vector3.Cross(Vector3.up, face);
            Vector3 H(float t) => c + side * 16f * Mathf.Pow(Mathf.Sin(t), 3f) * s / 16f + Vector3.up * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) * s / 16f;
            for (var k = 0; k < 16; k++) { var t0 = k * Mathf.PI * 2f / 16f; var t1 = (k + 1) * Mathf.PI * 2f / 16f; mk.Tri(m, c, H(t0), H(t1), c - face); mk.Tri(m, c, H(t0), H(t1), c + face); }
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Glitter grass: short silvery tufts with sparkles.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.8f, .72f, .9f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2008);
            const float st = 2.8f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .48f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.74f, .66f, .9f), new Color(.92f, .76f, .9f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .2f + (float)rng.NextDouble() * .3f, col, rng.NextDouble() < .2 ? Silver : Color.Lerp(col, Cream, .4f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Glitter grass", ctx.Root, mat);
        }
    }
}
