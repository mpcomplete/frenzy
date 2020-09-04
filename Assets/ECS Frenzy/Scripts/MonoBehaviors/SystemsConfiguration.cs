using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class SystemsConfiguration : MonoBehaviour {
  public DroneAssistant DroneAssistant;

  void Start() {
    // Weird way to get access to the world containing the targeted systems...
    foreach (var world in World.All) {
      var dr = world.GetExistingSystem<DroneAssistantRenderingSystem>();

      if (dr != null) {
        dr.DroneAssistant = DroneAssistant;
      }
    }
  }
}