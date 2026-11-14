using EverythingWorld.Music;
using EverythingWorld.Spotify;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Player
{
    /// The title screen and pause menu. On Play you stand on the hub's plaza under the ring of covers, the view
    /// turning slowly, the hub's radio playing, while the world builds; a panel down the left has the title and
    /// Enter the World (once the world's ready), Tutorial and Join Friends (still to come), Settings and Quit.
    /// Enter puts you at the hub's tower door. Esc in the game brings it back as a pause menu (unless a passport,
    /// an about card or the terminal is open: Esc closes those). Settings: orb colour, mouse sensitivity,
    /// sunglasses, playing through Spotify and logging in to it.
    public class MainMenu : MonoBehaviour
    {
        /// Set by the photo tour, which drives the player itself.
        public static bool Skip;
        public static bool Showing => instance != null && instance.open;
        static MainMenu instance;

        enum Page { Main, Settings }
        bool open = true, entered; Page page = Page.Main; float yaw;
        SimplePlayerController walker; PlayerInteractor hands; Transform hub;

        void Awake()
        {
            instance = this;
            walker = GetComponent<SimplePlayerController>(); hands = GetComponent<PlayerInteractor>();
            if (walker != null) walker.mouseSensitivity = PlayerPrefs.GetFloat("EW_MouseSensitivity", walker.mouseSensitivity);
        }

        void Start()
        {
            if (Skip) { open = false; entered = true; return; }
            var sw = FindFirstObjectByType<MusicOutputSwitch>(); if (sw != null) sw.useSpotify = PlayerPrefs.GetInt("EW_UseSpotify", sw.useSpotify ? 1 : 0) == 1;
            Hold(true);
        }

        /// Stands you on the hub's plaza for the view (on the first frame, after everything else has placed you).
        void Stage()
        {
            staged = true;
            var w = IslandWorldBuilder.Instance;
            if (w != null) foreach (var t in w.Towers) if (t.GetComponent<TowerInfo>().sceneId == 1000) hub = t;
            if (hub != null) { var p = hub.position + new Vector3(-26f, 0, -16f); Put(p); yaw = Quaternion.LookRotation(hub.position - p).eulerAngles.y; }
        }
        bool staged;

        void Put(Vector3 p)
        {
            var w = IslandWorldBuilder.Instance; p.y = Mathf.Max(w.Data.HeightAt(p.x, p.z), w.Data.layout.waterLevel) + 1.2f;
            var cc = GetComponent<CharacterController>(); if (cc != null) cc.enabled = false; transform.position = p; if (cc != null) cc.enabled = true;
        }

        void Hold(bool on)
        {
            if (walker != null) walker.enabled = !on; if (hands != null) hands.enabled = !on;
            Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = on;
        }

        void Update()
        {
            if (Skip) return;
            if (!staged && !entered) Stage();
            if (open && !entered && walker != null)
            {
                // the slow turn under the ring
                yaw += Time.deltaTime * 2.5f; transform.rotation = Quaternion.Euler(0, yaw, 0);
                if (walker.cameraPivot != null) walker.cameraPivot.localEulerAngles = new Vector3(-16f, 0, 0);
            }
            var kb = Keyboard.current; if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (open) { if (entered) { if (page == Page.Settings) page = Page.Main; else Resume(); } return; }
            if (walker != null && !walker.enabled) return;                       // the terminal or the passport has the keys
            if (AboutCard.IsOpen || AboutCard.ClosedFrame == Time.frameCount) return;   // Esc is closing a card
            open = true; page = Page.Main; Hold(true);
        }

        void Resume() { open = false; StartCoroutine(GiveBack()); }

        System.Collections.IEnumerator GiveBack() { yield return null; Hold(false); }

        void Enter()
        {
            entered = true; open = false;
            if (hub != null) { Put(hub.position - Vector3.forward * 10f); transform.rotation = Quaternion.Euler(0, 180f, 0); }
            if (walker != null && walker.cameraPivot != null) walker.cameraPivot.localEulerAngles = Vector3.zero;
            StartCoroutine(GiveBack());
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- drawing
        GUIStyle title, sub, button, buttonOff, label, small, swatch; Texture2D panel, gold, faint, white;

        static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        void Styles()
        {
            if (title != null) return;
            panel = Solid(new Color(.04f, .035f, .05f, .82f)); gold = Solid(new Color(1f, .8f, .45f)); faint = Solid(new Color(1f, 1f, 1f, .14f)); white = Solid(Color.white);
            title = new GUIStyle { font = GameFonts.Sign, fontSize = 50, wordWrap = true, normal = { textColor = new Color(1f, .86f, .58f) } };
            sub = new GUIStyle { font = GameFonts.Speech, fontSize = 17, wordWrap = true, normal = { textColor = new Color(.86f, .84f, .8f) } };
            button = new GUIStyle { font = GameFonts.Sign, fontSize = 26, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 8, 4, 4), normal = { textColor = new Color(.95f, .93f, .9f) }, hover = { textColor = new Color(1f, .8f, .45f), background = Solid(new Color(1f, 1f, 1f, .06f)) } };
            buttonOff = new GUIStyle(button) { normal = { textColor = new Color(.5f, .48f, .46f) }, hover = { textColor = new Color(.5f, .48f, .46f) } };
            label = new GUIStyle(sub) { fontSize = 16 }; small = new GUIStyle(sub) { fontSize = 13, normal = { textColor = new Color(.62f, .6f, .58f) } };
            swatch = new GUIStyle();
        }

        void OnGUI()
        {
            if (!open || Skip) return;
            Styles(); GUI.depth = -50;
            var w = IslandWorldBuilder.Instance; var lay = w != null ? w.Data.layout : null;
            const float W = 470f; var left = new Rect(0, 0, W, Screen.height);
            GUI.DrawTexture(left, panel); GUI.DrawTexture(new Rect(W - 2f, 0, 2f, Screen.height), gold);
            float x = 44f, y = Screen.height * .12f;
            GUI.Label(new Rect(x, y, W - 70f, 130f), "THE EVERYTHING WORLD", title); y += 128f;
            GUI.Label(new Rect(x, y, W - 70f, 24f), "A world built from one shared playlist.", sub); y += 26f;
            if (lay != null) GUI.Label(new Rect(x, y, W - 70f, 24f), $"{lay.hubSongs:N0} songs  ·  {lay.hubArtists:N0} artists  ·  {lay.islands.Length - 1} lands", small);
            y += 54f;
            var ready = w != null && !w.Loading;
            if (page == Page.Main)
            {
                if (entered) { if (GUI.Button(new Rect(x - 16f, y, W - 60f, 44f), "Resume", button)) Resume(); y += 50f; }
                else
                {
                    if (GUI.Button(new Rect(x - 16f, y, W - 60f, 44f), "Enter the World", ready ? button : buttonOff) && ready) Enter();
                    y += 50f;
                }
                GUI.Button(new Rect(x - 16f, y, W - 60f, 44f), "Tutorial   (soon)", buttonOff); y += 50f;
                GUI.Button(new Rect(x - 16f, y, W - 60f, 44f), "Join Friends   (soon)", buttonOff); y += 50f;
                if (GUI.Button(new Rect(x - 16f, y, W - 60f, 44f), "Settings", button)) page = Page.Settings; y += 50f;
                if (GUI.Button(new Rect(x - 16f, y, W - 60f, 44f), "Quit", button)) Quit(); y += 50f;
            }
            else Settings(x, y, W);
            // the world building, at the bottom
            if (w != null && !ready)
            {
                var (done, total, name) = w.LoadProgress;
                var by = Screen.height - 74f;
                GUI.Label(new Rect(x, by, W - 70f, 22f), name != null ? $"Building {name}…" : "Building the world…", small);
                GUI.DrawTexture(new Rect(x, by + 26f, W - 90f, 5f), faint); GUI.DrawTexture(new Rect(x, by + 26f, (W - 90f) * (total > 0 ? done / (float)total : 0f), 5f), gold);
                GUI.Label(new Rect(x, by + 36f, W - 70f, 22f), $"{done} of {total} lands", small);
            }
            else if (!entered) GUI.Label(new Rect(x, Screen.height - 50f, W - 70f, 22f), "The world is ready.", small);
        }

        void Settings(float x, float y, float W)
        {
            GUI.Label(new Rect(x, y, W - 70f, 30f), "SETTINGS", new GUIStyle(sub) { font = GameFonts.Sign, fontSize = 22, normal = { textColor = new Color(1f, .86f, .58f) } }); y += 40f;
            // the orb's colour
            GUI.Label(new Rect(x, y, W - 70f, 22f), "Your light", label); y += 26f;
            var pal = PlayerOrb.Palette;
            for (var i = 0; i < pal.Length; i++)
            {
                var r = new Rect(x + (i % 6) * 62f, y + (i / 6) * 40f, 52f, 30f);
                var old = GUI.color; GUI.color = pal[i].color; GUI.DrawTexture(r, white); GUI.color = old;
                if (PlayerOrb.Local != null && PlayerOrb.Local.Color == pal[i].color) { GUI.DrawTexture(new Rect(r.x, r.yMax + 2f, r.width, 3f), gold); }
                if (GUI.Button(r, GUIContent.none, swatch) && PlayerOrb.Local != null) PlayerOrb.Local.Choose(i);
            }
            y += 90f;
            // mouse sensitivity
            if (walker != null)
            {
                GUI.Label(new Rect(x, y, W - 70f, 22f), $"Mouse sensitivity   {walker.mouseSensitivity * 10f:0.0}", label); y += 26f;
                var v = GUI.HorizontalSlider(new Rect(x, y, W - 100f, 20f), walker.mouseSensitivity, .02f, .4f);
                if (!Mathf.Approximately(v, walker.mouseSensitivity)) { walker.mouseSensitivity = v; PlayerPrefs.SetFloat("EW_MouseSensitivity", v); }
                y += 34f;
            }
            // sunglasses
            var shades = GetComponent<Sunglasses>();
            if (shades != null)
            {
                var on = GUI.Toggle(new Rect(x, y, W - 70f, 24f), shades.On, "  Sunglasses on   (G in the game)", label);
                if (on != shades.On) { shades.Wear(on); PlayerPrefs.SetInt("EW_Sunglasses", on ? 1 : 0); }
                y += 34f;
            }
            // Spotify
            var sw = FindFirstObjectByType<MusicOutputSwitch>(); var sp = FindFirstObjectByType<SpotifyMusicOutput>();
            if (sw != null)
            {
                var use = GUI.Toggle(new Rect(x, y, W - 70f, 24f), sw.useSpotify, "  Play the music through Spotify", label);
                if (use != sw.useSpotify) { sw.useSpotify = use; PlayerPrefs.SetInt("EW_UseSpotify", use ? 1 : 0); }
                y += 30f;
            }
            if (sp != null)
            {
                GUI.Label(new Rect(x, y, W - 70f, 40f), "Spotify: " + sp.Status, small); y += 40f;
                if (GUI.Button(new Rect(x - 16f, y, 240f, 36f), "Log in to Spotify", new GUIStyle(button) { fontSize = 20 })) sp.Login();
                y += 44f;
                GUI.Label(new Rect(x, y, W - 70f, 22f), "Volume: - and = in the game", small); y += 30f;
            }
            if (GUI.Button(new Rect(x - 16f, y + 10f, 200f, 40f), "Back", button)) page = Page.Main;
        }
    }
}
