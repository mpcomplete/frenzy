using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.AllPredicted)]
  public struct UniqueClientsideSpawn : IComponentData {
    public Entity OwnerEntity;
    public uint SpawnTick;
    public uint Identifier;

    public static bool Same(UniqueClientsideSpawn a, UniqueClientsideSpawn b) {
      return a.SpawnTick == b.SpawnTick && a.Identifier == b.Identifier;
    }

    public UniqueClientsideSpawn(Entity ownerEntity, uint spawnTick, uint identifier) {
      OwnerEntity = ownerEntity;
      SpawnTick = spawnTick;
      Identifier = identifier;
    }
  }
}