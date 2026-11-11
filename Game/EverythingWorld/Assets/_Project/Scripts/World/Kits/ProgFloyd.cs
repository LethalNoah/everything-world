using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Floyd world (part of ProgKit): Pink Floyd is the heart of the Prog Marches, so besides the Prism and the
    /// band's own area, every Floyd song on the playlist is a place you can walk to in the marches round it, each
    /// with a stone sign and a glow that wakes while its song plays:
    /// - "Time": the hall of clocks, clocks of every kind on posts, all their hands spinning when it plays.
    /// - "Comfortably Numb": the wall, a long white brick wall with a hole knocked in it, a lone guitarist on top
    ///   under a spotlight that blazes during the song.
    /// - "Sheep": a stone pen of grazing sheep whose eyes glow red.
    /// - "Have a Cigar": an office out in the marsh, a desk and chair on a red rug, a gold record on an easel, a
    ///   giant cigar smoking in an ashtray.
    /// - "Astronomy Domine": a brass orrery, planets wheeling round a glowing sun.
    /// - "Green Is the Colour": a glade of green glowing flowers round a little pool.
    /// - "Fearless": a rocky tor with steps spiralling up to a lantern on the summit.
    /// - "Wish You Were Here": a giant glass fishbowl with two glowing fish swimming round in it.
    /// Also here: the see-through glass of the Prism and the spectrum as fading ribbons of light.
    public partial class ProgKit
    {
        public const string ComfortablyNumb = "5HNCy40Ni5BZJFw1TKzRsC", SheepSong = "55e99bv2wlgTYtNEYHWtN1", HaveACigar = "3CmHvyZQQAGkKkTjTBFWN6",
            AstronomyDomine = "2E3WHJXbZw6cAIK7Z1zDef", GreenIsTheColour = "7DDvRmOSP2Yx28CfL6W7aw", Fearless = "7AalBKBoLDR4UmRYRJpdbj";

        /// A glow material that brightens and breathes while its song plays (ProgLife).
        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        static Material SeeThroughM(Color col, bool additive)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = additive ? "Prog light" : "Prog glass" };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", col); return m;
        }

        /// A mesh drawn with a see-through material, with vertex colours (the particle shader needs them).
        static Renderer FadeMesh(Transform parent, string name, List<Vector3> v, List<Color> col, List<int> tri, Material m)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return r;
        }

        /// A band of light from a to b, two crossed sheets, full at a and fading to nothing at b.
        static void Ribbon(Transform parent, string name, Vector3 a, Vector3 b, float w, Material m)
        {
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            var dir = (b - a).normalized; var flat = Vector3.Cross(Vector3.up, dir).normalized; var tall = Vector3.Cross(dir, flat).normalized;
            foreach (var across in new[] { flat, tall })
            {
                const int Steps = 10; var b0 = v.Count;
                for (var i = 0; i <= Steps; i++)
                {
                    var f = i / (float)Steps; var p = Vector3.Lerp(a, b, f); var alpha = Mathf.SmoothStep(0f, 1f, f / .04f) * Mathf.Pow(1f - f, 1.3f);
                    v.Add(p - across * w / 2f); v.Add(p + across * w / 2f); col.Add(new Color(1, 1, 1, alpha)); col.Add(new Color(1, 1, 1, alpha));
                    if (i > 0) tri.AddRange(new[] { b0 + 2 * i - 2, b0 + 2 * i - 1, b0 + 2 * i + 1, b0 + 2 * i - 2, b0 + 2 * i + 1, b0 + 2 * i });
                }
            }
            FadeMesh(parent, name, v, col, tri, m);
        }

        // ---------------------------------------------------------------- the Floyd world
        readonly List<Vector3> placeSpots = new List<Vector3>();

        /// An open spot for a place of radius 'clear', preferably within reach of 'near' (Floyd's area).
        Vector3 PlaceSpot(LandContext ctx, Vector3 near, float clear)
        {
            for (var tries = 0; tries < 900; tries++)
            {
                var wide = tries > 500; var a = ctx.Range(0f, Mathf.PI * 2f); var r = ctx.Range(30f, wide ? 260f : 170f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(px, pz, clear + 8f) || !Open(ctx, px, pz, clear)) continue;
                if (Vector2.Distance(new Vector2(px, pz), new Vector2(center.x, center.z)) > 270f) continue;
                if (!Gentle(ctx, px, pz, clear + 6f, tries > 700 ? clear * .35f : clear * .18f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 26f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); noGrass.Add(new Vector3(p.x, clear * .85f, p.z)); return p;
            }
            return Vector3.positiveInfinity;
        }

        /// Whether the ground within r of (x, z) rises or falls less than 'drop'.
        bool Gentle(LandContext ctx, float x, float z, float r, float drop)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var k = 0; k < 12; k++) foreach (var f in new[] { .5f, 1f }) { var a = k * Mathf.PI / 6f; var h = ctx.Ground(x + Mathf.Cos(a) * r * f, z + Mathf.Sin(a) * r * f); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo < drop;
        }

        /// A trail from the nearest walk to the place, a sign beside its end, and the camera mark for the photo tour.
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
                for (var d = TrailW / 2f + .1f; ; d += 1.5f) { var dd = Mathf.Min(d, len); var q = from + dir * dd; if (!ctx.IsLand(q.x, q.z, 0f)) break; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count > 1) { Strip(mk, pts, WorldMaterials.Solid(new Color(.5f, .46f, .42f)), TrailW, .192f, ctx); foreach (var p in pts) Remember(p, 350); }
            }
            var side = Vector3.Cross(Vector3.up, face); var sp = ctx.OnGround(c.x + face.x * stop + side.x * 2.6f, c.z + face.z * stop + side.z * 2.6f); var rot = Quaternion.LookRotation(face);
            mk.Box(darkStone, sp + Vector3.up * .7f, new Vector3(.25f, 1.4f, .25f), rot); mk.Box(darkStone, sp + Vector3.up * 1.6f, new Vector3(2.6f, .6f, .12f), rot);
            mk.Box(GlowM(Violet, 2f), sp + Vector3.up * 1.27f + face * .07f, new Vector3(2.4f, .04f, .02f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 1.6f + face * .07f, face, .2f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PostCollider(ctx.Root, sp, 1.9f, .2f);
            Lamp(ctx, mk, ctx.OnGround(c.x + face.x * stop - side.x * 2.6f, c.z + face.z * stop - side.z * 2.6f));
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 9f) + side * 4f; cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.5f; var look = c + Vector3.up * 2.5f - cam - Vector3.up * 1.6f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            return face;
        }

        void FloydWorld(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var floyd = ctx.Artists.Find(a => a.name == "Pink Floyd"); var near = floyd != null ? ctx.OnGround(floyd.x, floyd.z) : center;
            placeSpots.Clear(); foreach (var a in ctx.Artists) placeSpots.Add(ctx.OnGround(a.x, a.z));
            void Place(float clear, string sign, string mark, System.Action<Vector3, Vector3> build)
            {
                var c = PlaceSpot(ctx, near, clear); if (float.IsInfinity(c.x)) { Debug.Log("The Prog Marches: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clear, sign, mark));
            }
            Place(12f, "TIME", "the hall of clocks", (c, f) => Clocks(ctx, mk, solid, c, f));
            Place(17f, "COMFORTABLY NUMB", "the wall", (c, f) => Wall(ctx, mk, solid, c, f));
            Place(11f, "SHEEP", "the sheep pen", (c, f) => SheepPen(ctx, mk, solid, c, f));
            Place(9f, "HAVE A CIGAR", "the cigar office", (c, f) => Cigar(ctx, mk, solid, c, f));
            Place(9f, "ASTRONOMY DOMINE", "the orrery", (c, f) => Orrery(ctx, mk, solid, c, f));
            Place(9f, "GREEN IS THE COLOUR", "the green glade", (c, f) => Glade(ctx, mk, solid, c, f));
            Place(10f, "FEARLESS", "the tor", (c, f) => Tor(ctx, mk, solid, c, f));
            Place(8f, "WISH YOU WERE HERE", "the fishbowl", (c, f) => Fishbowl(ctx, mk, solid, c, f));
        }

        /// The hall of clocks: clocks of every kind on posts and stands, a grandfather clock in the middle.
        void Clocks(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var faceM = WorldMaterials.Solid(new Color(.95f, .92f, .82f)); var glow = SongGlow(TimeSong, Amber, 1.4f);
            for (var k = 0; k < 13; k++)
            {
                var a = k * 27.7f * Mathf.Deg2Rad; var r = 3.5f + (k % 3) * 2.6f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r);
                var h = 1.6f + (k * 7 % 5) * .55f; var size = .45f + (k * 3 % 4) * .18f; var dir = Quaternion.Euler(0, ctx.Range(-40f, 40f), 0) * face; var rot = Quaternion.LookRotation(dir);
                mk.Prism(k % 2 == 0 ? brass : darkStone, p - up * .2f, up, .07f, h, 6); ArtistBits.PostCollider(ctx.Root, p, h, .12f);
                var fc = p + up * (h + size); mk.Prism(k % 3 == 0 ? brass : chrome, fc - dir * .08f, dir, size + .08f, .12f, 20); mk.Prism(faceM, fc + dir * .04f, dir, size, .02f, 20);
                mk.Prism(glow, fc - dir * .1f, -dir, size * .5f, .02f, 12);
                if (k % 4 == 1) foreach (var s in new[] { -1f, 1f }) mk.Blob(brass, fc + up * (size + .08f) + Vector3.Cross(up, dir) * s * size * .6f, new Vector3(.18f, .14f, .18f) * (size + .4f), ctx.Rng, 0f);
                for (var hh = 0; hh < 12; hh += 3) mk.Box(dark, fc + dir * .06f + Quaternion.AngleAxis(hh * 30f, dir) * up * size * .82f, new Vector3(.05f, size * .2f, .01f), Quaternion.AngleAxis(hh * 30f, dir) * rot);
                foreach (var (len, sp) in new[] { (.8f, 1f), (.55f, 1f / 12f) })
                {
                    var hk = new MeshKit(); hk.Box(dark, new Vector3(0, size * len / 2f, 0), new Vector3(.05f, size * len, .02f), Quaternion.identity);
                    var tr = hk.BuildSingle("Clock hand", life.transform, false).transform; tr.SetPositionAndRotation(fc + dir * .07f + dir * (sp < 1f ? .01f : .02f), rot * Quaternion.Euler(0, 0, ctx.Range(0f, 360f)));
                    life.clockHands.Add((tr, sp * 6f));
                }
            }
            // the grandfather clock
            var g = c; var grot = Quaternion.LookRotation(face);
            mk.Box(wood, g + up * 1.6f, new Vector3(1.1f, 3.2f, .7f), grot); mk.Box(wood, g + up * 3.6f, new Vector3(1.3f, .8f, .8f), grot); solid.Box(wood, g + up * 2f, new Vector3(1.3f, 4f, .8f), grot);
            mk.Prism(faceM, g + up * 3.5f + face * .41f, face, .5f, .02f, 20); mk.Box(glow, g + up * 1.8f + face * .36f, new Vector3(.6f, 1.6f, .02f), grot);
            var pend = new MeshKit(); pend.Box(brass, new Vector3(0, -.6f, 0), new Vector3(.04f, 1.2f, .02f), Quaternion.identity); pend.Prism(brass, new Vector3(0, -1.25f, -.02f), Vector3.forward, .16f, .04f, 14);
            var pt = pend.BuildSingle("Pendulum", life.transform, false).transform; pt.SetPositionAndRotation(g + up * 2.9f + face * .38f, grot); life.pendulum = pt; life.pendulumRot = grot;
            halos.Add(c + up * 2.5f, 6f, Amber, .25f);
            ArtistBits.PointLight(ctx.Root, c + up * 4f + face * 3f, Amber, 16f, 1.4f);
        }

        /// The wall: a long white brick wall with a hole knocked in it, bricks scattered, a guitarist on top in a
        /// spotlight.
        void Wall(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var white = WorldMaterials.Solid(new Color(.94f, .94f, .93f)); var mortar = WorldMaterials.Solid(new Color(.55f, .55f, .56f));
            const float L = 26f, H = 6.5f, T = 1.2f, hole0 = 3f, hole1 = 6.4f, holeTop = 4f;
            var b = c - face * 3f; b.y = ctx.Ground(b.x, b.z) - .3f;
            void Block(float x0, float x1, float y0, float y1)
            {
                var m = b + side * ((x0 + x1) / 2f) + up * ((y0 + y1) / 2f); var size = new Vector3(x1 - x0, y1 - y0, T);
                mk.Box(white, m, size, rot); solid.Box(white, m, size, rot);
            }
            Block(-L / 2f, hole0, 0, H + .3f); Block(hole1, L / 2f, 0, H + .3f); Block(hole0, hole1, holeTop, H + .3f);
            foreach (var s in new[] { 1f, -1f })
                for (var r = 1; r < 14; r++)
                {
                    var y = r * .48f; var off = face * s * (T / 2f + .01f);
                    if (y < holeTop) { mk.Box(mortar, b + side * ((-L / 2f + hole0) / 2f) + up * y + off, new Vector3(hole0 + L / 2f, .05f, .02f), rot); mk.Box(mortar, b + side * ((hole1 + L / 2f) / 2f) + up * y + off, new Vector3(L / 2f - hole1, .05f, .02f), rot); }
                    else mk.Box(mortar, b + up * y + off, new Vector3(L, .05f, .02f), rot);
                    for (var k = 0; k < 20; k++)
                    {
                        var x = -L / 2f + .65f + k * 1.3f + (r % 2) * .65f; if (x > L / 2f - .1f) continue; var yc = y - .24f;
                        if (yc < holeTop && x > hole0 - .05f && x < hole1 + .05f) continue;
                        mk.Box(mortar, b + side * x + up * yc + off, new Vector3(.05f, .48f, .02f), rot);
                    }
                }
            // fallen bricks in front of the hole
            for (var k = 0; k < 14; k++) { var bp = ctx.OnGround(b.x + side.x * ctx.Range(hole0, hole1) + face.x * ctx.Range(1f, 4f), b.z + side.z * ctx.Range(hole0, hole1) + face.z * ctx.Range(1f, 4f)); mk.Box(white, bp + up * .12f, new Vector3(1.2f, .45f, .6f), Quaternion.Euler(ctx.Range(-15f, 15f), ctx.Range(0f, 180f), ctx.Range(-15f, 15f))); }
            // the guitarist on top, in the spotlight
            var g = b + up * (H + .3f) - side * 4f; var suit = WorldMaterials.Solid(new Color(.1f, .1f, .12f));
            foreach (var s in new[] { -.12f, .12f }) mk.Box(suit, g + side * s + up * .45f, new Vector3(.16f, .9f, .18f), rot);
            mk.Box(suit, g + up * 1.25f, new Vector3(.48f, .7f, .26f), rot); mk.Blob(WorldMaterials.Solid(new Color(.8f, .66f, .55f)), g + up * 1.75f, new Vector3(.12f, .14f, .12f), ctx.Rng, 0f);
            var gtr = SongGlow(ComfortablyNumb, new Color(.95f, .95f, 1f), 1.2f);
            mk.Box(gtr, g + up * 1.05f + face * .2f, new Vector3(.5f, .35f, .08f), rot * Quaternion.Euler(0, 0, -25f)); mk.Box(dark, g + up * 1.3f + face * .2f + side * .45f, new Vector3(.75f, .06f, .05f), rot * Quaternion.Euler(0, 0, 25f));
            var spot = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>(); var top = g + up * 22f + face * 3f; var foot = g + up * .1f;
            const int N = 16; spot.Add(top); col.Add(new Color(1, 1, 1, .02f));
            for (var k = 0; k < N; k++) { var a = k * Mathf.PI * 2f / N; spot.Add(foot + (side * Mathf.Cos(a) + face * Mathf.Sin(a)) * 1.6f); col.Add(new Color(1, 1, 1, .7f)); tri.AddRange(new[] { 0, 1 + k, 1 + (k + 1) % N }); }
            var sm = SeeThroughM(new Color(1f, 1f, 1f, .15f), true); FadeMesh(ctx.Root, "Wall spotlight", spot, col, tri, sm); life.spotlight = sm;
            mk.Prism(GlowM(new Color(1f, 1f, .95f), 1.2f), g + up * .02f, up, 1.5f, .02f, 16);
            ArtistBits.PointLight(ctx.Root, g + up * 3f + face * 2f, new Color(.95f, .95f, 1f), 14f, 1.6f);
            ArtistBits.PointLight(ctx.Root, b + face * 6f + up * 3f, new Color(.85f, .85f, 1f), 18f, 1.2f);
        }

        /// The sheep pen: a low dry-stone wall round a flock of grazing sheep, their eyes glowing red.
        void SheepPen(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; const float R = 9f; var wool = WorldMaterials.Solid(new Color(.93f, .92f, .88f)); var black = WorldMaterials.Solid(new Color(.1f, .1f, .11f)); var eyes = SongGlow(SheepSong, new Color(1f, .15f, .1f), 1.2f);
            for (var k = 0; k < 28; k++)
            {
                var a0 = k * Mathf.PI * 2f / 28f; var a1 = (k + 1) * Mathf.PI * 2f / 28f; var mid = (a0 + a1) / 2f; var dir = new Vector3(Mathf.Cos(mid), 0, Mathf.Sin(mid));
                if (Vector3.Dot(dir, face) > .93f) continue;   // the gate
                var p0 = ctx.OnGround(c.x + Mathf.Cos(a0) * R, c.z + Mathf.Sin(a0) * R); var p1 = ctx.OnGround(c.x + Mathf.Cos(a1) * R, c.z + Mathf.Sin(a1) * R); var m = (p0 + p1) / 2f;
                mk.Box(stone, m + up * .45f, new Vector3(.7f, 1.1f, Vector3.Distance(p0, p1) + .2f), Quaternion.LookRotation(p1 - p0)); solid.Box(stone, m + up * .45f, new Vector3(.7f, 1.1f, Vector3.Distance(p0, p1) + .2f), Quaternion.LookRotation(p1 - p0));
            }
            for (var k = 0; k < 11; k++)
            {
                var a = k * 2.4f; var r = 2f + (k % 4) * 1.5f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r); var d = Quaternion.Euler(0, ctx.Range(0f, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(d); var sd = Vector3.Cross(up, d);
                mk.Blob(wool, p + up * .75f, new Vector3(.5f, .42f, .7f), ctx.Rng, .25f);
                var graze = k % 3 != 0; var head = p + up * (graze ? .35f : .85f) + d * .8f;
                mk.Box(black, head, new Vector3(.24f, .28f, .4f), rot * Quaternion.Euler(graze ? 50f : 0f, 0, 0));
                foreach (var s in new[] { -1f, 1f }) { mk.Box(eyes, head + sd * s * .13f + up * .06f + d * .05f, new Vector3(.03f, .06f, .06f), rot); mk.Box(black, head + sd * s * .18f + up * .12f - d * .1f, new Vector3(.16f, .05f, .08f), rot); }
                foreach (var s in new[] { -.2f, .2f }) foreach (var f in new[] { -.4f, .4f }) mk.Box(black, p + sd * s + d * f + up * .22f, new Vector3(.08f, .45f, .08f), rot);
                solid.Box(wool, p + up * .6f, new Vector3(.9f, 1.2f, 1.6f), rot);
            }
            halos.Add(c + up * 1f, 5f, new Color(1f, .3f, .2f), .15f);
            ArtistBits.PointLight(ctx.Root, c + up * 5f, new Color(1f, .85f, .7f), 16f, 1f);
        }

        /// Have a Cigar: an office out in the marsh, a big desk and a leather chair on a red rug, a gold record on an
        /// easel, a giant cigar smoking in a giant ashtray.
        void Cigar(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(WorldMaterials.Solid(new Color(.55f, .1f, .12f)), c + up * .04f, new Vector3(7f, .08f, 5f), rot);
            var desk = c - face * .5f + up * .1f; mk.Box(WorldMaterials.Solid(new Color(.35f, .2f, .12f), .7f), desk + up * .45f, new Vector3(3.2f, .9f, 1.4f), rot); solid.Box(wood, desk + up * .45f, new Vector3(3.2f, .9f, 1.4f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .3f, .2f)), desk + up * .91f, new Vector3(1.4f, .02f, .8f), rot); mk.Box(dark, desk + up * 1f + side * 1f, new Vector3(.3f, .15f, .25f), rot);
            var chair = c - face * 1.9f + up * .1f; var leather = WorldMaterials.Solid(new Color(.3f, .12f, .08f), .6f);
            mk.Box(leather, chair + up * .45f, new Vector3(1f, .3f, .9f), rot); mk.Box(leather, chair + up * 1.2f - face * .4f, new Vector3(1f, 1.3f, .25f), rot); solid.Box(leather, chair + up * .7f, new Vector3(1.1f, 1.4f, 1f), rot);
            // the gold record on an easel
            var ea = c - face * .8f + side * 3f; ea = ctx.OnGround(ea.x, ea.z);
            foreach (var s in new[] { -.5f, .5f }) mk.Box(wood, ea + side * s + up * 1f, new Vector3(.08f, 2.1f, .08f), rot * Quaternion.Euler(-10f, 0, 0)); mk.Box(wood, ea + up * .9f + face * .18f, new Vector3(1.4f, .08f, .2f), rot);
            mk.Box(dark, ea + up * 1.7f + face * .12f, new Vector3(1.5f, 1.5f, .05f), rot * Quaternion.Euler(-10f, 0, 0));
            mk.Prism(SongGlow(HaveACigar, new Color(1f, .78f, .3f), 1.6f), ea + up * 1.7f + face * .16f, rot * Quaternion.Euler(-10f, 0, 0) * Vector3.forward, .6f, .03f, 24);
            ArtistBits.PostCollider(ctx.Root, ea, 2.3f, .6f);
            // the cigar in its ashtray
            var at = c + face * 1.8f - side * 2.4f; at = ctx.OnGround(at.x, at.z);
            mk.Prism(chrome, at, up, 1.6f, .35f, 16); mk.Prism(dark, at + up * .3f, up, 1.3f, .06f, 16); solid.Prism(chrome, at, up, 1.6f, .4f, 16);
            var cdir = (side * .9f + up * .25f).normalized; var cs = at + up * .35f - cdir * 1.2f;
            mk.Prism(WorldMaterials.Solid(new Color(.45f, .28f, .15f)), cs, cdir, .22f, 3f, 10); mk.Prism(WorldMaterials.Solid(new Color(.8f, .65f, .3f)), cs + cdir * .5f, cdir, .23f, .25f, 10);
            mk.Prism(SongGlow(HaveACigar, new Color(1f, .4f, .1f), 2.4f), cs + cdir * 3f, cdir, .2f, .06f, 10);
            life.smoke.Add(cs + cdir * 3.1f);
            halos.Add(cs + cdir * 3f, .8f, new Color(1f, .45f, .15f), .7f);
            ArtistBits.PointLight(ctx.Root, c + up * 3.5f + face * 2f, new Color(1f, .8f, .55f), 12f, 1.3f);
            noGrass.Add(new Vector3(c.x, 5f, c.z));
        }

        /// Astronomy Domine: a brass orrery, five planets wheeling round a glowing sun on arms.
        void Orrery(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up;
            mk.Prism(darkStone, c - up * .3f, up, 2f, .9f, 12); mk.Prism(brass, c + up * .6f, up, .25f, 2.4f, 10); solid.Prism(darkStone, c - up * .3f, up, 2f, 3f, 12);
            var sun = c + up * 3.4f; mk.Blob(SongGlow(AstronomyDomine, new Color(1f, .85f, .4f), 2.2f), sun, Vector3.one * .9f, ctx.Rng, 0f); halos.Add(sun, 4f, new Color(1f, .8f, .4f), .5f);
            Color[] planet = { new Color(.7f, .7f, .75f), new Color(1f, .7f, .35f), new Color(.3f, .55f, 1f), new Color(1f, .35f, .25f), new Color(.85f, .75f, .55f) };
            for (var k = 0; k < 5; k++)
            {
                var r = 2.2f + k * 1.2f; var pk = new MeshKit(); var h = (k % 2) * .5f - .2f;
                pk.Box(brass, new Vector3(r / 2f, h, 0), new Vector3(r, .05f, .05f), Quaternion.identity);
                pk.Blob(GlowM(planet[k], 1.4f), new Vector3(r, h, 0), Vector3.one * (.22f + k % 3 * .12f), ctx.Rng, 0f);
                if (k == 4) pk.Prism(brass, new Vector3(r, h, 0), Vector3.up, .7f, .02f, 20);
                var tr = pk.BuildSingle("Orrery planet", life.transform, false).transform; tr.SetPositionAndRotation(sun, Quaternion.Euler(0, k * 77f, 0));
                life.planets.Add((tr, 24f / (k + 1.5f)));
            }
            ArtistBits.PointLight(ctx.Root, sun + face * 2f, new Color(1f, .85f, .5f), 14f, 1.4f);
        }

        /// Green Is the Colour: a glade of tall glowing green flowers round a little dark pool, a bench.
        void Glade(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var green = SongGlow(GreenIsTheColour, new Color(.35f, 1f, .45f), 1.6f); var stem = WorldMaterials.Solid(new Color(.25f, .45f, .25f));
            var pool = new Material(WorldMaterials.Glow(new Color(.05f, .2f, .12f), .1f)) { name = "Glade pool" }; pool.SetFloat("_Smoothness", .7f);
            for (var k = 0; k < 20; k++) { float a0 = k * Mathf.PI / 10f, a1 = (k + 1) * Mathf.PI / 10f; var w = c + up * .06f; mk.Tri(pool, w, w + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 2.6f, w + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 2.6f, w - up); }
            for (var k = 0; k < 14; k++)
            {
                var a = k * Mathf.PI * 2f / 14f; var r = 4.2f + (k % 2) * 1.6f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r); var h = 1.4f + (k % 3) * .6f;
                soft.Prism(stem, p - up * .1f, up, .04f, h, 5);
                mk.Cone(green, p + up * h, .35f, .5f, 6); mk.Blob(green, p + up * (h + .1f), Vector3.one * .16f, ctx.Rng, 0f);
                soft.Box(stem, p + up * h * .5f, new Vector3(.4f, .02f, .12f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 25f));
            }
            halos.Add(c + up * 1.5f, 6f, new Color(.35f, 1f, .45f), .25f);
            var bench = c - face * 4.6f; bench = ctx.OnGround(bench.x, bench.z); var rot = Quaternion.LookRotation(face);
            mk.Box(wood, bench + up * .45f, new Vector3(1.8f, .08f, .5f), rot); foreach (var s in new[] { -.75f, .75f }) mk.Box(darkStone, bench + Vector3.Cross(up, face) * s + up * .22f, new Vector3(.12f, .44f, .45f), rot); solid.Box(wood, bench + up * .3f, new Vector3(1.8f, .6f, .5f), rot);
            ArtistBits.PointLight(ctx.Root, c + up * 3f, new Color(.4f, 1f, .5f), 12f, 1.2f);
        }

        /// Fearless: a rocky tor with stone steps spiralling up round it to a lantern on the summit.
        void Tor(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; const float H = 8.4f, R = 4.4f;
            for (var k = 0; k < 5; k++) mk.Blob(darkStone, c + up * (1f + k * 1.6f), new Vector3(R - k * .15f, 1.3f, R - k * .2f), ctx.Rng, .2f);
            mk.Prism(stone, c + up * H, up, R - .3f, .3f, 12);
            solid.Prism(darkStone, c - up * .5f, up, R, H + .8f, 12);
            const int Steps = 26; var a0 = Mathf.Atan2(face.z, face.x);
            for (var k = 0; k < Steps; k++)
            {
                var a = a0 + k * (300f / Steps) * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var tang = Vector3.Cross(up, dir);
                var p = c + dir * (R + .8f); var g = ctx.Ground(p.x, p.z); var top = c.y + (k + 1) * (H + .3f) / Steps; var hgt = Mathf.Max(.2f, top - g + .2f);
                var sp = new Vector3(p.x, top - hgt / 2f, p.z); var rot = Quaternion.LookRotation(tang);
                mk.Box(stone, sp, new Vector3(1.8f, hgt, 1.3f), rot); solid.Box(stone, sp, new Vector3(1.8f, hgt, 1.3f), rot);
            }
            var lan = c + up * (H + .3f); mk.Prism(darkStone, lan, up, .12f, 1.6f, 6); var lg = SongGlow(Fearless, new Color(1f, .85f, .5f), 2.4f); mk.Prism(lg, lan + up * 1.6f, up, .25f, .45f, 8); mk.Prism(metal, lan + up * 2.05f, up, .3f, .1f, 8);
            halos.Add(lan + up * 1.8f, 2.4f, Amber, .7f); ArtistBits.PointLight(ctx.Root, lan + up * 2.2f, Amber, 14f, 1.4f); ArtistBits.PostCollider(ctx.Root, lan, 2.2f, .2f);
        }
        Material metal => chrome;

        /// Wish You Were Here: a giant glass fishbowl on a chrome stand, two glowing fish swimming round inside.
        void Fishbowl(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face)
        {
            var up = Vector3.up; var bc = c + up * 3.4f;
            mk.Prism(chrome, c - up * .2f, up, 1.6f, .6f, 16); mk.Prism(chrome, c + up * .4f, up, .4f, .7f, 10); mk.Prism(chrome, c + up * 1.1f, up, 1.4f, .2f, 16, 1.2f);
            ArtistBits.PostCollider(ctx.Root, c, 5.6f, 2.4f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(go.GetComponent<Collider>()); go.name = "Fishbowl glass";
            go.transform.SetParent(ctx.Root, false); go.transform.position = bc; go.transform.localScale = new Vector3(4.6f, 4.2f, 4.6f);
            var gr = go.GetComponent<Renderer>(); gr.sharedMaterial = SeeThroughM(new Color(.6f, .82f, 1f, .2f), false); gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var wgo = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(wgo.GetComponent<Collider>()); wgo.name = "Fishbowl water";
            wgo.transform.SetParent(ctx.Root, false); wgo.transform.position = bc - up * .3f; wgo.transform.localScale = new Vector3(4.2f, 3.4f, 4.2f);
            var wr = wgo.GetComponent<Renderer>(); wr.sharedMaterial = SeeThroughM(new Color(.3f, .55f, .9f, .22f), false); wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fishM = SongGlow(WishYouWereHere, new Color(1f, .55f, .2f), 1.6f);
            for (var k = 0; k < 2; k++)
            {
                var fk = new MeshKit(); fk.Blob(fishM, Vector3.zero, new Vector3(.18f, .3f, .5f), ctx.Rng, 0f); fk.Tri(fishM, new Vector3(0, 0, -.4f), new Vector3(0, .3f, -.8f), new Vector3(0, -.3f, -.8f), new Vector3(.1f, 0, -.6f)); fk.Tri(fishM, new Vector3(0, 0, -.4f), new Vector3(0, -.3f, -.8f), new Vector3(0, .3f, -.8f), new Vector3(-.1f, 0, -.6f));
                var tr = fk.BuildSingle("Fish", life.transform, false).transform; life.fish.Add((tr, bc - up * .3f, 1.1f + k * .35f, k * Mathf.PI));
            }
            halos.Add(bc, 3.2f, new Color(.5f, .75f, 1f), .2f);
            ArtistBits.PointLight(ctx.Root, bc + face * 3f, new Color(.7f, .85f, 1f), 12f, 1.3f);
        }
    }
}
