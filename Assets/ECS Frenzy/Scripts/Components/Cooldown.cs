using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  public struct Cooldown : IComponentData {
    [GhostField] public float Duration;
    [GhostField] public float TimeRemaining;

    public static Cooldown Reset(Cooldown cd) => new Cooldown { Duration = cd.Duration, TimeRemaining = cd.Duration };
  }
}