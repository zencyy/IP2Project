using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;

public class GestureCaster : MonoBehaviour
{
    [Header("Setup")]
    public TrailRenderer trailRenderer; 
    public Transform handIndexTip;
    // Drag your Main Camera here for mouse testing
    public Camera debugCamera; 

    [Header("Settings")]
    public bool enableMouseDebug = true; // Check this to test without headset!
    public float pointDistanceDelta = 0.05f;

    [Header("Recognition Tuning")]
    public float lineThreshold = 0.9f;   
    public float circleThreshold = 0.2f; 

    [Header("Events")]
    public UnityEvent OnLineDrawn;   
    public UnityEvent OnCircleDrawn; 

    private List<Vector3> points = new List<Vector3>();
    private bool isDrawing = false;
    private InputDevice targetDevice;

    void Start()
    {
        // Auto-find camera if not assigned
        if (debugCamera == null) debugCamera = Camera.main;

        // XR Device Setup (Kept for when you build to VR)
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) targetDevice = devices[0];
    }

    void Update()
    {
        bool isPinching = false;
        Vector3 currentHandPosition = handIndexTip.position;

        // --- MOUSE DEBUG LOGIC ---
        if (enableMouseDebug)
        {
            // Simulate Pinch with Left Click
            isPinching = Input.GetMouseButton(0);

            // Move the "Tip" to follow the mouse at a fixed distance (1 meter in front of cam)
            Ray ray = debugCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 mouseWorldPos = ray.GetPoint(1.0f); // 1.0f is depth
            
            // Force the hand tip object to move to mouse position
            handIndexTip.position = mouseWorldPos;
            currentHandPosition = mouseWorldPos;
        }
        // --- REAL VR LOGIC ---
        else 
        {
            if (!targetDevice.isValid) InitializeXR();
            
            if (targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerValue))
            {
                isPinching = triggerValue;
            }
        }

        // --- DRAWING STATE MACHINE ---
        if (isPinching && !isDrawing)
        {
            StartDrawing();
        }
        else if (isPinching && isDrawing)
        {
            UpdateDrawing(currentHandPosition);
        }
        else if (!isPinching && isDrawing)
        {
            EndDrawing();
        }
    }

    void InitializeXR()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) targetDevice = devices[0];
    }

    void StartDrawing()
    {
        isDrawing = true;
        points.Clear();
        trailRenderer.Clear();
        trailRenderer.emitting = true;
        // Add the very first point
        points.Add(handIndexTip.position); 
    }

    void UpdateDrawing(Vector3 currentPos)
    {
        // Trail follows the tip automatically if parented, 
        // but if not parented, update it here:
        trailRenderer.transform.position = currentPos;

        // Add point if we moved far enough
        if (points.Count > 0 && Vector3.Distance(points[points.Count - 1], currentPos) > pointDistanceDelta)
        {
            points.Add(currentPos);
        }
    }

    void EndDrawing()
    {
        isDrawing = false;
        trailRenderer.emitting = false;
        AnalyzeShape();
    }

    // --- MATH ---
    void AnalyzeShape()
    {
        if (points.Count < 5) return; 

        float totalPathLength = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            totalPathLength += Vector3.Distance(points[i - 1], points[i]);
        }

        float displacement = Vector3.Distance(points[0], points[points.Count - 1]);
        float ratio = displacement / totalPathLength;

        Debug.Log($"Shape Ratio: {ratio}");

        if (ratio > lineThreshold)
        {
            Debug.Log("Line Detected!");
            OnLineDrawn.Invoke();
        }
        else if (ratio < circleThreshold && totalPathLength > 0.3f) 
        {
            Debug.Log("Circle Detected!");
            OnCircleDrawn.Invoke();
        }
    }
}