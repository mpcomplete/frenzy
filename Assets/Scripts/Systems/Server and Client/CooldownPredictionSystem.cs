﻿using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using static Unity.Mathematics.math;

#if false
[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
[UpdateBefore(typeof(PlayerInputPredictionSystem))]
public class CooldownPredictionSystem : SystemBase {
  protected override void OnUpdate() {
    var dt = Time.DeltaTime;

    Entities
    .WithName("Update_Cooldowns")
    .ForEach((Entity e, ref Cooldown cooldown, ref CooldownStatus cooldownStatus) => {
      switch (cooldownStatus.Value) {
      case CooldownStatus.Status.Active:
        cooldown.TimeRemaining = max(0, cooldown.TimeRemaining - dt);
        cooldownStatus.Value = cooldown.TimeRemaining <= 0 ? CooldownStatus.Status.JustElapsed : cooldownStatus.Value;
        break;

      case CooldownStatus.Status.JustElapsed:
        cooldownStatus.Value = CooldownStatus.Status.Elapsed;
        break;

      case CooldownStatus.Status.JustActive:
        cooldown.TimeRemaining = max(0, cooldown.TimeRemaining - dt);
        cooldownStatus.Value = cooldown.TimeRemaining <= 0 ? CooldownStatus.Status.JustElapsed : CooldownStatus.Status.Active;
        break;

      case CooldownStatus.Status.Elapsed:
        break;
      }
    })
    .WithoutBurst()
    .Run();
  }
}
#endif