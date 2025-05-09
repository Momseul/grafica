using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

public class CameraController : MonoBehaviour
{
    public Transform target;
    public float distance = 15.0f;
    public float sensitivity = 0.1f; 
    public float scrollSensitivity = 1.0f; 
    public float minDistance = 5.0f;
    public float maxDistance = 30.0f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    private Vector2 lookInput = Vector2.zero;
    private float scrollInput = 0f;
    private bool isLookEnabled = false;

    void Start()
    {
         if (target != null) {
            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;
            distance = Vector3.Distance(transform.position, target.position);
            distance = Mathf.Clamp(distance, minDistance, maxDistance); 
         } else {
             Debug.LogWarning("Camera Controller target is not assigned!", this);
         }


    }

void LateUpdate()
{
    if (target != null)
    {
        if (isLookEnabled)
        {
            currentX += lookInput.x * sensitivity;
            currentY -= lookInput.y * sensitivity;
            currentY = Mathf.Clamp(currentY, -80f, 80f); 
        }

        if (scrollInput != 0)
        {
            distance -= scrollInput * scrollSensitivity; 

            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            scrollInput = 0f; 
        }

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 position = target.position - (rotation * Vector3.forward * distance);

        transform.rotation = rotation;
        transform.position = position;
    }
}


public void OnLook(InputAction.CallbackContext context)
{
    if (context.performed) 
    {
        lookInput = context.ReadValue<Vector2>();
    }
    else if (context.canceled) 
    {
        lookInput = Vector2.zero; 
    }
}

public void OnScroll(InputAction.CallbackContext context)
{

    Debug.Log($"OnScroll input: {context.phase}, Value: {context.ReadValue<Vector2>()}");

    if (context.performed)
    {
        Vector2 rawScrollValue = context.ReadValue<Vector2>();
        scrollInput = rawScrollValue.y; 
        Debug.Log($"Scroll Input SET TO: {scrollInput} (from raw Y: {rawScrollValue.y})");
    }
    else if (context.canceled)
    {
        Debug.Log("OnScroll performed is FALSE");
    }

}

public void OnEnableLook(InputAction.CallbackContext context)
{

    if (context.performed)
    {
        isLookEnabled = true;
    }
    else if (context.canceled)
    {
        isLookEnabled = false;
    }


}
}