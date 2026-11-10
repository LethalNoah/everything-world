using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The song places answering their songs (part of ProgLife): song glows brighten and breathe while their song
    /// plays; the hall of clocks ticks (and spins wildly during "Time"), its pendulum swings; the wall's spotlight
    /// blazes during "Comfortably Numb"; the cigar smokes; the orrery's planets wheel (faster during "Astronomy
    /// Domine"); the fish swim round their bowl; the sofa floats and turns; the Starless stars go out one by one;
    /// the radio sends rings of light up (strong during "The Spirit Of Radio"); lights chase along the pipeline
    /// (racing during "Pipeline").
    public partial class ProgLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public List<(Transform t, float speed)> clockHands = new List<(Transform, float)>();
        public Transform pendulum, sofa; public Quaternion pendulumRot = Quaternion.identity; public Vector3 sofaBase;
        public Material spotlight;
        public List<Vector3> smoke = new List<Vector3>();
        public List<(Transform t, float speed)> planets = new List<(Transform, float)>();
        public List<(Transform t, Vector3 c, float r, float phase)> fish = new List<(Transform, Vector3, float, float)>();
        public List<(Material m, Color c)> stars = new List<(Material, Color)>();
        public List<(Transform t, Material m, Vector3 at, float phase)> radioRings = new List<(Transform, Material, Vector3, float)>();
        public List<Material> pipeline = new List<Material>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        readonly HashSet<string> tracked = new HashSet<string> { ProgKit.TimeSong, ProgKit.ComfortablyNumb, ProgKit.Starless, ProgKit.Pipeline, ProgKit.SpiritOfRadio, ProgKit.AstronomyDomine, ProgKit.SofaNo1 };
        ParticleSystem cigarSmoke; float nextSmoke;

        float Amount(string song) => songAmount.TryGetValue(song, out var k) ? k : 0f;

        void FloydStart()
        {
            foreach (var g in songGlows) tracked.Add(g.song);
            cigarSmoke = Particles("Cigar smoke", 300, new Color(.8f, .8f, .82f, .35f), .3f, .9f, false);
            var sm = cigarSmoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(.3f, .7f);
        }

        void FloydUpdate(float dt, float t, string id, bool here, float energy)
        {
            foreach (var s in tracked) { var k = Amount(s); songAmount[s] = Mathf.MoveTowards(k, here && id == s ? 1f : 0f, dt / 2f); }
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + Amount(song) * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            var time = Amount(ProgKit.TimeSong);
            foreach (var (tr, speed) in clockHands) if (tr != null) tr.Rotate(0, 0, -speed * dt * (1f + time * 60f), Space.Self);
            if (pendulum != null) pendulum.rotation = pendulumRot * Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * (1f + time)) * 14f);
            if (spotlight != null) spotlight.SetColor("_BaseColor", new Color(1f, 1f, 1f, .12f + Amount(ProgKit.ComfortablyNumb) * .7f));
            var astro = Amount(ProgKit.AstronomyDomine);
            foreach (var (tr, speed) in planets) if (tr != null) tr.Rotate(0, speed * dt * (1f + energy + astro * 5f), 0, Space.World);
            foreach (var (tr, c, r, phase) in fish)
            {
                if (tr == null) continue; var a = t * .6f + phase; var p = c + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(t * .9f + phase) * .35f, Mathf.Sin(a) * r);
                tr.SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, Mathf.Sin(t * 6f + phase) * 12f, 0));
            }
            if (sofa != null) sofa.SetPositionAndRotation(sofaBase + Vector3.up * Mathf.Sin(t * .5f) * .5f, Quaternion.Euler(Mathf.Sin(t * .4f) * 6f, t * (8f + Amount(ProgKit.SofaNo1) * 30f), Mathf.Cos(t * .3f) * 5f));
            var starless = Amount(ProgKit.Starless);
            for (var k = 0; k < stars.Count; k++) { var (m, c) = stars[k]; if (m == null) continue; var gone = Mathf.Clamp01(starless * stars.Count - k); m.SetColor("_EmissionColor", c * (1f - gone * .97f) * (.85f + .15f * Mathf.Sin(t * 3f + k))); }
            var radio = Amount(ProgKit.SpiritOfRadio);
            foreach (var (tr, m, at, phase) in radioRings)
            {
                if (tr == null) continue; var f = Mathf.Repeat(t * (.25f + radio * .5f) + phase, 1f);
                tr.position = at + Vector3.up * f * 3f; tr.localScale = Vector3.one * (.6f + f * 3.4f);
                m.SetColor("_EmissionColor", new Color(.4f, .8f, 1f) * (1f - f) * (.4f + radio * 2.6f));
            }
            var pipe = Amount(ProgKit.Pipeline);
            if (pipeline.Count > 0)
            {
                var head = Mathf.Repeat(t * (3f + pipe * 9f), pipeline.Count + 3f);
                for (var k = 0; k < pipeline.Count; k++) { var d = head - k; var lit = d >= 0f && d < 3f ? 1f - d / 3f : 0f; pipeline[k].SetColor("_EmissionColor", new Color(1f, .8f, .35f) * (.15f + lit * (1.4f + pipe * 2f))); }
            }
            nextSmoke -= dt;
            if (here && smoke.Count > 0 && nextSmoke <= 0f) { nextSmoke = .25f; foreach (var s in smoke) { cigarSmoke.transform.position = s; cigarSmoke.Emit(1); } }
        }
    }
}
