using UnityEngine;

namespace EverythingWorld.Music
{
    /// Simple on-screen text: which zone you're in and what would be playing.
    public class MusicHud : MonoBehaviour
    {
        public MusicDirector director;
        public DryRunMusicOutput dryRun;

        GUIStyle style;

        void OnGUI()
        {
            if (director == null) return;
            style ??= new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.UpperLeft, wordWrap = true };

            var here = director.CurrentScene;
            var text = here != null ? $"Zone: {here.DisplayName} ({here.realmName}, scene {here.id})" : "Zone: none";
            if (dryRun != null && dryRun.LastTrack != null)
                text += $"\nWould play: {dryRun.LastTrack}\n{dryRun.LastTrack.Uri}";
            text += "\n\nDRY RUN: Spotify is not being controlled.\nWASD move, mouse look, Esc frees the cursor.";

            GUI.Box(new Rect(12, 12, 520, 140), text, style);
        }
    }
}
