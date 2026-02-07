using UnityEngine;
using TMPro; // Standard TextMeshPro namespace

public class MicButton : MonoBehaviour
{
    [Header("References")]
    public SentenceBuilder builderSV;
    public SentenceBuilderSVO builderSVO;
    
    [Header("Visuals")]
    public Material lockedMat;
    public Material unlockedMat;
    public Renderer statusLight;
    
    [Header("UI Feedback")]
    public TextMeshProUGUI feedbackText; 
    public float messageDuration = 3.0f; // How long text stays visible
    
    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip errorClip; 

    private bool isSVO = false;

    void Start()
    {
        if (builderSV == null && builderSVO == null)
        {
            builderSV = GetComponentInParent<SentenceBuilder>();
            builderSVO = GetComponentInParent<SentenceBuilderSVO>();
        }
        isSVO = (builderSVO != null);

        // Clear text on start
        ClearText();
    }

    public void OnPress()
    {
        // Cancel any existing clear timer so messages don't disappear too fast
        CancelInvoke(nameof(ClearText));

        // 1. CHECK VALIDITY FIRST
        bool isValidLogic = false;
        string constructedSentence = "";

        if (isSVO && builderSVO != null)
        {
            isValidLogic = builderSVO.IsCurrentSentenceValid(); 
            constructedSentence = builderSVO.GetCurrentSentenceString();
        }
        else if (!isSVO && builderSV != null)
        {
            isValidLogic = builderSV.IsCurrentSentenceValid(); 
            constructedSentence = builderSV.GetCurrentSentenceString();
        }

        // 2. REJECT IF WRONG BLOCKS
        if (!isValidLogic)
        {
            Debug.Log("Mic: Blocks are arranged incorrectly.");
            if (audioSource && errorClip) audioSource.PlayOneShot(errorClip);
            
            UpdateUI("Incorrect Sentence Order!", Color.red);
            Invoke(nameof(ClearText), messageDuration); // Disappear after 3s
            return; 
        }

        // 3. IF BLOCKS CORRECT -> START LISTENING
        UpdateUI("Listening...", Color.yellow);
        
        VoiceManager.Instance.ListenForPhrase(constructedSentence, (bool success) => 
        {
            if (success)
            {
                Debug.Log("Pronunciation Verified!");
                UpdateUI("Correct! Press Submit.", Color.green);
                UnlockSubmitButton();
                Invoke(nameof(ClearText), messageDuration); // Disappear after 3s
            }
            else
            {
                Debug.Log("Pronunciation Incorrect. Try again.");
                UpdateUI("Try Again...", Color.red);
                Invoke(nameof(ClearText), messageDuration); // Disappear after 3s
            }
        });
    }

    void UnlockSubmitButton()
    {
        if (statusLight) statusLight.material = unlockedMat;
        if (isSVO) builderSVO.SetVoiceVerified(true);
        else builderSV.SetVoiceVerified(true);
    }

    void UpdateUI(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }
    }

    // --- THE FIX ---
    void ClearText()
    {
        if (feedbackText) feedbackText.text = "";
    }
}