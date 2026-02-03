using UnityEngine;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Scene References")]
    public Transform subjectContainer; 
    public GameObject verbContainer;
    public GameObject objectContainer; 
    public Transform headCamera;       

    [Header("Item Database")]
    public List<GameObject> allPrefabs; 

    [Header("Debug Status")]
    public List<string> localInventory = new List<string>();
    public List<string> consumedItemIDs = new List<string>(); 
    public List<string> placedItemIDs = new List<string>();

    public bool verbsUnlocked = false;
    public bool objectsUnlocked = false; 
    
    public int currentSVSentenceCount = 0;
    public int currentSVOSentenceCount = 0; 

    private DatabaseReference dbReference;
    private string userId;

    void Awake() { Instance = this; }

    void Start()
    {
        dbReference = FirebaseDatabase.GetInstance("https://fymstudio-a8928-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;

        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            // START THE CHAIN
            LoadProgressFlags(); 
        }
    }

    // --- 1. LOAD FLAGS (STEP 1) ---
    // We MUST know if you unlocked stuff before we check inventory
    void LoadProgressFlags()
    {
        dbReference.Child("users").Child(userId).Child("progress").GetValueAsync().ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted)
            {
                if (task.Result.Exists)
                {
                    DataSnapshot p = task.Result;
                    
                    if (p.Child("verbs_unlocked").Exists) 
                        verbsUnlocked = (bool)p.Child("verbs_unlocked").Value;
                    
                    if (p.Child("objects_unlocked").Exists) 
                        objectsUnlocked = (bool)p.Child("objects_unlocked").Value;

                    if (p.Child("sentences_completed").Exists) 
                        currentSVSentenceCount = int.Parse(p.Child("sentences_completed").Value.ToString());

                    if (p.Child("svo_sentences_completed").Exists) 
                        currentSVOSentenceCount = int.Parse(p.Child("svo_sentences_completed").Value.ToString());
                }

                // Activate Containers based on flags
                if (verbsUnlocked && verbContainer != null) verbContainer.SetActive(true);
                if (objectsUnlocked && objectContainer != null) objectContainer.SetActive(true);

                // CHAIN STEP 2: Now load inventory
                LoadInventory();
            }
        });
    }

    // --- 2. LOAD INVENTORY (STEP 2) ---
    void LoadInventory()
    {
        dbReference.Child("users").Child(userId).Child("inventory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                localInventory.Clear();
                foreach (DataSnapshot child in task.Result.Children) localInventory.Add(child.Value.ToString());
                
                RefreshUI();
                
                // CHAIN STEP 3: Load Placed Items
                LoadPlacedItems();
            }
        });
    }

    // --- 3. LOAD PLACED ITEMS (STEP 3) ---
    void LoadPlacedItems()
    {
        dbReference.Child("users").Child(userId).Child("placed_items").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                placedItemIDs.Clear();
                foreach (DataSnapshot child in task.Result.Children)
                {
                    string wordID = child.Key;
                    
                    // Spawn logic
                    float x = float.Parse(child.Child("x").Value.ToString());
                    float y = float.Parse(child.Child("y").Value.ToString());
                    float z = float.Parse(child.Child("z").Value.ToString());
                    
                    float rX = float.Parse(child.Child("rX").Value.ToString());
                    float rY = float.Parse(child.Child("rY").Value.ToString());
                    float rZ = float.Parse(child.Child("rZ").Value.ToString());

                    SpawnVisualBlock(wordID, new Vector3(x, y, z), Quaternion.Euler(rX, rY, rZ));
                    placedItemIDs.Add(wordID);
                }

                // CHAIN STEP 4: Load Consumed Items
                LoadConsumedItems();
            }
        });
    }

    // --- 4. LOAD CONSUMED & FINISH (STEP 4) ---
    void LoadConsumedItems()
    {
        dbReference.Child("users").Child(userId).Child("consumed_items").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                consumedItemIDs.Clear();
                foreach (DataSnapshot child in task.Result.Children)
                {
                    consumedItemIDs.Add(child.Key);
                }

                // FINAL STEP:
                // 1. Hide Scene Blocks (using all lists we just loaded)
                SyncSceneState();
                
                // 2. Update HUD (Now that we have Flags + Consumed lists ready)
                UpdateProgressTracker();
            }
        });
    }

    // --- LOGIC HELPERS ---
    
    // UPDATED: Sync Scene Logic
    // Hides block if it is in Inventory OR Consumed.
    // DOES NOT hide if placed (so you can see it on the table).
    void SyncSceneState()
    {
        CheckContainer(subjectContainer);
        if (verbContainer) CheckContainer(verbContainer.transform);
        if (objectContainer) CheckContainer(objectContainer.transform);
    }

    void CheckContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
        {
            WordBlock b = child.GetComponent<WordBlock>();
            if (b != null)
            {
                // We DON'T include 'placedItemIDs' here because you want placed items to be visible.
                bool shouldHide = localInventory.Contains(b.wordID) 
                               || consumedItemIDs.Contains(b.wordID);
                
                child.gameObject.SetActive(!shouldHide);
            }
        }
    }

    // UPDATED: Progress Tracker
    public void UpdateProgressTracker()
    {
        // 1. Calculate Counts (Only needed for Phase 1/2 tracking)
        int subjectCount = 0;
        int verbCount = 0;
        int objectCount = 0;
        foreach (string id in localInventory) {
            if (id.StartsWith("Sub_")) subjectCount++;
            if (id.StartsWith("Verb_")) verbCount++;
            if (id.StartsWith("Obj_")) objectCount++;
        }

        if (ProgressHUD.Instance != null)
        {
            // PHASE 1: Subject Collection
            if (!verbsUnlocked)
            {
                ProgressHUD.Instance.UpdateProgress("Collect Subjects", subjectCount, 10);
            }
            // PHASE 2: Verb Collection
            else if (!AreShelfBlocksCollected(subjectContainer) || !AreShelfBlocksCollected(verbContainer.transform))
            {
                 // Note: If you have consumed blocks, 'subjectCount' is 0, but this doesn't matter
                 // because we are in the 'else' block of !verbsUnlocked.
                 ProgressHUD.Instance.UpdateProgress("Collect Verbs", verbCount, 10);
            }
            // PHASE 3: SV Sentences
            else if (!objectsUnlocked)
            {
                 ProgressHUD.Instance.UpdateProgress("Form Sentences (SV)", currentSVSentenceCount, 5);
            }
            // PHASE 4: Object Collection
            else if (!AreShelfBlocksCollected(objectContainer.transform))
            {
                 ProgressHUD.Instance.UpdateProgress("Collect Objects", objectCount, 5);
            }
            // PHASE 5: SVO Sentences
            else
            {
                 ProgressHUD.Instance.UpdateProgress("Form Sentences (SVO)", currentSVOSentenceCount, 5);
            }
        }
    }

    // UPDATED: Check if shelves are empty
    // Considers a block "collected" if it is hidden OR if it is placed on the table
    public bool AreShelfBlocksCollected(Transform container)
    {
        if (container == null) return true;
        foreach (Transform child in container)
        {
            WordBlock wb = child.GetComponent<WordBlock>();
            
            // If the block is Active (visible on shelf)
            if (wb != null && child.gameObject.activeSelf)
            {
                // Exception: It's active, but we placed it on the table? Then count it as collected.
                if (!placedItemIDs.Contains(wb.wordID)) 
                {
                    return false; // Actually missing from collection
                }
            }
        }
        return true;
    }

    // --- STANDARD FUNCTIONS (Copy/Paste these exactly as they were) ---

    public void CollectWord(string wordID)
    {
        if (localInventory.Contains(wordID)) return;
        localInventory.Add(wordID);
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).SetValueAsync(wordID);
        dbReference.Child("users").Child(userId).Child("placed_items").Child(wordID).RemoveValueAsync();
        if(placedItemIDs.Contains(wordID)) placedItemIDs.Remove(wordID);
        
        GameObject sceneObj = FindBlockInScene(wordID);
        if (sceneObj != null) sceneObj.SetActive(false);

        RefreshUI();
        UpdateProgressTracker();
        CheckProgression();
    }

    public void SpawnItem(string wordID)
    {
        if (!localInventory.Contains(wordID)) return;
        Vector3 spawnPos = headCamera.position + (headCamera.forward * 0.8f);
        Quaternion spawnRot = Quaternion.Euler(0, headCamera.eulerAngles.y, 0);
        SpawnVisualBlock(wordID, spawnPos, spawnRot);
        localInventory.Remove(wordID);
        RefreshUI();
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).RemoveValueAsync();
        SaveBlockLocation(wordID, spawnPos, spawnRot);
        UpdateProgressTracker();
    }

    public void MarkItemAsConsumed(string wordID)
    {
        if (!consumedItemIDs.Contains(wordID)) consumedItemIDs.Add(wordID);
        if (dbReference != null)
        {
            dbReference.Child("users").Child(userId).Child("consumed_items").Child(wordID).SetValueAsync(true);
            dbReference.Child("users").Child(userId).Child("placed_items").Child(wordID).RemoveValueAsync();
        }
        if (placedItemIDs.Contains(wordID)) placedItemIDs.Remove(wordID);
    }
    
    // Progression Checks
    void CheckProgression()
    {
        int subCount = 0;
        foreach(string s in localInventory) if(s.StartsWith("Sub_")) subCount++;
        if (subCount >= 10 && !verbsUnlocked)
        {
            verbsUnlocked = true;
            if (verbContainer != null) verbContainer.SetActive(true);
            dbReference.Child("users").Child(userId).Child("progress").Child("verbs_unlocked").SetValueAsync(true);
            UpdateProgressTracker();
        }
    }

    public void UnlockObjects()
    {
        if (objectsUnlocked) return;
        objectsUnlocked = true;
        if (objectContainer != null) objectContainer.SetActive(true);
        dbReference.Child("users").Child(userId).Child("progress").Child("objects_unlocked").SetValueAsync(true);
        UpdateProgressTracker();
    }

    public bool IsCollectionPhaseComplete()
    {
        if (!AreShelfBlocksCollected(subjectContainer)) return false;
        if (verbsUnlocked && !AreShelfBlocksCollected(verbContainer.transform)) return false;
        if (objectsUnlocked && !AreShelfBlocksCollected(objectContainer.transform)) return false;
        return true;
    }

    // Helpers
    public void RefreshUI() { if (InventoryUI.Instance != null) InventoryUI.Instance.UpdateDisplay(localInventory); }
    
    void SpawnVisualBlock(string wordID, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = GetPrefabByID(wordID);
        if (prefab != null) Instantiate(prefab, pos, rot);
    }
    
    GameObject GetPrefabByID(string id)
    {
        foreach (GameObject go in allPrefabs) {
             if(go == null) continue;
             WordBlock wb = go.GetComponent<WordBlock>();
             if ((wb != null && wb.wordID == id) || go.name == id) return go;
        }
        return null;
    }

    GameObject FindBlockInScene(string id)
    {
        GameObject go = FindInContainer(subjectContainer, id);
        if (go != null) return go;
        if (verbContainer) { go = FindInContainer(verbContainer.transform, id); if (go != null) return go; }
        if (objectContainer) { go = FindInContainer(objectContainer.transform, id); if (go != null) return go; }
        return null;
    }

    GameObject FindInContainer(Transform container, string id)
    {
        if (container == null) return null;
        foreach(Transform t in container) {
            WordBlock b = t.GetComponent<WordBlock>();
            if (b != null && b.wordID == id) return t.gameObject;
        }
        return null;
    }
    
    public void SaveBlockLocation(string wordID, Vector3 pos, Quaternion rot)
    {
        if (string.IsNullOrEmpty(userId)) return;
        Dictionary<string, object> locData = new Dictionary<string, object>();
        locData["x"] = pos.x; locData["y"] = pos.y; locData["z"] = pos.z;
        locData["rX"] = rot.eulerAngles.x; locData["rY"] = rot.eulerAngles.y; locData["rZ"] = rot.eulerAngles.z;
        dbReference.Child("users").Child(userId).Child("placed_items").Child(wordID).SetValueAsync(locData);
        if(!placedItemIDs.Contains(wordID)) placedItemIDs.Add(wordID);
    }
}