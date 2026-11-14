using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Small shared tools for laying a land out on purpose (see the build-land skill): a local frame for building a
    /// place, smooth roads through waypoints, nudging a chosen site onto good ground, photo-tour marks.
    public struct PlaceFrame
    {
        public Vector3 o; public Quaternion r;
        public PlaceFrame(Vector3 origin, Vector3 face) { o = origin; face.y = 0; r = Quaternion.LookRotation(face.sqrMagnitude > .0001f ? face.normalized : Vector3.forward); }
        /// A point in the place's own space: x to its right, y up, z the way it faces.
        public Vector3 P(float x, float y, float z) => o + r * new Vector3(x, y, z);
        public Vector3 Fwd => r * Vector3.forward;
        public Vector3 Right => r * Vector3.right;
    }

    public static class PlaceTools
    {
        public static Vector3 FaceTo(Vector3 from, Vector3 to) { var d = to - from; d.y = 0; return d.sqrMagnitude > .01f ? d.normalized : Vector3.forward; }

        /// A smooth line through the waypoints (Catmull-Rom), a point every 'step' metres, kept on this land.
        public static List<Vector3> Smooth(LandContext ctx, IList<Vector2> way, bool closed, float step = 2f, float inland = 3f)
        {
            var pts = new List<Vector3>(); var n = way.Count; var segs = closed ? n : n - 1;
            for (var i = 0; i < segs; i++)
            {
                Vector2 p0 = way[closed ? (i - 1 + n) % n : Mathf.Max(i - 1, 0)], p1 = way[i], p2 = way[(i + 1) % n], p3 = way[closed ? (i + 2) % n : Mathf.Min(i + 2, n - 1)];
                var k = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));
                for (var j = 0; j < k; j++)
                {
                    var t = j / (float)k; var t2 = t * t; var t3 = t2 * t;
                    var q = .5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                    if (ctx.IsLand(q.x, q.y, inland)) pts.Add(ctx.OnGround(q.x, q.y));
                }
            }
            if (closed && pts.Count > 0) pts.Add(pts[0]);
            else if (!closed && ctx.IsLand(way[n - 1].x, way[n - 1].y, inland)) pts.Add(ctx.OnGround(way[n - 1].x, way[n - 1].y));
            return pts;
        }

        /// The chosen site if it is good ground ('inland' metres inside the land and passing 'ok'), else the nearest
        /// spot that is.
        public static Vector3 Site(LandContext ctx, Vector2 want, float inland, System.Func<float, float, bool> ok = null)
        {
            bool Good(float x, float z) => ctx.IsLand(x, z, inland) && (ok == null || ok(x, z));
            if (Good(want.x, want.y)) return ctx.OnGround(want.x, want.y);
            for (var r = 6f; r < 240f; r += 6f)
                for (var a = 0f; a < 360f; a += 20f)
                {
                    var x = want.x + Mathf.Cos(a * Mathf.Deg2Rad) * r; var z = want.y + Mathf.Sin(a * Mathf.Deg2Rad) * r;
                    if (Good(x, z)) return ctx.OnGround(x, z);
                }
            return ctx.OnGround(want.x, want.y);
        }

        /// A mark the photo tour takes a picture from ("Shot: name").
        public static void Shot(LandContext ctx, string name, Vector3 cam, Vector3 look)
        {
            var mark = new GameObject("Shot: " + name).transform; mark.SetParent(ctx.Root, false);
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation((look - cam).normalized));
        }

        /// The nearest of 'pts' to p (flat distance).
        public static Vector3 Nearest(List<Vector3> pts, Vector3 p)
        {
            var best = pts.Count > 0 ? pts[0] : p; var bd = float.MaxValue;
            foreach (var q in pts) { var d = (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z); if (d < bd) { bd = d; best = q; } }
            return best;
        }

        /// Words that appear over something for a few seconds (a phone answered, a cat stroked).
        public static void Say(Transform parent, Vector3 at, string text, float seconds = 5.5f)
        {
            var w = parent.Find("Words"); var bubble = w != null ? w.GetComponent<SpeechBubble>() : null;
            if (bubble == null) bubble = SpeechBubble.Make(parent, "Words");
            bubble.Show(SpeechBubble.Wrap(text, 34), at, seconds);
        }

        /// A box collider on its own object (for things built into a shared mesh).
        public static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }
    }

    /// Something that swings when nudged and settles again (a tyre swing, a bell, a porch swing): call Push().
    public class Sway : MonoBehaviour
    {
        public Vector3 axis = Vector3.right; public float degrees = 30f, speed = 2.4f, decay = .3f, seconds = 14f;
        float t = -1f; Quaternion rest;
        void Start() { rest = transform.localRotation; }
        public void Push() { t = 0f; }
        void Update()
        {
            if (t < 0f) return; t += Time.deltaTime;
            transform.localRotation = rest * Quaternion.AngleAxis(Mathf.Sin(t * speed) * degrees * Mathf.Exp(-t * decay), axis);
            if (t > seconds) { t = -1f; transform.localRotation = rest; }
        }
    }

    /// Hides its object when its time runs out (PlaceTools.Say).
    public class FadingWords : MonoBehaviour
    {
        public float left;
        void Update() { left -= Time.deltaTime; if (left <= 0f) gameObject.SetActive(false); }
    }
}
