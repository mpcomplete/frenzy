using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public static class SystemConfig {
  public static RenderedPlayer RenderedPlayerPrefab;
  public static float PlayerMoveSpeed;
  public static float ControllerDeadzone = .19f;
}

public class SystemsConfiguration : MonoBehaviour {
  public RenderedPlayer RenderedPlayerPrefab;
  public float PlayerMoveSpeed = 5; 

  void Awake() {
    SystemConfig.RenderedPlayerPrefab = RenderedPlayerPrefab;
    SystemConfig.PlayerMoveSpeed = PlayerMoveSpeed;
  }
}