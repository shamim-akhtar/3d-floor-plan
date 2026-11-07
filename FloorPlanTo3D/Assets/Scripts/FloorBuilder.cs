using UnityEngine;

// Builds the floor finishes room by room, based on what HDB provides in a new 5-room BTO flat:
//  - Living/dining (and study, foyer, corridor): polished porcelain tiles (Optional Component Scheme)
//  - Bedrooms: vinyl strip flooring (Optional Component Scheme)
//  - Kitchen: glazed porcelain tiles (standard)
//  - Bathrooms, service yard, household shelter: glazed porcelain tiles (standard)
//  - Air-con ledge: cement screed
// Put this on the same GameObject as FloorPlanBuilder. Units are metres, same coordinates.
// Texture coordinates are set in metres, so tiles line up across rooms and keep their real size.
public class FloorBuilder : MonoBehaviour
{
    public enum Finish { LivingPorcelain, BedroomVinyl, KitchenPorcelain, WetAreaPorcelain, Cement }

    [System.Serializable]
    public class Zone
    {
        public string name;
        public Finish finish;
        public Vector2 min;   // (x, z) of one corner
        public Vector2 max;   // (x, z) of the opposite corner
    }

    [System.Serializable]
    public class FinishSettings
    {
        public Material material;
        public Vector2 patternSize = new Vector2(0.6f, 0.6f);  // real size of one texture repeat, metres
        public Color fallbackColour = Color.grey;
    }

    public float floorLevel = 0.005f;   // just above the ground plane to avoid flickering

    [Header("Finishes")]
    public FinishSettings livingPorcelain = new FinishSettings
        { patternSize = new Vector2(0.6f, 0.6f), fallbackColour = new Color(0.87f, 0.84f, 0.78f) };
    public FinishSettings bedroomVinyl = new FinishSettings
        { patternSize = new Vector2(1.2f, 1.2f), fallbackColour = new Color(0.72f, 0.56f, 0.40f) };
    public FinishSettings kitchenPorcelain = new FinishSettings
        { patternSize = new Vector2(0.6f, 0.6f), fallbackColour = new Color(0.63f, 0.61f, 0.58f) };
    public FinishSettings wetAreaPorcelain = new FinishSettings
        { patternSize = new Vector2(0.3f, 0.3f), fallbackColour = new Color(0.50f, 0.50f, 0.49f) };
    public FinishSettings cement = new FinishSettings
        { patternSize = new Vector2(1.0f, 1.0f), fallbackColour = new Color(0.62f, 0.62f, 0.62f) };

