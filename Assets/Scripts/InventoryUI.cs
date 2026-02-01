using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("Setup")]
    public Transform contentArea; // Drag the "Content" object here
    public GameObject buttonPrefab; // Drag your Button Prefab here

    void Awake()
    {
        Instance = this;
    }

    public void UpdateDisplay(List<string> items)
    {
        // 1. Clear old buttons
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        // 2. Create new buttons
        foreach (string itemID in items)
        {
            GameObject newBtn = Instantiate(buttonPrefab, contentArea);
            
            // Set Text
            TMP_Text btnText = newBtn.GetComponentInChildren<TMP_Text>();
            if(btnText != null) btnText.text = itemID;

            // Add Click Event
            Button b = newBtn.GetComponent<Button>();
            b.onClick.AddListener(() => OnItemClicked(itemID));
        }
    }

    void OnItemClicked(string itemID)
    {
        // Call Manager to spawn it
        InventoryManager.Instance.SpawnItem(itemID);
    }
}