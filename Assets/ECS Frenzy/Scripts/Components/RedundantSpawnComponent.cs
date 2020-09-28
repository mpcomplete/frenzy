using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.PredictedClient)]
  public struct RedundantSpawnComponent : IComponentData, IEquatable<RedundantSpawnComponent> {
    public uint SimulatedSpawnTick;
    public uint Identifier;

    public RedundantSpawnComponent(uint simulatedSpawnTick, uint identifier) {
      SimulatedSpawnTick = simulatedSpawnTick;
      Identifier = identifier;
    }

    public RedundantSpawnComponent(uint simulatedSpawnTick) {
      SimulatedSpawnTick = simulatedSpawnTick;
      Identifier = simulatedSpawnTick;
    }

    public bool Equals(RedundantSpawnComponent rc) {
      return SimulatedSpawnTick == rc.SimulatedSpawnTick && Identifier == rc.Identifier;
    }
  }
}