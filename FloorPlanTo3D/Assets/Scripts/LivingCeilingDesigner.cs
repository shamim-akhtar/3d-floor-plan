using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum CeilingStyle
{
    HDBStandard, Japandi, Scandinavian, Contemporary, Resort, Luxury, Country, Minimalist
}

// Builds a false ceiling with lighting for the living and dining area.
// Put this on the same GameObject as FloorPlanBuilder. Pick a style in the Inspector
// (the Scene view updates), or use the design menu in Play mode (DesignMenu.cs).
public class LivingCeilingDesigner : MonoBehaviour
{
    public class StyleInfo
    {
        public CeilingStyle style;
        public string name;
        public string description;
        public Color[] palette;
        public int kelvin;   // default colour temperature of the lights
    }

    public static readonly StyleInfo[] Styles =
    {
        new StyleInfo { style = CeilingStyle.HDBStandard, name = "HDB standard", kelvin = 5000,
            description = "Flat ceiling as handed over, with a plain batten light.",
            palette = new[] { C(0.97f, 0.97f, 0.96f), C(0.85f, 0.85f, 0.85f), C(0.95f, 0.97f, 1f) } },
        new StyleInfo { style = CeilingStyle.Japandi, name = "Japandi", kelvin = 3000,
            description = "Perimeter box-up with warm cove light, round downlights, timber fan and pendant.",
            palette = new[] { C(0.93f, 0.90f, 0.84f), C(0.72f, 0.56f, 0.40f), C(0.30f, 0.27f, 0.24f) } },
        new StyleInfo { style = CeilingStyle.Scandinavian, name = "Scandinavian", kelvin = 4000,
            description = "Clean flat ceiling with a shadow gap, white fan and dome pendant.",
            palette = new[] { C(0.98f, 0.98f, 0.97f), C(0.80f, 0.70f, 0.56f), C(0.62f, 0.68f, 0.70f) } },
        new StyleInfo { style = CeilingStyle.Contemporary, name = "Contemporary", kelvin = 4000,
            description = "Lowered flat ceiling with a recessed LED line loop and square downlights.",
            palette = new[] { C(0.96f, 0.96f, 0.96f), C(0.45f, 0.45f, 0.47f), C(0.12f, 0.12f, 0.13f) } },
        new StyleInfo { style = CeilingStyle.Resort, name = "Resort", kelvin = 2700,
            description = "Dark timber recess with warm cove, rattan pendant and timber fan.",
            palette = new[] { C(0.90f, 0.86f, 0.78f), C(0.36f, 0.24f, 0.15f), C(0.62f, 0.50f, 0.32f) } },
        new StyleInfo { style = CeilingStyle.Luxury, name = "Luxury", kelvin = 3000,
            description = "Two-step tray with double cove, gold trim and crystal-style chandeliers.",
            palette = new[] { C(0.95f, 0.93f, 0.90f), C(0.80f, 0.65f, 0.30f), C(0.20f, 0.18f, 0.17f) } },
        new StyleInfo { style = CeilingStyle.Country, name = "Country", kelvin = 2700,
            description = "White ceiling with exposed timber beams, lantern pendant and timber fan.",
            palette = new[] { C(0.96f, 0.94f, 0.88f), C(0.50f, 0.34f, 0.20f), C(0.20f, 0.20f, 0.20f) } },
        new StyleInfo { style = CeilingStyle.Minimalist, name = "Minimalist", kelvin = 4000,
            description = "No box-up. Black track lights and a slim black fan.",
            palette = new[] { C(0.97f, 0.97f, 0.97f), C(0.10f, 0.10f, 0.10f), C(0.70f, 0.70f, 0.70f) } },
    };

    public enum Brightness { Low, Medium, High }

    // One set of lighting values. Low, Medium and High each have their own set,
    // which you can adjust in the Inspector.
    [System.Serializable]
    public class LightLevel
    {
        [Tooltip("Distance between downlights, metres. Larger means fewer lights.")]
        public float downlightSpacing = 1.6f;
        public float downlightIntensity = 2f;
        [Tooltip("Distance between the soft lights that wash the ceiling from the cove, metres.")]
        public float coveSpacing = 2.2f;
        public float coveIntensity = 0.2f;
        [Tooltip("How bright the LED strips themselves look.")]
        public float ledGlow = 2.5f;
        [Tooltip("Pendants, chandeliers, fan light and batten light.")]
        public float featureIntensity = 1.6f;
    }

    public enum RoomKind { LivingDining, Bedroom, Kitchen, Lobby }

    [Header("Room")]
    public string roomName = "Living & dining";
    public RoomKind kind = RoomKind.LivingDining;
    [Tooltip("Order of this room in the design menu")]
    public int menuOrder = 0;

    [Header("Design")]
    public CeilingStyle style = CeilingStyle.Japandi;
    [Tooltip("0 uses the style's own colour temperature")]
    public int kelvinOverride = 0;

    [Header("Lights")]
    public Brightness brightness = Brightness.Medium;
    public bool ledOn = true;          // cove LEDs and LED lines
    public bool downlightsOn = true;   // downlights and track spotlights
    public bool featureLightsOn = true; // pendants, chandeliers, fan light, batten
    public bool fanOn = true;
    public bool addGlow = true;        // bloom so LED strips glow (Play mode)

