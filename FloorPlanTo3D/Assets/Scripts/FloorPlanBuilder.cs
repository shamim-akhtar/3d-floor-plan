using UnityEngine;

// Builds the floor plan walls as cubes. Units are metres.
// Origin (0,0,0) is the inner face of the left wall at the bottom (kitchen) facade line.
// +X runs left to right on the plan, +Z runs from the kitchen side up to the bedrooms.
public class FloorPlanBuilder : MonoBehaviour
{
    public float wallHeight = 2.6f;
    public float windowSillHeight = 0.9f;   // windows are built as a low wall plus a header
    public float windowHeadHeight = 2.1f;
    public float parapetHeight = 1.0f;
    public Material wallMaterial;
    public Material beamMaterial;

    [Header("Ceiling")]
    public bool buildCeiling = true;
    public Material ceilingMaterial;
    // Outer edges of the house in metres (covers all walls and beams)
    public float ceilingMinX = -0.67f, ceilingMaxX = 14.775f;
    public float ceilingMinZ = 0f, ceilingMaxZ = 9.575f;

    struct W
    {
        public string id, name, type; public float cx, cz, len, thick, rotY;
        public W(string i, string n, float x, float z, float l, float t, float r, string ty)
        { id = i; name = n; cx = x; cz = z; len = l; thick = t; rotY = r; type = ty; }
    }

    static readonly W[] walls =
    {
        new W("T1", "Top facade beam (study)", 0.05f, 9.425f, 1.14f, 0.3f, 0f, "beam"),
        new W("T2", "Top facade window (study/living)", 2.92f, 9.325f, 4.6f, 0.1f, 0f, "window"),
        new W("T3", "Top facade beam (bedroom 3)", 5.85f, 9.425f, 1.26f, 0.3f, 0f, "beam"),
        new W("T4", "Top facade window (bedroom 3/2)", 8.58f, 9.325f, 4.2f, 0.1f, 0f, "window"),
        new W("T5", "Top facade beam (bedroom 2/main)", 11.305f, 9.425f, 1.25f, 0.3f, 0f, "beam"),
        new W("T6", "Top facade window (main bedroom)", 13.203f, 9.325f, 2.545f, 0.1f, 0f, "window"),
        new W("R1", "Right wall upper", 14.625f, 8.875f, 1.4f, 0.3f, 90f, "beam"),
        new W("R2", "Right wall window (main bedroom)", 14.625f, 6.995f, 2.36f, 0.1f, 90f, "window"),
        new W("R3", "Right wall lower", 14.625f, 5.178f, 1.275f, 0.3f, 90f, "beam"),
        new W("L1", "Left wall (study)", 0.0f, 7.47f, 3.61f, 0.1f, 90f, "wall"),
        new W("L2", "Left wall (living/shelter)", -0.04f, 3.74f, 3.85f, 0.3f, 90f, "beam"),
        new W("S1", "Shelter top wall", 0.89f, 4.605f, 1.56f, 0.3f, 0f, "beam"),
        new W("S2", "Shelter right wall (above door)", 1.52f, 4.21f, 0.49f, 0.3f, 90f, "beam"),
        new W("S3", "Shelter right wall (below door)", 1.52f, 2.465f, 1.3f, 0.3f, 90f, "beam"),
        new W("S4", "Shelter bottom wall", 0.74f, 1.965f, 1.26f, 0.3f, 0f, "beam"),
        new W("E1", "Entrance wall (right of main door)", 3.26f, 1.975f, 0.92f, 0.1f, 0f, "wall"),
        new W("E2", "Short return wall near entrance", 3.77f, 2.445f, 1.04f, 0.1f, 90f, "wall"),
        new W("K1", "Kitchen column/wall at entrance", 3.97f, 2.39f, 1.15f, 0.3f, 90f, "beam"),
        new W("K2", "Kitchen left wall", 4.12f, 1.058f, 1.515f, 0.1f, 90f, "wall"),
        new W("K3", "Kitchen/service yard bottom wall", 6.75f, 0.15f, 6.42f, 0.3f, 0f, "beam"),
        new W("SY1", "Kitchen/service yard partition (upper)", 7.85f, 2.07f, 0.83f, 0.1f, 90f, "wall"),
        new W("SY2", "Kitchen/service yard partition (lower)", 7.85f, 0.503f, 0.405f, 0.1f, 90f, "wall"),
        new W("SY3", "Service yard right wall", 9.3f, 1.47f, 2.34f, 0.1f, 90f, "wall"),
        new W("W1", "WC2 left wall", 7.97f, 3.538f, 2.105f, 0.3f, 90f, "beam"),
        new W("W2", "WC1/WC2 partition", 10.04f, 3.615f, 1.95f, 0.1f, 90f, "wall"),
        new W("W3", "WC1 right wall", 12.57f, 3.538f, 2.105f, 0.3f, 90f, "beam"),
        new W("BT1", "Bath top wall (between doors)", 9.91f, 4.59f, 2.18f, 0.1f, 0f, "wall"),
        new W("BT2", "Bath top wall (right of WC1 door)", 12.06f, 4.59f, 0.72f, 0.1f, 0f, "wall"),
        new W("BB1", "Bath bottom wall", 10.27f, 2.64f, 4.3f, 0.1f, 0f, "wall"),
        new W("AL1", "Air-con ledge left", 10.0f, 2.148f, 0.985f, 0.1f, 90f, "parapet"),
        new W("AL2", "Air-con ledge bottom", 11.33f, 1.655f, 2.66f, 0.1f, 0f, "parapet"),
        new W("AL3", "Air-con ledge right", 12.66f, 2.07f, 0.83f, 0.1f, 90f, "parapet"),
        new W("BR1", "Bedroom 3 left wall", 5.425f, 7.488f, 3.575f, 0.1f, 90f, "wall"),
        new W("BR2", "Bedroom 3 bottom wall", 6.453f, 5.7f, 2.055f, 0.1f, 0f, "wall"),
        new W("BR3", "Bedroom 3/2 partition", 8.525f, 7.488f, 3.575f, 0.1f, 90f, "wall"),
        new W("BR4", "Bedroom 2 bottom wall", 10.633f, 5.7f, 1.885f, 0.1f, 0f, "wall"),
        new W("BR5", "Bedroom 2/main bedroom partition", 11.575f, 7.488f, 3.575f, 0.1f, 90f, "wall"),
        new W("MB1", "Main bedroom bottom wall", 13.598f, 4.59f, 1.755f, 0.1f, 0f, "wall"),
    };

