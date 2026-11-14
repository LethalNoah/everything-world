using UnityEngine;

namespace EverythingWorld.World
{
    /// The game's two typefaces (Resources/Fonts, both under the SIL Open Font License; licences beside them):
    /// - Sign: Fraunces 72pt SemiBold, a warm, characterful serif for signs, plaques, boards and titles;
    /// - Speech: Nunito SemiBold, soft and rounded, for what people say and the text on the screen.
    /// Falls back to Unity's built-in font if a file is missing.
    public static class GameFonts
    {
        static Font sign, speech, builtin;
        static Font Builtin => builtin != null ? builtin : builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font Sign { get { if (sign == null) { sign = Resources.Load<Font>("Fonts/Fraunces72pt-SemiBold"); if (sign == null) sign = Builtin; } return sign; } }
        public static Font Speech { get { if (speech == null) { speech = Resources.Load<Font>("Fonts/Nunito-SemiBold"); if (speech == null) speech = Builtin; } return speech; } }

        static Texture2D panel;
        /// A box for text on the screen: Nunito on a dark, slightly see-through panel with room round the words.
        public static GUIStyle Box(int size, TextAnchor anchor, bool wrap = false, bool signFont = false)
        {
            if (panel == null) { panel = new Texture2D(1, 1) { name = "Panel" }; panel.SetPixel(0, 0, new Color(.05f, .05f, .07f, .78f)); panel.Apply(); }
            var s = new GUIStyle(GUI.skin.box) { font = signFont ? Sign : Speech, fontSize = size, alignment = anchor, wordWrap = wrap, padding = new RectOffset(14, 14, 8, 8) };
            s.normal.background = panel; s.normal.textColor = new Color(.96f, .95f, .92f);
            return s;
        }
    }
}
