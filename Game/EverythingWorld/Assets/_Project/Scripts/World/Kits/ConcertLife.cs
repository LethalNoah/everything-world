using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Concert Isles by moonlight:
    /// - candles flicker in every lantern and chandelier; the fountains sparkle; fireflies and pale moths drift over the lawns;
    ///   the organ's pipes glow when it's played; the moon breathes;
    /// - "Moonlight Sonata": the moon swells and everything goes silver. "Clair de Lune": silver-blue. "Hallelujah": a warm
    ///   gold light from above. "Mars, the Bringer of War": red, pulsing like drums. "Toccata and Fugue": deep purple and
    ///   the organ blazes. "Morning Mood": a pink-gold dawn. "Spring": blossom petals. "William Tell": flashing gallop.
    ///   "The Rite of Spring": firelight. "Lacrimosa": soft rain. The Fifth: lightning on the opening notes. "Ave Maria": the
    ///   candles burn brighter. "Daisy Bell": a soft computer green. Every song's own light brightens while it plays.
    public class ConcertLife : MonoBehaviour
    {
        const string Land = "The Concert Isles";
        public Vector3 center;
        public string springSong, clairSong, morningSong, aveSong, hallSong, marsSong, riteSong, toccataSong, tellSong, moonSong, fifthSong, daisySong, lacrimosaSong;
        public Material candles, water, windows, moon, organ; public float played = -99f;
        public List<Vector3> fountains = new List<Vector3>();
        public List<Vector3> fires = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color candleBase, waterBase, windowBase, moonBase, organBase;
        float spring, clair, morning, ave, hall, mars, rite, toccata, tell, moonS, fifth, daisy, lacri, nextFall, nextSpark, flash;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 20; clouds.span = 2400f; clouds.key = "concert";
            clouds.minHeight = 140f; clouds.maxHeight = 230f; clouds.size = 3f; clouds.speed = .7f; clouds.cover = .25f;
            clouds.color = new Color(.56f, .66f, .76f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            candleBase = E(candles); waterBase = E(water); windowBase = E(windows); moonBase = E(moon); organBase = E(organ);
            dots = Particles("Fireflies, petals and rain", 1400, Color.white, .05f, .16f, true);
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

            spring = Ease(spring, On(springSong), 4f); clair = Ease(clair, On(clairSong), 5f); morning = Ease(morning, On(morningSong), 6f); ave = Ease(ave, On(aveSong), 4f); hall = Ease(hall, On(hallSong), 3f); mars = Ease(mars, On(marsSong), 3f); rite = Ease(rite, On(riteSong), 3f); toccata = Ease(toccata, On(toccataSong), 3f); tell = Ease(tell, On(tellSong), 2f); moonS = Ease(moonS, On(moonSong), 5f); fifth = Ease(fifth, On(fifthSong), 2f); daisy = Ease(daisy, On(daisySong), 4f); lacri = Ease(lacri, On(lacrimosaSong), 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            var lift = t - played < 6f ? 1f - (t - played) / 6f : 0f; var beat = Mathf.Repeat(now * 1.6f, 1f);
            if (candles != null) candles.SetColor("_EmissionColor", candleBase * (.85f + .15f * Mathf.PerlinNoise(t * 3f, 0f) + ave * .8f + lift * .3f));
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.8f + .2f * Mathf.Sin(t * 1.2f) + clair * .8f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + hall * .5f));
            if (moon != null) moon.SetColor("_EmissionColor", moonBase * (.9f + .1f * Mathf.Sin(t * .3f) + moonS * 1.2f + clair * .5f));
            if (organ != null) organ.SetColor("_EmissionColor", organBase * (1f + lift * 2.4f + toccata * 2f + hall * .8f));

            if ((fifth > .3f && Random.value < dt * .6f * fifth) || (tell > .3f && beat < .05f)) flash = 1f; flash = Mathf.MoveTowards(flash, 0f, dt * 4f);

            var tint = new Color(.76f, .9f, 1f); var amount = .06f;
            if (daisy > 0f) { tint = Color.Lerp(tint, new Color(.5f, 1f, .6f), daisy); amount = Mathf.Lerp(amount, .26f, daisy); }
            if (spring > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .9f), spring); amount = Mathf.Lerp(amount, .3f, spring); }
            if (morning > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .6f), morning); amount = Mathf.Lerp(amount, .4f, morning); }
            if (ave > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .5f), ave); amount = Mathf.Lerp(amount, .3f, ave); }
            if (hall > 0f) { tint = Color.Lerp(tint, new Color(1f, .86f, .5f), hall); amount = Mathf.Lerp(amount, .42f, hall); }
            if (clair > 0f) { tint = Color.Lerp(tint, new Color(.6f, .76f, 1f), clair); amount = Mathf.Lerp(amount, .36f, clair); }
            if (moonS > 0f) { tint = Color.Lerp(tint, new Color(.84f, .9f, 1f), moonS); amount = Mathf.Lerp(amount, .38f, moonS); }
            if (lacri > 0f) { tint = Color.Lerp(tint, new Color(.5f, .56f, .66f), lacri); amount = Mathf.Lerp(amount, .4f, lacri); }
            if (rite > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .2f), rite); amount = Mathf.Lerp(amount, .36f + Mathf.PerlinNoise(t * 4f, 1f) * .1f, rite); }
            if (toccata > 0f) { tint = Color.Lerp(tint, new Color(.5f, .3f, .8f), toccata); amount = Mathf.Lerp(amount, .44f, toccata); }
            if (mars > 0f) { tint = Color.Lerp(tint, new Color(1f, .2f, .14f), mars); amount = Mathf.Lerp(amount, beat < .2f ? .52f : .36f, mars); }
            if (tell > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .6f), tell); amount = Mathf.Lerp(amount, .3f, tell); }
            if (flash > 0f) { tint = Color.Lerp(tint, Color.white, flash); amount = Mathf.Lerp(amount, .6f, flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .2f + .15f * energy + lacri * .5f + mars * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpark)
            {
                nextSpark = t + .15f;
                foreach (var f in fountains) if ((f - cp).sqrMagnitude < 50f * 50f) dots.Emit(new ParticleSystem.EmitParams { position = f + new Vector3(Random.Range(-.2f, .2f), 0, Random.Range(-.2f, .2f)), velocity = new Vector3(Random.Range(-.6f, .6f), Random.Range(1.4f, 2.2f), Random.Range(-.6f, .6f)), startColor = new Color(.8f, .94f, 1f, .7f), startSize = .06f, startLifetime = 1.2f }, 1);
                foreach (var f in fires) if ((f - cp).sqrMagnitude < 50f * 50f) dots.Emit(new ParticleSystem.EmitParams { position = f, velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(1f, 2f), Random.Range(-.2f, .2f)), startColor = new Color(1f, .6f, .2f), startSize = .06f, startLifetime = 2f }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(.5f, 3f), Random.Range(-18f, 18f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .2f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(-.05f, .15f), Random.Range(-.2f, .2f)), startColor = Random.value < .7f ? new Color(.9f, 1f, .6f) : new Color(.86f, .9f, 1f), startSize = .06f, startLifetime = 5f }, 1);
                if (spring > .05f && Random.value < spring) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(5f, 9f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * .6f, -.6f, wind.y * .6f), startColor = new Color(1f, .8f, .9f), startSize = .12f, startLifetime = 8f }, 1);
                var glitter = Mathf.Max(hall, lift * .5f); if (Random.value < glitter) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(4f, 8f), Random.Range(-14f, 14f)), velocity = new Vector3(0, -.5f, 0), startColor = new Color(1f, .86f, .4f), startSize = .1f, startLifetime = 6f }, 1);
                if (lacri > .05f && Random.value < lacri) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x, -8f, wind.y), startColor = new Color(.7f, .8f, .9f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
            }
        }
    }
}
