
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class MonitorInputManager : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;

    private MonitorInput[] monitors;

    public static event Action<MonitorClickInfo> OnMonitorClick;

    private void Start()
    {
        monitors = FindObjectsByType<MonitorInput>(
            FindObjectsSortMode.None
        );


        if (playerCamera == null)
        {
            Debug.LogError(
                "[MonitorManager] Player Camera is missing!"
            );
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;


        if (playerCamera == null)
            return;

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray = playerCamera.ScreenPointToRay(mousePosition);

        foreach (MonitorInput monitor in monitors)
        {
            if (monitor == null)
                continue;

            if (monitor.TryClick(ray, out MonitorClickInfo info))
            {
                Debug.Log(
                    $"[MonitorManager] Hit {info.MonitorId}, UV: {info.UV}"
                );

                OnMonitorClick?.Invoke(info);
                break;
            }
        }
    }
}
