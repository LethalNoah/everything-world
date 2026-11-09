using System;
using UnityEngine;

namespace EverythingWorld.World
{
    /// How the sky looks over one land: sun, ambient light, fog and weather. Every land keeps its own sky,
    /// always at its best time of day (see WORLD_GUIDE.md). Read from Assets/_Project/Data/land_moods.json;
    /// lands not listed there use the "fallback" mood. Colours are "#RRGGBB".
    [Serializable]
    public class LandMood
    {
        public string land;                 // the land's name, as in scene_art.json
        public float sunPitch = 38f;        // degrees above the horizon
        public float sunYaw = -35f;         // compass direction the sunlight travels toward
        public float sunIntensity = 1.3f;
        public string sunColor = "#FFF2DB";
        public string skyLight = "#9EADBF"; // ambient light from above
        public string horizonLight = "#8C8C85";
        public string groundLight = "#59544D";
        public string fogColor = "#BDCCD1"; // also the colour of the sky itself
        public float fogStart = 350f, fogEnd = 1800f;
        public string weather = "";         // "", "snow" or "rain"
        public float weatherAmount;         // 0..1

        public static Color Hex(string s, Color fallback) =>
            !string.IsNullOrEmpty(s) && ColorUtility.TryParseHtmlString(s, out var c) ? c : fallback;
    }

    [Serializable]
    public class LandMoodSet
    {
        public LandMood fallback = new LandMood();
        public LandMood[] lands = new LandMood[0];
    }
}
