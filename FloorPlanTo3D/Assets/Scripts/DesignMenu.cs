using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// In-game design menu. Press Tab to open or close it.
// A strip of icons on the left picks the category; the panel beside it shows the options.
// Put this on any GameObject in the scene (for example FloorPlanBuilder).
public class DesignMenu : MonoBehaviour
{
    enum Category { Ceiling, Lighting, Walls, Carpentry }

    public LivingCeilingDesigner ceiling;   // the room currently shown; found automatically
    LivingCeilingDesigner[] rooms = new LivingCeilingDesigner[0];
    public bool startOpen = false;

    bool open;
    Category category = Category.Ceiling;
    Vector2 scroll;

    // Look
    static readonly Color Strip = new Color(0.11f, 0.11f, 0.12f, 0.97f);
    static readonly Color Panel = new Color(0.16f, 0.16f, 0.18f, 0.95f);
    static readonly Color Card = new Color(0.22f, 0.22f, 0.24f, 1f);
    static readonly Color CardHover = new Color(0.27f, 0.27f, 0.30f, 1f);
    static readonly Color Accent = new Color(0.85f, 0.64f, 0.26f, 1f);
    static readonly Color TextMain = new Color(0.95f, 0.94f, 0.92f);
    static readonly Color TextDim = new Color(0.68f, 0.67f, 0.65f);

    const float StripW = 78f, PanelW = 360f;

    Texture2D white;
    Texture2D[] icons;
    GUIStyle title, heading, body, small, iconLabel, cardName, cardBody, chip;
    bool stylesReady;

    void Start()
    {
        FindRooms();
        white = Texture2D.whiteTexture;
        icons = new[] { Icons.Ceiling(), Icons.Lighting(), Icons.Walls(), Icons.Carpentry() };
        if (startOpen) SetOpen(true);
    }

    void FindRooms()
    {
        rooms = FindObjectsByType<LivingCeilingDesigner>(FindObjectsSortMode.None);
        System.Array.Sort(rooms, (a, b) => a.menuOrder.CompareTo(b.menuOrder));
        if (ceiling == null && rooms.Length > 0) ceiling = rooms[0];
    }

    void Update()
    {
        if (TogglePressed()) SetOpen(!open);
        if (open) KeepCursorFree();
    }

    // Other scripts (or the editor) may try to lock the cursor again,
    // so while the menu is open we keep it free every frame.
    void LateUpdate()
    {
        if (open) KeepCursorFree();
    }

