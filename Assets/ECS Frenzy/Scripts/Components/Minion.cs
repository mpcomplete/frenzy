using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
using UnityEngine;
using Unity.Physics.Systems;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Minion : IComponentData {
    public enum Behavior { Idle, OnStanchion, Fighting }
    public Behavior CurrentBehavior;
  }

  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class MinionAISystem : ComponentSystem {
    BuildPhysicsWorld physicsWorldSystem;
    const float MinionAggroRange = 5f;  // TODO: move

    protected override void OnCreate() {
      physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
    }

    protected override void OnDestroy() {
      Hits.Dispose();
    }

    protected override void OnUpdate() {
      var query = Entities.WithAll<Stanchion, Team>().ToEntityQuery();
      using (var teams = query.ToComponentDataArray<Team>(Allocator.TempJob))
      using (var stanchions = query.ToEntityArray(Allocator.TempJob)) {
        Entities.ForEach((Entity e, ref Minion minion, ref Team team, ref LocalToWorld transform) => {
          switch (minion.CurrentBehavior) {
          case Minion.Behavior.Idle:
          case Minion.Behavior.OnStanchion:
            Entity target = FindTarget(transform.Position, team);
            if (target != Entity.Null) {
              EntityManager.SetComponentData<Target>(e, new Target { Value = target });
              minion.CurrentBehavior = Minion.Behavior.Fighting;
            } else if (minion.CurrentBehavior == Minion.Behavior.Idle) {
              for (int i = 0; i < teams.Length; i++) {
                if (teams[i].Value == team.Value)
                  target = stanchions[i];
              }
              EntityManager.SetComponentData<Target>(e, new Target { Value = target });
              minion.CurrentBehavior = Minion.Behavior.OnStanchion;
            }
            break;
          case Minion.Behavior.Fighting:
            break;
          }
        });
      }
    }

    NativeList<DistanceHit> Hits = new NativeList<DistanceHit>(Allocator.Persistent);
    Entity FindTarget(float3 pos, in Team team) {
      (float distsq, Entity e) closest = (float.MaxValue, Entity.Null);
      var fromPoint = new PointDistanceInput() { Filter = CollisionFilter.Default, Position = pos, MaxDistance = MinionAggroRange };
      if (physicsWorldSystem.PhysicsWorld.CollisionWorld.CalculateDistance(fromPoint, ref Hits)) {
        foreach (var hit in Hits) {
          Entity e = hit.Entity;
          float distsq = math.distancesq(hit.Position, pos);
          if (EntityManager.HasComponent<Minion>(e) &&
              EntityManager.GetComponentData<Team>(e).Value != team.Value &&
              distsq < closest.distsq) {
            closest = (distsq, e);
          }
        }
      }
      Hits.Clear();
      return closest.e;
    }
  }
}
