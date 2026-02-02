using UnityEngine;

public class HeadFollower : MonoBehaviour
{
    [Header("Settings")]
    public Transform headCamera;   // Drag your Main Camera here
    public float distance = 1.5f;  // Distance in front of player
    public float smoothness = 5.0f; // How fast it catches up (Lower = lazier)
    
    [Header("Offset")]
    public Vector3 offset = new Vector3(0, -0.2f, 0); // Slight adjustment (e.g., lower it so it's not blocking eyes)

    void LateUpdate()
    {
        if (headCamera == null) return;

        // 1. Calculate Target Position
        // Position is directly in front of the camera + offset
        Vector3 targetPosition = headCamera.position + (headCamera.forward * distance) + offset;
        
        // Optional: Keep height somewhat stable so looking down doesn't shove UI into the floor
        // (Comment this out if you WANT it to look down with you perfectly)
        // targetPosition.y = Mathf.Lerp(transform.position.y, headCamera.position.y + offset.y, Time.deltaTime * smoothness);

        // 2. Smoothly Move there
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothness);

        // 3. Rotate to face the camera
        // We use LookRotation so the UI always faces the user
        transform.rotation = Quaternion.LookRotation(transform.position - headCamera.position);
    }
}