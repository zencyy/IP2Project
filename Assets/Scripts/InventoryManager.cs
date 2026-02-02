using UnityEngine;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Scene References")]
    public Transform subjectContainer; // Drag 'Environment_Subjects' here
    public GameObject verbContainer;   // Drag 'Environment_Verbs' here
    public Transform headCamera;       // Drag Main Camera

    [Header("Item Database")]
    public List<GameObject> allPrefabs; // Drag all Blue Prefabs here

    [Header("Debug")]
    public List<string> localInventory = new List<string>();
    public bool verbsUnlocked = false;

    // Track objects currently placed in the world to avoid re-spawning
    private List<string> placedItemIDs = new List<string>();

    private DatabaseReference dbReference;
    private string userId;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Your specific URL
        dbReference = FirebaseDatabase.GetInstance("https://fymstudio-a8928-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;

        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            LoadUserData();
        }
    }

    // --- 1. MASTER LOAD FUNCTION ---
    public void LoadUserData()
    {
        // A. Load Unlock Status
        dbReference.Child("users").Child(userId).Child("progress").Child("verbs_unlocked")
            .GetValueAsync().ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted && task.Result.Value != null)
            {
                verbsUnlocked = (bool)task.Result.Value;
                if (verbsUnlocked && verbContainer != null) verbContainer.SetActive(true);
            }
        });

        // B. Load Inventory (What is in the pocket?)
        dbReference.Child("users").Child(userId).Child("inventory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                localInventory.Clear();
                foreach (DataSnapshot child in snapshot.Children) localInventory.Add(child.Value.ToString());

                RefreshUI();
                SyncSceneState(); // Hide original scene objects if we own them
                
                // NEW: Update the HUD when game starts
                UpdateProgressTracker(); 
            }
        });

        // C. Load Placed Items (What is sitting on the floor?)
        LoadPlacedItems();
    }

    // --- 2. LOAD PLACED ITEMS ---
    void LoadPlacedItems()
    {
        dbReference.Child("users").Child(userId).Child("placed_items").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                placedItemIDs.Clear();

                foreach (DataSnapshot child in snapshot.Children)
                {
                    string wordID = child.Key; 
                    
                    // Parse Position & Rotation
                    float x = float.Parse(child.Child("x").Value.ToString());
                    float y = float.Parse(child.Child("y").Value.ToString());
                    float z = float.Parse(child.Child("z").Value.ToString());
                    Vector3 pos = new Vector3(x, y, z);

                    float rX = float.Parse(child.Child("rX").Value.ToString());
                    float rY = float.Parse(child.Child("rY").Value.ToString());
                    float rZ = float.Parse(child.Child("rZ").Value.ToString());
                    Quaternion rot = Quaternion.Euler(rX, rY, rZ);

                    // Spawn it visually
                    SpawnVisualBlock(wordID, pos, rot);
                    
                    placedItemIDs.Add(wordID);
                }
                
                SyncSceneState(); 
            }
        });
    }

    // --- 3. COLLECT LOGIC (With Progress Check) ---
    public void CollectWord(string wordID)
    {
        if (localInventory.Contains(wordID)) return;

        // 1. Add to Inventory List & DB
        localInventory.Add(wordID);
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).SetValueAsync(wordID);

        // 2. Remove from 'Placed Items' (It's in pocket now, not on floor)
        dbReference.Child("users").Child(userId).Child("placed_items").Child(wordID).RemoveValueAsync();
        if(placedItemIDs.Contains(wordID)) placedItemIDs.Remove(wordID);

        // 3. Hide Original Scene Object
        GameObject sceneObj = FindBlockInScene(wordID);
        if (sceneObj != null) sceneObj.SetActive(false);

        // 4. Update UI & Progression
        RefreshUI();
        UpdateProgressTracker(); // Update HUD
        CheckProgression();      // Check if we unlocked Verbs
    }

    // --- 4. SPAWNING LOGIC ---
    public void SpawnItem(string wordID)
    {
        if (!localInventory.Contains(wordID)) return;

        // Calculate Position in front of player
        Vector3 spawnPos = headCamera.position + (headCamera.forward * 0.8f);
        Quaternion spawnRot = Quaternion.Euler(0, headCamera.eulerAngles.y, 0);

        // 1. Visually Spawn
        SpawnVisualBlock(wordID, spawnPos, spawnRot);

        // 2. Remove from Inventory
        localInventory.Remove(wordID);
        RefreshUI();
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).RemoveValueAsync();

        // 3. Add to 'Placed Items' Database
        SaveBlockLocation(wordID, spawnPos, spawnRot);
        
        // 4. Update HUD (Count goes down when you spawn items back out)
        UpdateProgressTracker();
    }

    // --- 5. PROGRESSION & HUD ---
    public void UpdateProgressTracker()
    {
        // 1. Calculate Counts
        int subjectCount = 0;
        int verbCount = 0;

        foreach (string id in localInventory)
        {
            if (id.StartsWith("Sub_")) subjectCount++;
            if (id.StartsWith("Verb_")) verbCount++;
        }

        // 2. SAVE TO DATABASE (New Feature)
        if (!string.IsNullOrEmpty(userId))
        {
            dbReference.Child("users").Child(userId).Child("progress").Child("subject_count").SetValueAsync(subjectCount);
            dbReference.Child("users").Child(userId).Child("progress").Child("verb_count").SetValueAsync(verbCount);
        }

        // 3. Update Visual HUD
        if (ProgressHUD.Instance != null)
        {
            if (!verbsUnlocked)
            {
                ProgressHUD.Instance.UpdateProgress("Collect Subjects", subjectCount, 10);
            }
            else
            {
                ProgressHUD.Instance.UpdateProgress("Collect Verbs", verbCount, 5); 
            }
        }
    }

    void CheckProgression()
    {
        int subjectCount = 0;
        foreach (string id in localInventory)
        {
            if (id.StartsWith("Sub_")) subjectCount++;
        }

        if (subjectCount >= 10 && !verbsUnlocked)
        {
            Debug.Log("LEVEL UP! Verbs Unlocked.");
            verbsUnlocked = true;
            if (verbContainer != null) verbContainer.SetActive(true);
            
            // Save unlock state
            dbReference.Child("users").Child(userId).Child("progress").Child("verbs_unlocked").SetValueAsync(true);
            
            UpdateProgressTracker(); // Refresh HUD to show next phase
        }
    }

    // --- 6. SAVING LOCATION ---
    public void SaveBlockLocation(string wordID, Vector3 pos, Quaternion rot)
    {
        if (string.IsNullOrEmpty(userId)) return;

        Dictionary<string, object> locData = new Dictionary<string, object>();
        locData["x"] = pos.x;
        locData["y"] = pos.y;
        locData["z"] = pos.z;
        locData["rX"] = rot.eulerAngles.x;
        locData["rY"] = rot.eulerAngles.y;
        locData["rZ"] = rot.eulerAngles.z;

        dbReference.Child("users").Child(userId).Child("placed_items").Child(wordID).SetValueAsync(locData);
        if(!placedItemIDs.Contains(wordID)) placedItemIDs.Add(wordID);
    }

    // --- HELPERS ---
    void SyncSceneState()
    {
        if (subjectContainer == null) return;
        foreach (Transform child in subjectContainer)
        {
            WordBlock b = child.GetComponent<WordBlock>();
            if (b != null)
            {
                if (localInventory.Contains(b.wordID) || placedItemIDs.Contains(b.wordID))
                {
                    child.gameObject.SetActive(false);
                }
                else
                {
                    child.gameObject.SetActive(true);
                }
            }
        }
    }

    void SpawnVisualBlock(string wordID, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = GetPrefabByID(wordID);
        if (prefab != null)
        {
            Instantiate(prefab, pos, rot);
        }
    }
    
    GameObject GetPrefabByID(string id)
    {
        foreach (GameObject go in allPrefabs)
        {
            if (go == null) continue;
            WordBlock wb = go.GetComponent<WordBlock>();
            if ((wb != null && wb.wordID == id) || go.name == id) return go;
        }
        return null;
    }

    GameObject FindBlockInScene(string id)
    {
        foreach (Transform child in subjectContainer)
        {
            WordBlock b = child.GetComponent<WordBlock>();
            if (b != null && b.wordID == id) return child.gameObject;
        }
        return null;
    }

    public void RefreshUI()
    {
        if (InventoryUI.Instance != null) InventoryUI.Instance.UpdateDisplay(localInventory);
    }
}