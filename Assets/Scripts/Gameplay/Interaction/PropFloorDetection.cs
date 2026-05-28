using UnityEngine;

/// <summary>
/// Prevents physics props from clipping through floors or falling indefinitely.
/// Detects when objects fall too far below spawn point and resets them.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PropFloorDetection : MonoBehaviour
{
    [Header("Floor Detection Settings")]
    [SerializeField] private float floorHeight = -10f; // World Y position of the floor
    [SerializeField] private float fallThresholdDistance = 5f; // How far below spawn before reset
    [SerializeField] private bool destroyOnFloorClip = false; // If true, destroy prop. If false, reposition.
    [SerializeField] private bool respectInitialHeight = true; // Use object's initial height as reference

    [Header("Out Of Bounds Respawn")]
    [SerializeField] private bool respawnWhenOutsideSafeBounds;
    [SerializeField, Min(0.1f)] private float maxHorizontalDistanceFromSpawn = 80f;
    [SerializeField, Min(0.1f)] private float maxVerticalDistanceFromSpawn = 30f;
    
    private Rigidbody cachedRigidbody;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float initialY;
    private float checkInterval = 0.5f;
    private float lastCheckTime;
    private bool spawnPointSet = false;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        // Defer capturing the spawn point until Start so scene setup/parents have stabilized.
        lastCheckTime = Time.time;
    }

    private void Start()
    {
        if (!spawnPointSet)
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            initialY = transform.position.y;
            spawnPointSet = true;
        }
    }

    private void FixedUpdate()
    {
        // Check periodically to reduce CPU cost
        if (Time.time - lastCheckTime < checkInterval)
            return;
        
        lastCheckTime = Time.time;
        
        CheckAndResetIfNeeded();
    }

    private void CheckAndResetIfNeeded()
    {
        if (cachedRigidbody == null)
            return;

        // Determine the threshold
        float fallThreshold = respectInitialHeight ? (initialY - fallThresholdDistance) : floorHeight;

        if (transform.position.y < fallThreshold || (respawnWhenOutsideSafeBounds && IsOutsideSafeBounds()))
        {
            RespawnOrDestroy();
        }
    }

    private bool IsOutsideSafeBounds()
    {
        Vector3 displacement = transform.position - spawnPosition;
        float horizontalDistanceSqr = displacement.x * displacement.x + displacement.z * displacement.z;
        float maxHorizontalDistanceSqr = maxHorizontalDistanceFromSpawn * maxHorizontalDistanceFromSpawn;

        if (horizontalDistanceSqr > maxHorizontalDistanceSqr)
        {
            return true;
        }

        if (Mathf.Abs(displacement.y) > maxVerticalDistanceFromSpawn)
        {
            return true;
        }

        return false;
    }

    private void RespawnOrDestroy()
    {
        if (destroyOnFloorClip && !respawnWhenOutsideSafeBounds)
        {
            Destroy(gameObject);
        }
        else
        {
            // Reset to spawn position
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            
            if (cachedRigidbody != null)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.WakeUp();
            }
        }
    }

    public void SetSpawnPoint(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
        initialY = position.y;
        spawnPointSet = true;
    }

    public void EnableSafeBoundsRespawn(bool enable)
    {
        respawnWhenOutsideSafeBounds = enable;
    }
}
