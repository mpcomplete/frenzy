using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour {
  public float Health = 100f;
  public float MaxHealth = 100f;

  public StatusEffects StatusEffects;
  public WeaponInfo Weapon;
  public Unit Target;

  [HideInInspector]
  public Team Team = null;

  public bool Alive { get => Health > 0f; }

  public virtual void AssignTeam(Team team) {
    this.Team = team;
    MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
    renderer.sharedMaterial = team.TeamConfiguration.Material;
  }

  public bool CanAttack(Unit target) {
    return (Vector3.Distance(target.transform.position, transform.position) < Weapon.Range);
  }

  bool isAttacking = false;
  public void Attack() {
    if (!isAttacking)
      StartCoroutine(AttackAnimation());
  }

  IEnumerator AttackAnimation() {
    Vector3 baseScale = transform.localScale;
    Vector3 targetScale = new Vector3(1.25f, .75f, 1.25f);

    isAttacking = true;
    for (float t = 0f; t < Weapon.AnimationPre; t += Time.deltaTime) {
      transform.localScale = Vector3.Lerp(baseScale, targetScale, 1 - Mathf.Pow(1 - t/Weapon.AnimationPre, 5f));
      yield return null;
    }

    // Deal damage.
    foreach (var target in FindTargets()) {
      target.TakeDamage(Weapon.Damage);
    }

    for (float t = 0f; t < Weapon.AnimationPost; t += Time.deltaTime) {
      transform.localScale = Vector3.Lerp(targetScale, baseScale, 1 - Mathf.Pow(1 - t/Weapon.AnimationPost, 5f));
      yield return null;
    }

    isAttacking = false;
    transform.localScale = baseScale;
  }

  static Collider[] hitResults = new Collider[256];
  IEnumerable<Unit> FindTargets() {
    if (Weapon.Targeted) {
      yield return Target;
    } else {
      int layers = Team.TeamConfiguration.AttackablePlayerLayerMask | Team.TeamConfiguration.AttackableMinionLayerMask;
      int numResults = Physics.OverlapSphereNonAlloc(transform.localPosition, Weapon.Range, hitResults, layers);
      for (int i = 0; i < numResults; i++) {
        Unit unit = hitResults[i].GetComponent<Unit>();
        if (unit && unit.Team != Team)
          yield return unit;
      }
    }
  }

  void TakeDamage(float damage) {
    if (!Alive)
      return;
    Health -= damage;
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
