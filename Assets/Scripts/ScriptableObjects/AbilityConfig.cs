using UnityEngine;

[CreateAssetMenu]
public class AbilityConfig : ScriptableObject {
  public AnimationCurve DashCurve;
  public float DashDuration;
  public float DashDistance;
}