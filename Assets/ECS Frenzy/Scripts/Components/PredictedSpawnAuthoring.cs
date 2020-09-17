using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

[GhostComponent(PrefabType=GhostPrefabType.PredictedClient)]
public class PredictedSpawnAuthoring : MonoBehaviour, IConvertGameObjectToEntity {
  public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
    dstManager.AddComponent<PredictedGhostSpawnRequestComponent>(entity);
  }
}