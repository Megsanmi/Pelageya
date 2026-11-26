using TMPro;
using UnityEngine;
using UnityEngine.UI;

using static UIMeshDrawUtils;

public class UIRadioMonitor : MaskableGraphic
{
    [Header("Monitor")]
    [SerializeField] private string monitorId = "RADIO";

    [Header("Screen Layout")]
    [SerializeField] private Vector2 screenOffset = new(0.05f, 0.15f);
    [SerializeField] private Vector2 screenSize = new(0.65f, 0.75f);

    [Header("Screen Appearance")]
    public Color backgroundColor = Color.black;
    public Color borderColor = new(0.2f, 1f, 0.4f, 1f);
    public float borderThickness = 3f;
    public Color scanlineColor = new(0.1f, 0.8f, 0.3f, 0.15f);
    public float scanlineSpacing = 8f;
    public float scanlineThickness = 1f;

    [Header("Wave (Idle)")]
    public Color waveColor = new(0.3f, 1f, 0.4f, 1f);
    public float waveThickness = 2f;
    [Range(8, 256)] public int waveSegments = 96;
    public float idleAmplitude = 1.5f;
    public float idleFrequency = 0.15f;
    public float idleSpeed = 4f;

    [Header("Wave (Active)")]
    public float activeAmplitude = 25f;
    public float activeFrequency = 0.25f;
    public float activeSpeed = 30f;
    public float activationDuration = 1.5f;
    public float envelopePower = 1.5f;

    [Header("Message")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private bool autoPositionText = true;
    [Range(0f, 1f)][SerializeField] private float messageAnchorY = 0.12f;
    [SerializeField] private float messageDuration = 4f;
    [SerializeField] private float messageFadeDuration = 0.5f;

    private float waveTime;
    private float activeTimer = -1f;
    private float messageTimer = -1f;

    public string MonitorId => monitorId;
    public bool IsActive => activeTimer >= 0f;

    protected override void OnEnable()
    {
        base.OnEnable();
        SetVerticesDirty();
    }

    private void Update()
    {
        waveTime += Time.deltaTime;

        if (activeTimer >= 0f)
        {
            activeTimer += Time.deltaTime;
            if (activeTimer >= activationDuration)
                activeTimer = -1f;
        }

        if (messageTimer >= 0f)
        {
            messageTimer += Time.deltaTime;
            UpdateMessageAlpha();

            if (messageTimer >= messageDuration)
            {
                messageTimer = -1f;

                if (messageText != null)
                {
                    messageText.text = string.Empty;
                    Color c = messageText.color;
                    c.a = 1f;
                    messageText.color = c;
                }
            }
        }

        SetVerticesDirty();
    }

    private void UpdateMessageAlpha()
    {
        if (messageText == null) return;

        float remain = messageDuration - messageTimer;
        float alpha = remain < messageFadeDuration
            ? Mathf.Clamp01(remain / Mathf.Max(0.001f, messageFadeDuration))
            : 1f;

        Color c = messageText.color;
        c.a = alpha;
        messageText.color = c;
    }

    public void Transmit(string message) => Activate(message);
    public void Receive(string message) => Activate(message);

    public void Activate(string message = null)
    {
        activeTimer = 0f;

        if (!string.IsNullOrEmpty(message))
            ShowMessage(message);

        SetVerticesDirty();
    }

    public void Stop()
    {
        activeTimer = -1f;
        SetVerticesDirty();
    }

    private void ShowMessage(string message)
    {
        if (messageText == null) return;

        messageText.text = message;
        messageTimer = 0f;

        Color c = messageText.color;
        c.a = 1f;
        messageText.color = c;

        if (autoPositionText && messageText.transform.parent == transform)
            PositionMessage();
    }

    private void PositionMessage()
    {
        Rect screen = GetScreenRect();
        RectTransform rt = messageText.rectTransform;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(
            screen.center.x,
            screen.yMin + screen.height * messageAnchorY
        );
    }

    public Rect GetScreenRect()
    {
        Rect monitor = GetPixelAdjustedRect();

        return new Rect(
            monitor.xMin + monitor.width * screenOffset.x,
            monitor.yMin + monitor.height * screenOffset.y,
            monitor.width * screenSize.x,
            monitor.height * screenSize.y
        );
    }

    private float GetActivity()
    {
        if (activeTimer < 0f) return 0f;

        float t = Mathf.Clamp01(activeTimer / activationDuration);
        return Mathf.Pow(1f - t, envelopePower);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect screen = GetScreenRect();
        if (screen.width <= 0f || screen.height <= 0f) return;

        DrawRect(vh, screen, backgroundColor);
        DrawScanlines(vh, screen);
        DrawWave(vh, screen);
        DrawRectOutline(vh, screen, borderThickness, borderColor);
    }

    private void DrawScanlines(VertexHelper vh, Rect screen)
    {
        if (scanlineSpacing <= 0.1f || scanlineColor.a <= 0.001f) return;

        int count = Mathf.FloorToInt(screen.height / scanlineSpacing);

        for (int i = 0; i <= count; i++)
        {
            float y = screen.yMin + i * scanlineSpacing;

            DrawLine(vh,
                new Vector2(screen.xMin, y),
                new Vector2(screen.xMax, y),
                scanlineThickness,
                scanlineColor);
        }
    }

    private void DrawWave(VertexHelper vh, Rect screen)
    {
        float activity = GetActivity();

        float amplitude = Mathf.Lerp(idleAmplitude, activeAmplitude, activity);
        float frequency = Mathf.Lerp(idleFrequency, activeFrequency, activity);
        float speed = Mathf.Lerp(idleSpeed, activeSpeed, activity);

        float midY = screen.center.y;
        float limit = Mathf.Max(1f, screen.height * 0.5f - waveThickness - 2f);

        int segs = Mathf.Clamp(waveSegments, 8, 256);
        float stepX = screen.width / segs;

        Vector2 prev = Vector2.zero;

        for (int i = 0; i <= segs; i++)
        {
            float x = screen.xMin + i * stepX;
            float phase = x * frequency + waveTime * speed;

            float yVal = Mathf.Sin(phase) * amplitude;

            if (activity > 0.01f)
                yVal += Mathf.Sin(phase * 2.3f + waveTime * 7f) * amplitude * 0.25f * activity;

            yVal = Mathf.Clamp(yVal, -limit, limit);

            Vector2 p = new Vector2(x, midY + yVal);

            if (i > 0)
                DrawLine(vh, prev, p, waveThickness, waveColor);

            prev = p;
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        screenSize.x = Mathf.Clamp01(screenSize.x);
        screenSize.y = Mathf.Clamp01(screenSize.y);
        screenOffset.x = Mathf.Clamp01(screenOffset.x);
        screenOffset.y = Mathf.Clamp01(screenOffset.y);

        waveSegments = Mathf.Clamp(waveSegments, 8, 256);
        waveThickness = Mathf.Max(0.1f, waveThickness);
        activationDuration = Mathf.Max(0.1f, activationDuration);
        messageDuration = Mathf.Max(0.1f, messageDuration);
        messageFadeDuration = Mathf.Clamp(messageFadeDuration, 0.05f, messageDuration);

        SetVerticesDirty();
    }
#endif
}