using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Sample Gardens in the late afternoon:
    /// - records spin on the turntables; tape reels turn; the marble machine's wheel rolls and its marbles climb the lift
    ///   and run down the tracks, faster after you turn the crank; the radio dials glow and drift; bees and seed fluff float
    ///   in the gold light; petals fall from Bibio's cherry tree; the glass fountain sprays;
    /// - "Since I Left You": a soft tropical teal. "Petals": pink petals everywhere. "Silver": silver light. "Sommarfågel":
    ///   bright summer gold. "Tokyo": neon pink. "Stars Without Light": dusk falls blue. "Sunshine Gradient": the light
    ///   slides from rose to gold. "Hearing Damage": everything goes muffled and grey. Dropping the needle turns the garden up.
    public class GardenLife : MonoBehaviour
    {
        const string Land = "The Sample Gardens";
        public Vector3 center, petalTree;
        public string petalSong, summerSong, starSong, silverSong, gradSong, sinceSong, tokyoSong, damageSong;
        public Material lamps, dials, glass; public float played = -99f, cranked = -99f;
        public List<Transform> records = new List<Transform>(), reels = new List<Transform>(), wheels = new List<Transform>();
        public List<(Transform t, float ph)> marbles = new List<(Transform, float)>();
        public List<Vector3> marbleTrack = new List<Vector3>(), fountains = new List<Vector3>();
        public List<(Transform t, Vector3 home)> spinners = new List<(Transform, Vector3)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, dialBase, glassBase;
        float petal, summer, star, silver, grad, since, tokyo, damage, marbleT, nextFall, nextSpray;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2400f; clouds.key = "gardens";
            clouds.minHeight = 120f; clouds.maxHeight = 210f; clouds.size = 3.4f; clouds.speed = .7f; clouds.cover = .35f;
            clouds.color = new Color(1f, .86f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); dialBase = E(dials); glassBase = E(glass);
            dots = Particles("Petals, fluff and bees", 1200, Color.white, .05f, .14f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 9f);
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

            petal = Ease(petal, On(petalSong), 4f); summer = Ease(summer, On(summerSong), 4f); star = Ease(star, On(starSong), 5f); silver = Ease(silver, On(silverSong), 4f); grad = Ease(grad, On(gradSong), 4f); since = Ease(since, On(sinceSong), 5f); tokyo = Ease(tokyo, On(tokyoSong), 3f); damage = Ease(damage, On(damageSong), 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            var lift = t - played < 8f ? 1f - (t - played) / 8f : 0f; var crank = t - cranked < 20f ? 1f - (t - cranked) / 20f : 0f;
            foreach (var r in records) if (r != null) r.Rotate(0, dt * 200f, 0, Space.Self);
            foreach (var r in reels) if (r != null) r.Rotate(0, 0, -dt * 90f, Space.Self);
            foreach (var w in wheels) if (w != null) w.Rotate(0, 0, dt * (20f + crank * 80f + summer * 60f), Space.Self);
            marbleT += dt * (.05f + crank * .2f + summer * .15f);
            if (marbleTrack.Count > 1) foreach (var (tr, ph) in marbles) { if (tr == null) continue; var k = Mathf.Repeat(marbleT + ph, 1f) * (marbleTrack.Count - 1); var i = Mathf.Min(marbleTrack.Count - 2, (int)k); tr.position = Vector3.Lerp(marbleTrack[i], marbleTrack[i + 1], k - i) + Vector3.up * .06f; }
            for (var i = 0; i < spinners.Count; i++) { var (tr, home) = spinners[i]; if (tr == null) continue; tr.position = home + Vector3.up * Mathf.Sin(t * .8f + i) * .1f; tr.Rotate(0, dt * 40f, 0, Space.World); }

            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + lift * .4f + star * .5f));
            if (dials != null) dials.SetColor("_EmissionColor", dialBase * (.8f + .2f * Mathf.Sin(t * 1.3f) + lift * 1.2f + energy * .3f));
            if (glass != null) glass.SetColor("_EmissionColor", glassBase * (1f + silver * .6f));

            var tint = new Color(1f, .82f, .58f); var amount = .1f;
            if (summer > 0f) { tint = Color.Lerp(tint, new Color(1f, .86f, .4f), summer); amount = Mathf.Lerp(amount, .34f, summer); }
            if (petal > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .84f), petal); amount = Mathf.Lerp(amount, .3f, petal); }
            if (since > 0f) { tint = Color.Lerp(tint, new Color(.5f, .86f, .84f), since); amount = Mathf.Lerp(amount, .34f, since); }
            if (silver > 0f) { tint = Color.Lerp(tint, new Color(.84f, .88f, .96f), silver); amount = Mathf.Lerp(amount, .34f, silver); }
            if (grad > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(1f, .5f, .6f), new Color(1f, .86f, .4f), Mathf.PingPong(now * .1f, 1f)), grad); amount = Mathf.Lerp(amount, .34f, grad); }
            if (tokyo > 0f) { tint = Color.Lerp(tint, new Color(1f, .3f, .7f), tokyo); amount = Mathf.Lerp(amount, .36f, tokyo); }
            if (star > 0f) { tint = Color.Lerp(tint, new Color(.3f, .36f, .6f), star); amount = Mathf.Lerp(amount, .46f, star); }
            if (damage > 0f) { tint = Color.Lerp(tint, new Color(.56f, .56f, .56f), damage); amount = Mathf.Lerp(amount, .44f, damage); }
            if (lift > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .6f), lift * .5f); amount = Mathf.Lerp(amount, .3f, lift); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .15f * energy + petal * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpray)
            {
                nextSpray = t + .15f;
                foreach (var f in fountains) if ((f - cp).sqrMagnitude < 40f * 40f) dots.Emit(new ParticleSystem.EmitParams { position = f, velocity = new Vector3(Random.Range(-.6f, .6f), Random.Range(1.2f, 2f), Random.Range(-.6f, .6f)), startColor = new Color(.8f, .94f, 1f, .7f), startSize = .06f, startLifetime = 1.2f }, 1);
                if (petalTree != Vector3.zero && (petalTree - cp).sqrMagnitude < 50f * 50f) dots.Emit(new ParticleSystem.EmitParams { position = petalTree + new Vector3(Random.Range(-1.4f, 1.4f), 0, Random.Range(-1.4f, 1.4f)), velocity = new Vector3(wind.x * .4f, -.5f, wind.y * .4f), startColor = new Color(1f, .78f, .86f), startSize = .1f, startLifetime = 7f }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(.5f, 4f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .2f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .3f, Random.Range(-.05f, .1f), wind.y * .3f), startColor = Random.value < .3f ? new Color(1f, .8f, .2f) : new Color(1f, .96f, .86f, .7f), startSize = Random.value < .3f ? .06f : .1f, startLifetime = 7f }, 1);
                if (petal > .05f && Random.value < petal) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(5f, 9f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * .6f, -.6f, wind.y * .6f), startColor = new Color(1f, .76f, .86f), startSize = .12f, startLifetime = 8f }, 1);
            }
        }
    }
}
