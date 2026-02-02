using UnityEngine;
using UnityEngine.XR; // Required for VR Input
using System.Collections.Generic;

public class InventoryController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public GameObject inventoryCanvas; // Drag your World Space Canvas here
    public Transform headCamera;       // Drag your Main Camera here

    [Header("Settings")]
    public float distanceFromFace = 1.0f; // How far away the menu opens

    private InputDevice targetDevice;
    private bool buttonWasPressed = false; // To prevent rapid flickering

    void Start()
    {
        // Start with inventory closed
        if (inventoryCanvas != null)
            inventoryCanvas.SetActive(false);

        InitializeInput();
    }

    void InitializeInput()
    {
        // We will listen to the Left Controller for the menu button
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);

        if (devices.Count > 0)
        {
            targetDevice = devices[0];
        }
    }

    void Update()
    {
        // --- 1. KEYBOARD FALLBACK (FOR EASY TESTING) ---
        // Press 'I' on your keyboard to toggle inventory immediately
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        // --- 2. VR CONTROLLER LOGIC ---
        // Re-check for device if we lost it
        if (!targetDevice.isValid) InitializeInput();

        bool isPressed = false;
        if (targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool buttonValue))
        {
            isPressed = buttonValue;
        }

        if (isPressed && !buttonWasPressed)
        {
            ToggleInventory();
            buttonWasPressed = true;
        }
        else if (!isPressed)
        {
            buttonWasPressed = false;
        }
    }

   void ToggleInventory()
    {
        bool isActive = !inventoryCanvas.activeSelf; 
        inventoryCanvas.SetActive(isActive);

        if (isActive)
        {
            PositionMenu();

            // REMOVED: InventoryManager.Instance.LoadUserData(); 
            // We trust the local list now. It is faster and prevents "ghost" items.
            
            // OPTIONAL: Just force the UI to redraw what we already know we have
            if (InventoryManager.Instance != null)
            {
                 InventoryManager.Instance.RefreshUI();
            }
        }
    }

    void PositionMenu()
    {
        // Move canvas in front of camera
        Vector3 spawnPos = headCamera.position + (headCamera.forward * distanceFromFace);
        
        // Keep height steady (optional: keeps menu at eye level but flat)
        spawnPos.y = headCamera.position.y; 

        inventoryCanvas.transform.position = spawnPos;
        
        // Make menu face the player
        inventoryCanvas.transform.LookAt(headCamera.position);
        inventoryCanvas.transform.Rotate(0, 180, 0); // Correct rotation so text isn't backwards
    }
}