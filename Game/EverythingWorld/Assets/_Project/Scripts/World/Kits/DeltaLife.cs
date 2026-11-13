using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Delta Crossroads on a hot afternoon with a storm coming:
    /// - dust drifts across the roads; the storm sits dark in the west and flickers with silent lightning now and
    ///   then (the same for everyone); a few heavy clouds cross; the blue bottles in the bottle trees glint; smoke
    ///   idles from the locomotive; a small black cloud rains on Albert King's sign and nowhere else;
    /// - Robert Johnson's songs: the afternoon goes dark as midnight, the bulbs gutter, eyes open in the cotton, and
    ///   at the head of the shadow on the west road something tall stands up. "Smokestack Lightnin'": lightning,
    ///   close and often, and the engine pours smoke. "Didn't It Rain": the storm finally breaks. "The Thrill Is
    ///   Gone": the colour goes out of the day. "Boom Boom" and Muddy Waters' songs: the juke joint's lights stomp.
    ///   Every song's own light brightens while it plays.
    public class DeltaLife : MonoBehaviour
    {
        const string Land = "The Delta Crossroads";
        public Vector3 center, smokeAt, cloudAt;
        public string wolfSong, rainSong, thrillSong, boomSong;
        public HashSet<string> johnson = new HashSet<string>(), muddy = new HashSet<string>();
        public Material bulbs, neon, bottles, eyes, storm; public Transform figure;
        public float played = -99f, stomped = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dust, smoke, drizzle;
        Color bulbBase, neonBase, bottleBase, stormBase;
        float devil, wolf, rain, thrill, boom, mud, nextDust, nextSmoke, nextDrizzle, flashUntil; long lastFlash = -1;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2600f; clouds.key = "delta";
            clouds.minHeight = 170f; clouds.maxHeight = 260f; clouds.size = 3.6f; clouds.speed = 4f; clouds.cover = .3f;
            clouds.color = new Color(.5f, .5f, .56f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            bulbBase = E(bulbs); neonBase = E(neon); bottleBase = E(bottles); stormBase = E(storm);
            dust = Particles("Dust", 500, new Color(.9f, .8f, .58f, .1f), .8f, 2.2f, false);
            var dm = dust.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            smoke = Particles("Engine smoke", 300, new Color(.2f, .2f, .22f, .32f), .8f, 1.6f, false);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .5f, 1f, 3f));
            drizzle = Particles("One small rain", 200, new Color(.7f, .8f, 1f, .7f), .03f, .05f, false);
            var zm = drizzle.main; zm.startLifetime = 1f; zm.gravityModifier = 1.2f;
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
            var dt = Time.deltaTime; var t = Time.time; var now = (float)(SharedChance.Now % 100000.0);
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;

            devil = Mathf.MoveTowards(devil, here && id != null && johnson.Contains(id) ? 1f : 0f, dt / 6f);
            mud = Mathf.MoveTowards(mud, here && id != null && muddy.Contains(id) ? 1f : 0f, dt / 2f);
            wolf = Mathf.MoveTowards(wolf, On(wolfSong) ? 1f : 0f, dt / 3f);
            rain = Mathf.MoveTowards(rain, On(rainSong) ? 1f : 0f, dt / 6f);
            thrill = Mathf.MoveTowards(thrill, On(thrillSong) ? 1f : 0f, dt / 5f);
            boom = Mathf.MoveTowards(boom, On(boomSong) ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // silent lightning in the storm bank: now and then for everyone; close and often for Smokestack Lightnin'
            var window = (long)(SharedChance.Now / 3.0);
            if (window != lastFlash) { lastFlash = window; if (SharedChance.Value(SharedChance.Key("delta lightning"), window) < .16 + wolf * .6 + rain * .3 + devil * .2) flashUntil = t + .45f; }
            var flash = t < flashUntil ? (Mathf.Sin((flashUntil - t) * 44f) > -.2f ? 1f : .15f) : 0f;
            if (storm != null) storm.SetColor("_EmissionColor", stormBase * (1f - devil * .6f) + new Color(.7f, .78f, 1f) * flash * .7f);

            // the bulbs: steady by day; they stomp in fours for Boom Boom and the juke joint, and gutter for Robert Johnson
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var beat = Mathf.Repeat(now * 2f, 1f); var st = t - stomped < 2.2f ? Mathf.Repeat((t - stomped) * 2f, 1f) : 1f;
            var stomp = Mathf.Max(boom, mud) * (beat < .25f ? 1.6f : 0f) + (st < .25f ? 1.6f : 0f); var gutter = devil * (Mathf.PerlinNoise(t * 9f, 3f) < .45f ? -.75f : .2f);
            if (bulbs != null) bulbs.SetColor("_EmissionColor", bulbBase * Mathf.Max(.12f, 1f + energy * .2f + lift * .4f + stomp + gutter));
            if (neon != null) neon.SetColor("_EmissionColor", neonBase * (1f + mud * 1.4f + boom * .8f + stomp * .4f + .06f * Mathf.Sin(t * 13f)));
            if (bottles != null) bottles.SetColor("_EmissionColor", bottleBase * (1f + .35f * Mathf.Sin(t * 1.4f) + devil * 2.4f));
            if (eyes != null) eyes.SetColor("_EmissionColor", new Color(1f, .15f, .08f) * (devil * (2.6f + .6f * Mathf.Sin(t * 3f))));
            if (figure != null) { var up = devil > .35f; if (figure.gameObject.activeSelf != up) figure.gameObject.SetActive(up); if (up) { var s = figure.localScale; s.y = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.35f, 1f, devil)); figure.localScale = s; } }

            // the sky: white heat; midnight for Robert Johnson; the storm for Didn't It Rain; grey for The Thrill Is Gone
            SkyDirector.SetSongRain(Land, rain * .85f);
            var tint = new Color(1f, .93f, .75f); var amount = .05f;
            if (thrill > 0f) { tint = Color.Lerp(tint, new Color(.5f, .54f, .64f), thrill); amount = Mathf.Lerp(amount, .5f, thrill); }
            if (wolf > 0f) { tint = Color.Lerp(tint, new Color(.3f, .34f, .5f), wolf); amount = Mathf.Lerp(amount, .45f, wolf); }
            if (rain > 0f) { tint = Color.Lerp(tint, new Color(.26f, .3f, .42f), rain); amount = Mathf.Lerp(amount, .6f, rain); }
            if (devil > 0f) { tint = Color.Lerp(tint, new Color(.03f, .03f, .1f), devil); amount = Mathf.Lerp(amount, .92f, devil); }
            if (flash > 0f && (wolf > 0f || rain > 0f)) { tint = Color.Lerp(tint, new Color(.8f, .85f, 1f), flash * .6f); amount *= 1f - flash * .5f; }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .45f + .2f * energy + .4f * Mathf.Max(wolf, rain) + .2f * devil); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextDust && rain < .5f)
            {
                nextDust = t + .1f;
                var p = cp + new Vector3(Random.Range(-26f, 26f), Random.Range(-1.2f, 1.5f), Random.Range(-26f, 26f));
                if (!IndoorSpaces.Contains(p)) dust.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * 1.6f, .1f, wind.y * 1.6f) }, 1);
            }
            if (smokeAt != Vector3.zero && t >= nextSmoke && (smokeAt - cp).sqrMagnitude < 160f * 160f)
            {
                nextSmoke = t + Mathf.Lerp(.5f, .05f, wolf);
                smoke.Emit(new ParticleSystem.EmitParams { position = smokeAt, velocity = new Vector3(wind.x * .8f, 1.4f + wolf * 1.6f, wind.y * .8f) }, 1);
            }
            if (cloudAt != Vector3.zero && t >= nextDrizzle && (cloudAt - cp).sqrMagnitude < 50f * 50f)
            {
                nextDrizzle = t + .03f;
                drizzle.Emit(new ParticleSystem.EmitParams { position = cloudAt + new Vector3(Random.Range(-1.2f, 1.2f), 0, Random.Range(-.8f, .8f)), velocity = Vector3.down * 3f }, 1);
            }
        }
    }
}
