using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.World
{
    /// Collects soft light halos (shader: Everything World/Glow Halo) and builds them into one mesh.
    /// Add a halo for every lantern, lit window and candle; they always face the camera.
    public class GlowHalos
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<Vector2> corner = new List<Vector2>();
        readonly List<Vector2> size = new List<Vector2>();
        readonly List<int> t = new List<int>();
        Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
        float biggest;

        public int Count => v.Count / 4;

        /// A halo of the given radius (metres) and colour; brightness 1 = a lantern.
        public void Add(Vector3 center, float radius, Color color, float brightness = 1f)
        {
            var i = v.Count;
            var col = new Color(color.r, color.g, color.b, brightness);
            foreach (var k in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
            {
                v.Add(center); c.Add(col); corner.Add(k); size.Add(new Vector2(radius, 0));
            }
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
            min = Vector3.Min(min, center); max = Vector3.Max(max, center);
            biggest = Mathf.Max(biggest, radius);
        }

        /// Builds the halos; spread over a whole land, they're split into chunks so far ones can be skipped.
        public GameObject Build(string name, Transform parent)
        {
            if (v.Count == 0) return null;
            var shader = Shader.Find("Everything World/Glow Halo");
            if (shader == null) { Debug.LogWarning("Glow Halo shader not found: no halos."); return null; }
            if (max.x - min.x > MeshKit.ChunkAbove || max.z - min.z > MeshKit.ChunkAbove)
            {
                var root = new GameObject(name); root.transform.SetParent(parent, false);
                var mat = new Material(shader) { name = "Glow halos" };
                var parts = new Dictionary<(int, int), GlowHalos>();
                for (var q = 0; q < v.Count; q += 4)
                {
                    var key = (Mathf.FloorToInt(v[q].x / MeshKit.ChunkSize), Mathf.FloorToInt(v[q].z / MeshKit.ChunkSize));
                    if (!parts.TryGetValue(key, out var h)) parts[key] = h = new GlowHalos();
                    h.Add(v[q], size[q].x, c[q], c[q].a);
                }
                foreach (var kv in parts) kv.Value.BuildWith($"{name} {kv.Key.Item1},{kv.Key.Item2}", root.transform, mat);
                return root;
            }
            return BuildWith(name, parent, new Material(shader) { name = "Glow halos" });
        }

        GameObject BuildWith(string name, Transform parent, Material material)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetUVs(0, corner); mesh.SetUVs(1, size); mesh.SetTriangles(t, 0);
            // the quads are spread out in the shader, so the bounds must allow for their size
            var b = new Bounds((min + max) / 2f, max - min);
            b.Expand(biggest * 2f + 1f);
            mesh.bounds = b;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }
    }
}
