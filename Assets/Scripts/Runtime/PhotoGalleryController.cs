using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows all archived photos inside the application with a preview,
/// a short definition, and a scrollable list for quick selection.
/// </summary>
public class PhotoGalleryController : MonoBehaviour
{
    [Serializable]
    public class PhotoDefinition
    {
        [SerializeField] private string title = "Aguinaldo Shrine Archive Photo";
        [SerializeField] [TextArea(2, 4)] private string definition = "Archive image from the Aguinaldo Shrine collection.";
        [SerializeField] private Sprite image;

        public string Title => title;
        public string Definition => definition;
        public Sprite Image => image;
    }

    [Header("Photo Data")]
    [SerializeField] private List<PhotoDefinition> photos = new List<PhotoDefinition>();
    [SerializeField] private int previewStartIndex;

    [Header("UI References")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image previewImage;
    [SerializeField] private Text photoTitleText;
    [SerializeField] private Text photoDefinitionText;
    [SerializeField] private Text counterText;
    [SerializeField] private RectTransform listContent;
    [SerializeField] private ScrollRect listScrollRect;

    [Header("Visual Style")]
    [SerializeField] private Color entryColor = new Color(0.93f, 0.95f, 0.98f, 1f);
    [SerializeField] private Color selectedEntryColor = new Color(0.16f, 0.46f, 0.88f, 1f);
    [SerializeField] private Color entryTextColor = new Color(0.1f, 0.14f, 0.2f, 1f);
    [SerializeField] private Color selectedEntryTextColor = Color.white;

    private readonly List<Button> entryButtons = new List<Button>();
    private readonly List<Image> entryBackgrounds = new List<Image>();
    private readonly List<Text> entryLabels = new List<Text>();

    private Font cachedFont;
    private Sprite cachedButtonSprite;
    private int currentPhotoIndex = -1;
    private GameObject qrShortcutButtonObject;

    private void Awake()
    {
        qrShortcutButtonObject = GameObject.Find("QrScanShortcutButton");

        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenGallery);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseGallery);
        }

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(ShowPreviousPhoto);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextPhoto);
        }
    }

    private void Start()
    {
        BuildPhotoList();
        UpdateOpenButtonState();

        if (photos.Count > 0)
        {
            ShowPhoto(Mathf.Clamp(previewStartIndex, 0, photos.Count - 1));
        }
        else
        {
            ClearPreview();
        }

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }

        if (PlayerPrefs.GetInt(PremiumHomeUIController.OpenGalleryOnLoadPrefKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(PremiumHomeUIController.OpenGalleryOnLoadPrefKey);
            OpenGallery();
        }
    }

    private void OnDestroy()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenGallery);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseGallery);
        }

        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPreviousPhoto);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPhoto);
        }
    }

    /// <summary>
    /// Opens the gallery overlay above the AR scene.
    /// </summary>
    public void OpenGallery()
    {
        if (photos.Count == 0 || overlayRoot == null)
        {
            return;
        }

        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();
        SetQrShortcutVisible(false);

        if (currentPhotoIndex < 0)
        {
            ShowPhoto(0);
        }
        else
        {
            RefreshPreview();
        }
    }

    /// <summary>
    /// Hides the gallery overlay and returns focus to the AR scene.
    /// </summary>
    public void CloseGallery()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }

        SetQrShortcutVisible(true);
    }

    /// <summary>
    /// Moves to the next photo in the gallery.
    /// </summary>
    public void ShowNextPhoto()
    {
        if (photos.Count == 0)
        {
            return;
        }

        ShowPhoto(Mathf.Min(currentPhotoIndex + 1, photos.Count - 1));
    }

    /// <summary>
    /// Moves to the previous photo in the gallery.
    /// </summary>
    public void ShowPreviousPhoto()
    {
        if (photos.Count == 0)
        {
            return;
        }

        ShowPhoto(Mathf.Max(currentPhotoIndex - 1, 0));
    }

    private void BuildPhotoList()
    {
        ClearPhotoList();

        if (listContent == null || photos.Count == 0)
        {
            return;
        }

        cachedFont = cachedFont != null
            ? cachedFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        cachedButtonSprite = cachedButtonSprite != null
            ? cachedButtonSprite
            : Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        for (int i = 0; i < photos.Count; i++)
        {
            CreatePhotoEntryButton(i);
        }
    }

    private void ClearPhotoList()
    {
        entryButtons.Clear();
        entryBackgrounds.Clear();
        entryLabels.Clear();

        if (listContent == null)
        {
            return;
        }

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            Transform child = listContent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CreatePhotoEntryButton(int index)
    {
        GameObject buttonObject = new GameObject(
            "PhotoEntry_" + (index + 1).ToString("000"),
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(listContent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.sizeDelta = new Vector2(0f, 78f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 78f;

        Image background = buttonObject.GetComponent<Image>();
        background.sprite = cachedButtonSprite;
        background.type = Image.Type.Sliced;
        background.color = entryColor;

        Button button = buttonObject.GetComponent<Button>();
        int capturedIndex = index;
        button.onClick.AddListener(() => ShowPhoto(capturedIndex));

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f, 10f);
        labelRect.offsetMax = new Vector2(-18f, -10f);

        Text label = labelObject.GetComponent<Text>();
        label.font = cachedFont;
        label.text = (capturedIndex + 1).ToString("000") + "  " + photos[capturedIndex].Title;
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = entryTextColor;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        entryButtons.Add(button);
        entryBackgrounds.Add(background);
        entryLabels.Add(label);
    }

    private void ShowPhoto(int photoIndex)
    {
        if (photos.Count == 0)
        {
            ClearPreview();
            return;
        }

        currentPhotoIndex = Mathf.Clamp(photoIndex, 0, photos.Count - 1);
        RefreshPreview();
        UpdateSelectionState();
        ScrollSelectionIntoView();
    }

    private void RefreshPreview()
    {
        if (currentPhotoIndex < 0 || currentPhotoIndex >= photos.Count)
        {
            ClearPreview();
            return;
        }

        PhotoDefinition currentPhoto = photos[currentPhotoIndex];

        if (previewImage != null)
        {
            previewImage.enabled = currentPhoto.Image != null;
            previewImage.sprite = currentPhoto.Image;
            previewImage.preserveAspect = true;
        }

        if (photoTitleText != null)
        {
            photoTitleText.text = currentPhoto.Title;
        }

        if (photoDefinitionText != null)
        {
            photoDefinitionText.text = currentPhoto.Definition;
        }

        if (counterText != null)
        {
            counterText.text = (currentPhotoIndex + 1) + " / " + photos.Count;
        }

        if (previousButton != null)
        {
            previousButton.interactable = currentPhotoIndex > 0;
        }

        if (nextButton != null)
        {
            nextButton.interactable = currentPhotoIndex < photos.Count - 1;
        }
    }

    private void UpdateSelectionState()
    {
        for (int i = 0; i < entryButtons.Count; i++)
        {
            bool isSelected = i == currentPhotoIndex;

            if (entryBackgrounds[i] != null)
            {
                entryBackgrounds[i].color = isSelected ? selectedEntryColor : entryColor;
            }

            if (entryLabels[i] != null)
            {
                entryLabels[i].color = isSelected ? selectedEntryTextColor : entryTextColor;
            }
        }
    }

    private void ScrollSelectionIntoView()
    {
        if (listScrollRect == null || entryButtons.Count <= 1 || currentPhotoIndex < 0)
        {
            return;
        }

        float normalizedPosition = 1f - (currentPhotoIndex / (float)(entryButtons.Count - 1));
        listScrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
    }

    private void UpdateOpenButtonState()
    {
        if (openButton != null)
        {
            openButton.interactable = photos.Count > 0;
        }
    }

    private void SetQrShortcutVisible(bool visible)
    {
        if (qrShortcutButtonObject == null)
        {
            qrShortcutButtonObject = GameObject.Find("QrScanShortcutButton");
        }

        if (qrShortcutButtonObject == null)
        {
            return;
        }

        if (qrShortcutButtonObject.activeSelf != visible)
        {
            qrShortcutButtonObject.SetActive(visible);
        }

        if (visible)
        {
            qrShortcutButtonObject.transform.SetAsLastSibling();
        }
    }

    private void ClearPreview()
    {
        if (previewImage != null)
        {
            previewImage.enabled = false;
            previewImage.sprite = null;
        }

        if (photoTitleText != null)
        {
            photoTitleText.text = AppLanguage.Text("gallery_empty_title");
        }

        if (photoDefinitionText != null)
        {
            photoDefinitionText.text = AppLanguage.Text("gallery_empty_body");
        }

        if (counterText != null)
        {
            counterText.text = "0 / 0";
        }

        if (previousButton != null)
        {
            previousButton.interactable = false;
        }

        if (nextButton != null)
        {
            nextButton.interactable = false;
        }
    }
}
