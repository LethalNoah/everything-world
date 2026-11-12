using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Funk Tropics at night, everything on the one:
    /// - the glowing plants pulse on a shared beat (the same for everyone), purple, lime and orange taking the bar in
    ///   turns; the light-up paving of the One chases; the mirror-ball fruit glitter; motes of coloured light drift in
    ///   the dark; the Mothership hums on her legs, lights racing round her rim; a few dark clouds, stars between;
    /// - "Flash Light": beams sweep the sky from the ship and the giant flashlight. "Give Up The Funk": the ship lifts
    ///   off her legs and the torn-off roof floats higher. "Purple Rain": it rains purple. "Maggot Brain": the whole
    ///   jungle goes dark but for one spotlight on one guitar. "Fire on the Bayou": the fire on the pond climbs.
    ///   "The Payback": the light goes red. Every song's own light brightens while it plays.
    public class FunkLife : MonoBehaviour
    {
        const string Land = "The Funk Tropics";
        public Vector3 center, shipHome, flashAt, flashDir, roofHome, princeAt, bayouAt;
        public string flashSong, roofSong, maggotSong, rainSong, bayouSong, paybackSong;
        public Material[] flora, tiles; public Material fruit, dome, belly; public Transform ship, roof; public Light bellyLight, maggotLight;
        public float plucked = -99f, synth = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem motes, rain, flames; readonly List<Transform> beams = new List<Transform>();
        Color[] floraBase, tileBase; Color fruitBase, domeBase, bellyBase;
        float flash, roofUp, maggot, purple, bayou, payback, nextMote, nextRain, nextFlame;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2400f; clouds.key = "funk";
            clouds.minHeight = 140f; clouds.maxHeight = 210f; clouds.size = 3f; clouds.speed = 2.4f; clouds.cover = .35f;
            clouds.color = new Color(.22f, .14f, .34f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            floraBase = new Color[flora.Length]; for (var i = 0; i < flora.Length; i++) floraBase[i] = E(flora[i]);
            tileBase = new Color[tiles.Length]; for (var i = 0; i < tiles.Length; i++) tileBase[i] = E(tiles[i]);
            fruitBase = E(fruit); domeBase = E(dome); bellyBase = E(belly);
            motes = Particles("Jungle motes", 500, Color.white, .05f, .12f, true);
            var mm = motes.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .3f);
            rain = Particles("Purple rain", 1500, new Color(.7f, .4f, 1f, .85f), .05f, .09f, true);
            var rm = rain.main; rm.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.6f); rm.gravityModifier = .6f;
            var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.Stretch; rr.lengthScale = 5f; rr.velocityScale = .06f;
            flames = Particles("Bayou fire", 400, new Color(1f, .5f, .15f, .9f), .3f, .7f, true);
            var fm = flames.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            var fs = flames.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 14f; fs.radius = 2.2f; fs.rotation = new Vector3(-90f, 0, 0);
            // three beams that sweep from the ship for Flash Light
            var bmat = Additive("Beam"); bmat.SetColor("_BaseColor", new Color(1f, .95f, .7f, .16f));
            for (var k = 0; k < 4; k++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(go.GetComponent<Collider>()); go.name = "Flash light beam"; go.transform.SetParent(transform, false);
                var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = bmat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; go.SetActive(false); beams.Add(go.transform);
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = Additive(name); m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
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
            bool On(string song) => here && song != null && id == song;

            flash = Mathf.MoveTowards(flash, On(flashSong) ? 1f : 0f, dt / 2f);
            roofUp = Mathf.MoveTowards(roofUp, On(roofSong) ? 1f : 0f, dt / 4f);
            maggot = Mathf.MoveTowards(maggot, On(maggotSong) ? 1f : 0f, dt / 6f);
            purple = Mathf.MoveTowards(purple, On(rainSong) ? 1f : 0f, dt / 4f);
            bayou = Mathf.MoveTowards(bayou, On(bayouSong) ? 1f : 0f, dt / 3f);
            payback = Mathf.MoveTowards(payback, On(paybackSong) ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the groove: 105 beats a minute off the shared clock; the three plant colours take the beats of the bar in turn
            var beats = SharedChance.Now * 1.75; var beat = (float)(beats % 1.0); var bar = (int)(beats % 4.0); var kick = Mathf.Pow(1f - beat, 3f);
            var hush = 1f - maggot * .88f; var lift = t - plucked < 2.5f ? 1.2f * Mathf.Exp(-(t - plucked) * 1.4f) : 0f;
            for (var i = 0; i < flora.Length; i++) if (flora[i] != null) flora[i].SetColor("_EmissionColor", floraBase[i] * (.55f + (bar % 3 == i || bar == 0 ? 1.1f : .25f) * kick * (.7f + energy * .6f) + lift) * hush);
            var chase = SharedChance.Now * (1.75 + flash * 3.5 + roofUp * 3.5 + (t - synth < 6f ? 5.0 : 0.0));
            for (var i = 0; i < tiles.Length; i++) if (tiles[i] != null) { var ph = Mathf.Repeat((float)(chase % 4.0) - i, 4f); tiles[i].SetColor("_EmissionColor", Color.Lerp(tileBase[i], new Color(1f, .15f, .1f) * 1.2f, payback * .7f) * (.4f + 1.5f * Mathf.Clamp01(1f - ph)) * hush); }
            if (fruit != null) fruit.SetColor("_EmissionColor", fruitBase * (.8f + .5f * Mathf.PerlinNoise(t * 3f, 1f) + kick * .3f) * hush);
            if (dome != null) dome.SetColor("_EmissionColor", domeBase * (.9f + .4f * Mathf.Sin(t * 1.3f) + flash * 1.5f + roofUp));
            if (belly != null) belly.SetColor("_EmissionColor", bellyBase * (1f + kick * .6f + roofUp * 2f)); if (bellyLight != null) bellyLight.intensity = (2.4f + kick * 1.2f + roofUp * 4f) * hush;
            if (maggotLight != null) maggotLight.intensity = 1.4f + maggot * 6f;

            // the ship hums on her legs, and lifts for Give Up The Funk; the torn-off roof floats higher
            if (ship != null) { ship.position = shipHome + Vector3.up * (Mathf.Sin(t * .8f) * .12f + roofUp * (3.2f + Mathf.Sin(t * 1.4f) * .5f)); ship.Rotate(0, (4f + roofUp * 30f + flash * 20f) * dt, 0, Space.World); }
            if (roof != null) roof.position = roofHome + Vector3.up * (Mathf.Sin(t * 1.1f) * .2f + roofUp * 5f);
            for (var k = 0; k < beams.Count; k++)
            {
                var on = flash > .02f; if (beams[k].gameObject.activeSelf != on) beams[k].gameObject.SetActive(on); if (!on) continue;
                var from = k == 0 && flashAt != Vector3.zero ? flashAt : (ship != null ? ship.position + Vector3.up * 2f : center);
                var dir = k == 0 && flashAt != Vector3.zero ? Quaternion.AngleAxis(Mathf.Sin(t * .9f) * 25f, Vector3.right) * flashDir : Quaternion.Euler(0, t * 40f + k * 120f, 0) * (Vector3.up * 1.2f + Vector3.forward).normalized;
                const float len = 90f; beams[k].position = from + dir * len / 2f; beams[k].rotation = Quaternion.FromToRotation(Vector3.up, dir); beams[k].localScale = new Vector3(1.6f * flash, len / 2f, 1.6f * flash);
            }

            // the sky: deep violet night; purple for the rain, near black for Maggot Brain, red for the Payback, flashes for Flash Light
            var tint = new Color(.2f, .1f, .34f); var amount = .12f;
            if (purple > 0f) { tint = Color.Lerp(tint, new Color(.5f, .2f, .8f), purple); amount = Mathf.Lerp(amount, .5f, purple); }
            if (bayou > 0f) { tint = Color.Lerp(tint, new Color(.9f, .4f, .12f), bayou); amount = Mathf.Lerp(amount, .28f, bayou); }
            if (payback > 0f) { tint = Color.Lerp(tint, new Color(.75f, .1f, .08f), payback); amount = Mathf.Lerp(amount, .4f, payback); }
            if (flash > 0f) { tint = Color.Lerp(tint, new Color(1f, .9f, .6f), flash * kick * .6f); amount = Mathf.Max(amount, .3f * flash * kick); }
            if (maggot > 0f) { tint = Color.Lerp(tint, new Color(.01f, .01f, .03f), maggot); amount = Mathf.Lerp(amount, .85f, maggot); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .2f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main;
            if (here && cam != null && t >= nextMote)
            {
                nextMote = t + .1f; Color[] cols = { FunkKit.Purple, FunkKit.Lime, FunkKit.Orange, FunkKit.Cyan, FunkKit.Pink };
                var p = cam.transform.position + new Vector3(Random.Range(-16f, 16f), Random.Range(-1f, 6f), Random.Range(-16f, 16f));
                motes.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x, .15f, wind.y) * .2f, startColor = cols[Random.Range(0, cols.Length)] * (1f - maggot * .8f) }, 1);
            }
            if (purple > 0f && here && cam != null && t >= nextRain)
            {
                nextRain = t + .03f;
                for (var k = 0; k < Mathf.CeilToInt(purple * 8f); k++) rain.Emit(new ParticleSystem.EmitParams { position = cam.transform.position + new Vector3(Random.Range(-18f, 18f), Random.Range(8f, 14f), Random.Range(-18f, 18f)), velocity = new Vector3(wind.x, -14f, wind.y) }, 1);
            }
            if (bayouAt != Vector3.zero && t >= nextFlame) { nextFlame = t + .06f; flames.transform.position = bayouAt; flames.Emit(1 + Mathf.CeilToInt(bayou * 5f)); }
        }
    }
}
