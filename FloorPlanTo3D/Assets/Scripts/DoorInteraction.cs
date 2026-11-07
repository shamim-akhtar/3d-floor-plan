using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Put this on the Player. Look at a door and press E (or left-click) to open or close it.
// A small dot shows the centre of the screen, and a prompt appears when a door is in reach.
public class DoorInteraction : MonoBehaviour
{
    public Transform cameraTransform;
    public float reach = 2.5f;
    public bool showPrompt = true;
    public bool logHits = false;   // tick to print what the ray hits in the Console

    Door target;

    void Start()
    {
        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }
        if (cameraTransform == null)
            Debug.LogWarning("DoorInteraction: no camera found. Assign Camera Transform in the Inspector.");
    }

    void Update()
    {
        if (cameraTransform == null) return;

        target = FindDoor();

        if (Pressed() && target != null)
            target.Toggle();
    }

    Door FindDoor()
    {
        // Check everything along the ray, nearest first, and skip the player's own colliders
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);
        var hits = Physics.RaycastAll(ray, reach, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (logHits) Debug.Log("Door ray hit: " + hit.collider.name);

            // The leaf is under the Hinge (which has the Door component).
            // The frame is beside it, so also look through the door's own children.
            var door = hit.collider.GetComponentInParent<Door>();
            if (door == null && hit.collider.transform.parent != null)
                door = hit.collider.transform.parent.GetComponentInChildren<Door>();
            if (door != null) return door;

            return null;   // something else (a wall) is in the way
        }

        // Nothing hit directly: accept the nearest door just in front of the player,
        // but only when looking roughly level (not up at the ceiling or down at the floor)
        if (Mathf.Abs(cameraTransform.forward.y) > 0.5f) return null;
        Door best = null;
        float bestDist = 1.5f;
        foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            Vector3 to = d.transform.position + Vector3.up * 1f - cameraTransform.position;
            float dist = to.magnitude;
            if (dist < bestDist && Vector3.Dot(cameraTransform.forward, to.normalized) > 0.5f)
            {
                best = d;
                bestDist = dist;
            }
        }
        return best;
    }

    bool Pressed()
    {
        if (FirstPersonController.InputBlocked) return false;
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

    void OnGUI()
    {
        if (!showPrompt || FirstPersonController.InputBlocked) return;
        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        GUI.Label(new Rect(cx - 4, cy - 10, 20, 20), "•");
        if (target != null)
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
            string text = target.isOpen ? "Press E to close" : "Press E to open";
            GUI.Label(new Rect(cx - 100, cy + 20, 200, 30), text, style);
        }
    }
}
