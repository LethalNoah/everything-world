using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Commonwealth's live show and its song moments (part of JamLife). Every Phish song gives the light rig its
    /// own look: the named ones below are designed, every other song gets colours and motion picked from its id, so no
    /// two songs look the same. On top of the rig:
    /// - "The Divided Sky": the sky splits in two over the whole land, a warm half and a night half with a seam of
    ///   light between them, and Gamehendge wakes (the Rhombus blazes, the Book glows, the Mockingbird flies faster);
    /// - "Harry Hood": glowsticks over the lawn, soft green and white light; "Reba": the rig goes still and blue and
    ///   fireflies rise over the lawn; "Split Open and Melt": red, the lights sag and the beams drip; "The Squirming
    ///   Coil": the rig goes dark but one white spot on a grand piano that rises on the stage; "Wading in the Velvet
    ///   Sea": a purple mist rolls over the lawn; "It's Ice": snow falls on the amphitheatre in ice-blue light;
    ///   "Fire": flames along the stage front; "Birds of a Feather": a flock wheels over the lawn; "Fluffhead": a
    ///   confetti storm; "Light": every beam straight up, white; "Maze": the lanterns along the trails chase each
    ///   other; "Slave to the Traffic Light": the rig and the trail lanterns turn red, amber and green and build;
    ///   "Bold as Love": every colour at once; "Tube" and "Stash": strobes; "Free": balloons; "Run Like An Antelope":
    ///   the antelope run; "Farmhouse": the farmhouse on the album trail lights up.
    public partial class JamLife
    {
        public const string Reba = "35K3Lf0ueeQ38V3nJOJjuj", Melt = "2YKSgzOabJDgDzSmy8R5dy", Coil = "3wGNOxXUzeqC00i6E8GwwL", Velvet = "3WmN0R4loh6ft6B2guDyat",
            Ice = "2EtxqjLs4wSBTNtkm5VqkE", Ice2 = "4HlPpD7gpUq0ErOFyTHLtu", Fire = "4SbNEUCeq3VxUTlDkDhHmK", Birds = "0xCUU7DSGDTVOjiZEyy0md", Fluffhead = "15CLBWAL6PgRTIa58MH3aV",
            LightSong = "6NICO0xzjR2vw8p5IG96Wv", Maze = "0aJNDN5nBJYzQ4cJVUYX1l", Traffic = "0WgJNTk3buv78KISvRWUuF", BoldAsLove = "11PGbp7WUACuotYT668LHF", Tube = "5HnIdC5hPmGqT6BmmZ18Qz",
            Stash = "0M7qIbIfd9IQy68DibTqQ9", Stash2 = "7zf6pOzTt27uz3xo0AoFgL", Sand = "7AcfBtVaUefLhVA0pLkVA4", Waste = "6dKntZwVJx0QLX6IM19gC7", Joy = "6h5F9pWXWwQEbGvOipNEsi",
            Hydrogen = "3R4gpFLPSfvSGR78fCUhKg", Martian = "6Wyi0TWZOBxZNstaWcF7Ia", RockyTop = "4sSbBE1OeeyVGVlII94W6Z", WallsOfTheCave = "4woOgga4VlB4igYS9jhaTp", FarmhouseSong = "4BVKiPQ2Ix7mohDYo11wyO";

        // set by the kit
        public Transform piano; public Vector3 pianoHome, lawn, stageFront, stageSide; public Light pianoSpot;
        public List<Material> trailLights = new List<Material>(); public Material farmWindows;
        public List<Transform> crowd = new List<Transform>(); public Transform mockingbird; public Vector3 mountainTop;

        enum Mode { Sweep, Still, Strobe, Triplet, Traffic, Up, Spot, Droop, Rainbow }
        struct Look { public Color a, b; public float speed, swing; public Mode mode; }
        static readonly Dictionary<string, Look> looks = new Dictionary<string, Look>
        {
            [JamKit.Hood] = new Look { a = new Color(.4f, 1f, .5f), b = Color.white, speed = .3f, swing = 10f, mode = Mode.Still },
            [Reba] = new Look { a = new Color(.15f, .35f, 1f), b = new Color(.4f, .7f, 1f), speed = .15f, swing = 6f, mode = Mode.Still },
            [Melt] = new Look { a = new Color(1f, .1f, .08f), b = new Color(1f, .35f, .1f), speed = .5f, swing = 18f, mode = Mode.Droop },
            [Coil] = new Look { a = Color.white, b = Color.white, speed = .1f, swing = 0f, mode = Mode.Spot },
            [Velvet] = new Look { a = new Color(.55f, .2f, .9f), b = new Color(.9f, .4f, .8f), speed = .25f, swing = 14f, mode = Mode.Sweep },
            [Ice] = new Look { a = new Color(.6f, .85f, 1f), b = Color.white, speed = .6f, swing = 22f, mode = Mode.Sweep },
            [Ice2] = new Look { a = new Color(.6f, .85f, 1f), b = Color.white, speed = .6f, swing = 22f, mode = Mode.Sweep },
            [Fire] = new Look { a = new Color(1f, .4f, .05f), b = new Color(1f, .85f, .2f), speed = 1.6f, swing = 35f, mode = Mode.Sweep },
            [Birds] = new Look { a = new Color(.95f, .9f, .7f), b = new Color(.5f, .8f, 1f), speed = .8f, swing = 28f, mode = Mode.Sweep },
            [Fluffhead] = new Look { a = new Color(1f, .5f, .8f), b = new Color(.5f, .9f, 1f), speed = 1f, swing = 30f, mode = Mode.Rainbow },
            [LightSong] = new Look { a = Color.white, b = new Color(.85f, .9f, 1f), speed = .2f, swing = 0f, mode = Mode.Up },
            [Maze] = new Look { a = new Color(1f, .2f, .6f), b = new Color(.3f, 1f, .9f), speed = 2.2f, swing = 40f, mode = Mode.Sweep },
            [Traffic] = new Look { a = Color.red, b = Color.green, speed = .6f, swing = 20f, mode = Mode.Traffic },
            [BoldAsLove] = new Look { a = Color.red, b = Color.blue, speed = .7f, swing = 26f, mode = Mode.Rainbow },
            [Tube] = new Look { a = Color.white, b = new Color(1f, .3f, .9f), speed = 2.6f, swing = 40f, mode = Mode.Strobe },
            [Stash] = new Look { a = new Color(1f, .7f, .2f), b = new Color(.6f, .2f, 1f), speed = 1.2f, swing = 30f, mode = Mode.Triplet },
            [Stash2] = new Look { a = new Color(1f, .7f, .2f), b = new Color(.6f, .2f, 1f), speed = 1.2f, swing = 30f, mode = Mode.Triplet },
            [Sand] = new Look { a = new Color(1f, .78f, .35f), b = new Color(1f, .55f, .2f), speed = 1f, swing = 26f, mode = Mode.Sweep },
            [Waste] = new Look { a = new Color(1f, .6f, .25f), b = new Color(1f, .85f, .55f), speed = .2f, swing = 8f, mode = Mode.Still },
            [Joy] = new Look { a = new Color(1f, .5f, .65f), b = new Color(1f, .85f, .7f), speed = .3f, swing = 12f, mode = Mode.Still },
            [Hydrogen] = new Look { a = new Color(.6f, .8f, 1f), b = new Color(.9f, .95f, 1f), speed = .15f, swing = 6f, mode = Mode.Still },
            [Martian] = new Look { a = new Color(.3f, 1f, .2f), b = new Color(.7f, 1f, .3f), speed = 1.4f, swing = 34f, mode = Mode.Sweep },
            [RockyTop] = new Look { a = new Color(1f, .85f, .3f), b = new Color(1f, .5f, .15f), speed = 1.8f, swing = 30f, mode = Mode.Sweep },
            [WallsOfTheCave] = new Look { a = new Color(1f, .45f, .1f), b = new Color(.5f, .15f, .05f), speed = .5f, swing = 16f, mode = Mode.Sweep },
        };

        Look current; string lookFor;
        float reba, melt, coil, velvet, ice, fire, birds, fluff, maze, traffic, split, farm;
        ParticleSystem fireflies, mist, snow, confetti, stageFire;
        readonly List<Transform> flock = new List<Transform>(); readonly List<Quaternion> crowdBase = new List<Quaternion>();
        Transform skyWarm, skyCool, seam; Material skyWarmM, skyCoolM, seamM;
        float nextFirefly, nextMist, nextSnow, nextConfetti, nextStageFire;

        static Material AlphaMat(string name, Color c, bool add)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", add ? 2f : 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(add ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 50; m.SetColor("_BaseColor", c);
            return m;
        }

        /// A piece of the sky dome (radius 1): longitudes a0..a1 (radians), from just below the horizon to the top.
        static Mesh Dome(float a0, float a1)
        {
            var v = new List<Vector3>(); var tri = new List<int>(); const int nl = 16, nh = 10;
            for (var i = 0; i <= nl; i++) for (var j = 0; j <= nh; j++)
            {
                var a = Mathf.Lerp(a0, a1, i / (float)nl); var e = Mathf.Lerp(-.12f, Mathf.PI / 2f, j / (float)nh);
                v.Add(new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e)));
                if (i < nl && j < nh) { var b = i * (nh + 1) + j; tri.AddRange(new[] { b, b + nh + 1, b + 1, b + 1, b + nh + 1, b + nh + 2 }); }
            }
            var m = new Mesh { name = "Sky half" }; m.SetVertices(v); m.SetColors(Enumerable.Repeat(Color.white, v.Count).ToList()); m.SetTriangles(tri, 0); m.RecalculateBounds(); m.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            return m;
        }

        Transform SkyPiece(string name, Mesh mesh, Material m)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.SetActive(false); return go.transform;
        }

        void ShowStart()
        {
            fireflies = Particles("Fireflies", 600, new Color(1f, .95f, .5f, 1f), .08f, .16f, true);
            var fm = fireflies.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); fm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f);
            var fn = fireflies.noise; fn.enabled = true; fn.strength = .6f; fn.frequency = .3f;
            mist = Particles("Velvet mist", 400, new Color(.5f, .22f, .8f, .35f), 3f, 6f, true);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f);
            snow = Particles("Snow", 1500, new Color(1f, 1f, 1f, .9f), .08f, .16f, true);
            var sm = snow.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f); sm.startSpeed = 0f; sm.gravityModifier = .06f;
            var sn = snow.noise; sn.enabled = true; sn.strength = .5f; sn.frequency = .2f;
            confetti = Particles("Confetti", 1500, Color.white, .15f, .3f, false);
            var cm = confetti.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f); cm.startSpeed = new ParticleSystem.MinMaxCurve(8f, 14f); cm.gravityModifier = .35f;
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .3f, .6f), new Color(.3f, .8f, 1f)); cm.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var cs = confetti.shape; cs.shapeType = ParticleSystemShapeType.Cone; cs.angle = 30f; cs.radius = 1f; cs.rotation = new Vector3(-70f, 0, 0);
            stageFire = Particles("Stage fire", 600, new Color(1f, .5f, .1f, .9f), .6f, 1.4f, true);
            var sf = stageFire.main; sf.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1f); sf.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            var sfs = stageFire.shape; sfs.shapeType = ParticleSystemShapeType.Cone; sfs.angle = 8f; sfs.radius = .3f; sfs.rotation = new Vector3(-90f, 0, 0);
            // a flock for "Birds of a Feather"
            var feather = WorldMaterials.Solid(new Color(.15f, .15f, .18f));
            for (var i = 0; i < 14; i++)
            {
                var mk = new MeshKit();
                mk.Blob(feather, Vector3.zero, new Vector3(.12f, .1f, .3f), new System.Random(i), 0f);
                foreach (var s in new[] { -1f, 1f }) { mk.Tri(feather, Vector3.zero, new Vector3(s * .8f, .1f, -.1f), new Vector3(0, 0, -.25f), Vector3.up); mk.Tri(feather, Vector3.zero, new Vector3(0, 0, -.25f), new Vector3(s * .8f, .1f, -.1f), Vector3.down); }
                var t = mk.BuildSingle("Bird", transform, false).transform; t.gameObject.SetActive(false); flock.Add(t);
            }
            // the divided sky: a warm half and a night half, and a seam of light between them
            skyWarmM = AlphaMat("Sky warm half", new Color(1f, .55f, .25f, 0f), false); skyCoolM = AlphaMat("Sky night half", new Color(.12f, .2f, .6f, 0f), false); seamM = AlphaMat("Sky seam", new Color(1f, .95f, .8f, 0f), true);
            skyWarm = SkyPiece("Divided sky (warm)", Dome(0f, Mathf.PI), skyWarmM); skyCool = SkyPiece("Divided sky (night)", Dome(Mathf.PI, Mathf.PI * 2f), skyCoolM);
            var sv = new List<Vector3>(); var st = new List<int>(); const int n = 48;
            for (var k = 0; k <= n; k++) { var e = Mathf.Lerp(-.1f, Mathf.PI + .1f, k / (float)n); foreach (var w in new[] { -.025f, .025f }) sv.Add(new Vector3(Mathf.Cos(e), Mathf.Sin(e), w)); if (k < n) { var b = k * 2; st.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); } }
            var seamMesh = new Mesh { name = "Sky seam" }; seamMesh.SetVertices(sv); seamMesh.SetColors(Enumerable.Repeat(Color.white, sv.Count).ToList()); seamMesh.SetTriangles(st, 0); seamMesh.RecalculateBounds(); seamMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            seam = SkyPiece("Divided sky (seam)", seamMesh, seamM);
        }

        Look LookFor(string id)
        {
            if (id != null && looks.TryGetValue(id, out var l)) return l;
            // any other song: colours and motion from its id, so every song looks its own
            var h = 0; if (id != null) foreach (var ch in id) h = (h * 31 + ch) & 0x7fffffff;
            var hue = (h % 360) / 360f; var modes = new[] { Mode.Sweep, Mode.Sweep, Mode.Still, Mode.Rainbow, Mode.Sweep };
            return new Look { a = Color.HSVToRGB(hue, .8f, 1f), b = Color.HSVToRGB(Mathf.Repeat(hue + .12f + (h % 7) * .05f, 1f), .7f, 1f), speed = .3f + (h % 9) * .12f, swing = 12f + (h % 5) * 6f, mode = modes[h % modes.Length] };
        }

        /// The rig, every frame: aim and colour each moving light.
        void ShowRig(float t, string id, bool isPhish, float energy, bool here)
        {
            if (id != lookFor) { lookFor = id; current = isPhish ? LookFor(id) : new Look { a = new Color(1f, .7f, .4f), b = new Color(.6f, .5f, 1f), speed = .4f, swing = 18f, mode = Mode.Sweep }; }
            var L = current; var speed = L.speed * (1f + energy * .5f) * (.6f + show * .6f);
            for (var i = 0; i < pods.Length; i++)
            {
                if (pods[i] == null) continue;
                Quaternion aim; var swing = L.swing * (.8f + show * .4f);
                switch (L.mode)
                {
                    case Mode.Up: aim = Quaternion.Euler(-90f + Mathf.Sin(t * .2f + i) * 3f, 0, 0); break;
                    case Mode.Droop: aim = Quaternion.Euler(40f + Mathf.Sin(t * speed + i * .7f) * swing * .4f, Mathf.Sin(t * speed * .6f + i) * swing, 0); break;
                    case Mode.Still: aim = Quaternion.Euler(25f + Mathf.Sin(t * .2f + i) * swing * .5f, Mathf.Sin(t * .15f + i * .5f) * swing, 0); break;
                    default: aim = Quaternion.Euler(Mathf.Sin(t * speed + i * .7f) * swing + 20f, Mathf.Sin(t * speed * .8f + i * 1.3f) * swing * 1.4f, 0); break;
                }
                pods[i].rotation = podHome[i] * aim;
                Color c; var on = 1f;
                switch (L.mode)
                {
                    case Mode.Rainbow: c = Color.HSVToRGB(Mathf.Repeat(i / (float)pods.Length + t * speed * .2f, 1f), .9f, 1f); break;
                    case Mode.Traffic: { var phase = Mathf.FloorToInt(t * (.5f + traffic * 1.5f) + i * .02f) % 3; c = phase == 0 ? new Color(1f, .1f, .05f) : phase == 1 ? new Color(1f, .7f, .05f) : new Color(.1f, 1f, .2f); break; }
                    case Mode.Strobe: c = Color.Lerp(L.a, L.b, Mathf.PingPong(i * .3f, 1f)); on = Mathf.Repeat(t * 8f + i * .13f, 1f) < .5f ? 1f : .05f; break;
                    case Mode.Triplet: c = Color.Lerp(L.a, L.b, Mathf.PingPong(i * .3f, 1f)); { var beat = Mathf.Repeat(t * 1.5f, 2f); on = beat < 1.2f && Mathf.Repeat(beat, .4f) < .2f ? 1f : .15f; } break;
                    case Mode.Spot: c = Color.white; on = i == pods.Length / 2 ? 1f : .02f; break;
                    default: c = Color.Lerp(L.a, L.b, .5f + .5f * Mathf.Sin(t * speed + i * .9f)); break;
                }
                if (hood > .3f) c = Color.Lerp(new Color(.4f, 1f, .5f), Color.white, .5f + .5f * Mathf.Sin(t * .5f + i));
                if (i < podLens.Length) podLens[i].SetColor("_EmissionColor", c * (1.5f + show * 1.5f + energy) * on);
                var flicker = L.mode == Mode.Sweep || L.mode == Mode.Rainbow ? (Mathf.Repeat(t * (1f + show * 3f) + i * .37f, 1f) < .85f ? 1f : .3f) : 1f;
                beamMats[i].SetColor("_BaseColor", new Color(c.r, c.g, c.b, here ? (.18f + show * .35f + energy * .15f) * on * flicker * (L.mode == Mode.Droop ? .7f + .3f * Mathf.Sin(t * 3f + i) : 1f) : 0f));
            }
        }

        static Vector3 Disc(float r) { var a = Random.value * Mathf.PI * 2f; var d = Mathf.Sqrt(Random.value) * r; return new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d); }

        void ShowMoments(float dt, float t, double now, string id, bool here, float progress)
        {
            float To(float v, bool on, float secs) => Mathf.MoveTowards(v, here && on ? 1f : 0f, dt / secs);
            reba = To(reba, id == Reba, 3f); melt = To(melt, id == Melt, 2f); coil = To(coil, id == Coil, 3f); velvet = To(velvet, id == Velvet, 4f);
            ice = To(ice, id == Ice || id == Ice2, 3f); fire = To(fire, id == Fire, 1f); birds = To(birds, id == Birds, 2f); fluff = To(fluff, id == Fluffhead, 1f);
            maze = To(maze, id == Maze, 1f); traffic = To(traffic, id == Traffic, 2f); farm = To(farm, id == FarmhouseSong, 2f);
            split = To(split, id == JamKit.DividedSky || id == JamKit.DividedSky2, 6f);
            var cam = Camera.main;
            // fireflies: a few always drift round you in the woods; Reba fills the lawn with them
            nextFirefly -= dt;
            if (here && cam != null) while (nextFirefly <= 0f)
            {
                nextFirefly += .12f / (1f + reba * 12f);
                var at = reba > .2f && lawn != Vector3.zero && Random.value < .7f ? lawn + Disc(18f) + Vector3.up * Random.Range(.5f, 4f) : cam.transform.position + Disc(25f) + Vector3.up * Random.Range(-.5f, 3f);
                fireflies.transform.position = at; fireflies.Emit(1);
            }
            nextMist -= dt;
            if (velvet > .1f && lawn != Vector3.zero) while (nextMist <= 0f) { nextMist += .06f; mist.transform.position = lawn + Disc(20f) + Vector3.up * .6f; mist.Emit(1); }
            nextSnow -= dt;
            if (ice > .1f && lawn != Vector3.zero) while (nextSnow <= 0f) { nextSnow += .01f; snow.transform.position = lawn + Disc(28f) + Vector3.up * 22f; snow.Emit(1); }
            nextConfetti -= dt;
            if (fluff > .3f && stage != Vector3.zero && nextConfetti <= 0f) { nextConfetti = .05f; confetti.transform.SetPositionAndRotation(stageFront + stageSide * Random.Range(-10f, 10f), Quaternion.LookRotation((phish - stage).normalized)); confetti.Emit(6); }
            nextStageFire -= dt;
            if (fire > .3f && stage != Vector3.zero && nextStageFire <= 0f) { nextStageFire = .08f; for (var k = -2; k <= 2; k++) { stageFire.transform.position = stageFront + stageSide * k * 5f; stageFire.Emit(2); } }
            // the flock wheels over the lawn
            for (var i = 0; i < flock.Count; i++)
            {
                var on = birds > .05f && lawn != Vector3.zero; flock[i].gameObject.SetActive(on); if (!on) continue;
                var a = t * .5f + i * .45f; var r = 14f + Mathf.Sin(i * 1.7f) * 4f;
                var p = lawn + new Vector3(Mathf.Cos(a) * r, 14f + Mathf.Sin(t * .8f + i) * 2f + (1f - birds) * 30f, Mathf.Sin(a) * r);
                flock[i].SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 10f + i) * 25f));
            }
            // the piano rises for The Squirming Coil, one spot on it
            if (piano != null) { piano.gameObject.SetActive(coil > .02f); piano.position = pianoHome + Vector3.up * (-2.4f + coil * 2.4f); }
            if (pianoSpot != null) pianoSpot.intensity = coil * 6f;
            // the lanterns along the trails: steady, chasing for Maze, red-amber-green for the Traffic Light
            for (var k = 0; k < trailLights.Count; k++)
            {
                Color c = new Color(1f, .78f, .45f) * 2.4f;
                if (maze > .05f) c = Color.Lerp(c, Color.HSVToRGB(Mathf.Repeat(k / 6f + t * .3f, 1f), .8f, 1f) * (Mathf.Repeat(t * 6f - k, 6f) < 1.2f ? 6f : .4f), maze);
                if (traffic > .05f) { var phase = Mathf.FloorToInt(t * (.5f + traffic) + k * .5f) % 3; c = Color.Lerp(c, (phase == 0 ? Color.red : phase == 1 ? new Color(1f, .7f, .05f) : Color.green) * 3.5f, traffic); }
                trailLights[k].SetColor("_EmissionColor", c);
            }
            if (farmWindows != null) farmWindows.SetColor("_EmissionColor", new Color(1f, .78f, .45f) * (1f + farm * 5f));
            // the crowd sways, more when Phish is on
            if (crowdBase.Count != crowd.Count) { crowdBase.Clear(); foreach (var c in crowd) crowdBase.Add(c.rotation); }
            for (var i = 0; i < crowd.Count; i++) crowd[i].rotation = crowdBase[i] * Quaternion.Euler(Mathf.Sin(t * (1.2f + show * 1.5f) + i * 1.3f) * (2f + show * 4f), 0, Mathf.Cos(t * (1f + show) + i) * (2f + show * 4f));
            // the Famous Mockingbird flies between Icculus's mountain and the Book, faster when the sky divides
            if (mockingbird != null && bookAt != Vector3.zero && mountainTop != Vector3.zero)
            {
                var k = Mathf.PingPong((float)(now * (.04f + split * .1f)), 1f); var arc = Vector3.Lerp(mountainTop, bookAt + Vector3.up * 3f, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 10f;
                var dir = (bookAt - mountainTop) * (Mathf.Repeat((float)(now * (.04f + split * .1f)), 2f) < 1f ? 1f : -1f); dir.y = 0;
                mockingbird.SetPositionAndRotation(arc, Quaternion.LookRotation(dir.sqrMagnitude > .01f ? dir.normalized : Vector3.forward) * Quaternion.Euler(0, 0, Mathf.Sin(t * 9f) * 20f));
            }
            // the divided sky: the seam opens first, then the halves fill with their colours
            var showSky = split > .01f && cam != null; skyWarm.gameObject.SetActive(showSky); skyCool.gameObject.SetActive(showSky); seam.gameObject.SetActive(showSky);
            if (showSky)
            {
                // the seam runs up from the horizon straight ahead of you, over the top and down behind: warm sky on one side, night on the other
                var fwd = cam.transform.forward; fwd.y = 0; if (fwd.sqrMagnitude < .01f) fwd = Vector3.forward;
                var yaw = Quaternion.LookRotation(Vector3.Cross(Vector3.up, fwd.normalized)); var p = cam.transform.position; const float R = 600f;
                foreach (var tr in new[] { skyWarm, skyCool, seam }) { tr.SetPositionAndRotation(p, yaw); tr.localScale = Vector3.one * R; }
                var halves = Mathf.SmoothStep(0, 1, Mathf.Clamp01(split * 1.6f - .4f));
                skyWarmM.SetColor("_BaseColor", new Color(1f, .5f, .2f, .65f * halves)); skyCoolM.SetColor("_BaseColor", new Color(.04f, .06f, .25f, .75f * halves));
                seamM.SetColor("_BaseColor", new Color(1f, .97f, .85f, Mathf.Clamp01(split * 2f)) * (1.6f + .3f * Mathf.Sin(t * 2f)));
            }
        }

        public float SplitAmount => split;
        public float MeltAmount => melt;
        public float IceAmount => ice;
        public float VelvetAmount => velvet;
        public float RebaAmount => reba;
    }
}
