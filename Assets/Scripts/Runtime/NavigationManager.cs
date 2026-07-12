using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using Object = UnityEngine.Object;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Controls the full AR tour flow, the route guide, the mini-map,
/// the location progression, and optional narration playback.
/// </summary>
public class NavigationManager : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform arrowAnchor;
    [SerializeField] private ArrowController arrowController;
    [SerializeField] private BillboardLabel floatingLabel;
    [SerializeField] private AudioSource narrationSource;
    [SerializeField] private Transform destinationBeacon;
    [SerializeField] private List<Transform> breadcrumbDots = new List<Transform>();

    [Header("UI References")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text instructionText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text centerBadgeText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button exitButton;

    [Header("Mini Map")]
    [SerializeField] private MiniMapController miniMapController;
    [SerializeField] private RectTransform mapViewport;
    [SerializeField] private RectTransform mapArea;
    [SerializeField] private RectTransform currentDot;
    [SerializeField] private RectTransform targetDot;
    [SerializeField] private RectTransform pathLine;
    [SerializeField] private List<RectTransform> mapPathSegments = new List<RectTransform>();
    [SerializeField] private Vector2 worldMin = new Vector2(-1f, -1f);
    [SerializeField] private Vector2 worldMax = new Vector2(7f, 12f);
    [SerializeField] private bool keepCurrentDotCentered = true;
    [SerializeField] private float miniMapFollowSmoothing = 10f;

    [Header("Progression")]
    [SerializeField] private float autoAdvanceDistance = 1.5f;
    [SerializeField] private float autoAdvanceCooldown = 0.75f;

    [Header("Tour Locations")]
    [SerializeField] private List<LocationTrigger> tourLocations = new List<LocationTrigger>();

    [Header("Guide Placement")]
    [SerializeField] private bool showCameraArrow = false;
    [SerializeField] private bool showLegacyWorldGuide = false;
    [SerializeField] private float arrowDistanceFromCamera = 1.15f;
    [SerializeField] private float arrowHeightOffset = -0.05f;
    [SerializeField] private float guideBlendWithCameraForward = 0.65f;
    [SerializeField] private bool keepArrowCenteredOnScreen = true;
    [SerializeField] private Vector2 arrowViewportPosition = new Vector2(0.5f, 0.34f);
    [SerializeField] private float breadcrumbStartDistance = 0.95f;
    [SerializeField] private float breadcrumbSpacing = 0.75f;
    [SerializeField] private float breadcrumbHeightOffset = 0.05f;
    [SerializeField] private float destinationBeaconHeight = 0.16f;

    [Header("Scenes")]
    [SerializeField] private string homeSceneName = "HomeScene";

    private readonly List<Vector3> activeRoutePoints = new List<Vector3>();

    private int currentLocationIndex = -1;
    private int targetLocationIndex;
    private bool tourReady;
    private bool tourComplete;
    private string pinnedStatusMessage;
    private float pinnedStatusUntil;
    private Transform routeLookTarget;
    private Vector2 currentMapOffset;
    private bool hasMiniMapOffset;
    private float lastAutoAdvanceTime = -999f;

    private void Awake()
    {
        if (arSession == null)
        {
            arSession = Object.FindFirstObjectByType<ARSession>();
        }

        if (xrOrigin == null)
        {
            xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        }

        if (arCamera == null)
        {
            arCamera = xrOrigin != null && xrOrigin.Camera != null ? xrOrigin.Camera : Camera.main;
        }

        if (mapViewport == null && mapArea != null)
        {
            mapViewport = mapArea.parent as RectTransform;
        }

        if (miniMapController == null && mapArea != null)
        {
            miniMapController = mapArea.GetComponent<MiniMapController>();
            if (miniMapController == null)
            {
                miniMapController = mapArea.gameObject.AddComponent<MiniMapController>();
            }
        }

        if (narrationSource == null)
        {
            narrationSource = GetComponent<AudioSource>();
        }

        if (tourLocations.Count == 0)
        {
            tourLocations.AddRange(Object.FindObjectsByType<LocationTrigger>(FindObjectsSortMode.None));
        }

        tourLocations.Sort((left, right) => left.SequenceOrder.CompareTo(right.SequenceOrder));

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(GoToNextLocation);
            nextButton.onClick.AddListener(GoToNextLocation);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBackToPreviousLocation);
            backButton.onClick.AddListener(GoBackToPreviousLocation);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitTour);
            exitButton.onClick.AddListener(ExitTour);
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(GoToNextLocation);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(GoBackToPreviousLocation);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitTour);
        }
    }

    private IEnumerator Start()
    {
        Application.targetFrameRate = 60;
        PinStatus(AppLanguage.Text("requesting_ar"), 8f);

        yield return RequestCameraPermissionIfNeeded();
        yield return InitializeARSession();

        if (arCamera == null)
        {
            PinStatus(AppLanguage.Text("ar_camera_missing"), 30f);
            yield break;
        }

        PrepareTour();
        RefreshNavigationUI();
        tourReady = true;
        PinStatus(AppLanguage.Text("move_slowly"), 4f);
    }

    private void Update()
    {
        if (!tourReady)
        {
            return;
        }

        UpdateGuideState();
        UpdateArrowAnchor();
        UpdateMiniMap();
        RefreshStatusDisplay();
    }

    /// <summary>
    /// Used by LocationTrigger so only the active stop can complete the tour step.
    /// </summary>
    public bool IsCurrentTarget(LocationTrigger location)
    {
        return !tourComplete &&
               targetLocationIndex >= 0 &&
               targetLocationIndex < tourLocations.Count &&
               tourLocations[targetLocationIndex] == location;
    }

    /// <summary>
    /// Called automatically when the user physically reaches a location,
    /// or manually when the Next button is used as a testing shortcut.
    /// </summary>
    public void NotifyLocationReached(LocationTrigger location, bool manualAdvance)
    {
        if (!IsCurrentTarget(location))
        {
            return;
        }

        location.SetVisited(true);
        currentLocationIndex = targetLocationIndex;

        if (manualAdvance)
        {
            PinStatus(AppLanguage.Text("manual_advance"), 3f);
        }
        else
        {
            PinStatus(AppLanguage.ArrivedAt(location.LocationName), 3f);
        }

        PlayNarration(location);

        targetLocationIndex++;
        if (targetLocationIndex >= tourLocations.Count)
        {
            CompleteTour();
            return;
        }

        RefreshNavigationUI();
    }

    /// <summary>
    /// Moves forward to the next stop without walking there.
    /// Useful when testing in the Editor or in a small room.
    /// </summary>
    public void GoToNextLocation()
    {
        if (tourComplete || targetLocationIndex < 0 || targetLocationIndex >= tourLocations.Count)
        {
            return;
        }

        NotifyLocationReached(tourLocations[targetLocationIndex], true);
    }

    /// <summary>
    /// Rewinds the route by one stop so the user can review a previous destination.
    /// </summary>
    public void GoBackToPreviousLocation()
    {
        if (tourLocations.Count == 0 || targetLocationIndex <= 0)
        {
            return;
        }

        int newTargetIndex = Mathf.Max(targetLocationIndex - 1, 0);
        for (int i = newTargetIndex; i < tourLocations.Count; i++)
        {
            tourLocations[i].SetVisited(false);
        }

        currentLocationIndex = newTargetIndex - 1;
        targetLocationIndex = newTargetIndex;
        tourComplete = false;

        if (arrowAnchor != null && showCameraArrow)
        {
            arrowAnchor.gameObject.SetActive(true);
        }

        RefreshNavigationUI();
        PinStatus(AppLanguage.RouteRewoundTo(tourLocations[targetLocationIndex].LocationName), 3f);
    }

    /// <summary>
    /// Returns to the home screen.
    /// </summary>
    public void ExitTour()
    {
        SceneManager.LoadScene(homeSceneName);
    }

    private IEnumerator RequestCameraPermissionIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);

            float timeout = 10f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