    public LightLevel low = new LightLevel
        { downlightSpacing = 2.4f, downlightIntensity = 1.3f, coveSpacing = 3.0f, coveIntensity = 0.12f, ledGlow = 1.5f, featureIntensity = 1.0f };
    public LightLevel medium = new LightLevel
        { downlightSpacing = 1.6f, downlightIntensity = 2.0f, coveSpacing = 2.2f, coveIntensity = 0.2f, ledGlow = 2.5f, featureIntensity = 1.6f };
    public LightLevel high = new LightLevel
        { downlightSpacing = 1.15f, downlightIntensity = 2.8f, coveSpacing = 1.6f, coveIntensity = 0.3f, ledGlow = 4.0f, featureIntensity = 2.4f };

    [Header("Areas (x, z in metres, measured to wall faces). Dining is only used for Living Dining.")]
    public Vector2 livingMin = new Vector2(0.11f, 4.755f);
    public Vector2 livingMax = new Vector2(5.375f, 9.275f);
    public Vector2 diningMin = new Vector2(1.67f, 2.815f);
    public Vector2 diningMax = new Vector2(7.82f, 4.755f);

    float H;            // ceiling height
    Transform root;
    Color lightColour;
    LightLevel L;       // current level
    bool hasDining;     // false for bedrooms
    readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    static Color C(float r, float g, float b) => new Color(r, g, b);

    struct Area
    {
        public float x0, z0, x1, z1;
        public Area(float a, float b, float c, float d) { x0 = a; z0 = b; x1 = c; z1 = d; }
        public float W => x1 - x0;
        public float D => z1 - z0;
        public Vector3 Centre(float y) => new Vector3((x0 + x1) / 2f, y, (z0 + z1) / 2f);
        public Area Inset(float m) => new Area(x0 + m, z0 + m, x1 - m, z1 - m);
    }

    // ------------------------------------------------------------------ entry points

    void Start()
    {
        Rebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
    }
#endif

    public void SetStyle(CeilingStyle s) { style = s; Rebuild(); }
    public void SetKelvin(int k) { kelvinOverride = k; Rebuild(); }
    public void SetBrightness(Brightness b) { brightness = b; Rebuild(); }
    public void SetLed(bool on) { ledOn = on; Rebuild(); }
    public void SetDownlights(bool on) { downlightsOn = on; Rebuild(); }
    public void SetFeatureLights(bool on) { featureLightsOn = on; Rebuild(); }
    public void SetFan(bool on)
    {
        fanOn = on;
        if (root == null) return;
        foreach (var f in root.GetComponentsInChildren<CeilingFan>()) f.spinning = on;
    }

    public static StyleInfo Info(CeilingStyle s)
    {
        foreach (var i in Styles) if (i.style == s) return i;
        return Styles[0];
    }

    // Adds a ceiling designer for each bedroom, with its room size already filled in
    [ContextMenu("Add Other Room Ceilings")]
    void AddBedrooms()
    {
        AddRoom("Bedroom 3", RoomKind.Bedroom, 1, new Vector2(5.475f, 5.75f), new Vector2(8.475f, 9.275f), CeilingStyle.Japandi);
        AddRoom("Bedroom 2", RoomKind.Bedroom, 2, new Vector2(8.575f, 5.75f), new Vector2(11.525f, 9.275f), CeilingStyle.Japandi);
        AddRoom("Main bedroom", RoomKind.Bedroom, 3, new Vector2(11.625f, 4.64f), new Vector2(14.475f, 9.275f), CeilingStyle.Resort);
        AddRoom("Kitchen", RoomKind.Kitchen, 4, new Vector2(4.17f, 0.30f), new Vector2(7.80f, 2.815f), CeilingStyle.Contemporary);
        AddRoom("Main bedroom lobby", RoomKind.Lobby, 5, new Vector2(10.09f, 4.64f), new Vector2(11.625f, 5.65f), CeilingStyle.Japandi);
    }

    void AddRoom(string name, RoomKind k, int order, Vector2 min, Vector2 max, CeilingStyle s)
    {
        foreach (var d in GetComponents<LivingCeilingDesigner>())
            if (d.roomName == name) return;   // already there
#if UNITY_EDITOR
        var c = Application.isPlaying ? gameObject.AddComponent<LivingCeilingDesigner>()
                                      : UnityEditor.Undo.AddComponent<LivingCeilingDesigner>(gameObject);
#else
        var c = gameObject.AddComponent<LivingCeilingDesigner>();
#endif
        c.roomName = name;
        c.kind = k;
        c.menuOrder = order;
        c.livingMin = min;
        c.livingMax = max;
        c.style = s;
        c.brightness = Brightness.Medium;
        c.Rebuild();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(c);
#endif
    }

