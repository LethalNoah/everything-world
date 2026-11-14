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

        bool show;
        float card = -99f;   // when N was last pressed
        const float CardSeconds = 6f;
        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; if (kb == null || EverythingWorld.World.GameKeys.Off) return;
            if (kb.f1Key.wasPressedThisFrame) show = !show;
            if (kb.nKey.wasPressedThisFrame) card = UnityEngine.Time.unscaledTime > card + CardSeconds - .6f ? UnityEngine.Time.unscaledTime : -99f;   // N again hides it
        }

        GUIStyle small, title, artist; static Texture2D white;
        /// Now playing: N shows a small card in the corner for a few seconds (the song, the artist, the station and
        /// how far through it is), then it fades away.
        void NowPlayingCard()
        {
            var age = UnityEngine.Time.unscaledTime - card; if (age < 0f || age > CardSeconds) return;
            var a = Mathf.Clamp01(age / .25f) * Mathf.Clamp01((CardSeconds - age) / .8f);
            var fonts = EverythingWorld.World.GameFonts.Box(15, TextAnchor.UpperLeft);
            small ??= new GUIStyle(fonts) { normal = { background = null, textColor = new Color(1f, .82f, .4f) }, padding = new RectOffset(0, 0, 0, 0) };
            title ??= new GUIStyle(EverythingWorld.World.GameFonts.Box(26, TextAnchor.UpperLeft, false, true)) { normal = { background = null, textColor = Color.white }, padding = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Clip };
            artist ??= new GUIStyle(fonts) { fontSize = 19, normal = { background = null, textColor = new Color(.85f, .88f, 1f) }, padding = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Clip };
            if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }

            var slot = director != null ? director.CurrentSlot : default;
            var playing = director != null && director.PlayingScene != null && slot.IsValid && !MusicDirector.Silent;
            var box = new Rect(24, Screen.height - 148, 460, 124);
            var keep = GUI.color; GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Box(box, "", fonts);
            if (!playing) GUI.Label(new Rect(box.x + 18, box.y + 46, 420, 30), "No music here", artist);
            else
            {
                GUI.Label(new Rect(box.x + 18, box.y + 12, 424, 22), "NOW PLAYING  \u00b7  " + director.PlayingScene.DisplayName.ToUpperInvariant(), small);
                GUI.Label(new Rect(box.x + 18, box.y + 36, 424, 36), slot.track.name, title);
                GUI.Label(new Rect(box.x + 18, box.y + 72, 424, 26), slot.track.artist, artist);
                // how far through: a thin gold bar, and the times
                var pos = slot.PositionMsAt(RadioSchedule.NowSeconds); var length = (int)((slot.endsAt - slot.startsAt) * 1000);
                var f = length > 0 ? Mathf.Clamp01(pos / (float)length) : 0f;
                var bar = new Rect(box.x + 18, box.y + 106, 330, 4);
                GUI.color = new Color(1f, 1f, 1f, .18f * a); GUI.DrawTexture(bar, white);
                GUI.color = new Color(1f, .8f, .35f, a); GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * f, bar.height), white);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(bar.xMax + 12, box.y + 97, 100, 22), Time(Mathf.Min(pos, length)) + " / " + Time(length), small);
            }
            GUI.color = keep;
        }

        /// The music details (for checking Spotify): F1 shows and hides them.
        void OnGUI()
        {
            if (director == null || EverythingWorld.Player.MainMenu.Showing) return;
            NowPlayingCard();
            if (!show) return;
            style ??= EverythingWorld.World.GameFonts.Box(17, TextAnchor.UpperLeft, true);

            var useSpotify = outputSwitch != null && outputSwitch.useSpotify;
            var here = director.CurrentScene;
            var text = here == null ? "Zone: none" : string.IsNullOrEmpty(here.realmName) ? $"Zone: {here.DisplayName} (artist station)" : $"Zone: {here.DisplayName} ({here.realmName}, scene {here.id})";

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
            text += "\nF1 hides this. L logs in to Spotify; - and = change its volume.";

            GUI.Box(new Rect(12, 12, 620, 200), text, style);
        }

        static string Time(int ms) => $"{ms / 60000}:{ms / 1000 % 60:00}";
    }
}
