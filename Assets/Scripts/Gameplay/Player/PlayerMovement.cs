using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OfficeFlipOut.UI;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float sprintspeed = 10f;
    public Transform cameraTransform;
    public float gravity = -9.81f; 
    
    private CharacterController controller;
    private float yVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ClipboardUIState.ShouldBlockGameplayInput)
        {
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Check if sprint key is held
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintspeed : speed;

          // Get camera directions
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Flatten them so you don’t move up/down when camera looks up/down
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        if (controller.isGrounded && yVelocity < 0)
        {
            yVelocity = -2f; // small stick-to-ground force
        }

        yVelocity += gravity * Time.deltaTime;

        Vector3 move = (forward * verticalInput + right * horizontalInput).normalized;

        Vector3 finalMove = move * currentSpeed;
        finalMove.y = yVelocity;

        controller.Move(finalMove * Time.deltaTime);

    }

    
}
