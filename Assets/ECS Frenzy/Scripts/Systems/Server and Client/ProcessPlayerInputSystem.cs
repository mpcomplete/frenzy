using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using ECSFrenzy.MonoBehaviors;
using ECSFrenzy.Networking;
using static Unity.Mathematics.math;
using static ECSFrenzy.Networking.Utils;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
  public class ProcessPlayerInputSystem : SystemBase {
    Entity FireballPrefabEntity;
    GhostTypeComponent FireballEntityGhostType;
    BeginSimulationEntityCommandBufferSystem CommandBufferSystem;

    Entity PredictedClientFireballPrefab(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) {
      NativeArray<GhostPrefabBuffer> clientPredictedPrefabs = entityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabs.clientPredictedPrefabs).ToNativeArray(Allocator.Temp);
      Entity clientPredictedFireball = FindGhostPrefab<NetworkFireball>(entityManager, clientPredictedPrefabs);
      Entity prefab = entityManager.Instantiate(clientPredictedFireball);

      entityManager.AddComponentData(prefab, default(Prefab));
      entityManager.AddComponentData(prefab, default(PredictedGhostSpawnRequestComponent));
      return prefab;
    }

    Entity ServerFireballPrefab(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) {
      DynamicBuffer<GhostPrefabBuffer> serverPrefabs = entityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabs.serverPrefabs);
      Entity prefab = FindGhostPrefab<NetworkFireball>(entityManager, serverPrefabs);

      return prefab;
    }

    // TODO: GhostType is ignored currently: should fix
    static bool ShoulSpawnPredictedProjectile(in DynamicBuffer<PredictedGhostSpawn> predictedGhostSpawns, in int ghostType, uint spawnTick) {
      for (int i = 0; i < predictedGhostSpawns.Length; i++) {
        // if (predictedGhostSpawns[i].ghostType == ghostType && predictedGhostSpawns[i].spawnTick == spawnTick)
        if (predictedGhostSpawns[i].spawnTick == spawnTick)
          return false;
      }
      return true;
    }

    protected override void OnCreate() {
      CommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate() {
      GhostPredictionSystemGroup group = World.GetExistingSystem<GhostPredictionSystemGroup>();
      uint tick = group.PredictingTick;
      float dt = Time.DeltaTime;
      float maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;
      bool isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
      EntityCommandBuffer.ParallelWriter commandBuffer = CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

      if (isServer) {
        if (FireballPrefabEntity == Entity.Null) {
          GhostPrefabCollectionComponent ghostPrefabs = GetSingleton<GhostPrefabCollectionComponent>();

          FireballPrefabEntity = ServerFireballPrefab(EntityManager, ghostPrefabs);
          FireballEntityGhostType = EntityManager.GetComponentData<GhostTypeComponent>(FireballPrefabEntity);
        }
        Entity fireballPrefabEntity = FireballPrefabEntity;
        GhostTypeComponent fireballGhostType = FireballEntityGhostType;

        Entities
        .WithAll<NetworkPlayer, PlayerInput>()
        .ForEach((Entity entity, int nativeThreadIndex, ref Translation position, ref Rotation rotation, ref MoveSpeed moveSpeed, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost, in GhostOwnerComponent ghostOwner) => {
          if (!GhostPredictionSystemGroup.ShouldPredict(tick, predictedGhost))
            return;

          // TODO: THIS stupid ass code is copied from the Asteroids project. However, this does not appear to do shit
          if (!inputBuffer.GetDataAtTick(tick, out PlayerInput input)) {
            input.didFire = 0;
          }

          if (input.horizontal == 0 && input.vertical == 0) {
            moveSpeed.Value = 0;
          } else {
            float3 direction = float3(input.horizontal, 0, input.vertical);
            float3 velocity = direction * dt * maxMoveSpeed;

            moveSpeed.Value = 1;
            position.Value += velocity;
            rotation.Value = Quaternion.LookRotation(direction, float3(0, 1, 0));
          }

          if (input.didFire != 0) {
            Debug.Log($"Server did fire a projectile on tick {tick}");
            Entity fireball = commandBuffer.Instantiate(nativeThreadIndex, fireballPrefabEntity);

            commandBuffer.SetComponent(nativeThreadIndex, fireball, ghostOwner);
            commandBuffer.SetComponent(nativeThreadIndex, fireball, position);
            commandBuffer.SetComponent(nativeThreadIndex, fireball, rotation);
            commandBuffer.SetComponent(nativeThreadIndex, fireball, new Heading { Value = forward(rotation.Value) });
          }
        }).ScheduleParallel();
      } else {
        if (FireballPrefabEntity == Entity.Null) {
          GhostPrefabCollectionComponent ghostPrefabs = GetSingleton<GhostPrefabCollectionComponent>();

          FireballPrefabEntity = PredictedClientFireballPrefab(EntityManager, ghostPrefabs);
          FireballEntityGhostType = EntityManager.GetComponentData<GhostTypeComponent>(FireballPrefabEntity);
        }
        Entity fireballPrefabEntity = FireballPrefabEntity;
        GhostTypeComponent fireballGhostType = FireballEntityGhostType;
        Entity spawnListEntity = GetSingletonEntity<PredictedGhostSpawnList>();
        BufferFromEntity<PredictedGhostSpawn> spawnListFromEntity = GetBufferFromEntity<PredictedGhostSpawn>();

        Entities
        .WithAll<NetworkPlayer, PlayerInput>()
        .WithReadOnly(spawnListFromEntity)
        .ForEach((Entity entity, int nativeThreadIndex, ref Translation position, ref Rotation rotation, ref MoveSpeed moveSpeed, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost, in GhostOwnerComponent ghostOwner) => {
          if (!GhostPredictionSystemGroup.ShouldPredict(tick, predictedGhost))
            return;

          // TODO: THIS stupid ass code is copied from the Asteroids project. However, this does not appear to do shit
          if (!inputBuffer.GetDataAtTick(tick, out PlayerInput input)) {
            input.didFire = 0;
          }

          if (input.horizontal == 0 && input.vertical == 0) {
            moveSpeed.Value = 0;
          } else {
            float3 direction = float3(input.horizontal, 0, input.vertical);
            float3 velocity = direction * dt * maxMoveSpeed;

            moveSpeed.Value = 1;
            position.Value += velocity;
            rotation.Value = Quaternion.LookRotation(direction, float3(0, 1, 0));
          }

          if (input.didFire != 0) {
            const int FAKE_GHOST_TYPE_INT = 0; // TODO: just a temporary disregarded value while I figure shit out
            DynamicBuffer<PredictedGhostSpawn> predictedGhostSpawnBuffer = spawnListFromEntity[spawnListEntity];

            if (ShoulSpawnPredictedProjectile(predictedGhostSpawnBuffer, FAKE_GHOST_TYPE_INT, tick)) {
              Debug.Log($"Client did fire a projectile on tick {tick}");
              Entity fireball = commandBuffer.Instantiate(nativeThreadIndex, fireballPrefabEntity);

              commandBuffer.SetComponent(nativeThreadIndex, fireball, ghostOwner);
              commandBuffer.SetComponent(nativeThreadIndex, fireball, position);
              commandBuffer.SetComponent(nativeThreadIndex, fireball, rotation);
              commandBuffer.SetComponent(nativeThreadIndex, fireball, new Heading { Value = forward(rotation.Value) });
            }
          }
        }).ScheduleParallel();
      }

      // Predict actions for all bullets
      Entities
      .WithAll<NetworkFireball>()
      .ForEach((ref Translation translation, in MoveSpeed moveSpeed, in Heading heading, in PredictedGhostComponent predictedGhost) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(tick, predictedGhost))
          return;

        translation.Value += dt * moveSpeed.Value * heading.Value;
      }).ScheduleParallel();
      CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}