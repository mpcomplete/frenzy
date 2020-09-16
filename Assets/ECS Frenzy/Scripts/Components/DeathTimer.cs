using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.Server)]
  public struct DeathTimer : IComponentData {
    public float TimeRemaining;
  }
}