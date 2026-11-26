using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static UIMeshDrawUtils;

public enum ContactType
{
    Friendly,
    Enemy,
    Unknown,
    Neutral
}

public struct MapContact
{
    public Vector2 Position;
    public ContactType Type;
}

public class UIGridMap : MaskableGraphic
{
    [Header("Map Bounds")]
    [SerializeField] private float mapHalfExtent = 50f;
    public float MapHalfExtent => mapHalfExtent;
    public float MapExtent => mapHalfExtent * 2f;

    [Header("Monitor")]
    [SerializeField] private string monitorId = "NAVIGATION";

    [Header("Grid Layout")]
    [Range(2, 50)] public int columns = 5;
    [Range(2, 50)] public int rows = 5;
    [SerializeField] private Vector2 gridOffset = new(0.05f, 0.15f);
    [SerializeField] private Vector2 gridSize = new(0.65f, 0.75f);

    [Header("Grid Appearance")]
    public float lineThickness = 1.5f;
    public float borderThickness = 3f;
    public Color backgroundColor = Color.black;
    public Color gridColor = new(0.1f, 0.8f, 0.3f, 0.45f);
    public Color borderColor = new(0.2f, 1f, 0.4f, 1f);

    [Header("Contacts")]
    public float contactSize = 8f;
    public float contactThickness = 2f;

    [Header("Unit Colors")]
    public Color unknownColor = Color.yellow;
    public Color tankerColor = Color.cyan;
    public Color shadowTankerColor = new(1f, 0.55f, 0.1f);
    public Color enemyColor = Color.red;
    public Color torpedoColor = new(1f, 0.3f, 1f);

    [Header("Legacy Contact Colors")]
    public Color friendlyColor = Color.cyan;
    public Color neutralColor = Color.white;

    [Header("Selection")]
    public Color selectionColor = Color.white;
    public float selectionRadius = 14f;
    public float selectionThickness = 2f;

    [Header("Click Feedback")]
    [SerializeField] private TextMeshProUGUI coordinatesText;
    [SerializeField] private string coordinateFormat = "X: {0:F1}  Y: {1:F1}";

    [SerializeField] private float waveSpeed = 180f;
    [SerializeField] private float waveLifetime = 0.7f;
    [SerializeField] private float waveThickness = 2f;
    [SerializeField] private Color waveColor = new(0.2f, 1f, 0.4f, 1f);

    [Header("Radar Range Display")]
    [Tooltip("Ссылка на монитор РЛС. Из него берутся центр и радиус.")]
    [SerializeField] private UIRadarMonitor radarMonitor;
    [SerializeField] private bool showRadarRange = true;
    [SerializeField] private Color radarRangeColor = new(0.2f, 1f, 0.4f, 0.7f);
    [SerializeField] private float radarRangeThickness = 1.5f;
    [Range(6, 120)][SerializeField] private int radarRangeDashCount = 48;
    [Range(0.1f, 0.9f)][SerializeField] private float radarRangeDashFill = 0.5f;

    [SerializeField] private UnitManager unitManager;

    private readonly Dictionary<Vector2Int, MapContact> manualContacts = new();
    private const float ContactQuantum = 0.1f;

    private MapUnit selectedUnit;
    private Vector2 waveMapPosition;
    private float waveTimer = -1f;

    public MapUnit SelectedUnit => selectedUnit;
    public System.Action<MapUnit> OnUnitSelected;

    protected override void OnEnable()
    {
        base.OnEnable();
        MonitorInputManager.OnMonitorClick += HandleMonitorClick;
        SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        MonitorInputManager.OnMonitorClick -= HandleMonitorClick;
        base.OnDisable();
    }

    private void Update()
    {
        SetVerticesDirty();
        if (waveTimer < 0f) return;
        waveTimer += Time.deltaTime;
        if (waveTimer >= waveLifetime) waveTimer = -1f;
    }

    // ---------------- Grid ----------------

    public Rect GetGridRect()
    {
        Rect monitor = GetPixelAdjustedRect();
        return new Rect(
            monitor.xMin + monitor.width * gridOffset.x,
            monitor.yMin + monitor.height * gridOffset.y,
            monitor.width * gridSize.x,
            monitor.height * gridSize.y);
    }

    public Vector2 LocalToMap(Vector2 local)
    {
        Rect grid = GetGridRect();
        if (grid.width <= 0f || grid.height <= 0f) return Vector2.zero;

        float nx = (local.x - grid.xMin) / grid.width;
        float ny = (local.y - grid.yMin) / grid.height;
        return new Vector2(
            Mathf.Lerp(-mapHalfExtent, mapHalfExtent, nx),
            Mathf.Lerp(-mapHalfExtent, mapHalfExtent, ny));
    }

