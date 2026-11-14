using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Player
{
    /// Look at something within reach and press E: sit on a seat, play with a toy, pick up a carryable.
    /// While seated, E stands you up again. While carrying, E puts the thing down and a click throws it.
    /// A small prompt shows what E will do. Added to the player by SimplePlayerController.
    public class PlayerInteractor : MonoBehaviour
    {
        public float reach = 3.2f;
        public float throwSpeed = 13f;

        SimplePlayerController walker; Transform eye;
        Interactable looking; Seat seated; Carryable held;
        GUIStyle style;

        void Start()
        {
            walker = GetComponent<SimplePlayerController>();
            eye = walker != null && walker.cameraPivot != null ? walker.cameraPivot : transform;
            if (GetComponent<InteractCues>() == null) gameObject.AddComponent<InteractCues>();   // the coloured glows that show what can be sat on, used, picked up or talked to
        }

        void Update()
        {
            var kb = Keyboard.current; var mouse = Mouse.current; if (kb == null || eye == null) return;
            looking = null;
            if (seated == null && held == null && !(walker != null && walker.Flying))
            {
                var hits = Physics.RaycastAll(eye.position, eye.forward, reach, ~0, QueryTriggerInteraction.Collide);
                var best = float.MaxValue;
                foreach (var h in hits)
                {
                    if (h.collider.transform.IsChildOf(transform)) continue;
                    var it = h.collider.GetComponentInParent<Interactable>();
                    if (it == null) { if (!h.collider.isTrigger && h.distance + 1f < best) { best = h.distance + 1f; looking = null; } continue; }   // something solid well in the way (a seat just inside a car still counts)
                    if (h.distance < best) { best = h.distance; looking = it; }
                }
            }
            if (seated != null && walker != null) walker.transform.position = seated.transform.position;   // a seat that moves (a gondola) carries you
            if (kb.eKey.wasPressedThisFrame)
            {
                if (seated != null) StandUp();
                else if (held != null) Drop(0f);
                else if (looking != null) looking.Use(this);
            }
            if (held != null)
            {
                var hold = eye.position + eye.forward * 1.5f - Vector3.up * .25f;
                var rb = held.GetComponent<Rigidbody>();
                rb.MovePosition(Vector3.Lerp(held.transform.position, hold, 1f - Mathf.Exp(-18f * Time.deltaTime)));
                rb.MoveRotation(Quaternion.Slerp(held.transform.rotation, Quaternion.LookRotation(Flat(eye.forward)), 1f - Mathf.Exp(-8f * Time.deltaTime)));
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked) Drop(throwSpeed);
            }
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > .001f ? v.normalized : Vector3.forward; }

        public void SitOn(Seat s)
        {
            if (walker == null) return;
            seated = s; walker.Sit(s.transform.position, s.facing, s.seatHeight);
        }

        void StandUp()
        {
            var stand = seated.StandPoint; seated = null;
            walker.Stand(stand);
        }

        public void PickUp(Carryable c)
        {
            held = c; c.Held = true;
            var rb = c.GetComponent<Rigidbody>(); rb.isKinematic = true;
            foreach (var col in c.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(col, GetComponent<CharacterController>(), true);
        }

        void Drop(float speed)
        {
            var c = held; held = null; c.Held = false;
            var rb = c.GetComponent<Rigidbody>(); rb.isKinematic = false;
            rb.linearVelocity = speed > 0f ? (eye.forward + Vector3.up * .15f).normalized * speed : Vector3.zero;
            rb.angularVelocity = speed > 0f ? Random.insideUnitSphere * 4f : Vector3.zero;
            var cc = GetComponent<CharacterController>();
            foreach (var col in c.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(col, cc, false);
        }

        void OnGUI()
        {
            string text = null;
            if (seated != null) return;   // standing up with E goes without saying
            else if (held != null) text = "E  Put down   ·   Click  Throw";
            else if (looking != null) text = "E  " + looking.Prompt;
            if (text == null) return;
            style ??= GameFonts.Box(18, TextAnchor.MiddleCenter);
            // the prompt takes the colour of its cue (blue sit, yellow use, white pick up, pink talk)
            style.normal.textColor = seated != null ? InteractCues.SitColor : held != null ? InteractCues.CarryColor : Color.Lerp(InteractCues.ColorOf(looking), Color.white, .25f);
            GUI.Box(new Rect(Screen.width / 2f - 160, Screen.height / 2f + 40, 320, 32), text, style);
        }
    }
}
