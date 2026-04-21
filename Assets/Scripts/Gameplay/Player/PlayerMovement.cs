using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OfficeFlipOut.UI;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    // Update is called once per frame
    void Update()
    {
        if (ClipboardUIState.ShouldBlockGameplayInput)
        {
            return;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        transform.Translate(Vector3.right * horizontalInput * speed * Time.deltaTime);

        transform.Translate(Vector3.forward * verticalInput * speed * Time.deltaTime);
    }

    
}
