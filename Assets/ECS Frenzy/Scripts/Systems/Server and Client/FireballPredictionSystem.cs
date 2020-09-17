using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

public class FireballPredictionSystem : SystemBase {
  BeginSimulationEntityCommandBufferSystem CommandBufferSystem;
  GhostPredictionSystemGroup PredictionSystemGroup;

  protected override void OnCreate() {
    CommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    PredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
  }

  protected override void OnUpdate() {
    float dt = Time.DeltaTime;
    uint predictingTick = PredictionSystemGroup.PredictingTick;

    Entities
    .WithName("Predict_Fireball_Position")
    .WithBurst()
    .WithAll<NetworkFireball>()
    .ForEach((ref Translation translation, in MoveSpeed moveSpeed, in Heading heading, in PredictedGhostComponent predictedGhost) => {
      if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost))
        return;

      translation.Value += dt * moveSpeed.Value * heading.Value;
    }).ScheduleParallel();
    CommandBufferSystem.AddJobHandleForProducer(Dependency);
  }
}