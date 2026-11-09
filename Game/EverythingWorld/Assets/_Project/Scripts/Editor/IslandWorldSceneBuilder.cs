using EverythingWorld.Player;
using EverythingWorld.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EverythingWorld.EditorTools
{
    /// Menu: Everything World > Build Island World Scene.
    /// Creates Assets/_Project/Scenes/IslandWorld.unity. The islands themselves are built when you press Play,
    /// from the data that build/islandmap.py writes, so after a new export you only need to press Play again.
    public static class IslandWorldSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/IslandWorld.unity";
        const string Data = "Assets/_Project/Data/";

        [MenuItem("Everything World/Build Island World Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scenes = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "scenes.json");
            var layout = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "world_layout.json");
            var heights = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "world_height.bytes");
            var islands = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "world_island.bytes");
            if (scenes == null || layout == null || heights == null || islands == null)
            {
                EditorUtility.DisplayDialog("Missing data", "The island world data isn't in Assets/_Project/Data yet.\nRun: python build/islandmap.py", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.FindWithTag("MainCamera")); // the player brings its own camera

            // late-afternoon sun and a soft haze, so far islands fade into the distance
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null) { sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f); sun.intensity = 1.3f; sun.color = new Color(1f, .95f, .86f); }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.74f, .80f, .82f);
            RenderSettings.fogStartDistance = 350f;
            RenderSettings.fogEndDistance = 1800f;

            var world = new GameObject("Island World").AddComponent<IslandWorldBuilder>();
            world.layoutJson = layout;
            world.heightBytes = heights;
            world.islandBytes = islands;

            var player = ZoneTestSceneBuilder.MakePlayer(new Vector3(0f, 50f, 0f)); // moved to the spawn tower on Play
            player.AddComponent<IslandExplorer>();
            var walker = player.GetComponent<SimplePlayerController>();
            walker.runSpeed = 16f; // the world is about 4.8 km across; a quick run helps while testing
            var cam = player.GetComponentInChildren<Camera>();
            cam.farClipPlane = 2500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;

            ZoneTestSceneBuilder.MakeMusic(scenes);

            EditorSceneManager.SaveScene(scene, ScenePath);
            ZoneTestSceneBuilder.AddToBuildSettings(ScenePath);
            Debug.Log($"Built {ScenePath}. Press Play: the islands are generated on start.");
        }
    }
}