    void KeepCursorFree()
    {
        FirstPersonController.InputBlocked = true;
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    void SetOpen(bool value)
    {
        open = value;
        FirstPersonController.InputBlocked = open;
        FirstPersonController.LockCursor(!open);
    }

    bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    // ------------------------------------------------------------------ drawing

    void OnGUI()
    {
        SetupStyles();

        // Scale the menu for large screens
        float scale = Mathf.Clamp(Screen.height / 900f, 1f, 2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float h = Screen.height / scale;

        if (!open)
        {
            var hint = new Rect(16, h - 44, 180, 30);
            Fill(hint, new Color(0, 0, 0, 0.55f));
            GUI.Label(new Rect(hint.x + 10, hint.y + 6, 170, 20), "Tab   Design menu", small);
            return;
        }

        DrawStrip(h);
        DrawPanel(h);
    }

    void DrawStrip(float h)
    {
        Fill(new Rect(0, 0, StripW, h), Strip);
        GUI.Label(new Rect(0, 14, StripW, 20), "DESIGN", title);

        string[] names = { "Ceiling", "Lighting", "Walls", "Carpentry" };
        for (int i = 0; i < names.Length; i++)
        {
            var r = new Rect(8, 54 + i * 86, StripW - 16, 74);
            var cat = (Category)i;
            bool selected = cat == category;
            bool hover = r.Contains(Event.current.mousePosition);

            if (selected) Fill(r, new Color(1, 1, 1, 0.09f));
            else if (hover) Fill(r, new Color(1, 1, 1, 0.05f));
            if (selected) Fill(new Rect(0, r.y + 8, 3, r.height - 16), Accent);

            var old = GUI.color;
            GUI.color = selected ? Accent : (Available(cat) ? TextMain : TextDim);
            GUI.DrawTexture(new Rect(r.x + (r.width - 34) / 2f, r.y + 10, 34, 34), icons[i]);
            GUI.color = old;
            iconLabel.normal.textColor = selected ? Accent : (Available(cat) ? TextMain : TextDim);
            GUI.Label(new Rect(r.x, r.y + 48, r.width, 18), names[i], iconLabel);

            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                category = cat;
                scroll = Vector2.zero;
            }
        }

        GUI.Label(new Rect(0, h - 30, StripW, 20), "Tab to close", iconLabel);
    }

    void DrawPanel(float h)
    {
        var p = new Rect(StripW, 0, PanelW, h);
        Fill(p, Panel);

        string roomLabel = ceiling != null ? ceiling.roomName.ToUpper() : "ROOM";
        GUI.Label(new Rect(p.x + 20, 16, PanelW - 40, 16), roomLabel, small);
        string[] titles = { "Ceiling style", "Lighting", "Wall colours", "Carpentry" };
        GUI.Label(new Rect(p.x + 20, 34, PanelW - 40, 28), titles[(int)category], heading);

        // Room tabs
        float top = 72;
        if (rooms.Length > 1)
        {
            // Up to three tabs per row
            int cols = Mathf.Min(3, rooms.Length);
            int rowsN = Mathf.CeilToInt(rooms.Length / (float)cols);
            float tabW = (PanelW - 40) / cols;
            for (int i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                if (room == null) continue;
                var r = new Rect(p.x + 20 + (i % cols) * tabW, top + (i / cols) * 34, tabW - 4, 30);
                bool selected = room == ceiling;
                bool hover = r.Contains(Event.current.mousePosition);
                Fill(r, selected ? Accent : (hover ? CardHover : Card));
                var st = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };
                st.normal.textColor = selected ? new Color(0.1f, 0.1f, 0.1f) : TextMain;
                GUI.Label(r, ShortName(room), st);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !selected)
                {
                    ceiling = room;
                    scroll = Vector2.zero;
                }
            }
            top += rowsN * 34 + 6;
        }
        Fill(new Rect(p.x + 20, top, PanelW - 40, 1), new Color(1, 1, 1, 0.08f));

        var area = new Rect(p.x, top + 10, PanelW, h - top - 10);

        if (ceiling == null)
        {
            GUI.Label(new Rect(area.x + 20, area.y + 10, PanelW - 40, 60),
                "Add Living Ceiling Designer to the FloorPlanBuilder object to use this menu.", body);
            return;
        }

