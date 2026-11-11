using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Song glows on the Bayou (part of BayouLife): each brightens and breathes while its song plays; the railroad
    /// crossing's lights flash in turn.
    public partial class BayouLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public List<Material> flashers = new List<Material>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        void SongGlowUpdate(float dt, float t, string id, bool here)
        {
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows)
            {
                if (m == null) continue; var k = songAmount[song];
                var flash = flashers.Contains(m) ? (Mathf.Repeat(t, 1f) < .5f ? 1f : .25f) : 1f;
                m.SetColor("_EmissionColor", c * flash * (1f + k * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            }
        }
    }
}
