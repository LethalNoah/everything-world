using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The quiet place in the Indie Downs, set apart from the paths: a great tree on level ground with a circular
    /// stone walkway round it, four standing stones carved with symbols the owner chose, benches facing the tree,
    /// lanterns hanging in its branches and low lights along the walk. No sign, no plaque.
    public partial class IndieDownsKit
    {
        const float QuietFlat = 26f, QuietBlend = 42f;
        const float WalkIn = 10.5f, WalkOut = 13f, StoneR = 15.6f;
        Vector3 quiet; bool hasQuiet;

        /// Finds the quiet place (well away from every area, gate and the green, on fairly level high ground)
        /// and levels the ground there before the terrain is built.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            hasQuiet = false;
            var isl = data.layout.islands[index]; var sid = isl.sceneId;
            var avoid = new List<Vector3>();                    // x, radius, z
            foreach (var a in data.layout.artists) if (a.sceneId == sid) avoid.Add(new Vector3(a.x, a.r, a.z));
            foreach (var b in data.layout.bridges)
            {
                if (b.a == sid) avoid.Add(new Vector3(b.ax, 30f, b.az));
                if (b.b == sid) avoid.Add(new Vector3(b.bx, 30f, b.bz));
            }
            avoid.Add(new Vector3(isl.labelX, LandContext.PlazaRadius, isl.labelZ));
            foreach (var t in data.layout.towers) avoid.Add(new Vector3(t.x, 40f, t.z));
            var step = data.Step; var best = float.MinValue;
            bool Here(float x, float z)
            {
                var c = Mathf.RoundToInt(x / step); var r = Mathf.RoundToInt((data.layout.depth - z) / step);
                return c >= 0 && r >= 0 && c < data.Nx && r < data.Nz && data.islandIds[r * data.Nx + c] == index;
            }
            for (var r = 0; r < data.Nz; r += 3)
            for (var c = 0; c < data.Nx; c += 3)
            {
                if (data.islandIds[r * data.Nx + c] != index) continue;
                float x = c * step, z = data.layout.depth - r * step, h = data.HeightAt(x, z);
                if (h < 4f) continue;
                bool ok = true; float lo = h, hi = h;
                for (var k = 0; k < 12 && ok; k++)
                {
                    var a = k * Mathf.PI / 6f;
                    float px = x + Mathf.Cos(a) * QuietBlend, pz = z + Mathf.Sin(a) * QuietBlend;
                    if (!Here(px, pz) || data.HeightAt(px, pz) < 3f) ok = false;
                    var q = data.HeightAt(x + Mathf.Cos(a) * QuietFlat, z + Mathf.Sin(a) * QuietFlat);
                    lo = Mathf.Min(lo, q); hi = Mathf.Max(hi, q);
                }
                if (!ok) continue;
                var d = float.MaxValue;
                foreach (var v in avoid) d = Mathf.Min(d, Vector2.Distance(new Vector2(x, z), new Vector2(v.x, v.z)) - v.y);
                if (d < QuietBlend + 10f) continue;
                var score = Mathf.Min(d, 200f) + h * .3f - (hi - lo) * 6f;
                if (score > best) { best = score; quiet = new Vector3(x, h, z); hasQuiet = true; }
            }
            if (!hasQuiet) { Debug.LogWarning("The Indie Downs: no room for the quiet place"); return; }
            // level it to the average height round the spot, easing back into the hills
            float sum = data.HeightAt(quiet.x, quiet.z); var n = 1;
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; sum += data.HeightAt(quiet.x + Mathf.Cos(a) * QuietFlat * .6f, quiet.z + Mathf.Sin(a) * QuietFlat * .6f); n++; }
            var level = sum / n;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * step;
                if (Mathf.Abs(z - quiet.z) > QuietBlend) continue;
                for (var c = 0; c < data.Nx; c++)
                {
                    var x = c * step;
                    if (Mathf.Abs(x - quiet.x) > QuietBlend) continue;
                    var i = r * data.Nx + c;
                    if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), new Vector2(quiet.x, quiet.z));
                    if (d < QuietFlat) data.heights[i] = level;
                    else if (d < QuietBlend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - QuietFlat) / (QuietBlend - QuietFlat)));
                }
            }
            quiet.y = level;
        }

        void QuietPlace(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var root = new GameObject("The quiet place").transform;
            root.SetParent(ctx.Root, false);
            var c = new Vector3(quiet.x, ctx.Ground(quiet.x, quiet.z), quiet.z);
            var pave = WorldMaterials.Solid(new Color(.82f, .8f, .75f)); var paveB = WorldMaterials.Solid(new Color(.74f, .72f, .67f));
            var warm = new Color(1f, .78f, .5f);
            // the walkway: a ring of laid stone
            const int segs = 56;
            for (var s = 0; s < segs; s++)
            {
                float a0 = s * Mathf.PI * 2f / segs, a1 = (s + 1) * Mathf.PI * 2f / segs;
                Vector3 P(float r, float a, float y) => c + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                var m = s % 2 == 0 ? pave : paveB;
                mk.Quad(m, P(WalkIn, a0, .1f), P(WalkIn, a1, .1f), P(WalkOut, a1, .1f), P(WalkOut, a0, .1f), c - Vector3.up * 5f);
                mk.Quad(m, P(WalkOut, a0, .1f), P(WalkOut, a1, .1f), P(WalkOut, a1, -.2f), P(WalkOut, a0, -.2f), c);
                mk.Quad(m, P(WalkIn, a0, .1f), P(WalkIn, a1, .1f), P(WalkIn, a1, -.2f), P(WalkIn, a0, -.2f), c + Vector3.up * 20f + (P(WalkIn, (a0 + a1) / 2f, 0) - c) * 3f);
            }
            // wildflowers in the grass round the trunk
            Color[] petals = { new Color(.97f, .96f, .92f), new Color(.98f, .86f, .4f), new Color(.72f, .6f, .9f), new Color(.95f, .7f, .78f) };
            for (var i = 0; i < 70; i++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = ctx.Range(2.6f, WalkIn - .6f);
                var p = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                mk.Box(leafB, p + Vector3.up * .15f, new Vector3(.03f, .3f, .03f), Quaternion.identity);
                mk.Blob(WorldMaterials.Solid(petals[i % petals.Length]), p + Vector3.up * .32f, new Vector3(.09f, .05f, .09f), ctx.Rng, .1f);
            }
            Tree(ctx, mk, root, c, warm);
            // the four stones, facing the tree, between the walkway and the meadow
            for (var k = 0; k < 4; k++)
            {
                var a = (k * 90f + 45f) * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Stone(ctx, mk, solid, c + outward * StoneR, -outward, k);
            }
            // benches on the outer edge of the walk, facing the tree, and low lights all round
            for (var k = 0; k < 4; k++)
            {
                var a = k * Mathf.PI / 2f;
                var outward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Bench(mk, solid, c + outward * (WalkOut - .55f) + Vector3.up * .1f, -outward);
            }
            var bollard = WorldMaterials.Glow(warm, 1.2f);
            for (var k = 0; k < 16; k++)
            {
                var a = (k + .5f) * Mathf.PI * 2f / 16f;
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (WalkOut + .45f);
                p.y = ctx.Ground(p.x, p.z);
                mk.Prism(stone, p, Vector3.up, .14f, .45f, 6);
                mk.Box(bollard, p + Vector3.up * .55f, new Vector3(.2f, .18f, .2f), Quaternion.identity);
                ArtistBits.PostCollider(root, p, .7f, .2f);
                halos.Add(p + Vector3.up * .55f, .7f, warm, .7f);
            }
            ArtistBits.PointLight(root, c + Vector3.up * 4f, warm, 24f, 1.1f);
        }

        /// A great broad tree: a thick trunk with roots, heavy branches and a wide crown, lanterns in its boughs.
        void Tree(LandContext ctx, MeshKit mk, Transform root, Vector3 c, Color warm)
        {
            var trunkH = 8f;
            mk.Prism(bark, c - Vector3.up * .3f, Vector3.up, 1.5f, trunkH + .3f, 10, .62f);
            ArtistBits.PostCollider(root, c, trunkH, 1.6f);
            for (var k = 0; k < 7; k++)
            {
                var a = k * Mathf.PI * 2f / 7f + .3f;
                var dir = new Vector3(Mathf.Cos(a), -.18f, Mathf.Sin(a)).normalized;
                mk.Prism(bark, c + Vector3.up * .9f + new Vector3(dir.x, 0, dir.z) * .9f, dir, .55f, 3.2f, 6, .3f);
                ArtistBits.PostCollider(root, c + new Vector3(dir.x, 0, dir.z) * 2.6f, .9f, .55f);   // the roots
            }
            var crownLeaf = new[] { leafA, leafB, WorldMaterials.Solid(new Color(.6f, .62f, .36f)) };
            var lanterns = new List<Vector3>();
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI * 2f / 6f + .6f;
                var flat = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var start = c + Vector3.up * (trunkH - 1.5f - (k % 2) * 1.2f);
                var dir = (flat + Vector3.up * .55f).normalized;
                var len = 7.5f;
                mk.Prism(bark, start, dir, .55f, len, 7, .45f);
                var end = start + dir * len;
                mk.Blob(crownLeaf[k % 3], end + Vector3.up * 1.5f, new Vector3(6f, 4.2f, 6f), ctx.Rng, .14f);
                mk.Blob(crownLeaf[(k + 1) % 3], end + flat * 3f + Vector3.up * .2f, new Vector3(4.5f, 3.4f, 4.5f), ctx.Rng, .14f);
                // a lantern hanging from the bough, about halfway out
                var hang = start + dir * (len * .55f);
                lanterns.Add(hang);
            }
            mk.Blob(crownLeaf[0], c + Vector3.up * (trunkH + 7f), new Vector3(9f, 5f, 9f), ctx.Rng, .12f);
            mk.Blob(crownLeaf[1], c + Vector3.up * (trunkH + 10f), new Vector3(6.5f, 3.6f, 6.5f), ctx.Rng, .12f);
            foreach (var h in lanterns)
            {
                var lamp = h - Vector3.up * 2.2f;
                mk.Box(iron, (h + lamp) / 2f + Vector3.up * .2f, new Vector3(.03f, 2f, .03f), Quaternion.identity);
                mk.Box(iron, lamp + Vector3.up * .3f, new Vector3(.36f, .06f, .36f), Quaternion.identity);
                mk.Box(WorldMaterials.Glow(warm, 2f), lamp, new Vector3(.3f, .45f, .3f), Quaternion.identity);
                mk.Box(iron, lamp - Vector3.up * .26f, new Vector3(.32f, .05f, .32f), Quaternion.identity);
                halos.Add(lamp, 1.6f, warm, .9f);
            }
        }

        /// A standing stone with one of the four symbols carved into the face toward the tree.
        void Stone(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, int which)
        {
            at.y = ctx.Ground(at.x, at.z);
            var rot = Quaternion.LookRotation(face);
            var right = Vector3.Cross(face, Vector3.up).normalized;      // the viewer's right, so nothing reads mirrored
            const float W = 2.3f, H = 2.6f, D = .55f;
            var grey = WorldMaterials.Solid(new Color(.72f, .71f, .68f));
            mk.Box(stone, at + Vector3.up * .15f, new Vector3(W + .5f, .3f, D + .5f), rot);
            mk.Box(grey, at + Vector3.up * (.3f + H / 2f), new Vector3(W, H, D), rot);
            mk.Prism(grey, at + Vector3.up * (.3f + H) - face * D / 2f, face, W / 2f, D, 16);   // a rounded top
            solid.Box(grey, at + Vector3.up * (.3f + H / 2f + .5f), new Vector3(W, H + 1f, D + .1f), rot);
            var o = at + Vector3.up * (.3f + H * .62f) + face * (D / 2f + .01f);
            const float s = .82f;                                        // symbol units to metres
            Vector3 W2(Vector2 p, float lift) => o + right * p.x * s + Vector3.up * p.y * s + face * lift;
            void Line(Material m, Vector2[] pts, float width, float lift)
            {
                for (var i = 0; i + 1 < pts.Length; i++)
                {
                    Vector3 a = W2(pts[i], lift), b = W2(pts[i + 1], lift);
                    var d = b - a; var len = d.magnitude;
                    if (len < 1e-4f) continue;
                    mk.Box(m, (a + b) / 2f, new Vector3(width * s, len + width * s * .7f, .06f), Quaternion.LookRotation(face, d / len));
                }
            }
            void Disc(Material m, Vector2 p, float r, float lift) => mk.Prism(m, W2(p, lift) - face * .03f, face, r * s, .06f, 18);
            Vector2[] Eye(Vector2 c, float w, float h)
            {
                var pts = new List<Vector2>();
                for (var k = 0; k <= 12; k++) { var t = k / 12f; var x = Mathf.Lerp(-w, w, t); pts.Add(c + new Vector2(x, h * (1f - (x / w) * (x / w)))); }
                for (var k = 1; k <= 12; k++) { var t = k / 12f; var x = Mathf.Lerp(w, -w, t); pts.Add(c + new Vector2(x, -h * (1f - (x / w) * (x / w)))); }
                return pts.ToArray();
            }
            var ink = WorldMaterials.Solid(new Color(.2f, .19f, .21f));
            switch (which)
            {
                case 0: // a heart on fire, with an eye in the centre
                {
                    var heart = new List<Vector2>();
                    for (var k = 0; k <= 40; k++)
                    {
                        var t = k / 40f * Mathf.PI * 2f;
                        var x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                        var y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                        heart.Add(new Vector2(x, y) / 21f + new Vector2(0, -.15f));
                    }
                    Line(WorldMaterials.Glow(new Color(.85f, .12f, .15f), 1.1f), heart.ToArray(), .1f, .02f);
                    var flame = WorldMaterials.Glow(new Color(1f, .5f, .12f), 1.6f);
                    var core = WorldMaterials.Glow(new Color(1f, .85f, .3f), 1.8f);
                    foreach (var (cx, h, w) in new[] { (-.42f, .5f, .14f), (0f, .78f, .17f), (.42f, .5f, .14f) })
                    {
                        var baseY = cx == 0f ? .36f : .5f;
                        var outer = new[] { new Vector2(cx - w, baseY), new Vector2(cx - w * 1.1f, baseY + h * .4f), new Vector2(cx - w * .3f, baseY + h * .75f), new Vector2(cx + w * .15f, baseY + h), new Vector2(cx + w * .6f, baseY + h * .6f), new Vector2(cx + w, baseY + h * .3f), new Vector2(cx + w * .8f, baseY) };
                        Line(flame, outer, .08f, .02f);
                        Line(core, new[] { new Vector2(cx, baseY + .02f), new Vector2(cx + w * .1f, baseY + h * .5f) }, .07f, .03f);
                    }
                    Line(ink, Eye(new Vector2(0, -.12f), .36f, .2f), .06f, .03f);
                    Disc(WorldMaterials.Solid(new Color(.35f, .5f, .62f)), new Vector2(0, -.12f), .13f, .04f);
                    Disc(ink, new Vector2(0, -.12f), .06f, .07f);
                    break;
                }
                case 1: // the blue and white evil eye
                {
                    Disc(WorldMaterials.Solid(new Color(.05f, .2f, .65f), .6f), Vector2.zero, .78f, .02f);
                    Disc(WorldMaterials.Solid(new Color(.97f, .97f, .97f), .6f), Vector2.zero, .54f, .06f);
                    Disc(WorldMaterials.Solid(new Color(.45f, .72f, 1f), .6f), Vector2.zero, .33f, .1f);
                    Disc(WorldMaterials.Solid(new Color(.05f, .05f, .08f), .6f), Vector2.zero, .15f, .14f);
                    break;
                }
                case 2: // the sign for Virgo
                {
                    var gold = WorldMaterials.Glow(new Color(.85f, .66f, .3f), .7f);
                    Line(gold, Smooth(new[] { new Vector2(-.75f, .5f), new Vector2(-.75f, -.7f) }), .1f, .02f);
                    Line(gold, Smooth(new[] { new Vector2(-.75f, .3f), new Vector2(-.62f, .52f), new Vector2(-.47f, .58f), new Vector2(-.34f, .48f), new Vector2(-.3f, .28f), new Vector2(-.3f, -.7f) }), .1f, .02f);
                    Line(gold, Smooth(new[] { new Vector2(-.3f, .3f), new Vector2(-.17f, .52f), new Vector2(-.02f, .58f), new Vector2(.11f, .48f), new Vector2(.15f, .28f), new Vector2(.15f, -.35f), new Vector2(.2f, -.6f), new Vector2(.36f, -.7f), new Vector2(.54f, -.58f), new Vector2(.6f, -.32f), new Vector2(.52f, -.08f), new Vector2(.36f, .0f), new Vector2(.24f, -.18f), new Vector2(.32f, -.48f), new Vector2(.52f, -.74f), new Vector2(.74f, -.9f) }), .1f, .03f);
                    break;
                }
                default: // a pair of eyes, a lightning bolt through one
                {
                    var iris = WorldMaterials.Solid(new Color(.4f, .55f, .62f));
                    foreach (var x in new[] { -.42f, .42f })
                    {
                        Line(ink, Eye(new Vector2(x, .05f), .33f, .18f), .06f, .02f);
                        Disc(iris, new Vector2(x, .05f), .12f, .03f);
                        Disc(ink, new Vector2(x, .05f), .055f, .06f);
                    }
                    var bolt = new[] { new Vector2(-.15f, .85f), new Vector2(-.5f, .25f), new Vector2(-.3f, .22f), new Vector2(-.66f, -.45f), new Vector2(-.42f, -.2f), new Vector2(-.6f, -.22f), new Vector2(-.3f, .55f) };
                    Line(WorldMaterials.Solid(new Color(.2f, .45f, .85f)), bolt, .2f, .05f);
                    Line(WorldMaterials.Glow(new Color(.9f, .15f, .15f), 1f), bolt, .12f, .08f);
                    break;
                }
            }
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
    }
}
