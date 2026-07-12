using UnityEngine;

/// <summary>
/// Keeps the floating 3D label facing the AR camera and updates its text.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private TextMesh labelText;
    [SerializeField] private MeshRenderer labelRenderer;
    [SerializeField] private bool keepLevel = true;
    [SerializeField] private float labelScale = 0.065f;
    [SerializeField] private float labelCharacterSize = 0.05f;
    [SerializeField] private int labelFontSize = 36;
    [SerializeField] private int maxCharactersPerLine = 14;

    private void Awake()
    {
        if (labelText == null)
        {
            labelText = GetComponent<TextMesh>();
        }

        if (labelRenderer == null)
        {
            labelRenderer = GetComponent<MeshRenderer>();
        }

        ApplyLabelStyle();
    }

    private void Reset()
    {
        labelText = GetComponent<TextMesh>();
        labelRenderer = GetComponent<MeshRenderer>();
        ApplyLabelStyle();
    }

    /// <summary>
    /// Lets the NavigationManager provide the active AR camera.
    /// </summary>
    public void SetTargetCamera(Transform cameraTransform)
    {
        targetCamera = cameraTransform;
    }

    /// <summary>
    /// Updates the label message shown above the arrow.
    /// </summary>
    public void SetMessage(string message)
    {
        if (labelText != null)
        {
            labelText.text = FormatMessage(message);
        }

        if (labelRenderer != null)
        {
            labelRenderer.enabled = !string.IsNullOrWhiteSpace(message);
        }
    }

    private void ApplyLabelStyle()
    {
        transform.localScale = Vector3.one * labelScale;

        if (labelText == null)
        {
            return;
        }

        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.characterSize = labelCharacterSize;
        labelText.fontSize = labelFontSize;
        labelText.lineSpacing = 0.85f;
        labelText.color = Color.white;
    }

    private string FormatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        message = message.Trim();
        if (message.Contains("\n") || message.Length <= maxCharactersPerLine)
        {
            return message;
        }

        int splitIndex = message.LastIndexOf(' ', Mathf.Min(maxCharactersPerLine, message.Length - 1));
        if (splitIndex <= 0)
        {
            return message;
        }

        string firstLine = message.Substring(0, splitIndex).Trim();
        string remainingText = message.Substring(splitIndex + 1).Trim();
        return firstLine + "\n" + FormatMessage(remainingText);
    }

    private void LateUpdate()
    {
        if (targetCamera == null || labelRenderer == null || !labelRenderer.enabled)
        {
            return;
        }

        Vector3 lookTarget = targetCamera.position;
        if (keepLevel)
        {
            lookTarget.y = transform.position.y;
        }

        Vector3 facingDirection = transform.position - lookTarget;
        if (facingDirection.sqrMagnitude > 0.001f)
        {
            transform.forward = facingDirection.normalized;
        }
    }
}
