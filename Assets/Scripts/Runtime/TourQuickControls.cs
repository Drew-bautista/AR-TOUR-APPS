using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Handles the compact top-bar controls for camera visibility/scanning and audio mute.
/// </summary>
public class TourQuickControls : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button cameraToggleButton;
    [SerializeField] private Button muteToggleButton;
    [SerializeField] private Image cameraButtonBackground;
    [SerializeField] private Image muteButtonBackground;
    [SerializeField] private GameObject cameraOffSlash;
    [SerializeField] private GameObject muteSlash;

    [Header("Scene References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private ARCameraBackground arCameraBackground;
    [SerializeField] private ImageRecognitionManager imageRecognitionManager;
    [SerializeField] private AudioManager scanAudioManager;
    [SerializeField] private Text statusText;

    [Header("Visual State")]
    [SerializeField] private Color enabledColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color disabledColor = new Color(0.88f, 0.14f, 0.18f, 0.92f);
    [SerializeField] private Color pressedColor = new Color(1f, 1f, 1f, 0.28f);

    private readonly Dictionary<AudioSource, bool> previousMuteStates = new Dictionary<AudioSource, bool>();
    private Coroutine cameraPulseRoutine;
    private Coroutine mutePulseRoutine;
    private bool cameraOff;
    private bool muted;
    private bool hasStoredAudioState;
    private float previousListenerVolume = 1f;
    private bool previousListenerPause;

    public void Configure(
        Button cameraButton,
        Button audioButton,
        Image cameraBackground,
        Image audioBackground,
        GameObject cameraSlash,
        GameObject audioSlash,
        Camera targetCamera,
        ARCameraManager targetCameraManager,
        ARCameraBackground targetCameraBackground,
        ImageRecognitionManager recognitionManager,
        AudioManager audioManager,
        Text topStatusText)
    {
        cameraToggleButton = cameraButton;
        muteToggleButton = audioButton;
        cameraButtonBackground = cameraBackground;
        muteButtonBackground = audioBackground;
        cameraOffSlash = cameraSlash;
        muteSlash = audioSlash;
        arCamera = targetCamera;
        arCameraManager = targetCameraManager;
        arCameraBackground = targetCameraBackground;
        imageRecognitionManager = recognitionManager;
        scanAudioManager = audioManager;
        statusText = topStatusText;
    }

    private void Awake()
    {
        ResolveMissingReferences();
        BindButtons();
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        if (cameraToggleButton != null)
        {
            cameraToggleButton.onClick.RemoveListener(ToggleCamera);
        }

        if (muteToggleButton != null)
        {
            muteToggleButton.onClick.RemoveListener(ToggleMute);
        }

        if (muted)
        {
            ApplyMuted(false);
        }
    }

    private void ResolveMissingReferences()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (arCameraManager == null && arCamera != null)
        {
            arCameraManager = arCamera.GetComponent<ARCameraManager>();
        }

        if (arCameraBackground == null && arCamera != null)
        {
            arCameraBackground = arCamera.GetComponent<ARCameraBackground>();
        }

        if (imageRecognitionManager == null)
        {
            imageRecognitionManager = Object.FindFirstObjectByType<ImageRecognitionManager>();
        }

        if (scanAudioManager == null)
        {
            scanAudioManager = Object.FindFirstObjectByType<AudioManager>();
        }
    }

    private void BindButtons()
    {
        if (cameraToggleButton != null)
        {
            cameraToggleButton.onClick.RemoveListener(ToggleCamera);
            cameraToggleButton.onClick.AddListener(ToggleCamera);
        }

        if (muteToggleButton != null)
        {
            muteToggleButton.onClick.RemoveListener(ToggleMute);
            muteToggleButton.onClick.AddListener(ToggleMute);
        }
    }

    private void ToggleCamera()
    {
        ApplyCameraOff(!cameraOff);
        cameraPulseRoutine = StartPressPulse(cameraToggleButton, cameraPulseRoutine);
    }

    private void ToggleMute()
    {
        ApplyMuted(!muted);
        mutePulseRoutine = StartPressPulse(muteToggleButton, mutePulseRoutine);
    }

    private void ApplyCameraOff(bool shouldTurnOff)
    {
        cameraOff = shouldTurnOff;

        imageRecognitionManager?.SetCameraScanningPausedByUser(cameraOff);

        if (arCameraBackground != null)
        {
            arCameraBackground.enabled = !cameraOff;
        }

        if (arCameraManager != null)
        {
            arCameraManager.enabled = !cameraOff;
        }

        if (arCamera != null && cameraOff)
        {
            arCamera.clearFlags = CameraClearFlags.SolidColor;
            arCamera.backgroundColor = Color.black;
        }

        if (statusText != null)
        {
            statusText.text = cameraOff
                ? AppLanguage.Text("camera_off")
                : AppLanguage.Text("camera_on");
        }

        UpdateVisuals();
    }

    private void ApplyMuted(bool shouldMute)
    {
        muted = shouldMute;

        if (muted)
        {
            if (!hasStoredAudioState)
            {
                previousListenerVolume = AudioListener.volume;
                previousListenerPause = AudioListener.pause;
                hasStoredAudioState = true;
            }

            previousMuteStates.Clear();
            AudioSource[] audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            for (int i = 0; i < audioSources.Length; i++)
            {
                if (audioSources[i] == null)
                {
                    continue;
                }

                previousMuteStates[audioSources[i]] = audioSources[i].mute;
                audioSources[i].mute = true;
            }

            scanAudioManager?.StopNarration();
            AudioListener.pause = true;
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.pause = hasStoredAudioState ? previousListenerPause : false;
            AudioListener.volume = hasStoredAudioState ? previousListenerVolume : 1f;

            foreach (KeyValuePair<AudioSource, bool> pair in previousMuteStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.mute = pair.Value;
                }
            }

            previousMuteStates.Clear();
            hasStoredAudioState = false;
        }

        if (statusText != null)
        {
            statusText.text = muted ? AppLanguage.Text("audio_muted") : AppLanguage.Text("audio_unmuted");
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        SetButtonVisual(cameraButtonBackground, cameraToggleButton, cameraOff);
        SetButtonVisual(muteButtonBackground, muteToggleButton, muted);

        if (cameraOffSlash != null)
        {
            cameraOffSlash.SetActive(cameraOff);
        }

        if (muteSlash != null)
        {
            muteSlash.SetActive(muted);
        }
    }

    private void SetButtonVisual(Image background, Button button, bool disabled)
    {
        Color color = disabled ? disabledColor : enabledColor;
        if (background != null)
        {
            background.color = color;
        }

        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = pressedColor;
        colors.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private Coroutine StartPressPulse(Button button, Coroutine runningRoutine)
    {
        if (button == null)
        {
            return runningRoutine;
        }

        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
        }

        return StartCoroutine(PressPulse(button.transform));
    }

    private IEnumerator PressPulse(Transform target)
    {
        if (target == null)
        {
            yield break;
        }

        target.localScale = Vector3.one * 0.9f;
        yield return new WaitForSecondsRealtime(0.08f);
        target.localScale = Vector3.one;
    }
}
