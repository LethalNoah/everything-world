using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The deep pass's storm and moving parts (part of IronLife): during about half the songs (shared clock, so
    /// friends get the same storms) a storm surge rolls in for the middle of the song: heavier rain, lightning every
    /// few seconds; outside a surge the lightning still comes more often than before. Strikes often hit the artists'
    /// beacons, throwing sparks. Glows tied to songs brighten while their song plays; the music box dancer turns;
    /// the teleporter crackles; mist lies over the zombies corner.
    public partial class IronLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public List<Vector3> beacons = new List<Vector3>(), teleporter = new List<Vector3>(), mists = new List<Vector3>();
        public List<Transform> spinners = new List<Transform>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        LineRenderer tele; ParticleSystem mist; float storm, nextTele, nextMist;
        long stormSong = long.MinValue; double stormFrom, stormTo;

        void StormStart()
        {
            tele = Line("Teleporter arc", new Color(.5f, .75f, 1f), .12f);
            mist = Particles("Zombie mist", 400, new Color(.6f, .75f, .65f, .22f), 3f, 6f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .3f);
        }

        /// How strong the storm surge is right now (0 to 1).
        float Storm(float dt)
        {
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != stormSong)
            {
                stormSong = song; var k = SharedChance.Key("iron-storm");
                if (SharedChance.Roll(k, song, .45)) { stormFrom = start + length * .25; stormTo = start + length * .8; } else stormFrom = stormTo = -1;
            }
            var now = SharedChance.Now; var on = stormFrom > 0 && now > stormFrom && now < stormTo;
            storm = Mathf.MoveTowards(storm, on ? 1f : 0f, dt / 6f); return storm;
        }

        void StormUpdate(float dt, float t, string id, bool here)
        {
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows)
            {
                if (m == null) continue; var k = songAmount[song];
                var fade = song == IronKit.FadeToBlack ? 1f - k * .95f : 1f + k * (2.5f + Mathf.Sin(t * 2.2f) * .6f);   // Fade To Black drains its light instead
                m.SetColor("_EmissionColor", c * fade);
            }
            foreach (var s in spinners) if (s != null) s.Rotate(0, 40f * dt, 0, Space.World);
            nextTele -= dt;
            if (here && teleporter.Count >= 2 && nextTele <= 0f) { nextTele = Random.Range(.05f, .25f); if (Random.value < .6f) { var a = teleporter[Random.Range(0, teleporter.Count)]; var b = teleporter[Random.Range(0, teleporter.Count)]; if (a != b) Jag(tele, a, b, 8, .4f); } else tele.positionCount = 0; }
            if (!here && tele.positionCount > 0) tele.positionCount = 0;
            nextMist -= dt;
            if (here && mists.Count > 0 && nextMist <= 0f) { nextMist = .3f; mist.transform.position = mists[Random.Range(0, mists.Count)] + new Vector3(Random.Range(-6f, 6f), Random.Range(.3f, 1.2f), Random.Range(-6f, 6f)); mist.Emit(1); }
        }

        /// Where a lightning strike lands: often on an artist's beacon near you, otherwise anywhere round you.
        Vector3 StrikePoint(Vector3 cam, out bool onBeacon)
        {
            onBeacon = false;
            if (beacons.Count > 0 && Random.value < .45f)
            {
                var near = new List<Vector3>(); foreach (var b in beacons) if ((b - cam).sqrMagnitude < 320f * 320f) near.Add(b);
                if (near.Count > 0) { onBeacon = true; return near[Random.Range(0, near.Count)]; }
            }
            var hit = cam + new Vector3(Random.Range(-260f, 260f), 0, Random.Range(-260f, 260f)); hit.y = cam.y - 10f; return hit;
        }
    }
}