    [ContextMenu("Apply Ceiling Style")]
    public void Rebuild()
    {
        var plan = GetComponent<FloorPlanBuilder>();
        H = plan != null ? plan.wallHeight : 2.6f;
        L = brightness == Brightness.Low ? low : brightness == Brightness.High ? high : medium;

        string container = "Ceiling - " + roomName;
        var old = transform.Find(container);
        if (old != null) DestroyImmediate(old.gameObject);
        var legacy = kind == RoomKind.LivingDining ? transform.Find("Living Ceiling") : null;   // from an earlier version
        if (legacy != null) DestroyImmediate(legacy.gameObject);
        root = new GameObject(container).transform;
        root.SetParent(transform, false);

        int k = kelvinOverride > 0 ? kelvinOverride : Info(style).kelvin;
        lightColour = Mathf.CorrelatedColorTemperatureToRGB(k);

        var living = new Area(livingMin.x, livingMin.y, livingMax.x, livingMax.y);
        var dining = new Area(diningMin.x, diningMin.y, diningMax.x, diningMax.y);
        hasDining = kind == RoomKind.LivingDining;

        switch (style)
        {
            case CeilingStyle.HDBStandard: BuildHDB(living, dining); break;
            case CeilingStyle.Japandi: BuildJapandi(living, dining); break;
            case CeilingStyle.Scandinavian: BuildScandi(living, dining); break;
            case CeilingStyle.Contemporary: BuildContemporary(living, dining); break;
            case CeilingStyle.Resort: BuildResort(living, dining); break;
            case CeilingStyle.Luxury: BuildLuxury(living, dining); break;
            case CeilingStyle.Country: BuildCountry(living, dining); break;
            case CeilingStyle.Minimalist: BuildMinimalist(living, dining); break;
        }

        if (addGlow && Application.isPlaying) EnsureGlow();
    }

    // ------------------------------------------------------------------ styles
    // 'room' is the main area. 'dining' is only built when hasDining is true.

    float FanBlade(float living) => kind == RoomKind.Bedroom ? living * 0.8f : living;

    // Fans, chandeliers and window coves only suit the living room and bedrooms
    bool HasFan => kind == RoomKind.LivingDining || kind == RoomKind.Bedroom;
    bool HasWindow => kind == RoomKind.LivingDining || kind == RoomKind.Bedroom;

    // Keeps a box-up band narrow enough for small rooms such as the lobby
    static float Band(Area a, float w) => Mathf.Min(w, Mathf.Min(a.W, a.D) * 0.22f);

    void MaybeFan(Vector3 at, int blades, Material bladeMat, Material bodyMat, float len)
    {
        if (HasFan) Fan(at, blades, bladeMat, bodyMat, FanBlade(len));
    }

    void BuildHDB(Area room, Area dining)
    {
        BattenLight(room.Centre(H), 1.2f);
        if (hasDining) BattenLight(dining.Centre(H), 1.2f);
    }

    void BuildJapandi(Area room, Area dining)
    {
        var plaster = Mat("Plaster warm", C(0.94f, 0.92f, 0.88f), 0.15f);
        float w = Band(room, 0.5f), drop = 0.22f;
        Tray(room, w, drop, plaster, Edge.All, true);
        BandDownlights(room, w, H - drop, false, Edge.All);

        var wood = Wood("Oak", C(0.78f, 0.62f, 0.45f));
        MaybeFan(room.Centre(H), 3, wood, Mat("Fan body oak", C(0.80f, 0.66f, 0.48f), 0.3f), 0.62f);

        if (!hasDining) return;
        Tray(dining, 0.4f, drop, plaster, Edge.All & ~Edge.Top, true);
        BandDownlights(dining, 0.4f, H - drop, false, Edge.Bottom);
        LinearPendant(dining.Centre(H), 1.4f, wood, 1.9f);
    }

    void BuildScandi(Area room, Area dining)
    {
        var dark = Mat("Shadow gap", C(0.15f, 0.15f, 0.15f), 0f);
        ShadowGap(room, dark);
        GridDownlights(room, H, false, true);
        var white = Mat("Fan white", C(0.95f, 0.95f, 0.94f), 0.4f);
        MaybeFan(room.Centre(H), 3, white, white, 0.6f);

        if (!hasDining) return;
        ShadowGap(dining, dark);
        GridDownlights(dining, H, false, true);
        DomePendant(dining.Centre(H), Mat("Pendant sage", C(0.62f, 0.68f, 0.64f), 0.3f), 1.85f);
    }

    void BuildContemporary(Area room, Area dining)
    {
        var plaster = Mat("Plaster white", C(0.96f, 0.96f, 0.96f), 0.1f);
        float drop = 0.15f, y = H - drop;
        FlatPanel(room, drop, plaster);
        var loop = room.Inset(Band(room, kind == RoomKind.LivingDining ? 0.45f : 0.4f));
        LedLoop(loop, y - 0.002f);
        // Curtain cove along the window
        if (HasWindow)
            LedStrip(new Vector3(room.Centre(0).x, y - 0.004f, room.z1 - 0.2f), new Vector3(room.W, 0.01f, 0.02f));
        GridDownlights(loop, y, true, HasFan);

        var black = Mat("Fan black", C(0.12f, 0.12f, 0.13f), 0.5f);
        MaybeFan(room.Centre(y), 3, Wood("Walnut", C(0.36f, 0.24f, 0.16f)), black, 0.6f);

        if (!hasDining) return;
        FlatPanel(dining, drop, plaster);
        var loopD = dining.Inset(0.4f);
        LedLoop(loopD, y - 0.002f);
        GridDownlights(loopD, y, true, true);
        LinearPendant(dining.Centre(y), 1.2f, black, 1.9f);
    }

