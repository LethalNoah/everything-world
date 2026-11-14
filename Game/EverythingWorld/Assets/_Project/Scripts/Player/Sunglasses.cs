using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EverythingWorld.Player
{
    /// Sunglasses: press G and a pair drops down from above onto your face. You see their rims blurred round the
    /// edge of your view (out of focus, as real glasses are), the lenses darken the edges a little, and the whole
    /// picture gets richer and deeper, with the brightest light and glare pulled down, like polarized lenses. Press G again to lift them off.
    /// Remembered between sessions. Handy for bright lands at night.
    public class Sunglasses : MonoBehaviour
    {
        public Key key = Key.G;
        public float seconds = .55f;

        Camera cam; Transform frame; MeshRenderer frameRenderer; Material mat;
        Volume volume; ColorAdjustments grade; ShadowsMidtonesHighlights smh; Tonemapping tone;
        bool on; float t;   // 0 = off (above your head), 1 = on your face
        float builtAspect = -1f, builtFov = -1f, builtNear = -1f, dist, halfH;

        void Start()
        {
            on = PlayerPrefs.GetInt("EW_Sunglasses", 0) == 1; t = on ? 1f : 0f;
            var go = new GameObject("Sunglasses grade"); go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 60; volume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            grade = profile.Add<ColorAdjustments>(true);
            // polarized: colours richer and deeper, the brightest light (glare, white skies, bloom) pulled down hardest,
            // the darks left alone; a near-neutral grey lens so colours stay true
            grade.postExposure.Override(-2.1f); grade.colorFilter.Override(new Color(.97f, .98f, 1f)); grade.contrast.Override(24f); grade.saturation.Override(32f);
            smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.highlights.Override(new Vector4(1f, 1f, 1f, -.9f)); smh.midtones.Override(new Vector4(1f, 1f, 1f, -.22f));
            smh.highlightsStart.Override(.3f); smh.highlightsEnd.Override(1f);
            // the big one: instead of clipping bright light to flat white, squeeze it into a filmic curve, so glare
            // turns back into detail you can see (light beyond white is kept until here; HDR is on)
            tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            volume.profile = profile;
        }

        public bool On => on;
        /// Puts them on or takes them off (instant = no animation; for the photo tour). Not remembered.
        public void Wear(bool wear, bool instant = false) { on = wear; if (instant) { t = wear ? 1f : 0f; if (volume != null) volume.weight = wear ? 1f : 0f; } }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[key].wasPressedThisFrame && !EverythingWorld.World.GameKeys.Off) { on = !on; PlayerPrefs.SetInt("EW_Sunglasses", on ? 1 : 0); }
            t = Mathf.MoveTowards(t, on ? 1f : 0f, Time.deltaTime / seconds);
            volume.weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, 1f, t));
            EverythingWorld.World.SkyDirector.BloomScale = Mathf.Lerp(1f, .12f, volume.weight);   // polarized lenses cut the glare
        }

        void LateUpdate()
        {
            if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
            if (cam == null) return;
            if (t <= 0f) { if (frame != null) frame.gameObject.SetActive(false); return; }
            if (frame == null || Mathf.Abs(cam.aspect - builtAspect) > .01f || Mathf.Abs(cam.fieldOfView - builtFov) > .5f || Mathf.Abs(cam.nearClipPlane - builtNear) > .001f) Build();
            frame.gameObject.SetActive(true);
            // putting them on: down from above your eyes, tipped forward, settling with a little bounce; taking them
            // off: straight back up
            var e = on ? EaseOutBack(t) : Mathf.SmoothStep(0f, 1f, t);
            var d = dist;
            frame.localPosition = new Vector3(0f, (1f - e) * halfH * 2.6f, d);
            frame.localRotation = Quaternion.Euler((1f - e) * -28f, 0f, 0f);
            // the post-processing has to be on to darken the picture (the sky only switches it on for bloom)
            if (volume.weight > .001f) { var data = cam.GetComponent<UniversalAdditionalCameraData>(); if (data != null) data.renderPostProcessing = true; }
        }

        static float EaseOutBack(float x) { const float c1 = 1.4f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f); }

        /// A quad just past the camera's near plane, a little bigger than the view, painted with the glasses.
        void Build()
        {
            builtAspect = cam.aspect; builtFov = cam.fieldOfView; builtNear = cam.nearClipPlane;
            var d = builtNear * 1.15f + .005f; dist = d;   // (keep builtNear as the camera's own value, or this rebuilds every frame)
            halfH = d * Mathf.Tan(builtFov * .5f * Mathf.Deg2Rad) * 1.04f; var halfW = halfH * builtAspect;
            if (frame == null)
            {
                frame = new GameObject("Sunglasses").transform;
                frameRenderer = frame.gameObject.AddComponent<MeshRenderer>(); frame.gameObject.AddComponent<MeshFilter>();
                frameRenderer.shadowCastingMode = ShadowCastingMode.Off; frameRenderer.receiveShadows = false;
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Sunglasses" };
                mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f); mat.SetFloat("_Cull", 0f); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Overlay; mat.SetColor("_BaseColor", Color.white);
                frameRenderer.sharedMaterial = mat;
            }
            frame.SetParent(cam.transform, false);
            var mesh = new Mesh { name = "Sunglasses" };
            mesh.vertices = new[] { new Vector3(-halfW, -halfH, 0), new Vector3(halfW, -halfH, 0), new Vector3(halfW, halfH, 0), new Vector3(-halfW, halfH, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds(); mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
            frame.GetComponent<MeshFilter>().sharedMesh = mesh;
            var old = mat.GetTexture("_BaseMap"); mat.SetTexture("_BaseMap", Paint(builtAspect)); if (old != null) Destroy(old);
        }

        /// The view through the glasses, painted once. Both eyes look through together, so the frame mostly
        /// disappears: what you notice is a soft dark rim along the top, dark corners where the frame turns back
        /// to your ears, a faint shadow of the nose at the bottom and of the bridge at the top, the lens a little
        /// darker toward its edge, and a faint glint. Everything this close to your eyes is out of focus.
        static Texture2D Paint(float aspect)
        {
            const int h = 360; var w = Mathf.RoundToInt(h * aspect);
            var px = new Color32[w * h];
            float Smooth(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));
            for (var j = 0; j < h; j++)
            {
                var y = (j + .5f) / h * 2f - 1f;
                for (var i = 0; i < w; i++)
                {
                    var x = ((i + .5f) / w * 2f - 1f) * aspect;
                    // one wide rounded lens: how far out toward its edge this point is (1 = the edge)
                    var ex = Mathf.Abs(x) / (aspect * 1.02f); var ey = y > 0f ? y / .97f : -y / 1.06f;
                    var f = Mathf.Pow(Mathf.Pow(ex, 3.4f) + Mathf.Pow(ey, 3.4f), 1f / 3.4f);
                    var frame = Smooth(.9f, 1.04f, f);                                                   // dark corners and edges
                    var brow = Smooth(.8f, .97f, y) * (.45f + .4f * Smooth(.3f, 1f, Mathf.Abs(x) / aspect)); // the top rim
                    var nose = Mathf.Exp(-(x * x) / (2f * .16f * .16f) - (y + 1.08f) * (y + 1.08f) / (2f * .2f * .2f)) * .55f;
                    var bridge = Mathf.Exp(-(x * x) / (2f * .07f * .07f) - (y - 1f) * (y - 1f) / (2f * .12f * .12f)) * .45f;
                    var lens = .03f + .2f * Smooth(.45f, 1f, f);
                    var a = Mathf.Clamp01(1f - (1f - lens) * (1f - frame * .97f) * (1f - brow) * (1f - nose) * (1f - bridge));
                    var c = new Color(.035f, .04f, .045f, a);
                    // a faint glint across each half, the way light catches a lens
                    var lx = Mathf.Abs(x) - aspect * .45f; var g = Mathf.Exp(-Mathf.Pow((lx * .8f + y * .6f - .25f) / .045f, 2f)) * .07f * (1f - frame) * Smooth(1f, .3f, Mathf.Abs(lx + y * .5f));
                    if (g > .001f) { c = Color.Lerp(c, Color.white, g / Mathf.Max(.0001f, a + g)); c.a = Mathf.Clamp01(a + g); }
                    px[j * w + i] = c;
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "Sunglasses", wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px); tex.Apply(true, true);
            return tex;
        }
    }
}
