﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
  [UpdateInGroup(typeof(GhostSpawnSystemGroup), OrderLast=true)]
  public class RedundantPredictiveSpawnCleanupSystem : SystemBase {
    BeginSimulationEntityCommandBufferSystem Barrier;

    protected override void OnCreate() {
      Barrier = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
      const int CAPACITY = 1024;

      var unique = new NativeHashSet<RedundantSpawnComponent>(CAPACITY, Allocator.TempJob);
      var commandBuffer = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
      var spawnListEntity = GetSingletonEntity<PredictedGhostSpawnList>();
      var spawnListFromEntity = GetBufferFromEntity<PredictedGhostSpawn>();
      var redundantSpawnFromEntity = GetComponentDataFromEntity<RedundantSpawnComponent>(true);

      Job
      .WithCode(() => {
        var spawnList = spawnListFromEntity[spawnListEntity];

        for (int i = 0; i < spawnList.Length; i++) {
          var spawn = spawnList[i];
          var redundantSpawn = redundantSpawnFromEntity[spawn.entity];

          if (unique.Contains(redundantSpawn)) {
            // UnityEngine.Debug.Log($"REDUNDANT: {redundantSpawn.SimulatedSpawnTick}/{redundantSpawn.Identifier}");
            commandBuffer.DestroyEntity(spawn.entity);
            spawnList.RemoveAtSwapBack(i);
            --i;
          } else {
            // UnityEngine.Debug.Log($"UNIQUE: {redundantSpawn.SimulatedSpawnTick}/{redundantSpawn.Identifier}");
            unique.Add(redundantSpawn);
          }
        }
      })
      .WithReadOnly(redundantSpawnFromEntity)
      .WithoutBurst()
      .Run();
      commandBuffer.Playback(EntityManager);
      commandBuffer.Dispose();
      unique.Dispose();
    }
  }
}