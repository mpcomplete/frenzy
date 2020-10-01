using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostSpawnSystemGroup), OrderLast=true)]
  public class RedundantPredictiveSpawnCleanupSystem : SystemBase {
    protected override void OnUpdate() {
      const int CAPACITY = 1024;

      var redundantSpawnFromEntity = GetComponentDataFromEntity<RedundantSpawnComponent>(true);
      var unique = new NativeHashSet<RedundantSpawnComponent>(CAPACITY, Allocator.TempJob);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      Entities
      .WithAll<PredictedGhostSpawnList>()
      .ForEach((Entity e, DynamicBuffer<PredictedGhostSpawn> spawnList) => {
        for (int i = 0; i < spawnList.Length; i++) {
          var spawn = spawnList[i];
          var redundantSpawn = redundantSpawnFromEntity[spawn.entity];

          if (unique.Contains(redundantSpawn)) {
            UnityEngine.Debug.Log($"<color=red>Destroyed redundant predictive spawn {redundantSpawn.SimulatedSpawnTick}</color>");
            ecb.DestroyEntity(spawn.entity);
            spawnList.RemoveAtSwapBack(i);
            --i;
          } else {
            unique.Add(redundantSpawn);
          }
        }
      })
      .WithReadOnly(redundantSpawnFromEntity)
      .WithDisposeOnCompletion(unique)
      .WithoutBurst()
      .Run();
      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }
}