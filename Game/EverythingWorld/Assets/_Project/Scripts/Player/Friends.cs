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
        const float SendEvery = .1f, Forget = 4f;

        ISession session; NetworkManager net; bool handlers; float nextSend;
        SimplePlayerController walker;

        class State { public Vector3 eye; public byte colour; public string name; public float seen; }
        readonly Dictionary<ulong, State> everyone = new Dictionary<ulong, State>();   // the host's table
        class Remote { public GameObject go; public PlayerOrb orb; public TextMesh label; public Vector3 target, shown; public float seen; }
        readonly Dictionary<ulong, Remote> remotes = new Dictionary<ulong, Remote>();

        void Awake() { Instance = this; walker = GetComponent<SimplePlayerController>(); }

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
                        foreach (var kv in everyone) { w.WriteValueSafe(kv.Key); w.WriteValueSafe(kv.Value.eye); w.WriteValueSafe(kv.Value.colour); w.WriteValueSafe(kv.Value.name); }
                        net.CustomMessagingManager.SendNamedMessageToAll(MsgAll, w, NetworkDelivery.UnreliableSequenced);
                    }
                    foreach (var kv in everyone) if (kv.Key != net.LocalClientId) Show(kv.Key, kv.Value);
                }
                else
                {
                    using (var w = new FastBufferWriter(96, Allocator.Temp, 512))
                    {
                        w.WriteValueSafe(me.eye); w.WriteValueSafe(me.colour); w.WriteValueSafe(me.name);
                        net.CustomMessagingManager.SendNamedMessage(MsgMine, NetworkManager.ServerClientId, w, NetworkDelivery.UnreliableSequenced);
                    }
                }
            }
            // drift each orb smoothly toward where it was last heard to be; forget the silent ones
            var stale = new List<ulong>();
            foreach (var kv in remotes)
            {
                var r = kv.Value; if (Time.time - r.seen > Forget) { stale.Add(kv.Key); continue; }
                r.shown = Vector3.Distance(r.shown, r.target) > 30f ? r.target : Vector3.Lerp(r.shown, r.target, 1f - Mathf.Exp(-10f * Time.deltaTime));
                r.orb.Place(r.shown); r.label.transform.position = r.shown + Vector3.up * .55f;
            }
            foreach (var k in stale) { Destroy(remotes[k].go); remotes.Remove(k); }
        }

        State MyState()
        {
            var eye = walker != null && walker.cameraPivot != null ? walker.cameraPivot.position : transform.position + Vector3.up * .7f;
            return new State { eye = eye, colour = (byte)(PlayerOrb.Local != null ? PlayerOrb.Local.Index : 0), name = PlayerName };
        }

        void OnMine(ulong sender, FastBufferReader r)
        {
            r.ReadValueSafe(out Vector3 eye); r.ReadValueSafe(out byte colour); r.ReadValueSafe(out string name);
            everyone[sender] = new State { eye = eye, colour = colour, name = name, seen = Time.time };
        }

        void OnAll(ulong sender, FastBufferReader r)
        {
            if (net.IsServer) return;   // the host already knows
            r.ReadValueSafe(out int n);
            for (var i = 0; i < n && i < 32; i++)
            {
                r.ReadValueSafe(out ulong id); r.ReadValueSafe(out Vector3 eye); r.ReadValueSafe(out byte colour); r.ReadValueSafe(out string name);
                if (id == net.LocalClientId) continue;
                Show(id, new State { eye = eye, colour = colour, name = name });
            }
        }

        void Show(ulong id, State s)
        {
            if (!remotes.TryGetValue(id, out var r))
            {
                var go = new GameObject("Friend " + id); go.SetActive(false);
                var orb = go.AddComponent<PlayerOrb>(); orb.isLocal = false; go.SetActive(true);
                var label = ArtistBits.Plain(ArtistBits.Label(go.transform, "", s.eye + Vector3.up * .55f, Vector3.forward, .13f, new Color(1f, .97f, .9f), TextAnchor.LowerCenter));
                label.gameObject.AddComponent<Billboard>();
                r = new Remote { go = go, orb = orb, label = label, shown = s.eye, target = s.eye }; remotes[id] = r;
            }
            r.target = s.eye; r.seen = Time.time;
            if (r.orb.Index != s.colour) r.orb.Choose(s.colour);
            if (r.label.text != s.name) r.label.text = s.name;
        }

        void ClearRemotes() { foreach (var r in remotes.Values) if (r.go != null) Destroy(r.go); remotes.Clear(); }

        void OnApplicationQuit() { if (session != null) { try { session.LeaveAsync(); } catch { } } }
    }
}
