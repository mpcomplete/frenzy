using Unity.Entities;
using Unity.Collections;
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
    Entity TestSpeculativePrefabEntity;
    BeginSimulationEntityCommandBufferSystem BeginSimulationEntityCommandBufferSystem;
    GhostPredictionSystemGroup GhostPredictionSystemGroup;
    bool IsServer;

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

    static Entity SpawnGhostEntity(Entity prefab, EntityCommandBuffer.ParallelWriter entityCommandBuffer, int nativeThreadIndex, bool onServer, in GhostOwnerComponent ghostOwnerComponent, uint tick, uint identifier) {
      var entity = entityCommandBuffer.Instantiate(nativeThreadIndex, prefab);
      
      entityCommandBuffer.SetComponent(nativeThreadIndex, entity, ghostOwnerComponent);
      if (!onServer) {
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, new RedundantSpawnComponent(tick, identifier));
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, new PredictedGhostSpawnRequestComponent());
      }
      return entity;
    }

    static Entity SpawnSpeculativeEntity(Entity prefab, EntityCommandBuffer.ParallelWriter entityCommandBuffer, int nativeThreadIndex, Entity owner, uint tick, uint identifier) {
      var entity = entityCommandBuffer.Instantiate(nativeThreadIndex, prefab);

      entityCommandBuffer.SetComponent(nativeThreadIndex, entity, new SpeculativeSpawn(owner, entity, tick, identifier));
      entityCommandBuffer.SetComponent<NewSpeculativeSpawnTag>(nativeThreadIndex, entity, new NewSpeculativeSpawnTag());
      return entity;
    }

    protected override void OnCreate() {
      BeginSimulationEntityCommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
      IsServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
    }

    protected override void OnUpdate() {
      if (FireballPrefabEntity == Entity.Null) {
        var ghostPrefabCollectionEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
        var ghostPrefabs = GetBuffer<GhostPrefabBuffer>(ghostPrefabCollectionEntity);
        var ghostPrefab = FindGhostPrefab(ghostPrefabs, e => EntityManager.HasComponent<NetworkFireball>(e));

        FireballPrefabEntity = GhostCollectionSystem.CreatePredictedSpawnPrefab(EntityManager, ghostPrefab);
        EntityManager.SetName(FireballPrefabEntity, "Predicted Fireball Prefab");
        TestSpeculativePrefabEntity = CreateSpeculativeSpawnPrefab(SystemConfig.Instance.SpeculativeSpawnTestPrefab, EntityManager, IsServer);
        EntityManager.SetName(TestSpeculativePrefabEntity, "Test Speculative Spawn Prefab");
      }

      var isServer = IsServer;
      var dt = Time.DeltaTime;
      var predictingTick = GhostPredictionSystemGroup.PredictingTick;
      var maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;
      var fireballPrefabEntity = FireballPrefabEntity;
      var speculativeTestPrefabEntity = TestSpeculativePrefabEntity;
      var bannerQuery = GetEntityQuery(typeof(Banner), typeof(Team));
      var banners = bannerQuery.ToEntityArray(Allocator.TempJob);
      var bannerTeams = bannerQuery.ToComponentDataArray<Team>(Allocator.TempJob);
      var teams = GetComponentDataFromEntity<Team>(true);
      var ghostOwners = GetComponentDataFromEntity<GhostOwnerComponent>(true);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var inputBuffers = GetBufferFromEntity<PlayerInput>(true);
      var beginSimECB = BeginSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

      Entities
      .WithName("Predict_Player_Input")
      .WithAll<NetworkPlayer, PlayerInput>()
      .WithAll<Team, GhostOwnerComponent>()
      .WithAll<PredictedGhostComponent, PlayerInput>()
      .ForEach((Entity playerEntity, int nativeThreadIndex, ref Translation position, ref Rotation rotation, ref PlayerState playerState) => {
        var team = teams[playerEntity];
        var predictedGhost = predictedGhosts[playerEntity];
        var ghostOwner = ghostOwners[playerEntity];
        var inputBuffer = inputBuffers[playerEntity];

        // Don't re-run prediction if no new data has arrived from the server for this entity since it was last predicted
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost)) {
          return;
        }

        // Only run prediction with playerinputs from exactly the currently-predicting tick
        if (!(inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input) && input.Tick == predictingTick)) {
          if (isServer) {
            UnityEngine.Debug.Log($"<color=white>Did NOT run server-side prediction because only available input had tick {input.Tick} while the predicting tick is {predictingTick}</color>");
          }
          return;
        } else {
          // if (isServer) {
          //   UnityEngine.Debug.Log($"<color=white>Did run server-side prediction input had tick {input.Tick} matching predicting tick is {predictingTick} and DidFire={input.didFire}</color>");
          // }
        }

        var direction = float3(input.horizontal, 0, input.vertical);
        var isMoving = input.horizontal != 0 || input.vertical != 0;
        var up = float3(0, 1, 0);

        position.Value = maxMoveSpeed * dt * direction + position.Value;
        rotation.Value = isMoving ? (quaternion)(Quaternion.LookRotation(direction, up)) : rotation.Value;
        playerState.FireballCooldownTimeRemaining = max(playerState.FireballCooldownTimeRemaining - dt, 0);
        playerState.IsMoving = isMoving;
        playerState.DidFireball = false;
        playerState.DidBanner = false;

        if (input.didFire == 1 && playerState.FireballCooldownTimeRemaining <= 0) {
          var fireball = SpawnGhostEntity(fireballPrefabEntity, beginSimECB, nativeThreadIndex, isServer, ghostOwner, input.Tick, input.Tick);

          beginSimECB.SetComponent(nativeThreadIndex, fireball, rotation);
          beginSimECB.SetComponent(nativeThreadIndex, fireball, (Heading)rotation.Value);
          beginSimECB.SetComponent(nativeThreadIndex, fireball, (position.Value + forward(rotation.Value) + up).ToTranslation());

          if (isServer) {
            UnityEngine.Debug.Log($"<color=white>Predictive spawn on input.Tick {input.Tick}</color>");
          } else {
            UnityEngine.Debug.Log($"Predictive spawn on input.Tick {input.Tick}");
          }

          if (!isServer) {
            var spawnSoundEntity = SpawnSpeculativeEntity(speculativeTestPrefabEntity, beginSimECB, nativeThreadIndex, playerEntity, input.Tick, input.Tick);

            UnityEngine.Debug.Log($"Speculative spawn on input.Tick {input.Tick}");
          }

          playerState.DidFireball = true;
          playerState.FireballCooldownTimeRemaining = playerState.FireballCooldownDuration;
        } else if (input.didBanner == 1) {
          for (int i = 0; i < bannerTeams.Length; i++) {
            if (bannerTeams[i].Value == team.Value) {
              // TODO: I think the banner itself needs to be owner-predicted and a ghost for this prediction to work properly
              beginSimECB.SetComponent(nativeThreadIndex, banners[i], position.Value.ToTranslation());
              playerState.DidBanner = true;
            }
          }
        }
      })
      .WithReadOnly(teams)
      .WithReadOnly(ghostOwners)
      .WithReadOnly(predictedGhosts)
      .WithReadOnly(inputBuffers)
      .WithDisposeOnCompletion(banners)
      .WithDisposeOnCompletion(bannerTeams)
      .WithBurst()
      .ScheduleParallel();
      BeginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}