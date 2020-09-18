using Unity.Entities;
using Unity.Jobs;
using static Unity.Mathematics.math;

namespace ECSFrenzy {
  public class CooldownSystem : SystemBase {
    BeginSimulationEntityCommandBufferSystem entityCommandBufferSystem;

    protected override void OnCreate() {
      entityCommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
      var dt = Time.DeltaTime;
      var ecb = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

      // TODO: not sure but MAYBE this should also run for JustActive cooldowns?
      Entities
      .WithName("Active_Cooldowns")
      .WithSharedComponentFilter<SharedCooldownStatus>(SharedCooldownStatus.Active)
      .WithoutBurst() // TODO: current version of Entities has a known bug that does not properly support changing SharedComponents
      .ForEach((Entity e, int nativeThreadIndex, in Cooldown cooldown) => {
        var updatedCooldown = cooldown;

        updatedCooldown.TimeRemaining = max(0, updatedCooldown.TimeRemaining - dt);
        if (cooldown.TimeRemaining <= 0) {
          ecb.SetSharedComponent<SharedCooldownStatus>(nativeThreadIndex, e, SharedCooldownStatus.JustElapsed);
        } else {
          ecb.SetComponent<Cooldown>(nativeThreadIndex, e, updatedCooldown);
        }
      }).ScheduleParallel();

      Entities
      .WithName("JustElapsed_Cooldowns")
      .WithAll<Cooldown>()
      .WithSharedComponentFilter<SharedCooldownStatus>(SharedCooldownStatus.JustElapsed)
      .WithoutBurst() // TODO: current version of Entities has a known bug that does not properly support changing SharedComponents
      .ForEach((Entity e, int nativeThreadIndex) => {
        ecb.SetSharedComponent<SharedCooldownStatus>(nativeThreadIndex, e, SharedCooldownStatus.Elapsed);
      }).ScheduleParallel();

      Entities
      .WithName("JustActive_Cooldowns")
      .WithAll<Cooldown>()
      .WithSharedComponentFilter<SharedCooldownStatus>(SharedCooldownStatus.JustActive)
      .WithoutBurst() // TODO: current version of Entities has a known bug that does not properly support changing SharedComponents
      .ForEach((Entity e, int nativeThreadIndex) => {
        ecb.SetSharedComponent<SharedCooldownStatus>(nativeThreadIndex, e, SharedCooldownStatus.Active);
      }).ScheduleParallel();
      entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}