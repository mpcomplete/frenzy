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

  void Update() {
    float dt = Time.deltaTime;

    MinionSystem.Execute(Team1, dt);
    MinionSystem.Execute(Team2, dt);
    BaseSystem.Update(Team1, dt);
    BaseSystem.Update(Team2, dt);
  }
}