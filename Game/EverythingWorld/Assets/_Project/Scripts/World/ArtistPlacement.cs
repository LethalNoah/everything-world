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
        /// Spread a crowded land out so there's room to wander between the artists. The layout keeps its shape
        /// (similar artists stay near each other) but is scaled up round 'about' (normally where you arrive)
        /// until neighbours are about 'spacing' metres apart, radio-station artists less so (they stay nearer the
        /// heart), and centred on where you arrive, so you land among them (no one further out than maxReach).
        /// Then each artist, the biggest first, settles on the nearest open spot to where that puts it:
        /// on firm ground, at least minBorder from any other land, clear of the plaza, the 'avoid' places
        /// (x, radius, z) and the artists already settled. Not too packed, not out at the edges.
        public static void Spread(IslandWorldData data, int index, float spacing = 80f, Vector2? about = null, float minBorder = 48f,
                                  float heartClear = 40f, IList<Vector3> avoid = null, ICollection<string> keep = null, string land = null,
                                  float maxReach = 420f)
        {
            var isl = data.layout.islands[index]; var sid = isl.sceneId;
            var heart = new Vector2(isl.labelX, isl.labelZ);
            int Id(float x, float z)
            {
                var cc = Mathf.RoundToInt(x / data.Step); var rr = Mathf.RoundToInt((data.layout.depth - z) / data.Step);
                if (cc < 0 || rr < 0 || cc >= data.Nx || rr >= data.Nz) return 255;
                var i = rr * data.Nx + cc;
                return data.heights[i] > 1.5f ? data.islandIds[i] : 254;
            }
            bool NearBorder(float x, float z)
            {
                for (var r = 5f; r < minBorder; r += 4f)
                    for (var k = 0; k < 32; k++)
                    {
                        var a = k * Mathf.PI / 16f; var o = Id(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r);
                        if (o != index && o < 254) return true;
                    }
                return false;
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
            bool Station(IslandWorldData.Artist a) => a.station || a.stationId != 0;

            var arts = new List<IslandWorldData.Artist>();
            foreach (var a in data.layout.artists) if (a.sceneId == sid) arts.Add(a);
            if (arts.Count < 2) return;
            // how close neighbours are now (the median distance to the nearest other artist)
            var nn = new List<float>();
            foreach (var a in arts)
            {
                var m = float.MaxValue;
                foreach (var o in arts) if (o != a) m = Mathf.Min(m, Vector2.Distance(new Vector2(a.x, a.z), new Vector2(o.x, o.z)));
                nn.Add(m);
            }
            nn.Sort();
            var scale = Mathf.Clamp(spacing / Mathf.Max(nn[nn.Count / 2], 1f), 1f, 4f);
            var target = about ?? heart;
            var mid = Vector2.zero; foreach (var a in arts) mid += new Vector2(a.x, a.z); mid /= arts.Count;
            arts.Sort((p, q) => Station(p) != Station(q) ? (Station(p) ? -1 : 1) : p.songs != q.songs ? q.songs.CompareTo(p.songs) : string.CompareOrdinal(p.name, q.name));
            var placed = new List<IslandWorldData.Artist>();
            if (keep != null) foreach (var a in arts) if (keep.Contains(a.name)) placed.Add(a);
            foreach (var a in arts)
            {
                if (keep != null && keep.Contains(a.name)) continue;
                var pos = new Vector2(a.x, a.z);
                var off = (pos - mid) * (Station(a) ? 1f + (scale - 1f) * .6f : scale);
                if (off.magnitude > maxReach) off = off.normalized * maxReach;
                var goal = target + off;
                var foot = Mathf.Clamp(a.r * .6f, 8f, 16f);
                bool Ok(Vector2 p)
                {
                    if (Vector2.Distance(p, heart) < heartClear + a.r) return false;
                    foreach (var o in placed) if (Vector2.Distance(p, new Vector2(o.x, o.z)) < o.r + a.r + 10f) return false;
                    if (avoid != null) foreach (var v in avoid) if (Vector2.Distance(p, new Vector2(v.x, v.z)) < v.y + a.r) return false;
                    return Firm(p.x, p.y, foot) && !NearBorder(p.x, p.y);
                }
                var found = false; var at = goal;
                for (var r = 0f; r <= 320f && !found; r += 5f)
                {
                    var steps = r == 0f ? 1 : Mathf.Max(8, Mathf.RoundToInt(r * Mathf.PI * 2f / 5f));
                    for (var k = 0; k < steps; k++)
                    {
                        var ang = k * Mathf.PI * 2f / steps;
                        var cand = goal + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                        if (Ok(cand)) { at = cand; found = true; break; }
                    }
                }
                if (!found) { Debug.LogWarning($"{land ?? isl.name}: no open spot to spread {a.name} to"); placed.Add(a); continue; }
                a.x = at.x; a.z = at.y;
                placed.Add(a);
            }
            Debug.Log($"{land ?? isl.name}: spread the artists out {scale:0.0}x, centred on where you arrive");
        }

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
