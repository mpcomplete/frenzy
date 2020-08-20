using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour {
  public float Health = 100f;
  public float MaxHealth = 100f;

  public float AttackPre = .3f;
  public float AttackPost = .2f;

  public bool Alive { get => Health > 0f; }

  bool isAttacking = false;
  public void Attack() {
    if (!isAttacking)
      StartCoroutine(AttackAnimation());
  }

  IEnumerator AttackAnimation() {
    float scale = 1f;
    Vector3 baseScale = transform.localScale;

    isAttacking = true;
    for (float t = 0f; t < AttackPre; t += Time.deltaTime) {
      scale = Mathf.Lerp(1f, 1.5f, 1 - Mathf.Pow(1 - t/AttackPre, 5f));
      transform.localScale = scale * baseScale;
      yield return null;
    }
    // deal damage
    for (float t = 0f; t < AttackPost; t += Time.deltaTime) {
      scale = Mathf.Lerp(1.5f, 1f, 1 - Mathf.Pow(1 - t/AttackPost, 5f));
      transform.localScale = scale * baseScale;
      yield return null;
    }

    isAttacking = false;
    transform.localScale = baseScale;
  }
}
