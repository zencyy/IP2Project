using UnityEngine;
using UnityEngine.AI; // 1. We need the AI namespace

[RequireComponent(typeof(LineRenderer))]
public class NavigationLine : MonoBehaviour
{
    public Transform playerOrigin; 
    public Transform target;       
    
    private LineRenderer line;
    private NavMeshPath path; // Stores the calculated corners
    
    public float lineHeight = 0.2f; // How high above ground (prevents flickering)

    void Start()
    {
        line = GetComponent<LineRenderer>();
        path = new NavMeshPath(); // Initialize the path memory
        
        // Make the line look smoother
        line.numCornerVertices = 5; 
    }

    void Update()
    {
        // 1. Safety Checks
        if (target == null || playerOrigin == null)
        {
            line.enabled = false;
            return;
        }

        // 2. Calculate the path on the Navigation Mesh
        // This math finds the path around trees and over hills
        bool foundPath = NavMesh.CalculatePath(playerOrigin.position, target.position, NavMesh.AllAreas, path);

        if (foundPath && path.status == NavMeshPathStatus.PathComplete)
        {
            line.enabled = true;
            
            // 3. Update the Line Renderer with the new path points
            DrawPath(path.corners); 
        }
        else
        {
            // If path is invalid (e.g. target is off the NavMesh), hide line or draw straight
            line.enabled = false; 
        }
    }

    void DrawPath(Vector3[] corners)
    {
        line.positionCount = corners.Length;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 point = corners[i];
            // LIFT the point slightly so it doesn't clip into the dirt
            point.y += lineHeight; 
            line.SetPosition(i, point);
        }
    }
}