using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  public struct Cooldown : IComponentData {
    [GhostField] public float Duration;
    [GhostField] public float TimeRemaining;
  }
}