using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using EverythingWorld.Music;
using EverythingWorld.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// The terminal on the hub's plaza: an old computer with a green screen you can type into. Press E to sit at
    /// it; type, Enter to send, Esc to stand up. It knows a few commands (HELP lists them), and one code, which it
    /// doesn't list, unlocks flying until the game closes. (The code is checked by its hash, so it isn't written
    /// out in the source.)
    public class HubTerminal : MonoBehaviour
    {
        public TextMesh screen;
        public Material glow; public Color glowColor;

        static string flyHash;   // the flying code's hash (see Code)

        readonly List<string> lines = new List<string>();
        string typing = ""; bool active; float caret; int lockedFrames;
        SimplePlayerController player; PlayerInteractor hands;

        void Start()
        {
            Print("EVERYTHING WORLD TERMINAL  v1.0");
            Print("Type HELP and press Enter.");
            Show();
        }

        static string Hash(string s)
        {
            using (var sha = SHA256.Create())
            {
                var b = sha.ComputeHash(Encoding.UTF8.GetBytes(s.Trim().ToUpperInvariant())); var sb = new StringBuilder();
                foreach (var x in b) sb.Append(x.ToString("x2")); return sb.ToString();
            }
        }

        /// Sits you down at the keyboard.
        public void Begin(PlayerInteractor who)
        {
            if (active) return;
            player = who != null ? who.GetComponent<SimplePlayerController>() : null; hands = who;
            if (player != null) player.enabled = false;
            if (hands != null) hands.enabled = false;
            var kb = Keyboard.current; if (kb != null) kb.onTextInput += OnChar;
            active = true; lockedFrames = 2; typing = "";
            Print("> (you sit down at the terminal)"); Show();
        }

        void End()
        {
            active = false; var kb = Keyboard.current; if (kb != null) kb.onTextInput -= OnChar;
            StartCoroutine(GiveBack());
            Show();
        }

        System.Collections.IEnumerator GiveBack()
        {
            yield return null;   // a frame later, so the Esc that stood you up doesn't also free the cursor
            if (player != null) player.enabled = true;
            if (hands != null) hands.enabled = true;
        }

        void OnChar(char c)
        {
            if (!active || lockedFrames > 0) return;
            if (c == '\b' || c == '\r' || c == '\n' || c == 27) return;   // handled as keys
            if (char.IsControl(c) || typing.Length >= 40) return;
            typing += c; Show();
        }

        void Update()
        {
            if (glow != null) glow.SetColor("_EmissionColor", glowColor * (1f + Mathf.Sin(Time.time * 3f) * .05f + (active ? .3f : 0f)));
            caret += Time.deltaTime;
            if (!active) { if (caret > .5f) { caret = 0f; Show(); } return; }
            if (lockedFrames > 0) { lockedFrames--; return; }
            var kb = Keyboard.current; if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame) { Print("> (you stand up)"); End(); return; }
            if (kb.backspaceKey.wasPressedThisFrame && typing.Length > 0) { typing = typing.Substring(0, typing.Length - 1); Show(); }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { var cmd = typing; typing = ""; Print("> " + cmd); Run(cmd); Show(); }
            if (caret > .5f) { caret = 0f; Show(); }
        }

        void Run(string cmd)
        {
            var c = cmd.Trim().ToUpperInvariant();
            if (c.Length == 0) return;
            if (Hash(c) == Code) { SimplePlayerController.FlightUnlocked = true; Print("ACCESS GRANTED. FLIGHT UNLOCKED."); Print("Press F to fly (until the game closes)."); return; }
            switch (c)
            {
                case "HELP": Print("HELP    this list"); Print("SONG    what the hub is playing"); Print("LANDS   how big the world is"); Print("TIME    the time"); Print("LOCK    lock flight again"); Print("CLEAR   clear the screen"); Print("EXIT    stand up (or Esc)"); break;
                case "CLEAR": case "CLS": lines.Clear(); break;
                case "EXIT": case "QUIT": case "BYE": Print("> (you stand up)"); End(); break;
                case "TIME": Print(System.DateTime.Now.ToString("dddd d MMMM yyyy, HH:mm")); break;
                case "SONG":
                    var d = MusicDirector.Instance;
                    Print(d != null && d.CurrentSlot.IsValid ? "NOW: " + Clip(d.CurrentSlot.track.name + " by " + d.CurrentSlot.track.artist, 46) : "NOTHING ON AIR.");
                    break;
                case "LANDS":
                    var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null;
                    if (data != null) { Print($"{data.layout.islands.Length - 1} LANDS, {data.layout.artists.Length} ARTISTS,"); Print($"{data.layout.bridges.Length} CROSSINGS, {data.layout.towers.Length} TOWERS."); }
                    break;
                case "LOCK": SimplePlayerController.FlightUnlocked = false; Print("FLIGHT LOCKED."); break;
                case "FLY": case "FLIGHT": Print(SimplePlayerController.FlightUnlocked ? "FLIGHT IS UNLOCKED. PRESS F." : "FLIGHT IS LOCKED. ENTER THE CODE."); break;
                case "HELLO": case "HI": Print("HELLO. WELCOME TO THE HUB."); break;
                default: Print("UNKNOWN COMMAND. TYPE HELP."); break;
            }
        }

        static string Code => flyHash ??= Hash(Secret());
        // the code, kept out of plain sight: built from its letters' positions in the alphabet
        static string Secret() { var p = new[] { 6, 18, 5, 5, 2, 9, 18, 4 }; var sb = new StringBuilder(); foreach (var i in p) sb.Append((char)('A' + i - 1)); return sb.ToString(); }

        static string Clip(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";

        void Print(string s)
        {
            foreach (var part in SpeechBubble.Wrap(s, 30).Split('\n')) lines.Add(part);
            while (lines.Count > 11) lines.RemoveAt(0);
        }

        void Show()
        {
            if (screen == null) return;
            var sb = new StringBuilder(); foreach (var l in lines) sb.Append(l).Append('\n');
            sb.Append(active ? "> " + typing + (caret < .25f ? "_" : " ") : (caret < .25f ? "PRESS E TO TYPE_" : "PRESS E TO TYPE"));
            screen.text = sb.ToString();
        }
    }
}
