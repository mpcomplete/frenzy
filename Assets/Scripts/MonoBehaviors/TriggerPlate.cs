using UnityEngine;
using UnityEngine.Events;

public class TriggerPlate : MonoBehaviour {
  public Team Team;
  public MeshRenderer Renderer;
  public Material MaterialEnabled;
  public Material MaterialDisabled;
  public UnityEvent Action;
  public float TriggerDelay = 3f;
  public float Cost;

  Player playerOnTrigger = null;
  float triggerEndTime = -1f;

  float fractionComplete => 1f - Mathf.Max(0, triggerEndTime - Time.time) / TriggerDelay;

  void Update() {
    UpdateMaterial();

    if (playerOnTrigger && CanPurchase()) {
      if (triggerEndTime < 0) {
        triggerEndTime = Time.time + TriggerDelay;
      } else if (Time.time >= triggerEndTime) {
        OnActivated();
      }
    }
  }

  void OnTriggerEnter(Collider other) {
    if (other.gameObject.GetComponent<Player>() is Player p)
      playerOnTrigger = p;
  }

  void OnTriggerExit(Collider other) {
    if (other.gameObject.GetComponent<Player>() is Player p && p == playerOnTrigger) {
      playerOnTrigger = null;
      triggerEndTime = -1f;
    }
  }

  void OnActivated() {
    triggerEndTime = -1f;
    Action.Invoke();
  }

  bool CanPurchase() {
    return Team.Money >= Cost;
  }

  public void SetCost(float cost) {
    Cost = cost;
    UpdateMaterial();
  }

  void UpdateMaterial() {
    if (CanPurchase()) {
      Renderer.sharedMaterial = MaterialEnabled;
      Renderer.material.SetFloat("Fraction", playerOnTrigger ? fractionComplete : 0);
    } else {
      Renderer.sharedMaterial = MaterialDisabled;
    }
  }
}
