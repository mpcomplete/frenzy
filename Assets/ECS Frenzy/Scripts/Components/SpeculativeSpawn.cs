using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct SpeculativeSpawn : IComponentData {
    public int Identifier;
    public int SpawnTick;
    public SpeculativeSpawn(int identifier, int spawnTick) {
      Identifier = identifier;
      SpawnTick = spawnTick;
    }
  }
}