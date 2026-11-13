using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Lo-Fi Hills on a rainy blue dusk:
    /// - it rains, steadily, always; low cloud; the windows glow and now and then one flickers like a television;
    ///   vending machines hum; the puddles shimmer; chimney smoke and steam off the mugs; a few fireflies under
    ///   the eaves;
    /// - "waon to ame": the rain comes harder and the chimes glow. "Underwater" and "Aquatic Ambience": everything
    ///   goes deep teal and bubbles rise. "Darkest (Dim)": the lights turn low. "Awakening of the Flowers" and
    ///   "Flowers": the rain stops, the light warms, petals drift. "Getsomerest/sleepwell": night falls and the
    ///   windows go out one by one. "Cup of Joe": amber. "Genesis": a pink first light. "Big Blue": blue, pulsing.
    ///   Every song's own light brightens while it plays.
    public class LofiLife : MonoBehaviour
    {
        const string Land = "The Lo-Fi Hills";
        public Vector3 center;
        public string ameSong, dimSong, restSong, joeSong, genesisSong, blueSong;
        public HashSet<string> water = new HashSet<string>(), flowers = new HashSet<string>();
        public Material windows, lamps, vending, puddles; public float played = -99f;
        public List<Vector3> smokes = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem smoke, dots;
        Color windowBase, lampBase, vendBase, puddleBase;
        float ame, dim, rest, joe, genesis, blue, under, bloom, nextSmoke, nextFall;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 60; clouds.span = 2400f; clouds.key = "lofihills";
            clouds.minHeight = 120f; clouds.maxHeight = 190f; clouds.size = 4.2f; clouds.speed = 1.6f; clouds.cover = .8f;
            clouds.color = new Color(.5f, .54f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            windowBase = E(windows); lampBase = E(lamps); vendBase = E(vending); puddleBase = E(puddles);
            smoke = Particles("Smoke and steam", 400, new Color(.9f, .92f, 1f, .16f), .12f, .3f, false);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .4f, 1f, 2.6f));
            dots = Particles("Bubbles and petals", 500, Color.white, .06f, .12f, true);
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

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;

            ame = Mathf.MoveTowards(ame, On(ameSong) ? 1f : 0f, dt / 5f);
            dim = Mathf.MoveTowards(dim, On(dimSong) ? 1f : 0f, dt / 6f);
            rest = Mathf.MoveTowards(rest, On(restSong) ? 1f : 0f, dt / 10f);
            joe = Mathf.MoveTowards(joe, On(joeSong) ? 1f : 0f, dt / 4f);
            genesis = Mathf.MoveTowards(genesis, On(genesisSong) ? 1f : 0f, dt / 6f);
            blue = Mathf.MoveTowards(blue, On(blueSong) ? 1f : 0f, dt / 3f);
            under = Mathf.MoveTowards(under, here && id != null && water.Contains(id) ? 1f : 0f, dt / 5f);
            bloom = Mathf.MoveTowards(bloom, here && id != null && flowers.Contains(id) ? 1f : 0f, dt / 8f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 1.4f) * .5f)));

            // the windows glow, and flicker a little like a television left on; the lamps hold steady; the machines hum
            var lift = t - played < 4f ? 1f - (t - played) / 4f : 0f; var low = 1f - dim * .65f - rest * .8f;
            var tv = Mathf.PerlinNoise(t * 3f, 4.2f) > .78f ? .88f : 1f;
            if (windows != null) windows.SetColor("_EmissionColor", Color.Lerp(windowBase, new Color(.2f, .7f, .8f) * 1.2f, under * .6f) * tv * Mathf.Max(.12f, low) * (1f + joe * .5f + lift * .3f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * Mathf.Max(.3f, 1f - dim * .55f - rest * .4f) * (1f + .04f * Mathf.Sin(t * .6f)));
            if (vending != null) vending.SetColor("_EmissionColor", vendBase * (.92f + .08f * Mathf.Sin(t * 9f)) * Mathf.Max(.4f, 1f - dim * .5f) * (1f + blue * (.6f + .5f * Mathf.Sin(t * 5f))));
            if (puddles != null) puddles.SetColor("_EmissionColor", puddleBase * (.8f + .3f * Mathf.PerlinNoise(t * 1.6f, .3f) + ame * .5f + blue * .8f));

            SkyDirector.SetSongRain(Land, Mathf.Clamp01(.42f + ame * .4f - bloom * .4f - genesis * .3f - under * .3f));
            var tint = new Color(.8f, .86f, 1f); var amount = .04f;
            if (joe > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .4f), joe); amount = Mathf.Lerp(amount, .32f, joe); }
            if (bloom > 0f) { tint = Color.Lerp(tint, new Color(1f, .86f, .74f), bloom); amount = Mathf.Lerp(amount, .36f, bloom); }
            if (genesis > 0f) { tint = Color.Lerp(tint, new Color(1f, .66f, .7f), genesis); amount = Mathf.Lerp(amount, .36f, genesis); }
            if (ame > 0f) { tint = Color.Lerp(tint, new Color(.56f, .66f, .9f), ame); amount = Mathf.Lerp(amount, .3f, ame); }
            if (blue > 0f) { tint = Color.Lerp(tint, new Color(.2f, .4f, 1f), blue); amount = Mathf.Lerp(amount, .3f + .12f * Mathf.Sin(t * 5f), blue); }
            if (under > 0f) { tint = Color.Lerp(tint, new Color(.1f, .6f, .66f), under); amount = Mathf.Lerp(amount, .5f + .05f * Mathf.Sin(t * .8f), under); }
            if (dim > 0f) { tint = Color.Lerp(tint, new Color(.16f, .12f, .24f), dim); amount = Mathf.Lerp(amount, .5f, dim); }
            if (rest > 0f) { tint = Color.Lerp(tint, new Color(.06f, .08f, .2f), rest); amount = Mathf.Lerp(amount, .66f, rest); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .22f + .1f * energy + ame * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSmoke)
            {
                nextSmoke = t + .2f;
                foreach (var p in smokes) if ((p - cp).sqrMagnitude < 60f * 60f) smoke.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .12f + Mathf.Sin(t + p.x) * .04f, .26f, wind.y * .12f) }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .08f; var p = cp + new Vector3(Random.Range(-16f, 16f), 0, Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p + Vector3.up)) return;
                if (under > .05f && Random.value < under) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(-1f, 2f), velocity = new Vector3(Mathf.Sin(t * 2f + p.x) * .2f, Random.Range(.5f, 1.2f), Mathf.Cos(t * 2f + p.z) * .2f), startColor = new Color(.6f, 1f, 1f, .7f), startSize = Random.Range(.06f, .2f) }, 1);
                if (bloom > .05f && Random.value < bloom * .6f) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(2f, 6f), velocity = new Vector3(wind.x * .5f, -.4f, wind.y * .5f), startColor = Random.value < .5f ? new Color(1f, .7f, .8f) : new Color(1f, .95f, .8f), startSize = .1f, startLifetime = 9f }, 1);
                if (rest > .3f && Random.value < .1f) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(1f, 5f), velocity = new Vector3(0, .1f, 0), startColor = new Color(.8f, .86f, 1f), startSize = .05f, startLifetime = 6f }, 1);
                if (Random.value < .06f) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(.4f, 2.4f), velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(-.05f, .1f), Random.Range(-.2f, .2f)), startColor = new Color(1f, .9f, .5f), startSize = .05f }, 1);
            }
        }
    }
}
