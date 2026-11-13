using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Melodic Bass Highlands at twilight:
    /// - the floating islands bob and drift, rising higher when the music swells and when the rune stone is touched; the
    ///   great crystal over the seven lions turns; paper lanterns rise and fall at Mr FijiWiji's; spray rises from the pool
    ///   under the great waterfall; motes of light drift; the runes breathe with the music;
    /// - "Crab Rave": orange light and the crabs dance. "Time to Say Goodbye": lanterns float up into the sky. "Insomnia":
    ///   deep blue night with the stars out. "Bloom": pink petals. "The Fall": the waterfalls brighten and it rains softly.
    ///   "Days to Come": a golden dawn. "Retro City": neon pink and blue. "Nightfall": dark violet. Every song's own light
    ///   brightens while it plays.
    public class HighLife : MonoBehaviour
    {
        const string Land = "The Melodic Bass Highlands";
        public Vector3 center, splash;
        public string fallSong, daysSong, insomniaSong, byeSong, retroSong, bloomSong, crabSong, nightSong;
        public Material runes, lamps, water, crystals; public float played = -99f;
        public List<(Transform t, Vector3 home, float phase)> floaters = new List<(Transform, Vector3, float)>();
        public List<(Transform t, Vector3 home)> spinners = new List<(Transform, Vector3)>();
        public List<(Transform t, Vector3 home, float phase)> lanterns = new List<(Transform, Vector3, float)>();
        public List<(Transform t, Vector3 home)> crabs = new List<(Transform, Vector3)>();
        public List<Vector3> mists = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color runeBase, lampBase, waterBase, crystalBase;
        float fall, days, insomnia, bye, retro, bloom, crab, night, lift, nextFall, nextSpray;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2400f; clouds.key = "highlands";
            clouds.minHeight = 90f; clouds.maxHeight = 180f; clouds.size = 3.6f; clouds.speed = .9f; clouds.cover = .45f;
            clouds.color = new Color(1f, .7f, .8f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            runeBase = E(runes); lampBase = E(lamps); waterBase = E(water); crystalBase = E(crystals);
            dots = Particles("Motes, petals and spray", 1400, Color.white, .05f, .16f, true);
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

            fall = Ease(fall, On(fallSong), 4f); days = Ease(days, On(daysSong), 5f); insomnia = Ease(insomnia, On(insomniaSong), 5f); bye = Ease(bye, On(byeSong), 4f); retro = Ease(retro, On(retroSong), 3f); bloom = Ease(bloom, On(bloomSong), 4f); crab = Ease(crab, On(crabSong), 2f); night = Ease(night, On(nightSong), 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            var touched = t - played < 8f ? Mathf.Sin(Mathf.Clamp01((t - played) / 8f) * Mathf.PI) : 0f;
            lift = Mathf.MoveTowards(lift, Mathf.Clamp01(energy * .6f + touched), dt * .5f);
            foreach (var (tr, home, ph) in floaters) if (tr != null) tr.position = home + Vector3.up * (Mathf.Sin(t * .3f + ph) * 1.2f + lift * 3f) + new Vector3(Mathf.Sin(t * .07f + ph), 0, Mathf.Cos(t * .05f + ph)) * 1.5f;
            for (var i = 0; i < spinners.Count; i++) { var (tr, home) = spinners[i]; if (tr == null) continue; tr.position = home + Vector3.up * Mathf.Sin(t * .8f + i) * .4f; tr.Rotate(0f, dt * (20f + energy * 60f), 0f, Space.World); }
            for (var i = 0; i < lanterns.Count; i++) { var (tr, home, ph) = lanterns[i]; if (tr == null) continue; var k = Mathf.Repeat(t / 14f + ph * .2f, 1f); var high = 3f + bye * 30f; tr.position = home + Vector3.up * (Mathf.Sin(k * Mathf.PI) * high) + new Vector3(Mathf.Sin(t * .3f + ph), 0, 0) * k * 2f; }
            for (var i = 0; i < crabs.Count; i++) { var (tr, home) = crabs[i]; if (tr == null) continue; var dance = .1f + crab * .9f; tr.position = home + Vector3.up * Mathf.Abs(Mathf.Sin(now * 4f + i)) * .2f * dance + new Vector3(Mathf.Sin(now * 3f + i), 0, 0) * .2f * dance; tr.rotation = Quaternion.Euler(0, (i * 60f) + Mathf.Sin(now * 4f + i) * 30f * dance, Mathf.Sin(now * 8f + i) * 10f * dance); }

            if (runes != null) runes.SetColor("_EmissionColor", runeBase * (.8f + .3f * Mathf.Sin(t * 1.1f) + energy * .5f + touched * 1.5f + night * .6f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + touched * .4f + bye * .6f));
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.8f + .2f * Mathf.Sin(t * 2f) + fall * 1.2f));
            if (crystals != null) crystals.SetColor("_EmissionColor", crystalBase * (.8f + .3f * Mathf.Sin(t * .9f) + energy * .5f + bloom * .6f));

            var tint = new Color(1f, .74f, .86f); var amount = .08f;
            if (days > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .4f), days); amount = Mathf.Lerp(amount, .36f, days); }
            if (bloom > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .8f), bloom); amount = Mathf.Lerp(amount, .32f, bloom); }
            if (fall > 0f) { tint = Color.Lerp(tint, new Color(.6f, .74f, 1f), fall); amount = Mathf.Lerp(amount, .34f, fall); }
            if (retro > 0f) { tint = Color.Lerp(tint, Mathf.PingPong(t * .6f, 1f) > .5f ? new Color(1f, .3f, .8f) : new Color(.3f, .8f, 1f), retro); amount = Mathf.Lerp(amount, .36f, retro); }
            if (crab > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .2f), crab); amount = Mathf.Lerp(amount, Mathf.Repeat(now * 2.1f, 1f) < .2f ? .5f : .34f, crab); }
            if (night > 0f) { tint = Color.Lerp(tint, new Color(.5f, .3f, .8f), night); amount = Mathf.Lerp(amount, .42f, night); }
            if (insomnia > 0f) { tint = Color.Lerp(tint, new Color(.2f, .26f, .6f), insomnia); amount = Mathf.Lerp(amount, .5f, insomnia); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .2f * energy + fall * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpray)
            {
                nextSpray = t + .08f;
                if (splash != Vector3.zero && (splash - cp).sqrMagnitude < 60f * 60f) dots.Emit(new ParticleSystem.EmitParams { position = splash + new Vector3(Random.Range(-1.4f, 1.4f), 0, Random.Range(-1.4f, 1.4f)), velocity = new Vector3(Random.Range(-.8f, .8f), Random.Range(1f, 2.4f), Random.Range(-.8f, .8f)), startColor = new Color(.8f, .9f, 1f, .6f), startSize = .12f, startLifetime = 1.4f }, 1);
                foreach (var m in mists) if ((m - cp).sqrMagnitude < 40f * 40f) dots.Emit(new ParticleSystem.EmitParams { position = m + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), velocity = new Vector3(0, .3f, 0), startColor = new Color(.7f, .86f, 1f, .4f), startSize = .4f, startLifetime = 3f }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(.5f, 5f), Random.Range(-18f, 18f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .2f + touched * .5f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(.05f, .3f), Random.Range(-.2f, .2f)), startColor = Random.value < .5f ? new Color(1f, .6f, .8f) : new Color(.8f, .6f, 1f), startSize = .07f, startLifetime = 5f }, 1);
                if (bloom > .05f && Random.value < bloom) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(5f, 9f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * .6f, -.6f, wind.y * .6f), startColor = new Color(1f, .6f, .8f), startSize = .14f, startLifetime = 8f }, 1);
                if (insomnia > .05f && Random.value < insomnia) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-60f, 60f), Random.Range(30f, 60f), Random.Range(-60f, 60f)), velocity = Vector3.zero, startColor = new Color(.9f, .9f, 1f), startSize = .5f, startLifetime = 6f }, 1);
                if (fall > .05f && Random.value < fall) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -8f, wind.y), startColor = new Color(.7f, .8f, 1f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
            }
        }
    }
}
