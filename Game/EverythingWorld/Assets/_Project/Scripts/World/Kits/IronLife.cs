using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Iron Range in its thunderstorm, answering the music:
    /// - low black storm clouds race over; lightning strikes now and then, flashing the whole range white; the forge
    ///   breathes out embers and smoke; the molten river and the lava pool throb with the music and spit sparks;
    ///   braziers burn; electric arcs crackle between the Sabbath pylons; the meteorite glows; mine carts circle Ozzy's
    ///   loop; the ace of spades turns;
    /// - "Enter Sandman": the forge erupts, embers pour up and the lightning comes thick and fast; "Master of Puppets":
    ///   the puppet strings blaze and the light turns red; "War Pigs": the sky burns red and the arcs roar; "115": the
    ///   meteorite floods the range with green light; "Crazy Train": the carts race; "Ace of Spades": the card spins
    ///   fast, throwing sparks.
    public class IronLife : MonoBehaviour
    {
        const string Land = "The Iron Range";
        public Vector3 center, forge, forgeFace = Vector3.forward, pool, arcA, arcB, meteorAt, crate;
        public Material lava, strings, meteor;
        public Vector3[] river = new Vector3[0], loop = new Vector3[0];
        public List<Vector3> fires = new List<Vector3>(), stars = new List<Vector3>();
        public List<Transform> carts = new List<Transform>(); public Transform card;

        CloudLayer clouds; ParticleSystem flame, embers, smoke, sparks, green, gold;
        LineRenderer bolt, arc; float boltLeft, nextBolt = 3f, flash;
        float[] loopLen = new float[0]; float loopTotal;
        float sandman, puppets, warPigs, e115, crazy, ace;
        float nextFlame, nextEmber, nextSmoke, nextSpark, nextGreen, nextGold, nextArc;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 70; clouds.span = 2400f; clouds.key = "iron";
            clouds.minHeight = 110f; clouds.maxHeight = 160f; clouds.size = 2.6f; clouds.speed = 6f; clouds.cover = .85f;
            clouds.color = new Color(.22f, .21f, .23f);
            flame = Particles("Brazier fire", 900, new Color(1f, .5f, .15f, .9f), .25f, .55f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            embers = Particles("Forge embers", 900, new Color(1f, .55f, .15f, 1f), .12f, .3f, true);
            var em = embers.main; em.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); em.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f); em.gravityModifier = -.05f;
            var es = embers.shape; es.shapeType = ParticleSystemShapeType.Cone; es.angle = 25f; es.radius = 3f;
            smoke = Particles("Forge smoke", 300, new Color(.12f, .1f, .1f, .5f), 6f, 12f, false);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); sm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            sparks = Particles("Molten sparks", 400, new Color(1f, .7f, .2f, 1f), .1f, .22f, true);
            var spm = sparks.main; spm.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.2f); spm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f); spm.gravityModifier = .8f;
            green = Particles("Element glow", 400, new Color(.3f, 1f, .4f, 1f), .15f, .4f, true);
            var gm = green.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f); gm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            gold = Particles("Crate beam", 300, new Color(1f, .85f, .4f, 1f), .1f, .25f, true);
            var gdm = gold.main; gdm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f); gdm.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
            var gs = gold.shape; gs.shapeType = ParticleSystemShapeType.Cone; gs.angle = 2f; gs.radius = .5f; gs.rotation = new Vector3(-90f, 0, 0);
            bolt = Line("Lightning", new Color(.85f, .9f, 1f), .9f); arc = Line("Electric arc", new Color(.65f, .55f, 1f), .2f);
            if (loop.Length > 1) { loopLen = new float[loop.Length]; for (var i = 1; i < loop.Length; i++) loopLen[i] = loopLen[i - 1] + Vector3.Distance(loop[i - 1], loop[i]); loopTotal = loopLen[loop.Length - 1]; }
        }

        LineRenderer Line(string name, Color c, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.widthMultiplier = width; lr.positionCount = 0;
            lr.sharedMaterial = new Material(WorldMaterials.Glow(c, 4f)) { name = name }; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lr;
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

        static void Jag(LineRenderer lr, Vector3 a, Vector3 b, int n, float wander)
        {
            lr.positionCount = n + 1;
            for (var i = 0; i <= n; i++) { var t = i / (float)n; var p = Vector3.Lerp(a, b, t); if (i > 0 && i < n) p += Random.insideUnitSphere * wander; lr.SetPosition(i, p); }
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            sandman = Mathf.MoveTowards(sandman, here && id == IronKit.Sandman ? 1f : 0f, dt / 2f);
            puppets = Mathf.MoveTowards(puppets, here && id == IronKit.Puppets ? 1f : 0f, dt / 2f);
            warPigs = Mathf.MoveTowards(warPigs, here && id == IronKit.WarPigs ? 1f : 0f, dt / 3f);
            e115 = Mathf.MoveTowards(e115, here && id == IronKit.Element115 ? 1f : 0f, dt / 2f);
            crazy = Mathf.MoveTowards(crazy, here && id == IronKit.CrazyTrain ? 1f : 0f, dt / 2f);
            ace = Mathf.MoveTowards(ace, here && id == IronKit.AceOfSpades ? 1f : 0f, dt / 2f);

            // lightning: a strike every few seconds (thick and fast in "Enter Sandman"), the range flashing white
            nextBolt -= dt; boltLeft -= dt; flash = Mathf.MoveTowards(flash, 0f, dt * 5f);
            var cam = Camera.main;
            if (here && nextBolt <= 0f && cam != null)
            {
                nextBolt = sandman > .3f ? Random.Range(.6f, 2f) : Random.Range(5f, 14f); boltLeft = .22f; flash = 1f;
                var hit = cam.transform.position + new Vector3(Random.Range(-260f, 260f), 0, Random.Range(-260f, 260f)); hit.y = cam.transform.position.y - 10f;
                Jag(bolt, hit + Vector3.up * 220f + Random.insideUnitSphere * 30f, hit, 14, 9f);
            }
            if (boltLeft <= 0f && bolt.positionCount > 0) bolt.positionCount = 0;

            var tint = new Color(1f, .8f, .65f); var amount = .1f;
            if (warPigs > 0f) { tint = Color.Lerp(tint, new Color(1f, .25f, .12f), warPigs); amount = Mathf.Lerp(amount, .55f, warPigs); }
            if (puppets > 0f) { tint = Color.Lerp(tint, new Color(1f, .3f, .3f), puppets * .7f); amount = Mathf.Max(amount, .4f * puppets); }
            if (e115 > 0f) { tint = Color.Lerp(tint, new Color(.35f, 1f, .45f), e115); amount = Mathf.Lerp(amount, .55f, e115); }
            if (flash > 0f) { tint = Color.Lerp(tint, new Color(.9f, .92f, 1f), flash); amount = Mathf.Max(amount, .7f * flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, .25f + sandman * .4f);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .7f + .4f * energy + sandman * .6f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            if (lava != null) lava.SetColor("_EmissionColor", IronKit.Molten * (1.2f + .35f * Mathf.Sin(t * 1.7f) + energy * .9f + sandman * 1.4f + warPigs * .6f));
            if (strings != null) strings.SetColor("_EmissionColor", Color.Lerp(new Color(.9f, .9f, .95f) * .35f, new Color(1f, .35f, .3f) * 4f, puppets));
            if (meteor != null) meteor.SetColor("_EmissionColor", new Color(.3f, 1f, .4f) * (1f + .5f * Mathf.Sin(t * 2.2f) + e115 * 4f));
            if (loop.Length > 1)
                for (var k = 0; k < carts.Count; k++)
                {
                    var s = Mathf.Repeat((float)(now * (6f + crazy * 14f)) + k * loopTotal / carts.Count, loopTotal);
                    var lo = 0; var hi = loop.Length - 1; while (hi - lo > 1) { var mid = (lo + hi) / 2; if (loopLen[mid] < s) lo = mid; else hi = mid; }
                    var f = (s - loopLen[lo]) / Mathf.Max(.001f, loopLen[hi] - loopLen[lo]); var dir = loop[hi] - loop[lo];
                    carts[k].SetPositionAndRotation(Vector3.Lerp(loop[lo], loop[hi], f) + Vector3.up * .1f, Quaternion.LookRotation(dir));
                }
            if (card != null) card.rotation = Quaternion.Euler(0, (float)(now * (30f + ace * 500f) % 360.0), Mathf.Sin(t * .5f) * 5f);

            nextArc -= dt;
            if (here && arcA != Vector3.zero && nextArc <= 0f) { nextArc = warPigs > .2f ? .03f : .09f; if (Random.value < .7f + warPigs * .3f) Jag(arc, arcA, arcB, 10, .5f + warPigs * .8f); else arc.positionCount = 0; }
            nextFlame -= dt;
            if (here && nextFlame <= 0f) { nextFlame = .15f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(1); } }
            nextEmber -= dt;
            if (here && forge != Vector3.zero && nextEmber <= 0f) { nextEmber = sandman > .2f ? .005f : .04f; embers.transform.SetPositionAndRotation(forge + forgeFace * 2f, Quaternion.LookRotation(Vector3.up + forgeFace * .4f)); embers.Emit(sandman > .2f ? 4 : 1); }
            nextSmoke -= dt;
            if (here && forge != Vector3.zero && nextSmoke <= 0f) { nextSmoke = .5f; smoke.transform.position = forge + Vector3.up * 8f + forgeFace * 2f; smoke.Emit(1); }
            nextSpark -= dt;
            if (here && river.Length > 0 && nextSpark <= 0f) { nextSpark = .06f; var p = Random.value < .3f ? pool + new Vector3(Random.Range(-8f, 8f), .3f, Random.Range(-8f, 8f)) : river[Random.Range(0, river.Length)] + Vector3.up * .3f; sparks.transform.position = p; sparks.Emit(ace > .2f ? 3 : 1); }
            if (here && ace > .2f && card != null && Random.value < .3f) { sparks.transform.position = card.position + Random.insideUnitSphere * 1.5f; sparks.Emit(2); }
            nextGreen -= dt;
            if (here && meteorAt != Vector3.zero && nextGreen <= 0f) { nextGreen = e115 > .2f ? .01f : .3f; green.transform.position = meteorAt + Random.insideUnitSphere * (1f + e115 * 3f); green.Emit(1); }
            nextGold -= dt;
            if (here && crate != Vector3.zero && nextGold <= 0f) { nextGold = .05f; gold.transform.SetPositionAndRotation(crate, Quaternion.identity); gold.Emit(1); }
        }
    }
}
