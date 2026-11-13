using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Boom Bap Harbour on a hot summer afternoon:
    /// - a hazy sky, a few clouds, heat; the hydrant under the El sprays an arc of water across the street; the train
    ///   goes over every minute (BoomTownLife); pigeons of dust blow about the lots;
    /// - "C.R.E.A.M.": dollar bills come down over the Block. Any Wu-Tang song: killer bees round your head.
    ///   "Empire State of Mind": the afternoon turns to evening and every window on the island lights.
    ///   "Juicy": gold sparkles rise at Biggie's. "Ms. Jackson": a summer shower ("you can't predict the weather").
    ///   "Hey Ya!": the whole block flashes like a camera. "B.O.B.": the lights strobe. "Sabotage": the patrol car's
    ///   lights spin. "The Message": the colour drains to concrete. Every song's own light brightens while it plays.
    public class BoomLife : MonoBehaviour
    {
        const string Land = "Boom Bap Harbour";
        public Vector3 center, hydrant, hydrantDir, biggieAt; public Transform trainAt;
        public string creamSong, empireSong, jacksonSong, heyYaSong, bobSong, sabotageSong, messageSong;
        public HashSet<string> wu = new HashSet<string>(), juicy = new HashSet<string>();
        public Material windows, bulbs, gold, sirenRed, sirenBlue; public float played = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem water, dots;
        Color windowBase, bulbBase, goldBase;
        float cream, bees, empire, juice, jackson, heyYa, bob, sabotage, message, nextWater, nextBill, nextBee, nextGold;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2600f; clouds.key = "boombap";
            clouds.minHeight = 180f; clouds.maxHeight = 270f; clouds.size = 3.4f; clouds.speed = 2.6f; clouds.cover = .24f;
            clouds.color = new Color(1f, .99f, .96f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            windowBase = new Color(1f, .85f, .5f); bulbBase = E(bulbs); goldBase = E(gold);
            water = Particles("Hydrant", 500, new Color(.8f, .92f, 1f, .7f), .08f, .16f, false);
            var wm = water.main; wm.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.6f); wm.gravityModifier = 1f;
            dots = Particles("Bills, bees and sparkles", 600, Color.white, .08f, .14f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(0f, 1f) });
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

            cream = Mathf.MoveTowards(cream, On(creamSong) ? 1f : 0f, dt / 2f);
            bees = Mathf.MoveTowards(bees, here && id != null && wu.Contains(id) ? 1f : 0f, dt / 2f);
            empire = Mathf.MoveTowards(empire, On(empireSong) ? 1f : 0f, dt / 6f);
            juice = Mathf.MoveTowards(juice, here && id != null && juicy.Contains(id) ? 1f : 0f, dt / 2f);
            jackson = Mathf.MoveTowards(jackson, On(jacksonSong) ? 1f : 0f, dt / 5f);
            heyYa = Mathf.MoveTowards(heyYa, On(heyYaSong) ? 1f : 0f, dt / 2f);
            bob = Mathf.MoveTowards(bob, On(bobSong) ? 1f : 0f, dt / 2f);
            sabotage = Mathf.MoveTowards(sabotage, On(sabotageSong) ? 1f : 0f, dt / 1.5f);
            message = Mathf.MoveTowards(message, On(messageSong) ? 1f : 0f, dt / 5f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            var lift = t - played < 4f ? 1f - (t - played) / 4f : 0f; var strobe = bob * (Mathf.Sin(now * 30f) > 0f ? 1.6f : -.6f); var flash = heyYa * (Mathf.Repeat(now * 2.6f, 1f) < .08f ? 3f : 0f);
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (.12f + empire * 1.5f + flash * .4f + Mathf.Max(0f, strobe) * .4f));
            if (bulbs != null) bulbs.SetColor("_EmissionColor", bulbBase * Mathf.Max(.2f, 1f + empire * 1.2f + lift * .6f + strobe + flash + energy * .2f));
            if (gold != null) gold.SetColor("_EmissionColor", goldBase * (1f + juice * 1.6f + lift * .8f + cream * .8f + .06f * Mathf.Sin(t * 3f)));
            var spin = Mathf.Repeat(t * 2.4f, 1f); if (sirenRed != null) sirenRed.SetColor("_EmissionColor", new Color(1f, .1f, .1f) * (.4f + sabotage * (spin < .5f ? 3f : 0f))); if (sirenBlue != null) sirenBlue.SetColor("_EmissionColor", new Color(.1f, .3f, 1f) * (.4f + sabotage * (spin >= .5f ? 3f : 0f)));

            SkyDirector.SetSongRain(Land, jackson * .45f);
            var tint = new Color(1f, .97f, .9f); var amount = .04f;
            if (jackson > 0f) { tint = Color.Lerp(tint, new Color(.62f, .68f, .78f), jackson); amount = Mathf.Lerp(amount, .4f, jackson); }
            if (message > 0f) { tint = Color.Lerp(tint, new Color(.56f, .56f, .58f), message); amount = Mathf.Lerp(amount, .5f, message); }
            if (bees > 0f) { tint = Color.Lerp(tint, new Color(1f, .86f, .4f), bees); amount = Mathf.Lerp(amount, .16f, bees); }
            if (empire > 0f) { tint = Color.Lerp(tint, new Color(.3f, .28f, .6f), empire); amount = Mathf.Lerp(amount, .66f, empire); }
            if (flash > 0f) { tint = Color.Lerp(tint, Color.white, .5f); amount *= .5f; }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .22f + .12f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (hydrant != Vector3.zero && t >= nextWater && (hydrant - cp).sqrMagnitude < 90f * 90f)
            {
                nextWater = t + .012f;
                water.Emit(new ParticleSystem.EmitParams { position = hydrant, velocity = hydrantDir * Random.Range(6.5f, 8f) + Random.insideUnitSphere * .5f }, 1);
            }
            if (cream > 0f && t >= nextBill)
            {
                nextBill = t + .05f; var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(5f, 9f), Random.Range(-16f, 16f));
                if (!IndoorSpaces.Contains(p)) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .6f + Random.Range(-.4f, .4f), -1.3f, wind.y * .6f + Random.Range(-.4f, .4f)), startColor = new Color(.35f, .75f, .4f, .95f), startSize = Random.Range(.14f, .22f) }, 1);
            }
            if (bees > 0f && t >= nextBee)
            {
                nextBee = t + .1f; var p = cp + new Vector3(Random.Range(-7f, 7f), Random.Range(-.4f, 2.4f), Random.Range(-7f, 7f));
                dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = Random.insideUnitSphere * 2.4f, startColor = new Color(1f, .85f, .1f, 1f), startSize = .07f, startLifetime = 1.6f }, 1);
            }
            if (juice > 0f && biggieAt != Vector3.zero && t >= nextGold)
            {
                nextGold = t + .06f;
                dots.Emit(new ParticleSystem.EmitParams { position = biggieAt + new Vector3(Random.Range(-5f, 5f), Random.Range(0f, 1f), Random.Range(-5f, 5f)), velocity = new Vector3(0, Random.Range(.8f, 1.8f), 0), startColor = new Color(1f, .85f, .35f, 1f), startSize = Random.Range(.08f, .16f) }, 1);
            }
        }
    }
}
