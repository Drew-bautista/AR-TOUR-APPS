using UnityEngine;

/// <summary>
/// Keeps a world-space scan exhibit panel readable by facing the AR camera.
/// </summary>
public class Scan3DWorldBillboard : MonoBehaviour
{
    [SerializeField] private Transform targetCamera;
    [SerializeField] private bool keepLevel;

    public void Configure(Transform cameraTransform, bool levelOnly)
    {
        targetCamera = cameraTransform;
        keepLevel = levelOnly;
    }

    private void LateUpdate()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 lookOrigin = targetCamera.position;
        if (keepLevel)
        {
            lookOrigin.y = transform.position.y;
        }

        Vector3 facingDirection = transform.position - lookOrigin;
        if (facingDirection.sqrMagnitude > 0.0001f)
        {
            transform.forward = facingDirection.normalized;
        }
    }
}