    // Rooms are rectangles measured to the wall centre lines. Walls sit on top, so small
    // overlaps under walls do not show. Zones must not overlap each other in open areas.
    public Zone[] zones =
    {
        new Zone { name = "Living and study",          finish = Finish.LivingPorcelain,  min = new Vector2(-0.04f, 5.7f),  max = new Vector2(5.425f, 9.425f) },
        new Zone { name = "Living beside shelter",     finish = Finish.LivingPorcelain,  min = new Vector2(-0.04f, 4.605f), max = new Vector2(1.52f, 5.7f) },
        new Zone { name = "Dining",                    finish = Finish.LivingPorcelain,  min = new Vector2(1.52f, 2.815f),  max = new Vector2(7.97f, 5.7f) },
        new Zone { name = "Entrance foyer",            finish = Finish.LivingPorcelain,  min = new Vector2(1.52f, 1.975f),  max = new Vector2(3.82f, 2.815f) },
        new Zone { name = "Bedroom corridor",          finish = Finish.LivingPorcelain,  min = new Vector2(7.97f, 4.59f),   max = new Vector2(10.04f, 5.7f) },
        new Zone { name = "Bedroom 3",                 finish = Finish.BedroomVinyl,     min = new Vector2(5.425f, 5.7f),   max = new Vector2(8.525f, 9.425f) },
        new Zone { name = "Bedroom 2",                 finish = Finish.BedroomVinyl,     min = new Vector2(8.525f, 5.7f),   max = new Vector2(11.575f, 9.425f) },
        new Zone { name = "Main bedroom",              finish = Finish.BedroomVinyl,     min = new Vector2(11.575f, 4.59f), max = new Vector2(14.625f, 9.425f) },
        new Zone { name = "Main bedroom entrance",     finish = Finish.BedroomVinyl,     min = new Vector2(10.04f, 4.59f),  max = new Vector2(11.575f, 5.7f) },
        new Zone { name = "Kitchen",                   finish = Finish.KitchenPorcelain, min = new Vector2(4.12f, 0.15f),   max = new Vector2(7.85f, 2.815f) },
        new Zone { name = "Bath/WC 2",                 finish = Finish.WetAreaPorcelain, min = new Vector2(7.97f, 2.64f),   max = new Vector2(10.04f, 4.59f) },
        new Zone { name = "Bath/WC 1",                 finish = Finish.WetAreaPorcelain, min = new Vector2(10.04f, 2.64f),  max = new Vector2(12.57f, 4.59f) },
        new Zone { name = "Service yard",              finish = Finish.WetAreaPorcelain, min = new Vector2(7.85f, 0.15f),   max = new Vector2(10.0f, 2.64f) },
        new Zone { name = "Household shelter",         finish = Finish.WetAreaPorcelain, min = new Vector2(-0.04f, 1.965f), max = new Vector2(1.52f, 4.605f) },
        new Zone { name = "Air-con ledge",             finish = Finish.Cement,           min = new Vector2(10.0f, 1.655f),  max = new Vector2(12.66f, 2.64f) },
    };

    void Start()
    {
        if (transform.Find("Floors") == null) Build();
    }

    [ContextMenu("Build Floors")]
    public void Build()
    {
        var old = transform.Find("Floors");
        if (old != null) DestroyImmediate(old.gameObject);

        var root = new GameObject("Floors").transform;
        root.SetParent(transform, false);

        foreach (var z in zones) BuildZone(root, z);
    }

    FinishSettings Settings(Finish f)
    {
        switch (f)
        {
            case Finish.LivingPorcelain: return livingPorcelain;
            case Finish.BedroomVinyl: return bedroomVinyl;
            case Finish.KitchenPorcelain: return kitchenPorcelain;
            case Finish.WetAreaPorcelain: return wetAreaPorcelain;
            default: return cement;
        }
    }

    void BuildZone(Transform root, Zone z)
    {
        var s = Settings(z.finish);
        float x0 = Mathf.Min(z.min.x, z.max.x), x1 = Mathf.Max(z.min.x, z.max.x);
        float z0 = Mathf.Min(z.min.y, z.max.y), z1 = Mathf.Max(z.min.y, z.max.y);
        float y = floorLevel;

        var mesh = new Mesh { name = z.name + " floor" };
        mesh.vertices = new[]
        {
            new Vector3(x0, y, z0), new Vector3(x0, y, z1),
            new Vector3(x1, y, z1), new Vector3(x1, y, z0),
        };
        // UVs in metres divided by the pattern size, so every room uses the same tile grid
        Vector2 p = new Vector2(Mathf.Max(0.01f, s.patternSize.x), Mathf.Max(0.01f, s.patternSize.y));
        mesh.uv = new[]
        {
            new Vector2(x0 / p.x, z0 / p.y), new Vector2(x0 / p.x, z1 / p.y),
            new Vector2(x1 / p.x, z1 / p.y), new Vector2(x1 / p.x, z0 / p.y),
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        var go = new GameObject(z.name);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = go.AddComponent<MeshRenderer>();
        rend.sharedMaterial = s.material != null ? s.material : Fallback(s.fallbackColour);

        // Thin box so the player can stand on it even without the ground plane
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3((x0 + x1) / 2f, y - 0.01f, (z0 + z1) / 2f);
        col.size = new Vector3(x1 - x0, 0.02f, z1 - z0);
    }

    // Only used if no material is assigned. Not saved as an asset.
    static Material Fallback(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", c);
        mat.color = c;
        return mat;
    }
}
