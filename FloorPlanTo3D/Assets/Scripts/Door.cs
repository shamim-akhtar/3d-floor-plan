using UnityEngine;

// Swings a door open and closed around its hinge.
// DoorBuilder adds this to each door's Hinge object automatically.
public class Door : MonoBehaviour
{
    public float openAngle = -90f;   // degrees; the sign sets which way it swings
    public float speed = 180f;       // degrees per second
    public bool isOpen;

    float closedYaw;
    float current;

    void Awake()
    {
        closedYaw = transform.localEulerAngles.y;
        current = isOpen ? openAngle : 0f;
    }

    public void Toggle()
    {
        isOpen = !isOpen;
    }

    void Update()
    {
        float target = isOpen ? openAngle : 0f;
        if (Mathf.Approximately(current, target)) return;
        current = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0f, closedYaw + current, 0f);
    }
}
