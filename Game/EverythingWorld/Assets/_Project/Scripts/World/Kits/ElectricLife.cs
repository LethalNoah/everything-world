using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Electric Fields, alive with the music:
    /// - a golden festival dusk, warmer for happy songs, a little violet for sad ones; soft peach cloud;
    /// - the Big Top's disco ball turns, throwing beams; lasers sweep from the main stage, faster and brighter in
    ///   loud songs; the Ferris wheel and the carousel turn; the festoon lights and the LED wall pulse with the
    ///   music;
    /// - "Kids": fireworks over the fields the whole song long, and confetti blasting out of the Big Top;
    ///   "Electric Feel": electric blue runs along every string of lights and the neon eel blazes;
    ///   "Disparate Youth": flame cannons on the main stage; LCD Soundsystem's dome sits dim until one of their
    ///   songs plays, then its mirror ball spins and every light in it comes on;
    /// - now and then (shared clock): a short fireworks show in a song, and a hot-air balloon drifting over.
    public class ElectricLife : MonoBehaviour
    {
        const string Land = "The Electric Fields";
        public Vector3 center, gate, mainStage, mainStageFront = Vector3.forward, lcdAt;
        public Material[] festoon = new Material[0];
        public Material mirrorTiles, eel, ledWall, lcdTiles, screen, wheelBulbs;
        public Transform discoBall, carousel, lcdBall, ferris;
        public List<Transform> gondolas = new List<Transform>();
        public List<Light> lcdLights = new List<Light>(), towerLights = new List<Light>();
        readonly List<Color> towerColors = new List<Color>();
        public List<Vector3> flames = new List<Vector3>(), towers = new List<Vector3>();

        static readonly Color Warm = new Color(1f, .82f, .55f), Electric = new Color(.3f, .75f, 1f);
        static readonly Color[] Neon = { new Color(1f, .3f, .6f), new Color(.3f, .8f, 1f), new Color(.7f, 1f, .3f), new Color(1f, .75f, .25f), new Color(.75f, .45f, 1f) };

        CloudLayer clouds;
        ParticleSystem fireworks, confetti, fire;
        Transform lasers, ballBeams, lcdBeams, balloon;
        Material laserMat, beamMat, lcdBeamMat;
        Light flash;
        float mood = .6f, lcd, feel, kids;
        long songKey = long.MinValue;
        double showAt = -1, balloonAt = -1, songStart, songLength;
        float nextBurst, nextFlame, nextConfetti;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 80; clouds.span = 2600f; clouds.key = "electric";
            clouds.minHeight = 110f; clouds.maxHeight = 200f; clouds.size = 1.3f; clouds.speed = 4f; clouds.cover = .4f;
            clouds.color = new Color(1f, .85f, .75f);
            fireworks = Particles("Fireworks", 5000, true, .6f, 1.2f);
            var fm = fireworks.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f); fm.startSpeed = new ParticleSystem.MinMaxCurve(12f, 20f); fm.gravityModifier = .35f;
            var fs = fireworks.shape; fs.shapeType = ParticleSystemShapeType.Sphere; fs.radius = .2f;
            confetti = Particles("Confetti", 2000, false, .12f, .22f);
            var cm = confetti.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); cm.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f); cm.gravityModifier = .12f;
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .3f, .6f), new Color(.3f, .8f, 1f));
            var cs = confetti.shape; cs.shapeType = ParticleSystemShapeType.Cone; cs.angle = 35f; cs.rotation = new Vector3(-90f, 0, 0);
            var rot = confetti.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            fire = Particles("Flame cannons", 1500, true, 1.4f, 2.6f);
            var firem = fire.main; firem.startLifetime = new ParticleSystem.MinMaxCurve(.5f, .9f); firem.startSpeed = new ParticleSystem.MinMaxCurve(12f, 18f); firem.startColor = new Color(1f, .55f, .2f, .9f);
            var fsh = fire.shape; fsh.shapeType = ParticleSystemShapeType.Cone; fsh.angle = 6f; fsh.rotation = new Vector3(-90f, 0, 0);
            flash = new GameObject("Firework flash").AddComponent<Light>(); flash.transform.SetParent(transform, false); flash.type = LightType.Point; flash.range = 220f; flash.intensity = 0f;
            MakeLasers(); MakeBallBeams(); MakeBalloon();
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
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f)));
            }
            tex.Apply(); return tex;
        }

        ParticleSystem Particles(string name, int max, bool glow, float s0, float s1)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1);
            var e = ps.emission; e.rateOverTime = 0f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
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
            return ps;
        }

        /// A bundle of long thin beams fanned out from one point (lasers, disco-ball spots).
        Transform Beams(string name, Vector3 at, int count, float len, float width, Color[] cols, float spread, out Material mat)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (var i = 0; i < count; i++)
            {
                var d = Quaternion.Euler(-spread * Random.Range(.2f, 1f), i * 360f / count, 0) * Vector3.forward;
                var side = Vector3.Cross(Vector3.up, d).normalized * width;
                var b = v.Count; var col = cols[i % cols.Length];
                v.Add(-side); v.Add(side); v.Add(d * len + side * 3f); v.Add(d * len - side * 3f);
                c.Add(col); c.Add(col); c.Add(new Color(col.r, col.g, col.b, 0f)); c.Add(new Color(col.r, col.g, col.b, 0f));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.position = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); mat = Additive(name, Texture2D.whiteTexture); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        void MakeLasers()
        {
            if (mainStage == Vector3.zero) return;
            lasers = Beams("Lasers", mainStage, 8, 260f, .08f, Neon, 35f, out laserMat);
        }

        void MakeBallBeams()
        {
            if (discoBall != null) ballBeams = Beams("Disco beams", discoBall.position, 18, 60f, .25f, new[] { new Color(1f, 1f, 1f, .5f), new Color(.6f, .85f, 1f, .5f), new Color(1f, .6f, .85f, .5f) }, -70f, out beamMat);
            if (lcdBall != null) lcdBeams = Beams("LCD beams", lcdBall.position, 14, 22f, .12f, new[] { new Color(1f, 1f, 1f, .6f), new Color(1f, .8f, .5f, .6f) }, -60f, out lcdBeamMat);
        }

        void MakeBalloon()
        {
            var mk = new MeshKit(); var rng = new System.Random(3);
            Color[] cols = { new Color(.95f, .3f, .35f), new Color(1f, .8f, .25f), new Color(.3f, .7f, .95f) };
            for (var k = 0; k < 12; k++)
            {
                float a0 = k * Mathf.PI * 2f / 12f, a1 = (k + 1) * Mathf.PI * 2f / 12f;
                for (var j = 0; j < 6; j++)
                {
                    float b0 = Mathf.PI * j / 6f, b1 = Mathf.PI * (j + 1) / 6f;
                    Vector3 P(float a, float b) => new Vector3(Mathf.Cos(a) * Mathf.Sin(b) * 7f, -Mathf.Cos(b) * 8f * (b > Mathf.PI / 2f ? .7f : 1f), Mathf.Sin(a) * Mathf.Sin(b) * 7f);
                    mk.Quad(WorldMaterials.Solid(cols[k % 3]), P(a0, b0), P(a1, b0), P(a1, b1), P(a0, b1), Vector3.zero);
                }
            }
            mk.Box(WorldMaterials.Solid(new Color(.5f, .38f, .25f)), new Vector3(0, -13f, 0), new Vector3(1.6f, 1.2f, 1.6f), Quaternion.identity);
            mk.Blob(WorldMaterials.Glow(new Color(1f, .6f, .25f), 3f), new Vector3(0, -8.5f, 0), Vector3.one * .5f, rng, 0f);
            balloon = mk.Build("Hot-air balloon", transform, false).transform;
            balloon.gameObject.SetActive(false);
        }

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
                songKey = song; songStart = start; songLength = length;
                var k = SharedChance.Key("electric-show");
                showAt = SharedChance.Roll(k, song, .35) ? start + length * (.2 + .5 * SharedChance.Value(k, song, 1)) : -1;
                k = SharedChance.Key("electric-balloon");
                balloonAt = SharedChance.Roll(k, song, .3) ? start + length * (.05 + .3 * SharedChance.Value(k, song, 1)) : -1;
            }

            mood = Mathf.MoveTowards(mood, Mathf.Clamp01(valence * 1.3f), dt / 4f);
            SkyDirector.SetSongTint(Land, Color.Lerp(new Color(.75f, .55f, 1f), new Color(1f, .7f, .45f), mood), .3f);
            if (here)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                Shader.SetGlobalFloat("_EW_Wind", .3f + .3f * energy);
                Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            }

            // the song moments
            kids = Mathf.MoveTowards(kids, here && id == ElectricKit.Kids ? 1f : 0f, dt / 2f);
            feel = Mathf.MoveTowards(feel, here && id == ElectricKit.ElectricFeel ? 1f : 0f, dt / 3f);
            lcd = Mathf.MoveTowards(lcd, here && (id == ElectricKit.AllMyFriends || id == ElectricKit.DanceYrselfClean) ? 1f : 0f, dt / 4f);
            var flamesOn = here && id == ElectricKit.DisparateYouth;

            // things that turn
            if (discoBall != null) discoBall.Rotate(0, dt * (12f + 25f * energy), 0, Space.World);
            if (ballBeams != null) { ballBeams.position = discoBall.position; ballBeams.rotation = discoBall.rotation; beamMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, here ? .25f + .5f * energy : 0f)); }
            if (carousel != null) carousel.Rotate(0, dt * 14f, 0, Space.World);
            if (ferris != null)
            {
                ferris.Rotate(0, 0, dt * 3f, Space.Self);
                var flatFace = Vector3.ProjectOnPlane(ferris.forward, Vector3.up).normalized;
                foreach (var g in gondolas) g.rotation = Quaternion.LookRotation(flatFace, Vector3.up);
            }
            if (lasers != null)
            {
                lasers.rotation = Quaternion.LookRotation(mainStageFront) * Quaternion.Euler(Mathf.Sin(t * (.3f + energy)) * 15f, Mathf.Sin(t * .5f) * 50f, 0) * Quaternion.Euler(0, t * (20f + 60f * energy), 0);
                laserMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, here ? Mathf.Clamp01(energy * 1.2f - .2f) * .8f : 0f));
            }

            // lights that follow the music
            for (var i = 0; i < festoon.Length; i++)
            {
                var wave = .5f + .5f * Mathf.Sin(t * (2f + 6f * feel) - i * 1.3f);
                var col = Color.Lerp(Warm, Electric, feel);
                festoon[i].SetColor("_EmissionColor", col * (1.2f + .9f * energy + feel * 2.5f * wave));
            }
            if (eel != null) eel.SetColor("_EmissionColor", Electric * (1.4f + 6f * feel + Mathf.Sin(t * 8f) * feel * 2f));
            if (towerColors.Count != towerLights.Count) { towerColors.Clear(); foreach (var l in towerLights) towerColors.Add(l != null ? l.color : Warm); }
            for (var i = 0; i < towerLights.Count; i++)
                if (towerLights[i] != null)
                {
                    towerLights[i].color = Color.Lerp(towerColors[i], Electric, feel);
                    towerLights[i].intensity = 1.4f + feel * (1.6f + 1.4f * Mathf.Sin(t * 6f - i));
                }
            if (ledWall != null) ledWall.SetColor("_EmissionColor", Color.Lerp(new Color(1f, .82f, .2f), Neon[(int)(t * 2f) % Neon.Length], .5f) * (1.2f + 1.8f * energy));
            if (wheelBulbs != null) wheelBulbs.SetColor("_EmissionColor", Warm * (1.4f + Mathf.Sin(t * 3f) * .5f + energy));
            if (screen != null) screen.SetColor("_EmissionColor", Color.Lerp(new Color(.6f, .7f, 1f), new Color(1f, .7f, .5f), .5f + .5f * Mathf.Sin(t * .7f)) * (1.2f + .4f * Mathf.PerlinNoise(t * 3f, 0f)));
            if (mirrorTiles != null) mirrorTiles.SetColor("_EmissionColor", new Color(.85f, .9f, 1f) * (.8f + 1.6f * energy));

            // LCD Soundsystem's dome: dim and waiting, alive while their song plays
            if (lcdBall != null) lcdBall.Rotate(0, dt * 30f * lcd, 0, Space.World);
            if (lcdTiles != null) lcdTiles.SetColor("_EmissionColor", new Color(.9f, .92f, 1f) * (.3f + 3f * lcd));
            foreach (var l in lcdLights) if (l != null) l.intensity = .3f + 2.2f * lcd;
            if (lcdBeams != null) { lcdBeams.position = lcdBall.position; lcdBeams.rotation = lcdBall.rotation; lcdBeamMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, lcd * .8f)); }

            // fireworks: all through "Kids", and a short show in some songs
            var show = here && (kids > .5f || (showAt > 0 && now > showAt && now < showAt + 35));
            nextBurst -= dt;
            if (show && nextBurst <= 0f)
            {
                nextBurst = kids > .5f ? Random.Range(.12f, .35f) : Random.Range(.8f, 2f);
                var a = Random.value * Mathf.PI * 2f; var r = Random.Range(20f, 90f);
                var over = kids > .5f && discoBall != null ? discoBall.position : center;
                var p = new Vector3(over.x + Mathf.Cos(a) * r, over.y + Random.Range(35f, 70f), over.z + Mathf.Sin(a) * r);
                var col = Neon[Random.Range(0, Neon.Length)];
                fireworks.transform.position = p;
                fireworks.Emit(new ParticleSystem.EmitParams { startColor = col, applyShapeToPosition = true }, Random.Range(160, 260));
                flash.transform.position = p; flash.color = col; flash.intensity = 6f;
            }
            flash.intensity = Mathf.MoveTowards(flash.intensity, 0f, dt * 18f);

            // confetti out of the top of the Big Top in "Kids"
            nextConfetti -= dt;
            if (here && kids > .5f && nextConfetti <= 0f && discoBall != null)
            {
                nextConfetti = .5f;
                confetti.transform.position = discoBall.position + Vector3.up * 3f;
                confetti.Emit(120);
            }

            // flame cannons in "Disparate Youth"
            nextFlame -= dt;
            if (flamesOn && nextFlame <= 0f && flames.Count > 0)
            {
                nextFlame = Random.Range(.05f, .12f);
                foreach (var f in flames) { fire.transform.position = f; fire.Emit(25); }
            }

            // a hot-air balloon drifting over
            var bt = (float)(now - balloonAt);
            var drifting = here && balloonAt > 0 && bt > 0f && bt < 140f;
            balloon.gameObject.SetActive(drifting);
            if (drifting)
            {
                var a = (float)(balloonAt % 6.283);
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                balloon.position = center - dir * 350f + dir * bt * 5f + Vector3.up * (95f + Mathf.Sin(bt * .2f) * 4f);
                balloon.rotation = Quaternion.Euler(0, bt * 2f, 0);
            }
        }
    }
}
