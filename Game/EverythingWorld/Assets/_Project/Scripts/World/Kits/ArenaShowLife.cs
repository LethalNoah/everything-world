using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The live show answering the music (part of ArenaLife): the crowd sways in waves, faster with the energy; the
    /// spotlights on the band breathe; the lasers sweep in fans over the crowd and the sky (turning cold blue for
    /// the Reaper, all blazing for the anthems); flame jets fire along the stage front on big beats; fireworks
    /// burst over the stadium near the end of the anthems (from the shared clock, so friends see the same ones);
    /// glows tied to songs brighten while their song plays; the fire pit roars for "Burnin' for You".
    public partial class ArenaLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public List<Transform> crowds = new List<Transform>();
        public List<(Transform t, Material m, Color c, int k)> lasers = new List<(Transform, Material, Color, int)>();
        public Vector3 laserForward = Vector3.forward, fireworksAt, burnPit; public List<Vector3> pyro = new List<Vector3>();
        public Material spots, reaperSingle;
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        static readonly HashSet<string> Anthems = new HashSet<string> { ArenaKit.Believin, ArenaKit.CarryOn, ArenaKit.Renegade, ArenaKit.Tiger, ArenaKit.Feeling, ArenaKit.WheelInTheSky, ArenaKit.BuiltThisCity };
        ParticleSystem jets, fireworks; int lastBeat = -1; float nextBurst;

        float Amount(string song) => songAmount.TryGetValue(song, out var k) ? k : 0f;

        void ShowStart()
        {
            jets = Particles("Flame jets", 1200, new Color(1f, .6f, .2f, 1f), .6f, 1.4f);
            var jm = jets.main; jm.startLifetime = new ParticleSystem.MinMaxCurve(.5f, .9f); jm.startSpeed = new ParticleSystem.MinMaxCurve(10f, 15f);
            var js = jets.shape; js.shapeType = ParticleSystemShapeType.Cone; js.angle = 5f; js.radius = .2f; js.rotation = new Vector3(-90f, 0, 0);
            fireworks = Particles("Fireworks", 2000, Color.white, .25f, .5f);
            var fm = fireworks.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f); fm.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f); fm.gravityModifier = .35f;
            var fs = fireworks.shape; fs.shapeType = ParticleSystemShapeType.Sphere; fs.radius = .5f;
        }

        void ShowUpdate(float dt, float t, string id, bool here, float energy, float progress, double now)
        {
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + Amount(song) * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            var anthem = here && id != null && Anthems.Contains(id);
            var cold = Mathf.Max(reap, Amount(ArenaKit.ReaperSingle));

            for (var i = 0; i < crowds.Count; i++)
                if (crowds[i] != null) crowds[i].localRotation = Quaternion.Euler(Mathf.Sin(t * (1.6f + energy * 2f) + i * .9f) * (1f + energy * 1.5f + (anthem ? 1.5f : 0f)) * .6f, 0, Mathf.Sin(t * (1.2f + energy) + i * 1.3f) * (1.2f + energy * 2f) * .5f);
            if (spots != null) spots.SetColor("_BaseColor", Color.Lerp(new Color(1f, .95f, .85f), new Color(.5f, .7f, 1f), cold) * new Color(1, 1, 1, .35f + .15f * Mathf.Sin(t * 1.5f) + energy * .2f));
            foreach (var (tr, m, c, k) in lasers)
            {
                if (tr == null) continue; tr.gameObject.SetActive(here);
                var fan = (k - 3.5f) * 9f + Mathf.Sin(t * (.6f + energy) + k * .4f) * 25f; var tilt = -8f - 18f * (.5f + .5f * Mathf.Sin(t * .45f + k * .7f));
                tr.rotation = Quaternion.LookRotation(laserForward) * Quaternion.Euler(tilt, fan, 0);
                var on = Mathf.Clamp01(.25f + energy * .8f + (anthem ? .6f : 0f)); var col = Color.Lerp(c, new Color(.35f, .55f, 1f), cold); col.a = on * (.6f + .4f * Mathf.Sin(t * 7f + k));
                m.SetColor("_BaseColor", col);
            }
            // flame jets on the beat, at about 120 bpm, when the music is big
            var beat = Mathf.FloorToInt((float)(now * 2.0));
            if (here && beat != lastBeat && pyro.Count > 0)
            {
                lastBeat = beat;
                if ((anthem && beat % 4 == 0) || (energy > .7f && beat % 8 == 0)) foreach (var p in pyro) { jets.transform.position = p; jets.Emit(40); }
            }
            // fireworks near the end of an anthem
            nextBurst -= dt;
            if (anthem && progress > .82f && progress < .97f && nextBurst <= 0f && fireworksAt != Vector3.zero)
            {
                nextBurst = Random.Range(.35f, .8f);
                var p = fireworksAt + new Vector3(Random.Range(-30f, 30f), Random.Range(0f, 25f), Random.Range(-30f, 30f));
                Color[] burst = { new Color(1f, .8f, .3f), new Color(.4f, .7f, 1f), new Color(1f, .35f, .3f), new Color(.6f, 1f, .5f), Color.white };
                var fm = fireworks.main; fm.startColor = burst[Random.Range(0, burst.Length)]; fireworks.transform.position = p; fireworks.Emit(140);
            }
            // the fire pit roars for Burnin' for You
            var burn = Amount(ArenaKit.BurninForYou);
            if (here && burn > .1f && burnPit != Vector3.zero && Random.value < .5f) { flame.transform.position = burnPit; flame.Emit(4); }
        }
    }
}
