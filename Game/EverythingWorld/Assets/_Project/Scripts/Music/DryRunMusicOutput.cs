using UnityEngine;

namespace EverythingWorld.Music
{
    /// Pretends to play music: logs what it would send to Spotify and remembers it for the HUD.
    /// Makes no network calls and never touches Spotify.
    public class DryRunMusicOutput : MonoBehaviour, IMusicOutput
    {
        public TrackInfo LastTrack { get; private set; }
        public SceneInfo LastScene { get; private set; }

        public void Play(TrackInfo track, SceneInfo scene, int positionMs, bool continuation)
        {
            LastTrack = track;
            LastScene = scene;
            Debug.Log($"[Dry run] Would play {track} from {positionMs / 60000}:{positionMs / 1000 % 60:00} " +
                      $"({track.Uri}) on station {scene.id}: {scene.DisplayName}{(continuation ? " (next song)" : "")}");
        }
    }
}
