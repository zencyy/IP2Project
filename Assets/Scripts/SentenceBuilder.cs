using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; 
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;

public class SentenceBuilder : MonoBehaviour
{
    // --- 1. DEFINE THE ANSWER KEY STRUCTURE ---
    [System.Serializable]
    public class ValidPair
    {
        public string subjectID; // e.g. "Sub_Cat"
        public string verbID;    // e.g. "Verb_Meows"
    }
    private bool isVoiceVerified = false;

    [Header("Answer Key")]
    [Tooltip("List all correct sentences here.")]
    public List<ValidPair> correctSentences; 

    [Header("Sockets")]
    public XRSocketInteractor subjectSocket;
    public XRSocketInteractor verbSocket;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip errorClip;
    public AudioClip duplicateClip; // Optional: Sound for "You already did this"
    public GameObject successParticles;

    private DatabaseReference dbReference;
    private string userId;
    private int sentencesFormedCount = 0;
    
    // Track history locally so we don't reward the same sentence twice
    private List<string> formedHistory = new List<string>();

    void Start()
    {
        // Use your specific URL
        dbReference = InventoryManager.Instance != null 
            ? FirebaseDatabase.GetInstance("https://fymstudio-a8928-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference 
            : null;

        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            LoadProgress();
        }
    }

    // --- 2. THE CHECK FUNCTION ---
    public void CheckSentence()
    {
        // --- NEW CHECK: PREVENT EARLY SENTENCE FORMATION ---
        if (InventoryManager.Instance != null)
        {
            // FIX 1: Use the new helper name 'IsCollectionPhaseComplete' 
            // OR ensure 'AreAllBlocksCollected' exists in your Manager.
            // I will use 'IsCollectionPhaseComplete' as it matches the Phase 5 update.
            if (!InventoryManager.Instance.IsCollectionPhaseComplete())
            {
                Debug.Log("You must collect ALL Subject and Verb blocks first!");
                PlaySound(errorClip); 
                return;
            }
        }
        if (!isVoiceVerified)
    {
        Debug.Log("You must speak the sentence to verify pronunciation first!");
        // Optional: Play a "Locked" sound or "Please Speak" prompt
        if (VoiceManager.Instance && VoiceManager.Instance.listenStartClip) 
            VoiceManager.Instance.audioSource.PlayOneShot(VoiceManager.Instance.listenStartClip);
        return;
    }
        // Standard Checks
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection)
        {
            PlaySound(errorClip);
            return;
        }

        string currentSubject = GetBlockID(subjectSocket);
        string currentVerb = GetBlockID(verbSocket);

        // A. Check if this specific combo was ALREADY formed
        string comboID = $"{currentSubject}_{currentVerb}"; 
        
        if (formedHistory.Contains(comboID))
        {
            Debug.Log("You already formed this sentence!");
            PlaySound(duplicateClip != null ? duplicateClip : errorClip);
            return; 
        }

        // B. Check Answer Key
        bool isMatch = false;
        foreach (ValidPair pair in correctSentences)
        {
            if (pair.subjectID == currentSubject && pair.verbID == currentVerb)
            {
                isMatch = true;
                break;
            }
        }

        if (isMatch)
        {
            HandleSuccess(currentSubject, currentVerb, comboID);
        }
        else
        {
            Debug.Log("Grammatically wrong or nonsense!");
            PlaySound(errorClip);
        }
    }

    // --- 3. SUCCESS LOGIC (Saves to DB) ---
   void HandleSuccess(string subjectID, string verbID, string comboID)
    {
        PlaySound(successClip);
        if (successParticles) Instantiate(successParticles, transform.position, Quaternion.identity);

        // 1. Update Local State
        formedHistory.Add(comboID);
        sentencesFormedCount = formedHistory.Count;

        // 2. Sync with InventoryManager
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.currentSVSentenceCount = sentencesFormedCount;
            InventoryManager.Instance.UpdateProgressTracker();
            
            // --- NEW: PERMANENTLY CONSUME ITEMS ---
            InventoryManager.Instance.MarkItemAsConsumed(subjectID);
            InventoryManager.Instance.MarkItemAsConsumed(verbID);
            // --------------------------------------
        }

        // 3. Save to Firebase (Sentence Progress)
        if (dbReference != null)
        {
            dbReference.Child("users").Child(userId).Child("formed_sentences").Child(comboID).SetValueAsync(true);
            dbReference.Child("users").Child(userId).Child("progress").Child("sentences_completed").SetValueAsync(sentencesFormedCount);
        }

        // 4. Destroy Visual Objects
        Destroy(GetBlockObject(subjectSocket));
        Destroy(GetBlockObject(verbSocket));

        // 5. Unlock Next Phase
        if (sentencesFormedCount >= 5)
        {
            Debug.Log("SV Phase Complete! Unlocking Objects...");
            if (InventoryManager.Instance != null) InventoryManager.Instance.UnlockObjects();
        }
    }

    // --- 4. LOAD PREVIOUS PROGRESS ---
    void LoadProgress()
    {
        dbReference.Child("users").Child(userId).Child("formed_sentences").GetValueAsync().ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DataSnapshot snapshot = task.Result;
                formedHistory.Clear();

                foreach (DataSnapshot child in snapshot.Children)
                {
                    formedHistory.Add(child.Key);
                }

                sentencesFormedCount = formedHistory.Count;
                
                // Update HUD immediately
                if (ProgressHUD.Instance != null) 
                    ProgressHUD.Instance.UpdateProgress("Form Sentences", sentencesFormedCount, 5);
            }
        });
    }

    // --- HELPERS ---
    string GetBlockID(XRSocketInteractor socket)
    {
        if (!socket.hasSelection) return "";
        WordBlock wb = socket.firstInteractableSelected.transform.GetComponent<WordBlock>();
        return wb != null ? wb.wordID : "";
    }

    GameObject GetBlockObject(XRSocketInteractor socket)
    {
        return socket.firstInteractableSelected.transform.gameObject;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    public string GetCurrentSentenceString()
    {
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection) return "";
        string s = GetBlockID(subjectSocket);
        string v = GetBlockID(verbSocket);
        return $"{s} {v}"; // Returns "Sub_Cat Verb_Eats"
    }

    // Used by MicButton to unlock the station
    public void SetVoiceVerified(bool state)
    {
        isVoiceVerified = state;
        Debug.Log($"SV Station Verified: {state}");
    }

    public bool IsCurrentSentenceValid()
    {
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection) return false;

        string s = GetBlockID(subjectSocket);
        string v = GetBlockID(verbSocket);

        foreach (ValidPair pair in correctSentences)
        {
            if (pair.subjectID == s && pair.verbID == v) return true;
        }
        return false;
    }
}