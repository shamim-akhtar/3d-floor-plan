using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Paints the walls room by room, and adds an optional feature wall in each room.
// Put this on the same GameObject as FloorPlanBuilder and FloorBuilder.
// It covers the room-facing side of every wall, beam, window sill/header and door header
// with a thin layer of paint, so one wall can have a different colour on each side.
// Rooms are found from the FloorBuilder zones.
public class WallDesigner : MonoBehaviour
{
    public enum FeatureFinish { None, AccentPaint, Limewash, TimberSlatsOak, TimberSlatsWalnut, FlutedPanel, Marble }

    public class Swatch
    {
        public string name;
        public Color colour;
        public Swatch(string n, string hex) { name = n; ColorUtility.TryParseHtmlString("#" + hex, out colour); }
    }

    public static readonly Swatch[] Swatches =
    {
        new Swatch("Pure white", "F4F4F2"), new Swatch("Warm white", "F1ECE2"), new Swatch("Ivory", "EEE6D3"),
        new Swatch("Linen", "E6DDCD"), new Swatch("Greige", "D8D0C4"), new Swatch("Mushroom", "C4B8A8"),
        new Swatch("Stone grey", "C9C7C2"), new Swatch("Pebble", "A9A49C"), new Swatch("Sage", "C5CFBE"),
        new Swatch("Eucalyptus", "9FB0A0"), new Swatch("Olive", "8A8763"), new Swatch("Dusty blue", "BFCAD3"),
        new Swatch("Denim", "6B7F95"), new Swatch("Navy", "2F3D52"), new Swatch("Blush", "E8D3CC"),
        new Swatch("Clay", "B48E78"), new Swatch("Terracotta", "C27A5C"), new Swatch("Mustard", "C9A24B"),
        new Swatch("Charcoal", "46484B"), new Swatch("Soft black", "222222"),
    };

    public static readonly string[] FinishNames =
        { "None", "Accent paint", "Limewash", "Timber slats (oak)", "Timber slats (walnut)", "Fluted panel", "Marble" };

    [System.Serializable]
    public class RoomWalls
    {
        public string roomName;
        [Tooltip("Floor zones (from Floor Builder) that belong to this room")]
        public string[] zones;
        public int colourIndex = 1;                 // index into Swatches
        public FeatureFinish feature = FeatureFinish.None;
        public int accentIndex = 18;
        [Tooltip("Which wall gets the feature. Walls are numbered from longest to shortest.")]
        public int featureWall = 0;
    }

    public bool paintBeams = true;

    public List<RoomWalls> rooms = new List<RoomWalls>
    {
        new RoomWalls { roomName = "Living & dining", colourIndex = 1, accentIndex = 4,
            zones = new[] { "Living and study", "Living beside shelter", "Dining", "Entrance foyer", "Bedroom corridor" } },
        new RoomWalls { roomName = "Bedroom 3", colourIndex = 3, accentIndex = 8, zones = new[] { "Bedroom 3" } },
        new RoomWalls { roomName = "Bedroom 2", colourIndex = 11, accentIndex = 12, zones = new[] { "Bedroom 2" } },
        new RoomWalls { roomName = "Main bedroom", colourIndex = 4, accentIndex = 15, zones = new[] { "Main bedroom", "Main bedroom entrance" } },
        new RoomWalls { roomName = "Kitchen", colourIndex = 0, accentIndex = 6, zones = new[] { "Kitchen" } },
    };

    // A painted strip of wall that faces into a room
    class Face
    {
        public int room;
        public Vector3 centre;     // on the wall surface
        public Vector3 normal;     // points into the room
        public Vector3 tangent;    // along the wall
        public float length, bottom, top;
    }

    Transform root;
    float H;
    readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    List<List<Face>> featureCandidates = new List<List<Face>>();

    // ------------------------------------------------------------------ public API

    public int CandidateCount(int room) =>
        room >= 0 && room < featureCandidates.Count ? featureCandidates[room].Count : 0;

    public float CandidateLength(int room, int i) =>
        i >= 0 && i < CandidateCount(room) ? featureCandidates[room][i].length : 0f;

