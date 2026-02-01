using UnityEngine;

public class WordBlock : MonoBehaviour
{
    public enum WordType { Subject, Verb, Object, Modifier }

    [Header("Word Settings")]
    public WordType myType;      // Select this in Inspector (e.g., Subject)
    public string wordText;      // The actual text (e.g., "The Cat")
    
    // Optional: Auto-update the text label
    void OnValidate()
    {
        var textMesh = GetComponentInChildren<TMPro.TMP_Text>();
        if (textMesh != null) textMesh.text = wordText;
    }
}