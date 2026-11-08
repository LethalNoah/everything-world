using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.Music
{
    /// One song in the playlist.
    [Serializable]
    public class TrackInfo
    {
        public string name;
        public string artist;
        public string id; // Spotify track ID

        public string Uri => "spotify:track:" + id;
        public override string ToString() => $"{name} by {artist}";
    }

    /// One music scene (a group of similar artists), as exported by build/export_unity.py.
    [Serializable]
    public class SceneInfo
    {
        public int id;
        public string name;      // from build/scene_art.json
        public int realm;
        public string realmName;
        public string[] tags;
        public float x, y, r;
        public float energy, acousticness, valence, danceability, tempo;
        public int[] neighbours;

        // Look brief (art plan), from build/scene_art.json
        public string terrain;
        public string[] palette; // "#RRGGBB label"
        public string landmark;
        public string props;
        public string mood;

        public TrackInfo[] tracks;

        /// The scene's name, or its top two tags if it hasn't been named.
        public string DisplayName =>
            !string.IsNullOrEmpty(name) ? name :
            tags == null || tags.Length == 0 ? $"Scene {id}" : string.Join(" / ", tags, 0, Mathf.Min(2, tags.Length));

        /// Palette colour i parsed from "#RRGGBB label", or grey if missing.
        public Color PaletteColor(int i)
        {
            if (palette != null && i < palette.Length && ColorUtility.TryParseHtmlString(palette[i].Split(' ')[0], out var c))
                return c;
            return Color.grey;
        }
    }

    /// All scenes, loaded from Assets/_Project/Data/scenes.json.
    [Serializable]
    public class SceneCatalog
    {
        public SceneInfo[] scenes;

        Dictionary<int, SceneInfo> byId;

        public static SceneCatalog FromJson(TextAsset json)
        {
            var catalog = JsonUtility.FromJson<SceneCatalog>(json.text);
            catalog.byId = new Dictionary<int, SceneInfo>();
            foreach (var s in catalog.scenes) catalog.byId[s.id] = s;
            return catalog;
        }

        public SceneInfo Get(int id) => byId.TryGetValue(id, out var s) ? s : null;
    }
}
