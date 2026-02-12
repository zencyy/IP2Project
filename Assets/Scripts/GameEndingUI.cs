using UnityEngine;
using TMPro;

public class GameEndingUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject endingPanel; 
    public TMP_Text messageText;   

    [Header("VR Settings")]
    public Transform playerHead;   
    public float distanceFromFace = 2.0f; 
    public float heightOffset = -0.2f;    

    [Header("Audio Control")]
    public AudioSource backgroundMusic; // Drag your "BackgroundMusic" object here
    public AudioSource sfxSource;       // Drag an AudioSource for the Victory Sound
    public AudioClip victorySound;      // Drag your Victory/Ending clip here

    void Start()
    {
        // 1. Auto-find camera if missing
        if (playerHead == null && Camera.main != null)
            playerHead = Camera.main.transform;

        // 2. Hide UI at start
        if (endingPanel != null) endingPanel.SetActive(false);
    }

    public void ShowVictory()
    {
        Debug.Log("Game Complete! Switching Audio & Showing UI.");
        
        // --- A. HANDLE MUSIC SWITCH ---
        // 1. Stop the looping background music
        if (backgroundMusic != null)
        {
            backgroundMusic.Stop(); 
        }

        // 2. Play the victory sound (One Shot)
        if (sfxSource != null && victorySound != null)
        {
            sfxSource.PlayOneShot(victorySound);
        }

        // --- B. SHOW UI (VR POSITIONING) ---
        if (endingPanel != null) 
        {
            if (playerHead != null)
            {
                // Teleport UI in front of player
                Vector3 targetPos = playerHead.position + (playerHead.forward * distanceFromFace);
                targetPos.y = playerHead.position.y + heightOffset; 

                endingPanel.transform.position = targetPos;
                endingPanel.transform.LookAt(playerHead);
                endingPanel.transform.Rotate(0, 180, 0); 
            }
            endingPanel.SetActive(true);
        }
    }
}