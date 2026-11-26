using UnityEngine;
using UnityEngine.UI;

public static class UIMeshDrawUtils
{
    public static void DrawRect(VertexHelper vh, Rect rect, Color color)
    {
        int i = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = new Vector2(rect.xMin, rect.yMin);
        vh.AddVert(v);

        v.position = new Vector2(rect.xMin, rect.yMax);
        vh.AddVert(v);

        v.position = new Vector2(rect.xMax, rect.yMax);
        vh.AddVert(v);

        v.position = new Vector2(rect.xMax, rect.yMin);
        vh.AddVert(v);

        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }

    public static void DrawRectOutline(
        VertexHelper vh,
        Rect rect,
        float thickness,
        Color color)
    {
        DrawLine(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), thickness, color);
        DrawLine(vh, new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMax, rect.yMax), thickness, color);
        DrawLine(vh, new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax), thickness, color);
        DrawLine(vh, new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), thickness, color);
    }

    public static void DrawLine(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        float thickness,
        Color color)
    {
        Vector2 direction = b - a;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;

        int i = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = a - normal;
        vh.AddVert(v);

        v.position = a + normal;
        vh.AddVert(v);

        v.position = b + normal;
        vh.AddVert(v);

        v.position = b - normal;
        vh.AddVert(v);

        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }

    public static void DrawCircle(
        VertexHelper vh,
        Vector2 center,
        float radius,
        float thickness,
        Color color,
        int segments = 48)
    {
        float step = Mathf.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * step;
            float a2 = (i + 1) * step;

            Vector2 a = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            Vector2 b = center + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * radius;

            DrawLine(vh, a, b, thickness, color);
        }
    }

    public static void DrawTriangle(
        VertexHelper vh,
        Vector2 center,
        float size,
        float thickness,
        Color color)
    {
        Vector2 top = center + Vector2.up * size;
        Vector2 left = center + new Vector2(-size, -size);
        Vector2 right = center + new Vector2(size, -size);

        DrawLine(vh, top, left, thickness, color);
        DrawLine(vh, left, right, thickness, color);
        DrawLine(vh, right, top, thickness, color);
    }

    public static void DrawDiamond(
        VertexHelper vh,
        Vector2 center,
        float size,
        float thickness,
        Color color)
    {
        Vector2 top = center + Vector2.up * size;
        Vector2 bottom = center + Vector2.down * size;
        Vector2 left = center + Vector2.left * size;
        Vector2 right = center + Vector2.right * size;

        DrawLine(vh, top, right, thickness, color);
        DrawLine(vh, right, bottom, thickness, color);
        DrawLine(vh, bottom, left, thickness, color);
        DrawLine(vh, left, top, thickness, color);
    }

    public static void DrawFilledTriangle(VertexHelper vh, Vector2 center, float size, Color color)
    {
        Vector2 top = center + Vector2.up * size;
        Vector2 left = center + new Vector2(-size * 0.866f, -size * 0.5f);
        Vector2 right = center + new Vector2(size * 0.866f, -size * 0.5f);

        int i = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = top; vh.AddVert(v);
        v.position = left; vh.AddVert(v);
        v.position = right; vh.AddVert(v);

        vh.AddTriangle(i, i + 1, i + 2);
    }

    public static void DrawFilledCircle(VertexHelper vh, Vector2 center, float radius, Color color, int segments = 24)
    {
        int c = vh.currentVertCount;

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = center;
        vh.AddVert(v);

        float step = Mathf.PI * 2f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float a = i * step;
            v.position = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            vh.AddVert(v);
        }

        for (int i = 0; i < segments; i++)
            vh.AddTriangle(c, c + 1 + i, c + 2 + i);
    }

    public static void DrawExclamation(VertexHelper vh, Vector2 center, float size, float thickness, Color color)
    {
        float half = size * 0.5f;

        DrawLine(vh,
            center + Vector2.up * (half * 0.75f),
            center + Vector2.up * (half * 0.05f),
            thickness, color);

        DrawFilledCircle(vh,
            center + Vector2.up * (half * -0.35f),
            thickness * 0.75f, color, 10);
    }

    public static void DrawChevron(VertexHelper vh, Vector2 center, float size, float thickness, Color color, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 forward = new(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 right = new(-forward.y, forward.x);

        Vector2 tip = center + forward * size;
        Vector2 left = center - forward * (size * 0.5f) - right * (size * 0.85f);
        Vector2 rgt = center - forward * (size * 0.5f) + right * (size * 0.85f);

        DrawLine(vh, left, tip, thickness, color);
        DrawLine(vh, tip, rgt, thickness, color);
    }
}
