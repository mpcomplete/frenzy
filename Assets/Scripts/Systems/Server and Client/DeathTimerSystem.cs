using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
  [UpdateInGroup(typeof(LateSimulationSystemGroup))]
  public class DeathTimerSystem : SystemBase {
    BeginSimulationEntityCommandBufferSystem BeginSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
      BeginSimulationEntityCommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
      var dt = Time.DeltaTime;
      var beginSimECB = BeginSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

      Entities
      .WithName("Kill_Zero_Health")
      .WithNone<DeathTimer>()
      .WithNone<NetworkPlayer>()  // TODO: remove, just for testing
      .WithBurst()
      .ForEach((Entity e, int nativeThreadIndex, ref Health health) => {
        if (health.Value <= 0f) {
          beginSimECB.AddComponent(nativeThreadIndex, e, new DeathTimer { TimeRemaining = .01f });
        }
      }).ScheduleParallel();

      Entities
      .WithName("Update_Death_Timer")
      .ForEach((Entity e, int nativeThreadIndex, ref DeathTimer deathTimer) => {
        deathTimer.TimeRemaining -= dt;

        if (deathTimer.TimeRemaining <= 0) {
          beginSimECB.DestroyEntity(nativeThreadIndex, e);
        }
      })
      .WithBurst()
      .ScheduleParallel();
      BeginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}