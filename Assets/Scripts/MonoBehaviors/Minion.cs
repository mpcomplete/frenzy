using UnityEngine.AI;
using UnityEngine;

public class Minion : Unit {
  public enum Behavior { Idle, Traveling, Fighting }

  public Behavior CurrentBehavior;
  public NavMeshAgent NavMeshAgent;
  public Unit Target;
  public float FightRadius = 3f;

  public Team SteveTeam = null;

  void OnDestroy() {
    SteveTeam.Minions.Remove(this);
  }
}