    public void SetColour(int room, int swatch) { rooms[room].colourIndex = swatch; Rebuild(); }
    public void SetAccent(int room, int swatch) { rooms[room].accentIndex = swatch; Rebuild(); }
    public void SetFeature(int room, FeatureFinish f) { rooms[room].feature = f; Rebuild(); }
    public void StepFeatureWall(int room, int step)
    {
        int n = CandidateCount(room);
        if (n == 0) return;
        rooms[room].featureWall = ((rooms[room].featureWall + step) % n + n) % n;
        Rebuild();
    }
    public void ColourAllRooms(int swatch)
    {
        foreach (var r in rooms) r.colourIndex = swatch;
        Rebuild();
    }

    // Wait one frame so the walls, doors and windows are built first
    System.Collections.IEnumerator Start()
    {
        yield return null;
        Rebuild();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Rebuild(); };
    }
#endif

    // ------------------------------------------------------------------ build

    [ContextMenu("Apply Wall Design")]
    public void Rebuild()
    {
        var plan = GetComponent<FloorPlanBuilder>();
        H = plan != null ? plan.wallHeight : 2.6f;

        var old = transform.Find("Wall Paint");
        if (old != null) DestroyImmediate(old.gameObject);
        root = new GameObject("Wall Paint").transform;
        root.SetParent(transform, false);

        var floors = GetComponent<FloorBuilder>();
        if (floors == null) { Debug.LogWarning("WallDesigner needs Floor Builder on the same GameObject to find the rooms."); return; }

        // Collect every wall-like block: walls, beams, sills, headers and the wall around doors
        var blocks = new List<Transform>();
        var walls = transform.Find("Walls");
        if (walls != null) foreach (Transform t in walls) blocks.Add(t);
        var doors = transform.Find("Doors");
        if (doors != null)
            foreach (Transform door in doors)
                foreach (Transform part in door)
                    if (part.name.StartsWith("Wall")) blocks.Add(part);

        var faces = new List<Face>();
        foreach (var b in blocks)
        {
            if (!paintBeams && b.name.Contains("beam")) continue;
            CollectFaces(b, floors, faces);
        }

        // Feature wall candidates: full-height faces at least 1.2 m long, longest first
        featureCandidates = new List<List<Face>>();
        for (int r = 0; r < rooms.Count; r++)
        {
            var list = faces.FindAll(f => f.room == r && f.length >= 1.2f && f.bottom < 0.01f && f.top > H - 0.01f);
            list.Sort((a, b) => b.length.CompareTo(a.length));
            featureCandidates.Add(list);
        }

        foreach (var f in faces)
        {
            var room = rooms[f.room];
            Face feature = null;
            if (room.feature != FeatureFinish.None && featureCandidates[f.room].Count > 0)
                feature = featureCandidates[f.room][Mathf.Clamp(room.featureWall, 0, featureCandidates[f.room].Count - 1)];

            if (f == feature) BuildFeature(f, room);
            else Skin(f, Paint(Swatches[Mathf.Clamp(room.colourIndex, 0, Swatches.Length - 1)].colour), "Paint");
        }
    }

    // Splits the two broad faces (and the two ends) of a block into strips by room
    void CollectFaces(Transform b, FloorBuilder floors, List<Face> faces)
    {
        Vector3 pos = b.localPosition, s = b.localScale;
        Quaternion rot = b.localRotation;
        Vector3 along = rot * Vector3.right, across = rot * Vector3.forward;
        float bottom = pos.y - s.y / 2f, top = pos.y + s.y / 2f;
        if (s.y < 0.02f) return;

        // Broad faces, sampled every 5 cm along the wall
        foreach (int side in new[] { -1, 1 })
        {
            Vector3 n = across * side;
            Vector3 surface = pos + n * (s.z / 2f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(s.x / 0.05f));
            float seg = s.x / steps;
            int current = -1; float start = 0f;
            for (int i = 0; i <= steps; i++)
            {
                int room = -1;
                float t = -s.x / 2f + (i + 0.5f) * seg;
                if (i < steps)
                {
                    Vector3 probe = surface + along * t + n * 0.06f;
                    room = RoomAt(floors, probe);
                }
                if (room != current)
                {
                    if (current >= 0)
                    {
                        float a = -s.x / 2f + start * seg, e = -s.x / 2f + i * seg;
                        faces.Add(new Face { room = current, normal = n, tangent = along, length = e - a,
                            centre = surface + along * ((a + e) / 2f), bottom = bottom, top = top });
                    }
                    current = room; start = i;
                }
            }
        }

        // End faces, where a wall stops at a doorway or opening
        foreach (int side in new[] { -1, 1 })
        {
            Vector3 n = along * side;
            Vector3 surface = pos + n * (s.x / 2f);
            int room = RoomAt(floors, surface + n * 0.06f);
            if (room >= 0 && s.z > 0.05f)
                faces.Add(new Face { room = room, normal = n, tangent = across, length = s.z,
                    centre = surface, bottom = bottom, top = top });
        }
    }

    int RoomAt(FloorBuilder floors, Vector3 p)
    {
        foreach (var z in floors.zones)
        {
            float x0 = Mathf.Min(z.min.x, z.max.x), x1 = Mathf.Max(z.min.x, z.max.x);
            float z0 = Mathf.Min(z.min.y, z.max.y), z1 = Mathf.Max(z.min.y, z.max.y);
            if (p.x < x0 || p.x > x1 || p.z < z0 || p.z > z1) continue;
            for (int r = 0; r < rooms.Count; r++)
                if (rooms[r].zones != null && System.Array.IndexOf(rooms[r].zones, z.name) >= 0) return r;
            return -1;   // a zone that is not painted (bathroom, service yard, shelter)
        }
        return -1;
    }

    // ------------------------------------------------------------------ finishes

    const float Offset = 0.0015f;

    GameObject Skin(Face f, Material m, string name, float lift = Offset)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name + " - " + rooms[f.room].roomName;
        go.transform.SetParent(root, false);
        go.transform.localPosition = f.centre + f.normal * lift + Vector3.up * ((f.bottom + f.top) / 2f - f.centre.y);
        go.transform.localRotation = Quaternion.LookRotation(f.normal, Vector3.up);
        go.transform.localScale = new Vector3(f.length, f.top - f.bottom, 0.002f);
        Finish(go, m);
        return go;
    }

    void BuildFeature(Face f, RoomWalls room)
    {
        Color accent = Swatches[Mathf.Clamp(room.accentIndex, 0, Swatches.Length - 1)].colour;
        float h = f.top - f.bottom;
        switch (room.feature)
        {
            case FeatureFinish.AccentPaint:
                Skin(f, Paint(accent), "Feature paint");
                break;

            case FeatureFinish.Limewash:
                Skin(f, Textured("Limewash " + accent, LimewashTexture(), accent, new Vector2(f.length / 1.5f, h / 1.5f), 0.05f), "Feature limewash");
                break;

            case FeatureFinish.Marble:
                Skin(f, Textured("Marble " + f.length + "x" + h, MarbleTexture(), Color.white, new Vector2(f.length / 0.6f, h / 1.2f), 0.6f), "Feature marble");
                break;

            case FeatureFinish.TimberSlatsOak:
            case FeatureFinish.TimberSlatsWalnut:
            {
                // Dark backing with vertical timber slats in front of it
                Skin(f, Paint(new Color(0.12f, 0.11f, 0.10f)), "Slat backing");
                Color wood = room.feature == FeatureFinish.TimberSlatsOak ? new Color(0.78f, 0.62f, 0.45f) : new Color(0.36f, 0.24f, 0.16f);
                var woodMat = Textured("Slat " + wood, WoodTexture(), wood, Vector2.one, 0.3f);
                float pitch = 0.05f, slatW = 0.03f, depth = 0.02f;
                int n = Mathf.FloorToInt((f.length - 0.02f) / pitch);
                float startT = -((n - 1) * pitch) / 2f;
                for (int i = 0; i < n; i++)
                {
                    var slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slat.name = "Timber slat";
                    slat.transform.SetParent(root, false);
                    slat.transform.localPosition = f.centre + f.tangent * (startT + i * pitch) + f.normal * (depth / 2f + 0.003f)
                                                   + Vector3.up * ((f.bottom + f.top) / 2f - f.centre.y);
                    slat.transform.localRotation = Quaternion.LookRotation(f.normal, Vector3.up);
                    slat.transform.localScale = new Vector3(slatW, h, depth);
                    Finish(slat, woodMat);
                }
                break;
            }

            case FeatureFinish.FlutedPanel:
            {
                // Rows of half-round flutes in the accent colour
                var m = Paint(accent, 0.25f);
                Skin(f, m, "Fluted backing");
                float pitch = 0.032f, r = 0.014f;
                int n = Mathf.FloorToInt((f.length - 0.02f) / pitch);
                float startT = -((n - 1) * pitch) / 2f;
                for (int i = 0; i < n; i++)
                {
                    var flute = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    flute.name = "Flute";
                    flute.transform.SetParent(root, false);
                    flute.transform.localPosition = f.centre + f.tangent * (startT + i * pitch) + f.normal * 0.002f
                                                    + Vector3.up * ((f.bottom + f.top) / 2f - f.centre.y);
                    flute.transform.localScale = new Vector3(r * 2f, h / 2f, r * 2f);
                    Finish(flute, m);
                }
                break;
            }
        }
    }

    static void Finish(GameObject go, Material m)
    {
        var rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = m;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        DestroyImmediate(go.GetComponent<Collider>());
    }

    // ------------------------------------------------------------------ materials and textures

    Material Paint(Color c, float smooth = 0.12f)
    {
        string key = "Paint " + c + smooth;
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = NewLit(key);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        mats[key] = m;
        return m;
    }

    Material Textured(string key, Texture2D tex, Color tint, Vector2 tiling, float smooth)
    {
        key += tiling;
        if (mats.TryGetValue(key, out var m) && m != null) return m;
        m = NewLit(key);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smooth);
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", tiling);
        m.mainTexture = tex;
        m.mainTextureScale = tiling;
        mats[key] = m;
        return m;
    }

    static Material NewLit(string name)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        return new Material(shader) { name = name };
    }

    static Texture2D limewash, marble, wood;

    static Texture2D LimewashTexture()
    {
        if (limewash != null) return limewash;
        const int n = 256;
        limewash = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Limewash" };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n, v = y / (float)n;
                // Tileable cloudy pattern from wrapped noise
                float a = Tile(u, v, 3f) * 0.6f + Tile(u, v, 9f) * 0.3f + Tile(u, v, 24f) * 0.1f;
                float k = 0.82f + a * 0.3f;
                px[y * n + x] = new Color(k, k, k);
            }
        limewash.SetPixels(px);
        limewash.Apply();
        return limewash;
    }

    static Texture2D MarbleTexture()
    {
        if (marble != null) return marble;
        const int w = 256, h = 512;   // one 600 x 1200 mm tile
        marble = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Marble" };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.012f + 10f, y * 0.012f) * 6f + Mathf.PerlinNoise(x * 0.05f, y * 0.05f + 5f);
                float vein = Mathf.Pow(Mathf.Abs(Mathf.Sin((x * 0.02f + y * 0.01f + n) * 1.6f)), 0.18f);
                float k = Mathf.Lerp(0.62f, 0.97f, vein);
                var c = new Color(k, k * 0.995f, k * 0.985f);
                if (x < 1 || y < 1) c = new Color(0.78f, 0.77f, 0.75f);   // grout line
                px[y * w + x] = c;
            }
        marble.SetPixels(px);
        marble.Apply();
        return marble;
    }

    static Texture2D WoodTexture()
    {
        if (wood != null) return wood;
        const int w = 64, h = 256;
        wood = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, name = "Slat wood" };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float g = Mathf.PerlinNoise(x * 0.35f, y * 0.02f) * 0.6f + Mathf.PerlinNoise(x * 1.3f, y * 0.1f) * 0.4f;
                float k = 0.82f + g * 0.3f;
                px[y * w + x] = new Color(k, k, k);
            }
        wood.SetPixels(px);
        wood.Apply();
        return wood;
    }

    // Perlin noise that repeats across the texture edges
    static float Tile(float u, float v, float f)
    {
        float a = Mathf.PerlinNoise(u * f, v * f);
        float b = Mathf.PerlinNoise((u - 1f) * f, v * f);
        float c = Mathf.PerlinNoise(u * f, (v - 1f) * f);
        float d = Mathf.PerlinNoise((u - 1f) * f, (v - 1f) * f);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }
}
