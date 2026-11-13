using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Liquid Rapids on a summer evening, on a rolling drum-and-bass clock (174 bpm, shared, so friends see the same
    /// thing):
    /// - the water's glow ripples, foam drifts down the river, spray rises at every fall and cascade, fireflies come out
    ///   over the grass, the mill wheel turns, the pendulum swings, the rope swing sways; lamps pulse softly on the beat;
    /// - "Everybody Loves The Sunshine": a deep gold glow. "Witchcraft": a purple storm with rain. "Afterglow": an
    ///   orange-pink sky. "Inner City Life": city gold. "Life Is Beautiful": soft pastels. "On Fire": orange with sparks.
    ///   "Peace Within Colors": the light cycles through the rainbow. "Desire": a deep red. Every song's own light
    ///   brightens while it plays.
    public class RapidsLife : MonoBehaviour
    {
        const string Land = "The Liquid Rapids";
        public Vector3 center, swingHome;
        public string sunshineSong, witchSong, afterglowSong, cityLifeSong, beautifulSong, fireSong, colorsSong, desireSong;
        public Material water, foam, lamps; public float played = -99f;
        public Transform swing, wheel; public Quaternion wheelRot = Quaternion.identity;
        public List<Vector3> sprays = new List<Vector3>(), river = new List<Vector3>(), fires = new List<Vector3>(), speakers = new List<Vector3>();
        public List<Transform> spinners = new List<Transform>();
        public List<(Transform t, Vector3 home)> bobbers = new List<(Transform, Vector3)>();
        public List<(Transform t, Quaternion home)> pendulums = new List<(Transform, Quaternion)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color waterBase, foamBase, lampBase;
        float sunshine, witch, afterglow, city, beautiful, fire, colors, desire, nextSpray, nextFall, wheelA;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 34; clouds.span = 2600f; clouds.key = "rapids";
            clouds.minHeight = 120f; clouds.maxHeight = 200f; clouds.size = 3.4f; clouds.speed = .7f; clouds.cover = .4f;
            clouds.color = new Color(1f, .78f, .66f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            waterBase = E(water); foamBase = E(foam); lampBase = E(lamps);
            dots = Particles("Spray, foam and fireflies", 1600, Color.white, .05f, .16f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 6f);
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

            sunshine = Ease(sunshine, On(sunshineSong), 4f); witch = Ease(witch, On(witchSong), 3f); afterglow = Ease(afterglow, On(afterglowSong), 4f); city = Ease(city, On(cityLifeSong), 4f);
            beautiful = Ease(beautiful, On(beautifulSong), 4f); fire = Ease(fire, On(fireSong), 3f); colors = Ease(colors, On(colorsSong), 4f); desire = Ease(desire, On(desireSong), 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the drum-and-bass clock: 174 bpm, the lamps breathe on every other beat
            var beatT = now * 174f / 60f; var beat = Mathf.Repeat(beatT / 2f, 1f); var kick = Mathf.Exp(-beat * 5f); var splash = t - played < 4f ? 1f - (t - played) / 4f : 0f;
            wheelA += dt * (40f + energy * 30f + witch * 60f); if (wheel != null) wheel.rotation = wheelRot * Quaternion.Euler(wheelA, 0, 0);
            if (swing != null) swing.rotation = Quaternion.Euler(Mathf.Sin(now * 1.1f) * (6f + splash * 30f), 0, Mathf.Sin(now * .7f) * 4f);
            foreach (var s in spinners) if (s != null) s.Rotate(0, 0, dt * (14f + energy * 20f), Space.Self);
            for (var i = 0; i < bobbers.Count; i++) { var (tr, home) = bobbers[i]; if (tr != null) tr.position = home + Vector3.up * Mathf.Sin(t * .8f + i) * .15f; }
            foreach (var (tr, home) in pendulums) if (tr != null) tr.rotation = home * Quaternion.Euler(0, 0, Mathf.Sin(now * Mathf.PI * 174f / 60f / 4f) * (24f + witch * 16f));

            if (water != null) water.SetColor("_EmissionColor", waterBase * (.85f + Mathf.Sin(now * 1.3f) * .1f + kick * .12f + sunshine * .3f));
            if (foam != null) foam.SetColor("_EmissionColor", foamBase * (.8f + Mathf.Sin(now * 2.1f) * .15f + kick * .15f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + kick * .15f + city * .5f + fire * .3f));

            var tint = new Color(1f, .74f, .56f); var amount = .14f;
            if (sunshine > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .4f), sunshine); amount = Mathf.Lerp(amount, .36f, sunshine); }
            if (afterglow > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(1f, .5f, .4f), new Color(.9f, .5f, .8f), Mathf.PingPong(now * .04f, 1f)), afterglow); amount = Mathf.Lerp(amount, .4f, afterglow); }
            if (city > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .3f), city); amount = Mathf.Lerp(amount, .34f, city); }
            if (beautiful > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(1f, .8f, .9f), new Color(.8f, .9f, 1f), Mathf.PingPong(now * .05f, 1f)), beautiful); amount = Mathf.Lerp(amount, .3f, beautiful); }
            if (fire > 0f) { tint = Color.Lerp(tint, new Color(1f, .45f, .15f), fire); amount = Mathf.Lerp(amount, .42f, fire); }
            if (colors > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .04f, 1f), .6f, 1f), colors); amount = Mathf.Lerp(amount, .36f, colors); }
            if (desire > 0f) { tint = Color.Lerp(tint, new Color(.8f, .1f, .2f), desire); amount = Mathf.Lerp(amount, .42f, desire); }
            if (witch > 0f) { tint = Color.Lerp(tint, new Color(.42f, .26f, .7f), witch); amount = Mathf.Lerp(amount, .46f, witch); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, witch);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .15f * energy + witch * 1f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpray)
            {
                nextSpray = t + .1f;
                foreach (var s in sprays) if ((s - cp).sqrMagnitude < 50f * 50f) dots.Emit(new ParticleSystem.EmitParams { position = s + new Vector3(Random.Range(-.6f, .6f), 0, Random.Range(-.6f, .6f)), velocity = new Vector3(Random.Range(-.4f, .4f), Random.Range(.6f, 1.6f), Random.Range(-.4f, .4f)), startColor = new Color(.86f, .94f, 1f, .35f), startSize = Random.Range(.2f, .5f), startLifetime = 1.6f }, 1);
                for (var i = 0; i < river.Count - 1; i += 3) if ((river[i] - cp).sqrMagnitude < 40f * 40f && Random.value < .3f) { var flow = (river[i + 1] - river[i]).normalized; dots.Emit(new ParticleSystem.EmitParams { position = river[i] + Vector3.up * .12f + new Vector3(Random.Range(-1.6f, 1.6f), 0, Random.Range(-1.6f, 1.6f)), velocity = flow * 2.4f, startColor = new Color(.9f, .96f, 1f, .5f), startSize = .14f, startLifetime = 3f }, 1); }
                foreach (var f in fires) if ((f - cp).sqrMagnitude < 40f * 40f && Random.value < .4f + fire) dots.Emit(new ParticleSystem.EmitParams { position = f, velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(1.4f, 2.6f), Random.Range(-.3f, .3f)), startColor = new Color(1f, .6f, .2f), startSize = .06f, startLifetime = 1.4f }, 1);
                foreach (var sp in speakers) if ((sp - cp).sqrMagnitude < 30f * 30f && beat < .1f) dots.Emit(new ParticleSystem.EmitParams { position = sp, velocity = (sp - center).normalized * .2f + Vector3.up * .3f, startColor = new Color(1f, .8f, .4f, .4f), startSize = .3f, startLifetime = .6f }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                if (Random.value < .45f) { var p = cp + new Vector3(Random.Range(-20f, 20f), Random.Range(.3f, 2.4f), Random.Range(-20f, 20f)); dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(-.1f, .2f), Random.Range(-.3f, .3f)), startColor = colors > .1f ? Color.HSVToRGB(Random.value, .7f, 1f) : new Color(1f, .9f, .4f), startSize = .07f, startLifetime = 4f }, 1); }
                if (fire > .05f && Random.value < fire) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(.2f, 1f), Random.Range(-14f, 14f)), velocity = Vector3.up * Random.Range(1f, 2.4f), startColor = new Color(1f, .5f, .2f), startSize = .06f, startLifetime = 2f }, 1);
                if (witch > .05f && Random.value < witch) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * 2f, -10f, wind.y * 2f), startColor = new Color(.7f, .66f, .9f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
            }
        }
    }
}
