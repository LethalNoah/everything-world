using EverythingWorld.Spotify;
using UnityEngine;

namespace EverythingWorld.Music
{
    /// Simple on-screen text: which zone you're in, what's playing and the Spotify status.
    public class MusicHud : MonoBehaviour
    {
        public MusicDirector director;
        public DryRunMusicOutput dryRun;
        public MusicOutputSwitch outputSwitch;
        public SpotifyMusicOutput spotify;

        GUIStyle style;

        void OnGUI()
        {
            if (director == null) return;
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.UpperLeft, wordWrap = true };

            var useSpotify = outputSwitch != null && outputSwitch.useSpotify;
            var here = director.CurrentScene;
            var text = here != null ? $"Zone: {here.DisplayName} ({here.realmName}, scene {here.id})" : "Zone: none";
            if (dryRun != null && dryRun.LastTrack != null)
                text += $"\n{(useSpotify ? "Song" : "Would play")}: {dryRun.LastTrack}";

            if (useSpotify && spotify != null)
            {
                text += $"\n\nSPOTIFY ON: {spotify.Status}";
                if (spotify.LastRequestMs >= 0) text += $"\nLast switch took {spotify.LastRequestMs:0} ms";
            }
            else
            {
                text += "\n\nDRY RUN: Spotify is not being controlled.";
                if (spotify != null) text += $"\nSpotify login: {spotify.Status}";
            }
            text += "\nWASD move, mouse look, Esc frees the cursor, L logs in to Spotify.";

            GUI.Box(new Rect(12, 12, 560, 190), text, style);
        }
    }
}
