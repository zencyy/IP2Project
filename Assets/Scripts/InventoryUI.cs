using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("Category Containers")]
    // Drag the specific "Content" objects from your hierarchy here
    public Transform subjectContent; 
    public Transform verbContent;
    public Transform objectContent;

    [Header("Setup")]
    public GameObject buttonPrefab; // Drag your Button Prefab here

    void Awake()
    {
        Instance = this;
    }

    public void UpdateDisplay(List<string> items)
    {
        // 1. Clear ALL old buttons first so we don't have duplicates
        ClearContainer(subjectContent);
        ClearContainer(verbContent);
        ClearContainer(objectContent);

        // 2. Loop through inventory and sort into correct columns
        foreach (string itemID in items)
        {
            // Check for Subject
            if (itemID.StartsWith("Sub_") || itemID.StartsWith("sub_"))
            {
                CreateButton(itemID, subjectContent);
            }
            // Check for Verb
            else if (itemID.StartsWith("Verb_") || itemID.StartsWith("verb_"))
            {
                CreateButton(itemID, verbContent);
            }
            // Check for Object
            else if (itemID.StartsWith("Obj_") || itemID.StartsWith("obj_") || itemID.StartsWith("Object_"))
            {
                CreateButton(itemID, objectContent);
            }
            else
            {
                // Fallback: If it doesn't match, put it in Objects or log it
                Debug.LogWarning("Unknown Category for: " + itemID);
            }
        }
    }

    // Helper function to create the button and clean up the text
    void CreateButton(string itemID, Transform categoryContainer)
    {
        GameObject newBtn = Instantiate(buttonPrefab, categoryContainer);
        
        // --- VISUALS ---
        TMP_Text btnText = newBtn.GetComponentInChildren<TMP_Text>();
        if(btnText != null) 
        {
            // Clean the name (e.g., turn "Sub_Dog" into "Dog")
            string displayName = itemID;
            if (itemID.Contains("_"))
            {
                displayName = itemID.Split('_')[1]; // Take the part after the "_"
            }
            btnText.text = displayName;
        }

        // --- FUNCTIONALITY ---
        Button b = newBtn.GetComponent<Button>();
        if (b != null)
        {
            // Use a lambda to pass the specific itemID
            b.onClick.AddListener(() => OnItemClicked(itemID));
        }
    }

    // Helper function to empty a container
    void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    void OnItemClicked(string itemID)
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SpawnItem(itemID);
        }
    }
}