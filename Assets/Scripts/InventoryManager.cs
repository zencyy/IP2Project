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
    
    public GameEndingUI gameEndingUI; 

    [Header("Item Database")]
    public List<GameObject> allPrefabs; 

    [Header("Debug Status")]
    public List<string> localInventory = new List<string>();
    public List<string> consumedItemIDs = new List<string>(); 
    public List<string> placedItemIDs = new List<string>();

    [Header("Audio Settings")]
    public AudioClip audioSubjectLoop;
    public AudioClip audioVerbLoop;
    public AudioClip audioObjectLoop;

    [Header("Navigation System")]    
    public NavigationLine navLine; 
    
    public Transform subjectZoneTrig;
    public Transform verbZoneTrig;
    public Transform svStationTrig; 
    public Transform objectZoneTrig;
    public Transform svoStationTrig; 

    // State Flags
    private bool reachedSub = false;
    private bool reachedVerb = false;
    private bool reachedSV = false;
    private bool reachedObj = false;
    private bool reachedSVO = false;
    
    public bool verbsUnlocked = false;
    public bool objectsUnlocked = false; 
    
    public int currentSVSentenceCount = 0;
    public int currentSVOSentenceCount = 0; 
    
    private bool gameEnded = false; 

    private DatabaseReference dbReference;
    private string userId;

    void Awake() { Instance = this; }

    void Start()
    {
        dbReference = FirebaseDatabase.GetInstance("https://fymstudio-a8928-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;

        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            LoadProgressFlags(); 
        }
    }

    // --- 1. LOAD FLAGS ---
    void LoadProgressFlags()
    {
        dbReference.Child("users").Child(userId).Child("progress").GetValueAsync().ContinueWithOnMainThread(task => 
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DataSnapshot p = task.Result;
                
                if (p.Child("verbs_unlocked").Exists) 
                    verbsUnlocked = bool.Parse(p.Child("verbs_unlocked").Value.ToString());
                
                if (p.Child("objects_unlocked").Exists) 
                    objectsUnlocked = bool.Parse(p.Child("objects_unlocked").Value.ToString());

                if (p.Child("sentences_completed").Exists) 
                    currentSVSentenceCount = int.Parse(p.Child("sentences_completed").Value.ToString());

                if (p.Child("svo_sentences_completed").Exists) 
                    currentSVOSentenceCount = int.Parse(p.Child("svo_sentences_completed").Value.ToString());

                if (p.Child("reached_sub").Exists) reachedSub = bool.Parse(p.Child("reached_sub").Value.ToString());
                if (p.Child("reached_verb").Exists) reachedVerb = bool.Parse(p.Child("reached_verb").Value.ToString());
                if (p.Child("reached_sv").Exists) reachedSV = bool.Parse(p.Child("reached_sv").Value.ToString());
                if (p.Child("reached_obj").Exists) reachedObj = bool.Parse(p.Child("reached_obj").Value.ToString());
                if (p.Child("reached_svo").Exists) reachedSVO = bool.Parse(p.Child("reached_svo").Value.ToString());

                if (verbsUnlocked && verbContainer != null) verbContainer.SetActive(true);
                if (objectsUnlocked && objectContainer != null) objectContainer.SetActive(true);

                LoadInventory();
            }
            else
            {
                LoadInventory();
            }
        });
    }

    // --- 2. LOAD INVENTORY ---
    void LoadInventory()
    {
        dbReference.Child("users").Child(userId).Child("inventory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                localInventory.Clear();
                if (task.Result.Exists)
                {
                    foreach (DataSnapshot child in task.Result.Children) localInventory.Add(child.Value.ToString());
                }
                RefreshUI();
                LoadPlacedItems();
            }
        });
    }

    // --- 3. LOAD PLACED ITEMS ---
    void LoadPlacedItems()
    {
        dbReference.Child("users").Child(userId).Child("placed_items").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                placedItemIDs.Clear();
                if (task.Result.Exists)
                {
                    foreach (DataSnapshot child in task.Result.Children)
                    {
                        string wordID = child.Key;
                        float x = float.Parse(child.Child("x").Value.ToString());
                        float y = float.Parse(child.Child("y").Value.ToString());
                        float z = float.Parse(child.Child("z").Value.ToString());
                        float rX = float.Parse(child.Child("rX").Value.ToString());
                        float rY = float.Parse(child.Child("rY").Value.ToString());
                        float rZ = float.Parse(child.Child("rZ").Value.ToString());

                        SpawnVisualBlock(wordID, new Vector3(x, y, z), Quaternion.Euler(rX, rY, rZ));
                        placedItemIDs.Add(wordID);
                    }
                }
                LoadConsumedItems();
            }
        });
    }

    // --- 4. LOAD CONSUMED ---
    void LoadConsumedItems()
    {
        dbReference.Child("users").Child(userId).Child("consumed_items").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                consumedItemIDs.Clear();
                if (task.Result.Exists)
                {
                    foreach (DataSnapshot child in task.Result.Children) consumedItemIDs.Add(child.Key);
                }
                SyncSceneState();
                UpdateProgressTracker();
            }
        });
    }

    // --- SPAWN LOGIC ---
    void SpawnVisualBlock(string wordID, Vector3 pos, Quaternion rot)
    {
        GameObject prefab = GetPrefabByID(wordID);
        if (prefab != null) 
        {
            GameObject clone = Instantiate(prefab, pos, rot);
            WordBlock wb = clone.GetComponent<WordBlock>();
            if (wb != null) wb.wordID = wordID; 
        }
    }

    // --- COLLECT LOGIC ---
    public void CollectWord(string wordID, GameObject collectedObj = null)
    {
        if (localInventory.Contains(wordID)) 
        {
            if (collectedObj != null) Destroy(collectedObj);
            dbReference.Child("users").Child(userId).Child("placed_items").Child(wordID).RemoveValueAsync();
            if(placedItemIDs.Contains(wordID)) placedItemIDs.Remove(wordID);
            return;
        }

        localInventory.Add(wordID);
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).SetValueAsync(wordID);

        dbReference.Child("users").Child(userId).Child("placed_items").Child(wordID).RemoveValueAsync();
        if(placedItemIDs.Contains(wordID)) placedItemIDs.Remove(wordID);
        
        if (collectedObj != null) Destroy(collectedObj);
        
        GameObject shelfObj = FindBlockInScene(wordID);
        if (shelfObj != null) shelfObj.SetActive(false);

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
        dbReference.Child("users").Child(userId).Child("inventory").Child(wordID).RemoveValueAsync();
        
        SaveBlockLocation(wordID, spawnPos, spawnRot);
        RefreshUI();
        UpdateProgressTracker();
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
    
    public void IncrementSVOSentence()
    {
        currentSVOSentenceCount++;
        dbReference.Child("users").Child(userId).Child("progress").Child("svo_sentences_completed").SetValueAsync(currentSVOSentenceCount);
        UpdateProgressTracker();
    }

    // --- PROGRESS TRACKER (FIXED COUNT LOGIC) ---
    public void UpdateProgressTracker()
    {
        int subjectCount = 0;
        int verbCount = 0;
        int objectCount = 0;

        foreach (string id in localInventory)
        {
            if (id.StartsWith("sub_")) subjectCount++;
            if (id.StartsWith("verb_")) verbCount++;
            if (id.StartsWith("obj_") || id.StartsWith("Object_")) objectCount++;
        }

        // Fail-Safe: If you have items, you MUST have reached that zone
        if (subjectCount > 0) reachedSub = true;
        if (verbCount > 0) reachedVerb = true;
        if (currentSVSentenceCount > 0) reachedSV = true;
        if (objectCount > 0) reachedObj = true;
        if (currentSVOSentenceCount > 0) reachedSVO = true;

        string uiText = "";
        Transform arrowTarget = null;
        
        // --- STRICT SEQUENTIAL LOGIC ---

        // 0. Game Over
        if (currentSVOSentenceCount >= 5)
        {
            uiText = "Congratulations! You Completed the Game!";
            arrowTarget = null;
            if (!gameEnded)
            {
                gameEnded = true;
                if (gameEndingUI != null) gameEndingUI.ShowVictory();
            }
        }
        // 1. SVO Phase (Objects Unlocked)
        else if (objectsUnlocked)
        {
            // A. Go to Zone
            if (!reachedObj) 
            {
                uiText = "Follow Line to Object Area"; 
                arrowTarget = objectZoneTrig; 
            }
            // B. Collect (Safety: Check count ONLY if we haven't reached the barn yet)
            else if (objectCount < 5 && !reachedSVO) 
            { 
                uiText = $"Collect Objects:"; 
                arrowTarget = null; 
            }
            // C. Go to Barn
            else if (!reachedSVO)
            { 
                uiText = "Go to the Red Barn"; 
                arrowTarget = svoStationTrig; 
            }
            // D. Form Sentences
            else 
            {
                 uiText = $"Form SVO Sentences:";
                 arrowTarget = null;
            }
        }
        // 2. SV Sentence Phase (Verbs Unlocked)
        else if (verbsUnlocked)
        {
            // FIX: If unlocked, IGNORE verbCount. Only check progression.
            if (!reachedSV)
            {
                uiText = "Go to the White House"; 
                arrowTarget = svStationTrig; 
            }
            else 
            {
                 uiText = $"Form SV Sentences";
                 arrowTarget = null;
            }
        }
        // 3. Subject Phase / Early Game (Must check counts here)
        else
        {
            // Initial State
            if (!reachedSub) 
            { 
                uiText = "Follow Line to Subject Area"; 
                arrowTarget = subjectZoneTrig; 
            }
            else if (subjectCount < 10) 
            { 
                uiText = $"Collect Subjects:"; 
                arrowTarget = null; 
            }
            // Transition to Verbs
            else if (!reachedVerb) 
            {
                uiText = "Follow Line to Verb Area";
                arrowTarget = verbZoneTrig;
            }
            else if (verbCount < 10) 
            {
                uiText = $"Collect Verbs:";
                arrowTarget = null;
            }
        }

        // --- Apply Updates ---
        if (ProgressHUD.Instance != null)
        {
            if (arrowTarget != null) ProgressHUD.Instance.UpdateProgress(uiText, 0, 0); 
            else
            {
                int current = 0; int max = 10;
                
                if(uiText.Contains("Subject")) { current = subjectCount; max = 10; }
                else if(uiText.Contains("Verb")) { current = verbCount; max = 10; }
                else if(uiText.Contains("SV Sen")) { current = currentSVSentenceCount; max = 5; }
                else if(uiText.Contains("Object")) { current = objectCount; max = 5; }
                else if(uiText.Contains("SVO") && !uiText.Contains("Congrat")) { current = currentSVOSentenceCount; max = 5; }
                else if(uiText.Contains("Congrat")) { current = 5; max = 5; }
                
                ProgressHUD.Instance.UpdateProgress(uiText, current, max);
            }
        }

        if (navLine != null) navLine.target = arrowTarget;
    }

    public void PlayerReachedZone(ZoneTrigger.ZoneType type)
    {
        bool stateChanged = false;

        switch (type)
        {
            case ZoneTrigger.ZoneType.SubjectZone: 
                if (!reachedSub) { reachedSub = true; SaveZoneFlag("reached_sub"); stateChanged = true; }
                break;
            case ZoneTrigger.ZoneType.VerbZone: 
                if (!reachedVerb) { reachedVerb = true; SaveZoneFlag("reached_verb"); stateChanged = true; }
                break;
            case ZoneTrigger.ZoneType.SVStation: 
                if (!reachedSV) { reachedSV = true; SaveZoneFlag("reached_sv"); stateChanged = true; }
                break;
            case ZoneTrigger.ZoneType.ObjectZone: 
                if (!reachedObj) { reachedObj = true; SaveZoneFlag("reached_obj"); stateChanged = true; }
                break;
            case ZoneTrigger.ZoneType.SVOStation: 
                if (!reachedSVO) { reachedSVO = true; SaveZoneFlag("reached_svo"); stateChanged = true; }
                break;
        }
        if (stateChanged) UpdateProgressTracker(); 
    }

    void SaveZoneFlag(string key)
    {
        if (dbReference != null && !string.IsNullOrEmpty(userId))
        {
            dbReference.Child("users").Child(userId).Child("progress").Child(key).SetValueAsync(true);
        }
    }

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
                bool shouldHide = localInventory.Contains(b.wordID) || consumedItemIDs.Contains(b.wordID);
                child.gameObject.SetActive(!shouldHide);
            }
        }
    }

    public bool AreShelfBlocksCollected(Transform container)
    {
        if (container == null) return true;
        foreach (Transform child in container)
        {
            WordBlock wb = child.GetComponent<WordBlock>();
            if (wb != null && child.gameObject.activeSelf)
            {
                if (!placedItemIDs.Contains(wb.wordID)) return false; 
            }
        }
        return true;
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
    
    void CheckProgression()
    {
        int subCount = 0;
        foreach(string s in localInventory) if(s.StartsWith("sub_")) subCount++;
        
        // Only verify this if we haven't unlocked yet
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

    public void RefreshUI() { if (InventoryUI.Instance != null) InventoryUI.Instance.UpdateDisplay(localInventory); }
    
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

    public bool IsItemKnown(string wordID)
    {
        return localInventory.Contains(wordID) || placedItemIDs.Contains(wordID) || consumedItemIDs.Contains(wordID);
    }
}