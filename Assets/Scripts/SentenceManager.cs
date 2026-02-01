using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactors; // Use this namespace for XRSocketInteractor

public class SentenceManager : MonoBehaviour
{
    [Header("The Drop Zones")]
    public XRSocketInteractor subjectSocket;
    public XRSocketInteractor verbSocket;
    public XRSocketInteractor objectSocket;

    [Header("Progression Objects")]
    public GameObject verbWordCollection;   // The parent of all Verb blocks (initially hidden)
    public GameObject objectWordCollection; // The parent of all Object blocks (initially hidden)

    [Header("Feedback")]
    public AudioSource successSound;

    void Start()
    {
        // 1. Hide advanced words at start (Progression System)
        if(verbWordCollection != null) verbWordCollection.SetActive(false);
        if(objectWordCollection != null) objectWordCollection.SetActive(false);

        // 2. Disable future sockets
        verbSocket.gameObject.SetActive(false);
        objectSocket.gameObject.SetActive(false);
    }

    // Call this function whenever a block is placed or removed
    public void CheckProgression()
    {
        // --- STEP 1: CHECK SUBJECT ---
        bool hasSubject = IsCorrectType(subjectSocket, WordBlock.WordType.Subject);

        if (hasSubject)
        {
            // Unlock Phase 2
            if (!verbSocket.gameObject.activeSelf)
            {
                verbSocket.gameObject.SetActive(true);
                verbWordCollection.SetActive(true); // Spawn/Show verb blocks
                PlayFeedback();
            }
        }
        else
        {
            // If they remove the subject, lock the rest again (Designing for Failure)
            verbSocket.gameObject.SetActive(false);
            objectSocket.gameObject.SetActive(false);
            return; // Stop checking
        }

        // --- STEP 2: CHECK VERB ---
        bool hasVerb = IsCorrectType(verbSocket, WordBlock.WordType.Verb);

        if (hasVerb)
        {
            // Unlock Phase 3
            if (!objectSocket.gameObject.activeSelf)
            {
                objectSocket.gameObject.SetActive(true);
                objectWordCollection.SetActive(true); // Spawn/Show object blocks
                PlayFeedback();
            }
        }
        
        // --- STEP 3: CHECK FULL SENTENCE ---
        if (hasSubject && hasVerb && IsCorrectType(objectSocket, WordBlock.WordType.Object))
        {
            Debug.Log("SENTENCE COMPLETE! YOU WIN!");
            // Add your "Level Complete" logic here
        }
    }

    // Helper function to check what is inside a socket
    bool IsCorrectType(XRSocketInteractor socket, WordBlock.WordType requiredType)
    {
        // 1. Check if socket has something
        if (!socket.hasSelection) return false;

        // 2. Get the object sitting in the socket
        UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable = socket.firstInteractableSelected;
        
        // 3. Get our custom WordBlock script from it
        WordBlock block = interactable.transform.GetComponent<WordBlock>();

        // 4. Check if it's the right type
        if (block != null && block.myType == requiredType)
        {
            return true;
        }
        return false;
    }

    void PlayFeedback()
    {
        if (successSound) successSound.Play();
    }
}