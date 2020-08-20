using UnityEngine.AI;
using UnityEngine;

public class Minion : Unit {
  public enum Behavior { Idle, Pursue, Fight }

  public Behavior CurrentBehavior;
  public NavMeshAgent NavMeshAgent;
  public float FightRadius = 3f;
}
