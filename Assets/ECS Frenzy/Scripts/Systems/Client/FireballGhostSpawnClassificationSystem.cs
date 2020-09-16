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

    /*
    When you fire a predicted projectile there are a few things that might happen:
      You simulate the predicted projectile until the ghost matching it shows up from the server 
        and you merge them
      You simulate the predicted projectile until the snapshot for that prediction shows up and there is no projectile 
        and you should destroy the erroneously-predicted projectile
      You simulate the predicted projectile and you do NOT spawn additional predicted projectiles that MAY be enqueued
      by your client-prediction code re-running frames. 
        here you should NOT spawn a projectile matching this projectile's unique signature
          creating a unique signature for a projectile has no single "right approach" but it could be handled by
          creating a structure of the id of the shooter, the tick of shooting, etc etc
          for my case, I will simply create an id from a tuple of the networkId and the the tick
          when a client wants to spawn something during prediction, they do this through a utility function that checks
          to see if there is already a predicted, spawned entity matching this one and ignores it if there is
    */

    protected override void OnUpdate() {
      Entity spawnListEntity = GetSingletonEntity<PredictedGhostSpawnList>();
      BufferFromEntity<PredictedGhostSpawn> spawnListFromEntity = GetBufferFromEntity<PredictedGhostSpawn>();
      // TODO: for debugging only
      uint tick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

      Entities
      .WithAll<GhostSpawnQueueComponent>()
      .WithoutBurst()
      .ForEach((DynamicBuffer<GhostSpawnBuffer> ghosts, DynamicBuffer<SnapshotDataBuffer> data) => {
        DynamicBuffer<PredictedGhostSpawn> predictedGhostSpawnBuffer = spawnListFromEntity[spawnListEntity];

        // if (predictedGhostSpawnBuffer.Length > 0 || ghosts.Length > 0) 
        //   UnityEngine.Debug.Log($"PredictingTick: {tick} / PredictedGhostCount: {predictedGhostSpawnBuffer.Length} / GhostsToSpawnCount: {ghosts.Length}");

        for (int i = 0; i < ghosts.Length; i++) {
          GhostSpawnBuffer ghost = ghosts[i];

          if (ghost.SpawnType == GhostSpawnBuffer.Type.Predicted) {
            for (int j = 0; j < predictedGhostSpawnBuffer.Length; j++) {
              const int TICK_DELTA = 5;
              PredictedGhostSpawn predictedGhostSpawn = predictedGhostSpawnBuffer[j];
              bool sameGhostType = ghost.GhostType == predictedGhostSpawn.ghostType;
              bool clause1 = !SequenceHelpers.IsNewer(predictedGhostSpawn.spawnTick, ghost.ServerSpawnTick + TICK_DELTA);
              bool clause2 = SequenceHelpers.IsNewer(predictedGhostSpawn.spawnTick + TICK_DELTA, ghost.ServerSpawnTick);

              if (sameGhostType && clause1 && clause2) {
                ghost.PredictedSpawnEntity = predictedGhostSpawn.entity;
                predictedGhostSpawnBuffer.RemoveAtSwapBack(j);
                break;
              }
            }
            ghosts[i] = ghost;
          }
        }
      }).Schedule();
    }
  }
}