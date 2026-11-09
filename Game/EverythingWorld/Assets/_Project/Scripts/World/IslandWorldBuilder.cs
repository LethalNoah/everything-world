using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.World
{
    /// Builds the island world when the scene starts, from the data exported by build/islandmap.py:
    /// low-poly terrain (flat-shaded, coloured per island from its look brief), bridges, causeways,
    /// fast-travel towers, island name signs and the sea. Nothing is hand-placed, so a new export
    /// just needs Play again.
    [DefaultExecutionOrder(-100)]
    public class IslandWorldBuilder : MonoBehaviour
    {
        [Header("Data from build/islandmap.py")]
        public TextAsset layoutJson;   // world_layout.json
        public TextAsset heightBytes;  // world_height.bytes
        public TextAsset islandBytes;  // world_island.bytes

        [Tooltip("Grid cells per terrain chunk (each cell is 5 m).")]
        public int chunkCells = 64;
        public bool islandNameSigns = true;

        public static IslandWorldBuilder Instance { get; private set; }
        public IslandWorldData Data { get; private set; }
        public List<Transform> Towers { get; } = new List<Transform>();

        Shader lit;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        // shared terrain colours
        static readonly Color Seabed = new Color32(126, 136, 104, 255);
        static readonly Color Sand = new Color32(222, 205, 160, 255);
        static readonly Color Rock = new Color32(138, 126, 112, 255);
        static readonly Color Peak = new Color32(214, 210, 200, 255);

        void Awake()
        {
            Instance = this;
            lit = Shader.Find("Universal Render Pipeline/Lit");
            Data = IslandWorldData.Load(layoutJson, heightBytes, islandBytes);
            var t0 = Time.realtimeSinceStartup;
            BuildTerrain();
            BuildSea();
            BuildBridges();
            BuildTowers();
            if (islandNameSigns) BuildSigns();
            Debug.Log($"Island world built in {(Time.realtimeSinceStartup - t0) * 1000:0} ms: " +
                      $"{Data.layout.islands.Length} islands, {Data.layout.bridges.Length} crossings, {Data.layout.towers.Length} towers");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        Material Mat(Color c, float smooth = .08f)
        {
            if (materials.TryGetValue(c, out var m)) return m;
            m = new Material(lit) { name = "World " + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            materials[c] = m;
            return m;
        }

        static Color Rgb(int[] c) => c != null && c.Length == 3 ? new Color32((byte)c[0], (byte)c[1], (byte)c[2], 255) : Color.grey;

        // ---------- terrain ----------
        void BuildTerrain()
        {
            var root = new GameObject("Terrain").transform;
            root.SetParent(transform, false);
            int cells = chunkCells;
            for (var r0 = 0; r0 < Data.Nz - 1; r0 += cells)
            for (var c0 = 0; c0 < Data.Nx - 1; c0 += cells)
                BuildChunk(root, c0, r0, Mathf.Min(cells, Data.Nx - 1 - c0), Mathf.Min(cells, Data.Nz - 1 - r0));
        }

        void BuildChunk(Transform root, int c0, int r0, int w, int h)
        {
            var verts = new List<Vector3>();
            var byMaterial = new Dictionary<Material, List<int>>();
            var corner = new Vector3[4];
            for (var r = r0; r < r0 + h; r++)
            for (var c = c0; c < c0 + w; c++)
            {
                corner[0] = Data.VertexPosition(c, r); corner[1] = Data.VertexPosition(c + 1, r);
                corner[2] = Data.VertexPosition(c + 1, r + 1); corner[3] = Data.VertexPosition(c, r + 1);
                var top = Mathf.Max(Mathf.Max(corner[0].y, corner[1].y), Mathf.Max(corner[2].y, corner[3].y));
                if (top < -2.5f) continue; // open sea: hidden under the water anyway

                var island = CellIsland(c, r);
                // alternate the diagonal so the facets don't line up
                if (((c + r) & 1) == 0)
                {
                    AddTri(verts, byMaterial, corner[0], corner[1], corner[2], island, c, r);
                    AddTri(verts, byMaterial, corner[0], corner[2], corner[3], island, c, r);
                }
                else
                {
                    AddTri(verts, byMaterial, corner[0], corner[1], corner[3], island, c, r);
                    AddTri(verts, byMaterial, corner[1], corner[2], corner[3], island, c, r);
                }
            }
            if (verts.Count == 0) return;

            var mesh = new Mesh { name = $"Terrain {c0},{r0}", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.subMeshCount = byMaterial.Count;
            var mats = new Material[byMaterial.Count];
            var i = 0;
            foreach (var kv in byMaterial) { mesh.SetTriangles(kv.Value, i); mats[i++] = kv.Key; }
            mesh.RecalculateNormals(); // every triangle has its own vertices, so this gives flat, faceted shading
            mesh.RecalculateBounds();

            var go = new GameObject(mesh.name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = mats;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
        }

        int CellIsland(int c, int r)
        {
            for (var k = 0; k < 4; k++)
            {
                var id = Data.IslandIndexAtVertex(c + (k & 1), r + (k >> 1));
                if (id != 255) return id;
            }
            return -1;
        }

        void AddTri(List<Vector3> verts, Dictionary<Material, List<int>> byMaterial, Vector3 a, Vector3 b, Vector3 c, int island, int col, int row)
        {
            // clockwise seen from above, so the faces point up
            var n = Vector3.Cross(b - a, c - a);
            if (n.y < 0) { var t = b; b = c; c = t; }
            var avg = (a.y + b.y + c.y) / 3f;
            var slope = Mathf.Max(Mathf.Max(a.y, b.y), c.y) - Mathf.Min(Mathf.Min(a.y, b.y), c.y);
            var mat = Mat(TerrainColor(avg, slope, island, col, row));
            if (!byMaterial.TryGetValue(mat, out var list)) byMaterial[mat] = list = new List<int>();
            list.Add(verts.Count); verts.Add(a);
            list.Add(verts.Count); verts.Add(b);
            list.Add(verts.Count); verts.Add(c);
        }

        Color TerrainColor(float height, float slope, int island, int col, int row)
        {
            if (height < -.2f) return Seabed;
            if (height < 1.6f || island < 0) return Sand;
            if (height > 38f) return Peak;
            if (slope > 7f || height > 26f) return Rock;
            var isl = Data.layout.islands[island];
            // patches of the island's two colours, about 40 m across
            var patch = Mathf.PerlinNoise(col * .12f + island * 7.3f, row * .12f);
            return patch > .55f ? Rgb(isl.tintB) : Rgb(isl.tintA);
        }

        // ---------- sea ----------
        void BuildSea()
        {
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea";
            Destroy(sea.GetComponent<Collider>());
            sea.transform.SetParent(transform, false);
            var size = Mathf.Max(Data.layout.width, Data.layout.depth) * 1.6f;
            sea.transform.localPosition = new Vector3(Data.layout.width / 2, Data.layout.waterLevel, Data.layout.depth / 2);
            sea.transform.localScale = new Vector3(size / 10f, 1, size / 10f);
            sea.GetComponent<Renderer>().sharedMaterial = Mat(new Color32(74, 128, 140, 255), .85f);
        }

        // ---------- bridges and causeways ----------
        void BuildBridges()
        {
            var root = new GameObject("Bridges").transform;
            root.SetParent(transform, false);
            var wood = Mat(new Color32(140, 98, 62, 255));
            var stone = Mat(new Color32(196, 184, 160, 255));
            foreach (var b in Data.layout.bridges)
            {
                var a = new Vector3(b.ax, 0, b.az); var e = new Vector3(b.bx, 0, b.bz);
                a.y = Mathf.Max(Data.HeightAt(a.x, a.z), 1f) + .15f;
                e.y = Mathf.Max(Data.HeightAt(e.x, e.z), 1f) + .15f;
                var dir = e - a; var len = dir.magnitude;
                var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                var width = b.causeway ? 6f : 3.5f;
                var go = new GameObject((b.causeway ? "Causeway " : "Bridge ") + b.a + "-" + b.b).transform;
                go.SetParent(root, false);

                Box(go, "Deck", (a + e) / 2 - Vector3.up * .2f, rot, new Vector3(width, .4f, len + 1f), b.causeway ? stone : wood, true);
                // railings with posts
                for (var side = -1; side <= 1; side += 2)
                {
                    var off = rot * Vector3.right * (width / 2 - .15f) * side;
                    Box(go, "Rail", (a + e) / 2 + off + Vector3.up * .9f, rot, new Vector3(.18f, .12f, len), b.causeway ? stone : wood, false);
                    for (var t = 0f; t <= len; t += b.causeway ? 6f : 3f)
                    {
                        var p = Vector3.Lerp(a, e, t / len) + off;
                        Box(go, "Post", p + Vector3.up * .45f, rot, new Vector3(.25f, .9f, .25f), b.causeway ? stone : wood, false);
                    }
                }
                // causeways stand on stone piers
                if (b.causeway)
                    for (var t = 8f; t < len - 4f; t += 14f)
                    {
                        var p = Vector3.Lerp(a, e, t / len); var depth = p.y + 10f;
                        Box(go, "Pier", p - Vector3.up * (depth / 2 + .4f), rot, new Vector3(width * .8f, depth, 2.4f), stone, false);
                    }
                // invisible side walls keep people from falling off
                for (var side = -1; side <= 1; side += 2)
                {
                    var wall = Box(go, "Edge", (a + e) / 2 + rot * Vector3.right * (width / 2) * side + Vector3.up * 1f, rot, new Vector3(.2f, 2.2f, len), null, true);
                    wall.GetComponent<Renderer>().enabled = false;
                }
            }
        }

        static GameObject Box(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 size, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }

        // ---------- towers ----------
        void BuildTowers()
        {
            var root = new GameObject("Towers").transform;
            root.SetParent(transform, false);
            var wall = Mat(new Color32(226, 214, 184, 255));
            var roof = Mat(new Color32(140, 47, 28, 255));
            var beacon = Mat(new Color32(240, 180, 70, 255));
            beacon.EnableKeyword("_EMISSION");
            beacon.SetColor("_EmissionColor", new Color(2.2f, 1.4f, .4f));
            foreach (var t in Data.layout.towers)
            {
                var ground = Data.HeightAt(t.x, t.z);
                var tower = new GameObject("Tower: " + t.name).transform;
                tower.SetParent(root, false);
                tower.position = new Vector3(t.x, ground, t.z);
                Part(tower, PrimitiveType.Cylinder, new Vector3(0, 14, 0), new Vector3(9, 14, 9), wall, true);      // 28 m shaft
                Part(tower, PrimitiveType.Cylinder, new Vector3(0, 29, 0), new Vector3(12, 1.2f, 12), wall, false); // balcony
                for (var k = 0; k < 8; k++)                                                                          // battlements
                {
                    var ang = k * Mathf.PI / 4;
                    Part(tower, PrimitiveType.Cube, new Vector3(Mathf.Cos(ang) * 5.4f, 31.2f, Mathf.Sin(ang) * 5.4f), new Vector3(1.6f, 2f, 1.6f), wall, false);
                }
                Part(tower, PrimitiveType.Sphere, new Vector3(0, 33, 0), Vector3.one * 3.2f, beacon, false);
                Part(tower, PrimitiveType.Cylinder, new Vector3(0, 38, 0), new Vector3(.3f, 4, .3f), wall, false);
                Part(tower, PrimitiveType.Cube, new Vector3(1.6f, 40.6f, 0), new Vector3(3.2f, 1.6f, .1f), roof, false); // pennant
                Part(tower, PrimitiveType.Cube, new Vector3(0, 1.8f, -4.4f), new Vector3(3, 3.6f, .6f), roof, false);   // door
                var light = new GameObject("Beacon light").AddComponent<Light>();
                light.transform.SetParent(tower, false);
                light.transform.localPosition = new Vector3(0, 33, 0);
                light.type = LightType.Point; light.range = 60; light.intensity = 4; light.color = new Color(1f, .75f, .4f);
                var info = tower.gameObject.AddComponent<TowerInfo>();
                info.towerName = t.name; info.realm = t.realm; info.sceneId = t.sceneId;
                Towers.Add(tower);
            }
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Destroy(go.GetComponent<Collider>());
        }

        // ---------- island name signs ----------
        void BuildSigns()
        {
            var root = new GameObject("Island names").transform;
            root.SetParent(transform, false);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var isl in Data.layout.islands)
            {
                var go = new GameObject("Name: " + isl.name);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(isl.labelX, Data.HeightAt(isl.labelX, isl.labelZ) + 45f, isl.labelZ);
                var text = go.AddComponent<TextMesh>();
                text.text = isl.name;
                text.font = font; text.fontSize = 64; text.characterSize = 1.6f;
                text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
                text.color = new Color(1f, .97f, .88f);
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                go.AddComponent<Billboard>();
            }
        }
    }
}
