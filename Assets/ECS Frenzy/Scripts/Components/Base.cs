﻿using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine.AI;

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
      Entities.ForEach((ref Base spawner, ref Team team) => {
        if (Time.ElapsedTime < spawner.NextSpawnTime)
          return;

        Entity minion = EntityManager.Instantiate(spawner.MinionPrefab);
        var transform = EntityManager.GetComponentData<LocalToWorld>(spawner.SpawnLocation);
        float3 position = transform.Position;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, 1))
          position = hit.position;
        EntityManager.SetComponentData(minion, new Translation { Value = position });
        EntityManager.SetComponentData(minion, new Rotation { Value = transform.Rotation });
        EntityManager.AddComponentData(minion, new Heading { Value = transform.Forward });
        EntityManager.AddComponentData(minion, new Team { Value = team.Value });
        spawner.NextSpawnTime = (float)Time.ElapsedTime + spawner.SpawnCooldown;
      });
    }
  }
}
