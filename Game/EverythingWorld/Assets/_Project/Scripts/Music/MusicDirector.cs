using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.Music
{
    /// Tunes the player into the radio station of the zone they're standing in (see RadioSchedule).
    /// - Entering a zone joins that station's current song at its current position (after a short delay,
    ///   so brushing past a border doesn't switch stations).
    /// - When the station's song ends, the station's next song plays: from the zone you're in now,
    ///   or from the last zone if you're between zones.
    /// - Leaving every zone keeps you on the last station.
    /// - Overlapping zones: the one entered most recently wins.
    public class MusicDirector : MonoBehaviour
    {
        public static MusicDirector Instance { get; private set; }

        [Tooltip("Assets/_Project/Data/scenes.json")]
        public TextAsset sceneData;

        [Tooltip("Component that receives play commands (the MusicOutputSwitch).")]
        public MonoBehaviour output;

        [Tooltip("Seconds you must stay in a new zone before the music switches.")]
        public float switchDelay = 0.75f;

        public SceneCatalog Catalog { get; private set; }
        /// The scene of the zone you're in: a trigger zone if you're inside one, otherwise the island under you.
        public SceneInfo CurrentScene =>
            zones.Count > 0 ? Catalog.Get(zones[zones.Count - 1].sceneId) : areaSceneId >= 0 ? Catalog.Get(areaSceneId) : null;
        /// The station you're tuned to, and the song it's on.
        public SceneInfo PlayingScene { get; private set; }
        public RadioSchedule.Slot CurrentSlot { get; private set; }

        readonly List<MusicZone> zones = new List<MusicZone>();
        IMusicOutput musicOutput;
        float switchAt = -1f;
        int areaSceneId = -1;

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

        /// Set by whatever knows where the player is (the IslandExplorer in the island world).
        /// -1 = not on an island (bridge, sea): the current station keeps playing.
        public void SetArea(int sceneId)
        {
            if (sceneId == areaSceneId) return;
            areaSceneId = sceneId;
            ScheduleSwitch();
        }

        void ScheduleSwitch()
        {
            var target = CurrentScene;
            // Nothing to do if we left all zones, or are back on the station that's already playing.
            switchAt = target == null || target == PlayingScene ? -1f : Time.time + switchDelay;
        }

        void Update()
        {
            // The station moved on to its next song.
            if (PlayingScene != null && CurrentSlot.IsValid && RadioSchedule.NowSeconds >= CurrentSlot.endsAt)
            {
                switchAt = -1f;
                TuneIn(CurrentScene ?? PlayingScene, continuation: true);
                return;
            }

            if (switchAt < 0f || Time.time < switchAt) return;
            switchAt = -1f;
            TuneIn(CurrentScene, continuation: false);
        }

        void TuneIn(SceneInfo scene, bool continuation)
        {
            if (scene == null) return;
            var now = RadioSchedule.NowSeconds;
            var slot = RadioSchedule.At(scene, now);
            if (!slot.IsValid) return;

            PlayingScene = scene;
            CurrentSlot = slot;
            musicOutput?.Play(slot.track, scene, slot.PositionMsAt(now), continuation);
        }
    }
}
