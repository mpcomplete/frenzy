using UnityEngine;

public class TriggerUpgrade : MonoBehaviour {
  public UpgradeType Upgrade;
  TriggerPlate trigger;
  Team team;

  void Start() {
    trigger = GetComponent<TriggerPlate>();
    team = trigger.Team;
    UpdateCost();
  }

  void UpdateCost() {
    trigger.SetCost(team.GetCostForNextUpgrade(Upgrade));
  }

  public void OnTriggered() {
    team.PurchaseNextUpgrade(Upgrade);
    UpdateCost();
  }

  void OnGUI() {
    Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
    GUI.color = Color.black;
    string label = Upgrade.ToString();
    label = label.Substring(0, 1) + label.Substring(6, 1);
    GUI.Label(new Rect(pos.x - 10, Camera.main.pixelHeight - pos.y - 24, 100, 24), label);
  }
}
