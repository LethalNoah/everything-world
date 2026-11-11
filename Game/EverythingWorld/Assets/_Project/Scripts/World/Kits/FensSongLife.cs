using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Fens' song places alive (part of FensLife): each glow brightens and breathes while its song plays, and
    /// the floating pieces (the drifting house, the moon cradle, the snow, the petals, the lanterns) bob, rock and
    /// turn, a little more while their song plays.
    public partial class FensLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        /// t, rest position, bob height, bob speed, rocking (degrees), turning (degrees a second), song
        public List<(Transform t, Vector3 rest, float bob, float speed, float rock, float turn, string song)> movers = new List<(Transform, Vector3, float, float, float, float, string)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        void SongPlacesUpdate(float dt, bool here, string id)
        {
            var t = Time.time;
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.5f + Mathf.Sin(t * 1.6f) * .6f)));
            for (var i = 0; i < movers.Count; i++)
            {
                var v = movers[i]; if (v.t == null) continue;
                songAmount.TryGetValue(v.song, out var amt); var k = 1f + amt;
                var ph = i * 1.37f;
                v.t.position = v.rest + Vector3.up * (Mathf.Sin(t * v.speed * k + ph) * v.bob * k);
                var yaw = v.turn != 0f ? Mathf.Repeat(t * v.turn * k + ph * 40f, 360f) : 0f;
                v.t.rotation = Quaternion.Euler(Mathf.Sin(t * v.speed * .8f + ph) * v.rock * k, yaw, Mathf.Cos(t * v.speed * .6f + ph) * v.rock * .6f * k);
            }
        }
    }
}