    // Builds anything missing when you press Play
    void Start()
    {
        if (transform.Find("Walls") == null) Build();
        else if (buildCeiling && transform.Find("Ceiling") == null) MakeCeiling();
    }

    // Rebuilds the walls and ceiling, then the windows and doors if those
    // components are on the same GameObject.
    [ContextMenu("Build Walls")]
    public void Build()
    {
        // Remove everything previously built (including old loose walls)
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        var wallRoot = new GameObject("Walls").transform;
        wallRoot.SetParent(transform, false);

        var windows = GetComponent<WindowBuilder>();

        foreach (var w in walls)
        {
            if (w.type == "window")
            {
                float sill = windowSillHeight, head = windowHeadHeight;
                if (windows != null) windows.GetOpeningHeights(w.id, ref sill, ref head);
                if (sill > 0.001f) MakeBlock(wallRoot, w, 0f, sill, " (sill)");
                if (head < wallHeight - 0.001f) MakeBlock(wallRoot, w, head, wallHeight, " (header)");
            }
            else if (w.type == "parapet")
                MakeBlock(wallRoot, w, 0f, parapetHeight, "");
            else
                MakeBlock(wallRoot, w, 0f, wallHeight, "");
        }

        if (buildCeiling) MakeCeiling();

        var floors = GetComponent<FloorBuilder>();
        if (floors != null) floors.Build();
        if (windows != null) windows.Build();
        var doors = GetComponent<DoorBuilder>();
        if (doors != null) doors.Build();
        foreach (var ceiling in GetComponents<LivingCeilingDesigner>()) ceiling.Rebuild();
    }

    // The ceiling is a one-sided plane facing down. Unity does not draw the back
    // of a plane, so it is invisible from above (Scene view looking down) but
    // visible from inside the house looking up (Play mode).
    void MakeCeiling()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Ceiling";
        go.transform.SetParent(transform, false);

        float sizeX = ceilingMaxX - ceilingMinX;
        float sizeZ = ceilingMaxZ - ceilingMinZ;
        go.transform.localPosition = new Vector3(
            (ceilingMinX + ceilingMaxX) / 2f, wallHeight, (ceilingMinZ + ceilingMaxZ) / 2f);
        go.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);   // face downwards
        go.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f); // Plane is 10 x 10 m

        var rend = go.GetComponent<Renderer>();
        // No shadows, so the ceiling does not darken the inside of the house
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mat = ceilingMaterial != null ? ceilingMaterial : wallMaterial;
        if (mat != null) rend.sharedMaterial = mat;
    }

    void MakeBlock(Transform parent, W w, float bottom, float top, string suffix)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = w.id + " " + w.name + suffix;
        go.transform.SetParent(parent, false);
        float h = top - bottom;
        go.transform.localPosition = new Vector3(w.cx, bottom + h / 2f, w.cz);
        go.transform.localRotation = Quaternion.Euler(0f, w.rotY, 0f);
        go.transform.localScale = new Vector3(w.len, h, w.thick);
        var mat = w.type == "beam" ? beamMaterial : wallMaterial;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
