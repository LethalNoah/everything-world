using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Song glows on the Downs (part of DownsLife): each brightens and breathes while its song plays.
    public partial class DownsLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        void SongGlowUpdate(float dt, bool here)
        {
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null); var t = Time.time;
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
        }
    }
}
