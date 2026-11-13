using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Trance Steppe at sunrise, on a 138 bpm trance clock (shared, so friends see the same thing):
    /// - every laser on the plain sweeps up into the dawn, fanning on the build and snapping together on the drop, every
    ///   32 beats; the giant sun swells slowly and its halo breathes; the Monolith's beam pulses; motes of light rise from
    ///   the grass; hands up at the stage brings every beam together over the sun;
    /// - "Sandstorm": an orange haze, wind and blowing sand. "Children": a dreamy pastel dawn. "Sun & Moon": the light goes
    ///   gold, then cool blue, then gold. "Kernkraft 400": green flashes on every beat. "Omen In The Rain": soft rain.
    ///   "Hate or Glory": a dark red. Every song's own light brightens while it plays.
    public class TranceLife : MonoBehaviour
    {
        const string Land = "The Trance Steppe";
        public Vector3 center, fwd = Vector3.forward, sunHome;
        public string sunMoonSong, sandSong, gloryHateSong, rainSong, childrenSong, kernSong;
        public Material lasers, beams, lamps, sun; public float played = -99f;
        public List<(Transform t, float ph)> sweepers = new List<(Transform, float)>();
        public Transform monolith, sunT;
        public List<Transform> hearts = new List<Transform>(), twisters = new List<Transform>();
        public List<(Transform t, Vector3 home)> spinners = new List<(Transform, Vector3)>();
        public List<Vector3> rainSpots = new List<Vector3>(), steam = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color laserBase, beamBase, lampBase, sunBase;
        float sunMoon, sand, hate, rain, children, kern, nextFall, nextSteam;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2600f; clouds.key = "trance";
            clouds.minHeight = 130f; clouds.maxHeight = 220f; clouds.size = 3.6f; clouds.speed = .8f; clouds.cover = .35f;
            clouds.color = new Color(1f, .7f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            laserBase = E(lasers); beamBase = E(beams); lampBase = E(lamps); sunBase = E(sun);
            dots = Particles("Motes, sand and rain", 1400, Color.white, .05f, .16f, true);
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

            sunMoon = Ease(sunMoon, On(sunMoonSong), 4f); sand = Ease(sand, On(sandSong), 3f); hate = Ease(hate, On(gloryHateSong), 3f); rain = Ease(rain, On(rainSong), 4f); children = Ease(children, On(childrenSong), 5f); kern = Ease(kern, On(kernSong), 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the trance clock: 138 bpm; a 32-beat phrase builds, then drops
            var beatT = now * 138f / 60f; var beat = Mathf.Repeat(beatT, 1f); var phrase = Mathf.Repeat(beatT / 32f, 1f); var build = Mathf.Clamp01(phrase / .75f); var dropped = phrase > .75f;
            var kick = Mathf.Exp(-beat * 6f); var hands = t - played < 6f ? 1f - (t - played) / 6f : 0f;
            var target = sunHome + Vector3.up * 120f;
            foreach (var (tr, ph) in sweepers)
            {
                if (tr == null) continue; var basePos = tr.position;
                var fan = Quaternion.Euler(-60f + Mathf.Sin(now * (.4f + build * .8f) + ph) * (25f + build * 20f), ph * 57f + now * (10f + build * 30f), 0) * Vector3.forward;
                var toSun = (target - basePos).normalized; var converge = Mathf.Max(hands, dropped ? (Mathf.Repeat(beatT, 2f) < 1f ? .8f : 0f) : 0f);
                var dir = Vector3.Slerp(new Vector3(fan.x, Mathf.Abs(fan.y) + .6f, fan.z).normalized, toSun, converge);
                tr.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            }
            if (monolith != null) monolith.localScale = new Vector3(1f + kick * .3f, 1f, 1f + kick * .3f);
            if (sunT != null) sunT.localScale = Vector3.one * (1f + Mathf.Sin(now * .05f) * .04f + hands * .06f);
            foreach (var h in hearts) if (h != null) h.localScale = Vector3.one * (1f + kick * .15f);
            foreach (var tw in twisters) if (tw != null) tw.Rotate(0, dt * (90f + sand * 360f), 0, Space.World);
            for (var i = 0; i < spinners.Count; i++) { var (tr, home) = spinners[i]; if (tr == null) continue; tr.position = home + Vector3.up * Mathf.Sin(t * .7f + i) * .2f; tr.Rotate(dt * 20f, dt * 30f, 0, Space.Self); }

            if (lasers != null) lasers.SetColor("_EmissionColor", (kern > .1f && beat < .12f ? new Color(.4f, 1f, .4f) * 3f : laserBase) * (.6f + build * .6f + (dropped ? kick * .8f : 0f) + hands));
            if (beams != null) beams.SetColor("_EmissionColor", beamBase * (.5f + build * .5f + (dropped ? kick : 0f) + hands * 1.5f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + hands * .5f + children * .3f));
            if (sun != null) sun.SetColor("_EmissionColor", sunBase * (1f + Mathf.Sin(now * .1f) * .1f + hands * .5f + sunMoon * Mathf.Max(0f, Mathf.Sin(now * .3f)) * .6f));

            var tint = new Color(1f, .72f, .6f); var amount = .12f + (dropped ? kick * .05f : 0f);
            if (children > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(1f, .8f, .9f), new Color(.8f, .86f, 1f), Mathf.PingPong(now * .05f, 1f)), children); amount = Mathf.Lerp(amount, .3f, children); }
            if (sunMoon > 0f) { tint = Color.Lerp(tint, Mathf.Sin(now * .3f) > 0f ? new Color(1f, .8f, .4f) : new Color(.5f, .6f, 1f), sunMoon); amount = Mathf.Lerp(amount, .34f, sunMoon); }
            if (rain > 0f) { tint = Color.Lerp(tint, new Color(.6f, .64f, .74f), rain); amount = Mathf.Lerp(amount, .38f, rain); }
            if (sand > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .3f), sand); amount = Mathf.Lerp(amount, .44f, sand); }
            if (hate > 0f) { tint = Color.Lerp(tint, new Color(.6f, .1f, .1f), hate); amount = Mathf.Lerp(amount, .44f, hate); }
            if (kern > 0f) { tint = Color.Lerp(tint, beat < .12f ? new Color(.4f, 1f, .4f) : tint, kern); amount = Mathf.Lerp(amount, beat < .12f ? .5f : .2f, kern); }
            if (hands > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .7f), hands * .6f); amount = Mathf.Lerp(amount, .4f, hands); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .35f + .15f * energy + sand * 1.2f + rain * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSteam)
            {
                nextSteam = t + .15f;
                foreach (var s in steam) if ((s - cp).sqrMagnitude < 60f * 60f) dots.Emit(new ParticleSystem.EmitParams { position = s, velocity = new Vector3(wind.x * .4f, 1f, wind.y * .4f), startColor = new Color(.9f, .9f, .9f, .25f), startSize = 1.4f, startLifetime = 5f }, 1);
                foreach (var r in rainSpots) if ((r - cp).sqrMagnitude < 30f * 30f) dots.Emit(new ParticleSystem.EmitParams { position = r + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), velocity = Vector3.down * 5f, startColor = new Color(.7f, .76f, .9f, .6f), startSize = .04f, startLifetime = .6f }, 2);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(.2f, 1.6f), Random.Range(-18f, 18f));
                if (Random.value < .25f + hands * .5f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(0, Random.Range(.3f, .8f) + hands * 2f, 0), startColor = Random.value < .5f ? new Color(1f, .86f, .6f) : new Color(.6f, 1f, .7f), startSize = .06f, startLifetime = 5f }, 1);
                if (sand > .05f && Random.value < sand) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(.3f, 4f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * 6f, Random.Range(-.2f, .4f), wind.y * 6f), startColor = new Color(.9f, .7f, .5f, .5f), startSize = .1f, startLifetime = 2f }, 1);
                if (rain > .05f && Random.value < rain) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -9f, wind.y), startColor = new Color(.7f, .76f, .9f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
            }
        }
    }
}
