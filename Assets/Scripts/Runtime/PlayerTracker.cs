using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Tracks the AR camera pose and exposes it as the indoor navigation player position.
/// </summary>
public class PlayerTracker : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform routeRoot;
    [SerializeField] private float positionSmoothing = 14f;
    [SerializeField] private float rotationSmoothing = 10f;

    private bool hasPoseSample;
    private Vector3 worldPosition;
    private Vector3 forwardDirection = Vector3.forward;

    public Vector3 WorldPosition => worldPosition;
    public Vector3 ForwardDirection => forwardDirection;
    public Vector3 LocalRoutePosition => routeRoot != null ? routeRoot.InverseTransformPoint(worldPosition) : worldPosition;

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
    }

    private void LateUpdate()
    {
        if (arCamera == null)
        {
            return;
        }

        Vector3 targetPosition = arCamera.transform.position;
        Vector3 cameraForward = arCamera.transform.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.001f)
        {
            cameraForward = Vector3.forward;
        }

        cameraForward.Normalize();

        if (!hasPoseSample)
        {
            worldPosition = targetPosition;
            forwardDirection = cameraForward;
            hasPoseSample = true;
        }
        else
        {
            worldPosition = Vector3.Lerp(worldPosition, targetPosition, positionSmoothing * Time.deltaTime);
            forwardDirection = Vector3.Slerp(forwardDirection, cameraForward, rotationSmoothing * Time.deltaTime);
        }

        transform.position = worldPosition;
        transform.rotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
    }

    public void SetRouteRoot(Transform newRouteRoot)
    {
        routeRoot = newRouteRoot;
    }
}
