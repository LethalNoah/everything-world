using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Hides what is too far away to matter, so a view across several built lands stays smooth (the hub looking
    /// toward the Indie Isles took 120-150 ms a frame before this). By distance from the camera to each thing:
    /// swaying grass and flowers 220 m, small props 400 m, glows 650 m, mid-sized scenery 900 m, big pieces
    /// (landmarks, land chunks) 1.6 km; point and spot lights once you're well outside their reach. Anything
    /// that moves with a land's life (clouds, birds, cars, fireworks) is left alone. A few thousand checks,
    /// spread over several frames.
    public class LandCuller : MonoBehaviour
    {
        struct Item { public Renderer r; public Bounds b; public float far2; }
        struct LightItem { public Light l; public Vector3 p; public float far2; }
        readonly List<Item> items = new List<Item>();
        readonly List<LightItem> lights = new List<LightItem>();
        int next, nextLight;
        Camera cam;

        public const float Grass = 220f, Small = 400f, Glow = 650f, Medium = 900f, Big = 1600f;

        public void Init(Transform lands)
        {
            items.Clear(); lights.Clear();
            var skip = new HashSet<Transform>();
            foreach (var cl in lands.GetComponentsInChildren<CloudLayer>(true)) skip.Add(cl.transform);   // a land's life: left alone
            foreach (var r in lands.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (UnderAny(r.transform, skip) || !r.enabled || r.GetComponent<Billboard>() != null) continue;   // hidden on purpose, or shown by its own script
                var b = r.bounds; var size = b.size.magnitude;
                var mat = r.sharedMaterial; var shader = mat != null && mat.shader != null ? mat.shader.name : "";
                float far;
                if (shader.Contains("Wind") || r.name.StartsWith("Soft")) far = Grass;
                else if (shader.Contains("Glow Halo")) far = Glow;
                else if (size < 8f) far = Small;
                else if (size < 60f) far = Medium;
                else far = Big;
                items.Add(new Item { r = r, b = b, far2 = far * far });
            }
            foreach (var l in lands.GetComponentsInChildren<Light>(true))
            {
                if (l.type == LightType.Directional || UnderAny(l.transform, skip)) continue;
                var far = l.range + 120f;
                lights.Add(new LightItem { l = l, p = l.transform.position, far2 = far * far });
            }
            Debug.Log($"Land culler: watching {items.Count} meshes and {lights.Count} lights");
        }

        static bool UnderAny(Transform t, HashSet<Transform> roots)
        {
            for (var p = t; p != null; p = p.parent) if (roots.Contains(p)) return true;
            return false;
        }

        /// Update everything at once (after a jump or a teleport, or before a photo).
        public void Refresh() { Step(items.Count, lights.Count); }

        void Update() => Step(Mathf.Max(1, items.Count / 6), Mathf.Max(1, lights.Count / 6));

        void Step(int count, int lightCount)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || items.Count == 0) return;
            var c = cam.transform.position;
            for (var k = 0; k < count && items.Count > 0; k++)
            {
                if (next >= items.Count) next = 0;
                var it = items[next++];
                if (it.r == null) continue;
                var on = it.b.SqrDistance(c) < it.far2;
                if (it.r.enabled != on) it.r.enabled = on;
            }
            for (var k = 0; k < lightCount && lights.Count > 0; k++)
            {
                if (nextLight >= lights.Count) nextLight = 0;
                var it = lights[nextLight++];
                if (it.l == null) continue;
                var on = (it.p - c).sqrMagnitude < it.far2;
                if (it.l.enabled != on) it.l.enabled = on;
            }
        }
    }
}
