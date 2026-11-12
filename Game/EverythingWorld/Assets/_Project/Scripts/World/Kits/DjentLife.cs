using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Djent Spires, precise and dazzling, answering the music:
    /// - the fractal citadel turns slowly over the summit (by the shared clock, so friends see it at the same angle),
    ///   its shaft of light breathing; floating cubes turn in step; the colour wheel, the gyroscope and the molecule
    ///   spin; the crystals and the frets brighten with the music's energy and the strings on the bridges shimmer;
    /// - Tesla coils crack arcs of light now and then, more in loud songs, a storm of them in "Odessa";
    /// - glitter falls round the moon ("It's Raining on the Moon"); the living room floats;
    /// - "The Sky Is Red": the sky turns red. "Electric Sunrise": a gold dawn. "The Summoning": the light drains to
    ///   a dark crimson and the sigil blazes. "Light": every crystal flashes white on the beat. Every song's own light
    ///   brightens while it plays.
    public class DjentLife : MonoBehaviour
    {
        const string Land = "The Djent Spires";
        public Vector3 center, citadelAt, spireTip, pliniSun, prismAt, neckAt, moleculeAt, sigilAt, rainMoon;
        public Material crystal, crystalDeep, frets, iceGlow, goldGlow, shaft, sigil;
        public Transform citadel, molecule; public string moleculeSong;
        public List<Vector3> lanterns = new List<Vector3>(), coils = new List<Vector3>(), cubeAt = new List<Vector3>(), floaters = new List<Vector3>();
        public List<Transform> cubes = new List<Transform>(), wheels = new List<Transform>(), gyros = new List<Transform>();
        public List<Material> strings = new List<Material>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem glitter;
        readonly List<LineRenderer> arcs = new List<LineRenderer>(); readonly List<float> arcLife = new List<float>();
        float red, sunrise, summon, flash, odessa, nextArc, nextGlitter;
        Color crystalBase, deepBase, fretBase, iceBase, goldBase, shaftBase;
        readonly Dictionary<Material, Color> stringBase = new Dictionary<Material, Color>();

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 45; clouds.span = 2600f; clouds.key = "djent";
            clouds.minHeight = 230f; clouds.maxHeight = 300f; clouds.size = 2.2f; clouds.speed = 3f; clouds.cover = .35f;
            clouds.color = new Color(.95f, .97f, 1f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            crystalBase = E(crystal); deepBase = E(crystalDeep); fretBase = E(frets); iceBase = E(iceGlow); goldBase = E(goldGlow); shaftBase = E(shaft);
            foreach (var s in strings) if (s != null) stringBase[s] = E(s);
            var arcMat = Additive("Tesla arc"); arcMat.SetColor("_BaseColor", new Color(.7f, .9f, 1f) * 4f);
            for (var k = 0; k < 6; k++)
            {
                var go = new GameObject("Tesla arc"); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = arcMat; lr.positionCount = 10; lr.widthMultiplier = .12f; lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; lr.enabled = false;
                arcs.Add(lr); arcLife.Add(0f);
            }
            glitter = Particles("Moon glitter", 400, new Color(.85f, .95f, 1f, 1f), .06f, .14f, true);
            var gm = glitter.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f); gm.startSpeed = 0f; gm.gravityModifier = .15f;
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
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static bool Playing(string id, string artist, string title)
        {
            if (id == null || !DjentSongs.Ids(artist, out var l)) return false;
            foreach (var (t, i) in l) if (i == id && t.StartsWith(title)) return true;
            return false;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = (float)(SharedChance.Now % 100000.0);
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            red = Mathf.MoveTowards(red, here && Playing(id, "Leprous", "The Sky Is Red") ? 1f : 0f, dt / 4f);
            sunrise = Mathf.MoveTowards(sunrise, here && Playing(id, "Plini", "Electric Sunrise") ? 1f : 0f, dt / 5f);
            summon = Mathf.MoveTowards(summon, here && Playing(id, "Sleep Token", "The Summoning") ? 1f : 0f, dt / 4f);
            var light = here && Playing(id, "Polyphia", "Light");
            odessa = Mathf.MoveTowards(odessa, here && Playing(id, "Animals As Leaders", "Odessa") ? 1f : 0f, dt / 2f);
            flash = light ? Mathf.Max(flash - dt * 3f, Mathf.Repeat(t * 2.1f, 1f) < .08f ? 1f : 0f) : Mathf.MoveTowards(flash, 0f, dt * 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)) * (s != null && m == sigil ? 1f + summon * 2f : 1f));

            // the crystals, frets and strings: brighter with the music, flashing white in "Light", dimmer in "The Summoning"
            var dim = 1f - summon * .55f;
            var glow = (1f + energy * .45f + .08f * Mathf.Sin(t * .7f) + flash * 2.5f) * dim;
            if (crystal != null) crystal.SetColor("_EmissionColor", crystalBase * glow);
            if (crystalDeep != null) crystalDeep.SetColor("_EmissionColor", deepBase * (glow + .2f * Mathf.Sin(t * 1.3f)));
            if (frets != null) frets.SetColor("_EmissionColor", fretBase * (1f + energy * .6f + sunrise * .8f) * dim);
            if (iceGlow != null) iceGlow.SetColor("_EmissionColor", iceBase * (1f + energy * .3f + flash * 2f) * dim);
            if (goldGlow != null) goldGlow.SetColor("_EmissionColor", goldBase * (1f + sunrise * 1.5f + .15f * Mathf.Sin(t * 1.1f)));
            if (shaft != null) shaft.SetColor("_EmissionColor", shaftBase * (.8f + .4f * Mathf.Sin(t * .9f) + energy * .6f + flash * 2f));
            foreach (var kv in stringBase) kv.Key.SetColor("_EmissionColor", kv.Value * (1f + energy * .8f * (.5f + .5f * Mathf.Sin(t * 37f)) + odessa * 1.5f));

            // the sky
            var tint = new Color(.75f, .88f, 1f); var amount = .06f;
            if (sunrise > 0f) { tint = Color.Lerp(tint, new Color(1f, .78f, .35f), sunrise); amount = Mathf.Lerp(amount, .4f, sunrise); }
            if (red > 0f) { tint = Color.Lerp(tint, new Color(1f, .12f, .1f), red); amount = Mathf.Lerp(amount, .62f, red); }
            if (summon > 0f) { tint = Color.Lerp(tint, new Color(.35f, .05f, .08f), summon); amount = Mathf.Lerp(amount, .55f, summon); }
            if (flash > 0f) { tint = Color.Lerp(tint, Color.white, flash); amount = Mathf.Max(amount, .35f * flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .4f + .3f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the citadel, the cubes and the spinning things (the shared clock, so everyone sees the same angles)
            if (citadel != null) citadel.SetPositionAndRotation(citadelAt + Vector3.up * Mathf.Sin(now * .25f) * 1.5f, Quaternion.Euler(0, now * 4f, 0));
            var cubeRot = Quaternion.Euler(now * 9f, now * 13f, 0);
            for (var k = 0; k < cubes.Count; k++) cubes[k].SetPositionAndRotation(cubeAt[k] + Vector3.up * Mathf.Sin(now * .8f + k * .3f) * .25f, cubeRot);
            foreach (var w in wheels) w.Rotate(Vector3.forward, dt * (30f + energy * 60f), Space.Self);
            foreach (var g in gyros) g.rotation = Quaternion.Euler(now * 40f, now * 27f, now * 11f);
            if (molecule != null) { var fast = here && id != null && id == moleculeSong ? 3f : 1f; molecule.SetPositionAndRotation(moleculeAt + Vector3.up * Mathf.Sin(now * .6f) * .3f, Quaternion.Euler(now * 8f * fast, now * 20f * fast, 0)); }

            if (!here) { foreach (var a in arcs) if (a.enabled) a.enabled = false; return; }
            // Tesla arcs
            nextArc -= dt;
            if (coils.Count > 0 && nextArc <= 0f)
            {
                nextArc = Random.Range(.4f, 2.6f) / (1f + energy * 2f + odessa * 10f);
                var k = arcs.FindIndex(a => !a.enabled);
                if (k >= 0)
                {
                    var from = coils[Random.Range(0, coils.Count)]; var dir = Random.onUnitSphere; dir.y = -Mathf.Abs(dir.y) * .6f;
                    var to = from + dir.normalized * Random.Range(3f, 7f); var lr = arcs[k];
                    for (var i = 0; i < lr.positionCount; i++) { var f = i / (lr.positionCount - 1f); lr.SetPosition(i, Vector3.Lerp(from, to, f) + (i == 0 || i == lr.positionCount - 1 ? Vector3.zero : Random.insideUnitSphere * .6f)); }
                    lr.enabled = true; arcLife[k] = Random.Range(.08f, .2f);
                }
            }
            for (var k = 0; k < arcs.Count; k++) if (arcs[k].enabled) { arcLife[k] -= dt; if (arcLife[k] <= 0f) arcs[k].enabled = false; }
            // glitter round the moon
            nextGlitter -= dt;
            if (rainMoon != Vector3.zero && nextGlitter <= 0f) { nextGlitter = .05f; glitter.transform.position = rainMoon + new Vector3(Random.Range(-2.4f, 2.4f), Random.Range(-.5f, .5f), Random.Range(-2.4f, 2.4f)); glitter.Emit(1); }
        }
    }

    /// Looks up the Djent Spires' songs from outside the kit.
    public static class DjentSongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => DjentKit.SongTable.TryGetValue(artist, out list);
    }
}
