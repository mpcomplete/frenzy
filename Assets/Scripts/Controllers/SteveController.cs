using UnityEngine;

public class SteveController : MonoBehaviour {
  public const int MAX_MINIONS = 2048;

  public Team Team1;
  public Team Team2;

  public InputSystem InputSystem = new InputSystem();
  public MinionSystem MinionSystem = new MinionSystem();
  public BaseSystem BaseSystem = new BaseSystem();

  void Start() {
    // Make Steve the happiest.
    Team1.Player.AssignTeam(Team1);
    Team2.Player.AssignTeam(Team2);
  }

  void Update() {
    float dt = Time.deltaTime;

    InputSystem.Update(Team1, dt);
    InputSystem.Update(Team2, dt);
    MinionSystem.Execute(Team1, dt);
    MinionSystem.Execute(Team2, dt);
    BaseSystem.Update(Team1, dt);
    BaseSystem.Update(Team2, dt);
  }
}