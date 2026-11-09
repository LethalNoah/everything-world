using System;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The island world as exported by build/islandmap.py: a height grid, which island owns each point,
    /// and the bridges, causeways and towers. 1 unit = 1 metre. Unity x = map x, Unity z = map height - map y
    /// (so north on the map is +z in the game).
    public class IslandWorldData
    {
        [Serializable] public class Island { public int sceneId; public string name, realm, hi, lo; public float labelX, labelZ; public int[] tintA, tintB; }
        [Serializable] public class Bridge { public int a, b; public bool causeway; public float ax, az, bx, bz; }
        [Serializable] public class Tower { public string name, realm; public int sceneId; public float x, z; }
        [Serializable] public class Layout
        {
            public float width, depth, step, waterLevel, spawnX, spawnZ;
            public int nx, nz;
            public Island[] islands;
            public Bridge[] bridges;
            public Tower[] towers;
        }

        public Layout layout;
        public float[] heights;   // metres, index = row * nx + col, row 0 = north edge
        public byte[] islandIds;  // index into layout.islands, 255 = water

        public int Nx => layout.nx;
        public int Nz => layout.nz;
        public float Step => layout.step;

        public static IslandWorldData Load(TextAsset layoutJson, TextAsset heightBytes, TextAsset islandBytes)
        {
            var d = new IslandWorldData { layout = JsonUtility.FromJson<Layout>(layoutJson.text) };
            var raw = heightBytes.bytes;
            d.heights = new float[raw.Length / 2];
            for (var i = 0; i < d.heights.Length; i++) d.heights[i] = (short)(raw[2 * i] | (raw[2 * i + 1] << 8)) / 10f;
            d.islandIds = islandBytes.bytes;
            if (d.heights.Length != d.Nx * d.Nz || d.islandIds.Length != d.Nx * d.Nz)
                Debug.LogError("Island world data sizes don't match world_layout.json. Rerun build/islandmap.py.");
            return d;
        }

        /// Height of the grid vertex in column col (west to east) and row (north to south).
        public float VertexHeight(int col, int row) =>
            heights[Mathf.Clamp(row, 0, Nz - 1) * Nx + Mathf.Clamp(col, 0, Nx - 1)];

        /// World position of a grid vertex.
        public Vector3 VertexPosition(int col, int row) =>
            new Vector3(col * Step, VertexHeight(col, row), layout.depth - row * Step);

        /// Ground height at a world position (bilinear between grid vertices).
        public float HeightAt(float x, float z)
        {
            var fc = x / Step; var fr = (layout.depth - z) / Step;
            int c = Mathf.FloorToInt(fc), r = Mathf.FloorToInt(fr);
            float tx = fc - c, tz = fr - r;
            var top = Mathf.Lerp(VertexHeight(c, r), VertexHeight(c + 1, r), tx);
            var bottom = Mathf.Lerp(VertexHeight(c, r + 1), VertexHeight(c + 1, r + 1), tx);
            return Mathf.Lerp(top, bottom, tz);
        }

        /// The island under a world position, or null over water.
        public Island IslandAt(float x, float z)
        {
            var c = Mathf.RoundToInt(x / Step); var r = Mathf.RoundToInt((layout.depth - z) / Step);
            if (c < 0 || r < 0 || c >= Nx || r >= Nz) return null;
            var id = islandIds[r * Nx + c];
            return id == 255 ? null : layout.islands[id];
        }

        public byte IslandIndexAtVertex(int col, int row) =>
            islandIds[Mathf.Clamp(row, 0, Nz - 1) * Nx + Mathf.Clamp(col, 0, Nx - 1)];
    }
}
