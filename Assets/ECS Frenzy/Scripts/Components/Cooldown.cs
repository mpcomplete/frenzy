using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  public struct Cooldown : IComponentData {
    [GhostField] public float Duration;
    [GhostField] public float TimeRemaining;

    public static void Activate(EntityCommandBuffer.ParallelWriter ecb, Entity e, int nativeThreadIndex, Cooldown cd) {
      ecb.SetComponent<Cooldown>(nativeThreadIndex, e, new Cooldown { Duration = cd.Duration, TimeRemaining = cd.Duration });
      ecb.SetSharedComponent<SharedCooldownStatus>(nativeThreadIndex, e, SharedCooldownStatus.JustActive);
    }

    public static void Reset(EntityCommandBuffer.ParallelWriter ecb, Entity e, int nativeThreadIndex, Cooldown cd) {
      ecb.SetComponent<Cooldown>(nativeThreadIndex, e, new Cooldown { Duration = cd.Duration, TimeRemaining = 0 });
      ecb.SetSharedComponent<SharedCooldownStatus>(nativeThreadIndex, e, SharedCooldownStatus.JustElapsed);
    }
  }
}