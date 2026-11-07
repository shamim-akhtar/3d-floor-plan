using UnityEngine;

// Swings a door open and closed around its hinge.
// DoorBuilder adds this to each door's Hinge object automatically.
// Press E while looking at a door to open it, and again to close it.
// With Auto Close on, an open door also closes by itself once the player walks away.
public class Door : MonoBehaviour
{
    public float openAngle = -90f;   // degrees; the sign sets which way it swings
    public float speed = 180f;       // degrees per second
    public bool isOpen;

    [Header("Auto close")]
    public bool autoClose = true;
    public float closeDelay = 3f;      // seconds after the player has moved away
    public float clearDistance = 2f;   // how far the player must be from the door

    float closedYaw;
    float current;
    float awayTimer;
    Transform player;

    void Awake()
    {
        closedYaw = transform.localEulerAngles.y;
        current = isOpen ? openAngle : 0f;
    }

    void Start()
    {
        var interaction = FindAnyObjectByType<DoorInteraction>();
        if (interaction != null) player = interaction.transform;
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        awayTimer = 0f;
    }

    public void Close()
    {
        isOpen = false;
        awayTimer = 0f;
    }

    void Update()
    {
        if (autoClose && isOpen && player != null)
        {
            // Measure from the middle of the doorway, at floor level
            Vector3 p = player.position; p.y = transform.position.y;
            if (Vector3.Distance(p, transform.position) > clearDistance)
            {
                awayTimer += Time.deltaTime;
                if (awayTimer >= closeDelay) Close();
            }
            else awayTimer = 0f;
        }

        float target = isOpen ? openAngle : 0f;
        if (Mathf.Approximately(current, target)) return;
        current = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0f, closedYaw + current, 0f);
    }
}
