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
        public int realm;
        public string[] tags;
        public float x, y, r;
        public float energy, acousticness, valence, danceability, tempo;
        public int[] neighbours;
        public TrackInfo[] tracks;

        /// Placeholder name until the scenes get real names: the top two tags.
        public string DisplayName =>
            tags == null || tags.Length == 0 ? $"Scene {id}" : string.Join(" / ", tags, 0, Mathf.Min(2, tags.Length));
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
