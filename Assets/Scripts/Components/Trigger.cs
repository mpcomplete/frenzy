using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;

[Serializable]
[GenerateAuthoringComponent]
public struct Trigger : IComponentData {
  public enum TriggerState { JustEntered, Entered, JustTriggered, JustExited, Exited }
  public TriggerState State;
  [GhostField] public float TimeRemaining;
  [GhostField] public int Cost;
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class TriggerSystem : JobComponentSystem {
  [BurstCompile]
  public struct TriggerJob : ITriggerEventsJob {
    [ReadOnly] public ComponentDataFromEntity<NetworkPlayer> Players;
    [ReadOnly] public ComponentDataFromEntity<Trigger> Triggers;
    public NativeList<Entity> ActiveTriggers;
    public void Execute(TriggerEvent e) {
      if (Triggers.HasComponent(e.EntityA) && Players.HasComponent(e.EntityB))
        ActiveTriggers.Add(e.EntityA);
      if (Players.HasComponent(e.EntityA) && Triggers.HasComponent(e.EntityB))
        ActiveTriggers.Add(e.EntityB);
    }
  }

  BuildPhysicsWorld buildPhysicsWorld;
  StepPhysicsWorld stepPhysicsWorld;
  protected override void OnCreate() {
    buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
    stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
  }

  protected override JobHandle OnUpdate(JobHandle inputDeps) {
    var activeTriggers = new NativeList<Entity>(Allocator.TempJob);

    var deps = inputDeps;
    deps = new TriggerJob {
      Players = GetComponentDataFromEntity<NetworkPlayer>(true),
      Triggers = GetComponentDataFromEntity<Trigger>(true),
      ActiveTriggers = activeTriggers,
    }.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld, deps);

    float dt = Time.DeltaTime;
    deps = Entities
    .WithReadOnly(activeTriggers)
    .WithDisposeOnCompletion(activeTriggers)
    .ForEach((Entity e, ref Trigger trigger) => {
      bool triggered = activeTriggers.Contains(e);

      switch (trigger.State) {
      case Trigger.TriggerState.Entered:
      case Trigger.TriggerState.JustEntered:
        if (triggered) {
          trigger.State = Trigger.TriggerState.Entered;
          trigger.TimeRemaining -= dt;

          if (trigger.TimeRemaining <= 0) {
            trigger.State = Trigger.TriggerState.JustTriggered;
          }
        } else {
          trigger.State = Trigger.TriggerState.JustExited;
        }
        break;
      case Trigger.TriggerState.JustTriggered:
        trigger.State = Trigger.TriggerState.JustExited;
        break;
      case Trigger.TriggerState.Exited:
      case Trigger.TriggerState.JustExited:
        if (triggered) {
          trigger.State = Trigger.TriggerState.JustEntered;
          trigger.TimeRemaining = (float)1f;
        } else {
          trigger.State = Trigger.TriggerState.Exited;
        }
        break;
      }
    }).Schedule(deps);

    return deps;
  }
}