    public Vector2 MapToLocal(Vector2 mapPos)
    {
        Rect grid = GetGridRect();
        if (grid.width <= 0f || grid.height <= 0f) return Vector2.zero;

        float nx = Mathf.InverseLerp(-mapHalfExtent, mapHalfExtent, mapPos.x);
        float ny = Mathf.InverseLerp(-mapHalfExtent, mapHalfExtent, mapPos.y);
        return new Vector2(grid.xMin + nx * grid.width, grid.yMin + ny * grid.height);
    }

    public bool IsInsideMap(Vector2 mapPos) =>
        mapPos.x >= -mapHalfExtent && mapPos.x <= mapHalfExtent &&
        mapPos.y >= -mapHalfExtent && mapPos.y <= mapHalfExtent;

    // ---------------- Manual contacts ----------------

    public void AddContact(Vector2 mapPos, ContactType type)
    {
        if (!IsInsideMap(mapPos)) return;
        manualContacts[ContactKey(mapPos)] = new MapContact { Position = mapPos, Type = type };
        SetVerticesDirty();
    }

    public void RemoveContact(Vector2 mapPos)
    {
        manualContacts.Remove(ContactKey(mapPos));
        SetVerticesDirty();
    }

    public void ClearContacts()
    {
        manualContacts.Clear();
        SetVerticesDirty();
    }

    public bool HasContact(Vector2 mapPos) => manualContacts.ContainsKey(ContactKey(mapPos));

    private static Vector2Int ContactKey(Vector2 pos) => new(
        Mathf.RoundToInt(pos.x / ContactQuantum),
        Mathf.RoundToInt(pos.y / ContactQuantum));

    // ---------------- Selection ----------------

    public void ClearSelection()
    {
        if (selectedUnit == null) return;
        selectedUnit = null;
        OnUnitSelected?.Invoke(null);
        SetVerticesDirty();
    }

    public void PlayWave(Vector2 mapPos)
    {
        if (!IsInsideMap(mapPos)) return;
        waveMapPosition = mapPos;
        waveTimer = 0f;
        SetVerticesDirty();
    }

    private MapUnit GetUnitAtPosition(Vector2 mapPos)
    {
        if (unitManager == null) return null;

        Rect grid = GetGridRect();
        float pixelRadius = contactSize + 12f;
        float mapRadius = grid.width > 0f
            ? (pixelRadius / grid.width) * (mapHalfExtent * 2f)
            : pixelRadius;

        MapUnit closest = null;
        float closestDist = float.MaxValue;

        foreach (var unit in unitManager.Units)
        {
            if (unit == null || !unit.IsDetected) continue;
            float d = Vector2.Distance(unit.Position, mapPos);
            if (d <= mapRadius && d < closestDist) { closestDist = d; closest = unit; }
        }

        return closest;
    }

    private void HandleMonitorClick(MonitorClickInfo info)
    {
        if (info.MonitorId != monitorId) return;
        if (info.Distance > 1.5f) return;

        Rect monitor = GetPixelAdjustedRect();
        Rect grid = GetGridRect();

        Vector2 localPos = new(
            monitor.xMin + info.UV.x * monitor.width,
            monitor.yMin + info.UV.y * monitor.height);

        if (!grid.Contains(localPos)) return;

        Vector2 mapPos = LocalToMap(localPos);

        waveMapPosition = mapPos;
        waveTimer = 0f;

        MapUnit clicked = GetUnitAtPosition(mapPos);
        if (clicked != selectedUnit)
        {
            selectedUnit = clicked;
            OnUnitSelected?.Invoke(selectedUnit);
        }

        if (coordinatesText != null)
        {
            coordinatesText.text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                coordinateFormat, mapPos.x, mapPos.y);
        }

