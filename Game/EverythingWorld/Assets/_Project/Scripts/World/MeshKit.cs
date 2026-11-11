using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.World
{
    /// Builds low-poly, flat-shaded meshes from simple shapes (boxes, prisms, blobs, roofs, quads), one
    /// submesh per material. Many small objects go into one mesh, so a whole forest is a few draw calls.
    /// Every shape is given its centre, and each triangle is turned to face away from it, so the winding
    /// is always right.
    public class MeshKit
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly Dictionary<Material, List<int>> tris = new Dictionary<Material, List<int>>();

        public int VertexCount => verts.Count;

        /// One triangle facing away from 'inside'.
        public void Tri(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Vector2 ua = default, Vector2 ub = default, Vector2 uc = default)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0) { (b, c) = (c, b); (ub, uc) = (uc, ub); }
            if (!tris.TryGetValue(m, out var list)) tris[m] = list = new List<int>();
            list.Add(verts.Count); verts.Add(a); uvs.Add(ua);
            list.Add(verts.Count); verts.Add(b); uvs.Add(ub);
            list.Add(verts.Count); verts.Add(c); uvs.Add(uc);
        }

        /// A four-corner face (a, b, c, d in order round the edge) facing away from 'inside'.
        public void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            Tri(m, a, b, c, inside);
            Tri(m, a, c, d, inside);
        }

        /// A flat picture (for example an album cover) facing 'normal', with the given texture rectangle.
        public void Picture(Material m, Vector3 center, Vector3 right, Vector3 up, Vector3 normal, Rect uv)
        {
            center += normal.normalized * .012f;   // lifted 12 mm off its wall, so a surface grown by Box can never hide it
            Vector3 a = center - right - up, b = center + right - up, c = center + right + up, d = center - right + up;
            var inside = center - normal;
            Tri(m, a, b, c, inside, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin), new Vector2(uv.xMax, uv.yMax));
            Tri(m, a, c, d, inside, new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax));
        }

        /// How much a box with this material is grown on every side (a few millimetres), so two boxes of different
        /// materials never share a face exactly and flicker against each other: glowing panels sit proud of whatever
        /// they're mounted on, and solid surfaces step apart from each other by material.
        static float Grow(Material m)
        {
            if (m == null) return 0f;
            if (m.IsKeywordEnabled("_EMISSION") || m.name.StartsWith("Glow") || m.name.StartsWith("Song glow")) return .014f;
            if (!m.HasProperty("_BaseColor")) return 0f;
            var c = m.GetColor("_BaseColor");
            int r = Mathf.RoundToInt(c.r * 255f), g = Mathf.RoundToInt(c.g * 255f), b = Mathf.RoundToInt(c.b * 255f);
            return .0018f * ((r + 3 * g + 5 * b) % 7);   // 0 to 11 mm in steps, chosen so the land audits' pairs separate
        }

        /// A box of the given full size, rotated.
        public void Box(Material m, Vector3 center, Vector3 size, Quaternion rot)
        {
            var h = size / 2f + Vector3.one * Grow(m);
            var p = new Vector3[8];
            for (var i = 0; i < 8; i++)
                p[i] = center + rot * new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            Quad(m, p[0], p[1], p[3], p[2], center); // back (-z)
            Quad(m, p[4], p[5], p[7], p[6], center); // front (+z)
            Quad(m, p[0], p[2], p[6], p[4], center); // left
            Quad(m, p[1], p[3], p[7], p[5], center); // right
            Quad(m, p[2], p[3], p[7], p[6], center); // top
            Quad(m, p[0], p[1], p[5], p[4], center); // bottom
        }

        public void Box(Material m, Vector3 center, Vector3 size) => Box(m, center, size, Quaternion.identity);

        /// A tapered prism (low-poly trunk, post, log or candle) from 'start' along 'dir' for 'length'.
        public void Prism(Material m, Vector3 start, Vector3 dir, float radius, float length, int sides, float topScale = 1f, float twist = 0f, bool capTop = true)
        {
            dir = dir.normalized;
            var rot = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0, twist, 0);
            var end = start + dir * length;
            var mid = (start + end) / 2f;
            var lo = new Vector3[sides]; var hi = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var a = i * Mathf.PI * 2f / sides;
                var o = rot * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                lo[i] = start + o * radius;
                hi[i] = end + o * radius * topScale;
            }
            // a prism lying down (a log, a beam) shows both ends, so it gets a lid at the start too
            var capStart = Mathf.Abs(dir.y) < .7f;
            for (var i = 0; i < sides; i++)
            {
                var j = (i + 1) % sides;
                Quad(m, lo[i], lo[j], hi[j], hi[i], mid);
                if (capTop) Tri(m, hi[i], hi[j], end, mid);
                if (capStart) Tri(m, lo[i], lo[j], start, mid);
            }
        }

        /// A cone or pyramid (roof tip, spruce layer, candle flame).
        public void Cone(Material m, Vector3 baseCenter, float radius, float height, int sides, float twist = 0f)
        {
            var apex = baseCenter + Vector3.up * height;
            var inside = baseCenter + Vector3.up * height * .3f;
            for (var i = 0; i < sides; i++)
            {
                float a0 = twist * Mathf.Deg2Rad + i * Mathf.PI * 2f / sides, a1 = twist * Mathf.Deg2Rad + (i + 1) * Mathf.PI * 2f / sides;
                var p0 = baseCenter + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius;
                var p1 = baseCenter + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                Tri(m, p0, p1, apex, inside);
                Tri(m, p0, p1, baseCenter, inside + Vector3.up * height); // underside
            }
        }

        static readonly Vector3[] Ico = MakeIco();
        static readonly int[] IcoFaces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        static Vector3[] MakeIco()
        {
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (var i = 0; i < v.Length; i++) v[i] = v[i].normalized;
            return v;
        }

        /// A lumpy low-poly blob (tree crown, bush, rock, snow pile).
        public void Blob(Material m, Vector3 center, Vector3 radii, System.Random rng, float lumpiness = .18f)
        {
            var p = new Vector3[12];
            for (var i = 0; i < 12; i++)
            {
                var j = 1f + ((float)rng.NextDouble() * 2f - 1f) * lumpiness;
                p[i] = center + Vector3.Scale(Ico[i], radii) * j;
            }
            for (var f = 0; f < IcoFaces.Length; f += 3) Tri(m, p[IcoFaces[f]], p[IcoFaces[f + 1]], p[IcoFaces[f + 2]], center);
        }

        /// A gable roof: 'size' is the footprint (x = width across the ridge, z = length along it), ridge height
        /// above 'eaveCenter', with an overhang. Optional snow on top.
        public void GableRoof(Material m, Vector3 eaveCenter, Vector3 size, float ridge, Quaternion rot, Material snowOnTop = null)
        {
            float hx = size.x / 2f, hz = size.z / 2f;
            Vector3 P(float x, float y, float z) => eaveCenter + rot * new Vector3(x, y, z);
            Vector3 l0 = P(-hx, 0, -hz), l1 = P(-hx, 0, hz), r0 = P(hx, 0, -hz), r1 = P(hx, 0, hz), t0 = P(0, ridge, -hz), t1 = P(0, ridge, hz);
            var inside = P(0, ridge * .35f, 0);
            Quad(m, l0, l1, t1, t0, inside);
            Quad(m, r0, r1, t1, t0, inside);
            Tri(m, l0, r0, t0, inside);
            Tri(m, l1, r1, t1, inside);
            Quad(m, l0, l1, r1, r0, inside + rot * Vector3.up * ridge); // underside
            if (snowOnTop != null)
            {
                var lift = rot * Vector3.up * .12f;
                Quad(snowOnTop, Vector3.Lerp(l0, t0, .25f) + lift, Vector3.Lerp(l1, t1, .25f) + lift, t1 + lift, t0 + lift, inside);
                Quad(snowOnTop, Vector3.Lerp(r0, t0, .25f) + lift, Vector3.Lerp(r1, t1, .25f) + lift, t1 + lift, t0 + lift, inside);
            }
        }

        public const float ChunkAbove = 200f, ChunkSize = 80f;

        /// Makes the mesh as one object, never split (for a mesh that is moved as a whole, like a cloud, or whose
        /// renderer the caller needs).
        public GameObject BuildSingle(string name, Transform parent, bool collider) => verts.Count == 0 ? null : BuildOne(name, parent, collider);

        GameObject BuildChunks(string name, Transform parent, bool collider)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.isStatic = true;
            var chunks = new Dictionary<(int, int), MeshKit>();
            foreach (var kv in tris)
            {
                var list = kv.Value;
                for (var i = 0; i < list.Count; i += 3)
                {
                    Vector3 a = verts[list[i]], b = verts[list[i + 1]], c = verts[list[i + 2]];
                    var centre = (a + b + c) / 3f;
                    var key = (Mathf.FloorToInt(centre.x / ChunkSize), Mathf.FloorToInt(centre.z / ChunkSize));
                    if (!chunks.TryGetValue(key, out var mk)) chunks[key] = mk = new MeshKit();
                    if (!mk.tris.TryGetValue(kv.Key, out var t)) mk.tris[kv.Key] = t = new List<int>();
                    t.Add(mk.verts.Count); mk.verts.Add(a); mk.uvs.Add(uvs[list[i]]);
                    t.Add(mk.verts.Count); mk.verts.Add(b); mk.uvs.Add(uvs[list[i + 1]]);
                    t.Add(mk.verts.Count); mk.verts.Add(c); mk.uvs.Add(uvs[list[i + 2]]);
                }
            }
            foreach (var kv in chunks) kv.Value.BuildOne($"{name} {kv.Key.Item1},{kv.Key.Item2}", root.transform, collider);
            return root;
        }

        /// Makes an invisible mesh that only blocks movement (walls, benches, firewood).
        public GameObject BuildCollider(string name, Transform parent)
        {
            if (verts.Count == 0) return null;
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            var all = new List<int>();
            foreach (var kv in tris) all.AddRange(kv.Value);
            mesh.SetTriangles(all, 0);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }

        /// Makes the mesh as a child of 'parent' (in world coordinates). Returns null if nothing was added.
        /// A mesh spread over a whole land is split into chunks (children of the returned object), so Unity
        /// only draws the parts in view and LandCuller can hide far ones: one land-wide mesh is drawn whole
        /// whenever any corner of it is on screen.
        public GameObject Build(string name, Transform parent, bool collider)
        {
            if (verts.Count == 0) return null;
            Vector3 lo = verts[0], hi = verts[0];
            foreach (var p in verts) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
            if (hi.x - lo.x > ChunkAbove || hi.z - lo.z > ChunkAbove) return BuildChunks(name, parent, collider);
            return BuildOne(name, parent, collider);
        }

        GameObject BuildOne(string name, Transform parent, bool collider)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = tris.Count;
            var mats = new Material[tris.Count];
            var i = 0;
            foreach (var kv in tris) { mesh.SetTriangles(kv.Value, i); mats[i++] = kv.Key; }
            mesh.RecalculateNormals(); // separate vertices per triangle, so the shading is flat and faceted
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
            return go;
        }
    }
}
