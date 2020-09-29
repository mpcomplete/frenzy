using System;
using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  public struct NewSpeculativeSpawnTag : IComponentData {}

  [Serializable]
  [BurstCompile]
  public struct SpeculativeSpawn : IComponentData {
    public Entity OwnerEntity;
    public Entity Entity;
    public uint SpawnTick;
    public uint Identifier;

    public static bool Same(SpeculativeSpawn a, SpeculativeSpawn b) {
      return a.SpawnTick == b.SpawnTick && a.Identifier == b.Identifier;
    }

    public SpeculativeSpawn(Entity ownerEntity, Entity entity, uint spawnTick, uint identifier) {
      OwnerEntity = ownerEntity;
      Entity = entity;
      SpawnTick = spawnTick;
      Identifier = identifier;
    }
  }

  [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
  [UpdateAfter(typeof(GhostSimulationSystemGroup))]
  public class SpeculativeSpawnSystemGroup : ComponentSystemGroup {}

  [UpdateInGroup(typeof(SpeculativeSpawnSystemGroup))]
  public class DestroyInvalidSpeculativeSpawnSystem : SystemBase {
    protected override void OnUpdate() {
      var newSpeculativeQuery = GetEntityQuery(ComponentType.ReadOnly<SpeculativeSpawn>(), ComponentType.ReadOnly<NewSpeculativeSpawnTag>());
      var newSpeculativeEntities = newSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var speculativeSpawns = GetComponentDataFromEntity<SpeculativeSpawn>(true);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      Entities
      .WithName("Destroy_Existing_Speculative_Spawns_That_Were_Not_Resimulated")
      .ForEach((Entity e, in SpeculativeSpawn speculativeSpawn) => {
        var predictedGhost = predictedGhosts[speculativeSpawn.OwnerEntity];
        var ownerWasResimulated = speculativeSpawn.SpawnTick > predictedGhost.PredictionStartTick;
        var spawnedDuringResimulation = false;

        for (int i = 0; i < newSpeculativeEntities.Length; i++) {
          var newSpeculativeEntity = newSpeculativeEntities[i];
          var newSpeculativeSpawn = speculativeSpawns[newSpeculativeEntity];

          spawnedDuringResimulation = spawnedDuringResimulation || SpeculativeSpawn.Same(newSpeculativeSpawn, speculativeSpawn);
        }

        if (ownerWasResimulated && !spawnedDuringResimulation) {
          ecb.DestroyEntity(speculativeSpawn.Entity);
          ecb.DestroyEntity(e);
        }
      })
      .WithReadOnly(predictedGhosts)
      .WithReadOnly(speculativeSpawns)
      .WithDisposeOnCompletion(newSpeculativeEntities)
      .WithoutBurst()
      .Run();
      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }

  [UpdateInGroup(typeof(SpeculativeSpawnSystemGroup))]
  public class DestroyOrPromoteNewSpeculativeSpawnSystem : SystemBase {
    protected override void OnUpdate() {
      var existingSpeculativeQuery = GetEntityQuery(ComponentType.ReadOnly<SpeculativeSpawn>(), ComponentType.Exclude<NewSpeculativeSpawnTag>());
      var existingSpeculativeEntities = existingSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var speculativeSpawns = GetComponentDataFromEntity<SpeculativeSpawn>(true);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      Entities
      .WithName("Destroy_or_Promote_New_Speculative_Spawns")
      .WithAll<NewSpeculativeSpawnTag>()
      .ForEach((Entity e, in SpeculativeSpawn speculativeSpawn) => {
        var isRedundantSpawn = false;

        for (int j = 0; j < existingSpeculativeEntities.Length; j++) {
          var existingSpeculativeEntity = existingSpeculativeEntities[j];
          var existingSpeculativeSpawn = speculativeSpawns[existingSpeculativeEntity];

          isRedundantSpawn = isRedundantSpawn || SpeculativeSpawn.Same(speculativeSpawn, existingSpeculativeSpawn);
        }
        if (isRedundantSpawn) {
          ecb.DestroyEntity(speculativeSpawn.Entity);
          ecb.DestroyEntity(e);
        } else {
          ecb.RemoveComponent<NewSpeculativeSpawnTag>(e);
        }
      })
      .WithReadOnly(predictedGhosts)
      .WithReadOnly(speculativeSpawns)
      .WithDisposeOnCompletion(existingSpeculativeEntities)
      .WithoutBurst()
      .Run();
      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }
}