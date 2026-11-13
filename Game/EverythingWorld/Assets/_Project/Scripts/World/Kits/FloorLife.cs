using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Four-on-the-Floor Steppe at night:
    /// - everything moves to a four-on-the-floor beat on the shared clock (so friends see it together): the LED wall pulses,
    ///   moving lights sweep, lasers fan back and forth, light towers swing their beams over the plain, glow sticks pulse,
    ///   the Ferris wheel turns with its gondolas hanging level, the cube's faces flash, the wave tiles ripple, balls bounce,
    ///   the heart beats; campfires flicker and spark; on "Drop the beat" the stage goes dark for a beat, then explodes with
    ///   light and confetti;
    /// - "Levels": rainbow strobes. "Wake Me Up": a golden dawn. "Don't You Worry Child": gold confetti. "Contact": white
    ///   flashes. "Snowcone": snow. "Rain": rain. "Ring of Fire": firelight and sparks. "Heartbeat": pink pulses. "Ocean
    ///   Simulator": deep blue waves. Every song's own light brightens while it plays.
    public class FloorLife : MonoBehaviour
    {
        const string Land = "The Four-on-the-Floor Steppe";
        public Vector3 center, stageFwd = Vector3.forward, wheelHub, wheelAxis, wheelPlaneA, wheelPlaneB; public float wheelRadius;
        public string snowSong, rainSong, oceanSong, levelsSong, wakeSong, contactSong, worrySong, heartSong, fireSong;
        public Material lamps, screens, sticks, fires, led, led2, cube, waves, wheelBulbs; public Transform wheel; public Quaternion wheelBase; public float played = -99f;
        public List<(Transform t, float ph)> movers = new List<(Transform, float)>(), lasers = new List<(Transform, float)>(), beams = new List<(Transform, float)>(), gondolas = new List<(Transform, float)>();
        public List<(Transform t, Vector3 home)> spinners = new List<(Transform, Vector3)>();
        public List<(Transform t, Vector3 home, float ph)> tiles = new List<(Transform, Vector3, float)>(), bouncers = new List<(Transform, Vector3, float)>();
        public List<Transform> hearts = new List<Transform>();
        public List<Vector3> fireSpots = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        static readonly Color[] Neon = { new Color(.3f, 1f, .4f), new Color(.2f, .5f, 1f), new Color(1f, .2f, .8f), new Color(1f, .7f, .3f), new Color(.4f, .9f, 1f) };

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, screenBase, stickBase, fireBase, ledBase, led2Base, cubeBase, waveBase, bulbBase;
        float snow, rain, ocean, levels, wake, contact, worry, heart, fire, nextFall, nextSpark;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 16; clouds.span = 2400f; clouds.key = "steppe";
            clouds.minHeight = 150f; clouds.maxHeight = 240f; clouds.size = 3f; clouds.speed = .8f; clouds.cover = .2f;
            clouds.color = new Color(.3f, .32f, .44f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); screenBase = E(screens); stickBase = E(sticks); fireBase = E(fires); ledBase = E(led); led2Base = E(led2); cubeBase = E(cube); waveBase = E(waves); bulbBase = E(wheelBulbs);
            dots = Particles("Confetti, sparks, snow and rain", 1600, Color.white, .06f, .16f, true);
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

            snow = Ease(snow, On(snowSong), 4f); rain = Ease(rain, On(rainSong), 4f); ocean = Ease(ocean, On(oceanSong), 4f); levels = Ease(levels, On(levelsSong), 2f); wake = Ease(wake, On(wakeSong), 5f); contact = Ease(contact, On(contactSong), 2f); worry = Ease(worry, On(worrySong), 3f); heart = Ease(heart, On(heartSong), 2f); fire = Ease(fire, On(fireSong), 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the beat: 126 bpm on the shared clock
            var beat = Mathf.Repeat(now * 2.1f, 1f); var kick = Mathf.Exp(-beat * 6f); var bar = Mathf.FloorToInt(now * 2.1f / 4f);
            var since = t - played; var blackout = since > 0f && since < .45f; var boom = since >= .45f && since < 4f ? 1f - (since - .45f) / 3.55f : 0f;
            var drive = Mathf.Clamp01(.4f + energy * .6f + boom);

            for (var i = 0; i < movers.Count; i++) { var (tr, ph) = movers[i]; if (tr == null) continue; tr.rotation = Quaternion.Euler(60f + Mathf.Sin(now * .9f + ph) * 40f, Mathf.Sin(now * .6f + ph * 2f) * 70f + (i % 2 == 0 ? 180f : 0f), 0); }
            for (var i = 0; i < lasers.Count; i++) { var (tr, ph) = lasers[i]; if (tr == null) continue; tr.gameObject.SetActive(!blackout && (drive > .5f || (bar + i) % 3 == 0)); tr.rotation = Quaternion.LookRotation(Quaternion.Euler(-12f - Mathf.Sin(now * 1.3f + ph) * 10f, Mathf.Sin(now * .8f + ph) * 50f, 0) * stageFwd); }
            for (var i = 0; i < beams.Count; i++) { var (tr, ph) = beams[i]; if (tr == null) continue; tr.rotation = Quaternion.Euler(-70f + Mathf.Sin(now * .5f + ph) * 15f, now * 20f + ph * 60f, 0); }
            for (var i = 0; i < spinners.Count; i++) { var (tr, home) = spinners[i]; if (tr == null) continue; tr.position = home + Vector3.up * Mathf.Sin(t * .7f + i) * .2f; tr.Rotate(0, dt * 40f, 0, Space.World); }
            foreach (var (tr, home, ph) in tiles) if (tr != null) tr.position = home + Vector3.up * (Mathf.Sin(now * 3f - ph) * .15f * (1f + ocean * 2f) + kick * .05f);
            foreach (var (tr, home, ph) in bouncers) if (tr != null) tr.position = home + Vector3.up * (.2f + Mathf.Abs(Mathf.Sin(now * Mathf.PI * 2.1f / 2f + ph)) * 1.4f);
            foreach (var h in hearts) if (h != null) h.localScale = Vector3.one * (1f + kick * (.15f + heart * .3f));
            if (wheel != null) { var ang = now * 4f; wheel.rotation = wheelBase * Quaternion.Euler(ang, 0, 0); var rad = ang * Mathf.Deg2Rad; foreach (var (g, a0) in gondolas) if (g != null) { var a = a0 - rad; g.position = wheelHub + (wheelPlaneA * Mathf.Cos(a) + wheelPlaneB * Mathf.Sin(a)) * wheelRadius; g.rotation = Quaternion.LookRotation(wheelAxis); } }

            var flash = blackout ? 0f : 1f;
            if (led != null) led.SetColor("_EmissionColor", (levels > .1f ? Color.HSVToRGB(Mathf.Repeat(now * .5f, 1f), .8f, 1f) * 1.4f : ledBase) * flash * (.6f + kick * .8f * drive + boom * 2f + contact * (beat < .1f ? 2f : 0f)));
            if (led2 != null) led2.SetColor("_EmissionColor", (levels > .1f ? Color.HSVToRGB(Mathf.Repeat(now * .5f + .5f, 1f), .8f, 1f) * 1.4f : led2Base) * flash * (.6f + (1f - kick) * .6f * drive + boom * 2f));
            if (screens != null) screens.SetColor("_EmissionColor", screenBase * (.8f + kick * .4f));
            if (sticks != null) sticks.SetColor("_EmissionColor", stickBase * (.7f + kick * .6f + boom));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + boom * .4f + wake * .4f));
            if (fires != null) fires.SetColor("_EmissionColor", fireBase * (.85f + .15f * Mathf.PerlinNoise(t * 4f, 2f) + fire * .8f));
            if (cube != null) cube.SetColor("_EmissionColor", cubeBase * (((bar % 4) == (int)(beat * 4f) ? 1.6f : .7f) + boom));
            if (waves != null) waves.SetColor("_EmissionColor", waveBase * (.8f + kick * .4f + ocean * 1f));
            if (wheelBulbs != null) wheelBulbs.SetColor("_EmissionColor", bulbBase * (Mathf.Repeat(now * .7f, 1f) < .5f ? 1.1f : .8f));

            var tint = new Color(.4f, .5f, 1f); var amount = .1f;
            if (wake > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .4f), wake); amount = Mathf.Lerp(amount, .4f, wake); }
            if (worry > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .4f), worry); amount = Mathf.Lerp(amount, .36f, worry); }
            if (ocean > 0f) { tint = Color.Lerp(tint, new Color(.2f, .4f, 1f), ocean); amount = Mathf.Lerp(amount, .42f, ocean); }
            if (rain > 0f) { tint = Color.Lerp(tint, new Color(.5f, .6f, .74f), rain); amount = Mathf.Lerp(amount, .36f, rain); }
            if (snow > 0f) { tint = Color.Lerp(tint, new Color(.8f, .9f, 1f), snow); amount = Mathf.Lerp(amount, .34f, snow); }
            if (fire > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .16f), fire); amount = Mathf.Lerp(amount, .4f + Mathf.PerlinNoise(t * 5f, 1f) * .1f, fire); }
            if (heart > 0f) { tint = Color.Lerp(tint, new Color(1f, .3f, .6f), heart); amount = Mathf.Lerp(amount, .3f + kick * .2f, heart); }
            if (levels > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .5f, 1f), .7f, 1f), levels); amount = Mathf.Lerp(amount, .3f + kick * .2f, levels); }
            if (contact > 0f) { tint = Color.Lerp(tint, Color.white, contact * (beat < .12f ? 1f : .3f)); amount = Mathf.Lerp(amount, beat < .12f ? .55f : .25f, contact); }
            if (boom > 0f) { tint = Color.Lerp(tint, Color.white, boom * .5f); amount = Mathf.Lerp(amount, .5f, boom); }
            if (blackout) amount = .7f;
            SkyDirector.SetSongTint(Land, blackout ? new Color(.05f, .05f, .1f) : tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .2f * energy + kick * .1f + rain * .4f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpark) { nextSpark = t + .15f; foreach (var f in fireSpots) if ((f - cp).sqrMagnitude < 50f * 50f) dots.Emit(new ParticleSystem.EmitParams { position = f + new Vector3(Random.Range(-.3f, .3f), 0, Random.Range(-.3f, .3f)), velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(1f, 2.4f + fire * 2f), Random.Range(-.3f, .3f)), startColor = new Color(1f, .6f, .2f), startSize = .06f, startLifetime = 2f }, 1 + (int)(fire * 3f)); }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(5f, 10f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                var conf = Mathf.Max(boom, worry * .7f); if (Random.value < conf) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(6f, 10f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.6f, .6f), -1.2f, Random.Range(-.6f, .6f)), startColor = worry > .3f ? new Color(1f, .82f, .3f) : Neon[Random.Range(0, Neon.Length)], startSize = .12f, startLifetime = 6f }, 1);
                if (snow > .05f && Random.value < snow) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(5f, 9f), Random.Range(-18f, 18f)), velocity = new Vector3(Random.Range(-.3f, .3f), -1f, Random.Range(-.3f, .3f)), startColor = Color.white, startSize = .1f, startLifetime = 8f }, 1);
                if (rain > .05f && Random.value < rain) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -9f, wind.y), startColor = new Color(.6f, .7f, .9f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
                if (Random.value < .1f) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(.3f, 3f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.2f, .2f), .1f, Random.Range(-.2f, .2f)), startColor = Neon[Random.Range(0, Neon.Length)], startSize = .05f, startLifetime = 4f }, 1);
            }
        }
    }
}
