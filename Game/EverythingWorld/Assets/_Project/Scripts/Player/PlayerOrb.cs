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
            ("Mint", new Color(.35f, 1f, .65f)), ("Forest", new Color(.08f, .42f, .2f)), ("Lime", new Color(.7f, 1f, .2f)),
        };

        public bool isLocal = true;
        /// A showcase orb (the hub's colour station): just the orb, its pattern and wisps; no beam, glow or light.
        public bool display;
        public Transform eye;                       // the local player's camera pivot (the orb sits there)
        public float radius = .24f;

        public static PlayerOrb Local { get; private set; }
        public Color Color { get; private set; } = new Color(.92f, .94f, 1f);

        Transform orb, beam, glowDisc, pattern, pattern2; Material orbMat, coreMat, beamMat, discMat, lineMat, lineMat2; Light lamp; MeshRenderer orbR, coreR; ParticleSystem wisps;
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
            orbMat = new Material(EverythingWorld.World.WorldMaterials.Glow(Color, .9f)) { name = "Orb shell" };
            coreMat = new Material(EverythingWorld.World.WorldMaterials.Glow(Color, 3f)) { name = "Orb core" };
            orbR = shell.GetComponent<MeshRenderer>(); orbR.sharedMaterial = orbMat; coreR = core.GetComponent<MeshRenderer>(); coreR.sharedMaterial = coreMat;
            // your own orb is round your eyes: you only see its shadow
            if (isLocal) { orbR.shadowCastingMode = ShadowCastingMode.ShadowsOnly; coreR.shadowCastingMode = ShadowCastingMode.Off; coreR.enabled = false; }
            else { orbR.shadowCastingMode = ShadowCastingMode.On; coreR.shadowCastingMode = ShadowCastingMode.Off; }
            // the pattern: thin flowing lines in a deeper, more saturated shade, on two shells turning different ways
            lineMat = Blended("Orb lines", LinesTexture(7, 0)); lineMat2 = Blended("Orb lines 2", LinesTexture(4, 1));
            pattern = Shell("Lines", lineMat, radius * 2.06f); pattern2 = Shell("Lines 2", lineMat2, radius * 2.12f);
            if (isLocal) { pattern.gameObject.SetActive(false); pattern2.gameObject.SetActive(false); }
            // the wisps: threads of light that rise and curl off the orb and trail behind it (never on your own orb)
            if (!isLocal) wisps = Wisps();
            lamp = new GameObject("Orb light").AddComponent<Light>(); lamp.transform.SetParent(orb, false); lamp.type = LightType.Point; lamp.range = 5f; lamp.intensity = .7f; lamp.shadows = LightShadows.None;
            if (isLocal) lamp.transform.localPosition = new Vector3(0, .2f, .3f);
            // the faint beam to the ground, and the glow where it lands
            beamMat = Additive("Orb beam", BeamTexture()); discMat = Additive("Orb glow", DiscTexture());
            beam = Quad("Beam", beamMat, root.parent == null ? null : root.parent); glowDisc = Quad("Ground glow", discMat, root.parent == null ? null : root.parent);
            if (display) { lamp.enabled = false; beam.gameObject.SetActive(false); glowDisc.gameObject.SetActive(false); }
            SetColor(isLocal ? Load() : Color);
        }

        Transform Shell(string name, Material m, float size)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(s.GetComponent<Collider>()); s.name = name; s.transform.SetParent(orb, false); s.transform.localScale = Vector3.one * size;
            var r = s.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; return s.transform;
        }

        /// A deeper, richer version of a colour for the lines (pale colours get a deep tint of their own).
        static Color Deep(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            if (s < .15f) return new Color(.32f, .38f, .78f);
            return Color.HSVToRGB(h, Mathf.Min(1f, s + .35f), Mathf.Min(v, .85f));
        }

        ParticleSystem Wisps()
        {
            var go = new GameObject("Wisps"); go.transform.SetParent(orb, false);
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = true; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 200;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.8f); main.startSpeed = new ParticleSystem.MinMaxCurve(.02f, .08f); main.startSize = new ParticleSystem.MinMaxCurve(.012f, .03f);
            var em = ps.emission; em.rateOverTime = 18f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = radius * .9f; sh.radiusThickness = .1f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(.18f, .38f); vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f); vel.orbitalY = new ParticleSystem.MinMaxCurve(.9f, 1.6f); vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f); vel.radial = new ParticleSystem.MinMaxCurve(.02f, .08f);
            var noise = ps.noise; noise.enabled = true; noise.strength = .25f; noise.frequency = 1.2f; noise.scrollSpeed = .4f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.7f, .2f), new GradientAlphaKey(.3f, .6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, .6f, 1f, 1.4f));
            // each mote leaves a thin ribbon that fades, so they read as threads of light curling away
            var tr = ps.trails; tr.enabled = true; tr.ratio = 1f; tr.lifetime = new ParticleSystem.MinMaxCurve(.35f, .6f); tr.minVertexDistance = .015f;
            tr.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f)); tr.inheritParticleColor = true; tr.dieWithParticles = true; tr.sizeAffectsWidth = true;
            var tg = new Gradient(); tg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(.95f, 0f), new GradientAlphaKey(0f, 1f) }); tr.colorOverTrail = tg;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = Additive("Orb wisps", DiscTexture()); r.trailMaterial = Additive("Orb wisp trails", RibbonTexture()); r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            return ps;
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
            var d = Deep(c); lineMat.SetColor("_BaseColor", new Color(d.r, d.g, d.b, .95f)); lineMat2.SetColor("_BaseColor", new Color(d.r, d.g, d.b, .6f));
            if (wisps != null) { var m = wisps.main; m.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(c, Color.white, .4f), Color.Lerp(c, d, .5f)); }
        }

        /// Puts the orb at an eye position (for other players' orbs; your own follows your eyes).
        public void Place(Vector3 eyePosition) => at = eyePosition;

        void LateUpdate()
        {
            if (isLocal && eye != null) at = eye.position;
            var t = Time.time + seed;
            var p = at + Vector3.up * (isLocal ? 0f : Mathf.Sin(t * 1.6f) * .06f);
            orb.position = p; orb.rotation = Quaternion.Euler(0, t * 20f, 0);
            if (pattern != null) { pattern.localRotation = Quaternion.Euler(18f, t * 14f, 8f); pattern2.localRotation = Quaternion.Euler(-24f, -t * 23f, -12f); }
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

        static Material Blended(string name, Texture2D tex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 2f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent + 5;
            return m;
        }

        /// Thin flowing lines round a sphere (its UVs: around and up): wavy bands that drift like currents.
        /// 'bands' sets how many; 'kind' 1 gives a second, looser pattern.
        static Texture2D LinesTexture(int bands, int kind)
        {
            const int w = 256, h = 128; var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                float u = (x + .5f) / w, v = (y + .5f) / h; var a2 = u * Mathf.PI * 2f;
                var wave = kind == 0 ? Mathf.Sin(a2 * 2f + v * 9f) * .09f + Mathf.Sin(a2 * 5f) * .025f : Mathf.Sin(a2 * 3f - v * 6f) * .14f;
                var f = (v + wave) * bands; var d = Mathf.Abs(f - Mathf.Round(f));
                var line = Mathf.Clamp01(1f - d / (kind == 0 ? .07f : .05f));
                var pole = Mathf.Clamp01(Mathf.Sin(v * Mathf.PI) * 3f);
                var a = line * line * pole * (kind == 0 ? 1f : .8f + .2f * Mathf.Sin(a2 * 7f));
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
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

        /// Soft across the ribbon, so a trail reads as a thin glowing thread.
        static Texture2D RibbonTexture()
        {
            const int w = 8, h = 32; var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) { var across = 1f - Mathf.Abs((y + .5f) / h * 2f - 1f); t.SetPixel(x, y, new Color(1, 1, 1, across * across)); }
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
