using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.Music
{
    /// Decides what music should play based on which zone the player is standing in.
    /// - Entering a zone picks a random track from that zone's scene (after a short delay,
    ///   so brushing past a border doesn't switch songs).
    /// - Leaving every zone keeps the current song going.
    /// - Overlapping zones: the one entered most recently wins.
    public class MusicDirector : MonoBehaviour
    {
        public static MusicDirector Instance { get; private set; }

        [Tooltip("Assets/_Project/Data/scenes.json")]
        public TextAsset sceneData;

        [Tooltip("Component that receives play commands (for now the DryRunMusicOutput).")]
        public MonoBehaviour output;

        [Tooltip("Seconds you must stay in a new zone before the music switches.")]
        public float switchDelay = 0.75f;

        public SceneCatalog Catalog { get; private set; }
        public SceneInfo CurrentScene => zones.Count > 0 ? Catalog.Get(zones[zones.Count - 1].sceneId) : null;
        public SceneInfo PlayingScene { get; private set; }

        readonly List<MusicZone> zones = new List<MusicZone>();
        readonly Dictionary<int, TrackInfo> lastTrackPerScene = new Dictionary<int, TrackInfo>();
        IMusicOutput musicOutput;
        float switchAt = -1f;

        void Awake()
        {
            Instance = this;
            Catalog = SceneCatalog.FromJson(sceneData);
            musicOutput = output as IMusicOutput;
            if (musicOutput == null) Debug.LogError("MusicDirector: 'output' must be a component that implements IMusicOutput.");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnterZone(MusicZone zone)
        {
            zones.Remove(zone);
            zones.Add(zone);
            ScheduleSwitch();
        }

        public void ExitZone(MusicZone zone)
        {
            zones.Remove(zone);
            ScheduleSwitch();
        }

        void ScheduleSwitch()
        {
            var target = CurrentScene;
            // Nothing to do if we left all zones, or are back in the scene that's already playing.
            switchAt = target == null || target == PlayingScene ? -1f : Time.time + switchDelay;
        }

        void Update()
        {
            if (switchAt < 0f || Time.time < switchAt) return;
            switchAt = -1f;
            var scene = CurrentScene;
            if (scene == null || scene.tracks == null || scene.tracks.Length == 0) return;

            var track = PickTrack(scene);
            PlayingScene = scene;
            musicOutput?.Play(track, scene);
        }

        /// Random track from the scene, avoiding the one this scene played last time.
        TrackInfo PickTrack(SceneInfo scene)
        {
            lastTrackPerScene.TryGetValue(scene.id, out var last);
            TrackInfo pick;
            do pick = scene.tracks[Random.Range(0, scene.tracks.Length)];
            while (pick == last && scene.tracks.Length > 1);
            lastTrackPerScene[scene.id] = pick;
            return pick;
        }
    }
}
