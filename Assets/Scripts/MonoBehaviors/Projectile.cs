using UnityEngine;

public class Projectile : MonoBehaviour {
  public Vector3 Acceleration;
  public Vector3 Velocity;
  public float Damage = 1f;
  public float Radius = 1f;
  public float DeathTimer;
  public Unit Unit;
  public LayerMask TargetLayerMask;
  public GameObject DeathSplosion;

  void OnDrawGizmos() {
    Gizmos.DrawWireSphere(transform.position, Radius);
  }
}