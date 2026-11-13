using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Far Shores on a bright blue afternoon:
    /// - white clouds cross; a sea breeze moves the palms; butterflies wander; the camp fire flickers and sparks;
    /// - "Águas de Março": a warm shower crosses the land. Mulatu Astatke's "Tezeta" (memory): the whole day turns
    ///   the colour of an old photograph. "Sleep Walk": the afternoon dims to a lilac dusk. Tinariwen's songs: the
    ///   wind gets up, sand blows across the dunes and the fire leaps. "Banana Boat (Day-O)": daylight come: everything
    ///   brightens. "Estrelar": gold sparkles rise round Marcos Valle's. "Mas, Que Nada!": petals rain on the beach bar.
    ///   Every song's own light brightens while it plays.
    public class FarLife : MonoBehaviour
    {
        const string Land = "The Far Shores";
        public Vector3 center, starsAt, sambaAt;
        public string aguasSong, sleepSong, dayoSong, estrelarSong, sambaSong;
        public HashSet<string> tezeta = new HashSet<string>(), desert = new HashSet<string>();
        public Material fire, lamps; public float played = -99f;
        public List<Vector3> sparks = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots, sand;
        Color fireBase, lampBase;
        float aguas, memory, sleep, wind, dayo, estrelar, samba, nextFly, nextSpark, nextSand, nextDrop;
        static readonly Color[] Wings = { new Color(1f, .8f, .2f), new Color(.95f, .45f, .3f), new Color(.3f, .7f, .9f), Color.white };
        static readonly Color[] Petals = { new Color(1f, .85f, .2f), new Color(.2f, .7f, .35f), new Color(.25f, .45f, .9f), Color.white };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2800f; clouds.key = "farshores";
            clouds.minHeight = 170f; clouds.maxHeight = 270f; clouds.size = 3.4f; clouds.speed = 4f; clouds.cover = .28f;
            clouds.color = new Color(1f, 1f, .98f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            fireBase = E(fire); lampBase = E(lamps);
            dots = Particles("Butterflies, sparks and petals", 700, Color.white, .06f, .12f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            sand = Particles("Blown sand", 600, new Color(.95f, .8f, .5f, .16f), .6f, 1.8f, false);
            var sm = sand.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
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

            aguas = Mathf.MoveTowards(aguas, On(aguasSong) ? 1f : 0f, dt / 5f);
            memory = Mathf.MoveTowards(memory, here && id != null && tezeta.Contains(id) ? 1f : 0f, dt / 6f);
            sleep = Mathf.MoveTowards(sleep, On(sleepSong) ? 1f : 0f, dt / 6f);
            wind = Mathf.MoveTowards(wind, here && id != null && desert.Contains(id) ? 1f : 0f, dt / 4f);
            dayo = Mathf.MoveTowards(dayo, On(dayoSong) ? 1f : 0f, dt / 3f);
            estrelar = Mathf.MoveTowards(estrelar, On(estrelarSong) ? 1f : 0f, dt / 2f);
            samba = Mathf.MoveTowards(samba, On(sambaSong) ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            var lift = t - played < 4f ? 1f - (t - played) / 4f : 0f;
            if (fire != null) fire.SetColor("_EmissionColor", fireBase * (.8f + .35f * Mathf.PerlinNoise(t * 5f, 2.1f) + wind * 1.2f + lift * .3f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + sleep * 2.4f + aguas * .8f));

            // the sky: blue; a shower for Águas de Março; sepia for Tezeta; lilac dusk for Sleep Walk; gold for the desert; brighter for Day-O
            SkyDirector.SetSongRain(Land, aguas * .5f);
            var tint = new Color(1f, .98f, .92f); var amount = .03f;
            if (dayo > 0f) { tint = Color.Lerp(tint, new Color(1f, .93f, .6f), dayo); amount = Mathf.Lerp(amount, .22f, dayo); }
            if (aguas > 0f) { tint = Color.Lerp(tint, new Color(.6f, .72f, .8f), aguas); amount = Mathf.Lerp(amount, .35f, aguas); }
            if (wind > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .4f), wind); amount = Mathf.Lerp(amount, .36f, wind); }
            if (memory > 0f) { tint = Color.Lerp(tint, new Color(.78f, .6f, .38f), memory); amount = Mathf.Lerp(amount, .6f, memory); }
            if (sleep > 0f) { tint = Color.Lerp(tint, new Color(.5f, .42f, .78f), sleep); amount = Mathf.Lerp(amount, .62f, sleep); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var w2 = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .4f + .15f * energy + wind * .6f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w2.x, 0, w2.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextFly && aguas < .5f)
            {
                nextFly = t + .35f; var p = cp + new Vector3(Random.Range(-12f, 12f), Random.Range(-.6f, 2.4f), Random.Range(-12f, 12f));
                if (!IndoorSpaces.Contains(p)) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.6f, .6f), Random.Range(-.1f, .3f), Random.Range(-.6f, .6f)), startColor = Wings[Random.Range(0, Wings.Length)] }, 1);
            }
            if (t >= nextSpark)
            {
                nextSpark = t + .1f;
                foreach (var p in sparks) if ((p - cp).sqrMagnitude < 80f * 80f) dots.Emit(new ParticleSystem.EmitParams { position = p + new Vector3(Random.Range(-.3f, .3f), 0, Random.Range(-.3f, .3f)), velocity = new Vector3(w2.x * .4f, Random.Range(1f, 2.2f) * (1f + wind), w2.y * .4f), startColor = new Color(1f, .6f, .2f), startSize = .06f, startLifetime = 2f }, 1);
                if (estrelar > 0f && starsAt != Vector3.zero) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = starsAt + new Vector3(Random.Range(-4f, 4f), Random.Range(0f, 1f), Random.Range(-4f, 4f)), velocity = new Vector3(0, Random.Range(.8f, 1.6f), 0), startColor = new Color(1f, .9f, .4f), startSize = Random.Range(.08f, .16f) }, 1);
            }
            if (t >= nextDrop && samba > 0f && sambaAt != Vector3.zero)
            {
                nextDrop = t + .06f;
                dots.Emit(new ParticleSystem.EmitParams { position = sambaAt + new Vector3(Random.Range(-6f, 6f), Random.Range(4f, 6f), Random.Range(-6f, 6f)), velocity = new Vector3(w2.x * .5f, -1.2f, w2.y * .5f), startColor = Petals[Random.Range(0, Petals.Length)], startSize = Random.Range(.1f, .16f) }, 1);
            }
            if (t >= nextSand && wind > 0f)
            {
                nextSand = t + .03f; var p = cp + new Vector3(Random.Range(-22f, 22f), Random.Range(-1f, 2f), Random.Range(-22f, 22f));
                if (!IndoorSpaces.Contains(p)) sand.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(w2.x, .05f, w2.y) * (5f + 4f * wind) }, 1);
            }
        }
    }
}
