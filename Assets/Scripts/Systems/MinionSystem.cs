using System.Collections.Generic;

public class MinionSystem {
  public void Execute(Stanchion stanchion, List<Minion> minions, float dt) {
    for (int i = 0; i < minions.Count; i++) {
      Update(stanchion, minions[i], dt);
    }
  }

  public void Update(Stanchion stanchion, Minion minion, float dt) {
    switch (minion.CurrentBehavior) {
    case Minion.Behavior.Idle:
      // search for something to pursue or fight
      if (stanchion != null) {
        minion.CurrentBehavior = Minion.Behavior.Pursue;
        minion.NavMeshAgent.SetDestination(stanchion.transform.position);
      }
    break;

    case Minion.Behavior.Pursue:
      // move towards that thing!
    break;

    case Minion.Behavior.Fight:
    // do fighting stuff
    break;
    }
  }
}