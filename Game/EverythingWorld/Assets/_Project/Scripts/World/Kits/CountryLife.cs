using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Country Plains at golden hour going into dusk:
    /// - the windmills turn with the wind; the wheat sways; the bonfire flickers and throws sparks; fireflies come up
    ///   out of the grass; the lamps glow; long gold clouds;
    /// - "Something in the Orange": a deep orange sunset. "American Honey": honey gold. "Big Green Tractor": green and
    ///   gold. "Colder Weather": snow. "Rain Is A Good Thing": rain. "Need You Now" and "Walkin' After Midnight": night
    ///   falls. "The Night The Lights Went Out In Georgia": every lamp goes out. "The Devil Went Down to Georgia": red
    ///   firelight and the bonfire roars. "Can't Fight The Moonlight": silver moonlight. "Hurt": the colour drains.
    ///   Every song's own light brightens while it plays.
    public class CountryLife : MonoBehaviour
    {
        const string Land = "The Country Plains";
        public Vector3 center, fireAt;
        public string orangeSong, honeySong, tractorSong, colderSong, rainSong, needSong, midnightSong, lightsSong, devilSong, moonSong, hurtSong;
        public Material windows, lamps, fire; public float played = -99f;
        public List<Transform> wheels = new List<Transform>(); public List<Vector3> smokes = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color windowBase, lampBase, fireBase;
        float nextSmoke, orange, honey, tractor, colder, rain, night, outA, devil, moon, hurt, nextFly, nextSpark, nextSnow;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 34; clouds.span = 2600f; clouds.key = "countryplains";
            clouds.minHeight = 180f; clouds.maxHeight = 260f; clouds.size = 4f; clouds.speed = 2f; clouds.cover = .34f;
            clouds.color = new Color(1f, .82f, .62f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            windowBase = E(windows); lampBase = E(lamps); fireBase = E(fire);
            dots = Particles("Sparks, fireflies and snow", 900, Color.white, .05f, .1f, true);
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

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;
            float Ease(float v, bool on, float secs) => Mathf.MoveTowards(v, on ? 1f : 0f, dt / secs);

            orange = Ease(orange, On(orangeSong), 5f); honey = Ease(honey, On(honeySong), 4f); tractor = Ease(tractor, On(tractorSong), 4f); colder = Ease(colder, On(colderSong), 5f); rain = Ease(rain, On(rainSong), 6f); night = Ease(night, On(needSong) || On(midnightSong), 6f); outA = Ease(outA, On(lightsSong), 3f); devil = Ease(devil, On(devilSong), 2f); moon = Ease(moon, On(moonSong), 5f); hurt = Ease(hurt, On(hurtSong), 6f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2f) * .6f)));

            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0); var windy = .4f + .2f * energy + rain * .4f + colder * .3f;
            foreach (var w in wheels) if (w != null) w.Rotate(0f, 0f, dt * 40f * windy, Space.Self);
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f;
            if (fire != null) fire.SetColor("_EmissionColor", fireBase * (.8f + .35f * Mathf.PerlinNoise(t * 5f, 2.1f) + devil * 1.6f + lift * .5f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + night * .7f) * (1f - outA * .97f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + night * .5f) * (1f - outA * .9f));

            SkyDirector.SetSongRain(Land, rain * .6f);
            var tint = new Color(1f, .9f, .74f); var amount = .05f;
            if (honey > 0f) { tint = Color.Lerp(tint, new Color(1f, .78f, .3f), honey); amount = Mathf.Lerp(amount, .36f, honey); }
            if (tractor > 0f) { tint = Color.Lerp(tint, new Color(.7f, .9f, .4f), tractor); amount = Mathf.Lerp(amount, .3f, tractor); }
            if (orange > 0f) { tint = Color.Lerp(tint, new Color(1f, .52f, .2f), orange); amount = Mathf.Lerp(amount, .45f, orange); }
            if (colder > 0f) { tint = Color.Lerp(tint, new Color(.8f, .88f, 1f), colder); amount = Mathf.Lerp(amount, .45f, colder); }
            if (rain > 0f) { tint = Color.Lerp(tint, new Color(.56f, .62f, .7f), rain); amount = Mathf.Lerp(amount, .4f, rain); }
            if (moon > 0f) { tint = Color.Lerp(tint, new Color(.6f, .7f, 1f), moon); amount = Mathf.Lerp(amount, .5f, moon); }
            if (hurt > 0f) { tint = Color.Lerp(tint, new Color(.5f, .5f, .5f), hurt); amount = Mathf.Lerp(amount, .55f, hurt); }
            if (night > 0f) { tint = Color.Lerp(tint, new Color(.16f, .2f, .4f), night); amount = Mathf.Lerp(amount, .6f, night); }
            if (outA > 0f) { tint = Color.Lerp(tint, new Color(.08f, .08f, .14f), outA); amount = Mathf.Lerp(amount, .66f, outA); }
            if (devil > 0f) { tint = Color.Lerp(tint, new Color(1f, .2f, .1f), devil); amount = Mathf.Lerp(amount, .3f + .2f * Mathf.PerlinNoise(t * 6f, 1f), devil); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", windy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSmoke) { nextSmoke = t + .25f; foreach (var p in smokes) if ((p - cp).sqrMagnitude < 60f * 60f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .3f, .5f, wind.y * .3f), startColor = new Color(.8f, .8f, .82f, .3f), startSize = .5f, startLifetime = 5f }, 1); }
            if (t >= nextSpark && (fireAt - cp).sqrMagnitude < 80f * 80f) { nextSpark = t + Mathf.Lerp(.1f, .02f, devil); dots.Emit(new ParticleSystem.EmitParams { position = fireAt + new Vector3(Random.Range(-.6f, .6f), 0, Random.Range(-.6f, .6f)), velocity = new Vector3(wind.x * .5f + Random.Range(-.3f, .3f), Random.Range(1.5f, 3f + devil * 3f), wind.y * .5f), startColor = new Color(1f, .6f, .2f), startSize = .07f, startLifetime = 2.2f }, 1); }
            if (t >= nextFly && rain < .5f && colder < .5f) { nextFly = t + Mathf.Lerp(.5f, .15f, night + outA); var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(.3f, 2.4f), Random.Range(-16f, 16f)); if (!IndoorSpaces.Contains(p)) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(-.05f, .15f), Random.Range(-.2f, .2f)), startColor = new Color(.9f, 1f, .4f), startSize = .06f, startLifetime = 4f }, 1); }
            if (colder > .05f && t >= nextSnow) { nextSnow = t + .03f; for (var k = 0; k < 2; k++) { var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(4f, 9f), Random.Range(-18f, 18f)); if (Random.value < colder && !IndoorSpaces.Contains(p)) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x, -1f, wind.y), startColor = Color.white, startSize = .08f, startLifetime = 8f }, 1); } }
        }
    }
}
