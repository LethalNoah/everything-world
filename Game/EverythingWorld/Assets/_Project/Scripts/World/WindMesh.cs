using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Ground cover that moves in the wind (the Wind Grass shader): grass tufts, flowers, bracken, bunting.
    /// Shared by every land; each land picks its own plants, colours, heights and how much they sway.
    /// Vertex colour alpha = how far each corner moves (0 at the root or the string, 1 at the tip).
    public class WindMesh
    {
        readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();

        public int Count => v.Count;

        public void Tri(Vector3 a, Vector3 b, Vector3 tip, Color baseC, Color tipC, float sway)
        {
            var i = v.Count;
            v.Add(a); v.Add(b); v.Add(tip);
            n.Add(Vector3.up); n.Add(Vector3.up); n.Add(Vector3.up);
            c.Add(new Color(baseC.r, baseC.g, baseC.b, 0f)); c.Add(new Color(baseC.r, baseC.g, baseC.b, 0f)); c.Add(new Color(tipC.r, tipC.g, tipC.b, sway));
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        /// A tuft of long grass with pale straw tips.
        public void Tuft(Vector3 at, float h, Color col, System.Random rng) =>
            Tuft(at, h, col, Color.Lerp(col, new Color(.94f, .91f, .78f), .3f), rng);

        /// A tuft of grass blades: 'col' at the root (darkened), 'tipC' at the tips, 'blades' blades 'width' wide.
        public void Tuft(Vector3 at, float h, Color col, Color tipC, System.Random rng, int blades = 3, float width = .1f)
        {
            for (var k = 0; k < blades; k++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * width;
                var lean = new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .4f * h;
                var hh = h * (.75f + (float)rng.NextDouble() * .35f);
                Tri(at - side, at + side, at + lean + Vector3.up * hh, col * .72f, tipC, 1f);
            }
        }

        /// A flower on a stem: a little star of petals.
        public void Flower(Vector3 at, float h, Color petal, System.Random rng)
        {
            var lean = new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .2f;
            var top = at + lean + Vector3.up * h;
            Tri(at - Vector3.right * .03f, at + Vector3.right * .03f, top, new Color(.3f, .45f, .25f), new Color(.35f, .5f, .28f), 1f);
            const float s = .11f;
            Tri(top + new Vector3(-s, 0, 0), top + new Vector3(s, 0, 0), top + new Vector3(0, s * 1.3f, 0), petal, petal, 1f);
            Tri(top + new Vector3(0, 0, -s), top + new Vector3(0, 0, s), top + new Vector3(0, s * 1.3f, 0), petal, petal, 1f);
            Tri(top + new Vector3(-s, 0, 0), top + new Vector3(s, 0, 0), top - new Vector3(0, s * .6f, 0), petal * .85f, petal, 1f);
        }

        /// A fern or bracken frond clump: arching fronds fanned round a centre.
        public void Bracken(Vector3 at, float h, Color col, Color tipC, System.Random rng, int fronds = 6)
        {
            for (var k = 0; k < fronds; k++)
            {
                var a = (k + (float)rng.NextDouble() * .6f) * Mathf.PI * 2f / fronds;
                var out_ = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var side = Vector3.Cross(Vector3.up, out_) * .18f;
                var mid = at + out_ * h * .45f + Vector3.up * h * .8f;
                var tip = at + out_ * h * 1.05f + Vector3.up * h * .35f;
                Tri(at - side * .3f, at + side * .3f, mid, col * .75f, col, .5f);
                Tri(mid - side, mid + side, tip, col, tipC, 1f);
            }
        }

        public void Flag(Vector3 l, Vector3 r, Vector3 tip, Color col) => Tri(l, r, tip, col, col * .92f, .7f);

        public void Build(string name, Transform parent, Material m)
        {
            if (v.Count == 0) return;
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetColors(c); mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            var b = mesh.bounds; b.Expand(2f); mesh.bounds = b;          // room for swaying
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = true;
        }

        /// A Wind Grass material: 'sway' = metres the tips move at full wind, 'flutter' = how fast.
        public static Material Material(float sway, float flutter, Color fallback)
        {
            var shader = Shader.Find("Everything World/Wind Grass");
            if (shader == null) return WorldMaterials.Solid(fallback);
            var m = new Material(shader) { name = "Wind " + sway };
            m.SetFloat("_Sway", sway);
            m.SetFloat("_Flutter", flutter);
            return m;
        }
    }
}
