using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public float speed = 5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveX, 0, moveY);
        movement = Vector3.ClampMagnitude(movement, 1); // Prevent faster diagonal movement

        transform.Translate(movement * speed * Time.deltaTime, Space.World);
        
    }
}
