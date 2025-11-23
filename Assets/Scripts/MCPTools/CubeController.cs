using UnityEngine;

public class CubeController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 90f;
    
    [Header("Input Settings")]
    public KeyCode moveForwardKey = KeyCode.W;
    public KeyCode moveBackwardKey = KeyCode.S;
    public KeyCode moveLeftKey = KeyCode.A;
    public KeyCode moveRightKey = KeyCode.D;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(moveForwardKey))
            movement += transform.forward;
        if (Input.GetKey(moveBackwardKey))
            movement -= transform.forward;
        if (Input.GetKey(moveLeftKey))
            movement -= transform.right;
        if (Input.GetKey(moveRightKey))
            movement += transform.right;

        if (movement != Vector3.zero)
        {
            transform.position += movement.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleRotation()
    {
        if (Input.GetKey(rotateLeftKey))
        {
            transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0);
        }
        if (Input.GetKey(rotateRightKey))
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }
}