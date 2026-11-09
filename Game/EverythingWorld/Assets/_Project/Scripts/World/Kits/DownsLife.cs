using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Indie Downs' weather, which follows the music more than anywhere else:
    /// - quiet songs: the sky clouds over, the light goes soft and grey-blue, the wind drops, the kites sink;
    /// - energetic songs: the sun breaks through warm, the wind picks up, the long grass and bunting race, the kites climb.
    /// Low clouds drift on the day's wind with their shadows sweeping across the hills, and seeds and pollen
    /// float through the air around you, carried faster when the wind is up. All of it comes from the
    /// song and the shared clock, so friends on the Downs see the same weather.
    /// Each song can also bring surprises, rolled from the song itself so everyone sees the same ones:
    /// - a gust when a new song starts (the grass ripples, a burst of seeds, the kites lurch, the windmill spins up);
    /// - a passing summer shower (likelier for quiet, sad songs), and a rainbow after it;
    /// - a murmuration of starlings swirling over the hills;
    /// - a hot-air balloon drifting across the sky for the length of the song.
    /// The waymarkers on each person's path glow and pulse, and blaze brighter while a song that person added plays.
    public class DownsLife : MonoBehaviour
    {
        const string Land = "The Indie Downs";
        public Vector3 center;
        public Vector3[] kiteAnchors = new Vector3[0];

        public Material noahMarker, seanMarker;
        public Transform[] sails = new Transform[0];
        public Dictionary<string, string> adderOf = new Dictionary<string, string>();   // track id -> who added it

        CloudLayer clouds;
        long songKey = long.MinValue; double songStart, songLength = 1;
        float gust, noahBoost, seanBoost;
        double showerAt = -1, starlingsAt = -1; bool balloonOn;
        const double ShowerLength = 55, RainbowLength = 75, StarlingLength = 60;
        ParticleSystem starlings; ParticleSystem.Particle[] birds; Vector3[] birdPhase;
        Transform rainbow; Material rainbowMat;
        Transform balloon;
        ParticleSystem seeds;
        Camera cam;
        float wind = .4f, mood = .5f;

        class Kite { public Transform body; public LineRenderer line; public Vector3 anchor; public float phase, reach; }
        readonly List<Kite> kites = new List<Kite>();
        static readonly Color[] KiteColors = { IndieDownsKit.NoahBlue, IndieDownsKit.SeanGreen, new Color(.9f, .3f, .3f), new Color(.95f, .8f, .3f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 110; clouds.span = 2600f; clouds.key = "downs";
            clouds.minHeight = 95f; clouds.maxHeight = 170f; clouds.size = 1.35f; clouds.speed = 7f; clouds.shadows = true;
            clouds.color = new Color(.97f, .96f, .94f);
            for (var i = 0; i < kiteAnchors.Length; i++) MakeKite(kiteAnchors[i], KiteColors[i % KiteColors.Length], i);
            MakeSeeds();
            MakeRainbow();
            MakeStarlings();
            MakeBalloon();
        }

        /// Thistledown, dandelion seeds and pollen catching the light: the Downs' own weather, like Lanternwood's snow.
        void MakeSeeds()
        {
            var go = new GameObject("Drifting seeds");
            go.transform.SetParent(transform, false);
            seeds = go.AddComponent<ParticleSystem>();
            seeds.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = seeds.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 3000;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(.05f, .14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .97f, .88f, .9f), new Color(1f, .88f, .6f, .8f));
            var shape = seeds.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(70f, 16f, 70f);
            var emission = seeds.emission; emission.rateOverTime = 0f;
            var col = seeds.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(1f, .8f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var noise = seeds.noise; noise.enabled = true; noise.strength = .6f; noise.frequency = .25f; noise.scrollSpeed = .3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = SeedMaterial();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            seeds.Play();
        }

        static Material SeedMaterial()
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Seed", wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                var a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1, 1, 1, a * a * (.5f + .5f * a)));
            }
            tex.Apply();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Drifting seeds" };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        void MakeKite(Vector3 anchor, Color color, int i)
        {
            var body = new GameObject("Kite").transform; body.SetParent(transform, false);
            var mk = new MeshKit();
            var cloth = WorldMaterials.Solid(color);
            Vector3 top = new Vector3(0, 1.1f, 0), bottom = new Vector3(0, -1.5f, 0), l = new Vector3(-.9f, 0, 0), r = new Vector3(.9f, 0, 0);
            foreach (var side in new[] { Vector3.forward, Vector3.back })
            {
                var inside = -side * .1f;
                mk.Tri(cloth, top, l, bottom, inside); mk.Tri(cloth, top, r, bottom, inside);
            }
            var tail = WorldMaterials.Solid(Color.Lerp(color, Color.white, .5f));
            for (var k = 0; k < 5; k++) mk.Box(tail, bottom - Vector3.up * (.6f + k * .55f) + Vector3.right * Mathf.Sin(k) * .15f, new Vector3(.3f, .12f, .05f), Quaternion.Euler(0, 0, k * 25f));
            mk.Build("Cloth", body, false).isStatic = false;
            var line = body.gameObject.AddComponent<LineRenderer>();
            line.positionCount = 2; line.widthMultiplier = .03f; line.useWorldSpace = true;
            line.sharedMaterial = WorldMaterials.Solid(new Color(.25f, .24f, .22f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // a stake in the ground where the string is tied
            var stake = new MeshKit();
            stake.Prism(WorldMaterials.Solid(new Color(.33f, .25f, .19f)), anchor, Vector3.up, .06f, .8f, 5);
            stake.Build("Kite stake", transform, false);
            kites.Add(new Kite { body = body, line = line, anchor = anchor + Vector3.up * .75f, phase = i * 1.7f, reach = 24f + i * 5f });
        }

        static Material SoftMaterial(string name, Color color, bool additive)
        {
            var m = SeedMaterial();
            m.name = name;
            m.SetColor("_BaseColor", color);
            if (additive) { m.SetFloat("_Blend", 2f); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); }
            return m;
        }

        void MakeRainbow()
        {
            var cols = new[] { new Color(.9f, .25f, .25f), new Color(.95f, .55f, .2f), new Color(.95f, .9f, .3f), new Color(.4f, .85f, .4f), new Color(.3f, .6f, .95f), new Color(.4f, .35f, .85f), new Color(.65f, .35f, .8f) };
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            const float R = 320f, band = 9f; const int segs = 64;
            for (var b = 0; b < cols.Length; b++)
            {
                float r0 = R - b * band, r1 = r0 - band;
                for (var i = 0; i < segs; i++)
                {
                    float a0 = Mathf.PI * i / segs, a1 = Mathf.PI * (i + 1) / segs;
                    var k = v.Count;
                    v.Add(new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, 0)); v.Add(new Vector3(Mathf.Cos(a1) * r0, Mathf.Sin(a1) * r0, 0));
                    v.Add(new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, 0)); v.Add(new Vector3(Mathf.Cos(a0) * r1, Mathf.Sin(a0) * r1, 0));
                    var edge = (b == 0 || b == cols.Length - 1) ? .25f : .55f;
                    for (var q = 0; q < 4; q++) c.Add(new Color(cols[b].r, cols[b].g, cols[b].b, edge));
                    t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
            }
            var mesh = new Mesh { name = "Rainbow" };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Rainbow");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            rainbowMat = SoftMaterial("Rainbow", new Color(1, 1, 1, 0), false);
            rainbowMat.SetTexture("_BaseMap", Texture2D.whiteTexture);
            rainbowMat.SetFloat("_Cull", 0f);
            r.sharedMaterial = rainbowMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rainbow = go.transform;
            go.SetActive(false);
        }

        void MakeStarlings()
        {
            var go = new GameObject("Starlings");
            go.transform.SetParent(transform, false);
            starlings = go.AddComponent<ParticleSystem>();
            starlings.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = starlings.main;
            main.loop = false; main.playOnAwake = false; main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 9999f; main.startSpeed = 0f; main.startSize = .55f;
            main.startColor = new Color(.12f, .11f, .13f, 1f);
            var emission = starlings.emission; emission.enabled = false;
            var shape = starlings.shape; shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = SoftMaterial("Starlings", Color.white, false);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            starlings.Play();
            var rng = new System.Random(99);
            birdPhase = new Vector3[320];
            for (var i = 0; i < birdPhase.Length; i++) birdPhase[i] = new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
        }

        void MakeBalloon()
        {
            var mk = new MeshKit();
            var bands = new[] { IndieDownsKit.NoahBlue, new Color(.97f, .93f, .82f), IndieDownsKit.SeanGreen, new Color(.97f, .93f, .82f), new Color(.88f, .3f, .28f), new Color(.95f, .8f, .3f) };
            float[] radii = { 1.4f, 3.6f, 5f, 5.6f, 5.5f, 4.9f, 3.6f };
            var y = 0f;
            for (var i = 0; i + 1 < radii.Length; i++)
            {
                mk.Prism(WorldMaterials.Solid(bands[i % bands.Length]), Vector3.up * y, Vector3.up, radii[i], 2.2f, 14, radii[i + 1] / radii[i]);
                y += 2.2f;
            }
            mk.Blob(WorldMaterials.Solid(bands[0]), Vector3.up * (y + .4f), new Vector3(3.7f, 1.6f, 3.7f), new System.Random(5), .02f);
            var wicker = WorldMaterials.Solid(new Color(.55f, .4f, .26f));
            mk.Box(wicker, Vector3.down * 3.2f, new Vector3(1.6f, 1.1f, 1.6f), Quaternion.identity);
            for (var k = 0; k < 4; k++)
            {
                var corner = Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward;
                Vector3 a = Vector3.down * 2.65f + corner * 1.05f, b = corner * 1.3f;
                var d = b - a;
                mk.Box(wicker, (a + b) / 2f, new Vector3(.05f, d.magnitude, .05f), Quaternion.LookRotation(Vector3.Cross(d, Vector3.up).sqrMagnitude > .01f ? Vector3.Cross(d, Vector3.up) : Vector3.forward, d.normalized));
            }
            var go = new GameObject("Hot-air balloon");
            go.transform.SetParent(transform, false);
            var built = mk.Build("Balloon", go.transform, false); built.isStatic = false;
            go.transform.localScale = Vector3.one * 1.6f;
            balloon = go.transform;
            go.SetActive(false);
        }

        void Update()
        {
            var now = SharedChance.Now;
            var dt = Time.deltaTime;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var valence = playing ? d.CurrentSlot.track.valence : .5f;

            // a new song: a gust, and the song's surprises rolled from the song itself
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey)
            {
                var first = songKey == long.MinValue;
                songKey = song; songStart = start; songLength = System.Math.Max(1.0, length);
                var tr = d.CurrentSlot.track;
                if (!first && now - start < 6) { gust = 1f; if (seeds != null && here) seeds.Emit(260); }
                var sad = tr.energy < .55f && tr.valence < .45f;
                var key = SharedChance.Key("downs-shower");
                showerAt = SharedChance.Roll(key, song, sad ? .55 : .15) ? start + length * (.1 + .25 * SharedChance.Value(key, song, 1)) : -1;
                key = SharedChance.Key("downs-starlings");
                starlingsAt = SharedChance.Roll(key, song, .3) ? start + length * (.2 + .45 * SharedChance.Value(key, song, 1)) : -1;
                balloonOn = SharedChance.Roll(SharedChance.Key("downs-balloon"), song, .22);
            }
            gust = Mathf.MoveTowards(gust, 0f, dt / 7f);

            // the passing shower and the rainbow after it
            var showerT = showerAt >= 0 ? now - showerAt : -1.0;
            var raining = showerT >= 0 && showerT < ShowerLength;
            var rainAmount = raining ? .5f * Mathf.Clamp01((float)System.Math.Min(showerT, ShowerLength - showerT) / 8f) : 0f;
            SkyDirector.SetSongRain(Land, rainAmount);
            var bowT = showerT - (ShowerLength - 12);
            var bow = bowT >= 0 && bowT < RainbowLength ? Mathf.Clamp01((float)System.Math.Min(bowT, RainbowLength - bowT) / 10f) : 0f;

            // how open the day feels: mostly energy, nudged by how bright the song is; showers close it in
            var goal = Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.3f, .8f, energy)) * .8f + valence * .2f);
            if (raining) goal *= .25f;
            mood = Mathf.MoveTowards(mood, goal, dt / 4f);
            wind = Mathf.MoveTowards(wind, .12f + .88f * Mathf.InverseLerp(.25f, .9f, energy), dt / 3f);
            var gusty = Mathf.Clamp01(wind + gust * .75f);

            // light: soft grey-blue overcast for quiet songs, warm sun breaking through for loud ones
            var overcast = new Color(.6f, .64f, .72f); var sun = new Color(1f, .9f, .72f);
            SkyDirector.SetSongTint(Land, Color.Lerp(overcast, sun, mood), Mathf.Lerp(.55f, .2f, mood));
            if (clouds != null) clouds.cover = Mathf.Lerp(.95f, .3f, mood);

            var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
            Shader.SetGlobalFloat("_EW_Wind", gusty);
            Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            var dir = new Vector3(w.x, 0, w.y);
            var t = Time.time;
            if (cam == null) cam = Camera.main;

            if (seeds != null && cam != null)
            {
                seeds.transform.position = cam.transform.position + Vector3.up * 3f;
                var emission = seeds.emission; emission.rateOverTime = here && !raining ? 90f + 120f * gusty : 0f;
                var vel = seeds.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                var drift = dir * (.4f + 3.6f * gusty);
                vel.x = drift.x; vel.y = .08f; vel.z = drift.z;
            }
            foreach (var k in kites)
            {
                var height = 6f + 26f * gusty;
                var sway = new Vector3(Mathf.Sin(t * .7f + k.phase), 0, Mathf.Cos(t * .5f + k.phase)) * (2f + 4f * gusty + gust * 6f);
                var p = k.anchor + dir * k.reach * (.5f + .5f * gusty) + Vector3.up * (height + Mathf.Sin(t * 1.3f + k.phase) * 1.5f * gusty) + sway;
                k.body.position = Vector3.Lerp(k.body.position == Vector3.zero ? p : k.body.position, p, dt * 1.5f);
                k.body.rotation = Quaternion.LookRotation(-dir, Vector3.up) * Quaternion.Euler(-35f + 15f * (1f - gusty), 0, Mathf.Sin(t * 2.1f + k.phase) * 18f * gusty);
                k.line.SetPosition(0, k.anchor);
                k.line.SetPosition(1, k.body.position);
            }
            foreach (var s in sails) if (s != null) s.Rotate(0, 0, -(12f + 110f * gusty) * dt, Space.Self);

            // waymarkers: a slow glow always, blazing for the person whose song is playing
            var who = playing && adderOf.TryGetValue(d.CurrentSlot.track.id, out var a) ? a : null;
            noahBoost = Mathf.MoveTowards(noahBoost, who == "npgallagher" ? 1f : 0f, dt / 1.5f);
            seanBoost = Mathf.MoveTowards(seanBoost, who == "Pyrosolstice" ? 1f : 0f, dt / 1.5f);
            if (noahMarker != null) noahMarker.SetColor("_EmissionColor", IndieDownsKit.NoahBlue * (1.6f + .7f * Mathf.Sin(t * 1.6f) + noahBoost * (2.6f + 1.8f * Mathf.Sin(t * 4f))));
            if (seanMarker != null) seanMarker.SetColor("_EmissionColor", IndieDownsKit.SeanGlow * (1.3f + .5f * Mathf.Sin(t * 1.6f + 1.5f) + seanBoost * (2.2f + 1.4f * Mathf.Sin(t * 4f))));

            // the rainbow, opposite the sun
            if (rainbow != null)
            {
                rainbow.gameObject.SetActive(here && bow > .01f && cam != null);
                if (rainbow.gameObject.activeSelf)
                {
                    var away = SkyDirector.Instance.sun != null ? SkyDirector.Instance.sun.transform.forward : Vector3.forward;
                    away.y = 0; away = away.sqrMagnitude > .01f ? away.normalized : Vector3.forward;
                    rainbow.SetPositionAndRotation(new Vector3(cam.transform.position.x, cam.transform.position.y - 60f, cam.transform.position.z) + away * 520f, Quaternion.LookRotation(away));
                    rainbowMat.SetColor("_BaseColor", new Color(1, 1, 1, bow));
                }
            }

            // a murmuration of starlings
            var flockT = starlingsAt >= 0 ? now - starlingsAt : -1.0;
            if (starlings != null)
            {
                if (here && flockT >= 0 && flockT < StarlingLength)
                {
                    var fade = Mathf.Clamp01((float)System.Math.Min(flockT, StarlingLength - flockT) / 6f);
                    var count = Mathf.RoundToInt(birdPhase.Length * fade);
                    if (birds == null || birds.Length != birdPhase.Length) birds = new ParticleSystem.Particle[birdPhase.Length];
                    var ft = (float)flockT;
                    var c = center + new Vector3(Mathf.Sin(ft * .05f) * 220f, 70f + Mathf.Sin(ft * .13f) * 12f, Mathf.Cos(ft * .04f) * 220f);
                    var R = 28f * (1f + .35f * Mathf.Sin(ft * .21f));
                    for (var i = 0; i < count; i++)
                    {
                        var ph = birdPhase[i];
                        var pos = c + new Vector3(
                            Mathf.Sin(ft * (.5f + ph.x * .4f) + ph.y * 6.28f) * R * (1f + .5f * Mathf.Sin(ft * .11f)),
                            Mathf.Sin(ft * (.7f + ph.y * .5f) + ph.z * 6.28f) * R * .4f,
                            Mathf.Sin(ft * (.45f + ph.z * .4f) + ph.x * 6.28f) * R * (1f + .5f * Mathf.Cos(ft * .09f)));
                        birds[i].position = pos; birds[i].startSize = .55f; birds[i].startColor = new Color32(30, 28, 33, 255);
                        birds[i].remainingLifetime = 9999f; birds[i].startLifetime = 9999f;
                    }
                    starlings.SetParticles(birds, count);
                }
                else if (starlings.particleCount > 0) starlings.Clear();
            }

            // a hot-air balloon drifting across for the whole song
            if (balloon != null)
            {
                var on = here && balloonOn;
                balloon.gameObject.SetActive(on);
                if (on)
                {
                    var f = (float)((now - songStart) / songLength);
                    var side = Vector3.Cross(Vector3.up, dir);
                    balloon.position = center + dir * Mathf.Lerp(-750f, 750f, f) + side * 160f + Vector3.up * (95f + Mathf.Sin(f * 9f) * 8f);
                    balloon.rotation = Quaternion.Euler(0, f * 40f, 0);
                }
            }
        }
    }
}
