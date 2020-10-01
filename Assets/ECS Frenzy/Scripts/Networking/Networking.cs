using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;
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
    public byte didFire;
    public byte didBanner;
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
    protected override void OnUpdate() {
      Entities
      .WithNone<NetworkStreamInGame>()
      .ForEach((Entity entity, ref NetworkIdComponent id) => {
        var requestEntity = PostUpdateCommands.CreateEntity();

        PostUpdateCommands.AddComponent<NetworkStreamInGame>(entity);
        PostUpdateCommands.AddComponent<JoinGameRequest>(requestEntity);
        PostUpdateCommands.AddComponent<SendRpcCommandRequestComponent>(requestEntity);
        PostUpdateCommands.SetComponent(requestEntity, new SendRpcCommandRequestComponent { TargetConnection = entity });
      });
    }
  }

  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class HandleRPCServer : ComponentSystem {
    protected override void OnUpdate() {
      Entities
      .WithNone<SendRpcCommandRequestComponent>()
      .ForEach((Entity requestEntity, ref JoinGameRequest joinGameRequest, ref ReceiveRpcCommandRequestComponent reqSrc) => {
        var networkId = EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value;
        var ghostPrefabCollectionEntity = GetSingletonEntity<GhostPrefabCollectionComponent>();
        var serverPrefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostPrefabCollectionEntity);
        var playerPrefab = Utils.FindGhostPrefab(serverPrefabs, e => EntityManager.HasComponent<NetworkPlayer>(e));
        var bannerPrefab = Utils.FindGhostPrefab(serverPrefabs, e => EntityManager.HasComponent<Banner>(e));
        var player = EntityManager.Instantiate(playerPrefab);

        PostUpdateCommands.AddBuffer<PlayerInput>(player);
        PostUpdateCommands.SetComponent(player, new GhostOwnerComponent { NetworkId = networkId });
        PostUpdateCommands.SetComponent(reqSrc.SourceConnection, new CommandTargetComponent { targetEntity = player });
        PostUpdateCommands.DestroyEntity(requestEntity);
        PostUpdateCommands.AddComponent<NetworkStreamInGame>(reqSrc.SourceConnection);

        var banner = EntityManager.Instantiate(bannerPrefab);
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

    static float2 StickInputsWithRadialDeadzone(float horizontal, float vertical, float deadzone) {
      var stickInput = float2(horizontal, vertical);
      var lengthInput = length(stickInput);

      return lengthInput < deadzone ? float2(0, 0) : stickInput / lengthInput;
    }

    static void AddNewPlayerInputWithDiscreteStateFusion(DynamicBuffer<PlayerInput> inputs, PlayerInput input) {
      inputs.GetDataAtTick(input.Tick, out PlayerInput currentInput);

      if (currentInput.Tick == input.Tick) {
        if (input.didFire != currentInput.didFire) {
          Debug.LogError($"Found tick already existing for estimated server tick {input.Tick} with DidFire conflict!");
        }
        if (input.didFire != currentInput.didFire) {
          Debug.LogError($"Found tick already existing for estimated server tick {input.Tick} with DidBanner conflict!");
        }
        input.didFire = (input.didFire == 1 || currentInput.didFire == 1) ? (byte)1 : (byte)0;
        input.didBanner = (input.didBanner == 1 || currentInput.didBanner == 1) ? (byte)1 : (byte)0;
      }
      inputs.AddCommandData(input);
    }

    protected override void OnCreate() {
      ClientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
      BeginSimulationEntityCommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      RequireSingletonForUpdate<NetworkIdComponent>();
      RequireSingletonForUpdate<CommandTargetComponent>();
    }

    protected override void OnUpdate() {
      var commandTargetEntity = GetSingletonEntity<CommandTargetComponent>();
      var localInputEntity = GetSingleton<CommandTargetComponent>().targetEntity;
      var estimatedServerTick = ClientSimulationSystemGroup.ServerTick;
      var ecb = BeginSimulationEntityCommandBufferSystem.CreateCommandBuffer();

      // We don't have anything configured as our Command Target yet so try to set that up
      if (localInputEntity == Entity.Null) {
        var localPlayerId = GetSingleton<NetworkIdComponent>().Value;

        Entities
        .WithAll<NetworkPlayer>()
        .WithNone<PlayerInput>()
        .ForEach((Entity ent, int nativeThreadIndex, ref GhostOwnerComponent ghostOwner) => {
          if (ghostOwner.NetworkId == localPlayerId) {
            var ctc = new CommandTargetComponent { targetEntity = ent };

            ecb.AddBuffer<PlayerInput>(ent);
            ecb.SetComponent(commandTargetEntity, ctc);
          }
        })
        .WithoutBurst()
        .Run();
      }

      float2 stickInput = StickInputsWithRadialDeadzone(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), SystemConfig.Instance.ControllerDeadzone);
      byte didFire = Input.GetButtonDown("Fire1") ? (byte)1 : (byte)0;
      byte didBanner = Input.GetButtonDown("Jump") ? (byte)1 : (byte)0;

      Entities
      .WithAll<NetworkPlayer, GhostOwnerComponent>()
      .ForEach((Entity e, DynamicBuffer<PlayerInput> playerInputs) => {
        var input = new PlayerInput {
          Tick = estimatedServerTick,
          horizontal = stickInput.x,
          vertical = stickInput.y,
          didFire = didFire,
          didBanner = didBanner
        };
        // We will wipe out existing input button actions if we overwrite them with new data targeting the same tick
        // This will appear to swallow user input rarely which will seem like an insidious bug and make people hate us
        // As such, anytime we are adding new data to the input buffer we should check for existing data matching
        // the Tick and fuse their discrete button values such that if the button is pressed in either one then the button
        // remains pressed
        AddNewPlayerInputWithDiscreteStateFusion(playerInputs, input);
      })
      .WithBurst()
      .Schedule();
      BeginSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}