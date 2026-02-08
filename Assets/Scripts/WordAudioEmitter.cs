using UnityEngine;

[RequireComponent(typeof(WordBlock))]
public class WordAudioEmitter : MonoBehaviour
{
    private AudioSource audioSource;
    private WordBlock wordBlock;

    [Header("3D Audio Settings")]
    [Tooltip("How close (in meters) to hear full volume")]
    public float minDistance = 1.0f; 
    
    [Tooltip("How far (in meters) before sound stops completely")]
    public float maxDistance = 15.0f; 

    void Start()
    {
        wordBlock = GetComponent<WordBlock>();
        
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsItemKnown(wordBlock.wordID))
        {
            Destroy(this); // Remove this script so no sound plays
            return;
        }
        // 1. Setup Audio Source dynamically
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true; // Continuous cue
        audioSource.spatialBlend = 1.0f; // 1.0 = Fully 3D (0.0 is 2D)
        
        // 2. Setup Distance (Hot/Cold Mechanic)
        audioSource.rolloffMode = AudioRolloffMode.Linear; // Linear is easier to understand for games
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        
        // 3. Get the correct clip from InventoryManager
        if (InventoryManager.Instance != null)
        {
            AssignClip();
        }
        
        // 4. Play
        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    void AssignClip()
    {
        string id = wordBlock.wordID;

        // Determine type based on ID prefix
        if (id.StartsWith("sub_"))
        {
            audioSource.clip = InventoryManager.Instance.audioSubjectLoop;
        }
        else if (id.StartsWith("verb_"))
        {
            audioSource.clip = InventoryManager.Instance.audioVerbLoop;
        }
        else if (id.StartsWith("obj_"))
        {
            audioSource.clip = InventoryManager.Instance.audioObjectLoop;
        }
    }
}