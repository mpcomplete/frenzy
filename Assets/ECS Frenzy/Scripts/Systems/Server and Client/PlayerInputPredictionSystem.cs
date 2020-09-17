using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using static Unity.Mathematics.math;
using static ECSFrenzy.Utils;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
  public class PlayerInputPredictionSystem : SystemBase {
    Entity FireballPrefabEntity;
    BeginSimulationEntityCommandBufferSystem CommandBufferSystem;
    GhostPredictionSystemGroup PredictionGroup;

    static Entity PredictedClientFireballPrefab(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) {
      bool IsPredictedSpawnFireball(Entity e) => entityManager.HasComponent<NetworkFireball>(e) && entityManager.HasComponent<PredictedGhostSpawnRequestComponent>(e);
      DynamicBuffer<GhostPrefabBuffer> clientPredictedPrefabs = entityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabs.clientPredictedPrefabs);

      return FindGhostPrefab(clientPredictedPrefabs, IsPredictedSpawnFireball);
    }

    static Entity ServerFireballPrefab(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) {
      bool IsNetworkFireball(Entity e) => entityManager.HasComponent<NetworkFireball>(e);
      DynamicBuffer<GhostPrefabBuffer> serverPrefabs = entityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabs.serverPrefabs);

      return FindGhostPrefab(serverPrefabs, IsNetworkFireball);
    }

    static float MoveSpeedFromInput(in PlayerInput input) => (input.horizontal == 0 && input.vertical == 0) ? 0 : 1;

    static float3 DirectionFromInput(in PlayerInput input) => float3(input.horizontal, 0, input.vertical);

    static float3 Velocity(in float3 direction, in float3 speed, in float dt) => dt * speed * direction;

    protected override void OnCreate() {
      CommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      PredictionGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate() {
      float dt = Time.DeltaTime;
      uint predictingTick = PredictionGroup.PredictingTick;
      float maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;
      bool isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
      EntityCommandBuffer.ParallelWriter commandBuffer = CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

      if (FireballPrefabEntity == Entity.Null) {
        if (isServer) {
          FireballPrefabEntity = ServerFireballPrefab(EntityManager, GetSingleton<GhostPrefabCollectionComponent>());
        } else {
          FireballPrefabEntity = PredictedClientFireballPrefab(EntityManager, GetSingleton<GhostPrefabCollectionComponent>());
        }
      }

      Entity fireballPrefabEntity = FireballPrefabEntity;

      Entities
      .WithName("Predict_Player_Input")
      .WithBurst()
      .WithAll<NetworkPlayer, PlayerInput>()
      .ForEach((Entity entity, int nativeThreadIndex, ref Translation position, ref Rotation rotation, ref MoveSpeed moveSpeed, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost, in GhostOwnerComponent ghostOwner) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost))
          return;

        inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input);

        float speed = MoveSpeedFromInput(input);
        float3 direction = DirectionFromInput(input);
        float3 velocity = Velocity(direction, maxMoveSpeed, dt);

        moveSpeed.Value = speed;
        position.Value += velocity;
        rotation.Value = (speed > 0) ? (quaternion)Quaternion.LookRotation(direction, float3(0, 1, 0)) : rotation.Value;

        if (isServer && input.didFire != 0) {
          Entity fireball = commandBuffer.Instantiate(nativeThreadIndex, fireballPrefabEntity);
          float3 spawnPosition = position.Value + forward(rotation.Value) + float3(0,1,0);

          commandBuffer.SetComponent(nativeThreadIndex, fireball, ghostOwner);
          commandBuffer.SetComponent(nativeThreadIndex, fireball, new Translation { Value = spawnPosition });
          commandBuffer.SetComponent(nativeThreadIndex, fireball, rotation);
          commandBuffer.SetComponent(nativeThreadIndex, fireball, new Heading { Value = forward(rotation.Value) });
        }
      }).ScheduleParallel();
      CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}