using UnityEngine;

public class MinionSystem {
  public void Execute(Team team, float dt) {
    for (int i = 0; i < team.Minions.Count; i++) {
      Update(team, team.Minions[i], dt);
    }
  }

  Collider[] colliders = new Collider[256];
  Unit SearchForTarget(LayerMask lm, Minion minion) {
    int potentialTargetCount = Physics.OverlapSphereNonAlloc(minion.transform.position, minion.FightRadius, colliders, lm);

    for (int i = 0; i < potentialTargetCount; i++) {
      if (colliders[i].TryGetComponent(out Unit targetMinion)) {
        return targetMinion;
      }
    }
    return null;
  }

  public void Update(Team team, Minion minion, float dt) {
    switch (minion.CurrentBehavior) {
    case Minion.Behavior.Idle:
    case Minion.Behavior.Traveling:
      LayerMask layerMask = team.TeamConfiguration.AttackableMinionLayerMask | team.TeamConfiguration.AttackablePlayerLayerMask;
      Unit target = SearchForTarget(layerMask, minion);

      if (target) {
        minion.Target = target;
        minion.CurrentBehavior = Minion.Behavior.Fighting;
      } else if (team.Stanchion != null) {
        minion.CurrentBehavior = Minion.Behavior.Traveling;
        minion.NavMeshAgent.SetDestination(team.Stanchion.transform.position);
      } else {
        minion.CurrentBehavior = Minion.Behavior.Idle;
      }
    break;

    case Minion.Behavior.Fighting:
      if (minion.Target && minion.Target.Alive) {
        minion.NavMeshAgent.SetDestination(minion.Target.transform.position);
        if (Vector3.Distance(minion.Target.transform.position, minion.transform.position) < minion.AttackRadius) {
          minion.Attack();
        }
      } else {
        minion.Target = null;
        minion.CurrentBehavior = Minion.Behavior.Idle;
      }
    break;
    }
  }
}