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
      Entity spawnListEntity = GetSingletonEntity<PredictedGhostSpawnList>();
      BufferFromEntity<PredictedGhostSpawn> spawnListFromEntity = GetBufferFromEntity<PredictedGhostSpawn>();

      Entities
      .WithAll<GhostSpawnQueueComponent>()
      .ForEach((DynamicBuffer<GhostSpawnBuffer> ghosts, DynamicBuffer<SnapshotDataBuffer> data) => {
        DynamicBuffer<PredictedGhostSpawn> predictedGhostSpawnBuffer = spawnListFromEntity[spawnListEntity];

        for (int i = 0; i < ghosts.Length; i++) {
          GhostSpawnBuffer ghost = ghosts[i];

          UnityEngine.Debug.Log($"New ghost from server of spawnType {ghost.SpawnType}");
          if (ghost.SpawnType == GhostSpawnBuffer.Type.Predicted) {
            for (int j = 0; j < predictedGhostSpawnBuffer.Length; j++) {
              const int TICK_DELTA = 5;
              PredictedGhostSpawn predictedGhostSpawn = predictedGhostSpawnBuffer[j];
              bool sameGhostType = ghost.GhostType == predictedGhostSpawn.ghostType;
              UnityEngine.Debug.Log("New predictively-spawned ghost from server");
              // bool clause1 = !SequenceHelpers.IsNewer(predictedGhostSpawn.spawnTick, ghost.ServerSpawnTick + TICK_DELTA);
              // bool clause2 = SequenceHelpers.IsNewer(predictedGhostSpawn.spawnTick + TICK_DELTA, ghost.ServerSpawnTick);

              if (sameGhostType/* && clause1 && clause2*/) {
                UnityEngine.Debug.Log("it happened");
                ghost.PredictedSpawnEntity = predictedGhostSpawn.entity;
                // predictedGhostSpawnBuffer.RemoveAtSwapBack(j);
                predictedGhostSpawnBuffer[j] = predictedGhostSpawnBuffer[predictedGhostSpawnBuffer.Length-1];
                predictedGhostSpawnBuffer.RemoveAt(predictedGhostSpawnBuffer.Length-1);
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