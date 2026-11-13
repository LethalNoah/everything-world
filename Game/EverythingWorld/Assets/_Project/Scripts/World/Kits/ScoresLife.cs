using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Isle of Scores under the stars:
    /// - shooting stars cross the sky; the flying saucer bobs and turns; swans glide round the lake; fireflies drift over
    ///   the grass; the braziers send up sparks; the music stands' lights and the shell breathe with the music;
    /// - "The Imperial March": everything goes red and dark, pulsing like a march. "Star Wars": the stars blaze and the gold
    ///   comes up. "Duel of the Fates": red and blue flicker over lava orange. "This Is Halloween": orange and sickly green,
    ///   embers drift. "What's This?": snow. "Ride of the Valkyries": a storm with lightning. "Swan Lake": silver moonlight.
    ///   "L'Estasi Dell'Oro": a blaze of gold. "Let It Grow": green. The X-Files theme: the saucer's beam pulses green.
    ///   "Soarin'": a bright windy sky. Conducting from the podium lifts every light for a moment.
    public class ScoresLife : MonoBehaviour
    {
        const string Land = "The Isle of Scores";
        public Vector3 center, lake, saucerHome;
        public string marchSong, starSong, duelSong, halloweenSong, whatsSong, valkSong, swanSong, goldSong, growSong, xfileSong, soarSong;
        public Material lamps, stands, stars, windows, water, shell; public Transform saucer; public float played = -99f;
        public List<Transform> swans = new List<Transform>();
        public List<Vector3> fires = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, standBase, starBase, windowBase, waterBase, shellBase;
        float march, star, duel, hallow, whats, valk, swan, gold, grow, xfile, soar, nextFall, nextSpark, nextShoot, flash;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 18; clouds.span = 2400f; clouds.key = "scores";
            clouds.minHeight = 150f; clouds.maxHeight = 240f; clouds.size = 3f; clouds.speed = .8f; clouds.cover = .2f;
            clouds.color = new Color(.4f, .46f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); standBase = E(stands); starBase = E(stars); windowBase = E(windows); waterBase = E(water); shellBase = E(shell);
            dots = Particles("Sparks, snow and fireflies", 1400, Color.white, .05f, .16f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 7f);
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

            march = Ease(march, On(marchSong), 3f); star = Ease(star, On(starSong), 3f); duel = Ease(duel, On(duelSong), 2f); hallow = Ease(hallow, On(halloweenSong), 3f); whats = Ease(whats, On(whatsSong), 4f); valk = Ease(valk, On(valkSong), 3f); swan = Ease(swan, On(swanSong), 5f); gold = Ease(gold, On(goldSong), 4f); grow = Ease(grow, On(growSong), 4f); xfile = Ease(xfile, On(xfileSong), 3f); soar = Ease(soar, On(soarSong), 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the saucer bobs and turns; the swans glide round the lake
            if (saucer != null) { saucer.position = saucerHome + Vector3.up * Mathf.Sin(t * .7f) * .5f + new Vector3(Mathf.Sin(t * .23f), 0, Mathf.Cos(t * .31f)) * .6f; saucer.rotation = Quaternion.Euler(Mathf.Sin(t * .9f) * 4f, t * 30f, 0f); }
            for (var i = 0; i < swans.Count; i++) { var sw = swans[i]; if (sw == null || lake == Vector3.zero) continue; var a = now * .04f + i * 1.26f; var r = 3.6f + Mathf.Sin(now * .07f + i) * 2.4f; sw.position = lake + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; sw.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))); }

            var lift = t - played < 6f ? 1f - (t - played) / 6f : 0f; var beat = Mathf.Repeat(now * 1.7f, 1f);
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + lift * .5f + gold * .5f));
            if (stands != null) stands.SetColor("_EmissionColor", standBase * (.8f + .3f * energy + lift * 2f + .1f * Mathf.Sin(t * 1.3f)));
            if (stars != null) stars.SetColor("_EmissionColor", starBase * (.8f + .2f * Mathf.Sin(t * 2.1f) + star * 1.4f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + hallow * .8f));
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.8f + .2f * Mathf.Sin(t * 1.1f) + swan * .8f));
            if (shell != null) shell.SetColor("_EmissionColor", shellBase * (.9f + .3f * energy + lift * 1.4f + star * .6f));

            if (valk > .3f && Random.value < dt * .5f * valk) flash = 1f; flash = Mathf.MoveTowards(flash, 0f, dt * 4f);

            var tint = new Color(.7f, .76f, 1f); var amount = .06f;
            if (soar > 0f) { tint = Color.Lerp(tint, new Color(.5f, .8f, 1f), soar); amount = Mathf.Lerp(amount, .3f, soar); }
            if (grow > 0f) { tint = Color.Lerp(tint, new Color(.4f, 1f, .4f), grow); amount = Mathf.Lerp(amount, .3f, grow); }
            if (gold > 0f) { tint = Color.Lerp(tint, new Color(1f, .78f, .3f), gold); amount = Mathf.Lerp(amount, .42f, gold); }
            if (swan > 0f) { tint = Color.Lerp(tint, new Color(.8f, .86f, 1f), swan); amount = Mathf.Lerp(amount, .34f, swan); }
            if (star > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .6f), star); amount = Mathf.Lerp(amount, .3f, star); }
            if (xfile > 0f) { tint = Color.Lerp(tint, new Color(.4f, 1f, .6f), xfile); amount = Mathf.Lerp(amount, .3f + Mathf.Sin(t * 3f) * .06f, xfile); }
            if (hallow > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(1f, .5f, .14f), new Color(.5f, 1f, .3f), Mathf.PingPong(t * .4f, 1f)), hallow); amount = Mathf.Lerp(amount, .42f, hallow); }
            if (whats > 0f) { tint = Color.Lerp(tint, new Color(.9f, .94f, 1f), whats); amount = Mathf.Lerp(amount, .34f, whats); }
            if (duel > 0f) { tint = Color.Lerp(tint, Mathf.PerlinNoise(t * 6f, 2f) > .5f ? new Color(1f, .2f, .2f) : new Color(.3f, .5f, 1f), duel); amount = Mathf.Lerp(amount, .4f, duel); }
            if (march > 0f) { tint = Color.Lerp(tint, new Color(1f, .1f, .1f), march); amount = Mathf.Lerp(amount, beat < .2f ? .55f : .38f, march); }
            if (valk > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(.3f, .34f, .46f), Color.white, flash), valk); amount = Mathf.Lerp(amount, .46f + flash * .2f, valk); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .2f + .15f * energy + valk * 1f + soar * .6f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpark) { nextSpark = t + .15f; foreach (var f in fires) if ((f - cp).sqrMagnitude < 50f * 50f) dots.Emit(new ParticleSystem.EmitParams { position = f + new Vector3(Random.Range(-.2f, .2f), 0, Random.Range(-.2f, .2f)), velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(1f, 2f), Random.Range(-.2f, .2f)), startColor = new Color(1f, .6f, .2f), startSize = .06f, startLifetime = 2f }, 1); }
            // a shooting star now and then, more of them while the main theme plays
            if (t >= nextShoot) { nextShoot = t + Random.Range(4f, 12f) * (1f - star * .8f); var sp = cp + new Vector3(Random.Range(-120f, 120f), Random.Range(90f, 140f), Random.Range(-120f, 120f)); var v = new Vector3(Random.Range(-1f, 1f), -.3f, Random.Range(-1f, 1f)).normalized * 60f; for (var k = 0; k < 12; k++) dots.Emit(new ParticleSystem.EmitParams { position = sp - v * k * .006f, velocity = v, startColor = new Color(1f, .96f, .86f), startSize = 1.6f - k * .1f, startLifetime = 1f }, 1); }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(.5f, 3f), Random.Range(-18f, 18f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .2f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(-.05f, .15f), Random.Range(-.2f, .2f)), startColor = new Color(.8f, 1f, .5f), startSize = .06f, startLifetime = 5f }, 1);
                if (whats > .05f && Random.value < whats) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(5f, 9f), Random.Range(-18f, 18f)), velocity = new Vector3(Random.Range(-.3f, .3f), -1f, Random.Range(-.3f, .3f)), startColor = new Color(.9f, .96f, 1f), startSize = .08f, startLifetime = 8f }, 1);
                if (hallow > .05f && Random.value < hallow) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(0f, 2f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.3f, .3f), Random.Range(.4f, 1f), Random.Range(-.3f, .3f)), startColor = Random.value < .5f ? new Color(1f, .5f, .1f) : new Color(.5f, 1f, .3f), startSize = .08f, startLifetime = 5f }, 1);
                var glitter = Mathf.Max(gold, lift * .6f); if (Random.value < glitter) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(4f, 8f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.3f, .3f), -.6f, Random.Range(-.3f, .3f)), startColor = new Color(1f, .8f, .3f), startSize = .1f, startLifetime = 6f }, 1);
                if (valk > .05f && Random.value < valk) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * 3f, -9f, wind.y * 3f), startColor = new Color(.7f, .8f, .9f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
            }
        }
    }
}
