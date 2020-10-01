using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
  [UpdateAfter(typeof(GhostSpawnClassificationSystem))]
  public class FireballGhostSpawnClassificationSystem : SystemBase {
    public static bool Matches(GhostSpawnBuffer ghostSpawnBuffer, PredictedGhostSpawn predictedGhostSpawn) {
      return ghostSpawnBuffer.GhostType == predictedGhostSpawn.ghostType;
    }

    protected override void OnCreate() {
      RequireSingletonForUpdate<GhostSpawnQueueComponent>();
      RequireSingletonForUpdate<PredictedGhostSpawnList>();
    }

    protected override void OnUpdate() {
      var spawnListEntity = GetSingletonEntity<PredictedGhostSpawnList>();
      var spawnListFromEntity = GetBufferFromEntity<PredictedGhostSpawn>();

      Entities
      .WithName("Try_Merge_New_Ghosts_With_Existing_Predictively_Spawned_Ghosts")
      .WithAll<GhostSpawnQueueComponent>()
      .ForEach((DynamicBuffer<GhostSpawnBuffer> ghosts, DynamicBuffer<SnapshotDataBuffer> data) => {
        var predictedGhostSpawnBuffer = spawnListFromEntity[spawnListEntity];

        for (int i = 0; i < ghosts.Length; i++) {
          var ghost = ghosts[i];

          if (ghost.SpawnType == GhostSpawnBuffer.Type.Predicted) {
            for (int j = 0; j < predictedGhostSpawnBuffer.Length; j++) {
              var predictedGhostSpawn = predictedGhostSpawnBuffer[j];

              if (Matches(ghost, predictedGhostSpawn)) {
                UnityEngine.Debug.Log($"<color=orange>Merged New Ghost {ghost.GhostType} with predicted {predictedGhostSpawn.ghostType}</color>");
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