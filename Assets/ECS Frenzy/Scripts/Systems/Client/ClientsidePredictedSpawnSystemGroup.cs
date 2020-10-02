using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostSpawnSystemGroup), OrderFirst=true)]
  public class ClientsidePredictedSpawnSystemGroup : ComponentSystemGroup {}

  [UpdateInGroup(typeof(ClientsidePredictedSpawnSystemGroup))]
  public class DestroyInvalidClientsideSpawnSystem : SystemBase {
    protected override void OnUpdate() {
      var newSpeculativeQuery = GetEntityQuery(ComponentType.ReadOnly<SpeculativeSpawn>(), ComponentType.ReadOnly<NewSpeculativeSpawnTag>());
      var newSpeculativeEntities = newSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var speculativeSpawns = GetComponentDataFromEntity<SpeculativeSpawn>(true);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      // TODO: probably should detect if the ownerEntity still exists
      // TODO: alternatively, perhaps these could all be destroyed automatically when the owner is destroyed?
      // TODO: so many choices... so little time

      Entities
      .WithName("Destroy_Existing_Clientside_Spawns_That_Were_Not_Resimulated")
      .WithNone<NewSpeculativeSpawnTag>()
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
      .Run();
      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }

  [UpdateInGroup(typeof(ClientsidePredictedSpawnSystemGroup))]
  public class DestroyOrPromoteNewClientsideSpawnSystem : SystemBase {
    protected override void OnUpdate() {
      var existingSpeculativeQuery = GetEntityQuery(ComponentType.ReadOnly<SpeculativeSpawn>(), ComponentType.Exclude<NewSpeculativeSpawnTag>());
      var existingSpeculativeEntities = existingSpeculativeQuery.ToEntityArray(Allocator.TempJob);
      var speculativeSpawns = GetComponentDataFromEntity<SpeculativeSpawn>(true);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      Entities
      .WithName("Destroy_or_Promote_New_Clientside_Spawns")
      .WithAll<NewSpeculativeSpawnTag>()
      .ForEach((Entity e, in SpeculativeSpawn speculativeSpawn) => {
        var isRedundantSpawn = false;

        for (int j = 0; j < existingSpeculativeEntities.Length; j++) {
          var existingSpeculativeEntity = existingSpeculativeEntities[j];
          var existingSpeculativeSpawn = speculativeSpawns[existingSpeculativeEntity];

          isRedundantSpawn = isRedundantSpawn || SpeculativeSpawn.Same(speculativeSpawn, existingSpeculativeSpawn);
        }
        if (isRedundantSpawn) {
          UnityEngine.Debug.Log($"<color=red>Destroyed redundant speculative spawn {speculativeSpawn.SpawnTick}</color>");
          ecb.DestroyEntity(speculativeSpawn.Entity);
          ecb.DestroyEntity(e);
        } else {
          ecb.RemoveComponent<NewSpeculativeSpawnTag>(e);
        }
      })
      .WithReadOnly(predictedGhosts)
      .WithReadOnly(speculativeSpawns)
      .WithDisposeOnCompletion(existingSpeculativeEntities)
      .Run();
      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }
}