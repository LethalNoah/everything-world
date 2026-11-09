using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Shared materials for everything built in code: plain colours, glowing colours and pictures.
    /// One material per colour, reused everywhere, so many objects can share a draw call.
    public static class WorldMaterials
    {
        /// Use the Toon Shaders Pro shader for plain colours when it's imported (set by IslandWorldBuilder).
        public static bool UseToon;
        static Shader lit, toon, unlit;
        static readonly Dictionary<(Color, float), Material> solid = new Dictionary<(Color, float), Material>();
        static readonly Dictionary<(Color, float), Material> glow = new Dictionary<(Color, float), Material>();
        static readonly Dictionary<Texture, Material> pictures = new Dictionary<Texture, Material>();

        static Shader Lit => lit != null ? lit : lit = Shader.Find("Universal Render Pipeline/Lit");
        static Shader Toon => toon != null ? toon : toon = Shader.Find("Toon Shaders Pro/URP/Toon");
        static Shader Unlit => unlit != null ? unlit : unlit = Shader.Find("Universal Render Pipeline/Unlit");

        /// A matte (or slightly shiny) colour.
        public static Material Solid(Color c, float smooth = .08f)
        {
            if (solid.TryGetValue((c, smooth), out var m)) return m;
            var useToon = UseToon && Toon != null;
            m = new Material(useToon ? Toon : Lit) { name = "Solid " + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            if (useToon)
            {
                // soft two-tone shading: lit side the true colour, shaded side a cooler, darker version
                m.SetColor("_MiddleTint", new Color(.82f, .82f, .86f));
                m.SetColor("_ShadowTint", new Color(.5f, .52f, .62f, 1f));
            }
            return solid[(c, smooth)] = m;
        }

        /// A colour that glows (lantern glass, lit windows, candle flames). strength 1 = softly lit.
        public static Material Glow(Color c, float strength)
        {
            if (glow.TryGetValue((c, strength), out var m)) return m;
            m = new Material(Lit) { name = "Glow " + ColorUtility.ToHtmlStringRGB(c) };
            m.SetColor("_BaseColor", c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * strength);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return glow[(c, strength)] = m;
        }

        /// A picture that ignores lighting, so album covers stay readable at dusk.
        public static Material Picture(Texture t)
        {
            if (t == null) return Solid(new Color(.12f, .12f, .14f));
            if (pictures.TryGetValue(t, out var m)) return m;
            m = new Material(Unlit) { name = "Picture " + t.name };
            m.SetTexture("_BaseMap", t);
            m.SetColor("_BaseColor", new Color(.92f, .92f, .92f));
            return pictures[t] = m;
        }
    }
}
