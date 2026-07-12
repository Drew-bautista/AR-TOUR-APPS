using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

/// <summary>
/// Calibrates the indoor route onto a detected floor plane and spawns blue AR arrows along the path.
/// </summary>
public class ARArrowSpawner : MonoBehaviour
{
    private static readonly List<ARRaycastHit> FloorHits = new List<ARRaycastHit>();

    [Header("AR References")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Navigation References")]
    [SerializeField] private WaypointManager waypointManager;
    [SerializeField] private PlayerTracker playerTracker;
    [SerializeField] private Transform routeCalibrationRoot;
    [SerializeField] private Transform arrowRoot;

    [Header("Arrow Placement")]
    [SerializeField] private int arrowCount = 6;
    [SerializeField] private float firstArrowOffset = 0.7f;
    [SerializeField] private float arrowSpacing = 0.8f;
    [SerializeField] private float arrowHeightOffset = 0.03f;
    [SerializeField] private float arrowMoveSmoothing = 12f;
    [SerializeField] private float arrowTurnSmoothing = 10f;
    [SerializeField] private float arrowScale = 0.34f;
    [SerializeField] private float destinationMarkerHeight = 0.08f;
    [SerializeField] private bool autoCalibrateOnFloorDetection = true;

    private readonly List<Transform> arrowPool = new List<Transform>();
    private readonly List<Vector3> activeRouteWorldPoints = new List<Vector3>();

    private Material arrowMaterial;
    private Material accentMaterial;
    private Transform destinationMarker;

    public bool IsCalibrated { get; private set; }
    public IReadOnlyList<Vector3> ActiveRouteWorldPoints => activeRouteWorldPoints;
    public Vector3 CurrentGuideDirection { get; private set; }
    public Transform RouteCalibrationRoot => routeCalibrationRoot;

    private void Awake()
    {
        if (xrOrigin == null)
        {
            xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        }

        if (arCamera == null)
        {
            arCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;
        }

        if (raycastManager == null && xrOrigin != null)
        {
            raycastManager = xrOrigin.GetComponent<ARRaycastManager>();
        }

        if (planeManager == null && xrOrigin != null)
        {
            planeManager = xrOrigin.GetComponent<ARPlaneManager>();
        }

        if (arrowRoot == null)
        {
            GameObject arrowRootObject = new GameObject("GroundArrows");
            arrowRoot = arrowRootObject.transform;
            if (routeCalibrationRoot != null)
            {
                arrowRoot.SetParent(routeCalibrationRoot, false);
            }
        }

        if (routeCalibrationRoot == null)
        {
            GameObject routeRootObject = new GameObject("RouteCalibrationRoot");
            routeCalibrationRoot = routeRootObject.transform;
        }

        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        EnsureMaterials();
        EnsureArrowPool();
        EnsureDestinationMarker();
        SetRouteVisualsActive(false);
    }

    private void Update()
    {
        if (!TryEnsureCalibration())
        {
            SetRouteVisualsActive(false);
            return;
        }

        UpdateArrowRoute();
    }

    public void ResetCalibration()
    {
        IsCalibrated = false;
        SetRouteVisualsActive(false);
    }

    private bool TryEnsureCalibration()
    {
        if (IsCalibrated)
        {
            return true;
        }

        if (!autoCalibrateOnFloorDetection || raycastManager == null || arCamera == null || routeCalibrationRoot == null)
        {
            return false;
        }

        Vector2 screenProbePoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.62f);
        if (!raycastManager.Raycast(screenProbePoint, FloorHits, TrackableType.PlaneWithinPolygon))
        {
            return false;
        }

        Pose calibrationPose = FloorHits[0].pose;
        Vector3 forward = arCamera.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        routeCalibrationRoot.SetPositionAndRotation(
            calibrationPose.position,
            Quaternion.LookRotation(forward.normalized, Vector3.up));

        if (playerTracker != null)
        {
            playerTracker.SetRouteRoot(routeCalibrationRoot);
        }

        if (waypointManager != null)
        {
            waypointManager.SetRouteRoot(routeCalibrationRoot);
        }

        IsCalibrated = true;
        SetRouteVisualsActive(true);
        return true;
    }

