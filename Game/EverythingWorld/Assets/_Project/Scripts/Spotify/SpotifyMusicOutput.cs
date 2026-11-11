using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EverythingWorld.Music;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace EverythingWorld.Spotify
{
    /// Plays tracks on the player's own Spotify app through the Spotify Web API (Spotify Connect).
    /// Sound comes from Spotify, not Unity. Needs Spotify Premium and Spotify open on some device.
    /// Press L in play mode to log in (opens your browser once; the login is remembered).
    ///
    /// Zone radio: songs start at the station's current position, and about two seconds later we check
    /// where Spotify really is and jump ("seek") if it's more than a second off, so friends stay in sync.
    public class SpotifyMusicOutput : MonoBehaviour, IMusicOutput
    {
        const string Api = "https://api.spotify.com/v1";
        const int MaxDriftMs = 1000;

        public string Status { get; private set; } = "Starting";
        /// How long the last "play" request took, from sending it to Spotify accepting it.
        public float LastRequestMs { get; private set; } = -1f;
        /// How far Spotify was from the station's position at the last check (positive = Spotify ahead).
        public int LastDriftMs { get; private set; }
        public bool HasDrift { get; private set; }
        public string DeviceName { get; private set; }

        SpotifyConfig config;
        SpotifyAuth auth;
        CancellationTokenSource loginCancel;
        string deviceId;
        PlayRequest pending;
        bool busy;
        TrackInfo lastStarted; // the song we last told Spotify to play

        /// One play command: the track and where the station is in it at a given moment.
        class PlayRequest
        {
            public TrackInfo track;
            public int positionMs;
            public float requestedAt; // Time.realtimeSinceStartup when positionMs was correct
            public bool continuation;

            /// The station's position in this track right now.
            public int PositionNow => positionMs + (int)((Time.realtimeSinceStartup - requestedAt) * 1000f);
        }

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable] class Device { public string id; public string name; public string type; public bool is_active; public bool is_restricted; }
        [Serializable] class DeviceList { public Device[] devices; }
        [Serializable] class Item { public string name; public string uri; public int duration_ms; }
        [Serializable] class PlayerState { public Device device; public bool is_playing; public int progress_ms; public Item item; }
        [Serializable] class Restrictions { public string reason; }
        [Serializable] class Album { public string uri; }
        [Serializable] class TrackCheck { public bool is_playable; public Restrictions restrictions; public Album album; }
#pragma warning restore 0649
        [Serializable] class PlayBody { public string[] uris; public int position_ms; }
        [Serializable] class TransferBody { public string[] device_ids; public bool play; }
        [Serializable] class Offset { public string uri; }
        [Serializable] class ContextBody { public string context_uri; public Offset offset; public int position_ms; }

        /// Ways to start a song. Starting it from inside its album is the default: on our test PC the
        /// desktop app ignored plain "play this song" requests but always obeyed the album version.
        enum StartMethod { FromAlbum, PlainPlay, TransferThenPlay }

        StartMethod method = StartMethod.FromAlbum;
        int playGeneration;
        readonly Dictionary<string, TrackCheck> trackCache = new Dictionary<string, TrackCheck>();

        void Awake()
        {
            config = SpotifyConfig.Load(out var problem);
            if (config == null) { Status = problem; return; }
            auth = new SpotifyAuth(config);
            Status = auth.IsLoggedIn ? "Logged in" : "Not logged in: press L";
        }

        void OnDestroy() => loginCancel?.Cancel();

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame) Login();
        }

        public async void Login()
        {
            if (auth == null || loginCancel != null) return;
            loginCancel = new CancellationTokenSource();
            Status = "Waiting for you to allow access in the browser...";
            try
            {
                await auth.LoginAsync(loginCancel.Token);
                Status = "Logged in";
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Status = "Login failed: " + e.Message;
                Debug.LogWarning("Spotify: " + e);
            }
            finally
            {
                loginCancel = null;
            }
        }

        /// Requests made while another is still running are merged: only the newest one gets played.
        public async void Play(TrackInfo track, SceneInfo scene, int positionMs, bool continuation)
        {
            pending = new PlayRequest { track = track, positionMs = positionMs, requestedAt = Time.realtimeSinceStartup, continuation = continuation };
            if (busy) return;
            busy = true;
            try
            {
                while (pending != null)
                {
                    var next = pending;
                    pending = null;
                    await PlayNow(next);
                }
            }
            catch (Exception e)
            {
                Status = "Error: " + e.Message;
                Debug.LogWarning("Spotify: " + e);
            }
            finally
            {
                busy = false;
            }
        }

        async Task PlayNow(PlayRequest req)
        {
            if (auth == null) return; // Status already explains the config problem
            if (!auth.IsLoggedIn) { Status = "Not logged in: press L"; return; }
            if (req.continuation && !await ShouldContinue()) return;

            var generation = ++playGeneration;
            var used = method;
            var watch = Stopwatch.StartNew();

            if (used == StartMethod.TransferThenPlay && !await TransferToDevice()) return;
            if (used == StartMethod.FromAlbum) await LookupTrack(req.track); // album needed for the request
            string Body() => BuildBody(used, req.track, req.PositionNow); // position is recomputed for every retry

            var (code, text, retryAfter) = await Send("PUT", PlayUrl(), Body());

            if (code == 401) // access token expired early: refresh once and retry
            {
                await auth.GetAccessTokenAsync(forceRefresh: true);
                (code, text, retryAfter) = await Send("PUT", PlayUrl(), Body());
            }
            if (code == 404) // no active device (or the remembered one went away): pick one and retry
            {
                if (!await ChooseDevice()) return;
                (code, text, retryAfter) = await Send("PUT", PlayUrl(), Body());
            }
            if (code == 429) // too many requests: wait as long as Spotify asks, then retry once
            {
                Status = $"Spotify asked us to slow down; waiting {retryAfter}s";
                await Task.Delay(TimeSpan.FromSeconds(Mathf.Clamp(retryAfter, 1, 30)));
                (code, text, _) = await Send("PUT", PlayUrl(), Body());
            }

            watch.Stop();
            if (code >= 200 && code < 300)
            {
                LastRequestMs = (float)watch.Elapsed.TotalMilliseconds;
                lastStarted = req.track;
                Status = $"Playing on {DeviceName ?? "your active Spotify device"}";
                Debug.Log($"[Spotify] Playing {req.track} from {FormatTime(req.positionMs)} via {Describe(used)} ({LastRequestMs:0} ms)");
                CheckPlaybackLater(req, generation, used);
            }
            else if (code == 403)
            {
                Status = "Spotify refused (403): needs Premium, and your account must be on the app's user list";
                Debug.LogWarning($"Spotify 403: {text}");
            }
            else
            {
                Status = $"Play failed ({code})";
                Debug.LogWarning($"Spotify play failed ({code}): {text}");
            }
        }

        /// When the station moves to its next song, only follow along if you're still listening to it:
        /// not if you paused Spotify, and not if you picked a different song yourself.
        async Task<bool> ShouldContinue()
        {
            if (lastStarted == null) return true;
            var state = await GetPlayerState();
            if (state == null || !state.is_playing)
            {
                Status = "Spotify is paused: the zone radio waits (walk into a zone to tune in again)";
                return false;
            }
            // A different song only a few seconds in is just the album moving on by itself; anything else is your choice.
            if (!IsSameSong(state, lastStarted) && state.progress_ms > 5000)
            {
                Status = $"You're playing {state.item?.name ?? "something else"}: walk into a zone to tune back in";
                Debug.Log($"[Spotify] Not switching songs: you picked {state.item?.name}");
                return false;
            }
            return true;
        }

        /// Spotify accepting the command doesn't guarantee the song starts: on some setups the desktop app
        /// stops the old song and never loads the new one. So two seconds later we ask Spotify what it's really
        /// doing. If the song is running, we correct its position if needed. If not, we try the other ways of
        /// starting it, and remember the one that works.
        async void CheckPlaybackLater(PlayRequest req, int generation, StartMethod used)
        {
            try
            {
                var state = await StartedAfter(req.track, generation, used);
                if (state != null)
                {
                    state = await FixVersion(req, state, generation, used);
                    await SyncPosition(req, state, generation);
                    return;
                }
                if (generation != playGeneration) return;

                // Is the track itself playable for this account/region?
                var info = await LookupTrack(req.track);
                if (info != null && !info.is_playable)
                {
                    Status = $"Song didn't start: not playable for you ({info.restrictions?.reason ?? "no reason given"})";
                    Debug.LogWarning($"[Spotify check] {req.track}: {Status}");
                    return;
                }

                foreach (StartMethod other in Enum.GetValues(typeof(StartMethod)))
                {
                    if (other == used) continue;
                    Status = $"Song didn't start; trying {Describe(other)}";
                    if (other == StartMethod.TransferThenPlay && !await TransferToDevice()) return;
                    if (generation != playGeneration) return;
                    await Send("PUT", PlayUrl(), BuildBody(other, req.track, req.PositionNow));
                    state = await StartedAfter(req.track, generation, other);
                    if (state != null)
                    {
                        method = other;
                        Debug.Log($"[Spotify] Switching to {Describe(other)} for the next songs");
                        await SyncPosition(req, state, generation);
                        return;
                    }
                    if (generation != playGeneration) return;
                }

                Status = "Song didn't start with any method. Try closing and reopening the Spotify app.";
                Debug.LogWarning($"[Spotify check] {req.track}: no method worked.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Spotify check] failed: " + e.Message);
            }
        }

        /// Starting a song from its album can land on a different version of it: Spotify swaps in the copy it offers
        /// in your country, and that album may hold a shorter edit (How Soon Is Now? played as ~3:40 instead of the
        /// station's 6:48). If Spotify's song is more than 15 s longer or shorter than the station's, ask for the exact
        /// track instead. If that doesn't take, go back to the album version so something still plays.
        async Task<PlayerState> FixVersion(PlayRequest req, PlayerState state, int generation, StartMethod used)
        {
            var want = req.track.duration_ms; var got = state.item?.duration_ms ?? 0;
            if (want <= 0 || got <= 0 || Math.Abs(got - want) <= 15000 || used == StartMethod.PlainPlay) return state;
            Debug.LogWarning($"[Spotify version] {req.track}: Spotify is playing a {FormatTime(got)} version, the station's is {FormatTime(want)}. Asking for the exact track.");
            if (generation != playGeneration) return state;
            await Send("PUT", PlayUrl(), BuildBody(StartMethod.PlainPlay, req.track, req.PositionNow));
            var exact = await StartedAfter(req.track, generation, StartMethod.PlainPlay);
            if (exact != null && Math.Abs((exact.item?.duration_ms ?? 0) - want) <= 15000)
            {
                Debug.Log($"[Spotify version] {req.track}: now playing the right version ({FormatTime(exact.item.duration_ms)})");
                return exact;
            }
            if (generation != playGeneration) return state;
            Debug.LogWarning($"[Spotify version] {req.track}: couldn't get the {FormatTime(want)} version; staying with the album's.");
            await Send("PUT", PlayUrl(), BuildBody(used, req.track, req.PositionNow));
            return await StartedAfter(req.track, generation, used) ?? state;
        }

        /// Compares Spotify's position with the station's and jumps if they're more than a second apart.
        async Task SyncPosition(PlayRequest req, PlayerState state, int generation)
        {
            var expected = req.PositionNow;
            LastDriftMs = state.progress_ms - expected;
            HasDrift = true;
            if (Math.Abs(LastDriftMs) <= MaxDriftMs)
            {
                Debug.Log($"[Spotify sync] {req.track}: {LastDriftMs:+0;-0} ms from the station, in sync");
                return;
            }
            if (generation != playGeneration) return;

            var target = req.PositionNow;
            var url = $"{Api}/me/player/seek?position_ms={target}" + (deviceId != null ? $"&device_id={deviceId}" : "");
            var (code, _, _) = await Send("PUT", url, null);
            Debug.Log($"[Spotify sync] {req.track}: {LastDriftMs:+0;-0} ms off, jumped to {FormatTime(target)} (HTTP {code})");
            if (code >= 200 && code < 300) Status += ", re-synced";
        }

        /// Waits two seconds, then asks Spotify whether this track is really playing. Logs what it finds.
        /// Returns Spotify's state if the song is running, otherwise null.
        async Task<PlayerState> StartedAfter(TrackInfo track, int generation, StartMethod how)
        {
            await Task.Delay(2000);
            if (this == null || generation != playGeneration) return null; // left play mode, or a newer song took over

            var state = await GetPlayerState();
            var onDevice = state?.device != null ? $"{state.device.name} ({state.device.type})" : "no device";
            var started = state != null && state.is_playing && IsSameSong(state, track);
            Debug.Log($"[Spotify check] {track} via {Describe(how)}: started={started}, playing={state?.is_playing}, " +
                      $"progress={state?.progress_ms} ms, item={state?.item?.name ?? "none"}, device={onDevice}");
            if (started) Status = $"Playing on {onDevice} (confirmed)";
            return started ? state : null;
        }

        async Task<PlayerState> GetPlayerState()
        {
            var (code, text, _) = await Send("GET", $"{Api}/me/player", null);
            return code == 200 && !string.IsNullOrEmpty(text) ? JsonUtility.FromJson<PlayerState>(text) : null;
        }

        /// The request body. FromAlbum needs the album, so call LookupTrack first (it's cached).
        string BuildBody(StartMethod how, TrackInfo track, int positionMs)
        {
            if (how == StartMethod.FromAlbum && trackCache.TryGetValue(track.id, out var info) && info.album?.uri != null)
                return JsonUtility.ToJson(new ContextBody { context_uri = info.album.uri, offset = new Offset { uri = track.Uri }, position_ms = positionMs });
            return JsonUtility.ToJson(new PlayBody { uris = new[] { track.Uri }, position_ms = positionMs });
        }

        /// Track details (album, playability), looked up once per track and cached.
        async Task<TrackCheck> LookupTrack(TrackInfo track)
        {
            if (trackCache.TryGetValue(track.id, out var cached)) return cached;
            var (code, text, _) = await Send("GET", $"{Api}/tracks/{track.id}?market=from_token", null);
            var info = code == 200 ? JsonUtility.FromJson<TrackCheck>(text) : null;
            if (info != null) trackCache[track.id] = info;
            return info;
        }

        async Task<bool> TransferToDevice()
        {
            if (deviceId == null && !await ChooseDevice()) return false;
            await Send("PUT", $"{Api}/me/player", JsonUtility.ToJson(new TransferBody { device_ids = new[] { deviceId }, play = false }));
            await Task.Delay(500);
            return true;
        }

        static string Describe(StartMethod how) => how switch
        {
            StartMethod.FromAlbum => "starting from its album",
            StartMethod.PlainPlay => "plain play",
            _ => "handing playback to the device first",
        };

        static string FormatTime(int ms) => $"{ms / 60000}:{ms / 1000 % 60:00}";

        /// Compares by name too: the album copy of a song can have a different track ID than the playlist copy,
        /// and a slightly different name ("Girls, Girls, Girls (Uncensored)", "Tusk - 2002 Remaster").
        static bool IsSameSong(PlayerState state, TrackInfo track)
        {
            if (state?.item == null || track == null) return false;
            if (state.item.uri == track.Uri) return true;
            string a = BaseName(state.item.name), b = BaseName(track.name);
            return a.Length > 0 && b.Length > 0 && (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal));
        }

        /// A song name without extras in brackets or after " - ", lower case.
        static string BaseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var cut = name.Length;
            foreach (var mark in new[] { " (", " [", " - " })
            {
                var i = name.IndexOf(mark, StringComparison.Ordinal);
                if (i > 0 && i < cut) cut = i;
            }
            return name.Substring(0, cut).Trim().ToLowerInvariant();
        }

        string PlayUrl() => deviceId == null ? $"{Api}/me/player/play" : $"{Api}/me/player/play?device_id={deviceId}";

        /// Picks a device: the active one, else this computer, else the first available.
        async Task<bool> ChooseDevice()
        {
            var (code, text, _) = await Send("GET", $"{Api}/me/player/devices", null);
            if (code != 200) { Status = $"Couldn't list Spotify devices ({code})"; return false; }

            var list = JsonUtility.FromJson<DeviceList>(text);
            Device best = null;
            foreach (var d in list.devices ?? Array.Empty<Device>())
            {
                if (d.is_restricted) continue;
                if (best == null || d.is_active && !best.is_active ||
                    !best.is_active && d.type == "Computer" && best.type != "Computer")
                    best = d;
            }
            if (best == null)
            {
                deviceId = null;
                DeviceName = null;
                Status = "No Spotify device found: open the Spotify app on this PC (or phone), then walk into a zone again";
                return false;
            }
            deviceId = best.id;
            DeviceName = best.name;
            return true;
        }

        async Task<(long code, string text, int retryAfter)> Send(string method, string url, string json)
        {
            var token = await auth.GetAccessTokenAsync();
            if (token == null) return (401, "not logged in", 0);

            using var req = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer() };
            if (json != null)
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.SetRequestHeader("Content-Type", "application/json");
            }
            req.SetRequestHeader("Authorization", "Bearer " + token);
            await req.SendWebRequest();

            int.TryParse(req.GetResponseHeader("Retry-After"), out var retryAfter);
            return (req.responseCode, req.downloadHandler.text, retryAfter);
        }
    }
}
