using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProjectileSystem {
  public void Execute(Team team, float dt) {
    for (int i = team.Projectiles.Count - 1; i >= 0; i--) {
      Projectile projectile = team.Projectiles[i];
      Vector3 previousPosition = projectile.transform.position;
      Vector3 nextPosition = previousPosition + projectile.Velocity * dt;
      Vector3 delta = nextPosition - previousPosition;
      Vector3 direction = delta.normalized;
      float distance = delta.magnitude;

      if (Physics.SphereCast(previousPosition, projectile.Radius, direction, out RaycastHit hit, distance, projectile.TargetLayerMask)) {
        projectile.transform.position = hit.point;
        projectile.DeathTimer = 0;
        Projectile.Instantiate(projectile.DeathSplosion, projectile.transform.position, Quaternion.identity);
        if (hit.transform.TryGetComponent(out Unit unit)) {
          unit.TakeDamage(team, projectile.Damage);
        }
        Debug.Log($"You hit {hit.transform.name}");
      } else {
        projectile.transform.position = nextPosition;
      }

      // dec deathtimers
      projectile.DeathTimer -= dt;

      // remove dead
      if (projectile.DeathTimer <= 0) {
        Projectile.Destroy(projectile.gameObject);
        team.Projectiles.Remove(projectile);
      }
    }
  }
}