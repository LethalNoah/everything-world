using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Math Rock Escarpment, quiet between bursts of noise:
    /// - leaves fall round you; the wind chimes sway; the string lights twinkle with the music; mist rises at the foot
    ///   of every fall and drifts over the low ground; Chon's bubbles rise; steam curls off tricot's soup; Covet's
    ///   signal blinks;
    /// - in the post-rock songs the quiet side swells with the song: the lanterns, the falls and the sky brighten
    ///   slowly from the quiet start toward the climax near the end, the mist thickens, then it all settles;
    /// - "Everlasting Light": a beam of light rises from MONO's lamp. "Ashes in the Snow": snow falls over the whole
    ///   land. "two moons": two moons hang in the sky. "Supermoon": a giant moon rises. "春雨 Spring Rain": a soft
    ///   rain. Every song's own light brightens while it plays.
    public partial class MathLife : MonoBehaviour
    {
        const string Land = "The Math Rock Escarpment";
        public Vector3 center, seaDir = Vector3.left, bigFall, ashTree, monoLamp, monoAt, rainCloud;
        public Material bulbs, lanterns, windows, falls, foam, signal;
        public List<Vector3> lanternAt = new List<Vector3>(), fallFoot = new List<Vector3>(), steamAt = new List<Vector3>(), bubbleAt = new List<Vector3>();
        public List<Transform> chimes = new List<Transform>(), bubbles = new List<Transform>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        readonly HashSet<string> postRockIds = new HashSet<string>();

        CloudLayer clouds; ParticleSystem leaves, mist, snow, beam, rain, steam;
        Transform moonA, moonB, superMoon;
        Color bulbBase, lanternBase, windowBase, fallBase, foamBase, signalBase;
        float swell, beamAmt, snowAmt, moonsAmt, superAmt, rainAmt, nextLeaf, nextMist, nextSnow, nextBeam, nextRain, nextSteam;
        readonly List<Quaternion> chimeRest = new List<Quaternion>();

        void Start()
        {
            foreach (var a in new[] { "MONO", "This Will Destroy You", "Explosions In The Sky", "Mogwai", "God Is An Astronaut", "65daysofstatic", "Do Make Say Think", "Saxon Shore", "Infinity Shred" })
                if (MathSongs.Ids(a, out var l)) foreach (var (_, id) in l) postRockIds.Add(id);
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2600f; clouds.key = "escarpment";
            clouds.minHeight = 180f; clouds.maxHeight = 260f; clouds.size = 2.6f; clouds.speed = 4f; clouds.cover = .35f;
            clouds.color = new Color(.98f, .96f, .92f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            bulbBase = E(bulbs); lanternBase = E(lanterns); windowBase = E(windows); fallBase = E(falls); foamBase = E(foam); signalBase = E(signal);
            foreach (var c in chimes) chimeRest.Add(c.rotation);
            leaves = Particles("Falling leaves", 800, new Color(.95f, .55f, .2f, 1f), .12f, .22f, false);
            var lm = leaves.main; lm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); lm.startSpeed = new ParticleSystem.MinMaxCurve(0f, .4f); lm.gravityModifier = .03f;
            lm.startColor = new ParticleSystem.MinMaxGradient(new Color(.95f, .5f, .15f), new Color(.8f, .25f, .12f));
            var rot = leaves.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            mist = Particles("Mist", 600, new Color(.9f, .93f, 1f, .3f), 4f, 9f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.3f, 1f);
            snow = Particles("Snow", 2000, new Color(.95f, .96f, 1f, .9f), .08f, .16f, false);
            var sm = snow.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f); sm.startSpeed = 0f; sm.gravityModifier = .04f;
            beam = Particles("Everlasting light", 800, new Color(1f, .95f, .85f, .8f), .4f, .9f, true);
            var bm = beam.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4f); bm.startSpeed = new ParticleSystem.MinMaxCurve(14f, 20f);
            var bs = beam.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 1.5f; bs.radius = .3f; bs.rotation = new Vector3(-90f, 0, 0);
            rain = Particles("Spring rain", 1500, new Color(.75f, .82f, .95f, .5f), .04f, .06f, false);
            var rm = rain.main; rm.startLifetime = 1.2f; rm.startSpeed = 0f; rm.gravityModifier = 2f;
            var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.Stretch; rr.velocityScale = .05f; rr.lengthScale = 3f;
            steam = Particles("Soup steam", 200, new Color(1f, 1f, 1f, .35f), .3f, .6f, false);
            var stm = steam.main; stm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f); stm.startSpeed = new ParticleSystem.MinMaxCurve(.4f, .8f);
            var ssh = steam.shape; ssh.shapeType = ParticleSystemShapeType.Cone; ssh.angle = 10f; ssh.radius = .15f; ssh.rotation = new Vector3(-90f, 0, 0);
            var moonM = new Material(WorldMaterials.Glow(new Color(.97f, .96f, .88f), 1.6f)) { name = "Moons" };
            Transform Moon(string name, float r) { var k = new MeshKit(); k.Prism(moonM, Vector3.zero, Vector3.forward, r, .3f, 40); var go = k.BuildSingle(name, transform, false); if (go == null) return null; go.SetActive(false); return go.transform; }
            moonA = Moon("Moon one", 14f); moonB = Moon("Moon two", 9f); superMoon = Moon("Supermoon", 60f);
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

        static bool Playing(string id, string artist, string title)
        {
            if (id == null || !MathSongs.Ids(artist, out var l)) return false;
            foreach (var (t, i) in l) if (i == id && t.StartsWith(title)) return true;
            return false;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            // the swell: how far through a post-rock song we are, building to the climax near the end
            var want = 0f;
            if (here && id != null && postRockIds.Contains(id))
            {
                if (MusicDirector.ForcedTrackId != null) want = .5f + .5f * Mathf.Sin(t * .2f);
                else if (SharedChance.CurrentSong(out _, out var start, out var length) && length > 1) { var p = (float)((SharedChance.Now - start) / length); want = Ease(.1f, .85f, p) * (1f - Ease(.93f, 1f, p)); }
            }
            swell = Mathf.MoveTowards(swell, want, dt / 3f);
            beamAmt = Mathf.MoveTowards(beamAmt, here && Playing(id, "MONO", "Everlasting Light") ? 1f : 0f, dt / 3f);
            snowAmt = Mathf.MoveTowards(snowAmt, here && Playing(id, "MONO", "Ashes in the Snow") ? 1f : 0f, dt / 5f);
            moonsAmt = Mathf.MoveTowards(moonsAmt, here && Playing(id, "toe", "two moons") ? 1f : 0f, dt / 4f);
            superAmt = Mathf.MoveTowards(superAmt, here && Playing(id, "65daysofstatic", "Supermoon") ? 1f : 0f, dt / 8f);
            rainAmt = Mathf.MoveTowards(rainAmt, here && Playing(id, "Elephant Gym", "春雨") ? 1f : 0f, dt / 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            var twinkle = .85f + .15f * Mathf.Sin(t * 3.1f) * Mathf.Sin(t * 1.7f);
            if (bulbs != null) bulbs.SetColor("_EmissionColor", bulbBase * (twinkle + energy * .5f + swell * .8f));
            if (lanterns != null) lanterns.SetColor("_EmissionColor", lanternBase * (1f + swell * 1.6f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + swell * .6f));
            if (falls != null) falls.SetColor("_EmissionColor", fallBase * (.9f + .1f * Mathf.Sin(t * 5f) + swell * 1.4f + energy * .3f));
            if (foam != null) foam.SetColor("_EmissionColor", foamBase * (1f + swell));
            if (signal != null) signal.SetColor("_EmissionColor", signalBase * (Mathf.Repeat(t, 1.2f) < .7f ? 1f : .1f));

            // the sky: clear autumn blue; warm gold as a post-rock song swells; white with snow
            var tint = new Color(1f, .8f, .5f); var amount = .04f + swell * .3f;
            if (snowAmt > 0f) { tint = Color.Lerp(tint, new Color(.85f, .9f, 1f), snowAmt); amount = Mathf.Max(amount, .3f * snowAmt); }
            if (superAmt > 0f) { tint = Color.Lerp(tint, new Color(.35f, .4f, .6f), superAmt); amount = Mathf.Max(amount, .45f * superAmt); }
            if (moonsAmt > 0f) { tint = Color.Lerp(tint, new Color(.5f, .55f, .75f), moonsAmt); amount = Mathf.Max(amount, .3f * moonsAmt); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, rainAmt * .4f);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .5f + .3f * energy + swell * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            // the chimes sway, the bubbles rise
            for (var k = 0; k < chimes.Count; k++) chimes[k].rotation = chimeRest[k] * Quaternion.Euler(Mathf.Sin(t * 1.3f + k) * (4f + energy * 6f), 0, Mathf.Sin(t * 1.7f + k * 2f) * (3f + energy * 5f));
            for (var k = 0; k < bubbles.Count; k++) { var ph = Mathf.Repeat(t * .12f + k * .37f, 1f); bubbles[k].position = bubbleAt[k] + new Vector3(Mathf.Sin(t * .7f + k) * 1.4f, 1f + ph * 7f, Mathf.Cos(t * .6f + k * 2f) * 1.4f); bubbles[k].localScale = Vector3.one * (ph < .9f ? 1f : (1f - ph) * 10f); }

            var cam = Camera.main != null ? Camera.main.transform : null;
            var camP = cam != null ? cam.position : center;
            // the moons
            void Place(Transform m, float amt, Vector3 offset, float scale)
            {
                if (m == null) return; var on = here && amt > .01f; if (m.gameObject.activeSelf != on) m.gameObject.SetActive(on); if (!on || cam == null) return;
                var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized; var right = Vector3.Cross(Vector3.up, fwd);
                var pos = camP + fwd * 500f + right * offset.x + Vector3.up * (offset.y * Mathf.Lerp(.4f, 1f, amt)); m.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - camP)); m.localScale = Vector3.one * scale * amt;
            }
            Place(moonA, moonsAmt, new Vector3(-60f, 160f, 0), 1f); Place(moonB, moonsAmt, new Vector3(70f, 120f, 0), 1f); Place(superMoon, superAmt, new Vector3(0, 110f, 0), 1f);

            if (!here) return;
            nextLeaf -= dt;
            if (nextLeaf <= 0f) { nextLeaf = .06f; leaves.transform.position = camP + new Vector3(Random.Range(-25f, 25f), Random.Range(6f, 14f), Random.Range(-25f, 25f)); leaves.Emit(1); }
            nextMist -= dt;
            if (nextMist <= 0f)
            {
                nextMist = .25f / (1f + swell * 2f);
                foreach (var f in fallFoot) if ((f - camP).sqrMagnitude < 200f * 200f && Random.value < .15f) { mist.transform.position = f; mist.Emit(1); }
                if (bigFall != Vector3.zero) { mist.transform.position = bigFall + Random.insideUnitSphere * 4f; mist.Emit(1); }
            }
            nextSnow -= dt;
            if (nextSnow <= 0f)
            {
                nextSnow = .015f;
                if (snowAmt > .05f) for (var k = 0; k < Mathf.CeilToInt(4 * snowAmt); k++) { snow.transform.position = camP + new Vector3(Random.Range(-35f, 35f), Random.Range(10f, 20f), Random.Range(-35f, 35f)); snow.Emit(1); }
                if (ashTree != Vector3.zero && Random.value < .15f) { snow.transform.position = ashTree + new Vector3(Random.Range(-4f, 4f), Random.Range(0f, 3f), Random.Range(-4f, 4f)); snow.Emit(1); }
            }
            nextBeam -= dt;
            if (beamAmt > .05f && monoLamp != Vector3.zero && nextBeam <= 0f) { nextBeam = .03f; beam.transform.SetPositionAndRotation(monoLamp, Quaternion.identity); beam.Emit(2); }
            nextRain -= dt;
            if (nextRain <= 0f)
            {
                nextRain = .02f;
                if (rainAmt > .05f) { for (var k = 0; k < 3; k++) { rain.transform.position = camP + new Vector3(Random.Range(-25f, 25f), Random.Range(10f, 16f), Random.Range(-25f, 25f)); rain.Emit(1); } }
                if (rainCloud != Vector3.zero && (rainCloud - camP).sqrMagnitude < 80f * 80f) { rain.transform.position = rainCloud + new Vector3(Random.Range(-1.4f, 1.4f), -.3f, Random.Range(-.5f, .5f)); rain.Emit(1); }
            }
            nextSteam -= dt;
            if (nextSteam <= 0f) { nextSteam = .2f; foreach (var s in steamAt) { steam.transform.position = s; steam.Emit(1); } }
        }
    }

    /// Looks up the Math Rock Escarpment's songs from outside the kit.
    public static class MathSongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => MathKit.SongTable.TryGetValue(artist, out list);
    }
}
