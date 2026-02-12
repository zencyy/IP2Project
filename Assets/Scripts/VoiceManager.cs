using UnityEngine;
using UnityEngine.Windows.Speech; 
using System.Collections.Generic;
using System.Linq;
using System;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip listenStartClip;
    public AudioClip listenSuccessClip;
    public AudioClip listenFailClip;

    private KeywordRecognizer keywordRecognizer;
    private Action<bool> currentCallback;
    private bool isListening = false;

    void Awake()
    {
        Instance = this;
    }

    public void ListenForPhrase(string phrase, Action<bool> onResult)
    {
        // Prevent double-clicks
        if (isListening) return;

        currentCallback = onResult;

        // Clean up text
        string cleanPhrase = CleanText(phrase);
        Debug.Log($"[Voice] Listening for: '{cleanPhrase}'");

        if(audioSource && listenStartClip) audioSource.PlayOneShot(listenStartClip);

        // --- THE FIX: ROBUST CLEANUP ---
        // We must Dispose() the old one even if it is NOT running.
        if (keywordRecognizer != null)
        {
            if (keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }
            keywordRecognizer.Dispose();
            keywordRecognizer = null;
        }
        // -------------------------------

        try 
        {
            keywordRecognizer = new KeywordRecognizer(new string[] { cleanPhrase });
            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
            keywordRecognizer.Start();
            
            isListening = true;
            
            CancelInvoke(nameof(StopListeningTimeout));
            Invoke(nameof(StopListeningTimeout), 5.0f);
        }
        catch (UnityException e)
        {
            Debug.LogError($"[Voice Error] Failed to start recognition: {e.Message}");
            // Fail gracefully so the game doesn't break
            CompleteListening(false);
        }
    }

    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (!isListening) return;

        Debug.Log($"[Voice] Heard: {args.text} (Confidence: {args.confidence})");

        if (args.confidence == ConfidenceLevel.Medium || args.confidence == ConfidenceLevel.High)
        {
            CompleteListening(true);
        }
    }

    void StopListeningTimeout()
    {
        if (isListening)
        {
            Debug.Log("[Voice] Timeout - No speech detected.");
            CompleteListening(false);
        }
    }

    void CompleteListening(bool success)
    {
        isListening = false;
        
        // Stop logic
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
        }

        if (success)
        {
            if(audioSource && listenSuccessClip) audioSource.PlayOneShot(listenSuccessClip);
        }
        else
        {
            if(audioSource && listenFailClip) audioSource.PlayOneShot(listenFailClip);
        }

        currentCallback?.Invoke(success);
    }

    private string CleanText(string raw)
    {
        // Remove standard prefixes
        string s = raw.Replace("sub_", "").Replace("verb_", "").Replace("obj_", "");
        
        // Also remove "Object" if your block is named "Object_Wallet" by mistake
        s = s.Replace("Object_", ""); 
        
        s = s.Replace("_", " ");
        return s.ToLower().Trim();
    }
}