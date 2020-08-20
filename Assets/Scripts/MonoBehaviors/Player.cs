using UnityEngine;

public class Player : MonoBehaviour {
  public float Speed = 1f;
  CharacterController controller;
  private void Start() {
    controller = GetComponent<CharacterController>();
  }

  // Update is called once per frame
  void Update() {
    Vector2 movement = Vector2.zero;
    if (Input.GetKey(KeyCode.W)) movement += Vector2.up;
    if (Input.GetKey(KeyCode.A)) movement += Vector2.left;
    if (Input.GetKey(KeyCode.S)) movement += Vector2.down;
    if (Input.GetKey(KeyCode.D)) movement += Vector2.right;
    if (movement != Vector2.zero) {
      movement = movement.normalized * Speed * Time.deltaTime;
      controller.Move(new Vector3(movement.x, 0, movement.y));
    }
  }
}
