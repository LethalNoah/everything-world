using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Dubstep Shards at night:
    /// - a half-time bass wobble on the shared clock drives everything (friends see it together): every crystal shard pulses
    ///   with the wobble, its rate changing bar by bar; woofers punch out on the kick; lasers sweep from the top of the
    ///   Speaker Cliff; the Core turns over its crater with shards orbiting it and spins faster on the drop; motes of crystal
    ///   dust drift; hitting the sub makes the whole canyon flare;
    /// - "Scary Monsters and Nice Sprites": lime strobes. "Bass Head": a deep purple wobble. "CHROMA": colours cycle.
    ///   "Blackout": the canyon goes dark with white flashes. "Lunar": silver. "Teardrop": blue, with rain. "Into The Light":
    ///   white. "Colorblind": blue and yellow. "Stargarden": stars fall. Every song's own light brightens while it plays.
    public class ShardLife : MonoBehaviour
    {
        const string Land = "The Dubstep Shards";
        public Vector3 center, stageFwd = Vector3.forward, coreHome;
        public string chromaSong, blackoutSong, lunarSong, starSong, bassSong, tearSong, colorSong, monsterSong, lightSong;
        public Material shards, shards2, lamps, cones, pool, coreM; public Transform core; public float played = -99f;
        public List<(Transform t, Vector3 home, float ph)> woofers = new List<(Transform, Vector3, float)>();
        public List<(Transform t, float ph)> lasers = new List<(Transform, float)>(), orbiters = new List<(Transform, float)>();
        public List<(Transform t, Vector3 home)> spinners = new List<(Transform, Vector3)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color shardBase, shard2Base, lampBase, coneBase, poolBase, coreBase;
        float chroma, blackout, lunar, star, bass, tear, color, monster, light, coreAngle, nextFall;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 20; clouds.span = 2400f; clouds.key = "shards";
            clouds.minHeight = 140f; clouds.maxHeight = 230f; clouds.size = 3f; clouds.speed = 1f; clouds.cover = .25f;
            clouds.color = new Color(.3f, .2f, .44f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            shardBase = E(shards); shard2Base = E(shards2); lampBase = E(lamps); coneBase = E(cones); poolBase = E(pool); coreBase = E(coreM);
            dots = Particles("Crystal dust, rain and stars", 1400, Color.white, .05f, .14f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 7f);
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
            float Ease(float v, bool on, float secs) => Mathf.MoveTowards(v, on ? 1f : 0f, dt / secs);

            chroma = Ease(chroma, On(chromaSong), 2f); blackout = Ease(blackout, On(blackoutSong), 2f); lunar = Ease(lunar, On(lunarSong), 4f); star = Ease(star, On(starSong), 4f); bass = Ease(bass, On(bassSong), 2f); tear = Ease(tear, On(tearSong), 3f); color = Ease(color, On(colorSong), 3f); monster = Ease(monster, On(monsterSong), 2f); light = Ease(light, On(lightSong), 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the wobble: 140 bpm in half time; the wobble rate changes each bar (1, 2, 3 or 4 per beat)
            var beatT = now * 140f / 60f / 2f; var beat = Mathf.Repeat(beatT, 1f); var bar = Mathf.FloorToInt(beatT / 4f); var rate = new[] { 1f, 2f, 3f, 4f, 2f, 1f, 4f, 3f }[bar & 7];
            var wob = .5f + .5f * Mathf.Sin(beatT * Mathf.PI * 2f * rate); var kick = Mathf.Exp(-beat * 7f);
            var since = t - played; var drop = since < 4f ? Mathf.Clamp01(1f - since / 4f) : 0f; var drive = Mathf.Clamp01(.35f + energy * .65f + drop + bass * .5f);
            var dark = blackout * (Mathf.Repeat(now * 2.3f, 1f) < .08f ? 0f : 1f);

            if (shards != null) shards.SetColor("_EmissionColor", (chroma > .1f ? Color.HSVToRGB(Mathf.Repeat(now * .3f, 1f), .7f, 1f) * 1.4f : color > .1f ? new Color(.2f, .4f, 1f) * 1.4f : shardBase) * ((.5f + wob * .9f * drive + drop * 2f) * (1f - dark * .85f)));
            if (shards2 != null) shards2.SetColor("_EmissionColor", (chroma > .1f ? Color.HSVToRGB(Mathf.Repeat(now * .3f + .5f, 1f), .7f, 1f) * 1.4f : color > .1f ? new Color(1f, .86f, .2f) * 1.4f : monster > .1f ? new Color(.6f, 1f, .2f) * 1.4f : shard2Base) * ((.5f + (1f - wob) * .9f * drive + drop * 2f) * (1f - dark * .85f)));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (.9f + kick * .3f + drop));
            if (cones != null) cones.SetColor("_EmissionColor", coneBase * (.4f + kick * 1.6f * drive + drop));
            if (pool != null) pool.SetColor("_EmissionColor", poolBase * (.7f + wob * .5f + drop));
            if (coreM != null) coreM.SetColor("_EmissionColor", coreBase * (.8f + wob * .5f + drop * 2f + light));

            foreach (var (tr, home, ph) in woofers) if (tr != null) { var k = Mathf.Exp(-Mathf.Repeat(beatT + ph * .1f, 1f) * 7f); tr.position = home + tr.forward * k * .25f * drive; }
            for (var i = 0; i < lasers.Count; i++) { var (tr, ph) = lasers[i]; if (tr == null) continue; tr.gameObject.SetActive(dark < .5f); tr.rotation = Quaternion.LookRotation(Quaternion.Euler(-14f - Mathf.Sin(now * .9f + ph) * 12f, Mathf.Sin(now * (.6f + rate * .15f) + ph) * 55f, 0) * stageFwd); }
            coreAngle += dt * (8f + energy * 20f + drop * 120f + bass * 30f);
            if (core != null) { core.position = coreHome + Vector3.up * Mathf.Sin(t * .5f) * .8f; core.rotation = Quaternion.Euler(0, coreAngle, 0); }
            foreach (var (tr, ph) in orbiters) if (tr != null) { var a = coreAngle * Mathf.Deg2Rad * .7f + ph; tr.position = coreHome + new Vector3(Mathf.Cos(a) * 9f, Mathf.Sin(a * 2f + ph) * 2f, Mathf.Sin(a) * 9f); tr.rotation = Quaternion.Euler(a * 40f, a * 70f, 0); }
            for (var i = 0; i < spinners.Count; i++) { var (tr, home) = spinners[i]; if (tr == null) continue; tr.position = home + Vector3.up * Mathf.Sin(t + i) * .2f; tr.Rotate(0, dt * (30f + kick * 60f), 0, Space.World); }

            var tint = new Color(.5f, .3f, 1f); var amount = .1f + wob * .05f * drive;
            if (light > 0f) { tint = Color.Lerp(tint, Color.white, light); amount = Mathf.Lerp(amount, .4f, light); }
            if (lunar > 0f) { tint = Color.Lerp(tint, new Color(.8f, .86f, 1f), lunar); amount = Mathf.Lerp(amount, .36f, lunar); }
            if (tear > 0f) { tint = Color.Lerp(tint, new Color(.3f, .5f, 1f), tear); amount = Mathf.Lerp(amount, .38f, tear); }
            if (color > 0f) { tint = Color.Lerp(tint, Mathf.Repeat(now * .5f, 1f) < .5f ? new Color(.2f, .4f, 1f) : new Color(1f, .9f, .2f), color); amount = Mathf.Lerp(amount, .36f, color); }
            if (star > 0f) { tint = Color.Lerp(tint, new Color(.7f, .8f, 1f), star); amount = Mathf.Lerp(amount, .3f, star); }
            if (chroma > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .3f, 1f), .7f, 1f), chroma); amount = Mathf.Lerp(amount, .4f, chroma); }
            if (bass > 0f) { tint = Color.Lerp(tint, new Color(.5f, .1f, .8f), bass); amount = Mathf.Lerp(amount, .3f + wob * .2f, bass); }
            if (monster > 0f) { tint = Color.Lerp(tint, new Color(.6f, 1f, .2f), monster); amount = Mathf.Lerp(amount, beat < .1f ? .55f : .3f, monster); }
            if (drop > 0f) { tint = Color.Lerp(tint, Color.white, drop * .5f); amount = Mathf.Lerp(amount, .5f, drop); }
            if (blackout > 0f) { tint = Color.Lerp(tint, dark > .5f ? new Color(.02f, .02f, .05f) : Color.white, blackout); amount = Mathf.Lerp(amount, dark > .5f ? .7f : .6f, blackout); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .2f * energy + wob * .15f * drive + tear * .4f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(.5f, 5f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .2f + drop * .6f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(.1f, .5f) + drop * 2f, Random.Range(-.3f, .3f)), startColor = Random.value < .5f ? new Color(.3f, .9f, 1f) : new Color(1f, .3f, .8f), startSize = .06f, startLifetime = 4f }, 1);
                if (tear > .05f && Random.value < tear) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -9f, wind.y), startColor = new Color(.5f, .7f, 1f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
                if (star > .05f && Random.value < star) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(8f, 14f), Random.Range(-16f, 16f)), velocity = new Vector3(Random.Range(-.2f, .2f), -1.4f, Random.Range(-.2f, .2f)), startColor = new Color(.9f, .95f, 1f), startSize = .12f, startLifetime = 8f }, 1);
            }
        }
    }
}
