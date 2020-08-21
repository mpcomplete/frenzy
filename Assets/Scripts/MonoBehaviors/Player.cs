using UnityEngine;
using UnityEngine.AI;

public class Player : Unit {
  public float Speed = 1f;

  [SerializeField] CharacterController controller;

  void OnGUI() {
    Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);
    GUI.Label(new Rect(pos.x - 10, Camera.main.pixelHeight - pos.y - 24, 100, 24), $"{Health}");
  }
}
