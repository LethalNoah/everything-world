using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Trip-Hop Undercity, three in the morning forever:
    /// - steam rises from the vents; water drips from the tunnel roof; the red and blue lights breathe slowly with the
    ///   music; signal lights change; the station lamps buzz and flicker now and then; the glass teardrop sways; a pendulum
    ///   swings; moths circle a lamp; the map wall in the control room pulses, and the red button sets off a red alarm;
    /// - "Glory Box": a warm amber spotlight glow. "Roads": rain falls and everything goes blue-grey. "Teardrop": deep blue.
    ///   "Angel": a cold white light. "Eyes on Fire": orange embers. "Heat Waves": a hot orange haze. "Porcelain": pale
    ///   soft white. "Clubbed to Death": green, with rain. Every song's own light brightens while it plays.
    public class UnderLife : MonoBehaviour
    {
        const string Land = "The Trip-Hop Undercity";
        public Vector3 center;
        public string glorySong, roadsSong, tearSong, angelSong, eyesSong, heatSong, porcelainSong, clubSong;
        public Material lamps, reds, colds, screens, water, map; public float played = -99f, alarm = -99f;
        public List<Vector3> vents = new List<Vector3>(), signals = new List<Vector3>(), drips = new List<Vector3>(), rainSpots = new List<Vector3>();
        public List<(Transform t, Vector3 home)> swings = new List<(Transform, Vector3)>();
        public List<(Transform t, Quaternion rot)> pendulums = new List<(Transform, Quaternion)>();
        public List<(Transform t, Vector3 home, float ph)> moths = new List<(Transform, Vector3, float)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, redBase, coldBase, screenBase, waterBase, mapBase;
        float glory, roads, tear, angel, eyes, heat, porcelain, club, nextFall, nextSteam;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 10; clouds.span = 2400f; clouds.key = "undercity";
            clouds.minHeight = 160f; clouds.maxHeight = 240f; clouds.size = 3f; clouds.speed = .4f; clouds.cover = .1f;
            clouds.color = new Color(.12f, .12f, .16f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); redBase = E(reds); coldBase = E(colds); screenBase = E(screens); waterBase = E(water); mapBase = E(map);
            dots = Particles("Steam, drips and rain", 1400, Color.white, .06f, .2f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 6f);
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

            glory = Ease(glory, On(glorySong), 4f); roads = Ease(roads, On(roadsSong), 5f); tear = Ease(tear, On(tearSong), 5f); angel = Ease(angel, On(angelSong), 4f); eyes = Ease(eyes, On(eyesSong), 3f); heat = Ease(heat, On(heatSong), 4f); porcelain = Ease(porcelain, On(porcelainSong), 5f); club = Ease(club, On(clubSong), 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // a slow trip-hop pulse, about 80 bpm
            var beat = Mathf.Repeat(now * 80f / 60f, 1f); var pulse = Mathf.Exp(-beat * 4f);
            var al = t - alarm < 6f ? 1f - (t - alarm) / 6f : 0f; var alFlash = al > 0f && Mathf.Repeat(t * 2f, 1f) < .5f;
            var buzz = Mathf.PerlinNoise(t * 9f, 3f) > .93f ? .4f : 1f;
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * buzz * (1f + glory * .6f + heat * .4f));
            if (reds != null) reds.SetColor("_EmissionColor", redBase * (.6f + pulse * .5f + (alFlash ? 2f : 0f) + eyes * .6f));
            if (colds != null) colds.SetColor("_EmissionColor", coldBase * (.7f + (1f - pulse) * .3f + tear * .8f + angel));
            if (screens != null) screens.SetColor("_EmissionColor", (alFlash ? new Color(1f, .2f, .2f) * 1.4f : screenBase) * (Mathf.PerlinNoise(t * 5f, 1f) > .85f ? .6f : 1f));
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.8f + .2f * Mathf.Sin(t * .7f) + tear * .6f));
            if (map != null) map.SetColor("_EmissionColor", mapBase * (.7f + pulse * .4f + al * 2f));

            foreach (var (tr, home) in swings) if (tr != null) { tr.position = home; tr.rotation = Quaternion.Euler(Mathf.Sin(t * .5f) * 4f, t * 6f, Mathf.Sin(t * .37f) * 3f); }
            foreach (var (tr, rot) in pendulums) if (tr != null) tr.rotation = rot * Quaternion.Euler(0, 0, Mathf.Sin(now * 1.1f) * 30f);
            foreach (var (tr, home, ph) in moths) if (tr != null) { var a = t * 2.4f + ph; tr.position = home + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 1.7f) * .3f, Mathf.Sin(a)) * .5f; tr.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 30f + ph) * 40f); }

            var tint = new Color(.36f, .44f, .7f); var amount = .12f + pulse * .03f;
            if (glory > 0f) { tint = Color.Lerp(tint, new Color(1f, .7f, .4f), glory); amount = Mathf.Lerp(amount, .34f, glory); }
            if (heat > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .3f), heat); amount = Mathf.Lerp(amount, .38f, heat); }
            if (eyes > 0f) { tint = Color.Lerp(tint, new Color(1f, .4f, .1f), eyes); amount = Mathf.Lerp(amount, .34f, eyes); }
            if (porcelain > 0f) { tint = Color.Lerp(tint, new Color(.86f, .88f, .92f), porcelain); amount = Mathf.Lerp(amount, .32f, porcelain); }
            if (roads > 0f) { tint = Color.Lerp(tint, new Color(.44f, .5f, .6f), roads); amount = Mathf.Lerp(amount, .44f, roads); }
            if (tear > 0f) { tint = Color.Lerp(tint, new Color(.2f, .34f, .8f), tear); amount = Mathf.Lerp(amount, .44f, tear); }
            if (angel > 0f) { tint = Color.Lerp(tint, new Color(.8f, .86f, 1f), angel); amount = Mathf.Lerp(amount, .38f, angel); }
            if (club > 0f) { tint = Color.Lerp(tint, new Color(.3f, 1f, .5f), club); amount = Mathf.Lerp(amount, .38f, club); }
            if (al > 0f) { tint = Color.Lerp(tint, new Color(1f, .14f, .1f), al * (alFlash ? 1f : .5f)); amount = Mathf.Lerp(amount, .5f, al); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .1f + .1f * energy + roads * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSteam)
            {
                nextSteam = t + .1f;
                foreach (var v in vents) if ((v - cp).sqrMagnitude < 45f * 45f) dots.Emit(new ParticleSystem.EmitParams { position = v + new Vector3(Random.Range(-.4f, .4f), 0, Random.Range(-.4f, .4f)), velocity = new Vector3(wind.x * .3f, Random.Range(.6f, 1.2f), wind.y * .3f), startColor = new Color(.7f, .72f, .78f, .25f), startSize = Random.Range(.8f, 1.6f), startLifetime = 4f }, 1);
                foreach (var dr in drips) if ((dr - cp).sqrMagnitude < 40f * 40f && Random.value < .3f) dots.Emit(new ParticleSystem.EmitParams { position = dr + Vector3.up * 5f + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f)), velocity = Vector3.down * 6f, startColor = new Color(.6f, .7f, .9f, .6f), startSize = .04f, startLifetime = .9f }, 1);
                foreach (var rs in rainSpots) if ((rs - cp).sqrMagnitude < 30f * 30f) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = rs + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)), velocity = Vector3.down * 6f, startColor = new Color(.6f, .7f, .9f, .5f), startSize = .04f, startLifetime = .7f }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                if (IndoorSpaces.Contains(cp + Vector3.up * 3f)) return;
                var rain = Mathf.Max(roads, club * .7f); if (rain > .05f && Random.value < rain) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -9f, wind.y), startColor = club > roads ? new Color(.4f, 1f, .5f, .5f) : new Color(.6f, .7f, .9f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
                if (eyes > .05f && Random.value < eyes) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-12f, 12f), Random.Range(0f, 2f), Random.Range(-12f, 12f)), velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(.4f, 1f), Random.Range(-.2f, .2f)), startColor = new Color(1f, .5f, .1f), startSize = .06f, startLifetime = 4f }, 1);
                if (Random.value < .08f) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-12f, 12f), Random.Range(.5f, 3f), Random.Range(-12f, 12f)), velocity = new Vector3(Random.Range(-.1f, .1f), .05f, Random.Range(-.1f, .1f)), startColor = new Color(.8f, .82f, .9f, .3f), startSize = .05f, startLifetime = 5f }, 1);
            }
        }
    }
}
