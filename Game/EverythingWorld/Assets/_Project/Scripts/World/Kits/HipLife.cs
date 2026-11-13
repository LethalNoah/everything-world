using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Hip-Hop Boroughs at golden hour:
    /// - the boombox on the Tower beats (and every speaker with it), harder when anybody plays; the Stage's lights
    ///   chase; gold dust hangs in the low sun; long gold clouds; confetti over the Stage whenever somebody wins;
    /// - "Not Like Us": the cones pound and confetti falls everywhere. "Alright": the light turns warmer and gold.
    ///   "HUMBLE.": the colour drains to stark white. "Lose Yourself": the light drops and pulses like a heartbeat.
    ///   "Runaway": cool and white. Kid Cudi's songs: night falls. "Fight The Power": red on the beat. "SICKO MODE":
    ///   the light switches colour, three times. "EARFQUAKE": pink, and the wind gets up. "I Gotta Feeling": confetti
    ///   and party colours. "Starships": pink sparks climb. "Paper Planes": paper planes drift over.
    ///   Every song's own light brightens while it plays.
    public class HipLife : MonoBehaviour
    {
        const string Land = "The Hip-Hop Boroughs";
        public Vector3 center, stageAt;
        public string alrightSong, humbleSong, notLikeUsSong, runawaySong, fightSong, sickoSong, earSong, feelingSong, starSong, planesSong;
        public HashSet<string> lose = new HashSet<string>(), cudi = new HashSet<string>();
        public Material cones, windows, lamps, stage; public float played = -99f;
        public List<Vector3> smokes = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dust, dots, smoke;
        Color coneBase, windowBase, lampBase, stageBase;
        float alright, humble, nlu, loseA, runaway, night, fight, sicko, ear, feeling, star, planes, nextDust, nextFall, nextSmoke;
        static readonly Color[] Confetti = { new Color(1f, .8f, .2f), new Color(1f, .95f, .8f), new Color(.6f, .35f, .9f), new Color(.95f, .3f, .3f), new Color(.3f, .7f, 1f) };
        static readonly Color[] Sicko = { new Color(1f, .3f, .2f), new Color(.3f, .5f, 1f), new Color(.5f, 1f, .4f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2600f; clouds.key = "hiphop";
            clouds.minHeight = 190f; clouds.maxHeight = 280f; clouds.size = 3.6f; clouds.speed = 2f; clouds.cover = .34f;
            clouds.color = new Color(1f, .82f, .6f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            coneBase = E(cones); windowBase = E(windows); lampBase = E(lamps); stageBase = E(stage);
            dust = Particles("Gold dust", 300, new Color(1f, .85f, .5f, .5f), .03f, .07f, true);
            var dm = dust.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            dots = Particles("Confetti", 900, Color.white, .08f, .14f, false);
            var cm = dots.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            smoke = Particles("Smoke", 200, new Color(1f, 1f, 1f, .16f), .12f, .3f, false);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .4f, 1f, 2.4f));
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

            alright = Mathf.MoveTowards(alright, On(alrightSong) ? 1f : 0f, dt / 4f);
            humble = Mathf.MoveTowards(humble, On(humbleSong) ? 1f : 0f, dt / 3f);
            nlu = Mathf.MoveTowards(nlu, On(notLikeUsSong) ? 1f : 0f, dt / 2f);
            loseA = Mathf.MoveTowards(loseA, here && id != null && lose.Contains(id) ? 1f : 0f, dt / 4f);
            runaway = Mathf.MoveTowards(runaway, On(runawaySong) ? 1f : 0f, dt / 5f);
            night = Mathf.MoveTowards(night, here && id != null && cudi.Contains(id) ? 1f : 0f, dt / 6f);
            fight = Mathf.MoveTowards(fight, On(fightSong) ? 1f : 0f, dt / 3f);
            sicko = Mathf.MoveTowards(sicko, On(sickoSong) ? 1f : 0f, dt / 3f);
            ear = Mathf.MoveTowards(ear, On(earSong) ? 1f : 0f, dt / 4f);
            feeling = Mathf.MoveTowards(feeling, On(feelingSong) ? 1f : 0f, dt / 3f);
            star = Mathf.MoveTowards(star, On(starSong) ? 1f : 0f, dt / 3f);
            planes = Mathf.MoveTowards(planes, On(planesSong) ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.6f) * .6f)));

            // the boombox: boom, boom-bap. Harder for the anthems and when anybody plays
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var bar = Mathf.Repeat(now * .75f, 2f); var beat = bar < 1f ? bar : Mathf.Repeat(bar * 2f, 1f); var thump = beat < .14f ? 1f - beat / .14f : 0f;
            if (cones != null) cones.SetColor("_EmissionColor", coneBase * (.5f + thump * (1f + nlu * 2.6f + lift * 1.6f + feeling * 1.2f + energy * .6f)));
            if (stage != null) stage.SetColor("_EmissionColor", stageBase * (.7f + .4f * Mathf.Sin(t * 3f) + lift * 1.2f + nlu));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + night * .8f + alright * .4f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + night * 1.2f));

            var tint = new Color(1f, .92f, .76f); var amount = .04f;
            if (alright > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .36f), alright); amount = Mathf.Lerp(amount, .34f, alright); }
            if (feeling > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(t * .08f, 1f), .5f, 1f), feeling); amount = Mathf.Lerp(amount, .3f, feeling); }
            if (ear > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .75f), ear); amount = Mathf.Lerp(amount, .36f, ear); }
            if (runaway > 0f) { tint = Color.Lerp(tint, new Color(.86f, .9f, 1f), runaway); amount = Mathf.Lerp(amount, .4f, runaway); }
            if (humble > 0f) { tint = Color.Lerp(tint, new Color(.92f, .92f, .92f), humble); amount = Mathf.Lerp(amount, .55f, humble); }
            if (fight > 0f) { tint = Color.Lerp(tint, new Color(1f, .16f, .12f), fight); amount = Mathf.Lerp(amount, .16f + thump * .22f, fight); }
            if (sicko > 0f) { tint = Color.Lerp(tint, Sicko[Mathf.FloorToInt(Mathf.Repeat(t / 9f, 3f))], sicko); amount = Mathf.Lerp(amount, .4f, sicko); }
            if (loseA > 0f) { var hb = Mathf.Repeat(t * 1.4f, 1f); tint = Color.Lerp(tint, new Color(.16f, .16f, .24f), loseA); amount = Mathf.Lerp(amount, .5f - (hb < .12f ? .14f : hb > .2f && hb < .3f ? .1f : 0f), loseA); }
            if (night > 0f) { tint = Color.Lerp(tint, new Color(.1f, .14f, .36f), night); amount = Mathf.Lerp(amount, .62f, night); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .15f * energy + ear * .7f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextDust)
            {
                nextDust = t + .12f; var p = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(-.8f, 4f), Random.Range(-14f, 14f));
                if (!IndoorSpaces.Contains(p)) dust.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .2f, Random.Range(-.03f, .08f), wind.y * .2f) }, 1);
            }
            if (t >= nextSmoke)
            {
                nextSmoke = t + .16f;
                foreach (var p in smokes) if ((p - cp).sqrMagnitude < 50f * 50f) smoke.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .1f, .3f, wind.y * .1f) }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f; var p = cp + new Vector3(Random.Range(-18f, 18f), 0, Random.Range(-18f, 18f));
                var party = Mathf.Max(nlu, feeling);
                if (lift > .05f && (stageAt - cp).sqrMagnitude < 90f * 90f) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = stageAt + new Vector3(Random.Range(-9f, 9f), Random.Range(0f, 3f), Random.Range(-7f, 7f)), velocity = new Vector3(Random.Range(-.6f, .6f), -1.1f, Random.Range(-.6f, .6f)), startColor = Confetti[Random.Range(0, Confetti.Length)] }, 1);
                if (lift > .05f) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(4f, 8f), velocity = new Vector3(Random.Range(-.5f, .5f), -1f, Random.Range(-.5f, .5f)), startColor = Confetti[Random.Range(0, Confetti.Length)] }, 1);
                if (party > .05f && Random.value < party) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(4f, 9f), Random.Range(-18f, 18f)), velocity = new Vector3(Random.Range(-.5f, .5f), -1f, Random.Range(-.5f, .5f)), startColor = Confetti[Random.Range(0, Confetti.Length)] }, 1);
                if (star > .05f && Random.value < star) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(0f, 2f), velocity = new Vector3(0, Random.Range(3f, 6f), 0), startColor = new Color(1f, .4f, .75f), startSize = .12f, startLifetime = 5f }, 1);
                if (planes > .05f && Random.value < planes * .5f) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * Random.Range(3f, 8f), velocity = new Vector3(wind.x * 2.4f, Random.Range(-.2f, .3f), wind.y * 2.4f), startColor = Color.white, startSize = .24f, startLifetime = 9f }, 1);
            }
        }
    }
}
