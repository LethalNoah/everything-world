using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Shows what you can do something with, as you come near it (within about 11 m), by colour and by shape:
    /// - somewhere to sit: a blue square glowing on the seat;
    /// - something to use: the thing glows yellow, a yellow diamond turning above it;
    /// - something to pick up: the thing glows white, a small white ball above it;
    /// - someone to talk to: a pink speech bubble over their head.
    /// The colours differ in brightness as well as hue, and each has its own shape, so they can be told apart
    /// without relying on hue. Lives on the player (PlayerInteractor adds it); works on everything the kits made
    /// with Interact.Seat / Toy / Carry and Residents.Make, with nothing to change in the lands.
    public class InteractCues : MonoBehaviour
    {
        public const float Range = 11f, FullWithin = 9f;
        public static readonly Color SitColor = new Color(.1f, .42f, 1f), UseColor = new Color(1f, .82f, .15f), CarryColor = new Color(1f, 1f, 1f), TalkColor = new Color(1f, .5f, .75f);

        class Cue
        {
            public Interactable it; public int kind;   // 0 sit, 1 use, 2 pick up, 3 talk
            public readonly List<Renderer> shells = new List<Renderer>(); public Transform marker; public Renderer markerR; public Collider box; public bool on, listed; public int seen;
        }

        readonly Dictionary<Interactable, Cue> cues = new Dictionary<Interactable, Cue>();
        readonly List<Cue> shown = new List<Cue>();
        readonly Collider[] found = new Collider[512];
        Material glow, solid; Mesh square, diamond, ball, bubble; MaterialPropertyBlock block; float nextScan; int scan; Transform eye;
        static readonly int ColorId = Shader.PropertyToID("_Color"), FillId = Shader.PropertyToID("_Fill"), RimId = Shader.PropertyToID("_Rim");

        public static Color ColorOf(Interactable it) => it is Seat ? SitColor : it is Carryable ? CarryColor : it is Resident ? TalkColor : UseColor;

        void Start()
        {
            var shader = Shader.Find("Everything World/Interact Glow");
            if (shader == null) { Debug.LogWarning("Interact Glow shader not found: no cues."); enabled = false; return; }
            glow = new Material(shader) { name = "Interact glow" }; glow.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            solid = new Material(shader) { name = "Interact marker" }; solid.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); block = new MaterialPropertyBlock();
            var cam = GetComponentInChildren<Camera>(); eye = cam != null ? cam.transform : transform;
            square = Flat(new[] { new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f), new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f) }, new[] { 0, 1, 2, 0, 2, 3 }, Vector3.up);
            // a speech bubble: a rectangle with a little tail at the bottom left, in the x-y plane
            bubble = Flat(new[] { new Vector3(-.5f, -.3f, 0), new Vector3(-.5f, .3f, 0), new Vector3(.5f, .3f, 0), new Vector3(.5f, -.3f, 0), new Vector3(-.3f, -.3f, 0), new Vector3(-.05f, -.3f, 0), new Vector3(-.32f, -.58f, 0) }, new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6 }, Vector3.back);
            diamond = Octahedron();
            ball = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        }

        static Mesh Flat(Vector3[] v, int[] t, Vector3 n)
        {
            var ns = new Vector3[v.Length]; for (var i = 0; i < ns.Length; i++) ns[i] = n;
            var m = new Mesh { name = "Cue shape", vertices = v, triangles = t, normals = ns }; m.RecalculateBounds(); return m;
        }

        static Mesh Octahedron()
        {
            Vector3[] p = { Vector3.up * .7f, Vector3.down * .7f, Vector3.right * .45f, Vector3.left * .45f, Vector3.forward * .45f, Vector3.back * .45f };
            int[] f = { 0, 4, 2, 0, 2, 5, 0, 5, 3, 0, 3, 4, 1, 2, 4, 1, 5, 2, 1, 3, 5, 1, 4, 3 };
            var v = new Vector3[f.Length]; var t = new int[f.Length]; for (var i = 0; i < f.Length; i++) { v[i] = p[f[i]]; t[i] = i; }
            var m = new Mesh { name = "Cue diamond", vertices = v, triangles = t }; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        Cue Make(Interactable it)
        {
            var c = new Cue { it = it, kind = it is Seat ? 0 : it is Carryable ? 2 : it is Resident ? 3 : 1, box = it.GetComponentInChildren<Collider>() };
            if (c.kind == 1 || c.kind == 2)
            {
                // the thing itself: its own meshes drawn again in light
                foreach (var mf in it.GetComponentsInChildren<MeshFilter>())
                {
                    var mr = mf.GetComponent<MeshRenderer>(); if (mr == null || mf.sharedMesh == null || mf.name == "Cue glow" || mf.name == "Cue marker") continue;
                    var go = new GameObject("Cue glow"); go.transform.SetParent(mf.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh; var r = go.AddComponent<MeshRenderer>();
                    var mats = new Material[Mathf.Max(1, mf.sharedMesh.subMeshCount)]; for (var i = 0; i < mats.Length; i++) mats[i] = glow; r.sharedMaterials = mats;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; r.enabled = false; c.shells.Add(r);
                }
            }
            var m = new GameObject("Cue marker"); m.transform.SetParent(transform.parent, false);   // not a child of the thing: it must not turn or scale with it
            m.AddComponent<MeshFilter>().sharedMesh = c.kind == 0 ? square : c.kind == 1 ? diamond : c.kind == 2 ? ball : bubble;
            var rr = m.AddComponent<MeshRenderer>(); rr.sharedMaterial = solid; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rr.receiveShadows = false; rr.enabled = false;
            m.transform.localScale = c.kind == 0 ? new Vector3(.34f, 1f, .34f) : c.kind == 1 ? Vector3.one * .16f : c.kind == 2 ? Vector3.one * .09f : Vector3.one * .3f;
            c.marker = m.transform; c.markerR = rr;
            return c;
        }

        void Show(Cue c, bool on)
        {
            if (c.on == on) return; c.on = on;
            foreach (var r in c.shells) if (r != null) r.enabled = on;
            if (c.markerR != null) c.markerR.enabled = on;
        }

        void LateUpdate()
        {
            if (glow == null) return;
            var from = eye.position; var t = Time.time;
            if (t >= nextScan)
            {
                nextScan = t + .25f; scan++;
                var n = Physics.OverlapSphereNonAlloc(from, Range, found, ~0, QueryTriggerInteraction.Collide);
                for (var i = 0; i < n; i++)
                {
                    var it = found[i].GetComponentInParent<Interactable>(); if (it == null || !it.isActiveAndEnabled) continue;
                    if (!cues.TryGetValue(it, out var c)) { cues[it] = c = Make(it); }
                    if (!c.listed) { c.listed = true; shown.Add(c); }
                    c.seen = scan;
                }
                for (var i = shown.Count - 1; i >= 0; i--)
                {
                    var c = shown[i];
                    if (c.it == null) { if (c.marker != null) Destroy(c.marker.gameObject); shown.RemoveAt(i); continue; }
                    if (c.seen != scan) { Show(c, false); c.listed = false; shown.RemoveAt(i); }
                }
            }
            var pulse = .8f + .2f * Mathf.Sin(t * 3f);
            foreach (var c in shown)
            {
                if (c.it == null) continue;
                var at = c.box != null ? c.box.bounds.center : c.it.transform.position; var top = c.box != null ? c.box.bounds.max.y : at.y;
                var d = Vector3.Distance(from, at); var a = Mathf.Clamp01((Range - d) / (Range - FullWithin)) * pulse;
                var hide = c.kind == 2 && ((Carryable)c.it).Held;
                if (c.kind == 3 && c.it.GetComponentInChildren<TextMesh>() != null) hide = true;   // they're speaking: the words are there instead
                Show(c, a > .01f && !hide); if (!c.on) continue;
                var col = ColorOf(c.it);
                if (c.shells.Count > 0) { block.SetColor(ColorId, new Color(col.r, col.g, col.b, a * (c.kind == 2 ? .7f : .9f))); block.SetFloat(FillId, .12f); block.SetFloat(RimId, 1.1f); foreach (var r in c.shells) if (r != null) r.SetPropertyBlock(block); }
                block.SetColor(ColorId, new Color(col.r, col.g, col.b, a * (c.kind == 0 ? 1f : .95f))); block.SetFloat(FillId, 1f); block.SetFloat(RimId, 0f); c.markerR.SetPropertyBlock(block);
                switch (c.kind)
                {
                    case 0: c.marker.SetPositionAndRotation(c.it.transform.position + Vector3.up * .03f, c.it.transform.rotation); break;
                    case 1: c.marker.SetPositionAndRotation(new Vector3(at.x, top + .3f + .04f * Mathf.Sin(t * 2f), at.z), Quaternion.Euler(0, t * 70f, 0)); break;
                    case 2: c.marker.position = new Vector3(at.x, top + .16f, at.z); break;
                    default:
                        var r3 = (Resident)c.it; var head = r3.head != null ? r3.head.position : c.it.transform.position + Vector3.up * 1.6f;
                        c.marker.SetPositionAndRotation(head + Vector3.up * .5f, Quaternion.LookRotation(head - from)); break;
                }
            }
        }

        void OnDisable() { foreach (var c in shown) { Show(c, false); c.listed = false; } shown.Clear(); }
    }
}
