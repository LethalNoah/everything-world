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

        CharacterController controller;
        float pitch;
        float verticalVelocity;
        Vector3 flyVelocity;
        GUIStyle hudStyle;

        /// True while flying (IslandExplorer leaves you be: no wading limit, and it keeps you above the ground).
        public bool Flying { get; private set; }

        void Awake() => controller = GetComponent<CharacterController>();

        void Start() => SetCursorLocked(true);

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            if (mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);
            if (keyboard.fKey.wasPressedThisFrame) SetFlying(!Flying);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                var look = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
                if (cameraPivot != null) cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }

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
