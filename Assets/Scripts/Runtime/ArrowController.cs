using UnityEngine;

/// <summary>
/// Rotates the AR arrow so it always faces the current destination.
/// The script uses LookAt() as requested, then smooths the result.
/// </summary>
public class ArrowController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private bool keepArrowLevel = true;
    [SerializeField] private float bobHeight = 0.04f;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private float scalePulseAmount = 0.05f;
    [SerializeField] private float scalePulseSpeed = 1.8f;

    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;
    }

    /// <summary>
    /// Called by the NavigationManager whenever the destination changes.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        gameObject.SetActive(newTarget != null);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Keep the arrow level so it feels like a floor guide instead of a flying pointer.
        Vector3 lookTarget = target.position;
        if (keepArrowLevel)
        {
            lookTarget.y = transform.position.y;
        }

        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion currentRotation = transform.rotation;
            transform.LookAt(lookTarget, Vector3.up);
            Quaternion desiredRotation = transform.rotation;
            transform.rotation = currentRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
        }

        // A subtle bob makes the arrow easier to notice in AR.
        Vector3 bobbedPosition = startLocalPosition;
        bobbedPosition.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = bobbedPosition;

        float pulseScale = 1f + Mathf.Sin(Time.time * scalePulseSpeed) * scalePulseAmount;
        transform.localScale = startLocalScale * pulseScale;
    }
}
