using UnityEngine;
using TMPro; // Needed for text updates

public class WordBlock : MonoBehaviour
{
    public enum WordType { Subject, Verb, Object, Modifier }

    [Header("Grammar Settings")]
    public WordType myType;      // e.g., Subject
    public string wordText;      // e.g., "The Cat"

    [Header("Inventory Settings")]
    // CRITICAL: This must match your Prefab Name exactly! (e.g. "Block_Cat")
    public string wordID;        
    public bool destroyOnCollect = true; // Should it disappear when dropped in bag?

    // Auto-update text in Editor
    void OnValidate()
    {
        var textMesh = GetComponentInChildren<TMP_Text>();
        if (textMesh != null) textMesh.text = wordText;
        
        // Auto-set ID to the GameObject name if empty (optional helper)
        if (string.IsNullOrEmpty(wordID)) wordID = gameObject.name;
    }
}