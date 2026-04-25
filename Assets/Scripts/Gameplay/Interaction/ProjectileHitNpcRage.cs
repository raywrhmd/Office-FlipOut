using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ProjectileHitNpcRage : MonoBehaviour
{
    private Rigidbody cachedRigidbody;
    private bool isArmed;
    private bool hasBeenConsumedByNpc;
    private float armedAtTime;
    private bool logDebug;

    public bool IsArmed => isArmed;
    public bool HasBeenConsumedByNpc => hasBeenConsumedByNpc;

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    public void Arm(bool enableDebugLogging)
    {
        logDebug = enableDebugLogging;

        isArmed = true;
        hasBeenConsumedByNpc = false;
        armedAtTime = Time.time;

        if (logDebug)
        {
            Debug.Log("[ProjectileHitNpcRage] Armed thrown object marker for '" + name + "'.", this);
        }
    }

    public bool TryConsumeForNpcHit(Rage_Meter npcMeter, float minImpactSpeed, Collision collision, bool logNpcDebug)
    {
        if (!isArmed || hasBeenConsumedByNpc)
        {
            return false;
        }

        if (Time.time < armedAtTime + 0.05f)
        {
            return false;
        }

        if (collision == null)
        {
            return false;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed)
        {
            if (logNpcDebug)
            {
                Debug.Log(
                    "[ProjectileHitNpcRage] Ignored low-impact hit from '" + name +
                    "' (" + impactSpeed.ToString("F2") + " < " + minImpactSpeed.ToString("F2") + ").",
                    this);
            }

            return false;
        }

        hasBeenConsumedByNpc = true;
        isArmed = false;

        if (logNpcDebug)
        {
            string npcName = npcMeter != null ? npcMeter.name : "<unknown npc>";
            Debug.Log(
                "[ProjectileHitNpcRage] Consumed thrown object '" + name +
                "' by NPC '" + npcName +
                "' at impact speed " + impactSpeed.ToString("F2") + ".",
                this);
        }

        return true;
    }

    public void MarkConsumedByNpc()
    {
        hasBeenConsumedByNpc = true;
        isArmed = false;
    }

    public int GetObjectInstanceId()
    {
        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponent<Rigidbody>();
        }

        return cachedRigidbody != null ? cachedRigidbody.GetInstanceID() : GetInstanceID();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || hasBeenConsumedByNpc || Time.time < armedAtTime + 0.05f || collision == null)
        {
            return;
        }

        Rage_Meter npcMeter = collision.collider != null ? collision.collider.GetComponentInParent<Rage_Meter>() : null;
        if (npcMeter != null && npcMeter.RegisterProjectileHit(this, collision))
        {
            MarkConsumedByNpc();
            Debug.Log(
                "[ProjectileHitNpcRage] Forwarded thrown object hit from '" + name + "' to NPC meter '" + npcMeter.name + "'.",
                this);
            return;
        }

        if (logDebug)
        {
            Debug.Log(
                "[ProjectileHitNpcRage] " + name +
                " collided at relative speed " + collision.relativeVelocity.magnitude.ToString("F2") +
                " but did not find a Rage_Meter to notify.",
                this);
        }
    }
}