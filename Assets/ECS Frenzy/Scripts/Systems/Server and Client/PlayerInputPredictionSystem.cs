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

    static Entity SpeculativeTestPrefab(World world, GameObject prefab) {
      var blobAssetStore = new BlobAssetStore(); 
      var conversionFlags = GameObjectConversionUtility.ConversionFlags.AssignName;
      var conversionSettings = new GameObjectConversionSettings(world, conversionFlags, blobAssetStore);
      var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, conversionSettings);

      blobAssetStore.Dispose();
      return entity;
    }

    static float3 DirectionFromInput(in PlayerInput input) => float3(input.horizontal, 0, input.vertical);

    static float3 PositionDelta(in float3 direction, in float3 speed, in float dt) => dt * speed * direction;

    protected override void OnCreate() {
      CommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate() {
      var isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;

      if (FireballPrefabEntity == Entity.Null) {
        var ghostPrefabCollectionEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
        var ghostPrefabs = GetBuffer<GhostPrefabBuffer>(ghostPrefabCollectionEntity);

        FireballPrefabEntity = GhostCollectionSystem.CreatePredictedSpawnPrefab(EntityManager, FindGhostPrefab(ghostPrefabs, e => EntityManager.HasComponent<NetworkFireball>(e)));

        if (!isServer) {
          TestSpeculativePrefabEntity = SpeculativeTestPrefab(World, SystemConfig.Instance.SpeculativeSpawnTestPrefab);
        }
      }

      var dt = Time.DeltaTime;
      var predictingTick = GhostPredictionSystemGroup.PredictingTick;
      var maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;
      var fireballPrefabEntity = FireballPrefabEntity;
      var speculativeTestPrefabEntity = TestSpeculativePrefabEntity;
      var playerAbilities = GetComponentDataFromEntity<PlayerAbilites>(true);
      var cooldowns = GetComponentDataFromEntity<Cooldown>(true);
      var query = GetEntityQuery(typeof(Banner), typeof(Team));
      var banners = query.ToEntityArray(Allocator.TempJob);
      var bannerTeams = query.ToComponentDataArray<Team>(Allocator.TempJob);
      var speculativeSpawnEntity = GetSingletonEntity<SpeculativeBuffers>();
      var speculativeBuffers = GetComponentDataFromEntity<SpeculativeBuffers>();
      var speculativeSpawnBuffers = GetBufferFromEntity<SpeculativeSpawnBufferEntry>(false);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
      var delayedECB = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();

      Entities
      .WithName("Predict_Player_Input")
      .WithReadOnly(playerAbilities)
      .WithReadOnly(cooldowns)
      .WithAll<NetworkPlayer, PlayerInput>()
      .ForEach((Entity playerEntity, in Translation position, in Rotation rotation, in PlayerState playerState, in Team team, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost, in GhostOwnerComponent ghostOwner) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost)) {
          return;
        }
        
        var foundAnyInputForThisTick = inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input);
        var foundInputForExactlyThisTick = foundAnyInputForThisTick && input.Tick == predictingTick; // InputBuffer not guaranteed to actually have PlayerInput for exactly this tick
        var newPlayerState = new PlayerState();
        var direction = DirectionFromInput(input);
        var speculativeSpawnBuffer = speculativeSpawnBuffers[speculativeSpawnEntity];
        var abilities = playerAbilities[playerEntity];

        newPlayerState.IsMoving = input.horizontal != 0 || input.vertical != 0;
        newPlayerState.FireballCooldown = max(playerState.FireballCooldown - dt, 0);

        // Discrete actions so insist on having exact data for the predictedTick
        if (foundInputForExactlyThisTick) {
          if (input.didFire > 0 && newPlayerState.FireballCooldown <= 0) {
            var spawnPosition = position.Value + forward(rotation.Value) + float3(0,1,0);
            var fireball = delayedECB.Instantiate(fireballPrefabEntity);

            newPlayerState.DidFireball = true;
            newPlayerState.FireballCooldown = 1f; // TODO: hard-coded here for a moment because I am tired. This should either go back to the generic cooldown system or be a parameter
            delayedECB.SetComponent(fireball, ghostOwner);
            delayedECB.SetComponent(fireball, rotation);
            delayedECB.SetComponent(fireball, (Heading)rotation.Value);
            delayedECB.SetComponent(fireball, spawnPosition.ToTranslation());

            if (!isServer) {
              delayedECB.SetComponent(fireball, new RedundantSpawnComponent(input.Tick));
            }

            if (!isServer) {
              var speculativeEntity = ecb.Instantiate(speculativeTestPrefabEntity);
              var speculativeSpawnBufferEntry = new SpeculativeSpawnBufferEntry { 
                OwnerEntity = playerEntity,
                Entity = speculativeEntity, 
                SpawnTick = input.Tick, 
                Identifier = input.Tick 
              };

              ecb.AppendToBuffer(speculativeSpawnEntity, speculativeSpawnBufferEntry);
            }
          } else if (input.didBanner != 0) {
            var playerTeam = team.Value;
            var playerPos = position.Value;

            for (int i = 0; i < bannerTeams.Length; i++) {
              if (bannerTeams[i].Value == playerTeam) {
                newPlayerState.DidBanner = true;
                ecb.SetComponent(banners[i], playerPos.ToTranslation());
              }
            }
          }
        }
        ecb.SetComponent(playerEntity, newPlayerState);
        ecb.SetComponent(playerEntity, (position.Value + PositionDelta(direction, maxMoveSpeed, dt)).ToTranslation());
        ecb.SetComponent(playerEntity, new Rotation { 
          Value = newPlayerState.IsMoving ? (quaternion)Quaternion.LookRotation(direction, float3(0,1,0)) : rotation.Value 
        });
      })
      .WithoutBurst()
      .Run();

      ecb.Playback(EntityManager);
      ecb.Dispose();
      banners.Dispose();
      bannerTeams.Dispose();
    }
  }
}