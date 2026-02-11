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

    // --- NEW: AUDIO SETTINGS ---
    [Header("Audio")]
    public AudioClip collectSound; // Drag your sound clip here in the Inspector
    [Range(0f, 1f)] public float soundVolume = 1.0f; // Control volume (0 to 1)

    void Start()
    {
        UpdateTextDisplay();
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

    public void Collect()
    {
        if (!isCollectable) return;

        // --- NEW: PLAY SOUND ---
        if (collectSound != null)
        {
            // We use PlayClipAtPoint because this object is about to be destroyed.
            // This creates a temporary object at this position to play the audio.
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }
        // -----------------------

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