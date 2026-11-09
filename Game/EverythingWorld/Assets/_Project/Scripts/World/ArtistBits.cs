using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Pieces every artist area uses, whatever the land: the plaque (name, songs, years, genres, who
    /// added them), walls of album covers, and a glowing marker. Each land kit decides what they're
    /// mounted on (a lantern post, a cabin wall, a statue plinth...).
    public static class ArtistBits
    {
        static Font font;
        static Material textMaterial;
        static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// Text that hides behind walls and trees and can't be seen from behind (shader: Everything World/World Text).
        public static Material TextMaterial
        {
            get
            {
                if (textMaterial != null) return textMaterial;
                var shader = Shader.Find("Everything World/World Text");
                if (shader == null) return textMaterial = Font.material;
                textMaterial = new Material(shader) { name = "World text", mainTexture = Font.material.mainTexture };
                // the font's texture is rebuilt when new letters are needed; keep pointing at the current one
                Font.textureRebuilt += f => { if (f == Font && textMaterial != null) textMaterial.mainTexture = f.material.mainTexture; };
                return textMaterial;
            }
        }

        /// Text in the world, 'height' metres tall per line, readable from the side 'facing' points to.
        public static TextMesh Label(Transform parent, string text, Vector3 pos, Vector3 facing, float height, Color color,
                                     TextAnchor anchor = TextAnchor.UpperCenter)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-facing, Vector3.up));
            var t = go.AddComponent<TextMesh>();
            t.font = Font;
            t.fontSize = 96;
            t.characterSize = height / 9.6f; // a line of a 96-point TextMesh is about 9.6 units at characterSize 1
            t.anchor = anchor;
            t.alignment = anchor == TextAnchor.UpperLeft || anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
            t.color = color;
            t.text = text;
            go.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial;
            return t;
        }

        static string Clip(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s.Substring(0, max - 1) + "…";

        /// The lines under an artist's name: songs and years, genres, who added them, artist radio.
        public static string Details(IslandWorldData.Artist a)
        {
            var lines = new List<string>();
            if (a.songs == 1 && a.songList != null && a.songList.Length == 1) lines.Add("\u201c" + Clip(a.songList[0].title, 32) + "\u201d");
            var songs = a.songs == 1 ? "1 song" : $"{a.songs} songs";
            lines.Add(string.IsNullOrEmpty(a.years) ? songs : $"{songs} · {a.years.Replace("-", "–")}");
            if (!string.IsNullOrEmpty(a.genres)) lines.Add(Clip(a.genres, 34));
            if (!string.IsNullOrEmpty(a.addedBy)) lines.Add("Added by " + Clip(a.addedBy, 30));
            if (a.station) lines.Add("♪ Artist radio: step inside");
            return string.Join("\n", lines);
        }

        /// A plaque board on two short posts. 'front' is the direction it faces (toward visitors).
        public static void Plaque(MeshKit mk, MeshKit colliders, Transform labels, IslandWorldData.Artist a, Vector3 groundPos,
                                  Vector3 front, Material wood, Material board, Color ink, float width = 1.5f)
        {
            front = Vector3.ProjectOnPlane(front, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, front);
            var rot = Quaternion.LookRotation(front);
            const float h = 1.05f;
            var center = groundPos + Vector3.up * 1.25f;
            foreach (var s in new[] { -1f, 1f })
                mk.Prism(wood, groundPos + right * (width / 2f - .08f) * s - front * .05f, Vector3.up, .06f, 1.85f, 5);
            mk.Box(board, center, new Vector3(width, h, .07f), rot);
            colliders.Box(board, center, new Vector3(width, h, .2f), rot);
            var face = center + front * .045f;
            Label(labels, Clip(a.name, 26), face + Vector3.up * (h / 2f - .1f), front, .15f, ink);
            Label(labels, Details(a), face + Vector3.up * (h / 2f - .32f), front, .075f, ink);
        }

        /// Album covers in a grid on a wall. 'normal' is the way the wall faces; titles go under each cover.
        public static void CoverWall(MeshKit pictures, Transform labels, LandContext ctx, IslandWorldData.Artist a, Vector3 center,
                                     Vector3 normal, int perRow, float size, int max, bool titles, Color ink)
        {
            if (a.songList == null || a.songList.Length == 0) return;
            normal = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
            var right = Vector3.Cross(normal, Vector3.up); // the viewer's right, facing the wall
            var mat = WorldMaterials.Picture(ctx.CoverAtlas);
            var blank = WorldMaterials.Solid(new Color(.14f, .13f, .15f));
            var n = Mathf.Min(max, a.songList.Length);
            var rows = Mathf.CeilToInt(n / (float)perRow);
            float gapX = size * 1.25f, gapY = size * (titles ? 1.45f : 1.2f);
            for (var i = 0; i < n; i++)
            {
                int row = i / perRow, col = i % perRow;
                var inRow = Mathf.Min(perRow, n - row * perRow);
                var p = center + right * ((col - (inRow - 1) / 2f) * gapX) + Vector3.up * (((rows - 1) / 2f - row) * gapY) + normal * .02f;
                var song = a.songList[i];
                if (song.cover >= 0)
                    pictures.Picture(mat, p, right * size / 2f, Vector3.up * size / 2f, normal, ctx.CoverRect(song.cover));
                else
                    pictures.Picture(blank, p, right * size / 2f, Vector3.up * size / 2f, normal, new Rect(0, 0, 1, 1));
                if (titles) Label(labels, Clip(song.title, 22), p - Vector3.up * (size / 2f + .04f) + normal * .01f, normal, size * .11f, ink);
            }
        }

        /// A glowing lantern hanging from a post: returns the glass, so the ArtistArea can brighten it.
        public static Renderer LanternPost(MeshKit mk, Transform parent, Vector3 groundPos, Vector3 armDir, Material wood, Material iron,
                                           Color glass, float height = 2.7f)
        {
            armDir = Vector3.ProjectOnPlane(armDir, Vector3.up).normalized;
            mk.Prism(wood, groundPos, Vector3.up, .09f, height, 6);
            PostCollider(parent, groundPos, height);
            mk.Box(wood, groundPos + Vector3.up * (height - .12f) + armDir * .35f, new Vector3(.08f, .08f, .8f), Quaternion.LookRotation(armDir));
            var hang = groundPos + Vector3.up * (height - .55f) + armDir * .65f;
            mk.Box(iron, hang + Vector3.up * .25f, new Vector3(.34f, .06f, .34f), Quaternion.identity);
            mk.Box(iron, hang - Vector3.up * .22f, new Vector3(.3f, .05f, .3f), Quaternion.identity);
            return GlassBox(parent, hang, new Vector3(.24f, .38f, .24f), glass);
        }

        /// An invisible pole that stops you walking through a post.
        public static void PostCollider(Transform parent, Vector3 groundPos, float height, float radius = .2f)
        {
            var go = new GameObject("Post collider");
            go.transform.SetParent(parent, false);
            go.transform.position = groundPos + Vector3.up * height / 2f;
            var c = go.AddComponent<CapsuleCollider>();
            c.radius = radius; c.height = height; c.direction = 1;
            go.isStatic = true;
        }

        /// A separate glowing box (lantern glass, candle flame), so it can be brightened on its own.
        public static Renderer GlassBox(Transform parent, Vector3 center, Vector3 size, Color glass, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Glow";
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            if (rotation.HasValue) go.transform.rotation = rotation.Value;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = WorldMaterials.Glow(glass, .9f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        /// A warm point light.
        /// indoors = true makes it cast shadows, so it stays inside the walls (costs more: use for rooms only).
        public static Light PointLight(Transform parent, Vector3 pos, Color color, float range, float intensity, bool indoors = false)
        {
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.color = color; l.range = range; l.intensity = intensity;
            l.shadows = indoors ? LightShadows.Soft : LightShadows.None;
            if (indoors) { l.shadowStrength = .95f; l.shadowNearPlane = .2f; }
            return l;
        }
    }
}
