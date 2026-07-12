using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the ordered indoor navigation route and advances through waypoints.
/// </summary>
public class WaypointManager : MonoBehaviour
{
    [Serializable]
    public class WaypointData
    {
        [SerializeField] private string waypointName = "Waypoint";
        [SerializeField] [TextArea(2, 4)] private string description = "Describe this stop.";
        [SerializeField] private Transform waypointTransform;
        [SerializeField] private float reachDistance = 1.1f;
        [SerializeField] private List<Transform> approachPoints = new List<Transform>();

        public string WaypointName => waypointName;
        public string Description => description;
        public Transform WaypointTransform => waypointTransform;
        public float ReachDistance => reachDistance;
        public IReadOnlyList<Transform> ApproachPoints => approachPoints;
    }

    [Header("Route Root")]
    [SerializeField] private Transform routeRoot;
    [SerializeField] private Vector2 mapMin = new Vector2(-1f, -1f);
    [SerializeField] private Vector2 mapMax = new Vector2(7f, 12f);

    [Header("Ordered Waypoints")]
    [SerializeField] private List<WaypointData> waypoints = new List<WaypointData>();

    private int currentWaypointIndex;
    private bool routeComplete;

    public event Action<int> WaypointChanged;
    public event Action RouteCompleted;

    public Transform RouteRoot => routeRoot;
    public Vector2 MapMin => mapMin;
    public Vector2 MapMax => mapMax;
    public int CurrentWaypointIndex => currentWaypointIndex;
    public int TotalWaypoints => waypoints.Count;
    public bool IsRouteComplete => routeComplete || waypoints.Count == 0;
    public WaypointData CurrentWaypoint => routeComplete || currentWaypointIndex < 0 || currentWaypointIndex >= waypoints.Count
        ? null
        : waypoints[currentWaypointIndex];

    private void Awake()
    {
        RemoveNullWaypoints();
    }

    public void SetRouteRoot(Transform newRouteRoot)
    {
        routeRoot = newRouteRoot;
    }

    public void ResetRoute()
    {
        RemoveNullWaypoints();
        routeComplete = waypoints.Count == 0;
        currentWaypointIndex = 0;
        WaypointChanged?.Invoke(currentWaypointIndex);
    }

    public bool TryAdvanceIfReached(Vector3 playerWorldPosition)
    {
        WaypointData currentWaypoint = CurrentWaypoint;
        if (currentWaypoint == null || currentWaypoint.WaypointTransform == null)
        {
            return false;
        }

        if (GetHorizontalDistance(playerWorldPosition, currentWaypoint.WaypointTransform.position) > currentWaypoint.ReachDistance)
        {
            return false;
        }

        AdvanceToNextWaypoint();
        return true;
    }

    public void AdvanceToNextWaypoint()
    {
        if (waypoints.Count == 0 || routeComplete)
        {
            return;
        }

        if (currentWaypointIndex >= waypoints.Count - 1)
        {
            routeComplete = true;
            RouteCompleted?.Invoke();
            return;
        }

        currentWaypointIndex++;
        WaypointChanged?.Invoke(currentWaypointIndex);
    }

    public void GoBackToPreviousWaypoint()
    {
        if (waypoints.Count == 0)
        {
            return;
        }

        if (routeComplete)
        {
            routeComplete = false;
            currentWaypointIndex = Mathf.Max(waypoints.Count - 1, 0);
            WaypointChanged?.Invoke(currentWaypointIndex);
            return;
        }

        if (currentWaypointIndex <= 0)
        {
            return;
        }

        currentWaypointIndex--;
        WaypointChanged?.Invoke(currentWaypointIndex);
    }

    public float GetDistanceToCurrentWaypoint(Vector3 playerWorldPosition)
    {
        WaypointData currentWaypoint = CurrentWaypoint;
        if (currentWaypoint == null || currentWaypoint.WaypointTransform == null)
        {
            return 0f;
        }

        return GetHorizontalDistance(playerWorldPosition, currentWaypoint.WaypointTransform.position);
    }

    public Vector3 GetCurrentWaypointWorldPosition()
    {
        WaypointData currentWaypoint = CurrentWaypoint;
        if (currentWaypoint == null || currentWaypoint.WaypointTransform == null)
        {
            return Vector3.zero;
        }

        return currentWaypoint.WaypointTransform.position;
    }

    public string GetCurrentWaypointName()
    {
        WaypointData currentWaypoint = CurrentWaypoint;
        return currentWaypoint == null ? "Route Complete" : currentWaypoint.WaypointName;
    }

    public string GetCurrentWaypointDescription()
    {
        WaypointData currentWaypoint = CurrentWaypoint;
        return currentWaypoint == null ? "You have finished the indoor AR route." : currentWaypoint.Description;
    }

    public float GetCurrentReachDistance()
    {
        WaypointData currentWaypoint = CurrentWaypoint;
        return currentWaypoint == null ? 0f : currentWaypoint.ReachDistance;
    }

    public List<Vector3> BuildCurrentRoute(Vector3 playerWorldPosition)
    {
        List<Vector3> routePoints = new List<Vector3>();
        WaypointData currentWaypoint = CurrentWaypoint;
        if (currentWaypoint == null || currentWaypoint.WaypointTransform == null)
        {
            return routePoints;
        }

        float floorHeight = routeRoot != null ? routeRoot.position.y : currentWaypoint.WaypointTransform.position.y;

        Vector3 playerPoint = FlattenToFloor(playerWorldPosition, floorHeight);
        routePoints.Add(playerPoint);

        IReadOnlyList<Transform> approachPoints = currentWaypoint.ApproachPoints;
        for (int i = 0; i < approachPoints.Count; i++)
        {
            Transform guidePoint = approachPoints[i];
            if (guidePoint == null)
            {
                continue;
            }

            Vector3 nextPoint = FlattenToFloor(guidePoint.position, floorHeight);
            if (GetHorizontalDistance(routePoints[routePoints.Count - 1], nextPoint) > 0.05f)
            {
                routePoints.Add(nextPoint);
            }
        }

        Vector3 targetPoint = FlattenToFloor(currentWaypoint.WaypointTransform.position, floorHeight);
        if (GetHorizontalDistance(routePoints[routePoints.Count - 1], targetPoint) > 0.05f)
        {
            routePoints.Add(targetPoint);
        }

        return routePoints;
    }

    public Vector3 GetLocalRoutePosition(Vector3 worldPosition)
    {
        return routeRoot != null ? routeRoot.InverseTransformPoint(worldPosition) : worldPosition;
    }

    public Vector2 LocalRoutePositionToMap(Vector3 localRoutePosition, RectTransform mapArea)
    {
        float x = Mathf.InverseLerp(mapMin.x, mapMax.x, localRoutePosition.x);
        float y = Mathf.InverseLerp(mapMin.y, mapMax.y, localRoutePosition.z);
        Vector2 mapSize = mapArea.rect.size;

        return new Vector2(
            (x - 0.5f) * mapSize.x,
            (y - 0.5f) * mapSize.y);
    }

    private void RemoveNullWaypoints()
    {
        waypoints.RemoveAll(waypoint => waypoint == null || waypoint.WaypointTransform == null);
    }

    private Vector3 FlattenToFloor(Vector3 worldPosition, float floorHeight)
    {
        worldPosition.y = floorHeight;
        return worldPosition;
    }

    private float GetHorizontalDistance(Vector3 start, Vector3 end)
    {
        start.y = 0f;
        end.y = 0f;
        return Vector3.Distance(start, end);
    }
}
