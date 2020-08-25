using UnityEngine;

public class TriggerUnlockSpawner : MonoBehaviour {
  public Base Spawner;
  public Minion MinionPrefab;

  void Start() {
    GetComponent<TriggerPlate>().Cost = 100;  // TODO: cost based on minion type?
  }

  public void OnTriggered() {
    Spawner.NextSpawnTime = Time.time;
    Spawner.MinionPrefab = MinionPrefab;
    Spawner.gameObject.SetActive(true);
    Destroy(transform.parent.gameObject); // TODO: hacky
  }
}
