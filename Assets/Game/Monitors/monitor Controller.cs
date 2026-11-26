
using UnityEngine;

public class MonitorController : MonoBehaviour
{
    [Header("Display")]
    public Renderer screen;
    public RenderTexture displayTexture;

    [Header("Settings")]
    public bool isPowered = true;

    private MaterialPropertyBlock block;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        screen.GetPropertyBlock(block);

        block.SetTexture(
            "_BaseMap",
            isPowered ? displayTexture : null
        );

        screen.SetPropertyBlock(block);
    }

    public void SetPower(bool state)
    {
        isPowered = state;
        UpdateDisplay();
    }
}
