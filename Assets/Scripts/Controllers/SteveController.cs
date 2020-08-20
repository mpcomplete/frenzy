using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteveController : MonoBehaviour {
  public const int MAX_MINIONS = 2048;

  public Team Team1;
  public Team Team2;

  public MinionSystem MinionSystem = new MinionSystem();
  public BaseSystem BaseSystem = new BaseSystem();

  void Start() {
    Team1.Minions = FindObjectsOfType<Minion>().ToList();
  }

  void Update() {
    float dt = Time.deltaTime;

    MinionSystem.Execute(Team1.Stanchion, Team1.Minions, dt);
    MinionSystem.Execute(Team2.Stanchion, Team2.Minions, dt);
    BaseSystem.Update(Team1, dt);
    BaseSystem.Update(Team2, dt);
  }

  void OnDrawGizmos() {
    void RenderFightStatus(List<Minion> minions, Player player) {
      foreach (var minion in minions) {
        bool inRangeToFight = Vector3.Distance(minion.transform.position, player.transform.position) <= minion.FightRadius;

        if (inRangeToFight) {
          Gizmos.color = Color.red;
          Gizmos.DrawLine(minion.transform.position, player.transform.position);
        } else {
          Gizmos.color = Color.yellow;
          Gizmos.DrawWireSphere(minion.transform.position, minion.FightRadius);
        }
      }
    }

    RenderFightStatus(Team1.Minions, Team2.Player);
    RenderFightStatus(Team2.Minions, Team1.Player);
  }
}