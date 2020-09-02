using System;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class NetworkConfiguration {
  public const ushort NETWORK_PORT = 7979;
}

public struct Disconnected : IComponentData {}

public struct JoinGameRequest : IRpcCommand {}

public struct PlayerInput : ICommandData<PlayerInput> {
  public uint Tick => tick;
  public uint tick;
  public int horizontal;
  public int vertical;

  public void Deserialize(uint tick, ref DataStreamReader reader) {
    this.tick = tick;
    horizontal = reader.ReadInt();
    vertical = reader.ReadInt();
  }

  public void Serialize(ref DataStreamWriter writer) {
    writer.WriteInt(horizontal);
    writer.WriteInt(vertical);
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
        UnityEngine.Debug.Log($"World does not contain either client or server simululation system group");
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
    NativeArray<Entity> entities = Entities.WithAll<NetworkPlayer>().ToEntityQuery().ToEntityArray(Allocator.TempJob);

    Debug.Log(entities);

    entities.Dispose();

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
  Entity FindGhost<T>() {
    var prefab = Entity.Null; 
    var ghostCollection = GetSingleton<GhostPrefabCollectionComponent>();
    var serverPrefabs = EntityManager.GetBuffer<GhostPrefabBuffer>(ghostCollection.serverPrefabs);
    for (int ghostId = 0; ghostId < serverPrefabs.Length; ghostId++) {
      if (EntityManager.HasComponent<T>(serverPrefabs[ghostId].Value)) {
        prefab = serverPrefabs[ghostId].Value;
      }
    }
    return prefab;
  }

  protected override void OnUpdate() {
    Entities
    .WithNone<SendRpcCommandRequestComponent>()
    .ForEach((Entity requestEntity, ref JoinGameRequest joinGameRequest, ref ReceiveRpcCommandRequestComponent reqSrc) => {
      int networkId = EntityManager.GetComponentData<NetworkIdComponent>(reqSrc.SourceConnection).Value;
      Entity prefab = FindGhost<NetworkPlayer>();
      Entity player = EntityManager.Instantiate(prefab);

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
    PlayerInput input = new PlayerInput {
      tick = World.GetExistingSystem<ClientSimulationSystemGroup>().ServerTick,
      horizontal = (Input.GetKey("a") ? -1 : 0) + (Input.GetKey("d") ? 1 : 0),
      vertical = (Input.GetKey("s") ? -1 : 0) + (Input.GetKey("w") ? 1 : 0),
    };

    playerInputs.AddCommandData(input);
  }
}

[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
public class MovePlayer : ComponentSystem {
  protected override void OnUpdate() {
    var group = World.GetExistingSystem<GhostPredictionSystemGroup>();
    var tick = group.PredictingTick;
    var dt = group.Time.DeltaTime;

    Entities
    .ForEach((DynamicBuffer<PlayerInput> inputBuffer, ref NetworkPlayer player, ref Translation translation, ref Rotation rotation, ref PredictedGhostComponent predictedGhost) => {
      if (!GhostPredictionSystemGroup.ShouldPredict(tick, predictedGhost))
        return;

      inputBuffer.GetDataAtTick(tick, out PlayerInput input);

      if (input.horizontal == 0 && input.vertical == 0)
        return;

      float3 direction = normalize(float3(input.horizontal, 0, input.vertical));
      float3 velocity = direction * dt * player.speed;

      translation.Value += velocity;
      rotation.Value = Quaternion.LookRotation(direction, float3(0, 1, 0));

      Debug.Log($"PlayerInput predicted at tick {tick}");
    });
  }
}