using UnityEngine;

namespace EverythingWorld.World
{
    /// Turns a sign to face the camera. With fadeWithFog, the sign fades out as it disappears into the
    /// current land's fog, and also when you're right underneath it, so far-off names don't crowd the sky.
    public class Billboard : MonoBehaviour
    {
        public bool fadeWithFog;
        public float hideWithin = 120f;

        TextMesh text;
        Renderer rend;
        Color baseColor;

        void Start()
        {
            text = GetComponent<TextMesh>();
            rend = GetComponent<Renderer>();
            if (text != null) baseColor = text.color;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var toSign = transform.position - cam.transform.position;
            transform.rotation = Quaternion.LookRotation(toSign);
            if (!fadeWithFog || text == null) return;

            var d = toSign.magnitude;
            var fogEnd = RenderSettings.fog ? RenderSettings.fogEndDistance : 3000f;
            var far = 1f - Mathf.InverseLerp(fogEnd * .45f, fogEnd * .85f, d);
            var near = Mathf.InverseLerp(hideWithin, hideWithin * 1.6f, d);
            var a = Mathf.Clamp01(Mathf.Min(far, near));
            rend.enabled = a > .01f;
            if (rend.enabled) text.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * a);
        }
    }
}
