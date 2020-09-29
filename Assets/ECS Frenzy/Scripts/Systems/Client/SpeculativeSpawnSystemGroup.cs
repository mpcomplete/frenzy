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
      var newSpeculativeQuery = GetEntityQuery(typeof(SpeculativeSpawn), typeof(NewSpeculativeSpawnTag));
      var existingSpeculativeQuery = GetEntityQuery(typeof(SpeculativeSpawn), ComponentType.Exclude<NewSpeculativeSpawnTag>());
      var newSpeculativeEntities = newSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var existingSpeculativeEntities = existingSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var speculativeSpawns = GetComponentDataFromEntity<SpeculativeSpawn>(true);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      Job
      .WithName("Manage_Speculative_Spawns")
      .WithCode(() => {
        for (int i = 0; i < existingSpeculativeEntities.Length; i++) {
          var existingEntity = existingSpeculativeEntities[i];
          var existingSpeculativeSpawn = speculativeSpawns[existingEntity];
          var predictedGhost = predictedGhosts[existingSpeculativeSpawn.OwnerEntity];
          var foundMatch = false;
          var resimulatedThisFrame = existingSpeculativeSpawn.SpawnTick > predictedGhost.PredictionStartTick;

          for (int j = 0; j < newSpeculativeEntities.Length; j++) {
            var newSpeculativeEntity = newSpeculativeEntities[j];
            var newSpeculativeSpawn = speculativeSpawns[newSpeculativeEntity];

            foundMatch = foundMatch || SpeculativeSpawn.Same(newSpeculativeSpawn, existingSpeculativeSpawn);
          }

          if (resimulatedThisFrame && !foundMatch) {
            ecb.DestroyEntity(existingSpeculativeSpawn.Entity);
            ecb.DestroyEntity(existingEntity);
          }
        }
      })
      .WithReadOnly(predictedGhosts)
      .WithReadOnly(speculativeSpawns)
      .WithoutBurst()
      .Run();

      newSpeculativeEntities.Dispose();
      existingSpeculativeEntities.Dispose();
      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }

  [UpdateInGroup(typeof(SpeculativeSpawnSystemGroup))]
  public class DestroyOrPromoteNewSpeculativeSpawnSystem : SystemBase {
    protected override void OnUpdate() {
      var newSpeculativeQuery = GetEntityQuery(ComponentType.ReadOnly<SpeculativeSpawn>(), ComponentType.ReadOnly<NewSpeculativeSpawnTag>());
      var existingSpeculativeQuery = GetEntityQuery(ComponentType.ReadOnly<SpeculativeSpawn>(), ComponentType.Exclude<NewSpeculativeSpawnTag>());
      var newSpeculativeEntities = newSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var existingSpeculativeEntities = existingSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var speculativeSpawns = GetComponentDataFromEntity<SpeculativeSpawn>(true);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      Job
      .WithName("Destroy_or_Promote_New_Speculative_Spawns")
      .WithCode(() => {
        for (int i = 0; i < newSpeculativeEntities.Length; i++) {
          var newSpeculativeEntity = newSpeculativeEntities[i];
          var newSpeculativeSpawn = speculativeSpawns[newSpeculativeEntity];
          var foundMatch = false;

          for (int j = 0; j < existingSpeculativeEntities.Length; j++) {
            var existingSpeculativeEntity = existingSpeculativeEntities[j];
            var existingSpeculativeSpawn = speculativeSpawns[existingSpeculativeEntity];

            foundMatch = foundMatch || SpeculativeSpawn.Same(newSpeculativeSpawn, existingSpeculativeSpawn);
          }
          if (foundMatch) {
            ecb.DestroyEntity(newSpeculativeSpawn.Entity);
            ecb.DestroyEntity(newSpeculativeEntity);
          } else {
            ecb.RemoveComponent<NewSpeculativeSpawnTag>(newSpeculativeEntity);
          }
        }
      })
      .WithReadOnly(predictedGhosts)
      .WithReadOnly(speculativeSpawns)
      .WithoutBurst()
      .Run();

      newSpeculativeEntities.Dispose();
      existingSpeculativeEntities.Dispose();
      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }
}