using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using ECSFrenzy.MonoBehaviors;
using ECSFrenzy.Networking;
using static Unity.Mathematics.math;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
  public class MovePlayer : ComponentSystem {
    protected override void OnUpdate() {
      GhostPredictionSystemGroup group = World.GetExistingSystem<GhostPredictionSystemGroup>();
      uint tick = group.PredictingTick;
      float dt = group.Time.DeltaTime;
      float maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;

      Entities
      .WithAll<NetworkPlayer>()
      .ForEach((DynamicBuffer<PlayerInput> inputBuffer, ref MoveSpeed moveSpeed, ref Translation translation, ref Rotation rotation, ref PredictedGhostComponent predictedGhost) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(tick, predictedGhost))
          return;

        inputBuffer.GetDataAtTick(tick, out PlayerInput input);

        if (input.horizontal == 0 && input.vertical == 0) {
          moveSpeed.Value = 0;
        } else {
          float3 direction = float3(input.horizontal, 0, input.vertical);
          float3 velocity = direction * dt * maxMoveSpeed;

          moveSpeed.Value = 1;
          translation.Value += velocity;
          rotation.Value = Quaternion.LookRotation(direction, float3(0, 1, 0));
        }
      });
    }
  }
}