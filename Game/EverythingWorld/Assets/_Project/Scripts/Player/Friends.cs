using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EverythingWorld.World;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace EverythingWorld.Player
{
    /// Playing with friends. One of you hosts (Unity's Multiplayer Services make a session with a short join code,
    /// connected through Unity's Relay, so nobody sets up their router); the others type the code. Only a little is
    /// shared, because everything else already matches: the world is built the same on every computer, the radio
    /// follows a shared clock, and each person's music comes from their own Spotify for wherever they stand.
    /// What is shared: each player's position (their orb, at eye height), orb colour and name, ten times a second.
    /// Each player sends theirs to the host; the host sends everyone's to everyone. Other players appear as their
    /// orbs (lines, wisps, the beam to the ground) with their name floating above.
    public class Friends : MonoBehaviour
    {
        public static Friends Instance { get; private set; }

        public string Status { get; private set; } = "";
        public string Code => session?.Code;
        public bool InSession => session != null;
        public bool IsHost => session != null && session.IsHost;
        public int Count => session != null ? session.PlayerCount : 0;
        public bool Busy { get; private set; }

        public static string PlayerName
        {
            get { var n = PlayerPrefs.GetString("EW_PlayerName", ""); return string.IsNullOrWhiteSpace(n) ? "Friend" : n; }
            set => PlayerPrefs.SetString("EW_PlayerName", (value ?? "").Trim().Length > 20 ? value.Trim().Substring(0, 20) : (value ?? "").Trim());
        }

        const string MsgMine = "ew/me", MsgAll = "ew/all";
        const float SendEvery = .05f, Forget = 4f;
        // friends are drawn a moment in the past, gliding between positions they've already sent, so their path is
        // smooth however unevenly the messages arrive (through the host and the relay); without this they jolt and slingshot
        const float Delay = .2f;

        ISession session; NetworkManager net; bool handlers; float nextSend;
        SimplePlayerController walker; Sunglasses shades;

        class State { public Vector3 eye; public byte colour, shades; public string name; public float seen, stamp, yaw; }   // stamp: the sender's clock when they sent it
        readonly Dictionary<ulong, State> everyone = new Dictionary<ulong, State>();   // the host's table
        class Remote
        {
            public GameObject go; public PlayerOrb orb; public TextMesh label; public Vector3 shown; public float seen;
            public readonly List<(float t, Vector3 p)> path = new List<(float, Vector3)>();   // what they sent, oldest first, by their clock
            public float clock; public bool started;                                          // where on their clock we're showing them
        }
        readonly Dictionary<ulong, Remote> remotes = new Dictionary<ulong, Remote>();

        void Awake() { Instance = this; walker = GetComponent<SimplePlayerController>(); }

        /// Everyone else in the game: name, orb colour and where their eyes are (for the paper map).
        public List<(string name, Color color, Vector3 eye)> Others()
        {
            var list = new List<(string, Color, Vector3)>();
            foreach (var r in remotes.Values) if (r.go != null) list.Add((r.label.text, r.orb.Color, r.shown));
            return list;
        }

        // ---------------------------------------------------------------- seeing each other
        GUIStyle tagStyle; static Texture2D dot, rim;
        /// A marker for each friend you can't plainly see (behind a wall, far off, or behind you): their colour as a
        /// glowing dot, their name and how far away they are. Off the screen, the marker waits at the edge, on the
        /// side they're on. Close by and in plain view it goes away (their orb and name are enough).
        void OnGUI()
        {
            if (remotes.Count == 0 || MainMenu.Showing || Event.current.type != EventType.Repaint) return;
            var cam = Camera.main; if (cam == null) return;
            tagStyle ??= new GUIStyle(GameFonts.Box(15, TextAnchor.MiddleCenter)) { padding = new RectOffset(8, 8, 3, 3) };
            if (dot == null) { dot = Dot(false); rim = Dot(true); }
            var from = cam.transform.position; var keep = GUI.color;
            foreach (var r in remotes.Values)
            {
                if (r.go == null) continue;
                var p = r.shown; var to = p - from; var dist = to.magnitude;
                var inView = Vector3.Dot(cam.transform.forward, to) > 0f;
                var blocked = dist > .6f && Physics.Linecast(from, p - to / dist * .5f, ~0, QueryTriggerInteraction.Ignore);
                if (inView && !blocked && dist < 25f) continue;   // plainly there
                var sp = cam.WorldToScreenPoint(p + Vector3.up * .25f);
                var at = new Vector2(sp.x, Screen.height - sp.y);
                var centre = new Vector2(Screen.width / 2f, Screen.height / 2f);
                const float edge = 46f;
                var onScreen = sp.z > 0f && at.x > edge && at.x < Screen.width - edge && at.y > edge && at.y < Screen.height - edge;
                if (!onScreen)
                {
                    // to the edge, on their side (behind you: the opposite way, so it still points the right way round)
                    var d = at - centre; if (sp.z < 0f) d = -d; if (d.sqrMagnitude < 1f) d = Vector2.down;
                    var sx = (Screen.width / 2f - edge) / Mathf.Max(.001f, Mathf.Abs(d.x)); var sy = (Screen.height / 2f - edge) / Mathf.Max(.001f, Mathf.Abs(d.y));
                    at = centre + d * Mathf.Min(sx, sy);
                }
                var c = r.orb.Color; var size = onScreen ? 22f : 18f;
                var box = new Rect(at.x - size / 2f, at.y - size / 2f, size, size);
                GUI.color = new Color(c.r, c.g, c.b, .95f); GUI.DrawTexture(box, dot); GUI.color = Color.white; GUI.DrawTexture(box, rim);
                GUI.color = Color.white;
                var text = $"{r.label.text}{(r.orb.shades ? "  \u00b7  shades on" : "")}  \u00b7  {(dist < 1000f ? $"{dist:0} m" : $"{dist / 1000f:0.0} km")}";
                var tw = tagStyle.CalcSize(new GUIContent(text)).x;
                var lx = Mathf.Clamp(at.x - tw / 2f, 6f, Screen.width - tw - 6f); var ly = at.y + size / 2f + 3f; if (ly > Screen.height - 34f) ly = at.y - size / 2f - 30f;
                GUI.Label(new Rect(lx, ly, tw, 26f), text, tagStyle);
            }
            GUI.color = keep;
        }

        /// A soft round marker: ring = a thin circle (drawn white, so it shows on any colour), else a filled disc with a glow (drawn in the friend's colour).
        static Texture2D Dot(bool ring)
        {
            const int n = 48; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Friend marker", filterMode = FilterMode.Bilinear };
            for (var j = 0; j < n; j++)
                for (var i = 0; i < n; i++)
                {
                    var d = new Vector2(i + .5f - n / 2f, j + .5f - n / 2f).magnitude / (n / 2f);
                    var a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - .74f) * 12f) : Mathf.Max(Mathf.Clamp01((.62f - d) * 20f), Mathf.Clamp01(1f - d) * .35f);
                    t.SetPixel(i, j, new Color(1f, 1f, 1f, a));
                }
            t.Apply(); return t;
        }

        // ---------------------------------------------------------------- joining up
        async Task<bool> SignIn()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    // a fresh profile each time the game starts, so two copies on one computer count as two players
                    var options = new InitializationOptions().SetProfile("p" + Guid.NewGuid().ToString("N").Substring(0, 12));
                    await UnityServices.InitializeAsync(options);
                }
                if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return true;
            }
            catch (Exception e) { Status = "Couldn't reach Unity's online services: " + Short(e); Debug.LogWarning("Friends: " + e); return false; }
        }

        void EnsureNetwork()
        {
            if (net != null) return;
            var go = new GameObject("Network"); DontDestroyOnLoad(go);
            var transport = go.AddComponent<UnityTransport>();
            net = go.AddComponent<NetworkManager>();
            net.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false, ConnectionApproval = false };
            net.OnClientDisconnectCallback += id => { if (net != null && id == net.LocalClientId && !net.IsServer) Lost("The host left the game."); };
        }

        public async void Host()
        {
            if (Busy || InSession) return; Busy = true; Status = "Starting a game…";
            try
            {
                if (!await SignIn()) return;
                EnsureNetwork();
                var options = new SessionOptions { MaxPlayers = 10 }.WithRelayNetwork();
                session = await MultiplayerService.Instance.CreateSessionAsync(options);
                Watch(); Status = "You're hosting. Share the code with friends.";
            }
            catch (Exception e) { session = null; Status = Explain(e); Debug.LogWarning("Friends: " + e); }
            finally { Busy = false; }
        }

        public async void Join(string code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (Busy || InSession || code.Length < 4) { if (code.Length < 4) Status = "Type the code your friend gave you."; return; }
            Busy = true; Status = "Joining " + code + "…";
            try
            {
                if (!await SignIn()) return;
                EnsureNetwork();
                session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
                Watch(); Status = "You're in " + code + ".";
            }
            catch (Exception e) { session = null; Status = Explain(e); Debug.LogWarning("Friends: " + e); }
            finally { Busy = false; }
        }

        public async void Leave()
        {
            if (session == null) return; var s = session; session = null; handlers = false; ClearRemotes(); everyone.Clear();
            Status = "You left the game.";
            try { await s.LeaveAsync(); } catch (Exception e) { Debug.LogWarning("Friends: " + e); }
        }

        void Watch()
        {
            session.RemovedFromSession += () => Lost("The game ended.");
            session.Deleted += () => Lost("The game ended.");
        }

        void Lost(string why) { session = null; handlers = false; ClearRemotes(); everyone.Clear(); Status = why; }

        static string Short(Exception e) { var m = e.Message; return m.Length > 120 ? m.Substring(0, 120) + "…" : m; }

        static string Explain(Exception e)
        {
            var m = e.Message ?? "";
            if (m.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("404", StringComparison.Ordinal) >= 0) return "No game with that code. Check it with your friend.";
            if (m.IndexOf("relay", StringComparison.OrdinalIgnoreCase) >= 0 || m.IndexOf("403", StringComparison.Ordinal) >= 0 || m.IndexOf("forbidden", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Unity's online services said no. Relay may need switching on for this project in the Unity Cloud dashboard.";
            if (m.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0) return "That game is full.";
            return "Something went wrong: " + Short(e);
        }

        // ---------------------------------------------------------------- sharing where everyone is
        void Update()
        {
            if (net == null || !net.IsListening || session == null) { if (remotes.Count > 0 && session == null) ClearRemotes(); return; }
            if (!handlers && net.CustomMessagingManager != null)
            {
                handlers = true;
                net.CustomMessagingManager.RegisterNamedMessageHandler(MsgMine, OnMine);
                net.CustomMessagingManager.RegisterNamedMessageHandler(MsgAll, OnAll);
            }
            if (Time.time >= nextSend)
            {
                nextSend = Time.time + SendEvery;
                var me = MyState();
                if (net.IsServer)
                {
                    me.seen = Time.time; everyone[net.LocalClientId] = me;
                    // forget anyone gone quiet, then send the whole table to everyone
                    var gone = new List<ulong>(); foreach (var kv in everyone) if (Time.time - kv.Value.seen > Forget) gone.Add(kv.Key); foreach (var g in gone) everyone.Remove(g);
                    using (var w = new FastBufferWriter(64 + everyone.Count * 80, Allocator.Temp, 4096))
                    {
                        w.WriteValueSafe(everyone.Count);
                        foreach (var kv in everyone) { w.WriteValueSafe(kv.Key); w.WriteValueSafe(kv.Value.eye); w.WriteValueSafe(kv.Value.stamp); w.WriteValueSafe(kv.Value.yaw); w.WriteValueSafe(kv.Value.shades); w.WriteValueSafe(kv.Value.colour); w.WriteValueSafe(kv.Value.name); }
                        net.CustomMessagingManager.SendNamedMessageToAll(MsgAll, w, NetworkDelivery.UnreliableSequenced);
                    }
                    foreach (var kv in everyone) if (kv.Key != net.LocalClientId) Show(kv.Key, kv.Value);
                }
                else
                {
                    using (var w = new FastBufferWriter(96, Allocator.Temp, 512))
                    {
                        w.WriteValueSafe(me.eye); w.WriteValueSafe(me.stamp); w.WriteValueSafe(me.yaw); w.WriteValueSafe(me.shades); w.WriteValueSafe(me.colour); w.WriteValueSafe(me.name);
                        net.CustomMessagingManager.SendNamedMessage(MsgMine, NetworkManager.ServerClientId, w, NetworkDelivery.UnreliableSequenced);
                    }
                }
            }
            // glide each orb along the path it sent, a moment behind; forget the silent ones
            var stale = new List<ulong>();
            foreach (var kv in remotes)
            {
                var r = kv.Value; if (Time.time - r.seen > Forget) { stale.Add(kv.Key); continue; }
                r.shown = Glide(r); r.orb.Place(r.shown); r.label.transform.position = r.shown + Vector3.up * .55f;
            }
            foreach (var k in stale) { Destroy(remotes[k].go); remotes.Remove(k); }
        }

        State MyState()
        {
            var eye = walker != null && walker.cameraPivot != null ? walker.cameraPivot.position : transform.position + Vector3.up * .7f;
            var look = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            if (shades == null) shades = GetComponent<Sunglasses>();
            return new State { eye = eye, stamp = Time.realtimeSinceStartup, yaw = Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg, shades = (byte)(shades != null && shades.On ? 1 : 0),
                               colour = (byte)(PlayerOrb.Local != null ? PlayerOrb.Local.Index : 0), name = PlayerName };
        }

        /// Where to draw a friend now: their clock runs alongside ours, Delay behind the newest thing they sent, and
        /// we draw them between the two positions either side of it. If messages stop for a moment they carry on
        /// briefly the way they were going, then wait; a big jump (a tower, H) is a jump, not a slingshot.
        static Vector3 Glide(Remote r)
        {
            var path = r.path; if (path.Count == 0) return r.shown;
            var newest = path[path.Count - 1].t; var want = newest - Delay;
            if (!r.started || Mathf.Abs(r.clock - want) > 1f) { r.clock = want; r.started = true; }
            else r.clock += Time.unscaledDeltaTime + (want - r.clock) * Mathf.Min(1f, Time.unscaledDeltaTime * 1.5f);   // keep pace, ease out drift
            while (path.Count > 2 && path[1].t <= r.clock) path.RemoveAt(0);   // drop what's behind us
            var a = path[0];
            if (r.clock <= a.t || path.Count == 1) return a.p;
            var b = path[1];
            if (r.clock <= b.t)
            {
                if ((b.p - a.p).sqrMagnitude > 30f * 30f) return b.p;   // they jumped
                return Vector3.Lerp(a.p, b.p, (r.clock - a.t) / Mathf.Max(.0001f, b.t - a.t));
            }
            // past the newest: keep going the same way for up to a quarter of a second, then hold
            var v = (b.p - a.p) / Mathf.Max(.0001f, b.t - a.t);
            return v.sqrMagnitude > 900f ? b.p : b.p + v * Mathf.Min(r.clock - b.t, .25f);
        }

        void OnMine(ulong sender, FastBufferReader r)
        {
            r.ReadValueSafe(out Vector3 eye); r.ReadValueSafe(out float stamp); r.ReadValueSafe(out float yaw); r.ReadValueSafe(out byte shades); r.ReadValueSafe(out byte colour); r.ReadValueSafe(out string name);
            everyone[sender] = new State { eye = eye, stamp = stamp, yaw = yaw, shades = shades, colour = colour, name = name, seen = Time.time };
        }

        void OnAll(ulong sender, FastBufferReader r)
        {
            if (net.IsServer) return;   // the host already knows
            r.ReadValueSafe(out int n);
            for (var i = 0; i < n && i < 32; i++)
            {
                r.ReadValueSafe(out ulong id); r.ReadValueSafe(out Vector3 eye); r.ReadValueSafe(out float stamp); r.ReadValueSafe(out float yaw); r.ReadValueSafe(out byte shades); r.ReadValueSafe(out byte colour); r.ReadValueSafe(out string name);
                if (id == net.LocalClientId) continue;
                Show(id, new State { eye = eye, stamp = stamp, yaw = yaw, shades = shades, colour = colour, name = name });
            }
        }

        void Show(ulong id, State s)
        {
            if (!remotes.TryGetValue(id, out var r))
            {
                var go = new GameObject("Friend " + id); go.SetActive(false);
                var orb = go.AddComponent<PlayerOrb>(); orb.isLocal = false; go.SetActive(true);
                var label = ArtistBits.Plain(ArtistBits.Label(go.transform, "", s.eye + Vector3.up * .55f, Vector3.forward, .2f, new Color(1f, .97f, .9f), TextAnchor.LowerCenter));   // big enough to read from a way off
                label.gameObject.AddComponent<Billboard>();
                r = new Remote { go = go, orb = orb, label = label, shown = s.eye }; remotes[id] = r;
            }
            // only new positions go on the path (the host passes the table on every time, sometimes with nothing new)
            var path = r.path;
            if (path.Count > 0 && s.stamp < path[path.Count - 1].t - 5f) path.Clear();   // their clock restarted
            if (path.Count == 0 || s.stamp > path[path.Count - 1].t) { path.Add((s.stamp, s.eye)); if (path.Count > 40) path.RemoveAt(0); r.seen = Time.time; }
            if (r.orb.Index != s.colour) r.orb.Choose(s.colour);
            r.orb.facing = s.yaw; r.orb.shades = s.shades != 0;
            if (r.label.text != s.name) r.label.text = s.name;
        }

        void ClearRemotes() { foreach (var r in remotes.Values) if (r.go != null) Destroy(r.go); remotes.Clear(); }

        void OnApplicationQuit() { if (session != null) { try { session.LeaveAsync(); } catch { } } }
    }
}
