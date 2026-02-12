using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem; 
using UnityEngine.InputSystem.Controls; 
using UnityEngine.InputSystem.XR;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip listenStartClip;
    public AudioClip listenSuccessClip;
    public AudioClip listenFailClip;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private KeywordRecognizer keywordRecognizer;
#endif

    private Action<bool> currentCallback;
    private bool isListening = false;

    void Awake()
    {
        Instance = this;
    }

    // --- THE ROBUST UPDATE LOOP ---
    void Update()
    {
#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
        if (isListening)
        {
            // 1. Loop through every single input device connected to the system
            foreach (var device in InputSystem.devices)
            {
                // We only care about XR Controllers (Left or Right hand)
                if (device is XRController)
                {
                    // 2. Try to find the "trigger" control on this device
                    var triggerControl = device.GetChildControl<AxisControl>("trigger");
                    
                    // 3. Check if it exists AND is pressed down (value > 0.5f)
                    if (triggerControl != null && triggerControl.ReadValue() > 0.5f)
                    {
                        Success($"Trigger on {device.name}");
                        return;
                    }

                    // 4. Also check the 'A' or 'X' button (Primary Button)
                    var primaryControl = device.GetChildControl<ButtonControl>("primaryButton");
                    if (primaryControl != null && primaryControl.isPressed)
                    {
                        Success($"Primary Button on {device.name}");
                        return;
                    }
                }
            }
        }
#endif
    }

    void Success(string source)
    {
        Debug.Log($"[Voice] Success detected via: {source}");
        CompleteListening(true);
    }

    public void ListenForPhrase(string phrase, Action<bool> onResult)
    {
        if (isListening) return;

        currentCallback = onResult;
        Debug.Log($"[Voice] Listening for: '{phrase}'");

        if (audioSource && listenStartClip) audioSource.PlayOneShot(listenStartClip);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // WINDOWS LOGIC (PC)
        if (keywordRecognizer != null) { keywordRecognizer.Dispose(); }
        
        try 
        {
            keywordRecognizer = new KeywordRecognizer(new string[] { phrase });
            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
            keywordRecognizer.Start();
            isListening = true;
            CancelInvoke(nameof(StopListeningTimeout));
            Invoke(nameof(StopListeningTimeout), 5.0f);
        }
        catch (UnityException e)
        {
            Debug.LogError($"[Voice Error] {e.Message}");
            CompleteListening(false);
        }
#else
        // QUEST LOGIC (ANDROID)
        Debug.Log("[Voice] Quest Mode: Press TRIGGER or 'A' Button to speak.");
        isListening = true;
        CancelInvoke(nameof(StopListeningTimeout));
        Invoke(nameof(StopListeningTimeout), 5.0f);
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        if (!isListening) return;
        if (args.confidence == ConfidenceLevel.Medium || args.confidence == ConfidenceLevel.High)
        {
            CompleteListening(true);
        }
    }
#endif

    void StopListeningTimeout()
    {
        if (isListening)
        {
            Debug.Log("[Voice] Timeout - No input detected.");
            CompleteListening(false);
        }
    }

    void CompleteListening(bool success)
    {
        isListening = false;
        
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (keywordRecognizer != null && keywordRecognizer.IsRunning) keywordRecognizer.Stop();
#endif
        CancelInvoke(nameof(StopListeningTimeout));

        if (success)
        {
            if (audioSource && listenSuccessClip) audioSource.PlayOneShot(listenSuccessClip);
        }
        else
        {
            if (audioSource && listenFailClip) audioSource.PlayOneShot(listenFailClip);
        }

        currentCallback?.Invoke(success);
    }

    private string CleanText(string raw)
    {
        return raw.ToLower().Trim();
    }
}