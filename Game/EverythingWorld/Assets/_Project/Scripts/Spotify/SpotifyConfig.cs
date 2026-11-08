using System;
using System.IO;
using UnityEngine;

namespace EverythingWorld.Spotify
{
    /// Settings from Assets/StreamingAssets/spotify_config.json (gitignored).
    /// Copy spotify_config.example.json next to it and paste your app's Client ID.
    [Serializable]
    public class SpotifyConfig
    {
        public string clientId;
        public int port = 8888; // must match the redirect URI in the Spotify dashboard: http://127.0.0.1:8888/callback

        public string RedirectUri => $"http://127.0.0.1:{port}/callback";

        public static string FilePath => Path.Combine(Application.streamingAssetsPath, "spotify_config.json");

        /// Returns null and an explanation if the file is missing or still has the placeholder.
        public static SpotifyConfig Load(out string problem)
        {
            problem = null;
            if (!File.Exists(FilePath))
            {
                problem = "No spotify_config.json in Assets/StreamingAssets (copy the example file).";
                return null;
            }
            var config = JsonUtility.FromJson<SpotifyConfig>(File.ReadAllText(FilePath));
            if (config == null || string.IsNullOrWhiteSpace(config.clientId) || config.clientId.Contains("PASTE"))
            {
                problem = "Paste your Client ID into Assets/StreamingAssets/spotify_config.json.";
                return null;
            }
            config.clientId = config.clientId.Trim();
            return config;
        }
    }
}
