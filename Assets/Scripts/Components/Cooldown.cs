using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.All)]
  public struct Cooldown : IComponentData {
    [GhostField] public float Duration;
    [GhostField] public float TimeRemaining;

    public static Cooldown Reset(Cooldown cd) => new Cooldown { Duration = cd.Duration, TimeRemaining = cd.Duration };
  }
}