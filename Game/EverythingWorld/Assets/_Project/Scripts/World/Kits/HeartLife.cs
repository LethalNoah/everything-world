using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Heartbreak Boardwalk at dusk:
    /// - the sun sits low and pink over the sea; the globe lamps glow; the neon buzzes and drops out now and then;
    ///   gulls drift; the sea glimmers; the carousel's lights come on whenever anybody plays;
    /// - "Summertime Sadness": a hot gold haze. "Ultraviolence": deep violet. "West Coast": pink and gold. "ocean eyes":
    ///   everything goes sea-blue and the water glows. "Happiness is a butterfly": butterflies. "Lasso The Moon": cool
    ///   silver moonlight. "good 4 u": a purple flicker. "Happier Than Ever": a storm gathers and the rain comes.
    ///   "No Time To Die": dark. "Counting Stars": night falls. "Stop The Wedding!": white petals. Every song's own
    ///   light brightens while it plays.
    public class HeartLife : MonoBehaviour
    {
        const string Land = "The Heartbreak Boardwalk";
        public Vector3 center, carousel, pierEnd;
        public string sadSong, violenceSong, westSong, oceanSong, butterflySong, moonSong, goodSong, happierSong, dieSong, starsSong, weddingSong;
        public Material lamps, neon, windows, sea; public float played = -99f;
        public List<Vector3> smokes = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, neonBase, windowBase, seaBase;
        float nextSmoke, sad, vio, west, ocean, fly, moon, good, happier, die, stars, wed, nextFall, nextGull;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2600f; clouds.key = "heartbreak";
            clouds.minHeight = 160f; clouds.maxHeight = 240f; clouds.size = 3.8f; clouds.speed = 1.4f; clouds.cover = .36f;
            clouds.color = new Color(1f, .7f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); neonBase = E(neon); windowBase = E(windows); seaBase = E(sea);
            dots = Particles("Gulls, petals and butterflies", 600, Color.white, .08f, .14f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
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
            float Ease(float v, string song, float secs) => Mathf.MoveTowards(v, On(song) ? 1f : 0f, dt / secs);

            sad = Ease(sad, sadSong, 5f); vio = Ease(vio, violenceSong, 5f); west = Ease(west, westSong, 5f); ocean = Ease(ocean, oceanSong, 5f); fly = Ease(fly, butterflySong, 4f); moon = Ease(moon, moonSong, 5f);
            good = Ease(good, goodSong, 2f); happier = Ease(happier, happierSong, 8f); die = Ease(die, dieSong, 6f); stars = Ease(stars, starsSong, 6f); wed = Ease(wed, weddingSong, 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 1.8f) * .6f)));

            // the neon buzzes and drops out for a beat now and then; the lamps breathe; the sea glimmers
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f;
            var buzz = Mathf.PerlinNoise(t * 4f, 3.1f) > .82f ? .15f : 1f;
            if (neon != null) neon.SetColor("_EmissionColor", neonBase * buzz * (1f + good * (Mathf.Repeat(t * 3f, 1f) < .5f ? 1.2f : 0f) + lift * .6f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (.92f + .08f * Mathf.Sin(t * .5f) + stars * .5f + die * .3f + lift * .5f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + stars * .6f));
            if (sea != null) sea.SetColor("_EmissionColor", seaBase * (.8f + .3f * Mathf.PerlinNoise(t * .8f, 1.7f) + ocean * 2.4f));

            SkyDirector.SetSongRain(Land, happier * .7f);
            var tint = new Color(1f, .84f, .8f); var amount = .05f;
            if (west > 0f) { tint = Color.Lerp(tint, new Color(1f, .66f, .6f), west); amount = Mathf.Lerp(amount, .34f, west); }
            if (sad > 0f) { tint = Color.Lerp(tint, new Color(1f, .78f, .42f), sad); amount = Mathf.Lerp(amount, .4f, sad); }
            if (wed > 0f) { tint = Color.Lerp(tint, new Color(1f, .96f, .94f), wed); amount = Mathf.Lerp(amount, .3f, wed); }
            if (fly > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .7f), fly); amount = Mathf.Lerp(amount, .24f, fly); }
            if (moon > 0f) { tint = Color.Lerp(tint, new Color(.76f, .82f, 1f), moon); amount = Mathf.Lerp(amount, .45f, moon); }
            if (ocean > 0f) { tint = Color.Lerp(tint, new Color(.4f, .7f, .9f), ocean); amount = Mathf.Lerp(amount, .45f, ocean); }
            if (good > 0f) { tint = Color.Lerp(tint, new Color(.7f, .3f, 1f), good); amount = Mathf.Lerp(amount, .3f + (Mathf.Repeat(t * 3f, 1f) < .5f ? .14f : 0f), good); }
            if (vio > 0f) { tint = Color.Lerp(tint, new Color(.46f, .26f, .7f), vio); amount = Mathf.Lerp(amount, .5f, vio); }
            if (happier > 0f) { tint = Color.Lerp(tint, new Color(.4f, .44f, .56f), happier); amount = Mathf.Lerp(amount, .4f, happier); }
            if (stars > 0f) { tint = Color.Lerp(tint, new Color(.16f, .2f, .44f), stars); amount = Mathf.Lerp(amount, .6f, stars); }
            if (die > 0f) { tint = Color.Lerp(tint, new Color(.1f, .08f, .12f), die); amount = Mathf.Lerp(amount, .62f, die); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .35f + .15f * energy + happier * .5f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSmoke) { nextSmoke = t + .25f; foreach (var p in smokes) if ((p - cp).sqrMagnitude < 40f * 40f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .1f, .25f, wind.y * .1f), startColor = new Color(.85f, .85f, .9f, .25f), startSize = .2f, startLifetime = 4f }, 1); }
            // gulls: little white shapes gliding high
            if (t >= nextGull) { nextGull = t + Random.Range(1.5f, 4f); var p = cp + new Vector3(Random.Range(-40f, 40f), Random.Range(14f, 26f), Random.Range(-40f, 40f)); dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * 3f + Random.Range(-1f, 1f), 0f, wind.y * 3f + Random.Range(-1f, 1f)), startColor = Color.white, startSize = .5f, startLifetime = 9f }, 1); }
            // the carousel's lights when anybody plays
            if (lift > .05f && (carousel - cp).sqrMagnitude < 60f * 60f && Random.value < lift) dots.Emit(new ParticleSystem.EmitParams { position = carousel + new Vector3(Random.Range(-5f, 5f), 4.2f, Random.Range(-5f, 5f)), velocity = Vector3.up * .2f, startColor = new Color(1f, .86f, .6f), startSize = .2f, startLifetime = 1.5f }, 1);
            if (t >= nextFall)
            {
                nextFall = t + .08f; var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(.5f, 5f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                if (fly > .05f && Random.value < fly * .6f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.6f, .6f), Random.Range(-.1f, .3f), Random.Range(-.6f, .6f)), startColor = Random.value < .5f ? new Color(1f, .7f, .8f) : new Color(.7f, .8f, 1f), startSize = .16f }, 1);
                if (wed > .05f && Random.value < wed) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * 5f, velocity = new Vector3(wind.x * .5f, -.7f, wind.y * .5f), startColor = Color.white, startSize = .1f, startLifetime = 8f }, 1);
                if (stars > .3f && Random.value < .08f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = Vector3.zero, startColor = new Color(1f, .9f, .5f), startSize = .05f }, 1);
            }
        }
    }
}
