﻿using System.Collections.Generic;
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
      Unit target = SearchForTarget(team.TeamConfiguration.AttackablePlayerLayerMask, minion);

      if (target) {
        minion.Target = target;
        minion.CurrentBehavior = Minion.Behavior.Fighting;
      } else if (team.Stanchion != null) {
        minion.CurrentBehavior = Minion.Behavior.Traveling;
        minion.NavMeshAgent.SetDestination(team.Stanchion.transform.position);
      }
    break;

    case Minion.Behavior.Fighting:
      if (minion.Target) {
        minion.NavMeshAgent.SetDestination(minion.Target.transform.position);
      } else {
        minion.CurrentBehavior = Minion.Behavior.Idle;
      }
    break;
    }
  }
}