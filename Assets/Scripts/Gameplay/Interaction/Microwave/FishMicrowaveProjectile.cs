using UnityEngine;

[DisallowMultipleComponent]
public class FishMicrowaveProjectile : MonoBehaviour
{
    private PhysicsGrab owner;
    private Rigidbody fishBody;
    private float snapDistance;
    private float minSpeed;
    private float expirationTime;
    private bool isArmed;

    public void Arm(PhysicsGrab physicsGrab, Rigidbody rb, float distance, float speedThreshold, float lifetime)
    {
        owner = physicsGrab;
        fishBody = rb;
        snapDistance = Mathf.Max(0.01f, distance);
        minSpeed = Mathf.Max(0f, speedThreshold);
        expirationTime = Time.time + Mathf.Max(0.1f, lifetime);
        isArmed = true;
    }

    private void Update()
    {
        if (!isArmed)
        {
            return;
        }

        if (owner == null || fishBody == null || Time.time >= expirationTime)
        {
            Destroy(this);
            return;
        }

        if (fishBody.linearVelocity.magnitude < minSpeed)
        {
            return;
        }

        if (owner.TrySnapThrownFishToMicrowave(fishBody, snapDistance))
        {
            isArmed = false;
            Destroy(this);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || owner == null || fishBody == null)
        {
            return;
        }

        if (fishBody.linearVelocity.magnitude < minSpeed)
        {
            return;
        }

        if (owner.TrySnapThrownFishToMicrowave(fishBody, snapDistance))
        {
            isArmed = false;
            Destroy(this);
        }
    }
}
