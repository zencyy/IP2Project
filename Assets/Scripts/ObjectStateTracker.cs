using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // For newer XR versions

public class ObjectStateTracker : MonoBehaviour
{
    private WordBlock wordBlock;
    private XRGrabInteractable grabInteractable;

    void Start()
    {
        wordBlock = GetComponent<WordBlock>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Listen for the "Drop" event (Select Exited)
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.AddListener(OnDrop);
        }
    }

    void OnDrop(SelectExitEventArgs args)
    {
        // When user lets go, save position to Firebase
        if (InventoryManager.Instance != null && wordBlock != null)
        {
            InventoryManager.Instance.SaveBlockLocation(wordBlock.wordID, transform.position, transform.rotation);
        }
    }

    // Cleanup listeners when destroyed
    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnDrop);
        }
    }
}