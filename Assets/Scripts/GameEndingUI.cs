using UnityEngine;
using TMPro; // If you want to change text dynamically

public class GameEndingUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject endingPanel; // Drag your 'Congratulations' Panel here
    public TMP_Text messageText;   // Optional: To customize the message

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip victorySound;

    void Start()
    {
        // Ensure the panel is hidden when the game starts
        if (endingPanel != null) endingPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        Debug.Log("Game Complete! Showing Ending UI.");
        
        if (endingPanel != null) 
        {
            endingPanel.SetActive(true);
        }

        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }
    }
}