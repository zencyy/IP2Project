using UnityEngine;
using TMPro; // Required for TextMeshPro

public class WordBlock : MonoBehaviour
{
    public enum WordType { Subject, Verb, Object, Modifier }

    [Header("Grammar Settings")]
    public WordType myType;      
    public string wordText;      // e.g. "They"

    [Header("Inventory Settings")]
    public string wordID;        // e.g. "Sub_They"
    public bool destroyOnCollect = true;

    // --- ADD THIS START FUNCTION ---
    void Start()
    {
        UpdateTextDisplay();
    }

    // This ensures text updates when you type in the Inspector (Editor only)
    void OnValidate()
    {
        UpdateTextDisplay();
        if (string.IsNullOrEmpty(wordID)) wordID = gameObject.name;
    }

    // Shared function to update the visual text
    public void UpdateTextDisplay()
    {
        // Find the TextMeshPro component on this object or its children
        TMP_Text textMesh = GetComponentInChildren<TMP_Text>();
        
        if (textMesh != null)
        {
            textMesh.text = wordText;
        }
    }
}