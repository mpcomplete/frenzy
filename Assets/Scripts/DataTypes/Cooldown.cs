using UnityEngine;

[System.Serializable]
public struct Cooldown {
  public float Duration;
  public float TimeRemaining;
  public static void Tick(ref Cooldown cd, float dt) {
    cd.TimeRemaining = Mathf.Max(0, cd.TimeRemaining - dt);
  }

  public static void Begin(ref Cooldown cd) {
    cd.TimeRemaining = cd.Duration;
  }
}