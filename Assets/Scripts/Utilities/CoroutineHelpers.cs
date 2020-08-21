using System.Collections;
using UnityEngine;

public static class CoroutineHelpers {
  public static IEnumerator EveryFrameForNSeconds(float duration, System.Action<float, float> action) {
    float timer = 0;

    while (timer < duration) {
      yield return null;  
      timer = Mathf.Min(duration, timer + Time.deltaTime);
      action.Invoke(timer, duration);
    }
  }
}