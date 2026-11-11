using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Small Hours' song places (part of SmallHoursLife): each glow brightens and breathes while its song plays.
    public partial class SmallHoursLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        void SongPlacesUpdate(float dt, string id)
        {
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == "The Small Hours";
            var t = Time.time;
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.5f + Mathf.Sin(t * 2f) * .6f)));
        }
    }
}
