using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Neo Soul Quarter on a candlelit evening:
    /// - candles flicker, the strings of lights breathe, incense smoke climbs, fireflies drift, a few long clouds lie
    ///   in the plum sky; the wheel turns slowly, on and on;
    /// - "Blue Lights": the whole quarter pulses blue. "Golden": everything turns gold. "Red Room": red. "Wildfires":
    ///   the light flickers orange and embers rise. "Pretty Wings": white feathers fall. "Ascension": lights rise out
    ///   of the streets into the sky. "Cranes in the Sky": paper cranes drift up. "Untitled (How Does It Feel)": the
    ///   quarter goes dark but for one warm spotlight. "On & On": the wheel turns faster. Every song's own light
    ///   brightens while it plays.
    public class NeoLife : MonoBehaviour
    {
        const string Land = "The Neo Soul Quarter";
        public Vector3 center;
        public string blueSong, goldenSong, redSong, wildSong, wingsSong, ascSong, cranesSong, untitledSong, onSong;
        public Material candles, windows, lamps; public Material[] bulbs; public float played = -99f;
        public Transform wheel; public Light spot; public Vector3 wildAt;
        public List<Vector3> smokes = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem smoke, dots;
        Color candleBase, windowBase, lampBase; Color[] bulbBase; float spotBase;
        float blue, golden, red, wild, wings, asc, cranes, untitled, on, nextSmoke, nextFly, nextFall;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 22; clouds.span = 2400f; clouds.key = "neosoul";
            clouds.minHeight = 170f; clouds.maxHeight = 240f; clouds.size = 3.2f; clouds.speed = 1.2f; clouds.cover = .3f;
            clouds.color = new Color(.86f, .56f, .6f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            candleBase = E(candles); windowBase = E(windows); lampBase = E(lamps); bulbBase = new Color[bulbs != null ? bulbs.Length : 0]; for (var i = 0; i < bulbBase.Length; i++) bulbBase[i] = E(bulbs[i]);
            if (spot != null) spotBase = spot.intensity;
            smoke = Particles("Incense smoke", 300, new Color(1f, .95f, .9f, .14f), .12f, .3f, false);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .4f, 1f, 2.4f));
            dots = Particles("Fireflies and feathers", 500, Color.white, .05f, .1f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .3f), new GradientAlphaKey(0f, 1f) });
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

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;

            blue = Mathf.MoveTowards(blue, On(blueSong) ? 1f : 0f, dt / 4f);
            golden = Mathf.MoveTowards(golden, On(goldenSong) ? 1f : 0f, dt / 5f);
            red = Mathf.MoveTowards(red, On(redSong) ? 1f : 0f, dt / 4f);
            wild = Mathf.MoveTowards(wild, On(wildSong) ? 1f : 0f, dt / 4f);
            wings = Mathf.MoveTowards(wings, On(wingsSong) ? 1f : 0f, dt / 4f);
            asc = Mathf.MoveTowards(asc, On(ascSong) ? 1f : 0f, dt / 4f);
            cranes = Mathf.MoveTowards(cranes, On(cranesSong) ? 1f : 0f, dt / 4f);
            untitled = Mathf.MoveTowards(untitled, On(untitledSong) ? 1f : 0f, dt / 6f);
            on = Mathf.MoveTowards(on, On(onSong) ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 1.6f) * .6f)));

            // candles flicker; the strings of lights breathe one after another; the windows hold steady
            var lift = t - played < 4f ? 1f - (t - played) / 4f : 0f; var dim = 1f - untitled * .7f;
            if (candles != null) candles.SetColor("_EmissionColor", candleBase * (.8f + .3f * Mathf.PerlinNoise(t * 5f, 1.3f) + wild * .5f) * Mathf.Max(dim, .6f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (.9f + .08f * Mathf.Sin(t * .7f) + lift * .4f) * dim);
            if (windows != null) windows.SetColor("_EmissionColor", Color.Lerp(windowBase, new Color(.3f, .5f, 1f) * 1.2f, blue * (.5f + .5f * Mathf.Sin(t * 2.4f))) * (1f + golden * .6f) * dim);
            if (bulbs != null) for (var i = 0; i < bulbs.Length; i++) if (bulbs[i] != null) bulbs[i].SetColor("_EmissionColor", bulbBase[i] * (.75f + .3f * Mathf.Sin(t * .9f + i * 2.1f) + energy * .3f + lift * .5f + asc * .6f) * dim);
            if (wheel != null) wheel.Rotate(0f, 0f, dt * (5f + on * 30f), Space.Self);
            if (spot != null) spot.intensity = spotBase * (1f + untitled * 2.5f);

            var tint = new Color(1f, .9f, .8f); var amount = .03f;
            if (golden > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .3f), golden); amount = Mathf.Lerp(amount, .4f, golden); }
            if (asc > 0f) { tint = Color.Lerp(tint, new Color(1f, .86f, .6f), asc); amount = Mathf.Lerp(amount, .2f, asc); }
            if (wings > 0f) { tint = Color.Lerp(tint, new Color(.9f, .86f, 1f), wings); amount = Mathf.Lerp(amount, .22f, wings); }
            if (cranes > 0f) { tint = Color.Lerp(tint, new Color(.7f, .8f, 1f), cranes); amount = Mathf.Lerp(amount, .25f, cranes); }
            if (red > 0f) { tint = Color.Lerp(tint, new Color(1f, .16f, .14f), red); amount = Mathf.Lerp(amount, .5f, red); }
            if (blue > 0f) { tint = Color.Lerp(tint, new Color(.2f, .4f, 1f), blue); amount = Mathf.Lerp(amount, .34f + .16f * Mathf.Sin(t * 2.4f), blue); }
            if (wild > 0f) { tint = Color.Lerp(tint, new Color(1f, .45f, .12f), wild); amount = Mathf.Lerp(amount, .3f + .16f * Mathf.PerlinNoise(t * 4f, 1f), wild); }
            if (untitled > 0f) { tint = Color.Lerp(tint, new Color(.12f, .08f, .1f), untitled); amount = Mathf.Lerp(amount, .6f, untitled); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .14f + .1f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSmoke)
            {
                nextSmoke = t + .14f;
                foreach (var p in smokes) if ((p - cp).sqrMagnitude < 50f * 50f) smoke.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Mathf.Sin(t * .8f + p.x) * .08f, .28f, Mathf.Cos(t * .7f + p.z) * .08f) }, 1);
            }
            if (t >= nextFly)
            {
                nextFly = t + .3f; var p = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(-.8f, 3f), Random.Range(-14f, 14f));
                if (!IndoorSpaces.Contains(p)) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.05f, .15f), Random.Range(-.25f, .25f)), startColor = new Color(1f, .85f, .4f) }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .07f; var p = cp + new Vector3(Random.Range(-18f, 18f), 0, Random.Range(-18f, 18f));
                if (IndoorSpaces.Contains(p)) return;
                if (wings > .05f && Random.value < wings) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(4f, 9f), velocity = new Vector3(Mathf.Sin(t + p.x) * .4f, -.7f, Mathf.Cos(t + p.z) * .4f), startColor = new Color(1f, .97f, 1f), startSize = .16f, startLifetime = 9f }, 1);
                if (asc > .05f && Random.value < asc) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(-1f, 1f), velocity = new Vector3(0, Random.Range(1.2f, 2.6f), 0), startColor = new Color(1f, .8f, .4f), startSize = .14f, startLifetime = 9f }, 1);
                if (cranes > .05f && Random.value < cranes * .6f) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(0f, 3f), velocity = new Vector3(wind.x * .5f, Random.Range(.5f, 1.1f), wind.y * .5f), startColor = new Color(.9f, .95f, 1f), startSize = .22f, startLifetime = 10f }, 1);
                if (wild > .05f && Random.value < wild) { var w = (wildAt - cp).sqrMagnitude < 60f * 60f ? wildAt + new Vector3(Random.Range(-1.6f, 1.6f), .3f, Random.Range(-1.6f, 1.6f)) : p + Vector3.up * Random.Range(0f, 2f); dots.Emit(new ParticleSystem.EmitParams { position = w, velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(1f, 2.4f), Random.Range(-.3f, .3f)), startColor = new Color(1f, .5f, .15f), startSize = .09f, startLifetime = 4f }, 1); }
            }
        }
    }
}
