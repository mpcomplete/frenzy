using UnityEngine;

public class TriggerUnlockSpawner : MonoBehaviour {
  public Base Spawner;
  public Minion MinionPrefab;
  Team team;

  void Start() {
    var trigger = GetComponent<TriggerPlate>();
    trigger.Cost = 100;  // TODO: cost based on minion type?
    team = trigger.Team;
  }

  public void OnTriggered() {
    Spawner.NextSpawnTime = Time.time;
    Spawner.MinionPrefab = MinionPrefab;
    Spawner.gameObject.SetActive(true);
    team.Money -= 100f;
    Destroy(transform.parent.gameObject); // TODO: hacky
  }

  void OnGUI() {
    Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
    GUI.Label(new Rect(pos.x - 10, Camera.main.pixelHeight - pos.y - 24, 100, 24), $"{MinionPrefab.name.Substring(0,2)}");
  }
}
