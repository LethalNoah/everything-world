using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Shoegaze Fens, alive with the music:
    /// - a soft pink dusk; warm rose for hopeful songs, deep violet for sad ones; low slow pink cloud;
    /// - petals always drifting down, and motes of light rising off the water, more for louder songs;
    /// - rings of light spreading over the water from the islands, faster and brighter as the song swells
    ///   (the wall of sound); the cathedral's stained glass breathes with it;
    /// - "When the Sun Hits" (Slowdive): the sun breaks through, shafts of gold pour into the fen, Slowdive's
    ///   sun and mirrors blaze, the petals and the rings turn gold, for as long as it plays;
    /// - now and then (the same for everyone, from the shared clock): a heron flying low over the water; the
    ///   cathedral bell ringing rings of light out over the fen as a song begins; a storm of petals in loud
    ///   songs; a little fleet of floating candles drifting across the water; and a heron standing somewhere
    ///   new in the shallows every day.
    public class FensLife : MonoBehaviour
    {
        const string Land = "The Shoegaze Fens";
        public Vector3 center, cathedral, belfry, slowdiveSun, hillBell;
        public float waterY, waterRadius = 150f;
        public List<Vector3> islands = new List<Vector3>(), hillSpots = new List<Vector3>();
        public Material[] glass = new Material[0];
        public Material sunDisc, mirrors;
        public Transform colorMass, altarOrb;

        static readonly Color HotPink = new Color(1f, .35f, .7f), Violet = new Color(.68f, .42f, 1f), Gold = new Color(1f, .78f, .4f), Lilac = new Color(.85f, .7f, 1f);
        static readonly Color[] GlassCols = { new Color(1f, .35f, .7f), new Color(.68f, .42f, 1f), new Color(.45f, .6f, 1f) };

        CloudLayer clouds;
        ParticleSystem petals, motes;
        Camera cam;
        float mood = .5f, sun, storm;
        Transform shafts; Material shaftMat;
        Light sunLight;

        class Ring { public Transform t; public MeshRenderer r; public float age = 99f, life, size; public Color col; }
        readonly List<Ring> rings = new List<Ring>();
        Mesh ringMesh; Material ringMat; MaterialPropertyBlock mpb;
        float ringTimer;

        // happenings
        long songKey = long.MinValue, heronDay = long.MinValue;
        double songStart, songLength, heronFlyAt = -1, stormAt = -1, bellAt = -1;
        bool candlesOn; Vector3 candleFrom, candleTo;
        Transform heronStand, heronFly, heronL, heronR, candles, bellRings;
        Material bellMat;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 110; clouds.span = 2400f; clouds.key = "fens";
            clouds.minHeight = 60f; clouds.maxHeight = 120f; clouds.size = 1.6f; clouds.speed = 3.5f; clouds.cover = .75f;
            clouds.color = new Color(.92f, .8f, .9f);
            petals = Particles("Petals", 900, new Color(1f, .78f, .86f, .9f), .12f, .22f, false);
            motes = Particles("Motes", 500, new Color(1f, .7f, .95f, .8f), .08f, .16f, true);
            MakeRings(); MakeShafts(); MakeHappenings();
            var go = new GameObject("Sunburst light"); go.transform.SetParent(transform, false);
            sunLight = go.AddComponent<Light>(); sunLight.type = LightType.Point; sunLight.range = 260f; sunLight.color = Gold; sunLight.intensity = 0f;
            go.transform.position = center + Vector3.up * 70f;
        }

        static Material Additive(string name, Texture tex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        static Texture2D Soft()
        {
            const int n = 32; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f)));
            }
            tex.Apply(); return tex;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool glow)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = glow ? new Vector3(60f, 1f, 60f) : new Vector3(50f, 2f, 50f);
            var e = ps.emission; e.rateOverTime = 0f;
            var colLife = ps.colorOverLifetime; colLife.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(1f, .75f), new GradientAlphaKey(0f, 1f) });
            colLife.color = g;
            var rotL = ps.rotationOverLifetime; rotL.enabled = !glow; rotL.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (glow) r.sharedMaterial = Additive(name, Soft());
            else
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
                m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                r.sharedMaterial = m;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        // ---------------------------------------------------------------- rings of light on the water
        void MakeRings()
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            const int n = 48;
            for (var k = 0; k <= n; k++)
            {
                var a = k * Mathf.PI * 2f / n;
                v.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); v.Add(new Vector3(Mathf.Cos(a) * .9f, 0, Mathf.Sin(a) * .9f));
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, 1));
                if (k < n) { var b = k * 2; t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
            }
            ringMesh = new Mesh { name = "Ring" }; ringMesh.SetVertices(v); ringMesh.SetUVs(0, uv); ringMesh.SetTriangles(t, 0); ringMesh.RecalculateBounds();
            ringMat = Additive("Water rings", Texture2D.whiteTexture);
            mpb = new MaterialPropertyBlock();
            for (var i = 0; i < 40; i++)
            {
                var go = new GameObject("Ring"); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = ringMesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = ringMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                rings.Add(new Ring { t = go.transform, r = r });
            }
        }

        void SpawnRing(Vector3 at, float size, float life, Color col)
        {
            foreach (var r in rings)
            {
                if (r.age < r.life) continue;
                r.age = 0f; r.life = life; r.size = size; r.col = col;
                r.t.position = new Vector3(at.x, waterY - .02f, at.z);
                r.t.gameObject.SetActive(true);
                return;
            }
        }

        // ---------------------------------------------------------------- shafts of gold for "When the Sun Hits"
        void MakeShafts()
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            var rng = new System.Random(5);
            for (var i = 0; i < 14; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = 20f + (float)rng.NextDouble() * waterRadius;
                var foot = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                var w = 5f + (float)rng.NextDouble() * 10f;
                var across = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                var lean = new Vector3(30f, 140f, -20f);
                var b = v.Count;
                v.Add(foot - across * w); v.Add(foot + across * w); v.Add(foot + across * w * 1.8f + lean); v.Add(foot - across * w * 1.8f + lean);
                var al = .12f + (float)rng.NextDouble() * .14f;
                c.Add(new Color(1f, .82f, .5f, al)); c.Add(new Color(1f, .82f, .5f, al)); c.Add(new Color(1f, .82f, .5f, 0f)); c.Add(new Color(1f, .82f, .5f, 0f));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "Sun shafts" }; mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Sun shafts"); go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(center.x, waterY, center.z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>(); shaftMat = Additive("Sun shafts", Texture2D.whiteTexture);
            rend.sharedMaterial = shaftMat; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shafts = go.transform; go.SetActive(false);
        }

        // ---------------------------------------------------------------- happenings
        void MakeHappenings()
        {
            var rng = new System.Random(31);
            var grey = WorldMaterials.Solid(new Color(.62f, .64f, .7f)); var pale = WorldMaterials.Solid(new Color(.88f, .88f, .9f)); var beak = WorldMaterials.Solid(new Color(.9f, .7f, .3f));
            // the standing heron
            var mk = new MeshKit();
            mk.Blob(grey, new Vector3(0, 1.1f, 0), new Vector3(.25f, .3f, .5f), rng, .05f);
            mk.Box(pale, new Vector3(0, 1.55f, .3f), new Vector3(.08f, .7f, .08f), Quaternion.Euler(20f, 0, 0));
            mk.Blob(pale, new Vector3(0, 1.92f, .42f), new Vector3(.1f, .1f, .14f), rng, 0f);
            mk.Box(beak, new Vector3(0, 1.9f, .62f), new Vector3(.04f, .04f, .3f), Quaternion.identity);
            foreach (var s in new[] { -.08f, .08f }) mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .3f)), new Vector3(s, .45f, 0), new Vector3(.03f, .9f, .03f), Quaternion.identity);
            heronStand = mk.Build("Heron", transform, false).transform; heronStand.gameObject.SetActive(false);
            // the flying heron
            var fly = new GameObject("Heron flying").transform; fly.SetParent(transform, false);
            var body = new MeshKit();
            body.Blob(grey, Vector3.zero, new Vector3(.22f, .22f, .55f), rng, .05f);
            body.Box(pale, new Vector3(0, .05f, .55f), new Vector3(.08f, .08f, .45f), Quaternion.identity);
            body.Box(beak, new Vector3(0, .05f, .9f), new Vector3(.04f, .04f, .28f), Quaternion.identity);
            body.Box(WorldMaterials.Solid(new Color(.3f, .3f, .3f)), new Vector3(0, 0, -.8f), new Vector3(.04f, .04f, .7f), Quaternion.identity);
            body.Build("Body", fly, false);
            foreach (var s in new[] { -1f, 1f })
            {
                var wing = new GameObject("Wing").transform; wing.SetParent(fly, false); wing.localPosition = new Vector3(s * .15f, .05f, 0);
                var w = new MeshKit();
                foreach (var inside in new[] { Vector3.up, Vector3.down })
                {
                    w.Tri(grey, new Vector3(0, 0, .3f), new Vector3(0, 0, -.3f), new Vector3(s * 1.6f, 0, -.2f), inside);
                    w.Tri(grey, new Vector3(0, 0, .3f), new Vector3(s * 1.6f, 0, -.2f), new Vector3(s * 1.3f, 0, .25f), inside);
                }
                w.Build("Wing", wing, false);
                if (s < 0) heronL = wing; else heronR = wing;
            }
            heronFly = fly; fly.gameObject.SetActive(false);
            // the candles that drift across the water
            var cmk = new MeshKit(); var wax = WorldMaterials.Solid(new Color(.96f, .93f, .9f)); var flame = WorldMaterials.Glow(new Color(1f, .7f, .45f), 3.2f);
            var lily = WorldMaterials.Solid(new Color(.4f, .55f, .42f));
            for (var i = 0; i < 14; i++)
            {
                var p = new Vector3(((float)rng.NextDouble() - .5f) * 14f, 0, ((float)rng.NextDouble() - .5f) * 10f);
                cmk.Prism(lily, p, Vector3.up, .35f, .03f, 8);
                cmk.Prism(wax, p + Vector3.up * .03f, Vector3.up, .06f, .14f, 6);
                cmk.Blob(flame, p + Vector3.up * .22f, new Vector3(.03f, .06f, .03f), rng, 0f);
            }
            candles = cmk.Build("Floating candles", transform, false).transform;
            var cl = new GameObject("Candle light").AddComponent<Light>(); cl.transform.SetParent(candles, false);
            cl.transform.localPosition = Vector3.up * 1f; cl.type = LightType.Point; cl.range = 14f; cl.intensity = 1.4f; cl.color = new Color(1f, .7f, .45f);
            candles.gameObject.SetActive(false);
            // the bell's rings of light
            var bgo = new GameObject("Bell rings"); bgo.transform.SetParent(transform, false);
            bellMat = Additive("Bell rings", Texture2D.whiteTexture);
            for (var i = 0; i < 3; i++)
            {
                var r = new GameObject("Bell ring"); r.transform.SetParent(bgo.transform, false);
                r.AddComponent<MeshFilter>().sharedMesh = ringMesh;
                var mr = r.AddComponent<MeshRenderer>(); mr.sharedMaterial = bellMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            bellRings = bgo.transform; bgo.SetActive(false);
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var valence = playing ? d.CurrentSlot.track.valence : .45f;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            // a new song: what happens during it
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey)
            {
                songKey = song; songStart = start; songLength = length;
                var k = SharedChance.Key("fens-heron");
                heronFlyAt = SharedChance.Roll(k, song, .4) ? start + length * (.1 + .6 * SharedChance.Value(k, song, 1)) : -1;
                k = SharedChance.Key("fens-storm");
                stormAt = playing && d.CurrentSlot.track.energy > .55f && SharedChance.Roll(k, song, .45) ? start + length * (.3 + .4 * SharedChance.Value(k, song, 1)) : -1;
                bellAt = SharedChance.Roll(SharedChance.Key("fens-bell"), song, .5) ? start + 1.5 : -1;
                k = SharedChance.Key("fens-candles");
                candlesOn = SharedChance.Roll(k, song, .35);
                var a = (float)(SharedChance.Value(k, song, 1) * Mathf.PI * 2.0);
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                candleFrom = center - dir * waterRadius * .7f + Vector3.Cross(Vector3.up, dir) * 20f; candleTo = center + dir * waterRadius * .7f + Vector3.Cross(Vector3.up, dir) * 20f;
            }

            // "When the Sun Hits"
            sun = Mathf.MoveTowards(sun, here && id == FensKit.SunSong ? 1f : 0f, dt / 7f);
            var sunE = Mathf.SmoothStep(0f, 1f, sun);

            // the light: rose for hopeful songs, violet for sad, gold when the sun hits
            mood = Mathf.MoveTowards(mood, Mathf.Clamp01(valence * 1.3f), dt / 4f);
            var tint = Color.Lerp(new Color(.62f, .45f, .95f), new Color(1f, .6f, .75f), mood);
            SkyDirector.SetSongTint(Land, Color.Lerp(tint, Gold, sunE), Mathf.Lerp(.35f, .7f, sunE));
            if (clouds != null) clouds.cover = Mathf.Lerp(.8f, .25f, sunE);
            if (sunLight != null) sunLight.intensity = here ? sunE * 2.2f : 0f;
            if (shafts != null)
            {
                shafts.gameObject.SetActive(here && sunE > .01f);
                shaftMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, sunE * (.85f + .15f * Mathf.Sin(t * .5f))));
            }
            if (sunDisc != null) sunDisc.SetColor("_EmissionColor", Gold * (2f + 10f * sunE + Mathf.Sin(t * 1.3f) * .3f));
            if (mirrors != null) mirrors.SetColor("_EmissionColor", new Color(1f, .9f, .7f) * (.3f + 3f * sunE));

            if (here)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                Shader.SetGlobalFloat("_EW_Wind", .3f + .35f * energy + storm * .6f);
                Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            }

            // the stained glass breathes with the music; the cloud of colour drifts; the altar orb floats
            for (var i = 0; i < glass.Length; i++)
                glass[i].SetColor("_EmissionColor", Color.Lerp(GlassCols[i % 3], Gold, sunE * .5f) * (.8f + 1.1f * energy + Mathf.Sin(t * (.6f + i * .23f)) * .25f));
            if (colorMass != null) { colorMass.Rotate(0, dt * (4f + 10f * energy), 0, Space.World); colorMass.localScale = Vector3.one * (1f + .06f * Mathf.Sin(t * .7f)); }
            if (altarOrb != null) altarOrb.localScale = Vector3.one * (1f + .1f * energy + .05f * Mathf.Sin(t * 1.4f));

            // petals and motes round you
            storm = Mathf.MoveTowards(storm, here && stormAt > 0 && now > stormAt && now < stormAt + 40 ? 1f : 0f, dt / 3f);
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                petals.transform.position = cam.transform.position + Vector3.up * 12f;
                var e = petals.emission; e.rateOverTime = here ? 18f + 30f * energy + storm * 220f : 0f;
                var main = petals.main; main.startColor = Color.Lerp(new Color(1f, .78f, .86f, .9f), new Color(1f, .85f, .5f, .9f), sunE);
                var vel = petals.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                var push = 1f + storm * 4f;
                vel.x = new ParticleSystem.MinMaxCurve(w.x * push - .4f, w.x * push + .4f); vel.y = new ParticleSystem.MinMaxCurve(-1.4f - storm, -.7f); vel.z = new ParticleSystem.MinMaxCurve(w.y * push - .4f, w.y * push + .4f);
                var p = cam.transform.position; p.y = waterY + .2f;
                motes.transform.position = p;
                var em = motes.emission; em.rateOverTime = here && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(center.x, center.z)) < waterRadius + 20f ? 6f + 40f * energy : 0f;
                var mm = motes.main; mm.startColor = Color.Lerp(new Color(1f, .6f, .95f, .8f), new Color(1f, .85f, .5f, .8f), sunE);
                var mv = motes.velocityOverLifetime; mv.enabled = true; mv.space = ParticleSystemSimulationSpace.World;
                mv.x = new ParticleSystem.MinMaxCurve(-.2f, .2f); mv.y = new ParticleSystem.MinMaxCurve(.25f, .6f + energy); mv.z = new ParticleSystem.MinMaxCurve(-.2f, .2f);
            }

            // rings of light on the water, from the islands, faster with the music
            ringTimer -= dt;
            if (here && ringTimer <= 0f && islands.Count > 0)
            {
                ringTimer = Mathf.Lerp(1.4f, .18f, energy);
                var isl = islands[Random.Range(0, islands.Count)];
                var a = Random.value * Mathf.PI * 2f;
                var at = new Vector3(isl.x + Mathf.Cos(a) * (isl.y + 3f), 0, isl.z + Mathf.Sin(a) * (isl.y + 3f));
                var col = sunE > .5f ? Gold : Random.value < .5f ? HotPink : Random.value < .5f ? Violet : Lilac;
                SpawnRing(at, 6f + 10f * energy, 3.5f + Random.value * 2f, col);
            }
            foreach (var r in rings)
            {
                if (r.age >= r.life) { if (r.t.gameObject.activeSelf) r.t.gameObject.SetActive(false); continue; }
                r.age += dt; var f = r.age / r.life;
                r.t.localScale = Vector3.one * Mathf.Lerp(.5f, r.size, Mathf.Sqrt(f));
                mpb.SetColor("_BaseColor", new Color(r.col.r, r.col.g, r.col.b, (1f - f) * (1f - f) * .8f));
                r.r.SetPropertyBlock(mpb);
            }

            Happenings(here, now, t, dt);
        }

        void Happenings(bool here, double now, float t, float dt)
        {
            // a heron standing in the shallows, somewhere new each day
            if (SharedChance.Day != heronDay && islands.Count > 1)
            {
                heronDay = SharedChance.Day;
                var k = SharedChance.Key("fens-heron-day");
                var isl = islands[1 + (int)(SharedChance.Value(k, heronDay) * (islands.Count - 1)) % (islands.Count - 1)];
                var a = (float)(SharedChance.Value(k, heronDay, 1) * Mathf.PI * 2.0);
                heronStand.SetPositionAndRotation(new Vector3(isl.x + Mathf.Cos(a) * (isl.y + 4f), waterY - .55f, isl.z + Mathf.Sin(a) * (isl.y + 4f)), Quaternion.Euler(0, a * Mathf.Rad2Deg + 70f, 0));
            }
            heronStand.gameObject.SetActive(here);

            // a heron flying low over the water
            var ft = (float)(now - heronFlyAt);
            var flying = here && heronFlyAt > 0 && ft > 0f && ft < 45f;
            heronFly.gameObject.SetActive(flying);
            if (flying)
            {
                var a = (float)(heronFlyAt % 6.283);
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = center - dir * (waterRadius + 60f) + dir * ft * 8f + Vector3.Cross(Vector3.up, dir) * Mathf.Sin(ft * .1f) * 15f;
                p.y = waterY + 9f + Mathf.Sin(ft * .4f) * 1.5f;
                heronFly.SetPositionAndRotation(p, Quaternion.LookRotation(dir));
                var flap = Mathf.Sin(t * 3.2f) * 28f;
                heronL.localRotation = Quaternion.Euler(0, 0, -flap); heronR.localRotation = Quaternion.Euler(0, 0, flap);
            }

            // the cathedral bell: three rings of light spreading from the tower as a song begins
            var bt = (float)(now - bellAt);
            var ringing = here && bellAt > 0 && bt > 0f && bt < 9f && belfry != Vector3.zero;
            bellRings.gameObject.SetActive(ringing);
            if (ringing)
            {
                bellRings.position = belfry;
                var mp = new MaterialPropertyBlock();
                for (var i = 0; i < bellRings.childCount; i++)
                {
                    var f = Mathf.Clamp01((bt - i * 1.4f) / 6f);
                    var c = bellRings.GetChild(i);
                    c.localScale = Vector3.one * Mathf.Lerp(1f, 120f, f);
                    mp.SetColor("_BaseColor", new Color(Lilac.r, Lilac.g, Lilac.b, f > 0f ? (1f - f) * .7f : 0f));
                    c.GetComponent<MeshRenderer>().SetPropertyBlock(mp);
                }
            }

            // floating candles drifting across the water, the whole song long
            candles.gameObject.SetActive(here && candlesOn && songLength > 0);
            if (here && candlesOn && songLength > 0)
            {
                var f = Mathf.Clamp01((float)((now - songStart) / songLength));
                var p = Vector3.Lerp(candleFrom, candleTo, f); p.y = waterY - .03f + Mathf.Sin(t * .8f) * .02f;
                candles.SetPositionAndRotation(p, Quaternion.Euler(0, t * 2f, 0));
            }
        }
    }
}
