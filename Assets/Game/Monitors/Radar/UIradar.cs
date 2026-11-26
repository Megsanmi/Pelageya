using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static UIMeshDrawUtils;

public class UIRadarMonitor : MaskableGraphic
{
    [Header("Monitor")]
    [SerializeField] private string monitorId = "RADAR";

    [Header("Screen Layout")]
    [SerializeField] private Vector2 screenOffset = new(0.05f, 0.15f);
    [SerializeField] private Vector2 screenSize = new(0.65f, 0.75f);
    [Range(0.1f, 0.5f)][SerializeField] private float scopeRadiusFactor = 0.42f;

    [Header("Radar")]
    [SerializeField] private Vector2 radarCenterMap = Vector2.zero;
    [SerializeField] private float maxRangeKm = 30f;
    [Tooltip("= simulationSpeed у UnitManager, если хочешь синхронное время")]
    [SerializeField] private float timeScale = 1f;
    [SerializeField] private UnitManager unitManager;

    [Header("Colors")]
    public Color backgroundColor = Color.black;
    public Color scopeColor = new(0.2f, 1f, 0.4f, 1f);
    public Color ringColor = new(0.1f, 0.8f, 0.3f, 0.4f);
    public Color bearingColor = new(0.1f, 0.8f, 0.3f, 0.25f);
    public Color sweepColor = new(0.2f, 1f, 0.4f, 0.9f);
    public Color blipColor = new(1f, 0.9f, 0.2f, 1f);

    [Header("Grid")]
    [Range(2, 8)] public int ringCount = 3;
    [Range(4, 24)] public int bearingLines = 12;
    public float scopeBorderThickness = 2f;
    public float ringThickness = 1f;
    public float bearingThickness = 1f;

    [Header("Sweep")]
    public float sweepSpeedDegPerSec = 90f;
    [Range(0f, 180f)] public float sweepTrailAngle = 55f;
    public float sweepLineThickness = 2f;
    [Range(4, 64)] public int sweepTrailSegments = 20;

    [Header("Blips")]
    public float blipSize = 5f;
    public float blipFadeDuration = 4f;

    private class Detection
    {
        public Vector2 mapPosition;
        public float lastSeenTime;
    }

    private readonly Dictionary<MapUnit, Detection> detections = new();
    private float sweepAngleDeg;
    private float timeSec;

    public string MonitorId => monitorId;
    public Vector2 RadarCenterMap => radarCenterMap;
    public float MaxRangeKm => maxRangeKm;

    private void Update()
    {
        float dt = Time.deltaTime * timeScale;
        timeSec += dt;

        float prevSweep = sweepAngleDeg;
        sweepAngleDeg = Mathf.Repeat(sweepAngleDeg + sweepSpeedDegPerSec * dt, 360f);

        UpdateDetections(prevSweep, sweepAngleDeg);
        SetVerticesDirty();
    }

    private void UpdateDetections(float prevAngle, float currAngle)
    {
        if (unitManager == null) return;

        foreach (var unit in unitManager.Units)
        {
            if (unit == null) continue;

            Vector2 rel = unit.Position - radarCenterMap;
            if (rel.magnitude > maxRangeKm) continue;

            float bearingDeg = Mathf.Repeat(Mathf.Atan2(rel.x, rel.y) * Mathf.Rad2Deg, 360f);
            if (!CrossedAngle(prevAngle, currAngle, bearingDeg)) continue;

            if (!detections.TryGetValue(unit, out var d))
            {
                d = new Detection();
                detections[unit] = d;
            }
            d.mapPosition = unit.Position;
            d.lastSeenTime = timeSec;

            unit.MarkDetected();
        }
    }

    private static bool CrossedAngle(float prev, float curr, float target)
    {
        if (curr >= prev) return target >= prev && target <= curr;
        return target >= prev || target <= curr;
    }

    private Rect GetScreenRect()
    {
        Rect monitor = GetPixelAdjustedRect();
        return new Rect(
            monitor.xMin + monitor.width * screenOffset.x,
            monitor.yMin + monitor.height * screenOffset.y,
            monitor.width * screenSize.x,
            monitor.height * screenSize.y);
    }

    private Vector2 GetScopeCenter() => GetScreenRect().center;

