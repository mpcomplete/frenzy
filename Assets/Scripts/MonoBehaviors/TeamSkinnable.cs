using UnityEngine;

public class TeamSkinnable : MonoBehaviour {
  public MeshRenderer Renderer;

  public void AssignTeam(Team team) {
    Renderer.sharedMaterial = team.TeamConfiguration.Material;
  }

  private void OnValidate() {
    Team team = GetComponentInParent<Team>();
    if (team) AssignTeam(team);
  }
}
