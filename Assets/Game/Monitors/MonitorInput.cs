
using UnityEngine;

public class MonitorInput : MonoBehaviour
{
    [Header("Monitor")]
    [SerializeField] private string monitorId = "NAVIGATION";
    [SerializeField] private Collider screenCollider;

    public string MonitorId => monitorId;

    private void Awake()
    {
        if (screenCollider == null)
            screenCollider = GetComponent<Collider>();

        if (screenCollider == null)
        {
            Debug.LogError(
                $"[{name}] Screen Collider is missing!",
                this
            );
        }
    }

    public bool TryClick(Ray ray, out MonitorClickInfo info)
    {
        info = default;

        if (screenCollider == null)
            return false;

        if (!screenCollider.Raycast(
            ray,
            out RaycastHit hit,
            100f))
        {
            return false;
        }

        info = new MonitorClickInfo(
            monitorId,
            hit.textureCoord,
            hit.point,
            hit.normal,
            hit.distance
        );

        return true;
    }
}

public struct MonitorClickInfo
{
    public string MonitorId;
    public Vector2 UV;
    public Vector3 WorldPosition;
    public Vector3 Normal;
    public float Distance;

    public MonitorClickInfo(
        string monitorId,
        Vector2 uv,
        Vector3 worldPosition,
        Vector3 normal,
        float distance)
    {
        MonitorId = monitorId;
        UV = uv;
        WorldPosition = worldPosition;
        Normal = normal;
        Distance = distance;
    }
}
