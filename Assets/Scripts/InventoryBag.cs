using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // Required for XR checks

public class InventoryBag : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Look for the WordBlock script
        WordBlock block = other.GetComponent<WordBlock>();
        
        // 2. Check if it's valid and NOT currently being held
        if (block != null && !IsBeingHeld(other.gameObject))
        {
            // 3. Add to Firebase Inventory
            // We use the 'wordID' from the WordBlock script
            InventoryManager.Instance.CollectWord(block.wordID);
            
            // 4. Visual Feedback & Cleanup
            Debug.Log($"Collected: {block.wordID}");
            
            if (block.destroyOnCollect)
            {
                Destroy(other.gameObject);
            }
        }
    }

    // Helper: Returns true if the player is currently grabbing the object
    bool IsBeingHeld(GameObject obj)
    {
        // We check the XR Interactable component
        var interactable = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        // If it's selected, it's being held
        if (interactable != null && interactable.isSelected) 
        {
            return true;
        }
        return false;
    }
}