using UnityEngine;

public class CameraPoint : MonoBehaviour
{
    [SerializeField] private Transform camTarget;
    [SerializeField] private CameraPoint parentCamPoint;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Transform GetCamTarget()
    {
        return camTarget;
    }
    public CameraPoint GetParentCamPoint()
    {
        return parentCamPoint;
    }
}