        SetVerticesDirty();
    }

    // ---------------- Mesh ----------------

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect grid = GetGridRect();
        if (grid.width <= 0 || grid.height <= 0) return;

        DrawRect(vh, grid, backgroundColor);

        float cellWidth = grid.width / columns;
        float cellHeight = grid.height / rows;

        for (int i = 1; i < columns; i++)
        {
            float x = grid.xMin + cellWidth * i;
            DrawLine(vh, new Vector2(x, grid.yMin), new Vector2(x, grid.yMax), lineThickness, gridColor);
        }
        for (int i = 1; i < rows; i++)
        {
            float y = grid.yMin + cellHeight * i;
            DrawLine(vh, new Vector2(grid.xMin, y), new Vector2(grid.xMax, y), lineThickness, gridColor);
        }

        DrawRectOutline(vh, grid, borderThickness, borderColor);

        // -------- Зона работы РЛС (радиус и центр берём из радара) --------
        if (showRadarRange && radarMonitor != null && radarMonitor.MaxRangeKm > 0.1f)
        {
            float pxPerMapUnit = grid.width / (mapHalfExtent * 2f);
            Vector2 radarCenterPx = MapToLocal(radarMonitor.RadarCenterMap);

            DrawDashedCircle(vh, radarCenterPx, radarMonitor.MaxRangeKm * pxPerMapUnit,
                radarRangeThickness, radarRangeColor,
                radarRangeDashCount, radarRangeDashFill);
        }

        foreach (var pair in manualContacts)
            DrawLegacyContact(vh, MapToLocal(pair.Value.Position), pair.Value.Type);

        if (unitManager != null)
        {
            foreach (var unit in unitManager.Units)
            {
                if (unit == null || !unit.IsDetected) continue;
                if (!IsInsideMap(unit.Position)) continue;
                DrawUnit(vh, MapToLocal(unit.Position), unit);
            }
        }

        if (selectedUnit != null && IsInsideMap(selectedUnit.Position))
        {
            Vector2 selLocal = MapToLocal(selectedUnit.Position);
            DrawCircle(vh, selLocal, selectionRadius, selectionThickness, selectionColor);
        }

        if (waveTimer >= 0f)
        {
            Vector2 local = MapToLocal(waveMapPosition);
            float radius = waveTimer * waveSpeed;
            Color c = waveColor;
            c.a *= 1f - waveTimer / waveLifetime;
            DrawCircle(vh, local, radius, waveThickness, c);
        }
    }

    private void DrawUnit(VertexHelper vh, Vector2 center, MapUnit unit)
    {
        if (!unit.IsIdentified)
        {
            DrawUnknownContact(vh, center);
            return;
        }

        switch (unit.Type)
        {
            case UnitType.Tanker:
                DrawCircle(vh, center, contactSize, contactThickness, tankerColor);
                break;

            case UnitType.ShadowTanker:
                DrawDiamond(vh, center, contactSize, contactThickness, shadowTankerColor);
                break;

            case UnitType.Enemy:
                DrawTriangle(vh, center, contactSize, contactThickness, enemyColor);
                break;

            case UnitType.Torpedo:
                DrawChevron(vh, center, contactSize, contactThickness, torpedoColor, unit.HeadingAngleDeg);
                break;
        }
    }

    private void DrawUnknownContact(VertexHelper vh, Vector2 center)
    {
        float triSize = contactSize * 1.4f;
        DrawFilledTriangle(vh, center, triSize, unknownColor);

        Color markColor = new(0.15f, 0.15f, 0.15f, 1f);
        DrawExclamation(vh, center - Vector2.up * (triSize * 0.1f), triSize * 0.55f, contactThickness, markColor);
    }

    private void DrawDashedCircle(VertexHelper vh, Vector2 center, float radius,
        float thickness, Color color, int dashCount, float dashFill)
    {
        if (dashCount <= 0 || radius <= 0.5f) return;

        float period = Mathf.PI * 2f / dashCount;
        float dashLen = period * Mathf.Clamp(dashFill, 0.05f, 0.95f);
        const int stepsPerDash = 4;

        for (int d = 0; d < dashCount; d++)
        {
            float startAngle = d * period;
            float endAngle = startAngle + dashLen;

            Vector2 prev = center + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
            for (int s = 1; s <= stepsPerDash; s++)
            {
                float a = Mathf.Lerp(startAngle, endAngle, (float)s / stepsPerDash);
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                DrawLine(vh, prev, p, thickness, color);
                prev = p;
            }
        }
    }

    private void DrawLegacyContact(VertexHelper vh, Vector2 center, ContactType type)
    {
        switch (type)
        {
            case ContactType.Friendly:
                DrawCircle(vh, center, contactSize, contactThickness, friendlyColor);
                DrawLine(vh,
                    center + Vector2.left * contactSize,
                    center + Vector2.right * contactSize,
                    contactThickness, friendlyColor);
                break;

            case ContactType.Enemy:
                DrawTriangle(vh, center, contactSize, contactThickness, enemyColor);
                break;

            case ContactType.Unknown:
                DrawDiamond(vh, center, contactSize, contactThickness, unknownColor);
                break;

            default:
                DrawCircle(vh, center, contactSize, contactThickness, neutralColor);
                break;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        gridSize.x = Mathf.Clamp01(gridSize.x);
        gridSize.y = Mathf.Clamp01(gridSize.y);
        gridOffset.x = Mathf.Clamp01(gridOffset.x);
        gridOffset.y = Mathf.Clamp01(gridOffset.y);
        columns = Mathf.Max(2, columns);
        rows = Mathf.Max(2, rows);
        mapHalfExtent = Mathf.Max(0.01f, mapHalfExtent);
        SetVerticesDirty();
    }
#endif
}