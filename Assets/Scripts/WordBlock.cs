using UnityEngine;
using TMPro; 

public class WordBlock : MonoBehaviour
{
    public enum WordType { Subject, Verb, Object, Modifier }

    [Header("Grammar Settings")]
    public WordType myType;      
    public string wordText;      

    [Header("Inventory Settings")]
    public string wordID;        
    public bool destroyOnCollect = true;

    [Header("Settings")]
    public bool isCollectable = true;

    void Start()
    {
        UpdateTextDisplay();
        // Auto-fix ID if empty
        if (string.IsNullOrEmpty(wordID)) wordID = gameObject.name;
    }

    void OnValidate()
    {
        UpdateTextDisplay();
        if (string.IsNullOrEmpty(wordID)) wordID = gameObject.name;
    }

    public void UpdateTextDisplay()
    {
        TMP_Text textMesh = GetComponentInChildren<TMP_Text>();
        if (textMesh != null) textMesh.text = wordText;
    }

    // --- THE FIX IS HERE ---
    // This function must be called by the XR Grab Interactable
    public void Collect()
    {
        if (!isCollectable) return;

        if (InventoryManager.Instance != null)
        {
            // We pass 'gameObject' so the manager destroys THIS specific clone
            InventoryManager.Instance.CollectWord(wordID, gameObject);
        }
        else
        {
            // Fallback: If manager is missing, just destroy self
            Destroy(gameObject); 
        }
    }
}