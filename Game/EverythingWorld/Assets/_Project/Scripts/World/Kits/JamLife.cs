using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Commonwealth at blue hour, answering the music:
    /// - the amphitheatre's moving lights swing and change colour, beams sweeping out over the lawn and into the
    ///   trees; they go wild when a Phish song is playing; the wash light cycles; the tethered balloon sways; the
    ///   metronome ticks; campfires crackle; sparkles drift round the floating album pieces;
    /// - "Harry Hood": a glowstick war, glowsticks arcing over the lawn, the lights turning a slow soft green and
    ///   white; "Free": big coloured balloons rise from the lawn; "The Divided Sky": Gamehendge wakes, the Rhombus
    ///   blazing, the Helping Friendly Book glowing, Wilson's window flickering, the light shifting between two
    ///   skies; "Run Like An Antelope": a herd of antelope runs across the meadow.
    public class JamLife : MonoBehaviour
    {
        const string Land = "The Jam Band Commonwealth";
        public Vector3 center, gate, lot, stage, phish, bookAt, gamehendge, meadow;
        public List<Vector3> fires = new List<Vector3>(), floaters = new List<Vector3>();
        public Transform[] pods = new Transform[0]; public Material[] podLens = new Material[0];
        public Material rhombus, book, wilsonWindow; public Light wash; public Transform balloon, metronome;

        CloudLayer clouds; ParticleSystem flame, glowsticks, balloons, sparkle;
        Material[] beamMats = new Material[0];
        readonly List<Transform> antelopes = new List<Transform>();
        Vector3 balloonHome; Quaternion[] podHome = new Quaternion[0];
        float hood, free, sky, antelope, show;
        long songKey = long.MinValue; double songStart, songLength = 1;
        float nextFlame, nextStick, nextBalloon, nextSparkle;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 45; clouds.span = 2400f; clouds.key = "jam";
            clouds.minHeight = 120f; clouds.maxHeight = 180f; clouds.size = 1.6f; clouds.speed = 3f; clouds.cover = .4f;
            clouds.color = new Color(.55f, .6f, .85f);
            flame = Particles("Fire", 800, new Color(1f, .55f, .2f, .9f), .3f, .8f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var fs = flame.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 12f; fs.radius = .4f; fs.rotation = new Vector3(-90f, 0, 0);
            glowsticks = Particles("Glowsticks", 2000, Color.white, .15f, .3f, true);
            var gm = glowsticks.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.6f); gm.startSpeed = new ParticleSystem.MinMaxCurve(12f, 18f); gm.gravityModifier = 1f;
            gm.startColor = new ParticleSystem.MinMaxGradient(new Color(.3f, 1f, .4f), new Color(1f, .3f, .8f));
            var gs = glowsticks.shape; gs.shapeType = ParticleSystemShapeType.Cone; gs.angle = 35f; gs.radius = 6f; gs.rotation = new Vector3(-90f, 0, 0);
            var gr = glowsticks.GetComponent<ParticleSystemRenderer>(); gr.renderMode = ParticleSystemRenderMode.Stretch; gr.velocityScale = .08f;
            balloons = Particles("Balloons", 400, Color.white, 1.4f, 2.2f, false);
            var bm = balloons.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f); bm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f); bm.gravityModifier = -.05f;
            bm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .3f, .4f), new Color(.3f, .6f, 1f));
            var bs = balloons.shape; bs.shapeType = ParticleSystemShapeType.Box; bs.scale = new Vector3(20f, .5f, 14f);
            sparkle = Particles("Sparkle", 800, new Color(.9f, .9f, 1f, 1f), .08f, .18f, true);
            var sm = sparkle.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f); sm.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .6f);
            var ss = sparkle.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 2.5f;
            // a beam cone under every moving light
            beamMats = new Material[pods.Length]; podHome = new Quaternion[pods.Length];
            for (var i = 0; i < pods.Length; i++)
            {
                podHome[i] = pods[i].rotation;
                var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
                const int seg = 8;
                for (var k = 0; k <= seg; k++)
                {
                    var a = k * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(d * .2f + Vector3.down * .3f); v.Add(d * 3.5f + Vector3.down * 34f);
                    col.Add(new Color(1, 1, 1, .5f)); col.Add(new Color(1, 1, 1, 0));
                    if (k < seg) { var b = k * 2; tri.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 }); }
                }
                var mesh = new Mesh { name = "Beam" }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
                var go = new GameObject("Beam"); go.transform.SetParent(pods[i], false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); beamMats[i] = Additive("Beam"); r.sharedMaterial = beamMats[i]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (balloon != null) balloonHome = balloon.position;
            // the antelope herd, waiting out of sight until their song
            var hide = WorldMaterials.Solid(new Color(.75f, .55f, .35f)); var belly = WorldMaterials.Solid(new Color(.95f, .9f, .82f)); var horn = WorldMaterials.Solid(new Color(.15f, .12f, .1f));
            for (var i = 0; i < 9; i++)
            {
                var mk = new MeshKit(); var rng = new System.Random(i);
                mk.Blob(hide, new Vector3(0, 1.1f, 0), new Vector3(.35f, .35f, .8f), rng, 0f); mk.Blob(belly, new Vector3(0, .92f, 0), new Vector3(.28f, .2f, .65f), rng, 0f);
                mk.Box(hide, new Vector3(0, 1.55f, .7f), new Vector3(.2f, .7f, .2f), Quaternion.Euler(-30f, 0, 0)); mk.Blob(hide, new Vector3(0, 1.95f, .95f), new Vector3(.15f, .17f, .28f), rng, 0f);
                foreach (var s in new[] { -.08f, .08f }) mk.Box(horn, new Vector3(s, 2.4f, .85f), new Vector3(.04f, .6f, .04f), Quaternion.Euler(-25f, 0, s * 80f));
                foreach (var (x, z) in new[] { (-.18f, -.55f), (.18f, -.55f), (-.18f, .5f), (.18f, .5f) }) mk.Box(hide, new Vector3(x, .45f, z), new Vector3(.07f, .9f, .07f), Quaternion.Euler(z > 0 ? 25f : -25f, 0, 0));
                var t = mk.BuildSingle("Antelope", transform, false).transform; t.gameObject.SetActive(false); antelopes.Add(t);
            }
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
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, add ? Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f) : (d < .9f ? 1f : 0f))); }
            tex.Apply();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = add ? Additive(name) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            if (!add) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; }
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            var isPhish = MusicDirector.ForcedTrackId != null || (playing && d.CurrentSlot.track.artist == "Phish");
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey) { songKey = song; songStart = start; songLength = System.Math.Max(1, length); }
            var progress = Mathf.Clamp01((float)((now - songStart) / songLength));
            if (MusicDirector.ForcedTrackId != null) progress = Mathf.Repeat(t / 20f, 1f);

            hood = Mathf.MoveTowards(hood, here && id == JamKit.Hood ? 1f : 0f, dt / 2f);
            free = Mathf.MoveTowards(free, here && id == JamKit.Free ? 1f : 0f, dt / 2f);
            sky = Mathf.MoveTowards(sky, here && (id == JamKit.DividedSky || id == JamKit.DividedSky2) ? 1f : 0f, dt / 3f);
            antelope = Mathf.MoveTowards(antelope, here && id == JamKit.Antelope ? 1f : 0f, dt / 1.5f);
            show = Mathf.MoveTowards(show, here && isPhish ? 1f : 0f, dt / 2f);

            var tint = new Color(.55f, .65f, 1f); var amount = .12f;
            if (sky > 0f) { tint = Color.Lerp(tint, Mathf.Repeat(t / 6f, 1f) < .5f ? new Color(.45f, .85f, 1f) : new Color(1f, .6f, .35f), sky); amount = Mathf.Lerp(amount, .5f, sky); }
            if (hood > 0f) { tint = Color.Lerp(tint, new Color(.55f, 1f, .6f), hood * .6f); amount = Mathf.Max(amount, .35f * hood); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .25f + .25f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the light show
            var speed = .4f + energy * .8f + show * 1.2f;
            for (var i = 0; i < pods.Length; i++)
            {
                if (pods[i] == null) continue;
                var swing = 20f + show * 25f;
                var aim = hood > .3f ? Quaternion.Euler(Mathf.Sin(t * .3f + i) * 8f + 25f, Mathf.Sin(t * .2f + i * .5f) * 10f, 0) : Quaternion.Euler(Mathf.Sin(t * speed + i * .7f) * swing + 20f, Mathf.Sin(t * speed * .8f + i * 1.3f) * swing * 1.4f, 0);
                pods[i].rotation = podHome[i] * aim;
                Color c;
                if (hood > .3f) c = Color.Lerp(new Color(.4f, 1f, .5f), Color.white, .5f + .5f * Mathf.Sin(t * .5f + i));
                else c = Color.HSVToRGB(Mathf.Repeat(i * .11f + t * (.03f + show * .08f), 1f), .75f, 1f);
                if (i < podLens.Length) podLens[i].SetColor("_EmissionColor", c * (1.5f + show * 1.5f + energy));
                beamMats[i].SetColor("_BaseColor", new Color(c.r, c.g, c.b, here ? (.25f + show * .45f + energy * .2f) * (Mathf.Repeat(t * (1f + show * 3f) + i * .37f, 1f) < .85f ? 1f : .2f) : 0f));
            }
            if (wash != null) wash.color = Color.HSVToRGB(Mathf.Repeat(t * .04f, 1f), .6f, 1f);
            if (balloon != null) { balloon.position = balloonHome + new Vector3(Mathf.Sin(t * .2f) * 2f, Mathf.Sin(t * .3f) * .8f, Mathf.Cos(t * .17f) * 2f); balloon.rotation = Quaternion.Euler(Mathf.Sin(t * .25f) * 3f, t * 2f, Mathf.Cos(t * .2f) * 3f); }
            if (metronome != null) metronome.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(metronome.forward, Vector3.up)) * Quaternion.Euler(0, 0, Mathf.Sin(t * (2f + energy * 3f)) * 30f);

            // Gamehendge
            if (rhombus != null) rhombus.SetColor("_EmissionColor", new Color(.55f, .85f, 1f) * (.9f + sky * (3f + Mathf.Sin(t * 4f))));
            if (book != null) book.SetColor("_EmissionColor", new Color(1f, .92f, .7f) * (.7f + sky * 3f));
            if (wilsonWindow != null) wilsonWindow.SetColor("_EmissionColor", new Color(1f, .4f, .2f) * (1f + sky * (2f * Mathf.PerlinNoise(t * 5f, 0f))));

            // antelope running across the meadow
            for (var i = 0; i < antelopes.Count; i++)
            {
                var on = antelope > .05f && meadow != Vector3.zero; antelopes[i].gameObject.SetActive(on);
                if (!on) continue;
                var dir = new Vector3(1f, 0, .35f).normalized; var side = Vector3.Cross(Vector3.up, dir);
                var x = Mathf.Repeat(t * 9f + i * 7f, 90f) - 45f;
                var p = meadow + dir * x + side * ((i % 3) - 1f) * 4f + side * Mathf.Sin(i * 1.7f) * 2f;
                var ray = Physics.Raycast(p + Vector3.up * 40f, Vector3.down, out var hit, 80f) ? hit.point.y : meadow.y;
                antelopes[i].SetPositionAndRotation(new Vector3(p.x, ray + Mathf.Abs(Mathf.Sin(t * 9f + i)) * .5f, p.z), Quaternion.LookRotation(dir) * Quaternion.Euler(Mathf.Sin(t * 9f + i) * 8f, 0, 0));
            }

            nextFlame -= dt;
            if (here && nextFlame <= 0f) { nextFlame = .1f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(2); } }
            nextStick -= dt;
            if (here && hood > .2f && nextStick <= 0f && phish != Vector3.zero) { nextStick = .04f; glowsticks.transform.position = Vector3.Lerp(stage, phish, .7f) + new Vector3(Random.Range(-10f, 10f), 1f, Random.Range(-10f, 10f)); glowsticks.Emit(3); }
            nextBalloon -= dt;
            if (here && free > .2f && nextBalloon <= 0f && phish != Vector3.zero) { nextBalloon = .25f; balloons.transform.position = phish + Vector3.up * 2f; balloons.Emit(2); }
            nextSparkle -= dt;
            if (here && nextSparkle <= 0f && floaters.Count > 0) { nextSparkle = .2f; foreach (var f in floaters) { sparkle.transform.position = f; sparkle.Emit(1); } if (sky > .2f && bookAt != Vector3.zero) { sparkle.transform.position = bookAt + Vector3.up; sparkle.Emit(6); } }
        }
    }
}
