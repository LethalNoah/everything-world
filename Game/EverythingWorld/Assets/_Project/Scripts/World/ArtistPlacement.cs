using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Moves artists that sit too close to another land inward, at runtime, before the land is built
    /// (call it from a kit's ShapeGround). Only the artists that are too close move; each goes to an open spot
    /// well inside the land, right beside the nearest other artist area (the layout puts similar artists next
    /// to each other), without wandering far from where it was and pulled a little toward where you arrive.
    /// Everyone else stays exactly where they are.
    public static class ArtistPlacement
    {
        /// minBorder: how far an artist must be from any other land (the land audit asks for 45 m).
        /// heartClear: keep this far from where you arrive (the plaza). keep: names never moved.
        /// avoid: other places to keep clear of (x, radius, z), such as a lake or a big landmark.
        public static void KeepFromBorders(IslandWorldData data, int index, float minBorder = 48f, float heartClear = 40f,
                                           ICollection<string> keep = null, IList<Vector3> avoid = null, float reach = 260f,
                                           float inward = .2f, string land = null)
        {
            var isl = data.layout.islands[index]; var sid = isl.sceneId;
            var heart = new Vector2(isl.labelX, isl.labelZ);
            int Id(float x, float z)
            {
                var cc = Mathf.RoundToInt(x / data.Step); var rr = Mathf.RoundToInt((data.layout.depth - z) / data.Step);
                if (cc < 0 || rr < 0 || cc >= data.Nx || rr >= data.Nz) return 255;
                var i = rr * data.Nx + cc;
                return data.heights[i] > 1.5f ? data.islandIds[i] : 254;      // 254: beach or water
            }
            // nearest other land, the way the land audit measures it
            float Border(float x, float z, float upTo)
            {
                for (var r = 5f; r <= upTo; r += 4f)
                    for (var k = 0; k < 32; k++)
                    {
                        var a = k * Mathf.PI / 16f; var o = Id(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r);
                        if (o != index && o < 254) return r;
                    }
                return 999f;
            }
            bool Firm(float x, float z, float r)
            {
                if (Id(x, z) != index) return false;
                for (var k = 0; k < 12; k++)
                {
                    var a = k * Mathf.PI / 6f;
                    if (Id(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r) != index) return false;
                }
                return true;
            }

            var arts = new List<IslandWorldData.Artist>();
            foreach (var a in data.layout.artists) if (a.sceneId == sid) arts.Add(a);
            foreach (var a in arts)
            {
                if (keep != null && keep.Contains(a.name)) continue;
                if (Border(a.x, a.z, minBorder) >= minBorder) continue;
                var from = new Vector2(a.x, a.z);
                var foot = Mathf.Clamp(a.r * .6f, 8f, 16f);
                var best = float.MaxValue; var at = from;
                for (var x = from.x - reach; x <= from.x + reach; x += 4f)
                for (var z = from.y - reach; z <= from.y + reach; z += 4f)
                {
                    var cand = new Vector2(x, z);
                    var moved = Vector2.Distance(cand, from);
                    if (moved > reach) continue;
                    var gap = 0f; var first = true;
                    foreach (var o in arts)
                    {
                        if (o == a) continue;
                        var g = Vector2.Distance(cand, new Vector2(o.x, o.z)) - o.r;
                        if (first || g < gap) { gap = g; first = false; }
                    }
                    var score = .5f * moved + gap + inward * Vector2.Distance(cand, heart);
                    if (score >= best) continue;
                    if (Vector2.Distance(cand, heart) < heartClear + a.r) continue;
                    var ok = true;
                    foreach (var o in arts)
                        if (o != a && Vector2.Distance(cand, new Vector2(o.x, o.z)) < o.r + a.r + 6f) { ok = false; break; }
                    if (ok && avoid != null)
                        foreach (var v in avoid)
                            if (Vector2.Distance(cand, new Vector2(v.x, v.z)) < v.y + a.r) { ok = false; break; }
                    if (!ok || !Firm(x, z, foot) || Border(x, z, minBorder) < minBorder) continue;
                    best = score; at = cand;
                }
                if (best == float.MaxValue) { Debug.LogWarning($"{land ?? isl.name}: no room further inside the land for {a.name}"); continue; }
                Debug.Log($"{land ?? isl.name}: moved {a.name} {Vector2.Distance(at, from):0} m inward, away from the next land");
                a.x = at.x; a.z = at.y;
            }
        }
    }
}
