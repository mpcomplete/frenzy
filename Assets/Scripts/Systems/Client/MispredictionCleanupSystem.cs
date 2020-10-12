using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
[UpdateInGroup(typeof(GhostSpawnSystemGroup), OrderFirst = true)]
public class MispredictionCleanupSystem : SystemBase {
  protected override void OnUpdate() {
    var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
    var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

    Entities
    .WithNone<Prefab>()
    .WithName("Destroy_Mispredicted_Clientside_Spawns")
    .ForEach((Entity e, in UniqueClientsideSpawn uniqueSpawn, in RepredictedClientsideSpawn resimulatedSpawn) => {
      var predictedGhost = predictedGhosts[uniqueSpawn.OwnerEntity];
      var wasResimulatedLastTick = uniqueSpawn.SpawnTick > predictedGhost.PredictionStartTick;

      if (wasResimulatedLastTick && !resimulatedSpawn.Value) {
        ecb.DestroyEntity(e);
      } else {
        ecb.SetComponent(e, new RepredictedClientsideSpawn { Value = false });
      }
    })
    .WithReadOnly(predictedGhosts)
    .WithBurst()
    .Run();
    ecb.Playback(EntityManager);
    ecb.Dispose();
  }
}
