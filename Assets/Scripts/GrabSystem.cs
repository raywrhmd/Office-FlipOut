using UnityEngine;

public class PhysicsGrab : MonoBehaviour
{
    public Camera cam;
    public Transform holdPoint;

    public float grabDistance = 3f;
    public float throwForce = 10f;

    private Rigidbody heldObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
                TryGrab();
            else
                Drop();
        }

        if (heldObject != null && Input.GetMouseButtonDown(0))
        {
            Throw();
        }
    }

    void FixedUpdate()
    {
        if (heldObject != null)
        {
            MoveHeldObject();
        }
    }

    void TryGrab()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, grabDistance))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();

            if (rb != null)
            {
                heldObject = rb;

                heldObject.useGravity = false;
                heldObject.linearVelocity = Vector3.zero;
                heldObject.angularVelocity = Vector3.zero;
            }
        }
    }

    void MoveHeldObject()
    {
        Vector3 moveDir = holdPoint.position - heldObject.position;

        heldObject.linearVelocity = moveDir * 10f;
    }

    void Drop()
    {
        heldObject.useGravity = true;
        heldObject = null;
    }

    void Throw()
    {
        heldObject.useGravity = true;
        heldObject.AddForce(cam.transform.forward * throwForce, ForceMode.Impulse);
        heldObject = null;
    }
}
