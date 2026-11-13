using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace EverythingWorld.Player
{
    /// A paper map to look down at: press M and a folded map of the whole world comes up from below into your
    /// hands, tipped toward you. A red pin stands where you are, with a little arrow for the way you're facing and
    /// a ring pulsing round it, and the name of the land you're in is written along the bottom. You can keep walking
    /// with it up. Press M again to put it away.
    public class MapInHand : MonoBehaviour
    {
        public Key key = Key.M;
        public float seconds = .4f;

        Camera cam; Transform map, pin, ring, arrow; TextMesh where; Material ringMat;
        bool up; float t; float w, h;
        IslandWorldData data; string shownLand;

        /// Raises or puts away the map (instant = no animation; for the photo tour).
        public void Hold(bool raise, bool instant = false) { up = raise; if (instant) t = raise ? 1f : 0f; }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[key].wasPressedThisFrame) up = !up;
            t = Mathf.MoveTowards(t, up ? 1f : 0f, Time.deltaTime / seconds);
        }

        void LateUpdate()
        {
            if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
            if (cam == null) return;
            if (t <= 0f) { if (map != null) map.gameObject.SetActive(false); return; }
            if (map == null && !Build()) return;
            if (map.parent != cam.transform) map.SetParent(cam.transform, false);
            map.gameObject.SetActive(true);
            // up from below, turning toward you as it comes
            var e = Mathf.SmoothStep(0f, 1f, t);
            map.localPosition = new Vector3(0f, Mathf.Lerp(-.6f, -.07f, e), Mathf.Lerp(.36f, .41f, e));
            map.localRotation = Quaternion.Euler(Mathf.Lerp(80f, 20f, e), 0f, Mathf.Lerp(-6f, 0f, e));
            // the pin where you are, the arrow the way you face, and the land you're in
            var p = transform.position; var lay = data.layout;
            var u = Mathf.Clamp01(p.x / lay.width); var v = Mathf.Clamp01(p.z / lay.depth);
            var at = new Vector3((u - .5f) * w, (v - .5f) * h, 0f);
            pin.localPosition = at + Vector3.up * .0f;
            var fwd = cam.transform.forward; var heading = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            arrow.localPosition = at - Vector3.forward * .003f; arrow.localRotation = Quaternion.Euler(0f, 0f, -heading);
            var pulse = Mathf.Repeat(Time.time * .8f, 1f);
            ring.localPosition = at - Vector3.forward * .002f; ring.localScale = Vector3.one * (1f + pulse * 2.2f);
            ringMat.SetColor("_BaseColor", new Color(.85f, .1f, .08f, (1f - pulse) * .8f));
            var isl = data.IslandAt(p.x, p.z); var name = isl != null ? isl.name : "At sea";
            if (name != shownLand) { shownLand = name; where.text = "You are in " + name; }
        }

        static Material Unlit(Color c, bool transparent = false)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Map in hand" };
            m.SetColor("_BaseColor", c);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent + 10;
            }
            m.SetFloat("_Cull", 0f);
            return m;
        }

        static Transform Piece(MeshKit mk, string name, Transform parent)
        {
            var go = mk.BuildSingle(name, parent, false); go.isStatic = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            return go.transform;
        }

        /// The map, built the first time it's raised: paper with the world painted on it, its frame of darker
        /// paper, two thumbs holding it, the pin, the arrow, the ring and the words.
        bool Build()
        {
            data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null;
            if (data == null) return false;
            var lay = data.layout;
            w = .38f; h = w * lay.depth / lay.width;
            map = new GameObject("Map in hand").transform;
            var paper = Unlit(Color.white); paper.SetTexture("_BaseMap", HubKit.PaintMap(data, true)); paper.mainTexture.wrapMode = TextureWrapMode.Clamp;
            var mk = new MeshKit();
            // the quad faces you (toward -z); MeshKit.Picture takes right, up and the normal
            mk.Picture(paper, Vector3.forward * .012f, Vector3.right * w / 2f, Vector3.up * h / 2f, Vector3.back, new Rect(0, 0, 1, 1));   // (Picture lifts itself 12 mm toward you)
            var margin = Unlit(new Color(.82f, .74f, .58f));
            mk.Picture(margin, Vector3.forward * .016f, Vector3.right * (w / 2f + .008f), Vector3.up * (h / 2f + .02f), Vector3.back, new Rect(0, 0, 1, 1));
            var skin = Unlit(new Color(.78f, .58f, .45f)); var skinD = Unlit(new Color(.66f, .48f, .37f));
            foreach (var s in new[] { -1f, 1f })
            {
                var th = new Vector3(s * (w / 2f - .01f), -h / 2f + .025f, -.012f);
                mk.Box(skin, th, new Vector3(.03f, .055f, .02f), Quaternion.Euler(0, 0, s * 25f));
                mk.Box(skinD, th + new Vector3(s * .012f, -.035f, .01f), new Vector3(.05f, .05f, .03f), Quaternion.Euler(0, 0, s * 25f));
            }
            Piece(mk, "Paper", map);
            // the pin: a red head on a short needle, standing out of the paper toward you
            var pm = new MeshKit(); var red = Unlit(new Color(.9f, .12f, .1f)); var steel = Unlit(new Color(.7f, .7f, .74f));
            pm.Prism(steel, Vector3.zero, Vector3.back, .0012f, .02f, 6);   // (a Prism: MeshKit grows boxes by a few mm, which would swallow a needle)
            pm.Blob(red, Vector3.back * .022f, Vector3.one * .008f, new System.Random(1), 0f);
            pm.Blob(Unlit(new Color(1f, .7f, .65f)), Vector3.back * .0275f + new Vector3(-.0025f, .0025f, 0), Vector3.one * .0022f, new System.Random(2), 0f);
            pin = Piece(pm, "Pin", map);
            var am = new MeshKit(); am.Tri(red, new Vector3(0, .024f, 0), new Vector3(-.008f, .008f, 0), new Vector3(.008f, .008f, 0), Vector3.forward);
            arrow = Piece(am, "Facing", map);
            var rm = new MeshKit(); ringMat = Unlit(new Color(.85f, .1f, .08f, .8f), true);
            for (var k = 0; k < 24; k++) { var a = k * Mathf.PI * 2f / 24f; rm.Box(ringMat, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .01f, new Vector3(.0028f, .0016f, .0005f), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); }
            ring = Piece(rm, "Ring", map);
            where = ArtistBits.Label(map, "", Vector3.zero, Vector3.back, .016f, new Color(.25f, .16f, .08f), TextAnchor.MiddleCenter);
            where.transform.localPosition = new Vector3(0, -h / 2f - .01f, -.002f); where.transform.localRotation = Quaternion.identity;
            var title = ArtistBits.Label(map, "THE EVERYTHING WORLD", Vector3.zero, Vector3.back, .013f, new Color(.4f, .14f, .08f), TextAnchor.MiddleCenter);
            title.transform.localPosition = new Vector3(0, h / 2f + .01f, -.002f); title.transform.localRotation = Quaternion.identity;
            foreach (var r in map.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            return true;
        }
    }
}
