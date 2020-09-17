using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientAndServerSimulationSystemGroup))]
  public class MoveAlongHeadingSystem : SystemBase {
    protected override void OnUpdate() {
      float dt = Time.DeltaTime;

      /*
      Entities
      .WithName("Move_Along_Heading")
      .WithNone<NetworkFireball>()
      .WithBurst()
      .ForEach((ref Translation translation, in Heading heading, in MoveSpeed moveSpeed) => {
        translation.Value += dt * moveSpeed.Value * heading.Value;
      }).ScheduleParallel();
      */
    }
  }
}