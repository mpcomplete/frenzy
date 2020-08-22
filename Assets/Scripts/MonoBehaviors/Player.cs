using UnityEngine;

public class Player : Unit {
  public float Speed = 1f;

  public bool IsMobile = true;
  public bool IsStunned { get => StatusEffects.StunTimeRemaining > 0; }

  public Cooldown Ability1Cooldown;
  public Cooldown Ability2Cooldown;
  public Cooldown Ability3Cooldown;
  public Cooldown Ability4Cooldown;
  public Coroutine AbilityRoutine = null;

  [SerializeField] CharacterController controller;

  public override void AssignTeam(Team team) {
    base.AssignTeam(team);
    gameObject.layer = team.TeamConfiguration.PlayerLayer;
  }

  void OnGUI() {
    Vector3 pos = Camera.main.WorldToScreenPoint(transform.position);

    GUI.Label(new Rect(pos.x - 10, Camera.main.pixelHeight - pos.y - 24, 100, 24), $"{Health}");
  }
}