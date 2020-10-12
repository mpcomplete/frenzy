using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.AllPredicted)]
  public struct UniqueClientsideSpawn : IComponentData {
    [GhostField] public Entity OwnerEntity;
    [GhostField] public uint SpawnTick;
    [GhostField] public uint Identifier;

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