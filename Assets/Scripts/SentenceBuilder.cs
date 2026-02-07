using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; 
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using TMPro; 

public class SentenceBuilder : MonoBehaviour
{
    [System.Serializable]
    public class ValidPair
    {
        public string subjectID; 
        public string verbID;    
    }
    private bool isVoiceVerified = false;

    [Header("Answer Key")]
    public List<ValidPair> correctSentences; 

    [Header("Sockets")]
    public XRSocketInteractor subjectSocket;
    public XRSocketInteractor verbSocket;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip errorClip;
    public AudioClip duplicateClip; 
    public GameObject successParticles;
    
    [Header("UI Feedback")]
    public TextMeshProUGUI feedbackText; 
    public float messageDuration = 3.0f; // Text disappears after 3 seconds

    private DatabaseReference dbReference;
    private string userId;
    private int sentencesFormedCount = 0;
    
    private List<string> formedHistory = new List<string>();

    void Start()
    {
        dbReference = InventoryManager.Instance != null 
            ? FirebaseDatabase.GetInstance("https://fymstudio-a8928-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference 
            : null;

        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            LoadProgress();
        }
        ClearText(); 
    }

    public void CheckSentence()
    {
        // 1. Check Phase
        if (InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.IsCollectionPhaseComplete())
            {
                Debug.Log("You must collect ALL Subject and Verb blocks first!");
                UpdateUI("Collect all blocks first!", Color.red); 
                PlaySound(errorClip); 
                return;
            }
        }
        
        // 2. Check Voice Verification
        if (!isVoiceVerified)
        {
            Debug.Log("Speak first!");
            UpdateUI("Use Microphone First!", Color.yellow); 
            
            if (VoiceManager.Instance && VoiceManager.Instance.listenStartClip) 
                VoiceManager.Instance.audioSource.PlayOneShot(VoiceManager.Instance.listenStartClip);
            return;
        }

        // 3. Check Sockets
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection)
        {
            UpdateUI("Place both blocks!", Color.red);
            PlaySound(errorClip);
            return;
        }

        string currentSubject = GetBlockID(subjectSocket);
        string currentVerb = GetBlockID(verbSocket);

        // 4. Check Duplicate
        string comboID = $"{currentSubject}_{currentVerb}"; 
        
        if (formedHistory.Contains(comboID))
        {
            UpdateUI("Already Formed!", Color.yellow);
            PlaySound(duplicateClip != null ? duplicateClip : errorClip);
            return; 
        }

        // 5. Check Answer Key
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
            UpdateUI("Incorrect Sentence.", Color.red);
            PlaySound(errorClip);
        }
    }

    void HandleSuccess(string subjectID, string verbID, string comboID)
    {
        PlaySound(successClip);
        UpdateUI("Correct!", Color.green);
        
        if (successParticles) Instantiate(successParticles, transform.position, Quaternion.identity);

        formedHistory.Add(comboID);
        sentencesFormedCount = formedHistory.Count;
        isVoiceVerified = false;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.currentSVSentenceCount = sentencesFormedCount;
            InventoryManager.Instance.UpdateProgressTracker();
            InventoryManager.Instance.MarkItemAsConsumed(subjectID);
            InventoryManager.Instance.MarkItemAsConsumed(verbID);
        }

        if (dbReference != null)
        {
            dbReference.Child("users").Child(userId).Child("formed_sentences").Child(comboID).SetValueAsync(true);
            dbReference.Child("users").Child(userId).Child("progress").Child("sentences_completed").SetValueAsync(sentencesFormedCount);
        }

        Destroy(GetBlockObject(subjectSocket));
        Destroy(GetBlockObject(verbSocket));

        if (sentencesFormedCount >= 5)
        {
            if (InventoryManager.Instance != null) InventoryManager.Instance.UnlockObjects();
        }
    }

    // --- UPDATED UI HELPER (The Magic Fix) ---
    void UpdateUI(string message, Color color)
    {
        if (feedbackText != null)
        {
            // 1. Stop any existing timer so we don't clear the new message too early
            CancelInvoke(nameof(ClearText));

            // 2. Show new message
            feedbackText.text = message;
            feedbackText.color = color;

            // 3. Start a new timer to hide it
            Invoke(nameof(ClearText), messageDuration);
        }
    }

    void ClearText()
    {
        if (feedbackText) feedbackText.text = "";
    }

    // ... (Standard Helpers below remain the same) ...
    void LoadProgress()
    {
        dbReference.Child("users").Child(userId).Child("formed_sentences").GetValueAsync().ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                formedHistory.Clear();
                foreach (DataSnapshot child in task.Result.Children) formedHistory.Add(child.Key);
                sentencesFormedCount = formedHistory.Count;
                if (ProgressHUD.Instance != null) ProgressHUD.Instance.UpdateProgress("Form Sentences", sentencesFormedCount, 5);
            }
        });
    }
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
        return $"{s} {v}"; 
    }
    public void SetVoiceVerified(bool state)
    {
        isVoiceVerified = state;
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