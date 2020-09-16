using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using ECSFrenzy.MonoBehaviors;
using static Unity.Mathematics.math;

namespace ECSFrenzy.Networking {
  public static class Utils {
    public static Entity FindGhostPrefab<T>(EntityManager entityManager, DynamicBuffer<GhostPrefabBuffer> prefabs) {
      var prefab = Entity.Null; 
      for (int ghostId = 0; ghostId < prefabs.Length; ghostId++) {
        if (entityManager.HasComponent<T>(prefabs[ghostId].Value)) {
          prefab = prefabs[ghostId].Value;
        }
      }
      return prefab;
    }

    public static Entity FindGhostPrefab<T>(EntityManager entityManager, NativeArray<GhostPrefabBuffer> prefabs) {
      var prefab = Entity.Null; 
      for (int ghostId = 0; ghostId < prefabs.Length; ghostId++) {
        if (entityManager.HasComponent<T>(prefabs[ghostId].Value)) {
          prefab = prefabs[ghostId].Value;
        }
      }
      return prefab;
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

    public void Deserialize(uint tick, ref DataStreamReader reader) {
      this.tick = tick;
      horizontal = reader.ReadFloat();
      vertical = reader.ReadFloat();
      didFire = reader.ReadInt();
    }

    public void Serialize(ref DataStreamWriter writer) {
      writer.WriteFloat(horizontal);
      writer.WriteFloat(vertical);
      writer.WriteInt(didFire);
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
        DynamicBuffer<GhostPrefabBuffer> serverPrefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(GetSingleton<GhostPrefabCollectionComponent>().serverPrefabs);
        Entity playerPrefabEntity = Utils.FindGhostPrefab<NetworkPlayer>(EntityManager, serverPrefabs);
        Entity player = EntityManager.Instantiate(playerPrefabEntity);

        UnityEngine.Debug.Log($"Server setting connection {networkId} to in game");
        PostUpdateCommands.AddBuffer<PlayerInput>(player);
        PostUpdateCommands.SetComponent(player, new GhostOwnerComponent { NetworkId = networkId });
        PostUpdateCommands.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent { targetEntity = player });
        PostUpdateCommands.DestroyEntity(requestEntity);
        PostUpdateCommands.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);
      });
    }
  }

  public class SendPlayerInputCommandSystem : CommandSendSystem<PlayerInput> {}

  public class ReceivePlayerInputCommandSystem : CommandReceiveSystem<PlayerInput> {}

  [UpdateInGroup(typeof(ClientSimulationSystemGroup))]
  public class SamplePlayerInput : ComponentSystem {
    protected override void OnCreate() {
      RequireSingletonForUpdate<NetworkIdComponent>();
    }

    protected override void OnUpdate() {
      Entity localInputEntity = GetSingleton<CommandTargetComponent>().targetEntity;

      if (localInputEntity == Entity.Null) {
        var localPlayerId = GetSingleton<NetworkIdComponent>().Value;

        Entities
        .WithAll<NetworkPlayer>()
        .WithNone<PlayerInput>()
        .ForEach((Entity ent, ref GhostOwnerComponent ghostOwner) => {
          if (ghostOwner.NetworkId == localPlayerId) {
            var e = GetSingletonEntity<CommandTargetComponent>();
            var ctc = new CommandTargetComponent { targetEntity = ent };

            PostUpdateCommands.AddBuffer<PlayerInput>(ent);
            PostUpdateCommands.SetComponent(e, ctc);
          }
        });
        return;
      }

      DynamicBuffer<PlayerInput> playerInputs = EntityManager.GetBuffer<PlayerInput>(localInputEntity);
      float2 stickInput = float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
      int didFire = Input.GetButtonDown("Fire1") ? 1 : 0;

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
        didFire = didFire
      };

      playerInputs.AddCommandData(input);
    }
  }
}