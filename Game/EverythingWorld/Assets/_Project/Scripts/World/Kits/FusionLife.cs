using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Fusion Isthmus at blue hour:
    /// - the strings of lights over the canal and the Rooftop twinkle in turn, a little quicker with the music; the
    ///   brass lamps and lit windows glow warm against the teal dusk; the canal shimmers; moths of light drift round
    ///   the lamps; a few dusky clouds, the first stars;
    /// - "Tidal Wave": the canal brightens and ripples. "Nightrider": the town dims and the lamps burn brighter.
    ///   "Turquoise Galaxy": the sky turns turquoise and full of stars. "Victory Dance": confetti over Ezra
    ///   Collective's party. "Strings of Light": every string of bulbs in the town blazes.
    ///   Every song's own light brightens while it plays. The dominoes fall when pushed, then stand again.
    public class FusionLife : MonoBehaviour
    {
        const string Land = "The Fusion Isthmus";
        public Vector3 center, victoryAt;
        public string tidalSong, nightSong, galaxySong, victorySong, stringsSong;
        public Material lamps, windows, water; public Material[] bulbs;
        public float played = -99f, dominoAt = -99f;
        public List<Transform> dominoes = new List<Transform>(); public List<Quaternion> dominoRot = new List<Quaternion>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem moths, confetti;
        Color lampBase, windowBase, waterBase, bulbBase;
        float tidal, night, galaxy, victory, strings, nextMoth, nextConfetti;
        static readonly Color[] Party = { FusionKit.Brass, FusionKit.Teal, new Color(1f, .5f, .4f), FusionKit.Indigo, new Color(.95f, .5f, .75f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 36; clouds.span = 2600f; clouds.key = "fusion";
            clouds.minHeight = 150f; clouds.maxHeight = 230f; clouds.size = 3f; clouds.speed = 1.8f; clouds.cover = .32f;
            clouds.color = new Color(.78f, .56f, .6f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); windowBase = E(windows); waterBase = E(water); bulbBase = bulbs != null && bulbs.Length > 0 ? E(bulbs[0]) : Color.black;
            moths = Particles("Moths of light", 400, new Color(1f, .9f, .65f, .8f), .03f, .07f);
            var mm = moths.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f);
            confetti = Particles("Confetti", 900, Color.white, .08f, .14f);
            var cm = confetti.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); cm.gravityModifier = .2f;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
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

            tidal = Mathf.MoveTowards(tidal, On(tidalSong) ? 1f : 0f, dt / 3f);
            night = Mathf.MoveTowards(night, On(nightSong) ? 1f : 0f, dt / 5f);
            galaxy = Mathf.MoveTowards(galaxy, On(galaxySong) ? 1f : 0f, dt / 4f);
            victory = Mathf.MoveTowards(victory, On(victorySong) ? 1f : 0f, dt / 2f);
            strings = Mathf.MoveTowards(strings, On(stringsSong) ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the bulbs twinkle in turn, easily; they lift when somebody plays, and blaze for Strings of Light
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var rate = .6f + energy * .8f + lift * 1.5f;
            if (bulbs != null) for (var i = 0; i < bulbs.Length; i++) if (bulbs[i] != null) { var ph = .5f + .5f * Mathf.Sin(now * rate * 2f + i * 2.1f); bulbs[i].SetColor("_EmissionColor", bulbBase * (.6f + .7f * ph + lift * .6f + strings * 2.2f + night * .5f)); }
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + energy * .2f + night * 1.4f + .03f * Mathf.Sin(t * 1.7f)));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + .04f * Mathf.Sin(t * 1.1f) + night * .6f));
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.85f + .15f * Mathf.Sin(t * .9f) + tidal * (1.4f + .7f * Mathf.Sin(t * 3f)) + galaxy * .6f));

            // the sky: teal dusk; darker for Nightrider, turquoise for the Galaxy, a warm lift for Victory Dance
            var tint = new Color(.2f, .42f, .5f); var amount = .06f;
            if (tidal > 0f) { tint = Color.Lerp(tint, new Color(.15f, .7f, .75f), tidal); amount = Mathf.Lerp(amount, .28f, tidal); }
            if (galaxy > 0f) { tint = Color.Lerp(tint, new Color(.1f, .85f, .8f), galaxy); amount = Mathf.Lerp(amount, .45f, galaxy); }
            if (victory > 0f) { tint = Color.Lerp(tint, new Color(1f, .7f, .4f), victory); amount = Mathf.Lerp(amount, .22f, victory); }
            if (night > 0f) { tint = Color.Lerp(tint, new Color(.03f, .05f, .12f), night); amount = Mathf.Lerp(amount, .7f, night); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .15f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main;
            if (here && cam != null && t >= nextMoth)
            {
                nextMoth = t + .14f;
                var p = cam.transform.position + new Vector3(Random.Range(-12f, 12f), Random.Range(0f, 4.5f), Random.Range(-12f, 12f));
                moths.Emit(new ParticleSystem.EmitParams { position = p, velocity = Random.insideUnitSphere * .3f }, 1);
            }
            if (victory > 0f && victoryAt != Vector3.zero && t >= nextConfetti)
            {
                nextConfetti = t + .12f;
                for (var k = 0; k < 6; k++) confetti.Emit(new ParticleSystem.EmitParams { position = victoryAt + new Vector3(Random.Range(-7f, 7f), Random.Range(2f, 5f), Random.Range(-7f, 7f)), velocity = new Vector3(wind.x, -.6f, wind.y), startColor = Party[Random.Range(0, Party.Length)] }, 1);
            }
            // the dominoes: each tips a moment after the one before, lies a while, then they stand again together
            var da = t - dominoAt;
            for (var i = 0; i < dominoes.Count; i++)
            {
                if (dominoes[i] == null) continue; var tip = 0f;
                if (da < 8f) tip = Mathf.Clamp01((da - i * .13f) * 5f); else if (da < 9.5f) tip = 1f - Mathf.Clamp01((da - 8f) / 1.5f);
                dominoes[i].rotation = dominoRot[i] * Quaternion.Euler(tip * 62f, 0, 0);
            }
        }
    }
}