    void BuildResort(Area room, Area dining)
    {
        var plaster = Mat("Plaster sand", C(0.91f, 0.87f, 0.80f), 0.15f);
        var timber = Wood("Teak", C(0.40f, 0.27f, 0.17f));
        float w = Band(room, kind == RoomKind.LivingDining ? 0.55f : 0.5f), drop = 0.25f;
        Tray(room, w, drop, plaster, Edge.All, true);
        TimberPanel(room.Inset(w + 0.08f), timber);
        BandDownlights(room, w, H - drop, false, Edge.All);
        MaybeFan(room.Centre(H), 5, timber, Mat("Fan bronze", C(0.30f, 0.22f, 0.15f), 0.4f), 0.58f);

        if (!hasDining) return;
        Tray(dining, 0.45f, drop, plaster, Edge.All & ~Edge.Top, true);
        TimberPanel(new Area(dining.x0 + 0.53f, dining.z0 + 0.53f, dining.x1 - 0.53f, dining.z1), timber);
        BandDownlights(dining, 0.45f, H - drop, false, Edge.Bottom);
        RattanPendant(dining.Centre(H), 1.85f);
    }

    void BuildLuxury(Area room, Area dining)
    {
        var plaster = Mat("Plaster ivory", C(0.95f, 0.93f, 0.90f), 0.25f);
        var gold = Mat("Gold", C(0.80f, 0.64f, 0.30f), 0.75f, 1f);
        bool bed = kind != RoomKind.LivingDining;
        float outer = Band(room, bed ? 0.4f : 0.45f);

        Tray(room, outer, 0.30f, plaster, Edge.All, true);                 // outer step
        var inner = room.Inset(outer);
        float step = Band(inner, bed ? 0.2f : 0.25f);
        Tray(inner, step, 0.14f, plaster, Edge.All, true);                 // inner step
        GoldTrim(inner, H - 0.30f, gold);
        GoldTrim(inner.Inset(step), H - 0.14f, gold);
        BandDownlights(room, outer, H - 0.30f, false, Edge.All);
        if (HasFan) Chandelier(room.Centre(H), gold, bed ? 0.32f : 0.45f, bed ? 2.05f : 1.95f);

        if (!hasDining) return;
        Tray(dining, 0.4f, 0.30f, plaster, Edge.All & ~Edge.Top, true);
        GoldTrim(new Area(dining.x0 + 0.4f, dining.z0 + 0.4f, dining.x1 - 0.4f, dining.z1), H - 0.30f, gold);
        BandDownlights(dining, 0.4f, H - 0.30f, false, Edge.Bottom);
        Chandelier(dining.Centre(H), gold, 0.32f, 1.95f);
    }

    void BuildCountry(Area room, Area dining)
    {
        var timber = Wood("Rustic pine", C(0.55f, 0.38f, 0.22f));
        float[] beams = Beams(room, timber, 1.1f);
        BetweenBeamDownlights(room, beams, kind == RoomKind.LivingDining ? 2 : 1);
        MaybeFan(room.Centre(H), 4, timber, Mat("Fan iron", C(0.18f, 0.18f, 0.18f), 0.3f), 0.62f);

        if (!hasDining) return;
        float[] beamsD = Beams(dining, timber, 1.1f);
        Box("Edge beam", new Vector3((dining.x0 + room.x1) / 2f, H - 0.09f, room.z0),
            new Vector3(room.x1 - dining.x0, 0.18f, 0.16f), timber);
        BetweenBeamDownlights(dining, beamsD, 1);
        Lantern(dining.Centre(H), Mat("Iron", C(0.15f, 0.15f, 0.15f), 0.3f), 1.8f);
    }

    void BuildMinimalist(Area room, Area dining)
    {
        var black = Mat("Matt black", C(0.08f, 0.08f, 0.08f), 0.2f);
        float len = Mathf.Min(room.W - 0.3f, Mathf.Max(1.0f, room.W - 1.4f));
        if (room.D < 1.6f)
            TrackLight(new Vector3(room.Centre(0).x, H, room.Centre(0).z), len, black);
        else
        {
            TrackLight(new Vector3(room.Centre(0).x, H, Mathf.Lerp(room.z0, room.z1, 0.25f)), len, black);
            TrackLight(new Vector3(room.Centre(0).x, H, Mathf.Lerp(room.z0, room.z1, 0.75f)), len, black);
        }
        MaybeFan(room.Centre(H), 3, black, black, 0.55f);

        if (!hasDining) return;
        TrackLight(new Vector3(dining.Centre(0).x, H, dining.Centre(0).z), dining.W - 1.6f, black);
    }

    // ------------------------------------------------------------------ ceiling elements

    [System.Flags] enum Edge { None = 0, Bottom = 1, Top = 2, Left = 4, Right = 8, All = 15 }

