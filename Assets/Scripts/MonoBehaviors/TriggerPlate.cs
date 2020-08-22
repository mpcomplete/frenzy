using UnityEngine;
using UnityEngine.Events;

public class TriggerPlate : MonoBehaviour {
  public UnityEvent Action;
  public float TriggerDelay = 3f;

  Player playerOnTrigger = null;
  float triggerEndTime;

  void Update() {
    if (playerOnTrigger) {
      if (Time.time >= triggerEndTime)
        OnActivated();
    }
  }

  void OnTriggerEnter(Collider other) {
    if (other.gameObject.GetComponent<Player>() is Player p) {
      playerOnTrigger = p;
      triggerEndTime = Time.time + TriggerDelay;
    }
  }

  void OnTriggerExit(Collider other) {
    if (other.gameObject.GetComponent<Player>() is Player p && p == playerOnTrigger) {
      playerOnTrigger = null;
    }
  }

  void OnActivated() {
    triggerEndTime = Time.time + TriggerDelay;
    Action.Invoke();
  }
}
