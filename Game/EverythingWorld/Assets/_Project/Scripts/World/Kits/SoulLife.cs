using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Soul Lowlands at golden hour:
    /// - the strings of bulbs over the square and Main Street chase slowly, quicker with the music; the globe lamps and
    ///   the windows glow warm and lift with the song; gold motes hang in the low sun; a few warm clouds drift;
    /// - "Sir Duke": the bulbs race and notes rise from the brass at Stevie Wonder's bandstand. "Superstition": the
    ///   lamps flicker and the light turns violet. "(Sittin' On) the Dock of the Bay": the light softens to rose and
    ///   gulls wheel over the dock. "Stand By Me": night falls, and the moon is the only light. "Oh Happy Day": the
    ///   whole land brightens gold. "Georgia on My Mind": peach blossom drifts from the tree.
    ///   Every song's own light brightens while it plays.
    public class SoulLife : MonoBehaviour
    {
        const string Land = "The Soul Lowlands";
        public Vector3 center, dukeAt, superAt, dockAt, georgiaAt, moonAt, happyAt;
        public string dukeSong, superSong, dockSong, georgiaSong, moonSong, happySong;
        public Material lamps, windows, flicker; public Material[] bulbs;
        public float riff = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem motes, notes, petals; Light happyLight;
        readonly List<Transform> gulls = new List<Transform>();
        Color lampBase, windowBase, bulbBase, flickerBase;
        float duke, super, dock, moon, happy, georgia, nextMote, nextNote, nextPetal;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 34; clouds.span = 2600f; clouds.key = "soul";
            clouds.minHeight = 150f; clouds.maxHeight = 230f; clouds.size = 2.8f; clouds.speed = 2f; clouds.cover = .3f;
            clouds.color = new Color(1f, .86f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); windowBase = E(windows); bulbBase = bulbs != null && bulbs.Length > 0 ? E(bulbs[0]) : Color.black; flickerBase = E(flicker);
            motes = Particles("Gold motes", 400, new Color(1f, .88f, .55f, .7f), .03f, .07f, true);
            var mm = motes.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .25f);
            notes = Particles("Sir Duke notes", 300, new Color(1f, .82f, .4f, .95f), .16f, .3f, true);
            var nm = notes.main; nm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); nm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.2f); nm.gravityModifier = -.05f;
            var ns = notes.shape; ns.shapeType = ParticleSystemShapeType.Cone; ns.angle = 28f; ns.radius = 2.4f; ns.rotation = new Vector3(-90f, 0, 0);
            petals = Particles("Peach blossom", 300, new Color(1f, .78f, .72f, .9f), .05f, .1f, false);
            var pm = petals.main; pm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); pm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f); pm.gravityModifier = .04f;
            var white = WorldMaterials.Solid(new Color(.97f, .97f, .95f));
            for (var k = 0; k < 7; k++) { var gk = new MeshKit(); gk.Box(white, Vector3.zero, new Vector3(.14f, .1f, .4f), Quaternion.identity); foreach (var s in new[] { -1f, 1f }) gk.Box(white, new Vector3(s * .42f, .04f, 0), new Vector3(.75f, .03f, .2f), Quaternion.Euler(0, 0, s * 14f)); var go = gk.BuildSingle("Gull", transform, false); if (go != null) { go.SetActive(false); gulls.Add(go.transform); } }
            if (happyAt != Vector3.zero) happyLight = ArtistBits.PointLight(transform, happyAt + Vector3.up * 6f, new Color(1f, .9f, .6f), 80f, 0f);
        }

        static Material Additive(string name)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool add)
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
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = add ? Additive(name) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            if (!add) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; }
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

            duke = Mathf.MoveTowards(duke, On(dukeSong) ? 1f : 0f, dt / 2f);
            super = Mathf.MoveTowards(super, On(superSong) ? 1f : 0f, dt / 3f);
            dock = Mathf.MoveTowards(dock, On(dockSong) ? 1f : 0f, dt / 5f);
            moon = Mathf.MoveTowards(moon, On(moonSong) ? 1f : 0f, dt / 6f);
            happy = Mathf.MoveTowards(happy, On(happySong) ? 1f : 0f, dt / 3f);
            georgia = Mathf.MoveTowards(georgia, On(georgiaSong) ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null && m != flicker) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            if (flicker != null) flicker.SetColor("_EmissionColor", flickerBase * (Mathf.PerlinNoise(t * 4f, 7f) > .35f ? 1.3f : .25f));

            // the bulbs chase: three sets taking turns, slow most of the time, racing for Sir Duke and the clavinet
            var rate = 1.1f + energy * 1.2f + duke * 5f + (t - riff < 6f ? 4f : 0f);
            if (bulbs != null) for (var i = 0; i < bulbs.Length; i++) if (bulbs[i] != null) { var ph = Mathf.Repeat(now * rate - i / 3f, 1f); bulbs[i].SetColor("_EmissionColor", bulbBase * (.45f + 1.3f * Mathf.Pow(Mathf.Clamp01(1f - ph * 2.2f), 2f) + happy * .8f + moon * .6f)); }
            var flick = super > 0f ? Mathf.Lerp(1f, Mathf.PerlinNoise(t * 9f, 2f) > .45f ? 1.2f : .15f, super) : 1f;
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + energy * .35f + moon * 1.2f + happy * .6f) * flick);
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + energy * .25f + moon * 1f + happy * .8f + .04f * Mathf.Sin(t * 1.7f)));

            // the sky: honey gold; violet for Superstition, rose for the dock, night for Stand By Me, blazing for Oh Happy Day
            var tint = new Color(1f, .86f, .6f); var amount = .08f;
            if (super > 0f) { tint = Color.Lerp(tint, new Color(.5f, .3f, .75f), super); amount = Mathf.Lerp(amount, .4f, super); }
            if (dock > 0f) { tint = Color.Lerp(tint, new Color(.95f, .6f, .62f), dock); amount = Mathf.Lerp(amount, .35f, dock); }
            if (moon > 0f) { tint = Color.Lerp(tint, new Color(.07f, .09f, .2f), moon); amount = Mathf.Lerp(amount, .78f, moon); }
            if (happy > 0f) { tint = Color.Lerp(tint, new Color(1f, .93f, .62f), happy); amount = Mathf.Lerp(amount, .3f + .08f * Mathf.Sin(t * 1.5f), happy); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (happyLight != null) happyLight.intensity = happy * (5f + Mathf.Sin(t * 1.5f));
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .35f + .25f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main;
            if (here && cam != null && t >= nextMote)
            {   // gold motes hanging in the low sun, round you
                nextMote = t + .12f;
                var p = cam.transform.position + new Vector3(Random.Range(-14f, 14f), Random.Range(-1f, 5f), Random.Range(-14f, 14f));
                motes.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x, .1f, wind.y) * .2f }, 1);
            }
            if (duke > 0f && dukeAt != Vector3.zero && t >= nextNote) { nextNote = t + .12f; notes.transform.position = dukeAt; notes.Emit(Mathf.CeilToInt(duke * 2f)); }
            if (georgia > 0f && georgiaAt != Vector3.zero && t >= nextPetal)
            {
                nextPetal = t + .08f;
                petals.Emit(new ParticleSystem.EmitParams { position = georgiaAt + new Vector3(Random.Range(-2.2f, 2.2f), Random.Range(-.6f, 1f), Random.Range(-2.2f, 2.2f)), velocity = new Vector3(wind.x * .5f, -.3f, wind.y * .5f) }, Mathf.CeilToInt(georgia * 2f));
            }
            // gulls wheel over the dock while its song plays
            for (var k = 0; k < gulls.Count; k++)
            {
                var on = dock > .02f && dockAt != Vector3.zero; if (gulls[k].gameObject.activeSelf != on) gulls[k].gameObject.SetActive(on); if (!on) continue;
                var a = now * (.35f + k * .03f) + k * .9f; var r = 9f + k * 2.2f; var h = 6f + k * 1.3f + Mathf.Sin(now * .6f + k) * 1.5f;
                var p = dockAt + new Vector3(Mathf.Cos(a) * r, h * dock, Mathf.Sin(a) * r);
                gulls[k].SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, -18f));
            }
        }
    }
}
