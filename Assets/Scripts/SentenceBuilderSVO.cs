using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors; 
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;

public class SentenceBuilderSVO : MonoBehaviour
{
    [System.Serializable]
    public class ValidSVO
    {
        public string subjectID; // Sub_Cat
        public string verbID;    // Verb_Eats
        public string objectID;  // Obj_Fish
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
    }

    public void CheckSentence()
    {
        // 1. NEW: Check if Collection is Complete (Must collect all objects first!)
        if (InventoryManager.Instance != null)
        {
            if (!InventoryManager.Instance.IsCollectionPhaseComplete())
            {
                Debug.Log("You must collect ALL Object blocks from the shelf first!");
                PlaySound(errorClip);
                return;
            }
        }

        // 2. Check if Sockets Full
        if (!subjectSocket.hasSelection || !verbSocket.hasSelection || !objectSocket.hasSelection)
        {
            PlaySound(errorClip);
            return;
        }

        // 3. Check Phase (Must have unlocked Objects first!)
        if (InventoryManager.Instance != null && !InventoryManager.Instance.objectsUnlocked)
        {
            Debug.Log("You haven't unlocked SVO sentences yet!");
            return;
        }

        string s = GetID(subjectSocket);
        string v = GetID(verbSocket);
        string o = GetID(objectSocket);

        // 4. Check Duplicate
        string comboID = $"{s}_{v}_{o}";
        if (formedHistory.Contains(comboID))
        {
            Debug.Log("Already formed this!");
            PlaySound(errorClip); 
            return;
        }

        // 5. Check Key
        bool isMatch = false;
        foreach (ValidSVO key in correctSentences)
        {
            if (key.subjectID == s && key.verbID == v && key.objectID == o)
            {
                isMatch = true;
                break;
            }
        }

        // 6. Success - Pass IDs to handle consumption
        if (isMatch) HandleSuccess(s, v, o, comboID);
        else PlaySound(errorClip);
    }

    // UPDATED: Now accepts s, v, o to delete them
    void HandleSuccess(string s, string v, string o, string comboID)
    {
        PlaySound(successClip);
        if (successParticles) Instantiate(successParticles, transform.position, Quaternion.identity);

        formedHistory.Add(comboID);
        svoFormedCount = formedHistory.Count;

        // Update Manager & Consume Items
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.currentSVOSentenceCount = svoFormedCount;
            InventoryManager.Instance.UpdateProgressTracker();

            // --- THE FIX: PERMANENTLY CONSUME ITEMS ---
            InventoryManager.Instance.MarkItemAsConsumed(s);
            InventoryManager.Instance.MarkItemAsConsumed(v);
            InventoryManager.Instance.MarkItemAsConsumed(o);
            // ------------------------------------------
        }

        // Save DB
        if (dbReference != null)
        {
            dbReference.Child("users").Child(userId).Child("formed_sentences_svo").Child(comboID).SetValueAsync(true);
            dbReference.Child("users").Child(userId).Child("progress").Child("svo_sentences_completed").SetValueAsync(svoFormedCount);
        }

        // Destroy Visuals
        Destroy(GetObj(subjectSocket));
        Destroy(GetObj(verbSocket));
        Destroy(GetObj(objectSocket));

        if (svoFormedCount >= 5) Debug.Log("SVO PHASE COMPLETE!");
    }

    void LoadProgress()
    {
        dbReference.Child("users").Child(userId).Child("formed_sentences_svo").GetValueAsync().ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                formedHistory.Clear();
                foreach (DataSnapshot child in task.Result.Children) formedHistory.Add(child.Key);
                svoFormedCount = formedHistory.Count;
                
                // Optional: Update HUD on load
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