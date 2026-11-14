using UnityEngine;

namespace EverythingWorld.World
{
    /// What someone says, over their head: Nunito in a dark rounded box, always facing you and always drawn over
    /// the scene, so it never blends into a bright sky or disappears behind a wall. Hides itself when its time
    /// is up.
    public class SpeechBubble : MonoBehaviour
    {
        static Material textMat, boxMat;
        TextMesh text; Transform box; MeshRenderer textRenderer; float left; Vector3 anchorAt;

        public static SpeechBubble Make(Transform parent, string name = "Words")
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var b = go.AddComponent<SpeechBubble>();
            var t = new GameObject("Text"); t.transform.SetParent(go.transform, false);
            b.text = t.AddComponent<TextMesh>(); b.text.font = GameFonts.Speech; b.text.fontSize = 96; b.text.characterSize = .15f / 9.6f;
            b.text.anchor = TextAnchor.MiddleCenter; b.text.alignment = TextAlignment.Center; b.text.color = new Color(.98f, .97f, .93f);
            b.textRenderer = t.GetComponent<MeshRenderer>(); b.textRenderer.sharedMaterial = TextMaterial();
            b.textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; b.textRenderer.receiveShadows = false;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(q.GetComponent<Collider>()); q.name = "Box";
            q.transform.SetParent(go.transform, false); q.transform.localPosition = new Vector3(0, 0, .002f);
            var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = BoxMaterial(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            b.box = q.transform;
            go.SetActive(false);
            return b;
        }

        static Material TextMaterial()
        {
            if (textMat != null) return textMat;
            var sh = Shader.Find("Everything World/Overlay Text"); var f = GameFonts.Speech;
            textMat = sh != null ? new Material(sh) { name = "Speech text", mainTexture = f.material.mainTexture } : f.material;
            Font.textureRebuilt += x => { if (x == GameFonts.Speech && textMat != null) textMat.mainTexture = x.material.mainTexture; };
            return textMat;
        }

        static Material BoxMaterial()
        {
            if (boxMat != null) return boxMat;
            var sh = Shader.Find("Everything World/Overlay Box");
            boxMat = sh != null ? new Material(sh) { name = "Speech box" } : new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(.06f, .06f, .07f) };
            return boxMat;
        }

        /// Shows the words with the bottom of the box at 'at' (a world position) for 'seconds'.
        public void Show(string words, Vector3 at, float seconds)
        {
            text.text = words; left = seconds; anchorAt = at; gameObject.SetActive(true); Place();
        }

        /// Breaks text into lines of about 'width' characters (keeping any line breaks already in it).
        public static string Wrap(string s, int width)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var para in (s ?? "").Split('\n'))
            {
                if (sb.Length > 0) sb.Append('\n');
                var line = 0;
                foreach (var word in para.Split(' '))
                {
                    if (line > 0 && line + 1 + word.Length > width) { sb.Append('\n'); line = 0; }
                    else if (line > 0) { sb.Append(' '); line++; }
                    sb.Append(word); line += word.Length;
                }
            }
            return sb.ToString();
        }

        public void Hide() { left = 0f; gameObject.SetActive(false); }

        void LateUpdate()
        {
            left -= Time.deltaTime; if (left <= 0f) { gameObject.SetActive(false); return; }
            Place();
        }

        void Place()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            // fit the box round the words with a margin, and stand it on its anchor
            var lb = textRenderer.localBounds; var size = new Vector2(Mathf.Max(lb.size.x, .2f) + .18f, Mathf.Max(lb.size.y, .1f) + .12f);
            box.localScale = new Vector3(size.x, size.y, 1f); box.localPosition = new Vector3(lb.center.x, lb.center.y, .002f);
            transform.position = anchorAt + Vector3.up * (size.y / 2f);
        }
    }
}
