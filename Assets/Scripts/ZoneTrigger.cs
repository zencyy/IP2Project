using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public enum ZoneType { SubjectZone, VerbZone, SVStation, ObjectZone, SVOStation }
    public ZoneType zoneType;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Tell the manager we arrived!
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.PlayerReachedZone(zoneType);
            }
            
            // Optional: Disable this trigger so it doesn't fire again
            gameObject.SetActive(false); 
        }
    }
}