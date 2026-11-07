using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Simple first person controller.
// WASD or arrow keys to move, mouse to look, Left Shift to run, Space to jump.
// Esc frees the cursor; click in the Game view to lock it again.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
  [Header("Movement")]
  public float walkSpeed = 2.0f;
  public float runSpeed = 4.0f;
  public float jumpHeight = 0.6f;
  public float gravity = -9.81f;

  [Header("Look")]
  public float mouseSensitivity = 0.15f;
  public float maxLookAngle = 85f;
  public Transform cameraTransform;
  [Tooltip("Vertical field of view in degrees. 75 is roughly 105 degrees horizontal on a 16:9 screen.")]
  public float fieldOfView = 75f;
  [Tooltip("Keep this smaller than the Character Controller radius to stop the camera seeing through walls.")]
  public float nearClip = 0.05f;

  CharacterController controller;
  float pitch;
  float verticalVelocity;

  void Start()
  {
    controller = GetComponent<CharacterController>();
    if (cameraTransform == null)
    {
      var cam = GetComponentInChildren<Camera>();
      if (cam != null) cameraTransform = cam.transform;
    }
    ApplyCameraSettings();
    LockCursor(true);
  }

  void Update()
  {
    ReadInput(out Vector2 move, out Vector2 look, out bool run, out bool jump,
              out bool escape, out bool click);

    if (escape) LockCursor(false);
    if (click && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);

    // Look around only while the cursor is locked
    if (Cursor.lockState == CursorLockMode.Locked)
    {
      transform.Rotate(0f, look.x * mouseSensitivity, 0f);
      pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -maxLookAngle, maxLookAngle);
      if (cameraTransform != null)
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    // Move relative to where the player is facing
    Vector3 dir = transform.right * move.x + transform.forward * move.y;
    if (dir.sqrMagnitude > 1f) dir.Normalize();
    float speed = run ? runSpeed : walkSpeed;

    if (controller.isGrounded)
    {
      if (verticalVelocity < 0f) verticalVelocity = -2f;
      if (jump) verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
    verticalVelocity += gravity * Time.deltaTime;

    Vector3 velocity = dir * speed + Vector3.up * verticalVelocity;
    controller.Move(velocity * Time.deltaTime);
  }

  void ReadInput(out Vector2 move, out Vector2 look, out bool run, out bool jump,
                 out bool escape, out bool click)
  {
    move = Vector2.zero; look = Vector2.zero;
    run = jump = escape = click = false;

#if ENABLE_INPUT_SYSTEM
    var kb = Keyboard.current;
    var mouse = Mouse.current;
    if (kb != null)
    {
      if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
      if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
      if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
      if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
      run = kb.leftShiftKey.isPressed;
      jump = kb.spaceKey.wasPressedThisFrame;
      escape = kb.escapeKey.wasPressedThisFrame;
    }
    if (mouse != null)
    {
      look = mouse.delta.ReadValue();
      click = mouse.leftButton.wasPressedThisFrame;
    }
#else
        move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        // Old Input Manager reports mouse movement in smaller units, so scale it up
        look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
        run = Input.GetKey(KeyCode.LeftShift);
        jump = Input.GetButtonDown("Jump");
        escape = Input.GetKeyDown(KeyCode.Escape);
        click = Input.GetMouseButtonDown(0);
#endif
  }

  void ApplyCameraSettings()
  {
    if (cameraTransform == null) return;
    var cam = cameraTransform.GetComponent<Camera>();
    if (cam == null) return;
    cam.fieldOfView = fieldOfView;
    cam.nearClipPlane = nearClip;
  }

  // Lets you adjust the FOV in the Inspector while playing
  void OnValidate()
  {
    if (Application.isPlaying) ApplyCameraSettings();
  }

  void LockCursor(bool locked)
  {
    Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    Cursor.visible = !locked;
  }
}