    private float GetScopeRadius()
    {
        Rect r = GetScreenRect();
        return Mathf.Min(r.width, r.height) * scopeRadiusFactor;
    }

    private Vector2 MapToLocal(Vector2 mapPos)
    {
        Vector2 rel = mapPos - radarCenterMap;
        if (rel.sqrMagnitude < 0.0001f) return GetScopeCenter();
        float n = Mathf.Min(rel.magnitude, maxRangeKm) / maxRangeKm;
        float rad = Mathf.Atan2(rel.x, rel.y);
        return GetScopeCenter() + new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * (n * GetScopeRadius());
    }

    private float GetBlipAlpha(Detection d)
    {
        if (blipFadeDuration <= 0.01f) return 1f;
        float age = timeSec - d.lastSeenTime;
        return age >= blipFadeDuration ? 0f : 1f - age / blipFadeDuration;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect screen = GetScreenRect();
        if (screen.width <= 0f || screen.height <= 0f) return;

        DrawRect(vh, screen, backgroundColor);

        Vector2 center = GetScopeCenter();
        float radius = GetScopeRadius();

        for (int i = 1; i <= ringCount; i++)
            DrawCircle(vh, center, radius * i / ringCount, ringThickness, ringColor);

        for (int i = 0; i < bearingLines; i++)
        {
            float rad = (360f * i / bearingLines) * Mathf.Deg2Rad;
            Vector2 dir = new(Mathf.Sin(rad), Mathf.Cos(rad));
            DrawLine(vh, center, center + dir * radius, bearingThickness, bearingColor);
        }

        if (sweepTrailAngle > 0.5f)
        {
            int segs = Mathf.Max(4, sweepTrailSegments);
            for (int i = 0; i < segs; i++)
            {
                float t0 = (float)i / segs;
                float t1 = (float)(i + 1) / segs;
                float b0 = Mathf.Repeat(sweepAngleDeg - sweepTrailAngle * (1f - t0), 360f);
                float b1 = Mathf.Repeat(sweepAngleDeg - sweepTrailAngle * (1f - t1), 360f);

                Vector2 d0 = new(Mathf.Sin(b0 * Mathf.Deg2Rad), Mathf.Cos(b0 * Mathf.Deg2Rad));
                Vector2 d1 = new(Mathf.Sin(b1 * Mathf.Deg2Rad), Mathf.Cos(b1 * Mathf.Deg2Rad));

                Color c = sweepColor;
                c.a *= t0;

                int idx = vh.currentVertCount;
                UIVertex v = UIVertex.simpleVert;
                v.color = c; v.position = center; vh.AddVert(v);
                v.color = c; v.position = center + d0 * radius; vh.AddVert(v);
                v.color = c; v.position = center + d1 * radius; vh.AddVert(v);
                vh.AddTriangle(idx, idx + 1, idx + 2);
            }
        }

        {
            float rad = sweepAngleDeg * Mathf.Deg2Rad;
            Vector2 dir = new(Mathf.Sin(rad), Mathf.Cos(rad));
            DrawLine(vh, center, center + dir * radius, sweepLineThickness, sweepColor);
        }

        foreach (var kv in detections)
        {
            if (kv.Key == null) continue;
            float alpha = GetBlipAlpha(kv.Value);
            if (alpha <= 0.01f) continue;

            Vector2 pos = MapToLocal(kv.Value.mapPosition);
            Color c = blipColor;
            c.a *= alpha;
            DrawFilledCircle(vh, pos, blipSize * Mathf.Lerp(0.5f, 1f, alpha), c, 12);
        }

        DrawCircle(vh, center, radius, scopeBorderThickness, scopeColor);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        screenOffset.x = Mathf.Clamp01(screenOffset.x);
        screenOffset.y = Mathf.Clamp01(screenOffset.y);
        screenSize.x = Mathf.Clamp01(screenSize.x);
        screenSize.y = Mathf.Clamp01(screenSize.y);
        maxRangeKm = Mathf.Max(0.1f, maxRangeKm);
        ringCount = Mathf.Max(1, ringCount);
        bearingLines = Mathf.Max(3, bearingLines);
        SetVerticesDirty();
    }
#endif
}