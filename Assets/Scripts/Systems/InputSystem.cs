using UnityEngine;
using UnityEngine.AI;

public class InputSystem {
  public void Update(Team team, float dt) {
    if (!team.Player.Alive)
      return;

    Vector2 movement = Vector2.zero;

    if (Input.GetKey(team.KeyMap.MoveUp))     movement += Vector2.up;
    if (Input.GetKey(team.KeyMap.MoveRight))  movement += Vector2.right;
    if (Input.GetKey(team.KeyMap.MoveDown))   movement += Vector2.down;
    if (Input.GetKey(team.KeyMap.MoveLeft))   movement += Vector2.left;

    if (movement != Vector2.zero) {
      movement = movement.normalized * team.Player.Speed * dt;
      Vector3 newPos = team.Player.transform.position + new Vector3(movement.x, 0, movement.y);
      if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, .3f, NavMesh.AllAreas))
        team.Player.transform.position = hit.position;
    }
  }
}