    private void UpdateArrowRoute()
    {
        if (waypointManager == null || playerTracker == null || waypointManager.IsRouteComplete)
        {
            SetRouteVisualsActive(false);
            return;
        }

        activeRouteWorldPoints.Clear();
        activeRouteWorldPoints.AddRange(waypointManager.BuildCurrentRoute(playerTracker.WorldPosition));

        if (activeRouteWorldPoints.Count < 2)
        {
            SetRouteVisualsActive(false);
            return;
        }

        SetRouteVisualsActive(true);
        CurrentGuideDirection = GetRouteDirectionAtDistance(activeRouteWorldPoints, 0.01f);

        float routeLength = GetRouteLength(activeRouteWorldPoints);
        for (int i = 0; i < arrowPool.Count; i++)
        {
            Transform arrow = arrowPool[i];
            if (arrow == null)
            {
                continue;
            }

            float routeSampleDistance = firstArrowOffset + (arrowSpacing * i);
            if (routeSampleDistance >= routeLength - 0.05f)
            {
                arrow.gameObject.SetActive(false);
                continue;
            }

            Vector3 position = SampleRoutePosition(activeRouteWorldPoints, routeSampleDistance);
            Vector3 direction = GetRouteDirectionAtDistance(activeRouteWorldPoints, routeSampleDistance);
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = CurrentGuideDirection;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 targetPosition = position + (Vector3.up * arrowHeightOffset);

            arrow.gameObject.SetActive(true);
            arrow.position = Vector3.Lerp(arrow.position, targetPosition, arrowMoveSmoothing * Time.deltaTime);
            arrow.rotation = Quaternion.Slerp(arrow.rotation, targetRotation, arrowTurnSmoothing * Time.deltaTime);

            float pulse = 1f + Mathf.Sin((Time.time * 2.8f) + i) * 0.04f;
            arrow.localScale = Vector3.one * (arrowScale * pulse);
        }

        UpdateDestinationMarker();
    }

    private void UpdateDestinationMarker()
    {
        if (destinationMarker == null || waypointManager == null || waypointManager.CurrentWaypoint == null)
        {
            return;
        }

        Vector3 targetPosition = waypointManager.GetCurrentWaypointWorldPosition();
        targetPosition.y = routeCalibrationRoot != null ? routeCalibrationRoot.position.y + destinationMarkerHeight : targetPosition.y;

        float pulse = 1f + Mathf.Sin(Time.time * 2.2f) * 0.08f;
        destinationMarker.position = targetPosition;
        destinationMarker.localScale = Vector3.one * pulse;
        destinationMarker.gameObject.SetActive(true);
    }

    private void SetRouteVisualsActive(bool isActive)
    {
        for (int i = 0; i < arrowPool.Count; i++)
        {
            if (arrowPool[i] != null)
            {
                arrowPool[i].gameObject.SetActive(isActive);
            }
        }

        if (destinationMarker != null)
        {
            destinationMarker.gameObject.SetActive(isActive);
        }
    }

    private void EnsureArrowPool()
    {
        while (arrowPool.Count < arrowCount)
        {
            int arrowIndex = arrowPool.Count + 1;
            arrowPool.Add(CreateArrowInstance("GroundArrow_" + arrowIndex));
        }
    }

    private void EnsureMaterials()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        arrowMaterial = new Material(shader)
        {
            color = new Color(0.13f, 0.44f, 0.95f)
        };

        accentMaterial = new Material(shader)
        {
            color = new Color(0.95f, 0.98f, 1f)
        };

        if (arrowMaterial.HasProperty("_EmissionColor"))
        {
            arrowMaterial.EnableKeyword("_EMISSION");
            arrowMaterial.SetColor("_EmissionColor", new Color(0.05f, 0.2f, 0.56f));
        }

