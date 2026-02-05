using UnityEngine;
using UnityEngine.Windows.Speech; // Standard Unity Windows Speech API
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
    private string currentTargetPhrase;
    private bool isListening = false;

    void Awake()
    {
        Instance = this;
    }

    // Call this from the Mic Button
    public void ListenForPhrase(string phrase, Action<bool> onResult)
    {
        if (isListening) return;

        currentTargetPhrase = phrase.ToLower();
        currentCallback = onResult;

        // Clean up text (Remove "Sub_", "Verb_", underscores)
        string cleanPhrase = CleanText(phrase);
        Debug.Log($"[Voice] Listening for: '{cleanPhrase}'");

        // Play "Bloop" sound
        if(audioSource && listenStartClip) audioSource.PlayOneShot(listenStartClip);

        // --- WINDOWS SPEECH RECOGNITION SETUP ---
        // We tell the recognizer to ONLY listen for the correct answer (and maybe a few decoys if you wanted)
        // This makes it accurate for verifying pronunciation of specific words.
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }

        keywordRecognizer = new KeywordRecognizer(new string[] { cleanPhrase });
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
        
        isListening = true;
        
        // Timeout after 5 seconds if nothing heard
        CancelInvoke(nameof(StopListeningTimeout));
        Invoke(nameof(StopListeningTimeout), 5.0f);
    }

    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (!isListening) return;

        Debug.Log($"[Voice] Heard: {args.text} (Confidence: {args.confidence})");

        // Basic confidence check
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

        // Return result to the button
        currentCallback?.Invoke(success);
    }

    // Helper to turn "Sub_Cat" + "Verb_Eats" into "Cat Eats"
    private string CleanText(string raw)
    {
        // Remove prefixes like "Sub_", "Verb_", "Obj_"
        string s = raw.Replace("Sub_", "").Replace("Verb_", "").Replace("Obj_", "");
        // Replace underscores with spaces
        s = s.Replace("_", " ");
        return s.ToLower();
    }
}