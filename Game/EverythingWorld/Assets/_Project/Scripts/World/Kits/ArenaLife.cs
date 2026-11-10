using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Arena Rock Ridge at night, answering the music:
    /// - searchlights sweep the sky from the stadium stage; the lighter flames in the stands glow with the song's
    ///   energy; the rig lights chase; the screens flicker; Journey's wheel turns in the sky; the tornado turns;
    ///   the Frankenstein coils spark; fires crackle;
    /// - "(Don't Fear) The Reaper": the light goes cold and blue, the searchlights slow and turn blue, the reaper's
    ///   eyes glow; "Don't Stop Believin'": the midnight train runs its track and the whole stadium's lights blaze
    ///   in waves; "Come Sail Away": the Styx ship rises into the sky, glowing, and comes back down; "Eye of the
    ///   Tiger": the tiger's eye lights and a spotlight hits the ring; "More Than a Feeling": the guitar ship flies
    ///   a wide loop over the ridge.
    public class ArenaLife : MonoBehaviour
    {
        const string Land = "Arena Rock Ridge";
        public Vector3 center, stadium, stage, searchFrom, buses, reaper, guitarShipHome, trainA, trainB, shipHome, ring;
        public Material crowd, reaperEyes, tigerEye; public Material[] rig = new Material[0]; public List<Material> screens = new List<Material>();
        public List<Vector3> fires = new List<Vector3>(), coils = new List<Vector3>();
        public Transform guitarShip, wheel, train, ship, tornado; public Light ringSpot;

        CloudLayer clouds; ParticleSystem flame, sparks;
        Transform beams; Material beamMat;
        float reap, believe, sail, tiger, feel;
        long songKey = long.MinValue; double songStart, songLength = 1;
        float nextFlame, nextSpark;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2400f; clouds.key = "arena";
            clouds.minHeight = 130f; clouds.maxHeight = 190f; clouds.size = 1.4f; clouds.speed = 3f; clouds.cover = .3f;
            clouds.color = new Color(.25f, .3f, .45f);
            flame = Particles("Fire", 800, new Color(1f, .55f, .2f, .9f), .3f, .8f);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var fs = flame.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 12f; fs.radius = .4f; fs.rotation = new Vector3(-90f, 0, 0);
            sparks = Particles("Sparks", 600, new Color(.6f, .85f, 1f, 1f), .1f, .25f);
            var sm = sparks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(.2f, .5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = .3f;
            if (searchFrom != Vector3.zero) beams = Beams(searchFrom, 5, out beamMat);
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

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1)
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
            var r = go.GetComponent<ParticleSystemRenderer>(); var m = Additive(name); m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        Transform Beams(Vector3 at, int count, out Material mat)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (var i = 0; i < count; i++)
            {
                var d = Quaternion.Euler(-62f + (i % 2) * 10f, i * 360f / count, 0) * Vector3.forward; var side = Vector3.Cross(Vector3.up, d).normalized * .9f;
                var b = v.Count; var col = new Color(.95f, .97f, 1f, .45f);
                v.Add(-side); v.Add(side); v.Add(d * 380f + side * 14f); v.Add(d * 380f - side * 14f);
                c.Add(col); c.Add(col); c.Add(new Color(1, 1, 1, 0)); c.Add(new Color(1, 1, 1, 0));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "Searchlights" }; mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Searchlights"); go.transform.SetParent(transform, false); go.transform.position = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); mat = Additive("Searchlights"); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey) { songKey = song; songStart = start; songLength = System.Math.Max(1, length); }
            var progress = Mathf.Clamp01((float)((now - songStart) / songLength));
            if (MusicDirector.ForcedTrackId != null) progress = Mathf.Repeat(t / 30f, 1f);

            reap = Mathf.MoveTowards(reap, here && id == ArenaKit.Reaper ? 1f : 0f, dt / 3f);
            believe = Mathf.MoveTowards(believe, here && id == ArenaKit.Believin ? 1f : 0f, dt / 2f);
            sail = Mathf.MoveTowards(sail, here && id == ArenaKit.SailAway ? 1f : 0f, dt / 3f);
            tiger = Mathf.MoveTowards(tiger, here && id == ArenaKit.Tiger ? 1f : 0f, dt / 1.5f);
            feel = Mathf.MoveTowards(feel, here && id == ArenaKit.Feeling ? 1f : 0f, dt / 3f);

            var tint = new Color(.55f, .65f, 1f); var amount = .12f;
            if (reap > 0f) { tint = Color.Lerp(tint, new Color(.3f, .45f, .9f), reap); amount = Mathf.Lerp(amount, .55f, reap); }
            if (believe > 0f) { tint = Color.Lerp(tint, new Color(1f, .7f, .4f), believe * .5f); amount = Mathf.Max(amount, .3f * believe); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .2f + .2f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // searchlights: sweeping white, slow and blue for the Reaper, swinging toward the ring for the Tiger
            if (beams != null)
            {
                beams.gameObject.SetActive(here);
                var speed = Mathf.Lerp(18f + 14f * energy, 5f, reap);
                beams.rotation = Quaternion.Euler(Mathf.Sin(t * .35f) * 12f, t * speed, 0);
                if (tiger > .5f && ring != Vector3.zero) { var to = ring - searchFrom; to.y = 0; beams.rotation = Quaternion.Slerp(beams.rotation, Quaternion.LookRotation(to) * Quaternion.Euler(0, Mathf.Sin(t) * 10f, 0), tiger); }
                beamMat.SetColor("_BaseColor", Color.Lerp(new Color(1f, 1f, 1f, .9f), new Color(.4f, .6f, 1f, .9f), reap));
            }
            // the crowd's lighters: glowing with the song, blazing in waves for Don't Stop Believin'
            if (crowd != null) crowd.SetColor("_EmissionColor", new Color(1f, .62f, .25f) * (.8f + energy * .8f + .2f * Mathf.Sin(t * 2f) + believe * (2f + 1.5f * Mathf.Sin(t * 3f))));
            for (var i = 0; i < rig.Length; i++) rig[i].SetColor("_EmissionColor", rig[i].GetColor("_BaseColor") * (((Mathf.FloorToInt(t * (3f + 6f * energy)) + i) % 3 == 0) ? 2.6f : .6f) * (1f - reap * .6f));
            for (var i = 0; i < screens.Count; i++) screens[i].SetColor("_EmissionColor", Color.Lerp(new Color(.4f, .5f, 1f), new Color(1f, .6f, .3f), Mathf.PerlinNoise(t * .7f + i, 0f)) * (.9f + energy));
            if (reaperEyes != null) reaperEyes.SetColor("_EmissionColor", new Color(.5f, .8f, 1f) * (.3f + reap * 4f));
            if (tigerEye != null) tigerEye.SetColor("_EmissionColor", new Color(1f, .7f, .1f) * (.5f + tiger * 3.5f));
            if (ringSpot != null) ringSpot.intensity = here ? .4f + tiger * 8f : 0f;

            if (wheel != null) wheel.Rotate(0, 0, (4f + 6f * energy) * dt, Space.Self);
            if (tornado != null) tornado.Rotate(0, (40f + 60f * energy) * dt, 0, Space.World);
            if (train != null)
            {
                var f = believe > .05f ? Mathf.PingPong(progress * 4f, 1f) : 0f;
                train.position = Vector3.Lerp(trainA, trainB, Mathf.SmoothStep(0f, 1f, f));
            }
            if (ship != null)
            {
                var rise = sail * Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI) * 70f;
                ship.position = shipHome + Vector3.up * (rise + Mathf.Sin(t * .6f) * .15f);
                ship.rotation = Quaternion.LookRotation(ship.forward) * Quaternion.Euler(0, 0, Mathf.Sin(t * .5f) * (1.5f + sail * 4f));
            }
            if (guitarShip != null)
            {
                var a = t * .25f;
                var loop = guitarShipHome + new Vector3(Mathf.Cos(a) * 70f - 70f, 30f + Mathf.Sin(a * 2f) * 8f, Mathf.Sin(a) * 70f);
                var hover = guitarShipHome + Vector3.up * Mathf.Sin(t * .8f) * .5f;
                var p = Vector3.Lerp(hover, loop, feel);
                if (feel > .01f) { var ahead = Vector3.Lerp(hover, guitarShipHome + new Vector3(Mathf.Cos(a + .05f) * 70f - 70f, 30f + Mathf.Sin((a + .05f) * 2f) * 8f, Mathf.Sin(a + .05f) * 70f), feel) - p; if (ahead.sqrMagnitude > .0001f) guitarShip.rotation = Quaternion.Slerp(guitarShip.rotation, Quaternion.LookRotation(ahead), dt * 2f); }
                guitarShip.position = p;
            }

            nextFlame -= dt;
            if (here && nextFlame <= 0f) { nextFlame = .1f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(2); } }
            nextSpark -= dt;
            if (here && nextSpark <= 0f && coils.Count >= 2) { nextSpark = Random.Range(.1f, .6f) / (.5f + energy); var c = coils[Random.Range(0, coils.Count)]; sparks.transform.position = c; sparks.Emit(Random.Range(8, 20)); }
        }
    }
}
