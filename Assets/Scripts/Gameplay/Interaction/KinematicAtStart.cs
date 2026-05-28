using UnityEngine;

/// <summary>
/// Ensures props start in kinematic state (locked in place).
/// They become dynamic when grabbed or when physics events require it.
/// This prevents shelf/counter items from falling on scene load.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class KinematicAtStart : MonoBehaviour
{
    private Rigidbody cachedRigidbody;
    private RageFlipOutProp rageFlipOutProp;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        rageFlipOutProp = GetComponent<RageFlipOutProp>();
        
        if (cachedRigidbody == null)
            return;

        // Clear velocities BEFORE making kinematic
        cachedRigidbody.linearVelocity = Vector3.zero;
        cachedRigidbody.angularVelocity = Vector3.zero;
        
        // Now lock it down
        cachedRigidbody.isKinematic = true;
        cachedRigidbody.useGravity = false;
        
        // Disable RageFlipOutProp's automatic physics forcing if it exists
        if (rageFlipOutProp != null)
        {
            rageFlipOutProp.enabled = false;
        }
    }

    /// <summary>
    /// Unlock the rigidbody - make it dynamic and responsive to physics.
    /// </summary>
    public void UnlockPhysics()
    {
        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = false;
            cachedRigidbody.useGravity = true;
        }
        
        // Re-enable RageFlipOutProp so it can react to events
        if (rageFlipOutProp != null && !rageFlipOutProp.enabled)
        {
            rageFlipOutProp.enabled = true;
        }
    }
}
