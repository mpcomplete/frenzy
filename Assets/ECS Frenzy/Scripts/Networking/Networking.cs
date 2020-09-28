using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace ECSFrenzy {
  public static class Utils {
    public static Entity FindGhostPrefab(DynamicBuffer<GhostPrefabBuffer> prefabs, System.Predicate<Entity> predicate) {
      for (int ghostId = 0; ghostId < prefabs.Length; ghostId++) {
        if (predicate(prefabs[ghostId].Value)) {
          return prefabs[ghostId].Value;
        }
      }
      return Entity.Null;
    }
  }

  public static class NetworkConfiguration {
    public const ushort NETWORK_PORT = 8787;
  }

  public struct Disconnected : IComponentData {}

  public struct JoinGameRequest : IRpcCommand {}

  [GhostComponent(PrefabType=GhostPrefabType.AllPredicted)]
  public struct PlayerInput : ICommandData {
    public uint Tick {get; set;}
    public float horizontal;
    public float vertical;
    public int didFire;
    public int didBanner;
  }

  public class FrenzyNetCodeBootstrap : ClientServerBootstrap {
    public override bool Initialize(string defaultWorldName) {
      UnityEngine.Debug.Log($"FrenzyNetCodeBootstrap Initialize with {defaultWorldName}");
      return base.Initialize(defaultWorldName);
    }
  }

  [UpdateInWorld(UpdateInWorld.TargetWorld.Default)]
  public class EstablishConnection : ComponentSystem {
    protected override void OnCreate() {
      EntityManager.CreateEntity(typeof(Disconnected));
      RequireSingletonForUpdate<Disconnected>();
    }

    protected override void OnUpdate() {
      EntityManager.DestroyEntity(GetSingletonEntity<Disconnected>());

      foreach (var world in World.All) {
        var network = world.GetExistingSystem<NetworkStreamReceiveSystem>(); 
        var clientSimGroup = world.GetExistingSystem<ClientSimulationSystemGroup>();
        var serverSimGroup = world.GetExistingSystem<ServerSimulationSystemGroup>();
        var port = NetworkConfiguration.NETWORK_PORT;

        if (clientSimGroup != null) {
          NetworkEndPoint endPoint;

          #if UNITY_EDITOR
          endPoint = NetworkEndPoint.Parse(ClientServerBootstrap.RequestedAutoConnect, port);
          #else
          endPoint = NetworkEndPoint.LoopbackIpv4;
          endPoint.Port = port;
          #endif

          network.Connect(endPoint);
          UnityEngine.Debug.Log($"Client connecting on port {port}");
        }
        #if UNITY_EDITOR || UNITY_SERVER
        else if (serverSimGroup != null) {
          NetworkEndPoint endPoint = NetworkEndPoint.LoopbackIpv4;

          endPoint.Port = port;
          network.Listen(endPoint);
          UnityEngine.Debug.Log($"Server listening on port {port}");
        }
        #endif
        else {
          // UnityEngine.Debug.Log($"World does not contain either client or server simululation system group");
        }
      }
    }
  }


  [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
  public class HandleRPCClient : ComponentSystem {
    EntityArchetype joinGameArchetype;

    protected override void OnCreate() {
      joinGameArchetype = EntityManager.CreateArchetype(new ComponentType[] { 
        typeof(JoinGameRequest),
        typeof(SendRpcCommandRequestComponent)
      });
    }

    protected override void OnUpdate() {
      Entities
      .WithNone<NetworkStreamInGame>()
      .ForEach((Entity entity, ref NetworkIdComponent id) => {
        Entity requestEntity = PostUpdateCommands.CreateEntity(joinGameArchetype);

        PostUpdateCommands.SetComponent(requestEntity, new SendRpcCommandRequestComponent { TargetConnection = entity });
        PostUpdateCommands.AddComponent<NetworkStreamInGame>(entity);
      });
    }
  }

  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class HandleRPCServer : ComponentSystem {
    protected override void OnUpdate() {
      Entities
      .WithNone<SendRpcCommandRequestComponent>()
      .ForEach((Entity requestEntity, ref JoinGameRequest joinGameRequest, ref ReceiveRpcCommandRequestComponent reqSrc) => {
        int networkId = EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value;
        Entity ghostPrefabCollectionEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
        DynamicBuffer<GhostPrefabBuffer> serverPrefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabCollectionEntity);
        Entity playerPrefab = Utils.FindGhostPrefab(serverPrefabs, e => EntityManager.HasComponent<NetworkPlayer>(e));
        Entity bannerPrefab = Utils.FindGhostPrefab(serverPrefabs, e => EntityManager.HasComponent<Banner>(e));
        Entity player = EntityManager.Instantiate(playerPrefab);

        Debug.Log($"Server setting connection {networkId} to in game");
        PostUpdateCommands.AddBuffer<PlayerInput>(player);
        PostUpdateCommands.SetComponent(player, new GhostOwnerComponent { NetworkId = networkId });
        PostUpdateCommands.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent { targetEntity = player });
        PostUpdateCommands.DestroyEntity(requestEntity);
        PostUpdateCommands.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);

        Entity banner = EntityManager.Instantiate(bannerPrefab);
        PostUpdateCommands.SetComponent(banner, new GhostOwnerComponent { NetworkId = networkId });
      });
    }
  }

  [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
  [UpdateBefore(typeof(CommandSendSystemGroup))]
  [UpdateBefore(typeof(GhostSimulationSystemGroup))]
  public class SamplePlayerInput : SystemBase {
    ClientSimulationSystemGroup ClientSimulationSystemGroup;
    BeginSimulationEntityCommandBufferSystem BeginSimulationEntityCommandBufferSystem;

    protected override void OnCreate() {
      ClientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
      BeginSimulationEntityCommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      RequireSingletonForUpdate<NetworkIdComponent>();
    }

    protected override void OnUpdate() {
      var localInputEntity = GetSingleton<CommandTargetComponent>().targetEntity;
      var estimatedServerTick = ClientSimulationSystemGroup.ServerTick;

      if (localInputEntity == Entity.Null) {
        var localPlayerId = GetSingleton<NetworkIdComponent>().Value;
        var commandTargetEntity = GetSingletonEntity<CommandTargetComponent>();
        var ecb = BeginSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        Entities
        .WithAll<NetworkPlayer>()
        .WithNone<PlayerInput>()
        .ForEach((Entity ent, int nativeThreadIndex, ref GhostOwnerComponent ghostOwner) => {
          if (ghostOwner.NetworkId == localPlayerId) {
            var ctc = new CommandTargetComponent { targetEntity = ent };

            ecb.AddBuffer<PlayerInput>(nativeThreadIndex, ent);
            ecb.SetComponent(nativeThreadIndex, commandTargetEntity, ctc);
          }
        })
        .ScheduleParallel();
        BeginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
      } else {
        var playerInputs = EntityManager.GetBuffer<PlayerInput>(localInputEntity);
        var stickInput = float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        int didFire = Input.GetButtonDown("Fire1") ? 1 : 0;
        int didBanner = Input.GetButtonDown("Jump") ? 1 : 0;

        if (length(stickInput) < SystemConfig.Instance.ControllerDeadzone) {
          stickInput = float2(0,0);
        } else {
          stickInput = normalize(stickInput);
        }

        Entities
        .WithAll<NetworkPlayer, PlayerInput>()
        .ForEach((Entity e, ref GhostOwnerComponent ghostOwner) => {
          PlayerInput input = new PlayerInput {
            Tick = estimatedServerTick,
            horizontal = stickInput.x,
            vertical = stickInput.y,
            didFire = didFire,
            didBanner = didBanner
          };

          playerInputs.AddCommandData(input);
        })
        .WithoutBurst()
        .WithStructuralChanges()
        .Run();
      }
    }
  }
}