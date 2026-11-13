using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Player
{
    /// Minimal first-person walker for testing: WASD to move, mouse to look, Shift to run, Space to jump,
    /// Esc to free the cursor, click to capture it again.
    /// F turns flying on and off: you fly where you look, straight through things, Space to rise, Ctrl (or C) to
    /// sink, Shift for a burst of extra speed. Turn it off and you drop back to the ground.
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        public Transform cameraPivot;
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        public float mouseSensitivity = 0.1f;
        public float gravity = -20f;
        public float jumpHeight = 1.2f;   // metres: enough to hop onto a bench or a low wall
        [Tooltip("Flying speed in metres per second.")]
        public float flySpeed = 70f;
        [Tooltip("Flying speed with Shift held, in metres per second.")]
        public float flyBoostSpeed = 220f;
        [Tooltip("How hard you shove loose things you walk into, in newtons.")]
        public float pushForce = 900f;

        CharacterController controller;
        float pitch;
        float verticalVelocity;
        Vector3 flyVelocity;
        GUIStyle hudStyle;

        /// True while flying (IslandExplorer leaves you be: no wading limit, and it keeps you above the ground).
        public bool Flying { get; private set; }

        /// True while sitting on a seat (PlayerInteractor): you can look around but not walk.
        public bool Seated { get; private set; }
        float standEye = -1f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (GetComponent<PlayerInteractor>() == null) gameObject.AddComponent<PlayerInteractor>();
            if (GetComponent<Sunglasses>() == null) gameObject.AddComponent<Sunglasses>();
        }

        /// Sits down on the seat surface, eyes about 0.75 m above it, facing out.
        public void Sit(Vector3 seat, Vector3 facing, float seatHeight)
        {
            if (Flying) SetFlying(false);
            Seated = true; controller.enabled = false; verticalVelocity = 0f;
            transform.SetPositionAndRotation(seat, Quaternion.LookRotation(facing));   // the body's middle on the seat
            if (cameraPivot != null) { if (standEye < 0f) standEye = cameraPivot.localPosition.y; var lp = cameraPivot.localPosition; lp.y = .75f; cameraPivot.localPosition = lp; }
            pitch = 0f;
        }

        /// Stands up at 'feet'.
        public void Stand(Vector3 feet)
        {
            Seated = false;
            if (cameraPivot != null && standEye >= 0f) { var lp = cameraPivot.localPosition; lp.y = standEye; cameraPivot.localPosition = lp; }
            transform.position = feet + Vector3.up * (controller.height / 2f + .05f); controller.enabled = true;
        }

        void Start() => SetCursorLocked(true);

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            if (mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);
            if (keyboard.fKey.wasPressedThisFrame && !Seated) SetFlying(!Flying);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var look = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
                if (cameraPivot != null) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }

            if (Seated) return;   // look around, but stay put

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            if (Flying) { Fly(keyboard, input); return; }

            var speed = keyboard.leftShiftKey.isPressed ? runSpeed : walkSpeed;
            var move = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded)
                verticalVelocity = keyboard.spaceKey.wasPressedThisFrame ? Mathf.Sqrt(2f * jumpHeight * -gravity) : -1f;
            else
            {
                if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;   // bumped your head
                verticalVelocity += gravity * Time.deltaTime;
            }
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }

        /// Flying: forward is where the camera looks, so you climb by looking up; Space and Ctrl go straight up and
        /// down. Speeds up and slows down smoothly so it doesn't jerk.
        void Fly(Keyboard keyboard, Vector2 input)
        {
            var look = cameraPivot != null ? cameraPivot.forward : transform.forward;
            var dir = transform.right * input.x + look * input.y;
            if (keyboard.spaceKey.isPressed) dir += Vector3.up;
            if (keyboard.leftCtrlKey.isPressed || keyboard.cKey.isPressed) dir += Vector3.down;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            var speed = keyboard.leftShiftKey.isPressed ? flyBoostSpeed : flySpeed;
            flyVelocity = Vector3.Lerp(flyVelocity, dir * speed, 1f - Mathf.Exp(-6f * Time.deltaTime));
            transform.position += flyVelocity * Time.deltaTime;
        }

        /// Turns flying on or off. While flying the character controller is off, so you pass through everything.
        public void SetFlying(bool on)
        {
            if (on == Flying) return;
            Flying = on;
            controller.enabled = !on;
            flyVelocity = Vector3.zero; verticalVelocity = 0f;
        }

        /// Walking into something loose (a ball of twine, a dropped duck) gives it a shove.
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic || hit.moveDirection.y < -.3f) return;
            var push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            rb.AddForceAtPosition(push * pushForce, hit.point, ForceMode.Force);
        }

        void OnGUI()
        {
            if (!Flying) return;
            hudStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect(Screen.width / 2f - 230, 12, 460, 30), "Flying  ·  Space up, Ctrl down, Shift faster  ·  F to land", hudStyle);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
