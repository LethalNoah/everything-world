using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EverythingWorld.EditorTools
{
    /// Making a copy of the game to give to friends, in two steps (menu: Everything World > Build).
    ///
    /// 1. Keep the shaders (while playing, once every land has loaded). The whole world is made by code when you
    ///    press Play, so a build can't see which shaders and shader settings it needs, and leaves them out: things
    ///    would come out pink, or lose their glow or see-through look. This saves one small material for each kind
    ///    the world uses into Resources/ShaderKeep, which makes the build keep them. Run it again after big changes.
    /// 2. Build for Windows: the world scene only, into a folder beside the repo (never inside it), ready to zip.
    public static class BuildGame
    {
        const string Scene = "Assets/_Project/Scenes/IslandWorld.unity";
        const string KeepDir = "Assets/_Project/Resources/ShaderKeep";

        // shaders that may only appear now and then (a song's event, a time of day), so always kept
        static readonly string[] Always =
        {
            "Everything World/Aurora Ribbon", "Everything World/Aurora", "Everything World/Chase Lights", "Everything World/Glow Halo",
            "Everything World/Interact Glow", "Everything World/Meteor", "Everything World/Overlay Box", "Everything World/Overlay Text",
            "Everything World/Star Lines", "Everything World/Stars", "Everything World/Sticker Stars", "Everything World/Water",
            "Everything World/Wind Grass", "Everything World/World Text", "Everything World/Zone Field",
            "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit",
        };

        static string BuildDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "..", "EverythingWorldBuild"));

        [MenuItem("Everything World/Build/1. Keep the shaders (in Play, after loading)")]
        public static void KeepShaders()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Keep the shaders", "Press Play first, choose Enter the World, and wait until every land has loaded. Then choose this again.", "OK");
                return;
            }
            Directory.CreateDirectory(KeepDir);
            var found = new List<Material>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                found.AddRange(r.sharedMaterials);
                if (r is ParticleSystemRenderer p && p.trailMaterial != null) found.Add(p.trailMaterial);
            }
            if (RenderSettings.skybox != null) found.Add(RenderSettings.skybox);
            foreach (var name in Always) { var s = Shader.Find(name); if (s != null) found.Add(new Material(s)); else Debug.LogWarning("Keep the shaders: can't find " + name); }

            int added = 0, kinds = 0; var seen = new HashSet<string>();
            foreach (var m in found)
            {
                if (m == null || m.shader == null || m.shader.name.StartsWith("Hidden/")) continue;
                var keywords = string.Join(" ", m.shaderKeywords.OrderBy(k => k));
                var id = m.shader.name + "|" + keywords;
                if (!seen.Add(id)) continue;
                kinds++;
                var file = $"{KeepDir}/{Safe(m.shader.name)}_{Hash(id)}.mat";
                if (File.Exists(file)) continue;
                var copy = new Material(m) { name = Path.GetFileNameWithoutExtension(file) };
                foreach (var tex in copy.GetTexturePropertyNames()) copy.SetTexture(tex, null);   // only the settings matter, not the pictures
                AssetDatabase.CreateAsset(copy, file); added++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Keep the shaders: the world uses {kinds} kinds of material; {added} new ones saved to {KeepDir}.");
            EditorUtility.DisplayDialog("Keep the shaders", $"Done: {kinds} kinds of material in the world, {added} new ones saved.\n\nStop Play, then choose Everything World > Build > 2. Build for Windows.", "OK");
        }

        [MenuItem("Everything World/Build/2. Build for Windows")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Build for Windows", "Stop Play first.", "OK"); return; }
            if (!Directory.Exists(KeepDir) || Directory.GetFiles(KeepDir, "*.mat").Length == 0)
            {
                EditorUtility.DisplayDialog("Build for Windows", "Do step 1 first (Keep the shaders), or the build will be missing shaders.", "OK");
                return;
            }
            if (!File.Exists(Path.Combine(Application.streamingAssetsPath, "spotify_config.json")) &&
                !EditorUtility.DisplayDialog("Build for Windows", "There's no spotify_config.json in StreamingAssets, so Spotify won't work in this build. Build anyway?", "Build", "Cancel"))
                return;

            var dir = BuildDir;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);   // a clean build each time (this folder is only ever a build)
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Path.Combine(dir, "Everything World.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var sum = report.summary;
            if (sum.result != BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build for Windows", $"The build failed ({sum.totalErrors} errors). The Console has the details.", "OK");
                return;
            }
            // these are for debugging crashes, not for players
            foreach (var extra in Directory.GetDirectories(dir, "*DoNotShip*")) Directory.Delete(extra, true);
            var mb = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) / (1024f * 1024f);
            Debug.Log($"Built Everything World for Windows: {dir} ({mb:0} MB, {sum.totalTime.TotalMinutes:0.0} min)");
            EditorUtility.RevealInFinder(Path.Combine(dir, "Everything World.exe"));
            EditorUtility.DisplayDialog("Build for Windows", $"Done: {mb:0} MB in\n{dir}\n\nTo share it: right-click the folder > Compress to ZIP file.", "OK");
        }

        static string Safe(string s) => new string(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        static string Hash(string s) { unchecked { uint h = 2166136261; foreach (var c in s) h = (h ^ c) * 16777619; return h.ToString("x8"); } }
    }
}
