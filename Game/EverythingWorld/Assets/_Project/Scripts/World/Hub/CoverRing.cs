using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Everything Hub's ring of album covers: every song in the order it was added, spiralling up round
    /// the mast and turning slowly. The cover of the song on the radio right now lights up.
    public class CoverRing : MonoBehaviour
    {
        public float degreesPerSecond = .12f;           // one full turn in 50 minutes: slow enough to look at a cover
        public Dictionary<string, Vector3> slotOf = new Dictionary<string, Vector3>(); // track id -> local position
        public Dictionary<string, Vector3> facingOf = new Dictionary<string, Vector3>();
        public Transform highlight;
        public Light highlightLight;
        public Material highlightMaterial;
        public Color highlightColor = Color.white;
        public Transform beam;          // a column of light from under the lit cover down to the ground
        public float beamDrop;
        string shownId;

        void Update()
        {
            transform.Rotate(0, degreesPerSecond * Time.deltaTime, 0, Space.Self);
            var d = MusicDirector.Instance;
            var id = d != null && d.CurrentSlot.IsValid ? d.CurrentSlot.track.id : null;
            if (id == shownId || highlight == null) return;
            shownId = id;
            if (id != null && slotOf.TryGetValue(id, out var local))
            {
                highlight.gameObject.SetActive(true);
                highlight.localPosition = local;
                highlight.localRotation = Quaternion.LookRotation(facingOf[id]);
                if (beam != null) beam.localScale = new Vector3(1f, Mathf.Max(.1f, local.y - beamDrop), 1f);
            }
            else highlight.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (highlightLight != null && highlight != null && highlight.gameObject.activeSelf)
            {
                var pulse = Mathf.Sin(Time.time * 3f);
                highlightLight.intensity = 3f + pulse * 1.2f;
                if (highlightMaterial != null) highlightMaterial.SetColor("_EmissionColor", highlightColor * (3f + pulse * 1.8f));
            }
        }
    }
}
