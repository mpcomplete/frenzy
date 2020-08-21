using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static CoroutineHelpers;

[System.Serializable]
public class InputSystem {
  public AbilityConfig AbilityConfig;

  IEnumerator Stun(Player player) {
    Debug.Log("Stun Start");
    Cooldown.Begin(ref player.Ability1Cooldown);
    yield return new WaitForSeconds(1.5f);
    player.AbilityRoutine = null;
    Debug.Log("Stun End");
  }

  IEnumerator Fire(Player player) {
    Debug.Log("Fire Start");
    Cooldown.Begin(ref player.Ability2Cooldown);
    yield return null;
    player.AbilityRoutine = null;
    Debug.Log("Fire End");
  }

  IEnumerator Dash(Player player) {
    Vector3 startPosition = player.transform.position;
    Vector3 endPosition = player.transform.forward * AbilityConfig.DashDistance + startPosition;

    void Move(float t, float duration) {
      float i = AbilityConfig.DashCurve.Evaluate(t / duration);
      TryMove(Vector3.Lerp(startPosition, endPosition, i), player.transform);
    }

    Cooldown.Begin(ref player.Ability3Cooldown);
    yield return EveryFrameForNSeconds(AbilityConfig.DashDuration, Move);
    player.AbilityRoutine = null;
  }

  IEnumerator Ultimate(Player player) {
    Debug.Log("Ultimate Start");
    Cooldown.Begin(ref player.Ability4Cooldown);
    yield return null;
    player.AbilityRoutine = null;
    Debug.Log("Ultimate End");
  }

  void TryMove(Vector3 targetPosition, Transform t) {
    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, .3f, NavMesh.AllAreas)) {
      t.position = hit.position;
    }
  }

  public void Update(Team team, float dt) {
    Player player = team.Player;

    if (!player.Alive)
      return;

    Cooldown.Tick(ref player.Ability1Cooldown, dt);
    Cooldown.Tick(ref player.Ability2Cooldown, dt);
    Cooldown.Tick(ref player.Ability3Cooldown, dt);
    Cooldown.Tick(ref player.Ability4Cooldown, dt);

    if (player.AbilityRoutine == null) {
      if (Input.GetKeyDown(team.KeyMap.Ability1)) {
        if (player.Ability1Cooldown.TimeRemaining <= 0) {
          player.AbilityRoutine = player.StartCoroutine(Stun(player));
        } else {
          Debug.Log($"Still {player.Ability1Cooldown.TimeRemaining} seconds remaining");
        }
      } else if (Input.GetKeyDown(team.KeyMap.Ability2)) {
        if (player.Ability2Cooldown.TimeRemaining <= 0) {
          player.AbilityRoutine = player.StartCoroutine(Fire(player));
        } else {
          Debug.Log($"Still {player.Ability2Cooldown.TimeRemaining} seconds remaining");
        }
      } else if (Input.GetKeyDown(team.KeyMap.Ability3)) {
        if (player.Ability3Cooldown.TimeRemaining <= 0) {
          player.AbilityRoutine = player.StartCoroutine(Dash(player));
        } else {
          Debug.Log($"Still {player.Ability3Cooldown.TimeRemaining} seconds remaining");
        }
      } else if (Input.GetKeyDown(team.KeyMap.Ability4)) {
        if (player.Ability4Cooldown.TimeRemaining <= 0) {
          player.AbilityRoutine = player.StartCoroutine(Ultimate(player));
        } else {
          Debug.Log($"Still {player.Ability4Cooldown.TimeRemaining} seconds remaining");
        } 
      } else if (Input.GetKeyDown(team.KeyMap.ToggleStanchion)) {
        team.Stanchion.transform.position = player.transform.position;
      }
    }

    Vector2 axes = Vector2.zero;

    if (Input.GetKey(team.KeyMap.MoveUp))     axes += Vector2.up;
    if (Input.GetKey(team.KeyMap.MoveRight))  axes += Vector2.right;
    if (Input.GetKey(team.KeyMap.MoveDown))   axes += Vector2.down;
    if (Input.GetKey(team.KeyMap.MoveLeft))   axes += Vector2.left;

    if (axes != Vector2.zero) {
      Vector3 heading = new Vector3(axes.x, 0, axes.y).normalized;
      Vector3 delta = heading * player.Speed * dt;

      player.transform.rotation = Quaternion.LookRotation(heading, Vector3.up);
      TryMove(player.transform.position + delta, player.transform);
    }
  }
}