#endif
        yield return null;
    }

    private IEnumerator InitializeARSession()
    {
        if (arSession == null)
        {
            yield break;
        }

        yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            PinStatus(AppLanguage.Text("installing_ar"), 10f);
            yield return ARSession.Install();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            PinStatus(AppLanguage.Text("unsupported_ar"), 30f);
        }
    }

    private void PrepareTour()
    {
        ApplyResponsiveTextSettings();

        if (titleText != null)
        {
            titleText.text = "Digital Heritage Archive tour";
        }

        for (int i = 0; i < tourLocations.Count; i++)
        {
            tourLocations[i].SetVisited(false);
            tourLocations[i].Configure(this, arCamera.transform);
        }

        targetLocationIndex = 0;

        if (floatingLabel != null)
        {
            floatingLabel.SetTargetCamera(arCamera.transform);
            floatingLabel.SetMessage(string.Empty);
            floatingLabel.gameObject.SetActive(showCameraArrow);
        }

        if (routeLookTarget == null)
        {
            routeLookTarget = new GameObject("RouteLookTarget").transform;
        }

        InitializeMiniMapController();

        if (miniMapController == null && keepCurrentDotCentered && currentDot != null && mapViewport != null && currentDot.parent == mapArea)
        {
            currentDot.SetParent(mapViewport, false);
        }

        if (arrowController != null)
        {
            arrowController.SetTarget(showCameraArrow && tourLocations.Count > 0 ? routeLookTarget : null);
        }

        if (arrowAnchor != null)
        {
            arrowAnchor.gameObject.SetActive(showCameraArrow);
        }

        currentMapOffset = Vector2.zero;
        hasMiniMapOffset = false;

        if (nextButton != null)
        {
            nextButton.interactable = tourLocations.Count > 0;
        }

        if (backButton != null)
        {
            backButton.interactable = false;
        }

        if (exitButton != null)
        {
            exitButton.interactable = true;
        }

        SetGuideObjectsActive(true);
    }

    private void RefreshNavigationUI()
    {
        if (tourLocations.Count == 0)
        {
            if (instructionText != null)
            {
                instructionText.text = AppLanguage.Text("no_locations");
            }

            if (descriptionText != null)
            {
                descriptionText.text = AppLanguage.Text("add_locations");
            }

            if (progressText != null)
            {
                progressText.text = AppLanguage.FormatProgress(0, 0);
            }

            if (distanceText != null)
            {
                distanceText.text = "--";
            }

            SetCenterBadgeText(string.Empty);

            return;
        }

        if (targetLocationIndex >= tourLocations.Count)
        {
            CompleteTour();
            return;
        }

        LocationTrigger targetLocation = tourLocations[targetLocationIndex];

        if (instructionText != null)
        {
            instructionText.text = GetGuidePrompt(targetLocation, false);
        }

        if (descriptionText != null)
        {
            descriptionText.text = GetCompactLocationName(targetLocation.LocationName) + ": " +
                                   AppLanguage.LocalizeTourDescription(targetLocation.LocationName, targetLocation.Description);
        }

        if (progressText != null)
        {
            progressText.text = AppLanguage.FormatProgress(targetLocationIndex + 1, tourLocations.Count);
        }

        SetCenterBadgeText(GetGuidePrompt(targetLocation, false));

        if (nextButton != null)
        {
            nextButton.interactable = true;
        }

        if (backButton != null)
        {
            backButton.interactable = targetLocationIndex > 0;
        }
    }

    private void InitializeMiniMapController()
    {
        if (miniMapController == null)
        {
            return;
        }

        miniMapController.ConfigureReferences(
            mapViewport,
            mapArea,
            currentDot,
            targetDot,
            pathLine,
            mapPathSegments,
            worldMin,
            worldMax);
        miniMapController.Initialize(tourLocations);
        miniMapController.StyleNavigationButtons(backButton, nextButton, exitButton);

        keepCurrentDotCentered = false;
        if (mapArea != null)
        {
            mapArea.anchoredPosition = Vector2.zero;
        }
    }

    private void UpdateGuideState()
    {
        if (tourComplete || targetLocationIndex < 0 || targetLocationIndex >= tourLocations.Count || arCamera == null)
        {
            SetGuideObjectsActive(false);
            return;
        }

        LocationTrigger targetLocation = tourLocations[targetLocationIndex];
        BuildActiveRoutePoints(targetLocation);

        Vector3 nextGuidePoint = GetNextGuidePoint(targetLocation);
        if (routeLookTarget != null)
        {
            routeLookTarget.position = nextGuidePoint;
        }

        if (showLegacyWorldGuide)
        {
            UpdateBreadcrumbs(targetLocation);
            UpdateDestinationBeacon(targetLocation);
        }
        else
        {
            SetGuideObjectsActive(false);
        }

        UpdateDistanceText(targetLocation);

        if (TryAutoProgress(targetLocation))
        {
            return;
        }
    }

    private void BuildActiveRoutePoints(LocationTrigger targetLocation)
    {
        activeRoutePoints.Clear();

        float guideFloor = targetLocation.transform.position.y;
        Vector3 currentPosition = arCamera.transform.position;
        currentPosition.y = guideFloor;
        activeRoutePoints.Add(currentPosition);

        IReadOnlyList<Transform> approachPoints = targetLocation.ApproachPoints;
        for (int i = 0; i < approachPoints.Count; i++)
        {
            Transform guidePoint = approachPoints[i];
            if (guidePoint == null)
            {
                continue;
            }

            Vector3 point = guidePoint.position;
            point.y = guideFloor;

            if (HorizontalDistance(currentPosition, point) <= 0.45f)
            {
                continue;
            }

            if (HorizontalDistance(activeRoutePoints[activeRoutePoints.Count - 1], point) > 0.05f)
            {
                activeRoutePoints.Add(point);
            }
        }

        Vector3 targetPoint = targetLocation.transform.position;
        targetPoint.y = guideFloor;

        if (HorizontalDistance(activeRoutePoints[activeRoutePoints.Count - 1], targetPoint) > 0.05f)
        {
            activeRoutePoints.Add(targetPoint);
        }
    }

    private void UpdateArrowAnchor()
    {
        if (!showCameraArrow)
        {
            if (arrowAnchor != null && arrowAnchor.gameObject.activeSelf)
            {
                arrowAnchor.gameObject.SetActive(false);
            }

            return;
        }

        if (arrowAnchor == null || arCamera == null || tourComplete)
        {
            return;
        }

        if (keepArrowCenteredOnScreen)
        {
            Vector3 centeredAnchorPosition = arCamera.ViewportToWorldPoint(new Vector3(
                Mathf.Clamp01(arrowViewportPosition.x),
                Mathf.Clamp01(arrowViewportPosition.y),
                Mathf.Max(arrowDistanceFromCamera, 0.2f)));

            centeredAnchorPosition += Vector3.up * arrowHeightOffset;
            arrowAnchor.position = centeredAnchorPosition;
            return;
        }

        Vector3 cameraForward = arCamera.transform.forward;
        cameraForward.y = 0f;
        if (cameraForward.sqrMagnitude < 0.001f)
        {
            cameraForward = Vector3.forward;
        }

        cameraForward.Normalize();

        Vector3 guideDirection = GetGuideDirection();
        if (guideDirection.sqrMagnitude < 0.001f)
        {
            guideDirection = cameraForward;
        }

        Vector3 anchorDirection = Vector3.Slerp(cameraForward, guideDirection, guideBlendWithCameraForward);
        anchorDirection.y = 0f;
        anchorDirection.Normalize();

        arrowAnchor.position = arCamera.transform.position +
                               (anchorDirection * arrowDistanceFromCamera) +
                               (Vector3.up * arrowHeightOffset);
    }

    private void UpdateBreadcrumbs(LocationTrigger targetLocation)
    {
        if (breadcrumbDots.Count == 0)
        {
            return;
        }

        float routeLength = GetRouteLength(activeRoutePoints);
        float guideFloor = targetLocation.transform.position.y + breadcrumbHeightOffset;

        for (int i = 0; i < breadcrumbDots.Count; i++)
        {
            Transform breadcrumb = breadcrumbDots[i];
            if (breadcrumb == null)
            {
                continue;
            }

            float sampleDistance = breadcrumbStartDistance + (breadcrumbSpacing * i);
            if (sampleDistance >= routeLength - 0.2f)
            {
                breadcrumb.gameObject.SetActive(false);
                continue;
            }

            Vector3 samplePosition = SampleRoutePosition(activeRoutePoints, sampleDistance);
            samplePosition.y = guideFloor + Mathf.Sin((Time.time * 2.8f) + (i * 0.4f)) * 0.015f;

            breadcrumb.gameObject.SetActive(true);
            breadcrumb.position = samplePosition;
            breadcrumb.rotation = Quaternion.identity;
        }
    }

    private void UpdateDestinationBeacon(LocationTrigger targetLocation)
    {
        if (destinationBeacon == null)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 2.2f) * 0.08f;
        destinationBeacon.gameObject.SetActive(true);
        destinationBeacon.position = targetLocation.transform.position +
                                     Vector3.up * (destinationBeaconHeight + Mathf.Sin(Time.time * 1.6f) * 0.03f);
        destinationBeacon.localScale = Vector3.one * pulse;
    }

    private void UpdateDistanceText(LocationTrigger targetLocation)
    {
        float routeDistance = GetRouteLength(activeRoutePoints);

        if (distanceText != null)
        {
            distanceText.text = FormatDistance(routeDistance);
        }

        string guidePrompt = GetGuidePrompt(
            targetLocation,
            routeDistance <= targetLocation.ReachDistance + 0.15f);

        if (instructionText != null)
        {
            instructionText.text = guidePrompt;
        }

        if (floatingLabel != null && showCameraArrow)
        {
            floatingLabel.SetMessage(guidePrompt);
        }

        SetCenterBadgeText(guidePrompt);
    }

    private bool TryAutoProgress(LocationTrigger targetLocation)
    {
        if (targetLocation == null || arCamera == null || tourComplete)
        {
            return false;
        }

        if (Time.unscaledTime - lastAutoAdvanceTime < autoAdvanceCooldown)
        {
            return false;
        }

        float directDistance = HorizontalDistance(arCamera.transform.position, targetLocation.transform.position);
        if (directDistance > Mathf.Max(0.1f, autoAdvanceDistance))
        {
            return false;
        }

        lastAutoAdvanceTime = Time.unscaledTime;
        NotifyLocationReached(targetLocation, false);
        return true;
    }

    private void UpdateMiniMap()
    {
        if (miniMapController != null && arCamera != null)
        {
            miniMapController.UpdateMap(
                arCamera.transform.position,
                activeRoutePoints,
                targetLocationIndex,
                currentLocationIndex,
                tourComplete);
            return;
        }

        if (mapArea == null || currentDot == null || targetDot == null || arCamera == null || tourLocations.Count == 0)
        {
            return;
        }

        if (mapViewport == null)
        {
            mapViewport = mapArea.parent as RectTransform;
        }

        Vector2 currentMapPosition = WorldToMapPosition(arCamera.transform.position);
        UpdateMiniMapFollow(currentMapPosition);
        currentDot.anchoredPosition = keepCurrentDotCentered
            ? currentMapPosition + mapArea.anchoredPosition
            : currentMapPosition;

        if (!tourComplete && targetLocationIndex >= 0 && targetLocationIndex < tourLocations.Count)
        {
            Vector2 targetMapPosition = WorldToMapPosition(tourLocations[targetLocationIndex].transform.position);
            targetDot.anchoredPosition = targetMapPosition;
            targetDot.gameObject.SetActive(true);

            if (mapPathSegments.Count > 0)
            {
                UpdateMapPathSegments();

                if (pathLine != null)
                {
                    pathLine.gameObject.SetActive(false);
                }
            }
            else if (pathLine != null)
            {
                Vector2 direction = targetMapPosition - currentMapPosition;
                pathLine.gameObject.SetActive(true);
                pathLine.anchoredPosition = currentMapPosition + direction * 0.5f;
                pathLine.sizeDelta = new Vector2(direction.magnitude, pathLine.sizeDelta.y);
                pathLine.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
        }
        else
        {
            targetDot.gameObject.SetActive(false);
            HideMapRoute();
        }
    }

    private void UpdateMiniMapFollow(Vector2 currentMapPosition)
    {
        if (!keepCurrentDotCentered || mapViewport == null)
        {
            mapArea.anchoredPosition = Vector2.zero;
            hasMiniMapOffset = false;
            return;
        }

        Vector2 desiredOffset = ClampMiniMapOffset(-currentMapPosition);

        if (!hasMiniMapOffset)
        {
            currentMapOffset = desiredOffset;
            hasMiniMapOffset = true;
        }
        else
        {
            float followBlend = 1f - Mathf.Exp(-miniMapFollowSmoothing * Time.deltaTime);
            currentMapOffset = Vector2.Lerp(currentMapOffset, desiredOffset, followBlend);
        }

        mapArea.anchoredPosition = currentMapOffset;
    }

    private Vector2 ClampMiniMapOffset(Vector2 desiredOffset)
    {
        if (mapViewport == null)
        {
            return desiredOffset;
        }

        float limitX = Mathf.Max(0f, (mapArea.rect.width - mapViewport.rect.width) * 0.5f);
        float limitY = Mathf.Max(0f, (mapArea.rect.height - mapViewport.rect.height) * 0.5f);

        return new Vector2(
            Mathf.Clamp(desiredOffset.x, -limitX, limitX),
            Mathf.Clamp(desiredOffset.y, -limitY, limitY));
    }

    private void UpdateMapPathSegments()
    {
        int segmentIndex = 0;
        for (int i = 0; i < activeRoutePoints.Count - 1 && segmentIndex < mapPathSegments.Count; i++)
        {
            Vector2 start = WorldToMapPosition(activeRoutePoints[i]);
            Vector2 end = WorldToMapPosition(activeRoutePoints[i + 1]);
            Vector2 direction = end - start;

            if (direction.sqrMagnitude < 0.01f)
            {
                continue;
            }

            RectTransform segment = mapPathSegments[segmentIndex];
            if (segment == null)
            {
                continue;
            }

            segment.gameObject.SetActive(true);
            segment.anchoredPosition = start + direction * 0.5f;
            segment.sizeDelta = new Vector2(direction.magnitude, segment.sizeDelta.y);
            segment.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            segmentIndex++;
        }

        for (int i = segmentIndex; i < mapPathSegments.Count; i++)
        {
            if (mapPathSegments[i] != null)
            {
                mapPathSegments[i].gameObject.SetActive(false);
            }
        }
    }

    private Vector2 WorldToMapPosition(Vector3 worldPosition)
    {
        float x = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x);
        float y = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z);
        Vector2 mapSize = mapArea.rect.size;

        return new Vector2(
            (x - 0.5f) * mapSize.x,
            (y - 0.5f) * mapSize.y);
    }

    private void PlayNarration(LocationTrigger location)
    {
        if (narrationSource == null || location.NarrationClip == null)
        {
            return;
        }

        narrationSource.Stop();
        narrationSource.clip = location.NarrationClip;
        narrationSource.Play();
    }

    private void CompleteTour()
    {
        tourComplete = true;

        if (instructionText != null)
        {
            instructionText.text = AppLanguage.Text("tour_complete");
        }

        if (descriptionText != null)
        {
            descriptionText.text = AppLanguage.IsFilipino
                ? "Naabot mo na ang huling stop ng Digital Heritage Archive tour."
                : "You have reached the final stop of the Digital Heritage Archive tour.";
        }

        if (progressText != null)
        {
            progressText.text = AppLanguage.FormatProgress(tourLocations.Count, tourLocations.Count);
        }

        if (distanceText != null)
        {
            distanceText.text = AppLanguage.Text("completed");
        }

        PinStatus(AppLanguage.Text("tour_finished_status"), 120f);

        if (floatingLabel != null)
        {
            floatingLabel.SetMessage(string.Empty);
        }

        SetCenterBadgeText(AppLanguage.Text("tour_complete"));

        if (arrowController != null)
        {
            arrowController.SetTarget(null);
        }

        if (arrowAnchor != null)
        {
            arrowAnchor.gameObject.SetActive(false);
        }

        SetGuideObjectsActive(false);

        if (nextButton != null)
        {
            nextButton.interactable = false;
        }

        ReviewUIController.EnsurePopupInScene();
        ReviewManager.EnsureExists().MarkTourCompletedAndRequestReview();
    }

    private void SetGuideObjectsActive(bool isActive)
    {
        isActive = isActive && showLegacyWorldGuide;

        if (destinationBeacon != null)
        {
            destinationBeacon.gameObject.SetActive(isActive);
        }

        for (int i = 0; i < breadcrumbDots.Count; i++)
        {
            if (breadcrumbDots[i] != null)
            {
                breadcrumbDots[i].gameObject.SetActive(isActive);
            }
        }

        if (!isActive)
        {
            HideMapRoute();
        }
    }

    private void HideMapRoute()
    {
        if (pathLine != null)
        {
            pathLine.gameObject.SetActive(false);
        }

        for (int i = 0; i < mapPathSegments.Count; i++)
        {
            if (mapPathSegments[i] != null)
            {
                mapPathSegments[i].gameObject.SetActive(false);
            }
        }
    }

    private Vector3 GetGuideDirection()
    {
        if (activeRoutePoints.Count > 1)
        {
            Vector3 direction = activeRoutePoints[1] - activeRoutePoints[0];
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }
        }

        if (targetLocationIndex >= 0 && targetLocationIndex < tourLocations.Count && arCamera != null)
        {
            Vector3 direction = tourLocations[targetLocationIndex].transform.position - arCamera.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }
        }

        return Vector3.zero;
    }

    private Vector3 GetNextGuidePoint(LocationTrigger targetLocation)
    {
        if (activeRoutePoints.Count > 1)
        {
            Vector3 nextPoint = activeRoutePoints[1];
            nextPoint.y = targetLocation.transform.position.y;
            return nextPoint;
        }

        return targetLocation.transform.position;
    }

    private float GetRouteLength(List<Vector3> points)
    {
        float totalLength = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            totalLength += HorizontalDistance(points[i], points[i + 1]);
        }

        return totalLength;
    }

    private Vector3 SampleRoutePosition(List<Vector3> points, float distanceAlongRoute)
    {
        if (points.Count == 0)
        {
            return Vector3.zero;
        }

        if (points.Count == 1 || distanceAlongRoute <= 0f)
        {
            return points[0];
        }

        float remainingDistance = distanceAlongRoute;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 start = points[i];
            Vector3 end = points[i + 1];
            float segmentLength = HorizontalDistance(start, end);

            if (segmentLength <= 0.001f)
            {
                continue;
            }

            if (remainingDistance <= segmentLength)
            {
                float t = remainingDistance / segmentLength;
                return Vector3.Lerp(start, end, t);
            }

            remainingDistance -= segmentLength;
        }

        return points[points.Count - 1];
    }

    private void RefreshStatusDisplay()
    {
        if (statusText == null || tourComplete || targetLocationIndex < 0 || targetLocationIndex >= tourLocations.Count)
        {
            return;
        }

        if (!string.IsNullOrEmpty(pinnedStatusMessage) && Time.unscaledTime <= pinnedStatusUntil)
        {
            statusText.text = pinnedStatusMessage;
            return;
        }

        float routeDistance = GetRouteLength(activeRoutePoints);
        float turnAngle = GetSignedGuideAngle();

        if (routeDistance <= tourLocations[targetLocationIndex].ReachDistance + 0.15f)
        {
            statusText.text = AppLanguage.Text("close_to_marker");
            return;
        }

        if (Mathf.Abs(turnAngle) < 18f)
        {
            statusText.text = AppLanguage.Text("straight_status");
        }
        else if (turnAngle > 0f)
        {
            statusText.text = AppLanguage.Text("right_status");
        }
        else
        {
            statusText.text = AppLanguage.Text("left_status");
        }
    }

    private float GetSignedGuideAngle()
    {
        if (arCamera == null)
        {
            return 0f;
        }

        Vector3 cameraForward = arCamera.transform.forward;
        cameraForward.y = 0f;
        if (cameraForward.sqrMagnitude < 0.001f)
        {
            return 0f;
        }

        Vector3 guideDirection = GetGuideDirection();
        if (guideDirection.sqrMagnitude < 0.001f)
        {
            return 0f;
        }

        return Vector3.SignedAngle(cameraForward.normalized, guideDirection, Vector3.up);
    }

    private void PinStatus(string message, float durationSeconds)
    {
        pinnedStatusMessage = message;
        pinnedStatusUntil = Time.unscaledTime + Mathf.Max(durationSeconds, 0f);

        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ApplyResponsiveTextSettings()
    {
        ConfigureResponsiveText(titleText, 22, 30);
        ConfigureResponsiveText(statusText, 14, 22);
        ConfigureResponsiveText(instructionText, 20, 34);
        ConfigureResponsiveText(progressText, 16, 20);
        ConfigureResponsiveText(centerBadgeText, 14, 24);
    }

    private static void ConfigureResponsiveText(Text textComponent, int minSize, int maxSize)
    {
        if (textComponent == null)
        {
            return;
        }

        textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        textComponent.verticalOverflow = VerticalWrapMode.Overflow;
        textComponent.resizeTextForBestFit = true;
        textComponent.resizeTextMinSize = minSize;
        textComponent.resizeTextMaxSize = maxSize;
    }

    private string GetGuidePrompt(LocationTrigger targetLocation, bool arriving)
    {
        if (arriving)
        {
            return AppLanguage.Text("arrived");
        }

        if (targetLocation == null)
        {
            return AppLanguage.Text("proceed");
        }

        float turnAngle = GetSignedGuideAngle();
        if (Mathf.Abs(turnAngle) < 18f)
        {
            return AppLanguage.Text("go_straight");
        }

        if (turnAngle > 0f)
        {
            return AppLanguage.Text("turn_right");
        }

        return AppLanguage.Text("turn_left");
    }

    private string GetCompactLocationName(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName))
        {
            return AppLanguage.IsFilipino ? "susunod na hinto" : "next stop";
        }

        if (locationName.Contains("Emilio Aguinaldo", StringComparison.OrdinalIgnoreCase))
        {
            return "Exhibit";
        }

        if (locationName.Contains("Independence Balcony", StringComparison.OrdinalIgnoreCase))
        {
            return "Balcony";
        }

        if (locationName.Contains("Artifact Room", StringComparison.OrdinalIgnoreCase))
        {
            return "Artifact Room";
        }

        if (locationName.Contains("Main Hall", StringComparison.OrdinalIgnoreCase))
        {
            return "Main Hall";
        }

        return locationName;
    }

    private void SetCenterBadgeText(string message)
    {
        if (centerBadgeText != null)
        {
            centerBadgeText.text = message;
        }
    }

    private string FormatDistance(float distanceMeters)
    {
        if (distanceMeters < 1f)
        {
            return AppLanguage.FormatDistance(distanceMeters);
        }

        return AppLanguage.FormatDistance(distanceMeters);
    }

    private float HorizontalDistance(Vector3 start, Vector3 end)
    {
        start.y = 0f;
        end.y = 0f;
        return Vector3.Distance(start, end);
    }
}
