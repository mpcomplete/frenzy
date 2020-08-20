using UnityEngine.AI;
using UnityEngine;

public class Minion : MonoBehaviour {
  public enum Behavior { Idle, Pursue, Fight }

  public Behavior CurrentBehavior;
  public NavMeshAgent NavMeshAgent;
  public float FightRadius = 3f;
}
