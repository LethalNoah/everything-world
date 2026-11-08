using EverythingWorld.Spotify;
using UnityEngine;

namespace EverythingWorld.Music
{
    /// Sends play commands to the dry run (always) and to Spotify (only when "Use Spotify" is ticked).
    /// Spotify is off by default: tick "Use Spotify" on the Music object (in the Inspector) to let the game control it.
    public class MusicOutputSwitch : MonoBehaviour, IMusicOutput
    {
        [Tooltip("When ticked, walking into a zone starts that song on your Spotify. Off = dry run only.")]
        public bool useSpotify;

        public DryRunMusicOutput dryRun;
        public SpotifyMusicOutput spotify;

        public void Play(TrackInfo track, SceneInfo scene)
        {
            if (dryRun != null) dryRun.Play(track, scene);
            if (useSpotify && spotify != null) spotify.Play(track, scene);
        }
    }
}
