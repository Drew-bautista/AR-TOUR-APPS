using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores tour metadata for a location and detects when the player reaches it.
/// Attach this script to an empty waypoint GameObject in the AR scene.
/// </summary>
public class LocationTrigger : MonoBehaviour
{
    [Header("Location Data")]
    [SerializeField] private string locationName = "New Location";
    [SerializeField] [TextArea(2, 4)] private string description = "Add a short description for this tour stop.";
    [SerializeField] private int sequenceOrder;
    [SerializeField] private float reachDistance = 1.25f;
    [SerializeField] private AudioClip narrationClip;
    [SerializeField] private List<Transform> approachPoints = new List<Transform>();

    private NavigationManager navigationManager;
    private Transform trackedPlayer;

    /// <summary>
    /// True after the player has already completed this stop in the current run.
    /// </summary>
    public bool HasBeenVisited { get; private set; }

    public string LocationName => locationName;
    public string Description => description;
    public int SequenceOrder => sequenceOrder;
    public float ReachDistance => reachDistance;
    public AudioClip NarrationClip => narrationClip;
    public IReadOnlyList<Transform> ApproachPoints => approachPoints;

    /// <summary>
    /// Called by the NavigationManager so each location can monitor the AR camera.
    /// </summary>
    public void Configure(NavigationManager manager, Transform playerTransform)
    {
        navigationManager = manager;
        trackedPlayer = playerTransform;
    }

    /// <summary>
    /// Lets the manager rewind or reset the tour during testing.
    /// </summary>
    public void SetVisited(bool visited)
    {
        HasBeenVisited = visited;
    }

    /// <summary>
    /// Returns the horizontal distance from the player to this tour stop.
    /// </summary>
    public float DistanceToPlayer()
    {
        if (trackedPlayer == null)
        {
            return float.PositiveInfinity;
        }

        // Indoor navigation should usually ignore vertical height differences.
        Vector3 playerPosition = trackedPlayer.position;
        Vector3 targetPosition = transform.position;
        playerPosition.y = 0f;
        targetPosition.y = 0f;

        return Vector3.Distance(playerPosition, targetPosition);
    }

    private void Update()
    {
        if (navigationManager == null || trackedPlayer == null || HasBeenVisited)
        {
            return;
        }

        if (!navigationManager.IsCurrentTarget(this))
        {
            return;
        }

        if (DistanceToPlayer() <= reachDistance)
        {
            navigationManager.NotifyLocationReached(this, false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.45f);
        Gizmos.DrawSphere(transform.position, 0.12f);
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, reachDistance);
    }
}
