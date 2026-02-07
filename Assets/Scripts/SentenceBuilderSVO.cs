using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; 
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using TMPro; 

public class SentenceBuilderSVO : MonoBehaviour
{
    [System.Serializable]
    public class ValidSVO
    {
        public string subjectID; 
        public string verbID;    
        public string objectID;  
    }

    [Header("Answer Key")]
    public List<ValidSVO> correctSentences; 
    
    private bool isVoiceVerified = false;

    [Header("Sockets")]
    public XRSocketInteractor subjectSocket;
    public XRSocketInteractor verbSocket;
    public XRSocketInteractor objectSocket;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip successClip;
    public AudioClip errorClip;
    public GameObject successParticles;

    [Header("UI Feedback")]
    public TextMeshProUGUI feedbackText; 
    public float messageDuration = 3.0f; // Text disappears after 3 seconds

    private DatabaseReference dbReference;
    private string userId;
    private int svoFormedCount = 0;
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
        // 1. Check Collection Phase
        if (InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.IsCollectionPhaseComplete())
            {
                UpdateUI("Collect items first!", Color.red);
                PlaySound(errorClip);
                return;
            }
        }

        // 2. Check Sockets
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection || !objectSocket.hasSelection)
        {
            UpdateUI("Place all 3 blocks!", Color.red);
            PlaySound(errorClip);
            return;
        }

        // 3. Check Game Phase
        if (InventoryManager.Instance != null && !InventoryManager.Instance.objectsUnlocked)
        {
            UpdateUI("SVO Locked.", Color.red);
            return;
        }

        // 4. Check Voice Verification
        if (!isVoiceVerified)
        {
            UpdateUI("Use Microphone First!", Color.yellow);
            if (VoiceManager.Instance != null && VoiceManager.Instance.listenStartClip != null) 
                VoiceManager.Instance.audioSource.PlayOneShot(VoiceManager.Instance.listenStartClip);
            else 
                PlaySound(errorClip);
            return;
        }
        
        string s = GetID(subjectSocket);
        string v = GetID(verbSocket);
        string o = GetID(objectSocket);

        // 5. Check Duplicate
        string comboID = $"{s}_{v}_{o}";
        if (formedHistory.Contains(comboID))
        {
            UpdateUI("Already Formed!", Color.yellow);
            PlaySound(errorClip); 
            return;
        }

        // 6. Check Answer Key
        bool isMatch = false;
        foreach (ValidSVO key in correctSentences)
        {
            if (key.subjectID == s && key.verbID == v && key.objectID == o)
            {
                isMatch = true;
                break;
            }
        }

        // 7. Result
        if (isMatch) HandleSuccess(s, v, o, comboID);
        else 
        {
            UpdateUI("Incorrect Sentence.", Color.red);
            PlaySound(errorClip);
        }
    }

    void HandleSuccess(string s, string v, string o, string comboID)
    {
        PlaySound(successClip);
        UpdateUI("Correct!", Color.green);
        
        if (successParticles) Instantiate(successParticles, transform.position, Quaternion.identity);

        formedHistory.Add(comboID);
        svoFormedCount = formedHistory.Count;
        isVoiceVerified = false; 

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.currentSVOSentenceCount = svoFormedCount;
            InventoryManager.Instance.UpdateProgressTracker();
            InventoryManager.Instance.MarkItemAsConsumed(s);
            InventoryManager.Instance.MarkItemAsConsumed(v);
            InventoryManager.Instance.MarkItemAsConsumed(o);
        }

        if (dbReference != null)
        {
            dbReference.Child("users").Child(userId).Child("formed_sentences_svo").Child(comboID).SetValueAsync(true);
            dbReference.Child("users").Child(userId).Child("progress").Child("svo_sentences_completed").SetValueAsync(svoFormedCount);
        }

        Destroy(GetObj(subjectSocket));
        Destroy(GetObj(verbSocket));
        Destroy(GetObj(objectSocket));
    }

    // --- UPDATED UI HELPER ---
    void UpdateUI(string message, Color color)
    {
        if (feedbackText != null)
        {
            // Stop old timer
            CancelInvoke(nameof(ClearText));

            // Show message
            feedbackText.text = message;
            feedbackText.color = color;

            // Start new timer
            Invoke(nameof(ClearText), messageDuration);
        }
    }

    void ClearText()
    {
        if (feedbackText) feedbackText.text = "";
    }

    // ... (Helpers remain same) ...
    void LoadProgress()
    {
        dbReference.Child("users").Child(userId).Child("formed_sentences_svo").GetValueAsync().ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                formedHistory.Clear();
                foreach (DataSnapshot child in task.Result.Children) formedHistory.Add(child.Key);
                svoFormedCount = formedHistory.Count;
                if (InventoryManager.Instance != null) InventoryManager.Instance.currentSVOSentenceCount = svoFormedCount;
            }
        });
    }
    string GetID(XRSocketInteractor socket)
    {
        return socket.firstInteractableSelected.transform.GetComponent<WordBlock>().wordID;
    }
    GameObject GetObj(XRSocketInteractor socket)
    {
        return socket.firstInteractableSelected.transform.gameObject;
    }
    void PlaySound(AudioClip clip) { if (audioSource && clip) audioSource.PlayOneShot(clip); }
    public string GetCurrentSentenceString()
    {
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection || !objectSocket.hasSelection) return "";
        return $"{GetID(subjectSocket)} {GetID(verbSocket)} {GetID(objectSocket)}";
    }
    public void SetVoiceVerified(bool state)
    {
        isVoiceVerified = state;
    }
    public bool IsCurrentSentenceValid()
    {
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection || !objectSocket.hasSelection) return false;
        string s = GetID(subjectSocket);
        string v = GetID(verbSocket);
        string o = GetID(objectSocket);
        foreach (ValidSVO key in correctSentences)
        {
            if (key.subjectID == s && key.verbID == v && key.objectID == o) return true;
        }
        return false;
    }
}