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
  public struct PlayerInput : ICommandData<PlayerInput> {
    public uint Tick => tick;
    public uint tick;
    public float horizontal;
    public float vertical;
    public int didFire;
    public int didBanner;

    public void Deserialize(uint tick, ref DataStreamReader reader) {
      this.tick = tick;
      horizontal = reader.ReadFloat();
      vertical = reader.ReadFloat();
      didFire = reader.ReadInt();
      didBanner = reader.ReadInt();
    }

    public void Serialize(ref DataStreamWriter writer) {
      writer.WriteFloat(horizontal);
      writer.WriteFloat(vertical);
      writer.WriteInt(didFire);
      writer.WriteInt(didBanner);
    }

    public void Deserialize(uint tick, ref DataStreamReader reader, PlayerInput baseline, NetworkCompressionModel compressionModel) {
      Deserialize(tick, ref reader);
    }

    public void Serialize(ref DataStreamWriter writer, PlayerInput baseline, NetworkCompressionModel compressionModel) {
      Serialize(ref writer);
    }
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
    EntityArchetype playAudioArchetype;

    protected override void OnCreate() {
      joinGameArchetype = EntityManager.CreateArchetype(new ComponentType[] { 
        typeof(JoinGameRequest),
        typeof(SendRpcCommandRequestComponent)
      });

      playAudioArchetype = EntityManager.CreateArchetype(new ComponentType[] {
        typeof(PlayAudio)
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
        DynamicBuffer<GhostPrefabBuffer> serverPrefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(GetSingleton<GhostPrefabCollectionComponent>().serverPrefabs);
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

  public class SendPlayerInputCommandSystem : CommandSendSystem<PlayerInput> {}

  public class ReceivePlayerInputCommandSystem : CommandReceiveSystem<PlayerInput> {}

  [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
  [UpdateBefore(typeof(SendPlayerInputCommandSystem))]
  [UpdateBefore(typeof(GhostSimulationSystemGroup))]
  public class SamplePlayerInput : SystemBase {
    protected override void OnCreate() {
      RequireSingletonForUpdate<NetworkIdComponent>();
    }

    protected override void OnUpdate() {
      Entity localInputEntity = GetSingleton<CommandTargetComponent>().targetEntity;
      BeginSimulationEntityCommandBufferSystem barrier = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();

      if (localInputEntity == Entity.Null) {
        int localPlayerId = GetSingleton<NetworkIdComponent>().Value;
        EntityCommandBuffer.ParallelWriter ecb = barrier.CreateCommandBuffer().AsParallelWriter();
        Entity commandTargetEntity = GetSingletonEntity<CommandTargetComponent>();

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
        barrier.AddJobHandleForProducer(Dependency);
      } else {
        DynamicBuffer<PlayerInput> playerInputs = EntityManager.GetBuffer<PlayerInput>(localInputEntity);
        float2 stickInput = float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        int didFire = Input.GetButtonDown("Fire1") ? 1 : 0;
        int didBanner = Input.GetButtonDown("Jump") ? 1 : 0;

        if (length(stickInput) < SystemConfig.Instance.ControllerDeadzone) {
          stickInput = float2(0,0);
        } else {
          stickInput = normalize(stickInput);
        }

        uint tick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick;

        PlayerInput input = new PlayerInput {
          tick = tick,
          horizontal = stickInput.x,
          vertical = stickInput.y,
          didFire = didFire,
          didBanner = didBanner
        };

        Entities
        .WithAll<NetworkPlayer, PlayerInput>()
        .ForEach((Entity e, ref GhostOwnerComponent ghostOwner) => {
          playerInputs.AddCommandData(input);
        })
        .WithoutBurst()
        .WithStructuralChanges()
        .Run();
      }
    }
  }
}