using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.PredictedClient)]
  public struct SpeculativeSpawn : IComponentData {
    public Entity OwnerEntity;
    public Entity Entity;
    public uint SpawnTick;
    public uint Identifier;

    public static bool Same(SpeculativeSpawn a, SpeculativeSpawn b) {
      return a.SpawnTick == b.SpawnTick && a.Identifier == b.Identifier;
    }

    public SpeculativeSpawn(Entity ownerEntity, Entity entity, uint spawnTick, uint identifier) {
      OwnerEntity = ownerEntity;
      Entity = entity;
      SpawnTick = spawnTick;
      Identifier = identifier;
    }
  }
}