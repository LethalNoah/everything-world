using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Hair Metal Strip at electric dusk, answering the music:
    /// - motorbikes cruise up and down the Strip with their headlights on (where they are comes from the shared
    ///   clock, so friends see the same bikes); every venue's marquee bulbs chase, faster in loud songs; speaker
    ///   cones pulse; the stars spin; the Club's pyro jets fire now and then in loud songs;
    /// - now and then (shared clock) the giant hairspray cans puff clouds of pink and gold glitter;
    /// - "Kickstart My Heart": the Club's pyro jets roar the whole song and flames burst along the Strip;
    ///   "November Rain": rain falls, the light goes blue, rose petals drift down over the Guns N' Roses stage;
    ///   "The Final Countdown": the red display counts down from ten, then fireworks burst over the Club;
    ///   "Livin' On A Prayer": every marquee blazes and pink and gold confetti falls along the Strip.
    public partial class HairLife : MonoBehaviour
    {
        const string Land = "The Hair Metal Strip";
        public Vector3 center, streetA, streetDir = Vector3.forward, club, glitterBall, roseStage, asteroid;
        public float streetLen = 300f; public float[] profile = new float[0];
        public List<Vector3> cans = new List<Vector3>(), pyro = new List<Vector3>();
        public List<Material> marqueeBulbs = new List<Material>(); public List<int> marqueeStarts = new List<int>();
        public List<Material> speakerCones = new List<Material>();
        public List<Transform> spinners = new List<Transform>();
        public Material[] countdown = new Material[0];
        // filled while building: each venue's bulbs (flattened into marqueeBulbs/marqueeStarts)
        public List<Material[]> marquees { get { return marqueesBuild; } }
        readonly List<Material[]> marqueesBuild = new List<Material[]>();

        CloudLayer clouds;
        ParticleSystem flame, glitter, petals, confetti, fireworks;
        readonly List<Transform> bikes = new List<Transform>();
        float kick, rain, count, prayer, mood = .5f;
        long songKey = long.MinValue; double songStart, songLength = 1, glitterAt = -1;
        float nextFlame, nextGlitter, nextPetal, nextConfetti, nextFirework, nextPyro;

        static readonly int[][] Digits =
        {
            new[] { 0, 1, 2, 3, 4, 5 }, new[] { 1, 2 }, new[] { 0, 1, 6, 4, 3 }, new[] { 0, 1, 6, 2, 3 }, new[] { 5, 6, 1, 2 },
            new[] { 0, 5, 6, 2, 3 }, new[] { 0, 5, 6, 4, 3, 2 }, new[] { 0, 1, 2 }, new[] { 0, 1, 2, 3, 4, 5, 6 }, new[] { 0, 1, 2, 3, 5, 6 },
        };

        /// Called by HairKit after every venue is built, so the bulbs survive being saved with the scene.
        public void Flatten()
        {
            marqueeBulbs.Clear(); marqueeStarts.Clear();
            foreach (var m in marqueesBuild) { marqueeStarts.Add(marqueeBulbs.Count); marqueeBulbs.AddRange(m); }
        }

        void Start()
        {
            if (marqueeStarts.Count == 0 && marqueesBuild.Count > 0) Flatten();
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 50; clouds.span = 2400f; clouds.key = "hair";
            clouds.minHeight = 110f; clouds.maxHeight = 170f; clouds.size = 1.5f; clouds.speed = 4f; clouds.cover = .35f;
            clouds.color = new Color(.85f, .45f, .7f);
            flame = Particles("Pyro", 2500, new Color(1f, .55f, .2f, .95f), .8f, 2f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(10f, 18f);
            var fs = flame.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 8f; fs.radius = .3f; fs.rotation = new Vector3(-90f, 0, 0);
            glitter = Particles("Glitter", 2500, new Color(1f, .5f, .85f, 1f), .12f, .3f, true);
            var gm = glitter.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); gm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); gm.gravityModifier = .05f;
            var gs = glitter.shape; gs.shapeType = ParticleSystemShapeType.Cone; gs.angle = 25f; gs.radius = .2f;
            var gc = glitter.colorOverLifetime; var gg = new Gradient();
            gg.SetKeys(new[] { new GradientColorKey(new Color(1f, .4f, .8f), 0f), new GradientColorKey(new Color(1f, .85f, .3f), .5f), new GradientColorKey(new Color(.6f, .9f, 1f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .6f), new GradientAlphaKey(0f, 1f) });
            gc.color = gg;
            petals = Particles("Rose petals", 800, new Color(.9f, .1f, .2f, 1f), .2f, .35f, false);
            var pm = petals.main; pm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f); pm.startSpeed = 0f; pm.gravityModifier = .04f;
            var pn = petals.noise; pn.enabled = true; pn.strength = .8f; pn.frequency = .3f;
            confetti = Particles("Confetti", 2500, new Color(1f, .4f, .8f, 1f), .15f, .3f, false);
            var cm = confetti.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); cm.startSpeed = 0f; cm.gravityModifier = .08f;
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .3f, .7f), new Color(1f, .85f, .2f));
            var cn = confetti.noise; cn.enabled = true; cn.strength = .6f;
            fireworks = Particles("Fireworks", 3000, Color.white, .25f, .45f, true);
            var wm = fireworks.main; wm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f); wm.startSpeed = new ParticleSystem.MinMaxCurve(12f, 20f); wm.gravityModifier = .4f;
            var ws = fireworks.shape; ws.shapeType = ParticleSystemShapeType.Sphere; ws.radius = .5f;
            Color[] bikeCols = { new Color(.08f, .08f, .1f), new Color(.85f, .1f, .5f), new Color(.9f, .75f, .2f) };
            for (var i = 0; i < 3; i++) bikes.Add(MakeBike(bikeCols[i]));
        }

        Transform MakeBike(Color paint)
        {
            var mk = new MeshKit(); var body = WorldMaterials.Solid(paint, .9f); var black = WorldMaterials.Solid(new Color(.06f, .06f, .07f)); var chrome = WorldMaterials.Solid(new Color(.85f, .86f, .9f), .95f);
            foreach (var z in new[] { -.8f, .85f }) mk.Prism(black, new Vector3(-.07f, .36f, z), Vector3.right, .36f, .14f, 12);
            mk.Box(body, new Vector3(0, .8f, .2f), new Vector3(.32f, .28f, .62f), Quaternion.identity);
            mk.Box(black, new Vector3(0, .82f, -.38f), new Vector3(.3f, .1f, .65f), Quaternion.identity);
            mk.Box(chrome, new Vector3(0, .55f, 0), new Vector3(.26f, .3f, .42f), Quaternion.identity);
            mk.Box(chrome, new Vector3(0, 1.15f, .7f), new Vector3(.95f, .05f, .05f), Quaternion.identity);
            mk.Box(black, new Vector3(0, 1.4f, -.3f), new Vector3(.4f, .7f, .3f), Quaternion.identity);            // the rider
            mk.Blob(black, new Vector3(0, 1.95f, -.25f), Vector3.one * .18f, new System.Random(1), 0f);
            mk.Prism(WorldMaterials.Glow(new Color(1f, .95f, .8f), 2.6f), new Vector3(0, .9f, .95f), Vector3.forward, .12f, .05f, 10);
            var t = mk.BuildSingle("Cruising bike", transform, false).transform;
            var head = new GameObject("Headlight").AddComponent<Light>(); head.transform.SetParent(t, false);
            head.type = LightType.Spot; head.spotAngle = 50f; head.range = 22f; head.intensity = 2.5f; head.color = new Color(1f, .93f, .8f);
            head.transform.localPosition = new Vector3(0, .9f, 1f); head.transform.localRotation = Quaternion.Euler(10f, 0, 0);
            return t;
        }

        static Material Additive(string name)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool add)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, add ? Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f) : (d < .8f ? 1f : 0f))); }
            tex.Apply();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = add ? Additive(name) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            if (!add) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; }
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        float StreetY(float t)
        {
            if (profile.Length == 0) return center.y;
            var f = Mathf.Clamp(t / 8f, 0, profile.Length - 1.001f); var i = Mathf.FloorToInt(f);
            return Mathf.Lerp(profile[i], profile[Mathf.Min(i + 1, profile.Length - 1)], f - i);
        }

        Vector3 StreetPoint(float t, float across) { var p = streetA + streetDir * t + Vector3.Cross(Vector3.up, streetDir) * across; p.y = StreetY(t); return p; }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var valence = playing ? d.CurrentSlot.track.valence : .6f;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey)
            {
                songKey = song; songStart = start; songLength = System.Math.Max(1, length);
                var k = SharedChance.Key("hair-glitter");
                glitterAt = SharedChance.Roll(k, song, .5) ? start + length * .4 * SharedChance.Value(k, song, 1) : -1;
            }
            var elapsed = (float)(now - songStart);
            if (MusicDirector.ForcedTrackId != null) elapsed = Mathf.Repeat(t, 40f);

            kick = Mathf.MoveTowards(kick, here && id == HairKit.Kickstart ? 1f : 0f, dt / 1.5f);
            rain = Mathf.MoveTowards(rain, here && id == HairKit.NovemberRain ? 1f : 0f, dt / 4f);
            count = Mathf.MoveTowards(count, here && id == HairKit.FinalCountdown ? 1f : 0f, dt / 1.5f);
            prayer = Mathf.MoveTowards(prayer, here && id == HairKit.Prayer ? 1f : 0f, dt / 2f);
            mood = Mathf.MoveTowards(mood, Mathf.Clamp01(valence * 1.2f), dt / 4f);

            var tint = Color.Lerp(new Color(.7f, .45f, 1f), new Color(1f, .5f, .7f), mood); var amount = .2f;
            if (rain > 0f) { tint = Color.Lerp(tint, new Color(.35f, .45f, .8f), rain); amount = Mathf.Lerp(amount, .5f, rain); }
            if (kick > 0f) { tint = Color.Lerp(tint, new Color(1f, .45f, .2f), kick * .7f); amount = Mathf.Max(amount, .4f * kick); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, rain * .8f);
            if (here)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                Shader.SetGlobalFloat("_EW_Wind", .2f + .2f * energy);
                Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            }

            // bikes cruising the Strip, up one side and back down the other
            for (var i = 0; i < bikes.Count; i++)
            {
                bikes[i].gameObject.SetActive(here);
                if (!here) continue;
                var loop = streetLen * 2f; var dd = (float)((now * (9f + i * 1.7f) + i * streetLen * .7f) % loop);
                var goingUp = dd < streetLen; var tt = goingUp ? dd : loop - dd;
                var p = StreetPoint(Mathf.Clamp(tt, 3f, streetLen - 3f), goingUp ? 3f : -3f);
                bikes[i].SetPositionAndRotation(p + Vector3.up * .05f, Quaternion.LookRotation(goingUp ? streetDir : -streetDir));
            }

            // marquees chase; prayer blazes them all
            if (marqueeStarts.Count > 0)
            {
                var chase = Mathf.FloorToInt(t * (4f + 8f * energy));
                for (var v = 0; v < marqueeStarts.Count; v++)
                {
                    var s0 = marqueeStarts[v]; var s1 = v + 1 < marqueeStarts.Count ? marqueeStarts[v + 1] : marqueeBulbs.Count; var n = s1 - s0;
                    for (var k = 0; k < n; k++) marqueeBulbs[s0 + k].SetColor("_EmissionColor", new Color(1f, .9f, .6f) * ((chase + v) % 4 == k % 4 ? 3f : .6f + prayer * 2.5f));
                }
            }
            foreach (var c in speakerCones) c.SetColor("_EmissionColor", new Color(.3f, .9f, 1f) * (.3f + energy * 1.5f * (.5f + .5f * Mathf.Sin(t * 12f))));
            foreach (var s in spinners) if (s != null) s.Rotate(0, 0, 40f * dt, Space.Self);

            // the countdown display: ten to zero over the first twenty seconds, then 00 flashing and fireworks
            if (countdown.Length >= 14)
            {
                var value = count > .01f ? Mathf.Max(0, 10 - Mathf.FloorToInt(elapsed / 2f)) : -1;
                var flash = value == 0 && Mathf.Repeat(t, .6f) < .3f;
                for (var dg = 0; dg < 2; dg++)
                {
                    var digit = value < 0 ? -1 : dg == 0 ? value / 10 : value % 10;
                    for (var s = 0; s < 7; s++)
                    {
                        var on = digit >= 0 && System.Array.IndexOf(Digits[digit], s) >= 0 && !flash;
                        countdown[dg * 7 + s].SetColor("_EmissionColor", new Color(1f, .1f, .08f) * (on ? 3.2f : .12f));
                    }
                }
                nextFirework -= dt;
                if (here && value == 0 && nextFirework <= 0f)
                {
                    nextFirework = Random.Range(.25f, .6f);
                    fireworks.transform.position = club + Vector3.up * Random.Range(40f, 70f) + new Vector3(Random.Range(-30f, 30f), 0, Random.Range(-30f, 30f));
                    var em = fireworks.main; em.startColor = new[] { new Color(1f, .3f, .7f), new Color(1f, .85f, .3f), new Color(.4f, .9f, 1f), new Color(.75f, .5f, 1f) }[Random.Range(0, 4)];
                    fireworks.Emit(120);
                }
            }

            // pyro: now and then in loud songs, the whole of Kickstart My Heart
            nextPyro -= dt;
            if (here && nextPyro <= 0f && pyro.Count > 0)
            {
                var burst = kick > .3f || (energy > .75f && Mathf.Repeat(t, 8f) < .8f);
                nextPyro = .06f;
                if (burst) foreach (var p in pyro) { flame.transform.position = p; flame.Emit(kick > .3f ? 10 : 5); }
            }
            nextFlame -= dt;
            if (here && kick > .3f && nextFlame <= 0f)
            {
                nextFlame = .15f;
                var tt = Random.Range(10f, streetLen - 10f); flame.transform.position = StreetPoint(tt, Random.value < .5f ? -8.5f : 8.5f); flame.Emit(12);
            }

            // glitter from the hairspray cans
            nextGlitter -= dt;
            var gt = (float)(now - glitterAt); var puff = (glitterAt > 0 && gt > 0f && gt < 25f) || prayer > .3f;
            if (here && puff && nextGlitter <= 0f && cans.Count > 0)
            {
                nextGlitter = .08f;
                foreach (var c in cans) { glitter.transform.position = c; glitter.transform.rotation = Quaternion.LookRotation(Vector3.forward + Vector3.up * .6f); glitter.Emit(6); }
            }

            // November Rain: petals over the rose stage; Prayer: confetti along the Strip
            nextPetal -= dt;
            if (here && rain > .2f && nextPetal <= 0f && roseStage != Vector3.zero)
            {
                nextPetal = .05f;
                petals.transform.position = roseStage + new Vector3(Random.Range(-14f, 14f), Random.Range(10f, 16f), Random.Range(-14f, 14f)); petals.Emit(2);
            }
            nextConfetti -= dt;
            if (here && prayer > .2f && nextConfetti <= 0f)
            {
                nextConfetti = .03f;
                var cam = Camera.main != null ? Camera.main.transform.position : center;
                confetti.transform.position = cam + new Vector3(Random.Range(-25f, 25f), Random.Range(10f, 18f), Random.Range(-25f, 25f)); confetti.Emit(3);
            }
            SongGlowUpdate(dt, t, id, here);
        }
    }
}
