using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The story scenes answering their songs (part of FolkLife): every glow an artist's scene registered with its song
    /// brightens and breathes while that song plays; the lighthouse beam turns, sweeping fast for "The Wreck of the
    /// Edmund Fitzgerald"; the village's bulbs and windows glow warmer on the land's own songs.
    public partial class FolkLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public Vector3 lighthouse, village;
        Transform beam; readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        void StoriesStart()
        {
            if (lighthouse == Vector3.zero) return;
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Lighthouse beam" };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetColor("_BaseColor", new Color(1f, .9f, .6f, .35f));
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            foreach (var s in new[] { 1f, -1f })
            {
                var b = v.Count; v.Add(Vector3.zero); v.Add(new Vector3(-2f, -1f, 40f * s)); v.Add(new Vector3(2f, -1f, 40f * s)); v.Add(new Vector3(0, 1.5f, 40f * s));
                col.Add(Color.white); col.Add(new Color(1, 1, 1, 0)); col.Add(new Color(1, 1, 1, 0)); col.Add(new Color(1, 1, 1, 0));
                tri.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3, b, b + 3, b + 1 });
            }
            var mesh = new Mesh { name = "Lighthouse beam" }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var go = new GameObject("Lighthouse beam"); go.transform.SetParent(transform, false); go.transform.position = lighthouse;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam = go.transform;
        }

        void StoriesUpdate(float dt, float t, string id, bool here)
        {
            foreach (var (song, m, c) in songGlows)
            {
                if (m == null) continue;
                songAmount.TryGetValue(song, out var k);
                k = Mathf.MoveTowards(k, here && !string.IsNullOrEmpty(song) && id == song ? 1f : 0f, dt / 2f); songAmount[song] = k;
                m.SetColor("_EmissionColor", c * (1f + k * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            }
            if (beam != null)
            {
                songAmount.TryGetValue(FolkKit.EdmundFitzgerald, out var storm);
                beam.Rotate(0, (30f + storm * 90f) * dt, 0, Space.World);
            }
        }
    }
}
