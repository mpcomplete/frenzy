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
    Vector3 baseScale = transform.localScale;
    Vector3 targetScale = new Vector3(1.25f, .75f, 1.25f);

    isAttacking = true;
    for (float t = 0f; t < AttackPre; t += Time.deltaTime) {
      transform.localScale = Vector3.Lerp(baseScale, targetScale, 1 - Mathf.Pow(1 - t/AttackPre, 5f));
      yield return null;
    }
    // deal damage
    for (float t = 0f; t < AttackPost; t += Time.deltaTime) {
      transform.localScale = Vector3.Lerp(targetScale, baseScale, 1 - Mathf.Pow(1 - t/AttackPost, 5f));
      yield return null;
    }

    isAttacking = false;
    transform.localScale = baseScale;
  }
}
