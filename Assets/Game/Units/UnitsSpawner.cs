// UnitsSpawner.cs
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Tanker,
    ShadowTanker,
    Enemy,
    Torpedo
}

public class UnitManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int maxUnits = 10;
    [SerializeField] private float minSpawnSeparationKm = 5f;

    [Header("Map Bounds (1 unit = 1 km)")]
    [SerializeField] private float mapHalfExtentKm = 50f;

    [Header("Speed Ranges (km/h)")]
    [SerializeField] private Vector2 tankerSpeed = new(40f, 80f);
    [SerializeField] private Vector2 shadowSpeed = new(60f, 120f);
    [SerializeField] private Vector2 enemySpeed = new(150f, 300f);
    [SerializeField] private Vector2 torpedoSpeed = new(400f, 700f);

    [Header("Spawn Weights")]
    [SerializeField] private float tankerWeight = 0.50f;
    [SerializeField] private float shadowWeight = 0.25f;
    [SerializeField] private float enemyWeight = 0.20f;
    [SerializeField] private float torpedoWeight = 0.05f;

    [Header("Turning (deg/sec)")]
    [SerializeField] private float minTurnRate = 20f;
    [SerializeField] private float maxTurnRate = 90f;

    [Header("Simulation")]
    [SerializeField] private float simulationSpeed = 5f;

    [Header("Wander")]
    [SerializeField] private float destinationRangeMinKm = 10f;
    [SerializeField] private float destinationRangeMaxKm = 40f;

    private readonly List<MapUnit> units = new();
    public IReadOnlyList<MapUnit> Units => units;
    public float MapHalfExtentKm => mapHalfExtentKm;

    private float timer;

    private void Update()
    {
        float dt = Time.deltaTime * simulationSpeed;
        MoveUnits(dt);

        if (!autoSpawn) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnUnit();
        }
    }

    private void MoveUnits(float dt)
    {
        for (int i = units.Count - 1; i >= 0; i--)
        {
            MapUnit unit = units[i];
            if (unit == null) { units.RemoveAt(i); continue; }

            float deltaKm = unit.SpeedKmh / 3600f * dt;
            if (unit.Move(deltaKm, dt))
                unit.SetDestination(RandomPointNear(unit.Position));
        }
    }

    public MapUnit SpawnUnit() => SpawnUnit(PickRandomType());

    public MapUnit SpawnUnit(UnitType type)
    {
        if (units.Count >= maxUnits) return null;

        Vector2 spawn = Vector2.zero;
        bool found = false;
        
        for (int attempt = 0; attempt < 32; attempt++)
        {
            spawn = RandomMapPoint();
            if (IsFree(spawn)) { found = true; break; }
        }

        if (!found) return null;
        
        
        Debug.Log("Unit Spawned");
        Vector2 destination = RandomPointNear(spawn);
        Vector2 speedRange = SpeedRangeFor(type);
        float speed = Random.Range(speedRange.x, speedRange.y);
        float turnRate = Random.Range(minTurnRate, maxTurnRate);

        MapUnit unit = new MapUnit(type, spawn, destination, speed, turnRate);
        units.Add(unit);
        return unit;
    }

    private Vector2 SpeedRangeFor(UnitType type) => type switch
    {
        UnitType.Tanker => tankerSpeed,
        UnitType.ShadowTanker => shadowSpeed,
        UnitType.Enemy => enemySpeed,
        UnitType.Torpedo => torpedoSpeed,
        _ => tankerSpeed
    };

    private UnitType PickRandomType()
    {
        float total = tankerWeight + shadowWeight + enemyWeight + torpedoWeight;
        if (total <= 0f) return UnitType.Tanker;

        float roll = Random.value * total;

        if ((roll -= tankerWeight) < 0f) return UnitType.Tanker;
        if ((roll -= shadowWeight) < 0f) return UnitType.ShadowTanker;
        if ((roll -= enemyWeight) < 0f) return UnitType.Enemy;
        return UnitType.Torpedo;
    }

    public void RemoveUnit(MapUnit unit)
    {
        if (unit == null) return;
        units.Remove(unit);
    }

    public void ClearUnits() => units.Clear();
    public int UnitCount => units.Count;

    private Vector2 RandomMapPoint() => new(
        Random.Range(-mapHalfExtentKm, mapHalfExtentKm),
        Random.Range(-mapHalfExtentKm, mapHalfExtentKm));

    private Vector2 RandomPointNear(Vector2 center)
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float range = Random.Range(destinationRangeMinKm, destinationRangeMaxKm);
            Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * range;

            if (Mathf.Abs(candidate.x) <= mapHalfExtentKm &&
                Mathf.Abs(candidate.y) <= mapHalfExtentKm)
                return candidate;
        }
        return RandomMapPoint();
    }

    private bool IsFree(Vector2 point)
    {
        float sqrSep = minSpawnSeparationKm * minSpawnSeparationKm;
        foreach (var u in units)
        {
            if (u == null) continue;
            if ((u.Position - point).sqrMagnitude < sqrSep) return false;
        }
        return true;
    }
}

[System.Serializable]
public class MapUnit
{
    public UnitType Type { get; private set; }
    public Vector2 Position { get; private set; }
    public Vector2 Destination { get; private set; }
    public float SpeedKmh { get; private set; }
    public Vector2 Heading { get; private set; } = Vector2.right;
    public float HeadingAngleDeg { get; private set; }
    public float TurnRateDegPerSec { get; private set; }

    public bool IsIdentified { get; private set; }
    public bool IsDetected { get; private set; }
    public void MarkDetected() => IsDetected = true;
    public bool RespondsToRadio => Type != UnitType.Torpedo;

    public MapUnit(UnitType type, Vector2 position, Vector2 destination, float speedKmh, float turnRateDeg)
    {
        Type = type;
        Position = position;
        Destination = destination;
        SpeedKmh = speedKmh;
        TurnRateDegPerSec = turnRateDeg;

        Vector2 dir = destination - position;
        if (dir.sqrMagnitude > 0.0001f)
        {
            HeadingAngleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Heading = dir.normalized;
        }
    }

    public void SetPosition(Vector2 position) => Position = position;
    public void SetDestination(Vector2 destination) => Destination = destination;
    public void SetSpeed(float speedKmh) => SpeedKmh = speedKmh;
    public void Identify() => IsIdentified = true;

    public bool Move(float deltaKilometers, float dt)
    {
        Vector2 toTarget = Destination - Position;
        float dist = toTarget.magnitude;
        float arrivalThreshold = Mathf.Max(0.5f, deltaKilometers);

        if (dist <= arrivalThreshold)
        {
            Position = Destination;
            UpdateHeading(toTarget, dt);
            return true;
        }

        UpdateHeading(toTarget, dt);
        Position += Heading * deltaKilometers;
        return false;
    }

    private void UpdateHeading(Vector2 toTarget, float dt)
    {
        if (toTarget.sqrMagnitude < 0.0001f) return;

        float desiredAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float maxTurn = TurnRateDegPerSec * dt;
        HeadingAngleDeg = Mathf.MoveTowardsAngle(HeadingAngleDeg, desiredAngle, maxTurn);

        float rad = HeadingAngleDeg * Mathf.Deg2Rad;
        Heading = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}