using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  [UpdateAfter(typeof(GhostSendSystem))]
  public class DeathTimerSystem : SystemBase {
    BeginSimulationEntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate() {
      commandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnDestroy() {
      entityCommandBuffer.Dispose();
    }

    protected override void OnUpdate() {
      float dt = Time.DeltaTime;
      EntityCommandBuffer.ParallelWriter ecbWriter = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

      Entities
      .WithName("Update_Death_Timer")
      .WithBurst()
      .ForEach((Entity e, int entityInQueryIndex, ref DeathTimer deathTimer) => {
        deathTimer.TimeRemaining -= dt;

        if (deathTimer.TimeRemaining <= 0) {
          ecbWriter.DestroyEntity(entityInQueryIndex, e);
        }
      }).ScheduleParallel();
      commandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}