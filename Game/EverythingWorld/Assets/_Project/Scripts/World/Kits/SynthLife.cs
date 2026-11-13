using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Synthwave Coast at night:
    /// - the striped sun breathes on the horizon; the grid pulses away from you to a slow synth beat; the neon hums; a
    ///   pink car cruises up and down the Coast Road with its lights on; stars; now and then a shooting star;
    /// - "Days of Thunder": lightning flickers over the sea and the rain comes. "Vampires": blood red. "Neon Odyssey":
    ///   the neon flares cyan. "Arcade Summer": a hot orange sunset. "Genesee Avenue": a dark red horror glow. "A Moment
    ///   Apart": shooting stars fall everywhere. "Feel It All Around": a warm hazy gold. "RYDEEN": the colours race.
    ///   "Always": soft pink. Every song's own light brightens while it plays.
    public class SynthLife : MonoBehaviour
    {
        const string Land = "The Synthwave Coast";
        public Vector3 center, sunAt, roadA, roadB, roadSide;
        public string thunderSong, vampSong, odysseySong, arcadeSong, geneseeSong, momentSong, feelSong, rydeenSong, alwaysSong;
        public Material grid, pink, cyan, sun, windows; public Transform car; public float played = -99f;
        public List<(Material m, Color c)> sunBands = new List<(Material, Color)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color gridBase, pinkBase, cyanBase, windowBase;
        float thunder, vamp, odyssey, arcade, genesee, moment, feel, rydeen, always, nextStar, nextFall, flash;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 16; clouds.span = 2600f; clouds.key = "synthwave";
            clouds.minHeight = 160f; clouds.maxHeight = 220f; clouds.size = 4f; clouds.speed = 1.2f; clouds.cover = .2f;
            clouds.color = new Color(.5f, .3f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            gridBase = E(grid); pinkBase = E(pink); cyanBase = E(cyan); windowBase = E(windows);
            dots = Particles("Shooting stars", 600, Color.white, .1f, .2f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
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

            thunder = Ease(thunder, On(thunderSong), 4f); vamp = Ease(vamp, On(vampSong), 4f); odyssey = Ease(odyssey, On(odysseySong), 3f); arcade = Ease(arcade, On(arcadeSong), 5f); genesee = Ease(genesee, On(geneseeSong), 4f); moment = Ease(moment, On(momentSong), 3f); feel = Ease(feel, On(feelSong), 5f); rydeen = Ease(rydeen, On(rydeenSong), 2f); always = Ease(always, On(alwaysSong), 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the grid pulses on a slow beat; the neon hums; the sun breathes; the car cruises
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var beat = Mathf.Repeat(now * 1.8f, 1f); var pulse = beat < .2f ? 1f - beat / .2f : 0f;
            if (thunder > .1f && Random.value < dt * .6f * thunder) flash = 1f; flash = Mathf.MoveTowards(flash, 0f, dt * 4f);
            if (grid != null) grid.SetColor("_EmissionColor", Color.Lerp(gridBase, Color.HSVToRGB(Mathf.Repeat(t * .3f, 1f), .8f, 1f) * 1.4f, rydeen) * (.55f + pulse * (.6f + energy * .4f + lift) + odyssey * .6f));
            if (pink != null) pink.SetColor("_EmissionColor", pinkBase * (.9f + .1f * Mathf.Sin(t * 30f) * .2f + lift * .5f + vamp * .6f));
            if (cyan != null) cyan.SetColor("_EmissionColor", cyanBase * (.9f + odyssey * 1.6f + lift * .4f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + always * .5f));
            for (var i = 0; i < sunBands.Count; i++) { var (m, c) = sunBands[i]; if (m != null) m.SetColor("_EmissionColor", c * (.9f + .1f * Mathf.Sin(t * .5f + i * .3f) + arcade * .6f + feel * .4f - genesee * .4f)); }
            if (car != null) { var k = Mathf.PingPong(t / 40f, 1f); var goingOut = Mathf.Repeat(t / 40f, 2f) < 1f; var p = Vector3.Lerp(roadA, roadB, Mathf.SmoothStep(0f, 1f, k)) - roadSide * (goingOut ? 0f : 4.4f); var dir = (roadB - roadA).normalized * (goingOut ? 1f : -1f); dir.y = 0; car.SetPositionAndRotation(p, Quaternion.LookRotation(dir)); }

            SkyDirector.SetSongRain(Land, thunder * .55f);
            var tint = new Color(.9f, .8f, 1f); var amount = .04f;
            if (always > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .84f), always); amount = Mathf.Lerp(amount, .3f, always); }
            if (feel > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .5f), feel); amount = Mathf.Lerp(amount, .34f, feel); }
            if (arcade > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .2f), arcade); amount = Mathf.Lerp(amount, .4f, arcade); }
            if (odyssey > 0f) { tint = Color.Lerp(tint, new Color(.3f, .9f, 1f), odyssey); amount = Mathf.Lerp(amount, .34f, odyssey); }
            if (rydeen > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(t * .4f, 1f), .7f, 1f), rydeen); amount = Mathf.Lerp(amount, .34f, rydeen); }
            if (vamp > 0f) { tint = Color.Lerp(tint, new Color(.8f, .06f, .14f), vamp); amount = Mathf.Lerp(amount, .5f, vamp); }
            if (genesee > 0f) { tint = Color.Lerp(tint, new Color(.4f, .04f, .06f), genesee); amount = Mathf.Lerp(amount, .6f, genesee); }
            if (thunder > 0f) { tint = Color.Lerp(tint, new Color(.4f, .4f, .7f), thunder); amount = Mathf.Lerp(amount, .4f - flash * .3f, thunder); if (flash > 0f) tint = Color.Lerp(tint, Color.white, flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .2f * energy + thunder * .4f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextStar) { nextStar = t + Mathf.Lerp(Random.Range(3f, 8f), .15f, moment); var p = cp + new Vector3(Random.Range(-120f, 120f), Random.Range(60f, 110f), Random.Range(-120f, 120f)); var v = new Vector3(Random.Range(-1f, 1f), -.4f, Random.Range(-1f, 1f)).normalized * 40f; for (var k = 0; k < 8; k++) dots.Emit(new ParticleSystem.EmitParams { position = p - v * k * .01f, velocity = v, startColor = Color.Lerp(Color.white, new Color(1f, .6f, .9f), k / 8f), startSize = 1.2f - k * .12f, startLifetime = 1.2f }, 1); }
        }
    }
}
