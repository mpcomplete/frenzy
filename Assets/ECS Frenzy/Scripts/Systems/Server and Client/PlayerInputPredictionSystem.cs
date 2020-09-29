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
    Entity TestSpeculativePrefabEntity;
    BeginSimulationEntityCommandBufferSystem CommandBufferSystem;
    GhostPredictionSystemGroup GhostPredictionSystemGroup;

    static Entity CreateSpeculativeSpawnPrefab(GameObject prefab, EntityManager entityManager, bool onServer) {
      if (!onServer) {
        var blobAssetStore = new BlobAssetStore(); 
        var conversionFlags = GameObjectConversionUtility.ConversionFlags.AssignName;
        var conversionSettings = new GameObjectConversionSettings(entityManager.World, conversionFlags, blobAssetStore);
        var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, conversionSettings);

        entityManager.AddComponent<SpeculativeSpawn>(entity);
        entityManager.AddComponent<NewSpeculativeSpawnTag>(entity);
        entityManager.AddComponent<Prefab>(entity);
        blobAssetStore.Dispose();
        return entity;
      } else {
        return Entity.Null;
      }
    }

    static Entity SpawnGhostEntity(Entity prefab, EntityCommandBuffer entityCommandBuffer, bool onServer, in GhostOwnerComponent ghostOwnerComponent, uint tick, uint identifier) {
      var entity = entityCommandBuffer.Instantiate(prefab);
      
      entityCommandBuffer.SetComponent(entity, ghostOwnerComponent);
      if (!onServer) {
        entityCommandBuffer.SetComponent(entity, new RedundantSpawnComponent(tick, identifier));
        entityCommandBuffer.SetComponent(entity, new PredictedGhostSpawnRequestComponent());
      }
      return entity;
    }

    static Entity SpawnSpeculativeEntity(Entity prefab, EntityCommandBuffer entityCommandBuffer, Entity owner, uint tick, uint identifier) {
      var entity = entityCommandBuffer.Instantiate(prefab);
      var speculativeEntity = entityCommandBuffer.CreateEntity();

      entityCommandBuffer.SetComponent(entity, new SpeculativeSpawn(owner, entity, tick, identifier));
      entityCommandBuffer.SetComponent<NewSpeculativeSpawnTag>(entity, new NewSpeculativeSpawnTag());
      return entity;
    }

    protected override void OnCreate() {
      CommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate() {
      var isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;

      if (FireballPrefabEntity == Entity.Null) {
        var ghostPrefabCollectionEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
        var ghostPrefabs = GetBuffer<GhostPrefabBuffer>(ghostPrefabCollectionEntity);
        var ghostPrefab = FindGhostPrefab(ghostPrefabs, e => EntityManager.HasComponent<NetworkFireball>(e));

        FireballPrefabEntity = GhostCollectionSystem.CreatePredictedSpawnPrefab(EntityManager, ghostPrefab);
        TestSpeculativePrefabEntity = CreateSpeculativeSpawnPrefab(SystemConfig.Instance.SpeculativeSpawnTestPrefab, EntityManager, isServer);
      }

      var dt = Time.DeltaTime;
      var predictingTick = GhostPredictionSystemGroup.PredictingTick;
      var maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;
      var fireballPrefabEntity = FireballPrefabEntity;
      var speculativeTestPrefabEntity = TestSpeculativePrefabEntity;
      var playerAbilities = GetComponentDataFromEntity<PlayerAbilites>(true);
      var cooldowns = GetComponentDataFromEntity<Cooldown>(true);
      var bannerQuery = GetEntityQuery(typeof(Banner), typeof(Team));
      var banners = bannerQuery.ToEntityArray(Allocator.TempJob);
      var bannerTeams = bannerQuery.ToComponentDataArray<Team>(Allocator.TempJob);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
      var delayedECB = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

      // NOTE: The only reason two different ECBs are used here is the development process of trying to limit parallelism when implementing some new systems
      // What needs to happen here is that both should be combined and a final requirement should be settled on for when exactly these commands should be played
      // back in order to guarantee that subsequent systems such as the SpeculativeSpawnSystem and the RedundantSpawnCleanupSystem see them when next running

      Entities
      .WithName("Predict_Player_Input")
      .WithAll<NetworkPlayer, PlayerInput>()
      .ForEach((Entity playerEntity, in Translation position, in Rotation rotation, in PlayerState playerState, in Team team, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost, in GhostOwnerComponent ghostOwner) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost)) {
          return;
        }
        
        var foundAnyInputForThisTick = inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input);
        var foundInputForExactlyThisTick = foundAnyInputForThisTick && input.Tick == predictingTick; // InputBuffer not guaranteed to actually have PlayerInput for exactly this tick
        var direction = float3(input.horizontal, 0, input.vertical);
        var abilities = playerAbilities[playerEntity];
        var newPlayerState = new PlayerState();
        var up = float3(0, 1, 0);

        newPlayerState.IsMoving = input.horizontal != 0 || input.vertical != 0;
        newPlayerState.FireballCooldown = max(playerState.FireballCooldown - dt, 0);

        // Discrete actions so insist on having exact data for the predictedTick
        if (foundInputForExactlyThisTick) {
          if (input.didFire > 0 && newPlayerState.FireballCooldown <= 0) {
            var fireball = SpawnGhostEntity(fireballPrefabEntity, delayedECB, isServer, ghostOwner, input.Tick, input.Tick);

            delayedECB.SetComponent(fireball, rotation);
            delayedECB.SetComponent(fireball, (Heading)rotation.Value);
            delayedECB.SetComponent(fireball, (position.Value + forward(rotation.Value) + up).ToTranslation());

            if (!isServer) {
              var spawnSoundEntity = SpawnSpeculativeEntity(speculativeTestPrefabEntity, ecb, playerEntity, input.Tick, input.Tick);
            }

            newPlayerState.DidFireball = true;
            newPlayerState.FireballCooldown = 1f; // TODO: hard-coded here for a moment because I am tired. This should either go back to the generic cooldown system or be a parameter
          } else if (input.didBanner != 0) {
            for (int i = 0; i < bannerTeams.Length; i++) {
              if (bannerTeams[i].Value == team.Value) {
                ecb.SetComponent(banners[i], position.Value.ToTranslation());
                newPlayerState.DidBanner = true;
              }
            }
          }
        }
        ecb.SetComponent(playerEntity, newPlayerState);
        ecb.SetComponent(playerEntity, (maxMoveSpeed * dt * direction + position.Value).ToTranslation());
        if (newPlayerState.IsMoving) {
          ecb.SetComponent(playerEntity, new Rotation { Value = (quaternion)(Quaternion.LookRotation(direction, up)) });
        }
      })
      .WithReadOnly(playerAbilities)
      .WithReadOnly(cooldowns)
      .WithDisposeOnCompletion(banners)
      .WithDisposeOnCompletion(bannerTeams)
      .WithoutBurst()
      .Run();

      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }
}