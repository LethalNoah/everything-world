using System;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Sits on an artist's area. When the radio you're tuned to plays one of this artist's songs, the
    /// area's marker lights up brighter, so you can follow the light to whoever is on.
    public class ArtistArea : MonoBehaviour
    {
        public string artistName;
        public int stationId;
        public float radius;
        public Renderer[] glowRenderers;
        public Light glowLight;
        public Color glowColor = new Color(1f, .66f, .3f);
        public float idleGlow = .9f, playingGlow = 2.6f;

        bool playing;
        float nextCheck, baseIntensity, baseRange;
        MaterialPropertyBlock block;

        void Start()
        {
            if (glowLight != null) { baseIntensity = glowLight.intensity; baseRange = glowLight.range; }
            Apply();
        }

        void Update()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + .5f;
            var d = MusicDirector.Instance;
            var now = d != null && d.CurrentSlot.IsValid &&
                      string.Equals(d.CurrentSlot.track.artist, artistName, StringComparison.OrdinalIgnoreCase);
            if (now == playing) return;
            playing = now;
            Apply();
        }

        void Apply()
        {
            block ??= new MaterialPropertyBlock();
            foreach (var r in glowRenderers ?? new Renderer[0])
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", glowColor * (playing ? playingGlow : idleGlow));
                r.SetPropertyBlock(block);
            }
            if (glowLight != null)
            {
                glowLight.intensity = playing ? baseIntensity * 3.5f : baseIntensity;
                glowLight.range = playing ? baseRange * 1.8f : baseRange;
            }
        }
    }
}
