using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; 
using System.Collections.Generic;

public class RaycastCollector : MonoBehaviour
{
    [Header("Settings")]
    public XRRayInteractor rayInteractor; 
    public InputHelpers.Button collectButton = InputHelpers.Button.SecondaryButton; 
    
    private bool buttonWasPressed = false;
    private InputDevice targetDevice;

    void Start()
    {
        if (rayInteractor == null) rayInteractor = GetComponent<XRRayInteractor>();
        InitializeInput();
    }

    void InitializeInput()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices); // Change to LeftHand if needed
        if (devices.Count > 0) targetDevice = devices[0];
    }

    void Update()
    {
        // 1. Keyboard Fallback (Press 'C')
        if (Input.GetKeyDown(KeyCode.C))
        {
            TryCollect(); // This will now collect whatever the ray is pointing at
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
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            WordBlock block = hit.collider.GetComponentInParent<WordBlock>();

            if (block != null)
            {
                // --- THE FIX IS HERE ---
                
                // OLD WAY (Bypassed the sound):
                // InventoryManager.Instance.CollectWord(block.wordID, block.gameObject);

                // NEW WAY (Runs the code in WordBlock.cs, which plays the sound):
                block.Collect(); 
                
                // -----------------------
            }
        }
    }
}