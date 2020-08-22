using UnityEngine;

public class TriggerUnlockSpawner : MonoBehaviour {
  public Base Spawner;
  public void OnTriggered() {
    Spawner.NextSpawnTime = Time.time;
    Spawner.gameObject.SetActive(true);
    Destroy(gameObject);
  }
}
