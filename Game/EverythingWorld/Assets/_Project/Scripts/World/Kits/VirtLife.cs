using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Mount Virtuoso, calm below and stormy above:
    /// - in the foothills, soft mist drifts low, cabin chimneys smoke, birds wheel round the white cliffs; the higher you
    ///   climb the darker the sky, the stronger the wind and rain, and the more often lightning flashes; at the summit the
    ///   storm is full and the lightning rod is struck every so often (and whenever someone plays the summit riff); the amp
    ///   lights burn; streaks of light chase each other round Nili Brosh's ledge; Buckethead's ghost drifts;
    /// - "Cliffs Of Dover": the storm parts and gold light breaks through. "Ghost": a cold pale light. "Near Light": warm
    ///   lamplight. "Caribbean Blue": a soft sea-blue. "Titles": a bright slow-motion gold. Every song's own light
    ///   brightens while it plays.
    public class VirtLife : MonoBehaviour
    {
        const string Land = "Mount Virtuoso";
        public Vector3 center, summit, rod; public float stormFrom;
        public string cliffsSong, ghostSong, nearSong, blueSong, titlesSong;
        public Material lamps, amps, water; public float played = -99f, strike = -99f;
        public List<(Transform t, Vector3 home, float ph)> birds = new List<(Transform, Vector3, float)>(), chasers = new List<(Transform, Vector3, float)>();
        public List<(Transform t, Vector3 home)> ghosts = new List<(Transform, Vector3)>();
        public List<Vector3> fires = new List<Vector3>(), chimneys = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, ampBase, waterBase;
        float cliffs, ghost, near, blue, titles, storm, flash, nextFall, nextSmoke, nextBolt;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2400f; clouds.key = "virtuoso";
            clouds.minHeight = 130f; clouds.maxHeight = 200f; clouds.size = 4f; clouds.speed = 1.4f; clouds.cover = .55f;
            clouds.color = new Color(.7f, .74f, .8f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); ampBase = E(amps); waterBase = E(water);
            dots = Particles("Mist, rain, sparks and lightning", 1600, Color.white, .05f, .2f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 8f);
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

            cliffs = Ease(cliffs, On(cliffsSong), 4f); ghost = Ease(ghost, On(ghostSong), 4f); near = Ease(near, On(nearSong), 4f); blue = Ease(blue, On(blueSong), 5f); titles = Ease(titles, On(titlesSong), 5f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            var cam = Camera.main; var cp = cam != null ? cam.transform.position : center;
            var target = here ? Mathf.Clamp01((cp.y - stormFrom) / 24f) * (1f - cliffs * .7f) : 0f; storm = Mathf.MoveTowards(storm, target, dt * .3f);
            // lightning: random flashes up high, a strike on the rod now and then and whenever someone plays the riff
            if (here && Random.value < dt * storm * .25f) flash = 1f;
            var bolt = false; if (t - strike < .1f || (here && t >= nextBolt && storm > .6f)) { bolt = true; flash = 1f; nextBolt = t + Random.Range(18f, 40f); strike = -99f; }
            flash = Mathf.MoveTowards(flash, 0f, dt * 3.5f);

            foreach (var (tr, home, ph) in birds) if (tr != null) { var a = t * .5f + ph; tr.position = home + new Vector3(Mathf.Cos(a) * 6f, Mathf.Sin(a * 2f) * .8f, Mathf.Sin(a) * 6f); tr.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 6f + ph) * 20f); }
            foreach (var (tr, home, ph) in chasers) if (tr != null) { var a = t * (1.6f + energy) + ph; tr.position = home + new Vector3(Mathf.Cos(a) * 3f, Mathf.Sin(a * 3f) * .5f, Mathf.Sin(a) * 3f); tr.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))); }
            foreach (var (tr, home) in ghosts) if (tr != null) tr.position = home + new Vector3(Mathf.Sin(t * .4f), Mathf.Sin(t * .7f) * .4f, Mathf.Cos(t * .3f)) * .8f;

            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + near * .6f + storm * .3f));
            if (amps != null) amps.SetColor("_EmissionColor", ampBase * (.8f + energy * .6f + flash));
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.9f + blue * .6f));

            var tint = new Color(.8f, .86f, 1f); var amount = .06f;
            if (blue > 0f) { tint = Color.Lerp(tint, new Color(.4f, .7f, 1f), blue); amount = Mathf.Lerp(amount, .3f, blue); }
            if (near > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .56f), near); amount = Mathf.Lerp(amount, .28f, near); }
            if (titles > 0f) { tint = Color.Lerp(tint, new Color(1f, .86f, .5f), titles); amount = Mathf.Lerp(amount, .34f, titles); }
            if (ghost > 0f) { tint = Color.Lerp(tint, new Color(.8f, .86f, 1f), ghost); amount = Mathf.Lerp(amount, .34f, ghost); }
            if (storm > 0f) { tint = Color.Lerp(tint, new Color(.3f, .32f, .4f), storm); amount = Mathf.Lerp(amount, .5f, storm); }
            if (cliffs > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .4f), cliffs * .8f); amount = Mathf.Lerp(amount, .34f, cliffs); }
            if (flash > 0f) { tint = Color.Lerp(tint, new Color(.9f, .94f, 1f), flash); amount = Mathf.Lerp(amount, .65f, flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .15f * energy + storm * 1.4f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            if (!here || cam == null) return;
            if (bolt && rod != Vector3.zero && (rod - cp).sqrMagnitude < 200f * 200f) { var p = rod; for (var k = 0; k < 60; k++) { p += new Vector3(Random.Range(-1.2f, 1.2f), .8f, Random.Range(-1.2f, 1.2f)); dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = Vector3.zero, startColor = new Color(.9f, .94f, 1f), startSize = .5f, startLifetime = .25f }, 1); } for (var k = 0; k < 30; k++) dots.Emit(new ParticleSystem.EmitParams { position = rod, velocity = Random.onUnitSphere * 6f, startColor = new Color(.8f, .9f, 1f), startSize = .1f, startLifetime = .6f }, 1); }
            if (t >= nextSmoke)
            {
                nextSmoke = t + .2f;
                foreach (var ch in chimneys) if ((ch - cp).sqrMagnitude < 80f * 80f) dots.Emit(new ParticleSystem.EmitParams { position = ch, velocity = new Vector3(wind.x * .6f, .8f, wind.y * .6f), startColor = new Color(.7f, .72f, .76f, .3f), startSize = 1.2f, startLifetime = 5f }, 1);
                foreach (var f in fires) if ((f - cp).sqrMagnitude < 40f * 40f) dots.Emit(new ParticleSystem.EmitParams { position = f, velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(.6f, 1.2f), Random.Range(-.3f, .3f)), startColor = new Color(1f, .5f, .2f), startSize = .1f, startLifetime = 1.4f }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                if ((1f - storm) > .3f && Random.value < .3f * (1f - storm)) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-22f, 22f), Random.Range(.2f, 2f), Random.Range(-22f, 22f)), velocity = new Vector3(wind.x * .2f, 0, wind.y * .2f), startColor = new Color(.84f, .88f, .94f, .18f), startSize = Random.Range(2.4f, 4.4f), startLifetime = 9f }, 1);
                if (storm > .05f) for (var k = 0; k < 1 + (int)(storm * 5f); k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 12f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * 4f, -11f, wind.y * 4f), startColor = new Color(.7f, .76f, .86f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
                if (storm > .7f && Random.value < .2f) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(4f, 8f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * 3f, -2f, wind.y * 3f), startColor = Color.white, startSize = .08f, startLifetime = 4f }, 1);
            }
        }
    }
}
