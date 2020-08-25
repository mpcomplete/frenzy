using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static CoroutineHelpers;

[System.Serializable]
public class InputSystem {
  public AbilityConfig AbilityConfig;
  public AudioSource StunSound;
  public AudioSource FireSound;
  public AudioSource DashSound;
  public AudioSource StanchionSound;
  public LayerMask LevelLayerMask;

  Collider[] Colliders = new Collider[256];
  IEnumerator Stun(Team team) {
    Player player = team.Player;
    LayerMask layerMask = team.TeamConfiguration.AttackableMinionLayerMask | team.TeamConfiguration.AttackablePlayerLayerMask;

    player.Ability1Cooldown.Begin();
    player.IsMobile = false;
    StunSound.Play();
    int count = Physics.OverlapSphereNonAlloc(player.transform.position, AbilityConfig.StunRadius, Colliders, layerMask);

    for (int i = 0; i < count; i++) {
      if (Colliders[i].TryGetComponent(out Unit unit)) {
        unit.StatusEffects.StunTimeRemaining = Mathf.Max(unit.StatusEffects.StunTimeRemaining, AbilityConfig.StunDuration);
      }
    }
    player.IsMobile = true;
    yield return null;
    player.AbilityRoutine = null;
  }

  IEnumerator Fire(Team team, Team enemyTeam) {
    Player player = team.Player;
    Vector3 playerStart = player.transform.position + Vector3.up;
    Projectile fireball = Projectile.Instantiate(AbilityConfig.FireBallPrefab, playerStart, player.transform.rotation);

    if (enemyTeam.Player) {
      fireball.Velocity = (enemyTeam.Player.transform.position + Vector3.up - playerStart).normalized * AbilityConfig.FireballSpeed;
      fireball.TargetLayerMask = team.TeamConfiguration.AttackablePlayerLayerMask | LevelLayerMask;
    } else {
      fireball.Velocity = player.transform.forward * AbilityConfig.FireballSpeed;
      fireball.TargetLayerMask = LevelLayerMask;
    }

    fireball.Damage = AbilityConfig.FireBallDamage;
    fireball.DeathTimer = AbilityConfig.FireBallLifespan;
    team.Projectiles.Add(fireball);
    FireSound.Play();
    player.Ability2Cooldown.Begin();
    yield return null;
    player.AbilityRoutine = null;
  }

  IEnumerator Dash(Player player) {
    Vector3 startPosition = player.transform.position;
    Vector3 endPosition = player.transform.forward * AbilityConfig.DashDistance + startPosition;

    // Necessary to ensure a path is calculated assuming you're "on the ground"
    startPosition.y = 0;
    endPosition.y = 0;
    DashSound.Play();
    void Move(float t, float duration) {
      float i = AbilityConfig.DashCurve.Evaluate(t / duration);
      TryMove(Vector3.Lerp(startPosition, endPosition, i), player.transform);
    }

    player.Ability3Cooldown.Begin();
    player.IsMobile = false;
    yield return EveryFrameForNSeconds(AbilityConfig.DashDuration, Move);
    player.IsMobile = true;
    player.AbilityRoutine = null;
  }

  IEnumerator Ultimate(Player player) {
    Debug.Log("Ultimate Start");
    player.Ability4Cooldown.Begin();
    yield return null;
    player.AbilityRoutine = null;
    Debug.Log("Ultimate End");
  }

  void TryMove(Vector3 targetPosition, Transform t) {
    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, AbilityConfig.MaxNavmeshSearchHeight, NavMesh.AllAreas)) {
      t.position = hit.position;
    }
  }

  public void Update(Team team, Team enemyTeam, float dt) {
    Player player = team.Player;

    player.Ability1Cooldown.Tick(dt);
    player.Ability2Cooldown.Tick(dt);
    player.Ability3Cooldown.Tick(dt);

    if (!player.Alive) {
      player.DeathCooldown.Tick(dt);
      if (player.DeathCooldown.Ready) {
        player.Respawn();
      } else {
        return;
      }
    }
    
    if (player.IsStunned)
      return;
    
    if (player.AbilityRoutine == null) {
      if (Input.GetKeyDown(team.KeyMap.Ability1) && player.Ability1Cooldown.TimeRemaining <= 0) {
        player.AbilityRoutine = player.StartCoroutine(Stun(team));
      } else if (Input.GetKeyDown(team.KeyMap.Ability2) && player.Ability2Cooldown.TimeRemaining <= 0) {
        player.AbilityRoutine = player.StartCoroutine(Fire(team, enemyTeam));
      } else if (Input.GetKeyDown(team.KeyMap.Ability3) && player.Ability3Cooldown.TimeRemaining <= 0) {
        player.AbilityRoutine = player.StartCoroutine(Dash(player));
      } else if (Input.GetKeyDown(team.KeyMap.Ability4) && player.Ability4Cooldown.TimeRemaining <= 0) {
        player.AbilityRoutine = player.StartCoroutine(Ultimate(player));
      } else if (Input.GetKeyDown(team.KeyMap.ToggleStanchion)) {
        StanchionSound.Play();
        team.Stanchion.transform.position = player.transform.position;
      } else if (Input.GetKey(team.KeyMap.Attack)) {
        player.Attack();
      }
    }

    if (player.IsMobile) {
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
}