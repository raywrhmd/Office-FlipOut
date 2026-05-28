using UnityEngine;

/// <summary>
/// Simple NPC facing: turn in place toward the player.
/// </summary>
[DisallowMultipleComponent]
public class NpcCameraBillboard : MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private bool autoFindPlayerTarget = true;
    [SerializeField] private Vector3 rotationOffsetEuler = new Vector3(0f, 180f, 0f);

    private void Awake()
    {
        ResolvePlayerTarget();
    }

    private void OnEnable()
    {
        ResolvePlayerTarget();
    }

    private void LateUpdate()
    {
        if (playerTarget == null && autoFindPlayerTarget)
        {
            ResolvePlayerTarget();
        }
        if (playerTarget == null)
        {
            return;
        }

        Vector3 toTarget = playerTarget.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = lookRotation * Quaternion.Euler(rotationOffsetEuler);
    }

    private void ResolvePlayerTarget()
    {
        if (playerTarget != null || !autoFindPlayerTarget)
        {
            return;
        }

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            playerTarget = player.transform;
            return;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            playerTarget = taggedPlayer.transform;
        }
    }
}
