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
}
