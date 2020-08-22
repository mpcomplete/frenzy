using UnityEngine;

[CreateAssetMenu]
public class AbilityConfig : ScriptableObject {
  public float MaxNavmeshSearchHeight = 2f;

  public AnimationCurve DashCurve;
  public float DashDuration;
  public float DashDistance;

  public float StunDuration = 2f;
  public float StunRadius = 1f;

  public Projectile FireBallPrefab;
  public float FireballSpeed;
  public float FireBallLifespan;
  public float FireBallDamage;
}