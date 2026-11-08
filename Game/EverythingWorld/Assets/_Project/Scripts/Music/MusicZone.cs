using UnityEngine;

namespace EverythingWorld.Music
{
    /// An area of the world tied to one music scene.
    /// Needs a collider with "Is Trigger" ticked; the player walking in or out is reported to the MusicDirector.
    [RequireComponent(typeof(Collider))]
    public class MusicZone : MonoBehaviour
    {
        [Tooltip("Scene id from scenes.json (0-58). See review/index.html for what each scene contains.")]
        public int sceneId;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) MusicDirector.Instance?.EnterZone(this);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) MusicDirector.Instance?.ExitZone(this);
        }
    }
}
