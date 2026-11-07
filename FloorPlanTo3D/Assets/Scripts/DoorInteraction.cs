using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Put this on the Player. Look at a door and press E (or left-click) to open or close it.
public class DoorInteraction : MonoBehaviour
{
    public Transform cameraTransform;
    public float reach = 2.5f;

    void Start()
    {
        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    void Update()
    {
        if (cameraTransform == null || !Pressed()) return;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, reach))
        {
            var door = hit.collider.GetComponentInParent<Door>();
            if (door != null) door.Toggle();
        }
    }

    bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                     && Cursor.lockState == CursorLockMode.Locked;
        return key || click;
#else
        return Input.GetKeyDown(KeyCode.E)
               || (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked);
#endif
    }
}
