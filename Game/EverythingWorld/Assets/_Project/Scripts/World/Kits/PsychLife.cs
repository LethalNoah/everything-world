using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Psychedelic Wilds in the heat of the day, answering the music:
    /// - the sky slowly cycles through colours; the mushrooms' glowing undersides pulse with the song; colour motes
    ///   drift round you; the dancing bears sway; the kaleidoscope turns; the Box of Rain bobs; the Eyes of the
    ///   World look slowly about; the tower beacon blinks; the generator puffs smoke; fires crackle;
    /// - "Box of Rain": rain pours from the little cloud over the box; "Here Comes Sunshine": the sky clears to
    ///   bright gold; "Eyes of the World": the eye lights up and follows you; "Franklin's Tower": the bell blazes
    ///   and sparks pour off the tower; "Shakedown Street": the bears really dance; "Song For The Deaf": the light
    ///   drops to a red desert dusk, the car radio blazes and the tower beacon races.
    public partial class PsychLife : MonoBehaviour
    {
        const string Land = "The Psychedelic Wilds";
        public Vector3 center, spiral, tower, rainCloud, dead, tower2, generator;
        public Material[] capGlow = new Material[0];
        public Transform[] bears = new Transform[0];
        public Transform rainBox, eye, kaleidoscope;
        public Material bell, iris, radio, beacon;
        public List<Vector3> fires = new List<Vector3>();
        // the Dead's world (PsychDead)
        public Transform ship, rowboat; public Material shipLantern, palace, wolfEyes, wolfMoon, rippleM, stella, samson, fadeaway;
        public List<Transform> ripples = new List<Transform>(); public Vector3 ripplePool;
        // the generator party (PsychArtists)
        public Material partyBulbs, slotReels; public Vector3 bonfire;

        CloudLayer clouds; ParticleSystem motes, rain, sparks, flame, smoke;
        Vector3[] bearHome = new Vector3[0]; Vector3 boxHome; Quaternion eyeHome;
        float box, sun, eyes, frank, shake, deaf;
        float nextMote, nextRain, nextSpark, nextFlame, nextSmoke;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 50; clouds.span = 2400f; clouds.key = "psych";
            clouds.minHeight = 120f; clouds.maxHeight = 180f; clouds.size = 1.7f; clouds.speed = 3f; clouds.cover = .35f;
            clouds.color = new Color(1f, .85f, .95f);
            motes = Particles("Colour motes", 1500, Color.white, .1f, .22f, true);
            var mm = motes.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .3f);
            mm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .4f, .8f), new Color(.4f, .9f, 1f));
            var mn = motes.noise; mn.enabled = true; mn.strength = .4f;
            rain = Particles("Box rain", 800, new Color(.7f, .85f, 1f, .7f), .05f, .08f, false);
            var rm = rain.main; rm.startLifetime = 1.2f; rm.startSpeed = 0f; rm.gravityModifier = 1.2f;
            var rs = rain.shape; rs.shapeType = ParticleSystemShapeType.Box; rs.scale = new Vector3(5f, .1f, 3f);
            var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.Stretch; rr.velocityScale = .06f;
            sparks = Particles("Tower sparks", 1200, new Color(1f, .8f, .4f, 1f), .12f, .25f, true);
            var sm = sparks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f); sm.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f); sm.gravityModifier = .3f;
            var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 1f;
            flame = Particles("Fire", 600, new Color(1f, .55f, .2f, .9f), .3f, .8f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var fs = flame.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 12f; fs.radius = .4f; fs.rotation = new Vector3(-90f, 0, 0);
            smoke = Particles("Generator smoke", 300, new Color(.5f, .5f, .5f, .35f), .5f, 1.2f, false);
            var km = smoke.main; km.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f); km.startSpeed = new ParticleSystem.MinMaxCurve(.8f, 1.6f);
            bearHome = new Vector3[bears.Length]; for (var i = 0; i < bears.Length; i++) bearHome[i] = bears[i].position;
            if (rainBox != null) boxHome = rainBox.position;
            if (eye != null) eyeHome = eye.rotation;
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
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
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            box = Mathf.MoveTowards(box, here && id == PsychKit.BoxOfRain ? 1f : 0f, dt / 2f);
            sun = Mathf.MoveTowards(sun, here && id == PsychKit.Sunshine ? 1f : 0f, dt / 3f);
            eyes = Mathf.MoveTowards(eyes, here && id == PsychKit.EyesOfTheWorld ? 1f : 0f, dt / 2f);
            frank = Mathf.MoveTowards(frank, here && id == PsychKit.Franklins ? 1f : 0f, dt / 2f);
            shake = Mathf.MoveTowards(shake, here && id == PsychKit.Shakedown ? 1f : 0f, dt / 2f);
            deaf = Mathf.MoveTowards(deaf, here && id == PsychKit.SongForTheDeaf ? 1f : 0f, dt / 3f);

            // the sky slowly cycling through colours; gold for Sunshine; a red desert dusk for Song For The Deaf
            var tint = Color.HSVToRGB(Mathf.Repeat(t / 120f, 1f), .4f, 1f); var amount = .2f;
            if (sun > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .5f), sun); amount = Mathf.Lerp(amount, .45f, sun); }
            if (deaf > 0f) { tint = Color.Lerp(tint, new Color(.9f, .35f, .25f), deaf); amount = Mathf.Lerp(amount, .6f, deaf); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .3f + .3f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }
            var cam = Camera.main != null ? Camera.main.transform.position : center;

            for (var i = 0; i < capGlow.Length; i++)
            {
                var c = Color.HSVToRGB(Mathf.Repeat(i / (float)capGlow.Length + t * .02f, 1f), .7f, 1f);
                capGlow[i].SetColor("_EmissionColor", c * (.8f + energy * 1f + .3f * Mathf.Sin(t * 1.5f + i)));
            }
            for (var i = 0; i < bears.Length; i++)
            {
                if (bears[i] == null) continue;
                var amp = .15f + shake * .9f; var sp = 2f + shake * 4f + energy * 2f;
                bears[i].position = bearHome[i] + Vector3.up * Mathf.Abs(Mathf.Sin(t * sp + i)) * amp * 1.5f;
                bears[i].rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(bears[i].forward, Vector3.up)) * Quaternion.Euler(0, Mathf.Sin(t * sp * .5f + i) * (10f + shake * 60f), Mathf.Sin(t * sp + i) * (6f + shake * 14f));
            }
            if (kaleidoscope != null) kaleidoscope.Rotate(0, 0, (15f + 40f * energy) * dt, Space.Self);
            if (rainBox != null) { rainBox.position = boxHome + Vector3.up * Mathf.Sin(t * .7f) * .5f; rainBox.Rotate(0, 12f * dt, 0, Space.World); }
            if (eye != null)
            {
                Quaternion look;
                if (eyes > .3f) { var to = cam - eye.position; look = to.sqrMagnitude > .1f ? Quaternion.LookRotation(to) : eyeHome; }
                else look = eyeHome * Quaternion.Euler(Mathf.Sin(t * .3f) * 10f, Mathf.Sin(t * .2f) * 35f, 0);
                eye.rotation = Quaternion.Slerp(eye.rotation, look, dt * 2f);
            }
            if (iris != null) iris.SetColor("_EmissionColor", new Color(.25f, .6f, 1f) * (.6f + eyes * 3f));
            if (bell != null) bell.SetColor("_EmissionColor", new Color(1f, .8f, .35f) * (.6f + frank * (3f + Mathf.Sin(t * 6f))));
            if (radio != null) radio.SetColor("_EmissionColor", new Color(1f, .55f, .2f) * (1.2f + deaf * 3f));
            if (beacon != null) beacon.SetColor("_EmissionColor", new Color(1f, .1f, .1f) * (Mathf.Repeat(t * (1f + deaf * 4f), 1.5f) < .4f ? 3f : .2f));

            nextMote -= dt;
            if (here && nextMote <= 0f) { nextMote = .08f; for (var k = 0; k < 3; k++) { motes.transform.position = cam + new Vector3(Random.Range(-25f, 25f), Random.Range(0f, 8f), Random.Range(-25f, 25f)); motes.Emit(1); } }
            nextRain -= dt;
            if (here && box > .1f && nextRain <= 0f && rainCloud != Vector3.zero) { nextRain = .03f; rain.transform.position = rainCloud; rain.Emit(5); }
            nextSpark -= dt;
            if (here && frank > .2f && nextSpark <= 0f && tower != Vector3.zero) { nextSpark = .05f; sparks.transform.position = tower; sparks.Emit(4); }
            nextFlame -= dt;
            if (here && nextFlame <= 0f) { nextFlame = .1f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(f == bonfire + Vector3.up * .3f || (bonfire != Vector3.zero && (f - bonfire).sqrMagnitude < 2f) ? 6 : 2); } }
            DeadUpdate(dt, t, id, here, energy);
            nextSmoke -= dt;
            if (here && nextSmoke <= 0f && generator != Vector3.zero) { nextSmoke = .5f; smoke.transform.position = generator + Vector3.up * 1.6f; smoke.Emit(1); }
        }
    }
}
