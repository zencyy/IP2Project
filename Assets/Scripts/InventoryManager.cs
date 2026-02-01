using UnityEngine;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Item Database")]
    public List<GameObject> allPrefabs; 

    [Header("References")]
    public Transform headCamera; 

    [Header("Debug")]
    public List<string> localInventory = new List<string>();

    private DatabaseReference dbReference;
    private string userId;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Use your specific URL
        dbReference = FirebaseDatabase.GetInstance("https://fymstudio-a8928-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;

        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            LoadInventory(); 
        }
    }

    // --- 1. COLLECT ITEM (NO DUPLICATES LOGIC) ---
    public void CollectWord(string wordID)
    {
        if (string.IsNullOrEmpty(userId)) return;

        // CHECK 1: Do we already have this in our local list?
        if (localInventory.Contains(wordID))
        {
            Debug.Log($"You already have '{wordID}' in your inventory!");
            return; // STOP HERE. Do not add it again.
        }

        // 1. Add to Local List
        localInventory.Add(wordID);
        RefreshUI();

        // 2. Add to Firebase (Using wordID as the Key)
        // OLD WAY: ...Child("inventory").Push().SetValueAsync(wordID);
        // NEW WAY: We use the ID as the key. "Subject" : "Subject"
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).SetValueAsync(wordID)
            .ContinueWithOnMainThread(task => 
            {
                if (task.IsCompleted) Debug.Log($"Saved '{wordID}' (No Duplicates)");
            });
    }

    // --- 2. SPAWN & CONSUME ITEM ---
    public void SpawnItem(string wordID)
    {
        if (!localInventory.Contains(wordID)) return;

        GameObject prefabToSpawn = null;
        foreach (GameObject go in allPrefabs)
        {
            if (go != null && go.name == wordID)
            {
                prefabToSpawn = go;
                break;
            }
        }

        if (prefabToSpawn != null)
        {
            Vector3 spawnPos = headCamera.position + (headCamera.forward * 1.0f);
            Quaternion spawnRot = Quaternion.Euler(0, headCamera.eulerAngles.y, 0);
            Instantiate(prefabToSpawn, spawnPos, spawnRot);

            // Remove it from inventory immediately
            RemoveItem(wordID);
        }
    }

    // --- 3. REMOVE LOGIC (SIMPLIFIED) ---
    void RemoveItem(string wordID)
    {
        // 1. Remove from Local List
        localInventory.Remove(wordID);
        RefreshUI();

        // 2. Remove from Firebase
        // Since we used wordID as the key, we don't need a query anymore! 
        // We can find it directly.
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).RemoveValueAsync();
    }

    // --- 4. LOAD & REFRESH ---
    public void LoadInventory()
    {
        dbReference.Child("users").Child(userId).Child("inventory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result; 
                localInventory.Clear();
                
                foreach (DataSnapshot child in snapshot.Children)
                {
                    localInventory.Add(child.Value.ToString());
                }
                RefreshUI();
            }
        });
    }

    void RefreshUI()
    {
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.UpdateDisplay(localInventory);
        }
    }
}