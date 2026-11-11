using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bayou on a hazy golden afternoon, answering the music:
    /// - warm low cloud, fireflies drifting everywhere (thicker in quiet songs), campfires crackling, the paddle
    ///   steamer's wheel turning at its landing, the bonfire on the hill smouldering;
    /// - now and then (the shared clock decides, so friends see the same) an alligator surfaces and cruises a
    ///   stretch of the river;
    /// - "Free Bird": a flock lifts off the porch roof and wheels up and away over the swamp; "Bad Moon Rising": a
    ///   huge moon rises over the trees and the light turns strange; "(Ghost) Riders in the Sky": pale riders
    ///   gallop across the sky; "Proud Mary": the steamer casts off and rolls down the river and back;
    ///   "Fire on the Mountain": the bonfire on the hill roars up, sparks pouring into the sky; "Born on the
    ///   Bayou": the haze thickens and the fireflies come out in thousands.
    public partial class BayouLife : MonoBehaviour
    {
        const string Land = "The Southern Rock Bayou";
        public Vector3 center, mountainFire, rainbow, freeBirds, moonDir = new Vector3(.6f, 0, .8f);
        public Vector3[] river = new Vector3[0];
        public List<Vector3> pools = new List<Vector3>(), fires = new List<Vector3>(), bridges = new List<Vector3>(), herons = new List<Vector3>();
        public List<Renderer> lanterns = new List<Renderer>();
        public Light mountainLight;
        public Transform steamer, steamerWheel;
        public int steamerIndex;
        public (Vector3 hang, Vector3 seat) ropeSwing;
        // the Wisp Swamp (BayouSwamp)
        public Vector3[] swampPath = new Vector3[0];
        public List<Vector3> swampPools = new List<Vector3>();    // (x, radius, z)
        public Vector3 oldTree, wreck; public float swampY;

        ParticleSystem wispPs; ParticleSystem.Particle[] wispParts;
        Vector3[] wispHome = new Vector3[0], wispPos = new Vector3[0]; float[] wispSeed = new float[0];
        Light[] guideLights = new Light[0];
        float elizabeth, gloom; const int Wisps = 46, Guides = 5;

        CloudLayer clouds;
        ParticleSystem flies, flame, sparks;
        readonly List<Transform> flock = new List<Transform>(), riders = new List<Transform>();
        Transform moon, gator;
        float freeBird, badMoon, ghost, proud, mountain, bayou, steamerPos;
        long songKey = long.MinValue; double songStart, songLength = 1, gatorAt = -1; int gatorIndex;
        float nextFly, nextFlame, nextSpark;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 80; clouds.span = 2400f; clouds.key = "bayou";
            clouds.minHeight = 90f; clouds.maxHeight = 150f; clouds.size = 1.5f; clouds.speed = 3f; clouds.cover = .5f;
            clouds.color = new Color(1f, .9f, .72f);
            flies = Particles("Fireflies", 3000, new Color(.85f, 1f, .35f, 1f), .12f, .22f, true);
            var fm = flies.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f); fm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f);
            var fn = flies.noise; fn.enabled = true; fn.strength = .6f; fn.frequency = .4f;
            var fc = flies.colorOverLifetime; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(.2f, .45f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
            fc.color = g;
            flame = Particles("Campfire flames", 1500, new Color(1f, .55f, .2f, .9f), .3f, .8f, true);
            var lm = flame.main; lm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1f); lm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var ls = flame.shape; ls.shapeType = ParticleSystemShapeType.Cone; ls.angle = 12f; ls.radius = .4f; ls.rotation = new Vector3(-90f, 0, 0);
            sparks = Particles("Sparks", 1500, new Color(1f, .7f, .3f, 1f), .08f, .18f, true);
            var sm = sparks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f); sm.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
            var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Cone; ss.angle = 20f; ss.radius = 1f; ss.rotation = new Vector3(-90f, 0, 0);

            var bird = WorldMaterials.Solid(new Color(.12f, .12f, .14f));
            for (var i = 0; i < 26; i++)
            {
                var mk = new MeshKit();
                foreach (var s in new[] { -1f, 1f }) { mk.Tri(bird, new Vector3(0, 0, .35f), new Vector3(s * 1f, .15f, -.1f), new Vector3(0, 0, -.35f), Vector3.up); mk.Tri(bird, new Vector3(0, 0, .35f), new Vector3(0, 0, -.35f), new Vector3(s * 1f, .15f, -.1f), Vector3.down); }
                var t = mk.BuildSingle("Free bird", transform, false).transform; t.gameObject.SetActive(false); flock.Add(t);
            }
            var ghostM = WorldMaterials.Glow(new Color(.75f, .9f, 1f), 1.6f);
            for (var i = 0; i < 7; i++)
            {
                var mk = new MeshKit();
                mk.Box(ghostM, new Vector3(0, 2f, 0), new Vector3(1f, 1.2f, 3f), Quaternion.identity);                       // horse
                mk.Box(ghostM, new Vector3(0, 3f, 1.8f), new Vector3(.6f, 1.4f, .7f), Quaternion.Euler(-35f, 0, 0));          // neck and head
                foreach (var (x, z) in new[] { (-.35f, -1.1f), (.35f, -1.1f), (-.35f, 1.1f), (.35f, 1.1f) }) mk.Box(ghostM, new Vector3(x, .7f, z), new Vector3(.2f, 1.5f, .2f), Quaternion.Euler(z > 0 ? 30f : -30f, 0, 0));
                mk.Box(ghostM, new Vector3(0, 3.3f, -.2f), new Vector3(.6f, 1.3f, .45f), Quaternion.identity);               // rider
                mk.Prism(ghostM, new Vector3(0, 4.15f, -.2f), Vector3.up, .7f, .06f, 10);                                   // hat brim
                mk.Prism(ghostM, new Vector3(0, 4.2f, -.2f), Vector3.up, .3f, .35f, 8);
                var t = mk.BuildSingle("Ghost rider", transform, false).transform; t.localScale = Vector3.one * 7f; t.gameObject.SetActive(false); riders.Add(t);
            }
            var mm = new MeshKit(); mm.Prism(WorldMaterials.Glow(new Color(1f, .82f, .55f), 1.4f), Vector3.zero, Vector3.forward, 1f, .01f, 40);
            moon = mm.BuildSingle("Bad moon", transform, false).transform; moon.gameObject.SetActive(false);
            var gk = new MeshKit(); var hide = WorldMaterials.Solid(new Color(.2f, .24f, .16f), .6f);
            gk.Box(hide, new Vector3(0, 0, 0), new Vector3(.9f, .3f, 3.2f), Quaternion.identity);
            gk.Box(hide, new Vector3(0, -.05f, 2.1f), new Vector3(.55f, .2f, 1f), Quaternion.identity);
            foreach (var s in new[] { -.2f, .2f }) gk.Blob(WorldMaterials.Glow(new Color(1f, .85f, .3f), 1.2f), new Vector3(s, .18f, 1.6f), Vector3.one * .08f, new System.Random(3), 0f);
            for (var k = 0; k < 6; k++) gk.Box(hide, new Vector3(0, .2f, -1.2f + k * .45f), new Vector3(.2f, .15f, .2f), Quaternion.identity);
            gator = gk.BuildSingle("Alligator", transform, false).transform; gator.gameObject.SetActive(false);
            MakeWisps();
        }

        /// Will-o'-the-wisps: each a bright blue-green core in a soft glow, drawn as particles placed by hand
        /// every frame. Most drift round a spot over the water; the guides float ahead of you on the boardwalk.
        void MakeWisps()
        {
            if (swampPools.Count == 0) return;
            wispPs = Particles("Wisps", (Wisps + Guides) * 2, new Color(.45f, 1f, .9f, 1f), .3f, .3f, true);
            var col = wispPs.colorOverLifetime; col.enabled = false;
            wispParts = new ParticleSystem.Particle[(Wisps + Guides) * 2];
            wispHome = new Vector3[Wisps + Guides]; wispPos = new Vector3[Wisps + Guides]; wispSeed = new float[Wisps + Guides];
            var rng = new System.Random(1971);
            for (var i = 0; i < Wisps + Guides; i++)
            {
                var pl = swampPools[rng.Next(swampPools.Count)];
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var rr = (float)rng.NextDouble() * pl.y * .9f;
                wispHome[i] = new Vector3(pl.x + Mathf.Cos(a) * rr, swampY + .8f + (float)rng.NextDouble() * 2.2f, pl.z + Mathf.Sin(a) * rr);
                wispPos[i] = wispHome[i]; wispSeed[i] = (float)rng.NextDouble() * 100f;
            }
            guideLights = new Light[2];
            for (var k = 0; k < 2; k++)
            {
                var l = new GameObject("Wisp light").AddComponent<Light>(); l.transform.SetParent(transform, false);
                l.type = LightType.Point; l.range = 9f; l.intensity = 1.6f; l.color = new Color(.4f, 1f, .85f); guideLights[k] = l;
            }
        }

        void UpdateWisps(float dt, float t, bool here, float energy)
        {
            if (wispPs == null) return;
            var cam = Camera.main != null ? Camera.main.transform.position : center;
            var nearSwamp = here && (swampPath.Length == 0 || (cam - swampPath[swampPath.Length / 2]).sqrMagnitude < 420f * 420f);
            wispPs.gameObject.SetActive(nearSwamp);
            foreach (var l in guideLights) l.enabled = nearSwamp;
            if (!nearSwamp) return;
            // where you are on the boardwalk, so the guides can float on ahead of you
            var at = -1; var bd = 30f * 30f;
            for (var i = 0; i < swampPath.Length; i++) { var d = (swampPath[i] - cam).sqrMagnitude; if (d < bd) { bd = d; at = i; } }
            for (var i = 0; i < Wisps + Guides; i++)
            {
                var sd = wispSeed[i];
                Vector3 target;
                if (elizabeth > .01f)
                {
                    // In Memory of Elizabeth Reed: every wisp gathers into a slow rising swirl round the old tree
                    var a = t * (.35f + (i % 5) * .05f) + sd; var rr = 5f + (i % 7) * 1.2f; var h = 1f + Mathf.Repeat(t * .6f + sd, 16f);
                    var swirl = oldTree + new Vector3(Mathf.Cos(a) * rr, h, Mathf.Sin(a) * rr);
                    target = Vector3.Lerp(Drift(i, t), swirl, elizabeth);
                }
                else if (i >= Wisps && at >= 0)
                {
                    var ahead = Mathf.Min(swampPath.Length - 1, at + 3 + (i - Wisps) * 3);
                    target = swampPath[ahead] + Vector3.up * (1.6f + Mathf.Sin(t * 1.3f + sd) * .3f) + new Vector3(Mathf.Sin(t * .7f + sd) * .8f, 0, Mathf.Cos(t * .6f + sd) * .8f);
                }
                else target = Drift(i, t);
                wispPos[i] = Vector3.Lerp(wispPos[i], target, 1f - Mathf.Exp(-dt * (i >= Wisps ? 1.2f : .8f)));
                var flick = .75f + .25f * Mathf.Sin(t * 3.1f + sd * 7f) + energy * .2f;
                var core = new Color(.8f, 1f, .95f, 1f); var glow = new Color(.2f, 1f, .8f, .5f * flick);
                wispParts[i * 2] = new ParticleSystem.Particle { position = wispPos[i], startSize = .6f * flick, startColor = core, remainingLifetime = 10f, startLifetime = 10f };
                wispParts[i * 2 + 1] = new ParticleSystem.Particle { position = wispPos[i], startSize = 3.6f * flick, startColor = glow, remainingLifetime = 10f, startLifetime = 10f };
            }
            wispPs.SetParticles(wispParts, wispParts.Length);
            for (var k = 0; k < guideLights.Length; k++) guideLights[k].transform.position = wispPos[elizabeth > .5f ? k * 7 : Wisps + k];
        }

        Vector3 Drift(int i, float t)
        {
            var sd = wispSeed[i];
            return wispHome[i] + new Vector3((Mathf.PerlinNoise(sd, t * .08f) - .5f) * 12f, (Mathf.PerlinNoise(sd + 3f, t * .12f) - .5f) * 1.6f, (Mathf.PerlinNoise(sd + 7f, t * .08f) - .5f) * 12f);
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = add ? Additive(name) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        Vector3 RiverAt(float f)
        {
            if (river.Length < 2) return center;
            f = Mathf.Clamp(f, 0, river.Length - 1.001f); var i = Mathf.FloorToInt(f);
            return Vector3.Lerp(river[i], river[i + 1], f - i);
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            SongGlowUpdate(dt, t, id, here);

            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey)
            {
                songKey = song; songStart = start; songLength = System.Math.Max(1, length);
                var k = SharedChance.Key("bayou-gator");
                gatorAt = SharedChance.Roll(k, song, .5) ? start + length * .6 * SharedChance.Value(k, song, 1) : -1;
                gatorIndex = river.Length > 20 ? 10 + (int)(SharedChance.Value(k, song, 2) * (river.Length - 40)) : 0;
            }
            var progress = Mathf.Clamp01((float)((now - songStart) / songLength));
            if (MusicDirector.ForcedTrackId != null) progress = Mathf.Repeat(t / 40f, 1f);   // the photo tour

            freeBird = Mathf.MoveTowards(freeBird, here && id == BayouKit.FreeBird ? 1f : 0f, dt / 3f);
            badMoon = Mathf.MoveTowards(badMoon, here && id == BayouKit.BadMoon ? 1f : 0f, dt / 4f);
            ghost = Mathf.MoveTowards(ghost, here && id == BayouKit.GhostRiders ? 1f : 0f, dt / 3f);
            proud = Mathf.MoveTowards(proud, here && id == BayouKit.ProudMary ? 1f : 0f, dt / 3f);
            mountain = Mathf.MoveTowards(mountain, here && id == BayouKit.FireOnTheMountain ? 1f : 0f, dt / 2f);
            bayou = Mathf.MoveTowards(bayou, here && id == BayouKit.BornOnTheBayou ? 1f : 0f, dt / 4f);
            elizabeth = Mathf.MoveTowards(elizabeth, here && id == BayouKit.ElizabethReed ? 1f : 0f, dt / 6f);
            UpdateWisps(dt, t, here, energy);

            // the light: gold haze; a strange dim red under the bad moon, cold under the ghost riders, thick swamp
            // haze for Born on the Bayou
            var tint = new Color(1f, .92f, .7f); var amount = .12f;
            if (badMoon > 0f) { tint = Color.Lerp(tint, new Color(.75f, .35f, .3f), badMoon); amount = Mathf.Lerp(amount, .55f, badMoon); }
            if (ghost > 0f) { tint = Color.Lerp(tint, new Color(.45f, .55f, .75f), ghost); amount = Mathf.Lerp(amount, .55f, ghost); }
            if (bayou > 0f) { tint = Color.Lerp(tint, new Color(.7f, .8f, .45f), bayou); amount = Mathf.Lerp(amount, .5f, bayou); }
            if (mountain > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .3f), mountain * .6f); amount = Mathf.Max(amount, .3f * mountain); }
            // walking into the Wisp Swamp the light dims to a blue-green gloom
            var camAt = Camera.main != null ? Camera.main.transform.position : center; var inSwamp = 0f;
            foreach (var pl in swampPools) { var dd = Vector2.Distance(new Vector2(camAt.x, camAt.z), new Vector2(pl.x, pl.z)) - pl.y; inSwamp = Mathf.Max(inSwamp, 1f - Mathf.SmoothStep(0f, 1f, dd / 70f)); }
            gloom = Mathf.MoveTowards(gloom, here ? inSwamp : 0f, dt / 3f);
            if (gloom > 0f) { tint = Color.Lerp(tint, new Color(.3f, .5f, .55f), gloom * .85f); amount = Mathf.Max(amount, .5f * gloom); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                Shader.SetGlobalFloat("_EW_Wind", .15f + .2f * energy);
                Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            }
            var cam = Camera.main != null ? Camera.main.transform.position : center;

            // fireflies round you, thicker in quiet songs and in thousands for Born on the Bayou
            nextFly -= dt;
            if (here && nextFly <= 0f)
            {
                nextFly = .05f;
                var n = Mathf.RoundToInt(2f + (1f - energy) * 3f + bayou * 18f);
                for (var k = 0; k < n; k++)
                {
                    var p = cam + new Vector3(Random.Range(-35f, 35f), 0, Random.Range(-35f, 35f));
                    var g = Physics.Raycast(p + Vector3.up * 60f, Vector3.down, out var hit, 120f) ? hit.point.y : cam.y - 1.6f;
                    flies.transform.position = new Vector3(p.x, g + Random.Range(.4f, 3.5f), p.z); flies.Emit(1);
                }
            }

            // campfires, and the fire on the mountain
            nextFlame -= dt;
            if (here && nextFlame <= 0f)
            {
                nextFlame = .08f;
                foreach (var f in fires) { if ((f - cam).sqrMagnitude > 160f * 160f) continue; flame.transform.position = f; flame.Emit(2); }
                if (mountainFire != Vector3.zero)
                {
                    flame.transform.position = mountainFire + Vector3.up * .6f;
                    var em = flame.main; em.startSizeMultiplier = 1f + mountain * 3f;
                    flame.Emit(Mathf.RoundToInt(3f + mountain * 30f));
                    em.startSizeMultiplier = 1f;
                }
            }
            nextSpark -= dt;
            if (here && mountain > .2f && nextSpark <= 0f) { nextSpark = .05f; sparks.transform.position = mountainFire + Vector3.up * 2f; sparks.Emit(Mathf.RoundToInt(6f * mountain)); }
            if (mountainLight != null) { var fl = mountainLight.GetComponent<FireFlicker>(); if (fl != null) fl.baseIntensity = 1.4f + mountain * 5f; mountainLight.range = 20f + mountain * 40f; }

            // the paddle steamer: wheel always turning; for Proud Mary it rolls down the river and back
            if (steamer != null && river.Length > 30)
            {
                var span = Mathf.Max(20f, river.Length - 30f);
                var target = steamerIndex;
                var f = proud > 0f && id == BayouKit.ProudMary ? Mathf.Lerp(steamerIndex, Mathf.Clamp(steamerIndex + Mathf.Sin(progress * Mathf.PI * 2f) * span * .45f, 12f, river.Length - 14f), proud) : (float)target;
                steamerPos = Mathf.MoveTowards(steamerPos == 0f ? steamerIndex : steamerPos, f, dt * 3f);
                var p = RiverAt(steamerPos); var a = RiverAt(steamerPos + 2f) - RiverAt(steamerPos - 2f); a.y = 0;
                if (a.sqrMagnitude > .01f) steamer.SetPositionAndRotation(p + Vector3.up * (-.4f + Mathf.Sin(t * .7f) * .05f), Quaternion.Slerp(steamer.rotation, Quaternion.LookRotation(a), dt * 2f));
                if (steamerWheel != null) steamerWheel.localRotation = Quaternion.Euler(t * (30f + proud * 120f), 0, 0);
            }

            // an alligator cruising a stretch of the river
            var gt = (float)(now - gatorAt); var gon = here && gatorAt > 0 && gt > 0f && gt < 60f && river.Length > 20;
            gator.gameObject.SetActive(gon);
            if (gon)
            {
                var f = gatorIndex + gt * .25f; var p = RiverAt(f); var a = RiverAt(f + 1f) - RiverAt(f - 1f); a.y = 0;
                if (a.sqrMagnitude > .01f) gator.SetPositionAndRotation(p + Vector3.up * -.08f + Vector3.Cross(Vector3.up, a.normalized) * 2f, Quaternion.LookRotation(a));
            }

            // Free Bird: the flock lifts off the porch roof and wheels up and away
            for (var i = 0; i < flock.Count; i++)
            {
                var on = freeBird > .05f && freeBirds != Vector3.zero;
                flock[i].gameObject.SetActive(on);
                if (!on) continue;
                var phase = Mathf.Repeat(t / 30f + i / (float)flock.Count, 1f);
                var a = phase * Mathf.PI * 4f + i;
                var r = 4f + phase * 90f; var h = phase * 70f;
                var p = freeBirds + new Vector3(Mathf.Cos(a) * r, h + Mathf.Sin(t * 2f + i) * .5f, Mathf.Sin(a) * r);
                flock[i].SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), .3f, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 9f + i) * 30f));
                flock[i].localScale = Vector3.one * (1f + phase);
            }

            // Bad Moon Rising: a huge moon climbing over the trees
            moon.gameObject.SetActive(badMoon > .01f);
            if (badMoon > .01f)
            {
                var dir = moonDir.normalized;
                var rise = Mathf.Lerp(-60f, 200f, Mathf.Clamp01(progress * 1.4f) * badMoon);
                moon.position = center + dir * 800f + Vector3.up * rise; moon.rotation = Quaternion.LookRotation(-dir);
                moon.localScale = Vector3.one * 170f;
            }

            // (Ghost) Riders in the Sky: pale riders galloping across
            for (var i = 0; i < riders.Count; i++)
            {
                var on = ghost > .05f; riders[i].gameObject.SetActive(on);
                if (!on) continue;
                var dir = new Vector3(1f, 0, .3f).normalized; var side = Vector3.Cross(Vector3.up, dir);
                var x = Mathf.Repeat(t * 30f + i * 50f, 1400f) - 700f;
                var p = center + dir * x + side * (Mathf.Sin(i * 1.7f) * 80f - 150f) + Vector3.up * (70f + i * 5f + Mathf.Abs(Mathf.Sin(t * 4f + i)) * 3f);
                riders[i].SetPositionAndRotation(p, Quaternion.LookRotation(dir) * Quaternion.Euler(Mathf.Sin(t * 8f + i) * 6f, 0, 0));
            }
        }
    }
}
