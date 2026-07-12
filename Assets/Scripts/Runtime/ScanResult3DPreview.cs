using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Turns the scan result preview image into a rotating 3D-style card inside the modal.
/// </summary>
public class ScanResult3DPreview : MonoBehaviour
{
    [SerializeField] private Image legacyPreviewImage;
    [SerializeField] private float rotationSpeed = 46f;
    [SerializeField] private float maxWidthRatio = 0.56f;
    [SerializeField] private float maxHeightRatio = 0.68f;

    private RectTransform frameRect;
    private RectTransform cardRoot;
    private RectTransform shadowRect;
    private Image frontImage;
    private Image backImage;
    private Image edgeImage;
    private Image shadowImage;
    private float currentAngle;

    public void Configure(Image previewImage)
    {
        legacyPreviewImage = previewImage;
        frameRect = previewImage != null && previewImage.transform.parent is RectTransform parentRect
            ? parentRect
            : transform as RectTransform;

        EnsureVisuals();
    }

    public void Show(Sprite sprite)
    {
        EnsureVisuals();

        if (legacyPreviewImage != null)
        {
            legacyPreviewImage.enabled = false;
            legacyPreviewImage.raycastTarget = false;
        }

        if (sprite == null || cardRoot == null)
        {
            Hide();
            return;
        }

        frontImage.sprite = sprite;
        backImage.sprite = sprite;
        edgeImage.sprite = sprite;
        shadowImage.sprite = sprite;

        frontImage.preserveAspect = true;
        backImage.preserveAspect = true;
        edgeImage.preserveAspect = true;
        shadowImage.preserveAspect = true;

        ResizeCard(sprite);
        currentAngle = 0f;
        cardRoot.localRotation = Quaternion.identity;
        cardRoot.gameObject.SetActive(true);
        shadowRect.gameObject.SetActive(true);
        cardRoot.SetAsLastSibling();
    }

    public void Hide()
    {
        if (cardRoot != null)
        {
            cardRoot.gameObject.SetActive(false);
        }

        if (shadowRect != null)
        {
            shadowRect.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (cardRoot == null || !cardRoot.gameObject.activeInHierarchy)
        {
            return;
        }

        currentAngle = Mathf.Repeat(currentAngle + (rotationSpeed * Time.unscaledDeltaTime), 360f);
        cardRoot.localRotation = Quaternion.Euler(0f, currentAngle, 0f);

        bool showingFront = currentAngle <= 90f || currentAngle >= 270f;
        frontImage.gameObject.SetActive(showingFront);
        backImage.gameObject.SetActive(!showingFront);

        float edgeAlpha = Mathf.Lerp(0.22f, 0.88f, Mathf.Abs(Mathf.Sin(currentAngle * Mathf.Deg2Rad)));
        Color edgeColor = edgeImage.color;
        edgeColor.a = edgeAlpha;
        edgeImage.color = edgeColor;
    }

    private void EnsureVisuals()
    {
        if (frameRect == null)
        {
            frameRect = transform as RectTransform;
        }

        if (frameRect == null || cardRoot != null)
        {
            return;
        }

        GameObject shadowObject = new GameObject("ScanPreview3DShadow", typeof(RectTransform), typeof(Image));
        shadowObject.transform.SetParent(frameRect, false);
        shadowRect = shadowObject.GetComponent<RectTransform>();
        shadowImage = shadowObject.GetComponent<Image>();
        shadowImage.color = new Color(0f, 0f, 0f, 0.28f);
        shadowImage.raycastTarget = false;

        GameObject rootObject = new GameObject("ScanPreview3DCard", typeof(RectTransform));
        rootObject.transform.SetParent(frameRect, false);
        cardRoot = rootObject.GetComponent<RectTransform>();
        cardRoot.anchorMin = new Vector2(0.5f, 0.5f);
        cardRoot.anchorMax = new Vector2(0.5f, 0.5f);
        cardRoot.pivot = new Vector2(0.5f, 0.5f);
        cardRoot.anchoredPosition = Vector2.zero;

        edgeImage = CreateCardImage("CardEdge", cardRoot, new Color(0.05f, 0.08f, 0.12f, 0.82f));
        frontImage = CreateCardImage("CardFront", cardRoot, Color.white);
        backImage = CreateCardImage("CardBack", cardRoot, new Color(0.86f, 0.9f, 0.96f, 1f));
        backImage.rectTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        Hide();
    }

    private Image CreateCardImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void ResizeCard(Sprite sprite)
    {
        Vector2 frameSize = frameRect != null && frameRect.rect.width > 1f && frameRect.rect.height > 1f
            ? frameRect.rect.size
            : new Vector2(820f, 320f);

        float spriteWidth = Mathf.Max(1f, sprite.rect.width);
        float spriteHeight = Mathf.Max(1f, sprite.rect.height);
        float scale = Mathf.Min(
            frameSize.x * Mathf.Clamp01(maxWidthRatio) / spriteWidth,
            frameSize.y * Mathf.Clamp01(maxHeightRatio) / spriteHeight);

        Vector2 cardSize = new Vector2(spriteWidth * scale, spriteHeight * scale);
        cardSize.x = Mathf.Clamp(cardSize.x, 110f, frameSize.x * 0.62f);
        cardSize.y = Mathf.Clamp(cardSize.y, 95f, frameSize.y * 0.74f);

        cardRoot.sizeDelta = cardSize;
        frontImage.rectTransform.sizeDelta = cardSize;
        backImage.rectTransform.sizeDelta = cardSize;
        edgeImage.rectTransform.sizeDelta = cardSize + new Vector2(18f, 18f);

        shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
        shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.anchoredPosition = new Vector2(0f, -14f);
        shadowRect.sizeDelta = cardSize + new Vector2(38f, 30f);
    }
}
