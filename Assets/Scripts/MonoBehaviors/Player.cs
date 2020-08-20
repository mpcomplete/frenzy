using UnityEngine;
using UnityEngine.AI;

public enum PlayerTeam {
  One = 1,
  Two = 2,
}

public class Player : Unit {
  public PlayerTeam Team = PlayerTeam.One;
  public float Speed = 1f;

  CharacterController controller;

  private void Start() {
    controller = GetComponent<CharacterController>();
  }

  // Update is called once per frame
  void Update() {
    Vector2 movement = Vector2.zero;
    if (Team == PlayerTeam.One) {
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
}
