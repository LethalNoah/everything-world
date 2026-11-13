using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Velvet Rooftops, late at night:
    /// - the pool shimmers; the neon breathes, slowly, a little quicker with the music; the red lights on the masts
    ///   and the drowned towers blink; the glass bridges glow; a few thin clouds, many stars; a waterfall runs off
    ///   TLC's tank; rain falls under Rihanna's umbrella and nowhere else;
    /// - "Umbrella": it rains on the whole city. "Diamonds": diamonds of light fall slowly out of the sky. "SOS": every
    ///   red light in the city blinks three short, three long, three short. "Blinding Lights": the night goes white
    ///   and red. "Pink + White": the sky turns pink. "Good Days": a soft dawn colour comes up. "Kill Bill": the city
    ///   goes red. "I Believe I Can Fly": feathers rise. Every song's own light brightens while it plays.
    public class VelvetLife : MonoBehaviour
    {
        const string Land = "The Velvet Rooftops";
        public Vector3 center, umbrellaAt, waterfallAt, flyAt;
        public string umbrellaSong, diamondsSong, sosSong, blindingSong, pinkSong, goodDaysSong, killBillSong, flySong;
        public Material pool, beacons, bridges; public Material[] neon, windows; public Material blinding; public float played = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem drops, dots;
        Color poolBase, beaconBase, bridgeBase, blindBase; Color[] neonBase, windowBase;
        float umbrella, diamonds, sos, blind, pink, good, kill, fly, nextDrop, nextFall, nextDiamond, nextFeather;
        // S O S: three short, three long, three short, a gap
        static readonly float[] Morse = { 1, 0, 1, 0, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0 };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 24; clouds.span = 2600f; clouds.key = "velvet";
            clouds.minHeight = 190f; clouds.maxHeight = 280f; clouds.size = 3f; clouds.speed = 1.6f; clouds.cover = .2f;
            clouds.color = new Color(.36f, .26f, .46f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            poolBase = E(pool); beaconBase = E(beacons); bridgeBase = E(bridges); blindBase = E(blinding);
            neonBase = new Color[neon != null ? neon.Length : 0]; for (var i = 0; i < neonBase.Length; i++) neonBase[i] = E(neon[i]);
            windowBase = new Color[windows != null ? windows.Length : 0]; for (var i = 0; i < windowBase.Length; i++) windowBase[i] = E(windows[i]);
            drops = Particles("Rain and water", 700, new Color(.75f, .88f, 1f, .7f), .04f, .08f, false);
            var dm = drops.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(.9f, 1.3f); dm.gravityModifier = 1f;
            dots = Particles("Diamonds and feathers", 500, Color.white, .08f, .16f, true);
            var tm = dots.main; tm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
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

            umbrella = Mathf.MoveTowards(umbrella, On(umbrellaSong) ? 1f : 0f, dt / 4f);
            diamonds = Mathf.MoveTowards(diamonds, On(diamondsSong) ? 1f : 0f, dt / 3f);
            sos = Mathf.MoveTowards(sos, On(sosSong) ? 1f : 0f, dt / 2f);
            blind = Mathf.MoveTowards(blind, On(blindingSong) ? 1f : 0f, dt / 2f);
            pink = Mathf.MoveTowards(pink, On(pinkSong) ? 1f : 0f, dt / 5f);
            good = Mathf.MoveTowards(good, On(goodDaysSong) ? 1f : 0f, dt / 6f);
            kill = Mathf.MoveTowards(kill, On(killBillSong) ? 1f : 0f, dt / 3f);
            fly = Mathf.MoveTowards(fly, On(flySong) ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var rate = .35f + energy * .5f + lift * .8f;
            if (neon != null) for (var i = 0; i < neon.Length; i++) if (neon[i] != null) neon[i].SetColor("_EmissionColor", neonBase[i] * (.75f + .3f * Mathf.Sin(now * rate * 2f + i * 1.3f) + lift * .5f + blind * .8f));
            if (pool != null) pool.SetColor("_EmissionColor", poolBase * (.9f + .12f * Mathf.Sin(t * 1.3f) + .06f * Mathf.Sin(t * 3.1f) + lift * .3f));
            if (bridges != null) bridges.SetColor("_EmissionColor", bridgeBase * (.85f + .15f * Mathf.Sin(t * .7f) + diamonds * .8f));
            var morse = Morse[Mathf.FloorToInt(Mathf.Repeat(now * 4f, Morse.Length))]; var blink = Mathf.Repeat(now * .7f, 1f) < .5f ? 1f : .15f;
            if (beacons != null) beacons.SetColor("_EmissionColor", beaconBase * Mathf.Lerp(blink, morse * 1.8f, sos));
            if (windows != null) for (var i = 0; i < windows.Length; i++) if (windows[i] != null) windows[i].SetColor("_EmissionColor", windowBase[i] * (1f + .05f * Mathf.Sin(t * .9f + i) + blind * .7f + good * .3f));
            if (blinding != null) blinding.SetColor("_EmissionColor", blindBase * (1f + blind * (2.6f + 1.2f * Mathf.Sin(now * 9f))));

            SkyDirector.SetSongRain(Land, umbrella * .55f);
            var tint = new Color(.6f, .45f, .8f); var amount = .06f;
            if (umbrella > 0f) { tint = Color.Lerp(tint, new Color(.3f, .34f, .5f), umbrella); amount = Mathf.Lerp(amount, .35f, umbrella); }
            if (good > 0f) { tint = Color.Lerp(tint, new Color(1f, .75f, .55f), good); amount = Mathf.Lerp(amount, .38f, good); }
            if (pink > 0f) { tint = Color.Lerp(tint, new Color(1f, .55f, .75f), pink); amount = Mathf.Lerp(amount, .5f, pink); }
            if (kill > 0f) { tint = Color.Lerp(tint, new Color(1f, .15f, .15f), kill); amount = Mathf.Lerp(amount, .4f, kill); }
            if (blind > 0f) { tint = Color.Lerp(tint, new Color(1f, .7f, .7f), blind); amount = Mathf.Lerp(amount, .4f + .1f * Mathf.Sin(now * 9f), blind); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .15f + .1f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextDrop && umbrellaAt != Vector3.zero && (umbrellaAt - cp).sqrMagnitude < 70f * 70f)
            {
                nextDrop = t + .02f; var a = Random.Range(0f, Mathf.PI * 2f);
                drops.Emit(new ParticleSystem.EmitParams { position = umbrellaAt + new Vector3(Mathf.Cos(a) * 2.5f, 0, Mathf.Sin(a) * 2.5f), velocity = Vector3.down * 2f }, 1);
            }
            if (t >= nextFall && waterfallAt != Vector3.zero && (waterfallAt - cp).sqrMagnitude < 70f * 70f)
            {
                nextFall = t + .015f;
                drops.Emit(new ParticleSystem.EmitParams { position = waterfallAt + new Vector3(Random.Range(-.6f, .6f), 0, Random.Range(-.1f, .1f)), velocity = new Vector3(0, -1f, 0), startColor = new Color(.6f, .95f, 1f, .8f), startSize = .1f }, 1);
            }
            if (diamonds > 0f && t >= nextDiamond)
            {
                nextDiamond = t + .05f; var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(4f, 12f), Random.Range(-18f, 18f));
                dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(0, -.7f, 0), startColor = new Color(.8f, .95f, 1f, 1f), startSize = Random.Range(.1f, .22f) }, 1);
            }
            if (fly > 0f && flyAt != Vector3.zero && t >= nextFeather)
            {
                nextFeather = t + .08f;
                dots.Emit(new ParticleSystem.EmitParams { position = flyAt + new Vector3(Random.Range(-3f, 3f), Random.Range(0f, 1f), Random.Range(-3f, 3f)), velocity = new Vector3(wind.x * .3f, Random.Range(.6f, 1.4f), wind.y * .3f), startColor = new Color(1f, 1f, .95f, .9f), startSize = Random.Range(.1f, .18f) }, 1);
            }
        }
    }
}
