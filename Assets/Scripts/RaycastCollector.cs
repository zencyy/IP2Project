using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; // Needed for XRRayInteractor in newer versions
using System.Collections.Generic;

public class RaycastCollector : MonoBehaviour
{
    [Header("Settings")]
    public XRRayInteractor rayInteractor; // Drag the Ray Interactor here
    public InputHelpers.Button collectButton = InputHelpers.Button.SecondaryButton; // 'Y' or 'B' button
    
    // Tracks button state to prevent accidental double-clicks
    private bool buttonWasPressed = false;
    private InputDevice targetDevice;

    void Start()
    {
        // Auto-find interactor if not assigned
        if (rayInteractor == null) rayInteractor = GetComponent<XRRayInteractor>();
        
        // Setup Input (Right Hand by default)
        InitializeInput();
    }

    void InitializeInput()
    {
        // We assume this script is on the Right Hand. Change to LeftHand if needed.
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) targetDevice = devices[0];
    }

    void Update()
    {
        // 1. Keyboard Fallback (For Simulator testing)
        // Press 'C' on keyboard to collect
        if (Input.GetKeyDown(KeyCode.C))
        {
            TryCollect();
        }

        // 2. VR Controller Input
        if (!targetDevice.isValid) InitializeInput();

        bool isPressed = false;
        targetDevice.IsPressed(collectButton, out isPressed);

        if (isPressed && !buttonWasPressed)
        {
            TryCollect();
            buttonWasPressed = true;
        }
        else if (!isPressed)
        {
            buttonWasPressed = false;
        }
    }

    void TryCollect()
    {
        // Check if the ray is hitting anything valid
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            // Try to find the WordBlock script on the object we hit
            // We use GetComponentInParent in case we hit a child text object
            WordBlock block = hit.collider.GetComponentInParent<WordBlock>();

            if (block != null)
            {
                // Send to Inventory Manager
                InventoryManager.Instance.CollectWord(block.wordID);
                Debug.Log($"Collected: {block.wordID}");

                // Destroy the object
                Destroy(block.gameObject);
            }
        }
    }
}