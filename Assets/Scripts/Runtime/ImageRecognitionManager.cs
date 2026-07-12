using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using Object = UnityEngine.Object;

/// <summary>
/// Handles live camera recognition with ARTrackedImageManager and also matches
/// gallery images against a predefined reference database.
/// </summary>
public class ImageRecognitionManager : MonoBehaviour
{
    private const string QrReferencePrefix = "qr-";
    private static readonly Dictionary<string, string> VideoQuizMediaKeysByItemId =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "scan-item-013", "att.4UPALrJaWNcL1rFsRBeARFu0UP8B2dBB58haSNUi1o0" },
            { "scan-item-018", "att.6AY3Aamn8uJgp6bH7VExt7OaW5Kaws6Zbn_d3RB1hek" },
            { "scan-item-006", "att.10jcFhOTiIJ93gcwokjBtZpwdhIdS2ga8wYuAj7hBdc" },
            { "scan-item-127", "att.TgqwnWu-F-o9tgSb03GQTNEv0XtpmdoN6kfT4veqTSQ" },
            { "scan-item-141", "att.V3EgYkGryHaaiV42sgk-m6Vr83rsNK7R2ICG9RAAoXM" },
            { "scan-item-176", "att.ZwJoStPTv-n2eHPWge4PT_XmZgOqTPxmrFzy2-LaPtM" }
        };

    [Serializable]
    public class ScanItemData
    {
        [SerializeField] private string id = "scan-item-001";
        [SerializeField] private string itemName = "Aguinaldo Shrine Scan Item";
        [SerializeField] private Sprite previewImage;
        [SerializeField] private Texture2D referenceTexture;
        [SerializeField] private Texture2D qrMarkerTexture;
        [SerializeField] [TextArea(2, 4)] private string description = "Reference image for the AR Scan Info System.";
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private float modelScale = 1f;
        [SerializeField] private Color modelTint = new Color(0.32f, 0.68f, 1f, 1f);
        [SerializeField] private bool showGeneratedReliefModel = true;
        [SerializeField] private float physicalWidthMeters = 0.18f;
        [SerializeField] private string averageHashHex = "0";
        [SerializeField] private string differenceHashHex = "0";
        [SerializeField] private float aspectRatio = 1f;
        [SerializeField] private bool useForTrackedImage = true;

        public string Id => id;
        public string ItemName => itemName;
        public Sprite PreviewImage => previewImage;
        public Texture2D ReferenceTexture => referenceTexture;
        public Texture2D QrMarkerTexture => qrMarkerTexture;
        public string Description => description;
        public AudioClip AudioClip => audioClip;
        public GameObject ModelPrefab => modelPrefab;
        public float ModelScale => modelScale;
        public Color ModelTint => modelTint;
        public bool ShowGeneratedReliefModel => showGeneratedReliefModel || modelPrefab == null;
        public float PhysicalWidthMeters => physicalWidthMeters;
        public string AverageHashHex => averageHashHex;
        public string DifferenceHashHex => differenceHashHex;
        public float AspectRatio => aspectRatio;
        public bool UseForTrackedImage => useForTrackedImage;
    }

    private enum ScanMode
    {
        None,
        Camera,
        QrMarker,
        Gallery
    }

    private struct ImageFingerprint
    {
        public ulong AverageHash;
        public ulong DifferenceHash;
        public float AspectRatio;
    }

    private static readonly Rect[] LiveScanCropPresets =
    {
        new Rect(0f, 0f, 1f, 1f),
        new Rect(0.04f, 0.04f, 0.92f, 0.92f),
        new Rect(0.08f, 0.08f, 0.84f, 0.84f),
        new Rect(0.12f, 0.12f, 0.76f, 0.76f),
        new Rect(0.16f, 0.16f, 0.68f, 0.68f),
        new Rect(0.2f, 0.2f, 0.6f, 0.6f),
        new Rect(0.04f, 0.16f, 0.92f, 0.68f),
        new Rect(0.16f, 0.04f, 0.68f, 0.92f)
    };

    [Header("Scene References")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private GalleryPicker galleryPicker;
    [SerializeField] private ScanUIController scanUIController;
    [SerializeField] private AudioManager audioManager;

    [Header("Scan Database")]
    [SerializeField] private List<ScanItemData> scanItems = new List<ScanItemData>();
    [SerializeField] private int maxMovingImages = 1;
    [SerializeField] private float qrMarkerPhysicalWidthMeters = 0.08f;

    [Header("Fingerprint Matching")]
    [SerializeField] private int galleryHashResolution = 8;
    [SerializeField] private int maxAcceptedHashDistance = 24;
    [SerializeField] private int maxAcceptedLiveCameraHashDistance = 30;
    [SerializeField] private float maxAcceptedAspectDifference = 0.25f;
    [SerializeField] private int minimumBestMatchMargin = 6;

    [Header("Live Camera Fallback")]
    [SerializeField] private int liveCameraSampleSize = 256;
    [SerializeField] private float liveCameraScanIntervalSeconds = 0.55f;
    [SerializeField] private int minimumLiveCameraStableMatches = 2;
    [SerializeField] private bool liveCameraFallbackMatchesTrackedImagesOnly = false;
    [SerializeField] private int noMatchReminderInterval = 5;

    [Header("Smart Tour Auto Scan")]
    [SerializeField] private bool startCameraScanningAutomatically = true;
    [SerializeField] private bool resumeAutomaticScanAfterClosingResult = true;
    [SerializeField] private float repeatedCameraMatchCooldownSeconds = 5f;

    [Header("Tracked Object Labels")]
    [SerializeField] private float trackedLabelHeight = 0.12f;
    [SerializeField] private float trackedLabelScale = 0.05f;
    [SerializeField] private int trackedLabelFontSize = 34;

    private readonly Dictionary<string, ScanItemData> scanItemsById = new Dictionary<string, ScanItemData>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ImageFingerprint> fingerprintsById = new Dictionary<string, ImageFingerprint>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TrackableId, BillboardLabel> trackedLabelsByTrackableId = new Dictionary<TrackableId, BillboardLabel>();
    private readonly Dictionary<TrackableId, GameObject> trackedModelsByTrackableId = new Dictionary<TrackableId, GameObject>();

    private Coroutine liveCameraScanRoutine;
    private bool recognitionLocked;
    private bool runtimeImageLibraryReady;
    private ScanMode currentScanMode;
    private ScanMode scanModeBeforeRecognitionLock;
    private int cameraScanAttemptCount;
    private string lastAutoMatchedItemId;
    private float lastAutoMatchedAt;
    private bool cameraScanningPausedByUser;
    private string pendingLiveCameraMatchId;
    private int pendingLiveCameraMatchCount;
    private GameObject floatingScanModelPreview;

    private void Awake()
    {
        if (xrOrigin == null)
        {
            xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        }

        if (trackedImageManager == null && xrOrigin != null)
        {
            trackedImageManager = xrOrigin.GetComponent<ARTrackedImageManager>();
            if (trackedImageManager == null)
            {
                trackedImageManager = xrOrigin.gameObject.AddComponent<ARTrackedImageManager>();
            }
        }

        if (arCameraManager == null && xrOrigin != null && xrOrigin.Camera != null)
        {
            arCameraManager = xrOrigin.Camera.GetComponent<ARCameraManager>();
        }

        if (galleryPicker == null)
        {
            galleryPicker = Object.FindFirstObjectByType<GalleryPicker>();
        }

        if (scanUIController == null)
        {
            scanUIController = Object.FindFirstObjectByType<ScanUIController>();
        }

        if (audioManager == null)
        {
            audioManager = Object.FindFirstObjectByType<AudioManager>();
        }

        if (trackedImageManager != null)
        {
            trackedImageManager.enabled = false;
            trackedImageManager.requestedMaxNumberOfMovingImages = Mathf.Max(1, maxMovingImages);
        }

        BuildScanDatabaseLookups();
        scanUIController?.Configure(this, audioManager);
    }

    private void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        }
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
        }
    }

    private IEnumerator Start()
    {
        yield return InitializeRuntimeImageLibrary();

        if (startCameraScanningAutomatically && !cameraScanningPausedByUser)
        {
            BeginCameraScan();
        }
    }

    /// <summary>
    /// Starts the real-time camera scan flow.
    /// </summary>
    public void BeginCameraScan()
    {
        if (cameraScanningPausedByUser)
        {
            currentScanMode = ScanMode.None;
            StopLiveCameraScanLoop();
            ClearFloatingScanModelPreview();
            scanUIController?.HideCameraHint();
            scanUIController?.ShowStatusMessage(AppLanguage.Text("camera_off"));
            return;
        }

        if (scanItemsById.Count == 0)
        {
            scanUIController?.ShowStatusMessage(AppLanguage.Text("no_scan_refs"));
            return;
        }

        currentScanMode = ScanMode.Camera;
        scanModeBeforeRecognitionLock = ScanMode.None;
        recognitionLocked = false;
        cameraScanAttemptCount = 0;
        ResetPendingLiveCameraMatch();
        ClearFloatingScanModelPreview();

        StartLiveCameraScanLoop();

        string hintMessage = runtimeImageLibraryReady
            ? AppLanguage.Text("camera_hint_ready")
            : AppLanguage.Text("camera_hint_loading");

        scanUIController?.ShowCameraHint(hintMessage);
    }

    /// <summary>
    /// Starts the exact QR marker scan flow.
    /// </summary>
    public void BeginQrMarkerScan()
    {
        if (cameraScanningPausedByUser)
        {
            currentScanMode = ScanMode.None;
            StopLiveCameraScanLoop();
            ClearFloatingScanModelPreview();
            scanUIController?.HideCameraHint();
            scanUIController?.ShowStatusMessage(AppLanguage.Text("camera_off"));
            return;
        }

        if (scanItemsById.Count == 0)
        {
            scanUIController?.ShowStatusMessage(AppLanguage.Text("no_scan_refs"));
            return;
        }

        currentScanMode = ScanMode.QrMarker;
        scanModeBeforeRecognitionLock = ScanMode.None;
        recognitionLocked = false;
        cameraScanAttemptCount = 0;
        ResetPendingLiveCameraMatch();
        ClearFloatingScanModelPreview();
        StopLiveCameraScanLoop();

        string hintMessage = runtimeImageLibraryReady
            ? AppLanguage.Text("qr_hint_ready")
            : AppLanguage.Text("qr_hint_loading");

        scanUIController?.ShowQrScanOverlay(hintMessage);
    }

    public void CloseQrMarkerScan()
    {
        if (currentScanMode == ScanMode.QrMarker)
        {
            currentScanMode = ScanMode.None;
        }

        recognitionLocked = false;
        scanModeBeforeRecognitionLock = ScanMode.None;
        ResetPendingLiveCameraMatch();
        ClearFloatingScanModelPreview();
        StopLiveCameraScanLoop();
        scanUIController?.HideQrScanOverlay();

        if (cameraScanningPausedByUser)
        {
            scanUIController?.ShowStatusMessage(AppLanguage.Text("camera_off"));
            return;
        }

        if (startCameraScanningAutomatically)
        {
            BeginCameraScan();
        }
    }

    /// <summary>
    /// Starts the gallery-based scan flow.
    /// </summary>
    public void BeginGalleryScan()
    {
        if (galleryPicker == null)
        {
            scanUIController?.ShowStatusMessage(AppLanguage.Text("gallery_missing"));
            return;
        }

        currentScanMode = ScanMode.Gallery;
        scanModeBeforeRecognitionLock = ScanMode.None;
        recognitionLocked = false;
        ResetPendingLiveCameraMatch();
        ClearFloatingScanModelPreview();
        StopLiveCameraScanLoop();
        scanUIController?.ShowStatusMessage(AppLanguage.Text("select_gallery"));

        galleryPicker.PickImage(HandleGalleryImagePicked, HandleGalleryImageFailed);
    }

    /// <summary>
    /// Clears the lock after the user closes the result popup.
    /// </summary>
    public void ClearRecognitionLock()
    {
        recognitionLocked = false;
        ResetPendingLiveCameraMatch();
        ClearFloatingScanModelPreview();

        if (cameraScanningPausedByUser)
        {
            currentScanMode = ScanMode.None;
            StopLiveCameraScanLoop();
            scanUIController?.HideCameraHint();
            return;
        }

        if (scanModeBeforeRecognitionLock == ScanMode.QrMarker)
        {
            BeginQrMarkerScan();
            return;
        }

        if (resumeAutomaticScanAfterClosingResult)
        {
            BeginCameraScan();
            return;
        }

        currentScanMode = ScanMode.None;
        StopLiveCameraScanLoop();
    }

    /// <summary>
    /// Cancels whichever scan mode was active.
    /// </summary>
    public void CancelScanMode()
    {
        recognitionLocked = false;
        ResetPendingLiveCameraMatch();
        ClearFloatingScanModelPreview();

        if (cameraScanningPausedByUser)
        {
            currentScanMode = ScanMode.None;
            StopLiveCameraScanLoop();
            scanUIController?.HideCameraHint();
            return;
        }

        if (startCameraScanningAutomatically)
        {
            BeginCameraScan();
            return;
        }

        currentScanMode = ScanMode.None;
        StopLiveCameraScanLoop();
    }

    public void SetCameraScanningPausedByUser(bool paused)
    {
        if (cameraScanningPausedByUser == paused)
        {
            return;
        }

        cameraScanningPausedByUser = paused;
        recognitionLocked = false;

        if (cameraScanningPausedByUser)
        {
            currentScanMode = ScanMode.None;
            StopLiveCameraScanLoop();
            ClearFloatingScanModelPreview();
            scanUIController?.HideCameraHint();
            scanUIController?.ShowStatusMessage(AppLanguage.Text("scan_paused"));
            return;
        }

        scanUIController?.ShowStatusMessage(AppLanguage.Text("scan_resumed"));
        if (startCameraScanningAutomatically)
        {
            BeginCameraScan();
        }
    }

    private IEnumerator InitializeRuntimeImageLibrary()
    {
        if (trackedImageManager == null)
        {
            yield break;
        }

        while (ARSession.state != ARSessionState.Unsupported && ARSession.state < ARSessionState.Ready)
        {
            yield return null;
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            scanUIController?.ShowStatusMessage(AppLanguage.Text("tracking_unsupported"));
            yield break;
        }

        RuntimeReferenceImageLibrary runtimeLibrary;
        try
        {
            runtimeLibrary = trackedImageManager.CreateRuntimeLibrary();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Image tracking runtime library could not be created: " + exception.Message);
            scanUIController?.ShowStatusMessage(AppLanguage.Text("tracking_init_failed"));
            yield break;
        }

        if (!(runtimeLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary))
        {
            scanUIController?.ShowStatusMessage(AppLanguage.Text("mutable_library_unsupported"));
            yield break;
        }

        trackedImageManager.referenceLibrary = runtimeLibrary;
        trackedImageManager.requestedMaxNumberOfMovingImages = Mathf.Max(1, maxMovingImages);
        trackedImageManager.enabled = true;

        List<AddReferenceImageJobState> addReferenceJobs = new List<AddReferenceImageJobState>();
        for (int i = 0; i < scanItems.Count; i++)
        {
            ScanItemData item = scanItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            if (item.UseForTrackedImage && item.ReferenceTexture != null)
            {
                TryScheduleReferenceImage(
                    mutableLibrary,
                    addReferenceJobs,
                    item.ReferenceTexture,
                    item.Id,
                    item.PhysicalWidthMeters > 0f ? item.PhysicalWidthMeters : 0.18f,
                    item.ItemName);
            }

            if (item.QrMarkerTexture != null)
            {
                TryScheduleReferenceImage(
                    mutableLibrary,
                    addReferenceJobs,
                    item.QrMarkerTexture,
                    BuildQrReferenceId(item.Id),
                    qrMarkerPhysicalWidthMeters > 0f ? qrMarkerPhysicalWidthMeters : 0.08f,
                    item.ItemName + " QR marker");
            }
        }

        for (int i = 0; i < addReferenceJobs.Count; i++)
        {
            while (!addReferenceJobs[i].jobHandle.IsCompleted)
            {
                yield return null;
            }

            addReferenceJobs[i].jobHandle.Complete();

            if (addReferenceJobs[i].status != AddReferenceImageJobStatus.Success)
            {
                Debug.LogWarning("A scan reference finished with status: " + addReferenceJobs[i].status);
            }
        }

        runtimeImageLibraryReady = addReferenceJobs.Count > 0;

        if (!runtimeImageLibraryReady && trackedImageManager != null)
        {
            trackedImageManager.enabled = false;
        }
    }

    private void TryScheduleReferenceImage(
        MutableRuntimeReferenceImageLibrary mutableLibrary,
        List<AddReferenceImageJobState> addReferenceJobs,
        Texture2D referenceTexture,
        string referenceName,
        float physicalWidthMeters,
        string logLabel)
    {
        if (mutableLibrary == null || addReferenceJobs == null || referenceTexture == null || string.IsNullOrWhiteSpace(referenceName))
        {
            return;
        }

        try
        {
            AddReferenceImageJobState jobState = mutableLibrary.ScheduleAddImageWithValidationJob(
                referenceTexture,
                referenceName,
                Mathf.Max(0.02f, physicalWidthMeters));

            addReferenceJobs.Add(jobState);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Scan reference image could not be added: " +
                logLabel +
                ". Reason: " +
                exception.Message);
        }
    }

    private void BuildScanDatabaseLookups()
    {
        scanItemsById.Clear();
        fingerprintsById.Clear();

        int hashResolution = Mathf.Clamp(galleryHashResolution, 4, 8);
        galleryHashResolution = hashResolution;

        for (int i = 0; i < scanItems.Count; i++)
        {
            ScanItemData item = scanItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            scanItemsById[item.Id] = item;

            if (TryParseFingerprint(item, out ImageFingerprint serializedFingerprint))
            {
                fingerprintsById[item.Id] = serializedFingerprint;
                continue;
            }

            Texture2D referenceTexture = item.ReferenceTexture;
            if (referenceTexture == null && item.PreviewImage != null)
            {
                referenceTexture = item.PreviewImage.texture;
            }

            if (referenceTexture == null || !referenceTexture.isReadable)
            {
                continue;
            }

            fingerprintsById[item.Id] = CreateFingerprint(referenceTexture);
        }
    }

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        RefreshTrackedImageLabels(eventArgs.added);
        RefreshTrackedImageLabels(eventArgs.updated);
        RefreshTrackedImageModels(eventArgs.added);
        RefreshTrackedImageModels(eventArgs.updated);
        RemoveTrackedImageLabels(eventArgs.removed);
        RemoveTrackedImageModels(eventArgs.removed);

        if (currentScanMode != ScanMode.Camera || recognitionLocked)
        {
            return;
        }

        TryHandleTrackedImages(eventArgs.added);
        TryHandleTrackedImages(eventArgs.updated);
    }

    private void TryHandleTrackedImages(IReadOnlyList<ARTrackedImage> trackedImages)
    {
        for (int i = 0; i < trackedImages.Count; i++)
        {
            ARTrackedImage trackedImage = trackedImages[i];
            if (trackedImage == null || trackedImage.trackingState != TrackingState.Tracking)
            {
                continue;
            }

            string referenceId = trackedImage.referenceImage.name;
            bool requireQrMarker = currentScanMode == ScanMode.QrMarker;
            if (TryGetMatchedItemForReferenceId(referenceId, requireQrMarker, out ScanItemData matchedItem, out bool isQrMarker))
            {
                string sourceLabel = isQrMarker
                    ? AppLanguage.Text("source_qr_scan")
                    : AppLanguage.Text("source_live_scan");

                if (TryShowMatchedItem(matchedItem, sourceLabel, true, false))
                {
                    return;
                }
            }
        }
    }

    private bool TryGetMatchedItemForReferenceId(
        string referenceId,
        bool requireQrMarker,
        out ScanItemData matchedItem,
        out bool isQrMarker)
    {
        matchedItem = null;
        isQrMarker = false;

        if (string.IsNullOrWhiteSpace(referenceId))
        {
            return false;
        }

        if (referenceId.StartsWith(QrReferencePrefix, StringComparison.OrdinalIgnoreCase))
        {
            string itemId = referenceId.Substring(QrReferencePrefix.Length);
            isQrMarker = true;
            return scanItemsById.TryGetValue(itemId, out matchedItem);
        }

        if (requireQrMarker)
        {
            return false;
        }

        return scanItemsById.TryGetValue(referenceId, out matchedItem);
    }

    private static string BuildQrReferenceId(string itemId)
    {
        return QrReferencePrefix + itemId;
    }

    private void RefreshTrackedImageModels(IReadOnlyList<ARTrackedImage> trackedImages)
    {
        for (int i = 0; i < trackedImages.Count; i++)
        {
            ARTrackedImage trackedImage = trackedImages[i];
            if (trackedImage == null)
            {
                continue;
            }

            if (trackedImage.trackingState != TrackingState.Tracking)
            {
                SetTrackedModelActive(trackedImage.trackableId, false);
                continue;
            }

            string referenceId = trackedImage.referenceImage.name;
            if (!TryGetMatchedItemForReferenceId(referenceId, false, out ScanItemData matchedItem, out _))
            {
                SetTrackedModelActive(trackedImage.trackableId, false);
                continue;
            }

            GameObject modelRoot = GetOrCreateTrackedImageModel(trackedImage, matchedItem);
            if (modelRoot != null)
            {
                modelRoot.SetActive(true);
            }
        }
    }

    private void RefreshTrackedImageLabels(IReadOnlyList<ARTrackedImage> trackedImages)
    {
        for (int i = 0; i < trackedImages.Count; i++)
        {
            ARTrackedImage trackedImage = trackedImages[i];
            if (trackedImage == null)
            {
                continue;
            }

            if (trackedImage.trackingState != TrackingState.Tracking)
            {
                SetTrackedImageLabelActive(trackedImage.trackableId, false);
                continue;
            }

            string referenceId = trackedImage.referenceImage.name;
            if (!TryGetMatchedItemForReferenceId(referenceId, false, out ScanItemData matchedItem, out _))
            {
                SetTrackedImageLabelActive(trackedImage.trackableId, false);
                continue;
            }

            BillboardLabel label = GetOrCreateTrackedImageLabel(trackedImage);
            label.SetTargetCamera(xrOrigin != null && xrOrigin.Camera != null
                ? xrOrigin.Camera.transform
                : null);
            label.SetMessage(matchedItem.ItemName);

            Transform labelTransform = label.transform;
            labelTransform.localPosition = Vector3.up * trackedLabelHeight;
            labelTransform.localRotation = Quaternion.identity;
            labelTransform.localScale = Vector3.one * trackedLabelScale;
            label.gameObject.SetActive(true);
        }
    }

    private void RemoveTrackedImageLabels(IReadOnlyList<KeyValuePair<TrackableId, ARTrackedImage>> removedImages)
    {
        for (int i = 0; i < removedImages.Count; i++)
        {
            KeyValuePair<TrackableId, ARTrackedImage> removedEntry = removedImages[i];
            TrackableId trackableId = removedEntry.Key;
            ARTrackedImage trackedImage = removedEntry.Value;

            if (trackableId == TrackableId.invalidId)
            {
                continue;
            }

            if (!trackedLabelsByTrackableId.TryGetValue(trackableId, out BillboardLabel label) || label == null)
            {
                trackedLabelsByTrackableId.Remove(trackableId);
                continue;
            }

            trackedLabelsByTrackableId.Remove(trackableId);

            if (Application.isPlaying)
            {
                Destroy(label.gameObject);
            }
            else
            {
                DestroyImmediate(label.gameObject);
            }
        }
    }

    private void RemoveTrackedImageModels(IReadOnlyList<KeyValuePair<TrackableId, ARTrackedImage>> removedImages)
    {
        for (int i = 0; i < removedImages.Count; i++)
        {
            TrackableId trackableId = removedImages[i].Key;
            if (trackableId == TrackableId.invalidId)
            {
                continue;
            }

            if (!trackedModelsByTrackableId.TryGetValue(trackableId, out GameObject modelRoot) || modelRoot == null)
            {
                trackedModelsByTrackableId.Remove(trackableId);
                continue;
            }

            trackedModelsByTrackableId.Remove(trackableId);
            DestroySceneObject(modelRoot);
        }
    }

    private void HandleGalleryImagePicked(Texture2D selectedTexture, string path)
    {
        if (selectedTexture == null)
        {
            currentScanMode = ScanMode.None;
            scanUIController?.ShowStatusMessage(AppLanguage.Text("gallery_load_failed"));
            return;
        }

        ScanItemData matchedItem = FindBestGalleryMatch(selectedTexture);

        if (Application.isPlaying)
        {
            Destroy(selectedTexture);
        }
        else
        {
            DestroyImmediate(selectedTexture);
        }

        if (matchedItem == null)
        {
            currentScanMode = ScanMode.None;
            scanUIController?.ShowStatusMessage(AppLanguage.Text("no_gallery_match"));
            return;
        }

        ShowMatchedItem(matchedItem, AppLanguage.Text("source_gallery"), true);
    }

    private void HandleGalleryImageFailed(string message)
    {
        currentScanMode = ScanMode.None;
        scanUIController?.ShowStatusMessage(message);
    }

    private ScanItemData FindBestGalleryMatch(Texture2D selectedTexture)
    {
        if (selectedTexture == null || !selectedTexture.isReadable || fingerprintsById.Count == 0)
        {
            return null;
        }

        ImageFingerprint selectedFingerprint = CreateFingerprint(selectedTexture);
        return FindBestMatchForFingerprint(selectedFingerprint, maxAcceptedHashDistance, maxAcceptedAspectDifference);
    }

    private ScanItemData FindBestMatchForFingerprint(ImageFingerprint selectedFingerprint, int maxAcceptedDistance, float maxAspectDifference)
    {
        ScanItemData bestItem = null;
        int bestCombinedDistance = int.MaxValue;
        int secondBestCombinedDistance = int.MaxValue;
        float bestAspectDifference = float.MaxValue;

        foreach (KeyValuePair<string, ImageFingerprint> pair in fingerprintsById)
        {
            int averageHashDistance = CountDifferentBits(selectedFingerprint.AverageHash ^ pair.Value.AverageHash);
            int differenceHashDistance = CountDifferentBits(selectedFingerprint.DifferenceHash ^ pair.Value.DifferenceHash);
            int combinedDistance = averageHashDistance + differenceHashDistance;
            float aspectDifference = Mathf.Abs(selectedFingerprint.AspectRatio - pair.Value.AspectRatio);

            bool isBetterMatch =
                combinedDistance < bestCombinedDistance ||
                (combinedDistance == bestCombinedDistance && aspectDifference < bestAspectDifference);

            if (!isBetterMatch)
            {
                if (combinedDistance < secondBestCombinedDistance)
                {
                    secondBestCombinedDistance = combinedDistance;
                }

                continue;
            }

            if (!scanItemsById.TryGetValue(pair.Key, out ScanItemData candidateItem))
            {
                continue;
            }

            secondBestCombinedDistance = bestCombinedDistance;
            bestItem = candidateItem;
            bestCombinedDistance = combinedDistance;
            bestAspectDifference = aspectDifference;
        }

        if (bestItem == null)
        {
            return null;
        }

        if (bestCombinedDistance > maxAcceptedDistance || bestAspectDifference > maxAspectDifference)
        {
            return null;
        }

        if (secondBestCombinedDistance != int.MaxValue &&
            (secondBestCombinedDistance - bestCombinedDistance) < minimumBestMatchMargin)
        {
            return null;
        }

        return bestItem;
    }

    private bool TryShowMatchedItem(ScanItemData matchedItem, string sourceLabel, bool enforceCameraCooldown, bool showFloatingPreview)
    {
        if (matchedItem == null)
        {
            return false;
        }

        if (enforceCameraCooldown &&
            !string.IsNullOrWhiteSpace(lastAutoMatchedItemId) &&
            string.Equals(lastAutoMatchedItemId, matchedItem.Id, StringComparison.OrdinalIgnoreCase) &&
            Time.unscaledTime < lastAutoMatchedAt + Mathf.Max(1f, repeatedCameraMatchCooldownSeconds))
        {
            return false;
        }

        ShowMatchedItem(matchedItem, sourceLabel, showFloatingPreview);

        if (enforceCameraCooldown)
        {
            lastAutoMatchedItemId = matchedItem.Id;
            lastAutoMatchedAt = Time.unscaledTime;
        }

        return true;
    }

    private void ShowMatchedItem(ScanItemData matchedItem, string sourceLabel, bool showFloatingPreview)
    {
        if (matchedItem == null)
        {
            return;
        }

        scanModeBeforeRecognitionLock = currentScanMode;
        recognitionLocked = true;
        currentScanMode = ScanMode.None;
        StopLiveCameraScanLoop();

        if (showFloatingPreview)
        {
            ShowFloatingScanModelPreview(matchedItem);
        }
        else
        {
            ClearFloatingScanModelPreview();
        }

        string videoQuizMediaKey = GetVideoQuizMediaKey(matchedItem);
        scanUIController?.ShowRecognitionResult(
            matchedItem.ItemName,
            matchedItem.Description,
            matchedItem.PreviewImage,
            matchedItem.AudioClip,
            matchedItem.Description,
            sourceLabel,
            videoQuizMediaKey,
            !string.IsNullOrEmpty(videoQuizMediaKey));

        audioManager?.PlayNarration(matchedItem.AudioClip, matchedItem.Description);
    }

    private static string GetVideoQuizMediaKey(ScanItemData item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        if (VideoQuizMediaKeysByItemId.TryGetValue(item.Id, out string mediaKey) &&
            ScanUIController.IsAllowedVideoQuizImageKey(mediaKey))
        {
            return mediaKey;
        }

        return string.Empty;
    }

    private void StartLiveCameraScanLoop()
    {
        StopLiveCameraScanLoop();

        if (cameraScanningPausedByUser)
        {
            return;
        }

        if (arCameraManager == null)
        {
            return;
        }

        liveCameraScanRoutine = StartCoroutine(LiveCameraScanLoop());
    }

    private void StopLiveCameraScanLoop()
    {
        if (liveCameraScanRoutine == null)
        {
            return;
        }

        StopCoroutine(liveCameraScanRoutine);
        liveCameraScanRoutine = null;
    }

    private IEnumerator LiveCameraScanLoop()
    {
        float waitDuration = Mathf.Max(0.25f, liveCameraScanIntervalSeconds);
        WaitForSecondsRealtime waitInstruction = new WaitForSecondsRealtime(waitDuration);

        while (currentScanMode == ScanMode.Camera && !recognitionLocked)
        {
            bool matchedFromFrame = TryRecognizeCurrentCameraFrame();
            if (!matchedFromFrame)
            {
                cameraScanAttemptCount++;
                if (cameraScanAttemptCount > 0 &&
                    noMatchReminderInterval > 0 &&
                    cameraScanAttemptCount % noMatchReminderInterval == 0)
                {
                    scanUIController?.ShowCameraHint(AppLanguage.Text("still_scanning"));
                }
            }

            yield return waitInstruction;
        }

        liveCameraScanRoutine = null;
    }

    private bool TryRecognizeCurrentCameraFrame()
    {
        if (arCameraManager == null || !arCameraManager.permissionGranted || fingerprintsById.Count == 0)
        {
            return false;
        }

        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            return false;
        }

        try
        {
            Vector2Int outputDimensions = GetCpuImageOutputDimensions(cpuImage.width, cpuImage.height, Mathf.Max(96, liveCameraSampleSize));
            XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                outputDimensions = outputDimensions,
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.None
            };

            int convertedDataSize = cpuImage.GetConvertedDataSize(conversionParams);
            NativeArray<byte> convertedImageData = new NativeArray<byte>(convertedDataSize, Allocator.Temp);

            try
            {
                cpuImage.Convert(conversionParams, convertedImageData);
                ScanItemData matchedItem = FindBestLiveCameraMatch(convertedImageData, outputDimensions.x, outputDimensions.y);
                if (matchedItem == null)
                {
                    ResetPendingLiveCameraMatch();
                    return false;
                }

                if (!RegisterStableLiveCameraMatch(matchedItem))
                {
                    return false;
                }

                return TryShowMatchedItem(matchedItem, AppLanguage.Text("source_live_frame"), true, true);
            }
            finally
            {
                if (convertedImageData.IsCreated)
                {
                    convertedImageData.Dispose();
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Live camera fallback scan failed: " + exception.Message);
            return false;
        }
        finally
        {
            cpuImage.Dispose();
        }
    }

    private ScanItemData FindBestLiveCameraMatch(NativeArray<byte> rgba32Data, int width, int height)
    {
        ScanItemData bestItem = null;
        int bestScore = int.MaxValue;
        int secondBestScore = int.MaxValue;

        foreach (KeyValuePair<string, ImageFingerprint> pair in fingerprintsById)
        {
            if (!scanItemsById.TryGetValue(pair.Key, out ScanItemData candidate))
            {
                continue;
            }

            if (liveCameraFallbackMatchesTrackedImagesOnly && !candidate.UseForTrackedImage)
            {
                continue;
            }

            int candidateScore = GetBestLiveCameraScoreForCandidate(rgba32Data, width, height, pair.Value);
            if (candidateScore < bestScore)
            {
                secondBestScore = bestScore;
                bestScore = candidateScore;
                bestItem = candidate;
            }
            else if (candidateScore < secondBestScore)
            {
                secondBestScore = candidateScore;
            }
        }

        if (bestItem == null || bestScore > maxAcceptedLiveCameraHashDistance)
        {
            return null;
        }

        if (secondBestScore != int.MaxValue && (secondBestScore - bestScore) < minimumBestMatchMargin)
        {
            return null;
        }

        return bestItem;
    }

    private bool RegisterStableLiveCameraMatch(ScanItemData matchedItem)
    {
        if (matchedItem == null || string.IsNullOrWhiteSpace(matchedItem.Id))
        {
            ResetPendingLiveCameraMatch();
            return false;
        }

        if (string.Equals(pendingLiveCameraMatchId, matchedItem.Id, StringComparison.OrdinalIgnoreCase))
        {
            pendingLiveCameraMatchCount++;
        }
        else
        {
            pendingLiveCameraMatchId = matchedItem.Id;
            pendingLiveCameraMatchCount = 1;
        }

        return pendingLiveCameraMatchCount >= Mathf.Max(1, minimumLiveCameraStableMatches);
    }

    private void ResetPendingLiveCameraMatch()
    {
        pendingLiveCameraMatchId = null;
        pendingLiveCameraMatchCount = 0;
    }

    private int GetBestLiveCameraScoreForCandidate(NativeArray<byte> rgba32Data, int width, int height, ImageFingerprint candidateFingerprint)
    {
        int bestScore = int.MaxValue;
        float candidateAspect = Mathf.Max(0.05f, candidateFingerprint.AspectRatio);

        for (int cropIndex = 0; cropIndex < LiveScanCropPresets.Length; cropIndex++)
        {
            Rect cropRect = FitRectToAspect(LiveScanCropPresets[cropIndex], candidateAspect);

            for (int quarterTurns = 0; quarterTurns < 4; quarterTurns++)
            {
                ImageFingerprint liveFingerprint = CreateFingerprintFromRgba32(
                    rgba32Data,
                    width,
                    height,
                    cropRect,
                    quarterTurns);

                int score =
                    CountDifferentBits(liveFingerprint.AverageHash ^ candidateFingerprint.AverageHash) +
                    CountDifferentBits(liveFingerprint.DifferenceHash ^ candidateFingerprint.DifferenceHash);

                if (score < bestScore)
                {
                    bestScore = score;
                }
            }
        }

        return bestScore;
    }

    private ImageFingerprint CreateFingerprint(Texture2D texture)
    {
        if (texture == null)
        {
            return default;
        }

        int resolution = Mathf.Clamp(galleryHashResolution, 4, 8);
        float[] brightnessSamples = new float[resolution * resolution];
        float brightnessSum = 0f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float sampleX = (x + 0.5f) / resolution;
                float sampleY = (y + 0.5f) / resolution;

                Color pixel = texture.GetPixelBilinear(sampleX, sampleY);
                float brightness = (pixel.r * 0.299f) + (pixel.g * 0.587f) + (pixel.b * 0.114f);

                int index = y * resolution + x;
                brightnessSamples[index] = brightness;
                brightnessSum += brightness;
            }
        }

        float averageBrightness = brightnessSum / brightnessSamples.Length;
        ulong hash = 0UL;

        for (int i = 0; i < brightnessSamples.Length; i++)
        {
            if (brightnessSamples[i] >= averageBrightness)
            {
                hash |= 1UL << i;
            }
        }

        ulong differenceHash = 0UL;
        int differenceHashBitIndex = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float leftSampleX = (x + 0.5f) / (resolution + 1f);
                float rightSampleX = (x + 1.5f) / (resolution + 1f);
                float sampleY = (y + 0.5f) / resolution;

                float leftBrightness = GetTextureBrightness(texture, leftSampleX, sampleY);
                float rightBrightness = GetTextureBrightness(texture, rightSampleX, sampleY);
                if (leftBrightness >= rightBrightness)
                {
                    differenceHash |= 1UL << differenceHashBitIndex;
                }

                differenceHashBitIndex++;
            }
        }

        return new ImageFingerprint
        {
            AverageHash = hash,
            DifferenceHash = differenceHash,
            AspectRatio = texture.height <= 0 ? 1f : texture.width / (float)texture.height
        };
    }

    private ImageFingerprint CreateFingerprintFromRgba32(
        NativeArray<byte> rgba32Data,
        int width,
        int height,
        Rect normalizedCrop,
        int quarterTurns)
    {
        int resolution = Mathf.Clamp(galleryHashResolution, 4, 8);
        float[] brightnessSamples = new float[resolution * resolution];
        float brightnessSum = 0f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float sampleX = (x + 0.5f) / resolution;
                float sampleY = (y + 0.5f) / resolution;
                float brightness = GetRgba32Brightness(rgba32Data, width, height, normalizedCrop, quarterTurns, sampleX, sampleY);

                int index = y * resolution + x;
                brightnessSamples[index] = brightness;
                brightnessSum += brightness;
            }
        }

        float averageBrightness = brightnessSamples.Length > 0
            ? brightnessSum / brightnessSamples.Length
            : 0f;

        ulong averageHash = 0UL;
        for (int i = 0; i < brightnessSamples.Length; i++)
        {
            if (brightnessSamples[i] >= averageBrightness)
            {
                averageHash |= 1UL << i;
            }
        }

        ulong differenceHash = 0UL;
        int differenceHashBitIndex = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float leftSampleX = (x + 0.5f) / (resolution + 1f);
                float rightSampleX = (x + 1.5f) / (resolution + 1f);
                float sampleY = (y + 0.5f) / resolution;

                float leftBrightness = GetRgba32Brightness(rgba32Data, width, height, normalizedCrop, quarterTurns, leftSampleX, sampleY);
                float rightBrightness = GetRgba32Brightness(rgba32Data, width, height, normalizedCrop, quarterTurns, rightSampleX, sampleY);

                if (leftBrightness >= rightBrightness)
                {
                    differenceHash |= 1UL << differenceHashBitIndex;
                }

                differenceHashBitIndex++;
            }
        }

        float cropAspect = normalizedCrop.height <= 0f
            ? 1f
            : (normalizedCrop.width * width) / (normalizedCrop.height * height);

        if (quarterTurns % 2 != 0 && cropAspect > 0f)
        {
            cropAspect = 1f / cropAspect;
        }

        return new ImageFingerprint
        {
            AverageHash = averageHash,
            DifferenceHash = differenceHash,
            AspectRatio = cropAspect
        };
    }

    private bool TryParseFingerprint(ScanItemData item, out ImageFingerprint fingerprint)
    {
        fingerprint = default;
        if (item == null)
        {
            return false;
        }

        if (!TryParseStoredHash(item.AverageHashHex, out ulong averageHash))
        {
            return false;
        }

        if (!TryParseStoredHash(item.DifferenceHashHex, out ulong differenceHash))
        {
            return false;
        }

        if (item.AspectRatio <= 0f)
        {
            return false;
        }

        fingerprint = new ImageFingerprint
        {
            AverageHash = averageHash,
            DifferenceHash = differenceHash,
            AspectRatio = item.AspectRatio
        };

        return true;
    }

    private bool TryParseStoredHash(string hashHex, out ulong parsedHash)
    {
        if (string.IsNullOrWhiteSpace(hashHex))
        {
            parsedHash = 0UL;
            return false;
        }

        return ulong.TryParse(hashHex, System.Globalization.NumberStyles.HexNumber, null, out parsedHash);
    }

    private float GetTextureBrightness(Texture2D texture, float normalizedX, float normalizedY)
    {
        Color pixel = texture.GetPixelBilinear(normalizedX, normalizedY);
        return (pixel.r * 0.299f) + (pixel.g * 0.587f) + (pixel.b * 0.114f);
    }

    private float GetRgba32Brightness(
        NativeArray<byte> rgba32Data,
        int width,
        int height,
        Rect normalizedCrop,
        int quarterTurns,
        float normalizedX,
        float normalizedY)
    {
        ApplyQuarterTurn(ref normalizedX, ref normalizedY, quarterTurns);

        float cropX = normalizedCrop.x + (normalizedX * normalizedCrop.width);
        float cropY = normalizedCrop.y + (normalizedY * normalizedCrop.height);

        int pixelX = Mathf.Clamp(Mathf.RoundToInt(cropX * Mathf.Max(0, width - 1)), 0, Mathf.Max(0, width - 1));
        int pixelY = Mathf.Clamp(Mathf.RoundToInt(cropY * Mathf.Max(0, height - 1)), 0, Mathf.Max(0, height - 1));
        int pixelIndex = ((pixelY * width) + pixelX) * 4;

        if (pixelIndex < 0 || pixelIndex + 2 >= rgba32Data.Length)
        {
            return 0f;
        }

        float red = rgba32Data[pixelIndex] / 255f;
        float green = rgba32Data[pixelIndex + 1] / 255f;
        float blue = rgba32Data[pixelIndex + 2] / 255f;
        return (red * 0.299f) + (green * 0.587f) + (blue * 0.114f);
    }

    private void ApplyQuarterTurn(ref float normalizedX, ref float normalizedY, int quarterTurns)
    {
        quarterTurns %= 4;
        if (quarterTurns < 0)
        {
            quarterTurns += 4;
        }

        float sourceX = normalizedX;
        float sourceY = normalizedY;

        switch (quarterTurns)
        {
            case 1:
                normalizedX = sourceY;
                normalizedY = 1f - sourceX;
                break;
            case 2:
                normalizedX = 1f - sourceX;
                normalizedY = 1f - sourceY;
                break;
            case 3:
                normalizedX = 1f - sourceY;
                normalizedY = sourceX;
                break;
        }
    }

    private Vector2Int GetCpuImageOutputDimensions(int sourceWidth, int sourceHeight, int maxDimension)
    {
        int longestSide = Mathf.Max(sourceWidth, sourceHeight);
        if (longestSide <= 0)
        {
            return new Vector2Int(128, 128);
        }

        float scale = Mathf.Min(1f, maxDimension / (float)longestSide);
        int outputWidth = Mathf.Max(64, Mathf.RoundToInt(sourceWidth * scale));
        int outputHeight = Mathf.Max(64, Mathf.RoundToInt(sourceHeight * scale));
        return new Vector2Int(outputWidth, outputHeight);
    }

    private Rect FitRectToAspect(Rect sourceRect, float targetAspectRatio)
    {
        if (sourceRect.width <= 0f || sourceRect.height <= 0f)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        float safeAspectRatio = Mathf.Max(0.05f, targetAspectRatio);
        float sourceAspectRatio = sourceRect.width / sourceRect.height;

        if (sourceAspectRatio > safeAspectRatio)
        {
            float croppedWidth = sourceRect.height * safeAspectRatio;
            float xOffset = (sourceRect.width - croppedWidth) * 0.5f;
            return new Rect(sourceRect.x + xOffset, sourceRect.y, croppedWidth, sourceRect.height);
        }

        float croppedHeight = sourceRect.width / safeAspectRatio;
        float yOffset = (sourceRect.height - croppedHeight) * 0.5f;
        return new Rect(sourceRect.x, sourceRect.y + yOffset, sourceRect.width, croppedHeight);
    }

    private int CountDifferentBits(ulong value)
    {
        int differentBits = 0;
        while (value != 0UL)
        {
            value &= value - 1UL;
            differentBits++;
        }

        return differentBits;
    }

    private GameObject GetOrCreateTrackedImageModel(ARTrackedImage trackedImage, ScanItemData matchedItem)
    {
        if (trackedImage == null || matchedItem == null)
        {
            return null;
        }

        if (trackedModelsByTrackableId.TryGetValue(trackedImage.trackableId, out GameObject existingModel) &&
            existingModel != null)
        {
            return existingModel;
        }

        GameObject modelRoot = BuildScan3DModel(matchedItem, trackedImage.size, false);
        if (modelRoot == null)
        {
            return null;
        }

        modelRoot.transform.SetParent(trackedImage.transform, false);
        modelRoot.transform.localPosition = Vector3.zero;
        modelRoot.transform.localRotation = Quaternion.identity;
        ConfigureScanModelMotion(modelRoot, false);
        trackedModelsByTrackableId[trackedImage.trackableId] = modelRoot;
        return modelRoot;
    }

    private GameObject BuildScan3DModel(ScanItemData item, Vector2 trackedSize, bool floatingPreview)
    {
        if (item == null)
        {
            return null;
        }

        float itemScale = Mathf.Clamp(item.ModelScale <= 0f ? 1f : item.ModelScale, 0.2f, 4f);
        if (item.ModelPrefab != null)
        {
            GameObject prefabInstance = Instantiate(item.ModelPrefab);
            prefabInstance.name = "Scan3DModel_" + item.Id;
            prefabInstance.transform.localScale = Vector3.one * itemScale;
            return prefabInstance;
        }

        if (!item.ShowGeneratedReliefModel)
        {
            return null;
        }

        Texture2D sourceTexture = item.ReferenceTexture;
        if (sourceTexture == null && item.PreviewImage != null)
        {
            sourceTexture = item.PreviewImage.texture;
        }

        float aspect = item.AspectRatio > 0.05f
            ? item.AspectRatio
            : sourceTexture != null && sourceTexture.height > 0
                ? sourceTexture.width / (float)sourceTexture.height
                : 1f;

        float defaultWidth = item.PhysicalWidthMeters > 0f
            ? item.PhysicalWidthMeters * (floatingPreview ? 0.92f : 0.9f)
            : 0.18f;
        float trackedWidth = trackedSize.x > 0.02f
            ? trackedSize.x * (floatingPreview ? 0.92f : 0.95f)
            : defaultWidth;
        float minWidth = floatingPreview ? 0.18f : 0.14f;
        float maxWidth = floatingPreview ? 0.28f : 0.36f;
        float modelWidth = Mathf.Clamp(Mathf.Max(defaultWidth, trackedWidth), minWidth, maxWidth);

        float modelHeight = Mathf.Clamp(modelWidth / Mathf.Max(0.05f, aspect), 0.08f, floatingPreview ? 0.2f : 0.26f);
        if (aspect > 1.35f)
        {
            modelHeight = Mathf.Max(modelHeight, modelWidth * 0.5f);
        }

        GameObject root = new GameObject("Scan3DModel_" + item.Id);
        root.transform.localScale = Vector3.one * itemScale;

        Color tint = item.ModelTint;
        if (tint.a <= 0.01f)
        {
            tint = new Color(0.32f, 0.68f, 1f, 1f);
        }

        Material baseMaterial = CreateRuntimeMaterial(new Color(0.055f, 0.07f, 0.095f, 1f), null, false);
        Material platformMaterial = CreateRuntimeMaterial(new Color(0.74f, 0.61f, 0.43f, 1f), null, false);
        Material shadowMaterial = CreateRuntimeMaterial(new Color(0f, 0f, 0f, 0.36f), null, false);
        Material frameMaterial = CreateRuntimeMaterial(tint, null, false);
        Material imageMaterial = CreateRuntimeMaterial(Color.white, sourceTexture, true);

        float smallestSide = Mathf.Min(modelWidth, modelHeight);
        float hoverLift = floatingPreview ? 0.045f : 0.055f;
        float platformThickness = Mathf.Clamp(smallestSide * 0.1f, 0.014f, 0.03f);
        float baseThickness = Mathf.Clamp(smallestSide * 0.06f, 0.008f, 0.02f);
        float railThickness = Mathf.Clamp(smallestSide * 0.055f, 0.007f, 0.02f);
        float reliefHeight = Mathf.Clamp(smallestSide * 0.34f, 0.026f, 0.09f);
        float platformWidth = modelWidth + (railThickness * 8f);
        float platformDepth = Mathf.Max(modelHeight + (railThickness * 7f), modelWidth * 0.62f);

        GameObject shadowDisc = CreatePrimitiveChild(root.transform, "3D_FloatingShadow", PrimitiveType.Cylinder, shadowMaterial);
        shadowDisc.transform.localPosition = new Vector3(0f, hoverLift * 0.18f, 0f);
        shadowDisc.transform.localScale = new Vector3(platformWidth * 0.92f, 0.0015f, platformDepth * 0.92f);

        GameObject platform = CreatePrimitiveChild(root.transform, "3D_FloatingPlatform", PrimitiveType.Cylinder, platformMaterial);
        platform.transform.localPosition = new Vector3(0f, hoverLift + (platformThickness * 0.5f), 0f);
        platform.transform.localScale = new Vector3(platformWidth, platformThickness * 0.5f, platformDepth);

        GameObject rim = CreatePrimitiveChild(root.transform, "3D_PlatformRim", PrimitiveType.Cylinder, frameMaterial);
        rim.transform.localPosition = new Vector3(0f, hoverLift + platformThickness + 0.002f, 0f);
        rim.transform.localScale = new Vector3(platformWidth * 1.03f, 0.0025f, platformDepth * 1.03f);

        float deckY = hoverLift + platformThickness;

        GameObject basePlate = CreatePrimitiveChild(root.transform, "3D_BasePlate", PrimitiveType.Cube, baseMaterial);
        basePlate.transform.localPosition = new Vector3(0f, deckY + (baseThickness * 0.5f), 0f);
        basePlate.transform.localScale = new Vector3(modelWidth + railThickness * 3f, baseThickness, modelHeight + railThickness * 3f);

        CreateFrameRail(root.transform, "3D_FrameTop", frameMaterial, 0f, modelHeight * 0.5f + railThickness, modelWidth + railThickness * 3f, railThickness, deckY, baseThickness, reliefHeight);
        CreateFrameRail(root.transform, "3D_FrameBottom", frameMaterial, 0f, -modelHeight * 0.5f - railThickness, modelWidth + railThickness * 3f, railThickness, deckY, baseThickness, reliefHeight);
        CreateFrameRail(root.transform, "3D_FrameLeft", frameMaterial, -modelWidth * 0.5f - railThickness, 0f, railThickness, modelHeight, deckY, baseThickness, reliefHeight);
        CreateFrameRail(root.transform, "3D_FrameRight", frameMaterial, modelWidth * 0.5f + railThickness, 0f, railThickness, modelHeight, deckY, baseThickness, reliefHeight);

        GameObject relief = new GameObject("3D_ImageRelief", typeof(MeshFilter), typeof(MeshRenderer));
        relief.transform.SetParent(root.transform, false);
        relief.transform.localPosition = Vector3.up * (deckY + baseThickness);
        Texture2D meshTexture = GetReadableTextureForMesh(sourceTexture);
        MeshFilter meshFilter = relief.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateImageReliefMesh(meshTexture, modelWidth, modelHeight, reliefHeight);
        if (meshTexture != null && meshTexture != sourceTexture)
        {
            Destroy(meshTexture);
        }

        MeshRenderer meshRenderer = relief.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = imageMaterial;

        GameObject glowCore = CreatePrimitiveChild(root.transform, "3D_CenterHighlight", PrimitiveType.Sphere, frameMaterial);
        glowCore.transform.localPosition = new Vector3(0f, deckY + baseThickness + reliefHeight + 0.012f, 0f);
        float glowScale = Mathf.Clamp(smallestSide * 0.13f, 0.016f, 0.046f);
        glowCore.transform.localScale = new Vector3(glowScale, glowScale * 0.45f, glowScale);

        float surfaceTopY = deckY + baseThickness + reliefHeight;
        CreateFloatingPhotoCard(root.transform, sourceTexture, imageMaterial, frameMaterial, modelWidth, modelHeight, aspect, surfaceTopY, floatingPreview);
        CreateScanTitleBadge(root.transform, item.ItemName, modelWidth, modelHeight, surfaceTopY, floatingPreview);
        CreateAccentMarkers(root.transform, frameMaterial, platformWidth, platformDepth, deckY + platformThickness * 0.35f);

        return root;
    }

    private Texture2D GetReadableTextureForMesh(Texture2D sourceTexture)
    {
        if (sourceTexture == null || sourceTexture.isReadable)
        {
            return sourceTexture;
        }

        int sourceWidth = Mathf.Max(1, sourceTexture.width);
        int sourceHeight = Mathf.Max(1, sourceTexture.height);
        float scale = Mathf.Min(1f, 256f / Mathf.Max(sourceWidth, sourceHeight));
        int copyWidth = Mathf.Max(32, Mathf.RoundToInt(sourceWidth * scale));
        int copyHeight = Mathf.Max(32, Mathf.RoundToInt(sourceHeight * scale));

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture readableTarget = RenderTexture.GetTemporary(copyWidth, copyHeight, 0, RenderTextureFormat.ARGB32);
        Texture2D readableCopy = null;

        try
        {
            Graphics.Blit(sourceTexture, readableTarget);
            RenderTexture.active = readableTarget;
            readableCopy = new Texture2D(copyWidth, copyHeight, TextureFormat.RGBA32, false);
            readableCopy.ReadPixels(new Rect(0f, 0f, copyWidth, copyHeight), 0, 0);
            readableCopy.Apply(false, false);
            return readableCopy;
        }
        catch (Exception)
        {
            if (readableCopy != null)
            {
                Destroy(readableCopy);
            }

            return null;
        }
        finally
        {
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(readableTarget);
        }
    }

    private Mesh CreateImageReliefMesh(Texture2D texture, float width, float height, float maxHeight)
    {
        const int grid = 18;
        Vector3[] vertices = new Vector3[(grid + 1) * (grid + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[grid * grid * 6];
        bool canSampleTexture = texture != null && texture.isReadable;

        for (int y = 0; y <= grid; y++)
        {
            for (int x = 0; x <= grid; x++)
            {
                float u = x / (float)grid;
                float v = y / (float)grid;
                float brightness = canSampleTexture
                    ? GetTextureBrightness(texture, u, v)
                    : 0.5f + (Mathf.Sin((u * 12f) + (v * 7f)) * 0.5f);

                float edgeFade = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 8f);
                float raisedHeight = Mathf.Lerp(maxHeight * 0.18f, maxHeight, Mathf.Clamp01(brightness)) * edgeFade;
                int index = (y * (grid + 1)) + x;
                vertices[index] = new Vector3((u - 0.5f) * width, raisedHeight, (v - 0.5f) * height);
                uvs[index] = new Vector2(u, v);
            }
        }

        int triangleIndex = 0;
        for (int y = 0; y < grid; y++)
        {
            for (int x = 0; x < grid; x++)
            {
                int bottomLeft = (y * (grid + 1)) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + grid + 1;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "GeneratedScanImageRelief"
        };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateFrameRail(
        Transform parent,
        string name,
        Material material,
        float localX,
        float localZ,
        float width,
        float depth,
        float deckY,
        float baseThickness,
        float reliefHeight)
    {
        GameObject rail = CreatePrimitiveChild(parent, name, PrimitiveType.Cube, material);
        rail.transform.localPosition = new Vector3(localX, deckY + baseThickness + (reliefHeight * 0.34f), localZ);
        rail.transform.localScale = new Vector3(width, reliefHeight * 0.68f, depth);
    }

    private void CreateFloatingPhotoCard(
        Transform parent,
        Texture sourceTexture,
        Material imageMaterial,
        Material frameMaterial,
        float modelWidth,
        float modelHeight,
        float aspect,
        float surfaceTopY,
        bool floatingPreview)
    {
        float cardMaxWidth = modelWidth * 0.76f;
        float cardMaxHeight = floatingPreview ? 0.15f : 0.2f;
        float cardWidth = cardMaxWidth;
        float cardHeight = cardWidth / Mathf.Max(0.05f, aspect);
        if (cardHeight > cardMaxHeight)
        {
            cardHeight = cardMaxHeight;
            cardWidth = Mathf.Min(cardMaxWidth, cardHeight * Mathf.Max(0.05f, aspect));
        }

        cardWidth = Mathf.Clamp(cardWidth, 0.1f, cardMaxWidth);
        cardHeight = Mathf.Clamp(cardHeight, 0.085f, cardMaxHeight);

        GameObject cardRoot = new GameObject("3D_FloatingPhotoCard");
        cardRoot.transform.SetParent(parent, false);
        cardRoot.transform.localPosition = new Vector3(0f, surfaceTopY + cardHeight * 0.62f + 0.052f, modelHeight * 0.28f);
        cardRoot.transform.localRotation = Quaternion.identity;

        Scan3DWorldBillboard billboard = cardRoot.AddComponent<Scan3DWorldBillboard>();
        billboard.Configure(GetArCameraTransform(), false);

        GameObject backing = CreateDoubleSidedCard(cardRoot.transform, "3D_PhotoCardGlow", frameMaterial, cardWidth + 0.026f, cardHeight + 0.026f);
        backing.transform.localPosition = new Vector3(0f, 0f, 0.006f);

        GameObject photo = CreateDoubleSidedCard(cardRoot.transform, "3D_PhotoCardImage", imageMaterial, cardWidth, cardHeight);
        photo.transform.localPosition = new Vector3(0f, 0f, -0.006f);

        if (sourceTexture == null)
        {
            GameObject placeholder = CreatePrimitiveChild(cardRoot.transform, "3D_PhotoPlaceholder", PrimitiveType.Cube, frameMaterial);
            placeholder.transform.localPosition = Vector3.zero;
            placeholder.transform.localScale = new Vector3(cardWidth * 0.75f, cardHeight * 0.08f, 0.006f);
        }
    }

    private void CreateScanTitleBadge(
        Transform parent,
        string title,
        float modelWidth,
        float modelHeight,
        float surfaceTopY,
        bool floatingPreview)
    {
        GameObject labelObject = new GameObject("3D_TitleBadge", typeof(TextMesh));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, surfaceTopY + (floatingPreview ? 0.16f : 0.2f), -modelHeight * 0.42f);
        labelObject.transform.localRotation = Quaternion.identity;

        TextMesh label = labelObject.GetComponent<TextMesh>();
        label.text = BuildShort3DTitle(title);
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = Mathf.Clamp(modelWidth * 0.032f, 0.005f, 0.01f);
        label.fontSize = 72;
        label.lineSpacing = 0.82f;
        label.color = Color.white;

        Scan3DWorldBillboard billboard = labelObject.AddComponent<Scan3DWorldBillboard>();
        billboard.Configure(GetArCameraTransform(), false);
    }

    private void CreateAccentMarkers(Transform parent, Material material, float platformWidth, float platformDepth, float y)
    {
        Vector3[] positions =
        {
            new Vector3(-platformWidth * 0.34f, y, -platformDepth * 0.24f),
            new Vector3(platformWidth * 0.36f, y, -platformDepth * 0.18f),
            new Vector3(-platformWidth * 0.28f, y, platformDepth * 0.26f),
            new Vector3(platformWidth * 0.31f, y, platformDepth * 0.23f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject marker = CreatePrimitiveChild(parent, "3D_AccentMarker_" + (i + 1), PrimitiveType.Sphere, material);
            marker.transform.localPosition = positions[i];
            float scale = Mathf.Clamp(Mathf.Min(platformWidth, platformDepth) * 0.045f, 0.012f, 0.028f);
            marker.transform.localScale = new Vector3(scale, scale * 0.55f, scale);
        }
    }

    private GameObject CreateDoubleSidedCard(Transform parent, string name, Material material, float width, float height)
    {
        GameObject card = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        card.transform.SetParent(parent, false);

        MeshFilter meshFilter = card.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateDoubleSidedQuadMesh(width, height);

        MeshRenderer meshRenderer = card.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        return card;
    }

    private Mesh CreateDoubleSidedQuadMesh(float width, float height)
    {
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        Mesh mesh = new Mesh
        {
            name = "GeneratedDoubleSidedScanCard"
        };

        mesh.vertices = new[]
        {
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[]
        {
            0, 2, 1,
            2, 3, 1,
            1, 2, 0,
            1, 3, 2
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private string BuildShort3DTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "3D VIEW";
        }

        string cleanTitle = title.Trim();
        cleanTitle = cleanTitle.Replace("Aguinaldo Shrine Archive Photo", "Archive Photo");
        if (cleanTitle.Length > 38)
        {
            cleanTitle = cleanTitle.Substring(0, 35).TrimEnd() + "...";
        }

        int splitIndex = cleanTitle.LastIndexOf(' ', Mathf.Min(18, cleanTitle.Length - 1));
        if (splitIndex > 5 && cleanTitle.Length > 22)
        {
            cleanTitle = cleanTitle.Substring(0, splitIndex) + "\n" + cleanTitle.Substring(splitIndex + 1);
        }

        return cleanTitle;
    }

    private Transform GetArCameraTransform()
    {
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            return xrOrigin.Camera.transform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType primitiveType, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);

        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyComponent(collider);
        }

        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return primitive;
    }

    private Material CreateRuntimeMaterial(Color color, Texture texture, bool useTextureShader)
    {
        Shader shader = Shader.Find(useTextureShader && texture != null ? "Unlit/Texture" : "Unlit/Color");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            color = color
        };

        if (texture != null)
        {
            material.mainTexture = texture;
        }

        return material;
    }

    private void ShowFloatingScanModelPreview(ScanItemData matchedItem)
    {
        ClearFloatingScanModelPreview();

        Transform cameraTransform = xrOrigin != null && xrOrigin.Camera != null
            ? xrOrigin.Camera.transform
            : null;

        if (cameraTransform == null)
        {
            return;
        }

        floatingScanModelPreview = BuildScan3DModel(matchedItem, new Vector2(0.2f, 0.14f), true);
        if (floatingScanModelPreview == null)
        {
            return;
        }

        floatingScanModelPreview.transform.SetParent(cameraTransform, false);
        floatingScanModelPreview.transform.localPosition = new Vector3(0f, -0.12f, 0.96f);
        floatingScanModelPreview.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
        ConfigureScanModelMotion(floatingScanModelPreview, true);
    }

    private void ConfigureScanModelMotion(GameObject modelRoot, bool floatingPreview)
    {
        if (modelRoot == null)
        {
            return;
        }

        Scan3DFloatingExhibit motion = modelRoot.GetComponent<Scan3DFloatingExhibit>();
        if (motion == null)
        {
            motion = modelRoot.AddComponent<Scan3DFloatingExhibit>();
        }

        if (floatingPreview)
        {
            motion.Configure(0.016f, 1.25f, 4.5f, 3.5f);
        }
        else
        {
            motion.Configure(0.011f, 1.05f, 3.2f, 1.25f);
        }
    }

    private void ClearFloatingScanModelPreview()
    {
        if (floatingScanModelPreview == null)
        {
            return;
        }

        DestroySceneObject(floatingScanModelPreview);
        floatingScanModelPreview = null;
    }

    private void SetTrackedModelActive(TrackableId trackableId, bool isActive)
    {
        if (trackedModelsByTrackableId.TryGetValue(trackableId, out GameObject modelRoot) &&
            modelRoot != null)
        {
            modelRoot.SetActive(isActive);
        }
    }

    private void DestroyComponent(Component component)
    {
        if (component == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(component);
        }
        else
        {
            DestroyImmediate(component);
        }
    }

    private void DestroySceneObject(Object sceneObject)
    {
        if (sceneObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(sceneObject);
        }
        else
        {
            DestroyImmediate(sceneObject);
        }
    }

    private BillboardLabel GetOrCreateTrackedImageLabel(ARTrackedImage trackedImage)
    {
        if (trackedLabelsByTrackableId.TryGetValue(trackedImage.trackableId, out BillboardLabel existingLabel) &&
            existingLabel != null)
        {
            return existingLabel;
        }

        GameObject labelObject = new GameObject("TrackedItemLabel", typeof(TextMesh), typeof(BillboardLabel));
        labelObject.transform.SetParent(trackedImage.transform, false);

        TextMesh textMesh = labelObject.GetComponent<TextMesh>();
        textMesh.text = string.Empty;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.05f;
        textMesh.fontSize = trackedLabelFontSize;
        textMesh.lineSpacing = 0.85f;
        textMesh.color = Color.white;

        BillboardLabel billboardLabel = labelObject.GetComponent<BillboardLabel>();
        billboardLabel.SetTargetCamera(xrOrigin != null && xrOrigin.Camera != null
            ? xrOrigin.Camera.transform
            : null);

        trackedLabelsByTrackableId[trackedImage.trackableId] = billboardLabel;
        return billboardLabel;
    }

    private void SetTrackedImageLabelActive(TrackableId trackableId, bool isActive)
    {
        if (trackedLabelsByTrackableId.TryGetValue(trackableId, out BillboardLabel label) &&
            label != null)
        {
            label.gameObject.SetActive(isActive);
        }
    }
}
