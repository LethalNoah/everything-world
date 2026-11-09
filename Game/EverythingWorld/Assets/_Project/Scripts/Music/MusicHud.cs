using EverythingWorld.Spotify;
using UnityEngine;

namespace EverythingWorld.Music
{
    /// Simple on-screen text: which zone you're in, what its station is playing and the Spotify status.
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

            var slot = director.CurrentSlot;
            if (director.PlayingScene != null && slot.IsValid)
            {
                var now = RadioSchedule.NowSeconds;
                var pos = slot.PositionMsAt(now);
                var length = (int)((slot.endsAt - slot.startsAt) * 1000);
                text += $"\nOn air ({director.PlayingScene.DisplayName}): {slot.track}  {Time(pos)} / {Time(length)}";
            }

            if (useSpotify && spotify != null)
            {
                text += $"\n\nSPOTIFY ON: {spotify.Status}";
                if (spotify.LastRequestMs >= 0) text += $"\nLast switch took {spotify.LastRequestMs:0} ms";
                if (spotify.HasDrift) text += $", Spotify was {spotify.LastDriftMs / 1000f:+0.0;-0.0} s from the station";
            }
            else
            {
                text += "\n\nDRY RUN: Spotify is not being controlled.";
                if (spotify != null) text += $"\nSpotify login: {spotify.Status}";
            }
            text += "\nWASD move, Space jumps, mouse look, Esc frees the cursor, L logs in to Spotify.";

            GUI.Box(new Rect(12, 12, 620, 200), text, style);
        }

        static string Time(int ms) => $"{ms / 60000}:{ms / 1000 % 60:00}";
    }
}
