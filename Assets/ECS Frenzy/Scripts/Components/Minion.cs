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
    public enum Behavior { Idle, OnBanner, Fighting }
    public Behavior CurrentBehavior;
  }

  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class MinionAISystem : SystemBase {
    BeginSimulationEntityCommandBufferSystem commandBufferSystem;
    BuildPhysicsWorld physicsWorldSystem;
    const float MinionAggroRange = 5f;  // TODO: move

    protected override void OnCreate() {
      commandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
    }

    protected override void OnUpdate() {
      var ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
      var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
      var query = GetEntityQuery(typeof(Banner), typeof(Team));
      var banners = query.ToEntityArray(Allocator.TempJob);
      var bannerTeams = query.ToComponentDataArray<Team>(Allocator.TempJob);
      Entities
        .WithDisposeOnCompletion(banners)
        .WithDisposeOnCompletion(bannerTeams)
        .ForEach((Entity e, int nativeThreadIndex, ref Minion minion, ref Team team, ref LocalToWorld transform) => {
        switch (minion.CurrentBehavior) {
        case Minion.Behavior.Idle:
        case Minion.Behavior.OnBanner:
          Entity target = FindTarget(collisionWorld, transform.Position, team);
          if (target != Entity.Null) {
            ecb.SetComponent(nativeThreadIndex, e, new Target { Value = target });
            minion.CurrentBehavior = Minion.Behavior.Fighting;
          } else if (minion.CurrentBehavior == Minion.Behavior.Idle) {
            for (int i = 0; i < bannerTeams.Length; i++) {
              if (bannerTeams[i].Value == team.Value)
                target = banners[i];
            }
            ecb.SetComponent(nativeThreadIndex, e, new Target { Value = target });
            minion.CurrentBehavior = Minion.Behavior.OnBanner;
          }
          break;
        case Minion.Behavior.Fighting:
          break;
        }
      }).ScheduleParallel();
    }

    static Entity FindTarget(in CollisionWorld collisionWorld, float3 pos, in Team team) {
      (float distsq, Entity e) closest = (float.MaxValue, Entity.Null);
      var hits = new NativeList<DistanceHit>(8, Allocator.Temp);
      // Collide with opposite team.
      uint layer = team.Value == 0 ? CollisionLayer.Team2 : CollisionLayer.Team1;
      var fromPoint = new PointDistanceInput() { Filter = new CollisionFilter { BelongsTo = layer, CollidesWith = layer }, Position = pos, MaxDistance = MinionAggroRange };
      if (collisionWorld.CalculateDistance(fromPoint, ref hits)) {
        for (int i = 0; i < hits.Length; i++) {
          var hit = hits[i];  // TODO: isn't random-access on a list slow?
          Entity e = hit.Entity;
          float distsq = math.distancesq(hit.Position, pos);
          //Debug.Assert(EntityManager.HasComponent<Team>(e), $"Minion target collided invalid object: {EntityManager.GetName(e)}.");
          //Debug.Assert(EntityManager.GetComponentData<Team>(e).Value != team.Value, "Minion target collided with own team. CollisionFilters are incorrect.");
          if (distsq < closest.distsq) {
            closest = (distsq, e);
          }
        }
      }
      if (hits.IsCreated)
        hits.Dispose();
      return closest.e;
    }
  }
}
