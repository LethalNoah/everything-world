using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EverythingWorld.EditorTools
{
    /// Started by build/take_shots.py: Unity runs in the background (-batchmode, with the editor closed), opens
    /// the world, presses Play, and the photo tour (ShotTour, which reads "-ewShots") walks the lands, saves
    /// photos to Logs/Shots/ and closes Unity when it's done.
    public static class ShotRunner
    {
        const string Scene = "Assets/_Project/Scenes/IslandWorld.unity";

        public static void Run()
        {
            Debug.Log("Shot tour: opening " + Scene);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            // never leave Unity running if something goes wrong: give up after 8 minutes
            var deadline = EditorApplication.timeSinceStartup + 8 * 60;
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    Debug.LogError("Shot tour: took too long, giving up");
                    EditorApplication.Exit(2);
                }
            };
            EditorApplication.EnterPlaymode();
        }
    }
}
