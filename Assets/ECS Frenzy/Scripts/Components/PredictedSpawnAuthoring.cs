using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

public class PredictedSpawnAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    if (dstManager.World.GetExistingSystem<ClientSimulationSystemGroup>() != null) {
      dstManager.AddComponent<PredictedGhostSpawnRequestComponent>(entity);
    }
  }
}