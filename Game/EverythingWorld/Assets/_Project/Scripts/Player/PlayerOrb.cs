using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.Player
{
    /// A player's body: a glowing orb floating at eye level, with a very faint beam of the same colour running
    /// down to the ground and a soft glow where it touches, so you can feel the ground you're following. It bobs
    /// gently and carries a little light. Your own orb is around your eyes, so you don't see it (only its shadow,
    /// its light, the beam and the glow below you); other players will see the whole thing (set isLocal false and
    /// call Place each frame, for the multiplayer to come). The colour is picked at the hub's colour station and
    /// remembered.
    public class PlayerOrb : MonoBehaviour
    {
        /// The colours on offer, with a word for each. Spread over brightness as well as hue, so they tell apart
        /// even for colour-blind friends.
        public static readonly (string name, Color color)[] Palette =
        {
            ("Moonlight", new Color(.92f, .94f, 1f)), ("Gold", new Color(1f, .78f, .25f)), ("Ember", new Color(1f, .42f, .12f)), ("Rose", new Color(1f, .3f, .45f)),
            ("Magenta", new Color(.95f, .25f, .95f)), ("Violet", new Color(.55f, .35f, 1f)), ("Ocean", new Color(.2f, .45f, 1f)), ("Ice", new Color(.35f, .9f, 1f)),
            ("Mint", new Color(.35f, 1f, .65f)), ("Lime", new Color(.7f, 1f, .2f)),
        };

        public bool isLocal = true;
        public Transform eye;                       // the local player's camera pivot (the orb sits there)
        public float radius = .24f;

        public static PlayerOrb Local { get; private set; }
        public Color Color { get; private set; } = new Color(.92f, .94f, 1f);

        Transform orb, beam, glowDisc; Material orbMat, coreMat, beamMat, discMat; Light lamp; MeshRenderer orbR, coreR;
        Vector3 at; float seed;

        void Awake()
        {
            if (isLocal) Local = this;
            seed = Random.value * 10f;
            var root = new GameObject(isLocal ? "My orb" : "Orb").transform; root.SetParent(null, false);
            if (!isLocal) root.SetParent(transform, false);
            orb = root;
            // the orb: a soft outer shell and a brighter core
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(shell.GetComponent<Collider>()); shell.name = "Shell"; shell.transform.SetParent(orb, false); shell.transform.localScale = Vector3.one * radius * 2f;
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(core.GetComponent<Collider>()); core.name = "Core"; core.transform.SetParent(orb, false); core.transform.localScale = Vector3.one * radius * 1.1f;
            orbMat = new Material(EverythingWorld.World.WorldMaterials.Glow(Color, 1.2f)) { name = "Orb shell" };
            coreMat = new Material(EverythingWorld.World.WorldMaterials.Glow(Color, 3f)) { name = "Orb core" };
            orbR = shell.GetComponent<MeshRenderer>(); orbR.sharedMaterial = orbMat; coreR = core.GetComponent<MeshRenderer>(); coreR.sharedMaterial = coreMat;
            // your own orb is round your eyes: you only see its shadow
            if (isLocal) { orbR.shadowCastingMode = ShadowCastingMode.ShadowsOnly; coreR.shadowCastingMode = ShadowCastingMode.Off; coreR.enabled = false; }
            else { orbR.shadowCastingMode = ShadowCastingMode.On; coreR.shadowCastingMode = ShadowCastingMode.Off; }
            lamp = new GameObject("Orb light").AddComponent<Light>(); lamp.transform.SetParent(orb, false); lamp.type = LightType.Point; lamp.range = 5f; lamp.intensity = .7f; lamp.shadows = LightShadows.None;
            if (isLocal) lamp.transform.localPosition = new Vector3(0, .2f, .3f);
            // the faint beam to the ground, and the glow where it lands
            beamMat = Additive("Orb beam", BeamTexture()); discMat = Additive("Orb glow", DiscTexture());
            beam = Quad("Beam", beamMat, root.parent == null ? null : root.parent); glowDisc = Quad("Ground glow", discMat, root.parent == null ? null : root.parent);
            SetColor(isLocal ? Load() : Color);
        }

        void OnDestroy() { if (orb != null) Destroy(orb.gameObject); if (beam != null) Destroy(beam.gameObject); if (glowDisc != null) Destroy(glowDisc.gameObject); }

        static Color Load()
        {
            var i = PlayerPrefs.GetInt("EW_OrbColour", 0);
            return Palette[Mathf.Clamp(i, 0, Palette.Length - 1)].color;
        }

        /// Takes one of the palette's colours (and remembers it, for your own orb).
        public void Choose(int index)
        {
            index = Mathf.Clamp(index, 0, Palette.Length - 1);
            if (isLocal) PlayerPrefs.SetInt("EW_OrbColour", index);
            SetColor(Palette[index].color);
        }

        public void SetColor(Color c)
        {
            Color = c;
            orbMat.SetColor("_BaseColor", c); orbMat.SetColor("_EmissionColor", c * 1.2f * .65f);
            coreMat.SetColor("_BaseColor", c); coreMat.SetColor("_EmissionColor", Color.Lerp(c, Color.white, .35f) * 3f * .65f);
            lamp.color = c;
            beamMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, .16f)); discMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, .5f));
        }

        /// Puts the orb at an eye position (for other players' orbs; your own follows your eyes).
        public void Place(Vector3 eyePosition) => at = eyePosition;

        void LateUpdate()
        {
            if (isLocal && eye != null) at = eye.position;
            var t = Time.time + seed;
            var p = at + Vector3.up * (isLocal ? 0f : Mathf.Sin(t * 1.6f) * .06f);
            orb.position = p; orb.rotation = Quaternion.Euler(0, t * 20f, 0);
            var pulse = 1f + Mathf.Sin(t * 2.2f) * .04f; orb.localScale = Vector3.one * pulse;
            // the beam: from just under the orb to the ground (or 30 m down), always turned to the camera
            var ground = p - Vector3.up * 30f; var hitSomething = false;
            foreach (var h in Physics.RaycastAll(p - Vector3.up * (radius + .02f), Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (!hitSomething || h.point.y > ground.y) { ground = h.point; hitSomething = true; }
            }
            var top = p - Vector3.up * radius * .7f; var len = Mathf.Max(.01f, top.y - ground.y);
            var cam = Camera.main; var face = cam != null ? cam.transform.position - (top + ground) / 2f : Vector3.forward; face.y = 0; if (face.sqrMagnitude < .001f) face = Vector3.forward;
            beam.SetPositionAndRotation((top + ground) / 2f, Quaternion.LookRotation(face.normalized)); beam.localScale = new Vector3(.12f, len, 1f);
            glowDisc.gameObject.SetActive(hitSomething);
            if (hitSomething) { glowDisc.SetPositionAndRotation(ground + Vector3.up * .03f, Quaternion.Euler(90f, 0, 0)); glowDisc.localScale = Vector3.one * (.9f + Mathf.Sin(t * 2.2f) * .05f); }
        }

        // ---------------------------------------------------------------- pieces
        static Transform Quad(string name, Material m, Transform parent)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = name; if (parent != null) q.transform.SetParent(parent, false);
            var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return q.transform;
        }

        static Material Additive(string name, Texture2D tex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        /// Soft across, fading out toward the bottom (the ground end) and right under the orb.
        static Texture2D BeamTexture()
        {
            const int w = 16, h = 64; var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                var across = 1f - Mathf.Abs((x + .5f) / w * 2f - 1f); var v = (y + .5f) / h;
                var a = across * across * Mathf.Lerp(.25f, 1f, v) * Mathf.Clamp01((1f - v) * 12f);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
        }

        static Texture2D DiscTexture()
        {
            const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f)));
            }
            t.Apply(); return t;
        }
    }
}
