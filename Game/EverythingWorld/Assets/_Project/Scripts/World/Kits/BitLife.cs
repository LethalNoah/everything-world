using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Game Isles at a pixel sunset:
    /// - coins spin and bob everywhere; PRESS START blinks; the pixel heart on the giant cabinet beats; the cabinet screens
    ///   flicker; the wall of pixels on meganeko's stage shimmers; chickens hop round the farm; the flag slides down its pole
    ///   when grabbed and creeps back up; the Ring's blue light breathes;
    /// - "MEGALOVANIA": blue-white flashes on the beat. "One-Winged Angel": red and black. "Wii Shop Channel": calm pastel
    ///   blue. The farm's seasons: "Winter" snow, "Spring" pink petals, "Summer" warm gold, "Fall" orange leaves. "Halo
    ///   Theme": a green-gold glow and the Ring blazes. "Hunter's Dream": pale silver mist. "Bob-Omb Battlefield": bright sun.
    ///   "Living Mice": a soft green. "Sharkatzor": the pixel wall races. Every song's own light brightens while it plays.
    public class BitLife : MonoBehaviour
    {
        const string Land = "The Game Isles";
        public Vector3 center, flagTop, flagBottom;
        public string winterSong, springSong, summerSong, fallSong, sharkSong, sodaSong, shopSong, miceSong, haloSong, angelSong, dreamSong, bombSong, megaSong;
        public Material lamps, coins, screens, blocks, heart, ring, pixelWall, pixelWall2; public Transform flag; public float played = -99f, flagGrab = -99f;
        public List<TextMesh> blink = new List<TextMesh>();
        public List<(Transform t, Vector3 home)> spinners = new List<(Transform, Vector3)>();
        public List<(Transform t, Vector3 home)> chickens = new List<(Transform, Vector3)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, coinBase, screenBase, blockBase, heartBase, ringBase, wallBase, wall2Base;
        float mega, angel, shop, winter, spring, summer, fall, halo, dream, bomb, mice, shark, nextFall;
        static readonly Color[] Petals = { new Color(1f, .7f, .84f), new Color(1f, .86f, .92f) };
        static readonly Color[] Leaves = { new Color(1f, .5f, .14f), new Color(.9f, .3f, .1f), new Color(1f, .76f, .2f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 34; clouds.span = 2400f; clouds.key = "eightbit";
            clouds.minHeight = 110f; clouds.maxHeight = 200f; clouds.size = 3.4f; clouds.speed = 1.2f; clouds.cover = .35f;
            clouds.color = new Color(1f, .76f, .66f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); coinBase = E(coins); screenBase = E(screens); blockBase = E(blocks); heartBase = E(heart); ringBase = E(ring); wallBase = E(pixelWall); wall2Base = E(pixelWall2);
            dots = Particles("Snow, petals and leaves", 1200, Color.white, .08f, .2f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
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

            mega = Ease(mega, On(megaSong), 2f); angel = Ease(angel, On(angelSong), 3f); shop = Ease(shop, On(shopSong), 4f); winter = Ease(winter, On(winterSong), 4f); spring = Ease(spring, On(springSong), 4f); summer = Ease(summer, On(summerSong), 4f); fall = Ease(fall, On(fallSong), 4f); halo = Ease(halo, On(haloSong), 4f); dream = Ease(dream, On(dreamSong), 5f); bomb = Ease(bomb, On(bombSong), 3f); mice = Ease(mice, On(miceSong), 4f); shark = Ease(shark, On(sharkSong) || On(sodaSong), 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // coins spin and bob; chickens hop; the flag slides down when grabbed and creeps back up
            for (var i = 0; i < spinners.Count; i++) { var (tr, home) = spinners[i]; if (tr == null) continue; tr.position = home + Vector3.up * Mathf.Sin(t * 2f + i * .7f) * .15f; tr.rotation = Quaternion.Euler(0f, t * 140f + i * 20f, 0f); }
            for (var i = 0; i < chickens.Count; i++) { var (tr, home) = chickens[i]; if (tr == null) continue; var hop = Mathf.Max(0f, Mathf.Sin(t * 5f + i * 2f)); var wander = new Vector3(Mathf.Sin(now * .3f + i * 2f), 0, Mathf.Cos(now * .23f + i)) * .8f; tr.position = home + wander + Vector3.up * hop * .14f; tr.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(now * .3f + i * 2f), 0, -Mathf.Sin(now * .23f + i) * .3f + .01f)); }
            if (flag != null) { var since = t - flagGrab; var k = since < 1.5f ? since / 1.5f : since < 10f ? 1f - (since - 1.5f) / 8.5f : 0f; flag.position = Vector3.Lerp(flagTop, flagBottom, Mathf.Clamp01(k)); }
            var on = Mathf.Repeat(t, 1.2f) < .8f; foreach (var b in blink) if (b != null) b.gameObject.SetActive(on || t - played < 3f);

            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var beat = Mathf.Repeat(now * 2.4f, 1f);
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + lift * .4f + .15f * energy));
            if (coins != null) coins.SetColor("_EmissionColor", coinBase * (1f + lift * 1.6f + .2f * Mathf.Sin(t * 3f)));
            if (screens != null) screens.SetColor("_EmissionColor", Color.Lerp(screenBase, Color.HSVToRGB(Mathf.Repeat(t * .3f, 1f), .6f, 1f) * 1.2f, .3f + lift * .5f) * (Mathf.PerlinNoise(t * 7f, 3f) > .85f ? .5f : 1f));
            if (blocks != null) blocks.SetColor("_EmissionColor", blockBase * (.9f + .2f * Mathf.Sin(t * 2.2f) + lift));
            if (heart != null) { var hb = Mathf.Repeat(t * 1.1f, 1f); heart.SetColor("_EmissionColor", heartBase * (hb < .1f || (hb > .2f && hb < .3f) ? 1.6f : 1f) * (1f + mega * 1.4f)); }
            if (ring != null) ring.SetColor("_EmissionColor", ringBase * (.9f + .2f * Mathf.Sin(t * .8f) + halo * 1.6f));
            var race = 1f + shark * 4f + energy;
            if (pixelWall != null) pixelWall.SetColor("_EmissionColor", wallBase * (Mathf.PerlinNoise(t * race, 1f) > .4f ? 1.2f : .3f));
            if (pixelWall2 != null) pixelWall2.SetColor("_EmissionColor", wall2Base * (Mathf.PerlinNoise(t * race, 7f) > .4f ? 1.2f : .3f));

            var tint = new Color(1f, .8f, .7f); var amount = .06f;
            if (bomb > 0f) { tint = Color.Lerp(tint, new Color(1f, .96f, .7f), bomb); amount = Mathf.Lerp(amount, .3f, bomb); }
            if (summer > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .36f), summer); amount = Mathf.Lerp(amount, .34f, summer); }
            if (spring > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .86f), spring); amount = Mathf.Lerp(amount, .3f, spring); }
            if (fall > 0f) { tint = Color.Lerp(tint, new Color(1f, .56f, .2f), fall); amount = Mathf.Lerp(amount, .34f, fall); }
            if (winter > 0f) { tint = Color.Lerp(tint, new Color(.7f, .8f, 1f), winter); amount = Mathf.Lerp(amount, .38f, winter); }
            if (mice > 0f) { tint = Color.Lerp(tint, new Color(.6f, .9f, .6f), mice); amount = Mathf.Lerp(amount, .26f, mice); }
            if (shop > 0f) { tint = Color.Lerp(tint, new Color(.7f, .86f, 1f), shop); amount = Mathf.Lerp(amount, .3f, shop); }
            if (dream > 0f) { tint = Color.Lerp(tint, new Color(.7f, .72f, .8f), dream); amount = Mathf.Lerp(amount, .4f, dream); }
            if (halo > 0f) { tint = Color.Lerp(tint, new Color(.6f, 1f, .6f), halo); amount = Mathf.Lerp(amount, .34f, halo); }
            if (angel > 0f) { tint = Color.Lerp(tint, new Color(.8f, .1f, .14f), angel); amount = Mathf.Lerp(amount, .5f, angel); }
            if (mega > 0f) { tint = Color.Lerp(tint, beat < .15f ? new Color(.9f, .96f, 1f) : new Color(.2f, .4f, 1f), mega); amount = Mathf.Lerp(amount, beat < .15f ? .55f : .36f, mega); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .15f * energy + fall * .5f + winter * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(5f, 10f), Random.Range(-18f, 18f));
                if (IndoorSpaces.Contains(p)) return;
                if (winter > .05f && Random.value < winter) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(5f, 9f), Random.Range(-18f, 18f)), velocity = new Vector3(Random.Range(-.3f, .3f), -1f, Random.Range(-.3f, .3f)), startColor = Color.white, startSize = .12f, startLifetime = 8f }, 1);
                if (spring > .05f && Random.value < spring) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .6f, -.6f, wind.y * .6f), startColor = Petals[Random.Range(0, Petals.Length)], startSize = .14f, startLifetime = 8f }, 1);
                if (fall > .05f && Random.value < fall) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .8f, -.8f, wind.y * .8f), startColor = Leaves[Random.Range(0, Leaves.Length)], startSize = .18f, startLifetime = 8f }, 1);
                var sparkle = Mathf.Max(lift, halo * .5f); if (Random.value < sparkle) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(.5f, 5f), Random.Range(-14f, 14f)), velocity = new Vector3(0, .4f, 0), startColor = new Color(1f, .86f, .3f), startSize = .12f, startLifetime = 3f }, 1);
            }
        }
    }
}
