using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections.Generic;
using System.Linq;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class CircleSelector : MonoBehaviour
{
    [Header("Input Settings")]
    public InputActionProperty drawAction; 
    public Transform pointerTip;           
    public bool enableMouseTesting = true; // NEW: Toggle this ON to test with mouse

    [Header("Detection Settings")]
    public LayerMask animalLayer;          
    public float minPointDistance = 0.05f; 
    public float loopThreshold = 0.3f;     

    [Header("UI Feedback")]
    public GameObject spellingCanvas;      
    public TextMeshProUGUI spellingText;   
    public LineRenderer lineRenderer;
    

    private List<Vector3> strokePoints = new List<Vector3>();
    private bool isDrawing = false;

    void Start()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        
        if (spellingCanvas) spellingCanvas.SetActive(false);
    }

    void Update()
    {
        bool isPressed = false;

        // 1. CHECK INPUT (VR or MOUSE)
        if (enableMouseTesting && Input.GetMouseButton(0))
        {
            isPressed = true;
            // Move the pointerTip to follow the mouse (floating 2 meters in front of camera)
            if (pointerTip != null)
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = 2.0f; // Distance from camera
                pointerTip.position = Camera.main.ScreenToWorldPoint(mousePos);
            }
        }
        else if (drawAction.action != null && drawAction.action.ReadValue<float>() > 0.5f)
        {
            isPressed = true;
        }

        // 2. DRAWING LOGIC
        if (isPressed)
        {
            if (!isDrawing) StartStroke();
            UpdateStroke();
        }
        else if (isDrawing)
        {
            EndStroke();
        }
    }

    void StartStroke()
    {
        isDrawing = true;
        strokePoints.Clear();
        lineRenderer.positionCount = 0;
        if (spellingCanvas) spellingCanvas.SetActive(false);
    }

    void UpdateStroke()
    {
        if (pointerTip == null) return;

        Vector3 currentPos = pointerTip.position;

        if (strokePoints.Count == 0 || Vector3.Distance(strokePoints.Last(), currentPos) > minPointDistance)
        {
            strokePoints.Add(currentPos);
            lineRenderer.positionCount = strokePoints.Count;
            lineRenderer.SetPositions(strokePoints.ToArray());
        }
    }

    void EndStroke()
    {
        isDrawing = false;

        if (strokePoints.Count < 10) 
        {
            ClearLine();
            return;
        }

        float gap = Vector3.Distance(strokePoints.First(), strokePoints.Last());
        
        float totalLength = 0;
        for(int i = 1; i < strokePoints.Count; i++)
            totalLength += Vector3.Distance(strokePoints[i-1], strokePoints[i]);

        if (gap < totalLength * 0.2f || gap < loopThreshold)
        {
            Debug.Log("Circle Detected!");
            CheckSelection();
        }
        else
        {
            Debug.Log("Not a circle - shape too open.");
            ClearLine();
        }
    }

    void CheckSelection()
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 p in strokePoints) center += p;
        center /= strokePoints.Count;

        Transform head = Camera.main.transform;
        Vector3 direction = (center - head.position).normalized;
        Debug.DrawRay(head.position, direction * 20f, Color.red, 2.0f);
        RaycastHit hit;
        // Raycast through the center of your drawn circle
        if (Physics.SphereCast(head.position, 0.2f, direction, out hit, 20f, animalLayer))
        {
            Debug.Log($"Selected: {hit.collider.name}");
            WordBlock wb = hit.collider.GetComponent<WordBlock>();
            if (wb != null) ShowSpelling(wb.wordID, hit.point);
        }
        
        Invoke(nameof(ClearLine), 1.5f);
    }

    void ShowSpelling(string id, Vector3 worldPos)
    {
        string cleanName = id.Replace("Sub_", "").Replace("Obj_", "").ToUpper();
        string spelledOut = string.Join("-", cleanName.ToCharArray()); 

        if (spellingText) spellingText.text = $"{cleanName}\n<size=70%>{spelledOut}</size>";
        
        if (spellingCanvas)
        {
            spellingCanvas.SetActive(true);
            spellingCanvas.transform.position = worldPos + Vector3.up * 0.5f; 
            spellingCanvas.transform.rotation = Quaternion.LookRotation(spellingCanvas.transform.position - Camera.main.transform.position);        }
    }

    void ClearLine()
    {
        lineRenderer.positionCount = 0;
    }
}