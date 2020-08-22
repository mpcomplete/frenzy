using UnityEngine;

public class TeamSkinnable : MonoBehaviour {
  public MeshRenderer Renderer;

  public void AssignTeam(Team team) {
    Renderer.sharedMaterial = team.TeamConfiguration.Material;
  }
}
