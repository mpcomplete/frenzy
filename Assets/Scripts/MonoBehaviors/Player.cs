using UnityEngine;
using UnityEngine.AI;

public class Player : Unit {
  public float Speed = 1f;

  [SerializeField] CharacterController controller;

  void Update() {
    if (!Alive)
      return;

    Vector2 movement = Vector2.zero;
    if (Team.TeamConfiguration.ControlScheme == 1) { // FIXME: custom control schemes
      if (Input.GetKey(KeyCode.W)) movement += Vector2.up;
      if (Input.GetKey(KeyCode.A)) movement += Vector2.left;
      if (Input.GetKey(KeyCode.S)) movement += Vector2.down;
      if (Input.GetKey(KeyCode.D)) movement += Vector2.right;
      if (Input.GetKey(KeyCode.Q)) Attack();
    } else {
      if (Input.GetKey(KeyCode.UpArrow)) movement += Vector2.up;
      if (Input.GetKey(KeyCode.LeftArrow)) movement += Vector2.left;
      if (Input.GetKey(KeyCode.DownArrow)) movement += Vector2.down;
      if (Input.GetKey(KeyCode.RightArrow)) movement += Vector2.right;
      if (Input.GetKey(KeyCode.RightControl)) Attack();
    }

    if (movement != Vector2.zero) {
      movement = movement.normalized * Speed * Time.deltaTime;
      Vector3 newPos = transform.position + new Vector3(movement.x, 0, movement.y);
      if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, .3f, NavMesh.AllAreas))
        transform.position = hit.position;
    }
  }

  void OnGUI() {
    Vector3 pos = Camera.main.WorldToScreenPoint(this.transform.position);
    GUI.Label(new Rect(pos.x - 10, Camera.main.pixelHeight - pos.y - 24, 100, 24), $"{Health}");
  }
}
