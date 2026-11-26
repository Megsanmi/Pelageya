
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class Cameracontroller : MonoBehaviour
{
    [Header("Начальная точка")]
    [SerializeField] private CameraPoint startCameraPoint;

    [Header("Настройки перехода")]
    [SerializeField] private float transitionDuration = 0.7f;

    private Coroutine transitionCoroutine;
    private CameraPoint currentCameraPoint;

    void Start()
    {
        ChangeTarget(startCameraPoint);
        currentCameraPoint = startCameraPoint;
        
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                
                CameraPoint point =
                    hit.collider.GetComponent<CameraPoint>();

                if (point != null)
                {
                    ChangeTarget(point);
                }
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (currentCameraPoint != null)
            {
                CameraPoint parent =
                    currentCameraPoint.GetParentCamPoint();

                if (parent != null)
                    ChangeTarget(parent);
            }
        }
    }

    public void ChangeTarget(CameraPoint newPoint)
    {
        if (newPoint == null) return;

        if (newPoint == currentCameraPoint) return;

        List<CameraPoint> path = GetPath(
            currentCameraPoint,
            newPoint
        );

        if (path.Count == 0) return;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        currentCameraPoint = newPoint;

        transitionCoroutine = StartCoroutine(
            MoveAlongPath(path)
        );
    }

    List<CameraPoint> GetPath(
        CameraPoint from,
        CameraPoint to)
    {
        List<CameraPoint> path = new List<CameraPoint>();

        if (from == null)
        {
            path.Add(to);
            return path;
        }

        CameraPoint fromParent = from.GetParentCamPoint();
        CameraPoint toParent = to.GetParentCamPoint();

        if (fromParent == toParent)
        {
            path.Add(to);
            return path;
        }

        HashSet<CameraPoint> ancestors =
            new HashSet<CameraPoint>();

        CameraPoint current = from;

        while (current != null)
        {
            ancestors.Add(current);
            current = current.GetParentCamPoint();
        }

        CameraPoint commonParent = null;
        current = to;

        while (current != null)
        {
            if (ancestors.Contains(current))
            {
                commonParent = current;
                break;
            }

            current = current.GetParentCamPoint();
        }

        if (commonParent == null)
        {
            path.Add(to);
            return path;
        }
        current = from;

        while (current != commonParent)
        {
            current = current.GetParentCamPoint();
            path.Add(current);
        }

        List<CameraPoint> downwardPath =
            new List<CameraPoint>();

        current = to;

        while (current != commonParent)
        {
            downwardPath.Add(current);
            current = current.GetParentCamPoint();
        }

        downwardPath.Reverse();
        path.AddRange(downwardPath);

        return path;
    }

    IEnumerator MoveAlongPath(List<CameraPoint> path)
    {
        foreach (CameraPoint point in path)
        {
            if (point == null) continue;

            Transform camTarget = point.GetCamTarget();

            if (camTarget == null) continue;

            yield return MoveToTarget(camTarget);
        }

        transitionCoroutine = null;
    }
    IEnumerator MoveToTarget(Transform newTarget)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 endPosition = newTarget.position;
        Quaternion endRotation = newTarget.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float t = elapsedTime / transitionDuration;

            transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                endRotation,
                t
            );

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;
    }
}
