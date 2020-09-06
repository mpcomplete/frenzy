using UnityEngine;

public class SteveController : MonoBehaviour {
  public const int MAX_MINIONS = 2048;

  public Team Team1;
  public Team Team2;

  public UI UI;

  public InputSystem InputSystem;
  public StatusEffectsSystem StatusEffectsSystem;
  public MinionSystem MinionSystem;
  public BaseSystem BaseSystem;
  public ProjectileSystem ProjectileSystem;
  public UISystem UISystem;

  void Start() {
    AudioListener.volume = .02f;
  }

  void Update() {
    float dt = Time.deltaTime;

    InputSystem.Update(Team1, Team2, dt);
    InputSystem.Update(Team2, Team1, dt);
    StatusEffectsSystem.Execute(Team1.Minions, dt);
    StatusEffectsSystem.Execute(Team2.Minions, dt);

    if (Team1.Player) {
      StatusEffectsSystem.Update(Team1.Player, dt);
    }

    if (Team2.Player) {
      StatusEffectsSystem.Update(Team2.Player, dt);
    }

    MinionSystem.Execute(Team1, dt);
    MinionSystem.Execute(Team2, dt);
    BaseSystem.Update(Team1, dt);
    BaseSystem.Update(Team2, dt);

    if (Team1.Player && Team2.Player) {
      UISystem.Update(UI, Team1.Player, Team2.Player, dt);
    }
  }

  void FixedUpdate() {
    float fdt = Time.fixedDeltaTime;
    ProjectileSystem.Execute(Team1, fdt);
    ProjectileSystem.Execute(Team2, fdt);
  }

  private void OnGUI() {
    Vector3 pos = UI.Player1HUD.transform.position;
    GUI.Label(new Rect(pos.x, Camera.main.pixelHeight - pos.y - 20, 100, 24), $"Money: {Team1.Money}");
    pos = UI.Player2HUD.transform.position;
    GUI.Label(new Rect(pos.x - 60, Camera.main.pixelHeight - pos.y - 20, 100, 24), $"Money: {Team2.Money}");
  }
}