        switch (category)
        {
            case Category.Ceiling: DrawCeiling(area); break;
            case Category.Lighting: DrawLighting(area); break;
            default:
                GUI.Label(new Rect(area.x + 20, area.y + 10, PanelW - 40, 60),
                    "Coming soon. Options for this category will appear here.", body);
                break;
        }
    }

    void DrawCeiling(Rect area)
    {
        const float cardH = 92f, gap = 10f;
        var styles = LivingCeilingDesigner.Styles;
        float contentH = styles.Length * (cardH + gap) + 10;

        scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, PanelW - 20, contentH), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
        for (int i = 0; i < styles.Length; i++)
        {
            var info = styles[i];
            var r = new Rect(20, i * (cardH + gap), PanelW - 50, cardH);
            bool selected = ceiling.style == info.style;
            bool hover = r.Contains(Event.current.mousePosition);

            Fill(r, hover && !selected ? CardHover : Card);
            if (selected) Outline(r, Accent, 2f);

            // Colour swatches
            for (int s = 0; s < info.palette.Length; s++)
                Fill(new Rect(r.x + 14 + s * 20, r.y + 14, 16, 16), info.palette[s]);

            GUI.Label(new Rect(r.x + 82, r.y + 10, r.width - 96, 22), info.name, cardName);
            chip.normal.textColor = TextDim;
            GUI.Label(new Rect(r.xMax - 70, r.y + 12, 60, 18), info.kelvin + "K", chip);
            GUI.Label(new Rect(r.x + 14, r.y + 38, r.width - 28, cardH - 44), info.description, cardBody);

            if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !selected)
                ceiling.SetStyle(info.style);
        }
        GUI.EndScrollView();
    }

    void DrawLighting(Rect area)
    {
        float x = 20, w = PanelW - 50;
        float contentH = 4 * 64 + 90 + 5 * 42 + 60;
        scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, PanelW - 20, contentH), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
        float y = 4;

        y = ToggleRow(x, y, w, "LED strips", "Cove lights and LED lines", ceiling.ledOn, ceiling.SetLed);
        y = ToggleRow(x, y, w, "Downlights and spotlights", "Recessed downlights and track spots", ceiling.downlightsOn, ceiling.SetDownlights);
        y = ToggleRow(x, y, w, "Pendants and fan light", "Pendants, chandeliers and the fan's light", ceiling.featureLightsOn, ceiling.SetFeatureLights);
        y = ToggleRow(x, y, w, "Ceiling fan", "Spin the fan blades", ceiling.fanOn, ceiling.SetFan);

        // Brightness: Low / Medium / High
        y += 6;
        GUI.Label(new Rect(x, y, w, 20), "Brightness", cardName);
        y += 26;
        string[] levels = { "Low", "Medium", "High" };
        float segW = w / 3f;
        for (int i = 0; i < 3; i++)
        {
            var r = new Rect(x + i * segW, y, segW - 4, 34);
            bool selected = (int)ceiling.brightness == i;
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, selected ? Accent : (hover ? CardHover : Card));
            var st = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = selected ? new Color(0.1f, 0.1f, 0.1f) : TextMain;
            GUI.Label(r, levels[i], st);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !selected)
                ceiling.SetBrightness((LivingCeilingDesigner.Brightness)i);
        }
        y += 50;

        GUI.Label(new Rect(x, y, w, 20), "Colour temperature", cardName);
        y += 26;

        int[] values = { 0, 2700, 3000, 4000, 5000 };
        string[] labels = { "Style default", "2700K  Warm", "3000K  Soft warm", "4000K  Neutral", "5000K  Cool daylight" };
        for (int i = 0; i < values.Length; i++)
        {
            var r = new Rect(x, y, w, 36);
            bool selected = ceiling.kelvinOverride == values[i];
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, hover && !selected ? CardHover : Card);
            if (selected) Outline(r, Accent, 2f);

            Color swatch = values[i] == 0
                ? Mathf.CorrelatedColorTemperatureToRGB(LivingCeilingDesigner.Info(ceiling.style).kelvin)
                : Mathf.CorrelatedColorTemperatureToRGB(values[i]);
            Fill(new Rect(r.x + 12, r.y + 10, 16, 16), swatch);
            GUI.Label(new Rect(r.x + 40, r.y + 8, w - 50, 20), labels[i], body);

            if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !selected)
                ceiling.SetKelvin(values[i]);
            y += 42;
        }
        GUI.EndScrollView();
    }

    float ToggleRow(float x, float y, float w, string label, string hint, bool value, System.Action<bool> set)
    {
        var r = new Rect(x, y, w, 56);
        Fill(r, Card);
        GUI.Label(new Rect(r.x + 14, r.y + 8, w - 90, 20), label, cardName);
        GUI.Label(new Rect(r.x + 14, r.y + 30, w - 90, 18), hint, cardBody);

        // Switch
        var sw = new Rect(r.xMax - 60, r.y + 17, 44, 22);
        Fill(sw, value ? Accent : new Color(0.4f, 0.4f, 0.42f));
        Fill(new Rect(value ? sw.xMax - 20 : sw.x + 2, sw.y + 2, 18, 18), TextMain);
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) set(!value);
        return y + 64;
    }

    // ------------------------------------------------------------------ helpers

    static string ShortName(LivingCeilingDesigner room)
    {
        if (room.kind == LivingCeilingDesigner.RoomKind.LivingDining) return "Living";
        if (room.kind == LivingCeilingDesigner.RoomKind.Lobby) return "Lobby";
        return room.roomName.Replace("bedroom", "bed").Replace("Bedroom", "Bed");
    }

    bool Available(Category c) => c == Category.Ceiling || c == Category.Lighting;

    void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = old;
    }

    void Outline(Rect r, Color c, float t)
    {
        Fill(new Rect(r.x, r.y, r.width, t), c);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
        Fill(new Rect(r.x, r.y, t, r.height), c);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    void SetupStyles()
    {
        if (stylesReady) return;
        stylesReady = true;
        title = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        title.normal.textColor = Accent;
        heading = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        heading.normal.textColor = TextMain;
        body = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        body.normal.textColor = TextMain;
        small = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
        small.normal.textColor = TextDim;
        iconLabel = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
        iconLabel.normal.textColor = TextDim;
        cardName = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        cardName.normal.textColor = TextMain;
        cardBody = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
        cardBody.normal.textColor = TextDim;
        chip = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleRight };
    }

    // ------------------------------------------------------------------ icons drawn in code

    static class Icons
    {
        const int S = 48;

        static Texture2D New()
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color[S * S];
            t.SetPixels(clear);
            return t;
        }

        // Coordinates from the top-left, like the screen
        static void R(Texture2D t, int x, int y, int w, int h)
        {
            for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    if (i >= 0 && i < S && j >= 0 && j < S) t.SetPixel(i, S - 1 - j, Color.white);
        }

        static void Disc(Texture2D t, int cx, int cy, int r)
        {
            for (int i = -r; i <= r; i++)
                for (int j = -r; j <= r; j++)
                    if (i * i + j * j <= r * r) R(t, cx + i, cy + j, 1, 1);
        }

        static Texture2D Done(Texture2D t) { t.Apply(); return t; }

        public static Texture2D Ceiling()
        {
            var t = New();
            R(t, 3, 6, 42, 3);                         // ceiling slab
            R(t, 3, 9, 11, 7); R(t, 34, 9, 11, 7);    // box-up on both sides
            R(t, 15, 10, 2, 2); R(t, 31, 10, 2, 2);   // cove LED dots
            R(t, 23, 9, 2, 10);                        // pendant wire
            R(t, 16, 19, 16, 3); R(t, 18, 22, 12, 3);  // shade
            R(t, 22, 28, 4, 2); R(t, 14, 30, 3, 2); R(t, 31, 30, 3, 2); R(t, 10, 34, 3, 2); R(t, 35, 34, 3, 2); // light rays
            R(t, 6, 18, 2, 2); R(t, 40, 18, 2, 2);     // downlights
            return Done(t);
        }

        public static Texture2D Lighting()
        {
            var t = New();
            Disc(t, 24, 19, 11);
            R(t, 18, 29, 12, 4); R(t, 19, 34, 10, 2); R(t, 20, 37, 8, 2); R(t, 22, 40, 4, 2);
            R(t, 23, 1, 2, 4); R(t, 4, 18, 5, 2); R(t, 39, 18, 5, 2);
            R(t, 9, 5, 3, 3); R(t, 36, 5, 3, 3);
            return Done(t);
        }

        public static Texture2D Walls()
        {
            var t = New();
            for (int row = 0; row < 5; row++)
            {
                int y = 6 + row * 7;
                int off = row % 2 == 0 ? 0 : -7;
                for (int x = off; x < S; x += 14) R(t, x + 3, y, 12, 5);
            }
            return Done(t);
        }

        public static Texture2D Carpentry()
        {
            var t = New();
            R(t, 8, 5, 32, 3); R(t, 8, 39, 32, 3); R(t, 8, 5, 3, 37); R(t, 37, 5, 3, 37);
            R(t, 23, 8, 2, 31);
            R(t, 18, 20, 2, 6); R(t, 28, 20, 2, 6);
            R(t, 10, 42, 3, 4); R(t, 35, 42, 3, 4);
            return Done(t);
        }
    }
}