        if (accentMaterial.HasProperty("_EmissionColor"))
        {
            accentMaterial.EnableKeyword("_EMISSION");
            accentMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.13f, 0.2f));
        }
    }

    private void EnsureDestinationMarker()
    {
        if (destinationMarker != null)
        {
            return;
        }

        GameObject markerRoot = new GameObject("DestinationMarker");
        markerRoot.transform.SetParent(routeCalibrationRoot, false);

        GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisc.name = "BaseDisc";
        baseDisc.transform.SetParent(markerRoot.transform, false);
        baseDisc.transform.localScale = new Vector3(0.25f, 0.01f, 0.25f);
        ApplyVisual(baseDisc, arrowMaterial);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Core";
        core.transform.SetParent(markerRoot.transform, false);
        core.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        core.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        ApplyVisual(core, accentMaterial);

        destinationMarker = markerRoot.transform;
    }

    private Transform CreateArrowInstance(string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(arrowRoot, false);

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Disc";
        disc.transform.SetParent(root.transform, false);
        disc.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
        ApplyVisual(disc, arrowMaterial);

        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "Shaft";
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localPosition = new Vector3(0f, 0.08f, 0.05f);
        shaft.transform.localScale = new Vector3(0.16f, 0.05f, 0.45f);
        ApplyVisual(shaft, accentMaterial);

        GameObject headLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headLeft.name = "HeadLeft";
        headLeft.transform.SetParent(root.transform, false);
        headLeft.transform.localPosition = new Vector3(-0.12f, 0.08f, 0.22f);
        headLeft.transform.localRotation = Quaternion.Euler(0f, 42f, 0f);
        headLeft.transform.localScale = new Vector3(0.14f, 0.05f, 0.28f);
        ApplyVisual(headLeft, accentMaterial);

        GameObject headRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headRight.name = "HeadRight";
        headRight.transform.SetParent(root.transform, false);
        headRight.transform.localPosition = new Vector3(0.12f, 0.08f, 0.22f);
        headRight.transform.localRotation = Quaternion.Euler(0f, -42f, 0f);
        headRight.transform.localScale = new Vector3(0.14f, 0.05f, 0.28f);
        ApplyVisual(headRight, accentMaterial);

        return root.transform;
    }

    private void ApplyVisual(GameObject primitive, Material material)
    {
        Collider primitiveCollider = primitive.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            Destroy(primitiveCollider);
        }

        MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private float GetRouteLength(List<Vector3> routePoints)
    {
        float totalLength = 0f;
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            totalLength += Vector3.Distance(routePoints[i], routePoints[i + 1]);
        }

        return totalLength;
    }

    private Vector3 SampleRoutePosition(List<Vector3> routePoints, float distanceAlongRoute)
    {
        if (routePoints.Count == 0)
        {
            return Vector3.zero;
        }

        if (distanceAlongRoute <= 0f || routePoints.Count == 1)
        {
            return routePoints[0];
        }

        float remainingDistance = distanceAlongRoute;
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(routePoints[i], routePoints[i + 1]);
            if (segmentLength <= 0.001f)
            {
                continue;
            }

            if (remainingDistance <= segmentLength)
            {
                float t = remainingDistance / segmentLength;
                return Vector3.Lerp(routePoints[i], routePoints[i + 1], t);
            }

            remainingDistance -= segmentLength;
        }

        return routePoints[routePoints.Count - 1];
    }

    private Vector3 GetRouteDirectionAtDistance(List<Vector3> routePoints, float distanceAlongRoute)
    {
        if (routePoints.Count < 2)
        {
            return Vector3.forward;
        }

        float remainingDistance = distanceAlongRoute;
        for (int i = 0; i < routePoints.Count - 1; i++)
        {
            Vector3 start = routePoints[i];
            Vector3 end = routePoints[i + 1];
            Vector3 direction = end - start;
            float segmentLength = direction.magnitude;
            if (segmentLength <= 0.001f)
            {
                continue;
            }

            if (remainingDistance <= segmentLength)
            {
                direction.y = 0f;
                return direction.normalized;
            }

            remainingDistance -= segmentLength;
        }

        Vector3 finalDirection = routePoints[routePoints.Count - 1] - routePoints[routePoints.Count - 2];
        finalDirection.y = 0f;
        return finalDirection.sqrMagnitude > 0.001f ? finalDirection.normalized : Vector3.forward;
    }
}
