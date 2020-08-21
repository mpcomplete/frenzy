using UnityEngine;

[CreateAssetMenu]
public class AbilityConfig : ScriptableObject {
  public float MaxNavmeshSearchHeight = 2f;

  public AnimationCurve DashCurve;
  public float DashDuration;
  public float DashDistance;

  public float StunUpswingDuration = .4f;
  public float StunDownswingDuration = .05f;
  public float StunDuration = 2f;
  public float StunRadius = 1f;
}