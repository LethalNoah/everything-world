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

        [Header("Look")]
        [Tooltip("Album covers for artist plaques: Assets/_Project/Art/CoverAtlas.jpg")]
        public Texture2D coverAtlas;
        [Tooltip("Use the Toon Shaders Pro shader for built scenery, if it's imported.")]
        public bool toonShading = true;
        [Tooltip("Shimmering curtains where lands meet, at bridge ends and round artist-radio areas.")]
        public bool zoneFields = true;
        [Tooltip("Frames per second to aim for. Without a cap the editor runs the GPU flat out (loud fans).")]
        public int targetFrameRate = 60;
        [Tooltip("In the editor, check each hand-built land after it's built (missing colliders, crowded or misplaced artists, empty stretches, light) and write a report and a picture to Logs/LandAudit/. Slow (it about doubles the start-up), so it's off for normal play; the photo tour always runs it.")]
        public bool auditLands = false;
        [Tooltip("Build a land's scenery only when you come within this many metres of where you arrive in it, nearest first, one land a frame (a short pause as each one appears). 0 builds every land at the start, which takes a minute or two.")]
        public float buildDistance = 1200f;
        [Tooltip("Build every land while a loading screen shows at the start (about half a minute to a minute), so there are no pauses later as you travel. Off: lands are built as you come within buildDistance of them.")]
        public bool loadAllAtStart = true;
        [Tooltip("Hide grass, small props, glows and lights on built lands when they're far away (LandCuller). Turn off to compare.")]
        public bool cullByDistance = true;

        public static IslandWorldBuilder Instance { get; private set; }
        public IslandWorldData Data { get; private set; }
        public List<Transform> Towers { get; } = new List<Transform>();

        LandKit[] kits;
        readonly HashSet<int> dressed = new HashSet<int>();
        readonly List<int> pending = new List<int>();   // lands with a kit, not built yet
        Transform landsRoot; LandCuller culler; Transform player;

        /// True for lands built in full (with a kit). Artist stations and rings only exist there for now,
        /// so you don't hit an invisible circle in an empty field that changes the music.
        public bool IsDressed(int sceneId) => dressed.Contains(sceneId);

        /// Set by the photo tour's quick mode (take_shots.py --quick): build only these lands (names without "The ")
        /// and skip the land audit, so a quick look at one place takes about a minute instead of six. The other
        /// lands stay bare ground, so far views are not to be trusted in a quick photo.
        public static HashSet<string> QuickLands;
        /// Set by the photo tour: build every land at the start (the tour needs them all) and, unless it's a quick tour, audit them.
        public static bool BuildEverything, ForceAudit;
        /// Set by the photo tour: audit only these lands (names without "The "); null audits every land built.
        public static HashSet<string> AuditOnly;
        /// True while the loading screen is up (every land being built at the start).
        public bool Loading { get; private set; }
        int loadTotal, loadDone; string loadName; float loadStarted;
        EverythingWorld.Player.SimplePlayerController heldPlayer;

        /// True once every land with a kit has been built.
        public bool AllBuilt => pending.Count == 0;
        static bool Wanted(string land) => QuickLands == null || QuickLands.Contains(land.StartsWith("The ") ? land.Substring(4) : land);

        // shared terrain colours
        static readonly Color Seabed = new Color32(126, 136, 104, 255);
        static readonly Color Sand = new Color32(222, 205, 160, 255);
        static readonly Color Rock = new Color32(138, 126, 112, 255);
        static readonly Color Peak = new Color32(214, 210, 200, 255);

        void Awake()
        {
            Instance = this;
            WorldMaterials.UseToon = toonShading;
            IndoorSpaces.Clear();
            SurpriseSpots.Clear();
            if (GetComponent<SurpriseDirector>() == null) gameObject.AddComponent<SurpriseDirector>();
            Data = IslandWorldData.Load(layoutJson, heightBytes, islandBytes);
            kits = new LandKit[Data.layout.islands.Length];
            for (var i = 0; i < kits.Length; i++)
            {
                kits[i] = Wanted(Data.layout.islands[i].name) ? LandKit.For(Data.layout.islands[i].name) : null;
                if (kits[i] != null) dressed.Add(Data.layout.islands[i].sceneId);
            }
            for (var i = 0; i < kits.Length; i++)
                if (kits[i] != null)
                    try { kits[i].ShapeGround(Data, i); } catch (System.Exception e) { Debug.LogException(e); }
            var t0 = Time.realtimeSinceStartup;
            BuildTerrain();
            BuildSea();
            BuildBridges();
            BuildTowers();
            if (islandNameSigns) BuildSigns();
            DressLands();
            if (zoneFields) ZoneFields.Build(Data, transform, IslandColors(), IsDressed);
            Application.targetFrameRate = targetFrameRate; // the editor otherwise renders as fast as the GPU can
            Debug.Log($"Island world built in {(Time.realtimeSinceStartup - t0) * 1000:0} ms: " +
                      $"{Data.layout.islands.Length} islands, {Data.layout.bridges.Length} crossings, {Data.layout.towers.Length} towers");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static Material Mat(Color c, float smooth = .08f) => WorldMaterials.Solid(c, smooth);

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
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterials = mats;
            // The ground receives shadows (trees, buildings) but doesn't cast them: with a low sun, every
            // facet of the low-poly hills would shade the ones behind it, giving a jagged dark patch round
            // the player that ends where Unity stops drawing detailed shadows.
            rend.shadowCastingMode = ShadowCastingMode.Off;
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
            var mat = Mat(Quantise(TerrainColor(avg, slope, island, col, row)));
            if (!byMaterial.TryGetValue(mat, out var list)) byMaterial[mat] = list = new List<int>();
            list.Add(verts.Count); verts.Add(a);
            list.Add(verts.Count); verts.Add(b);
            list.Add(verts.Count); verts.Add(c);
        }

        /// Round a ground colour to one of a limited set of shades. Each distinct colour is its own material,
        /// drawn separately, and the built lands blend their ground colours smoothly: unrounded, one terrain chunk
        /// had thousands of materials, and looking toward the built lands took 140 ms a frame (4 ms without the
        /// terrain). 24 steps per channel can't be seen on low-poly ground.
        static Color Quantise(Color c)
        {
            const float Q = 24f;
            return new Color(Mathf.Round(c.r * Q) / Q, Mathf.Round(c.g * Q) / Q, Mathf.Round(c.b * Q) / Q, 1f);
        }

        Color TerrainColor(float height, float slope, int island, int col, int row)
        {
            if (height < -.2f) return Seabed;
            if (height < 1.6f || island < 0) return Sand;
            // patches about 40 m across
            var patch = Mathf.PerlinNoise(col * .12f + island * 7.3f, row * .12f);
            if (kits[island] != null && kits[island].Ground(height, slope, patch, out var kitColor)) return kitColor;
            if (height > 38f) return Peak;
            if (slope > 7f || height > 26f) return Rock;
            var isl = Data.layout.islands[island];
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
            // the Water shader: ripples, shallows and depths, shore foam, sun glitter and a hint of sky reflection
            var water = Shader.Find("Everything World/Water");
            var r = sea.GetComponent<Renderer>();
            r.sharedMaterial = water != null ? new Material(water) { name = "Sea" } : Mat(new Color32(74, 128, 140, 255), .85f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---------- bridges and causeways ----------
        /// The deck of a crossing, as points along its middle (on the deck's surface), every few metres from the
        /// first land to the second. Each end walks inland from the planned landing until it reaches solid ground
        /// above the water, so no crossing stops short in the sea; over land the deck follows the ground (never
        /// under a rise, and flush with the ground where you step off); over water it runs straight between the
        /// shores, at least a metre above the sea. Shared with the bridge check in the photo tour.
        public static List<Vector3> BridgePath(IslandWorldData data, IslandWorldData.Bridge b)
        {
            var a = new Vector3(b.ax, 0, b.az); var e = new Vector3(b.bx, 0, b.bz);
            var along = e - a; along.y = 0; along.Normalize();
            bool Dry(Vector3 q, int island) { var isl = data.IslandAt(q.x, q.z); return isl != null && isl.sceneId == island && data.HeightAt(q.x, q.z) > .3f; }
            Vector3 Land(Vector3 p, Vector3 outward, int island)
            {
                // straight on along the crossing until it's dry ground on the right land (with dry ground beyond)
                for (var s = 0f; s <= 40f; s += 1f) { var q = p + outward * s; if (Dry(q, island) && Dry(q + outward * 2f, island)) return q + outward * 1.5f; }
                // else the nearest dry ground of that land, searching outward in rings (the crossing bends a little)
                for (var r = 2f; r <= 90f; r += 2f)
                {
                    Vector3 best = Vector3.zero; var bd = float.MaxValue;
                    for (var k = 0; k < 48; k++) { var ang = k * Mathf.PI / 24f; var q = p + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * r; if (!Dry(q, island)) continue; var dd = Vector3.Dot(q - p, outward); if (dd < -2f) continue; var score = (q - p).sqrMagnitude - dd * 2f; if (score < bd) { bd = score; best = q; } }
                    if (bd < float.MaxValue) { var o = (best - p); o.y = 0; o.Normalize(); return best + o * 1.5f; }
                }
                return p;
            }
            a = Land(a, -along, b.a); e = Land(e, along, b.b);
            var len = Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(e.x, 0, e.z));
            var n = Mathf.Max(2, Mathf.CeilToInt(len / 3f));
            float ya = Mathf.Max(data.HeightAt(a.x, a.z), 1f) + .15f, ye = Mathf.Max(data.HeightAt(e.x, e.z), 1f) + .15f;
            var pts = new List<Vector3>();
            for (var k = 0; k <= n; k++)
            {
                var t = k / (float)n; var p = Vector3.Lerp(a, e, t);
                var ground = data.HeightAt(p.x, p.z);
                var y = Mathf.Max(Mathf.Lerp(ya, ye, t), ground + .15f, 1f);
                pts.Add(new Vector3(p.x, y, p.z));
            }
            // soften: a deck can't kink sharply from one plank to the next (but never sink into the ground)
            for (var pass = 0; pass < 3; pass++)
                for (var k = 1; k < pts.Count - 1; k++)
                {
                    var p = pts[k]; var avg = (pts[k - 1].y + pts[k + 1].y) / 2f;
                    p.y = Mathf.Max(avg, data.HeightAt(p.x, p.z) + .15f, 1f); pts[k] = p;
                }
            return pts;
        }

        void BuildBridges()
        {
            var root = new GameObject("Bridges").transform;
            root.SetParent(transform, false);
            var wood = Mat(new Color32(140, 98, 62, 255));
            var stone = Mat(new Color32(196, 184, 160, 255));
            var up = Vector3.up;
            foreach (var b in Data.layout.bridges)
            {
                var pts = BridgePath(Data, b);
                var width = b.causeway ? 6f : 3.5f; var mat = b.causeway ? stone : wood;
                var mk = new MeshKit(); var walls = new MeshKit();
                var go = new GameObject((b.causeway ? "Causeway " : "Bridge ") + b.a + "-" + b.b).transform; go.SetParent(root, false);
                var post = 0f; var pier = 8f; var run = 0f;
                for (var k = 0; k + 1 < pts.Count; k++)
                {
                    Vector3 p = pts[k], q = pts[k + 1]; var d = q - p; var len = d.magnitude; if (len < .01f) continue;
                    var rot = Quaternion.LookRotation(d / len, up); var side = rot * Vector3.right; var mid = (p + q) / 2f;
                    mk.Box(mat, mid - rot * Vector3.up * .2f, new Vector3(width, .4f, len + .12f), rot);
                    walls.Box(mat, mid - rot * Vector3.up * .2f, new Vector3(width, .4f, len + .12f), rot);
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var off = side * (width / 2f - .15f) * s;
                        mk.Box(mat, mid + off + up * .9f, new Vector3(.18f, .12f, len + .05f), rot);
                        walls.Box(mat, mid + side * (width / 2f) * s + up * 1f, new Vector3(.2f, 2.2f, len + .05f), rot);   // keeps people from falling off
                    }
                    for (; post <= run + len; post += b.causeway ? 6f : 3f)
                    {
                        var t = (post - run) / len; var pp = Vector3.Lerp(p, q, t);
                        foreach (var s in new[] { -1f, 1f }) mk.Box(mat, pp + side * (width / 2f - .15f) * s + up * .45f, new Vector3(.25f, .9f, .25f), rot);
                    }
                    if (b.causeway)
                        for (; pier <= run + len; pier += 14f)
                        {
                            var pp = Vector3.Lerp(p, q, (pier - run) / len); var ground = Data.HeightAt(pp.x, pp.z);
                            if (ground > pp.y - 1.5f) continue;   // on land the deck sits on the ground
                            var depth = pp.y - Mathf.Min(ground, 0f) + 10f;
                            mk.Box(stone, pp - up * (depth / 2f + .4f), new Vector3(width * .8f, depth, 2.4f), Quaternion.LookRotation(new Vector3(d.x, 0, d.z)));
                        }
                    run += len;
                }
                var deck = mk.Build("Deck", go, false); if (deck != null) deck.isStatic = true;
                walls.BuildCollider("Deck and edges", go);
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
            var beacon = WorldMaterials.Glow(new Color32(240, 180, 70, 255), 2.4f);
            foreach (var t in Data.layout.towers)
            {
                var ground = Data.HeightAt(t.x, t.z);
                var tower = new GameObject("Tower: " + t.name).transform;
                tower.SetParent(root, false);
                tower.position = new Vector3(t.x, ground, t.z);
                if (!TowerStyles.Build(tower, t))     // each realm's own tower; the hub's mast keeps the plain shape
                {
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
                }
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

        /// Each land's curtain colour, from its look-brief palette in scenes.json (the MusicDirector's data).
        Color[] IslandColors()
        {
            var director = FindFirstObjectByType<EverythingWorld.Music.MusicDirector>();
            var catalog = director != null && director.sceneData != null ? EverythingWorld.Music.SceneCatalog.FromJson(director.sceneData) : null;
            var colors = new Color[Data.layout.islands.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                var isl = Data.layout.islands[i];
                var fallback = Color.Lerp(Rgb(isl.tintA), Color.white, .3f);
                colors[i] = ZoneFields.VividColor(catalog?.Get(isl.sceneId)?.palette, fallback);
            }
            return colors;
        }

        // ---------- lands with a kit: scenery and artist areas ----------
        void DressLands()
        {
            landsRoot = new GameObject("Lands").transform;
            landsRoot.SetParent(transform, false);
            // hide what's too far away to matter (detail, small props, glows, lights), so views across built
            // lands stay smooth; each land is added to it as it's built
            if (cullByDistance) culler = gameObject.AddComponent<LandCuller>();
            for (var i = 0; i < kits.Length; i++) if (kits[i] != null) pending.Add(i);
            var p = FindFirstObjectByType<EverythingWorld.Player.SimplePlayerController>();
            player = p != null ? p.transform : null;
            // build the lands near where you start (the spawn point, where IslandExplorer puts you on Play) now; the rest
            // as you come near them (Update)
            var everything = BuildEverything || buildDistance <= 0f;
            var start = new Vector3(Data.layout.spawnX, 0f, Data.layout.spawnZ);
            var t0 = Time.realtimeSinceStartup; var n = 0;
            for (var k = pending.Count - 1; k >= 0; k--)
            {
                var i = pending[k];
                if (!everything && Distance(i, start) > buildDistance) continue;
                pending.RemoveAt(k); DressLand(i); n++;
            }
            Debug.Log($"Built {n} lands at the start in {(Time.realtimeSinceStartup - t0) * 1000:0} ms; {pending.Count} more " + (loadAllAtStart && !everything && pending.Count > 0 ? "behind the loading screen" : "will be built as you come near them"));
            // the rest behind the loading screen, nearest first, one a frame (so the screen can show progress); you
            // stay put until it's done
            if (loadAllAtStart && !everything && pending.Count > 0)
            {
                Loading = true; loadTotal = pending.Count; loadDone = 0; loadStarted = Time.realtimeSinceStartup;
                heldPlayer = p; if (heldPlayer != null) heldPlayer.enabled = false;
            }
        }

        float Distance(int i, Vector3 at)
        {
            var isl = Data.layout.islands[i]; return Vector2.Distance(new Vector2(isl.labelX, isl.labelZ), new Vector2(at.x, at.z));
        }

        /// Builds the nearest unbuilt land once you're within buildDistance of it: one land a frame.
        void Update()
        {
            if (pending.Count == 0) return;
            var at = player != null ? player.position : Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            if (Loading)
            {
                int near = 0; for (var k = 1; k < pending.Count; k++) if (Distance(pending[k], at) < Distance(pending[near], at)) near = k;
                var li = pending[near]; pending.RemoveAt(near); loadName = Data.layout.islands[li].name;
                DressLand(li); loadDone++;
                if (pending.Count == 0)
                {
                    Loading = false; if (heldPlayer != null) heldPlayer.enabled = true;
                    Debug.Log($"Loading screen: built {loadTotal} lands in {Time.realtimeSinceStartup - loadStarted:0.0} s");
                }
                return;
            }
            int best = -1; var bestD = buildDistance;
            for (var k = 0; k < pending.Count; k++) { var d = Distance(pending[k], at); if (d <= bestD) { bestD = d; best = k; } }
            if (best < 0) return;
            var i = pending[best]; pending.RemoveAt(best);
            DressLand(i);
        }

        void DressLand(int i)
        {
            var t0 = Time.realtimeSinceStartup; var audit = 0f;
            try
            {
                var ctx = new LandContext(Data, i, landsRoot, coverAtlas);
                kits[i].Dress(ctx);
                foreach (var a in ctx.Artists) kits[i].BuildArtist(ctx, a);
                kits[i].Finish(ctx);
                if ((auditLands || ForceAudit) && Application.isEditor && QuickLands == null && (AuditOnly == null || AuditOnly.Contains(kits[i].Land.StartsWith("The ") ? kits[i].Land.Substring(4) : kits[i].Land)))
                {
                    var a0 = Time.realtimeSinceStartup;
                    try { LandAudit.Run(Data, ctx, kits[i].Land); } catch (System.Exception e) { Debug.LogException(e); }
                    audit = Time.realtimeSinceStartup - a0;
                }
                if (culler != null) culler.Add(ctx.Root);
                Debug.Log($"{kits[i].Land}: dressed in {(Time.realtimeSinceStartup - t0 - audit) * 1000:0} ms" + (audit > 0f ? $" (+{audit * 1000:0} ms audit)" : "") + $", {ctx.Artists.Count} artist areas");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e); // one broken land should not stop the rest of the world
            }
        }

        // ---------- the loading screen ----------
        GUIStyle loadTitle, loadText; Texture2D loadBack, loadBar, loadBarOff;
        void OnGUI()
        {
            if (!Loading) return;
            if (loadBack == null)
            {
                loadBack = new Texture2D(1, 1); loadBack.SetPixel(0, 0, new Color(.05f, .045f, .06f)); loadBack.Apply();
                loadBar = new Texture2D(1, 1); loadBar.SetPixel(0, 0, new Color(1f, .8f, .45f)); loadBar.Apply();
                loadBarOff = new Texture2D(1, 1); loadBarOff.SetPixel(0, 0, new Color(1f, 1f, 1f, .12f)); loadBarOff.Apply();
                loadTitle = new GUIStyle { font = GameFonts.Sign, fontSize = 46, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, .86f, .6f) } };
                loadText = new GUIStyle { font = GameFonts.Speech, fontSize = 18, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.85f, .83f, .8f) } };
            }
            GUI.depth = -100;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), loadBack);
            var cy = Screen.height / 2f;
            GUI.Label(new Rect(0, cy - 90, Screen.width, 60), "THE EVERYTHING WORLD", loadTitle);
            GUI.Label(new Rect(0, cy - 20, Screen.width, 30), loadName != null ? $"Building {loadName}\u2026" : "Getting ready\u2026", loadText);
            var w = Mathf.Min(520f, Screen.width - 80f); var bar = new Rect((Screen.width - w) / 2f, cy + 24, w, 8);
            GUI.DrawTexture(bar, loadBarOff); GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * (loadTotal > 0 ? loadDone / (float)loadTotal : 0f), bar.height), loadBar);
            GUI.Label(new Rect(0, cy + 40, Screen.width, 30), $"{loadDone} of {loadTotal} lands", loadText);
        }

        // ---------- island name signs ----------
        void BuildSigns()
        {
            var root = new GameObject("Island names").transform;
            root.SetParent(transform, false);
            var font = GameFonts.Sign;   // must match ArtistBits.TextMaterial (its letters come from this font)
            var textMat = ArtistBits.TextMaterial;
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
                go.GetComponent<MeshRenderer>().sharedMaterial = textMat;
                go.AddComponent<Billboard>().fadeWithFog = true;
            }
        }
    }
}
