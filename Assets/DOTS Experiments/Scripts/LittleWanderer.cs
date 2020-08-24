using UnityEngine;
using UnityEngine.AI;

public class LittleWanderer : MonoBehaviour {
  public NavMeshAgent NavMeshAgent;
  public Transform Destination;

  public void Start() {
    NavMeshAgent.SetDestination(Destination.position);
  }
}