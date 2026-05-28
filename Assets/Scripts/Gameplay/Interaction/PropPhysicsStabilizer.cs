using UnityEngine;

/// <summary>
/// Improves physics stability for interactive props by applying safe defaults and constraints.
/// Prevents jittering, excessive spinning, and unstable behavior.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PropPhysicsStabilizer : MonoBehaviour
{
    [Header("Physics Stability Settings")]
    [SerializeField] private float targetLinearDamping = 0.1f; // Prevents sliding
    [SerializeField] private float targetAngularDamping = 0.3f; // Prevents excessive spinning (default is 0.05)
    [SerializeField] private float maxLinearVelocity = 50f; // Clamp max speed
    [SerializeField] private float maxAngularVelocity = 20f; // Clamp max spin (in rad/s)
    [SerializeField] private bool improveCollisionDetection = true;
    [SerializeField] private CollisionDetectionMode collisionMode = CollisionDetectionMode.Continuous;
    [SerializeField] private RigidbodyConstraints constraintAxis = RigidbodyConstraints.None;
    
    [Header("Mass Settings")]
    [SerializeField] private bool adjustMass = true;
    [SerializeField] private float minimumMass = 0.1f;
    [SerializeField] private float maximumMass = 100f;
    
    private Rigidbody cachedRigidbody;
    private float originalLinearDamping;
    private float originalAngularDamping;
    private bool hasAppliedStabilization;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody == null)
            return;

        StabilizePhysics();
    }

    private void FixedUpdate()
    {
        if (cachedRigidbody == null)
            return;

        // Clamp velocities each frame to prevent runaway physics
        ClampVelocities();
    }

    public void StabilizePhysics()
    {
        if (cachedRigidbody == null)
            return;

        if (hasAppliedStabilization)
            return;

        // Store originals
        originalLinearDamping = cachedRigidbody.linearDamping;
        originalAngularDamping = cachedRigidbody.angularDamping;

        // Apply stability settings
        cachedRigidbody.linearDamping = targetLinearDamping;
        cachedRigidbody.angularDamping = targetAngularDamping;

        if (improveCollisionDetection)
        {
            cachedRigidbody.collisionDetectionMode = collisionMode;
        }

        if (adjustMass)
        {
            cachedRigidbody.mass = Mathf.Clamp(cachedRigidbody.mass, minimumMass, maximumMass);
        }

        if (constraintAxis != RigidbodyConstraints.None)
        {
            cachedRigidbody.constraints = constraintAxis;
        }

        hasAppliedStabilization = true;
    }

    private void ClampVelocities()
    {
        if (cachedRigidbody == null)
            return;

        // Clamp linear velocity
        if (cachedRigidbody.linearVelocity.sqrMagnitude > maxLinearVelocity * maxLinearVelocity)
        {
            cachedRigidbody.linearVelocity = Vector3.ClampMagnitude(cachedRigidbody.linearVelocity, maxLinearVelocity);
        }

        // Clamp angular velocity
        if (cachedRigidbody.angularVelocity.sqrMagnitude > maxAngularVelocity * maxAngularVelocity)
        {
            cachedRigidbody.angularVelocity = Vector3.ClampMagnitude(cachedRigidbody.angularVelocity, maxAngularVelocity);
        }
    }

    public void RestoreOriginalDamping()
    {
        if (cachedRigidbody == null)
            return;

        cachedRigidbody.linearDamping = originalLinearDamping;
        cachedRigidbody.angularDamping = originalAngularDamping;
    }
}
