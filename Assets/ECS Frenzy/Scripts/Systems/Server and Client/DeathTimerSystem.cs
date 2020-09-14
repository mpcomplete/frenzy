using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
  public class DeathTimerSystem : SystemBase {
    EntityCommandBuffer entityCommandBuffer;

    protected override void OnCreate() {
      entityCommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.MultiPlayback);
    }

    protected override void OnUpdate() {
      float dt = Time.DeltaTime;
      EntityCommandBuffer.ParallelWriter ecbWriter = entityCommandBuffer.AsParallelWriter();

      Entities
      .WithName("Update_Death_Timer")
      .WithBurst()
      .ForEach((Entity e, int entityInQueryIndex, ref DeathTimer deathTimer) => {
        deathTimer.TimeRemaining -= dt;

        if (deathTimer.TimeRemaining <= 0) {
          ecbWriter.DestroyEntity(entityInQueryIndex, e);
        }
      }).ScheduleParallel();

      entityCommandBuffer.Playback(EntityManager);
    }
  }
}