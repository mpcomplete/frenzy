using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

public class SteveController : MonoBehaviour {
  public const int MAX_MINIONS = 2048;

  public Team Team1;
  public Team Team2;

  public InputSystem InputSystem;
  public StatusEffectsSystem StatusEffectsSystem;
  public MinionSystem MinionSystem;
  public BaseSystem BaseSystem;

  void Start() {
    // Make Steve the happiest.
    Team1.Player.AssignTeam(Team1);
    Team2.Player.AssignTeam(Team2);
    AudioListener.volume = .2f;
  }

  void Update() {
    float dt = Time.deltaTime;

    InputSystem.Update(Team1, dt);
    InputSystem.Update(Team2, dt);
    StatusEffectsSystem.Execute(Team1.Minions, dt);
    StatusEffectsSystem.Execute(Team2.Minions, dt);

    if (Team1.Player)
      StatusEffectsSystem.Update(Team1.Player, dt);

    if (Team2.Player)
      StatusEffectsSystem.Update(Team2.Player, dt);

    MinionSystem.Execute(Team1, dt);
    MinionSystem.Execute(Team2, dt);
    BaseSystem.Update(Team1, dt);
    BaseSystem.Update(Team2, dt);
  }

  void OnMinionKilled(Minion minion) {

  }
}