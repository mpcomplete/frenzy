using UnityEngine;

[System.Serializable]
public struct Cooldown {
  public float Duration;
  public float TimeRemaining;

  public bool Ready => TimeRemaining <= 0;

  public void Tick(float dt) {
    TimeRemaining = Mathf.Max(0, TimeRemaining - dt);
  }

  public void Begin() {
    TimeRemaining = Duration;
  }
}