using UnityEngine;

public class TriggerUnlockSpawner : MonoBehaviour {
  public Base Spawner;
  public void OnTriggered(Minion minionPrefab) {
    Spawner.NextSpawnTime = Time.time;
    Spawner.MinionPrefab = minionPrefab;
    Spawner.gameObject.SetActive(true);
    Destroy(gameObject);
  }
}
