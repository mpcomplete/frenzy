using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour {
  public float Health = 100f;
  public float MaxHealth = 100f;

  public float AttackRadius = .75f;
  public float AttackDamage = 5f;

  // Animation
  public float AttackPre = .3f;
  public float AttackPost = .2f;

  [HideInInspector]
  public Team Team = null;

  public ParticleSystem BloodParticles;

  public bool Alive { get => Health > 0f; }

  bool isAttacking = false;
  public void Attack() {
    if (!isAttacking)
      StartCoroutine(AttackAnimation());
  }

  static Collider[] hitResults = new Collider[32];
  IEnumerator AttackAnimation() {
    Vector3 baseScale = transform.localScale;
    Vector3 targetScale = new Vector3(1.25f, .75f, 1.25f);

    isAttacking = true;
    for (float t = 0f; t < AttackPre; t += Time.deltaTime) {
      transform.localScale = Vector3.Lerp(baseScale, targetScale, 1 - Mathf.Pow(1 - t/AttackPre, 5f));
      yield return null;
    }

    // Deal damage.
    int numResults = Physics.OverlapSphereNonAlloc(transform.localPosition, AttackRadius, hitResults, Team.TeamConfiguration.AttackablePlayerLayerMask | Team.TeamConfiguration.AttackableMinionLayerMask);
    for (int i = 0; i < numResults; i++) {
      Unit unit = hitResults[i].GetComponent<Unit>();
      if (unit && unit.Team != Team)
        unit.TakeDamage(AttackDamage);
    }

    for (float t = 0f; t < AttackPost; t += Time.deltaTime) {
      transform.localScale = Vector3.Lerp(targetScale, baseScale, 1 - Mathf.Pow(1 - t/AttackPost, 5f));
      yield return null;
    }

    isAttacking = false;
    transform.localScale = baseScale;
  }

  void TakeDamage(float damage) {
    if (!Alive)
      return;
    Health -= damage;
    BloodParticles.Play();
    if (!Alive) {
      StartCoroutine(DeathAnimation());
    }
  }

  IEnumerator DeathAnimation() {
    Vector3 baseScale = transform.localScale;
    Vector3 targetScale = new Vector3(.01f, 1f, .01f);
    for (float t = 0f; t < 1f; t += Time.deltaTime) {
      transform.localScale = Vector3.Lerp(baseScale, targetScale, 1 - Mathf.Pow(1 - t, 3f));
      yield return null;
    }

    Destroy(gameObject);
  }
}
