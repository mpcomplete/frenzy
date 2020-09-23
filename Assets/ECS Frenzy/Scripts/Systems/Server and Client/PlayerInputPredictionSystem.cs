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
  [UpdateAfter(typeof(SpeculativeSpawnSystem))]
  public class PlayerInputPredictionSystem : SystemBase {
    Entity FireballPrefabEntity;
    Entity FireballAbilityPrefabEntity;
    Entity TestSpeculativePrefabEntity;
    BeginSimulationEntityCommandBufferSystem CommandBufferSystem;
    GhostPredictionSystemGroup GhostPredictionSystemGroup;
    int FireballNameHash;

    static Entity PredictedClientPrefab<T>(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) where T : IComponentData {
      bool matches(Entity e) => entityManager.HasComponent<T>(e) && entityManager.HasComponent<PredictedGhostSpawnRequestComponent>(e);
      DynamicBuffer<GhostPrefabBuffer> clientPredictedPrefabs = entityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabs.clientPredictedPrefabs);

      return FindGhostPrefab(clientPredictedPrefabs, matches);
    }

    static Entity ServerPrefab<T>(EntityManager entityManager, GhostPrefabCollectionComponent ghostPrefabs) where T : IComponentData {
      bool matches(Entity e) => entityManager.HasComponent<T>(e);
      DynamicBuffer<GhostPrefabBuffer> serverPrefabs = entityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabs.serverPrefabs);

      return FindGhostPrefab(serverPrefabs, matches);
    }

    static Entity SpeculativeTestPrefab(World world, GameObject prefab) {
      var blobAssetStore = new BlobAssetStore(); 
      var conversionFlags = GameObjectConversionUtility.ConversionFlags.AssignName;
      var conversionSettings = new GameObjectConversionSettings(world, conversionFlags, blobAssetStore);
      var entity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, conversionSettings);

      blobAssetStore.Dispose();
      return entity;
    }

    static float MoveSpeedFromInput(in PlayerInput input) => (input.horizontal == 0 && input.vertical == 0) ? 0 : 1;

    static float3 DirectionFromInput(in PlayerInput input) => float3(input.horizontal, 0, input.vertical);

    static float3 Velocity(in float3 direction, in float3 speed, in float dt) => dt * speed * direction;

    protected override void OnCreate() {
      CommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      GhostPredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
      FireballNameHash = Animator.StringToHash("Fireball");
    }

    protected override void OnUpdate() {
      var isServer = World.GetExistingSystem<ServerSimulationSystemGroup>() != null;
      var dt = Time.DeltaTime;
      var predictingTick = GhostPredictionSystemGroup.PredictingTick;
      var maxMoveSpeed = SystemConfig.Instance.PlayerMoveSpeed;

      if (FireballPrefabEntity == Entity.Null) {
        var ghostPrefabs = GetSingleton<GhostPrefabCollectionComponent>();

        if (isServer) {
          FireballPrefabEntity = ServerPrefab<NetworkFireball>(EntityManager, ghostPrefabs);
          FireballAbilityPrefabEntity = ServerPrefab<FireballAbility>(EntityManager, ghostPrefabs);
        } else {
          FireballPrefabEntity = PredictedClientPrefab<NetworkFireball>(EntityManager, ghostPrefabs);
          FireballAbilityPrefabEntity = PredictedClientPrefab<FireballAbility>(EntityManager, ghostPrefabs);
          TestSpeculativePrefabEntity = SpeculativeTestPrefab(World, SystemConfig.Instance.SpeculativeSpawnTestPrefab);
        }
      }

      var fireballPrefabEntity = FireballPrefabEntity;
      var fireballAbilityPrefabEntity = FireballAbilityPrefabEntity;
      var speculativeTestPrefabEntity = TestSpeculativePrefabEntity;
      var playerAbilities = GetComponentDataFromEntity<PlayerAbilites>(true);
      var cooldowns = GetComponentDataFromEntity<Cooldown>(true);
      var query = GetEntityQuery(typeof(Banner), typeof(Team));
      var banners = query.ToEntityArray(Allocator.TempJob);
      var bannerTeams = query.ToComponentDataArray<Team>(Allocator.TempJob);

      Entities
      .WithName("Predict_Player_Input")
      .WithReadOnly(playerAbilities)
      .WithReadOnly(cooldowns)
      .WithAll<NetworkPlayer, PlayerInput>()
      .ForEach((Entity entity, ref Translation position, ref Rotation rotation, ref MoveSpeed moveSpeed, in Team team, in DynamicBuffer<PlayerInput> inputBuffer, in PredictedGhostComponent predictedGhost, in GhostOwnerComponent ghostOwner) => {
        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost))
          return;

        inputBuffer.GetDataAtTick(predictingTick, out PlayerInput input);

        var speed = MoveSpeedFromInput(input);
        var direction = DirectionFromInput(input);
        var velocity = Velocity(direction, maxMoveSpeed, dt);

        moveSpeed.Value = speed;
        position.Value += velocity;
        rotation.Value = (speed > 0) ? (quaternion)Quaternion.LookRotation(direction, float3(0, 1, 0)) : rotation.Value;

        var abilities = playerAbilities[entity];
        var fireballCooldown = cooldowns[abilities.Ability1];

        if (input.didFire != 0 && fireballCooldown.TimeRemaining <= 0) {
          if (isServer) {
            var spawnPosition = position.Value + forward(rotation.Value) + float3(0,1,0);
            var fireball = EntityManager.Instantiate(fireballPrefabEntity);

            EntityManager.SetComponentData(fireball, ghostOwner);
            EntityManager.SetComponentData(fireball, rotation);
            EntityManager.SetComponentData(fireball, (Heading)rotation.Value);
            EntityManager.SetComponentData(fireball, spawnPosition.ToTranslation());
            EntityManager.SetComponentData<Cooldown>(abilities.Ability1, Cooldown.Reset(fireballCooldown));
            EntityManager.SetSharedComponentData<SharedCooldownStatus>(abilities.Ability1, SharedCooldownStatus.JustActive);
          } else {
            var speculativeEntity = EntityManager.Instantiate(speculativeTestPrefabEntity);
            var speculativeSpawn = new SpeculativeSpawn { SpawnTick = (int)input.Tick, Identifier = 0 };
            var prefabAudioSourceShit = EntityManager.GetComponentData<AudioSourceWrapper>(speculativeEntity);

            EntityManager.SetComponentData(speculativeEntity, new AudioSourceWrapper { Source = AudioSource.Instantiate(prefabAudioSourceShit.Source) });
            EntityManager.SetComponentData<SpeculativeSpawn>(speculativeEntity, speculativeSpawn);
          }
        }
        if (input.didBanner != 0) {
          var playerTeam = team.Value;
          var playerPos = position.Value;
          for (int i = 0; i < bannerTeams.Length; i++) {
            if (bannerTeams[i].Value == playerTeam) {
              EntityManager.SetComponentData(banners[i], playerPos.ToTranslation());
            }
          }
        }
      })
      .WithStructuralChanges()
      .WithoutBurst() // TODO: This is a known bug where burst and shared components don't play nicely together... totally idiotic
      .Run();

      banners.Dispose();
      bannerTeams.Dispose();
    }
  }
}