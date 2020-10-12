using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
public class PlayerInputPredictionSystem : SystemBase {
  Entity FireballPrefabEntity;
  Entity ChanneledBeamPrefabEntity;
  Entity TestClientOnlyPrefabEntity;
  BeginSimulationEntityCommandBufferSystem BeginSimulationEntityCommandBufferSystem;
  GhostPredictionSystemGroup GhostPredictionSystemGroup;
  bool loadedAllPrefabs;
  bool IsServer;

  static void LogServer(bool onServer, string message) {
    if (!onServer)
      return;

    UnityEngine.Debug.Log($"<color=white>{message}</color>");
  }

  static void LogClient(bool onServer, string message) {
    if (onServer)
      return;

    UnityEngine.Debug.Log($"<color=grey>{message}</color>");
  }

  static Entity CreateClientOnlyEntityPrefab(GameObject prefab, EntityManager entityManager, bool onServer) {
    if (!onServer) {
      var blobAssetStore = new BlobAssetStore();
      var conversionFlags = GameObjectConversionUtility.ConversionFlags.AssignName;
      var conversionSettings = new GameObjectConversionSettings(entityManager.World, conversionFlags, blobAssetStore);
      var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, conversionSettings);

      entityManager.AddComponent<Prefab>(entity);
      blobAssetStore.Dispose();
      return entity;
    } else {
      return Entity.Null;
    }
  }

  static bool SpawnPredictedGhostEntity(
  out Entity entity,
  NativeArray<Entity> existingClientsideEntities,
  NativeArray<UniqueClientsideSpawn> existingClientsideSpawns,
  Entity prefab,
  Entity owner,
  EntityCommandBuffer.ParallelWriter entityCommandBuffer,
  int nativeThreadIndex,
  bool onServer,
  in GhostOwnerComponent ghostOwnerComponent,
  uint tick,
  uint identifier) {
    if (onServer) {
      entity = entityCommandBuffer.Instantiate(nativeThreadIndex, prefab);
      entityCommandBuffer.SetComponent(nativeThreadIndex, entity, ghostOwnerComponent);
      return true;
    } else {
      var numberOfExistingEntities = existingClientsideEntities.Length;
      var clientsideSpawn = new UniqueClientsideSpawn(owner, tick, identifier);
      var existingEntity = Entity.Null;

      for (int i = 0; i < numberOfExistingEntities; i++) {
        if (UniqueClientsideSpawn.Same(clientsideSpawn, existingClientsideSpawns[i])) {
          existingEntity = existingClientsideEntities[i];
          break;
        }
      }

      if (existingEntity == Entity.Null) {
        entity = entityCommandBuffer.Instantiate(nativeThreadIndex, prefab);
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, ghostOwnerComponent);
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, default(PredictedGhostSpawnRequestComponent));
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, clientsideSpawn);
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, new RepredictedClientsideSpawn { Value = true });
        return true;
      } else {
        entity = existingEntity;
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, clientsideSpawn);
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, new RepredictedClientsideSpawn { Value = true });
        return false;
      }
    }
  }

  static bool SpawnClientOnlyEntity(
  out Entity entity,
  NativeArray<Entity> existingClientsideEntities,
  NativeArray<UniqueClientsideSpawn> existingClientsideSpawns,
  Entity prefab,
  Entity owner,
  EntityCommandBuffer.ParallelWriter entityCommandBuffer,
  int nativeThreadIndex,
  bool onServer,
  uint tick,
  uint identifier) {
    if (onServer) {
      UnityEngine.Debug.LogError($"You should NOT call SpawnClientOnlyEntity from the server");
      entity = Entity.Null;
      return false;
    } else {
      var numberOfExistingEntities = existingClientsideEntities.Length;
      var clientsideSpawn = new UniqueClientsideSpawn(owner, tick, identifier);
      var existingEntity = Entity.Null;

      for (int i = 0; i < numberOfExistingEntities; i++) {
        if (UniqueClientsideSpawn.Same(clientsideSpawn, existingClientsideSpawns[i])) {
          existingEntity = existingClientsideEntities[i];
          break;
        }
      }

      if (existingEntity == Entity.Null) {
        entity = entityCommandBuffer.Instantiate(nativeThreadIndex, prefab);
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, clientsideSpawn);
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, new RepredictedClientsideSpawn { Value = true });
        return true;
      } else {
        entity = existingEntity;
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, clientsideSpawn);
        entityCommandBuffer.SetComponent(nativeThreadIndex, entity, new RepredictedClientsideSpawn { Value = true });
        return false;
      }
    }
  }

  static void DestroyPredictedGhostEntity(Entity entity, EntityCommandBuffer.ParallelWriter entityCommandBuffer, int nativeThreadIndex) {
    entityCommandBuffer.DestroyEntity(nativeThreadIndex, entity);
  }

  protected override void OnCreate() {
    BeginSimulationEntityCommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
    IsServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
  }

  protected override void OnUpdate() {
    if (!loadedAllPrefabs) {
      var ghostPrefabCollectionEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
      var ghostPrefabs = GetBuffer<GhostPrefabBuffer>(ghostPrefabCollectionEntity);
      var fireballGhostPrefab = Utils.FindGhostPrefab(ghostPrefabs, e => EntityManager.HasComponent<NetworkFireball>(e));
      var channeledGhostPrefab = Utils.FindGhostPrefab(ghostPrefabs, e => EntityManager.HasComponent<ChanneledBeam>(e));

      FireballPrefabEntity = GhostCollectionSystem.CreatePredictedSpawnPrefab(EntityManager, fireballGhostPrefab);
      EntityManager.SetName(FireballPrefabEntity, "THE FIREBALL PREFAB");
      ChanneledBeamPrefabEntity = GhostCollectionSystem.CreatePredictedSpawnPrefab(EntityManager, channeledGhostPrefab);
      TestClientOnlyPrefabEntity = CreateClientOnlyEntityPrefab(SystemConfig.Instance.SpeculativeSpawnTestPrefab, EntityManager, IsServer);
      loadedAllPrefabs = true;
    }

    var isServer = IsServer;
    var dt = Time.DeltaTime;
    var predictingTick = GhostPredictionSystemGroup.PredictingTick;
    var maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;
    var fireballPrefabEntity = FireballPrefabEntity;
    var channeledBeamPrefabEntity = ChanneledBeamPrefabEntity;
    var clientOnlyPrefabEntity = TestClientOnlyPrefabEntity;
    var clientsideSpawnQuery = GetEntityQuery(typeof(UniqueClientsideSpawn), typeof(RepredictedClientsideSpawn));
    var existingClientsideEntities = clientsideSpawnQuery.ToEntityArray(Allocator.TempJob);
    var existingClientsideSpeculativeSpawns = clientsideSpawnQuery.ToComponentDataArray<UniqueClientsideSpawn>(Allocator.TempJob);
    var bannerQuery = GetEntityQuery(typeof(Banner), typeof(Team));
    var banners = bannerQuery.ToEntityArray(Allocator.TempJob);
    var bannerTeams = bannerQuery.ToComponentDataArray<Team>(Allocator.TempJob);
    var teams = GetComponentDataFromEntity<Team>(true);
    var channeledBeamAbilities = GetComponentDataFromEntity<ChanneledBeamAbility>(true);
    var ghostOwners = GetComponentDataFromEntity<GhostOwnerComponent>(true);
    var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
    var inputBuffers = GetBufferFromEntity<PlayerInput>(true);
    var beginSimECB = BeginSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

    Entities
    .WithName("Predict_Player_Input")
    .WithNone<Prefab>()
    .WithAll<NetworkPlayer, PlayerInput>()
    .WithAll<Team, GhostOwnerComponent>()
    .WithAll<PredictedGhostComponent, PlayerInput>()
    .WithDisposeOnCompletion(existingClientsideEntities)
    .WithDisposeOnCompletion(existingClientsideSpeculativeSpawns)
    .ForEach((Entity playerEntity, int nativeThreadIndex, ref Translation position, ref Rotation rotation, ref PlayerState playerState) => {
      var team = teams[playerEntity];
      var predictedGhost = predictedGhosts[playerEntity];
      var ghostOwner = ghostOwners[playerEntity];
      var inputBuffer = inputBuffers[playerEntity];
      var channeledBeamAbility = channeledBeamAbilities[playerEntity];

        // Don't re-run prediction if no new data has arrived from the server for this entity since it was last predicted
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost)) {
        return;
      }

        // Only run prediction with playerinputs from exactly the currently-predicting tick
        if (!(inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input) && input.Tick == predictingTick)) {
          // LogServer(isServer, $"Did NOT run server-side prediction because only available input had tick {input.Tick} while the predicting tick is {predictingTick}");
          return;
      } else {
          // LogServer(isServer, $"Did run server-side prediction because available input had tick {input.Tick} matching the predicting tick {predictingTick}");
        }

      var direction = float3(input.horizontal, 0, input.vertical);
      var isTryingToMove = input.horizontal != 0 || input.vertical != 0;
      var isTryingToFire = input.didFire > 0;
      var isTryingToBanner = input.didBanner > 0;
      var isTryingToChannel = input.isChanneling > 0;
      var up = float3(0, 1, 0);

        // Always update cooldowns... probably should not even be happening here honestly...
        playerState.FireballCooldownTimeRemaining = max(playerState.FireballCooldownTimeRemaining - dt, 0);

      if (isTryingToChannel && !playerState.IsChanneling) {
        if (SpawnPredictedGhostEntity(out Entity channeledBeam, existingClientsideEntities, existingClientsideSpeculativeSpawns, channeledBeamPrefabEntity, playerEntity, beginSimECB, nativeThreadIndex, isServer, ghostOwner, input.Tick, input.Tick)) {
          beginSimECB.SetComponent(nativeThreadIndex, playerEntity, new ChanneledBeamAbility { ChanneledBeam = channeledBeam });
        }

        playerState.IsMoving = false;
        playerState.DidBanner = false;
        playerState.DidFireball = false;
        playerState.IsChanneling = true;
        LogServer(isServer, $"Start channel beam");
      } else if (isTryingToChannel && playerState.IsChanneling) {
        playerState.IsMoving = false;
        playerState.DidBanner = false;
        playerState.DidFireball = false;
        playerState.IsChanneling = true;
        LogServer(isServer, $"Update channel beam");
      } else if (!isTryingToChannel && playerState.IsChanneling) {
        playerState.IsMoving = false;
        playerState.DidBanner = false;
        playerState.DidFireball = false;
        playerState.IsChanneling = false;

        DestroyPredictedGhostEntity(channeledBeamAbility.ChanneledBeam, beginSimECB, nativeThreadIndex);
        LogServer(isServer, $"Stop channel beam");
      } else if (isTryingToFire && playerState.FireballCooldownTimeRemaining <= 0) {
        if (SpawnPredictedGhostEntity(out Entity fireball, existingClientsideEntities, existingClientsideSpeculativeSpawns, fireballPrefabEntity, playerEntity, beginSimECB, nativeThreadIndex, isServer, ghostOwner, input.Tick, input.Tick * 10)) {
          beginSimECB.SetComponent(nativeThreadIndex, fireball, rotation);
          beginSimECB.SetComponent(nativeThreadIndex, fireball, (Heading)rotation.Value);
          beginSimECB.SetComponent(nativeThreadIndex, fireball, (position.Value + forward(rotation.Value) + up).ToTranslation());
        }

        if (!isServer) {
          SpawnClientOnlyEntity(out Entity sound, existingClientsideEntities, existingClientsideSpeculativeSpawns, clientOnlyPrefabEntity, playerEntity, beginSimECB, nativeThreadIndex, isServer, input.Tick, input.Tick);
        }

        playerState.IsMoving = false;
        playerState.DidBanner = false;
        playerState.IsChanneling = false;
        playerState.DidFireball = true;
        playerState.FireballCooldownTimeRemaining = playerState.FireballCooldownDuration;
      } else if (isTryingToBanner) {
        for (int i = 0; i < bannerTeams.Length; i++) {
          if (bannerTeams[i].Value == team.Value) {
            beginSimECB.SetComponent(nativeThreadIndex, banners[i], position.Value.ToTranslation());
            playerState.IsMoving = false;
            playerState.DidBanner = false;
            playerState.IsChanneling = false;
            playerState.DidFireball = false;
            playerState.DidBanner = true;
          }
        }

      } else if (isTryingToMove) {
        playerState.IsMoving = false;
        playerState.DidBanner = false;
        playerState.IsChanneling = false;
        playerState.DidFireball = false;
        playerState.IsMoving = true;
        position.Value = maxMoveSpeed * dt * direction + position.Value;
        rotation.Value = (quaternion)Quaternion.LookRotation(direction, up);
      } else {
        playerState.IsMoving = false;
        playerState.DidBanner = false;
        playerState.IsChanneling = false;
        playerState.DidFireball = false;
        playerState.IsMoving = false;
      }
    })
    .WithReadOnly(teams)
    .WithReadOnly(channeledBeamAbilities)
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
