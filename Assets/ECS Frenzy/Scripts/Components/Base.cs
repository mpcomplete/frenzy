using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Base : IComponentData {
    [GhostField] public float SpawnCooldown;
    public float NextSpawnTime;
    public Entity SpawnLocation;
    public Entity MinionPrefab;
  }

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class BaseSystem : ComponentSystem {
    protected override void OnUpdate() {
      Entities.ForEach((ref Base spawner) => {
        if (Time.ElapsedTime < spawner.NextSpawnTime)
          return;
        Entity minion = EntityManager.Instantiate(spawner.MinionPrefab);
        var transform = EntityManager.GetComponentData<LocalToWorld>(spawner.SpawnLocation);
        EntityManager.SetComponentData(minion, new Translation { Value = transform.Position });
        EntityManager.SetComponentData(minion, new Rotation { Value = transform.Rotation });
        spawner.NextSpawnTime = (float)Time.ElapsedTime + spawner.SpawnCooldown;
      });
    }
  }
}