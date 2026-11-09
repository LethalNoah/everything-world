using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Everything a land kit needs to dress one land: where its ground is, where it is free to build
    /// (away from artist plots, the plaza, towers and bridge ends), a random generator seeded by the
    /// land so it looks the same every time, and the cover atlas for album art.
    public class LandContext
    {
        public const float PlazaRadius = 55f;

        public readonly IslandWorldData Data;
        public readonly int Index;
        public readonly IslandWorldData.Island Island;
        public readonly Transform Root;
        public readonly System.Random Rng;
        public readonly Texture CoverAtlas;
        public readonly List<IslandWorldData.Artist> Artists = new List<IslandWorldData.Artist>();
        public readonly Vector3 Plaza;
        public float MinX, MaxX, MinZ, MaxZ;

        readonly List<Vector3> keepouts = new List<Vector3>(); // x, z = centre, y = radius

        public LandContext(IslandWorldData data, int index, Transform parent, Texture coverAtlas)
        {
            Data = data; Index = index; Island = data.layout.islands[index]; CoverAtlas = coverAtlas;
            Rng = new System.Random(7919 * (Island.sceneId + 11));
            Root = new GameObject("Land: " + Island.name).transform;
            Root.SetParent(parent, false);
            Plaza = OnGround(Island.labelX, Island.labelZ);

            // the land's extent, from the island grid
            int c0 = data.Nx, c1 = 0, r0 = data.Nz, r1 = 0;
            for (var r = 0; r < data.Nz; r++)
            for (var c = 0; c < data.Nx; c++)
                if (data.islandIds[r * data.Nx + c] == index)
                {
                    if (c < c0) c0 = c; if (c > c1) c1 = c; if (r < r0) r0 = r; if (r > r1) r1 = r;
                }
            MinX = c0 * data.Step; MaxX = c1 * data.Step; MaxZ = data.layout.depth - r0 * data.Step; MinZ = data.layout.depth - r1 * data.Step;

            foreach (var a in data.layout.artists ?? new IslandWorldData.Artist[0])
                if (a.sceneId == Island.sceneId) { Artists.Add(a); AddKeepout(a.x, a.z, a.r); }
            AddKeepout(Plaza.x, Plaza.z, PlazaRadius);
            foreach (var t in data.layout.towers) AddKeepout(t.x, t.z, 40f);
            foreach (var b in data.layout.bridges) { AddKeepout(b.ax, b.az, 22f); AddKeepout(b.bx, b.bz, 22f); }
        }

        public float Ground(float x, float z) => Data.HeightAt(x, z);
        public Vector3 OnGround(float x, float z, float lift = 0f) => new Vector3(x, Data.HeightAt(x, z) + lift, z);

        bool IsThisLand(float x, float z)
        {
            var c = Mathf.RoundToInt(x / Data.Step); var r = Mathf.RoundToInt((Data.layout.depth - z) / Data.Step);
            return c >= 0 && r >= 0 && c < Data.Nx && r < Data.Nz && Data.islandIds[r * Data.Nx + c] == Index;
        }

        /// On this land, above the beach, and at least 'inland' metres from its edge (roughly).
        public bool IsLand(float x, float z, float inland)
        {
            if (!IsThisLand(x, z) || Ground(x, z) < 1.5f) return false;
            if (inland <= 0f) return true;
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f;
                if (!IsThisLand(x + Mathf.Cos(a) * inland, z + Mathf.Sin(a) * inland)) return false;
            }
            return true;
        }

        public void AddKeepout(float x, float z, float r) => keepouts.Add(new Vector3(x, r, z));

        /// Not inside any artist plot, the plaza, a tower or a bridge end (plus 'pad' metres).
        public bool IsFree(float x, float z, float pad)
        {
            foreach (var k in keepouts)
            {
                float dx = x - k.x, dz = z - k.z, rr = k.y + pad;
                if (dx * dx + dz * dz < rr * rr) return false;
            }
            return true;
        }

        public float Range(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
        public bool Chance(float p) => Rng.NextDouble() < p;

        /// A random free spot on this land.
        public bool TrySpot(out Vector3 p, float inland, float pad, int tries = 60)
        {
            for (var i = 0; i < tries; i++)
            {
                float x = Range(MinX, MaxX), z = Range(MinZ, MaxZ);
                if (IsLand(x, z, inland) && IsFree(x, z, pad)) { p = OnGround(x, z); return true; }
            }
            p = default;
            return false;
        }

        /// The texture rectangle of one album cover on the cover sheet, from the sheet's layout as exported
        /// by islandmap.py, not the imported texture's size, because Unity may resize the texture when
        /// importing it (rectangles in 0..1 texture space don't change).
        public Rect CoverRect(int index)
        {
            var l = Data.layout;
            int cols = l.coverCols > 0 ? l.coverCols : 40, cell = l.coverCell > 0 ? l.coverCell : 96;
            float sheetW = l.coverSheetWidth > 0 ? l.coverSheetWidth : 3840f, sheetH = l.coverSheetHeight > 0 ? l.coverSheetHeight : 3744f;
            int col = index % cols, row = index / cols;
            float du = cell / sheetW, dv = cell / sheetH;
            // inset by a hair so a neighbouring cover never bleeds in at the edges
            const float inset = .004f;
            return new Rect((col + inset) * du, 1f - (row + 1 - inset) * dv, du * (1 - 2 * inset), dv * (1 - 2 * inset));
        }
    }
}
