using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The deep pass's moving parts (part of YachtLife): a big low sun sits on the horizon in a soft glow, a path of
    /// glittering light runs across the bay toward it; glows tied to songs brighten while their song plays; Daniel's
    /// plane circles high over Elton's area; koi swim round Steely Dan's pond; McCartney's searchlight sweeps;
    /// Redbone's sunburst turns.
    public partial class YachtLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public Transform plane; public Vector3 planeCenter;
        public List<(Transform t, Vector3 c, float r, float phase, Vector3 side, Vector3 front)> koi = new List<(Transform, Vector3, float, float, Vector3, Vector3)>();
        public List<Transform> searchlights = new List<Transform>(), spinners = new List<Transform>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        Transform sun, sunGlow, sunPath; Material sunM, glowM, pathM; ParticleSystem glints; float nextGlint;

        float Amount(string song) => songAmount.TryGetValue(song, out var k) ? k : 0f;

        static Material Soft(string name, bool add)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", add ? 2f : 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(add ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        static Transform FanMesh(Transform parent, string name, Material m, float alphaIn, float alphaOut, int n)
        {
            var v = new List<Vector3> { Vector3.zero }; var col = new List<Color> { new Color(1, 1, 1, alphaIn) }; var tri = new List<int>();
            for (var k = 0; k < n; k++) { var a = k * Mathf.PI * 2f / n; v.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0)); col.Add(new Color(1, 1, 1, alphaOut)); tri.AddRange(new[] { 0, 1 + k, 1 + (k + 1) % n }); }
            var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return go.transform;
        }

        void SunsetStart()
        {
            sunM = Soft("Sunset sun", false); sunM.SetColor("_BaseColor", new Color(1f, .82f, .55f, 1f)); sun = FanMesh(transform, "Sunset sun", sunM, 1f, 1f, 48);
            glowM = Soft("Sunset glow", true); glowM.SetColor("_BaseColor", new Color(1f, .55f, .3f, .55f)); sunGlow = FanMesh(transform, "Sunset glow", glowM, 1f, 0f, 48);
            // the path of light on the water: a long thin fan lying on the bay, bright near the sun and fading toward you
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            const int Steps = 14;
            for (var i = 0; i <= Steps; i++) { var f = i / (float)Steps; var w = Mathf.Lerp(1.5f, 9f, f); v.Add(new Vector3(-w, 0, f)); v.Add(new Vector3(w, 0, f)); var a = Mathf.SmoothStep(0f, 1f, f) * .8f; col.Add(new Color(1, 1, 1, 0)); col.Add(new Color(1, 1, 1, 0)); col[col.Count - 2] = new Color(1, 1, 1, a); col[col.Count - 1] = new Color(1, 1, 1, a); if (i > 0) tri.AddRange(new[] { 2 * i - 2, 2 * i - 1, 2 * i + 1, 2 * i - 2, 2 * i + 1, 2 * i }); }
            var tex = new Texture2D(8, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 8; x++) { var u = Mathf.Abs((x + .5f) / 8f * 2f - 1f); tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(1f - u, 1.5f))); }
            tex.Apply();
            var uvs = new List<Vector2>(); for (var i = 0; i <= Steps; i++) { uvs.Add(new Vector2(0, i / (float)Steps)); uvs.Add(new Vector2(1, i / (float)Steps)); }
            var mesh = new Mesh { name = "Sun path" }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetUVs(0, uvs); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            pathM = Soft("Sun path", true); pathM.SetTexture("_BaseMap", tex); pathM.SetColor("_BaseColor", new Color(1f, .65f, .35f, .7f));
            var go = new GameObject("Sun path"); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = pathM; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sunPath = go.transform;
            glints = Particles("Sun glints", 400, new Color(1f, .8f, .5f, 1f), .15f, .45f);
            var gm = glints.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(.3f, .8f); gm.startSpeed = 0f;
        }

        void SunsetUpdate(float dt, float t, string id, bool here, float energy)
        {
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + Amount(song) * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            var cam = Camera.main; var show = here && cam != null;
            sun.gameObject.SetActive(show); sunGlow.gameObject.SetActive(show); sunPath.gameObject.SetActive(here);
            var sd = RenderSettings.sun != null ? -RenderSettings.sun.transform.forward : new Vector3(.95f, .07f, .3f).normalized;
            var flat = new Vector3(sd.x, 0, sd.z).normalized; var low = (flat + Vector3.up * .045f).normalized;
            if (show)
            {
                var pos = cam.transform.position + low * 900f; var rot = Quaternion.LookRotation(-low);
                sun.SetPositionAndRotation(pos, rot); sun.localScale = Vector3.one * 34f;
                sunGlow.SetPositionAndRotation(pos + low * 2f, rot); sunGlow.localScale = Vector3.one * (150f + Mathf.Sin(t * .4f) * 6f);
            }
            if (here)
            {
                sunPath.SetPositionAndRotation(bay + Vector3.up * .07f - flat * bayR * .9f, Quaternion.LookRotation(flat)); sunPath.localScale = new Vector3(1f, 1f, bayR * 2f);
                pathM.SetColor("_BaseColor", new Color(1f, .62f, .32f, .55f + .15f * Mathf.Sin(t * 1.7f)));
                nextGlint -= dt;
                if (nextGlint <= 0f) { nextGlint = .03f; var f = Random.value; var side = Vector3.Cross(Vector3.up, flat); glints.transform.position = bay + Vector3.up * .15f + flat * (f * 2f - .9f) * bayR + side * Random.Range(-1f, 1f) * Mathf.Lerp(1.5f, 9f, f); glints.Emit(1); }
            }
            if (plane != null)
            {
                plane.gameObject.SetActive(here);
                var a = t * .18f; plane.SetPositionAndRotation(planeCenter + new Vector3(Mathf.Cos(a) * 40f, Mathf.Sin(t * .3f) * 3f, Mathf.Sin(a) * 40f), Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, 15f));
            }
            foreach (var (tr, c, r, phase, side, front) in koi)
            {
                if (tr == null) continue; var a = t * .35f + phase; var p = c + side * Mathf.Cos(a) * r * 1.3f + front * Mathf.Sin(a) * r * .8f;
                var dir = -side * Mathf.Sin(a) * 1.3f + front * Mathf.Cos(a) * .8f; tr.SetPositionAndRotation(p, Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, Mathf.Sin(t * 5f + phase) * 10f, 0));
            }
            foreach (var s in searchlights) if (s != null) s.rotation = Quaternion.Euler(-30f + Mathf.Sin(t * .4f) * 10f, t * 30f, 0);
            foreach (var s in spinners) if (s != null) s.Rotate(0, 0, (12f + energy * 30f) * dt, Space.Self);
        }
    }
}
