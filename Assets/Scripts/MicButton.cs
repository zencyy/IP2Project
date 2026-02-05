using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MicButton : MonoBehaviour
{
    [Header("References")]
    public SentenceBuilder builderSV;
    public SentenceBuilderSVO builderSVO;
    
    [Header("Visuals")]
    public Material lockedMat;
    public Material unlockedMat;
    public Renderer statusLight;
    
    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip errorClip; // Play when blocks are wrong

    private bool isSVO = false;

    void Start()
    {
        if (builderSV == null && builderSVO == null)
        {
            builderSV = GetComponentInParent<SentenceBuilder>();
            builderSVO = GetComponentInParent<SentenceBuilderSVO>();
        }
        isSVO = (builderSVO != null);
    }

    public void OnPress()
    {
        // 1. CHECK VALIDITY FIRST
        bool isValidLogic = false;
        string constructedSentence = "";

        if (isSVO && builderSVO != null)
        {
            isValidLogic = builderSVO.IsCurrentSentenceValid(); // Check Answer Key
            constructedSentence = builderSVO.GetCurrentSentenceString();
        }
        else if (!isSVO && builderSV != null)
        {
            isValidLogic = builderSV.IsCurrentSentenceValid(); // Check Answer Key
            constructedSentence = builderSV.GetCurrentSentenceString();
        }

        // 2. REJECT IF WRONG BLOCKS
        if (!isValidLogic)
        {
            Debug.Log("Mic: Blocks are arranged incorrectly. Cannot verify.");
            if (audioSource && errorClip) audioSource.PlayOneShot(errorClip);
            return; // Stop here. Don't listen.
        }

        // 3. IF BLOCKS CORRECT -> START LISTENING
        VoiceManager.Instance.ListenForPhrase(constructedSentence, (bool success) => 
        {
            if (success)
            {
                Debug.Log("Pronunciation Verified!");
                UnlockSubmitButton();
            }
            else
            {
                Debug.Log("Pronunciation Incorrect. Try again.");
            }
        });
    }

    void UnlockSubmitButton()
    {
        if (statusLight) statusLight.material = unlockedMat;
        if (isSVO) builderSVO.SetVoiceVerified(true);
        else builderSV.SetVoiceVerified(true);
    }
}