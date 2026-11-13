using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The IDM Glitchlands, always slightly broken:
    /// - on a fast, uneven clock (shared, so friends see the same glitches): floating blocks jump to new spots and back;
    ///   the Stack's blocks slip out of line; the monolith's sorted columns flicker between colours; code drifts up out of
    ///   the floor and resets; pulses of data race along the Bus; monitors flicker; now and then the whole world tears
    ///   with a flash of wrong colour; corrupting the data at the terminal makes everything jump at once;
    /// - "Acid Rain": green rain. "Snowy Lava": orange with snow. "The Water": deep blue. "Öngyilkos Vasárnap": grey,
    ///   with rain. "My Red Hot Car": red. "IDM Anthem": white glitch flashes. "Corot-7b": a hot orange planet light.
    ///   "Caffeine": everything glitches twice as fast. Every song's own light brightens while it plays.
    public class GlitchLife : MonoBehaviour
    {
        const string Land = "The IDM Glitchlands";
        public Vector3 center;
        public string cafSong, corotSong, anthemSong, lavaSong, carSong, gloomSong, waterSong, acidSong;
        public Material grid, lamps, code, blocks, monitors; public float played = -99f;
        public List<Material> sorts = new List<Material>();
        public List<(Transform t, Vector3 home, float ph)> jumpers = new List<(Transform, Vector3, float)>(), codes = new List<(Transform, Vector3, float)>();
        public List<(Transform t, Vector3 a, Vector3 b, float ph)> pulses = new List<(Transform, Vector3, Vector3, float)>();
        public List<(Transform t, Vector3 home, int k)> stack = new List<(Transform, Vector3, int)>();
        public List<(Transform t, Vector3 home)> spinners = new List<(Transform, Vector3)>();
        public List<Transform> blobs = new List<Transform>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        static readonly Color[] Sort = { new Color(.1f, .9f, .5f), new Color(.2f, .6f, 1f), new Color(1f, .2f, .4f), new Color(1f, .8f, .2f), new Color(.8f, .3f, 1f), new Color(.9f, .95f, 1f) };

        CloudLayer clouds; ParticleSystem dots;
        Color gridBase, lampBase, codeBase, blockBase, monBase;
        float caf, corot, anthem, lava, car, gloom, water, acid, tear, nextFall;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 16; clouds.span = 2400f; clouds.key = "glitch";
            clouds.minHeight = 140f; clouds.maxHeight = 220f; clouds.size = 2.6f; clouds.speed = 1.4f; clouds.cover = .2f;
            clouds.color = new Color(.2f, .3f, .3f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            gridBase = E(grid); lampBase = E(lamps); codeBase = E(code); blockBase = E(blocks); monBase = E(monitors);
            dots = Particles("Pixels, rain and snow", 1200, Color.white, .06f, .16f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool additive)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetTexture("_BaseMap", tex);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static float Hash(float x) { var h = Mathf.Sin(x * 127.1f) * 43758.5453f; return h - Mathf.Floor(h); }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = (float)(SharedChance.Now % 100000.0);
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;
            float Ease(float v, bool on, float secs) => Mathf.MoveTowards(v, on ? 1f : 0f, dt / secs);

            caf = Ease(caf, On(cafSong), 2f); corot = Ease(corot, On(corotSong), 4f); anthem = Ease(anthem, On(anthemSong), 2f); lava = Ease(lava, On(lavaSong), 4f); car = Ease(car, On(carSong), 3f); gloom = Ease(gloom, On(gloomSong), 5f); water = Ease(water, On(waterSong), 4f); acid = Ease(acid, On(acidSong), 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the glitch clock: steps of uneven length, faster with energy and coffee
            var speed = 2.9f * (1f + caf) * (1f + energy * .5f); var step = Mathf.Floor(now * speed); var corrupt = t - played < 3f ? 1f - (t - played) / 3f : 0f;
            foreach (var (tr, home, ph) in jumpers) if (tr != null) { var h = Hash(step * .37f + ph); var jump = h > .8f - corrupt * .7f; tr.position = home + (jump ? new Vector3(Hash(step + ph) - .5f, Hash(step * 1.3f + ph) - .5f, Hash(step * 1.7f + ph) - .5f) * 3f : Vector3.zero); }
            foreach (var (tr, home, k) in stack) if (tr != null) { var h = Hash(step * .21f + k); tr.position = home + (h > .85f - corrupt * .6f ? new Vector3(Hash(step + k) - .5f, 0, Hash(step * 2f + k) - .5f) * 1.6f : Vector3.zero); tr.rotation = Quaternion.Euler(0, k * 7f + (h > .9f ? 15f : 0f), 0); }
            foreach (var (tr, home, ph) in codes) if (tr != null) { var k = Mathf.Repeat(t * .08f + ph, 1f); tr.position = home + Vector3.up * (k * 12f); tr.gameObject.SetActive(Hash(step + ph * 10f) > .1f); }
            foreach (var (tr, a, b, ph) in pulses) if (tr != null) { var k = Mathf.Repeat(now * (.12f + caf * .12f) + ph, 1f); tr.position = Vector3.Lerp(a, b, k); tr.rotation = Quaternion.LookRotation((b - a).normalized); }
            for (var i = 0; i < spinners.Count; i++) { var (tr, home) = spinners[i]; if (tr == null) continue; tr.position = home + Vector3.up * Mathf.Sin(t * .6f + i) * .3f; tr.Rotate(0, dt * 12f, 0, Space.World); }
            foreach (var b in blobs) if (b != null) b.localScale = new Vector3(1f + Mathf.Sin(t * 2.1f) * .12f, 1f + Mathf.Sin(t * 1.7f + 1f) * .15f, 1f + Mathf.Sin(t * 2.6f + 2f) * .1f);
            for (var i = 0; i < sorts.Count; i++) if (sorts[i] != null) sorts[i].SetColor("_EmissionColor", Sort[(int)(Hash(step * .5f + i) * Sort.Length)] * .8f * (1f + corrupt));

            if (Random.value < dt * (.15f + energy * .3f + anthem * 1.5f)) tear = 1f; tear = Mathf.MoveTowards(tear, 0f, dt * 6f);
            var flick = Hash(step * .9f) > .92f ? .4f : 1f;
            if (grid != null) grid.SetColor("_EmissionColor", gridBase * (.8f + energy * .4f + corrupt) * flick);
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + corrupt * .5f));
            if (code != null) code.SetColor("_EmissionColor", codeBase * (.8f + Hash(step) * .5f + corrupt));
            if (blocks != null) blocks.SetColor("_EmissionColor", blockBase * (.8f + Hash(step * 1.1f) * .6f));
            if (monitors != null) monitors.SetColor("_EmissionColor", monBase * (Hash(step * 3f) > .85f ? .3f : 1f));

            var tint = new Color(.4f, .9f, .7f); var amount = .06f;
            if (water > 0f) { tint = Color.Lerp(tint, new Color(.2f, .5f, 1f), water); amount = Mathf.Lerp(amount, .38f, water); }
            if (gloom > 0f) { tint = Color.Lerp(tint, new Color(.5f, .52f, .56f), gloom); amount = Mathf.Lerp(amount, .44f, gloom); }
            if (lava > 0f) { tint = Color.Lerp(tint, new Color(1f, .45f, .14f), lava); amount = Mathf.Lerp(amount, .38f, lava); }
            if (corot > 0f) { tint = Color.Lerp(tint, new Color(1f, .55f, .2f), corot); amount = Mathf.Lerp(amount, .34f, corot); }
            if (car > 0f) { tint = Color.Lerp(tint, new Color(1f, .14f, .14f), car); amount = Mathf.Lerp(amount, .4f, car); }
            if (acid > 0f) { tint = Color.Lerp(tint, new Color(.5f, 1f, .2f), acid); amount = Mathf.Lerp(amount, .4f, acid); }
            if (tear > 0f) { tint = Color.Lerp(tint, Sort[(int)(Hash(step * 7f) * Sort.Length)], tear); amount = Mathf.Lerp(amount, .5f, tear); }
            if (corrupt > 0f) { tint = Color.Lerp(tint, Sort[(int)(Hash(step * 3.3f) * Sort.Length)], corrupt); amount = Mathf.Lerp(amount, .45f, corrupt); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .2f + .2f * energy + gloom * .4f + acid * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(.5f, 6f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .15f + corrupt * .6f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(0, Random.Range(.2f, .8f), 0), startColor = Sort[Random.Range(0, Sort.Length)], startSize = .1f, startLifetime = 3f }, 1);
                if (acid > .05f && Random.value < acid) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -9f, wind.y), startColor = new Color(.5f, 1f, .3f, .6f), startSize = .05f, startLifetime = 1.2f }, 1);
                if (gloom > .05f && Random.value < gloom) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -9f, wind.y), startColor = new Color(.7f, .72f, .76f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
                if (lava > .05f && Random.value < lava) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(5f, 9f), Random.Range(-16f, 16f)), velocity = new Vector3(Random.Range(-.3f, .3f), -1f, Random.Range(-.3f, .3f)), startColor = Color.white, startSize = .1f, startLifetime = 8f }, 1);
            }
        }
    }
}
