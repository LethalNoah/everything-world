using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EverythingWorld.Spotify
{
    /// Spotify login using "Authorization Code with PKCE": no client secret needed.
    /// 1. Opens Spotify's "Allow access?" page in your browser.
    /// 2. Spotify sends the browser back to http://127.0.0.1:PORT/callback, which this class listens on.
    /// 3. Trades the one-time code for tokens and saves them in Unity's per-user data folder
    ///    (Application.persistentDataPath, outside the repo), so you only log in once.
    public class SpotifyAuth
    {
        const string AuthorizeUrl = "https://accounts.spotify.com/authorize";
        const string TokenUrl = "https://accounts.spotify.com/api/token";
        // Only what playback needs: start/switch songs and see your devices.
        const string Scopes = "user-modify-playback-state user-read-playback-state";

#pragma warning disable 0649 // filled in by JsonUtility
        [Serializable]
        class TokenData
        {
            public string access_token;
            public string refresh_token;
            public int expires_in;
            public long expiresAtUnix;
        }

#pragma warning restore 0649

        readonly SpotifyConfig config;
        TokenData tokens;

        public static string TokenPath => Path.Combine(Application.persistentDataPath, "spotify_tokens.json");
        public bool IsLoggedIn => !string.IsNullOrEmpty(tokens?.refresh_token);

        public SpotifyAuth(SpotifyConfig config)
        {
            this.config = config;
            if (File.Exists(TokenPath))
            {
                try { tokens = JsonUtility.FromJson<TokenData>(File.ReadAllText(TokenPath)); }
                catch (Exception e) { Debug.LogWarning($"Spotify: couldn't read saved login ({e.Message}); log in again."); }
            }
        }

        public void Forget()
        {
            tokens = null;
            if (File.Exists(TokenPath)) File.Delete(TokenPath);
        }

        /// A valid access token, refreshed if it's about to expire. Null if not logged in.
        public async Task<string> GetAccessTokenAsync(bool forceRefresh = false)
        {
            if (!IsLoggedIn) return null;
            if (forceRefresh || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > tokens.expiresAtUnix - 60)
                await RefreshAsync();
            return tokens?.access_token;
        }

        public async Task LoginAsync(CancellationToken cancel)
        {
            var verifier = RandomUrlSafe(64);
            var challenge = Base64Url(SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(verifier)));
            var state = RandomUrlSafe(16);

            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{config.port}/");
            listener.Start();
            using var stopOnCancel = cancel.Register(() => listener.Stop());

            Application.OpenURL(AuthorizeUrl + "?" + Query(new Dictionary<string, string>
            {
                ["client_id"] = config.clientId,
                ["response_type"] = "code",
                ["redirect_uri"] = config.RedirectUri,
                ["code_challenge_method"] = "S256",
                ["code_challenge"] = challenge,
                ["scope"] = Scopes,
                ["state"] = state,
            }));

            // Wait for the browser to come back to /callback (ignore anything else, like favicon requests).
            HttpListenerContext ctx;
            while (true)
            {
                try { ctx = await listener.GetContextAsync(); }
                catch (Exception) when (cancel.IsCancellationRequested) { throw new OperationCanceledException(); }
                if (ctx.Request.Url.AbsolutePath == "/callback") break;
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
            }

            var code = ctx.Request.QueryString["code"];
            var error = ctx.Request.QueryString["error"];
            var ok = error == null && code != null && ctx.Request.QueryString["state"] == state;
            Respond(ctx, ok
                ? "Logged in to Spotify. You can close this tab and go back to Everything World."
                : "Spotify login didn't finish. Go back to Everything World and try again.");
            if (!ok) throw new Exception(error != null ? $"Spotify said: {error}" : "Login response didn't match (state check failed).");

            await RequestTokensAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = config.RedirectUri,
                ["client_id"] = config.clientId,
                ["code_verifier"] = verifier,
            });
        }

        async Task RefreshAsync()
        {
            await RequestTokensAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = tokens.refresh_token,
                ["client_id"] = config.clientId,
            });
        }

        async Task RequestTokensAsync(Dictionary<string, string> form)
        {
            using var req = UnityWebRequest.Post(TokenUrl, form);
            await req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                // A rejected refresh token means the login was revoked: forget it so the player logs in again.
                if (form["grant_type"] == "refresh_token" && req.responseCode == 400) Forget();
                throw new Exception($"Spotify token request failed ({req.responseCode}): {req.downloadHandler.text}");
            }

            var fresh = JsonUtility.FromJson<TokenData>(req.downloadHandler.text);
            if (string.IsNullOrEmpty(fresh.refresh_token)) fresh.refresh_token = tokens?.refresh_token; // refresh may not return a new one
            fresh.expiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + fresh.expires_in;
            tokens = fresh;
            File.WriteAllText(TokenPath, JsonUtility.ToJson(tokens));
        }

        static void Respond(HttpListenerContext ctx, string message)
        {
            var html = Encoding.UTF8.GetBytes(
                $"<html><body style='font-family:sans-serif;padding:3em'><h2>Everything World</h2><p>{message}</p></body></html>");
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = html.Length;
            ctx.Response.OutputStream.Write(html, 0, html.Length);
            ctx.Response.Close();
        }

        static string Query(Dictionary<string, string> values)
        {
            var parts = new List<string>();
            foreach (var kv in values) parts.Add(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value));
            return string.Join("&", parts);
        }

        static string RandomUrlSafe(int bytes)
        {
            var data = new byte[bytes];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);
            return Base64Url(data);
        }

        static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
