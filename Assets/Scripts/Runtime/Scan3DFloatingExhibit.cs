using UnityEngine;

/// <summary>
/// Adds a subtle hover and turntable motion to generated scan exhibits.
/// </summary>
public class Scan3DFloatingExhibit : MonoBehaviour
{
    [SerializeField] private float bobAmplitude = 0.014f;
    [SerializeField] private float bobSpeed = 1.2f;
    [SerializeField] private float yawAmplitude = 3.5f;
    [SerializeField] private float spinDegreesPerSecond = 1.5f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float timeOffset;
    private bool configured;

    public void Configure(float amplitude, float speed, float yaw, float spin)
    {
        bobAmplitude = Mathf.Max(0f, amplitude);
        bobSpeed = Mathf.Max(0.1f, speed);
        yawAmplitude = Mathf.Max(0f, yaw);
        spinDegreesPerSecond = spin;
        CaptureBasePose();
    }

    private void OnEnable()
    {
        if (!configured)
        {
            CaptureBasePose();
        }
    }

    private void CaptureBasePose()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        timeOffset = Random.Range(0f, 6.28f);
        configured = true;
    }

    private void LateUpdate()
    {
        if (!configured)
        {
            CaptureBasePose();
        }

        float time = Time.time + timeOffset;
        float bob = Mathf.Sin(time * bobSpeed) * bobAmplitude;
        float yaw = Mathf.Sin(time * bobSpeed * 0.55f) * yawAmplitude;
        yaw += time * spinDegreesPerSecond;

        transform.localPosition = baseLocalPosition + (Vector3.up * bob);
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, yaw, 0f);
    }
}
