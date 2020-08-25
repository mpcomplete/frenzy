using UnityEngine.AI;
using UnityEngine;

public class Minion : Unit {
  public enum Behavior { Idle, Traveling, Fighting }

  public Behavior CurrentBehavior;
  public NavMeshAgent NavMeshAgent;
  public float FightRadius = 3f;

  public override void AssignTeam(Team team) {
    base.AssignTeam(team);
    gameObject.layer = team.TeamConfiguration.MinionLayer;
  }

  protected override void OnDie() {
    Team.Minions.Remove(this);
    Destroy(gameObject);
  }
}