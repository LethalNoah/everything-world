using System;
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
    public class SpotifyMusicOutput : MonoBehaviour, IMusicOutput
    {
        const string Api = "https://api.spotify.com/v1";

        public string Status { get; private set; } = "Starting";
        /// How long the last "play" request took, from sending it to Spotify accepting it.
        public float LastRequestMs { get; private set; } = -1f;
        public string DeviceName { get; private set; }

        SpotifyConfig config;
        SpotifyAuth auth;
        CancellationTokenSource loginCancel;
        string deviceId;
        TrackInfo pending;
        bool busy;

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable] class Device { public string id; public string name; public string type; public bool is_active; public bool is_restricted; }
        [Serializable] class DeviceList { public Device[] devices; }
        [Serializable] class Item { public string name; public string uri; }
        [Serializable] class PlayerState { public Device device; public bool is_playing; public int progress_ms; public Item item; }
        [Serializable] class Restrictions { public string reason; }
        [Serializable] class Album { public string uri; }
        [Serializable] class TrackCheck { public bool is_playable; public Restrictions restrictions; public Album album; }
#pragma warning restore 0649
        [Serializable] class PlayBody { public string[] uris; }
        [Serializable] class TransferBody { public string[] device_ids; public bool play; }
        [Serializable] class Offset { public string uri; }
        [Serializable] class ContextBody { public string context_uri; public Offset offset; }

        /// Ways to start a song. Starting it from inside its album is the default: on our test PC the
        /// desktop app ignored plain "play this song" requests but always obeyed the album version.
        /// (Side effect: when the song ends, Spotify carries on with the rest of that album.)
        enum StartMethod { FromAlbum, PlainPlay, TransferThenPlay }

        StartMethod method = StartMethod.FromAlbum;
        int playGeneration;
        readonly System.Collections.Generic.Dictionary<string, TrackCheck> trackCache = new System.Collections.Generic.Dictionary<string, TrackCheck>();

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

        /// Requests made while another is still running are merged: only the newest track gets played.
        public async void Play(TrackInfo track, SceneInfo scene)
        {
            pending = track;
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

        async Task PlayNow(TrackInfo track)
        {
            if (auth == null) return; // Status already explains the config problem
            if (!auth.IsLoggedIn) { Status = "Not logged in: press L"; return; }

            var generation = ++playGeneration;
            var used = method;
            var watch = Stopwatch.StartNew();

            if (used == StartMethod.TransferThenPlay && !await TransferToDevice()) return;
            var body = await BuildBody(used, track);

            var (code, text, retryAfter) = await Send("PUT", PlayUrl(), body);

            if (code == 401) // access token expired early: refresh once and retry
            {
                await auth.GetAccessTokenAsync(forceRefresh: true);
                (code, text, retryAfter) = await Send("PUT", PlayUrl(), body);
            }
            if (code == 404) // no active device (or the remembered one went away): pick one and retry
            {
                if (!await ChooseDevice()) return;
                (code, text, retryAfter) = await Send("PUT", PlayUrl(), body);
            }
            if (code == 429) // too many requests: wait as long as Spotify asks, then retry once
            {
                Status = $"Spotify asked us to slow down; waiting {retryAfter}s";
                await Task.Delay(TimeSpan.FromSeconds(Mathf.Clamp(retryAfter, 1, 30)));
                (code, text, _) = await Send("PUT", PlayUrl(), body);
            }

            watch.Stop();
            if (code >= 200 && code < 300)
            {
                LastRequestMs = (float)watch.Elapsed.TotalMilliseconds;
                Status = $"Playing on {DeviceName ?? "your active Spotify device"}";
                Debug.Log($"[Spotify] Playing {track} via {Describe(used)} ({LastRequestMs:0} ms)");
                CheckPlaybackLater(track, generation, used);
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

        /// Spotify accepting the command doesn't guarantee the song starts: on some setups the desktop app
        /// stops the old song and never loads the new one. So two seconds later we ask Spotify what it's really
        /// doing. If the song isn't running, we try the other ways of starting it, and remember the one that works.
        async void CheckPlaybackLater(TrackInfo track, int generation, StartMethod used)
        {
            try
            {
                if (await StartedAfter(track, generation, used)) return;
                if (generation != playGeneration) return;

                // Is the track itself playable for this account/region?
                var info = await LookupTrack(track);
                if (info != null && !info.is_playable)
                {
                    Status = $"Song didn't start: not playable for you ({info.restrictions?.reason ?? "no reason given"})";
                    Debug.LogWarning($"[Spotify check] {track}: {Status}");
                    return;
                }

                foreach (StartMethod other in Enum.GetValues(typeof(StartMethod)))
                {
                    if (other == used) continue;
                    Status = $"Song didn't start; trying {Describe(other)}";
                    if (other == StartMethod.TransferThenPlay && !await TransferToDevice()) return;
                    if (generation != playGeneration) return;
                    await Send("PUT", PlayUrl(), await BuildBody(other, track));
                    if (await StartedAfter(track, generation, other))
                    {
                        method = other;
                        Debug.Log($"[Spotify] Switching to {Describe(other)} for the next songs");
                        return;
                    }
                    if (generation != playGeneration) return;
                }

                Status = "Song didn't start with any method. Try closing and reopening the Spotify app.";
                Debug.LogWarning($"[Spotify check] {track}: no method worked.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Spotify check] failed: " + e.Message);
            }
        }

        /// Waits two seconds, then asks Spotify whether this track is really playing. Logs what it finds.
        async Task<bool> StartedAfter(TrackInfo track, int generation, StartMethod how)
        {
            await Task.Delay(2000);
            if (this == null || generation != playGeneration) return false; // left play mode, or a newer song took over

            var (code, text, _) = await Send("GET", $"{Api}/me/player", null);
            var state = code == 200 && !string.IsNullOrEmpty(text) ? JsonUtility.FromJson<PlayerState>(text) : null;
            var onDevice = state?.device != null ? $"{state.device.name} ({state.device.type})" : "no device";
            // Compare by name too: the album copy of a song can have a different track ID than the playlist copy.
            var sameSong = state?.item != null &&
                           (state.item.uri == track.Uri || string.Equals(state.item.name, track.name, StringComparison.OrdinalIgnoreCase));
            var started = state != null && state.is_playing && state.progress_ms > 300 && sameSong;
            Debug.Log($"[Spotify check] {track} via {Describe(how)}: started={started}, playing={state?.is_playing}, " +
                      $"progress={state?.progress_ms} ms, item={state?.item?.name ?? "none"}, device={onDevice} (HTTP {code})");
            if (started) Status = $"Playing on {onDevice} (confirmed)";
            return started;
        }

        async Task<string> BuildBody(StartMethod how, TrackInfo track)
        {
            if (how == StartMethod.FromAlbum)
            {
                var albumUri = (await LookupTrack(track))?.album?.uri;
                if (albumUri != null) return AlbumBody(albumUri, track);
            }
            return JsonUtility.ToJson(new PlayBody { uris = new[] { track.Uri } });
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

        static string AlbumBody(string albumUri, TrackInfo track) =>
            JsonUtility.ToJson(new ContextBody { context_uri = albumUri, offset = new Offset { uri = track.Uri } });

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
