using EverythingWorld.Music;
using EverythingWorld.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EverythingWorld.EditorTools
{
    /// Menu: Everything World > Build Zone Test Scene.
    /// Creates Assets/_Project/Scenes/ZoneTest.unity: flat ground, two music zones and a player.
    /// Running it again rebuilds the scene from scratch.
    public static class ZoneTestSceneBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/ZoneTest.unity";
        const string MaterialDir = "Assets/_Project/Materials";
        const string DataPath = "Assets/_Project/Data/scenes.json";

        // The two scenes to test with (ids from scenes.json / the review page).
        const int ZoneASceneId = 1;  // post hardcore / metalcore
        const int ZoneBSceneId = 15; // folk / acoustic

        [MenuItem("Everything World/Build Zone Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var data = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if (data == null)
            {
                EditorUtility.DisplayDialog("Missing data", $"{DataPath} not found. Run: python build/export_unity.py", "OK");
                return;
            }
            var catalog = SceneCatalog.FromJson(data);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.FindWithTag("MainCamera")); // the player brings its own camera

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f); // 100 x 100 m
            ground.GetComponent<Renderer>().sharedMaterial = Mat("Ground", new Color(0.55f, 0.62f, 0.45f));

            // Zones
            MakeZone("Zone A", catalog.Get(ZoneASceneId), new Vector3(-15f, 0f, 15f), new Color(0.80f, 0.25f, 0.25f));
            MakeZone("Zone B", catalog.Get(ZoneBSceneId), new Vector3(15f, 0f, 15f), new Color(0.30f, 0.50f, 0.85f));

            // Player
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0f, 1.1f, -10f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            body.GetComponent<Renderer>().sharedMaterial = Mat("Player", new Color(0.95f, 0.85f, 0.4f));

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            camGo.AddComponent<Camera>().nearClipPlane = 0.1f;
            camGo.AddComponent<AudioListener>();

            var controller = player.AddComponent<SimplePlayerController>();
            controller.cameraPivot = pivot.transform;

            // Music
            var music = new GameObject("Music");
            var dryRun = music.AddComponent<DryRunMusicOutput>();
            var director = music.AddComponent<MusicDirector>();
            director.sceneData = data;
            director.output = dryRun;
            var hud = music.AddComponent<MusicHud>();
            hud.director = director;
            hud.dryRun = dryRun;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log($"Built {ScenePath}. Press Play and walk into the red or blue zone.");
        }

        static void MakeZone(string name, SceneInfo info, Vector3 position, Color color)
        {
            const float size = 16f;

            var zone = new GameObject($"{name} (scene {info.id})");
            zone.transform.position = position;
            var trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(size, 6f, size);
            trigger.center = new Vector3(0f, 3f, 0f);
            zone.AddComponent<MusicZone>().sceneId = info.id;

            // Coloured floor patch so you can see the zone
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.SetParent(zone.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            floor.transform.localScale = new Vector3(size, 0.04f, size);
            floor.GetComponent<Renderer>().sharedMaterial = Mat(name.Replace(" ", ""), color);

            // Floating label with the scene's tags
            var label = new GameObject("Label");
            label.transform.SetParent(zone.transform, false);
            label.transform.localPosition = new Vector3(0f, 4f, -size / 2f);
            var text = label.AddComponent<TextMesh>();
            text.text = $"Scene {info.id}\n{info.DisplayName}\n{info.tracks.Length} songs";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.25f;
            text.fontSize = 48;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
        }

        static Material Mat(string name, Color color)
        {
            if (!AssetDatabase.IsValidFolder(MaterialDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            var path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
