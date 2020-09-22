using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using static Unity.Mathematics.math;
using static ECSFrenzy.Utils;
using Unity.Collections;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
  public class PlayerInputPredictionSystem : SystemBase {
    Entity FireballPrefabEntity;
    Entity FireballAbilityPrefabEntity;
    BeginSimulationEntityCommandBufferSystem CommandBufferSystem;
    GhostPredictionSystemGroup PredictionGroup;

    static Entity PredictedClientPrefab<T>(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) where T : IComponentData {
      bool IsPredictedSpawnFireball(Entity e) => entityManager.HasComponent<T>(e) && entityManager.HasComponent<PredictedGhostSpawnRequestComponent>(e);
      DynamicBuffer<GhostPrefabBuffer> clientPredictedPrefabs = entityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabs.clientPredictedPrefabs);

      return FindGhostPrefab(clientPredictedPrefabs, IsPredictedSpawnFireball);
    }

    static Entity ServerPrefab<T>(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) where T : IComponentData {
      bool IsNetworkFireball(Entity e) => entityManager.HasComponent<T>(e);
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
      var dt = Time.DeltaTime;
      var predictingTick = PredictionGroup.PredictingTick;
      var maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;
      var isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
      var commandBuffer = CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

      if (FireballPrefabEntity == Entity.Null) {
        var ghostPrefabs = GetSingleton<GhostPrefabCollectionComponent>();
        if (isServer) {
          FireballPrefabEntity = ServerPrefab<NetworkFireball>(EntityManager, ghostPrefabs);
          FireballAbilityPrefabEntity = ServerPrefab<FireballAbility>(EntityManager, ghostPrefabs);
        } else {
          FireballPrefabEntity = PredictedClientPrefab<NetworkFireball>(EntityManager, ghostPrefabs);
          FireballAbilityPrefabEntity = PredictedClientPrefab<FireballAbility>(EntityManager, ghostPrefabs);
        }
      }

      var fireballPrefabEntity = FireballPrefabEntity;
      var fireballAbilityPrefabEntity = FireballAbilityPrefabEntity;

      // These are used on the server to check if actions that have cooldowns are available to be performed yet
      var playerAbilities = GetComponentDataFromEntity<PlayerAbilites>(true);
      var cooldowns = GetComponentDataFromEntity<Cooldown>(true);

      Entities
      .WithName("Predict_Player_Input")
      .WithoutBurst() // TODO: This is a known bug where burst and shared components don't play nicely together... totally idiotic
      .WithReadOnly(playerAbilities)
      .WithReadOnly(cooldowns)
      .WithAll<NetworkPlayer, PlayerInput>()
      .ForEach((Entity entity, int nativeThreadIndex, ref Translation position, ref Rotation rotation, ref MoveSpeed moveSpeed, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost, in GhostOwnerComponent ghostOwner) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost))
          return;

        inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input);

        var speed = MoveSpeedFromInput(input);
        var direction = DirectionFromInput(input);
        var velocity = Velocity(direction, maxMoveSpeed, dt);

        moveSpeed.Value = speed;
        position.Value += velocity;
        rotation.Value = (speed > 0) ? (quaternion)Quaternion.LookRotation(direction, float3(0, 1, 0)) : rotation.Value;

        if (isServer) {
          var abilities = playerAbilities[entity];
          var fireballCooldown = cooldowns[abilities.Ability1];

          if (input.didFire != 0 && fireballCooldown.TimeRemaining <= 0) {
            // create fireball ability ghost
            {
              var fireballAbility = commandBuffer.Instantiate(nativeThreadIndex, fireballAbilityPrefabEntity);

              // TODO: These ability instances should be "owned" by the player's entity and should be listed in their LinkedEntityGroup for automatic destruction if the player is destroyed
              commandBuffer.SetComponent(nativeThreadIndex, fireballAbility, ghostOwner);
              commandBuffer.SetComponent<FireballAbility>(nativeThreadIndex, fireballAbility, new FireballAbility { SpawnTick = (int)predictingTick });
            }
            // create fireball ghost
            {
              var spawnPosition = position.Value + forward(rotation.Value) + float3(0,1,0);
              var fireball = commandBuffer.Instantiate(nativeThreadIndex, fireballPrefabEntity);

              commandBuffer.SetComponent(nativeThreadIndex, fireball, ghostOwner);
              commandBuffer.SetComponent(nativeThreadIndex, fireball, new Translation { Value = spawnPosition });
              commandBuffer.SetComponent(nativeThreadIndex, fireball, rotation);
              commandBuffer.SetComponent(nativeThreadIndex, fireball, new Heading { Value = forward(rotation.Value) });
            }
            // activate fireball cooldown
            {
              Cooldown.Activate(commandBuffer, abilities.Ability1, nativeThreadIndex, fireballCooldown);
            }
          }
        }
      }).ScheduleParallel();

      EntityQuery query = GetEntityQuery(typeof(Banner), typeof(Team));
      var banners = query.ToEntityArray(Allocator.TempJob);
      var bannerTeams = query.ToComponentDataArray<Team>(Allocator.TempJob);

      Entities
      .WithName("Predict_Player_Input_Banner")
      .WithBurst()
      .WithAll<NetworkPlayer, PlayerInput>()
      .WithDisposeOnCompletion(banners)
      .WithDisposeOnCompletion(bannerTeams)
      .ForEach((Entity entity, int nativeThreadIndex, ref Translation position, in Team team, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost))
          return;

        inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input);
        if (input.didBanner != 0) {
          int playerTeam = team.Value;
          float3 playerPos = position.Value;
          for (int i = 0; i < bannerTeams.Length; i++) {
            if (bannerTeams[i].Value == playerTeam) {
              commandBuffer.SetComponent(nativeThreadIndex, banners[i], new Translation { Value = playerPos });
            }
          }
        }
      }).ScheduleParallel();

      CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}