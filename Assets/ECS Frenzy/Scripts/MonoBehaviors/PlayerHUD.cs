using UnityEngine;

namespace ECSFrenzy {
  public class PlayerHUD : MonoBehaviour {
    public Camera Camera;
    public Vector3 PlayerPosition;
    public float TimeRemaining;
    public float LabelWidth = 100;
    public float LabelHeight = 24;

    void OnGUI() {
      var screenPosition = Camera.WorldToScreenPoint(PlayerPosition);
      var rect = new Rect(screenPosition.x, Camera.pixelHeight - screenPosition.y, LabelWidth, LabelHeight);

      GUI.Label(rect, $"{TimeRemaining}");
    }
  }
}