    // A lowered band around the edge of an area (box-up), with an optional cove LED
    // on the inner face that washes light onto the recessed ceiling.
    void Tray(Area a, float w, float drop, Material body, Edge edges, bool cove)
    {
        float yTop = H, yBot = H - drop, yMid = (yTop + yBot) / 2f;
        bool b = (edges & Edge.Bottom) != 0, t = (edges & Edge.Top) != 0;
        bool l = (edges & Edge.Left) != 0, r = (edges & Edge.Right) != 0;
        float zs = a.z0 + (b ? w : 0f), ze = a.z1 - (t ? w : 0f);

        if (b) Box("Box-up", new Vector3(a.Centre(0).x, yMid, a.z0 + w / 2f), new Vector3(a.W, drop, w), body);
        if (t) Box("Box-up", new Vector3(a.Centre(0).x, yMid, a.z1 - w / 2f), new Vector3(a.W, drop, w), body);
        if (l) Box("Box-up", new Vector3(a.x0 + w / 2f, yMid, (zs + ze) / 2f), new Vector3(w, drop, ze - zs), body);
        if (r) Box("Box-up", new Vector3(a.x1 - w / 2f, yMid, (zs + ze) / 2f), new Vector3(w, drop, ze - zs), body);

        if (!cove) return;
        float xs = a.x0 + (l ? w : 0f), xe = a.x1 - (r ? w : 0f);
        if (b) CoveEdge(new Vector3((xs + xe) / 2f, 0, a.z0 + w + 0.01f), xe - xs, true, +1, drop);
        if (t) CoveEdge(new Vector3((xs + xe) / 2f, 0, a.z1 - w - 0.01f), xe - xs, true, -1, drop);
        if (l) CoveEdge(new Vector3(a.x0 + w + 0.01f, 0, (zs + ze) / 2f), ze - zs, false, +1, drop);
        if (r) CoveEdge(new Vector3(a.x1 - w - 0.01f, 0, (zs + ze) / 2f), ze - zs, false, -1, drop);
    }

