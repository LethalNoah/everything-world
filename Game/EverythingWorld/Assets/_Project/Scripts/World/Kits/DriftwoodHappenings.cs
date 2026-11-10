using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Things that happen on the Driftwood Shore now and then, so it's never quite the same twice. All of it is
    /// decided by the radio's shared clock (SharedChance), so friends standing together see the same thing:
    /// - gulls always wheeling over the shore, gliding and flapping; in some songs a big flock comes in off the sea;
    /// - in some songs an old sailing ship crosses the horizon, a lantern lit at its stern;
    /// - in some songs a whale out at sea: three spouts, then its tail rising and slipping under;
    /// - in the drizzly sad songs, lightning flickers far out over the sea;
    /// - in some hopeful songs the cloud breaks and shafts of light fall on the water;
    /// - every day a message in a bottle washes up somewhere different, glowing faintly green.
    public partial class DriftwoodLife
    {
        public Vector3 laneFrom, laneTo;
        public bool hasLane;

        class Gull { public Transform t, l, r; public float phase, speed, radius, height, flapRate; }
        readonly List<Gull> wheeling = new List<Gull>(), flock = new List<Gull>();
        Transform ship, fluke, bottle, shafts;
        ParticleSystem spout;
        LineRenderer bolt;
        Light flash;
        Material shaftMat;
        long eventSong = long.MinValue, bottleDay = long.MinValue;
        double flockAt = -1, whaleAt = -1, songStart, songLength;
        bool shipOn, sunbreak;
        Vector3 whaleSpot;
        int spoutsDone;
        float shaftsShown;

        void MakeHappenings()
        {
            var rng = new System.Random(4242);
            for (var i = 0; i < 9; i++)
            {
                var g = MakeGull(rng, 1f + (float)rng.NextDouble() * .3f);
                g.phase = (float)rng.NextDouble() * 100f; g.speed = 7f + (float)rng.NextDouble() * 5f;
                g.radius = 35f + (float)rng.NextDouble() * 90f; g.height = 22f + (float)rng.NextDouble() * 25f;
                g.flapRate = 7f + (float)rng.NextDouble() * 3f;
                wheeling.Add(g);
            }
            for (var i = 0; i < 18; i++)
            {
                var g = MakeGull(rng, .9f + (float)rng.NextDouble() * .3f);
                g.phase = (float)rng.NextDouble() * 10f; g.flapRate = 8f + (float)rng.NextDouble() * 3f;
                g.t.gameObject.SetActive(false);
                flock.Add(g);
            }
            if (hasLane) { MakeShip(); MakeWhale(); MakeLightning(); MakeShafts(); }
            MakeBottle();
        }

        // ---------------------------------------------------------------- gulls
        Gull MakeGull(System.Random rng, float scale)
        {
            var root = new GameObject("Gull").transform; root.SetParent(transform, false);
            var white = WorldMaterials.Solid(new Color(.95f, .95f, .94f)); var grey = WorldMaterials.Solid(new Color(.6f, .63f, .67f));
            var dark = WorldMaterials.Solid(new Color(.15f, .15f, .17f)); var beak = WorldMaterials.Solid(new Color(.95f, .75f, .25f));
            var body = new MeshKit();
            body.Blob(white, Vector3.zero, new Vector3(.16f, .14f, .42f), rng, 0f);
            body.Blob(white, new Vector3(0, .06f, .38f), Vector3.one * .11f, rng, 0f);
            body.Box(beak, new Vector3(0, .05f, .52f), new Vector3(.04f, .04f, .12f), Quaternion.identity);
            body.Tri(grey, new Vector3(-.12f, 0, -.35f), new Vector3(.12f, 0, -.35f), new Vector3(0, .02f, -.6f), Vector3.down);
            body.Tri(grey, new Vector3(-.12f, 0, -.35f), new Vector3(.12f, 0, -.35f), new Vector3(0, .02f, -.6f), Vector3.up);
            body.Build("Body", root, false);
            var g = new Gull { t = root };
            foreach (var s in new[] { -1f, 1f })
            {
                var wing = new GameObject(s < 0 ? "Left wing" : "Right wing").transform; wing.SetParent(root, false);
                wing.localPosition = new Vector3(s * .08f, .05f, 0);
                var mk = new MeshKit();
                Vector3 a = new Vector3(0, 0, .18f), b = new Vector3(0, 0, -.18f), c = new Vector3(s * .75f, 0, -.05f), d = new Vector3(s * 1.2f, 0, -.3f);
                foreach (var inside in new[] { Vector3.up, Vector3.down })
                {
                    mk.Tri(grey, a, b, c, inside);
                    mk.Tri(dark, c, b + new Vector3(s * .6f, 0, -.05f), d, inside);
                    mk.Tri(grey, b, b + new Vector3(s * .6f, 0, -.05f), c, inside);
                }
                mk.Build("Wing", wing, false);
                if (s < 0) g.l = wing; else g.r = wing;
            }
            root.localScale = Vector3.one * scale;
            return g;
        }

        static void Flap(Gull g, float t, bool flapping)
        {
            var ang = flapping ? Mathf.Sin(t * g.flapRate + g.phase) * 32f : 6f + Mathf.Sin(t * 1.3f + g.phase) * 3f;
            g.l.localRotation = Quaternion.Euler(0, 0, -ang); g.r.localRotation = Quaternion.Euler(0, 0, ang);
        }

        // ---------------------------------------------------------------- the ship
        void MakeShip()
        {
            var rng = new System.Random(77);
            var mk = new MeshKit();
            var hull = WorldMaterials.Solid(new Color(.22f, .2f, .2f)); var sail = WorldMaterials.Solid(new Color(.82f, .8f, .76f));
            for (var i = 0; i < 7; i++)
            {
                var t = i / 6f - .5f; var w = Mathf.Lerp(3.2f, .9f, Mathf.Abs(t) * 2f);
                mk.Box(hull, new Vector3(0, 1.2f, t * 22f), new Vector3(w * 2f, 2.6f, 3.3f), Quaternion.identity);
            }
            mk.Box(hull, new Vector3(0, 3f, -8f), new Vector3(4.4f, 1.6f, 5f), Quaternion.identity);
            foreach (var (z, h) in new[] { (5f, 17f), (-3f, 20f) })
            {
                mk.Prism(hull, new Vector3(0, 2f, z), Vector3.up, .25f, h, 6);
                for (var k = 0; k < 3; k++)
                {
                    var y = 5f + k * 4.6f; var w = 7f - k * 1.6f;
                    mk.Quad(sail, new Vector3(-w, y, z + .4f), new Vector3(w, y, z + .4f), new Vector3(w * .85f, y + 4f, z + .4f), new Vector3(-w * .85f, y + 4f, z + .4f), new Vector3(0, y + 2f, z - 1f));
                    mk.Quad(sail, new Vector3(-w, y, z + .4f), new Vector3(w, y, z + .4f), new Vector3(w * .85f, y + 4f, z + .4f), new Vector3(-w * .85f, y + 4f, z + .4f), new Vector3(0, y + 2f, z + 2f));
                }
            }
            mk.Tri(sail, new Vector3(0, 4f, 11f), new Vector3(0, 16f, 6f), new Vector3(0, 3f, 17f), Vector3.right);
            mk.Tri(sail, new Vector3(0, 4f, 11f), new Vector3(0, 16f, 6f), new Vector3(0, 3f, 17f), Vector3.left);
            mk.Box(WorldMaterials.Glow(new Color(1f, .7f, .35f), 4f), new Vector3(0, 5f, -11f), new Vector3(.6f, .8f, .6f), Quaternion.identity);
            var go = mk.Build("Passing ship", transform, false);
            ship = go.transform;
            var l = new GameObject("Ship lantern").AddComponent<Light>(); l.transform.SetParent(ship, false);
            l.transform.localPosition = new Vector3(0, 5f, -12f); l.type = LightType.Point; l.range = 40f; l.intensity = 3f; l.color = new Color(1f, .7f, .35f);
            go.SetActive(false);
        }

        // ---------------------------------------------------------------- the whale
        void MakeWhale()
        {
            var mk = new MeshKit(); var skin = WorldMaterials.Solid(new Color(.2f, .22f, .26f));
            mk.Prism(skin, new Vector3(0, -6f, 0), Vector3.up, .7f, 6f, 8, .5f);
            foreach (var s in new[] { -1f, 1f })
                foreach (var inside in new[] { Vector3.forward, Vector3.back })
                    mk.Tri(skin, new Vector3(0, 0, 0), new Vector3(s * 4.2f, 1.4f, 0), new Vector3(s * 1.4f, -.6f, 0), inside);
            fluke = mk.Build("Whale tail", transform, false).transform;
            fluke.gameObject.SetActive(false);

            var go = new GameObject("Whale spout"); go.transform.SetParent(transform, false);
            spout = go.AddComponent<ParticleSystem>();
            spout.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = spout.main; main.loop = false; main.playOnAwake = false; main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(.8f, 2f); main.gravityModifier = .9f;
            main.startColor = new Color(.92f, .94f, .96f, .5f);
            var shape = spout.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 9f; shape.radius = .3f;
            shape.rotation = new Vector3(-90f, 0, 0);
            var e = spout.emission; e.rateOverTime = 0f;
            var col = spout.colorOverLifetime; col.enabled = true;
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(.8f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = gr;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mist != null ? mist.GetComponent<ParticleSystemRenderer>().sharedMaterial : Additive("Spout", Texture2D.whiteTexture);
        }

        // ---------------------------------------------------------------- lightning and the breaking light
        void MakeLightning()
        {
            var go = new GameObject("Lightning"); go.transform.SetParent(transform, false);
            bolt = go.AddComponent<LineRenderer>();
            bolt.useWorldSpace = true; bolt.positionCount = 12; bolt.widthMultiplier = 2.2f;
            bolt.sharedMaterial = Additive("Lightning", Texture2D.whiteTexture);
            bolt.startColor = bolt.endColor = new Color(.85f, .9f, 1f, 1f);
            bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bolt.enabled = false;
            flash = new GameObject("Lightning flash").AddComponent<Light>(); flash.transform.SetParent(transform, false);
            flash.type = LightType.Point; flash.range = 900f; flash.color = new Color(.8f, .86f, 1f); flash.intensity = 0f;
        }

        void MakeShafts()
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            var rng = new System.Random(9);
            for (var i = 0; i < 7; i++)
            {
                var x = ((float)rng.NextDouble() - .5f) * 520f; var w = 14f + (float)rng.NextDouble() * 22f;
                var lean = 40f + (float)rng.NextDouble() * 30f;
                var b = v.Count;
                v.Add(new Vector3(x - w, 0, 0)); v.Add(new Vector3(x + w, 0, 0)); v.Add(new Vector3(x + w * 1.6f + lean, 160f, 0)); v.Add(new Vector3(x - w * 1.6f + lean, 160f, 0));
                var a = .1f + (float)rng.NextDouble() * .12f;
                c.Add(new Color(1f, .86f, .66f, a)); c.Add(new Color(1f, .86f, .66f, a)); c.Add(new Color(1f, .86f, .66f, 0f)); c.Add(new Color(1f, .86f, .66f, 0f));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "Light shafts" };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Light shafts"); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>(); shaftMat = Additive("Light shafts", Texture2D.whiteTexture);
            rend.sharedMaterial = shaftMat; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mid = (laneFrom + laneTo) / 2f;
            go.transform.SetPositionAndRotation(Vector3.Lerp(new Vector3(center.x, 0, center.z), mid, .75f), Quaternion.LookRotation(seaDir));
            shafts = go.transform;
            go.SetActive(false);
        }

        // ---------------------------------------------------------------- the bottle
        void MakeBottle()
        {
            var mk = new MeshKit();
            var glass = WorldMaterials.Glow(new Color(.35f, .85f, .5f), 1.6f);
            mk.Prism(glass, new Vector3(0, .12f, -.2f), Vector3.forward, .12f, .3f, 8);
            mk.Prism(glass, new Vector3(0, .12f, .1f), Vector3.forward, .12f, .12f, 8, .4f);
            mk.Prism(glass, new Vector3(0, .12f, .22f), Vector3.forward, .05f, .1f, 8);
            mk.Prism(WorldMaterials.Solid(new Color(.6f, .45f, .3f)), new Vector3(0, .12f, .32f), Vector3.forward, .045f, .05f, 8);
            mk.Box(WorldMaterials.Solid(new Color(.92f, .88f, .76f)), new Vector3(0, .12f, -.08f), new Vector3(.12f, .06f, .2f), Quaternion.identity);
            bottle = mk.Build("Message in a bottle", transform, false).transform;
            var l = new GameObject("Bottle glow").AddComponent<Light>(); l.transform.SetParent(bottle, false);
            l.transform.localPosition = new Vector3(0, .5f, 0); l.type = LightType.Point; l.range = 4f; l.intensity = 1.2f; l.color = new Color(.4f, 1f, .6f);
            bottle.gameObject.SetActive(false);
        }

        void PlaceBottle(long day)
        {
            bottleDay = day;
            if (bottle == null || landmarks.Count == 0) return;
            var key = SharedChance.Key("driftwood-bottle");
            var lm = landmarks[(int)(SharedChance.Value(key, day) * landmarks.Count) % landmarks.Count];
            var a = (float)(SharedChance.Value(key, day, 1) * Mathf.PI * 2.0);
            var p = lm + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 7f;
            if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out var hit, 60f)) p = hit.point;
            bottle.SetPositionAndRotation(p, Quaternion.Euler(0, a * Mathf.Rad2Deg, 8f));
            bottle.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- each frame
        void UpdateHappenings(bool here, float valence, bool drizzling)
        {
            var now = SharedChance.Now; var t = Time.time; var dt = Time.deltaTime;
            if (SharedChance.Day != bottleDay) PlaceBottle(SharedChance.Day);

            // a new song: what happens during it
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != eventSong)
            {
                eventSong = song; songStart = start; songLength = length;
                var k = SharedChance.Key("driftwood-flock");
                flockAt = SharedChance.Roll(k, song, .4) ? start + length * (.1 + .6 * SharedChance.Value(k, song, 1)) : -1;
                shipOn = hasLane && SharedChance.Roll(SharedChance.Key("driftwood-ship"), song, .35);
                k = SharedChance.Key("driftwood-whale");
                whaleAt = hasLane && SharedChance.Roll(k, song, .3) ? start + length * (.15 + .55 * SharedChance.Value(k, song, 1)) : -1;
                whaleSpot = Vector3.Lerp(laneFrom, laneTo, .25f + .5f * (float)SharedChance.Value(k, song, 2));
                spoutsDone = 0;
                sunbreak = hasLane && valence > .5f && SharedChance.Roll(SharedChance.Key("driftwood-sunbreak"), song, .4);
            }

            foreach (var g in wheeling) g.t.gameObject.SetActive(here);
            if (here)
            {
                // gulls wheeling over the shore
                foreach (var g in wheeling)
                {
                    var a = g.phase + (float)(now % 100000.0) * g.speed / g.radius;
                    var p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * g.radius + Vector3.up * (g.height + Mathf.Sin(a * 2f) * 3f);
                    var fwd = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                    g.t.SetPositionAndRotation(p, Quaternion.LookRotation(fwd) * Quaternion.Euler(0, 0, -18f));
                    Flap(g, t, Mathf.Sin(t * .35f + g.phase) > .4f);
                }
            }

            // a flock coming in off the sea
            var ft = (float)(now - flockAt);
            var flying = here && flockAt > 0 && ft > 0 && ft < 75f;
            for (var i = 0; i < flock.Count; i++)
            {
                var g = flock[i];
                g.t.gameObject.SetActive(flying);
                if (!flying) continue;
                var across = Vector3.Cross(Vector3.up, seaDir);
                var row = i / 2; var side = i % 2 == 0 ? 1f : -1f;
                var p = center + seaDir * (520f - ft * 14f) - seaDir * row * 3.5f + across * (side * row * 3f + Mathf.Sin(ft * .3f + i) * 1.5f) + Vector3.up * (38f + Mathf.Sin(ft * .5f + i) * 2f);
                g.t.SetPositionAndRotation(p, Quaternion.LookRotation(-seaDir));
                Flap(g, t, true);
            }

            // the passing ship, crossing the horizon over the whole song
            if (ship != null)
            {
                ship.gameObject.SetActive(here && shipOn);
                if (here && shipOn && songLength > 0)
                {
                    var f = Mathf.Clamp01((float)((now - songStart) / songLength));
                    var p = Vector3.Lerp(laneFrom, laneTo, f) + seaDir * 60f;
                    p.y = Mathf.Sin(t * .7f) * .3f;
                    ship.SetPositionAndRotation(p, Quaternion.LookRotation(laneTo - laneFrom) * Quaternion.Euler(Mathf.Sin(t * .5f) * 1.5f, 0, Mathf.Sin(t * .8f) * 3f));
                }
            }

            // the whale: three spouts, then the tail
            if (fluke != null)
            {
                var wt = (float)(now - whaleAt);
                var on = here && whaleAt > 0 && wt > 0 && wt < 30f;
                if (on && spoutsDone < 3 && wt > spoutsDone * 6f)
                {
                    spout.transform.position = whaleSpot + seaDir * spoutsDone * 6f;
                    spout.Emit(90);
                    spoutsDone++;
                }
                var tail = on && wt > 18f && wt < 28f;
                fluke.gameObject.SetActive(tail);
                if (tail)
                {
                    var u = (wt - 18f) / 10f;
                    var rise = Mathf.Sin(u * Mathf.PI);
                    fluke.SetPositionAndRotation(whaleSpot + seaDir * 22f + Vector3.up * (-6f + rise * 7.5f), Quaternion.LookRotation(Vector3.Cross(Vector3.up, seaDir)) * Quaternion.Euler(-30f + u * 60f, 0, 0));
                    if (u > .1f && u < .4f && Random.value < dt * 20f) spout.Emit(1);
                }
            }

            // lightning far out to sea in the drizzly songs
            if (bolt != null)
            {
                var win = (long)(now / 5.0);
                var k = SharedChance.Key("driftwood-lightning");
                var strike = here && drizzling && SharedChance.Roll(k, win, .3);
                var at = win * 5.0 + SharedChance.Value(k, win, 1) * 4.0;
                var age = (float)(now - at);
                var lit = strike && age > 0f && age < .45f && (age < .08f || (age > .18f && age < .3f));
                bolt.enabled = lit;
                flash.intensity = lit ? 7f : 0f;
                if (lit)
                {
                    var foot = Vector3.Lerp(laneFrom, laneTo, (float)SharedChance.Value(k, win, 2)) + seaDir * (100f + 200f * (float)SharedChance.Value(k, win, 3));
                    var rnd = new System.Random((int)(win & 0x7fffffff));
                    for (var i = 0; i < bolt.positionCount; i++)
                    {
                        var f = i / (float)(bolt.positionCount - 1);
                        bolt.SetPosition(i, foot + Vector3.up * (150f * (1f - f)) + new Vector3((float)rnd.NextDouble() - .5f, 0, (float)rnd.NextDouble() - .5f) * (i == 0 || i == bolt.positionCount - 1 ? 0f : 18f));
                    }
                    flash.transform.position = foot + Vector3.up * 120f;
                }
            }

            // the cloud breaking: shafts of light on the sea
            if (shafts != null)
            {
                var f = songLength > 0 ? (float)((now - songStart) / songLength) : 0f;
                var want = here && sunbreak && f > .25f && f < .8f ? 1f : 0f;
                shaftsShown = Mathf.MoveTowards(shaftsShown, want, dt / 8f);
                shafts.gameObject.SetActive(shaftsShown > .01f);
                shaftMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, shaftsShown * (.8f + .2f * Mathf.Sin(t * .4f))));
            }
        }

        /// How much the light is breaking through right now (0..1), for the sky tint and the clouds.
        float Sunbreak => shaftsShown;
    }
}
