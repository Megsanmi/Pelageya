using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RadioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIGridMap map;
    [SerializeField] private UIRadioMonitor radio;

    [Header("Microphone Click")]
    [SerializeField] private Collider microphoneCollider;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float maxRayDistance = 3f;
    [SerializeField] private bool blockClicksOverUI = true;

    [Header("Timing")]
    [SerializeField] private float minResponseDelay = 1.2f;
    [SerializeField] private float maxResponseDelay = 2.5f;
    [SerializeField] private float noResponseDelay = 3.0f;

    [Header("Messages")]
    [SerializeField] private string noTargetMessage = "ЦЕЛЬ НЕ ВЫБРАНА";
    [SerializeField] private string transmitFormat = "ЗАПРОС → {0}";
    [SerializeField] private string responseFormat = "{0}: {1}";
    [SerializeField] private string noResponseText = "НЕТ ОТВЕТА";

    [Header("Callsigns")]
    [SerializeField]
    private string[] callsignPrefixes =
        { "АЛЬФА", "БРАВО", "ЧАРЛИ", "ДЕЛЬТА", "ЭХО", "ФОКСТРОТ" };

    private readonly Dictionary<MapUnit, string> callsigns = new();
    private bool waiting;

    private void OnEnable()
    {
        if (map != null) map.OnUnitSelected += HandleUnitSelected;
    }

    private void OnDisable()
    {
        if (map != null) map.OnUnitSelected -= HandleUnitSelected;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;

        if (blockClicksOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (microphoneCollider == null) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var hits = Physics.RaycastAll(ray, maxRayDistance);

        bool micHit = false;
        foreach (var h in hits)
            if (h.collider == microphoneCollider) { micHit = true; break; }

        if (!micHit) return;

        HandleMicPressed();
    }

    private void HandleUnitSelected(MapUnit unit)
    {
        if (unit != null) EnsureCallsign(unit);
    }

    private void HandleMicPressed()
    {
        if (radio == null || waiting) return;

        MapUnit target = map != null ? map.SelectedUnit : null;

        if (target == null)
        {
            radio.Transmit(noTargetMessage);
            return;
        }

        string call = EnsureCallsign(target);
        radio.Transmit(string.Format(transmitFormat, call));
        StartCoroutine(RespondRoutine(target, call));
    }

    private IEnumerator RespondRoutine(MapUnit target, string call)
    {
        waiting = true;

        if (target != null && !target.RespondsToRadio)
        {
            yield return new WaitForSeconds(noResponseDelay);
            radio.Receive(string.Format(responseFormat, call, noResponseText));
            waiting = false;
            yield break;
        }

        float delay = Random.Range(minResponseDelay, maxResponseDelay);
        yield return new WaitForSeconds(delay);

        if (target == null)
        {
            radio.Receive(string.Format(responseFormat, call, "СВЯЗЬ ПОТЕРЯНА"));
        }
        else
        {
            target.Identify();
            radio.Receive(string.Format(responseFormat, call, BuildResponseFor(target)));
        }

        waiting = false;
    }

    private string EnsureCallsign(MapUnit unit)
    {
        if (unit == null) return "UNKNOWN";

        if (callsigns.TryGetValue(unit, out string existing))
            return existing;

        string prefix = callsignPrefixes != null && callsignPrefixes.Length > 0
            ? callsignPrefixes[Random.Range(0, callsignPrefixes.Length)]
            : "UNIT";

        string call = $"{prefix}-{Random.Range(1, 10)}";
        callsigns[unit] = call;
        return call;
    }

    private string BuildResponseFor(MapUnit unit)
    {
        switch (unit.Type)
        {
            case UnitType.Tanker:
                return Pick(
                    $"ТАНКЕР, КУРС {unit.HeadingAngleDeg:F0}°, {unit.SpeedKmh:F0} КМ/Ч",
                    $"ВСЁ ЧИСТО, СЛЕДУЮ НА {unit.HeadingAngleDeg:F0}°",
                    "ПОДТВЕРЖДАЮ, РЕЙС ШТАТНЫЙ");

            case UnitType.ShadowTanker:
                return Pick(
                    "ТАНКЕР... НИЧЕГО НЕ ЗНАЮ",
                    "ОБЫЧНЫЙ РЕЙС, БЕЗ ОСОБЕННОСТЕЙ",
                    "КУРС ДЕРЖУ, НЕ МЕШАЙТЕ",
                    "ПРИЁМ... СВЯЗЬ ПЛОХАЯ");

            case UnitType.Enemy:
                return Pick(
                    "ВАМ ЛУЧШЕ НЕ ЗНАТЬ",
                    "ПРОВАЛИВАЙТЕ",
                    "НЕ ВАШЕ ДЕЛО",
                    "ОТСТАНЬТЕ");

            default:
                return noResponseText;
        }
    }

    private static string Pick(params string[] variants) =>
        variants.Length == 0 ? string.Empty : variants[Random.Range(0, variants.Length)];
}