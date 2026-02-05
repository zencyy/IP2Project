using UnityEngine;

public class HeadFollower : MonoBehaviour
{
    [Header("Settings")]
    public Transform headCamera;
    public KeyCode toggleKey = KeyCode.Alpha7; // Default to '7' on top row
    
    [Header("Position Settings")]
    public float forwardDistance = 1.5f; 
    public float rightOffset = 0.6f;     
    public float heightOffset = -0.2f;   

    [Header("Smoothing")]
    public float positionSmoothness = 5.0f;
    public float rotationSmoothness = 5.0f;

    // Internal tracker
    private Canvas myCanvas;
    private bool isVisible = true;

    void Start()
    {
        // Automatically find the Canvas component on this object
        myCanvas = GetComponent<Canvas>();
        
        if (myCanvas == null)
        {
            // Try to find it in children if not on root
            myCanvas = GetComponentInChildren<Canvas>();
        }

        if (myCanvas == null)
        {
            Debug.LogError("HeadFollower: No 'Canvas' component found! Please attach this script to a UI Canvas.");
        }
    }

    void LateUpdate()
    {
        if (headCamera == null) return;

        // --- 1. TOGGLE INPUT ---
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleUI();
        }

        // --- 2. MOVEMENT (Only update pos if visible to save performance) ---
        if (isVisible)
        {
            Vector3 targetPos = headCamera.position;
            targetPos += headCamera.forward * forwardDistance;
            targetPos += headCamera.right * rightOffset;
            targetPos += headCamera.up * heightOffset;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionSmoothness);
            
            Quaternion targetRot = headCamera.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothness);
        }
    }

    public void ToggleUI()
    {
        isVisible = !isVisible;
        Debug.Log($"Toggle Key Pressed! Visible: {isVisible}");

        if (myCanvas != null)
        {
            // This hides the UI visuals but KEEPS the script running!
            myCanvas.enabled = isVisible;
        }
    }
}