using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Afrobeat Coast at sunset:
    /// - the great sun sits on the sea and its road of light shimmers; the coloured bulbs over Market Road chase down
    ///   the hill, faster with the music, and jump when the Big Drum is hit; the bonfires flicker and throw sparks;
    ///   smoke rises from the suya grills; fireflies come out; long clouds lie pink across the sky;
    /// - "Water No Get Enemy": the Shrine's fountain leaps and the evening turns sea-blue. "Last Last": the bonfires
    ///   roar up. "Essence": the sky goes rose and the windows warm. "Fantastic Man": every bulb runs through every
    ///   colour. "Rush": the bus tears up and down and the bulbs race. "Fall": bananas rain on Davido's stall.
    ///   "love nwantiti": hearts float up from CKay's. Every song's own light brightens while it plays.
    public class AfroLife : MonoBehaviour
    {
        const string Land = "The Afrobeat Coast";
        public Vector3 center, sunAt, fountainAt, bananasAt, heartsAt;
        public string waterSong, lastSong, essenceSong, fantasticSong, rushSong, fallSong, heartsSong;
        public Material fire, embers, sun, sunRoad, windows; public Material[] bulbs;
        public float played = -99f, rush;
        public List<Vector3> sparks = new List<Vector3>(), smoke = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem sparkDots, mist, water, drops, flies;
        Color fireBase, emberBase, sunBase, roadBase, windowBase; Color[] bulbBase;
        float watr, last, essence, fantastic, fall, hearts, nextSpark, nextSmoke, nextWater, nextDrop, nextFly;
        static readonly Color Banana = new Color(1f, .88f, .2f), Heart = new Color(1f, .35f, .6f);

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 34; clouds.span = 2600f; clouds.key = "afro";
            clouds.minHeight = 150f; clouds.maxHeight = 240f; clouds.size = 3.2f; clouds.speed = 2.2f; clouds.cover = .35f;
            clouds.color = new Color(1f, .56f, .5f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            fireBase = E(fire); emberBase = E(embers); sunBase = E(sun); roadBase = E(sunRoad); windowBase = E(windows);
            bulbBase = new Color[bulbs != null ? bulbs.Length : 0]; for (var i = 0; i < bulbBase.Length; i++) bulbBase[i] = E(bulbs[i]);
            sparkDots = Particles("Sparks", 500, new Color(1f, .6f, .2f, .95f), .04f, .09f, true);
            var sm = sparkDots.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f); sm.gravityModifier = -.05f;
            mist = Particles("Grill smoke", 300, new Color(.85f, .8f, .8f, .14f), .4f, .9f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); var sz = mist.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .5f, 1f, 2.2f));
            water = Particles("Fountain", 400, new Color(.5f, .9f, 1f, .85f), .05f, .1f, true);
            var wm = water.main; wm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f); wm.gravityModifier = 1f;
            drops = Particles("Falling things", 400, Color.white, .1f, .18f, true);
            var dm = drops.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            flies = Particles("Fireflies", 300, new Color(1f, .9f, .5f, .9f), .03f, .06f, true);
            var fm = flies.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
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

            watr = Mathf.MoveTowards(watr, On(waterSong) ? 1f : 0f, dt / 3f);
            last = Mathf.MoveTowards(last, On(lastSong) ? 1f : 0f, dt / 3f);
            essence = Mathf.MoveTowards(essence, On(essenceSong) ? 1f : 0f, dt / 4f);
            fantastic = Mathf.MoveTowards(fantastic, On(fantasticSong) ? 1f : 0f, dt / 2f);
            rush = Mathf.MoveTowards(rush, On(rushSong) ? 1f : 0f, dt / 2f);
            fall = Mathf.MoveTowards(fall, On(fallSong) ? 1f : 0f, dt / 2f);
            hearts = Mathf.MoveTowards(hearts, On(heartsSong) ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the bulbs chase down the hill; they jump when somebody plays; they run the rainbow for Fantastic Man
            var lift = t - played < 4f ? 1f - (t - played) / 4f : 0f; var rate = 1.1f + energy * 1.4f + lift * 2f + rush * 5f + fantastic * 2f;
            if (bulbs != null) for (var i = 0; i < bulbs.Length; i++) if (bulbs[i] != null)
            {
                var ph = Mathf.Repeat(now * rate - i / 3f, 1f); var c = bulbBase[i]; if (fantastic > 0f) c = Color.Lerp(c, Color.HSVToRGB(Mathf.Repeat(now * .6f + i / 3f, 1f), .9f, 1f) * 2.4f, fantastic);
                bulbs[i].SetColor("_EmissionColor", c * (.55f + (ph < .4f ? .9f : 0f) + lift * .8f));
            }
            var flick = .8f + .35f * Mathf.PerlinNoise(t * 5f, 1.3f) + last * 1.6f + lift * .3f;
            if (fire != null) fire.SetColor("_EmissionColor", fireBase * flick); if (embers != null) embers.SetColor("_EmissionColor", emberBase * (.8f + .3f * Mathf.PerlinNoise(t * 2f, 7f) + last));
            if (sun != null) sun.SetColor("_EmissionColor", sunBase * (1f + .05f * Mathf.Sin(t * .5f) - essence * .25f)); if (sunRoad != null) sunRoad.SetColor("_EmissionColor", roadBase * (.75f + .25f * Mathf.Sin(t * 1.7f) + .15f * Mathf.Sin(t * 4.3f)));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + essence * .8f + .04f * Mathf.Sin(t * 1.3f)));

            var tint = new Color(1f, .6f, .45f); var amount = .1f;
            if (watr > 0f) { tint = Color.Lerp(tint, new Color(.3f, .7f, 1f), watr); amount = Mathf.Lerp(amount, .3f, watr); }
            if (last > 0f) { tint = Color.Lerp(tint, new Color(1f, .3f, .2f), last); amount = Mathf.Lerp(amount, .3f, last); }
            if (essence > 0f) { tint = Color.Lerp(tint, new Color(1f, .45f, .7f), essence); amount = Mathf.Lerp(amount, .4f, essence); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .2f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpark)
            {
                nextSpark = t + Mathf.Lerp(.12f, .03f, last);
                foreach (var p in sparks) if ((p - cp).sqrMagnitude < 90f * 90f) sparkDots.Emit(new ParticleSystem.EmitParams { position = p + new Vector3(Random.Range(-.3f, .3f), 0, Random.Range(-.3f, .3f)), velocity = new Vector3(wind.x * .4f + Random.Range(-.3f, .3f), Random.Range(1f, 2.4f) * (1f + last), wind.y * .4f + Random.Range(-.3f, .3f)) }, 1);
            }
            if (t >= nextSmoke)
            {
                nextSmoke = t + .2f;
                foreach (var p in smoke) if ((p - cp).sqrMagnitude < 50f * 50f) mist.Emit(new ParticleSystem.EmitParams { position = p + new Vector3(Random.Range(-.4f, .4f), 0, Random.Range(-.2f, .2f)), velocity = new Vector3(wind.x * .4f, Random.Range(.5f, .9f), wind.y * .4f) }, 1);
            }
            if (fountainAt != Vector3.zero && t >= nextWater && (fountainAt - cp).sqrMagnitude < 60f * 60f)
            {
                nextWater = t + Mathf.Lerp(.06f, .012f, watr); var a = Random.Range(0f, Mathf.PI * 2f);
                water.Emit(new ParticleSystem.EmitParams { position = fountainAt, velocity = new Vector3(Mathf.Cos(a) * .9f, 3.4f + watr * 3.4f, Mathf.Sin(a) * .9f) }, 1);
            }
            if (t >= nextDrop)
            {
                nextDrop = t + .07f;
                if (fall > 0f && bananasAt != Vector3.zero) for (var k = 0; k < 2; k++) drops.Emit(new ParticleSystem.EmitParams { position = bananasAt + new Vector3(Random.Range(-4f, 4f), Random.Range(4f, 7f), Random.Range(-4f, 4f)), velocity = Vector3.down * 2.2f, startColor = Banana }, 1);
                if (hearts > 0f && heartsAt != Vector3.zero) drops.Emit(new ParticleSystem.EmitParams { position = heartsAt + new Vector3(Random.Range(-2.4f, 2.4f), Random.Range(0f, 1f), Random.Range(-2.4f, 2.4f)), velocity = new Vector3(Random.Range(-.2f, .2f), 1f, Random.Range(-.2f, .2f)), startColor = Heart, startSize = Random.Range(.14f, .26f) }, 1);
            }
            if (t >= nextFly)
            {
                nextFly = t + .16f; var p = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(-.5f, 3.5f), Random.Range(-14f, 14f));
                if (!IndoorSpaces.Contains(p)) flies.Emit(new ParticleSystem.EmitParams { position = p, velocity = Random.insideUnitSphere * .35f }, 1);
            }
        }
    }
}
