using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Glitterlands at dusk:
    /// - the Mirrorball turns and throws specks of light; every dance floor's tiles pulse to a four-on-the-floor beat
    ///   and shift colour; glitter drifts down everywhere; the crystal arches shimmer; confetti over the Sky Club when
    ///   anybody plays;
    /// - "Run Away With Me": a pink running sunset. "Dragostea din tei" and "Caramelldansen": the floors flash fast in
    ///   every colour. "Happy Nation": gold. "What Is Love": slow red pulses. "Red Wine Supernova": red-violet with a
    ///   burst of stars. "Supercut": the light flickers like film. "Mercurial World": everything goes silver. "Good Time":
    ///   confetti everywhere. "Image": electric blue. Every song's own light brightens while it plays.
    public class GlitLife : MonoBehaviour
    {
        const string Land = "The Glitterlands";
        public Vector3 center, sparkleAt, clubAt;
        public string runSong, dragSong, caraSong, happySong, loveSong, wineSong, cutSong, mercSong, goodSong, imageSong;
        public Material mirror, lamps, crystal; public Material[] tiles; public Transform ball; public float played = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots, specks;
        Color mirrorBase, lampBase, crystalBase; Color[] tileBase;
        float run, fast, happy, love, wine, cut, merc, good, image, nextFall, nextSpeck;
        static readonly Color[] Confetti = { new Color(1f, .5f, .8f), new Color(.7f, .6f, 1f), new Color(.5f, .9f, 1f), new Color(1f, .86f, .4f), Color.white };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 50; clouds.span = 2400f; clouds.key = "glitterlands";
            clouds.minHeight = 90f; clouds.maxHeight = 200f; clouds.size = 3.4f; clouds.speed = 1.4f; clouds.cover = .5f;
            clouds.color = new Color(1f, .82f, .94f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            mirrorBase = E(mirror); lampBase = E(lamps); crystalBase = E(crystal); tileBase = new Color[tiles != null ? tiles.Length : 0]; for (var i = 0; i < tileBase.Length; i++) tileBase[i] = E(tiles[i]);
            dots = Particles("Glitter and confetti", 1500, Color.white, .05f, .1f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            specks = Particles("Mirrorball specks", 600, Color.white, .25f, .45f, true);
            var sm = specks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.2f);
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

            run = Ease(run, On(runSong), 5f); fast = Ease(fast, On(dragSong) || On(caraSong), 2f); happy = Ease(happy, On(happySong), 4f); love = Ease(love, On(loveSong), 3f); wine = Ease(wine, On(wineSong), 4f); cut = Ease(cut, On(cutSong), 3f); merc = Ease(merc, On(mercSong), 5f); good = Ease(good, On(goodSong), 2f); image = Ease(image, On(imageSong), 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 3f) * .6f)));

            // the ball turns; the tiles pulse four to the floor and rotate their colours; the mirrors shimmer
            if (ball != null) ball.Rotate(0f, dt * 9f * (1f + fast * 2f), 0f, Space.World);
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var bpm = 2f * (1f + fast); var beat = Mathf.Repeat(now * bpm, 1f); var kick = beat < .15f ? 1f - beat / .15f : 0f;
            if (tiles != null) for (var i = 0; i < tiles.Length; i++) if (tiles[i] != null) { var shift = Mathf.FloorToInt(now * bpm / 4f + i) % tileBase.Length; var col = Color.Lerp(tileBase[i], tileBase[shift], .5f + .5f * fast); tiles[i].SetColor("_EmissionColor", col * (.5f + kick * (.8f + energy * .6f + lift + fast)) * (1f + love * (Mathf.Repeat(now, 2f) < .3f ? 1.2f : -.4f))); }
            if (mirror != null) mirror.SetColor("_EmissionColor", mirrorBase * (.8f + .4f * Mathf.PerlinNoise(t * 3f, 1f) + merc * 1.4f));
            if (crystal != null) crystal.SetColor("_EmissionColor", crystalBase * (.8f + .3f * Mathf.Sin(t * 1.3f) + wine * .8f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + kick * .3f + (cut > 0f && Mathf.PerlinNoise(t * 12f, 2f) > .6f ? -cut * .8f : 0f)));

            var tint = new Color(1f, .9f, 1f); var amount = .04f;
            if (happy > 0f) { tint = Color.Lerp(tint, new Color(1f, .84f, .36f), happy); amount = Mathf.Lerp(amount, .34f, happy); }
            if (run > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .6f), run); amount = Mathf.Lerp(amount, .36f, run); }
            if (fast > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * bpm / 2f, 1f), .6f, 1f), fast); amount = Mathf.Lerp(amount, .3f, fast); }
            if (image > 0f) { tint = Color.Lerp(tint, new Color(.3f, .6f, 1f), image); amount = Mathf.Lerp(amount, .4f, image); }
            if (merc > 0f) { tint = Color.Lerp(tint, new Color(.86f, .88f, .96f), merc); amount = Mathf.Lerp(amount, .5f, merc); }
            if (love > 0f) { tint = Color.Lerp(tint, new Color(.9f, .2f, .3f), love); amount = Mathf.Lerp(amount, .2f + (Mathf.Repeat(now, 2f) < .3f ? .25f : 0f), love); }
            if (wine > 0f) { tint = Color.Lerp(tint, new Color(.7f, .16f, .4f), wine); amount = Mathf.Lerp(amount, .44f, wine); }
            if (cut > 0f) { tint = Color.Lerp(tint, new Color(.9f, .86f, .8f), cut); amount = Mathf.Lerp(amount, .2f + (Mathf.PerlinNoise(t * 12f, 2f) > .6f ? .3f : 0f), cut); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .2f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            // the Mirrorball throws specks of light over the floor
            if (t >= nextSpeck && (sparkleAt - cp).sqrMagnitude < 80f * 80f) { nextSpeck = t + .03f; var dir = Random.onUnitSphere; dir.y = -Mathf.Abs(dir.y) - .3f; specks.Emit(new ParticleSystem.EmitParams { position = sparkleAt + dir.normalized * 5.4f + dir.normalized * Random.Range(4f, 14f), velocity = Vector3.zero, startColor = Confetti[Random.Range(0, Confetti.Length)] }, 1); }
            if (t >= nextFall)
            {
                nextFall = t + .05f; var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(3f, 8f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .5f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.3f, .3f), -.5f, Random.Range(-.3f, .3f)), startColor = Confetti[Random.Range(0, Confetti.Length)], startSize = .04f }, 1);
                var conf = Mathf.Max(good, lift * ((clubAt - cp).sqrMagnitude < 50f * 50f ? 1f : .3f));
                if (Random.value < conf) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(4f, 9f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.5f, .5f), -1f, Random.Range(-.5f, .5f)), startColor = Confetti[Random.Range(0, Confetti.Length)], startSize = .12f }, 1);
                if (wine > .3f && Random.value < .05f) { var b = cp + new Vector3(Random.Range(-40f, 40f), Random.Range(30f, 50f), Random.Range(-40f, 40f)); for (var k = 0; k < 60; k++) dots.Emit(new ParticleSystem.EmitParams { position = b, velocity = Random.onUnitSphere * Random.Range(6f, 10f), startColor = new Color(1f, .4f, .6f), startSize = .4f, startLifetime = 1.6f }, 1); }
            }
        }
    }
}
