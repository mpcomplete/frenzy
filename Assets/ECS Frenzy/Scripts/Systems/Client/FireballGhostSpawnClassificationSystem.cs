using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Networking.Transport.Utilities;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
  [UpdateAfter(typeof(GhostSpawnClassificationSystem))]
  public class FireballGhostSpawnClassificationSystem : SystemBase {
    protected override void OnCreate() {
      RequireSingletonForUpdate<GhostSpawnQueueComponent>();
      RequireSingletonForUpdate<PredictedGhostSpawnList>();
    }

    protected override void OnUpdate() {
      var spawnListEntity = GetSingletonEntity<PredictedGhostSpawnList>();
      var spawnListFromEntity = GetBufferFromEntity<PredictedGhostSpawn>();

      Entities
      .WithName("Predicted_Ghost_Classifier")
      .WithAll<GhostSpawnQueueComponent>()
      .ForEach((DynamicBuffer<GhostSpawnBuffer> ghosts, DynamicBuffer<SnapshotDataBuffer> data) => {
        var predictedGhostSpawnBuffer = spawnListFromEntity[spawnListEntity];

        for (int i = 0; i < ghosts.Length; i++) {
          var ghost = ghosts[i];

          if (ghost.SpawnType == GhostSpawnBuffer.Type.Predicted) {
            for (int j = 0; j < predictedGhostSpawnBuffer.Length; j++) {
              const int TICK_DELTA = 5;

              var predictedGhostSpawn = predictedGhostSpawnBuffer[j];
              var sameGhostType = ghost.GhostType == predictedGhostSpawn.ghostType;
              var clause1 = !SequenceHelpers.IsNewer(predictedGhostSpawn.spawnTick, ghost.ServerSpawnTick + TICK_DELTA);
              var clause2 = SequenceHelpers.IsNewer(predictedGhostSpawn.spawnTick + TICK_DELTA, ghost.ServerSpawnTick);

              if (sameGhostType && clause1 && clause2) {
                ghost.PredictedSpawnEntity = predictedGhostSpawn.entity;
                predictedGhostSpawnBuffer.RemoveAtSwapBack(j);
                break;
              }
            }
            ghosts[i] = ghost;
          }
        }
      })
      .WithoutBurst()
      .Run();
    }
  }
}