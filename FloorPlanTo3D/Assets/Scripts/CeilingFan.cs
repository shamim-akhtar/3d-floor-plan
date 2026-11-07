using UnityEngine;

// Spins the blades of a ceiling fan. LivingCeilingDesigner adds this to each fan it builds.
public class CeilingFan : MonoBehaviour
{
    public Transform rotor;
    public float rpm = 120f;
    public bool spinning = true;

    float speed;   // current degrees per second, eases up and down

    void Update()
    {
        if (rotor == null) return;
        float target = spinning ? rpm * 6f : 0f;   // rpm to degrees per second
        speed = Mathf.MoveTowards(speed, target, 360f * Time.deltaTime);
        rotor.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }
}
