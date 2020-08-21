using UnityEngine;

public static class MathHelpers {
  public static float ExponentialLerpTo(float a, float b, float epsilon, float dt) {
    return Mathf.Lerp(a, b, 1 - Mathf.Pow(epsilon, dt));
  }

  public static Vector3 ExponentialLerpTo(Vector3 a, Vector3 b, float epsilon, float dt) {
    return Vector3.Lerp(a, b, 1 - Mathf.Pow(epsilon, dt));
  }

  public static Color ExponentialLerpTo(Color a, Color b, float epsilon, float dt) {
    return Color.Lerp(a, b, 1 - Mathf.Pow(epsilon, dt));
  }

  public static Quaternion ExponentialSlerpTo(Quaternion a, Quaternion b, float epsilon, float dt) {
    return Quaternion.Slerp(a, b, 1 - Mathf.Pow(epsilon, dt));
  }
}