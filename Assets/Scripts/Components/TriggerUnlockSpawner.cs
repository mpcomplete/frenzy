using System;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
public struct TriggerUnlockSpawner : IComponentData {
  public Entity Spawner;
  public Entity MinionPrefab;
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateAfter(typeof(TriggerSystem))]
public class TriggerUnlockSpawnerSystem : ComponentSystem {
  protected override void OnUpdate() {
    Entities
    .ForEach((Entity e, ref Trigger trigger, ref TriggerUnlockSpawner unlock) => {
      if (trigger.State != Trigger.TriggerState.JustTriggered)
        return;
      EntityManager.SetEnabled(unlock.Spawner, true);
    });
  }
}