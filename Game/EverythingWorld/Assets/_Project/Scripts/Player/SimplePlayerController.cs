using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Player
{
    /// Minimal first-person walker for testing: WASD to move, mouse to look, Shift to run, Space to jump,
    /// Esc to free the cursor, click to capture it again.
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        public Transform cameraPivot;
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        public float mouseSensitivity = 0.1f;
        public float gravity = -20f;
        public float jumpHeight = 1.2f;   // metres: enough to hop onto a bench or a low wall

        CharacterController controller;
        float pitch;
        float verticalVelocity;

        void Awake() => controller = GetComponent<CharacterController>();

        void Start() => SetCursorLocked(true);

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            if (mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);

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

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
