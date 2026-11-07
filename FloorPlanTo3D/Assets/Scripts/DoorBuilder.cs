using UnityEngine;

// Builds doors in the door openings of the floor plan: frame, door leaf, handles,
// and the wall above the door. Put this on the same GameObject as FloorPlanBuilder.
// Each door gets a Door component so it can be opened in Play mode (see DoorInteraction).
// Units are metres, using the same coordinates as FloorPlanBuilder.
public class DoorBuilder : MonoBehaviour
{
    public enum DoorType { Main, Room, Bathroom, Shelter, ServiceYard }

    [System.Serializable]
    public class DoorData
    {
        public string name;
        public DoorType type;
        public Vector2 start;        // (x, z) of one side of the opening
        public Vector2 end;          // (x, z) of the other side
        public bool hingeAtStart;    // which side the hinges are on
        public Vector2 opensTowards; // direction the door swings into, e.g. (0, 1) = +Z
        public float wallThickness;
    }

    [Header("Materials")]
    public Material mainDoorMaterial;
    public Material roomDoorMaterial;
    public Material bathroomDoorMaterial;
    public Material shelterDoorMaterial;
    public Material frameMaterial;
    public Material handleMaterial;

    [Header("Sizes")]
    public float doorHeight = 2.1f;
    public float frameWidth = 0.05f;
    public float leafThickness = 0.04f;

    public DoorData[] doors =
    {
        new DoorData { name = "Main entrance door", type = DoorType.Main,
            start = new Vector2(1.67f, 1.975f), end = new Vector2(2.80f, 1.975f),
            hingeAtStart = true, opensTowards = new Vector2(0, 1), wallThickness = 0.2f },
        new DoorData { name = "Household shelter door", type = DoorType.Shelter,
            start = new Vector2(1.52f, 3.965f), end = new Vector2(1.52f, 3.115f),
            hingeAtStart = true, opensTowards = new Vector2(1, 0), wallThickness = 0.3f },
        new DoorData { name = "Bedroom 3 door", type = DoorType.Room,
            start = new Vector2(7.48f, 5.7f), end = new Vector2(8.47f, 5.7f),
            hingeAtStart = false, opensTowards = new Vector2(0, 1), wallThickness = 0.1f },
        new DoorData { name = "Bedroom 2 door", type = DoorType.Room,
            start = new Vector2(8.58f, 5.7f), end = new Vector2(9.69f, 5.7f),
            hingeAtStart = true, opensTowards = new Vector2(0, 1), wallThickness = 0.1f },
        new DoorData { name = "Main bedroom door", type = DoorType.Room,
            start = new Vector2(10.04f, 5.7f), end = new Vector2(10.04f, 4.59f),
            hingeAtStart = true, opensTowards = new Vector2(1, 0), wallThickness = 0.1f },
        new DoorData { name = "Bath/WC 2 door", type = DoorType.Bathroom,
            start = new Vector2(8.12f, 4.59f), end = new Vector2(8.82f, 4.59f),
            hingeAtStart = true, opensTowards = new Vector2(0, -1), wallThickness = 0.1f },
        new DoorData { name = "Bath/WC 1 door", type = DoorType.Bathroom,
            start = new Vector2(11.0f, 4.59f), end = new Vector2(11.7f, 4.59f),
            hingeAtStart = false, opensTowards = new Vector2(0, -1), wallThickness = 0.1f },
        new DoorData { name = "Service yard door", type = DoorType.ServiceYard,
            start = new Vector2(7.85f, 1.655f), end = new Vector2(7.85f, 0.705f),
            hingeAtStart = true, opensTowards = new Vector2(1, 0), wallThickness = 0.1f },
    };

    void Start()
    {
        if (transform.Find("Doors") == null) Build();
    }

    [ContextMenu("Build Doors")]
    public void Build()
    {
        var old = transform.Find("Doors");
        if (old != null) DestroyImmediate(old.gameObject);

        var root = new GameObject("Doors").transform;
        root.SetParent(transform, false);

        var plan = GetComponent<FloorPlanBuilder>();
        float wallHeight = plan != null ? plan.wallHeight : 2.6f;
        var wallMat = plan != null ? plan.wallMaterial : null;
        var beamMat = plan != null ? plan.beamMaterial : null;

        foreach (var d in doors) BuildDoor(root, d, wallHeight, wallMat, beamMat);
    }