    // The glowing strip along the inner edge, plus soft lights set well below the
    // ceiling so they give an even wash instead of bright spots.
    void CoveEdge(Vector3 centre, float length, bool alongX, int inward, float drop)
    {
        if (length < 0.2f) return;
        Vector3 size = alongX ? new Vector3(length, 0.03f, 0.02f) : new Vector3(0.02f, 0.03f, length);
        LedStrip(new Vector3(centre.x, H - 0.03f, centre.z), size);

        if (!ledOn) return;
        int n = Mathf.Max(1, Mathf.RoundToInt(length / L.coveSpacing));
        float y = H - Mathf.Max(drop, 0.22f);
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n - 0.5f;
            Vector3 p = new Vector3(centre.x, y, centre.z) +
                        (alongX ? new Vector3(t * length, 0, 0.15f * inward) : new Vector3(0.15f * inward, 0, t * length));
            AddLight(LightType.Point, p, L.coveIntensity, 2.2f, 0f);
        }
    }

    void FlatPanel(Area a, float drop, Material m)
    {
        Box("False ceiling", a.Centre(H - drop / 2f), new Vector3(a.W, drop, a.D), m);
    }

    void ShadowGap(Area a, Material dark)
    {
        float g = 0.03f, y = H - 0.004f;
        Box("Shadow gap", new Vector3(a.Centre(0).x, y, a.z0 + g / 2f), new Vector3(a.W, 0.008f, g), dark);
        Box("Shadow gap", new Vector3(a.Centre(0).x, y, a.z1 - g / 2f), new Vector3(a.W, 0.008f, g), dark);
        Box("Shadow gap", new Vector3(a.x0 + g / 2f, y, a.Centre(0).z), new Vector3(g, 0.008f, a.D), dark);
        Box("Shadow gap", new Vector3(a.x1 - g / 2f, y, a.Centre(0).z), new Vector3(g, 0.008f, a.D), dark);
    }

    void LedLoop(Area a, float y)
    {
        float s = 0.025f;
        LedStrip(new Vector3(a.Centre(0).x, y, a.z0), new Vector3(a.W + s, 0.004f, s));
        LedStrip(new Vector3(a.Centre(0).x, y, a.z1), new Vector3(a.W + s, 0.004f, s));
        LedStrip(new Vector3(a.x0, y, a.Centre(0).z), new Vector3(s, 0.004f, a.D));
        LedStrip(new Vector3(a.x1, y, a.Centre(0).z), new Vector3(s, 0.004f, a.D));
    }

    void TimberPanel(Area a, Material timber)
    {
        if (a.W <= 0.1f || a.D <= 0.1f) return;
        Box("Timber recess panel", a.Centre(H - 0.012f), new Vector3(a.W, 0.02f, a.D), timber);
    }

    void GoldTrim(Area a, float y, Material gold)
    {
        float s = 0.015f;
        Box("Gold trim", new Vector3(a.Centre(0).x, y - s / 2f, a.z0), new Vector3(a.W, s, s), gold);
        Box("Gold trim", new Vector3(a.Centre(0).x, y - s / 2f, a.z1), new Vector3(a.W, s, s), gold);
        Box("Gold trim", new Vector3(a.x0, y - s / 2f, a.Centre(0).z), new Vector3(s, s, a.D), gold);
        Box("Gold trim", new Vector3(a.x1, y - s / 2f, a.Centre(0).z), new Vector3(s, s, a.D), gold);
    }

    // Returns the x positions of the beams
    float[] Beams(Area a, Material timber, float spacing)
    {
        int n = Mathf.Max(1, Mathf.FloorToInt(a.W / spacing));
        float step = a.W / (n + 1);
        var xs = new float[n];
        for (int i = 1; i <= n; i++)
        {
            xs[i - 1] = a.x0 + step * i;
            Box("Timber beam", new Vector3(xs[i - 1], H - 0.08f, a.Centre(0).z), new Vector3(0.12f, 0.16f, a.D), timber);
        }
        return xs;
    }

    // ------------------------------------------------------------------ downlight layouts

    // Evenly spaced positions along a length, with half a gap at each end
    static float[] Even(float start, float end, int n)
    {
        var p = new float[n];
        for (int i = 0; i < n; i++) p[i] = Mathf.Lerp(start, end, (i + 0.5f) / n);
        return p;
    }

    int Count(float length, bool even)
    {
        float raw = length / Mathf.Max(0.3f, L.downlightSpacing);
        int n = Mathf.Max(1, Mathf.RoundToInt(raw));
        if (even && n % 2 == 1 && n > 1) n = raw > n ? n + 1 : n - 1;   // keep the centre free for a fan or pendant
        return n;
    }

    // Downlights along the middle of the box-up band, evenly spaced on each side.
    // Opposite sides get the same number, so the lights line up across the room.
    void BandDownlights(Area a, float w, float y, bool square, Edge edges)
    {
        bool b = (edges & Edge.Bottom) != 0, t = (edges & Edge.Top) != 0;
        bool l = (edges & Edge.Left) != 0, r = (edges & Edge.Right) != 0;

        // Long sides run the full length between the band corners
        float xs = a.x0 + w, xe = a.x1 - w;
        foreach (float x in Even(xs, xe, Count(xe - xs, false)))
        {
            if (b) Downlight(new Vector3(x, y, a.z0 + w / 2f), square);
            if (t) Downlight(new Vector3(x, y, a.z1 - w / 2f), square);
        }
        float zs = a.z0 + w, ze = a.z1 - w;
        if (ze - zs > 0.5f)
            foreach (float z in Even(zs, ze, Count(ze - zs, false)))
            {
                if (l) Downlight(new Vector3(a.x0 + w / 2f, y, z), square);
                if (r) Downlight(new Vector3(a.x1 - w / 2f, y, z), square);
            }
    }

    // A tidy grid of downlights over the whole area. With keepCentreClear, the counts
    // are kept even so no light sits under the fan or pendant in the middle.
    void GridDownlights(Area a, float y, bool square, bool keepCentreClear)
    {
        int nx = Count(a.W, keepCentreClear);
        int nz = a.D < L.downlightSpacing * 1.5f ? 1 : Count(a.D, keepCentreClear);
        foreach (float x in Even(a.x0, a.x1, nx))
            foreach (float z in Even(a.z0, a.z1, nz))
                Downlight(new Vector3(x, y, z), square);
    }

    void BetweenBeamDownlights(Area a, float[] beams, int rows)
    {
        var cols = new List<float>();
        float prev = a.x0;
        foreach (float b in beams) { cols.Add((prev + b) / 2f); prev = b; }
        cols.Add((prev + a.x1) / 2f);

        // At lower brightness, use every other gap
        int stride = brightness == Brightness.Low ? 2 : 1;
        if (brightness == Brightness.High) rows = Mathf.Max(rows, 2);
        float[] zs = Even(a.z0, a.z1, rows);
        for (int i = 0; i < cols.Count; i += stride)
            foreach (float z in zs)
                Downlight(new Vector3(cols[i], H - 0.002f, z), false);
    }

    // ------------------------------------------------------------------ light fittings

    void Downlight(Vector3 p, bool square)
    {
        var trim = Mat("Downlight trim", C(0.92f, 0.92f, 0.92f), 0.4f);
        var lens = Emissive(2.5f, downlightsOn);
        if (square)
        {
            Box("Downlight trim", p + Vector3.down * 0.004f, new Vector3(0.12f, 0.008f, 0.12f), trim);
            Box("Downlight", p + Vector3.down * 0.009f, new Vector3(0.09f, 0.004f, 0.09f), lens);
        }
        else
        {
            Cyl("Downlight trim", p + Vector3.down * 0.004f, 0.06f, 0.008f, trim);
            Cyl("Downlight", p + Vector3.down * 0.009f, 0.045f, 0.004f, lens);
        }
        if (downlightsOn) AddLight(LightType.Spot, p + Vector3.down * 0.02f, L.downlightIntensity, 4.5f, 80f);
    }

    void BattenLight(Vector3 ceiling, float length)
    {
        var white = Mat("Batten body", C(0.95f, 0.95f, 0.95f), 0.3f);
        Box("Batten base", ceiling + Vector3.down * 0.02f, new Vector3(length, 0.04f, 0.07f), white);
        Box("Batten tube", ceiling + Vector3.down * 0.05f, new Vector3(length - 0.06f, 0.025f, 0.035f), Emissive(2f, featureLightsOn));
        Feature(ceiling + Vector3.down * 0.15f, 1.4f, 5f);
    }

    void LinearPendant(Vector3 ceiling, float length, Material body, float height)
    {
        Vector3 p = new Vector3(ceiling.x, height, ceiling.z);
        Box("Linear pendant", p, new Vector3(length, 0.035f, 0.05f), body);
        Box("Linear pendant light", p + Vector3.down * 0.019f, new Vector3(length - 0.04f, 0.004f, 0.03f), Emissive(2f, featureLightsOn));
        Wire(new Vector3(p.x - length * 0.4f, p.y, p.z), ceiling.y);
        Wire(new Vector3(p.x + length * 0.4f, p.y, p.z), ceiling.y);
        if (featureLightsOn) AddLight(LightType.Spot, p + Vector3.down * 0.03f, L.featureIntensity, 3f, 110f);
    }

    void DomePendant(Vector3 ceiling, Material shade, float height)
    {
        Vector3 p = new Vector3(ceiling.x, height, ceiling.z);
        Sphere("Dome shade", p + Vector3.up * 0.06f, new Vector3(0.42f, 0.24f, 0.42f), shade);
        Sphere("Dome bulb", p - Vector3.up * 0.03f, new Vector3(0.08f, 0.08f, 0.08f), Emissive(3f, featureLightsOn));
        Wire(p + Vector3.up * 0.15f, ceiling.y);
        if (featureLightsOn) AddLight(LightType.Spot, p + Vector3.down * 0.05f, L.featureIntensity, 3f, 110f);
    }

    void RattanPendant(Vector3 ceiling, float height)
    {
        var rattan = Mat("Rattan", C(0.70f, 0.55f, 0.34f), 0.15f);
        Vector3 p = new Vector3(ceiling.x, height, ceiling.z);
        Sphere("Rattan shade", p, new Vector3(0.55f, 0.42f, 0.55f), rattan);
        Sphere("Rattan glow", p + Vector3.down * 0.17f, new Vector3(0.22f, 0.06f, 0.22f), Emissive(2f, featureLightsOn));
        Wire(p + Vector3.up * 0.2f, ceiling.y);
        Feature(p + Vector3.down * 0.3f, 1f, 3f);
    }

    void Lantern(Vector3 ceiling, Material iron, float height)
    {
        Vector3 p = new Vector3(ceiling.x, height, ceiling.z);
        float s = 0.32f, bar = 0.015f;
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                Box("Lantern post", p + new Vector3(i * s / 2f, 0, j * s / 2f), new Vector3(bar, s * 1.3f, bar), iron);
        Box("Lantern top", p + Vector3.up * s * 0.68f, new Vector3(s + 0.04f, 0.03f, s + 0.04f), iron);
        Box("Lantern base", p - Vector3.up * s * 0.65f, new Vector3(s, 0.02f, s), iron);
        Sphere("Lantern bulb", p, new Vector3(0.1f, 0.14f, 0.1f), Emissive(3f, featureLightsOn));
        Wire(p + Vector3.up * s * 0.7f, ceiling.y);
        Feature(p, 1.1f, 3.5f);
    }

    void Chandelier(Vector3 ceiling, Material gold, float radius, float height)
    {
        Vector3 c = new Vector3(ceiling.x, height, ceiling.z);
        Cyl("Chandelier rod", new Vector3(c.x, (height + ceiling.y) / 2f, c.z), 0.01f, ceiling.y - height, gold);
        Cyl("Chandelier canopy", new Vector3(c.x, ceiling.y - 0.01f, c.z), 0.07f, 0.02f, gold);
        var crystal = Emissive(2.5f, featureLightsOn);
        foreach (var ring in new[] { (radius, 0f), (radius * 0.6f, -0.12f) })
        {
            int n = 12;
            for (int i = 0; i < n; i++)
            {
                float ang = i * Mathf.PI * 2f / n;
                Vector3 p = c + new Vector3(Mathf.Cos(ang) * ring.Item1, ring.Item2, Mathf.Sin(ang) * ring.Item1);
                Sphere("Crystal", p, Vector3.one * 0.05f, crystal);
                Vector3 mid = (p + new Vector3(c.x, p.y, c.z)) / 2f;
                var arm = Box("Arm", mid, new Vector3(ring.Item1, 0.008f, 0.008f), gold);
                arm.transform.localRotation = Quaternion.Euler(0f, -ang * Mathf.Rad2Deg, 0f);
            }
        }
        Feature(c + Vector3.down * 0.1f, 1.5f, 4.5f);
    }

    void TrackLight(Vector3 ceilingCentre, float length, Material black)
    {
        Box("Track", ceilingCentre + Vector3.down * 0.02f, new Vector3(length, 0.04f, 0.04f), black);
        int heads = Mathf.Max(2, Mathf.RoundToInt(length / L.downlightSpacing));
        foreach (float x in Even(-length / 2f, length / 2f, heads))
        {
            Vector3 p = ceilingCentre + new Vector3(x, -0.13f, 0f);
            Cyl("Track spot", p, 0.035f, 0.14f, black);
            Cyl("Track spot lens", p + Vector3.down * 0.071f, 0.028f, 0.003f, Emissive(3f, downlightsOn));
            Cyl("Track stem", p + Vector3.up * 0.08f, 0.008f, 0.06f, black);
            if (downlightsOn) AddLight(LightType.Spot, p + Vector3.down * 0.08f, L.downlightIntensity, 4f, 50f);
        }
    }

    void Fan(Vector3 ceiling, int blades, Material bladeMat, Material bodyMat, float bladeLength)
    {
        var fan = new GameObject("Ceiling fan").transform;
        fan.SetParent(root, false);
        fan.localPosition = ceiling;

        Cyl("Canopy", new Vector3(0, -0.02f, 0), 0.07f, 0.04f, bodyMat, fan);
        Cyl("Downrod", new Vector3(0, -0.1f, 0), 0.012f, 0.14f, bodyMat, fan);
        Cyl("Motor", new Vector3(0, -0.22f, 0), 0.12f, 0.1f, bodyMat, fan);
        Cyl("Light kit", new Vector3(0, -0.28f, 0), 0.1f, 0.025f, Emissive(1.8f, featureLightsOn), fan);

        var rotor = new GameObject("Rotor").transform;
        rotor.SetParent(fan, false);
        rotor.localPosition = new Vector3(0, -0.22f, 0);
        for (int i = 0; i < blades; i++)
        {
            var pivot = new GameObject("Blade pivot").transform;
            pivot.SetParent(rotor, false);
            pivot.localRotation = Quaternion.Euler(0f, i * 360f / blades, 0f);
            var blade = Box("Blade", Vector3.zero, new Vector3(bladeLength, 0.012f, 0.13f), bladeMat, pivot);
            blade.transform.localPosition = new Vector3(0.12f + bladeLength / 2f, 0f, 0f);
            blade.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        }

        var spin = fan.gameObject.AddComponent<CeilingFan>();
        spin.rotor = rotor;
        spin.spinning = fanOn;

        Feature(ceiling + Vector3.down * 0.4f, 0.8f, 3.5f);
    }

    void Wire(Vector3 bottom, float ceilingY)
    {
        var wire = Mat("Wire", C(0.1f, 0.1f, 0.1f), 0.2f);
        float len = ceilingY - bottom.y;
        Cyl("Wire", new Vector3(bottom.x, bottom.y + len / 2f, bottom.z), 0.003f, len, wire);
    }

    void LedStrip(Vector3 centre, Vector3 size)
    {
        Box("LED strip", centre, size, Emissive(L.ledGlow, ledOn));
    }

    // ------------------------------------------------------------------ lights

    // A pendant, chandelier, fan or batten light. 'share' scales the level's feature intensity.
    void Feature(Vector3 p, float share, float range)
    {
        if (featureLightsOn) AddLight(LightType.Point, p, L.featureIntensity * share, range, 0f);
    }

    void AddLight(LightType type, Vector3 p, float intensity, float range, float angle)
    {
        var go = new GameObject(type == LightType.Spot ? "Spot light" : "Point light");
        go.transform.SetParent(root, false);
        go.transform.localPosition = p;
        if (type == LightType.Spot) go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var l = go.AddComponent<Light>();
        l.type = type;
        l.color = lightColour;
        l.intensity = intensity;
        l.range = range;
        if (type == LightType.Spot)
        {
            l.spotAngle = angle;
            l.innerSpotAngle = angle * 0.5f;
        }
        l.shadows = LightShadows.None;
    }

    void EnsureGlow()
    {
        const string glowName = "LED Glow (Bloom)";
        if (GameObject.Find(glowName) != null) return;
        var go = new GameObject(glowName);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(0.5f);
        bloom.scatter.Override(0.55f);
        vol.sharedProfile = profile;

        var cam = Camera.main;
        if (cam != null)
        {
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
        }
    }

    // ------------------------------------------------------------------ primitives and materials

    GameObject Box(string name, Vector3 centre, Vector3 size, Material m, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Setup(go, name, centre, size, m, parent);
        return go;
    }

    GameObject Cyl(string name, Vector3 centre, float radius, float height, Material m, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Setup(go, name, centre, new Vector3(radius * 2f, height / 2f, radius * 2f), m, parent);
        return go;
    }

    GameObject Sphere(string name, Vector3 centre, Vector3 size, Material m, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Setup(go, name, centre, size, m, parent);
        return go;
    }

    void Setup(GameObject go, string name, Vector3 centre, Vector3 size, Material m, Transform parent)
    {
        go.name = name;
        go.transform.SetParent(parent != null ? parent : root, false);
        go.transform.localPosition = centre;
        go.transform.localScale = size;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        DestroyImmediate(go.GetComponent<Collider>());
    }

    Material Mat(string key, Color c, float smooth, float metal = 0f)
    {
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = NewLit(key);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        mats[key] = m;
        return m;
    }

    // Glowing material for LEDs and lamp surfaces. When 'on' is false it is a plain off-white.
    Material Emissive(float strength, bool on)
    {
        string key = on ? "Glow " + lightColour + strength : "Glow off";
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = NewLit(key);
        if (on)
        {
            m.SetColor("_BaseColor", lightColour);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", lightColour * strength);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else m.SetColor("_BaseColor", C(0.9f, 0.9f, 0.88f));
        mats[key] = m;
        return m;
    }

    Material Wood(string key, Color tint)
    {
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = NewLit(key);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.3f);
        var tex = WoodTexture(tint);
        m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        mats[key] = m;
        return m;
    }

    static Material NewLit(string name)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name };
    }

    static Texture2D WoodTexture(Color tint)
    {
        const int w = 256, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Wood grain" };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float g = Mathf.PerlinNoise(x * 0.02f, y * 0.35f) * 0.6f + Mathf.PerlinNoise(x * 0.1f, y * 1.3f) * 0.4f;
                float k = 0.82f + g * 0.3f;
                px[y * w + x] = new Color(tint.r * k, tint.g * k, tint.b * k);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }
}
