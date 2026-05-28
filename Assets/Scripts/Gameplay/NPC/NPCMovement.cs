using UnityEngine;
using UnityEngine.AI;
using OfficeFlipOut.Systems;

public class NPCMovement : MonoBehaviour
{
    public Transform[] waypoints;

    [Header("Footstep Audio")]
    [SerializeField] private float footstepInterval = 1.2f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.2f;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private float footstepTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        GoToNextWaypoint();
    }

    void Update()
    {
        // Check if we've reached the current waypoint
        if(!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }

        // Handle footstep sounds
        UpdateFootstepAudio();
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.SetDestination(waypoints[currentWaypointIndex].position);

        // Move to next waypoint
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void UpdateFootstepAudio()
    {
        if (agent == null || agent.velocity.magnitude < 0.1f)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval)
        {
            AudioEvents.RequestOneShot(
                AudioManager.AudioClipType.WalkingSteps,
                transform.position,
                footstepVolume,
                spatialBlend: 1f,
                bus: AudioBus.Sfx
            );
            footstepTimer = 0f;
        }
    }
}
