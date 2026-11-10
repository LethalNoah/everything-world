using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Alt Rock Moors under their moody sky, answering the music:
    /// - grey cloud races over and golden breaks of sun drift slowly across the moor (from the shared clock, so friends
    ///   see the same light); the transmitter's warning lights blink; chimneys smoke; in the transmitter station the
    ///   song titles change on the walls and the televisions flicker with static; the drowned windows glow under the
    ///   pool; glowing fish swim through the air above it; the cube out of place sits a little wrong; a pale figure in
    ///   the grove flickers in and out; an old car drives the lonely road; Gorillaz' island floats, its sails turning;
    /// - "Everything In Its Right Place": the sky goes red, the cubes glow and the odd one slides into place, the ice
    ///   peaks glow red; "Pyramid Song": the light turns deep blue-green and the drowned village lights up; "Weird
    ///   Fishes / Arpeggi": the fish swarm and blaze; "Karma Police": dusk falls and the car's headlights come on as it
    ///   drives; "How to Disappear Completely": white mist swallows the moor and the figure stands clear; "Videotape":
    ///   the sky darkens and the televisions blaze; "Feel Good Inc.": the windmill spins hard; "Champagne Supernova":
    ///   the supernova over the bottle bursts bright.
    public class MoorsLife : MonoBehaviour
    {
        const string Land = "The Alt Rock Moors";
        static readonly string[] Titles = { "EVERYTHING IN ITS RIGHT PLACE", "KARMA POLICE", "LET DOWN", "PARANOID ANDROID", "PYRAMID SONG", "VIDEOTAPE", "OPTIMISTIC", "MAN OF WAR", "ILL WIND", "THE BENDS", "CREEP", "2 + 2 = 5", "WEIRD FISHES", "JIGSAW FALLING INTO PLACE", "HOW TO DISAPPEAR COMPLETELY" };
        public Vector3 center, mast, pool, oddHome, oddAway, islandBase, novaAt;
        public Quaternion oddRot = Quaternion.identity;
        public List<Vector3> chimneys = new List<Vector3>(), warning = new List<Vector3>();
        public Vector3[] road = new Vector3[0];
        public List<TextMesh> words = new List<TextMesh>();
        public Material static_, cubeGlow, drowned, ice, figureM, nova;
        public Transform oddCube, figure, island, sails; public Renderer water;

        CloudLayer clouds; ParticleSystem smoke; Material shaftM, warnM;
        readonly List<Transform> breaks = new List<Transform>(), fish = new List<Transform>(); readonly List<Vector3> fishSeed = new List<Vector3>();
        Transform car; Material headlights; float[] roadLen = new float[0]; float roadTotal;
        float right, pyramid, fishes, karma, disappear, video, feel, supernova, odd;
        float nextSmoke; int lastWord = -1;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 60; clouds.span = 2400f; clouds.key = "moors";
            clouds.minHeight = 120f; clouds.maxHeight = 170f; clouds.size = 2.4f; clouds.speed = 4f; clouds.cover = .75f;
            clouds.color = new Color(.72f, .74f, .77f);
            smoke = Particles("Chimney smoke", 300, new Color(.7f, .7f, .72f, .35f), .5f, 1.2f);
            var km = smoke.main; km.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); km.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.2f);
            // the breaks of sun: wide crossed sheets of golden light, drifting across the moor
            shaftM = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Sun breaks" };
            shaftM.SetFloat("_Surface", 1f); shaftM.SetFloat("_Blend", 0f); shaftM.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); shaftM.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            shaftM.SetFloat("_ZWrite", 0f); shaftM.SetFloat("_Cull", 0f); shaftM.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); shaftM.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            var tex = new Texture2D(8, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 8; x++) { var e = Mathf.Sin((x + .5f) / 8f * Mathf.PI); var v = y / 31f; tex.SetPixel(x, y, new Color(1, 1, 1, e * Mathf.SmoothStep(0f, .06f, v) * Mathf.SmoothStep(1f, .5f, v))); }
            tex.Apply(); shaftM.SetTexture("_BaseMap", tex); shaftM.SetColor("_BaseColor", new Color(1f, .85f, .5f, .3f));
            for (var i = 0; i < 6; i++)
            {
                var mesh = new Mesh { name = "Sun break" }; var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var cols = new List<Color>(); var tris = new List<int>();
                var lean = new Vector3(.35f, 1f, -.2f).normalized; var w = 16f + i * 2f; const float len = 140f;
                for (var s = 0; s < 3; s++)
                {
                    var across = Quaternion.Euler(0, s * 60f, 0) * Vector3.right; var b = verts.Count;
                    verts.Add(-across * w * .5f); verts.Add(across * w * .5f); verts.Add(lean * len + across * w * .9f); verts.Add(lean * len - across * w * .9f);
                    uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                    for (var k = 0; k < 4; k++) cols.Add(Color.white);
                    tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                }
                mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetColors(cols); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds(); mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 400f);
                var go = new GameObject("Sun break"); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = shaftM; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; breaks.Add(go.transform);
            }
            // the warning lights
            warnM = new Material(WorldMaterials.Glow(MoorsKit.Signal, 2f)) { name = "Warning lights" };
            var wk = new MeshKit(); foreach (var p in warning) wk.Blob(warnM, p, Vector3.one * .45f, new System.Random(1), 0f);
            if (warning.Count > 0) wk.BuildSingle("Warning lights", transform, false);
            // the fish over the sunken pool
            Color[] fc = { new Color(.3f, .8f, 1f), new Color(1f, .55f, .2f), new Color(.6f, 1f, .7f), new Color(1f, .4f, .7f) };
            for (var i = 0; i < 24; i++)
            {
                var m = new Material(WorldMaterials.Glow(fc[i % fc.Length], 1.4f)) { name = "Weird fish" }; var f = new MeshKit();
                f.Blob(m, Vector3.zero, new Vector3(.18f, .26f, .5f), new System.Random(i), 0f); f.Tri(m, new Vector3(0, 0, -.45f), new Vector3(0, .25f, -.8f), new Vector3(0, -.25f, -.8f), Vector3.right); f.Tri(m, new Vector3(0, 0, -.45f), new Vector3(0, -.25f, -.8f), new Vector3(0, .25f, -.8f), Vector3.left);
                var t = f.BuildSingle("Fish", transform, false).transform; fish.Add(t); fishSeed.Add(new Vector3(4f + (i * 7 % 10), 2f + (i * 5 % 8), i * .9f));
            }
            // the old car on the lonely road
            if (road.Length > 1)
            {
                roadLen = new float[road.Length]; for (var i = 1; i < road.Length; i++) roadLen[i] = roadLen[i - 1] + Vector3.Distance(road[i - 1], road[i]); roadTotal = roadLen[road.Length - 1];
                var ck = new MeshKit(); var body = WorldMaterials.Solid(new Color(.55f, .58f, .55f), .6f); var darkM = WorldMaterials.Solid(new Color(.08f, .08f, .09f));
                headlights = new Material(WorldMaterials.Glow(new Color(1f, .95f, .8f), .2f)) { name = "Headlights" };
                ck.Box(body, new Vector3(0, .65f, 0), new Vector3(1.7f, .6f, 4.2f), Quaternion.identity); ck.Box(body, new Vector3(0, 1.2f, -.3f), new Vector3(1.5f, .55f, 2f), Quaternion.identity);
                ck.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), new Vector3(0, 1.2f, -.3f), new Vector3(1.52f, .4f, 1.9f), Quaternion.identity);
                foreach (var s in new[] { -.85f, .85f }) foreach (var z in new[] { -1.35f, 1.35f }) ck.Prism(darkM, new Vector3(s, .35f, z), Vector3.right * Mathf.Sign(s), .35f, .2f, 10);
                foreach (var s in new[] { -.55f, .55f }) ck.Prism(headlights, new Vector3(s, .7f, 2.1f), Vector3.forward, .15f, .05f, 10);
                car = ck.BuildSingle("The car on the lonely road", transform, false).transform;
            }
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1)
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
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetTexture("_BaseMap", tex); var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            right = Mathf.MoveTowards(right, here && id == MoorsKit.RightPlace ? 1f : 0f, dt / 3f);
            pyramid = Mathf.MoveTowards(pyramid, here && id == MoorsKit.PyramidSong ? 1f : 0f, dt / 4f);
            fishes = Mathf.MoveTowards(fishes, here && id == MoorsKit.WeirdFishes ? 1f : 0f, dt / 2f);
            karma = Mathf.MoveTowards(karma, here && id == MoorsKit.KarmaPolice ? 1f : 0f, dt / 4f);
            disappear = Mathf.MoveTowards(disappear, here && id == MoorsKit.Disappear ? 1f : 0f, dt / 5f);
            video = Mathf.MoveTowards(video, here && id == MoorsKit.Videotape ? 1f : 0f, dt / 4f);
            feel = Mathf.MoveTowards(feel, here && id == MoorsKit.FeelGood ? 1f : 0f, dt / 2f);
            supernova = Mathf.MoveTowards(supernova, here && id == MoorsKit.Supernova ? 1f : 0f, dt / 2f);
            odd = Mathf.MoveTowards(odd, right > .5f ? 1f : 0f, dt / 6f);

            var tint = new Color(.92f, .93f, .96f); var amount = .08f;
            void Mix(Color c, float k, float a) { if (k <= 0f) return; tint = Color.Lerp(tint, c, k); amount = Mathf.Lerp(amount, a, k); }
            Mix(new Color(1f, .35f, .3f), right, .5f); Mix(new Color(.3f, .6f, .72f), pyramid, .5f); Mix(new Color(.4f, .55f, .9f), fishes * .7f, .35f);
            Mix(new Color(.55f, .45f, .8f), karma, .5f); Mix(new Color(.96f, .97f, 1f), disappear, .65f); Mix(new Color(.28f, .3f, .4f), video, .55f); Mix(new Color(1f, .85f, .6f), supernova * .7f, .4f);
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, 0f);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .5f + .3f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the breaks of sun drift across the moor with the wind, from the shared clock
            var wind = clouds != null ? new Vector3(clouds.Wind.x, 0, clouds.Wind.y) : Vector3.right; if (wind.sqrMagnitude < .01f) wind = Vector3.right; wind.Normalize();
            var cross = Vector3.Cross(Vector3.up, wind);
            for (var i = 0; i < breaks.Count; i++)
            {
                breaks[i].gameObject.SetActive(here && video < .5f && disappear < .5f);
                if (!here) continue;
                var along = Mathf.Repeat((float)(now * 3.5) + i * 133f, 800f) - 400f; var p = center + wind * along + cross * ((i - 2.5f) * 90f + Mathf.Sin(i * 1.7f) * 40f);
                p.y = center.y - 5f; breaks[i].position = p;
            }
            shaftM.SetColor("_BaseColor", new Color(1f, .85f, .5f, .32f + .08f * Mathf.Sin(t * .3f) + supernova * .15f));
            // the warning lights blink together, a slow beat
            if (warnM != null) warnM.SetColor("_EmissionColor", MoorsKit.Signal * (Mathf.Repeat((float)now, 1.6f) < .5f ? 4f : .15f));
            // the transmitter station: the titles change, the televisions flicker
            var wi = Mathf.FloorToInt((float)(now / 4.0));
            if (wi != lastWord && words.Count > 0) { lastWord = wi; for (var k = 0; k < words.Count; k++) if (words[k] != null) words[k].text = Titles[(wi * 3 + k * 5) % Titles.Length]; }
            for (var k = 0; k < words.Count; k++) if (words[k] != null) { var c = words[k].color; c.a = .6f + .4f * Mathf.PerlinNoise(t * 3f, k); words[k].color = c; }
            if (static_ != null) static_.SetColor("_EmissionColor", new Color(.75f, .8f, .85f) * (.5f + Random.value * .5f + video * 2.5f));
            // the cubes
            if (cubeGlow != null) cubeGlow.SetColor("_EmissionColor", Color.Lerp(new Color(.95f, .95f, .95f) * .08f, new Color(1f, .25f, .2f) * 1.6f, right));
            if (oddCube != null) oddCube.SetPositionAndRotation(Vector3.Lerp(oddAway, oddHome, Mathf.SmoothStep(0, 1, odd)), Quaternion.Slerp(oddRot * Quaternion.Euler(0, 23f, 0), oddRot, Mathf.SmoothStep(0, 1, odd)));
            if (ice != null) ice.SetColor("_EmissionColor", Color.Lerp(new Color(.75f, .88f, 1f) * .2f, new Color(1f, .3f, .25f) * 1.4f, right));
            // the pool
            if (drowned != null) drowned.SetColor("_EmissionColor", new Color(.5f, .9f, .85f) * (.5f + .2f * Mathf.Sin(t * .7f) + pyramid * 2.5f));
            for (var i = 0; i < fish.Count; i++)
            {
                var show = here && pool != Vector3.zero && (i < 12 || fishes > .3f); fish[i].gameObject.SetActive(show); if (!show) continue;
                var s = fishSeed[i]; var speed = (.25f + (i % 4) * .05f) * (1f + fishes * 1.5f); var a = t * speed + s.z;
                var p = pool + new Vector3(Mathf.Cos(a) * s.x, s.y + Mathf.Sin(t * 1.3f + i) * .8f + Mathf.Sin(a * 3f) * .6f, Mathf.Sin(a) * s.x);
                fish[i].SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), Mathf.Cos(a * 3f) * .3f, Mathf.Cos(a))) * Quaternion.Euler(0, Mathf.Sin(t * 8f + i) * 12f, 0));
            }
            // the figure in the grove
            if (figure != null)
            {
                var flick = disappear > .3f || Mathf.PerlinNoise(t * .7f, 3.3f) > .45f; figure.gameObject.SetActive(here && flick);
                if (figureM != null) figureM.SetColor("_EmissionColor", new Color(.85f, .9f, 1f) * (.6f + disappear * 2.4f));
            }
            // the car on the lonely road
            if (car != null)
            {
                car.gameObject.SetActive(here);
                if (here)
                {
                    var speed = 4f + karma * 6f; var s = Mathf.PingPong((float)(now * speed % (roadTotal * 2f)), roadTotal); var back = Mathf.Repeat((float)(now * speed), roadTotal * 2f) > roadTotal;
                    var lo = 0; var hi = road.Length - 1; while (hi - lo > 1) { var mid = (lo + hi) / 2; if (roadLen[mid] < s) lo = mid; else hi = mid; }
                    var f = (s - roadLen[lo]) / Mathf.Max(.001f, roadLen[hi] - roadLen[lo]); var dir = road[hi] - road[lo]; if (back) dir = -dir; dir.y = 0;
                    car.SetPositionAndRotation(Vector3.Lerp(road[lo], road[hi], f) + Vector3.up * .25f, Quaternion.LookRotation(dir.sqrMagnitude > .001f ? dir : Vector3.forward));
                    headlights.SetColor("_EmissionColor", new Color(1f, .95f, .8f) * (.2f + karma * 5f));
                }
            }
            // Gorillaz' island, Oasis' supernova
            if (island != null) island.SetPositionAndRotation(islandBase + Vector3.up * Mathf.Sin(t * .5f) * .8f, Quaternion.Euler(Mathf.Sin(t * .3f) * 3f, t * 3f, Mathf.Cos(t * .4f) * 3f));
            if (sails != null) sails.localRotation = Quaternion.Euler(0, 0, -(float)(now * (40f + feel * 300f) % 360.0));
            if (nova != null) nova.SetColor("_EmissionColor", new Color(1f, .9f, .6f) * (1.4f + .3f * Mathf.Sin(t * 1.5f) + supernova * (6f + 3f * Mathf.Sin(t * 6f))));

            nextSmoke -= dt;
            if (here && nextSmoke <= 0f) { nextSmoke = .7f; foreach (var c in chimneys) { smoke.transform.position = c; smoke.Emit(1); } }
        }
    }
}
