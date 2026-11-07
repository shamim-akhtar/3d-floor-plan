using UnityEngine;

// Builds window frames and glass in the window openings of the floor plan.
// Put this on the same GameObject as FloorPlanBuilder. FloorPlanBuilder reads the
// sill and head heights from here, so the wall openings always match the windows.
// Units are metres, using the same coordinates as FloorPlanBuilder.
public class WindowBuilder : MonoBehaviour
{
    [System.Serializable]
    public class WindowData
    {
        public string name;
        public string wallId;      // the window wall in FloorPlanBuilder this sits in
        public Vector2 start;      // (x, z) of one end of the window
        public Vector2 end;        // (x, z) of the other end
        public float sill;         // height of the bottom of the window
        public float head;         // height of the top of the window
    }

    [Header("Materials")]
    public Material frameMaterial;
    public Material glassMaterial;

    [Header("Frame sizes")]
    public float frameWidth = 0.05f;        // outer frame
    public float mullionWidth = 0.04f;      // vertical and horizontal bars
    public float frameDepth = 0.06f;
    public float maxPanelWidth = 0.75f;     // a vertical bar is added at least this often
    public float topPanelHeight = 0.45f;    // height of the top row of small panels
    public float bottomPanelHeight = 0.8f;  // tall windows also get a bottom row

    // Living room window is near full height. Bedroom windows are about 3/4 height.
    public WindowData[] windows = DefaultWindows();

    // The built-in list. Unity keeps the list saved in your scene, so after a script
    // update use "Reset Windows To Defaults" from the component's menu to pick up new values.
    static WindowData[] DefaultWindows() => new WindowData[]
    {
        new WindowData { name = "Living and study window", wallId = "T2",
            start = new Vector2(0.62f, 9.325f), end = new Vector2(4.805f, 9.325f), sill = 0.15f, head = 2.3f },
        new WindowData { name = "Bedroom 3 window", wallId = "T4",
            start = new Vector2(6.48f, 9.325f), end = new Vector2(8.475f, 9.325f), sill = 0.6f, head = 2.1f },
        new WindowData { name = "Bedroom 2 window", wallId = "T4",
            start = new Vector2(8.575f, 9.325f), end = new Vector2(10.68f, 9.325f), sill = 0.6f, head = 2.1f },
        new WindowData { name = "Main bedroom window", wallId = "T6",
            start = new Vector2(11.93f, 9.325f), end = new Vector2(14.17f, 9.325f), sill = 0.6f, head = 2.1f },
        new WindowData { name = "Main bedroom side window", wallId = "R2",
            start = new Vector2(14.625f, 8.175f), end = new Vector2(14.625f, 5.815f), sill = 0.6f, head = 2.1f },
    };

    [ContextMenu("Reset Windows To Defaults")]
    void ResetToDefaults()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Reset Windows To Defaults");
#endif
        windows = DefaultWindows();
        // Rebuild the whole house so wall openings, paint and ceilings all match
        var plan = GetComponent<FloorPlanBuilder>();
        if (plan != null) plan.Build(); else Build();
    }

    // Used by FloorPlanBuilder to size the wall opening for a window wall
    public void GetOpeningHeights(string wallId, ref float sill, ref float head)
    {
        foreach (var w in windows)
        {
            if (w.wallId != wallId) continue;
            sill = w.sill;
            head = w.head;
            return;
        }
    }

    void Start()
    {
        if (transform.Find("Windows") == null) Build();
    }

    [ContextMenu("Build Windows")]
    public void Build()
    {
        var old = transform.Find("Windows");
        if (old != null) DestroyImmediate(old.gameObject);

        var root = new GameObject("Windows").transform;
        root.SetParent(transform, false);

        var frameMat = frameMaterial != null ? frameMaterial : FallbackMaterial(new Color(0.15f, 0.16f, 0.17f), false);
        var glassMat = glassMaterial != null ? glassMaterial : FallbackMaterial(new Color(0.75f, 0.85f, 0.9f, 0.25f), true);

        foreach (var w in windows) BuildWindow(root, w, frameMat, glassMat);
    }

    void BuildWindow(Transform root, WindowData w, Material frameMat, Material glassMat)
    {
        var parent = new GameObject(w.name).transform;
        parent.SetParent(root, false);

        Vector3 a = new Vector3(w.start.x, 0f, w.start.y);
        Vector3 b = new Vector3(w.end.x, 0f, w.end.y);
        Vector3 dir = (b - a).normalized;
        float L = Vector3.Distance(a, b);
        float H = w.head - w.sill;
        float yaw = -Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        float f = frameWidth, m = mullionWidth;

        // Glass first, as one pane behind all the bars
        Block(parent, "Glass", a, dir, L / 2f, w.sill + H / 2f, L - 2f * f, H - 2f * f, 0.008f, yaw, glassMat, true);

        // Outer frame
        Block(parent, "Frame bottom", a, dir, L / 2f, w.sill + f / 2f, L, f, frameDepth, yaw, frameMat, false);
        Block(parent, "Frame top", a, dir, L / 2f, w.head - f / 2f, L, f, frameDepth, yaw, frameMat, false);
        Block(parent, "Frame left", a, dir, f / 2f, w.sill + H / 2f, f, H, frameDepth, yaw, frameMat, false);
        Block(parent, "Frame right", a, dir, L - f / 2f, w.sill + H / 2f, f, H, frameDepth, yaw, frameMat, false);

        // Vertical bars
        int panels = Mathf.Max(1, Mathf.CeilToInt((L - 2f * f) / maxPanelWidth));
        float step = (L - 2f * f) / panels;
        for (int i = 1; i < panels; i++)
            Block(parent, "Mullion " + i, a, dir, f + step * i, w.sill + H / 2f, m, H - 2f * f, frameDepth, yaw, frameMat, false);

        // Top row of small panels
        if (H > 1.0f)
            Block(parent, "Transom top", a, dir, L / 2f, w.head - topPanelHeight, L - 2f * f, m, frameDepth, yaw, frameMat, false);

        // Tall windows also get a bottom row, which also acts as a safety bar
        if (H > 1.7f)
            Block(parent, "Transom bottom", a, dir, L / 2f, w.sill + bottomPanelHeight, L - 2f * f, m, frameDepth, yaw, frameMat, false);
    }

    // Creates a cube centred at distance 'along' from 'start' in direction 'dir', at height y
    static void Block(Transform parent, string name, Vector3 start, Vector3 dir, float along, float y,
                      float length, float height, float depth, float yaw, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = start + dir * along + Vector3.up * y;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = new Vector3(length, height, depth);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider) DestroyImmediate(go.GetComponent<Collider>());
    }

    // Only used if no material is assigned. These are not saved as assets,
    // so assign the .mat files in the Inspector for anything you keep.
    static Material FallbackMaterial(Color c, bool transparent)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", c);
        mat.color = c;
        if (transparent)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return mat;
    }
}
