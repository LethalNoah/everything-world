using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Pop Republic on a bright day:
    /// - the ferris wheel turns, its cars hanging level; the fountains spray; confetti drifts over the Plaza and bursts
    ///   from the cannons when anybody plays; the photographers' flashes go off; the strings of bulbs chase; the beams
    ///   on K-Pop Square sweep through their colours;
    /// - "Firework": fireworks over the whole land. "Cruel Summer": a pink-orange sunset. "Lavender Haze": a lavender
    ///   haze. "Red" and "All Too Well": red light and red leaves falling. "Blank Space": everything goes white.
    ///   "Thriller": a green night falls. "Dynamite": disco colours and confetti. "Skyfall": the sky darkens and gold
    ///   comes down. "Snow Angel": snow. "Supernova": white flashes. "Roar": gold. "Juno": pink. K-pop songs light up
    ///   the square. Every song's own light brightens while it plays.
    public class PopLife : MonoBehaviour
    {
        const string Land = "The Pop Republic";
        public Vector3 center, stageAt;
        public string fireworkSong, cruelSong, hazeSong, blankSong, thrillerSong, dynamiteSong, skyfallSong, snowSong, novaSong, roarSong, junoSong;
        public HashSet<string> red = new HashSet<string>(), kpop = new HashSet<string>();
        public Material water, lamps, screen; public Material[] bulbs; public float played = -99f;
        public Transform wheel; public List<Transform> cars = new List<Transform>();
        public List<Material> beams = new List<Material>();
        public List<Vector3> sprays = new List<Vector3>(), cannons = new List<Vector3>(), flashes = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots, spray, fizz;
        Color waterBase, lampBase, screenBase; Color[] bulbBase; readonly List<Color> beamBase = new List<Color>();
        float fire, cruel, haze, redA, blank, thriller, dyn, sky, snow, nova, roar, juno, k, nextFall, nextSpray, nextFlash, nextBurst, lastCannon = -99f;
        static readonly Color[] Confetti = { new Color(1f, .5f, .74f), new Color(.3f, .82f, .92f), new Color(1f, .9f, .36f), Color.white, new Color(.74f, .62f, 1f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 28; clouds.span = 2600f; clouds.key = "poprepublic";
            clouds.minHeight = 170f; clouds.maxHeight = 260f; clouds.size = 3.2f; clouds.speed = 2.4f; clouds.cover = .3f;
            clouds.color = new Color(1f, .98f, 1f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            waterBase = E(water); lampBase = E(lamps); screenBase = E(screen); bulbBase = new Color[bulbs != null ? bulbs.Length : 0]; for (var i = 0; i < bulbBase.Length; i++) bulbBase[i] = E(bulbs[i]); foreach (var b in beams) beamBase.Add(E(b));
            dots = Particles("Confetti and fireworks", 2000, Color.white, .08f, .14f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            spray = Particles("Fountain spray", 600, new Color(.8f, .95f, 1f, .6f), .08f, .16f, false);
            var sm = spray.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f); sm.gravityModifier = 1f;
            fizz = Particles("Sparks and flashes", 1200, Color.white, .1f, .2f, true);
            var fm = fizz.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); fm.gravityModifier = .3f;
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

            fire = Mathf.MoveTowards(fire, On(fireworkSong) ? 1f : 0f, dt / 2f);
            cruel = Mathf.MoveTowards(cruel, On(cruelSong) ? 1f : 0f, dt / 5f);
            haze = Mathf.MoveTowards(haze, On(hazeSong) ? 1f : 0f, dt / 5f);
            redA = Mathf.MoveTowards(redA, here && id != null && red.Contains(id) ? 1f : 0f, dt / 5f);
            blank = Mathf.MoveTowards(blank, On(blankSong) ? 1f : 0f, dt / 4f);
            thriller = Mathf.MoveTowards(thriller, On(thrillerSong) ? 1f : 0f, dt / 6f);
            dyn = Mathf.MoveTowards(dyn, On(dynamiteSong) ? 1f : 0f, dt / 2f);
            sky = Mathf.MoveTowards(sky, On(skyfallSong) ? 1f : 0f, dt / 6f);
            snow = Mathf.MoveTowards(snow, On(snowSong) ? 1f : 0f, dt / 4f);
            nova = Mathf.MoveTowards(nova, On(novaSong) ? 1f : 0f, dt / 2f);
            roar = Mathf.MoveTowards(roar, On(roarSong) ? 1f : 0f, dt / 4f);
            juno = Mathf.MoveTowards(juno, On(junoSong) ? 1f : 0f, dt / 4f);
            k = Mathf.MoveTowards(k, here && id != null && kpop.Contains(id) ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 3f) * .6f)));

            // the wheel turns; its cars hang level
            if (wheel != null) { wheel.Rotate(0f, 0f, dt * 3f, Space.Self); foreach (var c in cars) if (c != null) c.rotation = Quaternion.LookRotation(wheel.forward, Vector3.up); }
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var beat = Mathf.Repeat(now * 2f, 1f);
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.9f + .15f * Mathf.Sin(t * 2f)));
            if (bulbs != null) for (var i = 0; i < bulbs.Length; i++) if (bulbs[i] != null) bulbs[i].SetColor("_EmissionColor", bulbBase[i] * (.6f + .6f * Mathf.Max(0f, Mathf.Sin(t * 4f - i * 2.1f)) + lift + dyn * .8f + energy * .3f));
            if (screen != null) screen.SetColor("_EmissionColor", Color.Lerp(screenBase, Color.HSVToRGB(Mathf.Repeat(t * .2f, 1f), .5f, 1f) * 1.4f, .3f + k * .6f) * (1f + (beat < .15f ? k * 1.4f : 0f)));
            for (var i = 0; i < beams.Count; i++) if (beams[i] != null) beams[i].SetColor("_EmissionColor", Color.Lerp(beamBase[i], Color.HSVToRGB(Mathf.Repeat(t * .15f + i * .17f, 1f), .6f, 1f) * 1.2f, .5f) * (.5f + .5f * Mathf.Sin(t * 1.5f + i) + k * 1.4f + lift));

            var tint = new Color(1f, .98f, 1f); var amount = .02f;
            if (roar > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .3f), roar); amount = Mathf.Lerp(amount, .3f, roar); }
            if (juno > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .8f), juno); amount = Mathf.Lerp(amount, .3f, juno); }
            if (cruel > 0f) { tint = Color.Lerp(tint, new Color(1f, .56f, .5f), cruel); amount = Mathf.Lerp(amount, .4f, cruel); }
            if (haze > 0f) { tint = Color.Lerp(tint, new Color(.76f, .64f, 1f), haze); amount = Mathf.Lerp(amount, .42f, haze); }
            if (dyn > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(t * .25f, 1f), .5f, 1f), dyn); amount = Mathf.Lerp(amount, .3f, dyn); }
            if (k > 0f) { tint = Color.Lerp(tint, new Color(.84f, .7f, 1f), k * .6f); amount = Mathf.Lerp(amount, .2f, k); }
            if (redA > 0f) { tint = Color.Lerp(tint, new Color(.95f, .2f, .22f), redA); amount = Mathf.Lerp(amount, .38f, redA); }
            if (blank > 0f) { tint = Color.Lerp(tint, Color.white, blank); amount = Mathf.Lerp(amount, .55f, blank); }
            if (snow > 0f) { tint = Color.Lerp(tint, new Color(.8f, .88f, 1f), snow); amount = Mathf.Lerp(amount, .35f, snow); }
            if (sky > 0f) { tint = Color.Lerp(tint, new Color(.16f, .14f, .22f), sky); amount = Mathf.Lerp(amount, .55f, sky); }
            if (thriller > 0f) { tint = Color.Lerp(tint, new Color(.12f, .3f, .16f), thriller); amount = Mathf.Lerp(amount, .62f, thriller); }
            if (nova > 0f) { var fl = Mathf.Repeat(now * .5f, 1f) < .08f ? 1f : 0f; tint = Color.Lerp(tint, new Color(1f, .96f, .8f), nova); amount = Mathf.Lerp(amount, .1f + fl * .5f, nova); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + sky * 1.5f + thriller * 1.5f));
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .15f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            // the fountains
            if (t >= nextSpray) { nextSpray = t + .05f; foreach (var p in sprays) if ((p - cp).sqrMagnitude < 60f * 60f) spray.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-1.4f, 1.4f), Random.Range(2f, 3.4f), Random.Range(-1.4f, 1.4f)) }, 1); }
            // the photographers
            if (t >= nextFlash) { nextFlash = t + Random.Range(.4f, 1.2f); if (flashes.Count > 0) { var p = flashes[Random.Range(0, flashes.Count)]; if ((p - cp).sqrMagnitude < 80f * 80f) fizz.Emit(new ParticleSystem.EmitParams { position = p, velocity = Vector3.zero, startColor = Color.white, startSize = .7f, startLifetime = .12f }, 1); } }
            // cannons fire when anybody plays
            if (lift > .9f && played > lastCannon) { lastCannon = played; foreach (var p in cannons) if ((p - cp).sqrMagnitude < 120f * 120f) for (var n = 0; n < 60; n++) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(5f, 9f), Random.Range(-2f, 2f)), startColor = Confetti[Random.Range(0, Confetti.Length)] }, 1); }
            // fireworks: bursts high over wherever you are
            if (fire > .1f && t >= nextBurst)
            {
                nextBurst = t + Random.Range(.5f, 1.2f); var b = cp + new Vector3(Random.Range(-60f, 60f), Random.Range(40f, 70f), Random.Range(-60f, 60f)); var c = Confetti[Random.Range(0, Confetti.Length)];
                for (var n = 0; n < 90; n++) fizz.Emit(new ParticleSystem.EmitParams { position = b, velocity = Random.onUnitSphere * Random.Range(8f, 13f), startColor = c, startSize = .5f, startLifetime = Random.Range(1.2f, 2f) }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .06f; var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(4f, 9f), Random.Range(-18f, 18f));
                if (IndoorSpaces.Contains(p)) return;
                var conf = Mathf.Max(dyn, Mathf.Max(.08f, lift * .6f)); if ((center - cp).sqrMagnitude > 40f * 40f && dyn < .1f && lift < .1f) conf = 0f;
                if (Random.value < conf) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.6f, .6f), -1f, Random.Range(-.6f, .6f)), startColor = Confetti[Random.Range(0, Confetti.Length)] }, 1);
                if (redA > .05f && Random.value < redA) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .8f, -.8f, wind.y * .8f), startColor = Random.value < .5f ? new Color(.9f, .2f, .15f) : new Color(1f, .5f, .2f), startSize = .16f, startLifetime = 8f }, 1);
                if (snow > .05f && Random.value < snow) for (var n = 0; n < 2; n++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(4f, 9f), Random.Range(-18f, 18f)), velocity = new Vector3(Random.Range(-.2f, .2f), -.9f, Random.Range(-.2f, .2f)), startColor = Color.white, startSize = .1f, startLifetime = 9f }, 1);
                if (sky > .05f && Random.value < sky) fizz.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * 6f, velocity = new Vector3(0, -3f, 0), startColor = new Color(1f, .8f, .3f), startSize = .12f, startLifetime = 4f }, 1);
                if (haze > .05f && Random.value < haze * .5f) dots.Emit(new ParticleSystem.EmitParams { position = p - Vector3.up * 4f, velocity = new Vector3(wind.x * .3f, .05f, wind.y * .3f), startColor = new Color(.8f, .7f, 1f, .3f), startSize = 2.4f, startLifetime = 6f }, 1);
            }
        }
    }
}
