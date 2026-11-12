using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Old Jazz Quarter past midnight, in the rain:
    /// - it rains on the whole town, always; low dark cloud; steam rises from the gratings and smoke hangs in the
    ///   club; the red marquee flickers now and then and its bulbs chase; notes drift up from the saxophone on the
    ///   bridge; the lamps and their pools of light breathe a little with the music;
    /// - Alice Coltrane's songs: the rain stops, gold comes into the sky and the stars come out over the hill.
    ///   "Astral Traveling": the stars come out too. "Strange Fruit": every lamp, window and sign in town goes down
    ///   low, and one light is left. "So What": the town turns blue. "Take the "A" Train": the train's windows flash
    ///   by. "In the Mood": the bulbs chase fast. "'Tis Autumn": leaves fall. "Blue Rondo à la Turk": the nine stones
    ///   light in twos and threes. Every song's own light brightens while it plays.
    public class JazzLife : MonoBehaviour
    {
        const string Land = "The Old Jazz Quarter";
        public Vector3 center, aliceAt, leavesAt;
        public string billieSong, soWhatSong, trainSong, moodSong, autumnSong, astralSong, rondoSong;
        public HashSet<string> aliceSongs = new HashSet<string>();
        public Material lamps, pools, windows, neon, blue, gold, stars, train; public Material[] bulbs, rondo;
        public float played = -99f;
        public List<Vector3> steam = new List<Vector3>(), smoke = new List<Vector3>(), notes = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem mist, leaves, noteDots;
        Color lampBase, poolBase, windowBase, neonBase, blueBase, goldBase, bulbBase, trainBase;
        float alice, astral, billie, soWhat, aTrain, mood, autumn, rondoOn, nextSteam, nextLeaf, nextNote, flickerUntil, nextFlicker;
        static readonly int[] RondoBeat = { 0, 2, 4, 6 };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 70; clouds.span = 2400f; clouds.key = "jazz";
            clouds.minHeight = 130f; clouds.maxHeight = 200f; clouds.size = 3.4f; clouds.speed = 2.4f; clouds.cover = .85f;
            clouds.color = new Color(.3f, .31f, .35f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); poolBase = E(pools); windowBase = E(windows); neonBase = E(neon); blueBase = E(blue); goldBase = E(gold); trainBase = E(train);
            bulbBase = bulbs != null && bulbs.Length > 0 ? E(bulbs[0]) : Color.black;
            mist = Particles("Steam and smoke", 500, new Color(.8f, .82f, .86f, .1f), .5f, 1.1f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); var sz = mist.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, .5f, 1f, 2.4f));
            leaves = Particles("Falling leaves", 300, new Color(1f, .68f, .25f, .95f), .1f, .18f, true);
            var lm = leaves.main; lm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); lm.gravityModifier = .06f;
            noteDots = Particles("Notes", 120, new Color(1f, .82f, .45f, .9f), .06f, .12f, true);
            var nm = noteDots.main; nm.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
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

            alice = Mathf.MoveTowards(alice, here && id != null && aliceSongs.Contains(id) ? 1f : 0f, dt / 6f);
            astral = Mathf.MoveTowards(astral, On(astralSong) ? 1f : 0f, dt / 5f);
            billie = Mathf.MoveTowards(billie, On(billieSong) ? 1f : 0f, dt / 6f);
            soWhat = Mathf.MoveTowards(soWhat, On(soWhatSong) ? 1f : 0f, dt / 4f);
            aTrain = Mathf.MoveTowards(aTrain, On(trainSong) ? 1f : 0f, dt / 2f);
            mood = Mathf.MoveTowards(mood, On(moodSong) ? 1f : 0f, dt / 2f);
            autumn = Mathf.MoveTowards(autumn, On(autumnSong) ? 1f : 0f, dt / 3f);
            rondoOn = Mathf.MoveTowards(rondoOn, On(rondoSong) ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the town's lights: they breathe a little, lift when somebody plays, and go right down for Strange Fruit
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var hush = 1f - billie * .9f;
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * hush * (1f + energy * .2f + lift * .3f + .04f * Mathf.Sin(t * 1.7f)));
            if (pools != null) pools.SetColor("_EmissionColor", poolBase * hush * (1f + energy * .25f + lift * .4f + .05f * Mathf.Sin(t * 1.3f)));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * hush * (1f + .04f * Mathf.Sin(t * 1.1f)));
            if (gold != null) gold.SetColor("_EmissionColor", goldBase * (1f + alice * 1.6f + .08f * Mathf.Sin(t * .8f)));
            if (blue != null) blue.SetColor("_EmissionColor", blueBase * hush * (1f + soWhat * (1.2f + .5f * Mathf.Sin(t * 2.4f))));
            // the marquee: a flicker now and then (the same for everyone), and its bulbs chasing
            if (t >= nextFlicker) { nextFlicker = t + 4f + (float)SharedChance.Value(SharedChance.Key("jazz neon"), (long)(SharedChance.Now / 4.0)) * 9f; flickerUntil = t + .5f; }
            var fl = t < flickerUntil ? (Mathf.Sin(t * 70f) > 0f ? .25f : 1f) : 1f;
            if (neon != null) neon.SetColor("_EmissionColor", neonBase * hush * fl * (1f + soWhat * .4f));
            var rate = 1.2f + energy * 1.2f + lift * 2f + mood * 5f;
            if (bulbs != null) for (var i = 0; i < bulbs.Length; i++) if (bulbs[i] != null) { var ph = Mathf.Repeat(now * rate - i / 3f, 1f); bulbs[i].SetColor("_EmissionColor", bulbBase * hush * (.35f + (ph < .34f ? 1.1f : 0f) + mood * .6f)); }
            if (train != null) train.SetColor("_EmissionColor", trainBase * hush * (1f + aTrain * (1.4f + 1.2f * Mathf.Sign(Mathf.Sin(t * 9f)))));
            if (stars != null) stars.SetColor("_EmissionColor", new Color(1f, .95f, .8f) * (Mathf.Max(alice, astral * .8f) * (2.2f + .5f * Mathf.Sin(t * 1.9f))));
            // Blue Rondo: nine stones, lit two, two, two, three
            if (rondo != null && rondo.Length == 9)
            {
                var step = Mathf.FloorToInt(Mathf.Repeat(now * (rondoOn > 0f ? 6f : 1.5f), 9f));
                for (var i = 0; i < 9; i++) if (rondo[i] != null) { var accent = System.Array.IndexOf(RondoBeat, i) >= 0; rondo[i].SetColor("_EmissionColor", JazzKit.Blue * (.25f + (i == step ? (accent ? 2.6f : 1.2f) : 0f) * (.4f + rondoOn * .6f))); }
            }

            // the sky and the rain: it always rains, except over Alice Coltrane's songs
            SkyDirector.SetSongRain(Land, .55f * (1f - alice));
            var tint = new Color(.5f, .52f, .58f); var amount = 0f;
            if (soWhat > 0f) { tint = Color.Lerp(tint, new Color(.25f, .45f, 1f), soWhat); amount = Mathf.Lerp(amount, .4f, soWhat); }
            if (autumn > 0f) { tint = Color.Lerp(tint, new Color(1f, .75f, .45f), autumn); amount = Mathf.Lerp(amount, .16f, autumn); }
            if (astral > 0f) { tint = Color.Lerp(tint, new Color(.45f, .4f, .9f), astral); amount = Mathf.Lerp(amount, .3f, astral); }
            if (alice > 0f) { tint = Color.Lerp(tint, new Color(1f, .72f, .35f), alice); amount = Mathf.Lerp(amount, .34f, alice); }
            if (billie > 0f) { tint = Color.Lerp(tint, new Color(.02f, .02f, .03f), billie); amount = Mathf.Lerp(amount, .6f, billie); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .15f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSteam)
            {
                nextSteam = t + .12f;
                foreach (var p in steam) if ((p - cp).sqrMagnitude < 70f * 70f) mist.Emit(new ParticleSystem.EmitParams { position = p + new Vector3(Random.Range(-.4f, .4f), 0, Random.Range(-.3f, .3f)), velocity = new Vector3(wind.x * .3f, Random.Range(.5f, 1f), wind.y * .3f) }, 1);
                foreach (var p in smoke) if ((p - cp).sqrMagnitude < 30f * 30f && Random.value < .25f) mist.Emit(new ParticleSystem.EmitParams { position = p + new Vector3(Random.Range(-3f, 3f), Random.Range(-.3f, .3f), Random.Range(-2f, 2f)), velocity = Random.insideUnitSphere * .08f, startSize = Random.Range(.5f, 1f) }, 1);
            }
            if (t >= nextNote)
            {
                nextNote = t + .7f;
                foreach (var p in notes) if ((p - cp).sqrMagnitude < 50f * 50f) noteDots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(.4f, .8f), Random.Range(-.3f, .3f)) }, 1);
            }
            if (leavesAt != Vector3.zero && t >= nextLeaf && (leavesAt - cp).sqrMagnitude < 80f * 80f)
            {
                nextLeaf = t + Mathf.Lerp(.5f, .04f, autumn);
                leaves.Emit(new ParticleSystem.EmitParams { position = leavesAt + new Vector3(Random.Range(-4f, 4f), Random.Range(3.5f, 6.5f), Random.Range(-4f, 4f)), velocity = new Vector3(wind.x * .5f + Random.Range(-.3f, .3f), -.5f, wind.y * .5f + Random.Range(-.3f, .3f)) }, 1);
            }
        }
    }
}
