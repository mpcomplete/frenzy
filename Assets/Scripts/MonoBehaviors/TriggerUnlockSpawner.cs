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

  void OnGUI() {
    Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
    GUI.color = Color.black;
    GUI.Label(new Rect(pos.x - 10, Camera.main.pixelHeight - pos.y - 24, 100, 24), $"{MinionPrefab.name.Substring(0,2)}");
  }
}
