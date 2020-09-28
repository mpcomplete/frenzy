﻿using Unity.Entities;
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
          // Debug.Log($"Did not predict {predictingTick}");
          return;
        }
        
        // Only run this code for predictionTicks that are actually predicted! ShouldPredict clause returning false means this tick was not actually re-simulated!
        // May be a better way to do this but this SHOULD record the oldest predicted tick each frame for use in SpeculativeSpawnSystem
        // NOTE: It's worth noting that this data is really capturing which ticks were re-simulated for THIS player ghost. there could have been
        // other ticks simulated for other ghost entities that are predicted by other systems. As such, it's not clear if this is the correct
        // place for this to be recorded... maybe this should be a component on the actual player prefab?

        if (!isServer) {
          var specBuffers = speculativeBuffers[speculativeSpawnEntity];
          var oldestTickSimulated = min(specBuffers.OldestTickSimulated, predictingTick);
          var newestTickSimulated = max(specBuffers.NewestTickSimulated, predictingTick);

          ecb.SetComponent(speculativeSpawnEntity, new SpeculativeBuffers(oldestTickSimulated, newestTickSimulated));
        }

        // Important: We are willing to use data related to some button presses to predict motion that is continuous even when we do not have
        // any Input data for EXACTLY the current tick we are predicting. However, we do not want to process discrete events if no input data
        // actually existed for the exact predicting tick we are currently simulating. Thus, we check to see if we foundInputForExactlyThisTick
        // when considering whether to process inputs associated with discrete events... This feels like a giant can of fuck.
        // Additionally, this raises a NEW question: When we "AddCommandData" to the inputs list and the targeted ServerTick happens
        // to already exist in the command data is it possible that their helper method "AddCommandData" could potentially overrite
        // an input action of pushing a button with a "newer" InputData that does not have that button pushed. This would extremely suck
        // because it would mean that the client would appear to swallow input actions occasionally which would drive players and myself 
        // crazy and lead to Armageddon.

        var foundAnyInputForThisTick = inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input);
        var foundInputForExactlyThisTick = foundAnyInputForThisTick && input.Tick == predictingTick;
        var isMoving = input.horizontal != 0 || input.vertical != 0;
        var didFireball = false;
        var didBanner = false;
        var fireballCooldown = max(playerState.FireballCooldown - dt, 0);
        var direction = DirectionFromInput(input);
        var speculativeSpawnBuffer = speculativeSpawnBuffers[speculativeSpawnEntity];
        var abilities = playerAbilities[playerEntity];

        // Discrete actions so insist on having exact data for the predictedTick
        if (foundInputForExactlyThisTick) {
          if (input.didFire > 0 && fireballCooldown <= 0) {
            didFireball = true;
            fireballCooldown = 1f; // TODO: hard-coded here for a moment because I am tired. This should either go back to the generic cooldown system or be a parameter
            var spawnPosition = position.Value + forward(rotation.Value) + float3(0,1,0);
            var fireball = delayedECB.Instantiate(fireballPrefabEntity);

            if (!isServer) {
              delayedECB.SetComponent(fireball, new RedundantSpawnComponent { SimulatedSpawnTick = input.Tick, Identifier = input.Tick });
            }
            delayedECB.SetComponent(fireball, ghostOwner);
            delayedECB.SetComponent(fireball, rotation);
            delayedECB.SetComponent(fireball, (Heading)rotation.Value);
            delayedECB.SetComponent(fireball, spawnPosition.ToTranslation());
            if (!isServer) {
              var speculativeEntity = ecb.Instantiate(speculativeTestPrefabEntity);
              var speculativeSpawnBufferEntry = new SpeculativeSpawnBufferEntry { Entity = speculativeEntity, SpawnTick = input.Tick, Identifier = input.Tick };

              ecb.AppendToBuffer(speculativeSpawnEntity, speculativeSpawnBufferEntry);
            }
          }
          //} else if (input.didBanner != 0) {
          //  var playerTeam = team.Value;
          //  var playerPos = position.Value;
          //  for (int i = 0; i < bannerTeams.Length; i++) {
          //    if (bannerTeams[i].Value == playerTeam) {
          //      EntityManager.SetComponentData(banners[i], playerPos.ToTranslation());
          //      didBanner = true;
          //    }
          //  }
          //}
        }
        ecb.SetComponent(playerEntity, (position.Value + PositionDelta(direction, maxMoveSpeed, dt)).ToTranslation());
        ecb.SetComponent(playerEntity, new Rotation { 
          Value = isMoving ? (quaternion)Quaternion.LookRotation(direction, float3(0,1,0)) : rotation.Value 
        });
        ecb.SetComponent(playerEntity, new PlayerState { 
          FireballCooldown = fireballCooldown,
          IsMoving = isMoving, 
          DidFireball = didFireball, 
          DidBanner = didBanner 
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