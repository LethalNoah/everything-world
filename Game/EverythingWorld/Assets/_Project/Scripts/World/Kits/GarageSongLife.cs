using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Garage Rock Row's song places (part of GarageLife): each glow brightens and pulses while its song plays.
    public partial class GarageLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        void SongPlacesUpdate(float dt, bool here, string id)
        {
            var t = Time.time;
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 1.5f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.5f + Mathf.Sin(t * 3f) * .8f)));
        }
    }
}