    void BuildDoor(Transform root, DoorData d, float wallHeight, Material wallMat, Material beamMat)
    {
        var parent = new GameObject(d.name).transform;
        parent.SetParent(root, false);

        Vector3 a = new Vector3(d.start.x, 0f, d.start.y);
        Vector3 b = new Vector3(d.end.x, 0f, d.end.y);
        Vector3 dir = (b - a).normalized;
        float L = Vector3.Distance(a, b);
        float yaw = Yaw(dir);
        float f = frameWidth;
        float depth = d.wallThickness + 0.02f;

        // Door leaf width: fill the opening, but not wider than a normal door of this type.
        // Any space left over is filled with wall on the side away from the hinges.
        float leafW = Mathf.Min(L - 2f * f, MaxLeafWidth(d.type));
        float frameSpan = leafW + 2f * f;
        float frameStart = d.hingeAtStart ? 0f : L - frameSpan;

        var frameMat = frameMaterial != null ? frameMaterial : wallMat;
        var aboveMat = d.wallThickness >= 0.25f && beamMat != null ? beamMat : wallMat;

        // Wall above the door
        float frameTop = doorHeight + f;
        if (wallHeight > frameTop + 0.001f)
            Block(parent, "Wall above door", a, dir, L / 2f, (frameTop + wallHeight) / 2f,
                  L, wallHeight - frameTop, d.wallThickness, yaw, aboveMat, true);

        // Wall filling any extra width
        float extra = L - frameSpan;
        if (extra > 0.01f)
        {
            float centre = d.hingeAtStart ? frameSpan + extra / 2f : extra / 2f;
            Block(parent, "Wall beside door", a, dir, centre, frameTop / 2f,
                  extra, frameTop, d.wallThickness, yaw, wallMat, true);
        }

        // Frame
        Block(parent, "Frame left", a, dir, frameStart + f / 2f, frameTop / 2f, f, frameTop, depth, yaw, frameMat, true);
        Block(parent, "Frame right", a, dir, frameStart + frameSpan - f / 2f, frameTop / 2f, f, frameTop, depth, yaw, frameMat, true);
        Block(parent, "Frame top", a, dir, frameStart + frameSpan / 2f, doorHeight + f / 2f, frameSpan, f, depth, yaw, frameMat, true);

        // Hinge pivot, with the leaf pointing from the hinge towards the other side
        float hingeAlong = d.hingeAtStart ? frameStart + f : frameStart + frameSpan - f;
        Vector3 leafDir = d.hingeAtStart ? dir : -dir;
        var hinge = new GameObject("Hinge").transform;
        hinge.SetParent(parent, false);
        hinge.localPosition = a + dir * hingeAlong;
        hinge.localRotation = Quaternion.Euler(0f, Yaw(leafDir), 0f);

        var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "Door leaf";
        leaf.transform.SetParent(hinge, false);
        leaf.transform.localPosition = new Vector3(leafW / 2f, doorHeight / 2f, 0f);
        leaf.transform.localScale = new Vector3(leafW - 0.005f, doorHeight - 0.01f, leafThickness);
        leaf.GetComponent<Renderer>().sharedMaterial = LeafMaterial(d.type, wallMat);

        // Handles on both sides, near the free edge
        var handleMat = handleMaterial != null ? handleMaterial : frameMat;
        for (int side = -1; side <= 1; side += 2)
        {
            float z = side * (leafThickness / 2f + 0.015f);
            Handle(hinge, "Handle rose", new Vector3(leafW - 0.07f, 1.0f, z), new Vector3(0.05f, 0.05f, 0.01f), handleMat);
            Handle(hinge, "Handle lever", new Vector3(leafW - 0.13f, 1.0f, side * (leafThickness / 2f + 0.04f)),
                   new Vector3(0.13f, 0.02f, 0.02f), handleMat);
        }

        // Swing direction: rotate so the leaf ends up pointing towards 'opensTowards'
        Vector3 opens = new Vector3(d.opensTowards.x, 0f, d.opensTowards.y);
        Vector3 hingeForward = hinge.localRotation * Vector3.forward;
        float s = Mathf.Sign(Vector3.Dot(hingeForward, opens));
        var door = hinge.gameObject.AddComponent<Door>();
        door.openAngle = -90f * s;
    }

    float MaxLeafWidth(DoorType t)
    {
        switch (t)
        {
            case DoorType.Main: return 1.0f;
            case DoorType.Room: return 0.9f;
            case DoorType.Bathroom: return 0.75f;
            case DoorType.Shelter: return 0.85f;
            default: return 0.85f;   // service yard
        }
    }

    Material LeafMaterial(DoorType t, Material fallback)
    {
        Material m = null;
        switch (t)
        {
            case DoorType.Main: m = mainDoorMaterial; break;
            case DoorType.Room: m = roomDoorMaterial; break;
            case DoorType.Bathroom: m = bathroomDoorMaterial; break;
            case DoorType.Shelter: m = shelterDoorMaterial; break;
            case DoorType.ServiceYard: m = roomDoorMaterial; break;
        }
        return m != null ? m : fallback;
    }

    static float Yaw(Vector3 dir) => -Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

    static void Block(Transform parent, string name, Vector3 start, Vector3 dir, float along, float y,
                      float length, float height, float depth, float yaw, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = start + dir * along + Vector3.up * y;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = new Vector3(length, height, depth);
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider) DestroyImmediate(go.GetComponent<Collider>());
    }

    static void Handle(Transform hinge, string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(hinge, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        DestroyImmediate(go.GetComponent<Collider>());
    }
}
