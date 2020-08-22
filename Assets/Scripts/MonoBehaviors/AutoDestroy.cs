using UnityEngine;

public class AutoDestroy : MonoBehaviour {
  public void Destroy() {
    Destroy(gameObject);
  }
}